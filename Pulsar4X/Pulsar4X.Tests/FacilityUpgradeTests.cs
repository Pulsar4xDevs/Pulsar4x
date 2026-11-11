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
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Upgrading an installed facility to another design of the same template, through the colony's
/// installation queue. The old facility keeps its line working until the job finishes.
/// </summary>
[TestFixture]
public class FacilityUpgradeTests
{
    static readonly Game Game = InitializeGame();
    const string Rate = "component-construction";

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
    public void Eligibility_ListsOnlyOtherDesignsOfTheSameTemplate()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 10);
        var current = Facility(world, "Factory Mk1", "test-factory", rate: 10, siteOnly: true);
        var better = Facility(world, "Factory Mk2", "test-factory", rate: 30, siteOnly: true);
        var other = Facility(world, "Refinery", "test-refinery", rate: 30, siteOnly: true);
        var retired = Facility(world, "Factory Mk0", "test-factory", rate: 5, siteOnly: true);
        retired.IsValid = false;
        string lineId = Install(colony, current);

        var line = Line(colony, lineId);
        Assert.That(line.FacilityDesignName, Is.EqualTo("Factory Mk1"));
        Assert.That(line.UpgradeTargets.Select(t => t.DesignId), Is.EqualTo(new[] { better.UniqueID }));
        Assert.That(line.UpgradeTargets.Select(t => t.DesignId), Does.Not.Contain(current.UniqueID));
        Assert.That(line.UpgradeTargets.Select(t => t.DesignId), Does.Not.Contain(other.UniqueID));
        Assert.That(line.UpgradeReason, Is.Empty);
        Assert.That(line.UpgradeInProgress, Is.False);
        Assert.That(line.UpgradeTargets.Single().Kind, Is.EqualTo("Assemble"));
        Assert.That(line.UpgradeTargets.Single().PointCost, Is.EqualTo(better.IndustryPointCosts));

        var lonely = Colony(world, pointsPerDay: 10);
        string refineryLine = Install(lonely, other);
        var none = Line(lonely, refineryLine);
        Assert.That(none.UpgradeTargets, Is.Empty);
        Assert.That(none.UpgradeReason, Is.EqualTo(FacilityUpgrade.NoOtherDesign));

        Assert.That(FacilityUpgrade.Candidates(world.Info, current), Is.EqualTo(new[] { better }));
    }

    [Test]
    public void AssembleUpgrade_SpendsTheNewRecipe_KeepsTheLineWorking_ThenSwapsAndMovesJobs()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 10);
        var current = Facility(world, "Factory Mk1", "test-factory", rate: 10, siteOnly: true, maxVolume: 100);
        var better = Facility(world, "Factory Mk2", "test-factory", rate: 30, siteOnly: true, maxVolume: 500);
        better.IndustryPointCosts = 20;
        better.ResourceCosts = new Dictionary<string, long> { ["iron"] = 4 };
        // Massless facilities need no infrastructure, so the line runs at its full rate.
        current.MassPerUnit = 0;
        better.MassPerUnit = 0;
        string lineId = Install(colony, current);
        var iron = StockIron(world, colony, 1000);
        var widgetJob = QueueWidget(world, colony, lineId);

        var queued = Translate(world, colony, new UpgradeFacilityCommand(colony.Id, lineId, better.UniqueID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var construction = colony.GetDataBlob<LocalConstructionDB>();
        var job = construction.BuildQueue.Single();
        Assert.That(job.Kind, Is.EqualTo("Upgrade (Assemble)"));
        Assert.That(job.UpgradeLineId, Is.EqualTo(lineId));
        Assert.That(job.PointCost, Is.EqualTo(20));
        Assert.That(job.ResourcesRemaining["iron"], Is.EqualTo(4));

        var inProgress = Line(colony, lineId);
        Assert.That(inProgress.UpgradeInProgress, Is.True);
        Assert.That(inProgress.UpgradeReason, Is.EqualTo(FacilityUpgrade.InProgress));
        var twice = Translate(world, colony, new UpgradeFacilityCommand(colony.Id, lineId, better.UniqueID));
        Assert.That(twice.Accepted, Is.False);
        Assert.That(twice.RejectionReason, Is.EqualTo(FacilityUpgrade.InProgress));
        Assert.That(construction.BuildQueue.Count, Is.EqualTo(1));

        var storage = colony.GetDataBlob<CargoStorageDB>();
        long ironBefore = storage.GetUnitsStored(iron, false);
        new LocalConstructionProcessor().ProcessEntity(colony, 0);
        Assert.That(job.PointsAccumulated, Is.EqualTo(10));
        Assert.That(storage.GetUnitsStored(iron, false), Is.EqualTo(ironBefore - 2));
        Assert.That(MountedDesigns(colony), Is.EqualTo(new[] { current.UniqueID }));

        // The old line keeps its rate and its job while the upgrade is under way.
        long pointsLeft = widgetJob.ProductionPointsLeft;
        new IndustryProcessor().ProcessEntity(colony, 0);
        long oldDay = pointsLeft - widgetJob.ProductionPointsLeft;
        Assert.That(oldDay, Is.GreaterThanOrEqualTo(10).And.LessThan(30));
        pointsLeft = widgetJob.ProductionPointsLeft;

        // The widget line draws iron too. Count the upgrade's second half from here.
        long ironMidway = storage.GetUnitsStored(iron, false);
        new LocalConstructionProcessor().ProcessEntity(colony, 0);
        Assert.That(construction.BuildQueue, Is.Empty);
        Assert.That(storage.GetUnitsStored(iron, false), Is.EqualTo(ironMidway - 2));
        Assert.That(MountedDesigns(colony), Is.EqualTo(new[] { better.UniqueID }));

        var industry = colony.GetDataBlob<IndustryAbilityDB>();
        var newId = colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single().UniqueID;
        Assert.That(industry.ProductionLines.Keys, Is.EqualTo(new[] { newId }));
        var line = industry.ProductionLines[newId];
        Assert.That(line.Name, Is.EqualTo("Factory Mk2"));
        Assert.That(line.IndustryTypeRates[Rate], Is.EqualTo(30));
        Assert.That(line.MaxVolume, Is.EqualTo(500));
        Assert.That(line.Jobs.Single(), Is.SameAs(widgetJob));
        Assert.That(widgetJob.ProductionPointsLeft, Is.EqualTo(pointsLeft));

        new IndustryProcessor().ProcessEntity(colony, 0);
        Assert.That(pointsLeft - widgetJob.ProductionPointsLeft, Is.GreaterThanOrEqualTo(30));
    }

    [Test]
    public void MountUpgrade_CostsByMass_ReturnsTheOldCrate_AndMovesJobs()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 100);
        var current = Facility(world, "Factory Mk1", "test-factory", rate: 10, siteOnly: false);
        var better = Facility(world, "Factory Mk2", "test-factory", rate: 30, siteOnly: false);
        better.MassPerUnit = 3_000_000;
        var oldCrate = new ComponentInstance(current);
        colony.AddComponent(oldCrate);
        string lineId = oldCrate.UniqueID;
        var newCrate = new ComponentInstance(better);
        var storage = colony.GetDataBlob<CargoStorageDB>();
        Assert.That(storage.AddCargoByUnit(newCrate, 1), Is.EqualTo(1));
        StockIron(world, colony, 1000);
        var widgetJob = QueueWidget(world, colony, lineId);

        var target = Line(colony, lineId).UpgradeTargets.Single();
        Assert.That(target.Kind, Is.EqualTo("Mount"));
        Assert.That(target.PointCost, Is.EqualTo(30));
        Assert.That(target.Reason, Is.Empty);

        var queued = Translate(world, colony, new UpgradeFacilityCommand(colony.Id, lineId, better.UniqueID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
        var job = colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Single();
        Assert.That(job.Kind, Is.EqualTo("Upgrade (Mount)"));
        Assert.That(job.Instance, Is.SameAs(newCrate));
        Assert.That(job.PointCost, Is.EqualTo(LocalConstructionJob.MountCost(better.MassPerUnit)));
        Assert.That(job.ResourcesRemaining, Is.Empty);

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        var mounted = colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single();
        Assert.That(mounted, Is.SameAs(newCrate));
        Assert.That(StoredInstances(colony), Is.EqualTo(new[] { oldCrate }));
        var industry = colony.GetDataBlob<IndustryAbilityDB>();
        Assert.That(industry.ProductionLines.Keys, Is.EqualTo(new[] { newCrate.UniqueID }));
        Assert.That(industry.ProductionLines[newCrate.UniqueID].Name, Is.EqualTo("Factory Mk2"));
        Assert.That(industry.ProductionLines[newCrate.UniqueID].IndustryTypeRates[Rate], Is.EqualTo(30));
        Assert.That(industry.ProductionLines[newCrate.UniqueID].Jobs.Single(), Is.SameAs(widgetJob));
    }

    [Test]
    public void InvalidCommands_AreRejected_AndDoNotSwap()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 100);
        var current = Facility(world, "Factory Mk1", "test-factory", rate: 10, siteOnly: true);
        var other = Facility(world, "Refinery", "test-refinery", rate: 30, siteOnly: true);
        var crateOnly = Facility(world, "Factory Crate", "test-factory", rate: 30, siteOnly: false);
        string lineId = Install(colony, current);

        var cases = new (GameCommand Command, string Reason)[]
        {
            (new UpgradeFacilityCommand(colony.Id, lineId, other.UniqueID), FacilityUpgrade.NotSameType),
            (new UpgradeFacilityCommand(colony.Id, lineId, current.UniqueID), FacilityUpgrade.SameDesign),
            (new UpgradeFacilityCommand(colony.Id, lineId, crateOnly.UniqueID), FacilityUpgrade.NoCrate),
            (new UpgradeFacilityCommand(colony.Id, "no-such-line", crateOnly.UniqueID), FacilityUpgrade.NoFacility),
        };
        foreach (var (command, reason) in cases)
        {
            var result = Translate(world, colony, command);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(reason));
        }
        Assert.That(Translate(world, colony, new UpgradeFacilityCommand(colony.Id, lineId, "missing-design")).Accepted, Is.False);

        var noPoints = Colony(world, pointsPerDay: 0);
        string idleLine = Install(noPoints, current);
        var refused = Translate(world, noPoints, new UpgradeFacilityCommand(noPoints.Id, idleLine, crateOnly.UniqueID));
        Assert.That(refused.Accepted, Is.False);

        Assert.That(Line(colony, lineId).UpgradeTargets.Single().Reason, Is.EqualTo(FacilityUpgrade.NoCrate));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        new LocalConstructionProcessor().ProcessEntity(colony, 0);
        Assert.That(MountedDesigns(colony), Is.EqualTo(new[] { current.UniqueID }));
        Assert.That(colony.GetDataBlob<IndustryAbilityDB>().ProductionLines.Keys, Is.EqualTo(new[] { lineId }));
    }

    [Test]
    public void FinishedUpgrade_ThatCannotReturnTheOldCrate_WaitsWithTheOldFacility()
    {
        var world = NewSystem();
        var colony = Colony(world, pointsPerDay: 100, cargoVolume: 10);
        var current = Facility(world, "Factory Mk1", "test-factory", rate: 10, siteOnly: false);
        current.VolumePerUnit = 50;
        var better = Facility(world, "Factory Mk2", "test-factory", rate: 30, siteOnly: false);
        var oldCrate = new ComponentInstance(current);
        colony.AddComponent(oldCrate);
        string lineId = oldCrate.UniqueID;
        var newCrate = new ComponentInstance(better);
        var storage = colony.GetDataBlob<CargoStorageDB>();
        Assert.That(storage.AddCargoByUnit(newCrate, 1), Is.EqualTo(1));
        var widgetJob = QueueWidget(world, colony, lineId);

        var queued = Translate(world, colony, new UpgradeFacilityCommand(colony.Id, lineId, better.UniqueID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
        var construction = colony.GetDataBlob<LocalConstructionDB>();

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        var job = construction.BuildQueue.Single();
        Assert.That(job.IsComplete, Is.True);
        Assert.That(colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single(), Is.SameAs(oldCrate));
        Assert.That(StoredInstances(colony), Is.EqualTo(new[] { newCrate }));
        var industry = colony.GetDataBlob<IndustryAbilityDB>();
        Assert.That(industry.ProductionLines.Keys, Is.EqualTo(new[] { lineId }));
        Assert.That(industry.ProductionLines[lineId].Jobs.Single(), Is.SameAs(widgetJob));

        storage.TypeStores["general-storage"].MaxVolume = 1000;
        storage.TypeStores["general-storage"].FreeVolume += 990;
        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(construction.BuildQueue, Is.Empty);
        Assert.That(colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single(), Is.SameAs(newCrate));
        Assert.That(StoredInstances(colony), Is.EqualTo(new[] { oldCrate }));
        Assert.That(industry.ProductionLines[newCrate.UniqueID].Jobs.Single(), Is.SameAs(widgetJob));
    }

    static CommandResult Translate(World world, Entity colony, GameCommand command)
        => new CommandTranslator(Game).Translate(world.Faction, colony, command);

    static ProductionLineView Line(Entity colony, string lineId)
    {
        var view = new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<IndustryView>();
        return view!.ProductionLines.Single(l => l.Id == lineId);
    }

    static string Install(Entity colony, ComponentDesign design)
    {
        var instance = new ComponentInstance(design);
        colony.AddComponent(instance);
        return instance.UniqueID;
    }

    static string[] MountedDesigns(Entity colony)
        => colony.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Select(c => c.Design.UniqueID).ToArray();

    static List<ComponentInstance> StoredInstances(Entity colony)
    {
        var store = colony.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"];
        return store.GetCargoables().Values.OfType<ComponentInstance>().ToList();
    }

    static ICargoable StockIron(World world, Entity colony, long units)
    {
        world.Info.Data.Unlock("iron");
        var iron = world.Info.Data.CargoGoods.GetAny("iron")!;
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(iron, units), Is.EqualTo(units));
        return iron;
    }

    /// <summary>A part the line builds slowly, so a job is still open across the swap.</summary>
    static IndustryJob QueueWidget(World world, Entity colony, string lineId)
    {
        var widget = new ComponentDesign
        {
            UniqueID = "test-widget-" + Guid.NewGuid().ToString("N"),
            Name = "Widget",
            CargoTypeID = "general-storage",
            MassPerUnit = 1,
            VolumePerUnit = 0.001,
            IndustryTypeID = Rate,
            IndustryPointCosts = 1000,
            ComponentMountType = ComponentMountType.ShipCargo,
            ResourceCosts = new Dictionary<string, long> { ["iron"] = 1 },
        };
        world.Info.InternalComponentDesigns[widget.UniqueID] = widget;
        world.Info.IndustryDesigns[widget.UniqueID] = widget;
        var job = new IndustryJob(world.Info, widget.UniqueID);
        job.InitialiseJob(1, false);
        IndustryTools.AddJob(colony, lineId, job);
        return job;
    }

    static ComponentDesign Facility(World world, string name, string template, int rate, bool siteOnly, double maxVolume = double.PositiveInfinity)
    {
        var mount = siteOnly
            ? ComponentMountType.PlanetInstallation
            : ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;
        var design = new ComponentDesign
        {
            UniqueID = template + "-" + Guid.NewGuid().ToString("N"),
            Name = name,
            TemplateID = template,
            CargoTypeID = "general-storage",
            MassPerUnit = 5_000_000,
            VolumePerUnit = 1,
            IndustryPointCosts = 100,
            ComponentMountType = mount,
        };
        design.AttributesByType[typeof(IndustryAtb)] =
            new IndustryAtb(new Dictionary<string, double> { [Rate] = rate }, maxVolume);
        world.Info.InternalComponentDesigns[design.UniqueID] = design;
        return design;
    }

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "FacilityUpgrade-" + Guid.NewGuid().ToString("N"));
        var system = new StarSystem();
        system.Initialize(Game, "FacilityUpgrade-" + Guid.NewGuid().ToString("N"), -1);
        system.SetActivityState(SystemActivityState.Stasis);
        return new World { System = system, Faction = faction, Info = faction.GetDataBlob<FactionInfoDB>() };
    }

    static Entity Colony(World world, long pointsPerDay, double cargoVolume = 100000)
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
            new CargoStorageDB("general-storage", cargoVolume),
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
