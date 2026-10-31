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
    static readonly Game Game = InitializeGame();

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

    static IEnumerable<ShipSnapshot> AllShips(FleetSnapshot fleet)
        => fleet.Ships.Concat(fleet.SubFleets.SelectMany(AllShips));

    static List<ComponentInstance> Mounted(Entity entity)
        => entity.GetDataBlob<ComponentInstancesDB>().AllComponents.Values.ToList();

    static Scene Build(int crates)
    {
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
