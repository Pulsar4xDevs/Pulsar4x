using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Civilian industry plan, slice 7: stock-keeping orders. A repeat job with a "when stock drops below X" level idles while
/// the colony holds X or more of its item, runs one batch (re-chaining its feeders) when stock drops below X, and needs a
/// seated industry head: the level is cleared when the head leaves.
/// </summary>
[TestFixture]
public class StockOrderTests
{
    const string RefineryId = "refinery-line";
    const string FactoryId = "factory-line";
    const string YardId = "yard-line";

    static readonly Game Game = InitializeGame();

    static Game InitializeGame()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 4, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        return game;
    }

    // The game allows 32 factions ever, and each test used to create its own. The whole fixture shares one owner
    // faction and one foreign faction; every test still builds its own star system, colony, designs and people.
    static readonly Lazy<Entity> SharedFaction = new(() => FactionFactory.CreateFaction(Game, "Stock-" + Guid.NewGuid().ToString("N")));
    static readonly Lazy<Entity> ForeignFaction = new(() => FactionFactory.CreateFaction(Game, "Other-" + Guid.NewGuid().ToString("N")));

    [SetUp]
    public void QuietSystems()
    {
        foreach (var sys in Game.Systems)
            sys.SetActivityState(SystemActivityState.Stasis);
    }

    sealed class World
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public FactionInfoDB Info = null!;
        public ComponentDesign Part = null!;   // component-construction, costs 40 plastic
        public ShipDesign Hull = null!;        // ship-assembly, costs 1 Part
    }

    static World NewWorld()
    {
        var faction = SharedFaction.Value;
        var info = faction.GetDataBlob<FactionInfoDB>();
        info.Data.Unlock("plastic");
        info.Data.Unlock("hydrocarbons");
        info.IndustryDesigns["plastic"] = (IConstructableDesign)info.Data.CargoGoods.GetAny("plastic")!;
        var system = new StarSystem();
        system.Initialize(Game, "Stock-" + Guid.NewGuid().ToString("N"), -1);
        system.SetActivityState(SystemActivityState.Stasis);

        var part = new ComponentDesign
        {
            UniqueID = "chain-part-" + Guid.NewGuid().ToString("N"),
            Name = "Gizmo",
            IndustryTypeID = "component-construction",
            ComponentMountType = ComponentMountType.ShipComponent,
            CargoTypeID = "general-storage",
            MassPerUnit = 100,
            VolumePerUnit = 10,
            IndustryPointCosts = 10,
        };
        part.ResourceCosts["plastic"] = 40;
        info.IndustryDesigns[part.UniqueID] = part;

        info.Data.Unlock("plastic-armor");
        var hull = new ShipDesign(
            info, "Hull Section",
            new List<(ComponentDesign, int)> { (part, 1) },
            (info.Data.Armor["plastic-armor"], 0f));
        hull.Initialise(info);
        hull.ResourceCosts.Clear();
        hull.ResourceCosts[part.UniqueID] = 1;
        info.IndustryDesigns[hull.UniqueID] = hull;
        return new World { System = system, Faction = faction, Info = info, Part = part, Hull = hull };
    }

    static Entity Colony(World world)
    {
        var planet = Entity.Create();
        world.System.AddEntity(planet, new List<BaseDataBlob>
        {
            new NameDB("Body"),
            new PositionDB(),
            MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
        });
        var lines = new Dictionary<string, IndustryAbilityDB.ProductionLine>
        {
            [RefineryId] = new() { Name = "Refinery", IndustryTypeRates = new() { ["refining"] = 500 } },
            [FactoryId] = new() { Name = "Factory", IndustryTypeRates = new() { ["component-construction"] = 100 } },
            [YardId] = new() { Name = "Ship Yard", MaxVolume = 1e9, IndustryTypeRates = new() { ["ship-assembly"] = 100 } },
        };
        var colony = Entity.Create(world.Faction.Id);
        world.System.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB("Stock Colony"),
            new ColonyInfoDB { PlanetEntity = planet },
            new CargoStorageDB("general-storage", 1e8),
            new IndustryAbilityDB(lines),
            new ActionQueueDB(),
        });
        return colony;
    }

    static Entity SeatHead(World world, Entity colony, string name = "Lee")
    {
        var person = CommanderFactory.Create(world.System, world.Faction.Id, new CommanderDB(name, 1, CommanderTypes.Civilian));
        Assert.That(IndustrySeat.Seat(colony, person, colony.StarSysDateTime), Is.Null);
        return person;
    }


    static readonly CommandTranslator Translator = new(Game);

    static CommandResult Queue(World world, Entity colony, string lineId, string designId, int quantity = 1)
        => Translator.Translate(world.Faction, colony,
            new QueueIndustryJobCommand(colony.Id, lineId, designId, quantity, false, false));

    static CommandResult QueueStock(World world, Entity colony, string lineId, string designId, int quantity, long below, bool repeat = true)
        => Translator.Translate(world.Faction, colony,
            new QueueIndustryJobCommand(colony.Id, lineId, designId, quantity, repeat, false, below));

    static CommandResult Edit(World world, Entity colony, string lineId, string jobId, int quantity, bool repeat, long? below)
        => Translator.Translate(world.Faction, colony, new EditIndustryJobCommand(colony.Id, lineId, jobId, quantity, repeat, below));

    static List<IndustryJob> Jobs(Entity colony, string lineId)
        => colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId].Jobs;

    static IndustryJob Gizmo(Entity colony) => Jobs(colony, FactoryId).Single();

    static void Gizmos(World world, Entity colony, long units)
        => colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(new ComponentInstance(world.Part), units);

    static long GizmoStock(World world, Entity colony)
        => colony.GetDataBlob<CargoStorageDB>().GetUnitsStoredByDesign(world.Part, false);

    static void Plastic(World world, Entity colony, long units)
        => colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(world.Info.Data.CargoGoods.GetAny("plastic")!, units);

    static long PlasticStock(World world, Entity colony)
        => colony.GetDataBlob<CargoStorageDB>().GetUnitsStoredByDesign(world.Info.Data.CargoGoods.GetAny("plastic")!, false);

    static void Day(Entity colony) => new IndustryProcessor().ProcessEntity(colony, 86400);

    static List<string> Log(Entity colony, string prefix)
        => colony.TryGetDataBlob<ShipLogDB>(out var log)
            ? log.Entries.Select(e => e.Text).Where(t => t.StartsWith(prefix)).ToList()
            : new List<string>();

    static IndustryView View(Entity colony)
        => new GameProjector(Game).ProjectEntity(colony, colony.FactionOwnerID).GetView<IndustryView>()!;

    // A colony with a head, a Gizmo stock order (level, quantity) queued on the factory, plastic for any number of batches.
    static (World world, Entity colony, Entity head) Ordered(long below, int quantity = 2, long plastic = 1000, long gizmos = 0)
    {
        var world = NewWorld();
        var colony = Colony(world);
        var head = SeatHead(world, colony);
        if (plastic > 0) Plastic(world, colony, plastic);
        if (gizmos > 0) Gizmos(world, colony, gizmos);
        var result = QueueStock(world, colony, FactoryId, world.Part.UniqueID, quantity, below);
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        return (world, colony, head);
    }

    // ----- idling and running -----

    [Test]
    public void AtTheLevel_TheJobIdles_AndBuildsNothing()
    {
        var (world, colony, _) = Ordered(below: 5, gizmos: 5);

        Day(colony);
        Day(colony);

        var job = Gizmo(colony);
        Assert.That(job.Status, Is.EqualTo(IndustryJobStatus.Idle));
        Assert.That(job.StockThreshold, Is.EqualTo(5));
        Assert.That(job.NumberCompleted, Is.EqualTo(0));
        Assert.That(GizmoStock(world, colony), Is.EqualTo(5));
        Assert.That(PlasticStock(world, colony), Is.EqualTo(1000), "an idle job takes no inputs");
    }

    [Test]
    public void AboveTheLevel_TheJobIdles()
    {
        var (world, colony, _) = Ordered(below: 5, gizmos: 9);

        Day(colony);

        Assert.That(Gizmo(colony).Status, Is.EqualTo(IndustryJobStatus.Idle));
        Assert.That(GizmoStock(world, colony), Is.EqualTo(9));
    }

    [Test]
    public void AnIdleJob_QueuedAtTheLevel_ChainsNoFeeders_AndSaysIdleStraightAway()
    {
        var (world, colony, _) = Ordered(below: 5, plastic: 0, gizmos: 5);

        Assert.That(Gizmo(colony).Status, Is.EqualTo(IndustryJobStatus.Idle), "no wait for the next day");
        Assert.That(Jobs(colony, RefineryId), Is.Empty);
        Assert.That(Log(colony, "Industry ("), Is.Empty);
    }

    [Test]
    public void BelowTheLevel_OneBatchRuns_ThenItWaitsForTheNextDrop()
    {
        var (world, colony, _) = Ordered(below: 5, quantity: 2, gizmos: 4);

        Day(colony);

        Assert.That(GizmoStock(world, colony), Is.EqualTo(6), "one batch of 2 on top of 4");
        Assert.That(Gizmo(colony).NumberCompleted, Is.EqualTo(0), "the repeat reset the batch");
        Assert.That(Gizmo(colony).Status, Is.EqualTo(IndustryJobStatus.Idle), "6 is at or above 5");

        Day(colony);
        Assert.That(GizmoStock(world, colony), Is.EqualTo(6), "no second batch while stock holds");

        // Stock drops again: the next day runs exactly one more batch.
        TakeGizmos(world, colony, 4);
        Assert.That(GizmoStock(world, colony), Is.EqualTo(2));
        Day(colony);
        Assert.That(GizmoStock(world, colony), Is.EqualTo(4), "2 + one batch of 2");
        Assert.That(Gizmo(colony).Status, Is.EqualTo(IndustryJobStatus.Queued).Or.EqualTo(IndustryJobStatus.Completed).Or.EqualTo(IndustryJobStatus.Idle));
    }

    static void TakeGizmos(World world, Entity colony, long units)
        => Assert.That(colony.GetDataBlob<CargoStorageDB>().RemoveUnitsByDesign(world.Part, units), Is.EqualTo(units));

    [Test]
    public void FarBelowTheLevel_ARunningBatchStillCompletes_BeforeStockIsRechecked()
    {
        var (world, colony, _) = Ordered(below: 100, quantity: 2, gizmos: 0);

        Day(colony);
        Assert.That(GizmoStock(world, colony), Is.EqualTo(2), "one batch a day while stock stays below the level");
        Day(colony);
        Assert.That(GizmoStock(world, colony), Is.EqualTo(4));
    }

    // ----- re-chaining -----

    [Test]
    public void WhenStockDrops_TheFeedersAreChainedAgain_WithTheirLogLines()
    {
        var (world, colony, head) = Ordered(below: 3, quantity: 1, plastic: 0, gizmos: 3);
        Assert.That(Jobs(colony, RefineryId), Is.Empty);

        TakeGizmos(world, colony, 1);
        Day(colony);

        var plastic = Jobs(colony, RefineryId).Single();
        Assert.That(plastic.ItemGuid, Is.EqualTo("plastic"));
        Assert.That(plastic.NumberOrdered, Is.EqualTo(40));
        Assert.That(plastic.Auto, Is.False);
        Assert.That(Log(colony, "Industry ("), Is.EqualTo(new[] { "Industry (Lee) queued 40 Plastic for Gizmo." }));
        Assert.That(Gizmo(colony).Status, Is.EqualTo(IndustryJobStatus.MissingResources));
    }

    [Test]
    public void AfterABatch_StillBelowTheLevel_TheNextBatchChainsItsFeedersAgain()
    {
        var (world, colony, _) = Ordered(below: 100, quantity: 1, plastic: 40);
        Assert.That(Jobs(colony, RefineryId), Is.Empty, "the plastic is on hand");

        Day(colony);

        Assert.That(GizmoStock(world, colony), Is.EqualTo(1));
        Assert.That(PlasticStock(world, colony), Is.EqualTo(0));
        Assert.That(Jobs(colony, RefineryId).Single().NumberOrdered, Is.EqualTo(40), "the next batch's plastic is chained");
        Assert.That(Log(colony, "Industry ("), Has.Count.EqualTo(1));
    }

    [Test]
    public void ABelowLevelOrder_QueuedWithNoStock_ChainsAtOnce_AheadOfTheJob()
    {
        var (world, colony, _) = Ordered(below: 3, quantity: 1, plastic: 0);

        Assert.That(Jobs(colony, RefineryId).Single().NumberOrdered, Is.EqualTo(40));
        Assert.That(Log(colony, "Industry ("), Is.EqualTo(new[] { "Industry (Lee) queued 40 Plastic for Gizmo." }));
    }

    // ----- rejections -----

    [Test]
    public void WithoutAHead_ANewStockOrder_IsRejected()
    {
        var world = NewWorld();
        var colony = Colony(world);

        var result = QueueStock(world, colony, FactoryId, world.Part.UniqueID, 1, 5);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("A stock order needs an industry head. Seat one on this colony first."));
        Assert.That(Jobs(colony, FactoryId), Is.Empty);
    }

    [Test]
    public void AStockOrder_MustRepeat()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);

        var result = QueueStock(world, colony, FactoryId, world.Part.UniqueID, 1, 5, repeat: false);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("A stock order needs a repeat job. Turn on Repeat first."));
        Assert.That(Jobs(colony, FactoryId), Is.Empty);
    }

    [Test]
    public void AStockLevel_MustBeOneOrMore()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);

        foreach (long level in new long[] { 0, -3 })
        {
            var result = QueueStock(world, colony, FactoryId, world.Part.UniqueID, 1, level);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo("A stock level must be 1 or more."));
        }
    }

    [Test]
    public void AShip_HasNoStockLevel()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);

        var result = QueueStock(world, colony, YardId, world.Hull.UniqueID, 1, 2);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("A stock order needs an item the colony stores. Ships have no stock level."));
    }

    [Test]
    public void APlainJob_NeedsNoHead_AndTheSixArgumentCommandStillWorks()
    {
        var world = NewWorld();
        var colony = Colony(world);

        var result = Translator.Translate(world.Faction, colony,
            new QueueIndustryJobCommand(colony.Id, FactoryId, world.Part.UniqueID, 1, true, false));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Gizmo(colony).StockThreshold, Is.Null);
    }

    // ----- clearing when the head leaves -----

    [Test]
    public void Unseating_ClearsTheLevel_AndTheJobRepeatsAsAPlainJob()
    {
        var (world, colony, head) = Ordered(below: 5, quantity: 2, gizmos: 5);
        Assert.That(Gizmo(colony).Status, Is.EqualTo(IndustryJobStatus.Idle));

        Assert.That(IndustrySeat.Unseat(colony, colony.StarSysDateTime), Is.Null);

        var job = Gizmo(colony);
        Assert.That(job.StockThreshold, Is.Null);
        Assert.That(job.Status, Is.Not.EqualTo(IndustryJobStatus.Idle));
        Assert.That(job.Auto, Is.True, "still a repeat job");
        Assert.That(Log(colony, "Industry: Gizmo on Factory lost its stock level because the industry head left; it now repeats as a plain job."), Has.Count.EqualTo(1));

        Day(colony);
        Assert.That(GizmoStock(world, colony), Is.GreaterThan(5), "it builds although stock was at the old level");
    }

    [Test]
    public void ALaterHead_DoesNotReviveTheLevel()
    {
        var (world, colony, _) = Ordered(below: 5, gizmos: 5);
        IndustrySeat.Unseat(colony, colony.StarSysDateTime);

        SeatHead(world, colony, "Kim");

        Assert.That(Gizmo(colony).StockThreshold, Is.Null);
    }

    [Test]
    public void ReplacingTheHead_KeepsTheLevel()
    {
        var (world, colony, _) = Ordered(below: 5, gizmos: 5);

        SeatHead(world, colony, "Kim");

        Assert.That(Gizmo(colony).StockThreshold, Is.EqualTo(5));
        Assert.That(Gizmo(colony).Status, Is.EqualTo(IndustryJobStatus.Idle));
    }

    [Test]
    public void TheHeadMovingToAnotherColony_ClearsTheLevelOnTheOldOne()
    {
        var (world, colony, head) = Ordered(below: 5, gizmos: 5);
        var other = Colony(world);

        Assert.That(IndustrySeat.Seat(other, head, other.StarSysDateTime), Is.Null);

        Assert.That(Gizmo(colony).StockThreshold, Is.Null);
    }

    [Test]
    public void TheHeadDying_ClearsTheLevel()
    {
        var (world, colony, head) = Ordered(below: 5, gizmos: 5);

        CommanderFactory.DestroyCommander(head);

        Assert.That(IndustrySeat.HasHead(colony), Is.False);
        var job = Gizmo(colony);
        Assert.That(job.StockThreshold, Is.Null);
        Assert.That(job.Status, Is.Not.EqualTo(IndustryJobStatus.Idle));
        Assert.That(Log(colony, "Industry: Gizmo on Factory lost its stock level because the industry head is gone"), Has.Count.EqualTo(1));
    }

    [Test]
    public void ADanglingSeat_ClearedOnLoadCleanup_ClearsTheLevel()
    {
        var (world, colony, head) = Ordered(below: 5, gizmos: 5);
        head.Destroy(); // gone without the clean death path, as in an old save

        Assert.That(IndustrySeat.ClearIfDangling(colony), Is.True);

        Assert.That(Gizmo(colony).StockThreshold, Is.Null);
    }

    [Test]
    public void IfTheSeatEmptiesUnnoticed_TheDailyPassClearsTheLevel()
    {
        var (world, colony, _) = Ordered(below: 5, quantity: 2, gizmos: 5);
        colony.GetDataBlob<IndustrySeatDB>().HeadId = IndustrySeatDB.Empty; // raw write, no hooks

        Day(colony);

        var job = Gizmo(colony);
        Assert.That(job.StockThreshold, Is.Null);
        Assert.That(GizmoStock(world, colony), Is.GreaterThan(5), "now a plain repeat job: it builds");
        Assert.That(Log(colony, "Industry: Gizmo on Factory lost its stock level"), Has.Count.EqualTo(1));
    }

    // ----- the view -----

    [Test]
    public void TheView_ShowsTheLevel_TheStock_AndWhyItIdles()
    {
        var (world, colony, _) = Ordered(below: 5, gizmos: 8);

        var job = View(colony).ProductionLines.Single(l => l.Id == FactoryId).Jobs.Single();

        Assert.That(job.StockBelow, Is.EqualTo(5));
        Assert.That(job.StockOnHand, Is.EqualTo(8));
        Assert.That(job.Status, Is.EqualTo("Idle"));
        Assert.That(job.IsIdle, Is.True);
        Assert.That(job.MissingResources, Is.False);
        Assert.That(job.IdleReason, Is.EqualTo("Stock 8 is at or above 5"));
        Assert.That(job.Repeat, Is.True);
    }

    [Test]
    public void TheView_OfAPlainJob_HasNoLevel()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        var job = View(colony).ProductionLines.Single(l => l.Id == FactoryId).Jobs.Single();

        Assert.That(job.StockBelow, Is.Null);
        Assert.That(job.StockOnHand, Is.Null);
        Assert.That(job.IsIdle, Is.False);
        Assert.That(job.IdleReason, Is.EqualTo(""));
    }

    [Test]
    public void AnIdleJob_DrawsNoPower()
    {
        var (world, colony, _) = Ordered(below: 5, gizmos: 5);

        var key = Pulsar4X.Energy.ColonyPowerProcessor.ProductionDrawKey(colony.GetDataBlob<IndustryAbilityDB>());

        Assert.That(key, Does.Contain("component-construction=-"));
    }

    // ----- commands -----

    [Test]
    public void TheQueueCommand_LogsTheLevel()
    {
        var (world, colony, _) = Ordered(below: 5, quantity: 2, gizmos: 5);

        Assert.That(Log(colony, "Industry: queued"), Is.EqualTo(new[]
        {
            "Industry: queued 2 Gizmo on Factory (repeating, when stock is below 5).",
        }));
    }

    [Test]
    public void Edit_SetsChangesAndClearsTheLevel()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);
        Gizmos(world, colony, 5);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        var id = Gizmo(colony).JobID;

        var set = Edit(world, colony, FactoryId, id, 3, true, 5);
        Assert.That(set.Accepted, Is.True, set.RejectionReason);
        Assert.That(Gizmo(colony).StockThreshold, Is.EqualTo(5));
        Assert.That(Gizmo(colony).NumberOrdered, Is.EqualTo(3));
        Assert.That(Gizmo(colony).Status, Is.EqualTo(IndustryJobStatus.Idle));

        Assert.That(Edit(world, colony, FactoryId, id, 3, true, 9).Accepted, Is.True);
        Assert.That(Gizmo(colony).StockThreshold, Is.EqualTo(9));
        Assert.That(Gizmo(colony).Status, Is.Not.EqualTo(IndustryJobStatus.Idle), "5 is now below the level of 9");

        var cleared = Edit(world, colony, FactoryId, id, 3, true, null);
        Assert.That(cleared.Accepted, Is.True, cleared.RejectionReason);
        Assert.That(Gizmo(colony).StockThreshold, Is.Null);
        Assert.That(Gizmo(colony).Auto, Is.True);

        Assert.That(Log(colony, "Industry: changed"), Is.EqualTo(new[]
        {
            "Industry: changed Gizmo on Factory to 3 (repeating, when stock is below 5).",
            "Industry: changed Gizmo on Factory to 3 (repeating, when stock is below 9).",
            "Industry: changed Gizmo on Factory to 3 (repeating).",
        }));
    }

    [Test]
    public void Edit_Rejections()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        var id = Gizmo(colony).JobID;

        var noHead = Edit(world, colony, FactoryId, id, 1, true, 5);
        Assert.That(noHead.Accepted, Is.False);
        Assert.That(noHead.RejectionReason, Is.EqualTo("A stock order needs an industry head. Seat one on this colony first."));

        SeatHead(world, colony);
        var notRepeat = Edit(world, colony, FactoryId, id, 1, false, 5);
        Assert.That(notRepeat.RejectionReason, Is.EqualTo("A stock order needs a repeat job. Turn on Repeat first."));

        Assert.That(Edit(world, colony, FactoryId, "nope", 1, true, 5).RejectionReason, Is.EqualTo("Job nope not found."));
        Assert.That(Edit(world, colony, "no-line", id, 1, true, 5).RejectionReason, Is.EqualTo("Production line no-line not found."));
        Assert.That(Edit(world, colony, FactoryId, id, 0, true, 5).RejectionReason, Is.EqualTo("Quantity must be between 1 and 65535."));
        Assert.That(Gizmo(colony).StockThreshold, Is.Null);
    }

    [Test]
    public void Edit_AddingALevelToARunningBatch_LetsItFinishFirst()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);
        Plastic(world, colony, 1000);
        Gizmos(world, colony, 50);
        Translator.Translate(world.Faction, colony, new QueueIndustryJobCommand(colony.Id, FactoryId, world.Part.UniqueID, 2, true, false));
        var job = Gizmo(colony);
        job.ProductionPointsLeft -= 3; // part-way through an item

        Assert.That(Edit(world, colony, FactoryId, job.JobID, 2, true, 5).Accepted, Is.True);

        Assert.That(job.Status, Is.Not.EqualTo(IndustryJobStatus.Idle), "stock is above the level, but the batch is under way");
        Day(colony);
        Assert.That(GizmoStock(world, colony), Is.EqualTo(52), "the running batch completed");
        Assert.That(job.Status, Is.EqualTo(IndustryJobStatus.Idle), "then it waits");
    }

    // ----- editing the quantity mid-batch -----

    static IndustryJob QueuedPlainJob(World world, Entity colony, int quantity, bool repeat)
    {
        Plastic(world, colony, 1000);
        Assert.That(Translator.Translate(world.Faction, colony,
            new QueueIndustryJobCommand(colony.Id, FactoryId, world.Part.UniqueID, quantity, repeat, false)).Accepted, Is.True);
        return Gizmo(colony);
    }

    [Test]
    public void Edit_ARepeatJob_ToLessThanBuilt_StartsANewBatch_InsteadOfStalling()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var job = QueuedPlainJob(world, colony, 5, repeat: true);
        job.NumberCompleted = 3;

        var result = Edit(world, colony, FactoryId, job.JobID, 2, true, null);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(job.NumberOrdered, Is.EqualTo(2));
        Assert.That(job.NumberCompleted, Is.EqualTo(0), "the count restarts, so it can meet the new quantity");
        Day(colony);
        Assert.That(GizmoStock(world, colony), Is.GreaterThan(0), "the repeat job builds");
        Assert.That(Jobs(colony, FactoryId), Has.Count.EqualTo(1), "and is still on the line");
    }

    [Test]
    public void Edit_ARepeatJob_ToExactlyWhatIsBuilt_AlsoStartsANewBatch()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var job = QueuedPlainJob(world, colony, 5, repeat: true);
        job.NumberCompleted = 3;

        Edit(world, colony, FactoryId, job.JobID, 3, true, null);

        Assert.That(job.NumberCompleted, Is.EqualTo(0));
        Assert.That(job.NumberOrdered, Is.EqualTo(3));
    }

    [Test]
    public void Edit_ARepeatJob_PartBuilt_KeepsTheProgress()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var job = QueuedPlainJob(world, colony, 5, repeat: true);
        job.NumberCompleted = 4;
        job.ProductionPointsLeft -= 3;
        long left = job.ProductionPointsLeft;

        Edit(world, colony, FactoryId, job.JobID, 2, true, null);

        Assert.That(job.NumberCompleted, Is.EqualTo(0));
        Assert.That(job.ProductionPointsLeft, Is.EqualTo(left), "the item in progress is not lost");
    }

    [Test]
    public void Edit_AOneShotJob_ToLessThanBuilt_FinishesAndLeavesTheLine()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var job = QueuedPlainJob(world, colony, 5, repeat: false);
        job.NumberCompleted = 3;

        var result = Edit(world, colony, FactoryId, job.JobID, 2, false, null);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Jobs(colony, FactoryId), Is.Empty, "3 are built and 2 were asked for: done");
    }

    [Test]
    public void Edit_AOneShotJob_PartBuilt_FinishesTheItemInProgress_ThenIsDone()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var job = QueuedPlainJob(world, colony, 5, repeat: false);
        job.NumberCompleted = 3;
        job.ProductionPointsLeft -= 3;

        Edit(world, colony, FactoryId, job.JobID, 2, false, null);

        Assert.That(job.NumberOrdered, Is.EqualTo(4), "one more: the part-built item");
        Day(colony);
        Assert.That(Jobs(colony, FactoryId), Is.Empty, "it completed and left; it did not stall");
        Assert.That(GizmoStock(world, colony), Is.EqualTo(1));
    }

    [Test]
    public void Edit_AStockOrder_ToLessThanBuilt_RestartsItsBatchAndKeepsItsLevel()
    {
        var (world, colony, _) = Ordered(below: 5, quantity: 5, gizmos: 2);
        var job = Gizmo(colony);
        Assert.That(job.StockBatchStarted, Is.True);
        job.NumberCompleted = 3;

        var result = Edit(world, colony, FactoryId, job.JobID, 2, true, 5);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(job.NumberCompleted, Is.EqualTo(0));
        Assert.That(job.StockThreshold, Is.EqualTo(5));
        Day(colony);
        Assert.That(GizmoStock(world, colony), Is.GreaterThanOrEqualTo(4), "it still builds its batch of 2");
    }

    // ----- save and load -----

    [Test]
    public void TheLevel_SurvivesSaveAndLoad()
    {
        var (world, colony, _) = Ordered(below: 5, quantity: 2, gizmos: 5);
        var job = Gizmo(colony);
        job.StockBatchStarted = true;

        string json = Game.Save(Game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        Assert.That(loaded.GlobalManagerDictionary.Values
            .Select(m => m.TryGetGlobalEntityById(colony.Id, out var e) ? e : null).First(e => e != null), Is.Not.Null);
        var loadedColony = loaded.GlobalManagerDictionary.Values
            .Select(m => m.TryGetGlobalEntityById(colony.Id, out var e) ? e : null).First(e => e != null)!;
        var loadedJob = loadedColony.GetDataBlob<IndustryAbilityDB>().ProductionLines[FactoryId].Jobs.Single();
        Assert.That(loadedJob.StockThreshold, Is.EqualTo(5));
        Assert.That(loadedJob.StockBatchStarted, Is.True);
        Assert.That(loadedJob.Status, Is.EqualTo(IndustryJobStatus.Idle));
        Assert.That(loadedJob.Auto, Is.True);
    }
}
