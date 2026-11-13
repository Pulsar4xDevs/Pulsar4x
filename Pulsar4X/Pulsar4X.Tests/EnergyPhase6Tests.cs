using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Energy economy phase 6, ships: the steam turbine's realistic numbers (30% conversion from
/// Reactor Power Conversion, same 15 kW at 2 t, size scaling), the fuel-cell generator, RTG
/// decay, ship loads (sensors on spare output, the asteroid miner's extraction energy), the
/// reactor's fuel in a warp's cost, and crew counted per component.
/// </summary>
[TestFixture]
public class EnergyPhase6Tests
{
    Game _game = null!;
    StarSystem _sys = null!;
    Entity _faction = null!;
    DateTime _start;
    int _designs;

    FactionDataStore Data => _faction.GetDataBlob<FactionInfoDB>().Data;

    [SetUp]
    public void SetUp()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        _game.Settings.EnforceSingleThread = true;
        _faction = FactionFactory.CreateBasicFaction(_game, "Phase Six", "P6", 0);
        _faction.FactionOwnerID = _faction.Id;
        foreach (var good in Data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
            Data.Unlock(good);
        _sys = new StarSystem();
        _sys.Initialize(_game, "Phase Six");
        _start = _sys.StarSysDateTime;
    }

    // ---- steam turbine ----

    [Test]
    public void SteamTurbine_Is50kWOfHeatAt30Percent_Still15kW_AndBurnsForTheHeat()
    {
        var gen = Design("steam-turbine-reactor").GetAttribute<EnergyGenerationAtb>();
        // 2 t, half core, 60% of the core is fuel: 600 kg x 1/12 kW/kg = 50 kW of heat; x 0.30 = 15 kW.
        Assert.That(gen.PowerOutputMax, Is.EqualTo(600.0 / 12 * 0.30).Within(1e-9));
        Assert.That(gen.PowerOutputMax, Is.EqualTo(15).Within(1e-9));
        // Fuel follows the heat: 50 kW / 40e9 kJ per kg (was 18.75 kW of heat at 80%).
        Assert.That(gen.FuelUsedAtMax, Is.EqualTo(50 / 40e9).Within(1e-21));
        Assert.That(gen.FuelUsedAtMax / (18.75 / 40e9), Is.EqualTo(50 / 18.75).Within(1e-9), "2.67x the old burn");
        Assert.That(gen.Lifetime, Is.EqualTo(600 / (50 / 40e9)).Within(1), "the 600 kg load lasts as long as it burns");
        Assert.That(gen.FuelType, Is.EqualTo("fissile-fuels"));
    }

    [TestCase(4000)]
    [TestCase(20000)]
    public void SteamTurbine_ScalesWithSize_ToThe0Point3(double mass)
    {
        var gen = Design("steam-turbine-reactor", ("Mass", mass)).GetAttribute<EnergyGenerationAtb>();
        double fuel = mass * 0.5 * 0.6;
        double heat = fuel * (1.0 / 12) * Math.Pow(mass / 2000, 0.3);
        Assert.That(gen.PowerOutputMax, Is.EqualTo(heat * 0.30).Within(1e-9));
        Assert.That(gen.PowerOutputMax / mass, Is.GreaterThan(15.0 / 2000), "bigger plants make more per kg");
        Assert.That(gen.FuelUsedAtMax, Is.EqualTo(heat / 40e9).Within(1e-20));
    }

    [Test]
    public void PowerConversion_RaisesTheTurbinesOutput_NotItsBurn()
    {
        var level0 = Design("steam-turbine-reactor").GetAttribute<EnergyGenerationAtb>();
        Data.Unlock(EnergyTech.PowerConversion);
        Data.IncrementTechLevel(EnergyTech.PowerConversion);
        Data.IncrementTechLevel(EnergyTech.PowerConversion);
        var level2 = Design("steam-turbine-reactor").GetAttribute<EnergyGenerationAtb>();
        // 0.30 + 2 x 0.015 = 0.33 of the same 50 kW.
        Assert.That(level2.PowerOutputMax, Is.EqualTo(50 * 0.33).Within(1e-9));
        Assert.That(level2.FuelUsedAtMax, Is.EqualTo(level0.FuelUsedAtMax).Within(1e-21), "same heat, same fuel");
        Assert.That(level0.PowerOutputMax, Is.EqualTo(15).Within(1e-9), "built reactors keep their design");
    }

    // ---- RTG ----

