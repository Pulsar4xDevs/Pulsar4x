using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Messaging;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;

namespace Pulsar4X.Tests;

/// <summary>
/// A loaded game has no "now" for ManagerSubPulse.AddSystemInterupt until the manager has processed
/// once, and the entity add methods used to be async void, so a throw there ended the test host
/// instead of failing a test.
/// </summary>
[TestFixture]
[NonParallelizable]
public class SubPulseReloadTests
{
    static Game BuildEarth()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);

        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);

        // Without a colony Sol sits in stasis and its clock never moves.
        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        game.LocalHumanFactionId = player.Id;
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;
        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");
        ColonyFactory.CreateFromBlueprint(game, player, species, sol, earth, store.Colonies["colony-earth"]);
        sol.UpdateActivityState();
        return game;
    }

    static Game Reloaded()
    {
        var game = BuildEarth();
        game.TimePulse.TimeStep(game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(3));
        string json = Game.Save(game);
        EntityIDGenerator.NextId = 0;
        Game.EntityIDCounter = 0;
        var loaded = Game.Load(json);
        loaded.Settings.EnforceSingleThread = true;
        return loaded;
    }

    [Test]
    public void SettingADataBlob_OnAReloadedSystem_SchedulesAfterTheSavedClock()
    {
        var loaded = Reloaded();
        var sol = loaded.Systems.Single(s => s.ID == "system-sol");
        var saved = sol.ManagerSubpulses.StarSysDateTime;
        Assert.That(saved, Is.GreaterThan(loaded.Settings.StartDateTime));

        var hotloopTypes = loaded.ProcessorManager.HotloopProcessors.Keys.ToHashSet();
        var blob = sol.GetAllEntites()
            .SelectMany(e => sol.GetAllDataBlobsForEntity(e.Id))
            .First(b => hotloopTypes.Contains(b.GetType()));
        Assert.DoesNotThrow(() => sol.SetDataBlob(blob.OwningEntity!.Id, blob));

        var next = sol.ManagerSubpulses.HotLoopProcessorsNextRun.Values
            .Where(t => t != null).Select(t => t!.Value).ToList();
        Assert.That(next, Is.Not.Empty);
        Assert.That(next.Min(), Is.GreaterThanOrEqualTo(saved));
    }

    [Test]
    public void CreatingAFaction_OnAReloadedGame_DoesNotThrow()
    {
        var loaded = Reloaded();
        Entity faction = null!;
        Assert.DoesNotThrow(() => faction = FactionFactory.CreateBasicFaction(loaded, "Reload Probe", "RLP", 0));
        Assert.That(faction.IsValid, Is.True);
    }

    [Test]
    public void ASubscriberThatThrows_FailsTheCaller_NotTheProcess()
    {
        var game = BuildEarth();
        bool armed = true;
        MessagePublisher.MessageHandler boom = _ => throw new InvalidOperationException("subscriber boom");
        MessagePublisher.Instance.Subscribe(MessageTypes.EntityAdded, boom, _ => armed);
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => FactionFactory.CreateBasicFaction(game, "Boom Probe", "BMP", 0));
            Assert.That(ex!.Message, Is.EqualTo("subscriber boom"));
        }
        finally
        {
            armed = false;
            MessagePublisher.Instance.Unsubscribe(MessageTypes.EntityAdded, boom);
        }
    }

    [Test]
    public void ASubscriberThatThrowsAfterAnAwait_IsRecordedOnTheClock_NotTheProcess()
    {
        var game = BuildEarth();
        bool armed = true;
        MessagePublisher.MessageHandler boom = async _ =>
        {
            await Task.Delay(20);
            throw new InvalidOperationException("late subscriber boom");
        };
        MessagePublisher.Instance.Subscribe(MessageTypes.EntityAdded, boom, _ => armed);
        try
        {
            Assert.DoesNotThrow(() => FactionFactory.CreateBasicFaction(game, "Late Probe", "LTP", 0));
            Assert.That(SpinWait.SpinUntil(() => game.TimePulse.LastFault != null, TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(game.TimePulse.LastFault!.Report, Does.Contain("late subscriber boom"));
        }
        finally
        {
            armed = false;
            MessagePublisher.Instance.Unsubscribe(MessageTypes.EntityAdded, boom);
        }
    }
}
