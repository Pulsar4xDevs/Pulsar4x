using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// Power rebalance: on quickstart day 1, in the warp-tech and early-tech starts, every player
/// colony's facilities make up 10-30 % of its demand (life support the rest), its plants cover the
/// whole demand at satisfaction 1 in every tier, and it starts with the 730-day fuel runway.
/// </summary>
[TestFixture]
public class FacilityShareOfDemandTests
{
    [TestCase("Pulsar4x.Scenario.WarpTechStart", 2)]
    [TestCase("Pulsar4x.Scenario.EarlyTech", 1)]
    public void Day1_FacilitiesAreATenthToAThirdOfDemand_AndFullyPowered(string scenario, int playerColonies)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenario);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345));
        Assert.That(created, Is.Not.Null);
        var (game, player) = (created!.Value.game, created.Value.faction);

        Entity[] Colonies() => game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyPowerDB>())
            .Where(c => c.FactionOwnerID == player.Id).ToArray();
        Assert.That(Colonies(), Has.Length.EqualTo(playerColonies));

        // The fuel runway at the start: 730 days at full nameplate load, for every colony on the grid.
        foreach (var colony in game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyPowerDB>()))
        {
            var power = colony.GetDataBlob<ColonyPowerDB>();
            foreach (var fuel in power.Plants.Where(p => p.FuelId != null).Select(p => p.FuelId!).Distinct())
            {
                double kgPerDay = power.Plants.Where(p => p.FuelId == fuel).Sum(p => p.Nameplate_kW * p.FuelKgPerKJ * 86400);
                Assert.That(ColonyPowerProcessor.FuelAvailable_kg(colony, power, fuel), Is.GreaterThanOrEqualTo(kgPerDay * 730 * (1 - 1e-9)),
                    $"{Name(colony)} {fuel}: 730 days at full load");
            }
        }

        foreach (var sys in game.Systems)
            sys.ManagerSubpulses.ProcessSystem(sys.StarSysDateTime + TimeSpan.FromDays(1));

        foreach (var colony in Colonies())
        {
            var power = colony.GetDataBlob<ColonyPowerDB>();
            double facilities = power.Consumers.Where(c => c.DesignId != ColonyPowerProcessor.LifeSupportLineId).Sum(c => c.Draw_kW);
            double share = facilities / power.Demand_kW;
            TestContext.Out.WriteLine($"{scenario} {Name(colony)}: demand {power.Demand_kW:N1} kW, life support {power.LifeSupport_kW:N1}, facilities {facilities:N1} ({share:P1}), delivered {power.Delivered_kW:N1}");
            Assert.That(power.Demand_kW, Is.EqualTo(power.LifeSupport_kW + facilities).Within(1e-6));
            Assert.That(share, Is.InRange(0.10, 0.30), $"{Name(colony)} facility share");
            Assert.That(power.Delivered_kW, Is.GreaterThanOrEqualTo(power.Demand_kW), $"{Name(colony)}: the plants cover day 1");
            Assert.That(power.TierSatisfaction.Values, Is.All.EqualTo(1), $"{Name(colony)}: every tier served");
        }

        // Life support oracles: 0.05 kW a head on Earth; on Mars (colony cost 2) 0.05 + 0.6 x 2.
        var earth = Colonies().Single(c => Name(c) == "Earth HQ").GetDataBlob<ColonyPowerDB>();
        Assert.That(earth.LifeSupport_kW, Is.EqualTo(earth.LifeSupportColonists * 0.05).Within(1e-6));
        var mars = Colonies().SingleOrDefault(c => Name(c) == "Mars HQ")?.GetDataBlob<ColonyPowerDB>();
        if (mars != null)
            Assert.That(mars.LifeSupport_kW, Is.EqualTo(mars.LifeSupportColonists * (0.05 + 0.6 * 2)).Within(1e-6));
    }

    static string Name(Entity colony) => colony.GetDataBlob<NameDB>().DefaultName;
}
