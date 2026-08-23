using System;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

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
        private static StarSystem _starSys = InitializeStarSystem();

        private static Game InitializeGame()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            return new Game(new NewGameSettings(), modDataStore);
        }

        private static StarSystem InitializeStarSystem()
        {
            var starSystem = new StarSystem();
            starSystem.Initialize(_game);
            _game.Systems.Add(starSystem);
            return starSystem;
        }

        [SetUp]
        public void SetUp()
        {
            _starSys.Initialize(_game, "Sol", -1);
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

        #endregion

        #region SOI parent switch

        [Test]
        public void SmallMoonSoi_SmallerThanOffset_MeansOrbitTheParent()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var epoch = new DateTime(2000, 1, 1);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            // Phobos-ish: sma ~9376 km, mass ~1.06e16 kg, radius ~11 km
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);

            var soi = OrbitMath.GetSOIRadius(phobos.GetDataBlob<OrbitDB>());
            var offset = new Vector3(13_000, 0, 0); // just above the surface

            Assert.Less(soi, offset.Length(),
                "Phobos-class body: SOI is smaller than a surface-offset stop, so drop-in must parent to Mars");
        }

        [Test]
        public void WarpDropIn_Phobos_OrbitAndKeplerAreWellFormed()
        {
            var sol = TestingUtilities.BasicSol(_starSys);
            var epoch = new DateTime(2000, 1, 1);
            var mars = AddOrbitingBody(sol, 0.64174e24, 3_396_200, smaAu: 1.524, epoch);
            var phobos = AddMoon(mars, 1.06e16, 11_266, sma_m: 9_376_000, epoch);

            var phobosSoi = OrbitMath.GetSOIRadius(phobos.GetDataBlob<OrbitDB>());
            var offset = new Vector3(13_000, 0, 0);
            Assert.Less(phobosSoi, offset.Length(), "precondition: Phobos cannot keep this stop");

            // Ship starts on a circular Sol orbit so SavedNewtonionVector is a real leftover
            // heliocentric velocity — same as a game ship that warps from the inner system.
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

            Assert.Multiple(() =>
            {
                Assert.AreSame(mars, orbitalParent, "too-small SOI must parent the ship to Mars, not Phobos");
                Assert.AreSame(mars, newOrbit.Parent, "OrbitDB.Parent must be Mars");
                Assert.AreSame(mars, ship.GetDataBlob<PositionDB>().Parent, "PositionDB.Parent must be Mars");

                AssertFiniteVec(pos2, "PositionDB.RelativePosition");
                AssertFiniteVec(posFromOrbit, "OrbitMath.GetPosition at epoch");
                Assert.Greater(pos2.Length(), marsRadius,
                    "drop-in r is inside Mars — renderer will lose the ship. " +
                    "Usually means r was Phobos-relative (the offset) instead of Mars-relative.");
                Assert.AreEqual(phobosR, pos2.Length(), phobosR * 0.25,
                    "Mars-relative r should be about Phobos's distance from Mars, not |offset| and not 1 AU");

                Assert.Less(ke.Eccentricity, 1.0,
                    "leftover SavedNewtonionVector is still in the departure frame, so the Mars orbit is hyperbolic. " +
                    "A hyperbolic / MaxValue-period OrbitDB is what typically makes the orbit widget disappear.");
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

        #region helpers

        private static void AssertFinite(double value, string name)
        {
            Assert.IsFalse(double.IsNaN(value), name + " is NaN");
            Assert.IsFalse(double.IsInfinity(value), name + " is Inf");
        }

        private static void AssertFiniteVec(Vector3 v, string name)
        {
            AssertFinite(v.X, name + ".X");
            AssertFinite(v.Y, name + ".Y");
            AssertFinite(v.Z, name + ".Z");
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
                Assert.AreNotEqual(TimeSpan.Zero, orbit.OrbitalPeriod, label + " period is zero");
                // Hyperbolic drop-ins set Period to MaxValue after NaN; widgets often choke on that.
                Assert.AreNotEqual(TimeSpan.MaxValue, orbit.OrbitalPeriod,
                    label + " period is MaxValue (usually e >= 1). Orbit widgets typically cannot draw this.");
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

        #endregion
    }
}
