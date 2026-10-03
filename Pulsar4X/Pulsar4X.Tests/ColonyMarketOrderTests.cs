using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class ColonyMarketOrderTests
{
    [Test]
    public void Surplus_PostsSell_AndReserve()
    {
        var scene = Scene.Build();
        scene.Stock("iron", 250);
        scene.Policy("iron", min: 100, max: 100, auto: false, ask: 11, bid: 7);
        Assert.That(ColonyAdministrator.Quality(scene.Colony), Is.EqualTo(1.0m));

        var plan = new RunMarketPlan().Plan(scene.Colony, new Goal(GoalType.RunMarket), scene.Colony.StarSysDateTime);
        Assert.That(MarketBook.TryGet(scene.Colony, "iron", out _), Is.False);
        Assert.That(plan.Actions, Has.Count.EqualTo(1));
        Assert.That(plan.Actions[0].IsFinished(), Is.False);

        Assert.That(scene.Game.OrderHandler.HandleOrder(plan.Actions[0]), Is.True);
        AssertListing(scene.Colony, "iron", sell: 150, ask: 11, buy: 0, bid: 7, reserve: 100);

        scene.Colony.SetDataBlob(new AdministratorDB(new ProcessedMaterial(new ProcessedMaterialBlueprint
        {
            UniqueID = "admin-lab",
            Name = "Admin",
            IndustryTypeID = "research",
            CargoTypeID = "general-storage",
            VolumePerUnit = 0.001,
            MassPerUnit = 1,
            OutputAmount = 1,
            IndustryPointCosts = 1,
            ResourceCosts = new Dictionary<string, long>(),
        })));
        Assert.That(scene.Colony.HasDataBlob<AdministratorDB>(), Is.True);
        Assert.That(ColonyAdministrator.Quality(scene.Colony), Is.EqualTo(1.0m));
        var matched = new RunMarketPlan().Plan(scene.Colony, new Goal(GoalType.RunMarket), scene.Colony.StarSysDateTime);
        Assert.That(matched.Actions, Is.Empty);

        AgentProcessor.AssignGoal(scene.Colony, new Goal(GoalType.RunMarket));
        AgentProcessor.RunAgentNow(scene.Colony);
        Assert.That(scene.Colony.TryGetDataBlob<GoalsDB>(out var goals), Is.True);
        Assert.That(goals!.ActiveGoal!.Status, Is.EqualTo(GoalStatus.Active));
        AssertListing(scene.Colony, "iron", sell: 150, ask: 11, buy: 0, bid: 7, reserve: 100);
    }

    [Test]
    public void Shortage_PostsBuy()
    {
        var scene = Scene.Build();
        scene.Stock("iron", 40);
        scene.Policy("iron", min: 100, max: 100, auto: false, ask: 12, bid: 9);

        AgentProcessor.AssignGoal(scene.Colony, new Goal(GoalType.RunMarket));
        AssertListing(scene.Colony, "iron", sell: 0, ask: 12, buy: 60, bid: 9, reserve: 100);
    }

    [TestCase(200, 120, 0)]
    [TestCase(30, 0, 50)]
    public void IndustryNeed_HoldsEightyEvenWhenMinIsTen(long stock, long sell, long buy)
    {
        var scene = Scene.Build();
        scene.Stock("iron", stock);
        scene.AddLine("refinery", "refining");
        scene.EnsureDesign("hold-job", "refining");
        scene.AddNeed("refinery", "hold-job", "iron", 80);
        scene.Policy("iron", min: 10, max: 10, auto: false, ask: 6, bid: 5);

        AgentProcessor.AssignGoal(scene.Colony, new Goal(GoalType.RunMarket));
        AssertListing(scene.Colony, "iron", sell, ask: 6, buy, bid: 5, reserve: 10);
        Assert.That(scene.JobCount("iron"), Is.EqualTo(0));
    }

    [Test]
    public void AutoProduce_EnqueuesTheGapOnce()
    {
        var scene = Scene.Build();
        scene.EnsureDesign("alloy", "refining");
        scene.AddLine("refinery", "refining");
        scene.StockDesign("alloy", 10);
        scene.Policy("alloy", min: 0, max: 40, auto: true, ask: 4, bid: 3);

        var planned = new RunMarketPlan().Plan(scene.Colony, new Goal(GoalType.RunMarket), scene.Colony.StarSysDateTime);
        Assert.That(scene.JobCount("alloy"), Is.EqualTo(0));
        Assert.That(planned.Actions.OfType<IndustryOrder2>().Count(), Is.EqualTo(1));

        AgentProcessor.AssignGoal(scene.Colony, new Goal(GoalType.RunMarket));
        Assert.That(scene.JobCount("alloy"), Is.EqualTo(1));
        Assert.That(scene.Ordered("alloy"), Is.EqualTo(30));
        AssertListing(scene.Colony, "alloy", sell: 10, ask: 4, buy: 0, bid: 3, reserve: 0);

        AgentProcessor.RunAgentNow(scene.Colony);
        Assert.That(scene.JobCount("alloy"), Is.EqualTo(1));
        Assert.That(scene.Ordered("alloy"), Is.EqualTo(30));
        Assert.That(scene.Colony.TryGetDataBlob<GoalsDB>(out var goals), Is.True);
        Assert.That(goals!.ActiveGoal!.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public void MissingLine_PostsTheListing_AndNamesTheCargo()
    {
        var scene = Scene.Build();
        scene.Policy("iron", min: 5, max: 20, auto: true, ask: 2, bid: 1);

        var plan = new RunMarketPlan().Plan(scene.Colony, new Goal(GoalType.RunMarket), scene.Colony.StarSysDateTime);
        Assert.That(plan.Message, Does.Contain("iron"));
        Assert.That(plan.Message, Does.Contain("no production line"));
        Assert.That(plan.Actions, Has.Count.EqualTo(1));
        Assert.That(plan.Actions[0], Is.InstanceOf<PostMarketListingAction>());

        Assert.That(scene.Game.OrderHandler.HandleOrder(plan.Actions[0]), Is.True);
        AssertListing(scene.Colony, "iron", sell: 0, ask: 2, buy: 5, bid: 1, reserve: 5);
        Assert.That(scene.JobCount("iron"), Is.EqualTo(0));
    }

    [Test]
    public void Capacity_PostsTheHighestDeficit_AndNamesTheSkip()
    {
        var scene = Scene.Build(capacity: 1);
        scene.Stock("copper", 50);
        scene.Policy("iron", min: 100, max: 100, auto: false, ask: 9, bid: 8);
        scene.Policy("copper", min: 0, max: 0, auto: false, ask: 3, bid: 2);

        AgentProcessor.AssignGoal(scene.Colony, new Goal(GoalType.RunMarket));
        AssertListing(scene.Colony, "iron", sell: 0, ask: 9, buy: 100, bid: 8, reserve: 100);
        Assert.That(MarketBook.TryGet(scene.Colony, "copper", out _), Is.False);

        Assert.That(scene.Colony.TryGetDataBlob<GoalsDB>(out var goals), Is.True);
        Assert.That(goals!.ActiveGoal!.Message, Does.Contain("copper"));
        Assert.That(goals.ActiveGoal.Message, Does.Contain("skipped for capacity"));
    }

    [Test]
    public void ColonyWake_StaysActive_WhenThePassHasNoActions()
    {
        var scene = Scene.Build();
        var result = new CommandTranslator(scene.Game).Translate(
            scene.Faction, scene.Colony, new RunMarketCommand(scene.Colony.Id));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);

        Assert.That(scene.Colony.TryGetDataBlob<GoalsDB>(out var goals), Is.True);
        var goal = goals!.ActiveGoal!;
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(goal.Type, Is.EqualTo(GoalType.RunMarket));
        Assert.That(goal.Message, Is.Not.EqualTo("managed entity is not a ship or a fleet"));

        AgentProcessor.RunAgentNow(scene.Colony);
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(goal.Message, Is.Not.EqualTo("managed entity is not a ship or a fleet"));
    }

    [Test]
    public void RunMarket_RejectsAColonyWithNoOffice()
    {
        var bare = Scene.Build(office: false);
        var translator = new CommandTranslator(bare.Game);
        var run = translator.Translate(bare.Faction, bare.Colony, new RunMarketCommand(bare.Colony.Id));
        Assert.That(run.Accepted, Is.False);
        Assert.That(run.RejectionReason, Is.EqualTo("The colony has no logistics office."));
        Assert.That(bare.Colony.HasDataBlob<GoalsDB>(), Is.False);

        var set = translator.Translate(
            bare.Faction, bare.Colony, new SetMarketPolicyCommand(bare.Colony.Id, "iron", 0, 1, false, 1, 1));
        Assert.That(set.Accepted, Is.False);
        Assert.That(set.RejectionReason, Is.EqualTo("The colony has no logistics office."));

        var scene = Scene.Build();
        var commands = new CommandTranslator(scene.Game);
        Assert.That(commands.Translate(scene.Faction, scene.Colony,
            new SetMarketPolicyCommand(scene.Colony.Id, "", 0, 1, false, 1, 1)).Accepted, Is.False);
        Assert.That(commands.Translate(scene.Faction, scene.Colony,
            new SetMarketPolicyCommand(scene.Colony.Id, "iron", -1, 1, false, 1, 1)).Accepted, Is.False);
        Assert.That(commands.Translate(scene.Faction, scene.Colony,
            new SetMarketPolicyCommand(scene.Colony.Id, "iron", 0, 1, false, -1, 1)).Accepted, Is.False);
        Assert.That(commands.Translate(scene.Faction, scene.Colony,
            new SetMarketPolicyCommand(scene.Colony.Id, "iron", 5, 4, false, 1, 1)).Accepted, Is.False);
        Assert.That(commands.Translate(scene.Faction, scene.Colony,
            new ClearMarketPolicyCommand(scene.Colony.Id, "missing")).Accepted, Is.True);
    }

    [Test]
    public void Prune_RunMarket_RequiresALogisticsOffice()
    {
        Assert.That(GoalsDB.BaseWeights[GoalType.RunMarket], Is.EqualTo(0.5f));

        var withOffice = Scene.Build();
        var goals = new GoalsDB();
        AgentProcessor.PruneImpossibleGoals(goals, withOffice.Colony);
        Assert.That(goals.CapabilityModifiers.ContainsKey(GoalType.RunMarket), Is.False);

        var bare = Scene.Build(office: false);
        var pruned = new GoalsDB();
        AgentProcessor.PruneImpossibleGoals(pruned, bare.Colony);
        Assert.That(pruned.CapabilityModifiers[GoalType.RunMarket], Is.EqualTo(-1f));
    }

    static void AssertListing(Entity colony, string cargoId, long sell, decimal ask, long buy, decimal bid, long reserve)
    {
        Assert.That(MarketBook.TryGet(colony, cargoId, out var listing), Is.True);
        Assert.That(listing.SellQuantity, Is.EqualTo(sell));
        Assert.That(listing.Ask, Is.EqualTo(ask));
        Assert.That(listing.BuyQuantity, Is.EqualTo(buy));
        Assert.That(listing.Bid, Is.EqualTo(bid));
        Assert.That(listing.Reserve, Is.EqualTo(reserve));
    }

    sealed class Scene
    {
        public Game Game = null!;
        public Entity Faction = null!;
        public Entity Colony = null!;
        public FactionInfoDB Info = null!;
        public CargoStorageDB Store = null!;

        public void Stock(string cargoId, long units)
        {
            Info.Data.Unlock(cargoId);
            var cargo = Info.Data.CargoGoods.GetAny(cargoId);
            Assert.That(cargo, Is.Not.Null);
            Assert.That(Store.AddCargoByUnit(cargo!, units), Is.EqualTo(units));
        }

        public void Policy(string cargoId, long min, long max, bool auto, decimal ask, decimal bid)
        {
            var result = new CommandTranslator(Game).Translate(
                Faction,
                Colony,
                new SetMarketPolicyCommand(Colony.Id, cargoId, min, max, auto, ask, bid));
            Assert.That(result.Accepted, Is.True, result.RejectionReason);
        }

        public ProcessedMaterial EnsureDesign(string id, string industryTypeId)
        {
            if (Info.IndustryDesigns.TryGetValue(id, out var existing) && existing is ProcessedMaterial have)
                return have;

            var material = new ProcessedMaterial(new ProcessedMaterialBlueprint
            {
                UniqueID = id,
                Name = id,
                ResourceCosts = new Dictionary<string, long>(),
                IndustryPointCosts = 1,
                IndustryTypeID = industryTypeId,
                OutputAmount = 1,
                CargoTypeID = "general-storage",
                MassPerUnit = 1,
                VolumePerUnit = 0.001,
            });
            Info.IndustryDesigns[id] = material;
            Info.Data.CargoGoods.Add(material);
            return material;
        }

        public void StockDesign(string id, long units)
        {
            var cargo = Info.Data.CargoGoods.GetAny(id);
            Assert.That(cargo, Is.Not.Null);
            Assert.That(Store.AddCargoByUnit(cargo!, units), Is.EqualTo(units));
        }

        public void AddLine(string lineId, string industryTypeId)
        {
            var line = new IndustryAbilityDB.ProductionLine
            {
                Name = lineId,
                IndustryTypeRates = new Dictionary<string, int> { [industryTypeId] = 10 },
            };
            Colony.SetDataBlob(new IndustryAbilityDB(lineId, line));
        }

        public void AddNeed(string lineId, string designId, string cargoId, long remaining)
        {
            var job = new IndustryJob(Info, designId);
            job.ResourcesRequiredRemaining[cargoId] = remaining;
            Assert.That(Colony.TryGetDataBlob<IndustryAbilityDB>(out var industry), Is.True);
            industry!.ProductionLines[lineId].Jobs.Add(job);
        }

        public int JobCount(string cargoId) => Jobs(cargoId).Count();

        public int Ordered(string cargoId) => Jobs(cargoId).Sum(job => job.NumberOrdered - job.NumberCompleted);

        IEnumerable<IndustryJob> Jobs(string cargoId)
        {
            if (!Colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
                yield break;
            foreach (var line in industry!.ProductionLines.Values)
            {
                foreach (var job in line.Jobs)
                {
                    if (job.ItemGuid == cargoId)
                        yield return job;
                }
            }
        }

        public static Scene Build(int capacity = 5, bool office = true)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var faction = FactionFactory.CreateFaction(game, "Colony Market");
            Assert.That(faction.TryGetDataBlob<FactionInfoDB>(out var info), Is.True);

            var system = new StarSystem();
            system.Initialize(game, "Market Orders", -1);

            var cargo = new CargoStorageDB("general-storage", 100000);
            var blobs = new List<BaseDataBlob>
            {
                new NameDB("Colony"),
                new ColonyInfoDB(),
                cargo,
                new ActionQueueDB(),
            };
            if (office)
                blobs.Add(new LogiBaseDB { Capacity = capacity });

            var colony = Entity.Create(faction.Id);
            system.AddEntity(colony, blobs);

            return new Scene
            {
                Game = game,
                Faction = faction,
                Colony = colony,
                Info = info!,
                Store = cargo,
            };
        }
    }
}
