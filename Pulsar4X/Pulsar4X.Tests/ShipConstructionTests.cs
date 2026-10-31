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
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class ShipConstructionTests
{
    static readonly ModDataStore Store = LoadStore();
    static readonly Game Game = InitializeGame();

    static ModDataStore LoadStore()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        return store;
    }

    static Game InitializeGame()
    {
        var game = new Game(new NewGameSettings { MaxSystems = 4, CreatePlayerFaction = false }, Store);
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
    public void ColonyShip_InRange_MountsInfrastructure_AndTheShipLosesIt()
    {
        var world = NewSystem();
        var planet = Planet(world);
        var colony = EmptyColony(world, planet);
        var ship = ColonyShip(world, planet);
        InRange(ship, colony);

        var infra = world.Info.ComponentDesigns["default-design-infrastructure"];
        var crate = new ComponentInstance(infra);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));
        Assert.That(ship.GetDataBlob<LocalConstructionDB>().PointsPerDay, Is.EqualTo(5));
        Assert.That(colony.GetDataBlob<CargoStorageDB>().TypeStores, Is.Empty);
        Assert.That(LocalConstructionProcessor.InTransferRange(ship, colony), Is.True);

        var view = new GameProjector(Game).ProjectEntity(ship, ship.FactionOwnerID).GetView<ConstructionView>();
        Assert.That(view!.IsShip, Is.True);
        Assert.That(view.PointsPerDay, Is.EqualTo(5));
        Assert.That(view.ColoniesInRange.Select(item => item.EntityId), Is.EqualTo(new[] { colony.Id }));
        Assert.That(view.Crates.Select(item => item.ComponentId), Does.Contain(crate.ID));
        Assert.That(view.Crates.Single(item => item.ComponentId == crate.ID).MountCost, Is.EqualTo(1));

        var cargo = new GameProjector(Game).ProjectEntity(ship, ship.FactionOwnerID).GetView<CargoStorageView>();
        var item = cargo!.Stores.SelectMany(store => store.Items).Single(row => row.Id == crate.ID);
        Assert.That(item.ShowInstall, Is.True);
        Assert.That(item.CanInstall, Is.True);
        Assert.That(item.MountCost, Is.EqualTo(1));
        Assert.That(cargo.InstallColonyIds, Is.EqualTo(new[] { colony.Id }));

        var queued = new CommandTranslator(Game).Translate(
            world.Faction, ship, new InstallComponentCommand(ship.Id, crate.ID, colony.Id));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = ship.GetDataBlob<LocalConstructionDB>().BuildQueue.Peek();
        Assert.That(job.Kind, Is.EqualTo("Mount"));
        Assert.That(job.Instance, Is.SameAs(crate));
        Assert.That(job.PointCost, Is.EqualTo(1));
        Assert.That(job.TargetEntityId, Is.EqualTo(colony.Id));
        Assert.That(Mounted(colony), Is.Empty);

        new LocalConstructionProcessor().ProcessEntity(ship, 0);

        Assert.That(ship.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(crate, false), Is.EqualTo(0));
        Assert.That(Mounted(colony).Single(), Is.SameAs(crate));
        Assert.That(Mounted(ship).Select(part => part.Design.UniqueID), Does.Not.Contain(infra.UniqueID));
        Assert.That(colony.HasDataBlob<LocalConstructionDB>(), Is.False);
    }

    [Test]
    public void OutOfRange_IsRejected()
    {
        var world = NewSystem();
        var planet = Planet(world);
        var colony = EmptyColony(world, planet);
        var ship = ColonyShip(world, planet);
        OutOfRange(ship, colony);

        var infra = world.Info.ComponentDesigns["default-design-infrastructure"];
        var crate = new ComponentInstance(infra);
        ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1);
        Assert.That(LocalConstructionProcessor.InTransferRange(ship, colony), Is.False);

        var rejected = new CommandTranslator(Game).Translate(
            world.Faction, ship, new InstallComponentCommand(ship.Id, crate.ID, colony.Id));
        Assert.That(rejected.Accepted, Is.False);
        Assert.That(rejected.RejectionReason, Does.Contain("range"));
        Assert.That(ship.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(crate, false), Is.EqualTo(1));
        Assert.That(Mounted(colony), Is.Empty);

        var cargo = new GameProjector(Game).ProjectEntity(ship, ship.FactionOwnerID).GetView<CargoStorageView>();
        var item = cargo!.Stores.SelectMany(store => store.Items).Single(row => row.Id == crate.ID);
        Assert.That(item.ShowInstall, Is.True);
        Assert.That(item.CanInstall, Is.False);
        Assert.That(cargo.InstallColonyIds, Is.Empty);
        var view = new GameProjector(Game).ProjectEntity(ship, ship.FactionOwnerID).GetView<ConstructionView>();
        Assert.That(view!.ColoniesInRange, Is.Empty);
    }

    [Test]
    public void AShipWithNoInstallationRate_IsRejected()
    {
        var world = NewSystem();
        var planet = Planet(world);
        var colony = EmptyColony(world, planet);
        var ship = Freighter(world, planet);
        InRange(ship, colony);

        var infra = world.Info.ComponentDesigns["default-design-infrastructure"];
        var crate = new ComponentInstance(infra);
        ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1);
        Assert.That(ship.HasDataBlob<LocalConstructionDB>(), Is.False);

        var install = new CommandTranslator(Game).Translate(
            world.Faction, ship, new InstallComponentCommand(ship.Id, crate.ID, colony.Id));
        Assert.That(install.Accepted, Is.False);
        Assert.That(install.RejectionReason, Does.Contain("installation"));

        var assemble = new CommandTranslator(Game).Translate(
            world.Faction, ship, new AddToConstructionQueueCommand(
                ship.Id, world.Info.ComponentDesigns["default-design-local-construction"].UniqueID, null, colony.Id));
        Assert.That(assemble.Accepted, Is.False);
        Assert.That(assemble.RejectionReason, Does.Contain("installation"));
        Assert.That(Mounted(colony), Is.Empty);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(crate, false), Is.EqualTo(1));

        var projected = new GameProjector(Game).ProjectEntity(ship, ship.FactionOwnerID);
        Assert.That(projected.GetView<ConstructionView>(), Is.Null);
        var item = projected.GetView<CargoStorageView>()!.Stores.SelectMany(store => store.Items).Single(row => row.Id == crate.ID);
        Assert.That(item.ShowInstall, Is.False);
        Assert.That(item.CanInstall, Is.False);
    }

    [Test]
    public void ASiteOnlyDesignOfferedAsACrate_IsRejected()
    {
        var world = NewSystem();
        var planet = Planet(world);
        var colony = EmptyColony(world, planet);
        var ship = ColonyShip(world, planet);
        InRange(ship, colony);

        var cityHall = world.Info.ComponentDesigns["default-design-city-hall"];
        Assert.That(LocalConstructionJob.IsSiteOnly(cityHall), Is.True);
        var crate = new ComponentInstance(cityHall);
        ship.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"] = new TypeStore(1_000_000_000);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));

        var rejected = new CommandTranslator(Game).Translate(
            world.Faction, ship, new AddToConstructionQueueCommand(ship.Id, cityHall.UniqueID, crate.ID, colony.Id));
        Assert.That(rejected.Accepted, Is.False);
        Assert.That(rejected.RejectionReason, Does.Contain("crate"));
        Assert.That(ship.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(crate, false), Is.EqualTo(1));
        Assert.That(Mounted(colony), Is.Empty);
        Assert.That(colony.HasDataBlob<LocalConstructionDB>(), Is.False);
    }

    [Test]
    public void AnOfficeCrate_IsCrateAbleNotSiteOnly()
    {
        var world = NewSystem();
        var office = world.Info.ComponentDesigns["default-design-local-construction"];
        Assert.That(LocalConstructionJob.IsCrate(office), Is.True);
        Assert.That(LocalConstructionJob.IsSiteOnly(office), Is.False);
    }

    [Test]
    public void MountingAnOfficeCrate_GivesTheEmptyColonyItsOwnPoints()
    {
        var world = NewSystem();
        var planet = Planet(world);
        var colony = EmptyColony(world, planet);
        var ship = ColonyShip(world, planet);
        InRange(ship, colony);

        var office = world.Info.ComponentDesigns["default-design-local-construction"];
        long rate = office.GetAttribute<ConstructionAtb>().ConstructionPoints["installation"];
        Assert.That(rate, Is.EqualTo(25));
        var crate = new ComponentInstance(office);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));
        Assert.That(colony.GetDataBlob<CargoStorageDB>().TypeStores, Is.Empty);

        var queued = new CommandTranslator(Game).Translate(
            world.Faction, ship, new AddToConstructionQueueCommand(ship.Id, office.UniqueID, crate.ID, colony.Id));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = ship.GetDataBlob<LocalConstructionDB>().BuildQueue.Peek();
        Assert.That(job.Kind, Is.EqualTo("Mount"));
        Assert.That(job.Instance, Is.SameAs(crate));
        Assert.That(job.TargetEntityId, Is.EqualTo(colony.Id));

        var processor = new LocalConstructionProcessor();
        for (int day = 0; day < 500 && ship.GetDataBlob<LocalConstructionDB>().BuildQueue.Count > 0; day++)
            processor.ProcessEntity(ship, 0);

        Assert.That(ship.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(crate, false), Is.EqualTo(0));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().PointsPerDay, Is.EqualTo(rate));
        Assert.That(Mounted(colony).Single(), Is.SameAs(crate));
        Assert.That(colony.GetDataBlob<CargoStorageDB>().TypeStores, Is.Empty);

        var colonyView = new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<ConstructionView>();
        Assert.That(colonyView!.PointsPerDay, Is.EqualTo(rate));
        Assert.That(colonyView.IsShip, Is.False);
    }

    [Test]
    public void AssemblingASiteOnlyDesign_MountsItOnTheEmptyColony_AndSpendsTheHold()
    {
        var world = NewSystem();
        var planet = Planet(world);
        var colony = EmptyColony(world, planet);
        var ship = ColonyShip(world, planet);
        InRange(ship, colony);

        var cityHall = world.Info.ComponentDesigns["default-design-city-hall"];
        ship.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"] = new TypeStore(1_000_000_000_000);
        Assert.That(cityHall.IndustryPointCosts, Is.GreaterThan(0));
        Stock(world, ship, cityHall.ResourceCosts);

        var queued = new CommandTranslator(Game).Translate(
            world.Faction, ship, new AddToConstructionQueueCommand(ship.Id, cityHall.UniqueID, null, colony.Id));
        Assert.That(queued.Accepted, Is.True, queued.RejectionReason);

        var job = ship.GetDataBlob<LocalConstructionDB>().BuildQueue.Peek();
        Assert.That(job.Kind, Is.EqualTo("Assemble"));
        Assert.That(job.PointCost, Is.EqualTo(cityHall.IndustryPointCosts));
        Assert.That(job.TargetEntityId, Is.EqualTo(colony.Id));

        var processor = new LocalConstructionProcessor();
        for (int day = 0; day < 100_000 && ship.GetDataBlob<LocalConstructionDB>().BuildQueue.Count > 0; day++)
            processor.ProcessEntity(ship, 0);

        Assert.That(ship.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        foreach (var (resourceId, _) in cityHall.ResourceCosts)
        {
            var good = world.Info.Data.CargoGoods.GetAny(resourceId)!;
            Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(good, false), Is.EqualTo(0), resourceId);
        }
        Assert.That(Mounted(colony).Single().Design, Is.SameAs(cityHall));
    }

    [Test]
    public void AnOfficeOnTheColony_DoesNotSpendTheShipsPoints()
    {
        var world = NewSystem();
        var planet = Planet(world);
        var colony = EmptyColony(world, planet);
        var ship = ColonyShip(world, planet);
        InRange(ship, colony);

        var office = world.Info.ComponentDesigns["default-design-local-construction"];
        colony.AddComponent(office);
        long colonyRate = colony.GetDataBlob<LocalConstructionDB>().PointsPerDay;
        Assert.That(colonyRate, Is.EqualTo(25));

        var infra = world.Info.ComponentDesigns["default-design-infrastructure"];
        var colonyCargo = colony.GetDataBlob<CargoStorageDB>();
        colonyCargo.TypeStores["general-storage"] = new TypeStore(100_000);
        var colonyCrate = new ComponentInstance(infra);
        Assert.That(colonyCargo.AddCargoByUnit(colonyCrate, 1), Is.EqualTo(1));
        colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Enqueue(new LocalConstructionJob(infra, colonyCrate));

        var shipCrate = new ComponentInstance(infra);
        ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(shipCrate, 1);
        Assert.That(new CommandTranslator(Game).Translate(
            world.Faction, ship, new AddToConstructionQueueCommand(ship.Id, infra.UniqueID, shipCrate.ID, colony.Id)).Accepted, Is.True);

        new LocalConstructionProcessor().ProcessEntity(ship, 0);

        Assert.That(ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(shipCrate, false), Is.EqualTo(0));
        Assert.That(Mounted(colony).Count(part => part.Design.UniqueID == infra.UniqueID), Is.EqualTo(1));
        var colonyJob = colony.GetDataBlob<LocalConstructionDB>().BuildQueue.Single();
        Assert.That(colonyJob.Instance, Is.SameAs(colonyCrate));
        Assert.That(colonyJob.PointsAccumulated, Is.EqualTo(0));
        Assert.That(colony.GetDataBlob<CargoStorageDB>().GetUnitsStored(colonyCrate, false), Is.EqualTo(1));
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().PointsPerDay, Is.EqualTo(colonyRate));
        Assert.That(ship.GetDataBlob<LocalConstructionDB>().PointsPerDay, Is.EqualTo(5));

        new LocalConstructionProcessor().ProcessEntity(colony, 0);
        Assert.That(colony.GetDataBlob<LocalConstructionDB>().BuildQueue, Is.Empty);
        Assert.That(Mounted(colony).Count(part => part == colonyCrate), Is.EqualTo(1));
    }

    static void Stock(World world, Entity ship, IReadOnlyDictionary<string, long> recipe)
    {
        var storage = ship.GetDataBlob<CargoStorageDB>();
        foreach (var (resourceId, amount) in recipe)
        {
            if (amount <= 0)
                continue;
            world.Info.Data.Unlock(resourceId);
            var good = world.Info.Data.CargoGoods.GetAny(resourceId)!;
            Assert.That(storage.AddCargoByUnit(good, amount), Is.EqualTo(amount), resourceId);
        }
    }

    static void InRange(Entity ship, Entity colony)
    {
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        colony.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
    }

    static void OutOfRange(Entity ship, Entity colony)
    {
        ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 0;
        colony.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 0;
    }

    static List<ComponentInstance> Mounted(Entity entity)
        => entity.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.ToList();

    static Entity ColonyShip(World world, Entity planet)
        => ShipFactory.CreateShip(world.Info.ShipDesigns["default-ship-design-colony-ship"], world.Faction, planet, "Colony Ship");

    static Entity Freighter(World world, Entity planet)
        => ShipFactory.CreateShip(world.Info.ShipDesigns["default-ship-design-freighter"], world.Faction, planet, "Freighter");

    static Entity EmptyColony(World world, Entity planet)
    {
        var colony = Entity.Create(world.Faction.Id);
        world.System.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB("Empty Colony"),
            new ColonyInfoDB { PlanetEntity = planet },
            new CargoStorageDB(),
            new ComponentInstancesDB(),
            new PositionDB(new Vector3(planet.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0), planet),
        });
        colony.GetDataBlob<NameDB>().SetName(world.Faction.Id, "Empty Colony");
        return colony;
    }

    static Entity Planet(World world)
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
        return planet;
    }

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "ShipBuild-" + Guid.NewGuid().ToString("N"));
        var info = faction.GetDataBlob<FactionInfoDB>();
        PrepareDesigns(faction, info);
        var system = new StarSystem();
        system.Initialize(Game, "ShipBuild-" + Guid.NewGuid().ToString("N"), -1);
        system.SetActivityState(SystemActivityState.Stasis);
        return new World { System = system, Faction = faction, Info = info };
    }

    static void PrepareDesigns(Entity faction, FactionInfoDB info)
    {
        var blueprint = Store.Colonies["colony-earth"];
        foreach (var id in blueprint.StartingItems ?? new List<string>())
        {
            info.Data.Unlock(id);
            if (info.Data.Techs.ContainsKey(id))
                info.Data.IncrementTechLevel(id);
        }
        info.Data.Unlock("plastic-armor");

        string[] designs =
        {
            "default-design-passive-sensor-s50",
            "default-design-bridge-ship",
            "default-design-ship-construction",
            "default-design-fuel-tank-1500",
            "default-design-shuttlebay",
            "default-design-cargo-hold-5t",
            "default-design-alcubierre-2k",
            "default-design-battery-2t",
            "default-design-reactor-2t",
            "default-design-raptor",
            "default-design-infrastructure",
            "default-design-local-construction",
            "default-design-city-hall",
        };

        ComponentDesigner.StartResearched = true;
        try
        {
            foreach (var id in designs)
            {
                var component = Game.StartingGameData.ComponentDesigns[id];
                info.Data.Unlock(component.TemplateId);
                ComponentDesignFromJson.Create(faction, info.Data, component);
            }
            ShipDesignFromJson.Create(faction, info.Data, Game.StartingGameData.ShipDesigns["default-ship-design-colony-ship"]);
            ShipDesignFromJson.Create(faction, info.Data, Game.StartingGameData.ShipDesigns["default-ship-design-freighter"]);
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    sealed class World
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public FactionInfoDB Info = null!;
    }
}
