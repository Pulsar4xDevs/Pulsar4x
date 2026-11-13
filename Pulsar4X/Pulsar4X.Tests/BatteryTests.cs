using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Battery rates: charge and discharge limits from the design's C-rate, the 90 % round trip
/// (all of the loss taken while charging), the ship step model and warp pulse, colony grid
/// batteries in the daily pass, the load refresh, and the battery fields of the power views.
/// Expected values are worked by hand from the template formulas, not read back from the engine.
/// </summary>
[TestFixture]
public class BatteryTests
{
    const double ThermalKgPerKJ = 1 / (0.33 * 42 * 1000);
    const double Day = 86400;

    ModDataStore _store = null!;
    Game _game = null!;
    StarSystem _sol = null!;
    Entity _player = null!;
    Entity _species = null!;
    Entity _earth = null!;
    DateTime _start;

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
        _game.LocalHumanFactionId = _player.Id;
        _sol = _game.Systems.Single(s => s.ID == "system-sol");
        _species = SpeciesFactory.CreateFromBlueprint(_sol, _store.Species["species-human"]);
        _species.FactionOwnerID = _player.Id;
        _player.GetDataBlob<FactionInfoDB>().Species.Add(_species);

        var earth = NameLookup.GetFirstEntityWithName(_sol, "Earth");
        _earth = ColonyFactory.CreateFromBlueprint(_game, _player, _species, _sol, earth, _store.Colonies["colony-earth"]);
        _start = _sol.StarSysDateTime;
    }

    ColonyPowerDB Power => _earth.GetDataBlob<ColonyPowerDB>();

    // ---- templates ----

    [Test]
    public void Templates_LiIonIsOneC_GridPackIsPointOneFiveC_BothNinetyPercentRoundTrip()
    {
        // battery-bank: 500 kJ/kg at tech level 0, 1C: capacity / 3600 s.
        var pack = FactionDesign("default-design-battery-2t").GetAttribute<EnergyStoreAtb>();
        Assert.That(pack.MaxStore, Is.EqualTo(2000 * 500).Within(1e-9));
        Assert.That(pack.MaxCharge_kW, Is.EqualTo(2000 * 500 / 3600.0).Within(1e-9));
        Assert.That(pack.MaxDischarge_kW, Is.EqualTo(2000 * 500 / 3600.0).Within(1e-9));
        Assert.That(pack.ChargeEfficiency, Is.EqualTo(0.9).Within(1e-12));
        Assert.That(pack.IsRated, Is.True);

        // grid-battery: 100 t default, 0.15C.
        var grid = TemplateDesign("grid-battery").GetAttribute<EnergyStoreAtb>();
        Assert.That(grid.MaxStore, Is.EqualTo(100000 * 500).Within(1e-9));
        Assert.That(grid.MaxCharge_kW, Is.EqualTo(100000 * 500 * 0.15 / 3600).Within(1e-9));
        Assert.That(grid.MaxDischarge_kW, Is.EqualTo(100000 * 500 * 0.15 / 3600).Within(1e-9));
        Assert.That(grid.ChargeEfficiency, Is.EqualTo(0.9).Within(1e-12));
    }

    [Test]
    public void OldTwoArgumentBattery_IsUnrated_NoLimitNoLoss()
    {
        var old = new EnergyStoreAtb("electricity", 1000);
        Assert.That(old.IsRated, Is.False);
        Assert.That(old.ChargeEfficiency, Is.EqualTo(1));
        // A saved design from before rates reads efficiency 0: that is lossless, not a dead battery.
        var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<EnergyStoreAtb>("{\"EnergyTypeID\":\"electricity\",\"MaxStore\":1000.0}")!;
        Assert.That(loaded.MaxStore, Is.EqualTo(1000));
        Assert.That(loaded.IsRated, Is.False);
        Assert.That(loaded.ChargeEfficiency, Is.EqualTo(1));
    }

    // ---- ship step model ----

    [Test]
    public void Ship_ChargeIsHeldToTheRate_LosesTenPercent_AndSpareOutputServesOtherLoads()
    {
        // 1000 kW reactor, 100 kW charge limit, 90 % charge efficiency, 50 kW of sensors.
        var ship = HandShip(reactorKw: 1000, fuelKgPerS: 0.002, fuel: 1000, stored: 0, storeMax: 1e6,
            charge: 100, discharge: 200, efficiency: 0.9);
        var db = ship.GetDataBlob<EnergyGenAbilityDB>();
        db.AuxDemand = 50;

        Assert.That(db.AuxServed, Is.EqualTo(50), "charging takes 100 kW of 1000 spare; the sensors run on the rest");
        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        // Stored: 100 kW x 0.9 x 100 s. Produced: 100 (charge) + 50 (sensors) = 150 kW.
        Assert.That(db.EnergyStored["electricity"], Is.EqualTo(9000).Within(1e-9));
        Assert.That(db.Load, Is.EqualTo(0.15).Within(1e-12));
        double burnt = 1000 - db.LocalFuel;
        Assert.That(burnt, Is.EqualTo(0.002 * 0.15 * 100).Within(1e-12));

        // Conservation: the fuel's electric energy = stored + charge loss + what the sensors used.
        double madeKJ = burnt / 0.002 * 1000;
        Assert.That(madeKJ, Is.EqualTo(9000 + 1000 + 50 * 100).Within(1e-6));

        Assert.That(EnergyGenProcessor.BatteryRate(db), Is.EqualTo(90).Within(1e-12));
    }

    [Test]
    public void Ship_DischargeIsHeldToTheRate_TheRestGoesUnmet()
    {
        var ship = HandShip(reactorKw: 100, fuelKgPerS: 0.001, fuel: 1000, stored: 1e5, storeMax: 1e5,
            charge: 100, discharge: 200, efficiency: 0.9);
        var db = ship.GetDataBlob<EnergyGenAbilityDB>();
        db.AddDemand(500, _start);   // 400 kW short of the plant; the battery gives 200 of it

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        Assert.That(db.EnergyStored["electricity"], Is.EqualTo(1e5 - 200 * 100).Within(1e-9), "no loss on discharge");
        Assert.That(db.Load, Is.EqualTo(1).Within(1e-12));
        Assert.That(1000 - db.LocalFuel, Is.EqualTo(0.001 * 100).Within(1e-12));
        Assert.That(EnergyGenProcessor.BatteryRate(db), Is.EqualTo(-200));
        var (tA, _, _) = EnergyGenProcessor.Phases(db);
        Assert.That(tA, Is.EqualTo(8e4 / 200).Within(1e-9), "empties at the rate, not the deficit");
    }

    [Test]
    public void Ship_FillingMidStep_BurnsTheSameFuelInOneStepOrTwo()
    {
        // 900 kJ of room fills at 100 kW x 0.9 in 10 s; then the plant follows a 20 kW load.
        Entity Make() => HandShip(reactorKw: 1000, fuelKgPerS: 0.002, fuel: 1000, stored: 0, storeMax: 900,
            charge: 100, discharge: 200, efficiency: 0.9);
        var one = Make();
        var two = Make();
        foreach (var e in new[] { one, two })
            e.GetDataBlob<EnergyGenAbilityDB>().AuxDemand = 20;

        EnergyGenProcessor.EnergyGen(one, _start.AddSeconds(100));
        EnergyGenProcessor.EnergyGen(two, _start.AddSeconds(10));
        EnergyGenProcessor.EnergyGen(two, _start.AddSeconds(100));

        // Charging: 100 + 20 kW for 10 s; then 20 kW for 90 s.
        double expected = 0.002 * (120.0 / 1000 * 10 + 20.0 / 1000 * 90);
        foreach (var e in new[] { one, two })
        {
            var db = e.GetDataBlob<EnergyGenAbilityDB>();
            Assert.That(db.EnergyStored["electricity"], Is.EqualTo(900).Within(1e-9));
            Assert.That(1000 - db.LocalFuel, Is.EqualTo(expected).Within(1e-12));
        }
    }

    [Test]
    public void Ship_ReactorRunningDryWhileCharging_SplitsTheStep_AndBurnsOnlyWhatItHad()
    {
        // 0.0001 kg at 0.002 kg/s x (100 / 1000 load) lasts 0.5 s.
        var ship = HandShip(reactorKw: 1000, fuelKgPerS: 0.002, fuel: 0.0001, stored: 0, storeMax: 1e6,
            charge: 100, discharge: 200, efficiency: 0.9);
        var db = ship.GetDataBlob<EnergyGenAbilityDB>();

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        Assert.That(db.LocalFuel, Is.EqualTo(0));
        Assert.That(db.EnergyStored["electricity"], Is.EqualTo(100 * 0.9 * 0.5).Within(1e-9));
    }

    [Test]
    public void WarpPulse_IsNotHeldToTheDischargeRate()
    {
        // A bubble creation books a 1 s pulse of its cost. The 200 kW discharge limit would let only
        // 200 kJ of a 50,000 kJ bubble through; the pulse takes it all from the store.
        var held = HandShip(reactorKw: 100, fuelKgPerS: 0.001, fuel: 1000, stored: 1e5, storeMax: 1e5,
            charge: 100, discharge: 200, efficiency: 0.9);
        var pulse = HandShip(reactorKw: 100, fuelKgPerS: 0.001, fuel: 1000, stored: 1e5, storeMax: 1e5,
            charge: 100, discharge: 200, efficiency: 0.9);
        foreach (var e in new[] { held, pulse })
            e.GetDataBlob<EnergyGenAbilityDB>().AddDemand(50_000, _start);

        EnergyGenProcessor.EnergyGen(held, _start.AddSeconds(1));
        EnergyGenProcessor.EnergyGenPulse(pulse, _start.AddSeconds(1));

        Assert.That(held.GetDataBlob<EnergyGenAbilityDB>().EnergyStored["electricity"], Is.EqualTo(1e5 - 200).Within(1e-9));
        var db = pulse.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(db.EnergyStored["electricity"], Is.EqualTo(1e5 - (50_000 - 100)).Within(1e-9));
        Assert.That(db.PulseUnclamped, Is.False, "the lift ends with the pulse");
        Assert.That(db.DischargeLimit_kW, Is.EqualTo(200));
    }

    [Test]
    public void StockWarpShip_StoreCanOpenTheBubble_AndOnlyThePulseExceedsTheRate()
    {
        // Stock warp ships carry 2-3 x 2 t packs. Their 1C discharge is far below the creation
        // pulse, which is why the pulse is lifted; their sustain stays within the plant.
        var reactor = FactionDesign("default-design-reactor-2t");
        var pack = FactionDesign("default-design-battery-2t");
        var drive = FactionDesign("default-design-alcubierre-2k");
        var ship = ComponentShip((reactor, 1), (pack, 2));
        var db = ship.GetDataBlob<EnergyGenAbilityDB>();
        double creation = drive.GetAttribute<Pulsar4X.Movement.WarpDriveAtb>().BubbleCreationCost;
        double storeMax = db.EnergyStoreMax["electricity"];
        TestContext.Out.WriteLine($"2k bubble creation {creation} kJ; store {storeMax} kJ; discharge limit {db.DischargeLimit_kW} kW; charge limit {db.ChargeLimit_kW} kW; plant {db.TotalOutputMax} kW; efficiency {db.ChargeEfficiency}");
        Assert.That(storeMax, Is.GreaterThanOrEqualTo(creation), "two packs hold the 2k drive's bubble");
        Assert.That(db.DischargeLimit_kW, Is.LessThan(creation), "a 1 s pulse is above the continuous rate");
    }

    [Test]
    public void ShipComponents_AddAndRemoveRates_AndTheLoadRebuildMatches()
    {
        var reactor = FactionDesign("default-design-reactor-2t");
        var pack = FactionDesign("default-design-battery-2t");
        double reactorKw = reactor.GetAttribute<EnergyGenerationAtb>().PowerOutputMax;
        double packKJ = 2000 * 500;
        var ship = ComponentShip((reactor, 1), (pack, 2));
        var db = ship.GetDataBlob<EnergyGenAbilityDB>();

        // Two packs at 1C plus the reactor's one-second buffer at its own output.
        Assert.That(db.StoreCharge_kW, Is.EqualTo(2 * packKJ / 3600 + reactorKw).Within(1e-6));
        Assert.That(db.StoreDischarge_kW, Is.EqualTo(2 * packKJ / 3600 + reactorKw).Within(1e-6));
        Assert.That(db.ChargeEfficiency, Is.EqualTo(1 - 2 * packKJ * 0.1 / (2 * packKJ + reactorKw)).Within(1e-12));

        (double, double, double, int) Rates() => (db.StoreCharge_kW, db.StoreDischarge_kW, db.StoreLoss_kJ, db.UnratedStores);
        var installed = Rates();
        db.StoreCharge_kW = 0;
        db.StoreDischarge_kW = 0;
        db.StoreLoss_kJ = 0;
        EnergyGenAbilityDB.RecomputeStoreRates(ship);
        Assert.That(Rates(), Is.EqualTo(installed));

        var instance = ship.GetDataBlob<ComponentInstancesDB>().GetComponentsBySpecificDesign(pack.UniqueID).First();
        ship.RemoveComponent(instance);
        Assert.That(db.StoreCharge_kW, Is.EqualTo(packKJ / 3600 + reactorKw).Within(1e-6));
        Assert.That(db.StoreLoss_kJ, Is.EqualTo(packKJ * 0.1).Within(1e-6));
    }

    // ---- colony grid ----

    [Test]
    public void Colony_SurplusCharges_HeldToTheRate_WithTheLoss()
    {
        _earth.AddComponent(TemplateDesign("grid-battery"));
        var power = Power;
        double rate = 100000 * 500 * 0.15 / 3600;          // 2,083 kW
        Assert.That(power.MaxCharge_kW, Is.EqualTo(rate).Within(1e-9));
        Assert.That(power.ChargeEfficiency, Is.EqualTo(0.9).Within(1e-12));
        double demand = power.Demand_kW;
        Assert.That(power.Delivered_kW - demand, Is.GreaterThan(rate), "Earth's seeded fission has room to charge");

        // A one-hour pass: room for 5e7 kJ would take 15,432 kW, so the 2,083 kW rate holds it.
        ColonyPowerProcessor.RunDay(_earth, 3600);
        Assert.That(power.BatteryCharge_kW, Is.EqualTo(rate).Within(1e-9));
        Assert.That(power.Stored_kJ, Is.EqualTo(rate * 0.9 * 3600).Within(1e-6));
        Assert.That(power.Battery_kW, Is.EqualTo(rate * 0.9).Within(1e-9));
        Assert.That(power.Dispatched_kW, Is.EqualTo((demand + rate) / power.GridEfficiency).Within(1e-6),
            "plants run for the charge on top of the demand");
        Assert.That(power.TierSatisfaction.Values, Is.All.EqualTo(1));

        // A full day: the room left is the limit, and the store ends full.
        double room = 5e7 - power.Stored_kJ;
        ColonyPowerProcessor.RunDay(_earth);
        Assert.That(power.BatteryCharge_kW, Is.EqualTo(room / (0.9 * Day)).Within(1e-9));
        Assert.That(power.Stored_kJ, Is.EqualTo(5e7).Within(1e-3));
        Assert.That(power.History.Last().Stored_kJ, Is.EqualTo(power.Stored_kJ));
        Assert.That(power.History.Last().Charge_kW, Is.EqualTo(power.BatteryCharge_kW));

        // Full: nothing more to charge, and the plants go back to the demand alone.
        ColonyPowerProcessor.RunDay(_earth);
        Assert.That(power.BatteryCharge_kW, Is.EqualTo(0));
        Assert.That(power.Battery_kW, Is.EqualTo(0));
        Assert.That(power.Dispatched_kW, Is.EqualTo(power.Demand_kW / power.GridEfficiency).Within(1e-6));
    }

    [Test]
    public void Colony_PlantRunsOutMidDay_BatteriesCoverTheShortfallBeforeAnyTierGoesShort()
    {
        SmallEarthOnPlant(TemplateDesign("thermal-plant", ("Size", 1)));
        _earth.GetDataBlob<ColonyInfoDB>().Population[_species.Id] = 5000;
        _earth.AddComponent(TemplateDesign("grid-battery"), 10);
        var power = Power;
        power.Stored_kJ = 5e8;                                  // ten full packs
        SetStock("hydrocarbons", 3000);                         // half a day at full load
        ColonyPowerProcessor.Recalc(_earth);
        double demand = power.Demand_kW;

        ColonyPowerProcessor.RunDay(_earth);

        double plant = 3000 / (Day * ThermalKgPerKJ);           // average kW the stock gave
        double delivered = plant * 0.92;
        double shortfall = demand - delivered;
        Assert.That(shortfall, Is.GreaterThan(0));
        // Ten packs give up to 20,833 kW, but 5e8 kJ over a day averages 5,787 kW.
        double discharge = Math.Min(shortfall, Math.Min(10 * 100000 * 500 * 0.15 / 3600, 5e8 / Day));
        Assert.That(power.BatteryDischarge_kW, Is.EqualTo(discharge).Within(1e-6));
        Assert.That(power.Battery_kW, Is.EqualTo(-discharge).Within(1e-6));
        Assert.That(power.Stored_kJ, Is.EqualTo(5e8 - discharge * Day).Within(1e-3));
        Assert.That(power.History.Last().Discharge_kW, Is.EqualTo(discharge).Within(1e-6));
        var expected = ColonyPowerProcessor.ShareByTier(delivered + discharge, power.DemandByTier);
        foreach (var (tier, share) in expected)
            Assert.That(power.TierSatisfaction[tier], Is.EqualTo(share).Within(1e-9), tier.ToString());
        Assert.That(power.TierSatisfaction[PowerTier.LifeSupport], Is.EqualTo(1), "the batteries keep life support up");

        // Without the batteries the same day leaves more of the demand short.
        var bare = ColonyPowerProcessor.ShareByTier(delivered, power.DemandByTier);
        Assert.That(bare.Values.Sum(), Is.LessThan(expected.Values.Sum()));
    }

    [Test]
    public void Colony_History_InterpolatesTheBatteryFieldsAcrossAGap()
    {
        var power = Power;
        var day0 = new DateTime(2100, 1, 1);
        power.RecordDay(new ColonyPowerSample(day0, 100, 200, 180, 0, 40, 0));
        power.RecordDay(new ColonyPowerSample(day0.AddDays(4), 100, 200, 180, 400, 0, 80));
        var mid = power.History[^3];
        Assert.That(mid.Date, Is.EqualTo(day0.AddDays(2)));
        Assert.That(mid.Stored_kJ, Is.EqualTo(200).Within(1e-9));
        Assert.That(mid.Charge_kW, Is.EqualTo(20).Within(1e-9));
        Assert.That(mid.Discharge_kW, Is.EqualTo(40).Within(1e-9));
    }

    // ---- save and load ----

    [Test]
    public void SaveRoundTrip_KeepsTheCharge_AndRefreshesDemandAndRatesOnLoad()
    {
        _earth.AddComponent(TemplateDesign("grid-battery"));
        ColonyPowerProcessor.RunDay(_earth);
        var power = Power;
        double stored = power.Stored_kJ;
        Assert.That(stored, Is.GreaterThan(0));
        var satisfaction = new Dictionary<PowerTier, double>(power.TierSatisfaction);
        double demand = power.Demand_kW;
        // Stale saved state: an old save's demand and a ship from before battery rates.
        power.Demand_kW = 1;
        power.Consumers = new List<ColonyPowerConsumer>();
        var ship = ComponentShip((FactionDesign("default-design-reactor-2t"), 1), (FactionDesign("default-design-battery-2t"), 2));
        var shipDb = ship.GetDataBlob<EnergyGenAbilityDB>();
        double rate = shipDb.StoreCharge_kW;
        shipDb.StoreCharge_kW = 0;
        shipDb.StoreDischarge_kW = 0;
        shipDb.StoreLoss_kJ = 0;
        shipDb.EnergyStored["electricity"] = 12345;

        string json = Game.Save(_game);
        var loaded = Game.Load(json);

        var sol = loaded.Systems.Single(s => s.ID == "system-sol");
        var colony = sol.GetAllEntitiesWithDataBlob<ColonyPowerDB>().Single(e => e.Id == _earth.Id);
        var after = colony.GetDataBlob<ColonyPowerDB>();
        Assert.That(after.Demand_kW, Is.EqualTo(demand).Within(1e-6), "demand recalculated on load");
        Assert.That(after.Consumers, Is.Not.Empty);
        Assert.That(after.Stored_kJ, Is.EqualTo(stored));
        Assert.That(after.MaxCharge_kW, Is.EqualTo(100000 * 500 * 0.15 / 3600).Within(1e-9));
        Assert.That(after.LastDispatch, Is.EqualTo(power.LastDispatch));
        Assert.That(after.TierSatisfaction, Is.EqualTo(satisfaction), "the last pass's satisfaction is kept");
        Assert.That(after.History.Last().Stored_kJ, Is.EqualTo(stored));

        var loadedShip = sol.GetAllEntitiesWithDataBlob<EnergyGenAbilityDB>().Single(e => e.Id == ship.Id).GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(loadedShip.StoreCharge_kW, Is.EqualTo(rate).Within(1e-9), "rates rebuilt from the components");
        Assert.That(loadedShip.ChargeEfficiency, Is.LessThan(1));
        Assert.That(loadedShip.EnergyStored["electricity"], Is.EqualTo(12345));
        Assert.That(loadedShip.PulseUnclamped, Is.False);
    }

    // ---- projection ----

    [Test]
    public void ShipPowerView_HasTheBatteryFields()
    {
        var ship = HandShip(reactorKw: 1000, fuelKgPerS: 0.002, fuel: 1000, stored: 1000, storeMax: 10000,
            charge: 100, discharge: 200, efficiency: 0.9);
        ship.SetDataBlob(new Pulsar4X.Ships.ShipInfoDB());
        var view = new GameProjector(_game).ProjectEntity(ship, _player.Id).GetView<ShipPowerView>()!;

        Assert.That(view.Battery_kW, Is.EqualTo(90).Within(1e-12));
        Assert.That(view.StoredFraction, Is.EqualTo(0.1).Within(1e-12));
        Assert.That(view.MaxCharge_kW, Is.EqualTo(100));
        Assert.That(view.MaxDischarge_kW, Is.EqualTo(200));
        Assert.That(view.TimeToFullOrEmpty_s, Is.EqualTo(9000 / 90.0).Within(1e-9));
        Assert.That(view.Charging_kW, Is.EqualTo(100), "plant side, held to the charge rate");
        Assert.That(view.FullIn_s, Is.EqualTo(9000 / 90.0).Within(1e-9));
        Assert.That(view.HasDispatched, Is.True);
        Assert.That(view.LastDispatch, Is.Null);

        // Draining: demand over output.
        ship.GetDataBlob<EnergyGenAbilityDB>().AddDemand(1500, _start);
        view = new GameProjector(_game).ProjectEntity(ship, _player.Id).GetView<ShipPowerView>()!;
        Assert.That(view.Battery_kW, Is.EqualTo(-200));
        Assert.That(view.TimeToFullOrEmpty_s, Is.EqualTo(1000 / 200.0).Within(1e-9));

        // No storage at all: zeros and null.
        var bare = HandShip(reactorKw: 10, fuelKgPerS: 0, fuel: 0, stored: 0, storeMax: 0, charge: 0, discharge: 0, efficiency: 1);
        bare.SetDataBlob(new Pulsar4X.Ships.ShipInfoDB());
        view = new GameProjector(_game).ProjectEntity(bare, _player.Id).GetView<ShipPowerView>()!;
        Assert.That(view.Battery_kW, Is.EqualTo(0));
        Assert.That(view.StoredFraction, Is.EqualTo(0));
        Assert.That(view.MaxCharge_kW, Is.EqualTo(0));
        Assert.That(view.TimeToFullOrEmpty_s, Is.Null);
    }

    [Test]
    public void ColonyPowerView_HasTheBatteryFields_AndAwaitsTheFirstPass()
    {
        _earth.AddComponent(TemplateDesign("grid-battery"));
        var projector = new GameProjector(_game);
        var view = projector.ProjectEntity(_earth, _player.Id).GetView<ColonyPowerView>()!;
        double rate = 100000 * 500 * 0.15 / 3600;
        Assert.That(view.HasDispatched, Is.False, "awaiting the daily pass");
        Assert.That(view.LastDispatch, Is.Null);
        Assert.That(view.StoredFraction, Is.EqualTo(0));
        Assert.That(view.MaxCharge_kW, Is.EqualTo(rate).Within(1e-9));
        Assert.That(view.MaxDischarge_kW, Is.EqualTo(rate).Within(1e-9));
        Assert.That(view.TimeToFullOrEmpty_s, Is.Null);

        ColonyPowerProcessor.RunDay(_earth, 3600);
        view = projector.ProjectEntity(_earth, _player.Id).GetView<ColonyPowerView>()!;
        Assert.That(view.HasDispatched, Is.True);
        Assert.That(view.LastDispatch, Is.EqualTo(_earth.StarSysDateTime));
        Assert.That(view.Battery_kW, Is.EqualTo(rate * 0.9).Within(1e-9));
        Assert.That(view.StoredFraction, Is.EqualTo(rate * 0.9 * 3600 / 5e7).Within(1e-12));
        Assert.That(view.TimeToFullOrEmpty_s, Is.EqualTo((5e7 - rate * 0.9 * 3600) / (rate * 0.9)).Within(1e-6));
        Assert.That(view.History.Last().Charge_kW, Is.EqualTo(rate).Within(1e-9));
        Assert.That(view.History.Last().Stored_kJ, Is.EqualTo(rate * 0.9 * 3600).Within(1e-6));
    }

    // ---- helpers ----

    Entity HandShip(double reactorKw, double fuelKgPerS, double fuel, double stored, double storeMax,
        double charge, double discharge, double efficiency)
    {
        var energyType = EnergyGenAbilityDB.FindEnergyType(_player, "electricity")!;
        var db = new EnergyGenAbilityDB(_start)
        {
            EnergyType = energyType,
            MaxOutputFromReactor = reactorKw,
            TotalFuelUseAtMax = (fuelKgPerS > 0 ? "fissile-fuels" : "", fuelKgPerS),
            LocalFuel = fuel,
            StoreCharge_kW = charge,
            StoreDischarge_kW = discharge,
            StoreLoss_kJ = storeMax * (1 - efficiency),
        };
        db.EnergyStored["electricity"] = stored;
        db.EnergyStoreMax["electricity"] = storeMax;
        var ship = Entity.Create(_player.Id);
        _sol.AddEntity(ship, new BaseDataBlob[]
        {
            new Pulsar4X.Movement.PositionDB { AbsolutePosition = Vector3.Zero },
            new MassVolumeDB { MassDry = 1000 },
            new NameDB("Battery test", _player.Id, "Battery test"),
            db,
        });
        return ship;
    }

    Entity ComponentShip(params (ComponentDesign design, int count)[] parts)
    {
        var ship = Entity.Create(_player.Id);
        _sol.AddEntity(ship, new BaseDataBlob[]
        {
            new Pulsar4X.Movement.PositionDB { AbsolutePosition = Vector3.Zero },
            new MassVolumeDB { MassDry = 1000 },
            new NameDB("Packs", _player.Id, "Packs"),
            new ComponentInstancesDB(),
        });
        foreach (var (design, count) in parts)
            ship.AddComponent(design, count);
        return ship;
    }

    void SmallEarthOnPlant(ComponentDesign plant)
    {
        _earth.GetDataBlob<ColonyInfoDB>().Population[_species.Id] = 1000;
        var instances = _earth.GetDataBlob<ComponentInstancesDB>();
        foreach (var id in Power.Plants.Select(p => p.DesignId).ToList())
            foreach (var c in instances.GetComponentsBySpecificDesign(id).ToList())
                _earth.RemoveComponent(c);
        _earth.AddComponent(plant);
    }

    void SetStock(string id, long units)
    {
        var storage = _earth.GetDataBlob<CargoStorageDB>();
        var cargo = _player.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(id)!;
        long have = storage.GetUnitsStored(cargo, false);
        if (have > 0)
            storage.RemoveCargoByUnit(cargo, have);
        storage.AddCargoByUnit(cargo, units);
        Power.FuelBurnDebt_kg.Remove(id);
    }

    ComponentDesign FactionDesign(string id) =>
        _player.GetDataBlob<FactionInfoDB>().InternalComponentDesigns[id];

    ComponentDesign TemplateDesign(string templateId, params (string key, double value)[] properties)
    {
        var data = _player.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-bat-" + string.Join("-", properties.Select(p => p.value)) + "-" + Guid.NewGuid().ToString("N")[..6],
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
}
