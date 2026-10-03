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
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class ColonySupplyTests
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
    public void Run_HandsTheSibling_AndPostsTheRoot()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var root = Colony(world, earth);
        var sibling = Colony(world, earth);
        Policy(root, "iron", min: 0, max: 0, auto: false, ask: 5, bid: 4);
        Policy(sibling, "iron", min: 0, max: 0, auto: false, ask: 5, bid: 4);
        var goal = new Goal(GoalType.SupplyLocal);

        var plan = new SupplyLocalPlan().Plan(root, goal, root.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var handed = plan.SubGoals.Single();
        Assert.That(handed.Sub.Id, Is.EqualTo(sibling.Id));
        Assert.That(handed.Goal.Type, Is.EqualTo(GoalType.RunMarket));
        Assert.That(handed.Goal.ParentGoalId, Is.EqualTo(goal.Id));
        Assert.That(plan.Actions.OfType<PostMarketListingAction>().Count(), Is.EqualTo(1));

        AgentProcessor.AssignGoal(sibling, handed.Goal);
        Assert.That(sibling.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.RunMarket));
    }

    [Test]
    public void Span_BodySkipsTheMoon_WellTakesIt_SystemReachesTheOtherPlanet()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var moon = Body(world.System, earth);
        var mars = Body(world.System, null);
        Assert.That(earth.GetDataBlob<PositionDB>().Children.Contains(moon), Is.True);
        Assert.That(earth.GetDataBlob<PositionDB>().Children.Contains(mars), Is.False);

        var root = Colony(world, earth);
        var moonColony = Colony(world, moon);
        var marsColony = Colony(world, mars);
        Policy(root, "iron", 0, 0, false, 5, 4);
        Policy(moonColony, "iron", 0, 0, false, 5, 4);
        Policy(marsColony, "iron", 0, 0, false, 5, 4);
        var goal = new Goal(GoalType.SupplyLocal);

        AttachBridge(root, AdminLevel.Ship);
        var body = new SupplyLocalPlan().Plan(root, goal, root.StarSysDateTime);
        Assert.That(HandedIds(body), Does.Not.Contain(moonColony.Id));
        Assert.That(HandedIds(body), Does.Not.Contain(marsColony.Id));

        AttachBridge(root, AdminLevel.Planet);
        var well = new SupplyLocalPlan().Plan(root, goal, root.StarSysDateTime);
        Assert.That(HandedIds(well), Does.Contain(moonColony.Id));
        Assert.That(HandedIds(well), Does.Not.Contain(marsColony.Id));

        AttachBridge(root, AdminLevel.System);
        var system = new SupplyLocalPlan().Plan(root, goal, root.StarSysDateTime);
        Assert.That(HandedIds(system), Does.Contain(moonColony.Id));
        Assert.That(HandedIds(system), Does.Contain(marsColony.Id));
        Assert.That(system.SubGoals.All(item => item.Goal.ParentGoalId == goal.Id));
    }

    [Test]
    public void FriendlyForeignColony_IsNotAMember()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var root = Colony(world, earth);
        var sibling = Colony(world, earth);
        Policy(root, "iron", 0, 0, false, 5, 4);
        Policy(sibling, "iron", 0, 0, false, 5, 4);

        var other = FactionFactory.CreateFaction(Game, "Neighbors-" + Guid.NewGuid().ToString("N"));
        world.Faction.GetDataBlob<FactionInfoDB>().Stances[other.Id] = FactionStance.Friendly;
        other.GetDataBlob<FactionInfoDB>().Stances[world.Faction.Id] = FactionStance.Friendly;
        var saved = world.Faction;
        world.Faction = other;
        var foreign = Colony(world, earth);
        Policy(foreign, "iron", 0, 0, false, 9, 8);
        world.Faction = saved;

        var plan = new SupplyLocalPlan().Plan(root, new Goal(GoalType.SupplyLocal), root.StarSysDateTime);

        Assert.That(HandedIds(plan), Does.Contain(sibling.Id));
        Assert.That(HandedIds(plan), Does.Not.Contain(foreign.Id));
    }

    [Test]
    public void Balance_CopiesThePrice_OntoTheSurplusColony()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var root = Colony(world, earth);
        var stocked = Colony(world, earth);
        Policy(root, "iron", min: 100, max: 100, auto: false, ask: 11, bid: 7);
        Stock(world, root, 40);
        Stock(world, stocked, 80);

        var plan = new SupplyLocalPlan().Plan(root, new Goal(GoalType.SupplyLocal) { SupplyMode = SupplyMode.Balance }, root.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(Row(root, "iron").Min, Is.EqualTo(100));
        var added = Row(stocked, "iron");
        Assert.That(added.Min, Is.EqualTo(0));
        Assert.That(added.Max, Is.EqualTo(0));
        Assert.That(added.AutoProduce, Is.False);
        Assert.That(added.Ask, Is.EqualTo(11));
        Assert.That(added.Bid, Is.EqualTo(7));
    }

    [Test]
    public void Balance_WithNoPrice_DoesNotAddARow()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var root = Colony(world, earth);
        var other = Colony(world, earth);
        Stock(world, root, 50);
        Stock(world, other, 50);

        var plan = new SupplyLocalPlan().Plan(root, new Goal(GoalType.SupplyLocal) { SupplyMode = SupplyMode.Balance }, root.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(HasRow(root, "iron"), Is.False);
        Assert.That(HasRow(other, "iron"), Is.False);
        Assert.That(plan.SubGoals, Is.Empty);
    }

    [Test]
    public void Stockpile_RaisesRootMin_AndLeavesTheExporter()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var root = Colony(world, earth);
        var exporter = Colony(world, earth);
        Policy(root, "iron", min: 10, max: 10, auto: true, ask: 8, bid: 4);
        Policy(exporter, "iron", min: 15, max: 15, auto: false, ask: 3, bid: 2);
        Stock(world, root, 20);
        Stock(world, exporter, 55);

        var plan = new SupplyLocalPlan().Plan(root, new Goal(GoalType.SupplyLocal) { SupplyMode = SupplyMode.Stockpile }, root.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var sink = Row(root, "iron");
        Assert.That(sink.Min, Is.EqualTo(60));
        Assert.That(sink.Max, Is.EqualTo(60));
        Assert.That(sink.Ask, Is.EqualTo(8));
        Assert.That(sink.Bid, Is.EqualTo(4));
        Assert.That(sink.AutoProduce, Is.True);
        var source = Row(exporter, "iron");
        Assert.That(source.Min, Is.EqualTo(15));
        Assert.That(source.Ask, Is.EqualTo(3));
        Assert.That(source.Bid, Is.EqualTo(2));
        Assert.That(source.AutoProduce, Is.False);
    }

    [Test]
    public void BusyMoveTo_AndBusySupplyLocal_AreLeftAlone()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var root = Colony(world, earth);
        var moving = Colony(world, earth);
        var supplying = Colony(world, earth);
        var free = Colony(world, earth);
        Policy(root, "iron", 0, 0, false, 5, 4);
        Policy(moving, "iron", 0, 0, false, 5, 4);
        Policy(supplying, "iron", 0, 0, false, 5, 4);
        Policy(free, "iron", 0, 0, false, 5, 4);
        moving.SetDataBlob(new GoalsDB
        {
            ActiveGoal = new Goal(GoalType.MoveTo) { Status = GoalStatus.Active },
        });
        supplying.SetDataBlob(new GoalsDB
        {
            ActiveGoal = new Goal(GoalType.SupplyLocal) { Status = GoalStatus.Planning },
        });

        var plan = new SupplyLocalPlan().Plan(root, new Goal(GoalType.SupplyLocal), root.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(HandedIds(plan), Is.EquivalentTo(new[] { free.Id }));
        Assert.That(moving.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.MoveTo));
        Assert.That(supplying.GetDataBlob<GoalsDB>().ActiveGoal!.Type, Is.EqualTo(GoalType.SupplyLocal));
    }

    [Test]
    public void EmptyPass_StaysActive()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var root = Colony(world, earth);

        var plan = new SupplyLocalPlan().Plan(root, new Goal(GoalType.SupplyLocal), root.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.Status, Is.Not.EqualTo(GoalStatus.Completed));
        Assert.That(plan.Actions, Is.Empty);
        Assert.That(plan.SubGoals, Is.Empty);
        Assert.That(GoalsDB.BaseWeights.ContainsKey(GoalType.SupplyLocal), Is.False);
    }

    [Test]
    public void Command_SetsTheLabel_AndRejectsABadTarget()
    {
        var world = NewSystem();
        var earth = Body(world.System, null);
        var translator = new CommandTranslator(Game);

        var balance = Colony(world, earth);
        var balanced = translator.Translate(world.Faction, balance, new SupplyLocalCommand(balance.Id, SupplyMode.Balance));
        Assert.That(balanced.Accepted, Is.True, balanced.RejectionReason);
        var balanceGoal = balance.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(balanceGoal.Type, Is.EqualTo(GoalType.SupplyLocal));
        Assert.That(balanceGoal.SupplyMode, Is.EqualTo(SupplyMode.Balance));
        Assert.That(balanceGoal.Name, Is.EqualTo("Balance"));
        Assert.That(balanceGoal.Status, Is.EqualTo(GoalStatus.Active));

        var stock = Colony(world, earth);
        var stocked = translator.Translate(world.Faction, stock, new SupplyLocalCommand(stock.Id, SupplyMode.Stockpile));
        Assert.That(stocked.Accepted, Is.True, stocked.RejectionReason);
        Assert.That(stock.GetDataBlob<GoalsDB>().ActiveGoal!.Name, Is.EqualTo("Stockpile"));

        var run = Colony(world, earth);
        var running = translator.Translate(world.Faction, run, new SupplyLocalCommand(run.Id, SupplyMode.Run));
        Assert.That(running.Accepted, Is.True, running.RejectionReason);
        Assert.That(run.GetDataBlob<GoalsDB>().ActiveGoal!.Name, Is.EqualTo("Run markets"));

        var bare = Entity.Create(world.Faction.Id);
        world.System.AddEntity(bare, new List<BaseDataBlob>
        {
            new NameDB("Not a colony"),
            new LogiBaseDB { Capacity = 5 },
        });
        var notColony = translator.Translate(world.Faction, bare, new SupplyLocalCommand(bare.Id, SupplyMode.Run));
        Assert.That(notColony.Accepted, Is.False);
        Assert.That(notColony.RejectionReason, Is.EqualTo("The target is not a colony."));

        var noOffice = Entity.Create(world.Faction.Id);
        var info = new ColonyInfoDB();
        info.PlanetEntity = earth;
        world.System.AddEntity(noOffice, new List<BaseDataBlob>
        {
            new NameDB("No office"),
            info,
        });
        var missing = translator.Translate(world.Faction, noOffice, new SupplyLocalCommand(noOffice.Id, SupplyMode.Run));
        Assert.That(missing.Accepted, Is.False);
        Assert.That(missing.RejectionReason, Is.EqualTo("The colony has no logistics office."));

        var other = FactionFactory.CreateFaction(Game, "Owners-" + Guid.NewGuid().ToString("N"));
        var saved = world.Faction;
        world.Faction = other;
        var foreign = Colony(world, earth);
        world.Faction = saved;
        var notYours = translator.Translate(saved, foreign, new SupplyLocalCommand(foreign.Id, SupplyMode.Run));
        Assert.That(notYours.Accepted, Is.False);
        Assert.That(notYours.RejectionReason, Is.EqualTo("The colony is not yours."));
    }

    static List<int> HandedIds(PlanResult plan)
        => plan.SubGoals.Select(item => item.Sub.Id).ToList();

    static void AttachBridge(Entity colony, AdminLevel level)
    {
        var admin = new AdminSpaceDB();
        admin.CommanderSeats.Add(new AdminSpaceAbilityState(level, "command-bridge"));
        colony.SetDataBlob(admin);
    }

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "Supply-" + Guid.NewGuid().ToString("N"));
        faction.GetDataBlob<FactionInfoDB>().Data.Unlock("iron");
        var system = new StarSystem();
        system.Initialize(Game, "Supply-" + Guid.NewGuid().ToString("N"), -1);
        return new World
        {
            System = system,
            Faction = faction,
            Iron = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("iron"),
        };
    }

    static Entity Body(StarSystem system, Entity? parent)
    {
        var body = Entity.Create();
        var position = new PositionDB();
        system.AddEntity(body, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            position,
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });
        if (parent != null)
            position.SetParent(parent);
        return body;
    }

    static Entity Colony(World world, Entity planet)
    {
        var colony = Entity.Create(world.Faction.Id);
        var info = new ColonyInfoDB();
        info.PlanetEntity = planet;
        world.System.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB("Colony"),
            info,
            new CargoStorageDB("general-storage", 100000),
            new LogiBaseDB { Capacity = 5 },
            new ActionQueueDB(),
        });
        return colony;
    }

    static void Policy(Entity colony, string cargoId, long min, long max, bool auto, decimal ask, decimal bid)
    {
        if (!colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
        {
            policy = new ColonyMarketPolicyDB();
            colony.SetDataBlob(policy);
        }
        policy.Rows[cargoId] = new MarketPolicyRow
        {
            CargoId = cargoId,
            Min = min,
            Max = max,
            AutoProduce = auto,
            Ask = ask,
            Bid = bid,
        };
    }

    static void Stock(World world, Entity colony, long units)
    {
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(world.Iron, units), Is.EqualTo(units));
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
        public StarSystem System;
        public Entity Faction;
        public ICargoable Iron;
    }
}
