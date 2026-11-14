using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// Every scenario colony starts with a modest grid battery bank (about 2-6 h of its day-1
/// demand), charged like its fuel runway, in both the warp-tech and the early-tech start.
/// </summary>
[TestFixture]
public class ScenarioBatteryTests
{
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    static Game Start(string? scenario)
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenario);
        var catalog = GameLifecycle.BuildCatalog(store);
        var systems = catalog.Systems.Where(s => s.StartingBodies.Count > 0).ToList();
        var request = new NewGameRequest(paths, "United Earth Corp", "UEC", catalog.Species.First().Id, catalog.Colonies.First().Id,
            systems.First().Id, systems.First().StartingBodies.First().Id, systems.Select(s => s.Id).ToList(), 10, 12345678, 100_000_000, true, "", "");
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null, "CreateGameCore returned null");
        return result!.Value.game;
    }

    static List<Entity> Colonies(Game game) =>
        game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyPowerDB>()).ToList();

    static string Name(Entity colony) => colony.GetDataBlob<NameDB>().DefaultName;

    static readonly HashSet<string> EarthColonies = new() { "Earth HQ", "Americas Compact", "Sino-Pacific Coalition", "Euro Consortium" };

    // Multi-faction phase 0: the three Earth blocs sit beside the company in both starts.
    [TestCase(WarpTechStart, new[] { "Earth HQ", "Mars HQ", "Luna Concord", "Ceres Depot", "Americas Compact", "Sino-Pacific Coalition", "Euro Consortium" })]
    [TestCase(EarlyTech, new[] { "Earth HQ", "Americas Compact", "Sino-Pacific Coalition", "Euro Consortium" })]   // early company + early blocs, no outposts
    [TestCase(null, new[] { "Earth HQ", "Americas Compact", "Sino-Pacific Coalition", "Euro Consortium" })]        // the default start is Early Tech (2026-11-15)
    public void EveryScenarioColony_StartsWithAFullBank_OfTwoToSixHours(string? scenario, string[] expected)
    {
        var colonies = Colonies(Start(scenario));
        Assert.That(colonies.Select(Name), Is.EquivalentTo(expected));
        foreach (var colony in colonies)
        {
            var power = colony.GetDataBlob<ColonyPowerDB>();
            string name = Name(colony);
            Assert.That(power.StoreMax_kJ, Is.GreaterThan(0), name);
            Assert.That(power.Stored_kJ, Is.EqualTo(power.StoreMax_kJ), name + " starts full");
            Assert.That(power.MaxCharge_kW, Is.GreaterThan(0), name);
            Assert.That(power.MaxDischarge_kW, Is.GreaterThan(0), name);
            Assert.That(power.Demand_kW, Is.GreaterThan(0), name);
            double hours = power.StoreMax_kJ / 3600 / power.Demand_kW;
            // Crew plan phase 4 cut garden-world life support to 0.01 kW a head, so Earth colonies get up
            // to 16 h: the 8,000-person company (~3.7 MW on a 350 t bank, ~13 h) and the 1M-person blocs
            // (~11 MW on 900 t, ~11.5 h). The sealed outposts keep 2-6 h.
            double maxHours = EarthColonies.Contains(name) ? 16.0 : 6.0;
            Assert.That(hours, Is.InRange(2.0, maxHours), name + $" bank covers 2-{maxHours} h of its demand");
        }
    }

    [TestCase(null)]
    [TestCase(EarlyTech)]
    public void FirstDay_TheFullBankTakesNoCharge_AndEveryTierIsServed(string? scenario)
    {
        foreach (var colony in Colonies(Start(scenario)))
        {
            var power = colony.GetDataBlob<ColonyPowerDB>();
            ColonyPowerProcessor.RunDay(colony);
            string name = Name(colony);
            // A full bank burns no extra fuel to fill itself; an empty one would (Earth +22 %).
            Assert.That(power.BatteryCharge_kW, Is.EqualTo(0), name);
            // The plants follow the load, so delivered meets demand to rounding: Earth's dispatch
            // leaves a ~1e-12 kW float residue that the bank covers. Anything real would be far larger.
            Assert.That(power.BatteryDischarge_kW, Is.EqualTo(0).Within(1e-6), name + ": the plants cover the day");
            Assert.That(power.Stored_kJ, Is.EqualTo(power.StoreMax_kJ).Within(1e-3), name);
            Assert.That(power.TierSatisfaction.Values, Is.All.EqualTo(1.0).Within(1e-9), name);
        }
    }

    [Test]
    public void LunasBank_IsCoveredByInfrastructure_SoItRunsAtFullEfficiency()
    {
        // Multi-faction phase 0 (Jon, 2026-11-15): Luna is 12,000 people on 60 infrastructure sites
        // with one 600 t bank, and the passive sensor's crew shrank, so its infrastructure now covers
        // the whole demand (it ran at ~0.40 before).
        var luna = Colonies(Start(WarpTechStart)).Single(c => Name(c) == "Luna Concord");
        Assert.That(Pulsar4X.Industry.InfrastructureProcessor.GetEfficiency(luna), Is.EqualTo(1.0).Within(1e-9));
    }
}
