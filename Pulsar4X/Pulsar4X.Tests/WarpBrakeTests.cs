using System;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.People;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Warp brake sheds a pasted speed by coasting out and warping back.
    /// The clock is not advanced across a real coast.
    /// </summary>
    public class WarpBrakeTests
    {
        CaptureEllipseTests _fx;

        [SetUp]
        public void SetUp()
        {
            _fx = new CaptureEllipseTests();
            _fx.SetUp();
        }

        [Test]
        public void ClosedForm_EarthPaste_ThreeBubbles_ThenCapture()
        {
            var (ship, mars) = _fx.EarthShipAimedAtMars(1, captained: true);

            Assert.IsTrue(MovePlanner.TryBrakeForecast(ship, mars, _fx.Epoch, out var forecast));
            Assert.AreEqual(3, forecast.Bubbles);

            double rLow = OrbitMath.LowOrbitRadius(forecast.DropIn);
            double budget = ship.GetDataBlob<NewtonThrustAbilityDB>().DeltaV;
            Assert.IsTrue(MovePlanner.TryArrivalBurn(
                ship, forecast.DropIn,
                new Vector3(rLow, 0, 0),
                new Vector3(0, forecast.ArrivalSpeed, 0),
                budget, _fx.Epoch,
                out _, out _, out _, out _),
                "speed after two coasts was " + forecast.ArrivalSpeed);
        }

        [Test]
        public void Nav25_Entry_IsOneWarp_AndRemembersThreeBubbles()
        {
            var (ship, mars) = _fx.EarthShipAimedAtMars(1, captained: true);
            GrantNav(ship, 25);
            var goal = MoveGoal(mars);

            var plan = new MoveToPlan().Plan(ship, goal, _fx.Epoch);

            Assert.AreEqual(GoalStatus.Active, plan.Status, plan.Message);
            Assert.AreEqual("Shedding speed, warp 1 of 3", plan.Message);
            Assert.AreEqual(3, goal.BrakeSteps);
            Assert.AreEqual(1, plan.Actions.Count);
            Assert.IsInstanceOf<WarpMoveAction>(plan.Actions[0]);
            Assert.IsFalse(plan.Actions.OfType<CirculariseAction>().Any());
        }

        [TestCase(24)]
        [TestCase(0)]
        public void NavBelowTheSkill_RefusesTheWarp(int skill)
        {
            var (ship, mars) = _fx.EarthShipAimedAtMars(1, captained: true);
            if (skill > 0)
                GrantNav(ship, skill);
            var goal = MoveGoal(mars);

            bool ok = MovePlanner.TryBuildMoveActions(ship, mars, out var actions, out string reason, _fx.Epoch, goal);

            Assert.IsFalse(ok);
            Assert.That(reason, Does.StartWith("Not enough Δv to orbit"));
            Assert.That(reason, Does.Not.Contain("Warp brake"));
            Assert.IsFalse(actions.OfType<WarpMoveAction>().Any());
        }

        [Test]
        public void AfterEntry_LowOrbitPaste_CoastsForWarpTwo()
        {
            var (ship, mars) = _fx.EarthShipAimedAtMars(1, captained: true);
            GrantNav(ship, 25);
            var goal = MoveGoal(mars);
            var entry = new MoveToPlan().Plan(ship, goal, _fx.Epoch);
            Assert.AreEqual(GoalStatus.Active, entry.Status, entry.Message);
            Assert.AreEqual(3, goal.BrakeSteps);

            var well = Well(ship, mars);
            _fx.Place(mars, ship, well.RLow, well.EarthSpeed);

            var plan = new MoveToPlan().Plan(ship, goal, _fx.Epoch);

            Assert.AreEqual(GoalStatus.Active, plan.Status, plan.Message);
            Assert.AreEqual(1, plan.Actions.Count, plan.Message);
            var coast = plan.Actions[0] as ShedSpeedAction;
            Assert.IsNotNull(coast, plan.Message);
            Assert.AreEqual("Shedding speed, warp 2 of 3", coast.Details);
            Assert.AreEqual("Shedding speed, warp 2 of 3", plan.Message);
        }

        [Test]
        public void AtTheCap_Nav25_WarpsBack_Nav0DoesNot()
        {
            var (ship, mars) = _fx.EarthShipAimedAtMars(1, captained: true);
            GrantNav(ship, 25);
            var well = Well(ship, mars);
            double once = SpeedAt(well.EarthSpeed, well.RLow, well.RCap, well.Mu);
            _fx.Place(mars, ship, well.RCap, once);
            var goal = MoveGoal(mars);
            goal.BrakeSteps = 3;

            var plan = new MoveToPlan().Plan(ship, goal, _fx.Epoch);

            Assert.AreEqual(GoalStatus.Active, plan.Status, plan.Message);
            Assert.AreEqual(1, plan.Actions.Count, plan.Message);
            Assert.IsInstanceOf<WarpMoveAction>(plan.Actions[0]);
            Assert.AreEqual("Shedding speed, warp 2 of 3", plan.Message);

            Assert.IsTrue(CommanderSkills.TryGetCommander(ship, out _, out var db));
            db.Skills[SkillDomain.Nav] = 0;
            MovePlanner.TryBuildMoveActions(ship, mars, out var actions, out _, _fx.Epoch, goal);
            Assert.IsFalse(actions.OfType<WarpMoveAction>().Any());
        }

        [Test]
        public void TwiceCoasted_LowOrbit_QueuesCircularise()
        {
            var (ship, mars) = _fx.EarthShipAimedAtMars(1, captained: true);
            GrantNav(ship, 25);
            var well = Well(ship, mars);
            double once = SpeedAt(well.EarthSpeed, well.RLow, well.RCap, well.Mu);
            double twice = SpeedAt(once, well.RLow, well.RCap, well.Mu);
            _fx.Place(mars, ship, well.RLow, twice);
            var goal = MoveGoal(mars);
            goal.BrakeSteps = 3;

            var plan = new MoveToPlan().Plan(ship, goal, _fx.Epoch);

            Assert.AreEqual(GoalStatus.Active, plan.Status, plan.Message);
            Assert.AreEqual(1, plan.Actions.Count, plan.Message);
            Assert.IsInstanceOf<CirculariseAction>(plan.Actions[0]);
            Assert.That(plan.Message, Does.Not.StartWith("Shedding speed"));
        }

        [Test]
        public void ThirtyKms_RefusesBeforeTheFirstExtraWarp()
        {
            var (ship, mars) = _fx.EarthShipAimedAtMars(1, captained: true);
            GrantNav(ship, 25);
            var well = Well(ship, mars);
            _fx.Place(mars, ship, well.RLow, 30_000);
            var goal = MoveGoal(mars);

            var plan = new MoveToPlan().Plan(ship, goal, _fx.Epoch);

            Assert.AreEqual(GoalStatus.Failed, plan.Status, plan.Message);
            Assert.That(plan.Message, Does.StartWith("Not enough Δv to orbit"));
            Assert.That(plan.Message, Does.Not.Contain("Warp brake"));
            Assert.IsFalse(plan.Actions.OfType<ShedSpeedAction>().Any());
        }

        [Test]
        public void ShedSpeed_WakesAfterNow_SucceedsAtTheCap_AndFailsIfItLeaves()
        {
            var (ship, mars) = _fx.EarthShipAimedAtMars(1, captained: true);
            var well = Well(ship, mars);
            double atCap = SpeedAt(well.EarthSpeed, well.RLow, well.RCap, well.Mu);
            _fx.Place(mars, ship, well.RLow, well.EarthSpeed);

            var early = ShedSpeedAction.Create(ship, mars.Id, well.RCap, "Shedding speed, warp 2 of 3", _fx.Epoch);
            early.Execute(_fx.Epoch);
            Assert.IsTrue(early.IsRunning, early.FailureMessage);
            Assert.Greater(early.WakeTime, ship.StarSysDateTime);
            early.WakeTime = _fx.Epoch;
            Assert.IsTrue(early.IsFinished());
            Assert.AreEqual(ActionStatus.Failed, early.Status);
            Assert.AreEqual("speed did not fall", early.FailureMessage);

            _fx.Place(mars, ship, well.RLow, well.EarthSpeed);
            var coast = ShedSpeedAction.Create(ship, mars.Id, well.RCap, "Shedding speed, warp 2 of 3", _fx.Epoch);
            coast.Execute(_fx.Epoch);
            Assert.IsTrue(coast.IsRunning, coast.FailureMessage);
            Assert.Greater(coast.WakeTime, ship.StarSysDateTime);
            Assert.IsFalse(coast.IsFinished());

            var clone = (ShedSpeedAction)coast.Clone();
            Assert.AreEqual(ActionStatus.Queued, clone.Status);
            Assert.IsFalse(clone.IsRunning);
            Assert.AreEqual(coast.ParentId, clone.ParentId);
            Assert.AreEqual(coast.CapRadius, clone.CapRadius);
            Assert.AreEqual(coast.Details, clone.Details);
            Assert.AreEqual(default(DateTime), clone.WakeTime);
            Assert.AreEqual(0, clone.SpeedAtExecute);

            _fx.Place(mars, ship, well.RCap, atCap);
            coast.WakeTime = _fx.Epoch;
            Assert.IsTrue(coast.IsFinished(), coast.FailureMessage);
            Assert.AreNotEqual(ActionStatus.Failed, coast.Status);

            _fx.Place(mars, ship, well.RLow, well.EarthSpeed);
            var left = ShedSpeedAction.Create(ship, mars.Id, well.RCap, "Shedding speed, warp 2 of 3", _fx.Epoch);
            left.Execute(_fx.Epoch);
            Assert.IsTrue(left.IsRunning, left.FailureMessage);
            var star = mars.GetDataBlob<OrbitDB>().Parent;
            ship.GetDataBlob<PositionDB>().SetParent(star);
            Assert.IsTrue(left.IsFinished());
            Assert.AreEqual(ActionStatus.Failed, left.Status);
            Assert.AreEqual("left the gravity well", left.FailureMessage);
        }

        static void GrantNav(Entity ship, int skill)
        {
            Assert.IsTrue(CommanderSkills.TryGetCommander(ship, out _, out var db));
            CommanderSkills.Grant(db, SkillDomain.Nav, skill);
        }

        static Goal MoveGoal(Entity mars) => new Goal(GoalType.MoveTo) { TargetEntityID = mars.Id };

        (double RLow, double RCap, double Mu, double EarthSpeed) Well(Entity ship, Entity mars)
        {
            double rLow = OrbitMath.LowOrbitRadius(mars);
            double rCap = 0.9 * OrbitMath.GetSOIRadius(mars.GetDataBlob<OrbitDB>());
            double mu = OrbitMath.SGP(mars, ship);
            double earth = MoveMath.GetRelativeFutureState(ship, _fx.Epoch).Velocity.Length();
            return (rLow, rCap, mu, earth);
        }

        static double SpeedAt(double speed, double rFrom, double rTo, double mu)
        {
            double v2 = speed * speed + 2.0 * mu * (1.0 / rTo - 1.0 / rFrom);
            Assert.That(v2, Is.GreaterThan(0));
            return Math.Sqrt(v2);
        }
    }
}
