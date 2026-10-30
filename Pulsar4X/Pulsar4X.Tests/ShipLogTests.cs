using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Industry.Orders;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.People;
using Pulsar4X.People.Orders;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Technology;
using GameEngine.People;

namespace Pulsar4X.Tests;

/// <summary>
/// The ship's log from <c>GameEngine/Engine/Orders/agent-log.md</c>.
/// </summary>
public class ShipLogTests
{
    static readonly Game Game = InitializeGame();
    StarSystem _sys;
    DateTime _epoch;

    static Game InitializeGame()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        var game = new Game(new NewGameSettings(), store);
        game.Settings.EnforceSingleThread = true;
        game.Settings.StrictNewtonion = true;
        return game;
    }

    [SetUp]
    public void SetUp()
    {
        foreach (var sys in Game.Systems)
            sys.SetActivityState(SystemActivityState.Stasis);
        _sys = new StarSystem();
        _sys.Initialize(Game, "Sol", -1);
        _sys.IncrementExternalObserver(priority: true);
        _epoch = _sys.StarSysDateTime;
    }

    [Test]
    public void CaptainMove_WritesTheGoalAndEachAction_AndSurvivesSave()
    {
        var (ship, mars) = MakeMoveShip("Courier");
        var goal = new Goal(GoalType.MoveTo) { TargetEntityID = mars.Id };
        AgentProcessor.AssignGoal(ship, goal);

        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active), goal.Message);
        var book = ship.GetDataBlob<ShipLogDB>();
        var own = book.Entries.Where(entry => entry.Kind == ShipLogKind.Own).ToList();
        Assert.That(own.Select(entry => entry.Text), Is.EqualTo(new[] { "Ordered: Move to Mars." }));

        var queued = ship.GetDataBlob<ActionQueueDB>().ActionsFor(goal);
        var actions = book.Entries.Where(entry => entry.Kind == ShipLogKind.Action).ToList();
        Assert.That(actions.Count, Is.EqualTo(queued.Count));
        Assert.That(actions.Count, Is.GreaterThan(0));
        foreach (var action in queued)
            Assert.That(actions.Any(entry => entry.Text.StartsWith(action.Name)), Is.True, action.Name);
        Assert.That(book.Entries.Any(entry => entry.Kind == ShipLogKind.Action && entry.Text.Contains("Warp Move")), Is.True);

        int count = book.Entries.Count;
        AgentProcessor.RunAgentNow(ship);
        Assert.That(book.Entries.Count, Is.EqualTo(count), "a second wake with the work unchanged adds nothing");

        var before = book.Entries.Select(entry => (entry.Kind, entry.Text, entry.At)).ToList();
        int cargoIds = Game.EntityIDCounter;
        int nextId = EntityIDGenerator.NextId;
        int shipId = ship.Id;
        try
        {
            string json = Game.Save(Game);
            EntityIDGenerator.NextId = 0;
            Game.EntityIDCounter = 0;
            var loaded = Game.Load(json);
            var loadedSys = loaded.Systems.Single(system => system.ManagerID == _sys.ManagerID);
            Assert.That(loadedSys.TryGetEntityById(shipId, out var loadedShip), Is.True);
            var loadedBook = loadedShip.GetDataBlob<ShipLogDB>();
            Assert.That(
                loadedBook.Entries.Select(entry => (entry.Kind, entry.Text, entry.At)),
                Is.EqualTo(before));
        }
        finally
        {
            if (Game.EntityIDCounter < cargoIds)
                Game.EntityIDCounter = cargoIds;
            if (EntityIDGenerator.NextId < nextId)
                EntityIDGenerator.NextId = nextId;
        }

        var stranger = FactionFactory.CreateFaction(Game, "log-stranger-" + Guid.NewGuid().ToString("N"));
        var projector = new GameProjector(Game);
        Assert.That(projector.ProjectEntity(ship, stranger.Id).HasView<ShipLogView>(), Is.False);
        var view = projector.ProjectEntity(ship, ship.FactionOwnerID).GetView<ShipLogView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Lines.Select(line => line.Text), Is.EqualTo(book.Entries.Select(entry => entry.Text)));
        Assert.That(view.Lines.Select(line => line.Kind), Is.EqualTo(book.Entries.Select(entry => entry.Kind)));
    }

    [Test]
    public void EmptyChairPlot_WritesAnActionLineAndNoGoalLine()
    {
        var (ship, mars) = MakeMoveShip("Plotter");
        var warp = WarpMoveAction.CreateWarpOnly(ship, mars, _epoch);
        Assert.That(Game.OrderHandler.HandleOrder(warp), Is.True);

        var book = ship.GetDataBlob<ShipLogDB>();
        Assert.That(book.Entries.Select(entry => entry.Kind), Is.EqualTo(new[] { ShipLogKind.Action }));
        Assert.That(book.Entries[0].Text, Does.StartWith("Warp Move"));
    }

    [Test]
    public void ColonyInstall_WritesTheGoalLineAndOneActionLine()
    {
        var faction = FactionFactory.CreateFaction(Game, "log-colony-" + Guid.NewGuid().ToString("N"));
        var colony = Colony(faction, "Yard");
        var design = new ComponentDesign
        {
            UniqueID = "log-mine-" + Guid.NewGuid().ToString("N"),
            Name = "Mine",
            CargoTypeID = "general-storage",
            MassPerUnit = 1000,
            VolumePerUnit = 1,
            ComponentMountType = ComponentMountType.ShipCargo | ComponentMountType.PlanetInstallation,
        };
        var crate = new ComponentInstance(design);
        Assert.That(colony.GetDataBlob<CargoStorageDB>().AddCargoByUnit(crate, 1), Is.EqualTo(1));

        AgentProcessor.AssignGoal(colony, new Goal(GoalType.RunIndustry) { Name = "Fit the mine" });
        Assert.That(Game.OrderHandler.HandleOrder(AddToConstructionQueueOrder.CreateMount(colony, crate)), Is.True);

        var book = colony.GetDataBlob<ShipLogDB>();
        Assert.That(book.Entries.Select(entry => entry.Kind), Is.EqualTo(new[] { ShipLogKind.Own, ShipLogKind.Action }));
        Assert.That(book.Entries[0].Text, Is.EqualTo("Ordered: Fit the mine."));
        Assert.That(book.Entries[1].Text, Is.EqualTo("Install Mine."));
    }

    [Test]
    public void AssigningACaptain_WritesWhoTookCommand_AndWhoLeft()
    {
        var faction = FactionFactory.CreateFaction(Game, "log-captain-" + Guid.NewGuid().ToString("N"));
        var courier = SeatedShip(faction, "Courier");
        var relay = SeatedShip(faction, "Relay");
        faction.GetDataBlob<FleetDB>().AddChild(courier);
        faction.GetDataBlob<FleetDB>().AddChild(relay);

        var ada = CommanderFactory.Create(_sys, faction.Id, new CommanderDB("Ada", 6, CommanderTypes.Navy));
        var translator = new CommandTranslator(Game);

        var seated = translator.Translate(faction, courier, new AssignCaptainCommand(courier.Id, ada.Id));
        Assert.That(seated.Accepted, Is.True, seated.RejectionReason);
        Assert.That(Texts(courier), Is.EqualTo(new[] { "Captain Ada took command." }));

        var again = translator.Translate(faction, courier, new AssignCaptainCommand(courier.Id, ada.Id));
        Assert.That(again.Accepted, Is.True, again.RejectionReason);
        Assert.That(Texts(courier), Is.EqualTo(new[] { "Captain Ada took command." }), "the same chair adds nothing");

        var moved = translator.Translate(faction, relay, new AssignCaptainCommand(relay.Id, ada.Id));
        Assert.That(moved.Accepted, Is.True, moved.RejectionReason);
        Assert.That(Texts(courier), Is.EqualTo(new[] { "Captain Ada took command.", "Captain Ada left command." }));
        Assert.That(Texts(relay), Is.EqualTo(new[] { "Captain Ada took command." }));

        var cleared = translator.Translate(faction, relay, new AssignCaptainCommand(relay.Id, -1));
        Assert.That(cleared.Accepted, Is.True, cleared.RejectionReason);
        Assert.That(Texts(relay), Is.EqualTo(new[] { "Captain Ada took command.", "Captain Ada left command." }));
        Assert.That(relay.GetDataBlob<ShipLogDB>().Entries.All(entry => entry.Kind == ShipLogKind.Own), Is.True);
    }

    [Test]
    public void AssigningAPost_WritesTheColonyLine_AndNotAnAction()
    {
        var faction = FactionFactory.CreateFaction(Game, "log-post-" + Guid.NewGuid().ToString("N"));
        var colony = Colony(faction, "Yard");
        var seats = new AdminSpaceDB();
        seats.CommanderSeats.Add(new AdminSpaceAbilityState(AdminLevel.Colony, "admin"));
        colony.SetDataBlob(seats);

        var ada = CommanderFactory.Create(_sys, faction.Id, new CommanderDB("Ada", 1, CommanderTypes.Civilian));
        Assert.That(Game.OrderHandler.HandleOrder(AssignAdministratorOrder.Create(colony, ada.Id, "admin")), Is.True);
        Assert.That(Texts(colony), Is.EqualTo(new[] { "Ada took the post." }));
        Assert.That(colony.GetDataBlob<ShipLogDB>().Entries[0].Kind, Is.EqualTo(ShipLogKind.Own));

        var lab = Entity.Create(faction.Id);
        _sys.AddEntity(lab, new List<BaseDataBlob>
        {
            new ResearcherDB(null) { LocationId = colony.Id },
        });
        var bea = CommanderFactory.Create(_sys, faction.Id, new CommanderDB("Bea", 1, CommanderTypes.Scientist));
        Assert.That(Game.OrderHandler.HandleOrder(AssignScientistOrder.Create(lab, bea.Id)), Is.True);
        Assert.That(Texts(colony), Is.EqualTo(new[] { "Ada took the post.", "Bea took the lab." }));

        Assert.That(Game.OrderHandler.HandleOrder(UnassignScientistOrder.Create(lab, bea.Id)), Is.True);
        Assert.That(Texts(colony), Is.EqualTo(new[]
        {
            "Ada took the post.",
            "Bea took the lab.",
            "Bea left the lab.",
        }));
    }

    [Test]
    public void FleetSurvey_WritesToAndFrom_AndDoesNotCopyTheShipBook()
    {
        var faction = FactionFactory.CreateFaction(Game, "log-survey-" + Guid.NewGuid().ToString("N"));
        var sol = TestingUtilities.BasicSol(_sys);
        var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, 1.0);
        var mars = AddBody(sol, "Mars", 0.64174e24, 3_396_200, 1.524);
        mars.SetDataBlob(new GeoSurveyableDB { PointsRequired = 100 });
        var leo = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
        var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, _epoch);
        var ship = MakeSurveyShip(earth, earthAbs + new Vector3(leo, 0, 0), faction, "Prospect");

        var fleet = FleetFactory.Create(_sys, faction.Id, "Survey Fleet");
        fleet.GetDataBlob<FleetDB>().AddChild(ship);

        var goal = new Goal(GoalType.ServeyBodies) { TargetEntityID = mars.Id };
        AgentProcessor.AssignGoal(fleet, goal);
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active), goal.Message);

        var fleetBook = fleet.GetDataBlob<ShipLogDB>();
        var shipBook = ship.GetDataBlob<ShipLogDB>();
        Assert.That(fleetBook.Entries.Any(entry => entry.Kind == ShipLogKind.Action), Is.False);
        Assert.That(fleetBook.Entries.Any(entry => entry.Kind == ShipLogKind.To && entry.Text == "To Prospect — Survey Mars."), Is.True);
        Assert.That(shipBook.Entries.Select(entry => entry.Text), Is.EqualTo(new[] { "From Survey Fleet — Survey Mars." }));

        int fleetCount = fleetBook.Entries.Count;
        AgentProcessor.RunAgentNow(fleet);
        Assert.That(fleetBook.Entries.Count, Is.EqualTo(fleetCount), "a second wake with the work unchanged adds nothing");

        AgentProcessor.RunAgentNow(ship);
        Assert.That(ship.GetDataBlob<GoalsDB>().ActiveGoal!.Status, Is.EqualTo(GoalStatus.Active),
            ship.GetDataBlob<GoalsDB>().ActiveGoal!.Message);
        var shipActions = shipBook.Entries.Where(entry => entry.Kind == ShipLogKind.Action).Select(entry => entry.Text).ToList();
        Assert.That(shipActions.Count, Is.GreaterThan(0));
        Assert.That(fleetBook.Entries.Select(entry => entry.Text), Has.No.Member(shipActions[0]));

        int beforeFinish = fleetBook.Entries.Count;
        ship.GetDataBlob<ActionQueueDB>().ActionList.Clear();
        AgentProcessor.RunAgentNow(ship);

        Assert.That(shipBook.Entries.Any(entry => entry.Kind == ShipLogKind.Own && entry.Text == "Mars surveyed."), Is.True);
        Assert.That(fleetBook.Entries.Count, Is.EqualTo(beforeFinish + 1));
        var report = fleetBook.Entries[^1];
        Assert.That(report.Kind, Is.EqualTo(ShipLogKind.From));
        Assert.That(report.Text, Is.EqualTo("From Prospect — Mars surveyed."));
        Assert.That(fleetBook.Entries.Any(entry => entry.Kind == ShipLogKind.Action), Is.False);
        foreach (var text in shipActions)
            Assert.That(fleetBook.Entries.Select(entry => entry.Text), Has.No.Member(text));
        Assert.That(fleetBook.Entries.Select(entry => entry.Text), Has.No.Member("Moving to Mars."));
    }

    [Test]
    public void FailedOrder_StatesWhy_OnTheShipAndOnTheReport()
    {
        var (ship, mars) = MakeMoveShip("Courier");
        var fleet = FleetFactory.Create(_sys, ship.FactionOwnerID, "Survey Fleet");

        var order = new FailedOrder(ship);
        order.FailNow("not enough ΔV");
        order.FailNow("not enough ΔV");
        Assert.That(Texts(ship), Is.EqualTo(new[] { "Warp Move to Mars — not enough ΔV." }));
        Assert.That(ship.GetDataBlob<ShipLogDB>().Entries[0].Kind, Is.EqualTo(ShipLogKind.Action));

        new FailedOrder(fleet).FailNow("not enough ΔV");
        Assert.That(fleet.HasDataBlob<ShipLogDB>(), Is.False);

        var goal = new Goal(GoalType.MoveTo)
        {
            TargetEntityID = mars.Id,
            Status = GoalStatus.Failed,
            Message = "not enough ΔV",
            IssuerEntityId = fleet.Id,
        };
        ShipLogBook.NoteOutcome(ship, goal, _epoch);
        Assert.That(Texts(ship)[^1], Is.EqualTo("Could not reach Mars. Not enough ΔV."));
        Assert.That(Texts(fleet), Is.EqualTo(new[] { "From Courier — Could not reach Mars. Not enough ΔV." }));

        goal.Message = "a move action failed";
        ShipLogBook.NoteOutcome(ship, goal, _epoch);
        Assert.That(Texts(ship)[^1], Is.EqualTo("Could not reach Mars."));

        goal.Message = "no path";
        ShipLogBook.NoteOutcome(ship, goal, _epoch);
        Assert.That(Texts(ship)[^1], Is.EqualTo("Could not reach Mars."));

        goal.Message = "The warp target is missing.";
        ShipLogBook.NoteOutcome(ship, goal, _epoch);
        Assert.That(Texts(ship)[^1], Is.EqualTo("Could not reach Mars. The warp target is missing."));

        goal.Message = "nothing to load";
        ShipLogBook.NoteOutcome(ship, goal, _epoch);
        Assert.That(Texts(ship)[^1], Is.EqualTo("Could not finish. Nothing to load."));

        goal.PlayerCancelled = true;
        goal.Message = "not enough ΔV";
        ShipLogBook.NoteOutcome(ship, goal, _epoch);
        Assert.That(Texts(ship)[^1], Is.EqualTo("Cancelled."));
    }

    (Entity Ship, Entity Mars) MakeMoveShip(string name)
    {
        var faction = FactionFactory.CreateFaction(Game, "log-move-" + Guid.NewGuid().ToString("N"));
        var sol = TestingUtilities.BasicSol(_sys);
        var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, 1.0);
        var mars = AddBody(sol, "Mars", 0.64174e24, 3_396_200, 1.524);
        var leo = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
        var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, _epoch);
        var ship = MakeFueledShip(earth, earthAbs + new Vector3(leo, 0, 0), faction, name);
        return (ship, mars);
    }

    static List<string> Texts(Entity entity)
        => entity.GetDataBlob<ShipLogDB>().Entries.Select(entry => entry.Text).ToList();

    Entity SeatedShip(Entity faction, string name)
    {
        var ship = Entity.Create(faction.Id);
        _sys.AddEntity(ship, new List<BaseDataBlob>
        {
            new ShipInfoDB(),
            new NameDB(name, faction.Id, name),
        });
        return ship;
    }

    Entity Colony(Entity faction, string name)
    {
        var colony = Entity.Create(faction.Id);
        _sys.AddEntity(colony, new List<BaseDataBlob>
        {
            new NameDB(name, faction.Id, name),
            new ColonyInfoDB(),
            new CargoStorageDB("general-storage", 100000),
            new ComponentInstancesDB(),
            new LocalConstructionDB { PointsPerDay = 10 },
        });
        return colony;
    }

    Entity AddBody(Entity parent, string name, double mass, double radius, double smaAu)
    {
        var orbit = OrbitDB.FromAsteroidFormat(parent, parent.GetDataBlob<MassVolumeDB>().MassDry, mass, smaAu, 0, 0, 0, 0, 0, _epoch);
        var body = Entity.Create();
        var pos = new PositionDB();
        _sys.AddEntity(body, new BaseDataBlob[]
        {
            pos,
            MassVolumeDB.NewFromMassAndRadius_m(mass, radius),
            orbit,
            new NameDB(name),
        });
        OrbitProcessor.ProcessEntity(body, _epoch);
        return body;
    }

    Entity MakeSurveyShip(Entity parent, Vector3 absolute, Entity faction, string name)
    {
        var ship = MakeFueledShip(parent, absolute, faction, name);
        ship.SetDataBlob(new GeoSurveyAbilityDB { Speed = 600 });
        return ship;
    }

    Entity MakeFueledShip(Entity parent, Vector3 absolute, Entity faction, string name)
    {
        var pos = new PositionDB { AbsolutePosition = absolute };
        var energy = new EnergyGenAbilityDB(_epoch)
        {
            EnergyType = new TestEnergyType(),
            EnergyStored = { ["electricity"] = 1e12 },
            EnergyStoreMax = { ["electricity"] = 1e12 },
        };
        var ship = Entity.Create();
        _sys.AddEntity(ship, new BaseDataBlob[]
        {
            pos,
            MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" },
            energy,
            new ActionQueueDB(),
            new ShipInfoDB(),
            new NameDB(name, faction.Id, name),
        });
        pos.SetParent(parent);
        ship.FactionOwnerID = faction.Id;
        ship.SetDataBlob(OrbitDB.FromPosition(parent, ship, _epoch));
        OrbitProcessor.ProcessEntity(ship, _epoch);

        var thrust = new NewtonThrustAbilityDB("test-fuel")
        {
            ThrustInNewtons = 1e9,
            ExhaustVelocity = 10_000,
            FuelBurnRate = 1e9 / 10_000,
        };
        double wet = ship.GetDataBlob<MassVolumeDB>().MassTotal;
        double dry = wet / Math.Exp(20_000 / thrust.ExhaustVelocity);
        thrust.SetFuel(Math.Max(wet - dry, 1), wet);
        ship.SetDataBlob(thrust);

        var data = faction.GetDataBlob<FactionInfoDB>().Data;
        data.Unlock("methalox");
        var fuel = data.CargoGoods.GetAny("methalox");
        thrust.FuelType = fuel.UniqueID;
        var storage = new CargoStorageDB(fuel.CargoTypeID, 1e12);
        storage.AddCargoByUnit(fuel, 1_000_000);
        ship.SetDataBlob(storage);
        return ship;
    }

    sealed class FailedOrder : EntityAction
    {
        readonly Entity _ship;

        public FailedOrder(Entity ship)
        {
            _ship = ship;
            BindCommanding(ship);
        }

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;
        public override bool IsBlocking => false;
        public override string Name => "Warp Move to Mars";
        public override string Details => "Warping";
        internal override bool IsValidCommand(Game game) => true;
        internal override void Execute(DateTime atDateTime) { }
        internal override bool IsFinished() => Status == ActionStatus.Failed;
        public override EntityAction Clone() => new FailedOrder(_ship);
        public void FailNow(string why) => Fail(why);
    }

    sealed class TestEnergyType : ICargoable
    {
        public int ID => 0;
        public string UniqueID => "electricity";
        public string CargoTypeID => "energy";
        public string Name => "electricity";
        public long MassPerUnit => 0;
        public double VolumePerUnit => 0;
    }
}
