using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Events;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Movement;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// Jon's playtest: Quickstart, Science Fleet geo-surveys Venus, one-shot pause ticked on Surveyor I's
/// warp. The clock stopped partway through the warp with a plain "Paused": the Strata Survey NPC's
/// own WarpExited (default-on halt in its own event log) stopped the shared clock, and the reason was
/// filed under that NPC faction. Only the human faction may stop the clock now.
/// </summary>
[TestFixture]
[NonParallelizable]
public class EventPauseGeoSurveyTests
{
    const string BaseMod = "Pulsar4x";
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    [Test]
    public void Quickstart_fleet_geo_survey_pauses_at_the_checked_warp_drop_in_only()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == BaseMod).Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), WarpTechStart);
        var catalog = GameLifecycle.BuildCatalog(store);
        var request = GameLifecycle.BuildQuickstartRequest(paths, catalog, 12345);
        var created = GameLifecycle.CreateGameCore(store, request);
        Assert.That(created, Is.Not.Null);
        var game = created!.Value.game;
        var player = created.Value.faction;
        game.Settings.EnforceSingleThread = true;
        Assert.That(game.LocalHumanFactionId, Is.EqualTo(player.Id));

        var translator = new CommandTranslator(game);
        var projector = new GameProjector(game);
        var fleet = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<FleetDB>())
            .Single(f => f.FactionOwnerID == player.Id && f.GetDataBlob<NameDB>().GetName(player.Id) == "Science Fleet");
        var sol = (StarSystem)fleet.Manager!;
        var venus = sol.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
            .Single(e => e.GetDataBlob<NameDB>().DefaultName == "Venus");
        var surveyor = fleet.GetDataBlob<FleetDB>().Children
            .Single(s => s.GetDataBlob<NameDB>().GetName(player.Id) == "Surveyor I");

        var exits = new List<Event>();
        Action<Event> onExit = e => exits.Add(e);
        EventManager.Instance.Subscribe(EventType.WarpExited, onExit);
        try
        {
            var order = translator.Translate(player, fleet, new GeoSurveyCommand(fleet.Id, venus.Id));
            Assert.That(order.Accepted, Is.True, order.RejectionReason);

            // Advance until the fleet agent has handed Surveyor I its child orders.
            WarpMoveAction? warp = null;
            for (int i = 0; i < 48 && warp == null; i++)
            {
                Step(game, TimeSpan.FromHours(1));
                warp = surveyor.GetDataBlob<ActionQueueDB>().ActionList.OfType<WarpMoveAction>().FirstOrDefault();
            }
            Assert.That(warp, Is.Not.Null, "Surveyor I never got a warp order");
            Assert.That(warp!.IsRunning, Is.False);
            var circularise = surveyor.GetDataBlob<ActionQueueDB>().ActionList.OfType<CirculariseAction>().Single();

            // The checkbox: the projector's OrderId for that row, sent as the UI sends it.
            var row = projector.ProjectEntity(surveyor, player.Id).GetView<OrdersView>()!.Orders
                .Single(o => o.Name == "Warp Move to Venus");
            Assert.That(row.OrderId, Is.EqualTo(warp.CmdID));
            var check = translator.Translate(player, surveyor, new SetOrderPauseCommand(surveyor.Id, row.OrderId, true));
            Assert.That(check.Accepted, Is.True, check.RejectionReason);

            // Five-day ticks. Any stop before drop-in must carry a reason for the player (the bug was a
            // silent stop); the player then presses play again.
            PauseReason? reason = null;
            for (int i = 0; i < 40; i++)
            {
                var before = game.TimePulse.GameGlobalDateTime;
                Step(game, TimeSpan.FromDays(5));
                Assert.That(game.TimePulse.LastFault, Is.Null, game.TimePulse.LastFault?.Report);
                if (game.TimePulse.GameGlobalDateTime >= before + TimeSpan.FromDays(5))
                    continue;
                reason = game.TimePulse.GetPauseReason(player.Id);
                Assert.That(reason, Is.Not.Null,
                    $"clock stopped at {game.TimePulse.GameGlobalDateTime:o} with no reason for the player");
                if (reason!.Event.EntityId == surveyor.Id)
                    break;
                reason = null;
            }
            Assert.That(reason, Is.Not.Null, "never paused for Surveyor I");

            // The NPC Strata Survey flight drops in before Surveyor I. It must not have stopped the clock.
            Assert.That(exits.Any(e => e.FactionId != player.Id && e.StarDate < reason!.Event.StarDate), Is.True,
                "an NPC warp exit should happen during this warp, or the regression is not covered");

            var dropIn = exits.Single(e => e.EntityId == surveyor.Id);
            Assert.That(game.TimePulse.GameGlobalDateTime, Is.EqualTo(dropIn.StarDate), "clock is at the drop-in instant");
            Assert.That(surveyor.HasDataBlob<WarpMovingDB>() && !surveyor.GetDataBlob<WarpMovingDB>().IsAtTarget, Is.False,
                "Surveyor I has arrived");
            Assert.That(warp.IsFinished(), Is.True);
            Assert.That(warp.PauseOnAction, Is.False, "one-shot is cleared");
            Assert.That(circularise.IsRunning, Is.False, "circularise has not started");
            Assert.That(circularise.Status, Is.EqualTo(ActionStatus.Queued));

            Assert.That(reason!.Event.StarDate, Is.EqualTo(dropIn.StarDate));
            Assert.That(reason.OrderName, Is.EqualTo(warp.Name));
            Assert.That(reason.OthersCount, Is.EqualTo(0), "the warp's one-shot and its WarpExited are one pause");
            var snapshot = projector.ProjectFaction(player.Id)!.PauseReason;
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.OrderName, Is.EqualTo("Warp Move to Venus"));
            Assert.That(snapshot.EntityId, Is.EqualTo(surveyor.Id));
            Assert.That(snapshot.EntityName, Is.EqualTo("Surveyor I"));
            Assert.That(snapshot.Message, Is.EqualTo("Surveyor I arrived at Venus"));

            // Play again: the goal re-plans its circularise at the drop-in and it runs, then the
            // survey starts, as it does with no pause (within a few hours of arrival).
            bool circularised = false;
            GeoSurveyOrder? survey = null;
            for (int i = 0; i < 12 && survey?.IsRunning != true; i++)
            {
                Step(game, TimeSpan.FromHours(1));
                Assert.That(game.TimePulse.LastFault, Is.Null, game.TimePulse.LastFault?.Report);
                var queue = surveyor.GetDataBlob<ActionQueueDB>().ActionList;
                circularised |= queue.OfType<CirculariseAction>().Any(c => c.IsRunning);
                survey = queue.OfType<GeoSurveyOrder>().SingleOrDefault();
            }
            Assert.That(circularised, Is.True, "circularise runs after unpause");
            Assert.That(survey?.IsRunning, Is.True, "then the geo survey starts");
        }
        finally
        {
            EventManager.Instance.Unsubscribe(EventType.WarpExited, onExit);
        }
    }

    static void Step(Game game, TimeSpan length)
    {
        game.TimePulse.TimeStep(game.TimePulse.GameGlobalDateTime + length);
        game.TimePulse.WaitUntilStopped();
    }
}
