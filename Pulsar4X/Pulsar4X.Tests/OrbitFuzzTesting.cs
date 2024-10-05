using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Modding;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests
{

    // Test: Time --> EllipticMeanAnomaly --> EccentricAnomaly --> TrueAnomaly --> *(position, velocity)* 
    // Test: Time <-- EllipticMeanAnomaly <-- EccentricAnomaly <-- TrueAnomaly <-----------| 

    // Test: Time --> HyperbolicMeanAnomaly --> HyperbolicAnomaly --> TrueAnomaly --> *(position, velocity)* 
    // Test: Time <-- HyperbolicMeanAnomaly <-- HyperbolicAnomaly <-- TrueAnomaly <-----------| 
    public class OrbitFuzzTesting
    {
        private static Game _game = InitializeGame();

        private static Game InitializeGame()
        {
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var game = new Game(new NewGameSettings(), modDataStore);

            return game;
        }

        private static StarSystem _starSys = InitializeStarSystem();

        private static StarSystem InitializeStarSystem()
        {
            var starSystem = new StarSystem();
            starSystem.Initialize(_game);
            _game.Systems.Add(starSystem);
            return starSystem;
        }

        static Entity parentBody = TestingUtilities.BasicSol(_starSys);
        static MassVolumeDB parentMassDB = parentBody.GetDataBlob<MassVolumeDB>();

        static List<(OrbitDB orbitDB, string TestName)> _allTestOrbitData = new List<(OrbitDB, string)>()
        {
            (
             OrbitDB.FromAsteroidFormat //circular orbit.
                 (
                 parentBody,
                 parentMassDB.MassDry,
                 1000,
                 1,
                 0,
                 0,
                 0,
                 0,
                 0,
                 new System.DateTime()
                 ),
             "Circular Orbit"
            ),
            (
             OrbitDB.FromAsteroidFormat( //elliptical orbit
                 parentBody,
                 parentMassDB.MassDry,
                 2.2e14,          //halleysBodyMass
                 17.834,     //halleysSemiMajAxis 
                 0.96714,     //halleysEccentricity
                 0,
                 0, //halleysLoAN
                 0, //halleysAoP
                 38.38,     //halleysMeanAnomaly at Epoch
                 new System.DateTime(1994, 2, 17)),
             "Elliptical 2d 0 LoAN 0 aop Orbit"
             ),
            (
                OrbitDB.FromAsteroidFormat( //elliptical orbit
                    parentBody,
                    parentMassDB.MassDry,
                    2.2e14,          //halleysBodyMass
                    17.834,     //halleysSemiMajAxis 
                    0.96714,     //halleysEccentricity
                    0,
                    0, //halleysLoAN
                    45.0, //halleysAoP
                    38.38,     //halleysMeanAnomaly at Epoch
                    new System.DateTime(1994, 2, 17)),
                "Elliptical 2d 0 LoAN, 45.0 aop Orbit"
            ),
            (
                OrbitDB.FromAsteroidFormat( //elliptical orbit
                    parentBody,
                    parentMassDB.MassDry,
                    2.2e14,          //halleysBodyMass
                    17.834,     //halleysSemiMajAxis 
                    0.96714,     //halleysEccentricity
                    0,
                    0, //halleysLoAN
                    111.33, //halleysAoP
                    38.38,     //halleysMeanAnomaly at Epoch
                    new System.DateTime(1994, 2, 17)),
                "Elliptical 2d 0 LoAN, 111.33 aop Orbit"
            ),
            /* THIS IS an INVALID test, for 2d orbits, LoAN should be 0!
            (
                OrbitDB.FromAsteroidFormat( //elliptical orbit
                    parentBody, 
                    parentMassDB.MassDry, 
                    2.2e14,          //halleysBodyMass
                    17.834,     //halleysSemiMajAxis 
                    0.96714,     //halleysEccentricity
                    0, 
                    58.42, //halleysLoAN
                    111.33, //halleysAoP
                    38.38,     //halleysMeanAnomaly at Epoch
                    new System.DateTime(1994, 2, 17)),
                "Elliptical 2d 58.42 LoAN and 111.33 aop Orbit"
            ),*/
            (
             OrbitDB.FromAsteroidFormat( //elliptical 2d retrograde orbit. 
                 parentBody,
                 parentMassDB.MassDry,
                 2.2e14,             //halleysBodyMass
                 17.834,         //halleysSemiMajAxis , 
                 0.96714,         //halleysEccentricity
                 72.26,
                 0, //halleysLoAN
                 111.33,  //halleysAoP
                 38.38,     //halleysMeanAnomaly at Epoch
                 new System.DateTime(1994, 2, 17)),
             "Elliptical 2d retrograde Orbit"
            ),
            (
             OrbitDB.FromAsteroidFormat( //elliptical 3d orbit. 
                 parentBody,
                 parentMassDB.MassDry,
                 2.2e14,            //halleysBodyMass
                 17.834,     //halleysSemiMajAxis , 
                 0.96714,     //halleysEccentricity
                 72.26,     //halleys3dInclination, note retrograde orbit (> 90degrees)
                 58.42, //halleysLoAN
                 111.33, //halleysAoP
                 38.38,     //halleysMeanAnomaly at Epoch
                 new System.DateTime(1994, 2, 17)),
             "Elliptical 3d Orbit"
            ),
            (
             OrbitDB.FromAsteroidFormat( //elliptical retrograde 3d orbit. 
                 parentBody,
                 parentMassDB.MassDry,
                 2.2e14,            //halleysBodyMass
                 17.834,     //halleysSemiMajAxis , 
                 0.96714,     //halleysEccentricity
                 162.26,     //halleys3dInclination, note retrograde orbit (> 90degrees)
                 58.42, //halleysLoAN
                 111.33, //halleysAoP
                 38.38,     //halleysMeanAnomaly at Epoch
                 new System.DateTime(1994, 2, 17)),
             "Elliptical Retrograde 3d Orbit"),

            (
                OrbitDB.FromAsteroidFormat( //Hyperbolic orbit
                    parentBody,
                    parentMassDB.MassDry,
                    2.2e14,          //halleysBodyMass
                    -17.834,     //halleysSemiMajAxis 
                    1.3,     //Hyperbolic Eccentricity
                    0,
                    0, //halleysLoAN
                    0, //halleysAoP
                    38.38,     //halleysMeanAnomaly at Epoch
                    new System.DateTime(1994, 2, 17)),
                "Hyperbolic 2d 0 LoAN, 0 aop Trajectory"
            ),
            (
                OrbitDB.FromAsteroidFormat( //Hyperbolic orbit
                    parentBody,
                    parentMassDB.MassDry,
                    2.2e14,          //halleysBodyMass
                    -17.834,     //halleysSemiMajAxis 
                    1.3,     //Hyperbolic Eccentricity
                    0,
                    0, //halleysLoAN
                    -90, //halleysAoP
                    38.38,     //halleysMeanAnomaly at Epoch
                    new System.DateTime(1994, 2, 17)),
                "Hyperbolic 2d 0 LoAN, 90 aop Trajectory"
            ),            
            (
              OrbitDB.FromAsteroidFormat( //Hyperbolic orbit
                  parentBody,
                  parentMassDB.MassDry,
                  2.2e14,          //halleysBodyMass
                  -17.834,     //halleysSemiMajAxis 
                  1.3,     //Hyperbolic Eccentricity
                  180,
                  0, //halleysLoAN
                  0, //halleysAoP
                  38.38,     //halleysMeanAnomaly at Epoch
                  new System.DateTime(1994, 2, 17)),
              "Hyperbolic 2d 0 LoAN, 0 aop retrograde Trajectory"
          ),            
            (
                OrbitDB.FromAsteroidFormat( //Hyperbolic orbit
                    parentBody,
                    parentMassDB.MassDry,
                    2.2e14,          //halleysBodyMass
                    -17.834,     //halleysSemiMajAxis 
                    1.3,     //Hyperbolic Eccentricity
                    180,
                    0, //halleysLoAN
                    -90, //halleysAoP
                    38.38,     //halleysMeanAnomaly at Epoch
                    new System.DateTime(1994, 2, 17)),
                "Hyperbolic 2d 0 LoAN, 90 aop retrograde Trajectory"
            ), 

        };

        double epsilonLen, epsilonRads, epsilont, sgp, o_a, o_e, o_i, o_Ω, o_M0, o_n, o_ω, o_lop;
        double periodInSeconds, segmentTime;
        DateTime o_epoch;

        private void SetupElements(OrbitDB orbit)
        {
            _starSys.Initialize(_game, "Sol", -1);
            // One effect of switching from AU to m is
            // an increase of the absolute magnitude of errors
            // due to the increased value of the lengths
            epsilonLen = 1e-5;
            epsilonRads = 1e-7;
            epsilont = 1e-2;

            sgp = orbit.GravitationalParameter_m3S2;
            o_a = orbit.SemiMajorAxis;
            o_e = orbit.Eccentricity;
            o_i = orbit.Inclination;
            o_Ω = orbit.LongitudeOfAscendingNode;
            o_M0 = orbit.MeanAnomalyAtEpoch;
            o_n = orbit.MeanMotion;
            o_ω = orbit.ArgumentOfPeriapsis;
            o_lop = o_Ω + o_ω;

            o_epoch = orbit.Epoch;

            if (o_e < 1)
            {
                periodInSeconds = orbit.OrbitalPeriod.TotalSeconds;
                segmentTime = periodInSeconds / 16;
            }
            else
            {
                double p = EllipseMath.SemiLatusRectum(o_a, o_e);
                double q = EllipseMath.Periapsis(o_e, o_a);
                var trueAnomalyAtPeriaps = EllipseMath.TrueAnomalyAtRadus(q, p, o_e);
                var ha = OrbitMath.GetHyperbolicAnomalyFromTrueAnomaly(o_e, trueAnomalyAtPeriaps);
                var hma = OrbitMath.GetHyperbolicMeanAnomaly(o_e, ha);
                var timeAtPeriaps = OrbitMath.TimeFromHyperbolicMeanAnomaly(sgp, o_a, hma);

                var ha2 = OrbitMath.GetHyperbolicAnomalyFromTrueAnomaly(o_e, Math.PI * 0.5);
                var hma2 = OrbitMath.GetHyperbolicMeanAnomaly(o_e, ha2);
                var timeAtP = OrbitMath.TimeFromHyperbolicMeanAnomaly(sgp, o_a, hma2);

                periodInSeconds = (timeAtP - timeAtPeriaps) * 2;
                segmentTime = periodInSeconds / 16;
            }
        }

        /// <summary>
        /// Tests: Time ⟶ EllipticMeanAnomaly  ⟶ EccentricAnomaly
        ///        Time ⟵ EllipticMeanAnomaly  ⟵        ↲
        /// Tests: Time ⟶ HyperblicMeanAnomaly ⟶ HyperbolicAnomaly
        ///        Time ⟵ HyperblicMeanAnomaly ⟵       ↲
        /// </summary>
        /// <param name="testData"></param>
        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestMeanAnomalyCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            SetupElements(orbitDB);

            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;

                double o_ν = OrbitMath.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitMath.GetPosition(orbitDB, segmentDatetime);
                var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbitDB, segmentDatetime);

                //calculate value, and inversion and compare. 
                double M1;
                if (o_e < 1)
                {
                    //calculate mean anomaly the easy way
                    double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly

                    //calculate it back the hard way. 
                    OrbitMath.TryGetEccentricAnomaly(o_e, o_M, out double o_E); //OrbitMath.GetEccentricAnomaly(orbitDB, o_M);
                    M1 = OrbitMath.GetEllipticMeanAnomaly(o_e, o_E);
                    double t1 = OrbitMath.TimeFromEllipticMeanAnomaly(o_M0, M1, o_n);

                    Assert.AreEqual(o_M, M1, epsilonRads, "MeanAnomaly M expected: " + Angle.ToDegrees(o_M) + " was: " + Angle.ToDegrees(M1));
                    Assert.AreEqual(timeSinceEpoch.TotalSeconds, t1, epsilont, "TimeFromMeanAnomaly t1 expected " + timeSinceEpoch.TotalSeconds + " was: " + t1);
                }
                else
                {
                    //calculate meanAnomaly the easy way
                    var o_Mh = OrbitMath.GetHyperbolicMeanAnomalyFromTime(o_n, timeSinceEpoch.TotalSeconds);

                    //calculate back to HyperbolicAnomaly H
                    OrbitMath.TryGetHyperbolicAnomaly(o_e, o_Mh, out var H);

                    M1 = OrbitMath.GetHyperbolicMeanAnomaly(o_e, H);
                    double t1 = OrbitMath.TimeFromHyperbolicMeanAnomaly(sgp, o_a, M1);
                    Assert.AreEqual(o_Mh, M1, epsilonRads, "MeanAnomaly Mh expected: " + Angle.ToDegrees(o_Mh) + " was: " + Angle.ToDegrees(M1));
                    Assert.AreEqual(timeSinceEpoch.TotalSeconds, t1, epsilont, "TimeFromMeanAnomaly t1 expected " + timeSinceEpoch.TotalSeconds + " was: " + t1);
                }


            }
        }


        /// <summary>
        /// Tests: EccentricAnomaly ⟶ TrueAnomaly
        ///        EccentricAnomaly ⟵      ↲
        /// Tests: HyperbolicAnomaly ⟶ TrueAnomaly
        ///        HyperbolicAnomaly ⟵     ↲
        /// </summary>
        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestEccHypAnomalyCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            SetupElements(orbitDB);

            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_Mh = OrbitMath.GetHyperbolicMeanAnomalyFromTime(o_n, timeSinceEpoch.TotalSeconds);
                double o_loAN = orbitDB.LongitudeOfAscendingNode;
                double o_aoP = orbitDB.ArgumentOfPeriapsis;
                double o_i = orbitDB.Inclination;
                double o_a = orbitDB.SemiMajorAxis;
                double o_e = orbitDB.Eccentricity;
                var o_p = EllipseMath.SemiLatusRectum(o_a, o_e);
                double o_ν = OrbitMath.GetTrueAnomaly(orbitDB, segmentDatetime);// orbitDB.GetTrueAnomaly(segmentDatetime);

                var posFromDB = OrbitMath.GetPosition(orbitDB, segmentDatetime);
                var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbitDB, segmentDatetime);

                if (o_e < 1)
                {
                    OrbitMath.TryGetEccentricAnomaly(o_e, o_M, out double o_E);
                    var truAnom = OrbitMath.TrueAnomalyFromEccentricAnomaly(o_e, o_E);
                    var r = EllipseMath.RadiusAtTrueAnomaly(truAnom, o_p, o_e);
                    var pos = OrbitMath.GetRelativePosition(o_loAN, o_aoP, o_i, truAnom, r);

                    var E1 = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(truAnom, o_e);

                    string message = "TrueAnomaly Expected: " + Angle.ToDegrees(o_ν).ToString() + "°\nBut was: " + Angle.ToDegrees(truAnom).ToString() + "° ";
                    Assert.AreEqual(o_ν, truAnom, epsilonRads, message);
                    message = "EccentricAnomaly Expected: " + Angle.ToDegrees(o_E).ToString() + "°\nBut was: " + Angle.ToDegrees(E1).ToString() + "° ";
                    Assert.AreEqual(o_E, E1, epsilonRads, message);
                }
                else
                {
                    OrbitMath.TryGetHyperbolicAnomaly(o_e, o_Mh, out double o_F);
                    var truAnom = OrbitMath.TrueAnomalyFromHyperbolicAnomaly(o_e, o_F);
                    var r = EllipseMath.RadiusAtTrueAnomaly(truAnom, o_p, o_e);
                    var pos = OrbitMath.GetRelativePosition(o_loAN, o_aoP, o_i, truAnom, r);
                    //var vel = OrbitMath.vel
                    //var truFromPos = OrbitMath.TrueAnomaly()
                    var F1 = OrbitMath.GetHyperbolicAnomalyFromTrueAnomaly(o_e, truAnom);

                    truAnom = Angle.NormaliseRadians(truAnom);


                    string message = i + " TrueAnomaly Expected: " + Angle.ToDegrees(o_ν).ToString() + "°\n" +
                                     "But was: " + Angle.ToDegrees(truAnom).ToString() + "° ";
                    Assert.AreEqual(o_ν, truAnom, epsilonRads, message);

                    message = i + " HyperbolicAnomaly Expected: " + Angle.ToDegrees(o_F).ToString() + "°\n" +
                              "But was: " + Angle.ToDegrees(F1).ToString() + "°\n" +
                              "For trueAnom of " + Angle.ToDegrees(truAnom).ToString() + "° ";
                    Assert.AreEqual(o_F, F1, epsilonRads, message);

                }
            }
        }


        /// <summary>
        /// Tests: TrueAnomaly  ⟶ *(position, velocity)*
        ///        TrueAnomaly  ⟵        ↲
        /// Tests: TrueAnomaly ⟶ *(position, velocity)*
        ///        TrueAnomaly ⟵       ↲
        /// </summary>
        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestTrueAnomalyCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            SetupElements(orbitDB);

            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_Mh = OrbitMath.GetHyperbolicMeanAnomalyFromTime(o_n, timeSinceEpoch.TotalSeconds);
                double o_loAN = orbitDB.LongitudeOfAscendingNode;
                double o_aoP = orbitDB.ArgumentOfPeriapsis;
                double o_i = orbitDB.Inclination;
                double o_a = orbitDB.SemiMajorAxis;
                double o_e = orbitDB.Eccentricity;
                var o_p = EllipseMath.SemiLatusRectum(o_a, o_e);
                double o_ν = OrbitMath.GetTrueAnomaly(orbitDB, segmentDatetime);// orbitDB.GetTrueAnomaly(segmentDatetime);

                var o_pos = OrbitMath.GetPosition(orbitDB, segmentDatetime);
                var o_vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbitDB, segmentDatetime);



                var pos1 = OrbitMath.GetPosition(o_a, o_e, o_loAN, o_aoP, o_i, o_ν);
                var r = EllipseMath.RadiusAtTrueAnomaly(o_ν, o_p, o_e);
                var pos2 = OrbitMath.GetRelativePosition(o_loAN, o_aoP, o_i, o_ν, r);
                var vel1 = OrbitalMath.ParentLocalVeclocityVector(sgp, pos1, o_a, o_e, o_ν, o_aoP, o_i, o_loAN);
                var state = OrbitMath.GetStateVectors(orbitDB, segmentDatetime);
                Vector3 eccentVector = OrbitMath.EccentricityVector(sgp, o_pos, o_vel);
                var truAnom = OrbitMath.TrueAnomaly(eccentVector, o_pos, o_vel);

                //testing differences between different position functions
                Assert.Multiple(() =>
                {
                    string message = i + " RelativePosition Length compare: \n" +
                                     "Expected: " + o_pos.Length() + "\n" +
                                     "But was:  " + pos1.Length();
                    Assert.AreEqual(o_pos.Length(), pos1.Length(), epsilonLen, message);
                    message = i + "RelativePosition X compare: \n" +
                             "Expected: " + o_pos.X + "\n" +
                             "But was:  " + pos1.X;
                    Assert.AreEqual(o_pos.X, pos1.X, epsilonLen, message);
                    message = i + "RelativePosition Y compare: \n" +
                               "Expected: " + o_pos.Y + "\n" +
                               "But was:  " + pos1.Y;
                    Assert.AreEqual(o_pos.Y, pos1.Y, epsilonLen, message);
                    message = i + "RelativePosition Z compare: \n" +
                              "Expected: " + o_pos.Z + "\n" +
                              "But was:  " + pos1.Z;
                    Assert.AreEqual(o_pos.Z, pos1.Z, epsilonLen, message);
                });
                Assert.Multiple(() =>
                {
                    string message = i + " RelativePosition Length compare: \n" +
                                     "Expected: " + o_pos.Length() + "\n" +
                                     "But was:  " + pos2.Length();
                    Assert.AreEqual(o_pos.Length(), pos2.Length(), epsilonLen, message);
                    message = i + "RelativePosition X compare: \n" +
                              "Expected: " + o_pos.X + "\n" +
                              "But was:  " + pos2.X;
                    Assert.AreEqual(o_pos.X, pos2.X, epsilonLen, message);
                    message = i + "RelativePosition Y compare: \n" +
                               "Expected: " + o_pos.Y + "\n" +
                               "But was:  " + pos2.Y;
                    Assert.AreEqual(o_pos.Y, pos2.Y, epsilonLen, message);
                    message = i + "RelativePosition Z compare: \n" +
                              "Expected: " + o_pos.Z + "\n" +
                              "But was:  " + pos2.Z;
                    Assert.AreEqual(o_pos.Z, pos2.Z, epsilonLen, message);
                });

                string message = i + "TrueAnomaly Expected: " + Angle.ToDegrees(o_ν).ToString() + "°\n" +
                                 "But was: " + Angle.ToDegrees(truAnom).ToString() + "° ";
                Assert.AreEqual(o_ν, truAnom, epsilonRads, message);



            }
        }


        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestOrbitalVelocityCalcs((OrbitDB orbitDB, string TestName) testData)
        {

            var orbitDB = testData.orbitDB;
            SetupElements(orbitDB);

            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitMath.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitMath.GetTrueAnomaly(orbitDB, segmentDatetime);
                var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbitDB, segmentDatetime);
                //var pv = OrbitMath.InstantaneousOrbitalVelocityPolarCoordinate(orbitDB, segmentDatetime);
                var pos = OrbitMath.GetPosition(orbitDB, segmentDatetime);

                var vel1 = (Vector3)OrbitMath.ObjectLocalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_ω);
                var plocVel = OrbitMath.ParentLocalVeclocityVector(sgp, pos, o_a, o_e, o_ν, o_ω, o_i, o_Ω);

                var pv1 = OrbitMath.ObjectLocalVelocityPolar(sgp, pos, o_a, o_e, o_ν, o_ω);
                var ev2 = OrbitMath.EccentricityVector(sgp, pos, plocVel);

                //var hackspeed = OrbitMath.Hackspeed(orbitDB, segmentDatetime);
                //var hackVector = OrbitMath.HackVelocityVector(orbitDB, segmentDatetime);


                Assert.AreEqual(vel1.Length(), plocVel.Length(), epsilonLen, "TestData: " + testData.TestName + "\n iteration: " + i);


                //Assert.AreEqual(pv.heading, pv1.heading, epsilonLen);
                //Assert.AreEqual(pv.speed, pv1.speed, epsilonLen);
                Assert.AreEqual(vel.Length(), vel1.Length(), epsilonLen);


                var e3 = ev2.Length();

                Assert.AreEqual(o_e, e3, epsilonLen, "TestData: " + testData.TestName + "\n iteration: " + i + "\n EccentricVector Magnitude should equal the Eccentricity");

            }
        }


        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestLoANCalc((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            SetupElements(orbitDB);

            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitMath.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitMath.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitMath.GetPosition(orbitDB, segmentDatetime);
                var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbitDB, segmentDatetime);

                var nodeVector = OrbitMath.CalculateNode(OrbitMath.CalculateAngularMomentum(pos, (Vector3)vel));
                double loAN = OrbitMath.CalculateLongitudeOfAscendingNode(nodeVector);
                string message = "Expected: " + Angle.ToDegrees(o_Ω).ToString() + "°\nBut was: " + Angle.ToDegrees(loAN).ToString() + "° ";
                AssertExtensions.AreAngleEqual(o_Ω, loAN, 1.0e-10, message);

            }
        }



        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestAngleOfPeriapsCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            SetupElements(orbitDB);

            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitMath.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitMath.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitMath.GetPosition(orbitDB, segmentDatetime);
                var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbitDB, segmentDatetime);

                Vector3 angularVelocity = Vector3.Cross(pos, (Vector3)vel);
                Vector3 nodeVector = Vector3.Cross(new Vector3(0, 0, 1), angularVelocity);
                Vector3 eccentVector = OrbitMath.EccentricityVector(sgp, pos, vel);

                var ω2 = OrbitMath.GetArgumentOfPeriapsis(pos, o_i, o_Ω, o_ν);

                //These two functions below need fixing, they don't give the correct values in testing.
                var ω1 = OrbitMath.GetArgumentOfPeriapsis1(nodeVector, eccentVector, pos, vel);
                var ω3 = OrbitMath.GetArgumentOfPeriapsis3(o_i, eccentVector, nodeVector);

                Assert.Multiple(() =>
                {
                    //Assert.AreEqual(o_ω, ω1, 1.0E-7, "i"+i+" AoP ω1 expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω1));
                    Assert.AreEqual(o_ω, ω2, 1.0E-7, "i" + i + " AoP ω2 expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω2));
                    //Assert.AreEqual(o_ω, ω3, 1.0E-7, "i"+i+" AoP ω4 expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω3));
                });

            }
        }



        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestingStaticKeplerConversions((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            SetupElements(orbitDB);

            //lets break the orbit up and check the rest of the paremeters at different points of the orbit:
            //Assert.AreEqual(periodInSeconds, orbitDB.OrbitalPeriod.TotalSeconds, 0.1);

            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;

                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitMath.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitMath.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitMath.GetPosition(orbitDB, segmentDatetime);
                var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbitDB, segmentDatetime);
                var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, vel, segmentDatetime);

                var ke_epoch = ke.Epoch;
                double ke_a = ke.SemiMajorAxis;
                double ke_e = ke.Eccentricity;
                double ke_i = ke.Inclination;
                double ke_Ω = ke.LoAN;
                double ke_M0 = ke.MeanAnomalyAtEpoch;
                double ke_n = ke.MeanMotion;
                double ke_ω = ke.AoP;
                double ke_lop = ke.LoAN + ke.AoP;

                Vector3 eccentricityVector = OrbitMath.EccentricityVector(sgp, pos, vel);
                //double ke_ν = OrbitMath.TrueAnomaly(eccentricityVector, pos, vel);
                //double ke_E = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(ke_ν, ke_e);

                //var E3 = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(o_ν, o_e);
                //Assert.AreEqual(0, Angle.DifferenceBetweenRadians(ke_E, E3), 1.0E-10);

                Assert.Multiple(() =>
                {
                    //these should not change (other than floating point errors) between each itteration
                    Assert.AreEqual(o_a, ke_a, 0.015, "SemiMajorAxis a"); //should be more accurate than this, though if testing from a given set of ke to state, and back, the calculated could be more acurate...
                    Assert.AreEqual(o_e, ke_e, epsilonLen, "Eccentricity e");
                    AssertExtensions.AreAngleEqual(o_i, ke_i, epsilonRads, "Inclination i expected: " + Angle.ToDegrees(o_i) + "° was: " + Angle.ToDegrees(ke_i) + "°");
                    AssertExtensions.AreAngleEqual(o_Ω, ke_Ω, epsilonRads, "LoAN Ω expected: " + Angle.ToDegrees(o_Ω) + "° was: " + Angle.ToDegrees(ke_Ω) + "°");
                    AssertExtensions.AreAngleEqual(o_ω, ke_ω, epsilonRads, "AoP ω expected: " + Angle.ToDegrees(o_ω) + "° was: " + Angle.ToDegrees(ke_ω) + "°");
                    AssertExtensions.AreAngleEqual(o_lop, ke_lop, epsilonRads, "LoP expected: " + Angle.ToDegrees(o_lop) + "° was: " + Angle.ToDegrees(ke_lop) + "°");
                    Assert.AreEqual(o_n, ke_n, epsilonLen, "MeanMotion n expected: " + Angle.ToDegrees(o_n) + "° was: " + Angle.ToDegrees(ke_n) + "°");
                });
            }
        }

        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestingVariableKeplerConversions((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            SetupElements(orbitDB);
            if (orbitDB.Eccentricity < 1)
                Assert.AreEqual(periodInSeconds, orbitDB.OrbitalPeriod.TotalSeconds, epsilont);

            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;

                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitMath.GetEccentricAnomaly(orbitDB, o_M);
                double o_Mh = OrbitMath.GetHyperbolicMeanAnomalyFromTime(o_n, timeSinceEpoch.TotalSeconds);
                double o_H = OrbitMath.GetHyperbolicAnomaly(orbitDB, o_Mh);
                double o_ν = OrbitMath.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitMath.GetPosition(orbitDB, segmentDatetime);
                var vel = OrbitMath.InstantaneousOrbitalVelocityVector_m(orbitDB, segmentDatetime);
                var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, vel, segmentDatetime);

                var ke_epoch = ke.Epoch;
                double ke_a = ke.SemiMajorAxis;
                double ke_e = ke.Eccentricity;
                double ke_i = ke.Inclination;
                double ke_Ω = ke.LoAN;
                double ke_M0 = ke.MeanAnomalyAtEpoch;
                double ke_n = ke.MeanMotion;
                double ke_ω = ke.AoP;

                Vector3 eccentricityVector = OrbitMath.EccentricityVector(sgp, pos, vel);
                double ke_ν = OrbitMath.TrueAnomaly(eccentricityVector, pos, vel);

                double ke_E = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(ke_ν, ke_e);
                double ke_E2 = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(o_ν, o_e);
                double ke_H = OrbitMath.GetHyperbolicAnomalyFromTrueAnomaly(ke_e, ke_ν);

                Assert.AreEqual(o_ν, ke_ν, epsilonRads, "true anomaly");
                Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ke_ν), epsilonRads, "True Anomaly ν expected: " + Angle.ToDegrees(o_ν) + " was: " + Angle.ToDegrees(ke_ν));
                Assert.AreEqual(o_e, ke_e, epsilonLen, "eccentricty");



                if (o_e < 1)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.AreEqual(0, Angle.DifferenceBetweenRadians(ke_E, ke_E2), epsilonRads, "EccentricAnomaly");
                        Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_E, ke_E), epsilonRads, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(ke_E));
                        //we're testing ke_M0 here because epoch for ke is *now*.
                        Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_M, ke_M0), epsilonRads, "MeanAnomaly M expected: " + Angle.ToDegrees(o_M) + " was: " + Angle.ToDegrees(ke_M0));
                    });
                }
                else
                {
                    Assert.Multiple(() =>
                    {

                        Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_H, ke_H), epsilonRads, "HyperbolicAnomaly H expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(ke_E));
                        //we're testing ke_M0 here because epoch for ke is *now*.
                        //Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_Mh, ke_M0), epsilonRads, "i: "+i+", HyperbolicMeanAnomaly Mh expected: " + Angle.ToDegrees(o_M) + " was: " + Angle.ToDegrees(ke_M0));

                    });
                }
            }
        }

    }
}
