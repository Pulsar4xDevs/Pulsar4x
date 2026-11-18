using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using GameEngine.Engine.Orders;

namespace Pulsar4X.Tests;

/// <summary>
/// Multi-faction Earth phase 7b: consumer goods, medical supplies and luxuries (monthly wants, satisfaction, growth,
/// shortage deaths, migration pull), plus the shipyard parts check and the ship crew counts.
/// </summary>
[TestFixture]
[NonParallelizable]
public class MultiFactionEarthPhase7bTests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";
    const string Consumer = ColonyNeeds.ConsumerGoodsId, Medical = ColonyNeeds.MedicalId, Luxury = ColonyNeeds.LuxuryId;

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
    static ICargoable Good(Entity c, string id) => c.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(id)!;

    static void SetStock(Entity c, string id, long units)
    {
        var storage = c.GetDataBlob<CargoStorageDB>();
        var good = Good(c, id);
        long have = storage.GetUnitsStored(good, false);
        if (have > units) storage.RemoveCargoByUnit(good, have - units);
        else if (units > have) storage.AddCargoByUnit(good, units - have);
    }

    static DateTime RunDays(Entity c, DateTime from, int days)
    {
        for (int i = 0; i < days; i++)
            ColonyNeeds.RunDay(c, from.AddDays(i));
        return from.AddDays(days);
    }

    [Test]
    public void Wants_AreTwoOneAndHalfAKiloPer1000PerMonth_AndDrawnOnTheMonthlyTick()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        Assert.That(ColonyNeeds.GoodsPerDay(mars, Consumer), Is.EqualTo(2.0 / 30).Within(1e-9));
        Assert.That(ColonyNeeds.GoodsPerDay(mars, Medical), Is.EqualTo(1.0 / 30).Within(1e-9));
        Assert.That(ColonyNeeds.GoodsPerDay(mars, Luxury), Is.EqualTo(0.5 / 30).Within(1e-9));
        Assert.That(ColonyNeeds.GoodsMet(mars, Consumer), Is.Null, "nothing is checked before the first monthly draw");
        long c0 = ColonyNeeds.Stock(mars, Consumer), m0 = ColonyNeeds.Stock(mars, Medical), l0 = ColonyNeeds.Stock(mars, Luxury);
        Assert.That(c0, Is.GreaterThan(0));
        var t0 = mars.StarSysDateTime;
        RunDays(mars, t0, 29);
        Assert.That(ColonyNeeds.Stock(mars, Consumer), Is.EqualTo(c0), "wants are monthly");
        RunDays(mars, t0, 31);
        Assert.That(ColonyNeeds.Stock(mars, Consumer), Is.EqualTo(c0 - 2));
        Assert.That(ColonyNeeds.Stock(mars, Medical), Is.EqualTo(m0 - 1));
        Assert.That(ColonyNeeds.Stock(mars, Luxury), Is.EqualTo(l0 - 1), "0.5 kg rounds up to the kilo");
        Assert.That(Needs(mars).ConsumerMet, Is.True);
        Assert.That(Needs(mars).MedicalMet, Is.True);
        Assert.That(Needs(mars).LuxuryMet, Is.True);
    }

    [Test]
    public void Satisfaction_AllWantsMet_RaisesTheTargetBy30_AndGrowthByTheGoodsFactors()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        RunDays(mars, mars.StarSysDateTime, 31);
        // 50 + wages 5 + consumer 10 + medical 10 + luxuries 10.
        Assert.That(Needs(mars).SatisfactionTarget, Is.EqualTo(85));
        Assert.That(ColonyNeeds.GoodsGrowthFactor(mars), Is.EqualTo(1.1 * 1.1).Within(1e-9));
        Assert.That(ColonyNeeds.GrowthFactor(mars), Is.EqualTo((0.5 + Needs(mars).Satisfaction / 100) * 1.21).Within(1e-9));
        Assert.That(ColonyNeeds.MigrationPull(mars), Is.EqualTo(1.2));
        Assert.That(ColonyNeeds.MedicalFactor(mars), Is.EqualTo(0.5));
        var parts = ColonyNeeds.Breakdown(mars, Needs(mars));
        Assert.That(parts.Sum(p => p.Delta), Is.EqualTo(85));
        Assert.That(parts.Select(p => p.Label), Does.Contain("Medical supplies"));
    }

    [Test]
    public void Satisfaction_MissingWants_CostTenFifteenAndFive_AndMedicalCutsGrowth()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        foreach (var id in new[] { Consumer, Medical, Luxury })
            SetStock(mars, id, 0);
        RunDays(mars, mars.StarSysDateTime, 31);
        Assert.That(Needs(mars).ConsumerMet, Is.False);
        Assert.That(Needs(mars).MedicalMet, Is.False);
        Assert.That(Needs(mars).LuxuryMet, Is.False);
        // 50 + wages 5 - 10 - 15 - 5.
        Assert.That(Needs(mars).SatisfactionTarget, Is.EqualTo(25));
        Assert.That(ColonyNeeds.GoodsGrowthFactor(mars), Is.EqualTo(0.75).Within(1e-9), "no medical: growth x0.75; missing consumer goods do not change growth");
        Assert.That(ColonyNeeds.MedicalFactor(mars), Is.EqualTo(1.0));
        Assert.That(ColonyNeeds.MigrationPull(mars), Is.EqualTo(1.0));
    }

    [Test]
    public void Medical_HalvesShortageDeaths()
    {
        long Deaths(bool medical)
        {
            Start(WarpTechStart);
            var mars = Named("Mars HQ");
            foreach (var line in mars.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values)
                line.IndustryTypeRates.Remove(ColonyNeeds.AgricultureId);
            SetStock(mars, "food", 0);
            if (!medical)
                SetStock(mars, Medical, 0);
            RunDays(mars, mars.StarSysDateTime, 101);
            Assert.That(Needs(mars).MedicalMet, Is.EqualTo(medical));
            return Needs(mars).DeathsTotal;
        }
        long with = Deaths(true), without = Deaths(false);
        Assert.That(without, Is.GreaterThan(0));
        Assert.That(with * 2, Is.EqualTo(without).Within(2), "medical supplies met halve the death step");
        Assert.That(with, Is.LessThan(without));
    }

    [Test]
    public void LunaAndCeres_WhenSupplied_SettleWellAboveTheLeaveThreshold()
    {
        Start(WarpTechStart);
        foreach (var name in new[] { "Luna Concord", "Ceres Depot" })
        {
            var c = Named(name);
            var t = c.StarSysDateTime;
            // Two years; a stand-in hauler tops every want up to its 180-day / 12-month hold each month.
            for (int month = 0; month < 24; month++)
            {
                t = RunDays(c, t, 30);
                foreach (var id in new[] { "food", "water" })
                {
                    long hold = (long)(180 * (id == "food" ? ColonyNeeds.FoodPerDay(c) : ColonyNeeds.WaterPerDay(c)));
                    if (ColonyNeeds.Stock(c, id) < hold) SetStock(c, id, hold);
                }
                foreach (var id in ColonyNeeds.GoodsIds)
                {
                    long hold = (long)Math.Ceiling(360 * ColonyNeeds.GoodsPerDay(c, id));
                    if (ColonyNeeds.Stock(c, id) < hold) SetStock(c, id, hold);
                }
            }
            TestContext.Out.WriteLine($"{name}: satisfaction {Needs(c).Satisfaction:F1} target {Needs(c).SatisfactionTarget:F1} " +
                $"parts {string.Join(", ", ColonyNeeds.Breakdown(c, Needs(c)).Select(p => $"{p.Label} {p.Delta:+0;-0}"))}");
            Assert.That(Needs(c).Satisfaction, Is.GreaterThanOrEqualTo(50), name + " settles at 50 or more");
            Assert.That(Needs(c).Satisfaction, Is.GreaterThan(30), name);
            Assert.That(ColonyNeeds.Leaving(c), Is.EqualTo(0), name + " posts no leavers");
            Assert.That(Needs(c).DeathsTotal, Is.EqualTo(0), name);
        }
    }

    [Test]
    public void Data_GoodsAreMadeStockedAndTradedByTheBlocs()
    {
        Start(WarpTechStart);
        var info = _player.GetDataBlob<FactionInfoDB>();
        foreach (var id in ColonyNeeds.GoodsIds)
        {
            Assert.That(info.IndustryDesigns.ContainsKey(id), Is.True, id + " has a recipe");
            TestContext.Out.WriteLine($"{id}: seed ask {Pulsar4X.Energy.EnergyPricing.SeedAsk(_game, info.Data, id)} cr/kg");
        }
        var ecn = Named("Euro Consortium"); var spc = Named("Sino-Pacific Coalition"); var amc = Named("Americas Compact");
        Assert.That(ColonyNeeds.Stock(ecn, Medical), Is.GreaterThan(0));
        Assert.That(ColonyNeeds.Stock(ecn, Luxury), Is.GreaterThan(0));
        Assert.That(ColonyNeeds.Stock(spc, Consumer), Is.GreaterThan(0));
        var ecnRows = ecn.GetDataBlob<ColonyMarketPolicyDB>().Rows;
        var spcRows = spc.GetDataBlob<ColonyMarketPolicyDB>().Rows;
        var amcRows = amc.GetDataBlob<ColonyMarketPolicyDB>().Rows;
        Assert.That(ecnRows[Medical].IsDemand(), Is.False, "ECN sells medical");
        Assert.That(ecnRows[Medical].Min, Is.GreaterThan(0), "and keeps its own use back");
        Assert.That(ecnRows[Luxury].IsDemand(), Is.False);
        Assert.That(spcRows[Consumer].IsDemand(), Is.False, "SPC sells consumer goods");
        Assert.That(spcRows[Medical].IsDemand(), Is.True, "SPC imports medical");
        Assert.That(amcRows[Medical].IsDemand(), Is.True);
        Assert.That(amcRows[Luxury].IsDemand(), Is.True);
        Assert.That(ecnRows[Consumer].IsDemand(), Is.True);
        // Every colony starts able to meet its wants for the first months.
        foreach (var c in Colonies())
            foreach (var id in ColonyNeeds.GoodsIds)
                Assert.That(ColonyNeeds.Stock(c, id), Is.GreaterThanOrEqualTo((long)Math.Floor(ColonyNeeds.GoodsPerDay(c, id) * 30)),
                    c.GetDataBlob<NameDB>().DefaultName + " " + id);
    }

    [Test]
    public void SpcImportsMedicalFromTheOtherBlocs_OnceItsBidRisesWithTheShortage()
    {
        Start(WarpTechStart);
        var ecn = Named("Euro Consortium"); var spc = Named("Sino-Pacific Coalition");
        var t0 = spc.StarSysDateTime;
        AgentProcessor.AssignGoal(ecn, new Goal(GoalType.OfferStock));          // ECN posts its medical sell listing
        Assert.That(MarketBook.TryGet(ecn, Medical, out var listing) && listing.SellQuantity > 0, Is.True);
        var amc = Named("Americas Compact");
        AgentProcessor.AssignGoal(amc, new Goal(GoalType.OfferStock));          // AMC holds medical too (its own spare)
        long sellersBefore = ColonyNeeds.Stock(ecn, Medical) + ColonyNeeds.Stock(amc, Medical);
        SetStock(spc, Medical, 0);
        var row = spc.GetDataBlob<ColonyMarketPolicyDB>().Rows[Medical];
        row.ShortSince = t0.AddDays(-90);                                       // a full shortage premium: bid 1.5 x the seed ask
        AgentProcessor.AssignGoal(spc, new Goal(GoalType.OfferStock));
        Assert.That(ColonyNeeds.Stock(spc, Medical), Is.GreaterThan(0), "SPC bought medical on Earth");
        Assert.That(ColonyNeeds.Stock(ecn, Medical) + ColonyNeeds.Stock(amc, Medical), Is.LessThan(sellersBefore),
            "it came out of the other blocs' stock");
    }

    [Test]
    public void EarlyTech_CompanyAndBlocsStartWithGoods()
    {
        Start(EarlyTech);
        foreach (var c in Colonies())
            foreach (var id in ColonyNeeds.GoodsIds)
                Assert.That(ColonyNeeds.Stock(c, id), Is.GreaterThanOrEqualTo((long)Math.Floor(ColonyNeeds.GoodsPerDay(c, id) * 30)),
                    c.GetDataBlob<NameDB>().DefaultName + " " + id);
    }

    [Test]
    public void NeedsView_ProjectsGoods()
    {
        Start(WarpTechStart);
        var mars = Named("Mars HQ");
        RunDays(mars, mars.StarSysDateTime, 31);
        SetStock(mars, Medical, 0);
        var view = new GameProjector(_game).ProjectEntity(mars, _player.Id).GetView<ColonyNeedsView>()!;
        Assert.That(view.ConsumerGoodsPerDay, Is.EqualTo(2.0 / 30).Within(1e-9));
        Assert.That(view.MedicalPerDay, Is.EqualTo(1.0 / 30).Within(1e-9));
        Assert.That(view.LuxuryPerDay, Is.EqualTo(0.5 / 30).Within(1e-9));
        Assert.That(view.ConsumerGoodsStock, Is.EqualTo(ColonyNeeds.Stock(mars, Consumer)));
        Assert.That(view.MedicalStock, Is.EqualTo(0));
        Assert.That(view.MedicalDaysOfStock, Is.EqualTo(0).Within(1e-9));
        Assert.That(view.ConsumerGoodsDaysOfStock, Is.GreaterThan(500));
        Assert.That(view.ConsumerGoodsMet, Is.True);
        Assert.That(view.MedicalMet, Is.True, "last month's draw was met; the stock was emptied after");
        Assert.That(view.MedicalShort, Is.False);
        Assert.That(view.GoodsGrowthFactor, Is.EqualTo(1.21).Within(1e-9));
        Assert.That(view.LuxuryMigrationPull, Is.EqualTo(1.2));
        Assert.That(view.MedicalFactor, Is.EqualTo(0.5));
        Assert.That(view.SatisfactionParts.Sum(p => p.Delta), Is.EqualTo(view.SatisfactionTarget));
        Assert.That(view.SatisfactionParts[0].Label, Is.EqualTo("Base"));
    }

    // ---- shipyard: parts in colony cargo ----

    [Test]
    public void Shipyard_CountsAndConsumesPartsStoredAsInstancesAndAsStacks()
    {
        Start(WarpTechStart);
        var earth = Colonies().First(c => c.FactionOwnerID == _player.Id && c.HasDataBlob<IndustryAbilityDB>() && c.HasDataBlob<LaunchComplexDB>());
        var info = _player.GetDataBlob<FactionInfoDB>();
        var storage = earth.GetDataBlob<CargoStorageDB>();
        var raptor = info.ComponentDesigns["default-design-raptor"];
        long stack0 = storage.GetUnitsStoredByDesign(raptor);

        // Factory output: one entry per part, each with its own ID (3 Raptors = 3 entries).
        for (int i = 0; i < 3; i++)
            Assert.That(storage.AddCargoByUnit(new ComponentInstance(raptor), 1), Is.EqualTo(1));
        Assert.That(storage.GetUnitsStored(raptor, false), Is.EqualTo(stack0), "no stack under the design ID");
        Assert.That(storage.GetUnitsStoredByDesign(raptor), Is.EqualTo(stack0 + 3), "the check counts the instances");

        // The yard's view of a Starship (3 Raptors) shows them as available.
        var view = new GameProjector(_game).ProjectEntity(earth, _player.Id).GetView<IndustryView>()!;
        var starship = view.ProductionLines.SelectMany(l => l.Constructibles).First(c => c.DesignId == "default-ship-design-starship");
        Assert.That(starship.Costs.First(c => c.Name == raptor.Name).Available, Is.EqualTo(stack0 + 3));

        // The consume path takes them, instances last-in unaffected otherwise.
        IDictionary<string, long> need = new Dictionary<string, long> { [raptor.UniqueID] = 2 };
        IndustryTools.ConsumeResources(storage, ref need);
        Assert.That(need[raptor.UniqueID], Is.EqualTo(0));
        Assert.That(storage.GetUnitsStoredByDesign(raptor), Is.EqualTo(stack0 + 1));

        // Both forms together: a stack under the design ID plus a loose instance.
        Assert.That(storage.AddCargoByUnit(raptor, 2), Is.EqualTo(2));
        Assert.That(storage.GetUnitsStoredByDesign(raptor), Is.EqualTo(stack0 + 3));
        need = new Dictionary<string, long> { [raptor.UniqueID] = stack0 + 3 + 5 };
        IndustryTools.ConsumeResources(storage, ref need);
        Assert.That(need[raptor.UniqueID], Is.EqualTo(5), "only what is there is taken");
        Assert.That(storage.GetUnitsStoredByDesign(raptor), Is.EqualTo(0));
    }

    // ---- ship crew counts ----

    [Test]
    public void ShipCrew_AfterAPartSwap_ShowsOneConsistentNeedAndUnderCrewedMatchesIt()
    {
        Start(WarpTechStart);
        var info = _player.GetDataBlob<FactionInfoDB>();
        var earth = Colonies().First(c => c.FactionOwnerID == _player.Id && c.HasDataBlob<NavalAcademyDB>());
        var pool = ColonyCrewPoolDB.Of(earth);
        pool.Trained.Clear();
        pool.Add(earth.GetDataBlob<ColonyInfoDB>().Population.Keys.First(), 100);

        var design = info.ShipDesigns["default-ship-design-starship"];
        var ship = ShipFactory.CreateShip(design, _player, earth.GetDataBlob<ColonyInfoDB>().PlanetEntity, "Crewview Starship");
        ship.GetDataBlob<ShipInfoDB>().CommanderID = 4242; // a captain sits the ship: an officer, not crew
        var projector = new GameProjector(_game);
        ShipView View() => projector.ProjectEntity(ship, _player.Id).GetView<ShipView>()!;

        var v0 = View();
        Assert.That(v0.CrewRequired, Is.EqualTo(design.CrewReq), "needed is the part crew; the captain is an officer");
        Assert.That(v0.OfficersAboard, Is.EqualTo(1));
        Assert.That(v0.CrewByComponent.Sum(r => r.Crew), Is.EqualTo(v0.CrewRequired), "the rows add up to the need");
        Assert.That(v0.CrewAboard, Is.EqualTo(v0.CrewRequired));
        Assert.That(v0.UnderCrewed, Is.False);

        // Refit: one more crewed part and a berth for it; the design on the ship is left behind.
        var crewed = info.ShipDesigns.Values.SelectMany(d => d.Components).Select(c => c.design)
            .First(d => d.CrewReq > 0 && !ShipCrew.IsEngine(d) && !ShipCrew.IsBridge(d) && ShipCrew.BerthsPer(d) == 0);
        var quarters = info.ComponentDesigns["default-design-crew-quarters-1"];
        ship.AddComponent(new ComponentInstance(crewed));
        ship.AddComponent(new ComponentInstance(quarters));
        ship.AddComponent(new ComponentInstance(quarters));
        ShipCrew.AfterRefit(ship, earth);

        var split = ShipCrew.SplitOf(ship);
        var v1 = View();
        Assert.That(split.Required, Is.GreaterThan(v0.CrewRequired));
        Assert.That(v1.CrewRequired, Is.EqualTo(split.Required), "the view's need is the engine's need");
        Assert.That(v1.CrewBerths, Is.EqualTo(split.Berths));
        Assert.That(v1.CrewByComponent.Sum(r => r.Crew), Is.EqualTo(v1.CrewRequired));
        Assert.That(v1.UnderCrewed, Is.True, "aboard is below what is needed");
        Assert.That(v1.CrewAboard, Is.LessThan(v1.CrewRequired));
        Assert.That(split.Capacity, Is.EqualTo(split.Required), "everyone short has a berth");

        ShipCrew.TopUp(ship, earth);
        var v2 = View();
        Assert.That(v2.CrewAboard, Is.EqualTo(v2.CrewRequired));
        Assert.That(v2.UnderCrewed, Is.False, "topped up: aboard equals needed");
    }
}
