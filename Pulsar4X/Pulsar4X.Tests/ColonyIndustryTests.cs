using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class ColonyIndustryTests
{
    const string LineId = "refinery-line";

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
    public void InputsOnHand_QueuesOneBatchAndOpensTheRow()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        var order = plan.Actions.OfType<IndustryOrder2>().Single();
        Assert.That(order.ItemID, Is.EqualTo("stainless-steel"));
        Assert.That(order.AutoAddSubJobs, Is.False);
        Assert.That(order.IsValidCommand(Game), Is.True);
        order.Execute(colony.StarSysDateTime);

        var jobs = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs;
        Assert.That(jobs, Has.Count.EqualTo(1));
        Assert.That(jobs[0].NumberOrdered, Is.EqualTo(1));
        Assert.That(jobs[0].Auto, Is.False);
        var row = Row(colony, "stainless-steel");
        Assert.That(row.Max, Is.EqualTo(world.Steel.OutputAmount));
        Assert.That(row.Min, Is.EqualTo(0));
        Assert.That(row.AutoProduce, Is.False);
        Assert.That(row.Ask, Is.EqualTo(0));
        Assert.That(row.Bid, Is.EqualTo(0));
    }

    [Test]
    public void SecondPass_WhileTheJobIsQueued_AddsNothing()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);
        var line = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId];
        line.Jobs.Add(new IndustryJob(world.Info, "stainless-steel"));

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions.OfType<IndustryOrder2>(), Is.Empty);
        Assert.That(line.Jobs, Has.Count.EqualTo(1));
    }

    [Test]
    public void ShortOneInput_DoesNothing()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        Stock(colony, world.Iron, 88);
        Stock(colony, world.Chromium, 11);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions, Is.Empty);
        Assert.That(colony.HasDataBlob<ColonyMarketPolicyDB>(), Is.False);
    }

    [Test]
    public void ExistingMaxZero_IsUnchangedAndQueuesNothing()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);
        Policy(colony, "stainless-steel", min: 4, max: 0, auto: false, ask: 3, bid: 2);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions.OfType<IndustryOrder2>(), Is.Empty);
        var row = Row(colony, "stainless-steel");
        Assert.That(row.Min, Is.EqualTo(4));
        Assert.That(row.Max, Is.EqualTo(0));
        Assert.That(row.AutoProduce, Is.False);
        Assert.That(row.Ask, Is.EqualTo(3));
        Assert.That(row.Bid, Is.EqualTo(2));
    }

    [Test]
    public void ExistingMaxAboveStock_QueuesOneBatchWithoutEditingTheRow()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);
        Policy(colony, "stainless-steel", min: 4, max: 250, auto: false, ask: 3, bid: 2);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions.OfType<IndustryOrder2>().Count(), Is.EqualTo(1));
        var row = Row(colony, "stainless-steel");
        Assert.That(row.Min, Is.EqualTo(4));
        Assert.That(row.Max, Is.EqualTo(250));
        Assert.That(row.AutoProduce, Is.False);
        Assert.That(row.Ask, Is.EqualTo(3));
        Assert.That(row.Bid, Is.EqualTo(2));
    }

    [Test]
    public void AutoProduceRow_IsLeftToTheMarket()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);
        Policy(colony, "stainless-steel", min: 0, max: 250, auto: true, ask: 1, bid: 1);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions, Is.Empty);
        Assert.That(Row(colony, "stainless-steel").AutoProduce, Is.True);
        Assert.That(Row(colony, "stainless-steel").Max, Is.EqualTo(250));
    }

    [Test]
    public void ComponentOnAFactoryLine_IsNotQueued()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: false);
        var factory = new IndustryAbilityDB.ProductionLine
        {
            Name = "Factory",
            IndustryTypeRates = new Dictionary<string, int> { ["component-construction"] = 100 },
        };
        colony.SetDataBlob(new IndustryAbilityDB("factory-line", factory));
        Stock(colony, world.Iron, 10);
        world.Info.IndustryDesigns["widget"] = new ComponentDesign
        {
            UniqueID = "widget",
            Name = "Widget",
            IndustryTypeID = "component-construction",
            ResourceCosts = new Dictionary<string, long> { ["iron"] = 1 },
        };

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions, Is.Empty);
        Assert.That(colony.HasDataBlob<ColonyMarketPolicyDB>(), Is.False);
    }

    [Test]
    public void PlayerJob_BlocksTheLine()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId].Jobs
            .Add(new IndustryJob(world.Info, "stainless-steel"));

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions, Is.Empty);
    }

    [Test]
    public void NoIndustry_ReturnsNoActions()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: false);
        PayForSteel(world, colony);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions, Is.Empty);
        Assert.That(colony.HasDataBlob<ColonyMarketPolicyDB>(), Is.False);
    }

    [Test]
    public void FullOffice_SkipsTheBatch()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true, capacity: 0);
        PayForSteel(world, colony);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions, Is.Empty);
        Assert.That(plan.Message, Does.Contain("stainless-steel skipped for capacity"));
        Assert.That(colony.HasDataBlob<ColonyMarketPolicyDB>(), Is.False);
    }

    [Test]
    public void FewerStoredBatches_Wins()
    {
        var world = NewSystem();
        Unlock(world.Info, "plastic");
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);
        Stock(colony, world.Steel, 1000);
        Stock(colony, (ICargoable)world.Info.IndustryDesigns["plastic"], 2);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions.OfType<IndustryOrder2>().Single().ItemID, Is.EqualTo("plastic"));
    }

    [Test]
    public void UnstoredRecipe_DoesNotJumpAStoredPile()
    {
        var world = NewSystem();
        Unlock(world.Info, "plastic");
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);
        Stock(colony, world.Steel, 100);

        var plan = new RunIndustryPlan().Plan(colony, new Goal(GoalType.RunIndustry), colony.StarSysDateTime);

        Assert.That(plan.Actions.OfType<IndustryOrder2>().Single().ItemID, Is.EqualTo("stainless-steel"));
    }

    [Test]
    public void OfferStock_ReturnsIndustryAndMarketAndKeepsItsGoal()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);
        var goal = new Goal(GoalType.OfferStock);

        var plan = new OfferStockPlan().Plan(colony, goal, colony.StarSysDateTime);

        Assert.That(goal.Type, Is.EqualTo(GoalType.OfferStock));
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.Actions.OfType<IndustryOrder2>().Count(), Is.EqualTo(1));
        Assert.That(plan.Actions.OfType<PostMarketListingAction>(), Is.Not.Empty);
    }

    [Test]
    public void BaseWeights_DoesNotContainRunIndustry()
    {
        Assert.That(GoalsDB.BaseWeights.ContainsKey(GoalType.RunIndustry), Is.False);
    }

    [Test]
    public void ColonyWake_ReplansDuringTheBatch_WithoutASecondJob()
    {
        var world = NewSystem();
        var colony = Colony(world, withRefinery: true);
        PayForSteel(world, colony);

        AgentProcessor.AssignGoal(colony, new Goal(GoalType.OfferStock));
        var goal = colony.GetDataBlob<GoalsDB>().ActiveGoal!;
        var line = colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[LineId];
        Assert.That(goal.Type, Is.EqualTo(GoalType.OfferStock));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(line.Jobs, Has.Count.EqualTo(1));
        Assert.That(IndustryOrders(colony, goal), Is.EqualTo(1));

        AgentProcessor.RunAgentNow(colony);

        Assert.That(goal.Type, Is.EqualTo(GoalType.OfferStock));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(line.Jobs, Has.Count.EqualTo(1));
        Assert.That(IndustryOrders(colony, goal), Is.EqualTo(1));
    }

    static int IndustryOrders(Entity colony, Goal goal)
        => colony.GetDataBlob<ActionQueueDB>().ActionsFor(goal).OfType<IndustryOrder2>().Count();

    static World NewSystem()
    {
        var faction = FactionFactory.CreateFaction(Game, "Industry-" + Guid.NewGuid().ToString("N"));
        var info = faction.GetDataBlob<FactionInfoDB>();
        Unlock(info, "iron");
        Unlock(info, "chromium");
        Unlock(info, "hydrocarbons");
        Unlock(info, "stainless-steel");
        var system = new StarSystem();
        system.Initialize(Game, "Industry-" + Guid.NewGuid().ToString("N"), -1);
        return new World
        {
            System = system,
            Faction = faction,
            Info = info,
            Iron = info.Data.CargoGoods.GetAny("iron")!,
            Chromium = info.Data.CargoGoods.GetAny("chromium")!,
            Hydrocarbons = info.Data.CargoGoods.GetAny("hydrocarbons")!,
            Steel = (ProcessedMaterial)info.Data.CargoGoods.GetAny("stainless-steel")!,
        };
    }

    static void Unlock(FactionInfoDB info, string id)
    {
        info.Data.Unlock(id);
        if (info.Data.CargoGoods.IsMaterial(id))
            info.IndustryDesigns[id] = (IConstructableDesign)info.Data.CargoGoods.GetAny(id)!;
    }

    static Entity Colony(World world, bool withRefinery, int capacity = 5)
    {
        var planet = Entity.Create();
        world.System.AddEntity(planet, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });

        var blobs = new List<BaseDataBlob>
        {
            new NameDB("Colony"),
            new ColonyInfoDB { PlanetEntity = planet },
            new CargoStorageDB("general-storage", 100000),
            new LogiBaseDB { Capacity = capacity },
            new ActionQueueDB(),
        };
        if (withRefinery)
        {
            blobs.Add(new IndustryAbilityDB(LineId, new IndustryAbilityDB.ProductionLine
            {
                Name = "Refinery",
                IndustryTypeRates = new Dictionary<string, int> { ["refining"] = 500 },
            }));
        }

        var colony = Entity.Create(world.Faction.Id);
        world.System.AddEntity(colony, blobs);
        return colony;
    }

    static void PayForSteel(World world, Entity colony)
    {
        Stock(colony, world.Iron, 88);
        Stock(colony, world.Chromium, 11);
        Stock(colony, world.Hydrocarbons, 1);
    }

    static void Stock(Entity colony, ICargoable cargo, long units)
    {
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(cargo, units), Is.EqualTo(units));
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

    static MarketPolicyRow Row(Entity colony, string cargoId)
        => colony.GetDataBlob<ColonyMarketPolicyDB>().Rows[cargoId];

    sealed class World
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public FactionInfoDB Info = null!;
        public ICargoable Iron = null!;
        public ICargoable Chromium = null!;
        public ICargoable Hydrocarbons = null!;
        public ProcessedMaterial Steel = null!;
    }
}
