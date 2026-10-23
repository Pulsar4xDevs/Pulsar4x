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
    {
        return new ComponentDesign
        {
            UniqueID = "refit-part-" + Guid.NewGuid().ToString("N"),
            Name = name,
            MassPerUnit = mass,
            VolumePerUnit = 1,
            CargoTypeID = "general-storage",
            ComponentMountType = ComponentMountType.ShipComponent,
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
