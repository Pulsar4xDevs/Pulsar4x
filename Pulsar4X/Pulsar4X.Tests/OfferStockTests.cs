using System;
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
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class OfferStockTests
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
    public void WarehouseStock_BecomesASellListing()
    {
        var world = NewSystem();
        var colony = Colony(world, Body(world.System), capacity: 5);
        Stock(world, colony, world.Iron, 40);

        AgentProcessor.AssignGoal(colony, new Goal(GoalType.OfferStock));

        var goal = colony.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(goal.Type, Is.EqualTo(GoalType.OfferStock));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
        var row = Row(colony, "iron");
        Assert.That(row.Min, Is.EqualTo(0));
        Assert.That(row.Max, Is.EqualTo(0));
        Assert.That(row.AutoProduce, Is.False);
        Assert.That(row.Ask, Is.EqualTo(0));
        Assert.That(row.Bid, Is.EqualTo(0));
        Assert.That(MarketBook.TryGet(colony, "iron", out var listing), Is.True);
        Assert.That(listing.SellQuantity, Is.EqualTo(40));
        Assert.That(listing.BuyQuantity, Is.EqualTo(0));
        Assert.That(listing.Reserve, Is.EqualTo(0));
        Assert.That(listing.Ask, Is.EqualTo(0));
    }

    [Test]
    public void ExistingRow_IsLeftAlone()
    {
        var world = NewSystem();
        var colony = Colony(world, Body(world.System), capacity: 5);
        Stock(world, colony, world.Iron, 40);
        Policy(colony, "iron", min: 3, max: 11, auto: true, ask: 9, bid: 7);

        var plan = new OfferStockPlan().Plan(colony, new Goal(GoalType.OfferStock), colony.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        var row = Row(colony, "iron");
        Assert.That(row.Min, Is.EqualTo(3));
        Assert.That(row.Max, Is.EqualTo(11));
        Assert.That(row.AutoProduce, Is.True);
        Assert.That(row.Ask, Is.EqualTo(9));
        Assert.That(row.Bid, Is.EqualTo(7));
    }

    [Test]
    public void FullOffice_KeepsTheLargestPile()
    {
        var world = NewSystem();
        var colony = Colony(world, Body(world.System), capacity: 1);
        Stock(world, colony, world.Iron, 100);
        Stock(world, colony, world.Titanium, 10);

        var plan = new OfferStockPlan().Plan(colony, new Goal(GoalType.OfferStock), colony.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.Message, Does.Contain("1 skipped for capacity"));
        Assert.That(HasRow(colony, "iron"), Is.True);
        Assert.That(HasRow(colony, "titanium"), Is.False);
        Assert.That(Row(colony, "iron").Ask, Is.EqualTo(0));
    }

    [Test]
    public void ClearedRow_ComesBackWhileTheJobIsActive()
    {
        var world = NewSystem();
        var colony = Colony(world, Body(world.System), capacity: 5);
        Stock(world, colony, world.Iron, 40);
        AgentProcessor.AssignGoal(colony, new Goal(GoalType.OfferStock));
        colony.GetDataBlob<ColonyMarketPolicyDB>().Rows.Remove("iron");

        var plan = new OfferStockPlan().Plan(colony, colony.GetDataBlob<GoalsDB>().ActiveGoal!, colony.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(HasRow(colony, "iron"), Is.True);
    }

    [Test]
    public void EmptyWarehouse_StaysActive()
    {
        var world = NewSystem();
        var colony = Colony(world, Body(world.System), capacity: 5);

        var plan = new OfferStockPlan().Plan(colony, new Goal(GoalType.OfferStock), colony.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.Actions, Is.Empty);
        Assert.That(colony.HasDataBlob<ColonyMarketPolicyDB>(), Is.False);
    }

    [Test]
    public void NoOffice_StaysActive()
    {
        var world = NewSystem();
        var colony = Entity.Create(world.Faction.Id);
        var info = new ColonyInfoDB();
        info.PlanetEntity = Body(world.System);
        world.System.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB("No office"),
            info,
            new CargoStorageDB("general-storage", 100000),
        });
        Stock(world, colony, world.Iron, 40);

        var plan = new OfferStockPlan().Plan(colony, new Goal(GoalType.OfferStock), colony.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.Actions, Is.Empty);
        Assert.That(colony.HasDataBlob<ColonyMarketPolicyDB>(), Is.False);
    }

    [Test]
    public void BaseWeights_DoesNotContainOfferStock()
    {
        Assert.That(GoalsDB.BaseWeights.ContainsKey(GoalType.OfferStock), Is.False);
    }

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "Offer-" + Guid.NewGuid().ToString("N"));
        var info = faction.GetDataBlob<FactionInfoDB>();
        info.Data.Unlock("iron");
        info.Data.Unlock("titanium");
        var system = new StarSystem();
        system.Initialize(Game, "Offer-" + Guid.NewGuid().ToString("N"), -1);
        return new World
        {
            System = system,
            Faction = faction,
            Iron = info.Data.CargoGoods.GetAny("iron"),
            Titanium = info.Data.CargoGoods.GetAny("titanium"),
        };
    }

    static Entity Body(StarSystem system)
    {
        var body = Entity.Create();
        system.AddEntity(body, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });
        return body;
    }

    static Entity Colony(World world, Entity planet, int capacity)
    {
        var colony = Entity.Create(world.Faction.Id);
        var info = new ColonyInfoDB();
        info.PlanetEntity = planet;
        world.System.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB("Colony"),
            info,
            new CargoStorageDB("general-storage", 100000),
            new LogiBaseDB { Capacity = capacity },
            new ActionQueueDB(),
        });
        return colony;
    }

    static void Policy(Entity colony, string cargoId, long min, long max, bool auto, decimal ask, decimal bid)
    {
        var policy = new ColonyMarketPolicyDB();
        policy.Rows[cargoId] = new MarketPolicyRow
        {
            CargoId = cargoId,
            Min = min,
            Max = max,
            AutoProduce = auto,
            Ask = ask,
            Bid = bid,
        };
        colony.SetDataBlob(policy);
    }

    static void Stock(World world, Entity colony, ICargoable cargo, long units)
    {
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(cargo, units), Is.EqualTo(units));
    }

    static MarketPolicyRow Row(Entity colony, string cargoId)
    {
        Assert.That(colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy), Is.True);
        Assert.That(policy!.Rows.ContainsKey(cargoId), Is.True);
        return policy.Rows[cargoId];
    }

    static bool HasRow(Entity colony, string cargoId)
        => colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) && policy.Rows.ContainsKey(cargoId);

    sealed class World
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public ICargoable Iron = null!;
        public ICargoable Titanium = null!;
    }
}
