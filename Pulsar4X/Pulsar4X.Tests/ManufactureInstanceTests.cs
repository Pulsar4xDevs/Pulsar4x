using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class ManufactureInstanceTests
{
    const string LineId = "factory-line";

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
    public void FactoryCompletion_StoresAnInstanceAndDoesNotMount()
    {
        var world = NewSystem();
        var colony = Colony(world, withOffice: true);
        var design = FactoryDesign();
        var job = new IndustryJob(design);
        job.InitialiseJob(2, false);
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs.Add(job);

        Complete(colony, design, job);
        Complete(colony, design, job);

        var stored = StoredInstances(colony);
        Assert.That(stored, Has.Count.EqualTo(2));
        Assert.That(stored.Select(instance => instance.ID).Distinct().Count(), Is.EqualTo(2));
        Assert.That(stored.All(instance => instance.Design == design), Is.True);
        var store = colony.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"];
        Assert.That(store.CurrentStoreInUnits.ContainsKey(design.ID), Is.False);
        Assert.That(stored.All(instance => store.CurrentStoreInUnits[instance.ID] == 1), Is.True);
        Assert.That(MountedCount(colony), Is.EqualTo(0));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
    }

    [Test]
    public void CheckedBox_OnAColonyWithAnOffice_EnqueuesAMountAndDoesNotInstall()
    {
        var world = NewSystem();
        var colony = Colony(world, withOffice: true);
        var design = FactoryDesign();
        world.Info.IndustryDesigns[design.UniqueID] = design;

        var queued = new CommandTranslator(Game).Translate(
            world.Faction,
            colony,
            new QueueIndustryJobCommand(colony.Id, LineId, design.UniqueID, 1, false, true));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs.Single();
        Assert.That(job.InstallOn, Is.SameAs(colony));
        Complete(colony, design, job);

        var stored = StoredInstances(colony).Single();
        var construction = colony.GetDataBlob<LocalConstructionDB>();
        var queuedMount = construction.BuildQueue.Peek();
        Assert.That(construction.BuildQueue.Count, Is.EqualTo(1));
        Assert.That(queuedMount.Instance, Is.SameAs(stored));
        Assert.That(queuedMount.PointCost, Is.EqualTo(LocalConstructionJob.MountCost(design.MassPerUnit)));
        Assert.That(queuedMount.PointCost, Is.Not.EqualTo(design.IndustryPointCosts));
        Assert.That(MountedCount(colony), Is.EqualTo(0));
    }

    [Test]
    public void CheckedBox_OnASiteOnlyDesign_QueuesTheMountCost()
    {
        var world = NewSystem();
        var colony = Colony(world, withOffice: true);
        var design = SiteOnlyDesign();
        var job = new IndustryJob(design);
        job.InitialiseJob(1, false);
        job.InstallOn = colony;

        Complete(colony, design, job);

        var construction = colony.GetDataBlob<LocalConstructionDB>();
        var queued = construction.BuildQueue.Peek();
        Assert.That(construction.BuildQueue.Count, Is.EqualTo(1));
        Assert.That(queued.Instance, Is.Null);
        Assert.That(queued.Kind, Is.EqualTo("Assemble"));
        Assert.That(queued.PointCost, Is.EqualTo(LocalConstructionJob.MountCost(design.MassPerUnit)));
        Assert.That(queued.PointCost, Is.EqualTo(1));
        Assert.That(queued.PointCost, Is.Not.EqualTo(design.IndustryPointCosts));
        Assert.That(queued.ResourcesRemaining, Is.Empty);
        Assert.That(StoredInstances(colony), Is.Empty);
        Assert.That(MountedCount(colony), Is.EqualTo(0));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(construction.BuildQueue, Is.Empty);
        Assert.That(MountedCount(colony), Is.EqualTo(1));
    }

    [Test]
    public void StuckSiteOnlyMount_InstallsAtTheMountCost()
    {
        var world = NewSystem();
        var colony = Colony(world, withOffice: true);
        var design = SiteOnlyDesign();
        var instance = new ComponentInstance(design);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(instance, 1), Is.EqualTo(1));

        var construction = colony.GetDataBlob<LocalConstructionDB>();
        construction.BuildQueue.Enqueue(new LocalConstructionJob(design, instance));
        Assert.That(construction.BuildQueue.Peek().PointCost, Is.EqualTo(1));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(construction.BuildQueue, Is.Empty);
        Assert.That(StoredInstances(colony), Is.Empty);
        Assert.That(MountedCount(colony), Is.EqualTo(1));
    }

    [Test]
    public void OldFactoryFollowUp_DropsToTheMountCost()
    {
        var world = NewSystem();
        var colony = Colony(world, withOffice: true);
        var design = SiteOnlyDesign();
        var construction = colony.GetDataBlob<LocalConstructionDB>();
        var queued = new LocalConstructionJob(design);
        queued.ResourcesRemaining.Clear();
        queued.PointsAccumulated = 25;
        construction.BuildQueue.Enqueue(queued);
        Assert.That(queued.PointCost, Is.EqualTo(45_000));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(construction.BuildQueue, Is.Empty);
        Assert.That(MountedCount(colony), Is.EqualTo(1));
    }

    [Test]
    public void ColonyWithNoOffice_LeavesTheInstanceInCargo()
    {
        var world = NewSystem();
        var colony = Colony(world, withOffice: false);
        var design = FactoryDesign();
        world.Info.IndustryDesigns[design.UniqueID] = design;

        var queued = new CommandTranslator(Game).Translate(
            world.Faction,
            colony,
            new QueueIndustryJobCommand(colony.Id, LineId, design.UniqueID, 1, false, true));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs.Single();
        Assert.That(job.InstallOn, Is.Null);
        job.InstallOn = colony;
        Complete(colony, design, job);

        Assert.That(StoredInstances(colony), Has.Count.EqualTo(1));
        Assert.That(colony.HasDataBlob<LocalConstructionDB>(), Is.False);
        Assert.That(MountedCount(colony), Is.EqualTo(0));
    }

    [Test]
    public void ShipConstructionCompletion_StoresTheInstanceAndDoesNotMount()
    {
        var world = NewSystem();
        Unlock(world.Info, "ship-construction");
        Unlock(world.Info, "iron");
        Unlock(world.Info, "aluminium");
        Unlock(world.Info, "copper");
        Unlock(world.Info, "plastic");
        Unlock(world.Info, "stainless-steel");

        ComponentDesign design;
        ComponentDesigner.StartResearched = true;
        try
        {
            design = ComponentDesignFromJson.Create(
                world.Faction,
                world.Info.Data,
                Game.StartingGameData.ComponentDesigns["default-design-ship-construction"]);
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }

        var colony = Colony(world, withOffice: true);
        var job = new IndustryJob(design);
        job.InitialiseJob(1, false);
        job.InstallOn = colony;
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs.Add(job);

        Complete(colony, design, job);

        var stored = StoredInstances(colony);
        Assert.That(stored, Has.Count.EqualTo(1));
        Assert.That(stored.Single().Design, Is.SameAs(design));
        Assert.That(MountedCount(colony), Is.EqualTo(0));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
    }

    [Test]
    public void RefiningBatch_AddsTheMaterial()
    {
        var world = NewSystem();
        Unlock(world.Info, "stainless-steel");
        var colony = Colony(world, withOffice: false);
        var material = (ProcessedMaterial)world.Info.IndustryDesigns["stainless-steel"];
        var line = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId];
        line.IndustryTypeRates["refining"] = 500;
        var job = new IndustryJob(material);
        job.InitialiseJob(1, false);
        line.Jobs.Add(job);

        material.OnConstructionComplete(
            colony, colony.GetDataBlob<CargoStorageDB>(), LineId, job, material);

        var storage = colony.GetDataBlob<CargoStorageDB>();
        Assert.That(storage.GetUnitsStored(material, false), Is.EqualTo(material.OutputAmount));
        Assert.That(StoredInstances(colony), Is.Empty);
        Assert.That(MountedCount(colony), Is.EqualTo(0));
    }

    [Test]
    public void ProductionCheckbox_IsEnabledOnlyForAColonyInstallationWithARate()
    {
        var world = NewSystem();
        Unlock(world.Info, "stainless-steel");
        var design = FactoryDesign();
        world.Info.IndustryDesigns[design.UniqueID] = design;
        var projector = new GameProjector(Game);

        var withOffice = Colony(world, withOffice: true);
        var enabled = FactoryRow(projector, withOffice, design.UniqueID);
        Assert.That(enabled.CanAutoInstall, Is.True);
        Assert.That(enabled.AutoInstallReason, Is.Empty);
        Assert.That(enabled.IndustryPointsPerUnit, Is.EqualTo(design.IndustryPointCosts));

        var steel = MaterialRow(projector, withOffice);
        Assert.That(steel.CanAutoInstall, Is.False);
        Assert.That(steel.AutoInstallReason, Does.Contain("not a colony installation"));

        var noOffice = Colony(world, withOffice: false);
        var disabled = FactoryRow(projector, noOffice, design.UniqueID);
        Assert.That(disabled.CanAutoInstall, Is.False);
        Assert.That(disabled.AutoInstallReason, Does.Contain("no installation points"));
    }

    static ConstructibleItemView FactoryRow(GameProjector projector, Entity colony, string designId)
    {
        var industry = projector.ProjectEntity(colony, colony.FactionOwnerID).GetView<IndustryView>();
        return industry!.ProductionLines.Single(line => line.Id == LineId)
            .Constructibles.Single(item => item.DesignId == designId);
    }

    static ConstructibleItemView MaterialRow(GameProjector projector, Entity colony)
    {
        var industry = projector.ProjectEntity(colony, colony.FactionOwnerID).GetView<IndustryView>();
        return industry!.ProductionLines.Single(line => line.Id == "refinery")
            .Constructibles.Single(item => item.DesignId == "stainless-steel");
    }

    static void Complete(Entity colony, ComponentDesign design, IndustryJob job)
    {
        design.OnConstructionComplete(
            colony, colony.GetDataBlob<CargoStorageDB>(), LineId, job, design);
    }

    static List<ComponentInstance> StoredInstances(Entity colony)
    {
        var store = colony.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"];
        return store.GetCargoables().Values.OfType<ComponentInstance>().ToList();
    }

    static int MountedCount(Entity colony)
        => colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Count;

    static ComponentDesign SiteOnlyDesign()
    {
        return new ComponentDesign
        {
            UniqueID = "test-academy-" + Guid.NewGuid().ToString("N"),
            Name = "Test Academy",
            CargoTypeID = "general-storage",
            MassPerUnit = 90_000,
            VolumePerUnit = 1,
            IndustryPointCosts = 45_000,
            IndustryTypeID = "installation-construction",
            ComponentMountType = ComponentMountType.PlanetInstallation,
            ResourceCosts = new Dictionary<string, long> { ["iron"] = 45_000 },
        };
    }

    static ComponentDesign FactoryDesign()
    {
        return new ComponentDesign
        {
            UniqueID = "test-factory-" + Guid.NewGuid().ToString("N"),
            Name = "Test Factory",
            CargoTypeID = "general-storage",
            MassPerUnit = 5_000_000,
            VolumePerUnit = 1,
            IndustryPointCosts = 100,
            IndustryTypeID = "installation-construction",
            ComponentMountType = ComponentMountType.ShipCargo | ComponentMountType.PlanetInstallation,
            ResourceCosts = new Dictionary<string, long>(),
        };
    }

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "Manufacture-" + Guid.NewGuid().ToString("N"));
        var system = new StarSystem();
        system.Initialize(Game, "Manufacture-" + Guid.NewGuid().ToString("N"), -1);
        system.SetActivityState(SystemActivityState.Stasis);
        return new World { System = system, Faction = faction, Info = faction.GetDataBlob<FactionInfoDB>() };
    }

    static void Unlock(FactionInfoDB info, string id)
    {
        info.Data.Unlock(id);
        if (info.Data.CargoGoods.IsMaterial(id))
            info.IndustryDesigns[id] = (IConstructableDesign)info.Data.CargoGoods.GetAny(id)!;
    }

    static Entity Colony(World world, bool withOffice)
    {
        var planet = Entity.Create();
        world.System.AddEntity(planet, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });

        var blobs = new List<BaseDataBlob>
        {
            new NameDB("Colony"),
            new ColonyInfoDB { PlanetEntity = planet },
            new CargoStorageDB("general-storage", 100000),
            new ComponentInstancesDB(),
            new IndustryAbilityDB(LineId, new IndustryAbilityDB.ProductionLine
            {
                Name = "Factory",
                IndustryTypeRates = new Dictionary<string, int> { ["installation-construction"] = 500 },
            }),
        };
        if (withOffice)
            blobs.Add(new LocalConstructionDB { PointsPerDay = 5 });

        var colony = Entity.Create(world.Faction.Id);
        world.System.AddEntity(colony, blobs);
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines["refinery"] = new IndustryAbilityDB.ProductionLine
        {
            Name = "Refinery",
            IndustryTypeRates = new Dictionary<string, int> { ["refining"] = 500 },
        };
        return colony;
    }

    sealed class World
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public FactionInfoDB Info = null!;
    }
}
