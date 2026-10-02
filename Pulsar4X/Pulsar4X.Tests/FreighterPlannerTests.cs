using System;
using System.Linq;
using GameEngine.Engine.Orders;
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
public class FreighterPlannerTests
{
    [Test]
    public void OwnedPair_LoadsTheHold_AndLeavesTheLedgerAlone()
    {
        var scene = Scene.Build(sell: 500, buy: 800, capacityUnits: 200);
        var money = scene.Faction.GetDataBlob<FactionInfoDB>().Money;
        int transactions = money.GetAllTransactions().Count;
        decimal funds = money.GetCurrentFunds();

        var result = scene.Haul();

        Assert.That(result.Accepted, Is.True);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal;
        Assert.That(goal!.Type, Is.EqualTo(GoalType.Freighter));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(goal.CargoId, Is.EqualTo("iron"));
        Assert.That(goal.SourceEntityId, Is.EqualTo(scene.Source.Id));
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.Dest.Id));
        Assert.That(MarketBook.Stock(scene.Ship, scene.Iron), Is.EqualTo(200));
        Assert.That(money.GetAllTransactions().Count, Is.EqualTo(transactions));
        Assert.That(money.GetCurrentFunds(), Is.EqualTo(funds));

        var queued = scene.Ship.GetDataBlob<ActionQueueDB>().ActionList;
        Assert.That(queued, Has.Count.EqualTo(1));
        var buy = (MarketExchangeAction)queued[0];
        Assert.That(buy.Side, Is.EqualTo(MarketSide.BuyFromMarket));
        Assert.That(buy.MarketEntityId, Is.EqualTo(scene.Source.Id));
    }

    [Test]
    public void FriendlyForeignBuy_IsIgnoredWhenItIsLarger()
    {
        var scene = Scene.Build(sell: 500, buy: 100, capacityUnits: 200, foreignBuy: 10000);

        var result = scene.Haul();

        Assert.That(result.Accepted, Is.True);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.Dest.Id));
        Assert.That(goal.DestEntityId, Is.Not.EqualTo(scene.Foreign!.Id));
        Assert.That(MarketBook.Stock(scene.Ship, scene.Iron), Is.EqualTo(100));
        Assert.That(MarketBook.Stock(scene.Foreign, scene.ForeignIron!), Is.EqualTo(0));
    }

    [Test]
    public void NoOwnBuyRequest_FailsWithNoHaul()
    {
        var scene = Scene.Build(sell: 500, buy: 0, capacityUnits: 200, foreignBuy: 800);
        var result = scene.Haul();

        Assert.That(result.RejectionReason, Is.EqualTo("no haul"));
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(goal.Message, Is.EqualTo("no haul"));
        Assert.That(goal.Type, Is.EqualTo(GoalType.Freighter));
        Assert.That(MarketBook.Stock(scene.Ship, scene.Iron), Is.EqualTo(0));
    }

    [Test]
    public void UnlistedSurplus_IsNotAHaul()
    {
        var scene = Scene.Build(sell: 0, buy: 800, capacityUnits: 200, sourceStock: 1000);
        var result = scene.Haul();

        Assert.That(result.RejectionReason, Is.EqualTo("no haul"));
        Assert.That(MarketBook.Stock(scene.Ship, scene.Iron), Is.EqualTo(0));
        Assert.That(MarketBook.Stock(scene.Source, scene.Iron), Is.EqualTo(1000));
    }

    [Test]
    public void AfterTheLoad_TheNextPlanMovesTowardTheBuyer()
    {
        var scene = Scene.Build(sell: 500, buy: 800, capacityUnits: 200);
        Assert.That(scene.Haul().Accepted, Is.True);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!;

        var next = new FreighterPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(next.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(next.Actions, Is.Not.Empty);
        Assert.That(next.Actions[0], Is.InstanceOf<WarpMoveAction>());
        Assert.That(next.Actions.OfType<MarketExchangeAction>(), Is.Empty);
        Assert.That(goal.CargoId, Is.EqualTo("iron"));
        Assert.That(goal.SourceEntityId, Is.EqualTo(scene.Source.Id));
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.Dest.Id));
    }

    [Test]
    public void EqualLoads_PreferTheShorterHaul()
    {
        var scene = Scene.Build(sell: 500, buy: 800, capacityUnits: 200, farSell: 500);
        var result = scene.Haul();

        Assert.That(result.Accepted, Is.True);
        Assert.That(scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!.SourceEntityId, Is.EqualTo(scene.Source.Id));
        Assert.That(MarketBook.Stock(scene.Ship, scene.Iron), Is.EqualTo(200));
    }

    [Test]
    public void LowFuel_FailsFreighter_AndDoesNotAssignRefuel()
    {
        var scene = Scene.Build(sell: 500, buy: 800, capacityUnits: 200, lowFuel: true);
        var result = scene.Haul();

        Assert.That(result.RejectionReason, Is.EqualTo("low on fuel"));
        var goal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(goal.Type, Is.EqualTo(GoalType.Freighter));
        Assert.That(scene.Ship.GetDataBlob<GoalsDB>().GivenGoal!.Type, Is.EqualTo(GoalType.Freighter));
    }

    [Test]
    public void Fleet_Fails()
    {
        var scene = Scene.Build(sell: 500, buy: 800, capacityUnits: 200);
        var fleet = Entity.Create(scene.Ship.FactionOwnerID);
        scene.System.AddEntity(fleet, new System.Collections.Generic.List<BaseDataBlob>
        {
            new FleetDB(),
            new CargoStorageDB("general-storage", 1000),
            new ActionQueueDB(),
        });

        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction, fleet, new FreighterCommand(fleet.Id));

        Assert.That(result.RejectionReason, Is.EqualTo("Freighter is one ship"));
        Assert.That(fleet.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.Freighter));
        Assert.That(fleet.GetDataBlob<GoalsDB>().ActiveGoal!.Status, Is.EqualTo(GoalStatus.Failed));
    }

    sealed class Scene
    {
        public Game Game = null!;
        public StarSystem System = null!;
        public Entity Faction = null!;
        public Entity Ship = null!;
        public Entity Source = null!;
        public Entity Dest = null!;
        public Entity? Foreign;
        public ICargoable Iron = null!;
        public ICargoable? ForeignIron;

        public CommandResult Haul()
            => new CommandTranslator(Game).Translate(Faction, Ship, new FreighterCommand(Ship.Id));

        public static Scene Build(
            long sell, long buy, int capacityUnits,
            long foreignBuy = 0, long farSell = 0, long sourceStock = -1, bool lowFuel = false)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var faction = FactionFactory.CreateFaction(game, "Haulers");
            faction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
            var iron = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron");

            var system = new StarSystem();
            system.Initialize(game, "Freight", -1);
            var star = Entity.Create();
            system.AddEntity(star, new System.Collections.Generic.List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });

            var sourcePos = new Vector3(1.496e11, 0, 0);
            var destPos = new Vector3(2.28e11, 0, 0);
            long stock = sourceStock >= 0 ? sourceStock : sell;
            var source = Colony(system, faction, iron, sourcePos, star, sell: sell, buy: 0, stock: stock);
            var dest = Colony(system, faction, iron, destPos, star, sell: 0, buy: buy, stock: 0);

            Entity? foreign = null;
            ICargoable? foreignIron = null;
            if (foreignBuy > 0)
            {
                var other = FactionFactory.CreateFaction(game, "Partners");
                other.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
                foreignIron = other.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron");
                faction.GetDataBlob<FactionInfoDB>().Stances[other.Id] = FactionStance.Friendly;
                other.GetDataBlob<FactionInfoDB>().Stances[faction.Id] = FactionStance.Friendly;
                foreign = Colony(system, other, foreignIron, new Vector3(1.6e11, 0, 0), star, sell: 0, buy: foreignBuy, stock: 0);
            }

            if (farSell > 0)
                Colony(system, faction, iron, new Vector3(5e11, 0, 0), star, sell: farSell, buy: 0, stock: farSell);

            double hold = iron.VolumePerUnit * (capacityUnits + 0.5);
            var ship = Entity.Create(faction.Id);
            system.AddEntity(ship, new System.Collections.Generic.List<BaseDataBlob>
            {
                new ShipInfoDB(),
                new CargoStorageDB("general-storage", hold),
                new PositionDB(sourcePos, star),
                new ActionQueueDB(),
                new WarpAbilityDB { MaxSpeed = 1e9 },
                new NewtonThrustAbilityDB("iron"),
                new NewtonMoveDB(star, Vector3.Zero),
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            });
            if (lowFuel)
                ship.SetDataBlob(new AgentDB { Caution = 4f });

            Assert.That(ship.GetDataBlob<CargoStorageDB>().GetFreeUnitSpace(iron), Is.EqualTo(capacityUnits));

            return new Scene
            {
                Game = game,
                System = system,
                Faction = faction,
                Ship = ship,
                Source = source,
                Dest = dest,
                Foreign = foreign,
                Iron = iron,
                ForeignIron = foreignIron,
            };
        }

        static Entity Colony(
            StarSystem system, Entity owner, ICargoable iron, Vector3 at, Entity star,
            long sell, long buy, long stock)
        {
            var colony = Entity.Create(owner.Id);
            var cargo = new CargoStorageDB("general-storage", 100000);
            var planet = Entity.Create();
            var body = new SystemBodyInfoDB();
            body.LengthOfDay = TimeSpan.Zero;
            system.AddEntity(planet, new System.Collections.Generic.List<BaseDataBlob>
            {
                new NameDB("Body"),
                body,
                new PositionDB(at, star),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
            });
            var colonyInfo = new ColonyInfoDB();
            colonyInfo.PlanetEntity = planet;
            system.AddEntity(colony, new System.Collections.Generic.List<BaseDataBlob>
            {
                new NameDB("Colony"),
                colonyInfo,
                cargo,
                new LogiBaseDB { Capacity = 5 },
                new PositionDB(at, star),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
                new ActionQueueDB(),
            });
            if (stock > 0)
                Assert.That(cargo.AddCargoByUnit(iron, stock), Is.EqualTo(stock));
            Assert.That(MarketBook.SetListing(colony, new MarketListing
            {
                CargoId = iron.UniqueID,
                SellQuantity = sell,
                Ask = 10,
                BuyQuantity = buy,
                Bid = 12,
            }), Is.True);
            return colony;
        }
    }
}
