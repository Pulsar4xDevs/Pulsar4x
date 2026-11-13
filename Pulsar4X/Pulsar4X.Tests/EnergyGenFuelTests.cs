using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Energy economy phase 0: reactor load and fuel, fuel summing, running dry,
/// old-save fuel clamp, uninstall, and the RTG's units.
/// </summary>
[TestFixture]
public class EnergyGenFuelTests
{
    const double ReactorKw = 15;
    const double FuelAtMaxKgS = 0.001;

    Game _game = null!;
    StarSystem _sys = null!;
    Entity _faction = null!;
    DateTime _start;

    [SetUp]
    public void SetUp()
    {
        _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, Load());
        _faction = FactionFactory.CreateBasicFaction(_game, "Power", "PWR", 0);
        _sys = new StarSystem();
        _sys.Initialize(_game, "Power");
        _start = _sys.StarSysDateTime;
    }

    [Test]
    public void SteadyDemand_WithAFullBattery_BurnsFuelAtTheLoad()
    {
        var ship = ShipWithReactor(stored: 100, storeMax: 100, fuel: 1000);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        power.AddDemand(7.5, _start);

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(100).Within(1e-9));
        Assert.That(power.Load, Is.EqualTo(0.5).Within(1e-9));
        Assert.That(power.LocalFuel, Is.EqualTo(1000 - FuelAtMaxKgS * 0.5 * 100).Within(1e-9));
    }

    [Test]
    public void Charging_IsFullLoad_AndLoadIsClampedToOne()
    {
        var ship = ShipWithReactor(stored: 0, storeMax: 1e6, fuel: 1000);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(10));

        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(150).Within(1e-9));
        Assert.That(power.Load, Is.EqualTo(1).Within(1e-9));
        Assert.That(power.LocalFuel, Is.EqualTo(1000 - FuelAtMaxKgS * 10).Within(1e-9));

        // Demand above output drains the battery; load stays at 1.
        power.AddDemand(30, _start.AddSeconds(10));
        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(20));
        Assert.That(power.Load, Is.EqualTo(1).Within(1e-9));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(0).Within(1e-9));
    }

    [Test]
    public void IdleWithAFullBattery_BurnsNoFuel()
    {
        var ship = ShipWithReactor(stored: 100, storeMax: 100, fuel: 1000);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();

        EnergyGenProcessor.EnergyGen(ship, _start.AddDays(1));

        Assert.That(power.Load, Is.EqualTo(0));
        Assert.That(power.LocalFuel, Is.EqualTo(1000));
    }

    [Test]
    public void FuelRunsOutMidStep_ReactorStopsAtTheEmptyInstant()
    {
        // Charging at 15 kW burns 0.001 kg/s, so 0.05 kg lasts 50 s.
        var ship = ShipWithReactor(stored: 500, storeMax: 1e6, fuel: 0.05);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        Assert.That(power.LocalFuel, Is.EqualTo(0));
        Assert.That(power.ReactorOutOfFuel, Is.True);
        Assert.That(power.TotalOutputMax, Is.EqualTo(0));
        Assert.That(power.MaxOutputFromReactor, Is.EqualTo(ReactorKw), "the nameplate is kept");
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(500 + ReactorKw * 50).Within(1e-3));

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(1000));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(500 + ReactorKw * 50).Within(1e-3));
    }

    [Test]
    public void ZeroFuel_GivesZeroOutput_AndDemandDrainsTheBattery()
    {
        var ship = ShipWithReactor(stored: 1000, storeMax: 1000, fuel: 0);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(power.TotalOutputMax, Is.EqualTo(0));

        power.AddDemand(5, _start);
        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(500).Within(1e-9));
        Assert.That(power.LocalFuel, Is.EqualTo(0));
    }

    [Test]
    public void APlantThatTracksNoFuel_NeverRunsDry()
    {
        // Test rigs and fuel-free plants set MaxOutputFromReactor with no fuel use.
        var ship = ShipWithReactor(stored: 0, storeMax: 1e6, fuel: 0, fuelAtMax: 0);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(10));

        Assert.That(power.ReactorOutOfFuel, Is.False);
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(150).Within(1e-9));
    }

    [TestCase(-5d)]
    [TestCase(double.NaN)]
    [TestCase(double.NegativeInfinity)]
    public void OldSave_OddFuelIsClampedToZeroOnLoad(double savedFuel)
    {
        var blob = new EnergyGenAbilityDB(_start)
        {
            MaxOutputFromReactor = ReactorKw,
            TotalFuelUseAtMax = ("fissile-fuels", FuelAtMaxKgS),
            LocalFuel = savedFuel,
        };

        var loaded = RoundTrip(blob);

        Assert.That(loaded.LocalFuel, Is.EqualTo(0));
        Assert.That(loaded.ReactorOutOfFuel, Is.True);
    }

    [Test]
    public void OldSave_InfiniteFuelBecomesMaxValue_AndGoodFuelIsKept()
    {
        var blob = new EnergyGenAbilityDB(_start) { LocalFuel = double.PositiveInfinity };
        Assert.That(RoundTrip(blob).LocalFuel, Is.EqualTo(double.MaxValue));

        blob.LocalFuel = 600;
        Assert.That(RoundTrip(blob).LocalFuel, Is.EqualTo(600));
    }

    [Test]
    public void NegativeFuel_IsClampedWhenProcessed()
    {
        var ship = ShipWithReactor(stored: 0, storeMax: 1000, fuel: -3);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(10));

        Assert.That(power.LocalFuel, Is.EqualTo(0));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(0));
    }

    [Test]
    public void Install_SumsFuelPerReactor()
    {
        var ship = OwnedShip();
        var a = new EnergyGenerationAtb("fissile-fuels", 0.001, "electricity", 15, 1000);
        var b = new EnergyGenerationAtb("fissile-fuels", 0.002, "electricity", 10, 500);

        a.OnComponentInstallation(ship, null!);
        b.OnComponentInstallation(ship, null!);

        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(power.EnergyType, Is.Not.Null, "electricity resolves though it is not a starting unlock");
        Assert.That(power.EnergyType!.UniqueID, Is.EqualTo("electricity"));
        Assert.That(power.LocalFuel, Is.EqualTo(1.0 + 1.0).Within(1e-12));
        Assert.That(power.MaxOutputFromReactor, Is.EqualTo(25));
        Assert.That(power.TotalFuelUseAtMax.maxUse, Is.EqualTo(0.003).Within(1e-12));
        Assert.That(power.EnergyStoreMax["electricity"], Is.EqualTo(25));
    }

    [Test]
    public void Uninstall_ReversesGenStoreAndSolar()
    {
        var ship = OwnedShip();
        var a = new EnergyGenerationAtb("fissile-fuels", 0.001, "electricity", 15, 1000);
        var b = new EnergyGenerationAtb("fissile-fuels", 0.002, "electricity", 10, 500);
        var battery = new EnergyStoreAtb("electricity", 1000);
        var panel = new EnergySolarGenerationAtb(550, 300, 20, 10, 50);
        a.OnComponentInstallation(ship, null!);
        b.OnComponentInstallation(ship, null!);
        battery.OnComponentInstallation(ship, null!);
        panel.OnComponentInstallation(ship, null!);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        power.EnergyStored["electricity"] = 1025;
        power.MaxOutputFromSolar = 3;

        b.OnComponentUninstallation(ship, null!);
        Assert.That(power.MaxOutputFromReactor, Is.EqualTo(15));
        Assert.That(power.TotalFuelUseAtMax.maxUse, Is.EqualTo(0.001).Within(1e-12));
        Assert.That(power.LocalFuel, Is.EqualTo(2.0 / 3.0).Within(1e-9), "b takes its share of what is left");
        Assert.That(power.EnergyStoreMax["electricity"], Is.EqualTo(1015));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(1015), "the store is clamped to the new cap");

        battery.OnComponentUninstallation(ship, null!);
        Assert.That(power.EnergyStoreMax["electricity"], Is.EqualTo(15));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(15));

        panel.OnComponentUninstallation(ship, null!);
        Assert.That(power.SolarPanels, Is.Empty);
        Assert.That(power.MaxOutputFromSolar, Is.EqualTo(0));

        a.OnComponentUninstallation(ship, null!);
        Assert.That(power.MaxOutputFromReactor, Is.EqualTo(0));
        Assert.That(power.TotalFuelUseAtMax.maxUse, Is.EqualTo(0));
        Assert.That(power.TotalFuelUseAtMax.type, Is.EqualTo(""));
        Assert.That(power.LocalFuel, Is.EqualTo(0));
        Assert.That(power.EnergyStoreMax["electricity"], Is.EqualTo(0));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(0));
    }

    [Test]
    public void Description_SaysKilowatts()
    {
        var atb = new EnergyGenerationAtb("fissile-fuels", 0.001, "electricity", 15, 1000);
        Assert.That(atb.AtbDescription(), Does.Contain("15 kW"));
        Assert.That(atb.AtbDescription(), Does.Not.Contain("Mw"));
    }

    [Test]
    public void Rtg_FuelLastsItsOperationalLifetime_InSeconds()
    {
        var data = _faction.GetDataBlob<FactionInfoDB>().Data;
        foreach (var good in data.LockedCargoGoods.GetAll().Values.Select(cargo => cargo.UniqueID).ToList())
            data.Unlock(good);
        ComponentDesigner.StartResearched = true;
        try
        {
            data.Unlock("tech-conductors");
            data.Unlock("rtg");
            var design = ComponentDesignFromJson.Create(_faction, data, new ComponentDesignBlueprint
            {
                UniqueID = "phase0-rtg",
                Name = "rtg",
                TemplateId = "rtg",
            });
            var gen = design.GetAttribute<EnergyGenerationAtb>();

            const double years = 5;
            const double fuelKg = 500; // [Mass] 1000 * 0.5
            Assert.That(gen.Lifetime, Is.EqualTo(years * 31557600).Within(1e-3));
            Assert.That(gen.FuelUsedAtMax * gen.Lifetime, Is.EqualTo(fuelKg).Within(1e-9),
                "LocalFuel from one RTG is its fuel mass, in kg");
            // Fuel kg * (0.1 / years) kW/kg * Efficiency % * 0.01. Efficiency is 10 + tech-conductors (0 here).
            Assert.That(gen.PowerOutputMax, Is.EqualTo(fuelKg * (0.1 / years) * 10 * 0.01).Within(1e-9));
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    [Test]
    public void Electricity_IsNotBuildableAndNotAStartingUnlock()
    {
        var store = Load();
        var electricity = store.ProcessedMaterials["electricity"];
        Assert.That(electricity.CargoTypeID, Is.EqualTo("battery-storage"), "the cargo type stays");
        Assert.That(electricity.IndustryTypeID, Is.Null.Or.Empty);
        Assert.That(electricity.OutputAmount, Is.EqualTo(0));
        Assert.That(electricity.IndustryPointCosts, Is.EqualTo(0));

        foreach (var colony in store.Colonies.Values)
            Assert.That(colony.StartingItems ?? new System.Collections.Generic.List<string>(),
                Does.Not.Contain("electricity").And.Not.Contain("reactor"), colony.UniqueID);
        Assert.That(store.Techs["tech-early-nuclear-power"].Unlocks.Values.SelectMany(ids => ids),
            Does.Not.Contain("reactor"));
        Assert.That(store.ComponentTemplates.ContainsKey("reactor"), Is.True, "the template stays for old designs");
    }

    // ---- phase 1 folds in the phase 0 review P3s ----

    [Test]
    public void SolarCarriesTheLoadFirst_TheReactorBurnsOnlyForTheRest()
    {
        var ship = ShipWithReactor(stored: 100, storeMax: 100, fuel: 1000);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        power.MaxOutputFromSolar = 5;
        power.AddDemand(10, _start);

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        // 10 kW demand: 5 from solar, 5 from the 15 kW reactor (a third of full burn).
        Assert.That(power.LocalFuel, Is.EqualTo(1000 - FuelAtMaxKgS * (5.0 / 15.0) * 100).Within(1e-9));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(100).Within(1e-9));
        Assert.That(power.Load, Is.EqualTo(10.0 / 20.0).Within(1e-9));
    }

    [Test]
    public void FuelRunsOut_AfterTheStoreFills_AtTheExactInstant()
    {
        // 15 kW reactor, 5 kW demand, 100 kJ free: full output fills the store in 10 s
        // (0.010 kg), then the reactor follows the 5 kW demand at 1/3 burn (1/3000 kg/s).
        // 0.012 kg lasts 10 + 0.002 * 3000 = 16 s. A uniform-rate estimate would say 30 s.
        var ship = ShipWithReactor(stored: 900, storeMax: 1000, fuel: 0.012);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        power.AddDemand(5, _start);

        Assert.That(EnergyGenProcessor.FuelOutTime(power, 100), Is.EqualTo(16).Within(1e-9));

        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        Assert.That(power.LocalFuel, Is.EqualTo(0));
        Assert.That(power.ReactorOutOfFuel, Is.True);
        // Full at 10 s, held to 16 s, then 84 s drawing 5 kW from the store.
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(1000 - 5 * 84).Within(1e-3));
    }

    [Test]
    public void FuelOutTime_WhenTheStoreEmptiesFirst_UsesFullOutputThroughout()
    {
        // Demand above output: the reactor runs flat out whether or not the store is empty.
        var ship = ShipWithReactor(stored: 150, storeMax: 1000, fuel: 0.05);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        power.AddDemand(30, _start);

        Assert.That(EnergyGenProcessor.FuelOutTime(power, 100), Is.EqualTo(50).Within(1e-9));
        Assert.That(EnergyGenProcessor.FuelOutTime(power, 10), Is.EqualTo(50).Within(1e-9));
    }

    [Test]
    public void SolarCovering_TheDemand_BurnsNoFuel_EvenWhileCharging()
    {
        var ship = ShipWithReactor(stored: 990, storeMax: 1000, fuel: 1);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        power.MaxOutputFromSolar = 5;
        power.AddDemand(5, _start);

        // Charging at full output (20 kW) for 10/15 s burns fuel; afterwards solar alone covers it.
        double tFill = 10.0 / 15.0;
        EnergyGenProcessor.EnergyGen(ship, _start.AddSeconds(100));

        Assert.That(power.LocalFuel, Is.EqualTo(1 - FuelAtMaxKgS * tFill).Within(1e-9));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(1000).Within(1e-9));
    }

    [Test]
    public void Install_CatchesUpBeforeChangingThePlant()
    {
        var energy = new EnergyGenAbilityDB(_start.AddSeconds(-100))
        {
            MaxOutputFromReactor = ReactorKw,
            TotalFuelUseAtMax = ("fissile-fuels", 0),
            LocalFuel = 0,
        };
        energy.EnergyStored["electricity"] = 0;
        energy.EnergyStoreMax["electricity"] = 1e6;
        var ship = Entity.Create(_faction.Id);
        _sys.AddEntity(ship, new BaseDataBlob[] { energy });
        energy.EnergyType = EnergyGenAbilityDB.FindEnergyType(ship, "electricity");

        new EnergyGenerationAtb("fissile-fuels", 0, "electricity", 10, 0).OnComponentInstallation(ship, null!);

        Assert.That(energy.EnergyStored["electricity"], Is.EqualTo(ReactorKw * 100).Within(1e-6),
            "the last 100 s ran at the old 15 kW, not the new 25 kW");
        Assert.That(energy.dateTimeLastProcess, Is.EqualTo(_start));

        new EnergyStoreAtb("electricity", 500).OnComponentInstallation(ship, null!);
        Assert.That(energy.EnergyStored["electricity"], Is.EqualTo(ReactorKw * 100).Within(1e-6));
        Assert.That(energy.EnergyStoreMax["electricity"], Is.EqualTo(1e6 + 10 + 500).Within(1e-6), "the new reactor adds its 10 kJ buffer, the battery 500");
    }

    [Test]
    public void SolarUninstall_WithPanelsLeft_RecomputesSolarOutput()
    {
        var store = Load();
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        var faction = FactionFactory.CreateBasicFaction(game, "Sun", "SUN", 0);
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var earth = Pulsar4X.Names.NameLookup.GetFirstEntityWithName(sol, "Earth");
        var ship = Entity.Create(faction.Id);
        sol.AddEntity(ship, new BaseDataBlob[]
        {
            new Pulsar4X.Movement.PositionDB(earth.GetDataBlob<Pulsar4X.Movement.PositionDB>().AbsolutePosition + new Pulsar4X.Orbital.Vector3(1e7, 0, 0)),
        });

        var big = new EnergySolarGenerationAtb(550, 300, 20, 10, 100);
        var small = new EnergySolarGenerationAtb(550, 300, 20, 10, 50);
        big.OnComponentInstallation(ship, null!);
        small.OnComponentInstallation(ship, null!);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        double both = power.MaxOutputFromSolar;
        Assert.That(both, Is.GreaterThan(0), "install sets solar output without waiting for the hourly pass");

        big.OnComponentUninstallation(ship, null!);

        Assert.That(power.SolarPanels, Has.Count.EqualTo(1));
        Assert.That(power.MaxOutputFromSolar, Is.EqualTo(both / 3).Within(both * 1e-9), "50 of the 150 m² are left");
    }

    Entity OwnedShip()
    {
        var ship = Entity.Create(_faction.Id);
        _sys.AddEntity(ship, Array.Empty<BaseDataBlob>());
        return ship;
    }

    Entity ShipWithReactor(double stored, double storeMax, double fuel, double fuelAtMax = FuelAtMaxKgS)
    {
        var energy = new EnergyGenAbilityDB(_start)
        {
            EnergyType = new TestElectricity(),
            MaxOutputFromReactor = ReactorKw,
            TotalFuelUseAtMax = ("fissile-fuels", fuelAtMax),
            LocalFuel = fuel,
        };
        energy.EnergyStored["electricity"] = stored;
        energy.EnergyStoreMax["electricity"] = storeMax;
        var ship = Entity.Create();
        _sys.AddEntity(ship, new BaseDataBlob[] { energy });
        return ship;
    }

    static EnergyGenAbilityDB RoundTrip(EnergyGenAbilityDB blob)
    {
        var settings = new JsonSerializerSettings { FloatFormatHandling = FloatFormatHandling.Symbol };
        string json = JsonConvert.SerializeObject(blob, settings);
        return JsonConvert.DeserializeObject<EnergyGenAbilityDB>(json, settings)!;
    }

    sealed class TestElectricity : ICargoable
    {
        public int ID => 0;
        public string UniqueID => "electricity";
        public string CargoTypeID => "energy";
        public string Name => "electricity";
        public long MassPerUnit => 0;
        public double VolumePerUnit => 0;
    }

    static ModDataStore Load()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        return store;
    }
}
