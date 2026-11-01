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

    const string CityHall = "default-design-city-hall";

    static void StockResources(Scene scene, Dictionary<string, long> amounts)
    {
        foreach (var (cargoId, amount) in amounts)
            Assert.That(scene.Source.GetDataBlob<CargoStorageDB>()
                .AddCargoByUnit(scene.Info.Data.CargoGoods.GetAny(cargoId)!, amount), Is.EqualTo(amount));
    }

    // ------------------------------------------------------------------ fleet Bootstrap: sealed packages

    const string MarsInfra = "default-design-infrastructure-mars";

    /// <summary>Sends a fleet Bootstrap order for the scene's ships and returns the fleet and its goal.</summary>
    static (Entity Fleet, Goal Goal, CommandResult Result) Order(
        Scene scene, int crates = 1, int cityHalls = 0, string? infra = null, string design = Infra, bool addShip = true)
    {
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        if (addShip)
            fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        var builds = cityHalls > 0 ? new[] { new BootstrapCrateRequest(CityHall, cityHalls) } : Array.Empty<BootstrapCrateRequest>();
        var result = scene.Translate(fleet, new FleetBootstrapCommand(fleet.Id, scene.Source.Id, scene.Dest.Id,
            new[] { new BootstrapCrateRequest(design, crates) }, builds, infra));
        var goal = fleet.TryGetDataBlob<GoalsDB>(out var goals) ? goals.ActiveGoal! : null!;
        return (fleet, goal, result);
    }

    static Goal ShipTrip(Entity ship) => ship.GetDataBlob<GoalsDB>().ActiveGoal!;

    static DateTime Now(Scene scene) => scene.Ship.StarSysDateTime;

    static PlanResult PlanFleet(Entity fleet, Goal goal, Scene scene) => new FleetBootstrapPlan().Plan(fleet, goal, Now(scene));

    static bool Sealed(Scene scene, string packageId) => ShipmentBook.TryFind(scene.Source, packageId, out _);

    static ShipmentCargo Package(Scene scene, string packageId)
    {
        Assert.That(ShipmentBook.TryFind(scene.Source, packageId, out var package), Is.True, "the package is closed");
        return package;
    }

    static List<ShipmentCargo> Packages(Scene scene, Goal goal) => goal.BootstrapPackageIds.Select(id => Package(scene, id)).ToList();

    static long Units(Entity holder, ICargoable item) => holder.GetDataBlob<CargoStorageDB>().GetUnitsStored(item, false);

    /// <summary>Runs a queued transfer to the end, like the cargo processor does between pulses.</summary>
    static void Drain(Entity holder)
    {
        holder.GetDataBlob<CargoStorageDB>().TransferRate = 1_000_000;
        var blob = holder.GetDataBlob<CargoTransferDB>();
        var processor = new CargoTransferProcessor();
        for (int i = 0; i < 8; i++)
        {
            processor.ProcessEntity(blob, 10);
            if (!blob.TransferData.EscroHeldInPrimary.Any(t => t.count > 0) && !blob.TransferData.EscroHeldInSecondary.Any(t => t.count > 0))
                break;
        }
        Assert.That(blob.TransferData.EscroHeldInPrimary.Any(t => t.count > 0) || blob.TransferData.EscroHeldInSecondary.Any(t => t.count > 0),
            Is.False, "transfer escrow did not clear");
        holder.Manager.Game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor))
            .ProcessEntity(holder, holder.StarSysDateTime);
    }

    static void SetColonyPoints(Scene scene, long pointsPerDay)
        => scene.Dest.SetDataBlob(new LocalConstructionDB { PointsPerDay = pointsPerDay });

    // ---- decision 15: the seal

    [Test]
    public void FleetBootstrap_SealsTheCratesAndTheResourcesIntoPackages_AtOrderBuild()
    {
        var scene = Build(crates: 2);
        var hall = scene.Info.ComponentDesigns[CityHall];
        var needs = BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 2 } });
        StockResources(scene, needs.ToDictionary(n => n.Key, n => n.Value * 3));

        var (_, goal, result) = Order(scene, crates: 2, cityHalls: 2);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(hall.ResourceCosts, Is.Not.Empty);
        // The goal points at the packages and not at loose crates.
        Assert.That(goal.BootstrapPackageIds, Is.Not.Empty);
        Assert.That(goal.BootstrapCrateIds, Is.Empty);
        var packages = Packages(scene, goal);
        Assert.That(packages.All(p => p.FleetOrder == BootstrapPackage.OrderPrefix + goal.Id), Is.True);
        Assert.That(packages.All(p => !p.IsMarket), Is.True, "a Bootstrap package is not a market shipment");
        // One package per cargo type: the crates are alone in theirs.
        var cratePackage = packages.Single(BootstrapPackage.IsCratePackage);
        Assert.That(cratePackage.Lines.All(line => line.IsCrate), Is.True);
        Assert.That(cratePackage.Lines.Sum(line => line.Units), Is.EqualTo(2));
        Assert.That(packages.Where(p => !BootstrapPackage.IsCratePackage(p)).SelectMany(p => p.Lines).All(line => !line.IsCrate), Is.True);
        // The crates the order sealed are no longer open stock.
        Assert.That(BootstrapColonyPlan.CratesOf(scene.Source, Infra), Is.Empty);
        // Each resource is sealed in the amount the facilities cost, and only that much.
        foreach (var (cargoId, amount) in needs)
        {
            var item = scene.Info.Data.CargoGoods.GetAny(cargoId)!;
            Assert.That(packages.SelectMany(p => p.Lines).Where(line => line.Open.ID == item.ID).Sum(line => line.Units), Is.EqualTo(amount));
            Assert.That(Units(scene.Source, item), Is.EqualTo(amount * 2), "the rest of the stock stays open");
        }
    }

    [Test]
    public void FleetBootstrap_SealedCratesAreGone_SoASecondOrderCannotTakeThem()
    {
        var scene = Build(crates: 1);
        Assert.That(Order(scene, crates: 1).Result.Accepted, Is.True);

        var second = Order(scene, crates: 1, addShip: false);

        Assert.That(second.Result.Accepted, Is.False);
        Assert.That(second.Result.RejectionReason, Does.StartWith("No suitable infrastructure"),
            "the sealed crate is out of the source's stock, so no infrastructure is left to fit");
    }

    [Test]
    public void FleetBootstrap_ShortOfCrates_IsRejectedNamingTheDesign_AndSealsNothing()
    {
        var scene = Build(crates: 1);
        Stock(scene, Factory);

        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        var result = scene.Translate(fleet, new FleetBootstrapCommand(fleet.Id, scene.Source.Id, scene.Dest.Id,
            new[] { new BootstrapCrateRequest(Infra, 1), new BootstrapCrateRequest(Factory, 2) }));

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Does.Contain("no finished crates of").And.Contain(scene.Info.ComponentDesigns[Factory].Name));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty, "the infrastructure crate must not stay sealed");
        Assert.That(BootstrapColonyPlan.CratesOf(scene.Source, Infra), Has.Count.EqualTo(1));
    }

    [Test]
    public void FleetBootstrap_ShortOfAResourceAtTheSource_IsRejectedNamingIt_AndSealsNothing()
    {
        var scene = Build(crates: 1);
        var needs = BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 1 } });
        var (cargoId, amount) = needs.First();
        StockResources(scene, needs.ToDictionary(n => n.Key, n => n.Key == cargoId ? n.Value - 1 : n.Value));

        var (_, _, result) = Order(scene, crates: 1, cityHalls: 1);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Does.StartWith("not enough ").And.EndWith(" at source"));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
        Assert.That(BootstrapColonyPlan.CratesOf(scene.Source, Infra), Has.Count.EqualTo(1));
    }

    [Test]
    public void FleetBootstrap_ARejectionAfterTheSeal_GivesThePackagesBack()
    {
        var scene = Build(crates: 1);
        var needs = BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 1 } });
        StockResources(scene, needs);

        // A fleet with no ship fails when the goal is assigned, which is after the seal.
        var (fleet, _, result) = Order(scene, crates: 1, cityHalls: 1, addShip: false);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("We have no subordinates to manage"));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
        Assert.That(BootstrapColonyPlan.CratesOf(scene.Source, Infra), Has.Count.EqualTo(1));
        foreach (var (cargoId, amount) in needs)
            Assert.That(Units(scene.Source, scene.Info.Data.CargoGoods.GetAny(cargoId)!), Is.EqualTo(amount));
        Assert.That(fleet.TryGetDataBlob<GoalsDB>(out var goals) && goals.ActiveGoal is { Status: GoalStatus.Active }, Is.False);
    }

    [Test]
    public void FleetBootstrap_CancelNow_ReleasesThePackages_AndLeavesTheCratesUnmarked()
    {
        var scene = Build(crates: 1);
        var (fleet, goal, result) = Order(scene);
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(ShipmentBook.At(scene.Source), Is.Not.Empty);

        new FleetBootstrapPlan().Cancel(fleet, goal, Now(scene), CancelTiming.Now);

        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
        var crate = scene.Crates[0];
        Assert.That(Units(scene.Source, crate), Is.EqualTo(1));
        Assert.That(BootstrapColonyPlan.CratesOf(scene.Source, Infra), Is.EquivalentTo(new[] { crate }), "released crates are open stock again");
    }

    [Test]
    public void FleetBootstrap_CancelWhenConvenient_KeepsThePackagesWhileAShipIsStillWorking()
    {
        var scene = Build(crates: 1);
        var (fleet, goal, _) = Order(scene);
        Assert.That(ShipTrip(scene.Ship).ParentGoalId, Is.EqualTo(goal.Id));

        new FleetBootstrapPlan().Cancel(fleet, goal, Now(scene), CancelTiming.WhenConvenient);
        goal.PlayerCancelled = true; // the cancel command marks the goal. The planner reads it.

        // The ship is still working its trip, so the order waits and the packages stay sealed.
        var waiting = PlanFleet(fleet, goal, scene);
        Assert.That(waiting.Status, Is.EqualTo(GoalStatus.Active), waiting.Message);
        Assert.That(ShipmentBook.At(scene.Source), Is.Not.Empty);

        // Once the ship's trip is over the order ends and gives the packages back.
        ShipTrip(scene.Ship).Status = GoalStatus.Failed;
        var done = PlanFleet(fleet, goal, scene);
        Assert.That(done.Status, Is.EqualTo(GoalStatus.Failed), done.Message);
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
    }

    [Test]
    public void FleetBootstrap_WhenTheOrderFails_ThePackagesAreReleased()
    {
        var scene = Build(crates: 1);
        var (fleet, goal, _) = Order(scene);
        Assert.That(ShipmentBook.At(scene.Source), Is.Not.Empty);
        // The ship loses its captain and the trip is dropped, so nothing can carry the package.
        var trip = ShipTrip(scene.Ship);
        trip.Status = GoalStatus.Failed;
        trip.Message = "out of range";
        scene.Ship.SetDataBlob(new GoalsDB { ActiveGoal = trip, GivenGoal = trip });
        scene.Ship.GetDataBlob<ShipInfoDB>().CommanderID = -1;

        var plan = PlanFleet(fleet, goal, scene);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo("no ship"));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
        Assert.That(BootstrapColonyPlan.CratesOf(scene.Source, Infra), Has.Count.EqualTo(1));
    }

    [Test]
    public void FleetBootstrap_ATripThatFailedAfterLoading_FailsTheOrder_ReleasesThePackage_AndLeavesTheCrateAboard()
    {
        var scene = Build(crates: 1);
        var (fleet, goal, _) = Order(scene);
        var trip = ShipTrip(scene.Ship);
        Assert.That(new BootstrapColonyPlan().Plan(scene.Ship, trip, Now(scene)).Status, Is.EqualTo(GoalStatus.Active));
        Drain(scene.Source);
        Assert.That(Units(scene.Ship, scene.Crates[0]), Is.EqualTo(1));

        trip.Status = GoalStatus.Failed;
        trip.Message = "out of range";
        var plan = PlanFleet(fleet, goal, scene);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Is.EqualTo("out of range"));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
        Assert.That(Units(scene.Ship, scene.Crates[0]), Is.EqualTo(1), "the crate stays where the trip left it");
    }

    // ---- decision 16: who mounts the crates

    [Test]
    public void FleetBootstrap_TheShipMounts_WhenItsBayHasMoreFreeCapacity()
    {
        var scene = Build(crates: 1);
        SetColonyPoints(scene, scene.Bay.PointsPerDay - 1);

        var (_, goal, result) = Order(scene);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var trip = ShipTrip(scene.Ship);
        Assert.That(trip.Type, Is.EqualTo(GoalType.BootstrapColony));
        Assert.That(trip.CargoId, Is.EqualTo(goal.BootstrapPackageIds[0]));
        Assert.That(goal.Message, Does.Contain("ship's Construction Bay").And.Contain($"{scene.Bay.PointsPerDay} free build points"));
    }

    [Test]
    public void FleetBootstrap_TheColonyMounts_WhenItHasMoreFreeCapacity()
    {
        var scene = Build(crates: 1);
        SetColonyPoints(scene, scene.Bay.PointsPerDay + 1);

        var (_, goal, result) = Order(scene);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var trip = ShipTrip(scene.Ship);
        Assert.That(trip.Type, Is.EqualTo(GoalType.DeliverShipment));
        Assert.That(trip.CargoId, Is.EqualTo(goal.BootstrapPackageIds[0]));
        Assert.That(goal.Message, Does.StartWith("The colony mounts the crates").And.Contain($"{scene.Bay.PointsPerDay + 1} free build points"));
    }

    [Test]
    public void FleetBootstrap_ATieInFreeCapacity_GoesToTheColony()
    {
        var scene = Build(crates: 1);
        SetColonyPoints(scene, scene.Bay.PointsPerDay);

        var choice = BootstrapPackage.Choose(scene.Ship, scene.Dest);
        var (_, _, result) = Order(scene);

        Assert.That(choice.ByShip, Is.False);
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(ShipTrip(scene.Ship).Type, Is.EqualTo(GoalType.DeliverShipment));
    }

    [Test]
    public void FleetBootstrap_Capacity_IsPointsPerDayLessTheWorkAlreadyQueued()
    {
        var scene = Build(crates: 1);
        long perDay = scene.Bay.PointsPerDay;
        // The colony makes twice the ship's points but is two days behind, so the ship has more free capacity.
        SetColonyPoints(scene, perDay * 2);
        var infra = scene.Info.ComponentDesigns[Infra];
        var job = new LocalConstructionJob(infra, new ComponentInstance(infra));
        job.PointCost = perDay * 3; // a heavier job, so the colony is a day behind
        scene.Dest.GetDataBlob<LocalConstructionDB>().BuildQueue.Enqueue(job);

        long colonyFree = BootstrapPackage.FreeBuildPoints(scene.Dest, out bool colonyCan);
        long shipFree = BootstrapPackage.FreeBuildPoints(scene.Ship, out bool shipCan);

        Assert.That(colonyCan && shipCan, Is.True);
        Assert.That(colonyFree, Is.EqualTo(perDay * 2 - job.PointCost));
        Assert.That(shipFree, Is.EqualTo(perDay));
        Assert.That(BootstrapPackage.Choose(scene.Ship, scene.Dest).ByShip, Is.True);
    }

    [Test]
    public void FleetBootstrap_AColonyWithNoBay_LeavesTheMountingToTheShip()
    {
        var scene = Build(crates: 1);
        Assert.That(scene.Dest.HasDataBlob<LocalConstructionDB>(), Is.False);

        var (_, goal, result) = Order(scene);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(ShipTrip(scene.Ship).Type, Is.EqualTo(GoalType.BootstrapColony));
        Assert.That(goal.Message, Does.Contain("the colony has no construction capacity"));
    }

    [Test]
    public void FleetBootstrap_WhenNeitherCanBuild_TheOrderIsRejected_AndThePackagesAreReleased()
    {
        var scene = Build(crates: 1);
        scene.Ship.RemoveDataBlob<LocalConstructionDB>();

        var (_, _, result) = Order(scene);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Does.Contain("Neither the colony nor a ship's Construction Bay can mount crates"));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
        Assert.That(BootstrapColonyPlan.CratesOf(scene.Source, Infra), Has.Count.EqualTo(1));
    }

    [Test]
    public void FleetBootstrap_AShipMount_KeepsTheCrateInTheShipsHold_UntilItIsConstructed()
    {
        var scene = Build(crates: 1);
        var (fleet, goal, result) = Order(scene);
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var trip = ShipTrip(scene.Ship);
        Assert.That(trip.Type, Is.EqualTo(GoalType.BootstrapColony));
        var crate = scene.Crates[0];

        // Load the package's crate line.
        var plan = new BootstrapColonyPlan().Plan(scene.Ship, trip, Now(scene));
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Drain(scene.Source);
        Assert.That(Units(scene.Ship, crate), Is.EqualTo(1));

        // The ship mounts from its own hold: no unload to the colony.
        plan = new BootstrapColonyPlan().Plan(scene.Ship, trip, Now(scene));
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var mount = plan.Actions.OfType<MountFromHoldAction>().Single();
        Assert.That(plan.Actions.OfType<CargoTransferOrder>(), Is.Empty);
        mount.ParentGoalId = trip.Id;
        scene.Ship.GetDataBlob<ActionQueueDB>().Enqueue(mount);
        Assert.That(mount.IsValidCommand(Game), Is.True);
        mount.Execute(Now(scene));
        Assert.That(mount.Status, Is.EqualTo(ActionStatus.Running), mount.FailureMessage);

        Assert.That(Units(scene.Ship, crate), Is.EqualTo(1), "still in the ship's hold while it waits to be built");
        Assert.That(Units(scene.Dest, crate), Is.EqualTo(0));
        Assert.That(scene.Bay.BuildQueue.Select(job => job.Instance), Is.EquivalentTo(new[] { crate }));
        var waiting = PlanFleet(fleet, goal, scene);
        Assert.That(waiting.Status, Is.EqualTo(GoalStatus.Active), waiting.Message);
        Assert.That(ShipmentBook.At(scene.Source), Is.Not.Empty, "the package stays until the crate is mounted");

        new LocalConstructionProcessor().ProcessEntity(scene.Ship, 0);

        Assert.That(Mounted(scene.Dest), Is.EquivalentTo(new[] { crate }));
        Assert.That(Units(scene.Ship, crate), Is.EqualTo(0));
        Assert.That(Units(scene.Dest, crate), Is.EqualTo(0), "it was never unloaded to the colony");
        scene.Ship.GetDataBlob<ActionQueueDB>().ActionList.Remove(mount);
        trip.Status = GoalStatus.Completed;
        var done = PlanFleet(fleet, goal, scene);
        Assert.That(done.Status, Is.EqualTo(GoalStatus.Completed), done.Message);
        Assert.That(done.Message, Is.EqualTo("Mounted 1 crate"));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
    }

    [Test]
    public void FleetBootstrap_AColonyMount_UnloadsTheCrate_ThenTheColonyBuildsIt()
    {
        var scene = Build(crates: 1);
        SetColonyPoints(scene, scene.Bay.PointsPerDay * 10);
        var (fleet, goal, result) = Order(scene);
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var trip = ShipTrip(scene.Ship);
        Assert.That(trip.Type, Is.EqualTo(GoalType.DeliverShipment));
        var crate = scene.Crates[0];

        var plan = new DeliverShipmentPlan().Plan(scene.Ship, trip, Now(scene));
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Drain(scene.Source);
        Assert.That(Units(scene.Ship, crate), Is.EqualTo(1));
        plan = new DeliverShipmentPlan().Plan(scene.Ship, trip, Now(scene));
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message + " " + string.Join(",", scene.Ship.GetDataBlob<ActionQueueDB>().ActionList.Select(a => a.GetType().Name)));
        Drain(scene.Dest);
        Assert.That(Units(scene.Dest, crate), Is.EqualTo(1));

        // The trip is done, but the package stays open until the colony has built the crate.
        plan = new DeliverShipmentPlan().Plan(scene.Ship, trip, Now(scene));
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed), plan.Message);
        Assert.That(ShipmentBook.At(scene.Source), Is.Not.Empty);
        trip.Status = GoalStatus.Completed;

        var mounting = PlanFleet(fleet, goal, scene);
        Assert.That(mounting.Status, Is.EqualTo(GoalStatus.Active), mounting.Message);
        var colonyBay = scene.Dest.GetDataBlob<LocalConstructionDB>();
        Assert.That(colonyBay.BuildQueue.Select(job => job.Instance), Is.EquivalentTo(new[] { crate }));

        new LocalConstructionProcessor().ProcessEntity(scene.Dest, 0);
        Assert.That(Mounted(scene.Dest), Is.EquivalentTo(new[] { crate }));

        var done = PlanFleet(fleet, goal, scene);
        Assert.That(done.Status, Is.EqualTo(GoalStatus.Completed), done.Message);
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
    }

    // ---- decision 17: the resources ride in the package, after the crates

    [Test]
    public void FleetBootstrap_TheResourcesWait_WhileACrateIsStillOut_EvenWithAFreeShip()
    {
        var scene = Build(crates: 1);
        StockResources(scene, BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 1 } }));
        var spare = ShipFactory.CreateShip(
            scene.Info.ShipDesigns["default-ship-design-colony-ship"], scene.Faction, scene.Planet, "Spare Ship");
        spare.GetDataBlob<ShipInfoDB>().CommanderID = 2;
        spare.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        fleet.GetDataBlob<FleetDB>().AddChild(spare);
        var result = scene.Translate(fleet, new FleetBootstrapCommand(fleet.Id, scene.Source.Id, scene.Dest.Id,
            new[] { new BootstrapCrateRequest(Infra, 1) }, new[] { new BootstrapCrateRequest(CityHall, 1) }));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var goal = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        var carrier = new[] { scene.Ship, spare }.Single(ship => ship.HasDataBlob<GoalsDB>() && ship.GetDataBlob<GoalsDB>().ActiveGoal?.ParentGoalId == goal.Id);
        var trip = ShipTrip(carrier);
        Assert.That(Package(scene, trip.CargoId).Lines.All(line => line.IsCrate), Is.True, "the crates are served first");

        // Before the carrier has loaded, and after, the free ship gets nothing.
        var plan = PlanFleet(fleet, goal, scene);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(plan.SubGoals, Is.Empty);
        new BootstrapColonyPlan().Plan(carrier, trip, Now(scene));
        Drain(scene.Source);
        plan = PlanFleet(fleet, goal, scene);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.SubGoals, Is.Empty, "the resources wait for the crates");
        Assert.That(Packages(scene, goal).Where(p => !BootstrapPackage.IsCratePackage(p)).All(p => p.Lines.All(line => !line.Released)), Is.True);
    }

    [Test]
    public void FleetBootstrap_TheResourcesAreHauledInTheirPackage_NotAsMoveGoods_OnceTheCratesAreMounted()
    {
        var scene = Build(crates: 1);
        var needs = BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 1 } });
        StockResources(scene, needs);
        var (fleet, goal, result) = Order(scene, crates: 1, cityHalls: 1);
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(goal.BootstrapSiteBuilds.Single().Count, Is.EqualTo(1));

        // The crate goes first: ship-mounted, since the colony has no bay.
        var trip = ShipTrip(scene.Ship);
        Assert.That(Package(scene, trip.CargoId).Lines.All(line => line.IsCrate), Is.True);
        new BootstrapColonyPlan().Plan(scene.Ship, trip, Now(scene));
        Drain(scene.Source);
        var mount = new BootstrapColonyPlan().Plan(scene.Ship, trip, Now(scene)).Actions.OfType<MountFromHoldAction>().Single();
        mount.ParentGoalId = trip.Id;
        scene.Ship.GetDataBlob<ActionQueueDB>().Enqueue(mount);
        Assert.That(mount.IsValidCommand(Game), Is.True);
        mount.Execute(Now(scene));
        new LocalConstructionProcessor().ProcessEntity(scene.Ship, 0);
        scene.Ship.GetDataBlob<ActionQueueDB>().ActionList.Remove(mount);
        trip.Status = GoalStatus.Completed;

        // Now the resources go, as a DeliverShipment of the resource package.
        var plan = PlanFleet(fleet, goal, scene);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        var (ship, haul) = plan.SubGoals.Single();
        Assert.That(ship.Id, Is.EqualTo(scene.Ship.Id));
        Assert.That(haul.Type, Is.EqualTo(GoalType.DeliverShipment));
        Assert.That(haul.Type, Is.Not.EqualTo(GoalType.MoveGoods));
        Assert.That(goal.BootstrapPackageIds, Does.Contain(haul.CargoId));
        Assert.That(BootstrapPackage.IsCratePackage(Package(scene, haul.CargoId)), Is.False);
        scene.Ship.SetDataBlob(new GoalsDB { ActiveGoal = haul, GivenGoal = haul });

        // Load, unload and unpack the package.
        for (int guard = 0; guard < 6; guard++)
        {
            var step = new DeliverShipmentPlan().Plan(scene.Ship, haul, Now(scene));
            if (step.Status != GoalStatus.Active)
                break;
            Assert.That(step.Message, Is.EqualTo("Loading").Or.EqualTo("Unloading"), step.Message);
            Drain(step.Message == "Unloading" ? scene.Dest : scene.Source);
            if (!Sealed(scene, haul.CargoId))
                break;
        }
        haul.Status = GoalStatus.Completed;
        var done = PlanFleet(fleet, goal, scene);
        Assert.That(done.Status, Is.EqualTo(GoalStatus.Completed), done.Message);
        Assert.That(done.Message, Is.EqualTo("Mounted 1 crate, hauled the resources"));
        foreach (var (cargoId, amount) in needs)
            Assert.That(Units(scene.Dest, scene.Info.Data.CargoGoods.GetAny(cargoId)!), Is.EqualTo(amount));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
    }

    // ---- decision 18: the best-fit infrastructure design

    static InfrastructureWindow Window(double minG, double maxG, double minP, double maxP) => new(minG, maxG, minP, maxP);

    [Test]
    public void InfrastructureFit_PicksTheWindowThatCoversTheBodyMostTightly()
    {
        var earth = ("earth", Window(8.8, 10.8, 0.9, 1.1));
        var mars = ("mars", Window(3.0, 4.9, 0.85, 1.05));

        Assert.That(InfrastructureFit.Best(new[] { earth, mars }, 9.81, 1.0), Is.EqualTo("earth"));
        Assert.That(InfrastructureFit.Best(new[] { earth, mars }, 3.7, 0.95), Is.EqualTo("mars"));
        Assert.That(InfrastructureFit.Best(new[] { mars, earth }, 3.7, 0.95), Is.EqualTo("mars"), "list order does not matter");
        Assert.That(InfrastructureFit.Best(new[] { earth, mars }, 20, 1.0), Is.Null);
        Assert.That(InfrastructureFit.Best(new[] { earth, mars }, 9.81, 5), Is.Null, "pressure is part of the window");
        Assert.That(InfrastructureFit.Best(Array.Empty<(string, InfrastructureWindow)>(), 9.81, 1.0), Is.Null);
        Assert.That(InfrastructureFit.NoFitMessage("Mars"), Is.EqualTo("No suitable infrastructure for Mars"));
    }

    [Test]
    public void InfrastructureFit_OfTwoCoveringWindows_TheCloserMidpointWins_ThenTheNarrowerWindow_ThenTheLowerId()
    {
        // Both cover 6, but "wide" is centred on it and "off" is not.
        var wide = ("wide", Window(2, 10, 0, 2));
        var off = ("off", Window(5, 8, 0, 2));
        Assert.That(InfrastructureFit.Rank(new[] { wide, off }, 6, 1), Is.EqualTo(new[] { "wide", "off" }));

        // The body sits dead centre of both, so the narrower window is the more specific design.
        var broad = ("a-broad", Window(0, 10, 0, 2));
        var narrow = ("z-narrow", Window(4, 6, 0.5, 1.5));
        Assert.That(InfrastructureFit.Rank(new[] { broad, narrow }, 5, 1), Is.EqualTo(new[] { "z-narrow", "a-broad" }));

        // An exact tie on both goes to the lower id.
        var twinB = ("b", Window(4, 6, 0.5, 1.5));
        var twinA = ("a", Window(4, 6, 0.5, 1.5));
        Assert.That(InfrastructureFit.Rank(new[] { twinB, twinA }, 5, 1), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void InfrastructureFit_AnAxisWithNoLimit_AddsNothingToTheDistance()
    {
        var open = ("open", Window(double.MinValue, double.MaxValue, 0.9, 1.1));
        var tight = ("tight", Window(8, 12, 0.9, 1.1));
        Assert.That(InfrastructureFit.Rank(new[] { open, tight }, 10, 1.0), Is.EqualTo(new[] { "tight", "open" }),
            "no limit on gravity is perfect on that axis, so both fit equally well, and then the narrower window wins");
        Assert.That(InfrastructureFit.Best(new[] { open }, 500, 1.0), Is.EqualTo("open"));
    }

    [Test]
    public void FleetBootstrap_TheBestFitDesignIsTheDefault_AndTheStockDesignsHaveTheirWindows()
    {
        var scene = Build(crates: 1);
        var earth = InfrastructureSuitability.WindowOf(scene.Info.ComponentDesigns[Infra]);
        var mars = InfrastructureSuitability.WindowOf(scene.Info.ComponentDesigns[MarsInfra]);
        Assert.That(earth, Is.Not.Null);
        Assert.That(mars, Is.Not.Null);
        Assert.That(earth!.Covers(9.81, 1.0), Is.True, earth.ToString());
        Assert.That(mars!.Covers(3.7, 0.95), Is.True, mars.ToString());
        Assert.That(InfrastructureSuitability.WindowOf(scene.Info.ComponentDesigns[Factory]), Is.Null);
        Stock(scene, MarsInfra);
        scene.SetClimate(3.7, 0.95f);

        // The player asked for the Earth design, but only the Mars one fits Mars, so Mars is the default.
        var (_, goal, result) = Order(scene, infra: null);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(goal.BootstrapInfrastructureId, Is.EqualTo(MarsInfra));
        Assert.That(goal.BootstrapCrates.Single().DesignId, Is.EqualTo(MarsInfra));
        Assert.That(Package(scene, goal.BootstrapPackageIds[0]).Lines.Single().Held, Is.InstanceOf<ComponentInstance>()
            .With.Property("Design").Property("UniqueID").EqualTo(MarsInfra));
    }

    [Test]
    public void FleetBootstrap_TheBestFitReadsSourceCargoOnly_SoACrateElsewhereDoesNotCount()
    {
        var scene = Build(crates: 1);
        // A Mars crate sits in the destination's cargo and not the source's.
        var marsCrate = new ComponentInstance(scene.Info.ComponentDesigns[MarsInfra]);
        scene.Dest.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"] = new TypeStore(1_000_000);
        Assert.That(scene.Dest.GetDataBlob<CargoStorageDB>().AddCargoByUnit(marsCrate, 1), Is.EqualTo(1));
        scene.SetClimate(3.7, 0.95f);

        var (_, _, result) = Order(scene);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("No suitable infrastructure for Body"));
    }

    [Test]
    public void FleetBootstrap_APlayersTweak_IsUsedInPlaceOfTheBestFit()
    {
        // Both designs are in stock. At 9.81 the Earth one is the best fit.
        var scene = Build(crates: 1);
        Stock(scene, MarsInfra);
        var best = Order(scene, infra: null);
        Assert.That(best.Result.Accepted, Is.True, best.Result.RejectionReason);
        Assert.That(best.Goal.BootstrapInfrastructureId, Is.EqualTo(Infra));

        // The same stock with the player's pick of the other design.
        var other = Build(crates: 1);
        Stock(other, MarsInfra);
        var tweaked = Order(other, infra: MarsInfra);

        Assert.That(tweaked.Result.Accepted, Is.True, tweaked.Result.RejectionReason);
        Assert.That(tweaked.Goal.BootstrapInfrastructureId, Is.EqualTo(MarsInfra));
        Assert.That(tweaked.Goal.BootstrapCrates.Single().DesignId, Is.EqualTo(MarsInfra));
    }

    [Test]
    public void FleetBootstrap_AnUnknownInfrastructureDesign_IsRejected()
    {
        var scene = Build(crates: 1);

        var (_, _, result) = Order(scene, infra: Factory);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Does.Contain("Unknown infrastructure design"));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
    }

    [Test]
    public void FleetBootstrap_NoFittingInfrastructure_IsRejectedWithTheStubsWording()
    {
        var scene = Build(crates: 1);
        scene.SetClimate(25, 1f);

        var (_, _, result) = Order(scene);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo("No suitable infrastructure for Body"));
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty);
        Assert.That(BootstrapColonyPlan.CratesOf(scene.Source, Infra), Has.Count.EqualTo(1));
    }

    [Test]
    public void FleetBootstrap_APlanetWithNoAtmosphere_HasPressureZero_SoTheStockDesignsDoNotFit()
    {
        var scene = Build(crates: 1);
        scene.Planet.RemoveDataBlob<AtmosphereDB>();

        var (_, _, result) = Order(scene);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionReason, Does.StartWith("No suitable infrastructure for"));
    }

    [Test]
    public void FleetBootstrap_AListWithNoInfrastructureLine_SkipsTheCheck()
    {
        var scene = Build(crates: 0);
        Stock(scene, Factory);
        scene.SetClimate(25, 1f);

        var (_, goal, result) = Order(scene, design: Factory);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(goal.BootstrapInfrastructureId, Is.Empty);
        Assert.That(goal.BootstrapCrates.Single().DesignId, Is.EqualTo(Factory));
    }

    [Test]
    public void FleetBootstrap_TheSuitabilityCheck_RunsAtOrderBuildOnly()
    {
        var scene = Build(crates: 1);
        var (fleet, goal, result) = Order(scene);
        Assert.That(result.Accepted, Is.True, result.RejectionReason);

        // The climate changes after the order is built. The running order does not look again.
        scene.SetClimate(25, 1f);
        var plan = PlanFleet(fleet, goal, scene);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(goal.BootstrapInfrastructureId, Is.EqualTo(Infra));
    }

    // ---- decision 18: the fleet window's own helpers

    static ComponentDesignsSnapshot InfraDesigns() => new()
    {
        Designs = new[]
        {
            new ComponentDesignSummary("earth", "Earth Infrastructure", "t", "T") { IsCrate = true, Infrastructure = Window(8.8, 10.8, 0.9, 1.1) },
            new ComponentDesignSummary("mars", "Mars Infrastructure", "t", "T") { IsCrate = true, Infrastructure = Window(3.0, 4.9, 0.85, 1.05) },
            new ComponentDesignSummary("factory", "Factory", "t", "T") { IsCrate = true },
        },
    };

    static BootstrapOrderList ListOf(ComponentDesignsSnapshot designs, params (string Id, int Count)[] lines)
    {
        var list = new BootstrapOrderList();
        foreach (var (id, count) in lines)
            Assert.That(list.Add(id, designs.Designs.Single(d => d.Id == id).Name, count), Is.True);
        return list;
    }

    [Test]
    public void BootstrapPicker_TheBestFitBecomesTheListsInfrastructureLine_AndTheTweakMergesLines()
    {
        var designs = InfraDesigns();
        var list = ListOf(designs, ("factory", 1), ("earth", 2));
        var atSource = new[]
        {
            new BootstrapPicker.InfrastructureOption("earth", "Earth Infrastructure", designs.Designs[0].Infrastructure!),
            new BootstrapPicker.InfrastructureOption("mars", "Mars Infrastructure", designs.Designs[1].Infrastructure!),
        };
        var mars = new BootstrapPicker.BootstrapClimate("Mars", 3.7, 0.95);

        Assert.That(BootstrapPicker.FittingInfrastructure(atSource, mars).Select(o => o.DesignId), Is.EqualTo(new[] { "mars" }));
        Assert.That(BootstrapPicker.ApplyBestFit(list, designs, atSource, mars), Is.True);
        Assert.That(list.Requests().Select(r => (r.DesignId, r.Count)), Is.EqualTo(new[] { ("factory", 1), ("mars", 2) }),
            "the line keeps its place and its count");

        // The player's tweak, and a second infrastructure line merges into the first.
        list = ListOf(designs, ("earth", 2), ("factory", 1));
        Assert.That(list.Add("mars", "Mars Infrastructure", 3), Is.True);
        Assert.That(list.UseInfrastructure(designs, "mars"), Is.True);
        Assert.That(list.Requests().Select(r => (r.DesignId, r.Count)), Is.EqualTo(new[] { ("mars", 5), ("factory", 1) }));
        Assert.That(list.InfrastructureEntry(designs)!.DesignId, Is.EqualTo("mars"));
        Assert.That(list.UseInfrastructure(designs, "factory"), Is.False, "a factory is not infrastructure");
        Assert.That(ListOf(designs, ("factory", 1)).UseInfrastructure(designs, "earth"), Is.False, "no infrastructure line to swap");
    }

    [Test]
    public void BootstrapPicker_NoFit_BlocksWithTheStubsWording_OnlyForAListWithInfrastructure()
    {
        var designs = InfraDesigns();
        var atSource = new[] { new BootstrapPicker.InfrastructureOption("earth", "Earth Infrastructure", designs.Designs[0].Infrastructure!) };
        var mars = new BootstrapPicker.BootstrapClimate("Mars", 3.7, 0.95);
        var earth = new BootstrapPicker.BootstrapClimate("Earth", 9.81, 1.0);

        Assert.That(BootstrapPicker.CheckInfrastructure(ListOf(designs, ("earth", 1)), designs, atSource, mars),
            Is.EqualTo("No suitable infrastructure for Mars"));
        Assert.That(BootstrapPicker.CheckInfrastructure(ListOf(designs, ("earth", 1)), designs, atSource, earth), Is.Null);
        Assert.That(BootstrapPicker.CheckInfrastructure(ListOf(designs, ("factory", 1)), designs, atSource, mars), Is.Null);
        Assert.That(BootstrapPicker.CheckInfrastructure(ListOf(designs, ("earth", 1)), designs, atSource, null), Is.Null, "an unknown climate blocks nothing");
        Assert.That(BootstrapPicker.ApplyBestFit(ListOf(designs, ("earth", 1)), designs, atSource, mars), Is.False);
    }

    [Test]
    public void BootstrapOrderList_OffersNoSecondInfrastructureDesign_OnceTheListHasOne()
    {
        var designs = InfraDesigns();
        var list = ListOf(designs, ("factory", 1));
        Assert.That(list.Addable(designs).Select(d => d.Id), Is.EqualTo(new[] { "earth", "mars" }));

        list.Add("earth", "Earth Infrastructure", 1);

        Assert.That(list.Addable(designs), Is.Empty);
    }

    [Test]
    public void BootstrapPicker_TheDestinationsClimateComesFromItsBodyAndAtmosphere_WithNoAtmosphereAsZero()
    {
        var scene = Build(crates: 0);
        var projector = new GameProjector(Game);
        var colony = projector.ProjectEntity(scene.Dest, scene.Faction.Id);
        var planet = projector.ProjectEntity(scene.Planet, scene.Faction.Id);
        Assert.That(colony, Is.Not.Null);
        Assert.That(planet, Is.Not.Null);

        var climate = BootstrapPicker.ClimateOf(colony, id => id == scene.Planet.Id ? planet : null);

        Assert.That(climate, Is.Not.Null);
        Assert.That(climate!.GravityMetresPerSec2, Is.EqualTo(9.81).Within(1e-9));
        Assert.That(climate.PressureAtm, Is.EqualTo(1.0).Within(1e-6));
        Assert.That(climate.PlanetName, Is.EqualTo("Body"));
        Assert.That(BootstrapPicker.ClimateOf(colony, _ => null), Is.Null);
    }

    [Test]
    public void TheProjectedInfrastructureDesign_CarriesItsWindow()
    {
        var scene = Build(crates: 0);
        var designs = new GameProjector(Game).ProjectComponentDesigns(scene.Faction.Id)!;

        var earth = designs.Designs.Single(d => d.Id == Infra);
        var factory = designs.Designs.Single(d => d.Id == Factory);

        Assert.That(earth.Infrastructure, Is.Not.Null);
        Assert.That(earth.Infrastructure!.Covers(9.81, 1.0), Is.True);
        Assert.That(factory.Infrastructure, Is.Null);
    }

    // ---- the command's other checks

    [Test]
    public void FleetBootstrapCommand_TakesSiteBuilds_AndRefusesACrateOrABadCount()
    {
        var scene = Build(crates: 1);
        StockResources(scene, BootstrapResources.Derive(scene.Info, new[] { new BootstrapCrateLine { DesignId = CityHall, Count = 3 } }));
        var fleet = FleetFactory.Create(scene.System, scene.Faction.Id, "Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(scene.Ship);
        CommandResult Send(params BootstrapCrateRequest[] builds)
            => scene.Translate(fleet, new FleetBootstrapCommand(fleet.Id, scene.Source.Id, scene.Dest.Id,
                new[] { new BootstrapCrateRequest(Infra, 1) }, builds));

        Assert.That(Send(new BootstrapCrateRequest(Infra, 1)).RejectionReason, Does.Contain("not a site-only"));
        Assert.That(Send(new BootstrapCrateRequest(CityHall, 0)).RejectionReason, Is.EqualTo("Site build counts must be positive."));
        Assert.That(Send(new BootstrapCrateRequest("no-such-design", 1)).Accepted, Is.False);
        Assert.That(ShipmentBook.At(scene.Source), Is.Empty, "a rejected order seals nothing");

        var result = Send(new BootstrapCrateRequest(CityHall, 1), new BootstrapCrateRequest(CityHall, 2));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var goal = fleet.GetDataBlob<GoalsDB>().ActiveGoal!;
        Assert.That(goal.BootstrapSiteBuilds.Select(l => (l.DesignId, l.Count)), Is.EqualTo(new[] { (CityHall, 3) }));
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
        Assert.That(shipGoal.ParentGoalId, Is.EqualTo(fleetGoal.Id));
        Assert.That(shipGoal.CargoId, Is.EqualTo(fleetGoal.BootstrapPackageIds[0]));
        Assert.That(Package(scene, shipGoal.CargoId).Lines.Sum(line => line.Units), Is.EqualTo(2));
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
        Assert.That(BootstrapOrderList.Validate(0, 1, 2, one, true), Is.EqualTo("No ship in this fleet has a captain."));
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
        // Small enough that the free unit count of a light resource stays inside an int.
        scene.Dest.GetDataBlob<CargoStorageDB>().TypeStores["general-storage"] = new TypeStore(1_000);
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
        // The low-gravity design only keeps its window once the Mars outpost's range extensions are researched.
        foreach (var id in new[] { "tech-infra-gravity-min-extension", "tech-infra-gravity-min-extension",
                     "tech-infra-gravity-min-extension", "tech-infra-pressure-min-extension" })
        {
            info.Data.Unlock(id);
            info.Data.IncrementTechLevel(id);
        }

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
            "default-design-infrastructure-mars",
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
                new SystemBodyInfoDB { LengthOfDay = TimeSpan.FromHours(24), Gravity = 9.81 },
                new AtmosphereDB { Pressure = 1f },
            });
            return planet;
        }

        /// <summary>The destination planet's gravity (m/s squared) and pressure (atm) as the infrastructure check reads them.</summary>
        public void SetClimate(double gravity, float pressure)
        {
            Planet.GetDataBlob<SystemBodyInfoDB>().Gravity = gravity;
            Planet.GetDataBlob<AtmosphereDB>().Pressure = pressure;
        }

        public Entity ColonyAt(Entity planet, Entity owner, string name)
        {
            var colony = Entity.Create(owner.Id);
            System.AddEntity(colony, new List<BaseDataBlob>
            {
                new NameDB(name),
                new ColonyInfoDB { PlanetEntity = planet },
                new CargoStorageDB(),
                new ActionQueueDB(),
                new ComponentInstancesDB(),
                new PositionDB(new Vector3(planet.GetDataBlob<MassVolumeDB>().RadiusInM, 0, 0), planet),
            });
            colony.GetDataBlob<NameDB>().SetName(owner.Id, name);
            return colony;
        }
    }
}
