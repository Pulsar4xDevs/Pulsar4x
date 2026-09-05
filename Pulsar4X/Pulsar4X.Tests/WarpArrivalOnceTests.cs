using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Arrival must happen once, at PredictedExitTime.
    ///
    /// ManagerSubPulse runs every due hotloop, then every instance interrupt, on the same
    /// substep. Warp's hotloop is 5 minutes and the arrival interrupt is PredictedExitTime,
    /// so those two often share a tick. Drop-in (SetOrbitHereSimpleNewt) must still run once.
    /// </summary>
    public class WarpArrivalOnceTests
    {
        /// <summary>Matches <see cref="WarpMoveProcessor.RunFrequency"/> so hotloop and instance share a substep.</summary>
        static readonly TimeSpan WarpHotloop = TimeSpan.FromMinutes(5);

        Game _game;
        StarSystem _starSys;
        List<(int entityId, DateTime at)> _dropIns;

        [SetUp]
        public void SetUp()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            _game = new Game(new NewGameSettings(), modDataStore);
            _game.Settings.StrictNewtonion = true;

            _starSys = new StarSystem();
            _starSys.Initialize(_game, "Sol", -1);

            _dropIns = new List<(int, DateTime)>();
            WarpMoveProcessor.TestDropIn = (entity, at) => _dropIns.Add((entity.Id, at));
        }

        [TearDown]
        public void TearDown()
        {
            WarpMoveProcessor.TestDropIn = null;
        }

        [Test]
        public void WarpMove_CalledTwiceAtPredictedExit_DropsInOnce()
        {
            var (ship, moveDB, eti) = StartWarpArrivingOnHotloopBoundary();

            WarpMoveProcessor.WarpMove(ship, moveDB, eti);
            WarpMoveProcessor.WarpMove(ship, moveDB, eti);

            AssertSingleDropIn(ship, eti);
            Assert.IsFalse(ship.HasDataBlob<WarpMovingDB>(), "warp blob is gone after drop-in");
            Assert.IsTrue(ship.HasDataBlob<OrbitDB>(), "drop-in attached an orbit");
        }

        [Test]
        public void HotloopThenInstance_AtPredictedExit_DropsInOnce()
        {
            var (ship, moveDB, eti) = StartWarpArrivingOnHotloopBoundary();
            int deltaSeconds = (int)(eti - _starSys.StarSysDateTime).TotalSeconds;

            // Same order as ManagerSubPulse.ProcessToNextInterupt: all hotloops, then instance queue.
            _game.ProcessorManager.GetProcessor<WarpMovingDB>()
                .ProcessManager(_starSys, deltaSeconds);
            _game.ProcessorManager.GetInstanceProcessor(nameof(WarpMoveProcessor))
                .ProcessEntity(ship, eti);

            AssertSingleDropIn(ship, eti);
        }

        [Test]
        public void InstanceThenHotloop_AtPredictedExit_DropsInOnce()
        {
            var (ship, _, eti) = StartWarpArrivingOnHotloopBoundary();
            int deltaSeconds = (int)(eti - _starSys.StarSysDateTime).TotalSeconds;

            _game.ProcessorManager.GetInstanceProcessor(nameof(WarpMoveProcessor))
                .ProcessEntity(ship, eti);
            _game.ProcessorManager.GetProcessor<WarpMovingDB>()
                .ProcessManager(_starSys, deltaSeconds);

            AssertSingleDropIn(ship, eti);
        }

        [Test]
        public void ProcessSystem_ThroughAlignedArrival_DropsInOnce()
        {
            var (ship, _, eti) = StartWarpArrivingOnHotloopBoundary();

            _starSys.ManagerSubpulses.ProcessSystem(eti);

            AssertSingleDropIn(ship, eti);
            Assert.AreEqual(eti, _starSys.StarSysDateTime,
                "system clock should stop on the arrival interrupt, not a later hotloop");
            Assert.IsFalse(ship.HasDataBlob<WarpMovingDB>());
            Assert.IsTrue(ship.HasDataBlob<OrbitDB>());
        }

        [Test]
        public void ProcessSystem_PastArrival_DoesNotDropInAgain()
        {
            var (ship, _, eti) = StartWarpArrivingOnHotloopBoundary();

            _starSys.ManagerSubpulses.ProcessSystem(eti);
            Assert.AreEqual(1, _dropIns.Count, "precondition: first pass dropped in");

            _starSys.ManagerSubpulses.ProcessSystem(eti + WarpHotloop);

            Assert.AreEqual(1, _dropIns.Count,
                "a later pulse must not call SetOrbitHereSimpleNewt again");
        }

        [Test]
        public void HotloopOvershoot_BeforePredictedExit_DoesNotDropIn()
        {
            var (ship, moveDB, _) = StartWarpArrivingIn(WarpHotloop + TimeSpan.FromMinutes(2));
            var hotloopTick = ship.StarSysDateTime + WarpHotloop;
            Assert.Less(hotloopTick, moveDB.PredictedExitTime);

            int deltaSeconds = (int)WarpHotloop.TotalSeconds;
            _game.ProcessorManager.GetProcessor<WarpMovingDB>()
                .ProcessManager(_starSys, deltaSeconds);

            Assert.IsEmpty(_dropIns,
                "hotloop may snap the bubble to the exit, but drop-in waits for PredictedExitTime");
            Assert.IsTrue(ship.HasDataBlob<WarpMovingDB>(), "still warping until the arrival interrupt");
        }

        [Test]
        public void InstanceAtPredictedExit_ShortOfExitByFpResidual_DropsIn()
        {
            var (ship, moveDB, eti) = StartWarpArrivingOnHotloopBoundary();
            var dir = Vector3.Normalise(moveDB.ExitPointAbsolute - (Vector3)moveDB._position);
            moveDB._position = (Vector2)(moveDB.ExitPointAbsolute - dir * 0.65);
            moveDB.LastProcessDateTime = eti;

            _game.ProcessorManager.GetInstanceProcessor(nameof(WarpMoveProcessor))
                .ProcessEntity(ship, eti);

            AssertSingleDropIn(ship, eti);
            Assert.IsFalse(ship.HasDataBlob<WarpMovingDB>(), "eti interrupt must drop in even if overshoot missed");
        }

        [Test]
        public void HotloopAtPredictedExit_ShortOfExitByFpResidual_DoesNotDropIn()
        {
            var (ship, moveDB, eti) = StartWarpArrivingOnHotloopBoundary();
            var dir = Vector3.Normalise(moveDB.ExitPointAbsolute - (Vector3)moveDB._position);
            moveDB._position = (Vector2)(moveDB.ExitPointAbsolute - dir * 0.65);
            moveDB.LastProcessDateTime = eti;
            int deltaSeconds = (int)(eti - _starSys.StarSysDateTime).TotalSeconds;

            _game.ProcessorManager.GetProcessor<WarpMovingDB>()
                .ProcessManager(_starSys, deltaSeconds);

            Assert.IsEmpty(_dropIns,
                "hotloop still requires an overshoot; the instance interrupt absorbs the residual");
            Assert.IsTrue(ship.HasDataBlob<WarpMovingDB>());
        }

        void AssertSingleDropIn(Entity ship, DateTime eti)
        {
            Assert.AreEqual(1, _dropIns.Count,
                $"SetOrbitHereSimpleNewt count={_dropIns.Count}: {string.Join("; ", _dropIns)}");
            Assert.AreEqual(ship.Id, _dropIns[0].entityId);
            Assert.AreEqual(eti, _dropIns[0].at,
                "drop-in datetime should be PredictedExitTime, not a later/earlier hotloop");
        }

        /// <summary>
        /// Transit equals the warp hotloop so ProcessSystem's T+5min substep has both
        /// WarpMoveProcessor.ProcessManager and the PredictedExitTime instance interrupt.
        /// </summary>
        (Entity ship, WarpMovingDB moveDB, DateTime eti) StartWarpArrivingOnHotloopBoundary()
            => StartWarpArrivingIn(WarpHotloop);

        (Entity ship, WarpMovingDB moveDB, DateTime eti) StartWarpArrivingIn(TimeSpan transit)
        {
            var epoch = _starSys.StarSysDateTime;
            var sol = TestingUtilities.BasicSol(_starSys);
            var planet = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, epoch);

            var planetAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(planet, epoch);
            var ship = AddWarpShip(sol, planetAbs + new Vector3(5e9, 0, 0));
            var offset = new Vector3(200_000, 0, 0);

            var moveDB = new WarpMovingDB(ship, planet, offset, default);
            ship.SetDataBlob(moveDB);

            var pos = ship.GetDataBlob<PositionDB>();
            pos.SetParent(pos.Root);
            moveDB.HasStarted = true;
            moveDB.LastProcessDateTime = epoch;
            moveDB._position = (Vector2)pos.AbsolutePosition;
            var dest = moveDB.ExitPointAbsolute;
            double seconds = transit.TotalSeconds;
            var delta = dest - (Vector3)moveDB._position;
            moveDB.CurrentNonNewtonionVectorMS = Vector3.Normalise(delta) * (delta.Length() / seconds);
            moveDB.PredictedExitTime = epoch + transit;

            // Circular leftover velocity around the target so drop-in is elliptical.
            // A departure-frame leftover makes e>=1; OrbitDB.OnSetToEntity then NaNs in TimeToRadius.
            double sgp = GeneralMath.StandardGravitationalParameter(
                ship.GetDataBlob<MassVolumeDB>().MassTotal + planet.GetDataBlob<MassVolumeDB>().MassTotal);
            double r = moveDB.ExitPointrelative.Length();
            var tangential = Vector3.Normalise(new Vector3(-moveDB.ExitPointrelative.Y, moveDB.ExitPointrelative.X, 0));
            moveDB.SavedNewtonionVector = tangential * Math.Sqrt(sgp / r);

            WarpMoveProcessor.ScheduleArrival(ship, moveDB);

            Assert.AreEqual(PositionDB.MoveTypes.Orbit, planet.GetDataBlob<PositionDB>().MoveType,
                "precondition: destination MoveType is Orbit so EndWarpMove takes SetOrbitHereSimpleNewt");
            return (ship, moveDB, moveDB.PredictedExitTime);
        }

        Entity AddWarpShip(Entity parent, Vector3 absolutePos)
        {
            var ent = Entity.Create();
            var pos = new PositionDB { AbsolutePosition = absolutePos };
            var warp = new WarpAbilityDB { MaxSpeed = 1e7, EnergyType = "electricity" };
            var energy = new EnergyGenAbilityDB(_starSys.StarSysDateTime)
            {
                EnergyType = new TestEnergyType()
            };
            energy.EnergyStored["electricity"] = 1e12;
            energy.EnergyStoreMax["electricity"] = 1e12;
            _starSys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
                warp,
                energy,
                new NameDB("test-ship")
            });
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

        Entity AddOrbitingBody(Entity parent, double mass, double radius_m, double smaAu, DateTime epoch)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, 0, 0, 0, 0, 0, epoch);
            var ent = Entity.Create();
            var pos = new PositionDB();
            _starSys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB("test-planet")
            });
            OrbitProcessor.ProcessEntity(ent, epoch);
            return ent;
        }
    }
}
