using System;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Engine.Api;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Player cancel. A failed goal with GivenGoal set already skips planning.
    /// The wake that must not replan is an active cancelled move whose warp just finished
    /// and whose circularise is still queued.
    /// </summary>
    public class CancelGoalTests
    {
        Game _game;
        StarSystem _sys;
        Entity _sol;

        [SetUp]
        public void SetUp()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            _game = new Game(new NewGameSettings(), modDataStore);
            _game.Settings.StrictNewtonion = true;
            _sys = new StarSystem();
            _sys.Initialize(_game, "Sol", -1);
            _sol = TestingUtilities.BasicSol(_sys);
        }

        [Test]
        public void Convenient_BeforeTheBubble_DropsTheTransit_AndDoesNotReplan()
        {
            var planet = AddPlanet();
            var ship = AddShip(_sol, PlanetAbsolute(planet));
            var goal = Give(ship, GoalType.MoveTo, planet.Id);
            EnqueueTransit(ship, goal, planet);

            AgentProcessor.CancelGoal(ship, CancelTiming.WhenConvenient, ship.StarSysDateTime);

            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList, Is.Empty);
            Assert.That(goal.PlayerCancelled, Is.True);
            Assert.That(goal.Status, Is.EqualTo(GoalStatus.Failed));
            Assert.That(goal.Message, Is.EqualTo("cancelled"));

            AgentProcessor.RunAgentNow(ship);

            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList, Is.Empty);
            Assert.That(ship.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(goal));
            Assert.That(goal.Message, Is.EqualTo("cancelled"));
        }

        [Test]
        public void Convenient_WhileWarping_KeepsWarpAndCircularise()
        {
            var planet = AddPlanet();
            var (ship, saved) = ShipInside(planet);
            var goal = Give(ship, GoalType.MoveTo, planet.Id);
            var warp = EnqueueTransit(ship, goal, planet);
            warp.Status = ActionStatus.Running;
            LightBubble(ship, saved);

            AgentProcessor.CancelGoal(ship, CancelTiming.WhenConvenient, ship.StarSysDateTime);

            var queue = ship.GetDataBlob<ActionQueueDB>();
            Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.True);
            Assert.That(queue.ActionsFor(goal).OfType<WarpMoveAction>().Count(), Is.EqualTo(1));
            Assert.That(queue.ActionsFor(goal).OfType<CirculariseAction>().Count(), Is.EqualTo(1));
            Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
            Assert.That(goal.Message, Is.EqualTo("finishing"));

            warp.Status = ActionStatus.Succeeded;
            AgentProcessor.RunAgentNow(ship);

            Assert.That(queue.ActionList.OfType<CirculariseAction>().Count(), Is.EqualTo(1));
            Assert.That(queue.ActionList.OfType<WarpMoveAction>().Any(), Is.False);
            Assert.That(goal.Message, Is.EqualTo("finishing"));
            Assert.That(goal.PlayerCancelled, Is.True);
            Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.True);
        }

        [Test]
        public void Now_WhileWarping_OrbitsTheCurrentSoi()
        {
            var planet = AddPlanet();
            var (ship, saved) = ShipInside(planet);
            var here = ship.GetDataBlob<PositionDB>().AbsolutePosition;
            double fromPlanet = (here - PlanetAbsolute(planet)).Length();
            Assert.That(fromPlanet, Is.LessThan(planet.GetSOI_m()), "precondition: ship is inside the planet");

            var goal = Give(ship, GoalType.MoveTo, planet.Id);
            EnqueueTransit(ship, goal, planet);
            LightBubble(ship, saved);
            var at = ship.StarSysDateTime;

            AgentProcessor.CancelGoal(ship, CancelTiming.Now, at);

            Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.False);
            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList, Is.Empty);
            var orbit = ship.GetDataBlob<OrbitDB>();
            Assert.That(orbit.Parent, Is.SameAs(planet));
            Assert.That(orbit.Eccentricity, Is.LessThan(0.01));
            var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbit, at);
            Assert.That((vel - saved).Length(), Is.LessThan(1.0));
            Assert.That(goal.Status, Is.EqualTo(GoalStatus.Failed));
            Assert.That(goal.Message, Is.EqualTo("cancelled"));
        }

        [Test]
        public void CancelWarpLine_DropsTheBubbleAndTheCircularise()
        {
            var planet = AddPlanet();
            var (ship, saved) = ShipInside(planet);
            var goal = Give(ship, GoalType.MoveTo, planet.Id);
            var warp = EnqueueTransit(ship, goal, planet);
            warp.Status = ActionStatus.Running;
            LightBubble(ship, saved);

            AgentProcessor.CancelAction(ship, warp, ship.StarSysDateTime);

            Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.False);
            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList, Is.Empty);
            Assert.That(goal.PlayerCancelled, Is.True);
            Assert.That(goal.Message, Is.EqualTo("cancelled"));

            AgentProcessor.RunAgentNow(ship);

            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList, Is.Empty);
            Assert.That(goal.Message, Is.EqualTo("cancelled"));
            Assert.That(ship.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(goal));
        }

        [Test]
        public void CancelCirculariseLine_LeavesTheBubble()
        {
            var planet = AddPlanet();
            var (ship, saved) = ShipInside(planet);
            var goal = Give(ship, GoalType.MoveTo, planet.Id);
            var warp = EnqueueTransit(ship, goal, planet);
            warp.Status = ActionStatus.Running;
            LightBubble(ship, saved);
            var circ = ship.GetDataBlob<ActionQueueDB>().ActionList.OfType<CirculariseAction>().Single();

            AgentProcessor.CancelAction(ship, circ, ship.StarSysDateTime);

            Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.True);
            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList.OfType<WarpMoveAction>().Count(), Is.EqualTo(1));
            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList.OfType<CirculariseAction>().Any(), Is.False);
            Assert.That(goal.Message, Is.EqualTo("finishing"));
            Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));

            AgentProcessor.RunAgentNow(ship);

            Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.True);
            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList.OfType<WarpMoveAction>().Count(), Is.EqualTo(1));
            Assert.That(goal.Message, Is.EqualTo("finishing"));
        }

        [Test]
        public void Convenient_OnAStandingFleet_DoesNotHandTheShipAgain()
        {
            var rock = AddRock(new Vector3(0, 0, 0));
            var fleet = AddFleet();
            var standing = fleet.GetDataBlob<GoalsDB>().GivenGoal;
            var waiting = AddSurveyShip(new Vector3(1000, 0, 0));
            var surveying = AddSurveyShip(new Vector3(1000, 0, 0));
            var free = AddSurveyShip(new Vector3(1000, 0, 0));
            var fleetDB = fleet.GetDataBlob<FleetDB>();
            fleetDB.AddChild(waiting);
            fleetDB.AddChild(surveying);
            fleetDB.AddChild(free);

            var waitingGoal = Give(waiting, GoalType.ServeyBodies, rock.Id);
            waitingGoal.ParentGoalId = standing.Id;
            var warp = WarpMoveAction.CreateWarpOnly(waiting, rock, waiting.StarSysDateTime);
            warp.ParentGoalId = waitingGoal.Id;
            waiting.GetDataBlob<ActionQueueDB>().Enqueue(warp);

            var surveyGoal = Give(surveying, GoalType.ServeyBodies, rock.Id);
            surveyGoal.ParentGoalId = standing.Id;
            var line = new GeoSurveyOrder(surveying, rock)
            {
                ParentGoalId = surveyGoal.Id,
                Status = ActionStatus.Running,
            };
            surveying.GetDataBlob<ActionQueueDB>().Enqueue(line);

            var at = fleet.StarSysDateTime;
            AgentProcessor.CancelGoal(waiting, CancelTiming.WhenConvenient, at);
            AgentProcessor.CancelGoal(surveying, CancelTiming.WhenConvenient, at);

            Assert.That(waiting.GetDataBlob<ActionQueueDB>().ActionList, Is.Empty, "a warp that has not lit stands down");
            Assert.That(waitingGoal.Message, Is.EqualTo("cancelled"));
            Assert.That(surveying.GetDataBlob<ActionQueueDB>().ActionList, Does.Contain(line));
            Assert.That(surveyGoal.Message, Is.EqualTo("finishing"));

            AgentProcessor.RunAgentNow(fleet);

            Assert.That(fleet.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(standing));
            Assert.That(standing.PlayerCancelled, Is.False);
            Assert.That(waiting.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(waitingGoal));
            Assert.That(surveying.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(surveyGoal));
            Assert.That(surveying.GetDataBlob<ActionQueueDB>().ActionList, Does.Contain(line));
            Assert.That(free.GetDataBlob<GoalsDB>().GivenGoal, Is.Not.Null);
            Assert.That(free.GetDataBlob<GoalsDB>().GivenGoal.ParentGoalId, Is.EqualTo(standing.Id));
            Assert.That(free.GetDataBlob<GoalsDB>().GivenGoal.Type, Is.EqualTo(GoalType.ServeyBodies));
        }

        [Test]
        public void FleetCancel_ClearsTheOrder_AndTheNextOrderIsHandedDown()
        {
            var planet = AddPlanet();
            var faction = FactionFactory.CreateFaction(_game, "flight");
            var fleet = AddFleet();
            var first = Give(fleet, GoalType.MoveTo, planet.Id);
            var a = AddShip(_sol, new Vector3(2e11, 0, 0));
            var b = AddShip(_sol, new Vector3(2e11, 1e9, 0));
            fleet.FactionOwnerID = faction.Id;
            a.FactionOwnerID = faction.Id;
            b.FactionOwnerID = faction.Id;
            var fleetDB = fleet.GetDataBlob<FleetDB>();
            fleetDB.AddChild(a);
            fleetDB.AddChild(b);
            var aGoal = Give(a, GoalType.MoveTo, planet.Id);
            aGoal.ParentGoalId = first.Id;
            var bGoal = Give(b, GoalType.MoveTo, planet.Id);
            bGoal.ParentGoalId = first.Id;

            AgentProcessor.CancelGoal(fleet, CancelTiming.Now, fleet.StarSysDateTime);

            Assert.That(fleet.GetDataBlob<GoalsDB>().ActiveGoal, Is.Null);
            Assert.That(fleet.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(first));
            Assert.That(first.Status, Is.EqualTo(GoalStatus.Failed));
            Assert.That(first.Message, Is.EqualTo("cancelled"));
            Assert.That(a.GetDataBlob<GoalsDB>().ActiveGoal, Is.Null);
            Assert.That(b.GetDataBlob<GoalsDB>().ActiveGoal, Is.Null);
            Assert.That(a.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(aGoal));

            var next = new Goal(GoalType.MoveTo) { TargetEntityID = planet.Id };
            AgentProcessor.AssignGoal(fleet, next);

            Assert.That(fleet.GetDataBlob<GoalsDB>().GivenGoal, Is.SameAs(next));
            Assert.That(next.PlayerCancelled, Is.False);
            Assert.That(next.Status, Is.Not.EqualTo(GoalStatus.Failed));
            Assert.That(a.GetDataBlob<GoalsDB>().ActiveGoal.ParentGoalId, Is.EqualTo(next.Id));
            Assert.That(b.GetDataBlob<GoalsDB>().ActiveGoal.ParentGoalId, Is.EqualTo(next.Id));
            Assert.That(a.GetDataBlob<GoalsDB>().ActiveGoal.PlayerCancelled, Is.False);

            AgentProcessor.RunAgentNow(a);

            Assert.That(a.GetDataBlob<ActionQueueDB>().ActionList.OfType<WarpMoveAction>().Any(), Is.True);
            Assert.That(a.GetDataBlob<GoalsDB>().ActiveGoal.PlayerCancelled, Is.False);
            Assert.That(a.GetDataBlob<GoalsDB>().ActiveGoal.Message, Does.Not.EqualTo("cancelled"));
        }

        [Test]
        public void Survey_Convenient_KeepsTheRunningLine()
        {
            var rock = AddRock(new Vector3(0, 0, 0));
            var ship = AddShip(_sol, new Vector3(1000, 0, 0));
            var goal = Give(ship, GoalType.ServeyBodies, rock.Id);
            var line = new GeoSurveyOrder(ship, rock)
            {
                ParentGoalId = goal.Id,
                Status = ActionStatus.Running,
            };
            ship.GetDataBlob<ActionQueueDB>().Enqueue(line);

            AgentProcessor.CancelGoal(ship, CancelTiming.WhenConvenient, ship.StarSysDateTime);

            Assert.That(ship.GetDataBlob<ActionQueueDB>().ActionList, Does.Contain(line));
            Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
            Assert.That(goal.Message, Is.EqualTo("finishing"));
        }

        [Test]
        public void Survey_Convenient_WhileWarping_DoesNotStartTheSurvey()
        {
            var rock = AddRock(new Vector3(1e11, 0, 0));
            var ship = AddShip(_sol, new Vector3(1e11, 0, 0));
            var goal = Give(ship, GoalType.ServeyBodies, rock.Id);
            var warp = WarpMoveAction.CreateWarpOnly(ship, rock, ship.StarSysDateTime);
            warp.ParentGoalId = goal.Id;
            warp.Status = ActionStatus.Running;
            var survey = new GeoSurveyOrder(ship, rock) { ParentGoalId = goal.Id };
            var queue = ship.GetDataBlob<ActionQueueDB>();
            queue.Enqueue(warp);
            queue.Enqueue(survey);
            LightBubble(ship, new Vector3(0, 1000, 0));

            AgentProcessor.CancelGoal(ship, CancelTiming.WhenConvenient, ship.StarSysDateTime);

            Assert.That(queue.ActionList, Does.Contain(warp));
            Assert.That(queue.ActionList, Does.Not.Contain(survey));
            Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.True);
            Assert.That(goal.Message, Is.EqualTo("finishing"));
        }

        [Test]
        public void Survey_Now_StopsTheWarp()
        {
            var rock = AddRock(new Vector3(1e11, 0, 0));
            var ship = AddShip(_sol, new Vector3(1e11, 0, 0));
            var goal = Give(ship, GoalType.ServeyBodies, rock.Id);
            var warp = WarpMoveAction.CreateWarpOnly(ship, rock, ship.StarSysDateTime);
            warp.ParentGoalId = goal.Id;
            warp.Status = ActionStatus.Running;
            var survey = new GeoSurveyOrder(ship, rock)
            {
                ParentGoalId = goal.Id,
                Status = ActionStatus.Running,
            };
            var queue = ship.GetDataBlob<ActionQueueDB>();
            queue.Enqueue(warp);
            queue.Enqueue(survey);
            double sgp = GeneralMath.StandardGravitationalParameter(
                ship.GetDataBlob<MassVolumeDB>().MassTotal + _sol.GetDataBlob<MassVolumeDB>().MassTotal);
            LightBubble(ship, new Vector3(0, Math.Sqrt(sgp / 1e11), 0));

            AgentProcessor.CancelGoal(ship, CancelTiming.Now, ship.StarSysDateTime);

            Assert.That(queue.ActionList, Is.Empty);
            Assert.That(ship.HasDataBlob<WarpMovingDB>(), Is.False);
            Assert.That(goal.Message, Is.EqualTo("cancelled"));
            Assert.That(ship.GetDataBlob<OrbitDB>().Parent, Is.SameAs(_sol));
        }

        [Test]
        public void Colony_IsRejected()
        {
            var colony = Entity.Create();
            var goal = new Goal(GoalType.OfferStock)
            {
                Status = GoalStatus.Active,
                Message = "selling",
            };
            _sys.AddEntity(colony, new BaseDataBlob[]
            {
                new ColonyInfoDB(),
                new GoalsDB { GivenGoal = goal, ActiveGoal = goal },
                new NameDB("colony"),
            });

            Assert.That(AgentProcessor.CancelGoal(colony, CancelTiming.Now, colony.StarSysDateTime), Is.False);
            Assert.That(goal.PlayerCancelled, Is.False);
            Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
            Assert.That(goal.Message, Is.EqualTo("selling"));

            var result = new CommandTranslator(_game).Translate(
                colony, colony, new CancelGoalCommand(colony.Id, CancelTiming.Now));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo("Cancel a ship or a fleet order."));
            Assert.That(goal.PlayerCancelled, Is.False);
        }

        Goal Give(Entity ship, GoalType type, int targetId)
        {
            var goal = new Goal(type)
            {
                Status = GoalStatus.Active,
                TargetEntityID = targetId,
            };
            ship.SetDataBlob(new GoalsDB { GivenGoal = goal, ActiveGoal = goal });
            return goal;
        }

        WarpMoveAction EnqueueTransit(Entity ship, Goal goal, Entity target)
        {
            var warp = WarpMoveAction.CreateWarpOnly(ship, target, ship.StarSysDateTime);
            warp.ParentGoalId = goal.Id;
            var circ = CirculariseAction.CreateCommand(ship);
            circ.ParentGoalId = goal.Id;
            var queue = ship.GetDataBlob<ActionQueueDB>();
            queue.Enqueue(warp);
            queue.Enqueue(circ);
            return warp;
        }

        void LightBubble(Entity ship, Vector3 saved)
        {
            var pos = ship.GetDataBlob<PositionDB>();
            var move = new WarpMovingDB(ship, pos.AbsolutePosition);
            move.HasStarted = true;
            move.IsAtTarget = false;
            move._position = (Vector2)pos.AbsolutePosition;
            move.SavedNewtonionVector = saved;
            move.PredictedExitTime = ship.StarSysDateTime + TimeSpan.FromDays(1);
            ship.SetDataBlob(move);
            pos.SetParent(pos.Root);
        }

        (Entity ship, Vector3 saved) ShipInside(Entity planet)
        {
            var here = PlanetAbsolute(planet) + new Vector3(2e7, 0, 0);
            var ship = AddShip(_sol, here);
            double sgp = GeneralMath.StandardGravitationalParameter(
                ship.GetDataBlob<MassVolumeDB>().MassTotal + planet.GetDataBlob<MassVolumeDB>().MassTotal);
            var saved = new Vector3(0, Math.Sqrt(sgp / 2e7), 0);
            return (ship, saved);
        }

        Vector3 PlanetAbsolute(Entity planet)
            => planet.GetDataBlob<PositionDB>().AbsolutePosition;

        Entity AddPlanet()
        {
            var epoch = _sys.StarSysDateTime;
            var parentMass = _sol.GetDataBlob<MassVolumeDB>().MassDry;
            double mass = 5.972e24;
            var orbit = OrbitDB.FromAsteroidFormat(_sol, parentMass, mass, 1.0, 0, 0, 0, 0, 0, epoch);
            var ent = Entity.Create();
            var pos = new PositionDB();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(mass, 6_371_000),
                orbit,
                new NameDB("planet"),
            });
            OrbitProcessor.ProcessEntity(ent, epoch);
            return ent;
        }

        Entity AddRock(Vector3 at)
        {
            var ent = Entity.Create();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                new PositionDB { AbsolutePosition = at },
                new SystemBodyInfoDB { BodyType = BodyType.Asteroid },
                new GeoSurveyableDB { PointsRequired = 100 },
                new NameDB("rock"),
            });
            return ent;
        }

        Entity AddFleet()
        {
            var standing = new Goal(GoalType.SurveyStanding) { Status = GoalStatus.Active };
            var ent = Entity.Create();
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                new FleetDB(),
                new GoalsDB { GivenGoal = standing, ActiveGoal = standing },
                new NameDB("flight"),
                new ActionQueueDB(),
            });
            return ent;
        }

        Entity AddSurveyShip(Vector3 at)
        {
            var ship = AddShip(null, at);
            ship.SetDataBlob(new GeoSurveyAbilityDB());
            return ship;
        }

        Entity AddShip(Entity parent, Vector3 absolute)
        {
            var ent = Entity.Create();
            var pos = new PositionDB { AbsolutePosition = absolute };
            var warp = new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" };
            var energy = new EnergyGenAbilityDB(_sys.StarSysDateTime)
            {
                EnergyType = new TestEnergyType(),
            };
            energy.EnergyStored["electricity"] = 1e12;
            energy.EnergyStoreMax["electricity"] = 1e12;
            _sys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
                warp,
                energy,
                new ShipInfoDB(),
                new ActionQueueDB(),
                new NameDB("ship"),
            });
            if (parent != null)
                pos.SetParent(parent);
            return ent;
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
