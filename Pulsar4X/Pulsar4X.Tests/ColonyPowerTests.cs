using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;

namespace Pulsar4X.Tests;

/// <summary>
/// Energy economy phase 1: the colony power grid (advisory). Demand from installations and
/// population, generation from plants, solar farms and batteries, priority sharing by tier.
/// </summary>
[TestFixture]
public class ColonyPowerTests
{
    ModDataStore _store = null!;
    Game _game = null!;
    StarSystem _sol = null!;
    Entity _player = null!;
    Entity _species = null!;
    Entity _earthColony = null!;

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
        _earthColony = ColonyFactory.CreateFromBlueprint(_game, _player, _species, _sol, earth, _store.Colonies["colony-earth"]);
    }

    ColonyPowerDB Power(Entity colony) => colony.GetDataBlob<ColonyPowerDB>();

    static ColonyPowerConsumer Line(ColonyPowerDB power, string designId) =>
        power.Consumers.Single(c => c.DesignId == designId);

    // ---- stock designs against the plan's §3 table ----

    [Test]
    public void EarthStart_StockDraws_MatchThePlan()
    {
        var power = Power(_earthColony);
        // Nameplates (phase 3: production lines draw their jobs' energy up to this).
        double PerUnit(string id) { var l = Line(power, id); return (l.Nameplate_kW > 0 ? l.Nameplate_kW : l.Draw_kW) / l.Count; }

        Assert.That(PerUnit("default-design-mine"), Is.EqualTo(140).Within(5), "extraction ~140 kW at full accessibility");
        Assert.That(PerUnit("default-design-refinery-site"), Is.InRange(100, 200), "refinery-site ~100-200 kW");
        Assert.That(PerUnit("default-design-factory-site"), Is.EqualTo(125).Within(1e-6), "factory-site 125 kW");
        Assert.That(PerUnit("default-design-shipyard"), Is.EqualTo(167 + 50).Within(1), "shipyard 167 kW + 50 kW hotel");
        Assert.That(Line(power, "default-design-local-construction-site").Draw_kW, Is.EqualTo(17).Within(0.5), "5 sites ~17 kW");
        Assert.That(Line(power, "default-design-research-lab").Draw_kW, Is.EqualTo(500).Within(1e-6), "2 labs 500 kW");
        Assert.That(PerUnit("default-design-city-hall"), Is.EqualTo(125).Within(1e-6), "City Hall 125 kW");
        Assert.That(PerUnit("default-design-logistics-office-site"), Is.EqualTo(20).Within(1e-6), "logistics site 20 kW");
        Assert.That(PerUnit("default-design-spaceport"), Is.EqualTo(75).Within(1), "spaceport ~75 kW");
        Assert.That(PerUnit("default-design-launch-complex"), Is.EqualTo(25).Within(1e-6), "launch complex 25 kW");
        Assert.That(PerUnit("default-design-warehouse"), Is.EqualTo(100).Within(1e-6), "1e6 m3 warehouse 100 kW");
        Assert.That(PerUnit("default-design-passive-sensor"), Is.EqualTo(50).Within(1e-6), "planet scanner 50 kW");

        Assert.That(power.Consumers.Any(c => c.DesignId.Contains("fuel-farm")), Is.False, "fuel farms draw later (cryogenic)");
        Assert.That(power.Consumers.Any(c => c.DesignId.Contains("infrastructure")), Is.False,
            "infrastructure's life support is the population line");
    }

    [Test]
    public void EarthStart_Tiers_FollowJonsOrder()
    {
        var power = Power(_earthColony);
        Assert.That(Line(power, ColonyPowerProcessor.LifeSupportLineId).Tier, Is.EqualTo(PowerTier.LifeSupport));
        Assert.That(Line(power, "default-design-city-hall").Tier, Is.EqualTo(PowerTier.Control));
        Assert.That(Line(power, "default-design-passive-sensor").Tier, Is.EqualTo(PowerTier.Control));
        Assert.That(Line(power, "default-design-warehouse").Tier, Is.EqualTo(PowerTier.Storage));
        Assert.That(Line(power, "default-design-refinery-site").Tier, Is.EqualTo(PowerTier.Industry));
        Assert.That(Line(power, "default-design-shipyard").Tier, Is.EqualTo(PowerTier.Industry));
        Assert.That(Line(power, "default-design-mine").Tier, Is.EqualTo(PowerTier.Mining));
        Assert.That(Line(power, "default-design-research-lab").Tier, Is.EqualTo(PowerTier.Research));

        var ranks = power.Consumers.Select(c => (int)c.Tier).ToList();
        Assert.That(ranks, Is.Ordered, "consumers are listed by priority");
        Assert.That((int)PowerTier.LifeSupport < (int)PowerTier.Control
                    && (int)PowerTier.Control < (int)PowerTier.Storage
                    && (int)PowerTier.Storage < (int)PowerTier.Industry
                    && (int)PowerTier.Industry < (int)PowerTier.Mining
                    && (int)PowerTier.Mining < (int)PowerTier.Charging
                    && (int)PowerTier.Charging < (int)PowerTier.Research, Is.True);
    }

    // ---- sums ----

    [Test]
    public void Recalc_SumsDemandByLineAndTier()
    {
        var power = Power(_earthColony);
        Assert.That(power.Demand_kW, Is.EqualTo(power.Consumers.Sum(c => c.Draw_kW)).Within(1e-6));
        Assert.That(power.Demand_kW, Is.EqualTo(power.DemandByTier.Values.Sum()).Within(1e-6));
        foreach (var tier in power.DemandByTier.Keys)
            Assert.That(power.DemandByTier[tier],
                Is.EqualTo(power.Consumers.Where(c => c.Tier == tier).Sum(c => c.Draw_kW)).Within(1e-6), tier.ToString());

        Assert.That(power.Generation_kW, Is.EqualTo(75000).Within(1e-6), "phase 2 seeds a 75 MW fission plant");
        foreach (var tier in power.DemandByTier.Keys)
            Assert.That(power.TierSatisfaction[tier], Is.EqualTo(1), "the seeded plant covers every tier");
    }

    [Test]
    public void AddingInstallations_RecalcsTheGrid()
    {
        var power = Power(_earthColony);
        double before = power.Demand_kW;
        var lab = FactionDesign("default-design-research-lab");

        _earthColony.AddComponent(lab, 3);

        // Labs draw their nameplate. The new buildings also move infrastructure efficiency,
        // which moves the industry lines' working draw, so check the lab line and the sum.
        Assert.That(Line(power, "default-design-research-lab").Draw_kW, Is.EqualTo(5 * 250).Within(1e-6));
        Assert.That(power.Demand_kW, Is.EqualTo(power.Consumers.Sum(c => c.Draw_kW)).Within(1e-6));
        Assert.That(power.Demand_kW, Is.GreaterThan(before + 3 * 250 - 100));
        Assert.That(Line(power, "default-design-research-lab").Count, Is.EqualTo(5));
    }

    [Test]
    public void DisabledInstallations_DrawNothing()
    {
        var power = Power(_earthColony);
        var labs = _earthColony.GetDataBlob<ComponentInstancesDB>().GetComponentsBySpecificDesign("default-design-research-lab").ToList();
        labs[0].IsEnabled = false;
        ColonyPowerProcessor.Recalc(_earthColony);
        Assert.That(Line(power, "default-design-research-lab").Draw_kW, Is.EqualTo(250).Within(1e-6));
    }

    // ---- life support ----

    [Test]
    public void LifeSupport_IsASmallBasePerColonist_OnAGardenWorld()
    {
        var power = Power(_earthColony);
        long people = _earthColony.GetDataBlob<ColonyInfoDB>().Population.Values.Sum();
        Assert.That(_species.GetDataBlob<SpeciesDB>().ColonyCost(_earthColony.GetDataBlob<ColonyInfoDB>().PlanetEntity), Is.EqualTo(0));
        Assert.That(power.LifeSupportColonists, Is.EqualTo(people));
        Assert.That(power.LifeSupport_kW, Is.EqualTo(people * ColonyPowerProcessor.LifeSupportBase_kWPerColonist).Within(1e-3),
            "non-hostile worlds pay less, but not zero");
    }

    [Test]
    public void LifeSupport_AddsAHostilityTerm_ThroughColonyCost()
    {
        var mars = NameLookup.GetFirstEntityWithName(_sol, "Mars");
        var colony = ColonyFactory.CreateColony(_player, _species, mars, 1000);
        var power = Power(colony);
        double cost = _species.GetDataBlob<SpeciesDB>().ColonyCost(mars);
        if (cost < 0 || cost > SpeciesDBExtensions.MAX_COST) cost = SpeciesDBExtensions.MAX_COST;

        Assert.That(cost, Is.GreaterThan(0), "Mars is hostile to humans");
        double perColonist = ColonyPowerProcessor.LifeSupportBase_kWPerColonist
                             + ColonyPowerProcessor.LifeSupportHostile_kWPerColonistPerCost * cost;
        Assert.That(power.LifeSupport_kW, Is.EqualTo(1000 * perColonist).Within(1e-6));
        Assert.That(power.LifeSupport_kW / 1000, Is.GreaterThan(ColonyPowerProcessor.LifeSupportBase_kWPerColonist));
    }

    [Test]
    public void LifeSupport_HostilityIsCappedAtMaxColonyCost()
    {
        var luna = NameLookup.GetFirstEntityWithName(_sol, "Luna");
        Assert.That(_species.GetDataBlob<SpeciesDB>().ColonyCost(luna), Is.GreaterThan(SpeciesDBExtensions.MAX_COST),
            "raw colony cost runs past the cap on Luna");
        var colony = ColonyFactory.CreateColony(_player, _species, luna, 1000);

        double perColonist = ColonyPowerProcessor.LifeSupportBase_kWPerColonist
                             + ColonyPowerProcessor.LifeSupportHostile_kWPerColonistPerCost * SpeciesDBExtensions.MAX_COST;
        Assert.That(Power(colony).LifeSupport_kW, Is.EqualTo(1000 * perColonist).Within(1e-6), "1.9 kW per colonist");
    }

    // ---- tier sharing ----

    [Test]
    public void ShareByTier_ServesHigherTiersFirst()
    {
        var demand = new Dictionary<PowerTier, double>
        {
            [PowerTier.LifeSupport] = 100,
            [PowerTier.Control] = 50,
            [PowerTier.Industry] = 100,
            [PowerTier.Research] = 100,
        };

        var share = ColonyPowerProcessor.ShareByTier(200, demand);

        Assert.That(share[PowerTier.LifeSupport], Is.EqualTo(1));
        Assert.That(share[PowerTier.Control], Is.EqualTo(1));
        Assert.That(share[PowerTier.Storage], Is.EqualTo(1), "no demand is satisfied");
        Assert.That(share[PowerTier.Industry], Is.EqualTo(0.5));
        Assert.That(share[PowerTier.Mining], Is.EqualTo(1));
        Assert.That(share[PowerTier.Research], Is.EqualTo(0), "research is last");

        Assert.That(ColonyPowerProcessor.ShareByTier(-5, demand)[PowerTier.LifeSupport], Is.EqualTo(0));
    }

    // ---- plants ----

    [Test]
    public void ThermalPlant_OnEarth_GivesItsNameplateAndTracksFuel()
    {
        var plant = TemplateDesign("thermal-plant", ("Size", 10));
        double seeded = Power(_earthColony).Generation_kW;
        _earthColony.AddComponent(plant, 2);
        var power = Power(_earthColony);

        var line = power.Plants.Single(p => p.DesignId == plant.UniqueID);
        Assert.That(line.Kind, Is.EqualTo(ColonyPowerPlantKind.Fuel));
        Assert.That(line.Output_kW, Is.EqualTo(20000).Within(1e-6));
        Assert.That(power.Generation_kW, Is.EqualTo(seeded + 20000).Within(1e-6));
        Assert.That(power.Delivered_kW, Is.EqualTo((seeded + 20000) * ColonyPowerDB.DefaultGridEfficiency).Within(1e-6));
        double kgPerDay = 20000 / (0.33 * 42 * 1000) * 86400;
        Assert.That(power.FuelAtFull_kgPerDay["hydrocarbons"], Is.EqualTo(kgPerDay).Within(1e-6));
        Assert.That(line.FuelAtFull_kgPerDay, Is.EqualTo(kgPerDay).Within(1e-6));
        Assert.That(_earthColony.HasDataBlob<EnergyGenAbilityDB>(), Is.False, "colonies do not run the ship model");
    }

    [Test]
    public void Plants_OnDifferentFuels_AreTrackedPerFuel()
    {
        var thermal = TemplateDesign("thermal-plant", ("Size", 10));
        var turbine = TemplateDesign("oxidiser-turbine", ("Size", 5));
        double seeded = Power(_earthColony).Generation_kW;
        _earthColony.AddComponent(thermal);
        _earthColony.AddComponent(turbine);
        var power = Power(_earthColony);

        Assert.That(power.Generation_kW, Is.EqualTo(seeded + 15000).Within(1e-6));
        Assert.That(power.FuelAtFull_kgPerDay.Keys, Is.EquivalentTo(new[] { "fissile-fuels", "hydrocarbons", "methalox" }));
        Assert.That(power.FuelAtFull_kgPerDay["methalox"], Is.EqualTo(5000 / (0.35 * 10 * 1000) * 86400).Within(1e-6));
    }

    [Test]
    public void ThermalPlant_WithoutOxygen_GivesNothing_ButATurbineDoes()
    {
        var mars = NameLookup.GetFirstEntityWithName(_sol, "Mars");
        var colony = ColonyFactory.CreateColony(_player, _species, mars, 1000);
        var thermal = TemplateDesign("thermal-plant", ("Size", 10));
        var turbine = TemplateDesign("oxidiser-turbine", ("Size", 5));
        colony.AddComponent(thermal);
        colony.AddComponent(turbine);
        // Plants need fuel in the stockpile (phase 2): both fuels are there, only one can burn.
        colony.AddComponent(FactionDesign("default-design-fuel-tank-3000"));
        var goods = _player.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
        Pulsar4X.Storage.CargoTransferProcessor.AddRemoveCargoMass(colony, goods.GetAny("methalox")!, 1000);
        Pulsar4X.Storage.CargoTransferProcessor.AddRemoveCargoMass(colony, goods.GetAny("hydrocarbons")!, 1000);
        ColonyPowerProcessor.Recalc(colony);
        var power = Power(colony);

        var dead = power.Plants.Single(p => p.DesignId == thermal.UniqueID);
        Assert.That(dead.Output_kW, Is.EqualTo(0));
        Assert.That(dead.Nameplate_kW, Is.EqualTo(10000).Within(1e-6));
        Assert.That(dead.Note, Does.Contain("oxygen"));
        Assert.That(power.FuelAtFull_kgPerDay.ContainsKey("hydrocarbons"), Is.False);
        Assert.That(power.Generation_kW, Is.EqualTo(5000).Within(1e-6));
    }

    [Test]
    public void FissionPlant_IsUnlockedByTheFissionPowerPlantTech_AfterEarlyNuclearPower()
    {
        // Phase 4: the plant moved to its own unlock, which Nuclear Power opens.
        Assert.That(_store.Techs["tech-early-nuclear-power"].Unlocks.Values.SelectMany(ids => ids), Does.Contain("tech-fission-power-plant"));
        Assert.That(_store.Techs["tech-fission-power-plant"].Unlocks.Values.SelectMany(ids => ids), Does.Contain("fission-plant"));
        var fission = TemplateDesign("fission-plant", ("Size", 50));
        var atb = fission.GetAttribute<PowerPlantAtb>();
        Assert.That(atb.FuelId, Is.EqualTo("fissile-fuels"));
        Assert.That(atb.Output_kW, Is.EqualTo(50000));
        Assert.That(atb.FuelAtFull_kgPerS * 86400, Is.EqualTo(50000 / (0.33 * 40e6 * 1000) * 86400).Within(1e-12));
        Assert.That(fission.MassPerUnit, Is.EqualTo(50 * 20000), "50 W/kg");
    }

    [Test]
    public void SolarFarm_AndGridBattery_FeedTheGrid()
    {
        var farm = TemplateDesign("solar-farm");
        var battery = TemplateDesign("grid-battery");
        double seeded = Power(_earthColony).Generation_kW;
        _earthColony.AddComponent(farm);
        _earthColony.AddComponent(battery, 2);
        var power = Power(_earthColony);

        var solar = power.Plants.Single(p => p.DesignId == farm.UniqueID);
        Assert.That(solar.Kind, Is.EqualTo(ColonyPowerPlantKind.Solar));
        Assert.That(solar.Output_kW, Is.GreaterThan(0));
        // 1e5 m² at 1 AU, a few % to ~20 % panel efficiency, half a day, Earth's air: 1-10 MW.
        Assert.That(solar.Output_kW, Is.InRange(1000, 10000));
        var earth = _earthColony.GetDataBlob<ColonyInfoDB>().PlanetEntity;
        Assert.That(ColonyPowerProcessor.SolarAtmosphereFactor(earth), Is.EqualTo(1 / (1 + 1.5 * earth.GetDataBlob<AtmosphereDB>().Pressure)).Within(1e-9));
        Assert.That(power.Generation_kW, Is.EqualTo(seeded + solar.Output_kW).Within(1e-6));

        double storePerUnit = battery.GetAttribute<EnergyStoreAtb>().MaxStore;
        Assert.That(power.StoreMax_kJ, Is.EqualTo(2 * storePerUnit).Within(1e-6));
        Assert.That(_earthColony.HasDataBlob<EnergyGenAbilityDB>(), Is.False, "colony atbs skip the ship model");
    }

    [Test]
    public void HostileWorlds_GetLessSunThroughThickAir_AndAllOfItWithout()
    {
        var luna = NameLookup.GetFirstEntityWithName(_sol, "Luna");
        Assert.That(ColonyPowerProcessor.SolarAtmosphereFactor(luna), Is.EqualTo(1).Within(0.01), "airless");
        Assert.That(ColonyPowerProcessor.HasAtmosphericOxygen(luna), Is.False);
        Assert.That(ColonyPowerProcessor.HasAtmosphericOxygen(_earthColony.GetDataBlob<ColonyInfoDB>().PlanetEntity), Is.True);
    }

    [Test]
    public void DailyProcessor_PicksUpPopulationChanges()
    {
        var info = _earthColony.GetDataBlob<ColonyInfoDB>();
        var power = Power(_earthColony);
        double before = power.LifeSupport_kW;
        info.Population[_species.Id] += 1000;

        new ColonyPowerHotloopProcessor().ProcessManager(_sol, 86400);

        Assert.That(power.LifeSupport_kW, Is.EqualTo(before + 1000 * ColonyPowerProcessor.LifeSupportBase_kWPerColonist).Within(1e-2));
    }

    // ---- history ----

    [Test]
    public void History_TakesOneSamplePerDailyPass()
    {
        var power = Power(_earthColony);
        Assert.That(power.History, Is.Empty, "no daily pass yet");

        // A watching client keeps the system in the foreground, where the daily pass runs
        // every day. A background system throttles hotloops tenfold.
        _sol.IncrementExternalObserver(priority: true);
        DateTime start = _game.TimePulse.GameGlobalDateTime;
        _game.TimePulse.TimeStep(start + TimeSpan.FromDays(3));
        Assert.That(_game.TimePulse.LastFault, Is.Null, _game.TimePulse.LastFault?.Report);

        Assert.That(power.History, Has.Count.EqualTo(3), "passes on day 0, day 1 and day 2");
        for (int i = 1; i < power.History.Count; i++)
            Assert.That(power.History[i].Date.Date - power.History[i - 1].Date.Date, Is.EqualTo(TimeSpan.FromDays(1)), "one sample per calendar day");
        var last = power.History[^1];
        Assert.That(last.Demand_kW, Is.EqualTo(power.Demand_kW).Within(1e-6));
        Assert.That(last.Generation_kW, Is.EqualTo(power.Generation_kW).Within(1e-6));
        Assert.That(last.Delivered_kW, Is.EqualTo(power.Dispatched_kW * power.GridEfficiency).Within(1e-6));
        Assert.That(last.Stored_kJ, Is.EqualTo(0), "batteries are not charged yet");

        // The clock now stands on day 3: a pass adds that day, and a second pass the same day
        // replaces it rather than adding another.
        ColonyPowerProcessor.RunDay(_earthColony);
        Assert.That(power.History, Has.Count.EqualTo(4));
        ColonyPowerProcessor.RunDay(_earthColony);
        Assert.That(power.History, Has.Count.EqualTo(4));
        Assert.That(power.History[^1].Date, Is.EqualTo(_sol.StarSysDateTime));
    }

    [Test]
    public void History_CapsAtNinetyDays_DroppingTheOldest()
    {
        var power = Power(_earthColony);
        var day0 = new DateTime(2100, 1, 1);
        for (int i = 0; i < 100; i++)
            power.RecordDay(new ColonyPowerSample(day0.AddDays(i), i, i, i, 0));

        Assert.That(ColonyPowerDB.HistoryDays, Is.EqualTo(90));
        Assert.That(power.History, Has.Count.EqualTo(90));
        Assert.That(power.History[0].Date, Is.EqualTo(day0.AddDays(10)));
        Assert.That(power.History[^1].Date, Is.EqualTo(day0.AddDays(99)));
        Assert.That(power.History.Select(h => h.Demand_kW), Is.EqualTo(Enumerable.Range(10, 90).Select(i => (double)i)));

        var copy = (ColonyPowerDB)power.Clone();
        Assert.That(copy.History, Is.EqualTo(power.History));
        Assert.That(copy.History, Is.Not.SameAs(power.History));
    }

    [Test]
    public void History_FillsAGapWithInterpolatedDays()
    {
        var power = Power(_earthColony);
        var day0 = new DateTime(2100, 1, 1, 6, 0, 0);
        power.RecordDay(new ColonyPowerSample(day0, 100, 200, 50, 0));
        power.RecordDay(new ColonyPowerSample(day0.AddDays(10), 300, 400, 250, 1000));

        Assert.That(power.History, Has.Count.EqualTo(11), "the first sample, 9 filled days and the new sample");
        for (int i = 0; i < power.History.Count; i++)
            Assert.That(power.History[i].Date, Is.EqualTo(day0.AddDays(i)), "consecutive days");

        var mid = power.History[5];
        Assert.That(mid.Demand_kW, Is.EqualTo(200).Within(1e-9));
        Assert.That(mid.Generation_kW, Is.EqualTo(300).Within(1e-9));
        Assert.That(mid.Delivered_kW, Is.EqualTo(150).Within(1e-9));
        Assert.That(mid.Stored_kJ, Is.EqualTo(500).Within(1e-9));

        var third = power.History[3];
        Assert.That(third.Demand_kW, Is.EqualTo(160).Within(1e-9));
        Assert.That(third.Generation_kW, Is.EqualTo(260).Within(1e-9));
        Assert.That(third.Delivered_kW, Is.EqualTo(110).Within(1e-9));
        Assert.That(third.Stored_kJ, Is.EqualTo(300).Within(1e-9));

        Assert.That(power.History[^1], Is.EqualTo(new ColonyPowerSample(day0.AddDays(10), 300, 400, 250, 1000)));
    }

    [Test]
    public void History_GapLongerThanTheCap_KeepsTheLastNinetyDays()
    {
        var power = Power(_earthColony);
        var day0 = new DateTime(2100, 1, 1);
        power.RecordDay(new ColonyPowerSample(day0, 0, 0, 0, 0));
        var end = day0.AddDays(1000);
        power.RecordDay(new ColonyPowerSample(end, 1000, 2000, 3000, 4000));

        Assert.That(power.History, Has.Count.EqualTo(ColonyPowerDB.HistoryDays));
        for (int i = 0; i < power.History.Count; i++)
            Assert.That(power.History[i].Date, Is.EqualTo(end.AddDays(i - (ColonyPowerDB.HistoryDays - 1))), "consecutive days ending at the new sample");
        var first = power.History[0];
        Assert.That(first.Demand_kW, Is.EqualTo(911).Within(1e-9), "day 911 of a 0→1000 ramp");
        Assert.That(first.Stored_kJ, Is.EqualTo(3644).Within(1e-9));
        Assert.That(power.History[^1].Demand_kW, Is.EqualTo(1000));
    }

    [Test]
    public void History_NullFromAnOldSave_IsTreatedAsEmpty()
    {
        var power = Power(_earthColony);
        power.History = null!;

        var view = new GameProjector(_game).ProjectEntity(_earthColony, _player.Id).GetView<ColonyPowerView>();
        Assert.That(view!.History, Is.Empty);

        var copy = (ColonyPowerDB)power.Clone();
        Assert.That(copy.History, Is.Not.Null.And.Empty);

        var day0 = new DateTime(2100, 1, 1);
        power.RecordDay(new ColonyPowerSample(day0, 300, 400, 250, 0));
        Assert.That(power.History, Has.Count.EqualTo(1));
    }

    [Test]
    public void Projector_ShowsPowerHistoryToTheOwnerOnly()
    {
        var power = Power(_earthColony);
        var day0 = new DateTime(2100, 1, 1);
        power.RecordDay(new ColonyPowerSample(day0, 300, 400, 250, 0));
        power.RecordDay(new ColonyPowerSample(day0.AddDays(1), 320, 400, 320, 0));

        var projector = new GameProjector(_game);
        var view = projector.ProjectEntity(_earthColony, _player.Id).GetView<ColonyPowerView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.History, Is.EqualTo(new[]
        {
            new ColonyPowerSampleView(day0, 300, 400, 250, 0),
            new ColonyPowerSampleView(day0.AddDays(1), 320, 400, 320, 0),
        }));

        var rival = FactionFactory.CreateBasicFaction(_game, "Rival", "RVL", 0);
        Assert.That(projector.ProjectEntity(_earthColony, rival.Id).HasView<ColonyPowerView>(), Is.False);
    }

    // ---- API ----

    [Test]
    public void Projector_ShowsThePowerViewToTheOwnerOnly()
    {
        _earthColony.AddComponent(TemplateDesign("thermal-plant", ("Size", 10)));
        var projector = new GameProjector(_game);
        var mine = projector.ProjectEntity(_earthColony, _player.Id);
        var view = mine.GetView<ColonyPowerView>();

        Assert.That(view, Is.Not.Null);
        var power = Power(_earthColony);
        Assert.That(view!.Demand_kW, Is.EqualTo(power.Demand_kW).Within(1e-6));
        Assert.That(view.Generation_kW, Is.EqualTo(75000 + 10000).Within(1e-6), "seeded fission plus the thermal plant");
        Assert.That(view.Balance_kW, Is.EqualTo(power.Balance_kW).Within(1e-6));
        Assert.That(view.Consumers[0].Tier, Is.EqualTo("Life support"));
        Assert.That(view.Consumers[0].TierRank, Is.EqualTo(1));
        Assert.That(view.Tiers.Select(t => t.TierRank), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
        Assert.That(view.Plants, Has.Count.EqualTo(2));
        Assert.That(view.Plants.Select(p => p.FuelName), Is.All.Not.Empty);
        Assert.That(view.Fuel.Select(f => f.FuelId), Is.EquivalentTo(new[] { "fissile-fuels", "hydrocarbons" }));
        var fissile = view.Fuel.Single(f => f.FuelId == "fissile-fuels");
        Assert.That(fissile.Stock_kg, Is.EqualTo(359).Within(1e-9));
        Assert.That(fissile.RunwayDays, Is.Null, "no day run yet");
        ColonyPowerProcessor.RunDay(_earthColony);
        fissile = projector.ProjectEntity(_earthColony, _player.Id).GetView<ColonyPowerView>()!.Fuel.Single(f => f.FuelId == "fissile-fuels");
        Assert.That(fissile.BurntLastDay_kg, Is.GreaterThan(0));
        Assert.That(fissile.RunwayDays, Is.EqualTo(fissile.Stock_kg / fissile.BurntLastDay_kg).Within(1e-6));

        var rival = FactionFactory.CreateBasicFaction(_game, "Rival", "RVL", 0);
        Assert.That(projector.ProjectEntity(_earthColony, rival.Id).HasView<ColonyPowerView>(), Is.False);
    }

    // ---- helpers ----

    ComponentDesign FactionDesign(string id) =>
        _player.GetDataBlob<FactionInfoDB>().InternalComponentDesigns[id];

    ComponentDesign TemplateDesign(string templateId, params (string key, double value)[] properties)
    {
        var data = _player.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-test-" + string.Join("-", properties.Select(p => p.value)),
            Name = templateId,
            TemplateId = templateId,
            Properties = properties.Select(p => new ComponentDesignBlueprint.Property
            {
                Key = p.key,
                Value = new Newtonsoft.Json.Linq.JValue(p.value),
            }).ToList(),
        };
        return ComponentDesignFromJson.Create(_player, data, blueprint);
    }
}
