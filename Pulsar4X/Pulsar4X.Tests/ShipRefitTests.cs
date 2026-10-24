using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class ShipRefitTests
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
    public void Yard_SwapsOneComponent_AndCopiesTheMassCost()
    {
        var world = NewSystem();
        var design = Hull(world, "Light Hull", 1000);
        var colony = Colony(world, design.MassPerUnit, refitPoints: 5);
        var ship = Ship(world, colony, design);
        var mounted = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single();
        var incoming = Part(world, "Spare", 1000);
        var spare = new ComponentInstance(incoming);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(spare, 1), Is.EqualTo(1));
        long expected = LocalConstructionJob.MountCost(mounted.MassPerUnit) + LocalConstructionJob.MountCost(spare.MassPerUnit);
        var office = MountColonyPart(colony);
        int ships = ShipCount(world);
        var translator = new CommandTranslator(Game);

        var queued = translator.Translate(world.Faction, colony, new QueueShipRefitCommand(
            colony.Id, LineId, ship.Id, new[] { mounted.ID }, new[] { spare.ID }));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = colony.GetDataBlob<LocalConstructionDB>().RefitQueue.Single();
        Assert.That(job.PointCost, Is.EqualTo(expected));
        Assert.That(job.PointCost, Is.EqualTo(2));
        var blocked = translator.Translate(world.Faction, ship, new AddToConstructionQueueCommand(ship.Id, "no-such-design"));
        Assert.That(blocked.Accepted, Is.False);
        Assert.That(blocked.RejectionReason, Does.Contain("in refit"));
        var stored = translator.Translate(world.Faction, ship, new UninstallComponentCommand(ship.Id, "not-a-part"));
        Assert.That(stored.RejectionReason, Does.Not.Contain("in refit"));

        var shipView = new GameProjector(Game).ProjectEntity(ship, ship.FactionOwnerID).GetView<ShipRefitView>();
        Assert.That(shipView, Is.Not.Null);
        Assert.That(shipView!.PointCost, Is.EqualTo(expected));
        Assert.That(shipView.Removing, Does.Contain(mounted.Name));
        Assert.That(shipView.Mounting, Does.Contain(spare.Name));
        var line = YardLine(world, colony);
        Assert.That(line.RefitPointsPerDay, Is.EqualTo(5));
        Assert.That(line.RefitJobs.Single().PointCost, Is.EqualTo(expected));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(colony.GetDataBlob<LocalConstructionDB>().RefitQueue, Is.Empty);
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().GetUnitsStored(mounted, false), Is.EqualTo(1));
        Assert.That(colony.GetDataBlob<CargoStorageDB>().GetUnitsStored(spare, false), Is.EqualTo(0));
        Assert.That(ShipRefitJob.OnShip(ship, spare.ID), Is.True);
        Assert.That(ShipRefitJob.OnShip(ship, mounted.ID), Is.False);
        Assert.That(ShipRefitJob.OnShip(colony, office.ID), Is.True);
        Assert.That(ShipCount(world), Is.EqualTo(ships));
        Assert.That(new GameProjector(Game).ProjectEntity(ship, ship.FactionOwnerID).GetView<ShipRefitView>(), Is.Null);
        var after = translator.Translate(world.Faction, ship, new AddToConstructionQueueCommand(ship.Id, "no-such-design"));
        Assert.That(after.RejectionReason, Does.Not.Contain("in refit"));
    }

    [Test]
    public void Office_WithOnlyInstallation_RejectsTheRefit()
    {
        var world = NewSystem();
        var design = Hull(world, "Light Hull", 1000);
        var colony = Colony(world, design.MassPerUnit, refitPoints: 0, installationPoints: 5);
        var ship = Ship(world, colony, design);
        var mounted = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single();
        var spare = new ComponentInstance(Part(world, "Spare", 1000));
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(spare, 1), Is.EqualTo(1));

        var refused = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueShipRefitCommand(
            colony.Id, LineId, ship.Id, new[] { mounted.ID }, new[] { spare.ID }));

        Assert.That(refused.Accepted, Is.False);
        Assert.That(refused.RejectionReason, Does.Contain("refit"));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().RefitQueue, Is.Empty);
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().PointsPerDay, Is.EqualTo(5));
    }

    [Test]
    public void ShipOverTheSlip_IsRejected()
    {
        var world = NewSystem();
        var design = Hull(world, "Light Hull", 1000);
        var colony = Colony(world, design.MassPerUnit - 1, refitPoints: 5);
        var ship = Ship(world, colony, design);
        var mounted = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single();
        var spare = new ComponentInstance(Part(world, "Spare", 1000));
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(spare, 1), Is.EqualTo(1));
        var translator = new CommandTranslator(Game);
        var command = new QueueShipRefitCommand(
            colony.Id, LineId, ship.Id, new[] { mounted.ID }, new[] { spare.ID });

        var refused = translator.Translate(world.Faction, colony, command);
        Assert.That(refused.Accepted, Is.False);
        Assert.That(refused.RejectionReason, Does.Contain("slip"));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().RefitQueue, Is.Empty);
        Assert.That(YardLine(world, colony).RefitShips.Single().OverSlip, Is.True);

        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].MaxVolume = design.MassPerUnit;
        var fits = translator.Translate(world.Faction, colony, command);
        Assert.That(fits.Accepted, Is.True, fits.RejectionReason);
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().RefitQueue, Has.Count.EqualTo(1));
    }

    [Test]
    public void ColonyInstallation_IsRejected()
    {
        var world = NewSystem();
        var design = Hull(world, "Light Hull", 1000);
        var colony = Colony(world, design.MassPerUnit, refitPoints: 5);
        var ship = Ship(world, colony, design);
        var crate = new ComponentInstance(new ComponentDesign
        {
            UniqueID = "refit-office-" + Guid.NewGuid().ToString("N"),
            Name = "Office",
            MassPerUnit = 1000,
            VolumePerUnit = 1,
            CargoTypeID = "general-storage",
            ComponentMountType = ComponentMountType.PlanetInstallation,
        });
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));

        var refused = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueShipRefitCommand(
            colony.Id, LineId, ship.Id, Array.Empty<int>(), new[] { crate.ID }));

        Assert.That(refused.Accepted, Is.False);
        Assert.That(refused.RejectionReason, Does.Contain("installation"));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().RefitQueue, Is.Empty);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().GetUnitsStored(crate, false), Is.EqualTo(1));
    }

    [Test]
    public void Freighter_RefitToColonyShip_KeepsTheSensorAndTank()
    {
        var world = NewSystem();
        var sensorFlags = Game.StartingGameData.ComponentTemplates["passive-sensor"].MountType;
        var tankFlags = Game.StartingGameData.ComponentTemplates["stainless-steel-fuel-tank"].MountType;
        var bayFlags = Game.StartingGameData.ComponentTemplates["ship-construction"].MountType;
        Assert.That(sensorFlags.HasFlag(ComponentMountType.PlanetInstallation), Is.True);
        Assert.That(tankFlags.HasFlag(ComponentMountType.PlanetInstallation), Is.True);

        var sensor = FlaggedPart(world, "Passive Sensor S50", sensorFlags);
        var tank = FlaggedPart(world, "Fuel Tank 1500m^3", tankFlags);
        var bay = FlaggedPart(world, "Construction Bay", bayFlags);
        world.Info.Data.Unlock("plastic");
        world.Info.Data.Unlock("plastic-armor");
        var armor = (world.Info.Data.Armor["plastic-armor"], 0f);
        var freighter = new ShipDesign(
            world.Info, "Freighter", new List<(ComponentDesign, int)> { (sensor, 1), (tank, 1) }, armor);
        freighter.Initialise(world.Info);
        freighter.ResourceCosts.Clear();
        var colonyShip = new ShipDesign(
            world.Info, "Colony Ship",
            new List<(ComponentDesign, int)> { (sensor, 1), (tank, 1), (bay, 1) }, armor);
        colonyShip.Initialise(world.Info);
        colonyShip.ResourceCosts.Clear();

        var colony = Colony(world, Math.Max(freighter.MassPerUnit, colonyShip.MassPerUnit), refitPoints: 5);
        var ship = Ship(world, colony, freighter);
        var spare = new ComponentInstance(bay);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(spare, 1), Is.EqualTo(1));

        var line = YardLine(world, colony);
        var aboard = line.RefitShips.Single();
        Assert.That(aboard.Parts.Select(part => part.DesignId), Is.EquivalentTo(new[] { sensor.UniqueID, tank.UniqueID }));
        var target = line.RefitTargets.Single(view => view.DesignId == colonyShip.UniqueID);
        var diff = RefitDesignDiff.Compute(target, aboard.Parts, line.RefitCargo);
        Assert.That(diff.RemoveInstanceIds, Is.Empty);
        Assert.That(diff.MountInstanceIds, Is.EqualTo(new[] { spare.ID }));
        Assert.That(diff.Missing, Is.Empty);

        var queued = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueShipRefitCommand(
            colony.Id, LineId, ship.Id, diff.RemoveInstanceIds, diff.MountInstanceIds, colonyShip.UniqueID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
    }

    [Test]
    public void ColonyShipDiff_MountsTheBayWhenItIsInCargo()
    {
        var freighter = Game.StartingGameData.ShipDesigns["default-ship-design-freighter"];
        var colonyShip = Game.StartingGameData.ShipDesigns["default-ship-design-colony-ship"];
        var onShip = new List<RefitPartView>();
        int next = 1;
        foreach (var part in freighter.Components)
        {
            for (int i = 0; i < part.Amount; i++)
                onShip.Add(new RefitPartView(next++, part.Id, 1, 1) { DesignId = part.Id });
        }
        var target = new RefitTargetView(colonyShip.UniqueID, colonyShip.Name)
        {
            Parts = colonyShip.Components
                .Select(part => new RefitTargetPartView(part.Id, part.Id, (int)part.Amount))
                .ToList(),
        };
        var bay = new RefitPartView(next, "Construction Bay", 5000, 1)
        {
            DesignId = "default-design-ship-construction",
        };

        var withCargo = RefitDesignDiff.Compute(target, onShip, new[] { bay });
        Assert.That(withCargo.RemoveInstanceIds, Is.Empty);
        Assert.That(withCargo.MountInstanceIds, Is.EqualTo(new[] { bay.ComponentId }));
        Assert.That(withCargo.Missing, Is.Empty);

        var without = RefitDesignDiff.Compute(target, onShip, Array.Empty<RefitPartView>());
        Assert.That(without.RemoveInstanceIds, Is.Empty);
        Assert.That(without.MountInstanceIds, Is.Empty);
        Assert.That(without.Missing, Has.Count.EqualTo(1));
        Assert.That(without.Missing[0].DesignId, Is.EqualTo("default-design-ship-construction"));
        Assert.That(without.Missing[0].Count, Is.EqualTo(1));
    }

    [Test]
    public void MissingBay_QueuesAComponentJobAndNotARefit()
    {
        var world = NewSystem();
        var bay = new ComponentDesign
        {
            UniqueID = "bay-" + Guid.NewGuid().ToString("N"),
            Name = "Construction Bay",
            IndustryTypeID = "component-construction",
            ComponentMountType = ComponentMountType.ShipComponent | ComponentMountType.ShipCargo,
            CargoTypeID = "general-storage",
            MassPerUnit = 5000,
            VolumePerUnit = 500,
            IndustryPointCosts = 5000,
        };
        world.Info.IndustryDesigns[bay.UniqueID] = bay;
        var colony = Colony(world, 1_000_000, refitPoints: 5);
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId]
            .IndustryTypeRates["component-construction"] = 100;

        var queued = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueIndustryJobCommand(
            colony.Id, LineId, bay.UniqueID, 1, false, false));

        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().RefitQueue, Is.Empty);
        var job = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs.Single();
        Assert.That(job.ItemGuid, Is.EqualTo(bay.UniqueID));
        Assert.That(job.InstallOn, Is.Null);
        Assert.That(YardLine(world, colony).Constructibles.Select(item => item.DesignId), Does.Contain(bay.UniqueID));
    }

    [Test]
    public void TargetDesign_AdoptsTheDesignWhenThePartsMatch()
    {
        var world = NewSystem();
        var (freighter, colonyShip, bay) = FreighterAndColonyShip(world);
        var colony = Colony(world, Math.Max(freighter.MassPerUnit, colonyShip.MassPerUnit), refitPoints: 5);
        var ship = Ship(world, colony, freighter);
        var spare = new ComponentInstance(bay);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(spare, 1), Is.EqualTo(1));

        var queued = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueShipRefitCommand(
            colony.Id, LineId, ship.Id, Array.Empty<int>(), new[] { spare.ID }, colonyShip.UniqueID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().RefitQueue.Single().TargetDesignId, Is.EqualTo(colonyShip.UniqueID));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(ship.GetDataBlob<ShipInfoDB>().Design.UniqueID, Is.EqualTo(colonyShip.UniqueID));
        Assert.That(ship.GetDataBlob<ShipInfoDB>().Tanker, Is.False);
    }

    [Test]
    public void ManualRefit_KeepsTheOldDesign()
    {
        var world = NewSystem();
        var (freighter, _, bay) = FreighterAndColonyShip(world);
        var colony = Colony(world, freighter.MassPerUnit, refitPoints: 5);
        var ship = Ship(world, colony, freighter);
        var spare = new ComponentInstance(bay);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(spare, 1), Is.EqualTo(1));

        var queued = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueShipRefitCommand(
            colony.Id, LineId, ship.Id, Array.Empty<int>(), new[] { spare.ID }));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(ship.GetDataBlob<ShipInfoDB>().Design.UniqueID, Is.EqualTo(freighter.UniqueID));
        Assert.That(ShipRefitJob.OnShip(ship, spare.ID), Is.True);
    }

    [Test]
    public void TargetDesign_LeavesTheNameWhenThePartsDoNotMatch()
    {
        var world = NewSystem();
        var (freighter, colonyShip, bay) = FreighterAndColonyShip(world);
        var colony = Colony(world, Math.Max(freighter.MassPerUnit, colonyShip.MassPerUnit), refitPoints: 5);
        var ship = Ship(world, colony, freighter);
        var kept = ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.Single();
        var spare = new ComponentInstance(bay);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(spare, 1), Is.EqualTo(1));

        var queued = new CommandTranslator(Game).Translate(world.Faction, colony, new QueueShipRefitCommand(
            colony.Id, LineId, ship.Id, new[] { kept.ID }, new[] { spare.ID }, colonyShip.UniqueID));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        new LocalConstructionProcessor().ProcessEntity(colony, 0);

        Assert.That(ship.GetDataBlob<ShipInfoDB>().Design.UniqueID, Is.EqualTo(freighter.UniqueID));
        Assert.That(ShipRefitJob.OnShip(ship, spare.ID), Is.True);
        Assert.That(ShipRefitJob.OnShip(ship, kept.ID), Is.False);
    }

    static (ShipDesign Freighter, ShipDesign ColonyShip, ComponentDesign Bay) FreighterAndColonyShip(World world)
    {
        world.Info.Data.Unlock("plastic");
        world.Info.Data.Unlock("plastic-armor");
        var armor = (world.Info.Data.Armor["plastic-armor"], 0f);
        var kept = Part(world, "Kept", 1000);
        var bay = Part(world, "Bay", 1000);
        var freighter = new ShipDesign(world.Info, "Freighter", new List<(ComponentDesign, int)> { (kept, 1) }, armor);
        freighter.Initialise(world.Info);
        freighter.ResourceCosts.Clear();
        var colonyShip = new ShipDesign(
            world.Info, "Colony Ship", new List<(ComponentDesign, int)> { (kept, 1), (bay, 1) }, armor);
        colonyShip.Initialise(world.Info);
        colonyShip.ResourceCosts.Clear();
        return (freighter, colonyShip, bay);
    }

    static ComponentInstance MountColonyPart(Entity colony)
    {
        var office = new ComponentInstance(new ComponentDesign
        {
            UniqueID = "refit-mounted-office-" + Guid.NewGuid().ToString("N"),
            Name = "Office",
            MassPerUnit = 1000,
            VolumePerUnit = 1,
            CargoTypeID = "general-storage",
            ComponentMountType = ComponentMountType.PlanetInstallation,
        });
        colony.AddComponent(office);
        return office;
    }

    static ComponentDesign Part(World world, string name, long mass)
        => FlaggedPart(world, name, ComponentMountType.ShipComponent, mass);

    static ComponentDesign FlaggedPart(World world, string name, ComponentMountType mount, long mass = 1000)
    {
        return new ComponentDesign
        {
            UniqueID = "refit-part-" + Guid.NewGuid().ToString("N"),
            Name = name,
            MassPerUnit = mass,
            VolumePerUnit = 1,
            CargoTypeID = "general-storage",
            ComponentMountType = mount,
        };
    }

    static ShipDesign Hull(World world, string name, long partMass)
    {
        world.Info.Data.Unlock("plastic");
        world.Info.Data.Unlock("plastic-armor");
        var part = Part(world, "Part", partMass);
        var design = new ShipDesign(
            world.Info,
            name,
            new List<(ComponentDesign, int)> { (part, 1) },
            (world.Info.Data.Armor["plastic-armor"], 0f));
        design.Initialise(world.Info);
        design.ResourceCosts.Clear();
        return design;
    }

    static Entity Ship(World world, Entity colony, ShipDesign design)
    {
        var planet = colony.GetDataBlob<ColonyInfoDB>().PlanetEntity;
        var ship = ShipFactory.CreateShip(design, world.Faction, planet, "Refit Ship");
        ship.SetDataBlob(new CargoStorageDB());
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        colony.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        return ship;
    }

    static Entity Colony(World world, double slip, long refitPoints, long installationPoints = 0)
    {
        var planet = Entity.Create();
        world.System.AddEntity(planet, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            new PositionDB(0, 0, 0),
            new OrbitDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6_000_000),
            new SystemBodyInfoDB { LengthOfDay = TimeSpan.FromHours(24) },
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
            new PositionDB(new Vector3(planet.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0), planet),
            new CargoStorageDB("general-storage", 1e8),
            new IndustryAbilityDB(LineId, line),
            new ComponentInstancesDB(),
            new LocalConstructionDB { RefitPointsPerDay = refitPoints, PointsPerDay = installationPoints },
        });
        colony.GetDataBlob<NameDB>().SetName(world.Faction.Id, "Yard");
        return colony;
    }

    static ProductionLineView YardLine(World world, Entity colony)
        => new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<IndustryView>()!
            .ProductionLines.Single(line => line.Id == LineId);

    static int ShipCount(World world)
        => world.System.GetAllEntitiesWithDataBlob<ShipInfoDB>().Count;

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "Refit-" + Guid.NewGuid().ToString("N"));
        var system = new StarSystem();
        system.Initialize(Game, "Refit-" + Guid.NewGuid().ToString("N"), -1);
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
