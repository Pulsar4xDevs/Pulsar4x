using System;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Logistics;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// The market jobs (OfferStock, RunMarket) post listings whenever stock moves. That is a change to the
/// market book, not an event for the colony log. Before the fix the log got an alternating pair,
/// "Offer stock." then "Post Market Listing.", every few days for the life of the colony.
/// </summary>
[TestFixture]
[NonParallelizable]
public class MarketLogSpamTests
{
    static Game StartEarlyTech(bool early = true)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        string? scenarioId = early ? "Pulsar4x.Scenario.EarlyTech" : null;
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenarioId);
        var catalog = GameLifecycle.BuildCatalog(store);
        var systems = catalog.Systems.Where(x => x.StartingBodies.Count > 0).ToList();
        var request = new NewGameRequest(
            ModManifestPaths: paths, FactionName: "United Earth Corp", FactionAbbreviation: "UEC",
            SpeciesId: catalog.Species.First().Id, ColonyId: catalog.Colonies.First().Id,
            SystemId: systems.First().Id, BodyId: systems.First().StartingBodies.First().Id,
            EnabledSystems: systems.Select(x => x.Id).ToList(), MaxSystems: 10, MasterSeed: 12345678,
            StartingFunds: 100_000_000, EleStart: early, SMPassword: "", PlayerPassword: "");
        var result = GameLifecycle.CreateGameCore(store, request);
        return result!.Value.game;
    }


    [TestCase(true)]
    [TestCase(false)]
    public void MarketJobs_DoNotFillTheColonyLog(bool earlyTech)
    {
        var game = StartEarlyTech(earlyTech);
        game.Settings.EnforceSingleThread = true;
        var t0 = game.TimePulse.GameGlobalDateTime;
        for (int d = 1; d <= 90; d++)
            game.TimePulse.TimeStep(t0 + TimeSpan.FromDays(d));

        var colonies = game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Where(colony => colony.HasDataBlob<ColonyMarketPolicyDB>())
            .ToList();
        Assert.That(colonies, Is.Not.Empty, "no colony has a market policy, so the test would prove nothing");

        int listed = 0;
        foreach (var colony in colonies)
        {
            var policy = colony.GetDataBlob<ColonyMarketPolicyDB>();
            listed += policy.Rows.Keys.Count(id => MarketBook.TryGet(colony, id, out _));

            var lines = colony.TryGetDataBlob<ShipLogDB>(out var book)
                ? book.Entries.Select(entry => entry.Text).ToList()
                : new System.Collections.Generic.List<string>();
            string name = colony.GetDataBlob<NameDB>().GetName(colony.FactionOwnerID);

            Assert.That(lines.Count(line => line.StartsWith("Post Market Listing")), Is.EqualTo(0),
                $"{name}: {string.Join(" | ", lines)}");
            // The order line ("Ordered: Offer stock.") is written once. The bare route line is not repeated.
            Assert.That(lines.Count(line => line == "Offer stock."), Is.EqualTo(0),
                $"{name}: {string.Join(" | ", lines)}");
            Assert.That(lines.Count(line => line == "Ordered: Offer stock."), Is.LessThanOrEqualTo(1), name);
        }

        // The planner still ran: the book has listings. Only the log lines are gone.
        Assert.That(listed, Is.GreaterThan(0), "the market book was never written");
    }
}