    [Test]
    public void Rtg_DecaysByHalfEachHalfLife_AndBurnsNothing()
    {
        const double halfLife = 1000;
        var ship = OwnedShip();
        var rtg = new EnergyGenerationAtb("fissile-fuels", 0.0, "electricity", 10, 0.0, halfLife, 0.0);
        rtg.OnComponentInstallation(ship, null!);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(power.DecaySources, Has.Count.EqualTo(1));
        Assert.That(power.MaxOutputFromReactor, Is.EqualTo(0), "not a reactor: it never runs dry");
        Assert.That(power.TotalFuelUseAtMax.type, Is.Null.Or.Empty);
        Assert.That(power.TotalOutputMax, Is.EqualTo(10).Within(1e-12));

        EnergyGenProcessor.EnergyGen(ship, _start + TimeSpan.FromSeconds(halfLife));
        Assert.That(power.TotalOutputMax, Is.EqualTo(5).Within(1e-9));
        EnergyGenProcessor.EnergyGen(ship, _start + TimeSpan.FromSeconds(2 * halfLife));
        Assert.That(power.TotalOutputMax, Is.EqualTo(2.5).Within(1e-9));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(10).Within(1e-9), "its 1 s store filled");
        Assert.That(power.LocalFuel, Is.EqualTo(0));

        var copy = JsonConvert.DeserializeObject<List<DecaySource>>(JsonConvert.SerializeObject(power.DecaySources))!;
        Assert.That(copy, Has.Count.EqualTo(1));
        Assert.That(((EnergyGenAbilityDB)power.Clone()).DecaySources, Has.Count.EqualTo(1), "the copy keeps it");
        Assert.That(copy[0].OutputAt(_start + TimeSpan.FromSeconds(3 * halfLife)), Is.EqualTo(1.25).Within(1e-9));

