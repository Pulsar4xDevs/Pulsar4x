using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class OrbitTesting2
    {
        Game game;
        StarSystem starSys;
        Entity parentBody;
        MassVolumeDB parentMassDB;
        List<OrbitDB> allTestOrbitData = new List<OrbitDB>();
        OrbitDB testOrbitDB;

        double sgp { get => testOrbitDB.GravitationalParameterAU; }
        double o_a { get => testOrbitDB.SemiMajorAxis; }
        double o_e { get => testOrbitDB.Eccentricity; }
        double o_i { get => Angle.ToRadians(testOrbitDB.Inclination); }
        double o_Ω { get => Angle.ToRadians(testOrbitDB.LongitudeOfAscendingNode); }
        double o_M0 { get => Angle.ToRadians(testOrbitDB.MeanAnomalyAtEpoch); }
        double o_n { get => Angle.ToRadians(testOrbitDB.MeanMotion); }
        double o_ω { get => Angle.ToRadians(testOrbitDB.ArgumentOfPeriapsis); }
        double o_lop { get => o_Ω + o_ω; }
        DateTime o_epoch { get => testOrbitDB.Epoch; }

        double o_M;// = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); 
        double o_E;// = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
        double o_ν;// = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);


        //[OneTimeSetUp]
        public OrbitTesting2()
        {
            game = new Game();
            starSys = new StarSystem(game, "Sol", -1);
            parentBody = TestingUtilities.BasicSol(starSys);
            parentMassDB = parentBody.GetDataBlob<MassVolumeDB>();



            //first test something easy:
            double test_a = 1; //AU
            double test_e = 0;
            double test_i = 0;      //°
            double test_loan = 0;   //°
            double test_aop = 0;    //°
            double test_M0 = 0;     //°
            double test_bodyMass = 1000;
            OrbitDB test_db = OrbitDB.FromAsteroidFormat(parentBody, parentMassDB.Mass, test_bodyMass, test_a, test_e, test_i, test_loan, test_aop, test_M0, new System.DateTime());
            allTestOrbitData.Add(test_db);
            testOrbitDB = test_db;


            double halleysSemiMajAxis = 17.834; //AU
            double halleysEccentricity = 0.96714;
            double halleysInclination = 0; //180; //162.26° note retrograde orbit.
            double halleysLoAN = 0; //58.42; //°
            double halleysAoP = 111.33;//°
            double halleysMeanAnomaly = 38.38;//°
            double halleysBodyMass = 2.2e14;
            OrbitDB halleysOrbitDB = OrbitDB.FromAsteroidFormat(parentBody, parentMassDB.Mass, halleysBodyMass, halleysSemiMajAxis, halleysEccentricity, halleysInclination, halleysLoAN, halleysAoP, halleysMeanAnomaly, new System.DateTime(1994, 2, 17));
            allTestOrbitData.Add(halleysOrbitDB);


            halleysInclination = 180; //° test 2d retrograde orbit.
            halleysLoAN = 0; //58.42; //°
            halleysOrbitDB = OrbitDB.FromAsteroidFormat(parentBody, parentMassDB.Mass, halleysBodyMass, halleysSemiMajAxis, halleysEccentricity, halleysInclination, halleysLoAN, halleysAoP, halleysMeanAnomaly, new System.DateTime(1994, 2, 17));
            allTestOrbitData.Add(halleysOrbitDB);

            //hard mode test, 3d redtrograde orbit.
            halleysInclination = 162.26; //° note retrograde orbit.
            halleysLoAN = 58.42; //°
            halleysOrbitDB = OrbitDB.FromAsteroidFormat(parentBody, parentMassDB.Mass, halleysBodyMass, halleysSemiMajAxis, halleysEccentricity, halleysInclination, halleysLoAN, halleysAoP, halleysMeanAnomaly, new System.DateTime(1994, 2, 17));
            allTestOrbitData.Add(halleysOrbitDB);
        }

        [SetUp]
        public void TestSetup()
        {
            //testOrbitDB = allTestOrbitData[testOrbitIndex];

        }


        [Test]
        public void TestEccentricAnomalyCalcs()
        {
            double periodInSeconds = testOrbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;

                o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                o_E = OrbitProcessor.GetEccentricAnomaly(testOrbitDB, o_M);
                o_ν = OrbitProcessor.GetTrueAnomaly(testOrbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(testOrbitDB, segmentDatetime);
                var vel = OrbitMath.PreciseOrbitalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_lop);

                double linierEccentricity = o_e * o_a;

                var E1 = OrbitMath.GetEccentricAnomalyNewtonsMethod(o_e, o_M); //newtons method.
                var E2 = OrbitMath.GetEccentricAnomalyNewtonsMethod2(o_e, o_M); //newtons method. 
                var E3 = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(o_ν, o_e);
                var E4 = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, o_a, linierEccentricity, o_ω);
                //var E5 = OrbitMath.GetEccentricAnomalyFromStateVectors2(sgp, o_a, pos, vel);
                Assert.Multiple(() =>
                {
                    Assert.AreEqual(o_E, E1, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E1));// these two should be calculatd the same way.  
                    Assert.AreEqual(o_E, E2, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E2));
                    Assert.AreEqual(o_E, E3, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E3));
                    Assert.AreEqual(o_E, E4, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E4));
                    //Assert.AreEqual(o_E, E5, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E5));
                });
            }
        }

        [Test]
        public void TestMeanAnomalyCalcs()
        {
            double periodInSeconds = testOrbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;

            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                o_E = OrbitProcessor.GetEccentricAnomaly(testOrbitDB, o_M);
                o_ν = OrbitProcessor.GetTrueAnomaly(testOrbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(testOrbitDB, segmentDatetime);
                var vel = OrbitMath.PreciseOrbitalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_lop);

                var M1 = OrbitMath.GetMeanAnomaly(o_e, o_E);

                Assert.AreEqual(o_M, M1, 1.0E-7, "MeanAnomaly M expected: " + Angle.ToDegrees(o_M) + " was: " + Angle.ToDegrees(M1));
            }
        }

        [Test]
        public void TestAngleOfPeriapsCalcs()
        {
            double periodInSeconds = testOrbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                o_E = OrbitProcessor.GetEccentricAnomaly(testOrbitDB, o_M);
                o_ν = OrbitProcessor.GetTrueAnomaly(testOrbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(testOrbitDB, segmentDatetime);
                var vel = OrbitMath.PreciseOrbitalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_lop);

                Vector4 angularVelocity = Vector4.Cross(pos, vel);
                Vector4 nodeVector = Vector4.Cross(new Vector4(0, 0, 1, 0), angularVelocity);
                Vector4 eccentVector = OrbitMath.EccentricityVector(sgp, pos, vel);


                //var ω1 = OrbitMath.ArgumentOfPeriapsis(nodeVector, eccentVector, position, velocity);
                //var ω2 = OrbitMath.ArgumentOfPeriapsis(nodeVector, eccentVector, position, velocity, o_Ω);
                var ω3 = OrbitMath.ArgumentOfPeriapsis2(pos, o_i, o_Ω, o_ν);

                Assert.Multiple(() =>
                {
                //Assert.AreEqual(o_ω, ω1, 1.0E-7, "AoP ω expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω1));
                //Assert.AreEqual(ω1, ω2, 1.0E-7, "AoP ω expected: " + Angle.ToDegrees(ω1) + " was: " + Angle.ToDegrees(ω2));
                Assert.AreEqual(o_ω, ω3, 1.0E-7, "AoP ω expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω3));
                });

            }
        }

        [Test]
        public void TrueAnomalyCalcs()
        {
        
            double periodInSeconds = testOrbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                o_E = OrbitProcessor.GetEccentricAnomaly(testOrbitDB, o_M);
                o_ν = OrbitProcessor.GetTrueAnomaly(testOrbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(testOrbitDB, segmentDatetime);
                var vel = OrbitMath.PreciseOrbitalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_lop);

                double aop = OrbitMath.ArgumentOfPeriapsis2(pos, i, o_Ω, o_ν);
                double ea = o_e * o_a;
                double eccentricAnomaly = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, o_a, ea, aop);

                var ν1 = OrbitMath.TrueAnomaly(sgp, pos, vel);

                var ν2 = OrbitMath.TrueAnomaly(o_E, pos, vel);
                var ν3 = OrbitMath.TrueAnomalyFromEccentricAnomaly(o_e, eccentricAnomaly);
                var ν4 = OrbitMath.TrueAnomalyFromEccentricAnomaly2(o_e, eccentricAnomaly);
                //var ν5 = OrbitMath.TrueAnomaly2(ev, pos, vel);
                //var ν6 = OrbitMath.TrueAnomaly(pos, aop);
                var d1 = Angle.ToDegrees(ν1);
                var d2 = Angle.ToDegrees(ν2);
                var d3 = Angle.ToDegrees(ν3);
                var d4 = Angle.ToDegrees(ν4);
                //var d5 = Angle.ToDegrees(ν5);
                //var d6 = Angle.ToDegrees(ν6);

                Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ν1), 1.0E-7, "True Anomaly ν expected: " + Angle.ToDegrees(o_ν) + " was: " + Angle.ToDegrees(ν1));
                Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ν2), 1.0E-7, "True Anomaly ν expected: " + Angle.ToDegrees(o_ν) + " was: " + Angle.ToDegrees(ν2));
                Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ν3), 1.0E-7, "True Anomaly ν expected: " + Angle.ToDegrees(o_ν) + " was: " + Angle.ToDegrees(ν3));
                Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ν4), 1.0E-7, "True Anomaly ν expected: " + Angle.ToDegrees(o_ν) + " was: " + Angle.ToDegrees(ν4));
                //Assert.AreEqual(0, Angle.DifferenceBetweenRadians(directAngle, aop - ν5), angleΔ);
                //Assert.AreEqual(0, Angle.DifferenceBetweenRadians(directAngle, aop - ν6), angleΔ);


            }
        }

        [Test]
        public void TestOrbitalVelocityCalcs()
        {
            double periodInSeconds = testOrbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                o_E = OrbitProcessor.GetEccentricAnomaly(testOrbitDB, o_M);
                o_ν = OrbitProcessor.GetTrueAnomaly(testOrbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(testOrbitDB, segmentDatetime);
                var vel = OrbitMath.PreciseOrbitalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_lop);

                Vector4 angularVelocity = Vector4.Cross(pos, vel);
                double r = pos.Length();
                double speedau = OrbitMath.PreciseOrbitalSpeed(sgp, r, o_a);
                (double speed, double heading) polarVelocity = OrbitMath.PreciseOrbitalVelocityPolarCoordinate(sgp, pos, o_a, o_e, o_ν, o_lop);
                //Tuple<double, double> polarVelocity2 = OrbitMath.PreciseOrbitalVelocityPolarCoordinate2(sgp, pos, o_a, o_e, o_ν, o_lop);
                double heading = OrbitMath.HeadingFromPeriaps(pos, o_e, o_a, o_ν);
                heading += o_lop;
                //Assert.IsTrue(angularVelocity.Z > 0); //TODO:this will break if we test an anti clockwise orbit.
                Assert.IsTrue(speedau > 0); //I'm assuming that speed will be <0 if retrograde orbit. 
                Assert.AreEqual(vel.Length(), speedau, 1.0E-7);
                Assert.AreEqual(vel.Length(), polarVelocity.Item1, 1.0E-7);

                double hackHeading = OrbitMath.HackVelocityHeading(testOrbitDB, segmentDatetime);
                double hackheadD = Angle.ToDegrees(hackHeading);
                double headingD = Angle.ToDegrees(heading);


                if (o_e == 0)//we can make this work with ellipses if we add the lop to the position. 
                {
                    if (pos.X > 0 && pos.Y > 0)//top right quadrant
                    {
                        //Assert.IsTrue(polarVelocity.Item2 > Math.PI * 0.5 && polarVelocity.Item2 < Math.PI);
                        //Assert.IsTrue(hackHeading > Math.PI * 0.5 && hackHeading < Math.PI);
                        Assert.IsTrue(heading >= Math.PI * 0.5 && heading <= Math.PI);
                    }
                    if (pos.X < 0 && pos.Y > 0)//top left quadrant
                    {
                        //Assert.IsTrue(polarVelocity.Item2 > Math.PI && polarVelocity.Item2 < Math.PI * 1.5);
                        //Assert.IsTrue(hackHeading > Math.PI && hackHeading < Math.PI * 1.5);
                        Assert.IsTrue(heading >= Math.PI && heading <= Math.PI * 1.5);
                    }
                    if (pos.X < 0 && pos.Y < 0)//bottom left quadrant
                    {
                        //Assert.IsTrue(polarVelocity.Item2 > Math.PI * 1.5 && polarVelocity.Item2 < Math.PI * 2); 
                        //Assert.IsTrue(hackHeading > Math.PI * 1.5 && hackHeading < Math.PI * 2);
                        Assert.IsTrue(heading >= Math.PI * 1.5 && heading <= Math.PI * 2);
                    }
                    if (pos.X > 0 && pos.Y < 0)//bottom right quadrant
                    {
                        //Assert.IsTrue(polarVelocity.Item2 > 0 && polarVelocity.Item2 < Math.PI * 0.5);
                        //Assert.IsTrue(hackHeading > 0 && hackHeading < Math.PI * 0.5);
                        Assert.IsTrue(heading >= 0 && heading <= Math.PI * 0.5);
                    }
                }

            }
        }


        [Test]
        public void TestingKeplerConversions()
        {
            double periodInSeconds = OrbitMath.GetOrbitalPeriodInSeconds(sgp, o_a);
            Assert.AreEqual(periodInSeconds, testOrbitDB.OrbitalPeriod.TotalSeconds, 0.1);

            //lets break the orbit up and check the rest of the paremeters at different points of the orbit:
            double segmentTime = periodInSeconds / 16;

            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;

                o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                o_E = OrbitProcessor.GetEccentricAnomaly(testOrbitDB, o_M);
                o_ν = OrbitProcessor.GetTrueAnomaly(testOrbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(testOrbitDB, segmentDatetime);
                var vel = OrbitMath.PreciseOrbitalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_Ω);

                var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, vel, segmentDatetime);

                var ke_epoch = ke.Epoch;
                double ke_a = ke.SemiMajorAxis;
                double ke_e = ke.Eccentricity;
                double ke_i = ke.Inclination;
                double ke_Ω = ke.LoAN;
                double ke_M0 = ke.MeanAnomalyAtEpoch;
                double ke_n = ke.MeanMotion;
                double ke_ω = ke.AoP;

                double ke_E = OrbitMath.GetEccentricAnomalyNewtonsMethod(ke.Eccentricity, ke_M0);
                double ke_ν = OrbitMath.TrueAnomalyFromEccentricAnomaly(ke_e, ke_E);


                Assert.Multiple(() =>
                {
                    //these should not change (other than floating point errors) between each itteration
                    Assert.AreEqual(o_a, ke_a, 0.001, "SemiMajorAxis a"); //should be more accurate than this, though if testing from a given set of ke to state, and back, the calculated could be more acurate...
                    Assert.AreEqual(o_e, ke_e, 0.00001, "Eccentricity e");
                    Assert.AreEqual(o_i, ke_i, 1.0E-7, "Inclination i expected: " + Angle.ToDegrees(o_i) + " was: " + Angle.ToDegrees(ke_i));
                    Assert.AreEqual(o_Ω, ke_Ω, 1.0E-7, "LoAN Ω expected: " + Angle.ToDegrees(o_Ω) + " was: " + Angle.ToDegrees(ke_Ω));
                    Assert.AreEqual(o_ω, ke_ω, 1.0E-7, "AoP ω expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ke_ω));
                    Assert.AreEqual(o_n, ke_n, 1.0E-7, "MeanMotion n expected: " + Angle.ToDegrees(o_n) + " was: " + Angle.ToDegrees(ke_n));

                    //these will change between itterations:

                    Assert.AreEqual(o_E, ke_E, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(ke_E));
                    Assert.AreEqual(o_ν, ke_ν, 1.0E-7, "True Anomaly ν expected: " + Angle.ToDegrees(o_ν) + " was: " + Angle.ToDegrees(ke_ν));
                    Assert.AreEqual(o_M, ke_M0, 1.0E-7, "MeanAnomaly M expected: " + Angle.ToDegrees(o_M) + " was: " + Angle.ToDegrees(ke_M0));

                });
            }
        }
    }
}
