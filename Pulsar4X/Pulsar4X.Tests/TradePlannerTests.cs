using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class TradePlannerTests
{
    [Test]
    public void InRangeShip_BuysAtEarth_AndTheAgentStoresTheRoute()
    {
        var scene = Scene.Build();
        var result = scene.Trade();

        Assert.That(result.Accepted, Is.True);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal, Is.Not.Null);
        Assert.That(goal!.Type, Is.EqualTo(GoalType.Trade));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(goal.CargoId, Is.EqualTo("iron"));
        Assert.That(goal.SourceEntityId, Is.EqualTo(scene.Earth.Id));
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.Mars.Id));
        Assert.That(MarketBook.Stock(scene.Ship, scene.Iron), Is.EqualTo(30));

        var queued = scene.Ship.GetDataBlob<ActionQueueDB>().ActionList;
        Assert.That(queued, Has.Count.EqualTo(1));
        Assert.That(queued[0], Is.InstanceOf<MarketExchangeAction>());
        var buy = (MarketExchangeAction)queued[0];
        Assert.That(buy.Side, Is.EqualTo(MarketSide.BuyFromMarket));
        Assert.That(buy.MarketEntityId, Is.EqualTo(scene.Earth.Id));
        Assert.That(queued[0], Is.Not.InstanceOf<WarpMoveAction>());
    }

    [Test]
    public void AfterTheBuy_TheNextPlanMovesTowardMars_AndKeepsThePair()
    {
        var scene = Scene.Build();
        Assert.That(scene.Trade().Accepted, Is.True);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!;

        var next = new TradePlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(next.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(next.Actions, Is.Not.Empty);
        Assert.That(next.Actions[0], Is.InstanceOf<WarpMoveAction>());
        Assert.That(next.Actions.OfType<MarketExchangeAction>(), Is.Empty);
        Assert.That(goal.CargoId, Is.EqualTo("iron"));
        Assert.That(goal.SourceEntityId, Is.EqualTo(scene.Earth.Id));
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.Mars.Id));
    }

    [Test]
    public void NoPositiveScore_FailsWithNoRoute()
    {
        var scene = Scene.Build(ask: 20, bid: 10);
        var result = scene.Trade();

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("no route"));
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal!.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(goal.Message, Is.EqualTo("no route"));
        Assert.That(goal.Type, Is.EqualTo(GoalType.Trade));
    }

    [Test]
    public void HostileMarket_IsNotACandidate()
    {
        var scene = Scene.Build(marsFriendly: false, bid: 1000);
        var result = scene.Trade();

        Assert.That(result.RejectionReason, Is.EqualTo("no route"));
        Assert.That(scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!.Message, Is.EqualTo("no route"));
        Assert.That(MarketBook.Stock(scene.Ship, scene.Iron), Is.EqualTo(0));
    }

    [Test]
    public void LowFuel_FailsTrade_AndDoesNotAssignRefuel()
    {
        var scene = Scene.Build(lowFuel: true);
        var result = scene.Trade();

        Assert.That(result.RejectionReason, Is.EqualTo("low on fuel"));
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal!.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(goal.Message, Is.EqualTo("low on fuel"));
        Assert.That(goal.Type, Is.EqualTo(GoalType.Trade));
        Assert.That(scene.Ship.GetDataBlob<GoalsDB>().GivenGoal!.Type, Is.EqualTo(GoalType.Trade));
    }

    [Test]
    public void Fleet_Fails()
    {
        var scene = Scene.Build();
        var fleet = Entity.Create(scene.Ship.FactionOwnerID);
        scene.System.AddEntity(fleet, new System.Collections.Generic.List<BaseDataBlob>
        {
            new FleetDB(),
            new CargoStorageDB("general-storage", 1000),
            new ActionQueueDB(),
        });

        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction, fleet, new TradeCommand(fleet.Id));

        Assert.That(result.RejectionReason, Is.EqualTo("Trade is one ship"));
        Assert.That(fleet.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.Trade));
        Assert.That(fleet.GetDataBlob<GoalsDB>().ActiveGoal!.Status, Is.EqualTo(GoalStatus.Failed));
    }

    [Test]
    public void ShipWithoutCargoStorage_IsRejected()
    {
        var scene = Scene.Build();
        var bare = Entity.Create(scene.Faction.Id);
        scene.System.AddEntity(bare, new System.Collections.Generic.List<BaseDataBlob>
        {
            new ShipInfoDB(),
            new ActionQueueDB(),
        });

        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction, bare, new TradeCommand(bare.Id));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("The ship has no cargo storage."));
        Assert.That(bare.HasDataBlob<GoalsDB>(), Is.False);
    }

    [Test]
    public void Prune_TradeNeedsCargoAndADrive()
    {
        var scene = Scene.Build();
        var goals = new GoalsDB();
        AgentProcessor.PruneImpossibleGoals(goals, scene.Ship);
        Assert.That(goals.CapabilityModifiers.ContainsKey(GoalType.Trade), Is.False);

        scene.Ship.RemoveDataBlob<WarpAbilityDB>();
        AgentProcessor.PruneImpossibleGoals(goals, scene.Ship);
        Assert.That(goals.CapabilityModifiers.ContainsKey(GoalType.Trade), Is.False);

        scene.Ship.RemoveDataBlob<NewtonThrustAbilityDB>();
        AgentProcessor.PruneImpossibleGoals(goals, scene.Ship);
        Assert.That(goals.CapabilityModifiers[GoalType.Trade], Is.EqualTo(-1f));
        Assert.That(goals.CapabilityModifiers[GoalType.Freighter], Is.EqualTo(-1f));
    }

    sealed class Scene
    {
        public Game Game = null!;
        public StarSystem System = null!;
        public Entity Faction = null!;
        public Entity Ship = null!;
        public Entity Earth = null!;
        public Entity Mars = null!;
        public ICargoable Iron = null!;

        public CommandResult Trade()
            => new CommandTranslator(Game).Translate(Faction, Ship, new TradeCommand(Ship.Id));

        public static Scene Build(decimal ask = 10, decimal bid = 12, bool marsFriendly = true, bool lowFuel = false)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var faction = FactionFactory.CreateFaction(game, "Haulers");
            faction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
            var iron = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron");

            Entity marsFaction = faction;
            if (!marsFriendly)
            {
                marsFaction = FactionFactory.CreateFaction(game, "Closed");
                marsFaction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
            }

            var system = new StarSystem();
            system.Initialize(game, "Trade", -1);
            var star = Entity.Create();
            system.AddEntity(star, new System.Collections.Generic.List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });

            var earthPos = new Vector3(1.496e11, 0, 0);
            var marsPos = new Vector3(2.28e11, 0, 0);

            var earth = Market(system, faction, earthPos, star, sell: 30, ask: ask, buy: 0, bid: 0, stock: 40);
            var mars = Market(system, marsFaction, marsPos, star, sell: 0, ask: 0, buy: 30, bid: bid, stock: 0);

            var ship = Entity.Create(faction.Id);
            var warp = new WarpAbilityDB { MaxSpeed = 1e9 };
            system.AddEntity(ship, new System.Collections.Generic.List<BaseDataBlob>
            {
                new ShipInfoDB(),
                new CargoStorageDB("general-storage", 100000),
                new PositionDB(earthPos, star),
                new ActionQueueDB(),
                warp,
                new NewtonThrustAbilityDB("iron"),
                new NewtonMoveDB(star, Vector3.Zero),
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            });
            if (lowFuel)
                ship.SetDataBlob(new AgentDB { Caution = 4f });

            return new Scene
            {
                Game = game,
                System = system,
                Faction = faction,
                Ship = ship,
                Earth = earth,
                Mars = mars,
                Iron = iron,
            };
        }

        static Entity Market(
            StarSystem system, Entity owner, Vector3 at, Entity star,
            long sell, decimal ask, long buy, decimal bid, long stock)
        {
            var market = Entity.Create(owner.Id);
            var cargo = new CargoStorageDB("general-storage", 100000);
            system.AddEntity(market, new System.Collections.Generic.List<BaseDataBlob>
            {
                cargo,
                new LogiBaseDB { Capacity = 5 },
                new PositionDB(at, star),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
                new ActionQueueDB(),
            });
            if (stock > 0)
            {
                var good = owner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron");
                Assert.That(cargo.AddCargoByUnit(good, stock), Is.EqualTo(stock));
            }
            Assert.That(MarketBook.SetListing(market, new MarketListing
            {
                CargoId = "iron",
                SellQuantity = sell,
                Ask = ask,
                BuyQuantity = buy,
                Bid = bid,
            }), Is.True);
            return market;
        }
    }
}
