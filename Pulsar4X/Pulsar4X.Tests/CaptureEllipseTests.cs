using System;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// A short tank captures into an ellipse. A captain refuses the warp when even that is out of reach.
    /// </summary>
    public class CaptureEllipseTests
    {
        static readonly Game Game = InitializeGame();
        StarSystem _sys;
        DateTime _epoch;

        static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);
            game.Settings.EnforceSingleThread = true;
            return game;
        }

        [SetUp]
        public void SetUp()
        {
            foreach (var sys in Game.Systems)
                sys.SetActivityState(SystemActivityState.Stasis);
            _sys = new StarSystem();
            _sys.Initialize(Game, "Capture", -1);
            _sys.IncrementExternalObserver(priority: true);
            _epoch = _sys.StarSysDateTime;
        }

        [Test]
        public void CaptureOrbitAtNinetyPercentSoi_IsBound()
        {
            var (mars, ship, r, mu) = MarsAndShip(deltaV: 1);
            double soi = OrbitMath.GetSOIRadius(mars.GetDataBlob<OrbitDB>());
            double ra = 0.9 * soi;
            var pos = new Vector3(r, 0, 0);
            double a = (r + ra) / 2.0;
            double vCap = Math.Sqrt(mu * (2.0 / r - 1.0 / a));
            var ke = OrbitMath.KeplerFromPositionAndVelocity(mu, pos, new Vector3(0, vCap, 0), _epoch);

            Assert.Less(ke.Eccentricity, 1, "apoapsis at 90% of the sphere must be an ellipse");
            Assert.LessOrEqual(ke.Apoapsis, soi, "apoapsis must stay inside the sphere");
            Assert.Greater(ke.Apoapsis, r);
        }

        [Test]
        public void HyperbolicLeftover_BudgetBetweenCaptureAndCircularise_Captures()
        {
            var speeds = MarsSpeeds();
            double budget = (speeds.CaptureDv + speeds.CirculariseDv) / 2.0;
            var (mars, ship, _, _) = MarsAndShip(budget);
            Place(mars, ship, speeds.R, speeds.Hyperbolic);
            double fuelBefore = FuelMass(ship);
            double soi = OrbitMath.GetSOIRadius(mars.GetDataBlob<OrbitDB>());

            var action = RunCircularise(ship);

            Assert.AreEqual(ActionStatus.Succeeded, action.Status, action.FailureMessage);
            Assert.That(action.Details, Does.StartWith("Elliptical orbit around"));
            Assert.IsTrue(ship.TryGetDataBlob<OrbitDB>(out var orbit));
            Assert.Less(orbit.Eccentricity, 1);
            Assert.GreaterOrEqual(orbit.Eccentricity, 0.05, "a partial burn is not a full circularise");
            Assert.LessOrEqual(orbit.Apoapsis, soi * 0.9 * 1.01);
            Assert.Less(FuelMass(ship), fuelBefore);
        }

        [Test]
        public void HyperbolicLeftover_BudgetUnderCapture_FailsWithoutSpending()
        {
            var speeds = MarsSpeeds();
            double budget = speeds.CaptureDv * 0.5;
            var (mars, ship, _, _) = MarsAndShip(budget);
            Place(mars, ship, speeds.R, speeds.Hyperbolic);
            double fuelBefore = FuelMass(ship);
            double eBefore = ship.GetDataBlob<OrbitDB>().Eccentricity;

            var action = RunCircularise(ship);

            Assert.AreEqual(ActionStatus.Failed, action.Status);
            Assert.AreEqual("not enough ΔV", action.FailureMessage);
            Assert.AreEqual(fuelBefore, FuelMass(ship));
            Assert.GreaterOrEqual(ship.GetDataBlob<OrbitDB>().Eccentricity, 1);
            Assert.AreEqual(eBefore, ship.GetDataBlob<OrbitDB>().Eccentricity);
        }

        [Test]
        public void BudgetThatPaysCircularise_EndsNearCircular()
        {
            var speeds = MarsSpeeds();
            var (mars, ship, _, _) = MarsAndShip(speeds.CirculariseDv + 500);
            Place(mars, ship, speeds.R, speeds.Hyperbolic);

            var action = RunCircularise(ship);

            Assert.AreEqual(ActionStatus.Succeeded, action.Status, action.FailureMessage);
            Assert.IsTrue(ship.TryGetDataBlob<OrbitDB>(out var orbit));
            Assert.Less(orbit.Eccentricity, 0.05);
        }

        [Test]
        public void Ellipse_BudgetTrimsButCannotCircularise_SucceedsLower()
        {
            var (mars, ship, r, mu) = MarsAndShip(1);
            double e = 0.4;
            double a = r / (1.0 - e);
            double v = Math.Sqrt(mu * (2.0 / r - 1.0 / a));
            double vCirc = Math.Sqrt(mu / r);
            double circDv = v - vCirc;
            Assert.Greater(circDv, 1, "precondition: the trim is more than the 0.01 m/s snap");
            SetBudget(ship, circDv * 0.5);
            Place(mars, ship, r, v);
            double eBefore = ship.GetDataBlob<OrbitDB>().Eccentricity;

            var action = RunCircularise(ship);

            Assert.AreEqual(ActionStatus.Succeeded, action.Status, action.FailureMessage);
            Assert.That(action.Details, Does.StartWith("Elliptical orbit around"));
            Assert.IsTrue(ship.TryGetDataBlob<OrbitDB>(out var orbit));
            Assert.Less(orbit.Eccentricity, eBefore);
            Assert.GreaterOrEqual(orbit.Eccentricity, 0.05);
        }

        [Test]
        public void InWellMoveTo_QueuesTheEllipse()
        {
            var speeds = MarsSpeeds();
            double budget = (speeds.CaptureDv + speeds.CirculariseDv) / 2.0;
            var (mars, ship, _, _) = MarsAndShip(budget);
            Place(mars, ship, speeds.R, speeds.Hyperbolic);
            var goal = new Goal(GoalType.MoveTo) { TargetEntityID = mars.Id };

            var queued = new MoveToPlan().Plan(ship, goal, _epoch);

            Assert.AreEqual(GoalStatus.Active, queued.Status, queued.Message);
            Assert.IsNotNull(queued.Actions);
            Assert.AreEqual(1, queued.Actions.Count, queued.Message);
            Assert.IsInstanceOf<CirculariseAction>(queued.Actions[0]);

            RunCircularise(ship, (CirculariseAction)queued.Actions[0]);
            if (ship.TryGetDataBlob<ActionQueueDB>(out var queue))
                queue.ActionList.Clear();

            PlanResult done = default;
            for (int i = 0; i < 4 && done.Status != GoalStatus.Completed; i++)
            {
                done = new MoveToPlan().Plan(ship, goal, _epoch);
                if (done.Status == GoalStatus.Completed)
                    break;
                Assert.AreEqual(GoalStatus.Active, done.Status, done.Message);
                var next = done.Actions.OfType<CirculariseAction>().Single();
                RunCircularise(ship, next);
                if (ship.TryGetDataBlob<ActionQueueDB>(out queue))
                    queue.ActionList.Clear();
            }

            Assert.AreEqual(GoalStatus.Completed, done.Status, done.Message);
            Assert.That(done.Message, Does.StartWith("Elliptical orbit around"));
            goal.Status = GoalStatus.Completed;
            goal.Message = done.Message;
            ShipLogBook.NoteOutcome(ship, goal, _epoch);
            Assert.That(ship.GetDataBlob<ShipLogDB>().Entries[^1].Text,
                Does.StartWith("Arrived at Mars. Elliptical orbit around"));
        }

        [Test]
        public void CaptainedShip_UnderCapture_RefusesTheWarp()
        {
            var (ship, mars) = EarthShipAimedAtMars(budget: 1, captained: true);

            bool ok = MovePlanner.TryBuildMoveActions(ship, mars, out var actions, out string reason, _epoch);

            Assert.IsFalse(ok);
            Assert.That(reason, Does.StartWith("Not enough Δv to orbit"));
            Assert.IsFalse(actions.OfType<WarpMoveAction>().Any());

            var goal = new Goal(GoalType.MoveTo)
            {
                TargetEntityID = mars.Id,
                Status = GoalStatus.Failed,
                Message = reason,
            };
            ShipLogBook.NoteOutcome(ship, goal, _epoch);
            string text = ship.GetDataBlob<ShipLogDB>().Entries[^1].Text;
            Assert.That(text, Does.StartWith("Could not reach Mars."));
            Assert.That(text, Does.Contain("Not enough Δv to orbit"));
        }

        [Test]
        public void EmptyChair_UnderCapture_StillPlotsWarpAndCircularise()
        {
            var (ship, mars) = EarthShipAimedAtMars(budget: 1, captained: false);

            Assert.IsTrue(MovePlanner.TryBuildMoveActions(ship, mars, out var actions, out string reason, _epoch), reason);
            Assert.IsInstanceOf<WarpMoveAction>(actions[0]);
            Assert.IsInstanceOf<CirculariseAction>(actions[1]);
        }

        (double R, double Mu, double CaptureDv, double CirculariseDv, double Hyperbolic) MarsSpeeds()
        {
            var (mars, ship, r, mu) = MarsAndShip(1);
            double soi = OrbitMath.GetSOIRadius(mars.GetDataBlob<OrbitDB>());
            double ra = 0.9 * soi;
            double a = (r + ra) / 2.0;
            double vCirc = Math.Sqrt(mu / r);
            double vEsc = Math.Sqrt(2.0 * mu / r);
            double vCap = Math.Sqrt(mu * (2.0 / r - 1.0 / a));
            double vHyper = vEsc + (vEsc - vCap) * 0.25;
            return (r, mu, vHyper - vCap, vHyper - vCirc, vHyper);
        }

        (Entity Mars, Entity Ship, double R, double Mu) MarsAndShip(double deltaV)
        {
            var sol = TestingUtilities.BasicSol(_sys);
            const double marsMass = 0.64174e24;
            var orbit = OrbitDB.FromAsteroidFormat(sol, sol.GetDataBlob<MassVolumeDB>().MassDry,
                marsMass, 1.524, 0, 0, 0, 0, 0, _epoch);
            var mars = Entity.Create();
            _sys.AddEntity(mars, new BaseDataBlob[]
            {
                new PositionDB(),
                MassVolumeDB.NewFromMassAndRadius_m(marsMass, 3_396_200),
                orbit,
                new NameDB("Mars"),
            });
            OrbitProcessor.ProcessEntity(mars, _epoch);

            var faction = FactionFactory.CreateFaction(Game, "cap-" + Guid.NewGuid().ToString("N"));
            var ship = Entity.Create();
            double r = OrbitMath.LowOrbitRadius(mars);
            var marsAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(mars, _epoch);
            var pos = new PositionDB { AbsolutePosition = marsAbs + new Vector3(r, 0, 0) };
            _sys.AddEntity(ship, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
                new NameDB("burner"),
                new ShipInfoDB(),
                new ActionQueueDB(),
            });
            pos.SetParent(mars);
            ship.FactionOwnerID = faction.Id;
            AttachFuel(ship, faction, deltaV);
            double mu = OrbitMath.SGP(mars, ship);
            return (mars, ship, r, mu);
        }

        void Place(Entity mars, Entity ship, double r, double speed)
        {
            var marsAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(mars, _epoch);
            var pos = ship.GetDataBlob<PositionDB>();
            pos.SetParent(mars);
            pos.RelativePosition = new Vector3(r, 0, 0);
            pos.AbsolutePosition = marsAbs + pos.RelativePosition;
            ship.SetDataBlob(OrbitDB.FromVelocity(mars, ship, new Vector3(0, speed, 0), _epoch));
        }

        (Entity Ship, Entity Mars) EarthShipAimedAtMars(double budget, bool captained)
        {
            var sol = TestingUtilities.BasicSol(_sys);
            var earth = AddBody(sol, "Earth", 5.972e24, 6_371_000, 1.0);
            var mars = AddBody(sol, "Mars", 0.64174e24, 3_396_200, 1.524);
            var faction = FactionFactory.CreateFaction(Game, "gate-" + Guid.NewGuid().ToString("N"));
            double leo = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, _epoch);
            var pos = new PositionDB { AbsolutePosition = earthAbs + new Vector3(leo, 0, 0) };
            var ship = Entity.Create();
            _sys.AddEntity(ship, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
                new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" },
                new NameDB("pilot"),
                new ShipInfoDB(),
                new ActionQueueDB(),
            });
            pos.SetParent(earth);
            ship.FactionOwnerID = faction.Id;
            ship.SetDataBlob(OrbitDB.FromPosition(earth, ship, _epoch));
            OrbitProcessor.ProcessEntity(ship, _epoch);
            AttachFuel(ship, faction, budget);
            if (captained)
            {
                var commander = CommanderFactory.Create(_sys, faction.Id, CommanderFactory.CreateShipCaptain(Game));
                ship.GetDataBlob<ShipInfoDB>().CommanderID = commander.Id;
            }
            return (ship, mars);
        }

        Entity AddBody(Entity sol, string name, double mass, double radius, double smaAu)
        {
            var orbit = OrbitDB.FromAsteroidFormat(sol, sol.GetDataBlob<MassVolumeDB>().MassDry,
                mass, smaAu, 0, 0, 0, 0, 0, _epoch);
            var body = Entity.Create();
            _sys.AddEntity(body, new BaseDataBlob[]
            {
                new PositionDB(),
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius),
                orbit,
                new NameDB(name),
            });
            OrbitProcessor.ProcessEntity(body, _epoch);
            return body;
        }

        void AttachFuel(Entity ship, Entity faction, double deltaV)
        {
            var data = faction.GetDataBlob<FactionInfoDB>().Data;
            data.Unlock("methalox");
            var fuel = data.CargoGoods.GetAny("methalox");
            Assert.IsNotNull(fuel);
            var thrust = new NewtonThrustAbilityDB(fuel.UniqueID)
            {
                ThrustInNewtons = 1e9,
                ExhaustVelocity = 10_000,
                FuelBurnRate = 1e6,
            };
            ship.SetDataBlob(thrust);
            var storage = new CargoStorageDB(fuel.CargoTypeID, 1e12);
            ship.SetDataBlob(storage);
            SetBudget(ship, deltaV);
        }

        void SetBudget(Entity ship, double deltaV)
        {
            var thrust = ship.GetDataBlob<NewtonThrustAbilityDB>();
            var mass = ship.GetDataBlob<MassVolumeDB>();
            var faction = ship.FactionOwnerID;
            Assert.IsTrue(Game.GlobalManager.TryGetGlobalEntityById(faction, out var factionEntity));
            var fuel = factionEntity.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
            double dry = mass.MassDry;
            double fuelMass = Math.Max(1, dry * (Math.Exp(deltaV / thrust.ExhaustVelocity) - 1));
            long units = Math.Max(1, (long)Math.Round(fuelMass / fuel.MassPerUnit));
            var storage = ship.GetDataBlob<CargoStorageDB>();
            double have = storage.GetMassStored(fuel, false);
            if (have > 0)
                storage.AddRemoveCargoByMass(fuel, -have);
            storage.AddCargoByUnit(fuel, units);
            mass.UpdateMassTotal(storage);
            thrust.SetFuel(storage.GetMassStored(fuel, false), mass.MassTotal);
        }

        double FuelMass(Entity ship)
        {
            var fuel = ship.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny("methalox");
            return ship.GetDataBlob<CargoStorageDB>().GetMassStored(fuel, false);
        }

        CirculariseAction RunCircularise(Entity ship, CirculariseAction action = null)
        {
            action ??= CirculariseAction.CreateCommand(ship, _epoch);
            if (!ship.HasDataBlob<ActionQueueDB>())
                ship.SetDataBlob(new ActionQueueDB());
            var queue = ship.GetDataBlob<ActionQueueDB>();
            if (!queue.ActionList.Contains(action))
                queue.Enqueue(action);

            var processor = Game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor));
            processor.ProcessEntity(ship, _epoch);
            if (action.Status is ActionStatus.Failed or ActionStatus.Succeeded)
                return action;

            var until = _epoch + TimeSpan.FromHours(6);
            for (var t = _epoch + TimeSpan.FromMinutes(1); t <= until; t += TimeSpan.FromMinutes(1))
            {
                NewtonSimpleProcessor.ProcessEntity(ship, t);
                processor.ProcessEntity(ship, t);
                if (action.Status is ActionStatus.Failed or ActionStatus.Succeeded)
                    return action;
            }
            return action;
        }
    }
}
