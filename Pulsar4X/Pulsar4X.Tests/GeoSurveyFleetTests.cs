using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using GameEngine.People;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Messaging;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Api;
using Pulsar4X.Engine.Api;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Fleet <see cref="GoalType.ServeyBodies"/> on Mars: two surveyors + a tanker.
    /// <see cref="ServeyBodyPlanner"/> hands POIs (Mars + moons) to geo-capable ships
    /// and sends a flagged fleet tanker to orbit the parent via <see cref="GoalType.MoveTo"/>.
    /// </summary>
    public class GeoSurveyFleetTests
    {
        private static readonly Game Game = InitializeGame();
        private StarSystem _sys;
        private DateTime _epoch;

        static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);
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
        public void GeoSurveyOrder_NameAndDetails_UseOwnerFactionProgress()
        {
            var scene = BuildMarsSurveyFleet();
            var order = new GeoSurveyOrder(scene.SurveyorA, scene.Mars);

            Assert.That(order.Details, Is.EqualTo("0%"));
            Assert.That(order.Name, Does.Contain("(0%)"));

            var geo = scene.Mars.GetDataBlob<GeoSurveyableDB>();
            geo.GeoSurveyStatus[scene.Faction.Id] = geo.PointsRequired / 2;

            Assert.That(order.Details, Is.EqualTo("50%"));
            Assert.That(order.Name, Does.Contain("(50%)"));
        }

        [Test]
        public void GeoSurveyOrder_publishes_the_body_when_the_survey_completes()
        {
            var scene = BuildMarsSurveyFleet();
            var body = scene.Mars;
            var geo = body.GetDataBlob<GeoSurveyableDB>();
            geo.PointsRequired = 1;

            var messages = new List<Message>();
            MessagePublisher.MessageHandler handler = m =>
            {
                messages.Add(m);
                return Task.CompletedTask;
            };
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityChanged, handler);
            try
            {
                var order = new GeoSurveyOrder(scene.SurveyorA, body);
                order.Execute(_epoch);
                messages.Clear();
                order.Execute(_epoch.AddDays(1));
            }
            finally
            {
                MessagePublisher.Instance.Unsubscribe(MessageTypes.EntityChanged, handler);
            }

            Assert.That(geo.IsSurveyComplete(scene.Faction.Id), Is.True);
            Assert.That(messages.Any(m => m.EntityId == body.Id && m.FactionId == scene.Faction.Id),
                Is.True, "the body window reads GeoSurveyView from the body's snapshot");
        }

        [Test]
        public void ServeyBodyPlanner_AssignsMarsPhobos_AndTankerMoveToParent()
        {
            var scene = BuildMarsSurveyFleet();

            var planner = new ServeyBodyPlanner();
            var plan = planner.Plan(scene.Fleet, scene.Goal, _epoch);

            Assert.AreNotEqual(GoalStatus.Failed, plan.Status, plan.Message);
            Assert.AreEqual(3, plan.SubGoals.Count,
                "two surveyors + tanker MoveTo parent");

            var assignedShips = plan.SubGoals.Select(s => s.Sub.Id).ToHashSet();
            Assert.IsTrue(assignedShips.Contains(scene.SurveyorA.Id), "Surveyor A should be tasked");
            Assert.IsTrue(assignedShips.Contains(scene.SurveyorB.Id), "Surveyor B should be tasked");
            Assert.IsTrue(assignedShips.Contains(scene.Tanker.Id), "tanker should MoveTo the parent");

            var surveyGoals = plan.SubGoals.Where(s => s.Goal.Type == GoalType.ServeyBodies).ToList();
            Assert.AreEqual(2, surveyGoals.Count);
            Assert.AreEqual(scene.Mars.Id, surveyGoals[0].Goal.TargetEntityID,
                "targeted parent is first");
            Assert.AreEqual(scene.Phobos.Id, surveyGoals[1].Goal.TargetEntityID,
                "inner moon (Phobos) before outer (Deimos)");

            var tankerGoal = plan.SubGoals.Single(s => s.Sub.Id == scene.Tanker.Id);
            Assert.AreEqual(GoalType.MoveTo, tankerGoal.Goal.Type);
            Assert.AreEqual(scene.Mars.Id, tankerGoal.Goal.TargetEntityID,
                "tanker orbits the targeted parent, not a moon");
        }

        [Test]
        public void ServeyBodyPlanner_BodySpan_AssignsParentOnly()
        {
            var scene = BuildMarsSurveyFleet();
            AttachBridge(scene.SurveyorA, AdminLevel.Ship);

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);
            var surveyGoals = plan.SubGoals.Where(s => s.Goal.Type == GoalType.ServeyBodies).ToList();
            Assert.AreEqual(1, surveyGoals.Count, plan.Message);
            Assert.AreEqual(scene.Mars.Id, surveyGoals[0].Goal.TargetEntityID);
        }

        [Test]
        public void ServeyBodyPlanner_SystemSpan_MoonTarget_AssignsMoonNotMercury()
        {
            var scene = BuildEarthMoonSurveyFleet(AdminLevel.System, targetEarth: false);

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);

            Assert.AreNotEqual(GoalStatus.Failed, plan.Status, plan.Message);
            var ids = SurveyTargets(plan);
            Assert.That(ids, Is.EquivalentTo(new[] { scene.Luna.Id }),
                "Luna has no children; System span must not take Earth or Mercury");

            var tankerGoal = plan.SubGoals.Single(s => s.Sub.Id == scene.Tanker.Id);
            Assert.AreEqual(GoalType.MoveTo, tankerGoal.Goal.Type);
            Assert.AreEqual(scene.Luna.Id, tankerGoal.Goal.TargetEntityID);
        }

        [Test]
        public void ServeyBodyPlanner_SystemSpan_Star_SurveysBodies_NotTheStar()
        {
            var scene = BuildEarthMoonSurveyFleet(AdminLevel.System, targetEarth: true);
            var sol = scene.Earth.GetDataBlob<PositionDB>().Parent;
            Assert.That(sol, Is.Not.Null);
            sol.SetDataBlob(new StarInfoDB());
            sol.SetDataBlob(new GeoSurveyableDB { PointsRequired = 50 });
            scene.Goal.TargetEntityID = sol.Id;

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);

            Assert.AreNotEqual(GoalStatus.Failed, plan.Status, plan.Message);
            var ids = SurveyTargets(plan);
            Assert.That(ids, Does.Not.Contain(sol.Id));
            Assert.That(ids.Count, Is.EqualTo(2));
            Assert.That(ids.All(id => id == scene.Mercury.Id || id == scene.Earth.Id || id == scene.Luna.Id), Is.True);
            Assert.That(plan.SubGoals.Any(s => s.Sub.Id == scene.Tanker.Id), Is.False,
                "tanker must not be sent to the star");
        }

        [Test]
        public void ServeyBodyPlanner_SystemSpan_Planet_IncludesMoonNotMercury()
        {
            var scene = BuildEarthMoonSurveyFleet(AdminLevel.System, targetEarth: true);

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);
            var ids = SurveyTargets(plan);

            Assert.That(ids, Is.EquivalentTo(new[] { scene.Earth.Id, scene.Luna.Id }),
                "Earth plus its moon; Mercury is a sibling and stays out");
        }

        [Test]
        public void ServeyBodyPlanner_Asteroid_AssignsNeighborsInsideBaseRadius()
        {
            var scene = BuildAsteroidSurveyFleet(tanker: false, surveyors: 3);

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);
            var ids = SurveyTargets(plan);

            Assert.AreNotEqual(GoalStatus.Failed, plan.Status, plan.Message);
            Assert.That(ids, Is.EquivalentTo(new[] { scene.Anchor.Id, scene.Near.Id }));
        }

        [Test]
        public void ServeyBodyPlanner_Asteroid_TankerWidensTheRadius()
        {
            var scene = BuildAsteroidSurveyFleet(tanker: true, surveyors: 3);

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);
            var ids = SurveyTargets(plan);

            Assert.That(ids, Is.EquivalentTo(new[] { scene.Anchor.Id, scene.Near.Id, scene.Mid.Id }));
            var tankerGoal = plan.SubGoals.Single(s => s.Sub.Id == scene.Tanker.Id);
            Assert.AreEqual(GoalType.MoveTo, tankerGoal.Goal.Type);
            Assert.AreEqual(scene.Anchor.Id, tankerGoal.Goal.TargetEntityID);
        }

        [Test]
        public void ServeyBodyPlanner_Asteroid_StuckTankerKeepsTheBaseRadius()
        {
            var scene = BuildAsteroidSurveyFleet(tanker: true, surveyors: 3);
            scene.Tanker.RemoveDataBlob<WarpAbilityDB>();
            scene.Tanker.RemoveDataBlob<NewtonThrustAbilityDB>();

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);
            var ids = SurveyTargets(plan);

            Assert.That(ids, Is.EquivalentTo(new[] { scene.Anchor.Id, scene.Near.Id }));
            Assert.IsFalse(plan.SubGoals.Any(s => s.Sub.Id == scene.Tanker.Id));
        }

        [Test]
        public void ServeyBodyPlanner_Asteroid_ShipSpanStillIncludesTheNearRock()
        {
            var scene = BuildAsteroidSurveyFleet(tanker: false, surveyors: 2);
            AttachBridge(scene.SurveyorA, AdminLevel.Ship);
            scene.Fleet.GetDataBlob<FleetDB>().FlagShipID = scene.SurveyorA.Id;

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);
            var ids = SurveyTargets(plan);

            Assert.That(ids, Is.EquivalentTo(new[] { scene.Anchor.Id, scene.Near.Id }));
        }

        [Test]
        public void ServeyBodyPlanner_PlanetTarget_IgnoresNearbyAsteroid()
        {
            var scene = BuildMarsSurveyFleet();
            var sol = scene.Mars.GetDataBlob<PositionDB>().Parent!;
            var marsPos = scene.Mars.GetDataBlob<PositionDB>().AbsolutePosition;
            var rock = AddPlacedBody(sol, "Nearby rock", marsPos + new Vector3(0.1 * AuMetres, 0, 0), BodyType.Asteroid);
            var earth = scene.SurveyorA.GetDataBlob<PositionDB>().Parent!;
            var earthAbs = earth.GetDataBlob<PositionDB>().AbsolutePosition;
            var leoR = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var fleet = scene.Fleet.GetDataBlob<FleetDB>();
            fleet.AddChild(MakeSurveyShip(earth, earthAbs + new Vector3(leoR, leoR, 0), scene.Faction, "Surveyor C"));
            fleet.AddChild(MakeSurveyShip(earth, earthAbs + new Vector3(-leoR, leoR, 0), scene.Faction, "Surveyor D"));

            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);
            var ids = SurveyTargets(plan);

            Assert.That(ids, Is.EquivalentTo(new[] { scene.Mars.Id, scene.Phobos.Id, scene.Deimos.Id }));
            Assert.IsFalse(ids.Contains(rock.Id));
        }

        [Test, Timeout(15000)]
        public void FleetSurvey_MarsAndMoons_ShipsSurvey_TankerMovesToParent()
        {
            var scene = BuildMarsSurveyFleet();

            AgentProcessor.AssignGoal(scene.Fleet, scene.Goal);
            Assert.AreNotEqual(GoalStatus.Failed, scene.Goal.Status, scene.Goal.Message);

            // ProcessSystem, not TimeStep: the static Game's master pulse can keep a
            // jump-pair key at GameGlobalDateTime and SimulateTimeUntil 0-spans.
            // One day is past Earth→Mars warp; GeoSurveyOrder gates points to +1 day
            // after Execute starts (~t+26h). That first tick currently hangs the
            // subpulse (replan after complete → warp-to-self), so stop at 24h.
            var wall = Stopwatch.StartNew();
            _sys.ManagerSubpulses.ProcessSystem(_epoch + TimeSpan.FromDays(1));
            Assert.Less(wall.ElapsedMilliseconds, 10000,
                "1-day ProcessSystem hung. " + Describe(scene));

            string dump = Describe(scene);
            Assert.AreNotEqual(GoalStatus.Failed, scene.Goal.Status, dump);
            Assert.IsTrue(IsSurveying(scene.SurveyorA), "Surveyor A should be geo-surveying. " + dump);
            Assert.IsTrue(IsSurveying(scene.SurveyorB), "Surveyor B should be geo-surveying. " + dump);

            var targets = new HashSet<int>();
            if (scene.SurveyorA.TryGetDataBlob<GoalsDB>(out var ga) && ga.ActiveGoal != null)
                targets.Add(ga.ActiveGoal.TargetEntityID);
            if (scene.SurveyorB.TryGetDataBlob<GoalsDB>(out var gb) && gb.ActiveGoal != null)
                targets.Add(gb.ActiveGoal.TargetEntityID);
            Assert.IsTrue(targets.SetEquals(new[] { scene.Mars.Id, scene.Phobos.Id }),
                "parent then inner moon; Deimos waits. " + dump);

            Assert.IsFalse(HasSurveySubgoal(scene.Tanker, scene.Goal),
                "tanker is support, not a surveyor. " + dump);
            Assert.IsTrue(scene.Tanker.TryGetDataBlob<GoalsDB>(out var tankerGoals)
                          && tankerGoals.ActiveGoal != null
                          && tankerGoals.ActiveGoal.Type == GoalType.MoveTo
                          && tankerGoals.ActiveGoal.TargetEntityID == scene.Mars.Id,
                "tanker should MoveTo the parent body. " + dump);
        }

        [Test]
        public void ServeyBodyPlanner_LeavesAShipOnItsOwnGoal_AndTasksTheSibling()
        {
            var scene = BuildMarsSurveyFleet();
            GiveOwnGoal(scene.SurveyorA, GoalType.MoveTo, scene.Mars.Id);
            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);

            Assert.AreEqual(GoalStatus.Active, plan.Status, plan.Message);
            Assert.IsFalse(plan.SubGoals.Any(s => s.Sub.Id == scene.SurveyorA.Id),
                "a ship with its own order is not given a survey");
            Assert.IsTrue(plan.SubGoals.Any(s => s.Sub.Id == scene.SurveyorB.Id));
        }

        [Test]
        public void ServeyBodyPlanner_DoesNotRetaskATankerOnItsOwnGoal()
        {
            var scene = BuildMarsSurveyFleet();
            GiveOwnGoal(scene.Tanker, GoalType.MoveTo, scene.Mars.Id);
            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);

            Assert.IsFalse(plan.SubGoals.Any(s => s.Sub.Id == scene.Tanker.Id));
            Assert.IsTrue(plan.SubGoals.Any(s => s.Goal.Type == GoalType.ServeyBodies));
        }

        [Test]
        public void ServeyBodyPlanner_TasksAShipOnceItsOwnGoalHasFinished()
        {
            var scene = BuildMarsSurveyFleet();
            var done = new Goal(GoalType.MoveTo)
            {
                TargetEntityID = scene.Mars.Id,
                Status = GoalStatus.Completed,
            };
            scene.SurveyorA.SetDataBlob(new GoalsDB { ActiveGoal = done, GivenGoal = done });
            var plan = new ServeyBodyPlanner().Plan(scene.Fleet, scene.Goal, _epoch);

            Assert.IsTrue(plan.SubGoals.Any(s => s.Sub.Id == scene.SurveyorA.Id));
        }

        [Test]
        public void MoveToPlan_LeavesOwnGoalAndPlayerPlot_AndStaysActive()
        {
            var scene = BuildMarsSurveyFleet();
            GiveOwnGoal(scene.SurveyorA, GoalType.MoveTo, scene.Mars.Id);
            scene.SurveyorB.GetDataBlob<ActionQueueDB>().Enqueue(new RenameAction());
            var fleetGoal = new Goal(GoalType.MoveTo) { TargetEntityID = scene.Mars.Id };
            var plan = new MoveToPlan().Plan(scene.Fleet, fleetGoal, _epoch);

            Assert.AreEqual(GoalStatus.Active, plan.Status, plan.Message);
            Assert.IsFalse(plan.SubGoals.Any(s => s.Sub.Id == scene.SurveyorA.Id));
            Assert.IsFalse(plan.SubGoals.Any(s => s.Sub.Id == scene.SurveyorB.Id));
            Assert.IsTrue(plan.SubGoals.Any(s => s.Sub.Id == scene.Tanker.Id),
                "a ship with no order of its own is still tasked");
        }

        [Test]
        public void MoveToBody_RequiresACaptain_WarpDoesNot()
        {
            var scene = BuildMarsSurveyFleet();
            var translator = new CommandTranslator(Game);

            var blocked = translator.Translate(scene.Faction, scene.SurveyorA,
                new MoveToBodyCommand(scene.SurveyorA.Id, scene.Mars.Id));
            Assert.IsFalse(blocked.Accepted);
            Assert.That(blocked.RejectionReason, Does.Contain("captain"));

            // Warp rejects a body this faction has not seen. Test planets are unowned until marked neutral.
            scene.Mars.FactionOwnerID = Pulsar4X.Engine.Game.NeutralFactionId;
            _sys.ShowNeutralEntityToFaction(scene.Faction.Id, scene.Mars.Id);
            var warp = translator.Translate(scene.Faction, scene.SurveyorA,
                new WarpMoveCommand(scene.SurveyorA.Id, scene.Mars.Id));
            Assert.IsTrue(warp.Accepted, warp.RejectionReason);
            Assert.IsTrue(scene.SurveyorA.GetDataBlob<ActionQueueDB>().ActionList.Count > 0);

            var captain = CommanderFactory.Create(_sys, scene.Faction.Id, CommanderFactory.CreateShipCaptain(Game));
            scene.SurveyorB.GetDataBlob<ShipInfoDB>().CommanderID = captain.Id;
            var allowed = translator.Translate(scene.Faction, scene.SurveyorB,
                new MoveToBodyCommand(scene.SurveyorB.Id, scene.Mars.Id));
            Assert.IsTrue(allowed.Accepted, allowed.RejectionReason);
            Assert.IsTrue(scene.SurveyorB.TryGetDataBlob<GoalsDB>(out var goals)
                          && goals.ActiveGoal != null
                          && goals.ActiveGoal.Type == GoalType.MoveTo);
        }

        [Test]
        public void GoToBody_EmptyChair_QueuesActions_WithoutAGoal()
        {
            var scene = BuildMarsSurveyFleet();
            var translator = new CommandTranslator(Game);
            scene.Mars.FactionOwnerID = Pulsar4X.Engine.Game.NeutralFactionId;
            _sys.ShowNeutralEntityToFaction(scene.Faction.Id, scene.Mars.Id);

            var plotted = translator.Translate(scene.Faction, scene.SurveyorA,
                new GoToBodyCommand(scene.SurveyorA.Id, scene.Mars.Id));
            Assert.IsTrue(plotted.Accepted, plotted.RejectionReason);
            Assert.IsFalse(scene.SurveyorA.HasDataBlob<GoalsDB>());
            Assert.Greater(scene.SurveyorA.GetDataBlob<ActionQueueDB>().ActionList.Count, 0);

            scene.SurveyorB.GetDataBlob<ShipInfoDB>().CommanderID =
                CommanderFactory.Create(_sys, scene.Faction.Id, CommanderFactory.CreateShipCaptain(Game)).Id;
            var blocked = translator.Translate(scene.Faction, scene.SurveyorB,
                new GoToBodyCommand(scene.SurveyorB.Id, scene.Mars.Id));
            Assert.IsFalse(blocked.Accepted);
            Assert.That(blocked.RejectionReason, Does.Contain("captain"));
        }

        [Test]
        public void GeoSurvey_EmptyChair_AtBody_QueuesOrderWithoutAGoal()
        {
            var scene = BuildMarsSurveyFleet();
            var translator = new CommandTranslator(Game);
            scene.Mars.FactionOwnerID = Pulsar4X.Engine.Game.NeutralFactionId;
            _sys.ShowNeutralEntityToFaction(scene.Faction.Id, scene.Mars.Id);
            ParkAtBody(scene.SurveyorA, scene.Mars);

            var plotted = translator.Translate(scene.Faction, scene.SurveyorA,
                new GeoSurveyCommand(scene.SurveyorA.Id, scene.Mars.Id));
            Assert.IsTrue(plotted.Accepted, plotted.RejectionReason);
            Assert.IsFalse(scene.SurveyorA.HasDataBlob<GoalsDB>());
            Assert.That(scene.SurveyorA.GetDataBlob<ActionQueueDB>().ActionList,
                Has.Some.InstanceOf<GeoSurveyOrder>());
        }

        [Test]
        public void GeoSurvey_EmptyChair_NotAtBody_IsRejected()
        {
            var scene = BuildMarsSurveyFleet();
            var translator = new CommandTranslator(Game);
            scene.Mars.FactionOwnerID = Pulsar4X.Engine.Game.NeutralFactionId;
            _sys.ShowNeutralEntityToFaction(scene.Faction.Id, scene.Mars.Id);

            var blocked = translator.Translate(scene.Faction, scene.SurveyorA,
                new GeoSurveyCommand(scene.SurveyorA.Id, scene.Mars.Id));
            Assert.IsFalse(blocked.Accepted);
            Assert.That(blocked.RejectionReason, Does.Contain("not at").IgnoreCase);
            Assert.IsFalse(scene.SurveyorA.HasDataBlob<GoalsDB>());
        }

        static void ParkAtBody(Entity ship, Entity body)
        {
            if (ship.HasDataBlob<OrbitDB>())
                ship.RemoveDataBlob<OrbitDB>();
            var pos = ship.GetDataBlob<PositionDB>();
            pos.SetParent(body);
            pos.RelativePosition = Vector3.Zero;
        }

        static void GiveOwnGoal(Entity ship, GoalType type, int targetId)
        {
            var goal = new Goal(type) { TargetEntityID = targetId, Status = GoalStatus.Active };
            ship.SetDataBlob(new GoalsDB { ActiveGoal = goal, GivenGoal = goal });
        }

        Scene BuildMarsSurveyFleet()
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, smaAu: 1.0, geoPoints: 50);
            var mars = AddBody(sol, "Mars", 0.64174e24, 3_396_200, smaAu: 1.524, geoPoints: 575);
            var phobos = AddMoon(mars, "Phobos", 1.07e16, 11_100, sma_m: 9_376_000, geoPoints: 100, e: 0.0151);
            var deimos = AddMoon(mars, "Deimos", 1.51e15, 6_200, sma_m: 23_463_200, geoPoints: 50, e: 0.00033);

            Assert.Contains(phobos, mars.GetDataBlob<PositionDB>().Children.ToList(),
                "Phobos must be a PositionDB child of Mars for PlanSubGoals");
            Assert.Contains(deimos, mars.GetDataBlob<PositionDB>().Children.ToList(),
                "Deimos must be a PositionDB child of Mars for PlanSubGoals");

            var faction = FactionFactory.CreateFaction(Game, "survey-" + Guid.NewGuid().ToString("N"));
            var leoR = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, _epoch);

            var surveyorA = MakeSurveyShip(earth, earthAbs + new Vector3(leoR, 0, 0), faction, "Surveyor A");
            var surveyorB = MakeSurveyShip(earth, earthAbs + new Vector3(0, leoR, 0), faction, "Surveyor B");
            var tanker = MakeTanker(earth, earthAbs + new Vector3(-leoR, 0, 0), faction, "Tanker");

            var fleet = FleetFactory.Create(_sys, faction.Id, "Mars Survey Flotilla");
            fleet.GetDataBlob<FleetDB>().AddChild(surveyorA);
            fleet.GetDataBlob<FleetDB>().AddChild(surveyorB);
            fleet.GetDataBlob<FleetDB>().AddChild(tanker);
            fleet.GetDataBlob<FleetDB>().FlagShipID = surveyorA.Id;
            AttachBridge(surveyorA, AdminLevel.Planet);

            var fuel = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
            Assert.IsTrue(FuelSituation.TryFindFleetTanker(surveyorA, fuel, out var tankerId, out var tankerReason),
                tankerReason);
            Assert.AreEqual(tanker.Id, tankerId, "FuelSituation should pick the fleet mate with the most fuel");

            return new Scene
            {
                Faction = faction,
                Fleet = fleet,
                Goal = new Goal(GoalType.ServeyBodies) { TargetEntityID = mars.Id },
                SurveyorA = surveyorA,
                SurveyorB = surveyorB,
                Tanker = tanker,
                Mars = mars,
                Phobos = phobos,
                Deimos = deimos,
            };
        }

        Scene BuildEarthMoonSurveyFleet(AdminLevel flagshipLevel, bool targetEarth)
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var mercury = AddBody(sol, "Mercury", 0.330e24, 2_440_000, smaAu: 0.387, geoPoints: 50);
            var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, smaAu: 1.0, geoPoints: 50);
            var luna = AddMoon(earth, "Luna", 7.346e22, 1_737_400, sma_m: 384_400_000, geoPoints: 50, e: 0.055);

            var faction = FactionFactory.CreateFaction(Game, "luna-" + Guid.NewGuid().ToString("N"));
            var leoR = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, _epoch);
            var surveyorA = MakeSurveyShip(earth, earthAbs + new Vector3(leoR, 0, 0), faction, "Surveyor A");
            var surveyorB = MakeSurveyShip(earth, earthAbs + new Vector3(0, leoR, 0), faction, "Surveyor B");
            var tanker = MakeTanker(earth, earthAbs + new Vector3(-leoR, 0, 0), faction, "Tanker");

            var fleet = FleetFactory.Create(_sys, faction.Id, "Earth Moon Survey");
            var fleetDB = fleet.GetDataBlob<FleetDB>();
            fleetDB.AddChild(surveyorA);
            fleetDB.AddChild(surveyorB);
            fleetDB.AddChild(tanker);
            fleetDB.FlagShipID = surveyorA.Id;
            AttachBridge(surveyorA, flagshipLevel);

            return new Scene
            {
                Faction = faction,
                Fleet = fleet,
                Goal = new Goal(GoalType.ServeyBodies)
                {
                    TargetEntityID = targetEarth ? earth.Id : luna.Id,
                },
                SurveyorA = surveyorA,
                SurveyorB = surveyorB,
                Tanker = tanker,
                Mercury = mercury,
                Earth = earth,
                Luna = luna,
            };
        }

        static double AuMetres => SbdbSmallBodyImporter.AuInKm * 1000.0;

        static HashSet<int> SurveyTargets(PlanResult plan)
        {
            return plan.SubGoals
                .Where(s => s.Goal.Type == GoalType.ServeyBodies)
                .Select(s => s.Goal.TargetEntityID)
                .ToHashSet();
        }

        AsteroidScene BuildAsteroidSurveyFleet(bool tanker, int surveyors)
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var origin = sol.GetDataBlob<PositionDB>().AbsolutePosition;
            var anchor = AddPlacedBody(sol, "Anchor", origin + new Vector3(3 * AuMetres, 0, 0), BodyType.Asteroid);
            var near = AddPlacedBody(sol, "Near", origin + new Vector3(3.1 * AuMetres, 0, 0), BodyType.Asteroid);
            var mid = AddPlacedBody(sol, "Mid", origin + new Vector3(3.35 * AuMetres, 0, 0), BodyType.Asteroid);
            var far = AddPlacedBody(sol, "Far", origin + new Vector3(3.8 * AuMetres, 0, 0), BodyType.Asteroid);

            var faction = FactionFactory.CreateFaction(Game, "rocks-" + Guid.NewGuid().ToString("N"));
            var fleet = FleetFactory.Create(_sys, faction.Id, "Rock Survey");
            var fleetDB = fleet.GetDataBlob<FleetDB>();
            Entity first = null!;
            for (int i = 0; i < surveyors; i++)
            {
                var ship = MakeSurveyShip(sol, origin + new Vector3(3 * AuMetres, 10_000 * (i + 1), 0), faction, "Surveyor " + i);
                fleetDB.AddChild(ship);
                if (i == 0)
                    first = ship;
            }
            fleetDB.FlagShipID = first.Id;

            Entity tankerEntity = null!;
            if (tanker)
            {
                tankerEntity = MakeTanker(sol, origin + new Vector3(3 * AuMetres, -10_000, 0), faction, "Tanker");
                fleetDB.AddChild(tankerEntity);
            }

            return new AsteroidScene
            {
                Fleet = fleet,
                Goal = new Goal(GoalType.ServeyBodies) { TargetEntityID = anchor.Id },
                SurveyorA = first,
                Tanker = tankerEntity,
                Anchor = anchor,
                Near = near,
                Mid = mid,
                Far = far,
            };
        }

        Entity AddPlacedBody(Entity parent, string name, Vector3 absolute, BodyType kind)
        {
            var ent = Entity.Create();
            var pos = new PositionDB();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(1e18, 100_000),
                new NameDB(name),
                new GeoSurveyableDB { PointsRequired = 100 },
            });
            ent.SetDataBlob(new SystemBodyInfoDB { BodyType = kind });
            pos.SetParent(parent);
            pos.AbsolutePosition = absolute;
            return ent;
        }

        Entity AddBody(Entity parent, string name, double mass, double radius_m, double smaAu, uint geoPoints)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, 0, 0, 0, 0, 0, _epoch);
            var ent = Entity.Create();
            var pos = new PositionDB();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB(name),
                new GeoSurveyableDB { PointsRequired = geoPoints },
            });
            OrbitProcessor.ProcessEntity(ent, _epoch);
            return ent;
        }

        Entity AddMoon(Entity parent, string name, double mass, double radius_m, double sma_m, uint geoPoints, double e)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            double smaAu = Distance.MToAU(sma_m);
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, e, 0, 0, 0, 0, _epoch);
            var ent = Entity.Create();
            var pos = new PositionDB();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB(name),
                new GeoSurveyableDB { PointsRequired = geoPoints },
            });
            OrbitProcessor.ProcessEntity(ent, _epoch);
            return ent;
        }

        static void AttachBridge(Entity ship, AdminLevel level)
        {
            var admin = new AdminSpaceDB();
            admin.CommanderSeats.Add(new AdminSpaceAbilityState(level, "command-bridge"));
            ship.SetDataBlob(admin);
        }

        Entity MakeSurveyShip(Entity parent, Vector3 abs, Entity faction, string name)
        {
            var ship = MakeFueledWarpShip(parent, abs, faction, name);
            ship.SetDataBlob(new GeoSurveyAbilityDB { Speed = 600 });
            return ship;
        }

        Entity MakeTanker(Entity parent, Vector3 abs, Entity faction, string name)
        {
            var ship = MakeFueledWarpShip(parent, abs, faction, name);
            ship.GetDataBlob<ShipInfoDB>().Tanker = true;
            var fuel = faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
            ship.GetDataBlob<CargoStorageDB>().AddCargoByUnit(fuel, 20_000_000);
            return ship;
        }

        Entity MakeFueledWarpShip(Entity parent, Vector3 abs, Entity faction, string name)
        {
            var pos = new PositionDB { AbsolutePosition = abs };
            var warp = new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" };
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
                warp,
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

        static bool HasSurveySubgoal(Entity ship, Goal parent)
        {
            if (!ship.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
                return false;
            return goals.ActiveGoal.ParentGoalId == parent.Id
                   && goals.ActiveGoal.Type == GoalType.ServeyBodies;
        }

        static bool IsSurveying(Entity ship)
        {
            return ship.TryGetDataBlob<ActionQueueDB>(out var q)
                   && q.ActionList.Any(a => a is GeoSurveyOrder && a.Status != ActionStatus.Failed);
        }

        string Describe(Scene scene)
        {
            string Body(Entity e)
            {
                e.TryGetDataBlob<GeoSurveyableDB>(out var g);
                var done = g != null && g.IsSurveyComplete(scene.Fleet.FactionOwnerID);
                var left = g != null && g.GeoSurveyStatus.TryGetValue(scene.Fleet.FactionOwnerID, out var v) ? v : g?.PointsRequired;
                var n = e.TryGetDataBlob<NameDB>(out var ndb) ? ndb.DefaultName : e.Id.ToString();
                return $"{n}:done={done} left={left}";
            }

            string Ship(Entity s)
            {
                var g = s.TryGetDataBlob<GoalsDB>(out var gd) && gd.ActiveGoal != null
                    ? $"{gd.ActiveGoal.Type}:{gd.ActiveGoal.Status} tgt={gd.ActiveGoal.TargetEntityID} '{gd.ActiveGoal.Message}'"
                    : "no-goal";
                var q = s.TryGetDataBlob<ActionQueueDB>(out var aq)
                    ? string.Join(",", aq.ActionList.Select(x => $"{x.GetType().Name}:{x.Status}"))
                    : "";
                var n = s.TryGetDataBlob<NameDB>(out var ndb) ? ndb.DefaultName : s.Id.ToString();
                double fuelFrac = -1;
                if (FuelSituation.TryGetFuelMass(s, out _, out var stored, out var cap) && cap > 0)
                    fuelFrac = stored / cap;
                return $"{n} [{g}] fuel={fuelFrac:0.00} queue=[{q}]";
            }

            return $"t={_sys.StarSysDateTime:u} fleetGoal={scene.Goal.Status} '{scene.Goal.Message}' "
                   + $"{Body(scene.Mars)}; {Body(scene.Phobos)}; {Body(scene.Deimos)} | "
                   + $"{Ship(scene.SurveyorA)}; {Ship(scene.SurveyorB)}; {Ship(scene.Tanker)}";
        }

        class AsteroidScene
        {
            public Entity Fleet = null!;
            public Goal Goal = null!;
            public Entity SurveyorA = null!;
            public Entity Tanker = null!;
            public Entity Anchor = null!;
            public Entity Near = null!;
            public Entity Mid = null!;
            public Entity Far = null!;
        }

        class Scene
        {
            public Entity Faction = null!;
            public Entity Fleet = null!;
            public Goal Goal = null!;
            public Entity SurveyorA = null!;
            public Entity SurveyorB = null!;
            public Entity Tanker = null!;
            public Entity Mars = null!;
            public Entity Phobos = null!;
            public Entity Deimos = null!;
            public Entity Mercury = null!;
            public Entity Earth = null!;
            public Entity Luna = null!;
        }

        class TestEnergyType : ICargoable
        {
            public int ID => 0;
            public string UniqueID => "electricity";
            public string CargoTypeID => "energy";
            public string Name => "electricity";
            public long MassPerUnit => 0;
            public double VolumePerUnit => 0;
        }
    }
}
