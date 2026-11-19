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
/// Automatic sub-jobs (feeder jobs). Civilian industry plan, slice 3: queuing a job chains the feeder jobs for
/// its missing buildable inputs only while the colony has an industry head seated. Plan orders never chain.
/// </summary>
[TestFixture]
public class IndustryChainingTests
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
    static readonly Lazy<Entity> SharedFaction = new(() => FactionFactory.CreateFaction(Game, "Chain-" + Guid.NewGuid().ToString("N")));
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
        system.Initialize(Game, "Chain-" + Guid.NewGuid().ToString("N"), -1);
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
            new NameDB("Chain Colony"),
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

    static CommandResult Queue(World world, Entity colony, string lineId, string designId, int quantity = 1)
        => new CommandTranslator(Game).Translate(world.Faction, colony,
            new QueueIndustryJobCommand(colony.Id, lineId, designId, quantity, false, false));

    static List<IndustryJob> Jobs(Entity colony, string lineId)
        => colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[lineId].Jobs;

    static List<string> IndustryLog(Entity colony)
        => colony.TryGetDataBlob<ShipLogDB>(out var log)
            ? log.Entries.Select(e => e.Text).Where(t => t.StartsWith("Industry (")).ToList()
            : new List<string>();

    [Test]
    public void FactoryJob_ShortOfPlastic_QueuesARefiningJob()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);

        var result = Queue(world, colony, FactoryId, world.Part.UniqueID);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Jobs(colony, FactoryId).Select(j => j.ItemGuid), Is.EqualTo(new[] { world.Part.UniqueID }));
        var plastic = Jobs(colony, RefineryId).Single();
        Assert.That(plastic.ItemGuid, Is.EqualTo("plastic"));
        Assert.That(plastic.NumberOrdered, Is.EqualTo(40));
        Assert.That(plastic.Auto, Is.False, "a feeder job is one-shot");
    }

    [Test]
    public void FactoryJob_WithPlasticOnHand_QueuesNoFeeder()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);
        var plastic = world.Info.Data.CargoGoods.GetAny("plastic")!;
        colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(plastic, 40);

        Assert.That(Queue(world, colony, FactoryId, world.Part.UniqueID).Accepted, Is.True);

        Assert.That(Jobs(colony, FactoryId), Has.Count.EqualTo(1));
        Assert.That(Jobs(colony, RefineryId), Is.Empty);
    }

    [Test]
    public void ShipyardJob_ShortOfComponents_QueuesFactoryJobs_Recursively()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);

        var result = Queue(world, colony, YardId, world.Hull.UniqueID);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Jobs(colony, YardId).Select(j => j.ItemGuid), Is.EqualTo(new[] { world.Hull.UniqueID }));
        var part = Jobs(colony, FactoryId).Single();
        Assert.That(part.ItemGuid, Is.EqualTo(world.Part.UniqueID));
        Assert.That(part.NumberOrdered, Is.EqualTo(1));
        var plastic = Jobs(colony, RefineryId).Single();
        Assert.That(plastic.ItemGuid, Is.EqualTo("plastic"));
        Assert.That(plastic.NumberOrdered, Is.EqualTo(40));
    }

    // ----- the gate -----

    [Test]
    public void NoHead_AFactoryJob_AddsOnlyItself()
    {
        var world = NewWorld();
        var colony = Colony(world);

        var result = Queue(world, colony, FactoryId, world.Part.UniqueID);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Jobs(colony, FactoryId).Select(j => j.ItemGuid), Is.EqualTo(new[] { world.Part.UniqueID }));
        Assert.That(Jobs(colony, RefineryId), Is.Empty);
        Assert.That(IndustryLog(colony), Is.Empty);
    }

    [Test]
    public void NoHead_AShipyardJob_AddsOnlyItself()
    {
        var world = NewWorld();
        var colony = Colony(world);

        Assert.That(Queue(world, colony, YardId, world.Hull.UniqueID).Accepted, Is.True);

        Assert.That(Jobs(colony, YardId), Has.Count.EqualTo(1));
        Assert.That(Jobs(colony, FactoryId), Is.Empty);
        Assert.That(Jobs(colony, RefineryId), Is.Empty);
    }

    [Test]
    public void TheJob_StillWaitsOnItsInputs_WithoutAHead()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        new IndustryProcessor().ProcessEntity(colony, 86400);

        var job = Jobs(colony, FactoryId).Single();
        Assert.That(job.Status, Is.EqualTo(IndustryJobStatus.MissingResources));
        Assert.That(job.NumberCompleted, Is.EqualTo(0));
    }

    [Test]
    public void SeatingAHead_StartsChaining_AndUnseatingStopsIt()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Assert.That(Jobs(colony, RefineryId), Is.Empty, "no head yet");

        var head = SeatHead(world, colony);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Assert.That(Jobs(colony, RefineryId), Has.Count.EqualTo(1));

        IndustrySeat.Unseat(colony, colony.StarSysDateTime);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Assert.That(Jobs(colony, RefineryId), Has.Count.EqualTo(1), "the unseated head's chaining stops");
        Assert.That(Jobs(colony, FactoryId), Has.Count.EqualTo(3));
        Assert.That(head.GetDataBlob<CommanderDB>().AssignedTo, Is.EqualTo(-1));
    }

    [Test]
    public void WithAHead_ChainingStaysOnThisColony()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var neighbour = Colony(world);
        SeatHead(world, colony);

        Queue(world, colony, YardId, world.Hull.UniqueID);

        Assert.That(Jobs(colony, RefineryId), Has.Count.EqualTo(1));
        Assert.That(Jobs(neighbour, RefineryId), Is.Empty);
        Assert.That(Jobs(neighbour, FactoryId), Is.Empty);
        Assert.That(Jobs(neighbour, YardId), Is.Empty);
    }

    [Test]
    public void AHeadElsewhere_DoesNotChainThisColonysJobs()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var other = Colony(world);
        SeatHead(world, other);

        Queue(world, colony, FactoryId, world.Part.UniqueID);

        Assert.That(Jobs(colony, RefineryId), Is.Empty);
        Assert.That(Jobs(other, RefineryId), Is.Empty);
    }

    [Test]
    public void PlanOrders_DoNotChain_EvenWithAHead()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony);
        var job = new IndustryJob(world.Info, world.Part.UniqueID);
        job.InitialiseJob(1, false);
        var order = IndustryOrder2.CreateNewJobOrder(world.Faction.Id, colony, FactoryId, job);
        order.AutoAddSubJobs = false; // what RunIndustryPlan and RunMarketPlan set
        Assert.That(order.IsValidCommand(Game), Is.True);

        order.Execute(colony.StarSysDateTime);

        Assert.That(Jobs(colony, FactoryId), Has.Count.EqualTo(1));
        Assert.That(Jobs(colony, RefineryId), Is.Empty);
        Assert.That(IndustryLog(colony), Is.Empty);
    }

    // ----- the log -----

    [Test]
    public void EachChainedJob_GetsOneIndustryLogLine_InTheHeadsName()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony, "Lee");

        Queue(world, colony, YardId, world.Hull.UniqueID);

        Assert.That(IndustryLog(colony), Is.EqualTo(new[]
        {
            "Industry (Lee) queued 1 Gizmo for Hull Section.",
            "Industry (Lee) queued 40 Plastic for Gizmo.",
        }));
    }

    [Test]
    public void TheLogLine_NamesTheCurrentHead()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony, "Lee");
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        SeatHead(world, colony, "Moss");
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        Assert.That(IndustryLog(colony), Is.EqualTo(new[]
        {
            "Industry (Lee) queued 40 Plastic for Gizmo.",
            "Industry (Moss) queued 40 Plastic for Gizmo.",
        }));
    }

    [Test]
    public void TwoIdenticalChainedJobs_BothAreLogged()
    {
        var world = NewWorld();
        var colony = Colony(world);
        SeatHead(world, colony, "Lee");

        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        Assert.That(IndustryLog(colony), Has.Count.EqualTo(2));
        Assert.That(Jobs(colony, RefineryId), Has.Count.EqualTo(2));
    }

    [Test]
    public void AChainedJob_NoLineAccepts_IsNotLogged()
    {
        var world = NewWorld();
        var colony = Colony(world);
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines.Remove(RefineryId);
        SeatHead(world, colony);

        Queue(world, colony, FactoryId, world.Part.UniqueID);

        Assert.That(Jobs(colony, FactoryId), Has.Count.EqualTo(1));
        Assert.That(IndustryLog(colony), Is.Empty);
    }

    // ----- the missing-inputs view (slice 5) -----

    static IndustryView View(Entity colony, int? viewer = null)
        => new GameProjector(Game).ProjectEntity(colony, viewer ?? colony.FactionOwnerID).GetView<IndustryView>()!;

    static IReadOnlyList<MissingInputView> Missing(Entity colony, string lineId, int index = 0)
        => View(colony).ProductionLines.Single(l => l.Id == lineId).Jobs[index].MissingInputs;

    static void Stock(World world, Entity colony, string cargoId, long units)
        => Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(world.Info.Data.CargoGoods.GetAny(cargoId)!, units), Is.EqualTo(units));

    [Test]
    public void MissingRawResource_IsListedButNotBuildable()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, RefineryId, "plastic"); // plastic needs 1 hydrocarbons; none on hand

        var missing = Missing(colony, RefineryId).Single();

        Assert.That(missing.ItemId, Is.EqualTo("hydrocarbons"));
        Assert.That(missing.Name, Is.EqualTo("Hydrocarbons"));
        Assert.That((missing.Required, missing.InStock, missing.Short), Is.EqualTo((1L, 0L, 1L)));
        Assert.That(missing.CanBuild, Is.False);
        Assert.That(missing.DesignId, Is.Empty);
        Assert.That(missing.LineId, Is.Empty);
        Assert.That(missing.LineKind, Is.Empty);
        Assert.That(missing.QueueQuantity, Is.EqualTo(0));
    }

    [Test]
    public void MissingPlastic_IsBuildableAtTheRefinery()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        var missing = Missing(colony, FactoryId).Single();

        Assert.That(missing.ItemId, Is.EqualTo("plastic"));
        Assert.That(missing.Name, Is.EqualTo("Plastic"));
        Assert.That((missing.Required, missing.InStock, missing.Short), Is.EqualTo((40L, 0L, 40L)));
        Assert.That(missing.CanBuild, Is.True);
        Assert.That(missing.DesignId, Is.EqualTo("plastic"));
        Assert.That(missing.LineId, Is.EqualTo(RefineryId));
        Assert.That(missing.LineName, Is.EqualTo("Refinery"));
        Assert.That(missing.LineKind, Is.EqualTo(IndustryLineKinds.Refinery));
        Assert.That(missing.QueueQuantity, Is.EqualTo(40));
    }

    [Test]
    public void MissingComponent_IsBuildableAtTheFactory_NotTheShipyard()
    {
        var world = NewWorld();
        var colony = Colony(world);
        // A leftover component rate on the yard must not win, as in chaining.
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines[YardId].IndustryTypeRates["component-construction"] = 1000;
        Queue(world, colony, YardId, world.Hull.UniqueID);

        var missing = Missing(colony, YardId).Single();

        Assert.That(missing.ItemId, Is.EqualTo(world.Part.UniqueID));
        Assert.That(missing.Name, Is.EqualTo("Gizmo"));
        Assert.That(missing.CanBuild, Is.True);
        Assert.That(missing.DesignId, Is.EqualTo(world.Part.UniqueID));
        Assert.That(missing.LineId, Is.EqualTo(FactoryId));
        Assert.That(missing.LineKind, Is.EqualTo(IndustryLineKinds.Factory));
        Assert.That(missing.QueueQuantity, Is.EqualTo(1));
    }

    [Test]
    public void AShipyardLine_IsReportedAsAShipyard()
    {
        var world = NewWorld();
        var colony = Colony(world);
        // An input whose industry type is ship assembly, to exercise the kind (stock hulls are not inputs in real data).
        var slipItem = new ComponentDesign
        {
            UniqueID = "chain-slip-" + Guid.NewGuid().ToString("N"),
            Name = "Slip Frame",
            IndustryTypeID = "ship-assembly",
            ComponentMountType = ComponentMountType.ShipComponent,
            CargoTypeID = "general-storage",
            MassPerUnit = 1,
            VolumePerUnit = 1,
            IndustryPointCosts = 1,
        };
        world.Info.IndustryDesigns[slipItem.UniqueID] = slipItem;
        world.Part.ResourceCosts.Clear();
        world.Part.ResourceCosts[slipItem.UniqueID] = 1;
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        var missing = Missing(colony, FactoryId).Single();

        Assert.That(missing.LineId, Is.EqualTo(YardId));
        Assert.That(missing.LineKind, Is.EqualTo(IndustryLineKinds.Shipyard));
    }

    [Test]
    public void StockOnHand_ReducesTheShortfall()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Stock(world, colony, "plastic", 15);
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        var missing = Missing(colony, FactoryId).Single();

        Assert.That((missing.Required, missing.InStock, missing.Short), Is.EqualTo((40L, 15L, 25L)));
        Assert.That(missing.QueueQuantity, Is.EqualTo(25));
    }

    [Test]
    public void NothingMissing_GivesAnEmptyList()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Stock(world, colony, "plastic", 40);
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        Assert.That(Missing(colony, FactoryId), Is.Empty);

        // A job with no inputs at all is empty too.
        world.Part.ResourceCosts.Clear();
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Assert.That(Missing(colony, FactoryId, 1), Is.Empty);
    }

    [Test]
    public void AnInputNoLineTakes_IsListedAsNotBuildableHere()
    {
        var world = NewWorld();
        var colony = Colony(world);
        colony.GetDataBlob<IndustryAbilityDB>().ProductionLines.Remove(RefineryId);
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        var missing = Missing(colony, FactoryId).Single();

        Assert.That(missing.ItemId, Is.EqualTo("plastic"));
        Assert.That(missing.Short, Is.EqualTo(40));
        Assert.That(missing.CanBuild, Is.False, "this colony has no refinery");
        Assert.That(missing.LineId, Is.Empty);
    }

    [Test]
    public void QueueQuantity_IsCappedAtWhatAJobCanHold()
    {
        var world = NewWorld();
        var colony = Colony(world);
        world.Part.ResourceCosts["plastic"] = 100_000;
        Queue(world, colony, FactoryId, world.Part.UniqueID);

        var missing = Missing(colony, FactoryId).Single();

        Assert.That(missing.Short, Is.EqualTo(100_000));
        Assert.That(missing.QueueQuantity, Is.EqualTo(ushort.MaxValue));
    }

    [Test]
    public void TheQuantityAndLine_MatchWhatChainingQueues()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Stock(world, colony, "plastic", 15);
        Queue(world, colony, FactoryId, world.Part.UniqueID); // no head: only itself
        var missing = Missing(colony, FactoryId).Single();
        Assert.That(Jobs(colony, RefineryId), Is.Empty);

        SeatHead(world, colony);
        Queue(world, colony, FactoryId, world.Part.UniqueID); // a head: chains the same shortfall

        var chained = Jobs(colony, RefineryId).Single();
        Assert.That(chained.ItemGuid, Is.EqualTo(missing.DesignId));
        Assert.That(chained.NumberOrdered, Is.EqualTo(missing.QueueQuantity));
    }

    [Test]
    public void TheView_CanQueueTheMissingInput_WithTheExistingCommand()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        var missing = Missing(colony, FactoryId).Single();

        var result = new CommandTranslator(Game).Translate(world.Faction, colony,
            new QueueIndustryJobCommand(colony.Id, missing.LineId, missing.DesignId, missing.QueueQuantity, false, false));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var queued = Jobs(colony, RefineryId).Single();
        Assert.That(queued.ItemGuid, Is.EqualTo("plastic"));
        Assert.That(queued.NumberOrdered, Is.EqualTo(40));
        Assert.That(queued.Auto, Is.False);
    }

    [Test]
    public void AForeignViewer_SeesNoIndustryView()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        var other = ForeignFaction.Value;

        Assert.That(new GameProjector(Game).ProjectEntity(colony, other.Id).GetView<IndustryView>(), Is.Null);
    }

    // ----- cancel / priority / edit log lines -----

    static List<string> IndustryLines(Entity colony)
        => colony.GetDataBlob<ShipLogDB>().Entries.Select(e => e.Text).Where(t => t.StartsWith("Industry:")).ToList();

    [Test]
    public void CancellingAJob_WritesALogLine()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        var id = Jobs(colony, FactoryId).Single().JobID;

        var result = new CommandTranslator(Game).Translate(world.Faction, colony, new CancelIndustryJobCommand(colony.Id, FactoryId, id));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(Jobs(colony, FactoryId), Is.Empty);
        Assert.That(IndustryLines(colony), Is.EqualTo(new[]
        {
            "Industry: queued 1 Gizmo on Factory.",
            "Industry: cancelled Gizmo on Factory.",
        }));
    }

    [Test]
    public void MovingAJob_WritesALogLine_OnlyWhenItMoves()
    {
        var world = NewWorld();
        var colony = Colony(world);
        for (int i = 0; i < 3; i++)
            Queue(world, colony, FactoryId, world.Part.UniqueID);
        var last = Jobs(colony, FactoryId)[2].JobID;
        var translator = new CommandTranslator(Game);

        translator.Translate(world.Faction, colony, new ChangeIndustryJobPriorityCommand(colony.Id, FactoryId, last, -1));
        Assert.That(Jobs(colony, FactoryId)[1].JobID, Is.EqualTo(last));
        Assert.That(IndustryLines(colony).Last(), Is.EqualTo("Industry: moved Gizmo up on Factory."));

        translator.Translate(world.Faction, colony, new ChangeIndustryJobPriorityCommand(colony.Id, FactoryId, last, 1));
        Assert.That(Jobs(colony, FactoryId)[2].JobID, Is.EqualTo(last));
        Assert.That(IndustryLines(colony).Last(), Is.EqualTo("Industry: moved Gizmo down on Factory."));

        int lines = IndustryLines(colony).Count;
        translator.Translate(world.Faction, colony, new ChangeIndustryJobPriorityCommand(colony.Id, FactoryId, last, 1));
        Assert.That(IndustryLines(colony), Has.Count.EqualTo(lines), "already last: it does not move, so no line");
    }

    [Test]
    public void EditingAJob_WritesALogLine()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        var id = Jobs(colony, FactoryId).Single().JobID;

        Assert.That(Game.OrderHandler.HandleOrder(IndustryOrder2.CreateEditJobOrder(world.Faction.Id, colony, FactoryId, id, 10, true)), Is.True);
        Assert.That(IndustryLines(colony).Last(), Is.EqualTo("Industry: changed Gizmo on Factory to 10 (repeating)."));

        Assert.That(Game.OrderHandler.HandleOrder(IndustryOrder2.CreateEditJobOrder(world.Faction.Id, colony, FactoryId, id, 3, false)), Is.True);
        Assert.That(IndustryLines(colony).Last(), Is.EqualTo("Industry: changed Gizmo on Factory to 3."));
        var job = Jobs(colony, FactoryId).Single();
        Assert.That((job.NumberOrdered, job.Auto), Is.EqualTo(((ushort)3, false)));
    }

    [Test]
    public void ChangingAJobThatIsGone_WritesNothing()
    {
        var world = NewWorld();
        var colony = Colony(world);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        int lines = IndustryLines(colony).Count;

        new CommandTranslator(Game).Translate(world.Faction, colony, new CancelIndustryJobCommand(colony.Id, FactoryId, "no-such-job"));

        Assert.That(IndustryLines(colony), Has.Count.EqualTo(lines));
    }

    // ----- a head who is gone (dangling seat) -----

    [Test]
    public void DestroyedHead_ClearsTheSeat_AndJobsStopChaining()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var head = SeatHead(world, colony);
        Assert.That(IndustrySeat.HasHead(colony), Is.True);

        CommanderFactory.DestroyCommander(head);

        Assert.That(IndustrySeat.HasHead(colony), Is.False);
        Assert.That(IndustrySeat.HeadId(colony), Is.EqualTo(-1), "the seat is emptied, not left pointing at a dead id");
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Assert.That(Jobs(colony, RefineryId), Is.Empty, "no head, no chaining");
        Assert.That(IndustryLog(colony), Is.Empty);
    }

    [Test]
    public void ADanglingHeadId_CountsAsEmpty()
    {
        var world = NewWorld();
        var colony = Colony(world);
        IndustrySeat.Of(colony).HeadId = 987_654_321; // nobody has this id

        Assert.That(IndustrySeat.HasHead(colony), Is.False);
        Assert.That(IndustrySeat.TryGetHead(colony, out _), Is.False);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Assert.That(Jobs(colony, RefineryId), Is.Empty);
    }

    [Test]
    public void AHeadOfAnotherFaction_CountsAsEmpty()
    {
        var world = NewWorld();
        var colony = Colony(world);
        var other = ForeignFaction.Value;
        var stranger = CommanderFactory.Create(world.System, other.Id, new CommanderDB("Stranger", 1, CommanderTypes.Civilian));
        IndustrySeat.Of(colony).HeadId = stranger.Id;

        Assert.That(IndustrySeat.HasHead(colony), Is.False);
        Assert.That(IndustrySeat.TryGetHead(colony, out _), Is.False);
        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Assert.That(Jobs(colony, RefineryId), Is.Empty);
    }

    [Test]
    public void AfterTheHeadDies_ANewHeadCanBeSeated_AndChainingResumes()
    {
        var world = NewWorld();
        var colony = Colony(world);
        CommanderFactory.DestroyCommander(SeatHead(world, colony, "Old"));

        SeatHead(world, colony, "New");

        Queue(world, colony, FactoryId, world.Part.UniqueID);
        Assert.That(Jobs(colony, RefineryId), Has.Count.EqualTo(1));
        Assert.That(IndustryLog(colony).Single(), Does.StartWith("Industry (New) queued"));
    }
}
