using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;
using Pulsar4X.Technology;
using Pulsar4X.Api;
using Pulsar4X.Engine.Api;

namespace Pulsar4X.Tests;

/// <summary>
/// Energy economy phase 2: the daily grid pass burns plant fuel from the stockpile, shares
/// power out by tier, scales industry, mining and research, and raises shortage and
/// fuel-out events. Scenario colonies start fully powered with a fuel runway.
/// </summary>
[TestFixture]
public class ColonyPowerEnforcementTests
{
    const double ThermalKgPerKJ = 1 / (0.33 * 42 * 1000);
    const double TurbineKgPerKJ = 1 / (0.35 * 10 * 1000);

    ModDataStore _store = null!;
    Game _game = null!;
    StarSystem _sol = null!;
    Entity _player = null!;
    Entity _species = null!;
    Entity _earth = null!;
    readonly List<Event> _events = new();

    [SetUp]
    public void SetUp()
    {
        _store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", _store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", _store);

        _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, _store);
        _game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(_game, _store.Systems["system-sol"]);

        _player = FactionFactory.CreateBasicFaction(_game, "United Earth Corp", "UEC", 0);
        _player.FactionOwnerID = _player.Id;
        _sol = _game.Systems.Single(s => s.ID == "system-sol");
        _species = SpeciesFactory.CreateFromBlueprint(_sol, _store.Species["species-human"]);
        _species.FactionOwnerID = _player.Id;
        _player.GetDataBlob<FactionInfoDB>().Species.Add(_species);

        var earth = NameLookup.GetFirstEntityWithName(_sol, "Earth");
        _earth = ColonyFactory.CreateFromBlueprint(_game, _player, _species, _sol, earth, _store.Colonies["colony-earth"]);

        _events.Clear();
        EventManager.Instance.Subscribe(EventType.PowerShortage, OnEvent);
        EventManager.Instance.Subscribe(EventType.PlantOutOfFuel, OnEvent);
    }

    [TearDown]
    public void TearDown()
    {
        EventManager.Instance.Unsubscribe(EventType.PowerShortage, OnEvent);
        EventManager.Instance.Unsubscribe(EventType.PlantOutOfFuel, OnEvent);
    }

    void OnEvent(Event e) => _events.Add(e);

    ColonyPowerDB Power => _earth.GetDataBlob<ColonyPowerDB>();

    // ---- brownout and priority ----

    [Test]
    public void Brownout_ServesTiersInOrder_AndScalesTheShortTier()
    {
        // 1,000 people (10 kW life support at the garden 0.01 kW, plus the trained pool) plus Earth's facilities: 2.2 MW of control (the 2 MW
        // spaceport), 0.5 MW of storage, idle industry, the mine and two idle labs (1.2 MW), about
        // 4.2 MW in all. One 4 MW thermal plant delivers 3,680 kW: research is the tier left short.
        SmallEarthOnPlant(TemplateDesign("thermal-plant", ("Size", 4)));

        ColonyPowerProcessor.RunDay(_earth);

        var p = Power;
        const double delivered = 4000 * 0.92;
        Assert.That(p.Demand_kW, Is.GreaterThan(delivered));
        Assert.That(p.Dispatched_kW, Is.EqualTo(4000).Within(1e-6), "the plant runs flat out");
        double left = delivered;
        foreach (PowerTier tier in Enum.GetValues(typeof(PowerTier)))
        {
            double demand = p.DemandByTier.TryGetValue(tier, out var d) ? d : 0;
            double expected = demand <= 0 ? 1 : Math.Min(1, left / demand);
            Assert.That(p.TierSatisfaction[tier], Is.EqualTo(expected).Within(1e-9), tier.ToString());
            left = Math.Max(0, left - demand);
        }
        Assert.That(p.TierSatisfaction[PowerTier.LifeSupport], Is.EqualTo(1));
        Assert.That(p.TierSatisfaction[PowerTier.Control], Is.EqualTo(1));
        Assert.That(p.TierSatisfaction[PowerTier.Research], Is.LessThan(1).And.GreaterThan(0), "research is last, and short");

        // More people: life support and control eat the plant, industry goes dark first.
        _earth.GetDataBlob<ColonyInfoDB>().Population[_species.Id] = 250_000; // 2,500 kW at the garden 0.01 kW each (crew plan phase 4)
        ColonyPowerProcessor.RunDay(_earth);
        Assert.That(p.DemandByTier[PowerTier.LifeSupport], Is.EqualTo(2500).Within(10), "250k people plus the small trained pool");
        Assert.That(p.TierSatisfaction[PowerTier.LifeSupport], Is.EqualTo(1));
        Assert.That(p.TierSatisfaction[PowerTier.Control], Is.EqualTo((delivered - p.DemandByTier[PowerTier.LifeSupport]) / p.DemandByTier[PowerTier.Control]).Within(1e-9));
        Assert.That(p.TierSatisfaction[PowerTier.Industry], Is.EqualTo(0));
        Assert.That(p.TierSatisfaction[PowerTier.Mining], Is.EqualTo(0));
        Assert.That(p.TierSatisfaction[PowerTier.Research], Is.EqualTo(0));
        Assert.That(ColonyPowerProcessor.Satisfaction(_earth, PowerTier.Charging), Is.EqualTo(1), "no demand, no penalty");
    }

    [Test]
    public void NoPlant_LifeSupportGetsWhateverThereIs_First()
    {
        SmallEarthOnPlant(TemplateDesign("thermal-plant", ("Size", 1)));
        var info = _earth.GetDataBlob<ColonyInfoDB>();
        info.Population[_species.Id] = 200_000; // 2 MW of life support at the garden 0.01 kW each, more than the plant
        ColonyPowerProcessor.RunDay(_earth);

        double lifeSupport = Power.DemandByTier[PowerTier.LifeSupport];
        Assert.That(lifeSupport, Is.EqualTo(2000).Within(10), "200k people plus the small trained pool");
        Assert.That(Power.TierSatisfaction[PowerTier.LifeSupport], Is.EqualTo(920.0 / lifeSupport).Within(1e-9));
        foreach (var tier in new[] { PowerTier.Control, PowerTier.Storage, PowerTier.Industry, PowerTier.Mining, PowerTier.Research })
            Assert.That(Power.TierSatisfaction[tier], Is.EqualTo(0), tier.ToString());

        RemoveAll(Power.Plants.Select(pl => pl.DesignId).ToList());
        ColonyPowerProcessor.RunDay(_earth);
        Assert.That(Power.Generation_kW, Is.EqualTo(0));
        Assert.That(Power.TierSatisfaction.Values, Is.All.EqualTo(0).Or.EqualTo(1));
        Assert.That(ColonyPowerProcessor.Satisfaction(_earth, PowerTier.LifeSupport), Is.EqualTo(0));
    }

    [Test]
    public void ColonyWithoutAGrid_IsUnaffected()
    {
        _earth.RemoveDataBlob<ColonyPowerDB>();
        Assert.That(ColonyPowerProcessor.Satisfaction(_earth, PowerTier.Industry), Is.EqualTo(1));
        Assert.That(ColonyPowerProcessor.Satisfaction(null, PowerTier.Research), Is.EqualTo(1));
    }

    // ---- the multipliers ----

    [Test]
    public void Mining_ScalesWithTheMiningTier()
    {
        double full = MinedInADay(1);
        double half = MinedInADay(0.5);
        double none = MinedInADay(0);
        Assert.That(full, Is.GreaterThan(0));
        Assert.That(half, Is.EqualTo(full / 2).Within(full * 0.02 + 15));
        Assert.That(none, Is.EqualTo(0));
    }

    [Test]
    public void Industry_ScalesWithTheIndustryTier()
    {
        double full = IndustryPointsInADay(1);
        double half = IndustryPointsInADay(0.5);
        Assert.That(full, Is.GreaterThan(10));
        Assert.That(half, Is.EqualTo(full / 2).Within(full * 0.05 + 2));
        Assert.That(IndustryPointsInADay(0), Is.EqualTo(0));
    }

    [Test]
    public void Research_ScalesWithTheResearchTier()
    {
        var lab = _sol.GetAllEntitiesWithDataBlob<ResearcherDB>()
            .First(l => l.GetDataBlob<ResearcherDB>().LocationId == _earth.Id);
        var db = lab.GetDataBlob<ResearcherDB>();
        Assert.That(ResearchProcessor.ResearchPowerEfficiency(lab, db), Is.EqualTo(1));

        Power.TierSatisfaction[PowerTier.Research] = 0.25;
        Assert.That(ResearchProcessor.ResearchPowerEfficiency(lab, db), Is.EqualTo(0.25));
    }

    // ---- fuel ----

    [Test]
    public void Fuel_IsEnergyOverEfficiencyTimesHeatingValue_FromTheStockpile()
    {
        // 5 MW thermal against 1,639.8 kW demand: the plant follows the load (demand / 0.92).
        SmallEarthOnPlant(TemplateDesign("thermal-plant", ("Size", 5)));
        double before = Stock("hydrocarbons");

        ColonyPowerProcessor.RunDay(_earth);

        double generated = Power.Demand_kW / ColonyPowerDB.DefaultGridEfficiency;
        double kg = generated * 86400 * ThermalKgPerKJ;
        Assert.That(Power.Dispatched_kW, Is.EqualTo(generated).Within(1e-6));
        Assert.That(Power.FuelBurnt_kgLastDay["hydrocarbons"], Is.EqualTo(kg).Within(1e-6));
        Assert.That(before - Stock("hydrocarbons"), Is.EqualTo(kg).Within(1e-6), "part units are carried, not lost");
        Assert.That(Power.TierSatisfaction.Values, Is.All.EqualTo(1));
    }

    [Test]
    public void Fission_BurnsGramsADay_AndTheMarketHoldsTheRunway()
    {
        // Earth's stock start: one 75 MW fission plant and 730 days of fuel at full load.
        Assert.That(ColonyPowerProcessor.FuelReserveUnits(_earth, "fissile-fuels"), Is.EqualTo(359));
        double before = Stock("fissile-fuels");
        ColonyPowerProcessor.RunDay(_earth);
        double kg = Power.Demand_kW / 0.92 * 86400 / (0.33 * 40e6 * 1000);
        Assert.That(before - Stock("fissile-fuels"), Is.EqualTo(kg).Within(1e-9));
        Assert.That(kg, Is.LessThan(0.5));
    }

    [Test]
    public void PlantStops_WhenItsFuelRunsOut_AndAnotherFuelTakesUpTheSlack()
    {
        SmallEarthOnPlant(TemplateDesign("thermal-plant", ("Size", 1)));
        // 5,000 people keep demand high enough that the thermal plant's share needs more than its stock.
        _earth.GetDataBlob<ColonyInfoDB>().Population[_species.Id] = 5000;
        SetStock("hydrocarbons", 3000);   // half a day at full load (6,234 kg/day)

        ColonyPowerProcessor.RunDay(_earth);

        Assert.That(Stock("hydrocarbons"), Is.EqualTo(0));
        Assert.That(Power.Dispatched_kW, Is.EqualTo(3000 / (86400 * ThermalKgPerKJ)).Within(1e-6));
        var line = Power.Plants.Single(p => p.FuelId == "hydrocarbons");
        Assert.That(line.Output_kW, Is.EqualTo(0));
        Assert.That(line.Note, Is.EqualTo(ColonyPowerProcessor.OutOfFuelNote));
        Assert.That(_events.Count(e => e.EventType == EventType.PlantOutOfFuel), Is.EqualTo(1));

        ColonyPowerProcessor.RunDay(_earth);
        Assert.That(Power.Dispatched_kW, Is.EqualTo(0));
        Assert.That(Power.TierSatisfaction[PowerTier.LifeSupport], Is.EqualTo(0));
        Assert.That(_events.Count(e => e.EventType == EventType.PlantOutOfFuel), Is.EqualTo(1), "one warning per run-out");

        // A methalox turbine alongside: when the thermal plant's fuel gives out mid-day,
        // the turbine covers the rest of the demand.
        SetStock("hydrocarbons", 3000);
        _earth.AddComponent(TemplateDesign("oxidiser-turbine", ("Size", 5)));
        double hydrocarbons = Stock("hydrocarbons");
        Assert.That(hydrocarbons, Is.GreaterThan(0).And.LessThan(6234), "less than a day at full load");
        ColonyPowerProcessor.RunDay(_earth);
        double demandGen = Power.Demand_kW / 0.92;
        double thermal = hydrocarbons / (86400 * ThermalKgPerKJ);
        Assert.That(Power.Dispatched_kW, Is.EqualTo(demandGen).Within(1e-6));
        Assert.That(Power.FuelBurnt_kgLastDay["methalox"],
            Is.EqualTo((demandGen - thermal) * 86400 * TurbineKgPerKJ).Within(1e-6));
        Assert.That(Power.TierSatisfaction.Values, Is.All.EqualTo(1));
        Assert.That(_events.Count(e => e.EventType == EventType.PlantOutOfFuel), Is.EqualTo(2), "back in stock, then out again");
    }

    [Test]
    public void Market_HoldsThePlantRunway_AndBidsForTheShortfall()
    {
        var full = PlannedListing("fissile-fuels");
        Assert.That(full, Is.Not.Null, "the fuel pile is listed");
        Assert.That(full!.SellQuantity, Is.EqualTo(0), "the whole stock is the runway");
        Assert.That(full.BuyQuantity, Is.EqualTo(0));

        SetStock("fissile-fuels", 100);
        var low = PlannedListing("fissile-fuels");
        Assert.That(low!.BuyQuantity, Is.EqualTo(359 - 100));
        Assert.That(low.SellQuantity, Is.EqualTo(0));
    }

    /// <summary>The listing the market pass wants for this cargo (posted or already standing).</summary>
    MarketListing? PlannedListing(string cargoId)
    {
        var plan = new RunMarketPlan().Plan(_earth, new Goal(GoalType.RunMarket), _earth.StarSysDateTime);
        var field = typeof(PostMarketListingAction).GetField("_listing",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var posted = plan.Actions.OfType<PostMarketListingAction>()
            .Select(a => (MarketListing)field.GetValue(a)!)
            .FirstOrDefault(l => l.CargoId == cargoId);
        if (posted != null)
            return posted;
        return MarketBook.TryGet(_earth, cargoId, out var standing) ? standing : null;
    }

    // ---- events ----

    [Test]
    public void ShortageEvent_OnceWhenItStarts_AndAgainAfterRecovery()
    {
        SmallEarthOnPlant(TemplateDesign("thermal-plant", ("Size", 1)));
        ColonyPowerProcessor.RunDay(_earth);
        ColonyPowerProcessor.RunDay(_earth);
        var shortages = _events.Where(e => e.EventType == EventType.PowerShortage).ToList();
        Assert.That(shortages, Has.Count.EqualTo(1));
        foreach (var (tier, share) in Power.TierSatisfaction.Where(kv => kv.Value < 1))
            Assert.That(shortages[0].Message, Does.Contain($"{ColonyPowerProcessor.TierName(tier)} {share * 100:0}%"));
        Assert.That(shortages[0].Message, Does.Contain("Research"));
        Assert.That(shortages[0].EntityId, Is.EqualTo(_earth.Id));
        Assert.That(shortages[0].FactionId, Is.EqualTo(_player.Id));

        _earth.AddComponent(TemplateDesign("thermal-plant", ("Size", 5)));
        ColonyPowerProcessor.RunDay(_earth);
        Assert.That(Power.Short, Is.False);
        Assert.That(_events.Count(e => e.EventType == EventType.PowerShortage), Is.EqualTo(1));

        var info = _earth.GetDataBlob<ColonyInfoDB>();
        info.Population[_species.Id] = 1_000_000;
        ColonyPowerProcessor.RunDay(_earth);
        Assert.That(_events.Count(e => e.EventType == EventType.PowerShortage), Is.EqualTo(2));
    }

    [Test]
    public void DailyProcessor_RunsTheDay()
    {
        double before = Stock("fissile-fuels");
        new ColonyPowerHotloopProcessor().ProcessManager(_sol, 86400);
        Assert.That(Stock("fissile-fuels"), Is.LessThan(before));
        Assert.That(Power.LastDispatch, Is.EqualTo(_earth.StarSysDateTime));
        Assert.That(new ColonyPowerHotloopProcessor().FirstRunOffset, Is.LessThan(new ResearchProcessor().FirstRunOffset));
        Assert.That(new ColonyPowerHotloopProcessor().FirstRunOffset, Is.LessThan(new MineResourcesProcessor().FirstRunOffset));
        Assert.That(new ColonyPowerHotloopProcessor().FirstRunOffset, Is.LessThan(new IndustryProcessor().FirstRunOffset));
    }

    // ---- TessaBot's phase 1 P3s ----

    [Test]
    public void RemovingAFacility_RecalcsTheGrid()
    {
        var lab = _earth.GetDataBlob<ComponentInstancesDB>().GetComponentsBySpecificDesign("default-design-research-lab").First();
        Assert.That(Power.Consumers.Single(c => c.DesignId == "default-design-research-lab").Count, Is.EqualTo(2));

        _earth.RemoveComponent(lab);

        // Removing a building also moves infrastructure efficiency, and with it the industry
        // lines' working draw, so check the lab line and the sum rather than a fixed delta.
        var line = Power.Consumers.Single(c => c.DesignId == "default-design-research-lab");
        Assert.That(line.Count, Is.EqualTo(1));
        Assert.That(line.Draw_kW, Is.EqualTo(0.2 * 3000).Within(1e-6), "the idle 3 MW lab left");
        Assert.That(Power.Demand_kW, Is.EqualTo(Power.Consumers.Sum(c => c.Draw_kW)).Within(1e-6));
    }

    [Test]
    public void PopulationChanges_ArePickedUp_ByThePopulationPass()
    {
        double before = Power.LifeSupport_kW;
        _earth.GetDataBlob<ColonyInfoDB>().Population[_species.Id] += 10_000;
        new PopulationProcessor().ProcessEntity(_earth, 0);
        // Pooled trained crew live at the colony and count too (crew plan phase 4).
        long people = _earth.GetDataBlob<ColonyInfoDB>().Population.Values.Sum()
            + (_earth.TryGetDataBlob<Pulsar4X.Colonies.ColonyCrewPoolDB>(out var pool) ? pool.Trained.Values.Sum() : 0);
        Assert.That(Power.LifeSupport_kW, Is.EqualTo(people * ColonyPowerProcessor.LifeSupportBase_kWPerColonist).Within(1e-6));
        Assert.That(Power.LifeSupport_kW, Is.GreaterThan(before));
    }

    [Test]
    public void PopulationGrowth_IsAnAnnualRate_AppliedPerMonth()
    {
        // 500,000 people: 20 / 0.5^(1/3) = 25 % a year, capped at 10 %. Compounded per 30-day tick.
        Assert.That(PopulationProcessor.GrowthPerTick(500_000), Is.EqualTo(Math.Pow(1.10, 30 / 365.25) - 1).Within(1e-12));
        // 8 million: 20 / 2 = 10 %. 1 billion: 20 / 10 = 2 %.
        Assert.That(PopulationProcessor.GrowthPerTick(8_000_000), Is.EqualTo(Math.Pow(1.10, 30 / 365.25) - 1).Within(1e-12));
        Assert.That(PopulationProcessor.GrowthPerTick(1_000_000_000), Is.EqualTo(Math.Pow(1.02, 30 / 365.25) - 1).Within(1e-12));
        Assert.That(PopulationProcessor.GrowthPerTick(0), Is.EqualTo(0));

        // A year of ticks multiplies to exactly the 10 % cap.
        double year = Math.Pow(1 + PopulationProcessor.GrowthPerTick(500_000), 365.25 / PopulationProcessor.TickDays);
        Assert.That(year, Is.EqualTo(1.10).Within(1e-12));
    }

    [Test]
    public void DamagedFacilities_DrawInProportionToTheirHealth()
    {
        var labs = _earth.GetDataBlob<ComponentInstancesDB>().GetComponentsBySpecificDesign("default-design-research-lab").ToList();
        labs[0].HealthPercent = 0.5f;
        ColonyPowerProcessor.Recalc(_earth);
        var line = Power.Consumers.Single(c => c.DesignId == "default-design-research-lab");
        Assert.That(line.Draw_kW, Is.EqualTo(0.2 * 3000 * 1.5).Within(1e-3), "two idle 3 MW labs, one at half health");
        Assert.That(line.Count, Is.EqualTo(2));
    }

    // ---- scenarios ----

    [TestCase(null, 4)]
    [TestCase("Pulsar4x.Scenario.EarlyTech", 1)]
    public void EveryScenarioColony_StartsFullyPowered_WithAFuelRunway(string? scenario, int colonies)
    {
        var game = StartGameLikeTheMenu(scenario);
        var all = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();
        Assert.That(all, Has.Count.EqualTo(colonies));
        var earth = all.Single(c => c.GetDataBlob<NameDB>().DefaultName.Contains("Earth"));
        Assert.That(earth.GetDataBlob<ColonyInfoDB>().Population.Values.Sum(), Is.EqualTo(500_000),
            "Earth starts as one corporation, not the planet");

        foreach (var colony in all)
        {
            string name = colony.GetDataBlob<NameDB>().DefaultName;
            var power = colony.GetDataBlob<ColonyPowerDB>();
            var fuels = power.Plants.Where(p => p.Kind == ColonyPowerPlantKind.Fuel).Select(p => p.FuelId!).Distinct().ToList();
            Assert.That(fuels, Is.Not.Empty, name + " has a fuel plant");
            foreach (var fuel in fuels)
            {
                long reserve = ColonyPowerProcessor.FuelReserveUnits(colony, fuel);
                Assert.That(ColonyPowerProcessor.FuelAvailable_kg(colony, power, fuel), Is.GreaterThanOrEqualTo(reserve),
                    $"{name}: {ColonyPowerProcessor.FuelReserveDays} days of {fuel} at full load");
            }

            ColonyPowerProcessor.RunDay(colony);

            Assert.That(power.Demand_kW, Is.GreaterThan(0), name);
            Assert.That(power.Delivered_kW, Is.GreaterThanOrEqualTo(power.Demand_kW), name);
            foreach (var (tier, share) in power.TierSatisfaction)
                Assert.That(share, Is.EqualTo(1), $"{name} {tier}");
            foreach (var fuel in fuels)
            {
                double perDay = power.FuelBurnt_kgLastDay[fuel];
                double left = ColonyPowerProcessor.FuelAvailable_kg(colony, power, fuel);
                Assert.That(left / perDay, Is.GreaterThan(ColonyPowerProcessor.FuelReserveDays),
                    $"{name}: runway at the day-1 load exceeds the full-load reserve");
            }
        }
    }

    // ---- helpers ----

    /// <summary>Earth with 1,000 people, its stock plants removed and this plant installed.</summary>
    void SmallEarthOnPlant(ComponentDesign plant)
    {
        _earth.GetDataBlob<ColonyInfoDB>().Population[_species.Id] = 1000;
        RemoveAll(Power.Plants.Select(p => p.DesignId).ToList());
        _earth.AddComponent(plant);
    }

    void RemoveAll(List<string> designIds)
    {
        var instances = _earth.GetDataBlob<ComponentInstancesDB>();
        foreach (var id in designIds)
            foreach (var c in instances.GetComponentsBySpecificDesign(id).ToList())
                _earth.RemoveComponent(c);
    }

    ICargoable Cargo(string id) => _player.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(id)!;

    double Stock(string id) => ColonyPowerProcessor.FuelAvailable_kg(_earth, Power, id);

    void SetStock(string id, long units)
    {
        var storage = _earth.GetDataBlob<CargoStorageDB>();
        var cargo = Cargo(id);
        long have = storage.GetUnitsStored(cargo, false);
        if (have > 0)
            storage.RemoveCargoByUnit(cargo, have);
        storage.AddCargoByUnit(cargo, units);
        Power.FuelBurnDebt_kg.Remove(id);
    }

    double MinedInADay(double share)
    {
        var mining = _earth.GetDataBlob<MiningDB>();
        Power.TierSatisfaction[PowerTier.Mining] = share;
        var storage = _earth.GetDataBlob<CargoStorageDB>();
        double before = storage.TypeStores.Values.Sum(s => s.CurrentStoreInUnits.Values.Sum());
        var processor = new MineResourcesProcessor();
        processor.Init(_game);
        processor.ProcessEntity(_earth, 0);
        return storage.TypeStores.Values.Sum(s => s.CurrentStoreInUnits.Values.Sum()) - before;
    }

    double IndustryPointsInADay(double share)
    {
        var industry = _earth.GetDataBlob<IndustryAbilityDB>();
        var info = _player.GetDataBlob<FactionInfoDB>();
        var (lineId, line) = industry.ProductionLines.First(l => l.Value.IndustryTypeRates.ContainsKey("refining"));
        foreach (var l in industry.ProductionLines.Values)
            l.Jobs.Clear();
        var job = new IndustryJob(info, "stainless-steel");
        job.InitialiseJob(500, false);
        line.Jobs.Add(job);
        SetStock("iron", 10_000_000);
        SetStock("chromium", 10_000_000);
        SetStock("nickel", 10_000_000);
        Power.TierSatisfaction[PowerTier.Industry] = share;
        new IndustryProcessor().ProcessEntity(_earth, 0);
        return job.NumberCompleted * (double)job.ProductionPointsCost + (job.ProductionPointsCost - job.ProductionPointsLeft);
    }

    ComponentDesign TemplateDesign(string templateId, params (string key, double value)[] properties)
    {
        var data = _player.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-p2-" + string.Join("-", properties.Select(p => p.value)) + "-" + Guid.NewGuid().ToString("N")[..6],
            Name = templateId + " " + string.Join(" ", properties.Select(p => p.value)),
            TemplateId = templateId,
            Properties = properties.Select(p => new ComponentDesignBlueprint.Property
            {
                Key = p.key,
                Value = new Newtonsoft.Json.Linq.JValue(p.value),
            }).ToList(),
        };
        return ComponentDesignFromJson.Create(_player, data, blueprint);
    }

    static Game StartGameLikeTheMenu(string? scenarioId)
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenarioId);
        var catalog = GameLifecycle.BuildCatalog(store);
        var systems = catalog.Systems.Where(s => s.StartingBodies.Count > 0).ToList();
        var request = new NewGameRequest(
            ModManifestPaths: paths,
            FactionName: "United Earth Corp",
            FactionAbbreviation: "UEC",
            SpeciesId: catalog.Species.First().Id,
            ColonyId: catalog.Colonies.First().Id,
            SystemId: systems.First().Id,
            BodyId: systems.First().StartingBodies.First().Id,
            EnabledSystems: systems.Select(s => s.Id).ToList(),
            MaxSystems: 10,
            MasterSeed: 12345678,
            StartingFunds: 100_000_000,
            EleStart: true,
            SMPassword: "",
            PlayerPassword: "");
        return GameLifecycle.CreateGameCore(store, request)!.Value.game;
    }
}