        rtg.OnComponentUninstallation(ship, null!);
        Assert.That(power.DecaySources, Is.Empty);
        Assert.That(power.TotalOutputMax, Is.EqualTo(0));
        Assert.That(power.EnergyStoreMax["electricity"], Is.EqualTo(0));
    }

    [Test]
    public void Rtg_TemplateIsGphsLike_5WPerKg()
    {
        var gen = Design("rtg").GetAttribute<EnergyGenerationAtb>();
        Assert.That(gen.PowerOutputMax, Is.EqualTo(1000 * 0.15 * 0.57 * 0.06).Within(1e-9));
        Assert.That(gen.PowerOutputMax / 1000 * 1000, Is.EqualTo(5.13).Within(1e-9), "5.1 W/kg");
        Assert.That(gen.HalfLife_s, Is.EqualTo(87.7 * 31557600).Within(1e-3));
        // After 20 years: 0.5^(20 / 87.7) = 85.4%.
        Assert.That(new DecaySource(gen.PowerOutputMax, gen.HalfLife_s, _start).OutputAt(_start.AddYears(20)) / gen.PowerOutputMax,
            Is.EqualTo(Math.Pow(0.5, 20 * 365.0 / (87.7 * 365.25))).Within(1e-3));
    }

    // ---- fuel cell ----

    [Test]
    public void FuelCell_Template_60WPerKg_AtTheFuelsChemicalEnergy()
    {
        var design = Design("fuel-cell");
        var gen = design.GetAttribute<EnergyGenerationAtb>();
        Assert.That(design.MassPerUnit, Is.EqualTo(250));
        Assert.That(gen.PowerOutputMax, Is.EqualTo(250 * 60 / 1000.0).Within(1e-9));
        Assert.That(gen.FuelType, Is.EqualTo("hydrolox"));
        Assert.That(gen.TankFed, Is.EqualTo(1));
        Assert.That(gen.HalfLife_s, Is.EqualTo(0));
        // 15 kW / (13,300 kJ/kg x 0.6) kg/s.
        Assert.That(gen.FuelUsedAtMax, Is.EqualTo(15 / (13300 * 0.6)).Within(1e-12));
        Assert.That(Data.Techs.ContainsKey(EnergyTech.FuelCells) || Data.LockedTechs.ContainsKey(EnergyTech.FuelCells), Is.True);
    }

    [Test]
    public void FuelCell_BurnsPropellantFromTheHold_AndStopsWhenItIsGone()
    {
        var ship = OwnedShip();
        var fuel = Data.CargoGoods.GetAny("hydrolox")!;
        var cargo = new CargoStorageDB(fuel.CargoTypeID, 1e9);
        ship.SetDataBlob(cargo);
        cargo.AddCargoByUnit(fuel, 10);
        double kgPerS = 0.01; // at 10 kW
        var cell = new EnergyGenerationAtb("hydrolox", kgPerS, "electricity", 10, 0.0, 0.0, 1.0);
        cell.OnComponentInstallation(ship, null!);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(power.MaxOutputFromCells, Is.EqualTo(10));
        Assert.That(power.MaxOutputFromReactor, Is.EqualTo(0));
        Assert.That(power.LocalFuel, Is.EqualTo(0), "no built-in fuel");
        Assert.That(power.TotalOutputMax, Is.EqualTo(10));
        power.EnergyStored["electricity"] = power.EnergyStoreMax["electricity"];

        // 5 kW for 100 s: half load, 0.5 kg.
        power.AddDemand(5, _start);
        EnergyGenProcessor.EnergyGen(ship, _start + TimeSpan.FromSeconds(100));
        double used = 0.5 * kgPerS * 100;
        Assert.That(cargo.GetUnitsStored(fuel, false) * fuel.MassPerUnit - power.CellFuelDebt_kg,
            Is.EqualTo(10 * fuel.MassPerUnit - used).Within(1e-9));

        // Run far past the end: the hold empties at the exact instant, then the store drains.
        EnergyGenProcessor.EnergyGen(ship, _start + TimeSpan.FromDays(1));
        Assert.That(cargo.GetUnitsStored(fuel, false), Is.EqualTo(0));
        Assert.That(power.CellsOutputMax, Is.EqualTo(0));
        Assert.That(power.TotalOutputMax, Is.EqualTo(0));
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(0).Within(1e-9));

        cell.OnComponentUninstallation(ship, null!);
        Assert.That(power.MaxOutputFromCells, Is.EqualTo(0));
        Assert.That(power.CellFuelUseAtMax.type, Is.Empty);
    }

    [Test]
    public void FuelCell_RunsAfterTheReactor()
    {
        var ship = OwnedShip();
        var fuel = Data.CargoGoods.GetAny("hydrolox")!;
        var cargo = new CargoStorageDB(fuel.CargoTypeID, 1e9);
        ship.SetDataBlob(cargo);
        cargo.AddCargoByUnit(fuel, 1000);
        new EnergyGenerationAtb("fissile-fuels", 0.001, "electricity", 10, 1e6).OnComponentInstallation(ship, null!);
        new EnergyGenerationAtb("hydrolox", 0.01, "electricity", 10, 0.0, 0.0, 1.0).OnComponentInstallation(ship, null!);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(power.TotalOutputMax, Is.EqualTo(20));
        var (reactor, cells) = EnergyGenProcessor.BurnRates(power, 15);
        Assert.That(reactor, Is.EqualTo(0.001).Within(1e-15), "reactor flat out");
        Assert.That(cells, Is.EqualTo(0.005).Within(1e-15), "cells carry the other 5 kW");
        (reactor, cells) = EnergyGenProcessor.BurnRates(power, 5);
        Assert.That(reactor, Is.EqualTo(0.0005).Within(1e-15));
        Assert.That(cells, Is.EqualTo(0));
    }

    // ---- ship loads ----

    [Test]
    public void Sensor_RunsOnSpareOutput_WhenTheStoreIsFull_AndIsShedInWarp()
    {
        var ship = OwnedShip();
        var reactor = new EnergyGenerationAtb("fissile-fuels", 0.001, "electricity", 15, 1e6);
        reactor.OnComponentInstallation(ship, null!);
        var sensor = new PowerConsumerAtb(0.055, 2);
        sensor.OnComponentInstallation(ship, null!);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(power.AuxDemand, Is.EqualTo(0.055).Within(1e-12));

        // Charging: the sensor waits, the plant runs flat out for the store.
        Assert.That(power.AuxServed, Is.EqualTo(0));
        power.EnergyStored["electricity"] = power.EnergyStoreMax["electricity"];
        Assert.That(power.AuxServed, Is.EqualTo(0.055).Within(1e-12));
        double fuel0 = power.LocalFuel;
        EnergyGenProcessor.EnergyGen(ship, _start + TimeSpan.FromSeconds(1000));
        Assert.That(fuel0 - power.LocalFuel, Is.EqualTo(0.001 * 0.055 / 15 * 1000).Within(1e-12), "the sensor's fuel");
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(power.EnergyStoreMax["electricity"]).Within(1e-9), "the store stays full");

        // Warp takes the whole plant: the sensor is shed, the bubble is unaffected.
        power.AddDemand(15, _start + TimeSpan.FromSeconds(1000));
        Assert.That(power.AuxServed, Is.EqualTo(0));
        Assert.That(EnergyGenProcessor.Phases(power).pB, Is.EqualTo(15).Within(1e-12));
        power.AddDemand(-15, _start + TimeSpan.FromSeconds(1000));

        sensor.OnComponentUninstallation(ship, null!);
        Assert.That(power.AuxDemand, Is.EqualTo(0));
    }

    [Test]
    public void ShipLoad_WithNoPlant_IsHarmless()
    {
        var ship = OwnedShip();
        new PowerConsumerAtb(1, 2).OnComponentInstallation(ship, null!);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        Assert.That(power.AuxDemand, Is.EqualTo(1));
        Assert.DoesNotThrow(() => EnergyGenProcessor.EnergyGen(ship, _start + TimeSpan.FromHours(1)));
        Assert.That(ShipPower.DrawEnergy(ship, 100, 3600, _start + TimeSpan.FromHours(2)), Is.EqualTo(0));
    }

    [Test]
    public void DrawEnergy_UsesSpareOutputFirst_ThenTheBattery()
    {
        var ship = OwnedShip();
        new EnergyGenerationAtb("fissile-fuels", 0.001, "electricity", 1, 1e6).OnComponentInstallation(ship, null!);
        new EnergyStoreAtb("electricity", 1000).OnComponentInstallation(ship, null!);
        var power = ship.GetDataBlob<EnergyGenAbilityDB>();
        power.EnergyStored["electricity"] = power.EnergyStoreMax["electricity"];
        double fuel0 = power.LocalFuel;
        DateTime at = _start + TimeSpan.FromSeconds(3600);
        // An hour of 1 kW spare = 3,600 kJ, then 400 kJ of the 1,001 kJ store.
        double got = ShipPower.DrawEnergy(ship, 4000, 3600, at);
        Assert.That(got, Is.EqualTo(4000).Within(1e-9));
        Assert.That(fuel0 - power.LocalFuel, Is.EqualTo(0.001 * 3600).Within(1e-9), "the plant ran flat out for the hour");
        Assert.That(power.EnergyStored["electricity"], Is.EqualTo(1001 - 400).Within(1e-9));
        Assert.That(ShipPower.DrawEnergy(ship, 1e9, 0, at), Is.EqualTo(601).Within(1e-9), "only the store is left");
    }

    // ---- warp fuel ----

    [Test]
    public void WarpCost_IncludesThePlantsFuel_InDispatchOrder()
    {
        var ship = EnergyPricing.ReferenceHauler;
        double d = 0.1 * UniversalConstants.Units.MetersPerAu;
        var cost = MoveCostCalculator.Warp(ship, d);
        Assert.That(cost.FuelUsed_kg["fissile-fuels"], Is.EqualTo(cost.EnergyUsed_kJ * 50 / 40e9 / 15).Within(1e-15));

        // 10 kW of free output (solar or isotopes) and fuel cells besides: sustain (15 kW) takes the
        // free 10 then 5 from the reactor; creation is made back by all three by output.
        var mixed = ship with
        {
            FreeOutputMax = 10, EnergyOutputMax = 35,
            CellOutputMax = 10, CellFuelType = "hydrolox", CellFuelPerKJ = 1e-4,
        };
        var c = MoveCostCalculator.Warp(mixed, d);
        double duration = c.Duration_s;
        double reactorKJ = 5 * duration + ship.BubbleCreationCost * 15 / 35;
        double cellKJ = ship.BubbleCreationCost * 10 / 35;
        Assert.That(c.FuelUsed_kg["fissile-fuels"], Is.EqualTo(reactorKJ * ship.ReactorFuelPerKJ).Within(1e-12));
        Assert.That(c.FuelUsed_kg["hydrolox"], Is.EqualTo(cellKJ * 1e-4).Within(1e-9));

        var none = ship with { ReactorFuelPerKJ = 0 };
        Assert.That(MoveCostCalculator.Warp(none, d).FuelUsed_kg, Is.Empty);
    }

    [Test]
    public void FissileFuel_EmbodiedEnergy_MatchesTheOracle()
    {
        Assert.That(EmbodiedEnergy.For(Data).PerKg("fissile-fuels"), Is.EqualTo(EnergyPriceOracle.Fissile_kWhPerKg).Within(1e-9));
    }

    // ---- crew ----

    [Test]
    public void Crew_CountsEveryComponent()
    {
        var info = _faction.GetDataBlob<FactionInfoDB>();
        var drive = Design("alcubierre-warp-drive", ("Mass", 2000));
        Assert.That(drive.CrewReq, Is.EqualTo(8), "sub-linear in mass (0.0275 x mass^0.75), not one per kg");
        var bay = Design("ship-construction", ("Size", 1));
        var ship = new ShipDesign(info, "crew-test", new List<(ComponentDesign, int)> { (drive, 1), (bay, 3) },
            (Data.Armor.Values.Concat(Data.LockedArmor.Values).First(), 0f), "crew-test-" + Guid.NewGuid().ToString("N"));
        Assert.That(ship.CrewReq, Is.EqualTo(drive.CrewReq + 3 * bay.CrewReq));
    }

    // ---- stock ships ----

    [Test]
    [NonParallelizable]
    public void StockShips_PowerBudgets_WarpStillWorks()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), "Pulsar4x.Scenario.WarpTechStart");
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345));
        var game = created!.Value.game;
        var player = created.Value.faction;
        var ships = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            .Where(s => s.FactionOwnerID == player.Id).ToList();
        Assert.That(ships, Is.Not.Empty);
        foreach (var ship in ships)
        {
            string name = ship.GetDataBlob<NameDB>().GetName(player.Id);
            var design = ship.GetDataBlob<ShipInfoDB>().Design;
            int reactors = design.Components.Where(c => c.design.TemplateID == "steam-turbine-reactor").Sum(c => c.count);
            if (!ship.TryGetDataBlob<EnergyGenAbilityDB>(out var power))
            {
                TestContext.Out.WriteLine($"{name}: no power");
                continue;
            }
            Assert.That(power.MaxOutputFromReactor, Is.EqualTo(15.0 * reactors).Within(1e-6), name);
            double sustain = ship.TryGetDataBlob<WarpAbilityDB>(out var warp) ? warp.BubbleSustainCost : 0;
            TestContext.Out.WriteLine($"{name}: reactors {power.MaxOutputFromReactor:0.###} kW, solar {power.MaxOutputFromSolar:0.###} kW,"
                + $" ship loads {power.AuxDemand:0.###} kW, warp sustain {sustain:0.###} kW, store {power.EnergyStoreMax.Values.Sum():N0} kJ,"
                + $" crew {design.CrewReq} (one per component type before: {design.Components.Sum(c => c.design.CrewReq)}), design {design.Name}");
            if (warp != null)
            {
                Assert.That(WarpMath.WarpRefusal(warp.MaxSpeed, sustain, power.TotalOutputMax), Is.Empty, name + " can warp");
                Assert.That(power.AuxDemand, Is.LessThan(power.TotalOutputMax), name);
            }
        }
        Assert.That(ships.Any(s => s.GetDataBlob<NameDB>().GetName(player.Id) == "Tanker I"), Is.True);
        // The Freighter is a design here, not a starting ship: its parts still pass the designer's warp checks.
        var freighter = player.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.Single(d => d.Name == "Freighter");
        Assert.That(WarpMath.DesignWarpWarnings(freighter.Components), Is.Empty);
        Assert.That(freighter.Components.Where(c => c.design.TemplateID == "steam-turbine-reactor").Sum(c => c.count), Is.GreaterThan(0));
        TestContext.Out.WriteLine($"Freighter design: {freighter.Components.Where(c => c.design.TryGetAttribute<EnergyGenerationAtb>(out _)).Sum(c => c.design.GetAttribute<EnergyGenerationAtb>().PowerOutputMax * c.count):0.###} kW,"
            + $" ship loads {freighter.Components.Where(c => c.design.TryGetAttribute<PowerConsumerAtb>(out _)).Sum(c => c.design.GetAttribute<PowerConsumerAtb>().Draw_kW * c.count):0.###} kW, crew {freighter.CrewReq}");
    }

    // ---- helpers ----

    Entity OwnedShip()
    {
        var ship = Entity.Create(_faction.Id);
        _sys.AddEntity(ship, Array.Empty<BaseDataBlob>());
        return ship;
    }

    ComponentDesign Design(string templateId, params (string key, double value)[] properties)
    {
        Data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-p6-" + (++_designs),
            Name = templateId,
            TemplateId = templateId,
        };
        if (properties.Length > 0)
            blueprint.Properties = properties.Select(p => new ComponentDesignBlueprint.Property { Key = p.key, Value = new JValue(p.value) }).ToList();
        ComponentDesigner.StartResearched = true;
        try { return ComponentDesignFromJson.Create(_faction, Data, blueprint); }
        finally { ComponentDesigner.StartResearched = false; }
    }

}
