using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

[TestFixture]
public class BootstrapColonyTests
{
    const string Infra = "default-design-infrastructure";

    static readonly ModDataStore Store = LoadStore();
    static Game Game = InitializeGame();
    static int _factionsMade;

    static ModDataStore LoadStore()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        return store;
    }

    static Game InitializeGame()
    {
        var game = new Game(new NewGameSettings { MaxSystems = 4, CreatePlayerFaction = false }, Store);
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
    public void LoadsAtTheSource_MountsFromTheHold_AndFinishesWhenTheCratesAreOnTheColony()
    {
        var scene = Build(crates: 2);
        var goal = scene.Goal(Infra, 2);

        var plan = new BootstrapColonyPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.Actions.OfType<LoadGoodsAction>().Count(), Is.EqualTo(1));
        Assert.That(goal.BootstrapCrateIds, Is.EquivalentTo(scene.Crates.Select(crate => crate.ID)));

        // A second plan with the load queued must not load twice or finish.
        var load = plan.Actions.OfType<LoadGoodsAction>().Single();
        load.ParentGoalId = goal.Id;
        scene.Ship.GetDataBlob<ActionQueueDB>().Enqueue(load);
        plan = new BootstrapColonyPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.Actions, Is.Empty);
        scene.Ship.GetDataBlob<ActionQueueDB>().ActionList.Remove(load);

        scene.Load();
        plan = new BootstrapColonyPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var mount = plan.Actions.OfType<MountFromHoldAction>().Single();
        Assert.That(plan.Actions, Has.Count.EqualTo(1));

        mount.ParentGoalId = goal.Id;
        scene.Ship.GetDataBlob<ActionQueueDB>().Enqueue(mount);
        Assert.That(mount.IsValidCommand(Game), Is.True);
        Assert.That(mount.IsFinished(), Is.False);
        mount.Execute(scene.Ship.StarSysDateTime);
        Assert.That(mount.Status, Is.EqualTo(ActionStatus.Running), mount.FailureMessage);
        Assert.That(scene.Bay.BuildQueue.Select(job => job.Instance), Is.EquivalentTo(scene.Crates));
        Assert.That(scene.Bay.BuildQueue.All(job => job.TargetEntityId == scene.Dest.Id), Is.True);
        Assert.That(mount.IsFinished(), Is.False);

        plan = new BootstrapColonyPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.Actions, Is.Empty);

        new LocalConstructionProcessor().ProcessEntity(scene.Ship, 0);

        Assert.That(mount.IsFinished(), Is.True);
        Assert.That(mount.Status, Is.Not.EqualTo(ActionStatus.Failed));
        Assert.That(scene.Bay.BuildQueue, Is.Empty);
        Assert.That(Mounted(scene.Dest), Is.EquivalentTo(scene.Crates));
        Assert.That(scene.Ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(scene.Crates[0], false), Is.EqualTo(0));

        scene.Ship.GetDataBlob<ActionQueueDB>().ActionList.Remove(mount);
        plan = new BootstrapColonyPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed));
        Assert.That(plan.Message, Is.EqualTo("Mounted 2 crates"));
    }

    [Test]
    public void OnlyTheCratesThisGoalPicked_CountAsDone()
    {
        var scene = Build(crates: 1);
        var elsewhere = new ComponentInstance(scene.Info.ComponentDesigns[Infra]);
        scene.Dest.AddComponent(elsewhere);
        var goal = scene.Goal(Infra, 1);

        var plan = new BootstrapColonyPlan().Plan(scene.Ship, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(goal.BootstrapCrateIds, Is.EqualTo(new[] { scene.Crates[0].ID }));
    }

    [Test]
    public void ShipWithoutABay_FailsClearly()
    {
        var scene = Build(crates: 1);
        var freighter = ShipFactory.CreateShip(
            scene.Info.ShipDesigns["default-ship-design-freighter"], scene.Faction, scene.Planet, "Freighter");

        var plan = new BootstrapColonyPlan().Plan(freighter, scene.Goal(Infra, 1), freighter.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo("The ship has no Construction Bay."));
    }

    [Test]
    public void MissingCrates_AreNamed()
    {
        var scene = Build(crates: 1);

        var plan = new BootstrapColonyPlan().Plan(scene.Ship, scene.Goal(Infra, 2), scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo("no finished crates of " + scene.Info.ComponentDesigns[Infra].Name + " at source"));
    }

    [Test]
    public void ASiteOnlyDesign_IsNotACrate()
    {
        var scene = Build(crates: 1);
        var cityHall = scene.Info.ComponentDesigns["default-design-city-hall"];

        var plan = new BootstrapColonyPlan().Plan(
            scene.Ship, scene.Goal("default-design-city-hall", 1), scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo(cityHall.Name + " is not a crate-able facility."));
    }

    [Test]
    public void CratesThatDoNotFitTheHold_FailTheGoal()
    {
        var scene = Build(crates: 2);
        var hold = scene.Ship.GetDataBlob<CargoStorageDB>();
        double one = scene.Crates[0].VolumePerUnit;
        hold.TypeStores["general-storage"] = new TypeStore(one * 1.5);

        var plan = new BootstrapColonyPlan().Plan(scene.Ship, scene.Goal(Infra, 2), scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo("doesn't fit hold"));
    }

    [Test]
    public void Mounting_FailsWhenTheColonyIsOutOfTransferRange_AndLeavesNothingQueued()
    {
        var scene = Build(crates: 2);
        scene.Load();
        scene.Ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 0;
        scene.Dest.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 0;
        var mount = MountFromHoldAction.Create(scene.Ship, scene.Dest.Id, scene.Crates.Select(crate => crate.ID).ToList());

        Assert.That(mount.IsValidCommand(Game), Is.True);
        mount.Execute(scene.Ship.StarSysDateTime);

        Assert.That(mount.Status, Is.EqualTo(ActionStatus.Failed));
        Assert.That(mount.FailureMessage, Is.EqualTo("The colony is out of transfer range."));
        Assert.That(mount.IsFinished(), Is.True);
        Assert.That(scene.Bay.BuildQueue, Is.Empty);
    }

    [Test]
    public void Mounting_FailsWhenARangeIsLostMidway()
    {
        var scene = Build(crates: 2);
        scene.Load();
        var mount = MountFromHoldAction.Create(scene.Ship, scene.Dest.Id, scene.Crates.Select(crate => crate.ID).ToList());
        Assert.That(mount.IsValidCommand(Game), Is.True);
        mount.Execute(scene.Ship.StarSysDateTime);
        Assert.That(mount.Status, Is.EqualTo(ActionStatus.Running), mount.FailureMessage);

        scene.Dest.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 0;
        scene.Ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 0;

        Assert.That(mount.IsFinished(), Is.True);
        Assert.That(mount.Status, Is.EqualTo(ActionStatus.Failed));
        Assert.That(mount.FailureMessage, Is.EqualTo("The colony is out of transfer range."));
        Assert.That(scene.Bay.BuildQueue, Is.Empty);
    }

    [Test]
    public void Mounting_FailsWhenACrateLeavesTheHold()
    {
        var scene = Build(crates: 2);
        scene.Load();
        var mount = MountFromHoldAction.Create(scene.Ship, scene.Dest.Id, scene.Crates.Select(crate => crate.ID).ToList());
        Assert.That(mount.IsValidCommand(Game), Is.True);
        mount.Execute(scene.Ship.StarSysDateTime);

        scene.Ship.GetDataBlob<CargoStorageDB>().RemoveCargoByUnit(scene.Crates[1], 1);

        Assert.That(mount.IsFinished(), Is.True);
        Assert.That(mount.Status, Is.EqualTo(ActionStatus.Failed));
        Assert.That(mount.FailureMessage, Is.EqualTo("A crate is missing from the hold."));
        Assert.That(scene.Bay.BuildQueue, Is.Empty);
    }

    [Test]
    public void Mounting_FailsWhenTheColonyIsGone()
    {
        var scene = Build(crates: 1);
        scene.Load();
        var mount = MountFromHoldAction.Create(scene.Ship, scene.Dest.Id, new[] { scene.Crates[0].ID });
        Assert.That(mount.IsValidCommand(Game), Is.True);
        mount.Execute(scene.Ship.StarSysDateTime);
        Assert.That(mount.Status, Is.EqualTo(ActionStatus.Running), mount.FailureMessage);

        scene.Dest.Destroy();

        Assert.That(mount.IsFinished(), Is.True);
        Assert.That(mount.Status, Is.EqualTo(ActionStatus.Failed));
        Assert.That(mount.FailureMessage, Is.EqualTo("The colony is gone."));
    }

    [Test]
    public void Translator_AssignsTheGoal_OnACaptainedShipInReach()
    {
        var scene = Build(crates: 1);
        var result = scene.Translate(scene.Ship, scene.Command(Infra, 1));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var goal = scene.Ship.GetDataBlob<GoalsDB>().GivenGoal;
        Assert.That(goal, Is.Not.Null);
        Assert.That(goal!.Type, Is.EqualTo(GoalType.BootstrapColony));
        Assert.That(goal.SourceEntityId, Is.EqualTo(scene.Source.Id));
        Assert.That(goal.DestEntityId, Is.EqualTo(scene.Dest.Id));
        Assert.That(goal.BootstrapCrates.Single().DesignId, Is.EqualTo(Infra));
        Assert.That(goal.BootstrapCrates.Single().Count, Is.EqualTo(1));
    }

    [Test]
    public void Translator_RejectsAShipWithoutACaptain()
    {
        var scene = Build(crates: 1);
        scene.Ship.GetDataBlob<ShipInfoDB>().CommanderID = -1;

        var result = scene.Translate(scene.Ship, scene.Command(Infra, 1));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Does.Contain("captain"));
    }

    [Test]
    public void Translator_RejectsAShipWithoutABay()
    {
        var scene = Build(crates: 1);
        var freighter = ShipFactory.CreateShip(
            scene.Info.ShipDesigns["default-ship-design-freighter"], scene.Faction, scene.Planet, "Freighter");
        freighter.GetDataBlob<ShipInfoDB>().CommanderID = 1;

        var result = scene.Translate(freighter, new BootstrapColonyCommand(
            freighter.Id, scene.Source.Id, scene.Dest.Id, new[] { new BootstrapCrateRequest(Infra, 1) }));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("The ship has no Construction Bay."));
    }

    [Test]
    public void Translator_RejectsASiteOnlyDesign_AndBadCounts_AndSameColony()
    {
        var scene = Build(crates: 1);
        var cityHall = scene.Info.ComponentDesigns["default-design-city-hall"];

        var siteOnly = scene.Translate(scene.Ship, scene.Command("default-design-city-hall", 1));
        Assert.That(siteOnly.Accepted, Is.False);
        Assert.That(siteOnly.RejectionReason, Is.EqualTo(cityHall.Name + " is not a crate-able facility."));

        var zero = scene.Translate(scene.Ship, scene.Command(Infra, 0));
        Assert.That(zero.Accepted, Is.False);

        var same = scene.Translate(scene.Ship, new BootstrapColonyCommand(
            scene.Ship.Id, scene.Source.Id, scene.Source.Id, new[] { new BootstrapCrateRequest(Infra, 1) }));
        Assert.That(same.Accepted, Is.False);
        Assert.That(same.RejectionReason, Is.EqualTo("Pick two different colonies."));

        var none = scene.Translate(scene.Ship, new BootstrapColonyCommand(
            scene.Ship.Id, scene.Source.Id, scene.Dest.Id, new List<BootstrapCrateRequest>()));
        Assert.That(none.Accepted, Is.False);

        Assert.That(scene.Ship.TryGetDataBlob<GoalsDB>(out var goals) && goals.GivenGoal != null, Is.False);
    }

    [Test]
    public void Translator_RejectsAColonyThatIsNotYours()
    {
        var scene = Build(crates: 1);
        var other = FactionFactory.CreateFaction(Game, "Others-" + Guid.NewGuid().ToString("N"));
        var foreign = scene.ColonyAt(scene.Planet, other, "Foreign Colony");

        var result = scene.Translate(scene.Ship, new BootstrapColonyCommand(
            scene.Ship.Id, scene.Source.Id, foreign.Id, new[] { new BootstrapCrateRequest(Infra, 1) }));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("The colony is not yours."));
    }

    [Test]
    public void Translator_RejectsADestinationOutsideTheShipsReach()
    {
        var scene = Build(crates: 1);
        var farPlanet = scene.NewPlanet();
        var far = scene.ColonyAt(farPlanet, scene.Faction, "Far Colony");

        var result = scene.Translate(scene.Ship, new BootstrapColonyCommand(
            scene.Ship.Id, scene.Source.Id, far.Id, new[] { new BootstrapCrateRequest(Infra, 1) }));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("The target is outside this ship's reach."));
    }

    [Test]
    public void ASiteOnlyDesign_CannotBeFreighted_ButACrateAndAColonyWarehouseStillWork()
    {
        var scene = Build(crates: 1);
        var cityHall = scene.Info.ComponentDesigns["default-design-city-hall"];
        Assert.That(LocalConstructionJob.IsSiteOnly(cityHall), Is.True);
        var siteOnly = new ComponentInstance(cityHall);
        var hold = scene.Ship.GetDataBlob<CargoStorageDB>();
        var warehouse = scene.Source.GetDataBlob<CargoStorageDB>();

        // The colony warehouse still takes it (factory output, uninstall), the ship's hold does not.
        Assert.That(warehouse.AddCargoByUnit(siteOnly, 1), Is.EqualTo(1));
        Assert.That(hold.AddCargoByUnit(new ComponentInstance(cityHall), 1), Is.EqualTo(0));
        Assert.That(hold.AddRemoveCargoByMass(new ComponentInstance(cityHall), 1), Is.EqualTo(0));
        Assert.That(hold.AddRemoveCargoByVolume(new ComponentInstance(cityHall), 1), Is.EqualTo(0));
        Assert.That(hold.GetFreeUnitSpace(siteOnly), Is.EqualTo(0));
        Assert.That(hold.GetFreeVolume(siteOnly), Is.EqualTo(0));

        // The load path clamps it to nothing, so a load line for it moves no units.
        var lines = new List<MoveGoodsLine> { new() { ItemId = siteOnly.ID, Requested = 1 } };
        Assert.That(MoveGoodsPlan.ClampLines(scene.Source, scene.Ship, lines, useLoaded: false), Is.Empty);

        // A crate still loads.
        var crateLines = new List<MoveGoodsLine> { new() { ItemId = scene.Crates[0].ID, Requested = 1 } };
        Assert.That(MoveGoodsPlan.ClampLines(scene.Source, scene.Ship, crateLines, useLoaded: false), Has.Count.EqualTo(1));
        Assert.That(hold.AddCargoByUnit(scene.Crates[0], 1), Is.EqualTo(1));
    }

    [Test]
    public void Projection_MarksCrateRowsWithTheirDesign_AndShipsWithABay()
    {
        var scene = Build(crates: 1);
        scene.Load();
        var freighter = ShipFactory.CreateShip(
            scene.Info.ShipDesigns["default-ship-design-freighter"], scene.Faction, scene.Planet, "Freighter");

        var projector = new GameProjector(Game);
        var entity = projector.ProjectEntity(scene.Ship, scene.Faction.Id);
        var row = entity.GetView<CargoStorageView>()!.Stores.SelectMany(store => store.Items)
            .Single(item => item.Id == scene.Crates[0].ID);
        Assert.That(row.IsCrate, Is.True);
        Assert.That(row.DesignId, Is.EqualTo(Infra));

        var root = scene.Faction.GetDataBlob<FleetDB>();
        root.AddChild(scene.Ship);
        root.AddChild(freighter);
        var ships = projector.ProjectFleetHierarchy(scene.Faction.Id);
        var all = ships.UnattachedShips.Concat(ships.Fleets.SelectMany(AllShips)).ToList();
        Assert.That(all.Single(ship => ship.Id == scene.Ship.Id).HasConstructionBay, Is.True);
        Assert.That(all.Single(ship => ship.Id == freighter.Id).HasConstructionBay, Is.False);
    }

    const string Factory = "default-design-factory";

    static Goal FleetGoal(Scene scene, params (string Design, int Count)[] list) => new(GoalType.FleetBootstrap)
    {
        Name = "Bootstrap",
        SourceEntityId = scene.Source.Id,
        DestEntityId = scene.Dest.Id,
        TargetEntityID = scene.Dest.Id,
        BootstrapCrates = list.Select(line => new BootstrapCrateLine { DesignId = line.Design, Count = line.Count }).ToList(),
    };

    static ComponentInstance Stock(Scene scene, string designId)
    {
        var crate = new ComponentInstance(scene.Info.ComponentDesigns[designId]);
        Assert.That(scene.Source.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));
        return crate;
    }

    [Test]
    public void FleetBootstrap_HandsEveryCrateToAShipInASubFleet_AsAPrefilledShipGoal()
    {
        var scene = Build(crates: 2);
        var outer = FleetFactory.Create(scene.System, scene.Faction.Id, "Outer");
        var inner = FleetFactory.Create(scene.System, scene.Faction.Id, "Inner");
        inner.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        outer.GetDataBlob<FleetDB>().AddChild(inner);
        var goal = FleetGoal(scene, (Infra, 2));

        var plan = new FleetBootstrapPlan().Plan(outer, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var (ship, trip) = plan.SubGoals.Single();
        Assert.That(ship.Id, Is.EqualTo(scene.Ship.Id));
        Assert.That(trip.Type, Is.EqualTo(GoalType.BootstrapColony));
        Assert.That(trip.ParentGoalId, Is.EqualTo(goal.Id));
        Assert.That(trip.DestEntityId, Is.EqualTo(scene.Dest.Id));
        Assert.That(trip.BootstrapCrateIds, Is.EquivalentTo(scene.Crates.Select(crate => crate.ID)));
        Assert.That(goal.BootstrapAssigned.Single().Count, Is.EqualTo(2));
    }

    [Test]
    public void FleetBootstrap_TakesCratesInPriorityOrder_ASmallHoldTakesTheTopOfTheList_ThenTheNextTrip()
    {
        var scene = Build(crates: 1);
        var factory = Stock(scene, Factory);
        var hold = scene.Ship.GetDataBlob<CargoStorageDB>();
        // Room for the factory crate and not a crumb more than half of the smaller one.
        hold.TypeStores["general-storage"] = new TypeStore(factory.VolumePerUnit + Math.Min(factory.VolumePerUnit, scene.Crates[0].VolumePerUnit) * 0.5);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        // Factory is on top, so it rides first even though the infrastructure crate was stocked first.
        var goal = FleetGoal(scene, (Factory, 1), (Infra, 1));

        var plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.SubGoals.Single().Goal.BootstrapCrateIds, Is.EqualTo(new[] { factory.ID }));

        // The ship comes back empty and free. The next plan sends the rest.
        plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.SubGoals.Single().Goal.BootstrapCrateIds, Is.EqualTo(new[] { scene.Crates[0].ID }));
        Assert.That(FleetBootstrapPlan.Remaining(goal), Is.Empty);
    }

    [Test]
    public void FleetBootstrap_WithNoShipAbleToTakeIt_FailsAndSaysWhy()
    {
        var scene = Build(crates: 1);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        scene.Ship.GetDataBlob<ShipInfoDB>().CommanderID = -1;
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);

        var plan = new FleetBootstrapPlan().Plan(fleet, FleetGoal(scene, (Infra, 1)), scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo("no ship"));
    }

    [Test]
    public void FleetBootstrap_ShortOfCrates_FailsNamingTheDesign()
    {
        var scene = Build(crates: 1);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);

        var plan = new FleetBootstrapPlan().Plan(fleet, FleetGoal(scene, (Factory, 1)), scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Does.Contain("no finished crates of").And.Contain(scene.Info.ComponentDesigns[Factory].Name));
    }

    [Test]
    public void FleetBootstrap_ATripThatFailed_GivesItsCratesBack_AndBenchesThatShip()
    {
        var scene = Build(crates: 1);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        var goal = FleetGoal(scene, (Infra, 1));
        var plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);
        var trip = plan.SubGoals.Single().Goal;
        Assert.That(goal.BootstrapAssigned.Single().Count, Is.EqualTo(1));

        trip.Status = GoalStatus.Failed;
        trip.Message = "out of range";
        scene.Ship.SetDataBlob(new GoalsDB { ActiveGoal = trip, GivenGoal = trip });
        plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);

        // The crate was not mounted, so it is owed again. The only ship failed this goal, so nothing is left to carry it.
        Assert.That(goal.BootstrapAssigned.Single().Count, Is.EqualTo(0));
        Assert.That(goal.BootstrapCrateIds, Is.Empty);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo("out of range"));
    }

    [Test]
    public void FleetBootstrap_EverythingHandedOutAndNothingInFlight_IsDone()
    {
        var scene = Build(crates: 2);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        var goal = FleetGoal(scene, (Infra, 2));
        goal.BootstrapAssigned.Add(new BootstrapCrateLine { DesignId = Infra, Count = 2 });

        var plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed));
        Assert.That(plan.Message, Is.EqualTo("Mounted 2 crates"));
    }

    const string CityHall = "default-design-city-hall";

    /// <summary>A fleet goal whose crates all went over, wanting the listed site-only facilities built.</summary>
    static Goal ResourceGoal(Scene scene, int cityHalls)
    {
        var goal = FleetGoal(scene, (Infra, 1));
        goal.BootstrapAssigned.Add(new BootstrapCrateLine { DesignId = Infra, Count = 1 });
        goal.BootstrapSiteBuilds.Add(new BootstrapCrateLine { DesignId = CityHall, Count = cityHalls });
        return goal;
    }

    static void StockResources(Scene scene, Dictionary<string, long> amounts)
    {
        foreach (var (cargoId, amount) in amounts)
            Assert.That(scene.Source.GetDataBlob<CargoStorageDB>()
                .AddCargoByUnit(scene.Info.Data.CargoGoods.GetAny(cargoId)!, amount), Is.EqualTo(amount));
    }

    [Test]
    public void BootstrapResources_AreTheFacilitysConstructionCost_TimesTheQuantity_SummedAcrossFacilities()
    {
        var scene = Build(crates: 0);
        var hall = scene.Info.ComponentDesigns[CityHall];
        Assert.That(hall.ResourceCosts, Is.Not.Empty, "the test needs a design with a construction cost");

        var one = BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 1 } });
        var three = BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 3 } });
        Assert.That(one, Is.EquivalentTo(hall.ResourceCosts.Where(c => c.Value > 0)));
        foreach (var (cargoId, amount) in one)
            Assert.That(three[cargoId], Is.EqualTo(amount * 3));

        // Two listings of the same cost add up, and a design the faction does not have adds nothing.
        var twice = BootstrapResources.Derive(scene.Info, new[]
        {
            new BootstrapCrateLine { DesignId = CityHall, Count = 1 },
            new BootstrapCrateLine { DesignId = CityHall, Count = 2 },
            new BootstrapCrateLine { DesignId = "no-such-design", Count = 5 },
        });
        Assert.That(twice, Is.EquivalentTo(three));
    }

    [Test]
    public void FleetBootstrap_HaulsTheResourcesTheSiteBuildsCost_OnlyAfterTheCratesHaveArrived()
    {
        var scene = Build(crates: 1);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        var needed = BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 2 } });
        StockResources(scene, needed);

        // Crates first: the crate is still at the source, so the ship is handed the crate and no resources.
        var goal = FleetGoal(scene, (Infra, 1));
        goal.BootstrapSiteBuilds.Add(new BootstrapCrateLine { DesignId = CityHall, Count = 2 });
        var plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.SubGoals.Single().Goal.Type, Is.EqualTo(GoalType.BootstrapColony));
        Assert.That(goal.BootstrapHauled, Is.Empty);

        // The crate trip is under way, so the resources keep waiting for it.
        scene.Ship.SetDataBlob(new GoalsDB { ActiveGoal = plan.SubGoals.Single().Goal, GivenGoal = plan.SubGoals.Single().Goal });
        plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.SubGoals, Is.Empty);
        Assert.That(goal.BootstrapHauled, Is.Empty);

        // The crate arrived: the ship is sent for the resources the two facilities cost.
        scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!.Status = GoalStatus.Completed;
        plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);
        var (ship, haul) = plan.SubGoals.Single();
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(ship.Id, Is.EqualTo(scene.Ship.Id));
        Assert.That(haul.Type, Is.EqualTo(GoalType.MoveGoods));
        Assert.That(haul.ParentGoalId, Is.EqualTo(goal.Id));
        Assert.That(haul.SourceEntityId, Is.EqualTo(scene.Source.Id));
        Assert.That(haul.DestEntityId, Is.EqualTo(scene.Dest.Id));
        Assert.That(haul.MoveGoodsLines, Is.Not.Empty);
        foreach (var line in haul.MoveGoodsLines)
        {
            var cargo = scene.Info.Data.CargoGoods.GetAny(line.ItemId);
            Assert.That(line.Requested, Is.LessThanOrEqualTo(needed[cargo.UniqueID]));
            Assert.That(goal.BootstrapHauled[cargo.UniqueID], Is.EqualTo(line.Requested));
        }
    }

    [Test]
    public void FleetBootstrap_AHaulThatDeliveredLess_OwesTheRest_AndTheGoalIsDoneOnceItAllArrived()
    {
        var scene = Build(crates: 0);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        var needed = BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 1 } });
        StockResources(scene, needed);
        var goal = ResourceGoal(scene, cityHalls: 1);

        var plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);
        var haul = plan.SubGoals.Single().Goal;
        long handed = haul.MoveGoodsLines.Sum(line => line.Requested);
        Assert.That(handed, Is.GreaterThan(0));

        // The trip finished having delivered nothing of one line: that line is owed again.
        var owed = haul.MoveGoodsLines[0];
        haul.MoveGoodsLines.ForEach(line => line.Delivered = line.Requested);
        owed.Delivered = 0;
        haul.Status = GoalStatus.Completed;
        scene.Ship.SetDataBlob(new GoalsDB { ActiveGoal = haul, GivenGoal = haul });
        plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);

        var again = plan.SubGoals.Single().Goal;
        Assert.That(again.MoveGoodsLines.Select(l => l.ItemId), Does.Contain(owed.ItemId));
        Assert.That(goal.BootstrapHauled.Values.Sum(), Is.EqualTo(handed));

        // Everything delivered and nothing in flight: the order is done.
        again.MoveGoodsLines.ForEach(line => line.Delivered = line.Requested);
        again.Status = GoalStatus.Completed;
        goal.BootstrapHauled.Clear();
        foreach (var (cargoId, amount) in needed)
            goal.BootstrapHauled[cargoId] = amount;
        scene.Ship.SetDataBlob(new GoalsDB { ActiveGoal = again, GivenGoal = again });
        plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed), plan.Message);
    }

    [Test]
    public void FleetBootstrap_ShortOfAResourceAtTheSource_FailsNamingIt()
    {
        var scene = Build(crates: 0);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        var goal = ResourceGoal(scene, cityHalls: 1);

        var plan = new FleetBootstrapPlan().Plan(fleet, goal, scene.Ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Does.StartWith("not enough ").And.EndWith(" at source"));
    }

    [Test]
    public void FleetBootstrapCommand_TakesSiteBuilds_AndRefusesACrateOrABadCount()
    {
        var scene = Build(crates: 1);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        CommandResult Send(params BootstrapCrateRequest[] builds)
            => scene.Translate(fleet, new FleetBootstrapCommand(fleet.Id, scene.Source.Id, scene.Dest.Id,
                new[] { new BootstrapCrateRequest(Infra, 1) }, builds));

        Assert.That(Send(new BootstrapCrateRequest(Infra, 1)).RejectionReason, Does.Contain("not a site-only"));
        Assert.That(Send(new BootstrapCrateRequest(CityHall, 0)).RejectionReason, Is.EqualTo("Site build counts must be positive."));
        Assert.That(Send(new BootstrapCrateRequest("no-such-design", 1)).Accepted, Is.False);

        var result = Send(new BootstrapCrateRequest(CityHall, 1), new BootstrapCrateRequest(CityHall, 2));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var goal = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(goal.BootstrapSiteBuilds.Select(l => (l.DesignId, l.Count)), Is.EqualTo(new[] { (CityHall, 3) }));
    }

    [Test]
    public void BootstrapSiteBuildList_ShowsTheDerivedResources_AsCostTimesCount()
    {
        var designs = new ComponentDesignsSnapshot
        {
            Designs = new[]
            {
                new ComponentDesignSummary("hall", "Hall", "t", "T")
                {
                    IsSiteOnly = true,
                    ResourceCosts = new[] { new ResourceCostLine("iron", "Iron", 10), new ResourceCostLine("steel", "Steel", 4) },
                },
                new ComponentDesignSummary("tower", "Tower", "t", "T")
                {
                    IsSiteOnly = true,
                    ResourceCosts = new[] { new ResourceCostLine("iron", "Iron", 5) },
                },
                new ComponentDesignSummary("crate", "Crate", "t", "T") { IsCrate = true },
            },
        };
        var list = new BootstrapSiteBuildList();

        Assert.That(list.Addable(designs).Select(d => d.Id), Is.EqualTo(new[] { "hall", "tower" }), "crates are not site builds");
        Assert.That(list.Add("hall", "Hall", 3), Is.True);
        Assert.That(list.Add("tower", "Tower", 2), Is.True);
        Assert.That(list.Add("hall", "Hall", 0), Is.False);

        var totals = list.DerivedResources(designs);
        Assert.That(totals.Select(t => (t.Name, t.Amount)), Is.EqualTo(new[] { ("Iron", 40L), ("Steel", 12L) }));
        Assert.That(list.Requests().Select(r => (r.DesignId, r.Count)), Is.EqualTo(new[] { ("hall", 3), ("tower", 2) }));
        list.Remove(0);
        Assert.That(list.DerivedResources(designs).Single().Amount, Is.EqualTo(10));
    }

    [Test]
    public void FleetBootstrapCommand_GoesToTheShipsOfTheFleet_AndTheShipFollowsTheFleetsGoal()
    {
        var scene = Build(crates: 2);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        var command = new FleetBootstrapCommand(fleet.Id, scene.Source.Id, scene.Dest.Id,
            new[] { new BootstrapCrateRequest(Infra, 1), new BootstrapCrateRequest(Infra, 1) });

        var result = scene.Translate(fleet, command);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var fleetGoal = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(fleetGoal.Type, Is.EqualTo(GoalType.FleetBootstrap));
        // Two lines for one design merge into one, quantity two.
        Assert.That(fleetGoal.BootstrapCrates.Select(line => (line.DesignId, line.Count)), Is.EqualTo(new[] { (Infra, 2) }));
        var shipGoal = scene.Ship.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(shipGoal.Type, Is.EqualTo(GoalType.BootstrapColony));
        Assert.That(shipGoal.ParentGoalId, Is.EqualTo(fleetGoal.Id));
        Assert.That(shipGoal.BootstrapCrateIds, Is.EquivalentTo(scene.Crates.Select(crate => crate.ID)));
    }

    [Test]
    public void FleetBootstrapCommand_IsRefused_ForAShip_ForASiteOnlyDesign_AndForAnEmptyList()
    {
        var scene = Build(crates: 1);
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        CommandResult Send(Entity target, params BootstrapCrateRequest[] lines)
            => scene.Translate(target, new FleetBootstrapCommand(target.Id, scene.Source.Id, scene.Dest.Id, lines));

        Assert.That(Send(scene.Ship, new BootstrapCrateRequest(Infra, 1)).Accepted, Is.False);
        Assert.That(Send(fleet, new BootstrapCrateRequest("default-design-city-hall", 1)).RejectionReason, Does.Contain("not a crate-able"));
        Assert.That(Send(fleet).RejectionReason, Is.EqualTo("Nothing to build."));
        Assert.That(Send(fleet, new BootstrapCrateRequest(Infra, 0)).RejectionReason, Is.EqualTo("Crate counts must be positive."));
    }

    [Test]
    public void TheModsDefaultBootstrapList_IsTheMinimalColony_AndEveryLineIsCrateable()
    {
        var scene = Build(crates: 0);
        var list = Store.BootstrapDefaults["default-bootstrap-list"];

        Assert.That(list.Crates.Select(line => line.DesignId), Is.EqualTo(new[]
        {
            "default-design-local-construction",
            "default-design-factory",
            "default-design-auto-mine",
            "default-design-planetary-spaceport-crate",
            "default-design-infrastructure",
        }));
        foreach (var line in list.Crates)
        {
            Assert.That(Store.ComponentDesigns.ContainsKey(line.DesignId), line.DesignId);
            var template = Store.ComponentTemplates[Store.ComponentDesigns[line.DesignId].TemplateId];
            Assert.That(template.MountType.ToString(), Does.Contain("PlanetInstallation").And.Contain("ShipCargo"), line.DesignId);
        }
    }

    static ComponentDesignsSnapshot Designs(params (string Id, string Name, bool Crate)[] designs) => new()
    {
        Designs = designs.Select(d => new ComponentDesignSummary(d.Id, d.Name, "t", "T") { IsCrate = d.Crate }).ToList(),
        BootstrapDefaults = new[] { new BootstrapCrateRequest("a", 2), new BootstrapCrateRequest("b", 1) },
    };

    [Test]
    public void TheBootstrapList_StartsFromTheDefaults_Reorders_EditsQuantities_AndTakesExtraCrates()
    {
        var designs = Designs(("a", "Alpha", true), ("b", "Bravo", true), ("c", "Charlie", true), ("x", "Site only", false));
        var list = BootstrapOrderList.FromDefaults(designs);
        Assert.That(list.Requests().Select(r => (r.DesignId, r.Count)), Is.EqualTo(new[] { ("a", 2), ("b", 1) }));

        Assert.That(list.MoveUp(0), Is.False);
        Assert.That(list.MoveDown(0), Is.True);
        Assert.That(list.Requests().Select(r => r.DesignId), Is.EqualTo(new[] { "b", "a" }));
        Assert.That(list.MoveDown(1), Is.False);
        Assert.That(list.MoveUp(1), Is.True);
        Assert.That(list.Requests().Select(r => r.DesignId), Is.EqualTo(new[] { "a", "b" }));

        list.SetCount(1, 0);
        Assert.That(list.Entries[1].Count, Is.EqualTo(1), "a quantity never drops below one, remove the line instead");
        list.SetCount(1, 5000);
        Assert.That(list.Entries[1].Count, Is.EqualTo(BootstrapOrderList.MaxCount));

        // Only crate-able designs not already listed are offered.
        Assert.That(list.Addable(designs).Select(d => d.Id), Is.EqualTo(new[] { "c" }));
        Assert.That(list.Add("c", "Charlie", 3), Is.True);
        Assert.That(list.Add("c", "Charlie", 1), Is.True);
        Assert.That(list.Requests().Last(), Is.EqualTo(new BootstrapCrateRequest("c", 4)));
        Assert.That(list.Add("c", "Charlie", 0), Is.False);

        Assert.That(list.Remove(0), Is.True);
        Assert.That(list.Requests().Select(r => r.DesignId), Is.EqualTo(new[] { "b", "c" }));
    }

    [Test]
    public void TheFleetBootstrapChecks_NameWhatIsMissing_InOrder()
    {
        var one = new[] { new BootstrapCrateRequest("a", 1) };
        Assert.That(BootstrapOrderList.Validate(0, 1, 2, one, true), Does.Contain("Construction Bay"));
        Assert.That(BootstrapOrderList.Validate(1, -1, 2, one, true), Is.EqualTo("Pick a source colony."));
        Assert.That(BootstrapOrderList.Validate(1, 1, -1, one, true), Is.EqualTo("Pick a destination colony."));
        Assert.That(BootstrapOrderList.Validate(1, 1, 1, one, true), Is.EqualTo("Pick two different colonies."));
        Assert.That(BootstrapOrderList.Validate(1, 1, 2, Array.Empty<BootstrapCrateRequest>(), true), Is.EqualTo("Nothing to build."));
        Assert.That(BootstrapOrderList.Validate(1, 1, 2, one, false), Does.Contain("outside"));
        Assert.That(BootstrapOrderList.Validate(1, 1, 2, one, true), Is.Null);
    }

    static IEnumerable<ShipSnapshot> AllShips(FleetSnapshot fleet)
        => fleet.Ships.Concat(fleet.SubFleets.SelectMany(AllShips));

    static List<ComponentInstance> Mounted(Entity entity)
        => entity.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.ToList();

    [Test]
    public void ColonyShipDesign_CarriesAWellBridge_SoItsHardwareSpanIsWell()
    {
        var scene = Build(0);
        var design = scene.Info.ShipDesigns["default-ship-design-colony-ship"];
        var bridges = design.Components.Select(part => part.design.UniqueID).Where(id => id.StartsWith("default-design-bridge")).ToList();

        Assert.That(bridges, Is.EquivalentTo(new[] { "default-design-bridge-ship", "default-design-bridge-well" }));
        Assert.That(CommandSpan.Of(scene.Ship), Is.EqualTo(CommandSpanKind.Well));
        // With no officer seated the chair, not the bridge, is what holds reach to this ship.
        Assert.That(CommandSpan.LimiterOf(scene.Ship), Is.EqualTo(CommandSpan.SpanLimiter.EmptyChair));
        Assert.That(CommandSpan.Fix(scene.Ship, CommandSpanKind.Well), Does.StartWith("The chair is empty."));
        Assert.That(CommandSpan.Fix(scene.Ship, CommandSpanKind.System), Does.Contain("Fit a System Command Bridge"));
    }

    static Scene Build(int crates)
    {
        // A game holds at most 32 factions and every scene makes one, so start a fresh game before it runs out.
        if (_factionsMade++ >= 24)
        {
            Game = InitializeGame();
            _factionsMade = 1;
        }
        var faction = FactionFactory.CreateFaction(Game, "Bootstrap-" + Guid.NewGuid().ToString("N"));
        var info = faction.GetDataBlob<FactionInfoDB>();
        PrepareDesigns(faction, info);
        var system = new StarSystem();
        system.Initialize(Game, "Bootstrap-" + Guid.NewGuid().ToString("N"), -1);
        system.SetActivityState(SystemActivityState.Stasis);

        var scene = new Scene { System = system, Faction = faction, Info = info };
        scene.Planet = scene.NewPlanet();
        scene.Source = scene.ColonyAt(scene.Planet, faction, "Source Colony");
        scene.Dest = scene.ColonyAt(scene.Planet, faction, "Empty Colony");
        scene.Source.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"] = new TypeStore(1_000_000_000);
        scene.Ship = ShipFactory.CreateShip(
            info.ShipDesigns["default-ship-design-colony-ship"], faction, scene.Planet, "Colony Ship");
        scene.Ship.GetDataBlob<ShipInfoDB>().CommanderID = 1;
        scene.Bay = scene.Ship.GetDataBlob<LocalConstructionDB>();

        foreach (var holder in new[] { scene.Ship, scene.Source, scene.Dest })
            holder.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;

        for (int i = 0; i < crates; i++)
        {
            var crate = new ComponentInstance(info.ComponentDesigns[Infra]);
            Assert.That(scene.Source.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));
            scene.Crates.Add(crate);
        }
        return scene;
    }

    static void PrepareDesigns(Entity faction, FactionInfoDB info)
    {
        var blueprint = Store.Colonies["colony-earth"];
        foreach (var id in blueprint.StartingItems ?? new List<string>())
        {
            info.Data.Unlock(id);
            if (info.Data.Techs.ContainsKey(id))
                info.Data.IncrementTechLevel(id);
        }
        info.Data.Unlock("plastic-armor");

        string[] designs =
        {
            "default-design-passive-sensor-s50",
            "default-design-bridge-ship",
            "default-design-bridge-well",
            "default-design-ship-construction",
            "default-design-fuel-tank-1500",
            "default-design-shuttlebay",
            "default-design-cargo-hold-5t",
            "default-design-alcubierre-2k",
            "default-design-battery-2t",
            "default-design-reactor-2t",
            "default-design-raptor",
            "default-design-infrastructure",
            "default-design-city-hall",
            "default-design-factory",
        };

        ComponentDesigner.StartResearched = true;
        try
        {
            foreach (var id in designs)
            {
                var component = Game.StartingGameData.ComponentDesigns[id];
                info.Data.Unlock(component.TemplateId);
                ComponentDesignFromJson.Create(faction, info.Data, component);
            }
            ShipDesignFromJson.Create(faction, info.Data, Game.StartingGameData.ShipDesigns["default-ship-design-colony-ship"]);
            ShipDesignFromJson.Create(faction, info.Data, Game.StartingGameData.ShipDesigns["default-ship-design-freighter"]);
        }
        finally
        {
            ComponentDesigner.StartResearched = false;
        }
    }

    sealed class Scene
    {
        public StarSystem System = null!;
        public Entity Faction = null!;
        public FactionInfoDB Info = null!;
        public Entity Planet = null!;
        public Entity Source = null!;
        public Entity Dest = null!;
        public Entity Ship = null!;
        public LocalConstructionDB Bay = null!;
        public List<ComponentInstance> Crates = new();

        public Goal Goal(string designId, int count) => new Goal(GoalType.BootstrapColony)
        {
            Name = "Bootstrap colony",
            SourceEntityId = Source.Id,
            DestEntityId = Dest.Id,
            TargetEntityID = Dest.Id,
            BootstrapCrates = new List<BootstrapCrateLine> { new() { DesignId = designId, Count = count } },
        };

        public BootstrapColonyCommand Command(string designId, int count) => new(
            Ship.Id, Source.Id, Dest.Id, new[] { new BootstrapCrateRequest(designId, count) });

        public CommandResult Translate(Entity ship, GameCommand command)
            => new CommandTranslator(Game).Translate(Faction, ship, command);

        /// <summary>What the finished load leaves: the crates in the ship's hold, not at the source.</summary>
        public void Load()
        {
            var hold = Ship.GetDataBlob<CargoStorageDB>();
            var source = Source.GetDataBlob<CargoStorageDB>();
            foreach (var crate in Crates)
            {
                Assert.That(source.RemoveCargoByUnit(crate, 1), Is.EqualTo(1));
                Assert.That(hold.AddCargoByUnit(crate, 1), Is.EqualTo(1));
            }
        }

        public Entity NewPlanet()
        {
            var planet = Entity.Create();
            System.AddEntity(planet, new List<BaseDataBlob>
            {
                new NameDB("Body"),
                new PositionDB(0, 0, 0),
                new OrbitDB(),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6_000_000),
                new SystemBodyInfoDB { LengthOfDay = TimeSpan.FromHours(24) },
            });
            return planet;
        }

        public Entity ColonyAt(Entity planet, Entity owner, string name)
        {
            var colony = Entity.Create(owner.Id);
            System.AddEntity(colony, new List<BaseDataBlob>
            {
                new NameDB(name),
                new ColonyInfoDB { PlanetEntity = planet },
                new CargoStorageDB(),
                new ComponentInstancesDB(),
                new PositionDB(new Vector3(planet.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0), planet),
            });
            colony.GetDataBlob<NameDB>().SetName(owner.Id, name);
            return colony;
        }
    }
}
