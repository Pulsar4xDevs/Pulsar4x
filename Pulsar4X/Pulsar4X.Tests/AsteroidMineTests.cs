using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class AsteroidMineTests
{
    static readonly Game Game = InitializeGame();
    StarSystem _sys = null!;
    DateTime _epoch;

    static Game InitializeGame()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 2, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        game.Settings.StrictNewtonion = true;
        return game;
    }

    [SetUp]
    public void SetUp()
    {
        foreach (var sys in Game.Systems)
            sys.SetActivityState(SystemActivityState.Stasis);
        _sys = new StarSystem();
        _sys.Initialize(Game, "Mine-" + Guid.NewGuid().ToString("N"), -1);
        _sys.IncrementExternalObserver(priority: true);
        _epoch = _sys.StarSysDateTime;
    }

    [Test]
    public void Deplete_ZeroHalfOriginal_DoesNotDivide()
    {
        var deposit = new MineralDeposit
        {
            Amount = new Masked<long>(10, AccessLevel.Full),
            HalfOriginalAmount = 0,
            Accessibility = 1,
        };

        MiningHelper.Deplete(deposit, 4);

        Assert.That(deposit.Amount.Actual, Is.EqualTo(6));
        Assert.That(deposit.Accessibility, Is.EqualTo(0.1));
    }

    [Test]
    public void SurveyedAsteroid_GetsAMineOrder_OtherBodiesDoNot()
    {
        var faction = Faction();
        var star = TestingUtilities.BasicSol(_sys);
        var rock = Body(faction, star, "Rock", BodyType.Asteroid, new Vector3(2e11, 0, 0), surveyed: false);
        var moon = Body(faction, star, "Moon", BodyType.Moon, new Vector3(3e11, 0, 0), surveyed: true);
        var mars = Body(faction, star, "Mars", BodyType.Terrestrial, new Vector3(4e11, 0, 0), surveyed: true);
        Deposit(faction, rock, 50_000);
        Deposit(faction, moon, 50_000);
        Deposit(faction, mars, 50_000);
        var ship = Miner(star, rock.GetDataBlob<PositionDB>().AbsolutePosition, faction);
        Park(ship, rock);

        var plan = new MineAsteroidsPlan().Plan(ship, new Goal(GoalType.MineAsteroids), _epoch);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed), plan.Message);
        Assert.That(plan.Actions.OfType<AsteroidMineOrder>(), Is.Empty);

        rock.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[faction.Id] = 0;
        var mining = new MineAsteroidsPlan().Plan(ship, new Goal(GoalType.MineAsteroids), _epoch);
        Assert.That(mining.Status, Is.EqualTo(GoalStatus.Active), mining.Message);
        Assert.That(mining.Actions.OfType<AsteroidMineOrder>().Single().Target.Id, Is.EqualTo(rock.Id));
    }

    [Test, Timeout(20000)]
    public void ParkedOneDay_MovesOre_AndKeepsTheClockRunning()
    {
        var faction = Faction();
        var star = TestingUtilities.BasicSol(_sys);
        var rock = Body(faction, star, "Rock", BodyType.Asteroid, new Vector3(2e11, 0, 0), surveyed: true);
        var deposit = Deposit(faction, rock, 50_000);
        var ship = Miner(star, rock.GetDataBlob<PositionDB>().AbsolutePosition, faction);
        Park(ship, rock);
        var iron = Iron(faction);

        var order = new AsteroidMineOrder(ship, rock);
        order.Execute(_epoch);
        order.Execute(_epoch + TimeSpan.FromDays(1));

        Assert.That(Stored(ship, iron), Is.EqualTo(100));
        Assert.That(deposit.Amount.Actual, Is.EqualTo(49_900));

        var goal = new Goal(GoalType.MineAsteroids) { TargetEntityID = rock.Id };
        var plan = new MineAsteroidsPlan().Plan(ship, goal, _epoch);
        Assert.That(plan.Actions.OfType<WarpMoveAction>(), Is.Empty, plan.Message);
        Assert.That(plan.Actions.OfType<AsteroidMineOrder>(), Is.Not.Empty);

        AgentProcessor.AssignGoal(ship, goal);
        Assert.That(goal.Status, Is.Not.EqualTo(GoalStatus.Failed), goal.Message);

        var wall = Stopwatch.StartNew();
        _sys.ManagerSubpulses.ProcessSystem(_epoch + TimeSpan.FromDays(2));
        Assert.That(wall.ElapsedMilliseconds, Is.LessThan(15000), "2-day ProcessSystem hung");
        Assert.That(Stored(ship, iron), Is.GreaterThan(0));
    }

    [Test]
    public void FullHold_ReturnsToTheOwnedColonyCloserInTime()
    {
        var faction = Faction();
        var star = TestingUtilities.BasicSol(_sys);
        var home = Body(faction, star, "Home", BodyType.Terrestrial, new Vector3(1.5e11, 0, 0), surveyed: true);
        var rock = Body(faction, star, "Rock", BodyType.Asteroid, new Vector3(3e11, 0, 0), surveyed: true);
        Deposit(faction, rock, 50_000);
        var ship = Miner(home, home.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(200_000, 0, 0), faction);
        FillHold(ship, faction);

        var near = Colony(faction, star, ship.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(1e9, 0, 0), "general-storage", 1e6);
        var far = Colony(faction, star, ship.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(1e12, 0, 0), "general-storage", 1e6);

        var goal = new Goal(GoalType.MineAsteroids) { TargetEntityID = rock.Id };
        var plan = new MineAsteroidsPlan().Plan(ship, goal, _epoch);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(goal.DestEntityId, Is.EqualTo(near.Id));
        Assert.That(goal.DestEntityId, Is.Not.EqualTo(far.Id));
        Assert.That(plan.Actions.OfType<AsteroidMineOrder>(), Is.Empty);
        Assert.That(plan.Actions.OfType<WarpMoveAction>().Single().TargetEntityGuid, Is.EqualTo(PlanetId(near)));
    }

    [Test]
    public void OwnedColonyBeatsACloserFriendlyBuyer()
    {
        var faction = Faction();
        var friend = Faction();
        Befriend(faction, friend);
        var star = TestingUtilities.BasicSol(_sys);
        var home = Body(faction, star, "Home", BodyType.Terrestrial, new Vector3(1.5e11, 0, 0), surveyed: true);
        var ship = Miner(home, home.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(200_000, 0, 0), faction);
        var shipAt = ship.GetDataBlob<PositionDB>().AbsolutePosition;
        FillHold(ship, faction);

        var buyer = Colony(friend, star, shipAt + new Vector3(1e8, 0, 0), "general-storage", 1e6);
        Buyer(buyer);
        var owned = Colony(faction, star, shipAt + new Vector3(1e10, 0, 0), "general-storage", 1e6);

        var goal = new Goal(GoalType.MineAsteroids);
        var plan = new MineAsteroidsPlan().Plan(ship, goal, _epoch);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(goal.DestEntityId, Is.EqualTo(owned.Id));
        Assert.That(goal.DestEntityId, Is.Not.EqualTo(buyer.Id));
    }

    [Test]
    public void NoOwnedStorage_SellsTowardTheFriendlyBuyer()
    {
        var faction = Faction();
        var friend = Faction();
        Befriend(faction, friend);
        var star = TestingUtilities.BasicSol(_sys);
        var home = Body(faction, star, "Home", BodyType.Terrestrial, new Vector3(1.5e11, 0, 0), surveyed: true);
        var ship = Miner(home, home.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(200_000, 0, 0), faction);
        var shipAt = ship.GetDataBlob<PositionDB>().AbsolutePosition;
        FillHold(ship, faction);

        Colony(faction, star, shipAt + new Vector3(1e8, 0, 0), "fuel-storage", 1e6);
        var buyer = Colony(friend, star, shipAt + new Vector3(1e10, 0, 0), "general-storage", 1e6);
        Buyer(buyer);

        var goal = new Goal(GoalType.MineAsteroids);
        var plan = new MineAsteroidsPlan().Plan(ship, goal, _epoch);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(goal.DestEntityId, Is.EqualTo(buyer.Id));
        Assert.That(plan.Actions.OfType<MarketExchangeAction>(), Is.Empty);
        Assert.That(plan.Actions.OfType<WarpMoveAction>().Single().TargetEntityGuid, Is.EqualTo(PlanetId(buyer)));
    }

    [Test]
    public void NoBuyer_StaysActiveAndKeepsTheCargo()
    {
        var faction = Faction();
        var star = TestingUtilities.BasicSol(_sys);
        var home = Body(faction, star, "Home", BodyType.Terrestrial, new Vector3(1.5e11, 0, 0), surveyed: true);
        var ship = Miner(home, home.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(200_000, 0, 0), faction);
        FillHold(ship, faction);
        long before = Stored(ship, Iron(faction));
        Colony(faction, star, ship.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(1e9, 0, 0), "fuel-storage", 10);

        var goal = new Goal(GoalType.MineAsteroids);
        AgentProcessor.AssignGoal(ship, goal);

        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active), goal.Message);
        Assert.That(goal.Message, Does.Contain("no place"));
        Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList, Has.Some.InstanceOf<UnloadWaitOrder>());
        Assert.That(Stored(ship, Iron(faction)), Is.EqualTo(before));
    }

    [Test]
    public void SecondShip_DoesNotTakeTheFirstShipsRock()
    {
        var faction = Faction();
        var star = TestingUtilities.BasicSol(_sys);
        var home = Body(faction, star, "Home", BodyType.Terrestrial, new Vector3(1.5e11, 0, 0), surveyed: true);
        var rockA = Body(faction, star, "A", BodyType.Asteroid, new Vector3(2e11, 0, 0), surveyed: true);
        var rockB = Body(faction, star, "B", BodyType.Asteroid, new Vector3(4e11, 0, 0), surveyed: true);
        Deposit(faction, rockA, 50_000);
        Deposit(faction, rockB, 50_000);
        var first = Miner(home, home.GetDataBlob<PositionDB>().AbsolutePosition, faction, "First");
        var second = Miner(home, rockB.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(1e8, 0, 0), faction, "Second");
        var taken = new Goal(GoalType.MineAsteroids)
        {
            TargetEntityID = rockA.Id,
            Status = GoalStatus.Active,
        };
        first.SetDataBlob(new GoalsDB { ActiveGoal = taken, GivenGoal = taken });

        var goal = new Goal(GoalType.MineAsteroids);
        var plan = new MineAsteroidsPlan().Plan(second, goal, _epoch);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(goal.TargetEntityID, Is.EqualTo(rockB.Id));
        Assert.That(plan.Actions.OfType<AsteroidMineOrder>().Single().Target.Id, Is.EqualTo(rockB.Id));
    }

    [Test]
    public void Fleet_ParcelsOneRockPerShip()
    {
        var faction = Faction();
        var star = TestingUtilities.BasicSol(_sys);
        var rockA = Body(faction, star, "A", BodyType.Asteroid, new Vector3(2e11, 0, 0), surveyed: true);
        var rockB = Body(faction, star, "B", BodyType.Asteroid, new Vector3(5e11, 0, 0), surveyed: true);
        Deposit(faction, rockA, 50_000);
        Deposit(faction, rockB, 50_000);
        var shipA = Miner(star, rockA.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(1e8, 0, 0), faction, "A");
        var shipB = Miner(star, rockB.GetDataBlob<PositionDB>().AbsolutePosition + new Vector3(1e8, 0, 0), faction, "B");
        var fleet = FleetFactory.Create(_sys, faction.Id, "Miners " + Guid.NewGuid().ToString("N"));
        fleet.GetDataBlob<FleetDB>().AddChild(shipA);
        fleet.GetDataBlob<FleetDB>().AddChild(shipB);

        var plan = new MineAsteroidsPlan().Plan(fleet, new Goal(GoalType.MineAsteroids), _epoch);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var targets = plan.SubGoals.Select(pair => pair.Goal.TargetEntityID).ToList();
        Assert.That(targets, Is.EquivalentTo(new[] { rockA.Id, rockB.Id }));
    }

    [Test]
    public void Command_RequiresACaptain()
    {
        var faction = Faction();
        var star = TestingUtilities.BasicSol(_sys);
        var ship = Miner(star, new Vector3(1.5e11, 0, 0), faction);
        var translator = new CommandTranslator(Game);

        var blocked = translator.Translate(faction, ship, new MineAsteroidsCommand(ship.Id));
        Assert.That(blocked.Accepted, Is.False);
        Assert.That(blocked.RejectionReason, Does.Contain("captain"));

        var captain = CommanderFactory.Create(_sys, faction.Id, CommanderFactory.CreateShipCaptain(Game));
        ship.GetDataBlob<ShipInfoDB>().CommanderID = captain.Id;
        var allowed = translator.Translate(faction, ship, new MineAsteroidsCommand(ship.Id));
        Assert.That(allowed.Accepted, Is.True, allowed.RejectionReason);
        Assert.That(ship.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.MineAsteroids));
    }

    [Test]
    public void ModernTechnology_DoesNotUnlockTheMiner()
    {
        var faction = Faction();
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock("tech-modern-technology");
        data.IncrementTechLevel("tech-modern-technology");

        Assert.That(data.ComponentTemplates.ContainsKey("asteroid-miner"), Is.False);
        Assert.That(data.LockedComponentTemplates.ContainsKey("asteroid-miner"), Is.True);
        Assert.That(data.Techs["tech-asteroid-mining"].Level, Is.EqualTo(0));
    }

    [Test, Timeout(180000)]
    public void EarthStart_HasTheMiningShipAndTheTech()
    {
        var store = Game.StartingGameData;
        StarSystemFactory.LoadFromBlueprint(Game, store.Systems["system-sol"]);
        var sol = Game.Systems.Single(s => s.ID == "system-sol");
        sol.SetActivityState(SystemActivityState.Stasis);

        var player = FactionFactory.CreateBasicFaction(Game, "UEC-" + Guid.NewGuid().ToString("N"), "UEC", 0);
        player.FactionOwnerID = player.Id;
        var species = SpeciesFactory.CreateFromBlueprint(sol, store.Species["species-human"]);
        species.FactionOwnerID = player.Id;
        var earth = NameLookup.GetFirstEntityWithName(sol, "Earth");
        ColonyFactory.CreateFromBlueprint(Game, player, species, sol, earth, store.Colonies["colony-earth"]);

        var data = player.GetDataBlob<FactionInfoDB>().Data;
        var info = player.GetDataBlob<FactionInfoDB>();
        Assert.That(data.Techs["tech-asteroid-mining"].Level, Is.EqualTo(1));
        Assert.That(data.ComponentTemplates.ContainsKey("asteroid-miner"), Is.True);
        Assert.That(info.ShipDesigns.ContainsKey("default-ship-design-miner"), Is.True);

        var prospector = sol.GetAllEntitiesWithDataBlob<ShipInfoDB>()
            .Single(ship => ship.GetDataBlob<NameDB>().GetName(player.Id) == "Prospector I");
        Assert.That(prospector.HasDataBlob<AsteroidMineAbilityDB>(), Is.True);
        Assert.That(prospector.GetDataBlob<AsteroidMineAbilityDB>().UnitsPerDay, Is.EqualTo(100));
    }

    Entity Faction()
    {
        return FactionFactory.CreateFaction(Game, "mine-" + Guid.NewGuid().ToString("N"));
    }

    static void Befriend(Entity a, Entity b)
    {
        a.GetDataBlob<FactionInfoDB>().Stances[b.Id] = FactionStance.Friendly;
        b.GetDataBlob<FactionInfoDB>().Stances[a.Id] = FactionStance.Friendly;
    }

    static Mineral Iron(Entity faction)
    {
        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock("iron");
        return data.CargoGoods.GetMineral("iron");
    }

    MineralDeposit Deposit(Entity faction, Entity body, long amount)
    {
        var iron = Iron(faction);
        var minerals = body.GetDataBlob<MineralsDB>();
        var deposit = new MineralDeposit
        {
            Amount = new Masked<long>(amount, AccessLevel.Full),
            HalfOriginalAmount = amount / 2,
            Accessibility = 1,
        };
        minerals.Minerals[iron.ID] = deposit;
        return deposit;
    }

    Entity Body(Entity faction, Entity parent, string name, BodyType kind, Vector3 absolute, bool surveyed)
    {
        var entity = Entity.Create();
        var pos = new PositionDB();
        _sys.AddEntity(entity, new BaseDataBlob[]
        {
            pos,
            MassVolumeDB.NewFromMassAndRadius_m(1e18, 100_000),
            new NameDB(name),
            new GeoSurveyableDB { PointsRequired = 100 },
            new MineralsDB(),
        });
        entity.SetDataBlob(new SystemBodyInfoDB { BodyType = kind });
        pos.SetParent(parent);
        pos.AbsolutePosition = absolute;
        pos.MoveType = PositionDB.MoveTypes.None;
        // The star from BasicSol owns the root orbit. A ship cannot orbit a body that has none:
        // SetDataBlob is async void, and the missing parent OrbitDB crashes the test host.
        entity.SetDataBlob(OrbitDB.FromPosition(parent, entity, _epoch));
        if (surveyed)
            entity.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[faction.Id] = 0;
        return entity;
    }

    Entity Miner(Entity parent, Vector3 absolute, Entity faction, string name = "Miner")
    {
        var ship = MakeFueledWarpShip(parent, absolute, faction, name + Guid.NewGuid().ToString("N"));
        ship.SetDataBlob(new AsteroidMineAbilityDB { UnitsPerDay = 100 });
        ship.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"] = new TypeStore(1e6);
        return ship;
    }

    void FillHold(Entity ship, Entity faction)
    {
        var iron = Iron(faction);
        var cargo = ship.GetDataBlob<CargoStorageDB>();
        cargo.TypeStores["general-storage"] = new TypeStore(iron.VolumePerUnit * 10);
        Assert.That(cargo.AddCargoByUnit(iron, 10), Is.EqualTo(10));
        Assert.That(CargoMath.GetFreeUnitSpace(cargo, iron), Is.EqualTo(0));
    }

    static long Stored(Entity ship, Mineral iron)
    {
        var cargo = ship.GetDataBlob<CargoStorageDB>();
        if (!cargo.TypeStores.TryGetValue(iron.CargoTypeID, out var store))
            return 0;
        return store.CurrentStoreInUnits.TryGetValue(iron.ID, out var units) ? units : 0;
    }

    Entity Colony(Entity owner, Entity star, Vector3 at, string cargoType, double volume)
    {
        // A colony is not a place. Move planning follows ColonyInfoDB.PlanetEntity.
        var planet = Body(owner, star, "World", BodyType.Terrestrial, at, surveyed: false);
        var colony = Entity.Create(owner.Id);
        _sys.AddEntity(colony, new List<BaseDataBlob>
        {
            new CargoStorageDB(cargoType, volume),
            new ColonyInfoDB { PlanetEntity = planet },
            new LogiBaseDB { Capacity = 5 },
            new PositionDB(at, star) { MoveType = PositionDB.MoveTypes.None },
            new NameDB("Colony " + Guid.NewGuid().ToString("N")),
            new ActionQueueDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });
        colony.FactionOwnerID = owner.Id;
        return colony;
    }

    static int PlanetId(Entity colony)
    {
        return colony.GetDataBlob<ColonyInfoDB>().PlanetEntity.Id;
    }

    static void Buyer(Entity colony)
    {
        Assert.That(MarketBook.SetListing(colony, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 0,
            Ask = 0,
            BuyQuantity = 1000,
            Bid = 12,
        }), Is.True);
    }

    static void Park(Entity ship, Entity rock)
    {
        if (ship.HasDataBlob<OrbitDB>())
            ship.RemoveDataBlob<OrbitDB>();
        // The body was given an orbit so a ship can parent to it. Leave that orbit on and
        // the two-day pulse carries the rock away from a ship that is sitting still.
        if (rock.HasDataBlob<OrbitDB>())
            rock.RemoveDataBlob<OrbitDB>();
        var pos = ship.GetDataBlob<PositionDB>();
        pos.AbsolutePosition = rock.GetDataBlob<PositionDB>().AbsolutePosition;
        pos.MoveType = PositionDB.MoveTypes.None;
        rock.GetDataBlob<PositionDB>().MoveType = PositionDB.MoveTypes.None;
    }

    void Survey(Entity faction, Entity body)
    {
        body.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[faction.Id] = 0;
    }

    Entity MakeFueledWarpShip(Entity parent, Vector3 abs, Entity faction, string name)
    {
        var pos = new PositionDB { AbsolutePosition = abs };
        var warp = new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" };
        var energy = new EnergyGenAbilityDB(_epoch)
        {
            EnergyType = new TestEnergyType(),
            EnergyStored = { ["electricity"] = 1e12 },
            EnergyStoreMax = { ["electricity"] = 1e12 },
        };
        var ship = Entity.Create();
        _sys.AddEntity(ship, new BaseDataBlob[]
        {
            pos,
            MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            warp,
            energy,
            new ActionQueueDB(),
            new ShipInfoDB(),
            new NameDB(name, faction.Id, name),
        });
        pos.SetParent(parent);
        ship.FactionOwnerID = faction.Id;
        ship.SetDataBlob(OrbitDB.FromPosition(parent, ship, _epoch));
        OrbitProcessor.ProcessEntity(ship, _epoch);

        var thrust = new NewtonThrustAbilityDB("test-fuel")
        {
            ThrustInNewtons = 1e9,
            ExhaustVelocity = 10_000,
            FuelBurnRate = 1e9 / 10_000,
        };
        double wet = ship.GetDataBlob<MassVolumeDB>().MassTotal;
        double dry = wet / Math.Exp(20_000 / thrust.ExhaustVelocity);
        thrust.SetFuel(Math.Max(wet - dry, 1), wet);
        ship.SetDataBlob(thrust);

        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock("methalox");
        var fuel = data.CargoGoods.GetAny("methalox");
        thrust.FuelType = fuel.UniqueID;
        var storage = new CargoStorageDB(fuel.CargoTypeID, 1e12);
        storage.AddCargoByUnit(fuel, 1_000_000);
        ship.SetDataBlob(storage);
        return ship;
    }

    sealed class TestEnergyType : ICargoable
    {
        public int ID => 0;
        public string UniqueID => "electricity";
        public string CargoTypeID => "energy";
        public string Name => "electricity";
        public long MassPerUnit => 0;
        public double VolumePerUnit => 0;
    }
}
