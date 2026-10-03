using System.Collections.Generic;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
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
public class AutonomousTradeTests
{
    [Test]
    public void IdleShip_WithAProfitablePair_PicksTrade()
    {
        var scene = Scene.Build(greed: 2f, withPair: true);
        AgentProcessor.RunAgentNow(scene.Ship);

        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal, Is.Not.Null);
        Assert.That(goal!.Type, Is.EqualTo(GoalType.Trade));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(goal.CargoId, Is.EqualTo("iron"));
        Assert.That(scene.Ship.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(goal));
    }

    [Test]
    public void GivenMoveTo_IsLeftAlone()
    {
        var scene = Scene.Build(greed: 2f, withPair: true);
        var move = new Goal(GoalType.MoveTo) { Status = GoalStatus.Active };
        var goals = scene.Ship.GetDataBlob<GoalsDB>();
        goals.GivenGoal = move;
        goals.ActiveGoal = null;

        AgentProcessor.RunAgentNow(scene.Ship);

        Assert.That(goals.GivenGoal, Is.SameAs(move));
        Assert.That(goals.GivenGoal!.Type, Is.EqualTo(GoalType.MoveTo));
        Assert.That(goals.ActiveGoal, Is.Null);
    }

    [Test]
    public void ColonyWithPolicy_PicksRunMarket()
    {
        var scene = Scene.Build(withColony: true, policy: true);
        AgentProcessor.RunAgentNow(scene.Colony!);

        var goal = scene.Colony!.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal, Is.Not.Null);
        Assert.That(goal!.Type, Is.EqualTo(GoalType.RunMarket));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void ColonyWithoutPolicy_StaysIdle()
    {
        var scene = Scene.Build(withColony: true, policy: false);
        AgentProcessor.RunAgentNow(scene.Colony!);

        Assert.That(scene.Colony!.GetDataBlob<GoalsDB>().ActiveGoal, Is.Null);
        Assert.That(scene.Colony.GetDataBlob<GoalsDB>().GivenGoal, Is.Null);
    }

    [Test]
    public void ShipDoesNotPickRunMarket_AndColonyDoesNotPickTrade()
    {
        var scene = Scene.Build(greed: 2f, withPair: true, withColony: true, policy: true);
        AgentProcessor.RunAgentNow(scene.Ship);
        AgentProcessor.RunAgentNow(scene.Colony!);

        Assert.That(scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.Trade));
        Assert.That(scene.Colony!.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.RunMarket));
    }

    [Test]
    public void MakeProfit_HasNoPlanner_AndGreedChoosesTheShipTask()
    {
        Assert.That(GoalRoles.RoleOf(GoalType.MakeProfit), Is.EqualTo(GoalRole.Drive));

        var forced = Scene.Build(withPair: true);
        AgentProcessor.AssignGoal(forced.Ship, new Goal(GoalType.MakeProfit));
        var failed = forced.Ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(failed.Type, Is.EqualTo(GoalType.MakeProfit));
        Assert.That(failed.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(failed.Message, Does.Contain("no planner"));

        var greedy = Scene.Build(greed: 2f, withPair: true);
        AgentProcessor.RunAgentNow(greedy.Ship);
        Assert.That(greedy.Ship.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.Trade));

        var plain = Scene.Build(withPair: false);
        AgentProcessor.RunAgentNow(plain.Ship);
        Assert.That(plain.Ship.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.Freighter));

        var unstaffed = Scene.Build(withPair: false);
        unstaffed.Ship.RemoveDataBlob<AgentDB>();
        AgentProcessor.RunAgentNow(unstaffed.Ship);
        Assert.That(unstaffed.Ship.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.Freighter));
    }

    sealed class Scene
    {
        public Entity Ship = null!;
        public Entity? Colony;

        public static Scene Build(float? greed = null, bool withPair = false, bool withColony = false, bool policy = false)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var faction = FactionFactory.CreateFaction(game, "Haulers");
            faction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");

            var system = new StarSystem();
            system.Initialize(game, "Auto", -1);
            var star = Entity.Create();
            system.AddEntity(star, new List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });

            var here = new Vector3(1.496e11, 0, 0);
            var there = new Vector3(2.28e11, 0, 0);
            if (withPair)
            {
                Market(system, faction, here, star, sell: 30, buy: 0, stock: 40);
                Market(system, faction, there, star, sell: 0, buy: 30, stock: 0);
            }

            var shipBlobs = new List<BaseDataBlob>
            {
                new ShipInfoDB(),
                new CargoStorageDB("general-storage", 100000),
                new PositionDB(here, star),
                new ActionQueueDB(),
                new GoalsDB(),
                new WarpAbilityDB { MaxSpeed = 1e9 },
                new LogiBaseDB { Capacity = 5 },
            };
            if (greed != null)
                shipBlobs.Add(new AgentDB { Greed = greed.Value });

            var ship = Entity.Create(faction.Id);
            system.AddEntity(ship, shipBlobs);

            Entity? colony = null;
            if (withColony)
            {
                var rows = new ColonyMarketPolicyDB();
                if (policy)
                {
                    rows.Rows["iron"] = new MarketPolicyRow
                    {
                        CargoId = "iron",
                        Min = 10,
                        Max = 10,
                        Ask = 10,
                        Bid = 12,
                    };
                }

                colony = Entity.Create(faction.Id);
                system.AddEntity(colony, new List<BaseDataBlob>
                {
                    new NameDB("Colony"),
                    new ColonyInfoDB(),
                    new CargoStorageDB("general-storage", 100000),
                    new LogiBaseDB { Capacity = 5 },
                    new PositionDB(there, star),
                    new ActionQueueDB(),
                    new GoalsDB(),
                    new WarpAbilityDB { MaxSpeed = 1e9 },
                    rows,
                });
            }

            return new Scene { Ship = ship, Colony = colony };
        }

        static void Market(StarSystem system, Entity owner, Vector3 at, Entity star, long sell, long buy, long stock)
        {
            var market = Entity.Create(owner.Id);
            var cargo = new CargoStorageDB("general-storage", 100000);
            system.AddEntity(market, new List<BaseDataBlob>
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
                Ask = 10,
                BuyQuantity = buy,
                Bid = 12,
            }), Is.True);
        }
    }
}
