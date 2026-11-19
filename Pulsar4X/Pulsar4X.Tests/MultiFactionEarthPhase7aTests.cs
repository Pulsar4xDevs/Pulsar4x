using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Multi-faction Earth phase 7a: food and water (0.5 kg + 1 kg per person per day off-world), the harsh shortage
/// clock, satisfaction, farms, and the Earth bloc food balance.
/// </summary>
[TestFixture]
public class MultiFactionEarthPhase7aTests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";

    Game _game = null!;
    Entity _player = null!;

    void Start(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenario);
        var request = GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678);
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
    }

    List<Entity> Colonies() => _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();
    Entity Named(string name) => Colonies().Single(c => c.GetDataBlob<NameDB>().DefaultName == name);
    static ColonyNeedsDB Needs(Entity c) => c.GetDataBlob<ColonyNeedsDB>();
    static long Pop(Entity c) => NavalAcademyCourse.PopulationOf(c.GetDataBlob<ColonyInfoDB>());
    static ICargoable Good(Entity c, string id) => c.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(id)!;

    static void SetStock(Entity c, string id, long units)
    {
        var storage = c.GetDataBlob<CargoStorageDB>();
        var good = Good(c, id);
        long have = storage.GetUnitsStored(good, false);
        if (have > units) storage.RemoveCargoByUnit(good, have - units);
        else if (units > have) storage.AddCargoByUnit(good, units - have);
    }

    /// <summary>Takes the farms out so a food-only shortage can be staged.</summary>
    static void NoFarms(Entity c)
    {
        foreach (var line in c.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values)
            line.IndustryTypeRates.Remove(ColonyNeeds.AgricultureId);
    }

    static int AgriculturePoints(Entity c) => c.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values
        .Sum(l => l.IndustryTypeRates.TryGetValue(ColonyNeeds.AgricultureId, out var r) ? r : 0);

    /// <summary>Runs the needs pass once a day for <paramref name="days"/> days starting at <paramref name="from"/>.</summary>
    static DateTime RunDays(Entity c, DateTime from, int days)
    {
        for (int i = 0; i < days; i++)
            ColonyNeeds.RunDay(c, from.AddDays(i));
        return from.AddDays(days);
    }

    List<Event> Capture(params EventType[] types)
    {
        var list = new List<Event>();
        foreach (var t in types)
            EventManager.Instance.Subscribe(t, list.Add);
        return list;
    }

    static void Release(List<Event> list, params EventType[] types)
    {
        foreach (var t in types)
            EventManager.Instance.Unsubscribe(t, list.Add);
    }

    [Test]
    public void Mars_EatsHalfAKiloOfFoodAndAKiloOfWaterPerPersonPerDay()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        Assert.That(Pop(mars), Is.EqualTo(1000));
        // 180 days of stock, a prefab farm (1 t/day) and an ice mine.
        Assert.That(ColonyNeeds.Stock(mars, "food"), Is.EqualTo(90_000));
        Assert.That(AgriculturePoints(mars), Is.EqualTo(1000));
        Assert.That(ColonyNeeds.FoodPerDay(mars), Is.EqualTo(500));
        Assert.That(ColonyNeeds.WaterPerDay(mars), Is.EqualTo(1000));
        NoFarms(mars);
        long food = ColonyNeeds.Stock(mars, "food"), water = ColonyNeeds.Stock(mars, "water");
        ColonyNeeds.RunDay(mars, mars.StarSysDateTime);
        Assert.That(ColonyNeeds.Stock(mars, "food"), Is.EqualTo(food - 500));
        Assert.That(ColonyNeeds.Stock(mars, "water"), Is.EqualTo(water - 1000));
        Assert.That(Needs(mars).ShortSince, Is.Null);
    }

    [Test]
    public void CompanyEarthColony_EatsNothingExtra()
    {
        Start(WarpTechStart);
        var earth = Colonies().Single(c => c.FactionOwnerID == _player.Id && c.GetDataBlob<NameDB>().DefaultName == "Earth HQ");
        Assert.That(ColonyNeeds.SelfSupplied(earth), Is.True);
        Assert.That(ColonyNeeds.FoodPerDay(earth), Is.EqualTo(0));
        Assert.That(ColonyNeeds.DaysOfStock(earth, "food"), Is.Null);
        long water = ColonyNeeds.Stock(earth, "water");
        RunDays(earth, earth.StarSysDateTime, 5);
        Assert.That(ColonyNeeds.Stock(earth, "water"), Is.EqualTo(water));
        Assert.That(Needs(earth).ShortSince, Is.Null);
    }

    [Test]
    public void FoodShortage_StopsGrowthAtDay0_DeclinesFromDay30_KillsOnePercentAtDay90()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        int species = mars.GetDataBlob<ColonyInfoDB>().Population.Keys.Single();
        NoFarms(mars);
        SetStock(mars, "food", 0);
        SetStock(mars, "medical-supplies", 0);                  // medical met would halve the deaths (phase 7b); this pins the base rule
        var types = new[] { EventType.SupplyShortage, EventType.SupplyWarning, EventType.PopulationLeft, EventType.PopulationStarved };
        var events = Capture(types);
        try
        {
            var t0 = mars.StarSysDateTime;
            ColonyNeeds.RunDay(mars, t0);
            Assert.That(Needs(mars).ShortSince, Is.EqualTo(t0));
            Assert.That(PopulationProcessor.Adjusted(mars, 0.01), Is.EqualTo(0), "growth stops on day 0");
            Assert.That(events.Count(e => e.EventType == EventType.SupplyShortage), Is.EqualTo(1));

            var t = RunDays(mars, t0.AddDays(1), 29);           // days 1..29
            Assert.That(Pop(mars), Is.EqualTo(1000));
            Assert.That(MigrationPlanner.Crowded(mars, species), Is.EqualTo(0));

            t = RunDays(mars, t, 30);                           // days 30..59: −1 %/month
            Assert.That(Pop(mars), Is.InRange(989, 991));
            Assert.That(Needs(mars).LeftTotal, Is.InRange(9, 11));
            // Leavers are offered relocation (counted as crowded) before anyone dies.
            Assert.That(MigrationPlanner.Crowded(mars, species), Is.GreaterThanOrEqualTo(49));
            Assert.That(Needs(mars).DeathsTotal, Is.EqualTo(0));

            t = RunDays(mars, t, 30);                           // days 60..89
            Assert.That(Needs(mars).DeathsTotal, Is.EqualTo(0), "no deaths before day 90");
            // Day 90: 1 % × unfed share (1) × population (the day's decline runs after the deaths step).
            long before = Pop(mars);
            ColonyNeeds.RunDay(mars, t);
            Assert.That(Needs(mars).DeathsTotal, Is.EqualTo((long)Math.Floor(0.01 * before)));
            Assert.That(Needs(mars).DeathsTotal, Is.InRange(9, 10));
            Assert.That(events.Any(e => e.EventType == EventType.PopulationStarved), Is.True);
            Assert.That(events.Any(e => e.EventType == EventType.PopulationLeft), Is.True);
            Assert.That(events.Count(e => e.EventType == EventType.SupplyWarning), Is.GreaterThanOrEqualTo(4), "day 30, 60, 70, 80");
        }
        finally { Release(events, types); }
    }

    [Test]
    public void WaterShortage_CountsDouble()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        SetStock(mars, "water", 0);                             // food stays: 90 t
        SetStock(mars, "medical-supplies", 0);                  // base rule; medical met would halve it (phase 7b)
        var t0 = mars.StarSysDateTime;
        var t = RunDays(mars, t0, 90);                          // days 0..89
        Assert.That(Needs(mars).DeathsTotal, Is.EqualTo(0));
        Assert.That(ColonyNeeds.Stock(mars, "food"), Is.GreaterThan(0), "only water is short");
        long before = Pop(mars);
        ColonyNeeds.RunDay(mars, t);                            // day 90
        long dead = Needs(mars).DeathsTotal;
        Assert.That(dead, Is.InRange(19, 20), $"2 % of ~{before}");
        Assert.That(dead, Is.EqualTo((long)Math.Floor(0.02 * before)));
    }

    [Test]
    public void TenSuppliedDays_ResetTheClock()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        NoFarms(mars);
        SetStock(mars, "food", 0);
        var t = RunDays(mars, mars.StarSysDateTime, 5);
        Assert.That(Needs(mars).ShortSince, Is.Not.Null);
        SetStock(mars, "food", 50_000);
        t = RunDays(mars, t, 9);
        Assert.That(Needs(mars).ShortSince, Is.Not.Null, "9 supplied days are not enough");
        RunDays(mars, t, 1);
        Assert.That(Needs(mars).ShortSince, Is.Null);
        Assert.That(ColonyNeeds.GrowthStopped(mars), Is.False);
    }

    [Test]
    public void LoadingASaveWithEmptyStock_StartsTheClockAtLoad()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        NoFarms(mars);
        // An old save: no needs record, empty stores, a year after the start.
        mars.RemoveDataBlob<ColonyNeedsDB>();
        SetStock(mars, "food", 0);
        var loaded = mars.StarSysDateTime.AddDays(365);
        ColonyNeeds.Ensure(mars);
        ColonyNeeds.RunDay(mars, loaded);
        Assert.That(Needs(mars).ShortSince, Is.EqualTo(loaded), "not backdated");
        // One long jump right after load still waits the full 90 days.
        ColonyNeeds.RunDay(mars, loaded.AddDays(30));
        ColonyNeeds.RunDay(mars, loaded.AddDays(60));
        ColonyNeeds.RunDay(mars, loaded.AddDays(89));
        Assert.That(Needs(mars).DeathsTotal, Is.EqualTo(0));
    }

    [Test]
    public void Satisfaction_StartsAt60_MovesAFifthTowardItsTarget_AndScalesGrowthOutputAndLeaving()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        var t0 = mars.StarSysDateTime;
        Assert.That(Needs(mars).Satisfaction, Is.EqualTo(60));
        var t = RunDays(mars, t0, 31);
        // Supplied, company wages paid, not overcrowded: 50 + 5, plus 30 for the three goods met (phase 7b).
        Assert.That(Needs(mars).SatisfactionTarget, Is.EqualTo(85));
        Assert.That(Needs(mars).Satisfaction, Is.EqualTo(65).Within(1e-9));

        SetStock(mars, "water", 0);
        RunDays(mars, t, 30);
        Assert.That(Needs(mars).SatisfactionTarget, Is.EqualTo(55), "water short −30 (goods still met)");
        Assert.That(Needs(mars).Satisfaction, Is.EqualTo(65 + 0.2 * (55 - 65)).Within(1e-9));
        Assert.That(ColonyNeeds.GrowthFactor(mars), Is.EqualTo((0.5 + Needs(mars).Satisfaction / 100) * ColonyNeeds.GoodsGrowthFactor(mars)).Within(1e-9));

        Needs(mars).Satisfaction = 15;
        Assert.That(ColonyNeeds.OutputFactor(mars), Is.EqualTo(0.9));
        Assert.That(ColonyNeeds.Leaving(mars), Is.EqualTo((long)Math.Floor(Pop(mars) * 0.05)), "below 30 posts leavers");
        Needs(mars).Satisfaction = 85;
        Assert.That(ColonyNeeds.OutputFactor(mars), Is.EqualTo(1.05));
        Needs(mars).Satisfaction = 50;
        Assert.That(ColonyNeeds.OutputFactor(mars), Is.EqualTo(1.0));
    }

    [Test]
    public void EarthBlocs_AmcFarmsFiveTonnesADay_SpcAndEcnBuyThreeEach()
    {
        foreach (var scenario in new[] { WarpTechStart, EarlyTech })
        {
            Start(scenario);
            var amc = Named("Americas Compact");
            Assert.That(AgriculturePoints(amc), Is.EqualTo(5000), scenario);
            var amcRow = amc.GetDataBlob<ColonyMarketPolicyDB>().Rows["food"];
            Assert.That(amcRow.Max, Is.EqualTo(450_000));
            Assert.That(amcRow.Bid, Is.EqualTo(0m), "AMC sells food");
            Assert.That(amcRow.Ask, Is.EqualTo(5.94m));
            foreach (var name in new[] { "Sino-Pacific Coalition", "Euro Consortium" })
            {
                var row = Named(name).GetDataBlob<ColonyMarketPolicyDB>().Rows["food"];
                Assert.That(row.UsePerDay, Is.EqualTo(3000).Within(1e-9), name);
                Assert.That(row.Min, Is.EqualTo(90_000), name);
                Assert.That(row.Bid, Is.EqualTo(Math.Round(5.94m * 0.75m, 2, MidpointRounding.AwayFromZero)), name);
            }
            // Earth's own people are fed by the world: no bloc draws from stock.
            Assert.That(ColonyNeeds.FoodPerDay(amc), Is.EqualTo(0));
            SetStock(amc, "food", 0);
            long grown = ColonyNeeds.FarmDays(amc, 1);
            TestContext.Out.WriteLine($"{scenario}: AMC farms grew {grown} kg in a day (5,000 at full staff and power)");
            Assert.That(grown, Is.InRange(1, 5000));
        }
    }

    [Test]
    public void Luna_HasFarmsAndAHundredEightyDaysOfGapStock()
    {
        Start(WarpTechStart);
        var luna = Named("Luna Concord");
        Assert.That(Pop(luna), Is.EqualTo(12_000));
        Assert.That(AgriculturePoints(luna), Is.EqualTo(4000), "2 farm sites, 2 t/day each");
        // Luna's map data has no water deposit (iron only), so its water is bought: 12 t people + 4 t farms a day.
        Assert.That(ColonyNeeds.Stock(luna, "food"), Is.EqualTo(360_000));
        Assert.That(ColonyNeeds.Stock(luna, "water"), Is.EqualTo(2_880_000));
        var rows = luna.GetDataBlob<ColonyMarketPolicyDB>().Rows;
        Assert.That(rows["food"].Min, Is.EqualTo(180 * 2000));
        Assert.That(rows["water"].Min, Is.EqualTo(180 * 16000));
        Assert.That(rows["food"].Bid, Is.GreaterThan(0m));
        Assert.That(rows["water"].Bid, Is.GreaterThan(0m));
        // The gap rows govern Luna's hold (not 180 days of full use).
        Assert.That(ColonyNeeds.HoldUnits(luna, "food"), Is.EqualTo(0));
        Assert.That(ColonyNeeds.HoldUnits(Named("Mars HQ"), "food"), Is.EqualTo(180 * 500));
    }

    [Test]
    public void NeedsView_ProjectsDaysShortageAndSatisfaction()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        var view = new GameProjector(_game).ProjectEntity(mars, _player.Id).GetView<ColonyNeedsView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.FoodPerDay, Is.EqualTo(500));
        Assert.That(view.FoodDaysOfStock, Is.EqualTo(180).Within(1e-9));
        Assert.That(view.ShortageState, Is.EqualTo("Supplied"));
        Assert.That(view.Satisfaction, Is.EqualTo(60));
        Assert.That(view.GrowthFactor, Is.EqualTo(1.1).Within(1e-9));
    }

    /// <summary>
    /// Two game years headless, no player action. Nothing hauls in the first 180 days. Luna and Ceres post buy rows for
    /// their gap; with no NPC freighters yet, the test stands in for the hauler from day 180 by filling what they bid
    /// for once a month. Nobody may die.
    /// </summary>
    [Test, Explicit("Long: two game years headless (several minutes); run by name."), Category("Slow"), Category("Explicit")]
    public void WarpTech_TwoYearsHeadless_ZeroDeaths()
    {
        Start(WarpTechStart);
        var luna = Named("Luna Concord");
        var ceres = Named("Ceres Depot");
        bool lunaBid = false;
        for (int month = 1; month <= 24; month++)
        {
            _game.TimePulse.TimeStep(_game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(30));
            Assert.That(_game.TimePulse.LastFault, Is.Null);
            if (MarketBook.TryGet(luna, "food", out var listing) && listing.BuyQuantity > 0)
                lunaBid = true;
            if (month * 30 >= 180)
            {
                foreach (var c in new[] { luna, ceres })
                {
                    foreach (var id in new[] { "food", "water" })
                    {
                        long target = (long)(180 * (id == "food" ? ColonyNeeds.FoodPerDay(c) : ColonyNeeds.WaterPerDay(c)));
                        if (ColonyNeeds.Stock(c, id) < target)
                            SetStock(c, id, target);
                    }
                    foreach (var id in ColonyNeeds.GoodsIds)
                    {
                        long hold = (long)Math.Ceiling(360 * ColonyNeeds.GoodsPerDay(c, id));
                        if (ColonyNeeds.Stock(c, id) < hold)
                            SetStock(c, id, hold);
                    }
                }
            }
            if (month == 6)
                Assert.That(Needs(luna).DeathsTotal, Is.EqualTo(0), "no hauls in the first 180 days");
        }
        foreach (var c in Colonies())
        {
            TestContext.Out.WriteLine($"{c.GetDataBlob<NameDB>().DefaultName}: pop {Pop(c)} food {ColonyNeeds.Stock(c, "food")} water {ColonyNeeds.Stock(c, "water")} " +
                                      $"deaths {Needs(c).DeathsTotal} left {Needs(c).LeftTotal} S {Needs(c).Satisfaction:F1}");
            Assert.That(Needs(c).DeathsTotal, Is.EqualTo(0), c.GetDataBlob<NameDB>().DefaultName);
        }
        Assert.That(lunaBid, Is.True, "Luna's buy rows post the gap");
        Assert.That(Needs(luna).Satisfaction, Is.GreaterThanOrEqualTo(50), "supplied Luna settles well above the leave threshold");
        Assert.That(Needs(ceres).Satisfaction, Is.GreaterThanOrEqualTo(50), "supplied Ceres settles well above the leave threshold");
        Assert.That(Needs(Named("Mars HQ")).ShortSince, Is.Null, "Mars feeds itself");
    }

    [Test, Explicit("Long: two game years headless (several minutes); run by name."), Category("Slow"), Category("Explicit")]
    public void EarlyTech_TwoYearsHeadless_ZeroDeaths()
    {
        Start(EarlyTech);
        for (int month = 1; month <= 24; month++)
        {
            _game.TimePulse.TimeStep(_game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(30));
            Assert.That(_game.TimePulse.LastFault, Is.Null);
        }
        foreach (var c in Colonies())
            Assert.That(Needs(c).DeathsTotal, Is.EqualTo(0), c.GetDataBlob<NameDB>().DefaultName);
    }
}
