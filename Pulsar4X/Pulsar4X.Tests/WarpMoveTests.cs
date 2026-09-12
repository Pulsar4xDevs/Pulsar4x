using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
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
    /// Contracts around warp intercept, exit points, and the Kepler drop-in used by
    /// WarpMoveProcessor.SetOrbitHereSimpleNewt.
    /// </summary>
    public class WarpMoveTests
    {
        private const double EpsilonLen = 1.0; // metres — same "oops" threshold as OrbitDB.FromVelocity

        private static Game _game = InitializeGame();
        private static StarSystem _starSys;

        private static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);
            // TimeStep is otherwise Task.Run and the test would race it.
            game.Settings.EnforceSingleThread = true;
            game.Settings.StrictNewtonion = true;
            return game;
        }

        [SetUp]
        public void SetUp()
        {
            // A fresh system each test: Initialize() does not clear entities, and leftover
            // WarpMovingDBs from other tests get processed by ProcessSystem.
            // TimePulse skips Stasis systems. A faction ship would otherwise promote
            // the new system to Background (10× slower orbit hotloops) via
            // HasFactionEntities — same as an unfocused system in-game. Pin it
            // Foreground like the player's focused map.
            foreach (var sys in _game.Systems)
                sys.SetActivityState(SystemActivityState.Stasis);
            _starSys = new StarSystem();
            _starSys.Initialize(_game, "Sol", -1);
            _starSys.IncrementExternalObserver(priority: true);
        }

        #region GetRalitivePosition

        [Test]
        public void GetRalitivePosition_EntityEntity_IsFirstMinusSecond()
        {
            var a = AddBody(new Vector3(10_000, 0, 0));
            var b = AddBody(new Vector3(0, 0, 0));

            var ab = MoveMath.GetRalitivePosition(a, b);
            Assert.AreEqual(10_000, ab.X, EpsilonLen, "a relative to b should be a - b");
            Assert.AreEqual(0, ab.Y, EpsilonLen);
            Assert.AreEqual(0, ab.Z, EpsilonLen);
        }

        [Test]
        public void GetRalitivePosition_EntityThenPoint_IsEntityMinusPoint()
        {
            var parent = AddBody(new Vector3(1_000, 0, 0));
            var exitAbs = new Vector3(4_000, 0, 0);

            var result = MoveMath.GetRalitivePosition(parent, exitAbs);
            Assert.AreEqual(-3_000, result.X, EpsilonLen,
                "old (Entity, Vector3) overload is entity minus point — parent as seen from the exit");
        }

        [Test]
        public void GetRalitivePosition_PointThenEntity_IsPointMinusEntity()
        {
            var parent = AddBody(new Vector3(1_000, 0, 0));
            var exitAbs = new Vector3(4_000, 0, 0);

            var result = MoveMath.GetRalitivePosition(exitAbs, parent);
            Assert.AreEqual(3_000, result.X, EpsilonLen,
                "new (Vector3, Entity) overload is point minus entity — exit as seen from the parent");
        }

        [Test]
        public void GetRalitivePosition_SnapshotIgnoresOrbitAtLaterTime()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var epoch = new DateTime(2000, 1, 1);
            var planet = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, epoch);

            var later = epoch + TimeSpan.FromDays(90);
            var futureAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(planet, later);
            var blobAbs = planet.GetDataBlob<PositionDB>().AbsolutePosition;
            var exitAbs = futureAbs + new Vector3(200_000, 0, 0);

            var fromBlob = MoveMath.GetRalitivePosition(exitAbs, planet);
            var fromFuture = exitAbs - futureAbs;

            Assert.AreNotEqual(0, (futureAbs - blobAbs).Length(),
                "sanity: planet has moved in 90 days so blob-now and future pos must differ");
            Assert.Greater((fromBlob - fromFuture).Length(), 1_000,
                "snapshot relative pos is not the same as relative pos at 'later' — drop-in needs a DateTime");
        }

        #endregion

        #region Intercept and ExitPointAbsolute

        [Test]
        public void GetInterceptPosition_OrbitTarget_AlreadyIncludesOffset()
        {
            var (ship, planet, offset, start) = MakeShipAndOrbitingPlanet();

            var intercept = WarpMath.GetInterceptPosition(ship, planet, start, offset);
            var bodyAtIntercept = OrbitMath.GetAbsolutePosition(planet.GetDataBlob<OrbitDB>(), intercept.etiDateTime);
            var expectedExit = bodyAtIntercept + offset;

            Assert.AreEqual(0, (intercept.position - expectedExit).Length(), 10,
                "intercept.position is already body(t) + offset, not the body's centre");
        }

        [Test]
        public void WarpMovingDB_ExitPointAbsolute_MustNotAddOffsetTwice()
        {
            var (ship, planet, offset, _) = MakeShipAndOrbitingPlanet();

            // Constructor starts the intercept search at StarSysDateTime, not at the
            // planet orbit epoch we used to build the scene. Using a different clock
            // here picks a different point on the same 1 AU circle (same magnitude,
            // different X/Y) and is not a double-offset.
            var startTime = ship.StarSysDateTime;
            var intercept = WarpMath.GetInterceptPosition(ship, planet, startTime, offset);
            var moveDB = new WarpMovingDB(ship, planet, offset, default);

            Assert.AreEqual(offset, moveDB.ExitPointrelative, "relative exit is the planning offset");
            Assert.AreEqual(startTime, moveDB.EntryDateTime,
                "constructor EntryDateTime is StarSysDateTime; intercept must use that same instant");

            var extraOffset = (moveDB.ExitPointAbsolute - intercept.position).Length();
            Assert.AreEqual(0, extraOffset, 10,
                "ExitPointAbsolute must equal intercept.position computed at EntryDateTime. " +
                "If this fails by about |offset|, the constructor is adding offset a second time.");

            var bodyAtExit = OrbitMath.GetAbsolutePosition(planet.GetDataBlob<OrbitDB>(), moveDB.PredictedExitTime);
            var expectedAbs = bodyAtExit + offset;
            Assert.AreEqual(0, (moveDB.ExitPointAbsolute - expectedAbs).Length(), 10,
                "ExitPointAbsolute should be one offset from the body at PredictedExitTime");
        }

        /// <summary>
        /// Live start: WarpMoveAction sets WarpMovingDB (OnSetToEntity strips OrbitDB)
        /// then TryStartWarp recomputes the intercept. GetAbsoluteFuturePosition used to
        /// fall through to (0,0) because MoveType was still Orbit with no orbit blob,
        /// so the search ran as if the ship sat at the sun.
        /// </summary>
        [Test]
        public void GetAbsoluteFuturePosition_AfterWarpBlobStripsOrbit_IsNotTheOrigin()
        {
            var (ship, planet, offset, start) = MakeShipInEarthOrbit();
            var before = (Vector3)MoveMath.GetAbsoluteFuturePosition(ship, start);
            Assert.Greater(before.Length(), 1e10, "precondition: ship is near 1 AU, not the sun");

            ship.SetDataBlob(new WarpMovingDB(ship, planet, offset, default));

            Assert.IsFalse(ship.HasDataBlob<OrbitDB>(), "OnSetToEntity must have stripped the orbit");
            Assert.AreEqual(PositionDB.MoveTypes.Orbit, ship.GetDataBlob<PositionDB>().MoveType,
                "MoveType stays Orbit until MoveStateProcessor; that is the live footgun");

            var after = (Vector3)MoveMath.GetAbsoluteFuturePosition(ship, start);
            Assert.Greater(after.Length(), 1e10,
                "future pos must not become the system origin after the orbit blob is gone");
            Assert.Less((after - before).Length(), 1e7,
                "stripped-orbit fallback should be the last AbsolutePosition, not a new Kepler");
        }

        [Test]
        public void TryStartWarp_AfterOrbitRemoved_AimsFromShipNotSun()
        {
            var (ship, planet, offset, start) = MakeShipInEarthOrbit();
            AttachWarpEnergy(ship);
            var startAbs = ship.GetDataBlob<PositionDB>().AbsolutePosition;

            var moveDB = new WarpMovingDB(ship, planet, offset, default);
            ship.SetDataBlob(moveDB);
            Assert.IsFalse(ship.HasDataBlob<OrbitDB>());

            Assert.IsTrue(WarpMoveProcessor.TryStartWarp(ship, moveDB, start),
                "precondition: batteries are charged");

            var bodyAtExit = OrbitMath.GetAbsolutePosition(planet.GetDataBlob<OrbitDB>(), moveDB.PredictedExitTime);
            var exitError = (moveDB.ExitPointAbsolute - (bodyAtExit + offset)).Length();
            var flightDist = (moveDB.ExitPointAbsolute - (Vector3)moveDB._position).Length();
            var flightTime = (moveDB.PredictedExitTime - start).TotalSeconds;
            var expectedDist = ship.GetDataBlob<WarpAbilityDB>().MaxSpeed * flightTime;

            Assert.Multiple(() =>
            {
                Assert.Greater(((Vector3)moveDB._position).Length(), 1e10,
                    "bubble start is the ship at Earth, not the sun");
                Assert.Less(((Vector3)moveDB._position - startAbs).Length(), 1e7,
                    "bubble start should match the ship's AbsolutePosition at start");
                Assert.Less(exitError, 1e6,
                    "ExitPointAbsolute must be the body at PredictedExitTime plus offset, not a sun-relative guess");
                Assert.AreEqual(0, flightDist - expectedDist, 1e6,
                    "PredictedExitTime must be the travel time from the ship's start, not from the origin");
            });
        }

        [Test]
        public void TryStartWarp_EarthToPhobos_ExitIsNearPhobosAtEti()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var epoch = new DateTime(2000, 1, 1);
            var earth = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, epoch);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);

            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, epoch);
            var ship = AddShip(earth, 1e6, earthAbs + new Vector3(6_571_000, 0, 0));
            ship.SetDataBlob(OrbitDB.FromPosition(earth, ship, epoch));
            OrbitProcessor.ProcessEntity(ship, epoch);
            AttachWarpEnergy(ship);

            var offset = new Vector3(13_000, 0, 0);
            var moveDB = new WarpMovingDB(ship, phobos, offset, default);
            ship.SetDataBlob(moveDB);
            Assert.IsTrue(WarpMoveProcessor.TryStartWarp(ship, moveDB, epoch));

            var phobosAtExit = OrbitMath.GetAbsolutePosition(phobos.GetDataBlob<OrbitDB>(), moveDB.PredictedExitTime);
            var miss = (moveDB.ExitPointAbsolute - (phobosAtExit + offset)).Length();
            var flightDist = (moveDB.ExitPointAbsolute - (Vector3)moveDB._position).Length();
            var flightTime = (moveDB.PredictedExitTime - epoch).TotalSeconds;
            var expectedDist = ship.GetDataBlob<WarpAbilityDB>().MaxSpeed * flightTime;

            Assert.Multiple(() =>
            {
                Assert.Less(miss, 1e6,
                    "live TryStartWarp intercept must be at Phobos(eti)+offset, not a point aimed from the sun");
                Assert.AreEqual(0, flightDist - expectedDist, 1e6,
                    "eti must be the flight time from Earth, not from the origin");
            });
        }

        #endregion

        #region SOI parent switch



        /// <summary>
        /// Phobos SOI is smaller than a surface offset, so drop-in parents to Mars.
        ///
        /// This is the SetOrbitHereSimpleNewt branch (soi &lt; |offset|): r is Mars-relative,
        /// leftover v is still start-parent-relative (relative warp mode). That leftover is
        /// often enough for e ≥ 1 around Mars — that is the relative-mode rule, not a
        /// malformed orbit. Hyperbolic elements with finite a, e, µ are valid; Period is
        /// stored as MaxValue because there is no closed period.
        ///
        /// Failures that still matter: NaN/zero Kepler, parent still Phobos, r ≈ |offset|
        /// (used the Phobos frame), or r inside Mars (mixed-time ExitPointAbsolute vs Mars).
        /// </summary>
        [Test]
        public void WarpDropIn_Phobos_ParentsToMarsWithFiniteTrajectory()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var epoch = new DateTime(2000, 1, 1);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);

            var phobosSoi = OrbitMath.GetSOIRadius(phobos.GetDataBlob<OrbitDB>());
            var offset = new Vector3(13_000, 0, 0);
            Assert.Less(phobosSoi, offset.Length(), "precondition: Phobos cannot keep this stop");

            // Circular around Sol so leftover v is start-parent-relative (Sol), as relative
            // mode stores it. Not transformed into Mars's frame on exit.
            var shipMass = 1e6;
            var shipStartAbs = new Vector3(1.0e11, 0, 0);
            var ship = AddShip(sol, shipMass, shipStartAbs);
            ship.SetDataBlob(OrbitDB.FromPosition(sol, ship, epoch));

            var moveDB = new WarpMovingDB(ship, phobos, offset, default);
            Assert.AreEqual(0, moveDB.EndpointTargetOrbit.StandardGravParameter, 0,
                "CreateWarpOnly / default endpoint orbit takes the FromKeplerElements path");

            var at = moveDB.PredictedExitTime;
            var targetSoi = OrbitMath.GetSOIRadius(phobos.GetDataBlob<OrbitDB>());
            Assert.Less(targetSoi, moveDB.ExitPointrelative.Length());

            // Same branch as SetOrbitHereSimpleNewt when soi < |offset|.
            var orbitalParent = phobos.GetSOIParentEntity();
            var combinedMass = shipMass + orbitalParent.GetDataBlob<MassVolumeDB>().MassTotal;
            var sgp = GeneralMath.StandardGravitationalParameter(combinedMass);
            var parentAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(orbitalParent, at);
            var r = moveDB.ExitPointAbsolute - parentAbs;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, r, moveDB.SavedNewtonionVector, at);

            AssertKeplerWellFormed(ke, "Kepler from Phobos parent-switch drop-in");

            var newOrbit = OrbitDB.FromKeplerElements(orbitalParent, shipMass, ke, at);
            ship.SetDataBlob(newOrbit);
            OrbitProcessor.ProcessEntity(ship, at);

            AssertOrbitWellFormed(newOrbit, "OrbitDB after Phobos parent-switch drop-in");

            var pos2 = ship.GetDataBlob<PositionDB>().RelativePosition;
            var posFromOrbit = OrbitMath.GetPosition(newOrbit, at);
            var marsRadius = mars.GetDataBlob<MassVolumeDB>().RadiusInM;
            var phobosR = OrbitMath.GetPosition(phobos.GetDataBlob<OrbitDB>(), at).Length();
            double rLen = r.Length();

            Assert.Multiple(() =>
            {
                Assert.AreSame(mars, orbitalParent, "too-small SOI must parent the ship to Mars, not Phobos");
                Assert.AreSame(mars, newOrbit.Parent, "OrbitDB.Parent must be Mars");
                Assert.AreSame(mars, ship.GetDataBlob<PositionDB>().Parent, "PositionDB.Parent must be Mars");

                AssertFiniteVec(pos2, "PositionDB.RelativePosition");
                AssertFiniteVec(posFromOrbit, "OrbitMath.GetPosition at epoch");

                Assert.Greater(rLen, marsRadius,
                    "Mars-relative r is inside Mars (Phobos-frame offset used as Mars-relative, " +
                    "or ExitPointAbsolute vs Mars at different times).");
                Assert.Greater(rLen, offset.Length() * 10,
                    "r is still ~the Phobos-relative offset; parent switch did not change frame");
                Assert.Less(rLen, 1e11,
                    "r is heliocentric-scale; drop-in parented to Sol or used an absolute vector as Mars-relative");
                // Intercept residual + frozen ExitPointAbsolute vs Mars(at) can shift |r|
                // vs Phobos SMA by thousands of km. That is not "inside Mars" or "13 km offset".
                Assert.Greater(rLen, phobosR * 0.5, "|r| much closer than Phobos — mixed-time intercept?");
                Assert.Less(rLen, phobosR * 2.0, "|r| much farther than Phobos — mixed-time intercept?");

                Assert.Greater(pos2.Length(), marsRadius, "PositionDB after attach is inside Mars");
            });
        }

        [Test]
        public void LargeBodySoi_LargerThanOffset_MeansOrbitTheTarget()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var epoch = new DateTime(2000, 1, 1);
            var earth = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, epoch);

            var soi = OrbitMath.GetSOIRadius(earth.GetDataBlob<OrbitDB>());
            var offset = new Vector3(200_000, 0, 0); // LEO-ish

            Assert.Greater(soi, offset.Length(),
                "Earth-class body: planned offset sits inside SOI, so drop-in should parent to the target");
        }

        #endregion

        #region Earth to Phobos warp order

        public enum EarthStartOrbit
        {
            /// <summary>Circular prograde LEO. Leftover v is ~7.8 km/s into Mars — often hyperbolic.</summary>
            CircularPrograde,
            /// <summary>Circular retrograde LEO (i = π).</summary>
            CircularRetrograde,
            /// <summary>e = 0.4 prograde, periapsis at the LEO radius.</summary>
            EccentricPrograde,
            /// <summary>
            /// Earth-relative leftover equals circular speed around Mars at the predicted
            /// drop-in r, so the relative-mode dump should be a near-circular Mars orbit.
            /// </summary>
            LeftoverCircularAtMars,
            /// <summary>Same as LeftoverCircularAtMars but retrograde around Mars.</summary>
            LeftoverCircularAtMarsRetrograde,
        }

        public static IEnumerable EarthToBodyStartOrbits()
        {
            foreach (EarthStartOrbit kind in Enum.GetValues(typeof(EarthStartOrbit)))
            {
                yield return new TestCaseData(kind, true).SetName($"{kind}-Phobos");
                yield return new TestCaseData(kind, false).SetName($"{kind}-Mars");
            }
        }

        /// <summary>
        /// In-game MoveTo codeflow: CommandTranslator → AssignGoal → warp → drop-in
        /// (WakeActionQueue → agent Plan from the world as it is) → next TimePulse
        /// (circularise Execute) → keep pulsing until the MoveTo goal itself completes.
        /// Must not start a second warp. Phobos SOI is smaller than a low-orbit offset,
        /// so drop-in parents to Mars; Mars is the destination well for both cases.
        /// </summary>
        [Test, TestCaseSource(nameof(EarthToBodyStartOrbits))]
        public void WarpOrder_EarthToPhobos_ArrivesOnTimeInPlaceWithWellFormedOrbit(
            EarthStartOrbit startOrbit, bool destIsPhobos)
        {
            var epoch = _starSys.StarSysDateTime;
            var sol = TestingUtilities.BasicSol(_starSys);
            var earth = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, epoch);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);
            mars.SetDataBlob(new SystemBodyInfoDB { BodyType = BodyType.Terrestrial });
            phobos.SetDataBlob(new SystemBodyInfoDB { BodyType = BodyType.Moon });

            Entity dest = destIsPhobos ? phobos : mars;
            Entity expectedParent = mars;

            var faction = FactionFactory.CreateFaction(_game, "move-to-" + Guid.NewGuid().ToString("N"));
            var leoR = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, epoch);
            var ship = MakeMoveToShip(earth, earthAbs + new Vector3(leoR, 0, 0), faction);

            var offsetGuess = new Vector3(OrbitMath.LowOrbitRadius(dest), 0, 0);
            ApplyEarthStartOrbit(startOrbit, ship, earth, mars, dest, epoch, offsetGuess);
            var offset = PhobosWarpOffset(ship, dest);
            if (startOrbit is EarthStartOrbit.LeftoverCircularAtMars
                or EarthStartOrbit.LeftoverCircularAtMarsRetrograde)
            {
                ApplyEarthStartOrbit(startOrbit, ship, earth, mars, dest, epoch, offset);
                offset = PhobosWarpOffset(ship, dest);
            }

            if (destIsPhobos)
            {
                var phobosSoi = OrbitMath.GetSOIRadius(phobos.GetDataBlob<OrbitDB>());
                Assert.Less(phobosSoi, offset.Length(), "precondition: drop-in parents to Mars");
            }

            var dropIns = new List<(int entityId, DateTime at)>();
            WarpMoveProcessor.TestDropIn = (entity, at) => dropIns.Add((entity.Id, at));
            try
            {
                // Same seam as CommandTranslator.TranslateMoveToBody.
                var goal = new Goal(GoalType.MoveTo) { TargetEntityID = dest.Id };
                AgentProcessor.AssignGoal(ship, goal);
                Assert.AreNotEqual(GoalStatus.Failed, goal.Status, DescribeMove(ship, goal));
                Assert.IsTrue(ship.HasDataBlob<WarpMovingDB>(),
                    "AssignGoal should have planned and started warp. " + DescribeMove(ship, goal));

                var moveDB = ship.GetDataBlob<WarpMovingDB>();
                Assert.IsTrue(moveDB.HasStarted, "bubble should have started. " + DescribeMove(ship, goal));
                var eti = moveDB.PredictedExitTime;
                var exitAbs = moveDB.ExitPointAbsolute;
                Assert.Greater(eti, epoch, "intercept is in the future");
                AssertFiniteVec(exitAbs, "ExitPointAbsolute at warp start");

                AdvanceTo(eti);

                Assert.AreEqual(1, dropIns.Count,
                    "drop-in must run once at PredictedExitTime. " + DescribeMove(ship, goal));
                Assert.IsTrue(ship.HasDataBlob<OrbitDB>(), "drop-in attached an orbit");
                Assert.IsFalse(ship.HasDataBlob<WarpMovingDB>(),
                    "bubble is gone after arrival; a second warp must not have started. "
                    + DescribeMove(ship, goal));

                var dropAt = dropIns[0].at;
                var orbit = ship.GetDataBlob<OrbitDB>();
                var pos = ship.GetDataBlob<PositionDB>();
                var posFromOrbit = OrbitMath.GetPosition(orbit, dropAt);
                var rel = pos.RelativePosition;
                var destAbs = OrbitMath.GetAbsolutePosition(dest.GetDataBlob<OrbitDB>(), dropAt);
                var marsRadius = mars.GetDataBlob<MassVolumeDB>().RadiusInM;

                Assert.Multiple(() =>
                {
                    Assert.AreEqual(ship.Id, dropIns[0].entityId);
                    Assert.AreEqual(eti, dropAt,
                        "drop-in datetime is PredictedExitTime, not a later hotloop");

                    AssertOrbitWellFormed(orbit, $"{startOrbit} drop-in orbit");
                    Assert.AreSame(expectedParent, orbit.Parent, "drop-in SOI parent");
                    Assert.AreSame(expectedParent, pos.Parent, "PositionDB.Parent");

                    AssertFiniteVec(rel, "PositionDB.RelativePosition");
                    AssertFiniteVec(pos.AbsolutePosition, "PositionDB.AbsolutePosition");
                    AssertFiniteVec(posFromOrbit, "OrbitMath.GetPosition at drop-in");
                    AssertFiniteVec(pos.Velocity, "PositionDB.Velocity");

                    // PositionDB.AbsolutePosition is hotloop-quantized to the parent's last
                    // orbit tick. The drop-in contract is the Kepler state at eti.
                    var absAtDrop = (Vector3)MoveMath.GetAbsoluteFuturePosition(ship, dropAt);
                    Assert.Less((absAtDrop - exitAbs).Length(), 1e6,
                        "ship absolute pos at drop-in should be the intercept exit");
                    Assert.Less((absAtDrop - (destAbs + offset)).Length(), 1e6,
                        "ship should be one offset from the destination body at drop-in");
                    Assert.Greater(rel.Length(), marsRadius, "Mars-relative r is inside Mars");
                    Assert.Less((rel - posFromOrbit).Length(), 1e3,
                        "PositionDB.RelativePosition must match the orbit equations at drop-in");
                });

                var queue = ship.GetDataBlob<ActionQueueDB>();
                Assert.IsFalse(queue.ActionList.Any(a => a is WarpMoveAction && a.IsRunning),
                    "warp action must have finished at drop-in. " + DescribeMove(ship, goal));

                // In-game: next TimePulse after drop-in. Same-time follow-on after Split()
                // plus HandleOrder's GameGlobalDateTime lag both need this extra step.
                AdvanceTo(eti + TimeSpan.FromMinutes(1));
                Assert.AreEqual(1, dropIns.Count,
                    "must not warp again after drop-in. " + DescribeMove(ship, goal));
                Assert.IsFalse(ship.HasDataBlob<WarpMovingDB>(),
                    "must not start another warp after drop-in. " + DescribeMove(ship, goal));
                Assert.IsFalse(queue.ActionList.Any(a => a is WarpMoveAction
                                                         && a.Status is ActionStatus.Queued or ActionStatus.Running),
                    "must not have queued another warp. " + DescribeMove(ship, goal));

                bool circulariseStarted = ship.HasDataBlob<NewtonSimpleMoveDB>()
                    || queue.ActionList.Any(a => a is NewtonSimpleAction
                                                 && a.Status is ActionStatus.Running or ActionStatus.Succeeded);
                bool alreadyParked = ship.TryGetDataBlob<OrbitDB>(out var afterOrbit)
                    && afterOrbit.Eccentricity < 0.05
                    && afterOrbit.Parent == expectedParent;
                Assert.IsTrue(circulariseStarted || alreadyParked || goal.Status == GoalStatus.Completed,
                    "expected circularise / parked orbit / completed goal after the next timestep. "
                    + DescribeMove(ship, goal));

                // Run until the MoveTo goal itself should have completed — not just drop-in.
                var deadline = eti + TimeSpan.FromDays(2);
                while (goal.Status is not (GoalStatus.Completed or GoalStatus.Failed)
                       && _starSys.StarSysDateTime < deadline)
                {
                    AdvanceTo(_starSys.StarSysDateTime + TimeSpan.FromMinutes(30));
                    Assert.AreEqual(1, dropIns.Count,
                        "MoveTo must not warp again while working the goal. " + DescribeMove(ship, goal));
                    Assert.IsFalse(ship.HasDataBlob<WarpMovingDB>(),
                        "MoveTo must not start another warp. " + DescribeMove(ship, goal));
                }

                Assert.AreEqual(GoalStatus.Completed, goal.Status,
                    "MoveTo should complete. " + DescribeMove(ship, goal));
                Assert.IsTrue(ship.TryGetDataBlob<OrbitDB>(out var finalOrbit), "finished on an orbit");
                AssertOrbitWellFormed(finalOrbit, "final orbit");
                Assert.AreSame(expectedParent, finalOrbit.Parent);
                Assert.Greater(ship.GetDataBlob<PositionDB>().RelativePosition.Length(), marsRadius);
            }
            finally
            {
                WarpMoveProcessor.TestDropIn = null;
            }
        }

        /// <summary>
        /// Preview circularise after warp-to-Phobos must use the drop-in parent (Mars) µ.
        /// Phobos SOI is smaller than low-orbit offset, so SetOrbitHereSimpleNewt parents
        /// to Mars; a Phobos-frame Kepler here is how distance-to-parent became ~1.64e8 AU.
        /// </summary>
        [Test]
        public void MoveToPlan_EarthToPhobos_NewtonSimpleDistanceToParentIsNotInterstellar()
        {
            var epoch = _starSys.StarSysDateTime;
            var sol = TestingUtilities.BasicSol(_starSys);
            var earth = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, epoch);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);

            var leoR = earth.GetDataBlob<MassVolumeDB>().RadiusInM + 200_000;
            var earthAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(earth, epoch);
            var ship = AddPhobosWarpShip(earth, earthAbs + new Vector3(leoR, 0, 0));
            ApplyEarthStartOrbit(EarthStartOrbit.CircularPrograde, ship, earth, mars, phobos, epoch,
                new Vector3(OrbitMath.LowOrbitRadius(phobos), 0, 0));

            Assert.IsTrue(MovePlanner.TryBuildMoveActions(ship, phobos, out var actions, out var reason),
                reason);
            Assert.GreaterOrEqual(actions.Count, 2, "MoveTo warp plan is transit then a Mars-frame follow-on");
            Assert.IsInstanceOf<WarpMoveAction>(actions[0]);
            Assert.IsInstanceOf<NewtonSimpleAction>(actions[1]);
            var newt = (NewtonSimpleAction)actions[1];

            var queue = ship.GetDataBlob<ActionQueueDB>();
            // Warp only on the queue: NewtonSimple.Execute calls NewtonMove which NREs on
            // empty cargo. The Kepler elements below are still the live planner's.
            queue.Enqueue(actions[0]);

            var dropIns = new List<(int entityId, DateTime at)>();
            WarpMoveProcessor.TestDropIn = (entity, at) => dropIns.Add((entity.Id, at));
            try
            {
                _game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor))
                    .ProcessEntity(ship, epoch);
                Assert.IsTrue(ship.HasDataBlob<WarpMovingDB>(), "warp should have started");
                var eti = ship.GetDataBlob<WarpMovingDB>().PredictedExitTime;
                _starSys.ManagerSubpulses.ProcessSystem(eti);
                Assert.AreEqual(1, dropIns.Count, "warp drop-in at PredictedExitTime");

                var parent = ship.GetSOIParentEntity();
                Assert.AreSame(mars, parent, "drop-in parents to Mars");
                var marsSgp = GeneralMath.StandardGravitationalParameter(
                    mars.GetDataBlob<MassVolumeDB>().MassTotal);
                double startRatio = newt.StartKE.StandardGravParameter / marsSgp;
                double targetRatio = newt.TargetKE.StandardGravParameter / marsSgp;
                Assert.Greater(startRatio, 0.9, "preview StartKE is Mars-µ, not Phobos-µ");
                Assert.Less(startRatio, 1.1, "preview StartKE is Mars-µ, not Phobos-µ");
                Assert.Greater(targetRatio, 0.9, "preview TargetKE is Mars-µ, not Phobos-µ");
                Assert.Less(targetRatio, 1.1, "preview TargetKE is Mars-µ, not Phobos-µ");

                var r = OrbitMath.GetStateVectors(newt.StartKE, newt.ActionOnDate).position;
                Assert.Greater(r.Length(), 1e6,
                    "StartKE r is Mars-relative (Phobos SMA scale), not Phobos low-orbit ~13 km");
                Assert.Less(r.Length(), 5e7, "StartKE r is still in the Mars system");

                Assert.DoesNotThrow(
                    () => new NewtonSimpleMoveDB(parent, newt.StartKE, newt.TargetKE, newt.ActionOnDate));
            }
            finally
            {
                WarpMoveProcessor.TestDropIn = null;
            }
        }

        [Test]
        public void MoveToPlan_HyperbolicAroundMarsNearPhobos_MatchesPhobosOrbit()
        {
            var epoch = _starSys.StarSysDateTime;
            var sol = TestingUtilities.BasicSol(_starSys);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);
            phobos.SetDataBlob(new SystemBodyInfoDB { BodyType = BodyType.Moon });

            var phobosAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(phobos, epoch);
            var ship = AddPhobosWarpShip(mars, phobosAbs);
            AttachThrust(ship);
            var r = ship.GetDataBlob<PositionDB>().RelativePosition;
            double mu = OrbitMath.SGP(mars, ship);
            double vCirc = Math.Sqrt(mu / r.Length());
            var perp = Vector3.Normalise(new Vector3(-r.Y, r.X, 0));
            ship.SetDataBlob(OrbitDB.FromVelocity(mars, ship, perp * vCirc * 1.5, epoch));
            OrbitProcessor.ProcessEntity(ship, epoch);

            Assert.IsTrue(MovePlanner.TryBuildMoveActions(ship, phobos, out var actions, out var reason),
                reason);
            Assert.AreEqual(1, actions.Count, "in Phobos's orbit with leftover e: circularise to match, do not warp");
            Assert.IsInstanceOf<NewtonSimpleAction>(actions[0]);
            var newt = (NewtonSimpleAction)actions[0];

            var marsSgp = GeneralMath.StandardGravitationalParameter(
                mars.GetDataBlob<MassVolumeDB>().MassTotal);
            Assert.Greater(newt.StartKE.StandardGravParameter / marsSgp, 0.9);
            Assert.Less(newt.StartKE.StandardGravParameter / marsSgp, 1.1);
            Assert.Less(newt.TargetKE.Eccentricity, 0.05, "match is a circular Mars orbit");
            Assert.AreEqual(r.Length(), newt.TargetKE.SemiMajorAxis, r.Length() * 0.01,
                "circularise at current r, which is already Phobos SMA");
            Assert.DoesNotThrow(
                () => new NewtonSimpleMoveDB(mars, newt.StartKE, newt.TargetKE, newt.ActionOnDate));
        }

        [Test]
        public void MoveToPlan_CircularAroundMarsNearPhobos_IsAlreadyThere()
        {
            var epoch = _starSys.StarSysDateTime;
            var sol = TestingUtilities.BasicSol(_starSys);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);
            phobos.SetDataBlob(new SystemBodyInfoDB { BodyType = BodyType.Moon });

            var phobosAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(phobos, epoch);
            var offset = new Vector3(OrbitMath.LowOrbitRadius(phobos), 0, 0);
            var ship = AddPhobosWarpShip(mars, phobosAbs + offset);
            ship.SetDataBlob(OrbitDB.FromPosition(mars, ship, epoch));
            OrbitProcessor.ProcessEntity(ship, epoch);

            Assert.IsTrue(MovePlanner.TryBuildMoveActions(ship, phobos, out var actions, out var reason),
                reason);
            Assert.IsEmpty(actions, "matching Phobos's orbit around Mars and alongside is arrived");
        }

        [Test]
        public void MoveToPlan_CircularAroundMarsOppositePhobos_PhasesToMatch()
        {
            var epoch = _starSys.StarSysDateTime;
            var sol = TestingUtilities.BasicSol(_starSys);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);
            phobos.SetDataBlob(new SystemBodyInfoDB { BodyType = BodyType.Moon });

            var marsAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(mars, epoch);
            var phobosAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(phobos, epoch);
            var rPhobos = phobosAbs - marsAbs;
            var ship = AddPhobosWarpShip(mars, marsAbs - rPhobos);
            AttachThrust(ship);
            ship.SetDataBlob(OrbitDB.FromPosition(mars, ship, epoch));
            OrbitProcessor.ProcessEntity(ship, epoch);

            Assert.IsTrue(MovePlanner.TryBuildMoveActions(ship, phobos, out var actions, out var reason),
                reason);
            Assert.IsNotEmpty(actions, "same orbit, wrong place: phase, do not call it arrived");
            Assert.IsFalse(actions.Exists(a => a is WarpMoveAction), "do not warp; match Phobos's orbit");
            Assert.IsTrue(actions.TrueForAll(a => a is NewtonSimpleAction));
        }

        #endregion

        #region Kepler drop-in reconstructs r

        [Test]
        public void KeplerFromExitRelative_ReconstructsSamePositionAtEpoch()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var epoch = new DateTime(2000, 1, 1);
            var earth = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, epoch);

            var shipMass = 1e6;
            var ship = AddShip(sol, shipMass, new Vector3(1e11, 0, 0));

            var r = new Vector3(200_000, 0, 0);
            var combined = shipMass + earth.GetDataBlob<MassVolumeDB>().MassTotal;
            var sgp = GeneralMath.StandardGravitationalParameter(combined);
            var circSpeed = Math.Sqrt(sgp / r.Length());
            var vel = new Vector3(0, circSpeed, 0);

            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, r, vel, epoch);
            var newOrbit = OrbitDB.FromKeplerElements(earth, shipMass, ke, epoch);
            ship.SetDataBlob(newOrbit);
            OrbitProcessor.ProcessEntity(ship, epoch);

            var pos2 = ship.GetDataBlob<PositionDB>().RelativePosition;
            var reconstructed = OrbitMath.GetPosition(newOrbit, epoch);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, (reconstructed - r).Length(), EpsilonLen,
                    "orbit equations at epoch must return the r used to build the elements");
                Assert.AreEqual(0, (pos2 - r).Length(), EpsilonLen,
                    "after SetDataBlob + ProcessEntity, PositionDB.RelativePosition must be that same r " +
                    "(the pos2 vs ExitPointrelative check for the inside-SOI path)");
            });
        }

        [Test]
        public void ParentSwitchDropIn_UsesExitAbsoluteMinusParentAtSameTime()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var epoch = new DateTime(2000, 1, 1);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);

            var offset = new Vector3(13_000, 0, 0);
            var soi = OrbitMath.GetSOIRadius(phobos.GetDataBlob<OrbitDB>());
            Assert.Less(soi, offset.Length(), "precondition: this is a parent-switch drop");

            var at = epoch + TimeSpan.FromHours(3);
            var exitAbs = OrbitMath.GetAbsolutePosition(phobos.GetDataBlob<OrbitDB>(), at) + offset;
            var parentAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(mars, at);
            var r = exitAbs - parentAbs;

            var shipMass = 1e6;
            var ship = AddShip(sol, shipMass, exitAbs);
            var combined = shipMass + mars.GetDataBlob<MassVolumeDB>().MassTotal;
            var sgp = GeneralMath.StandardGravitationalParameter(combined);
            var vel = new Vector3(0, Math.Sqrt(sgp / r.Length()), 0);

            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, r, vel, at);
            var newOrbit = OrbitDB.FromKeplerElements(mars, shipMass, ke, at);
            ship.SetDataBlob(newOrbit);
            OrbitProcessor.ProcessEntity(ship, at);

            var pos2 = ship.GetDataBlob<PositionDB>().RelativePosition;
            Assert.AreEqual(0, (pos2 - r).Length(), 10,
                "parent-switch path: pos2 must equal ExitPointAbsolute - parentAbs(atDateTime)");
        }

        #endregion

        #region Arrival interrupt

        [Test]
        public void WarpMoveProcessor_IsRegisteredAsInstanceProcessor()
        {
            var proc = _game.ProcessorManager.GetInstanceProcessor(nameof(WarpMoveProcessor));
            Assert.IsInstanceOf<WarpMoveProcessor>(proc);
        }

        [Test]
        public void ScheduleArrival_AddsInstanceInterruptAtPredictedExitTime()
        {
            var (ship, planet, offset, _) = MakeShipAndOrbitingPlanet();
            var moveDB = new WarpMovingDB(ship, planet, offset, default);
            moveDB.HasStarted = true;
            Assert.Greater(moveDB.PredictedExitTime, ship.StarSysDateTime,
                "precondition: intercept is in the future");

            WarpMoveProcessor.ScheduleArrival(ship, moveDB);

            bool found = false;
            foreach (var qi in ship.Manager.ManagerSubpulses.InstanceProcessorsQueue)
            {
                if (qi.Time == moveDB.PredictedExitTime
                    && qi.Item.Item1 == nameof(WarpMoveProcessor)
                    && qi.Item.Item2 == ship)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "warp start must pin an instance interrupt at PredictedExitTime");
        }

        [Test]
        public void InstanceProcessEntity_NoWarpBlob_DoesNotThrow()
        {
            var ship = AddBody(new Vector3(1e11, 0, 0));
            var proc = (WarpMoveProcessor)_game.ProcessorManager.GetInstanceProcessor(nameof(WarpMoveProcessor));
            Assert.DoesNotThrow(() => proc.ProcessEntity(ship, ship.StarSysDateTime));
        }

        #endregion

        #region helpers

        private static void AssertFinite(double value, string name)
        {
            Assert.IsFalse(double.IsNaN(value), name + " is NaN");
            Assert.IsFalse(double.IsInfinity(value), name + " is Inf");
            Assert.AreNotEqual(value, double.MaxValue,  name + " is MaxValue");
            Assert.AreNotEqual(value, double.MinValue,  name + " is MinValue");
        }

        private static void AssertFiniteVec(Vector3 v, string name)
        {
            AssertFinite(v.X, name + ".X");
            AssertFinite(v.Y, name + ".Y");
            AssertFinite(v.Z, name + ".Z");
        }
        private static void AssertFiniteVec(Vector2 v, string name)
        {
            AssertFinite(v.X, name + ".X");
            AssertFinite(v.Y, name + ".Y");
        }

        private static void AssertKeplerWellFormed(KeplerElements ke, string label)
        {
            Assert.Multiple(() =>
            {
                AssertFinite(ke.SemiMajorAxis, label + " SMA");
                AssertFinite(ke.Eccentricity, label + " e");
                AssertFinite(ke.Inclination, label + " i");
                AssertFinite(ke.LoAN, label + " LoAN");
                AssertFinite(ke.AoP, label + " AoP");
                AssertFinite(ke.MeanAnomalyAtEpoch, label + " M0");
                AssertFinite(ke.MeanMotion, label + " n");
                AssertFinite(ke.StandardGravParameter, label + " µ");
                Assert.Greater(ke.StandardGravParameter, 0, label + " µ must be > 0");
                Assert.GreaterOrEqual(ke.Eccentricity, 0, label + " e must be >= 0");
            });
        }

        /// <summary>
        /// Finite Kepler on the OrbitDB. Hyperbolic (e ≥ 1) is allowed: Period is MaxValue
        /// because there is no closed orbit, not because the elements are garbage.
        /// </summary>
        private static void AssertOrbitWellFormed(OrbitDB orbit, string label)
        {
            Assert.Multiple(() =>
            {
                Assert.IsFalse(orbit.IsStationary, label + " should not be the empty stationary orbit");
                AssertFinite(orbit.SemiMajorAxis, label + " SMA");
                AssertFinite(orbit.Eccentricity, label + " e");
                AssertFinite(orbit.Inclination, label + " i");
                AssertFinite(orbit.LongitudeOfAscendingNode, label + " LoAN");
                AssertFinite(orbit.ArgumentOfPeriapsis, label + " AoP");
                AssertFinite(orbit.MeanAnomalyAtEpoch, label + " M0");
                AssertFinite(orbit.MeanMotion, label + " n");
                AssertFinite(orbit.GravitationalParameter_m3S2, label + " µ");
                Assert.Greater(orbit.GravitationalParameter_m3S2, 0, label + " µ must be > 0");
                Assert.GreaterOrEqual(orbit.Eccentricity, 0, label + " e must be >= 0");
                if (orbit.Eccentricity < 1)
                {
                    Assert.AreNotEqual(TimeSpan.Zero, orbit.OrbitalPeriod, label + " elliptical period is zero");
                    Assert.AreNotEqual(TimeSpan.MaxValue, orbit.OrbitalPeriod, label + " elliptical period is MaxValue");
                }
                AssertFiniteVec((Vector3)orbit._position,  label + " _position");
            });
        }

        private static Entity AddBody(Vector3 absolutePos)
        {
            var ent = Entity.Create();
            _starSys.AddEntity(ent, new BaseDataBlob[]
            {
                new PositionDB { AbsolutePosition = absolutePos },
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 10),
                new NameDB()
            });
            return ent;
        }

        private static Entity AddShip(Entity parent, double mass, Vector3 absolutePos)
        {
            var ent = Entity.Create();
            var pos = new PositionDB { AbsolutePosition = absolutePos };
            var warp = new WarpAbilityDB();
            warp.MaxSpeed = 1e7;
            _starSys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(mass, 20),
                warp,
                new NameDB()
            });
            pos.SetParent(parent);
            return ent;
        }

        private static Entity AddOrbitingBody(Entity parent, double mass, double radius_m, double smaAu, DateTime epoch)
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
                new NameDB()
            });
            OrbitProcessor.ProcessEntity(ent, epoch);
            return ent;
        }

        private static Entity AddMoon(Entity parent, double mass, double radius_m, double sma_m, DateTime epoch)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            // FromAsteroidFormat takes sma in AU
            double smaAu = Distance.MToAU(sma_m);
            var orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, mass, smaAu, 0, 0, 0, 0, 0, epoch);
            var ent = Entity.Create();
            var pos = new PositionDB();
            _starSys.AddEntity(ent, new BaseDataBlob[]
            {
                pos,
                MassVolumeDB.NewFromMassAndRadius_m(mass, radius_m),
                orbit,
                new NameDB()
            });
            OrbitProcessor.ProcessEntity(ent, epoch);
            return ent;
        }

        private static (Entity ship, Entity planet, Vector3 offset, DateTime start) MakeShipAndOrbitingPlanet()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var start = new DateTime(2000, 1, 1);
            var planet = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, start);
            var planetAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(planet, start);
            var ship = AddShip(sol, 1e6, planetAbs + new Vector3(5e9, 0, 0));
            var offset = new Vector3(200_000, 0, 0);
            return (ship, planet, offset, start);
        }

        /// <summary>
        /// Live-like: ship has an OrbitDB around Earth so MoveType is Orbit.
        /// SetDataBlob(WarpMovingDB) then strips that orbit.
        /// </summary>
        private static (Entity ship, Entity planet, Vector3 offset, DateTime start) MakeShipInEarthOrbit()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var start = new DateTime(2000, 1, 1);
            var planet = AddOrbitingBody(sol, 5.972e24, 6_371_000, smaAu: 1.0, start);
            var planetAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(planet, start);
            var ship = AddShip(planet, 1e6, planetAbs + new Vector3(6_571_000, 0, 0));
            ship.SetDataBlob(OrbitDB.FromPosition(planet, ship, start));
            OrbitProcessor.ProcessEntity(ship, start);
            var offset = new Vector3(200_000, 0, 0);
            return (ship, planet, offset, start);
        }

        private static void AttachWarpEnergy(Entity ship)
        {
            var warp = ship.GetDataBlob<WarpAbilityDB>();
            warp.EnergyType = "electricity";
            warp.BubbleCreationCost = 0;
            var energy = new EnergyGenAbilityDB(ship.StarSysDateTime)
            {
                EnergyType = new TestEnergyType()
            };
            energy.EnergyStored["electricity"] = 1e12;
            energy.EnergyStoreMax["electricity"] = 1e12;
            ship.SetDataBlob(energy);
        }

        private static void AttachThrust(Entity ship, double deltaVBudget = 20_000)
        {
            var thrust = new NewtonThrustAbilityDB("test-fuel")
            {
                ThrustInNewtons = 1e9,
                ExhaustVelocity = 10_000,
                FuelBurnRate = 1
            };
            double wet = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            double dry = wet / Math.Exp(deltaVBudget / thrust.ExhaustVelocity);
            thrust.SetFuel(wet - dry, wet);
            ship.SetDataBlob(thrust);
        }

        private static Entity AddPhobosWarpShip(Entity parent, Vector3 absolutePos)
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
                new ActionQueueDB(),
                new NameDB("phobos-run")
            });
            pos.SetParent(parent);
            return ent;
        }

        private static Vector3 PhobosWarpOffset(Entity ship, Entity phobos)
        {
            var vel = MoveMath.GetRelativeState(ship).Velocity;
            double r = OrbitMath.LowOrbitRadius(phobos);
            if (vel.Length() < 1)
                return new Vector3(r, 0, 0);
            return Vector3.Normalise(new Vector3(-vel.Y, vel.X, 0)) * r;
        }

        /// <summary>
        /// In-game time: TimePulse.TimeStep, which ProcessSystem's the focused system
        /// then advances GameGlobalDateTime. Tests that only ProcessSystem never
        /// move the global clock, so HandleOrder (which uses GameGlobalDateTime)
        /// would execute follow-on actions at the warp-start instant.
        /// </summary>
        private void AdvanceTo(DateTime when)
        {
            if (when <= _game.TimePulse.GameGlobalDateTime)
            {
                if (when > _starSys.StarSysDateTime)
                    _starSys.ManagerSubpulses.ProcessSystem(when);
                return;
            }

            _game.TimePulse.TimeStep(when);
        }

        private static string DescribeMove(Entity ship, Goal goal)
        {
            string orbit = ship.TryGetDataBlob<OrbitDB>(out var o)
                ? $"e={o.Eccentricity:G4} sma={o.SemiMajorAxis:G4} parent={o.Parent?.Id}"
                : "no-orbit";
            string blobs = (ship.HasDataBlob<WarpMovingDB>() ? " WarpMovingDB" : "")
                           + (ship.HasDataBlob<NewtonSimpleMoveDB>() ? " NewtonSimpleMoveDB" : "");
            string queue = "";
            if (ship.TryGetDataBlob<ActionQueueDB>(out var q))
            {
                queue = string.Join(", ", q.ActionList.Select(a =>
                    $"{a.GetType().Name}:{a.Status}{(a.IsRunning ? "/run" : "")}"));
            }

            return $"t={ship.StarSysDateTime:u} goal={goal.Status} '{goal.Message}' {orbit}{blobs} queue=[{queue}]";
        }

        private static Entity MakeMoveToShip(Entity parent, Vector3 absolutePos, Entity faction)
        {
            var ship = AddPhobosWarpShip(parent, absolutePos);
            ship.FactionOwnerID = faction.Id;
            ship.SetDataBlob(new ShipInfoDB());
            AttachThrust(ship);

            var data = faction.GetDataBlob<FactionInfoDB>().Data;
            data.Unlock("methalox");
            var fuel = data.CargoGoods.GetAny("methalox");
            Assert.IsNotNull(fuel, "basemod should have methalox");
            ship.GetDataBlob<NewtonThrustAbilityDB>().FuelType = fuel.UniqueID;
            var storage = new CargoStorageDB(fuel.CargoTypeID, 1e12);
            storage.AddCargoByUnit(fuel, 1_000_000);
            ship.SetDataBlob(storage);
            return ship;
        }

        private static void ApplyEarthStartOrbit(
            EarthStartOrbit kind, Entity ship, Entity earth, Entity mars, Entity dest,
            DateTime epoch, Vector3 interceptOffset)
        {
            var earthMass = earth.GetDataBlob<MassVolumeDB>().MassDry;
            var shipMass = ship.GetDataBlob<MassVolumeDB>().MassDry;
            var rEarth = ship.GetDataBlob<PositionDB>().RelativePosition.Length();

            switch (kind)
            {
                case EarthStartOrbit.CircularPrograde:
                    ship.SetDataBlob(OrbitDB.FromPosition(earth, ship, epoch));
                    break;
                case EarthStartOrbit.CircularRetrograde:
                    ship.SetDataBlob(OrbitDB.FromAsteroidFormat_r(
                        earth, earthMass, shipMass, rEarth, 0, Math.PI, 0, 0, 0, epoch));
                    break;
                case EarthStartOrbit.EccentricPrograde:
                    // Periapsis at current radius, e = 0.4.
                    ship.SetDataBlob(OrbitDB.FromAsteroidFormat_r(
                        earth, earthMass, shipMass, rEarth / 0.6, 0.4, 0, 0, 0, 0, epoch));
                    break;
                case EarthStartOrbit.LeftoverCircularAtMars:
                case EarthStartOrbit.LeftoverCircularAtMarsRetrograde:
                {
                    var intercept = WarpMath.GetInterceptPosition(ship, dest, epoch, interceptOffset);
                    var marsAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(mars, intercept.etiDateTime);
                    var rMars = intercept.position - marsAbs;
                    var mu = GeneralMath.StandardGravitationalParameter(
                        shipMass + mars.GetDataBlob<MassVolumeDB>().MassTotal);
                    var speed = Math.Sqrt(mu / rMars.Length());
                    var perp = Vector3.Normalise(new Vector3(-rMars.Y, rMars.X, 0));
                    if (kind == EarthStartOrbit.LeftoverCircularAtMarsRetrograde)
                        perp = -perp;
                    ship.SetDataBlob(OrbitDB.FromVelocity(earth, ship, perp * speed, epoch));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            OrbitProcessor.ProcessEntity(ship, epoch);
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

        #endregion
    }
}
