using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class FleetLogisticsTests
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
    public void TwoTradePairs_GoToDifferentShips()
    {
        var scene = TradeScene(secondPair: true);

        var plan = new FleetTradePlan().Plan(scene.Fleet, scene.Goal, scene.Fleet.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var trades = plan.SubGoals.Where(item => item.Goal.Type == GoalType.Trade).ToList();
        Assert.That(trades, Has.Count.EqualTo(2));
        var pairs = trades.Select(item => (item.Goal.SourceEntityId, item.Goal.DestEntityId)).ToList();
        Assert.That(pairs[0], Is.Not.EqualTo(pairs[1]));
    }

    [Test]
    public void OneTradePair_LeavesTheSecondShipFree()
    {
        var scene = TradeScene(secondPair: false);

        var plan = new FleetTradePlan().Plan(scene.Fleet, scene.Goal, scene.Fleet.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var trades = plan.SubGoals.Where(item => item.Goal.Type == GoalType.Trade).ToList();
        Assert.That(trades, Has.Count.EqualTo(1));
    }

    [Test]
    public void Contract_Splits80AcrossHoldsOf30And50_AndCapsTheBuy()
    {
        var scene = ContractScene();

        var plan = new FleetFreighterPlan().Plan(scene.Fleet, scene.Goal, scene.Fleet.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var hauls = plan.SubGoals.Where(item => item.Goal.Type == GoalType.Freighter).ToList();
        Assert.That(hauls.Select(item => item.Goal.UnitShare).OrderBy(share => share), Is.EqualTo(new long[] { 30, 50 }));
        Assert.That(hauls.All(item => item.Goal.SourceEntityId == scene.Source.Id && item.Goal.DestEntityId == scene.Dest.Id));

        var tanker = plan.SubGoals.Single(item => item.Goal.Type == GoalType.MoveTo);
        Assert.That(tanker.Sub.Id, Is.EqualTo(scene.Tanker.Id));
        Assert.That(tanker.Goal.TargetEntityID, Is.EqualTo(scene.DestPlanet.Id));

        var small = hauls.Single(item => item.Goal.UnitShare == 30);
        var leg = new FreighterPlan().Plan(small.Sub, small.Goal, small.Sub.StarSysDateTime);
        Assert.That(leg.Status, Is.EqualTo(GoalStatus.Active), leg.Message);
        var buy = leg.Actions.OfType<MarketExchangeAction>().Single();
        Assert.That(buy.Side, Is.EqualTo(MarketSide.BuyFromMarket));
        Assert.That(buy.RequestedUnits, Is.EqualTo(30));
    }

    [Test]
    public void LiveMoveTo_IsLeftAlone()
    {
        var scene = FreightPair();
        scene.Busy.SetDataBlob(new GoalsDB
        {
            ActiveGoal = new Goal(GoalType.MoveTo) { Status = GoalStatus.Active },
        });

        var plan = new FleetFreighterPlan().Plan(scene.Fleet, scene.Goal, scene.Fleet.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.SubGoals.Select(item => item.Sub.Id), Does.Not.Contain(scene.Busy.Id));
        Assert.That(plan.SubGoals.Any(item => item.Sub.Id == scene.Free.Id && item.Goal.Type == GoalType.Freighter));
    }

    [Test]
    public void Span_BodySkipsTheMoon_WellTakesIt_SystemReachesTheOtherPlanet()
    {
        var scene = SpanScene();

        AttachBridge(scene.Ship, AdminLevel.Ship);
        var body = new FleetFreighterPlan().Plan(scene.Fleet, scene.Goal, scene.Fleet.StarSysDateTime);
        Assert.That(HaulDest(body), Is.EqualTo(scene.EarthBuy.Id), body.Message);

        AttachBridge(scene.Ship, AdminLevel.Planet);
        var well = new FleetFreighterPlan().Plan(scene.Fleet, scene.Goal, scene.Fleet.StarSysDateTime);
        Assert.That(HaulDest(well), Is.EqualTo(scene.MoonBuy.Id), well.Message);

        AttachBridge(scene.Ship, AdminLevel.System);
        var system = new FleetFreighterPlan().Plan(scene.Fleet, scene.Goal, scene.Fleet.StarSysDateTime);
        Assert.That(HaulDest(system), Is.EqualTo(scene.MarsBuy.Id), system.Message);
    }

    [Test]
    public void HandedChild_ListingGone_Completes_AndAFleetStillRejectsShipFreighter()
    {
        var scene = FreightPair();
        var handed = new Goal(GoalType.Freighter)
        {
            ParentGoalId = scene.Goal.Id,
            CargoId = "iron",
            SourceEntityId = 900_001,
            DestEntityId = 900_002,
            UnitShare = 10,
        };

        var closed = new FreighterPlan().Plan(scene.Free, handed, scene.Free.StarSysDateTime);

        Assert.That(closed.Status, Is.EqualTo(GoalStatus.Completed));
        Assert.That(closed.Message, Is.EqualTo("share closed"));

        var delivered = new Goal(GoalType.Freighter)
        {
            ParentGoalId = scene.Goal.Id,
            CargoId = "iron",
            SourceEntityId = scene.Source.Id,
            DestEntityId = scene.Buy.Id,
            UnitShare = 10,
            Message = MarketRun.ShareInFlight,
        };
        var done = new FreighterPlan().Plan(scene.Free, delivered, scene.Free.StarSysDateTime);
        Assert.That(done.Status, Is.EqualTo(GoalStatus.Completed));
        Assert.That(done.Message, Is.EqualTo("share delivered"));

        var fleetOnly = Entity.Create(scene.Faction.Id);
        scene.System.AddEntity(fleetOnly, new List<BaseDataBlob>
        {
            new FleetDB(),
            new CargoStorageDB("general-storage", 1000),
            new ActionQueueDB(),
        });
        var rejected = new CommandTranslator(Game).Translate(
            scene.Faction, fleetOnly, new FreighterCommand(fleetOnly.Id));
        Assert.That(rejected.RejectionReason, Is.EqualTo("Freighter is one ship"));
    }

    static int HaulDest(PlanResult plan)
    {
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        return plan.SubGoals.Single(item => item.Goal.Type == GoalType.Freighter).Goal.DestEntityId;
    }

    static void AttachBridge(Entity ship, AdminLevel level)
    {
        var admin = new AdminSpaceDB();
        admin.CommanderSeats.Add(new AdminSpaceAbilityState(level, "command-bridge"));
        ship.SetDataBlob(admin);
    }

    static TradeWorld TradeScene(bool secondPair)
    {
        var world = NewSystem();
        var anchor = Body(world.System, world.Star, "Anchor", new Vector3(1.5e11, 0, 0));
        var sellA = Office(world, anchor, new Vector3(0, 0, 0), sell: 20, ask: 10, buy: 0, bid: 0, stock: 20, colony: false);
        var buyA = Office(world, anchor, new Vector3(1e9, 0, 0), sell: 0, ask: 0, buy: 20, bid: 40, stock: 0, colony: false);
        if (secondPair)
        {
            Office(world, anchor, new Vector3(0, 1e9, 0), sell: 20, ask: 10, buy: 0, bid: 0, stock: 20, colony: false);
            Office(world, anchor, new Vector3(0, -1e9, 0), sell: 0, ask: 0, buy: 20, bid: 25, stock: 0, colony: false);
        }

        var fleet = Fleet(world, out var ships, holds: new[] { 100, 100 });
        return new TradeWorld
        {
            Fleet = fleet,
            Goal = new Goal(GoalType.FleetTrade) { TargetEntityID = anchor.Id },
            FirstSell = sellA,
            FirstBuy = buyA,
            Ships = ships,
        };
    }

    static ContractWorld ContractScene()
    {
        var world = NewSystem();
        var earth = Body(world.System, world.Star, "Earth", new Vector3(1.5e11, 0, 0));
        var mars = Body(world.System, world.Star, "Mars", new Vector3(2.28e11, 0, 0));
        var source = Office(world, earth, new Vector3(1e7, 0, 0), sell: 80, ask: 10, buy: 0, bid: 0, stock: 80, colony: true);
        var dest = Office(world, mars, new Vector3(1e7, 0, 0), sell: 0, ask: 0, buy: 80, bid: 12, stock: 0, colony: true);
        var fleet = Fleet(world, out var ships, holds: new[] { 30, 50 }, at: new Vector3(1e7, 0, 0), parent: earth);
        var tanker = Ship(world, 100, new Vector3(1.5e11, 1e8, 0), earth, tanker: true);
        fleet.GetDataBlob<FleetDB>().AddChild(tanker);
        return new ContractWorld
        {
            Fleet = fleet,
            Goal = new Goal(GoalType.FleetFreighter)
            {
                CargoId = "iron",
                SourceEntityId = source.Id,
                DestEntityId = dest.Id,
                TargetEntityID = mars.Id,
            },
            Source = source,
            Dest = dest,
            DestPlanet = mars,
            Tanker = tanker,
            Ships = ships,
        };
    }

    static FreightWorld FreightPair()
    {
        var world = NewSystem();
        var earth = Body(world.System, world.Star, "Earth", new Vector3(1.5e11, 0, 0));
        var source = Office(world, earth, new Vector3(0, 0, 0), sell: 40, ask: 10, buy: 0, bid: 0, stock: 40, colony: true);
        var buy = Office(world, earth, new Vector3(1e8, 0, 0), sell: 0, ask: 0, buy: 40, bid: 12, stock: 0, colony: true);
        var fleet = Fleet(world, out var ships, holds: new[] { 40, 40 });
        return new FreightWorld
        {
            Game = world.Game,
            System = world.System,
            Faction = world.Faction,
            Fleet = fleet,
            Goal = new Goal(GoalType.FleetFreighter) { TargetEntityID = earth.Id },
            Free = ships[0],
            Busy = ships[1],
            Source = source,
            Buy = buy,
        };
    }

    static SpanWorld SpanScene()
    {
        var world = NewSystem();
        var earth = Body(world.System, world.Star, "Earth", new Vector3(1.5e11, 0, 0));
        var moon = Body(world.System, earth, "Moon", new Vector3(3e8, 0, 0));
        var mars = Body(world.System, world.Star, "Mars", new Vector3(2.28e11, 0, 0));
        Office(world, earth, new Vector3(0, 0, 0), sell: 300, ask: 10, buy: 0, bid: 0, stock: 300, colony: true);
        var earthBuy = Office(world, earth, new Vector3(1e7, 0, 0), sell: 0, ask: 0, buy: 40, bid: 12, stock: 0, colony: true);
        var moonBuy = Office(world, moon, new Vector3(0, 0, 0), sell: 0, ask: 0, buy: 100, bid: 12, stock: 0, colony: true);
        var marsBuy = Office(world, mars, new Vector3(0, 0, 0), sell: 0, ask: 0, buy: 250, bid: 12, stock: 0, colony: true);

        var other = FactionFactory.CreateFaction(Game, "Partners-" + Guid.NewGuid().ToString("N"));
        other.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
        world.Faction.GetDataBlob<FactionInfoDB>().Stances[other.Id] = FactionStance.Friendly;
        other.GetDataBlob<FactionInfoDB>().Stances[world.Faction.Id] = FactionStance.Friendly;
        var saved = world.Faction;
        world.Faction = other;
        Office(world, earth, new Vector3(-1e7, 0, 0), sell: 0, ask: 0, buy: 10000, bid: 50, stock: 0, colony: true);
        world.Faction = saved;

        var fleet = Fleet(world, out var ships, holds: new[] { 500 });
        return new SpanWorld
        {
            Fleet = fleet,
            Ship = ships[0],
            Goal = new Goal(GoalType.FleetFreighter) { TargetEntityID = earth.Id },
            EarthBuy = earthBuy,
            MoonBuy = moonBuy,
            MarsBuy = marsBuy,
        };
    }

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "Haulers-" + Guid.NewGuid().ToString("N"));
        faction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
        var system = new StarSystem();
        system.Initialize(Game, "FleetTrade-" + Guid.NewGuid().ToString("N"), -1);
        var star = Entity.Create();
        system.AddEntity(star, new List<BaseDataBlob>
        {
            new NameDB("Star"),
            new PositionDB(0, 0, 0),
            MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
        });
        return new World { Game = Game, System = system, Faction = faction, Star = star, Iron = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron") };
    }

    static Entity Body(StarSystem system, Entity parent, string name, Vector3 at)
    {
        var body = Entity.Create();
        var info = new SystemBodyInfoDB();
        info.LengthOfDay = TimeSpan.Zero;
        system.AddEntity(body, new List<BaseDataBlob>
        {
            new NameDB(name),
            info,
            new PositionDB(at, parent),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });
        return body;
    }

    static Entity Office(World world, Entity parent, Vector3 at, long sell, decimal ask, long buy, decimal bid, long stock, bool colony)
    {
        var office = Entity.Create(world.Faction.Id);
        var cargo = new CargoStorageDB("general-storage", 100000);
        var blobs = new List<BaseDataBlob>
        {
            new NameDB("Office"),
            cargo,
            new LogiBaseDB { Capacity = 5 },
            new PositionDB(at, parent),
            MassVolumeDB.NewFromMassAndRadius_m(1e20, 1e5),
            new ActionQueueDB(),
        };
        if (colony)
        {
            var info = new ColonyInfoDB();
            info.PlanetEntity = parent;
            blobs.Add(info);
        }
        world.System.AddEntity(office, blobs);
        if (stock > 0)
            Assert.That(cargo.AddCargoByUnit(world.Iron, stock), Is.EqualTo(stock));
        Assert.That(MarketBook.SetListing(office, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = sell,
            Ask = ask,
            BuyQuantity = buy,
            Bid = bid,
        }), Is.True);
        return office;
    }

    static Entity Fleet(World world, out List<Entity> ships, int[] holds, Vector3 at = default, Entity parent = null)
    {
        ships = new List<Entity>();
        var fleet = FleetFactory.Create(world.System, world.Faction.Id, "Convoy");
        var fleetDB = fleet.GetDataBlob<FleetDB>();
        for (int i = 0; i < holds.Length; i++)
        {
            var ship = Ship(world, holds[i], at + new Vector3(i * 1000, 0, 0), parent ?? world.Star, tanker: false);
            fleetDB.AddChild(ship);
            ships.Add(ship);
        }
        fleetDB.FlagShipID = ships[0].Id;
        return fleet;
    }

    static Entity Ship(World world, int units, Vector3 at, Entity parent, bool tanker)
    {
        double hold = world.Iron.VolumePerUnit * (units + 0.5);
        var ship = Entity.Create(world.Faction.Id);
        var info = new ShipInfoDB();
        if (tanker)
            info.Tanker = true;
        world.System.AddEntity(ship, new List<BaseDataBlob>
        {
            info,
            new CargoStorageDB("general-storage", hold),
            new PositionDB(at, parent),
            new ActionQueueDB(),
            new WarpAbilityDB { MaxSpeed = 1e9 },
            new NewtonThrustAbilityDB("iron"),
            new NewtonMoveDB(parent, Vector3.Zero),
            MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
        });
        Assert.That(ship.GetDataBlob<CargoStorageDB>().GetFreeUnitSpace(world.Iron), Is.EqualTo(units));
        return ship;
    }

    sealed class World
    {
        public Game Game;
        public StarSystem System;
        public Entity Faction;
        public Entity Star;
        public ICargoable Iron;
    }

    sealed class TradeWorld
    {
        public Entity Fleet;
        public Goal Goal;
        public Entity FirstSell;
        public Entity FirstBuy;
        public List<Entity> Ships;
    }

    sealed class ContractWorld
    {
        public Entity Fleet;
        public Goal Goal;
        public Entity Source;
        public Entity Dest;
        public Entity DestPlanet;
        public Entity Tanker;
        public List<Entity> Ships;
    }

    sealed class FreightWorld
    {
        public Game Game;
        public StarSystem System;
        public Entity Faction;
        public Entity Fleet;
        public Goal Goal;
        public Entity Free;
        public Entity Busy;
        public Entity Source;
        public Entity Buy;
    }

    sealed class SpanWorld
    {
        public Entity Fleet;
        public Entity Ship;
        public Goal Goal;
        public Entity EarthBuy;
        public Entity MoonBuy;
        public Entity MarsBuy;
    }
}
