using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class ColonyConstructionTests
{
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
    public void MountingAStoredFactory_RemovesThatInstance_AndCostsByMass()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 50);
        var design = FactoryDesign();
        design.IndustryPointCosts = 100_000;
        world.Info.InternalComponentDesigns[design.UniqueID] = design;
        var crate = new ComponentInstance(design);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));

        var queued = new CommandTranslator(Game).Translate(
            world.Faction, colony, new InstallComponentCommand(colony.Id, crate.ID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Peek();
        Assert.That(job.Kind, Is.EqualTo("Mount"));
        Assert.That(job.Instance, Is.SameAs(crate));
        Assert.That(job.PointCost, Is.EqualTo(50));
        Assert.That(job.PointCost, Is.Not.EqualTo(design.IndustryPointCosts));
        Assert.That(job.ResourcesRemaining, Is.Empty);
        Assert.That(MountedCount(colony), Is.EqualTo(0));
        Assert.That(StoredInstances(colony).Single(), Is.SameAs(crate));

        design.IndustryPointCosts = 1_000_000;
        design.MassPerUnit = 1;
        Assert.That(job.PointCost, Is.EqualTo(50));

        var again = new CommandTranslator(Game).Translate(
            world.Faction, colony, new InstallComponentCommand(colony.Id, crate.ID));
        Assert.That(again.Accepted, Is.False);
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Count, Is.EqualTo(1));

        var view = new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<ConstructionView>();
        Assert.That(view!.BuildQueue.Single().Kind, Is.EqualTo("Mount"));
        Assert.That(view.BuildQueue.Single().PointCost, Is.EqualTo(50));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        Assert.That(StoredInstances(colony), Is.Empty);
        var mounted = colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single();
        Assert.That(mounted, Is.SameAs(crate));
    }

    [Test]
    public void Mount_DoesNotFinishOnTheFirstDay_WhenTheCostExceedsTheDaysPoints()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 5);
        var design = FactoryDesign();
        world.Info.InternalComponentDesigns[design.UniqueID] = design;
        var crate = new ComponentInstance(design);
        colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1);

        var queued = new CommandTranslator(Game).Translate(
            world.Faction, colony, new AddToConstructionQueueCommand(colony.Id, design.UniqueID, crate.ID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        var job = colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Single();
        Assert.That(job.Instance, Is.SameAs(crate));
        Assert.That(job.PointsAccumulated, Is.EqualTo(5));
        Assert.That(job.IsComplete, Is.False);
        Assert.That(MountedCount(colony), Is.EqualTo(0));
        Assert.That(StoredInstances(colony).Single(), Is.SameAs(crate));
    }

    [Test]
    public void AssemblingAMine_SpendsTheRecipe_AndCostsIndustryPoints()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 20);
        var mine = MineDesign();
        world.Info.InternalComponentDesigns[mine.UniqueID] = mine;
        var (iron, copper) = Stock(world, colony, ironUnits: 4, copperUnits: 2);

        var queued = new CommandTranslator(Game).Translate(
            world.Faction, colony, new AddToConstructionQueueCommand(colony.Id, mine.UniqueID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Peek();
        Assert.That(job.Kind, Is.EqualTo("Assemble"));
        Assert.That(job.Instance, Is.Null);
        Assert.That(job.PointCost, Is.EqualTo(mine.IndustryPointCosts));
        Assert.That(job.PointCost, Is.EqualTo(20));
        Assert.That(job.ResourcesRemaining["iron"], Is.EqualTo(4));
        Assert.That(job.ResourcesRemaining["copper"], Is.EqualTo(2));

        mine.IndustryPointCosts = 999;
        mine.ResourceCosts["iron"] = 40;
        Assert.That(job.PointCost, Is.EqualTo(20));
        Assert.That(job.ResourcesRemaining["iron"], Is.EqualTo(4));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        var storage = colony.GetDataBlob<CargoStorageDB>();
        Assert.That(storage.GetUnitsStored(iron, false), Is.EqualTo(0));
        Assert.That(storage.GetUnitsStored(copper, false), Is.EqualTo(0));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        var mounted = colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single();
        Assert.That(mounted.Design, Is.SameAs(mine));
    }

    [Test]
    public void Assemble_ShortOneInput_LeavesStockUnchanged()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 20);
        var mine = MineDesign();
        world.Info.InternalComponentDesigns[mine.UniqueID] = mine;
        var (iron, copper) = Stock(world, colony, ironUnits: 3, copperUnits: 2);

        var queued = new CommandTranslator(Game).Translate(
            world.Faction, colony, new AddToConstructionQueueCommand(colony.Id, mine.UniqueID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        var storage = colony.GetDataBlob<CargoStorageDB>();
        Assert.That(storage.GetUnitsStored(iron, false), Is.EqualTo(3));
        Assert.That(storage.GetUnitsStored(copper, false), Is.EqualTo(2));
        Assert.That(MountedCount(colony), Is.EqualTo(0));
        var job = colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Single();
        Assert.That(job.PointsAccumulated, Is.EqualTo(0));
        Assert.That(job.ResourcesRemaining["iron"], Is.EqualTo(4));
    }

    [Test]
    public void OneUnitInput_IsConsumedOnTheFinishingDay()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 1);
        var mine = MineDesign();
        mine.IndustryPointCosts = 3;
        mine.ResourceCosts = new Dictionary<string, long> { ["iron"] = 1 };
        world.Info.InternalComponentDesigns[mine.UniqueID] = mine;
        var (iron, _) = Stock(world, colony, ironUnits: 1, copperUnits: 0);

        Assert.That(new CommandTranslator(Game).Translate(
            world.Faction, colony, new AddToConstructionQueueCommand(colony.Id, mine.UniqueID)).Accepted, Is.True);

        var processor = new LocalConstructionProcessor();
        var storage = colony.GetDataBlob<CargoStorageDB>();
        processor.ProcessEntity(colony, 0);
        processor.ProcessEntity(colony, 0);
        Assert.That(storage.GetUnitsStored(iron, false), Is.EqualTo(1));
        Assert.That(MountedCount(colony), Is.EqualTo(0));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Peek().PointsAccumulated, Is.EqualTo(2));

        processor.ProcessEntity(colony, 0);
        Assert.That(storage.GetUnitsStored(iron, false), Is.EqualTo(0));
        Assert.That(MountedCount(colony), Is.EqualTo(1));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
    }

    [Test]
    public void ComponentInstance_HasNoPointCostField()
    {
        var names = typeof(ComponentInstance)
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Select(member => member.Name);
        Assert.That(names, Does.Not.Contain("PointCost"));
        Assert.That(names, Does.Not.Contain("IndustryPointCosts"));
        Assert.That(LocalConstructionJob.MountCost(5_000_000), Is.EqualTo(50));
        Assert.That(LocalConstructionJob.MountCost(1_000), Is.EqualTo(1));
    }

    [Test]
    public void ADesignIdAlone_DoesNotMount()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 100_000);
        var design = FactoryDesign();
        design.IndustryPointCosts = 10;
        var crate = new ComponentInstance(design);
        colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1);

        var construction = colony.GetDataBlob<LocalConstructionDB>();
        construction.BuildQueue.Enqueue(new LocalConstructionJob(design));
        construction.BuildQueue.Enqueue(new LocalConstructionJob(design, crate));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(MountedCount(colony), Is.EqualTo(0));
        Assert.That(StoredInstances(colony).Single(), Is.SameAs(crate));
        Assert.That(construction.BuildQueue.Count, Is.EqualTo(2));
        Assert.That(construction.BuildQueue.Peek().Instance, Is.Null);
        Assert.That(construction.BuildQueue.Peek().PointsAccumulated, Is.EqualTo(0));
        Assert.That(construction.BuildQueue.ElementAt(1).PointsAccumulated, Is.EqualTo(0));

        world.Info.InternalComponentDesigns[design.UniqueID] = design;
        var rejected = new CommandTranslator(Game).Translate(
            world.Faction, colony, new AddToConstructionQueueCommand(colony.Id, design.UniqueID));
        Assert.That(rejected.Accepted, Is.False);
        Assert.That(rejected.RejectionReason, Does.Contain("cargo"));

        var free = new ComponentDesign
        {
            UniqueID = "free-site-" + Guid.NewGuid().ToString("N"),
            Name = "Free Site",
            IndustryPointCosts = 0,
            ComponentMountType = ComponentMountType.PlanetInstallation,
        };
        var bare = Colony(world, pointsPerDay: 100);
        bare.GetDataBlob<LocalConstructionDB>().BuildQueue.Enqueue(new LocalConstructionJob(free));
        new LocalConstructionProcessor().ProcessEntity(bare, 0);
        Assert.That(MountedCount(bare), Is.EqualTo(0));
        Assert.That(bare.GetDataBlob<LocalConstructionDB>().BuildQueue.Count, Is.EqualTo(1));
    }

    [Test]
    public void ConstructionView_ListsCratesAndSiteDesigns()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 5);
        world.Info.Data.Unlock("iron");

        var stored = FactoryDesign();
        stored.Name = "Stored Factory";
        stored.IndustryPointCosts = 100_000;
        var absent = FactoryDesign();
        absent.Name = "Absent Factory";
        var part = new ComponentDesign
        {
            UniqueID = "ship-part-" + Guid.NewGuid().ToString("N"),
            Name = "Ship Part",
            ComponentMountType = ComponentMountType.ShipComponent,
            IndustryPointCosts = 10,
        };
        var mine = MineDesign();
        world.Info.InternalComponentDesigns[stored.UniqueID] = stored;
        world.Info.InternalComponentDesigns[absent.UniqueID] = absent;
        world.Info.InternalComponentDesigns[part.UniqueID] = part;
        world.Info.InternalComponentDesigns[mine.UniqueID] = mine;

        var crate = new ComponentInstance(stored);
        colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1);

        var view = new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<ConstructionView>();
        Assert.That(view!.Crates.Select(item => item.ComponentId), Is.EqualTo(new[] { crate.ID }));
        Assert.That(view.Crates.Single().MountCost, Is.EqualTo(50));
        Assert.That(view.Crates.Select(item => item.DesignId), Does.Not.Contain(absent.UniqueID));
        Assert.That(view.SiteDesigns.Select(item => item.DesignId), Is.EqualTo(new[] { mine.UniqueID }));
        Assert.That(view.SiteDesigns.Single().IndustryPointCosts, Is.EqualTo(20));
        Assert.That(view.SiteDesigns.Single().MissingResources, Does.Contain("Iron"));
        Assert.That(view.SiteDesigns.Select(item => item.DesignId), Does.Not.Contain(part.UniqueID));
        Assert.That(view.SiteDesigns.Select(item => item.DesignId), Does.Not.Contain(stored.UniqueID));

        var cargo = new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<CargoStorageView>();
        var item = cargo!.Stores.SelectMany(store => store.Items).Single(row => row.Id == crate.ID);
        Assert.That(item.CanInstall, Is.True);
        Assert.That(item.MountCost, Is.EqualTo(50));

        var ship = Entity.Create(world.Faction.Id);
        var shipCrate = new ComponentInstance(stored);
        world.System.AddEntity(ship, new List<BaseDataBlob>
        {
            new CargoStorageDB("general-storage", 100000),
            new ComponentInstancesDB(),
            new LocalConstructionDB { PointsPerDay = 50 },
        });
        ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(shipCrate, 1);

        var shipCargo = new GameProjector(Game).ProjectEntity(ship, ship.FactionOwnerID).GetView<CargoStorageView>();
        Assert.That(shipCargo!.Stores.SelectMany(store => store.Items).Single().CanInstall, Is.False);
        var rejected = new CommandTranslator(Game).Translate(
            world.Faction, ship, new InstallComponentCommand(ship.Id, shipCrate.ID));
        Assert.That(rejected.Accepted, Is.False);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(shipCrate, false), Is.EqualTo(1));
        Assert.That(ship.GetDataBlob<ComponentInstancesDB>().AllComponents, Is.Empty);
    }

    static (ICargoable Iron, ICargoable Copper) Stock(World world, Entity colony, long ironUnits, long copperUnits)
    {
        world.Info.Data.Unlock("iron");
        world.Info.Data.Unlock("copper");
        var iron = world.Info.Data.CargoGoods.GetAny("iron")!;
        var copper = world.Info.Data.CargoGoods.GetAny("copper")!;
        var storage = colony.GetDataBlob<CargoStorageDB>();
        if (ironUnits > 0)
            Assert.That(storage.AddCargoByUnit(iron, ironUnits), Is.EqualTo(ironUnits));
        if (copperUnits > 0)
            Assert.That(storage.AddCargoByUnit(copper, copperUnits), Is.EqualTo(copperUnits));
        return (iron, copper);
    }

    static List<ComponentInstance> StoredInstances(Entity colony)
    {
        var store = colony.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"];
        return store.GetCargoables().Values.OfType<ComponentInstance>().ToList();
    }

    static int MountedCount(Entity colony)
        => colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Count;

    static ComponentDesign FactoryDesign()
    {
        return new ComponentDesign
        {
            UniqueID = "test-factory-" + Guid.NewGuid().ToString("N"),
            Name = "Test Factory",
            CargoTypeID = "general-storage",
            MassPerUnit = 5_000_000,
            VolumePerUnit = 1,
            IndustryPointCosts = 100_000,
            ComponentMountType = ComponentMountType.ShipCargo | ComponentMountType.PlanetInstallation,
        };
    }

    static ComponentDesign MineDesign()
    {
        return new ComponentDesign
        {
            UniqueID = "test-mine-" + Guid.NewGuid().ToString("N"),
            Name = "Mine",
            IndustryPointCosts = 20,
            MassPerUnit = 5_000_000,
            ComponentMountType = ComponentMountType.PlanetInstallation,
            ResourceCosts = new Dictionary<string, long> { ["iron"] = 4, ["copper"] = 2 },
        };
    }

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "ColonyBuild-" + Guid.NewGuid().ToString("N"));
        var system = new StarSystem();
        system.Initialize(Game, "ColonyBuild-" + Guid.NewGuid().ToString("N"), -1);
        system.SetActivityState(SystemActivityState.Stasis);
        return new World { System = system, Faction = faction, Info = faction.GetDataBlob<FactionInfoDB>() };
    }

    static Entity Colony(World world, long pointsPerDay)
    {
        var planet = Entity.Create();
        world.System.AddEntity(planet, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });

        var colony = Entity.Create(world.Faction.Id);
        world.System.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB("Colony"),
            new ColonyInfoDB { PlanetEntity = planet },
            new CargoStorageDB("general-storage", 100000),
            new ComponentInstancesDB(),
            new LocalConstructionDB { PointsPerDay = pointsPerDay },
        });
        return colony;
    }

    sealed class World
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public FactionInfoDB Info = null!;
    }
}
