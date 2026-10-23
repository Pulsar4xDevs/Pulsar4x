using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class YardHoldsHullTests
{
    const string LineId = "yard-line";

    static readonly Game Game = InitializeGame();

    static Game InitializeGame()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 4, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        return game;
    }

    [SetUp]
    public void QuietSystems()
    {
        foreach (var sys in Game.Systems)
            sys.SetActivityState(SystemActivityState.Stasis);
    }

    [Test]
    public void ShipAssemblyCompletion_LeavesNoShip_AndOneHullOnTheYard()
    {
        var world = NewSystem();
        var design = Hull(world, "Light Hull", 1000);
        var colony = Colony(world, design.MassPerUnit, padTonnage: null);
        int ships = ShipCount(world);

        Complete(world, colony, design);

        Assert.That(ShipCount(world), Is.EqualTo(ships));
        var hull = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Hulls.Single();
        Assert.That(hull.DesignId, Is.EqualTo(design.UniqueID));
        Assert.That(hull.Name, Is.Not.Empty);
        Assert.That(hull.Mass, Is.EqualTo(design.MassPerUnit));

        var line = YardLine(world, colony);
        Assert.That(line.IsShipyard, Is.True);
        Assert.That(line.SlipSize, Is.EqualTo(design.MassPerUnit));
        Assert.That(line.Hulls.Single().DesignName, Is.EqualTo(design.Name));
        Assert.That(line.Hulls.Single().Reason, Is.EqualTo(LaunchComplexProcessor.NoFreePad));
        Assert.That(YardView(world, colony).Pads, Is.Empty);
        var listed = line.Constructibles.Single(item => item.DesignId == design.UniqueID);
        Assert.That(listed.Mass, Is.EqualTo(design.MassPerUnit));
        Assert.That(listed.OverSlip, Is.False);
    }

    [Test]
    public void Pad_WithFuelAndTonnage_CreatesTheShip_AndClearsTheHull()
    {
        var world = NewSystem();
        var light = Hull(world, "Light Hull", 1000);
        var heavy = Hull(world, "Heavy Hull", 8000);
        Assert.That(heavy.MassPerUnit, Is.GreaterThan(light.MassPerUnit));
        var colony = Colony(world, heavy.MassPerUnit, light.MassPerUnit);
        Complete(world, colony, light);
        Complete(world, colony, heavy);
        string lightName = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Hulls
            .Single(hull => hull.DesignId == light.UniqueID).Name;

        new LaunchComplexProcessor().ProcessEntity(colony, 0);

        var waiting = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Hulls;
        Assert.That(waiting.Select(hull => hull.DesignId), Is.EquivalentTo(new[] { light.UniqueID, heavy.UniqueID }));
        Assert.That(ShipCount(world), Is.EqualTo(0));
        var beforeFuel = YardLine(world, colony).Hulls;
        Assert.That(beforeFuel.Single(hull => hull.DesignId == light.UniqueID).Reason, Is.EqualTo(LaunchComplexProcessor.NotEnoughFuel));
        Assert.That(beforeFuel.Single(hull => hull.DesignId == heavy.UniqueID).Reason, Is.EqualTo(LaunchComplexProcessor.OverTonnage));
        var pads = YardView(world, colony).Pads;
        Assert.That(pads.Single().MaxTonnage, Is.EqualTo(light.MassPerUnit));
        Assert.That(pads.Single().Free, Is.True);

        StockFuel(world, colony);
        new LaunchComplexProcessor().ProcessEntity(colony, 0);

        var left = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Hulls.Single();
        Assert.That(left.DesignId, Is.EqualTo(heavy.UniqueID));
        Assert.That(YardLine(world, colony).Hulls.Single().Reason, Is.EqualTo(LaunchComplexProcessor.OverTonnage));
        var ship = world.System.GetAllEntitiesWithDataBlob<ShipInfoDB>().Single();
        Assert.That(ship.GetDataBlob<NameDB>().GetName(world.Faction.Id), Is.EqualTo(lightName));
        Assert.That(ship.GetDataBlob<ShipInfoDB>().Design, Is.SameAs(light));
    }

    [Test]
    public void NoPad_HullRemainsAfterTheLaunchTick()
    {
        var world = NewSystem();
        var design = Hull(world, "Light Hull", 1000);
        var colony = Colony(world, design.MassPerUnit, padTonnage: null);
        Complete(world, colony, design);
        int ships = ShipCount(world);

        new LaunchComplexProcessor().ProcessEntity(colony, 0);
        new LaunchComplexProcessor().ProcessManager(world.System, 0);

        Assert.That(ShipCount(world), Is.EqualTo(ships));
        Assert.That(colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Hulls, Has.Count.EqualTo(1));
        Assert.That(YardLine(world, colony).Hulls.Single().Reason, Is.EqualTo(LaunchComplexProcessor.NoFreePad));
    }

    [Test]
    public void OverSlip_IsRefused()
    {
        var world = NewSystem();
        var design = Hull(world, "Light Hull", 1000);
        var colony = Colony(world, design.MassPerUnit, padTonnage: null);
        var translator = new CommandTranslator(Game);

        var fits = translator.Translate(world.Faction, colony, new QueueIndustryJobCommand(
            colony.Id, LineId, design.UniqueID, 1, false, false));
        Assert.That(fits.Accepted, Is.True, fits.RejectionReason);
        Assert.That(colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs, Has.Count.EqualTo(1));

        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].MaxVolume = design.MassPerUnit - 1;
        var refused = translator.Translate(world.Faction, colony, new QueueIndustryJobCommand(
            colony.Id, LineId, design.UniqueID, 1, false, false));
        Assert.That(refused.Accepted, Is.False);
        Assert.That(refused.RejectionReason, Does.Contain("slip"));
        Assert.That(colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs, Has.Count.EqualTo(1));

        var widget = new ComponentDesign
        {
            UniqueID = "yard-widget-" + Guid.NewGuid().ToString("N"),
            Name = "Widget",
            MassPerUnit = design.MassPerUnit + 1000,
            IndustryTypeID = "component-construction",
            IndustryPointCosts = 1,
            CargoTypeID = "general-storage",
        };
        world.Info.IndustryDesigns[widget.UniqueID] = widget;
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].IndustryTypeRates["component-construction"] = 10;
        var componentJob = translator.Translate(world.Faction, colony, new QueueIndustryJobCommand(
            colony.Id, LineId, widget.UniqueID, 1, false, false));
        Assert.That(componentJob.Accepted, Is.True, componentJob.RejectionReason);

        var line = YardLine(world, colony);
        Assert.That(line.Constructibles.Single(item => item.DesignId == design.UniqueID).OverSlip, Is.True);
        Assert.That(line.Constructibles.Single(item => item.DesignId == design.UniqueID).SlipReason, Does.Contain("slip"));
        Assert.That(line.Constructibles.Single(item => item.DesignId == widget.UniqueID).OverSlip, Is.False);
    }

    static void Complete(World world, Entity colony, ShipDesign design)
    {
        var job = new IndustryJob(world.Info, design.UniqueID);
        job.InitialiseJob(1, false);
        design.OnConstructionComplete(colony, colony.GetDataBlob<CargoStorageDB>(), LineId, job, design);
    }

    static void StockFuel(World world, Entity colony)
    {
        world.Info.Data.Unlock("methalox");
        var fuel = world.Info.Data.CargoGoods.GetAny("methalox");
        Assert.That(fuel, Is.Not.Null);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(fuel!, 2_000_000), Is.EqualTo(2_000_000));
    }

    static IndustryView YardView(World world, Entity colony)
        => new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<IndustryView>()!;

    static ProductionLineView YardLine(World world, Entity colony)
        => YardView(world, colony).ProductionLines.Single(line => line.Id == LineId);

    static int ShipCount(World world)
        => world.System.GetAllEntitiesWithDataBlob<ShipInfoDB>().Count;

    static ShipDesign Hull(World world, string name, long partMass)
    {
        world.Info.Data.Unlock("plastic");
        world.Info.Data.Unlock("plastic-armor");
        var part = new ComponentDesign
        {
            UniqueID = "yard-part-" + Guid.NewGuid().ToString("N"),
            Name = "Part",
            MassPerUnit = partMass,
            VolumePerUnit = 1,
            CargoTypeID = "general-storage",
        };
        var design = new ShipDesign(
            world.Info,
            name,
            new List<(ComponentDesign, int)> { (part, 1) },
            (world.Info.Data.Armor["plastic-armor"], 0f));
        design.Initialise(world.Info);
        // The part id is not a cargo good. Leave the recipe empty so queueing does not try to build it.
        design.ResourceCosts.Clear();
        return design;
    }

    static Entity Colony(World world, double slip, long? padTonnage)
    {
        var planet = Entity.Create();
        world.System.AddEntity(planet, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            new PositionDB(),
            new OrbitDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6_000_000),
        });

        var line = new IndustryAbilityDB.ProductionLine
        {
            Name = "Ship Yard",
            MaxVolume = slip,
            IndustryTypeRates = new Dictionary<string, int> { ["ship-assembly"] = 100 },
        };
        var colony = Entity.Create(world.Faction.Id);
        world.System.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB("Yard"),
            new ColonyInfoDB { PlanetEntity = planet },
            new CargoStorageDB("fuel-storage", 1e8),
            new IndustryAbilityDB(LineId, line),
        });
        if (padTonnage is long tonnage)
            colony.SetDataBlob(new LaunchComplexDB("pad", new LaunchPad { MaxTonnage = tonnage }));
        return colony;
    }

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "Yard-" + Guid.NewGuid().ToString("N"));
        var system = new StarSystem();
        system.Initialize(Game, "Yard-" + Guid.NewGuid().ToString("N"), -1);
        system.SetActivityState(SystemActivityState.Stasis);
        return new World { System = system, Faction = faction, Info = faction.GetDataBlob<FactionInfoDB>() };
    }

    sealed class World
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public FactionInfoDB Info = null!;
    }
}
