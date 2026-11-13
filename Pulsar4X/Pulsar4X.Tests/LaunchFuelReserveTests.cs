using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Jon's playtest: Earth's fissile fuels were all gone by hour 4 of day 1. The launch complex
/// burned every fuel in the store as launch propellant, plant fuel first, to lift ISS Hermes.
/// A launch now burns only its pad's launch fuel (rp-1, methalox or hydrolox, picked in the
/// launch complex design), leaves a power plant fuel's runway reserve alone (methalox is one when
/// the colony runs a Methalox Turbine), and takes nothing unless it can pay the whole cost.
/// </summary>
[TestFixture]
public class LaunchFuelReserveTests
{
    const string Fissile = "fissile-fuels";

    static long Stock(Entity colony, ICargoable item)
    {
        var store = colony.GetDataBlob<CargoStorageDB>().TypeStores[item.CargoTypeID];
        return store.CurrentStoreInUnits.TryGetValue(item.ID, out var units) ? units : 0;
    }

    [Test]
    public void WarpTechStart_EarthKeepsItsFissileRunway_ThroughDay1AndDay30()
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), "Pulsar4x.Scenario.WarpTechStart");
        var catalog = GameLifecycle.BuildCatalog(store);
        var start = catalog.DefaultStart!;
        var request = NewGameRequests.FromDefaultStart(catalog, paths, start.FactionName!, start.FactionAbbreviation!, 1);
        var created = GameLifecycle.CreateGameCore(store, request);
        Assert.That(created, Is.Not.Null);
        var (game, faction, sol, earthBody) = created!.Value;
        game.Settings.EnforceSingleThread = true;

        var earth = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.FactionOwnerID == faction.Id && c.GetDataBlob<ColonyInfoDB>().PlanetEntity == earthBody);
        var fissile = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(Fissile)!;
        long reserve = ColonyPowerProcessor.FuelReserveUnits(earth, Fissile);
        Assert.That(reserve, Is.GreaterThan(0), "Earth's fission plant keeps a runway");
        Assert.That(Stock(earth, fissile), Is.GreaterThanOrEqualTo(reserve), "the scenario starts with the runway");

        // Day 1, hour by hour: the launch complex's first pass is at hour 4.
        var t0 = game.TimePulse.GameGlobalDateTime;
        for (int hour = 1; hour <= 24; hour++)
        {
            game.TimePulse.TimeStep(t0 + TimeSpan.FromHours(hour));
            Assert.That(game.TimePulse.LastFault, Is.Null, game.TimePulse.LastFault?.Report);
            Assert.That(Stock(earth, fissile), Is.GreaterThanOrEqualTo(reserve), $"hour {hour}");
        }
        Assert.That(sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Any(s => s.GetName(faction.Id) == "ISS Hermes"), Is.True,
            "the starting launch still goes ahead, on propellant");

        // Day 30: only Earth's own plants may have burned into the runway. At full nameplate they
        // burn the reserve over FuelReserveDays, so 30 days takes at most this much.
        game.TimePulse.TimeStep(t0 + TimeSpan.FromDays(30));
        Assert.That(game.TimePulse.LastFault, Is.Null, game.TimePulse.LastFault?.Report);
        long plantBurn30Days = (long)Math.Ceiling(reserve * 30 / ColonyPowerProcessor.FuelReserveDays);
        Assert.That(Stock(earth, fissile), Is.GreaterThanOrEqualTo(reserve - plantBurn30Days));
    }

    static (Game game, Entity player, StarSystem sol, Entity planet, Entity earth) EarthFromBlueprint()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        var sol = game.Systems.Single(s => s.ID == "system-sol");
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;
        player.GetDataBlob<FactionInfoDB>().Species.Add(species);
        var planet = NameLookup.GetFirstEntityWithName(sol, "Earth");
        var earth = ColonyFactory.CreateFromBlueprint(game, player, species, sol, planet, store.Colonies["colony-earth"]);
        return (game, player, sol, planet, earth);
    }

    static double HermesCost(Entity player, Entity planet, Entity earth)
    {
        var hermes = earth.GetDataBlob<LaunchComplexDB>().LaunchQueue.Single(e => e.ShipName == "ISS Hermes");
        var design = player.GetDataBlob<FactionInfoDB>().ShipDesigns[hermes.DesignId];
        return OrbitMath.FuelCostToOrbit(planet, design.MassPerUnit, OrbitMath.LowOrbitRadius(planet));
    }

    static void SetStock(Entity colony, ICargoable item, double kg)
    {
        var cargo = colony.GetDataBlob<CargoStorageDB>();
        CargoTransferProcessor.AddRemoveCargoMass(colony, item, -cargo.GetMassStored(item, false));
        if (kg > 0)
            CargoTransferProcessor.AddRemoveCargoMass(colony, item, kg);
        Assert.That(Stock(colony, item), Is.EqualTo((long)Math.Round(kg)).Within(1), $"room for {item.UniqueID}");
    }

    [Test]
    public void Launch_OnMethalox_WithAMethaloxTurbine_LeavesTheRunway_AndTakesNothingWhenShort()
    {
        var (_, player, sol, planet, earth) = EarthFromBlueprint();
        var info = player.GetDataBlob<FactionInfoDB>();
        var methalox = info.Data.CargoGoods.GetAny("methalox")!;
        Assert.That(ColonyPowerProcessor.FuelReserveUnits(earth, "methalox"), Is.EqualTo(0), "no methalox plant yet");

        // A 1 MW Methalox Turbine (the oxidiser-turbine template) makes methalox a plant fuel at Earth.
        info.Data.Unlock("oxidiser-turbine");
        var turbine = ComponentDesignFromJson.Create(player, info.Data, new ComponentDesignBlueprint
        {
            UniqueID = "test-methalox-turbine-1mw",
            Name = "Methalox Turbine",
            TemplateId = "oxidiser-turbine",
            Properties = new() { new ComponentDesignBlueprint.Property { Key = "Size", Value = new Newtonsoft.Json.Linq.JValue(1.0) } },
        });
        earth.AddComponent(turbine);
        ColonyPowerProcessor.Recalc(earth);
        long reserve = ColonyPowerProcessor.FuelReserveUnits(earth, "methalox");
        Assert.That(reserve, Is.GreaterThan(0), "the turbine keeps a methalox runway");

        // A methalox launch complex: Earth's pad burns methalox.
        var pad = earth.GetDataBlob<LaunchComplexDB>().Pads.Values.Single();
        pad.LaunchFuelId = "methalox";
        Assert.That(pad.LaunchFuel, Is.EqualTo("methalox"));
        double cost = HermesCost(player, planet, earth);
        Assert.That(cost, Is.GreaterThan(2000));

        // The runway plus 1000 kg: plenty of methalox in the store, but only 1000 kg a launch may take.
        SetStock(earth, methalox, reserve + 1000);
        var rp1 = info.Data.CargoGoods.GetAny("rp-1")!;
        long rp1Before = Stock(earth, rp1);
        int ships = sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Count;
        var processor = new LaunchComplexProcessor();
        processor.ProcessEntity(earth, 0);
        Assert.That(sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Count, Is.EqualTo(ships), "not enough above the runway: no launch");
        Assert.That(Stock(earth, methalox), Is.EqualTo(reserve + 1000), "a launch that cannot go takes nothing");
        Assert.That(Stock(earth, rp1), Is.EqualTo(rp1Before), "RP-1 is not this pad's fuel");
        Assert.That(LaunchComplexProcessor.PadHoldReason(earth, pad), Is.EqualTo(LaunchComplexProcessor.NotEnoughOf("Methalox")));

        // Enough above the runway, 500 kg to spare: the launch goes and takes only methalox above it.
        SetStock(earth, methalox, reserve + Math.Ceiling(cost) + 500);
        processor.ProcessEntity(earth, 0);
        Assert.That(sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Count, Is.EqualTo(ships + 1), "the launch goes");
        Assert.That(Stock(earth, methalox), Is.GreaterThanOrEqualTo(reserve));
        Assert.That(Stock(earth, methalox), Is.EqualTo(reserve + 500).Within(2));
        Assert.That(Stock(earth, rp1), Is.EqualTo(rp1Before));
    }

    [Test]
    public void DefaultPad_WithoutRp1_DoesNotLaunch_OnSurplusFissile()
    {
        var (game, player, sol, planet, earth) = EarthFromBlueprint();
        var info = player.GetDataBlob<FactionInfoDB>();
        var fissile = info.Data.CargoGoods.GetAny(Fissile)!;
        var rp1 = info.Data.CargoGoods.GetAny("rp-1")!;
        var pad = earth.GetDataBlob<LaunchComplexDB>().Pads.Values.Single();
        Assert.That(pad.LaunchFuel, Is.EqualTo("rp-1"), "the default launch complex");

        // No RP-1. Fissile: the runway plus more than the whole launch cost, so a launch that fell
        // back to any fuel in the store could go.
        long reserve = ColonyPowerProcessor.FuelReserveUnits(earth, Fissile);
        double cost = HermesCost(player, planet, earth);
        SetStock(earth, rp1, 0);
        SetStock(earth, fissile, reserve + Math.Ceiling(cost) + 1000);
        long fissileBefore = Stock(earth, fissile);
        int ships = sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Count;

        // Past the launch complex's first pass at hour 4 (and its next, a day later).
        sol.UpdateActivityState();
        var t0 = game.TimePulse.GameGlobalDateTime;
        game.TimePulse.TimeStep(t0 + TimeSpan.FromHours(30));
        Assert.That(game.TimePulse.LastFault, Is.Null, game.TimePulse.LastFault?.Report);
        Assert.That(earth.StarSysDateTime, Is.GreaterThan(t0 + TimeSpan.FromHours(4)));

        Assert.That(sol.GetAllEntitiesWithDataBlob<ShipInfoDB>().Count, Is.EqualTo(ships), "no launch without RP-1");
        Assert.That(earth.GetDataBlob<LaunchComplexDB>().Pads.Values.Single().ShipName, Is.EqualTo("ISS Hermes"), "Hermes waits on the pad");
        Assert.That(Stock(earth, fissile), Is.EqualTo(fissileBefore), "fissile unchanged");
        Assert.That(LaunchComplexProcessor.PadHoldReason(earth, pad), Is.EqualTo(LaunchComplexProcessor.NotEnoughOf("RP-1")));
    }
}
