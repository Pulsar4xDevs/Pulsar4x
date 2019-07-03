using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    public class OrbitTesting2
    {
        static Game game = new Game();
        static StarSystem starSys = new StarSystem(game, "Sol", -1);
        static Entity parentBody = TestingUtilities.BasicSol(starSys);
        static MassVolumeDB parentMassDB = parentBody.GetDataBlob<MassVolumeDB>();
        
    
        static List<(OrbitDB orbitDB, string TestName)> _allTestOrbitData = new List<(OrbitDB, string)>()
        {
            
            (
             OrbitDB.FromAsteroidFormat //circular orbit.
                 (
                 parentBody, 
                 parentMassDB.Mass, 
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
                 parentMassDB.Mass, 
                 2.2e14,          //halleysBodyMass
                 17.834,     //halleysSemiMajAxis 
                 0.96714,     //halleysEccentricity
                 0, 
                 0, //halleysLoAN
                 111.33, //halleysAoP
                 38.38,     //halleysMeanAnomaly at Epoch
                 new System.DateTime(1994, 2, 17)),
             "Elliptical Orbit"
             ),
            (
             OrbitDB.FromAsteroidFormat( //elliptical 2d retrograde orbit. 
                 parentBody, 
                 parentMassDB.Mass, 
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
                 parentMassDB.Mass, 
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
                 parentMassDB.Mass, 
                 2.2e14,            //halleysBodyMass
                 17.834,     //halleysSemiMajAxis , 
                 0.96714,     //halleysEccentricity
                 162.26,     //halleys3dInclination, note retrograde orbit (> 90degrees)
                 58.42, //halleysLoAN
                 111.33, //halleysAoP
                 38.38,     //halleysMeanAnomaly at Epoch
                 new System.DateTime(1994, 2, 17)),
             "Elliptical Retrograde 3d Orbit")
        };
        
        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestOrbitalVelocityCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            
            double sgp = orbitDB.GravitationalParameterAU; 
            double o_a = orbitDB.SemiMajorAxis; 
            double o_e = orbitDB.Eccentricity; 
            double o_i = Angle.ToRadians(orbitDB.Inclination); 
            double o_Ω = Angle.ToRadians(orbitDB.LongitudeOfAscendingNode); 
            double o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch); 
            double o_n = Angle.ToRadians(orbitDB.MeanMotion); 
            double o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis); 
            double o_lop = OrbitMath.LonditudeOfPeriapsis2d(o_Ω, o_ω, o_i);
        
            DateTime o_epoch = orbitDB.Epoch; 
            
            double periodInSeconds = orbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;


            double sgpInk3S2 = orbitDB.GravitationalParameter;
            
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(orbitDB, segmentDatetime);
                var pv = OrbitProcessor.InstantaneousOrbitalVelocityPolarCoordinate(orbitDB, segmentDatetime);
                
                
                var pos = OrbitProcessor.GetPosition_AU(orbitDB, segmentDatetime);
                
                var vel1 = (Vector3)OrbitMath.InstantaneousOrbitalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_ω);
                var plocVel = OrbitMath.ParentLocalVeclocityVector(sgp, pos, o_a, o_e, o_ν, o_ω, o_i, o_Ω);
                
                
                var pv1 = OrbitMath.InstantaneousOrbitalVelocityPolarCoordinate(sgp, pos, o_a, o_e, o_ν, o_ω);
                var ev2 = OrbitMath.EccentricityVector(sgp, pos, plocVel);

                var hackspeed = OrbitMath.Hackspeed(orbitDB, segmentDatetime);
                var hackVector = OrbitMath.HackVelocityVector(orbitDB, segmentDatetime);
                
                Assert.AreEqual(hackspeed, hackVector.Length(), 1.0e-10, "TestData: " + testData.TestName +"\n iteration: " + i);
                Assert.AreEqual(vel1.Length(), plocVel.Length(), 1.0e-10, "TestData: " + testData.TestName +"\n iteration: " + i);
                Assert.AreEqual(hackspeed, vel1.Length(), 1.0e-10, "TestData: " + testData.TestName +"\n iteration: " + i);
                
                
                Assert.AreEqual(hackVector.X, plocVel.X, 1.0e-10);
                Assert.AreEqual(hackVector.Y, plocVel.Y, 1.0e-10);
                Assert.AreEqual(hackVector.Z, plocVel.Z, 1.0e-10);
                Assert.AreEqual(pv.heading, pv1.heading, 1.0e-7);
                Assert.AreEqual(pv.speed, pv1.speed, 1.0e-7);
                Assert.AreEqual(vel.Length(), vel1.Length(), 1.0e-7);
                Assert.AreEqual(vel.Length(), pv.speed, 1.0e-7);
                
    
   
                var e3 = ev2.Length();
    
                Assert.AreEqual(o_e, e3, 1.0e-5, "TestData: " + testData.TestName +"\n iteration: " + i + "\n EccentricVector Magnitude should equal the Eccentricity");

            }
        }
        
        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestLoANCalc((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            
            double sgp = orbitDB.GravitationalParameterAU; 
            double o_a = orbitDB.SemiMajorAxis; 
            double o_e = orbitDB.Eccentricity; 
            double o_i = Angle.ToRadians(orbitDB.Inclination); 
            double o_Ω = Angle.ToRadians(orbitDB.LongitudeOfAscendingNode); 
            double o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch); 
            double o_n = Angle.ToRadians(orbitDB.MeanMotion); 
            double o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis); 
            double o_lop = OrbitMath.LonditudeOfPeriapsis2d(o_Ω, o_ω, o_i);
        
            DateTime o_epoch = orbitDB.Epoch; 

 
            double periodInSeconds = orbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(orbitDB, segmentDatetime);
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(orbitDB, segmentDatetime);


                var nodeVector = OrbitMath.CalculateNode(OrbitMath.CalculateAngularMomentum(pos, (Vector3)vel));
                double loAN = OrbitMath.CalculateLongitudeOfAscendingNode(nodeVector);
                
                Assert.AreEqual(o_Ω, loAN, 1.0e-10);

            }
        }
        
        
        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TrueAnomalyCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            
            double sgp = orbitDB.GravitationalParameterAU; 
            double o_a = orbitDB.SemiMajorAxis; 
            double o_e = orbitDB.Eccentricity; 
            double o_i = Angle.ToRadians(orbitDB.Inclination); 
            double o_Ω = Angle.ToRadians(orbitDB.LongitudeOfAscendingNode); 
            double o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch); 
            double o_n = Angle.ToRadians(orbitDB.MeanMotion); 
            double o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis); 
            double o_lop = OrbitMath.LonditudeOfPeriapsis2d(o_Ω, o_ω, o_i);
        
            DateTime o_epoch = orbitDB.Epoch; 

 
            double periodInSeconds = orbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(orbitDB, segmentDatetime);
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(orbitDB, segmentDatetime);

                double aop = OrbitMath.GetArgumentOfPeriapsis2(pos, i, o_Ω, o_ν);
                double ea = o_e * o_a;
                double eccentricAnomaly = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, o_a, ea, aop);

                double ν1 = OrbitMath.TrueAnomaly(sgp, pos, (Vector3)vel);
                Vector3 ev = OrbitMath.EccentricityVector(sgp, pos, (Vector3)vel);
                double ν2 = OrbitMath.TrueAnomaly(ev, pos, (Vector3)vel);
                double ν3 = OrbitMath.TrueAnomalyFromEccentricAnomaly(o_e, o_E);
                double ν4 = OrbitMath.TrueAnomalyFromEccentricAnomaly2(o_e, o_E);

                double d0 = Angle.ToDegrees(o_ν);
                double d1 = Angle.ToDegrees(ν1);
                double d2 = Angle.ToDegrees(ν2);
                double d3 = Angle.ToDegrees(ν3);
                double d4 = Angle.ToDegrees(ν4);

                if(o_e > 1.0e-7) // because this test will fail if we have a circular orbit. 
                    Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ν1), 1.0E-7, "True Anomaly ν expected: " + d0 + " was: " + d1);
                else
                    Assert.AreEqual(0, ev.Length());
                
                Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ν2), 1.0E-7, "True Anomaly ν expected: " + d0 + " was: " + d2);
                Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ν3), 1.0E-7, "True Anomaly ν expected: " + d0 + " was: " + d3);
                //Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_ν, ν4), 1.0E-7, "True Anomaly ν expected: " + d0 + " was: " + d4);


            }
        }

        
        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestEccentricAnomalyCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            
            double sgp = orbitDB.GravitationalParameterAU; 
            double o_a = orbitDB.SemiMajorAxis; 
            double o_e = orbitDB.Eccentricity; 
            double o_i = Angle.ToRadians(orbitDB.Inclination); 
            double o_Ω = Angle.ToRadians(orbitDB.LongitudeOfAscendingNode); 
            double o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch); 
            double o_n = Angle.ToRadians(orbitDB.MeanMotion); 
            double o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis); 
            double o_lop = OrbitMath.LonditudeOfPeriapsis2d(o_Ω, o_ω, o_i);
        
            DateTime o_epoch = orbitDB.Epoch; 
            
            double periodInSeconds = orbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;

                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(orbitDB, segmentDatetime);
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(orbitDB, segmentDatetime);

                double linierEccentricity = o_e * o_a;

                var E1 = OrbitMath.GetEccentricAnomalyNewtonsMethod(o_e, o_M); //newtons method.
                var E2 = OrbitMath.GetEccentricAnomalyNewtonsMethod2(o_e, o_M); //newtons method. 
                var E3 = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(o_ν, o_e);
                var E4 = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, o_a, linierEccentricity, o_ω);
                var E5 = OrbitMath.GetEccentricAnomalyFromStateVectors2(sgp, o_a, pos, (Vector3)vel);
                Assert.Multiple(() =>
                {
                    
                    Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_E, E1), "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E1));// these two should be calculatd the same way.  
                    Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_E, E2), 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E2));
                    Assert.AreEqual(0, Angle.DifferenceBetweenRadians(o_E, E3), 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E3));
                    //Assert.AreEqual(o_E, E4, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E4));
                    //Assert.AreEqual(o_E, E5, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E5));
                });
            }
        }

        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestMeanAnomalyCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            
            double sgp = orbitDB.GravitationalParameterAU; 
            double o_a = orbitDB.SemiMajorAxis; 
            double o_e = orbitDB.Eccentricity; 
            double o_i = Angle.ToRadians(orbitDB.Inclination); 
            double o_Ω = Angle.ToRadians(orbitDB.LongitudeOfAscendingNode); 
            double o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch); 
            double o_n = Angle.ToRadians(orbitDB.MeanMotion); 
            double o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis); 
            double o_lop = OrbitMath.LonditudeOfPeriapsis2d(o_Ω, o_ω, o_i);
        
            DateTime o_epoch = orbitDB.Epoch; 


            
            double periodInSeconds = orbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;

            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(orbitDB, segmentDatetime);
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(orbitDB, segmentDatetime);

                var M1 = OrbitMath.GetMeanAnomaly(o_e, o_E);

                Assert.AreEqual(o_M, M1, 1.0E-7, "MeanAnomaly M expected: " + Angle.ToDegrees(o_M) + " was: " + Angle.ToDegrees(M1));
            }
        }

        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestAngleOfPeriapsCalcs((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            
            double sgp = orbitDB.GravitationalParameterAU; 
            double o_a = orbitDB.SemiMajorAxis; 
            double o_e = orbitDB.Eccentricity; 
            double o_i = Angle.ToRadians(orbitDB.Inclination); 
            double o_Ω = Angle.ToRadians(orbitDB.LongitudeOfAscendingNode); 
            double o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch); 
            double o_n = Angle.ToRadians(orbitDB.MeanMotion); 
            double o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis); 
            double o_lop = OrbitMath.LonditudeOfPeriapsis2d(o_Ω, o_ω, o_i);
        
            DateTime o_epoch = orbitDB.Epoch; 

            double periodInSeconds = orbitDB.OrbitalPeriod.TotalSeconds;
            double segmentTime = periodInSeconds / 16;
            //lets break the orbit up and check the paremeters at different points of the orbit:
            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;
                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(orbitDB, segmentDatetime);
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector(orbitDB, segmentDatetime);

                Vector3 angularVelocity = Vector3.Cross(pos, (Vector3)vel);
                Vector3 nodeVector = Vector3.Cross(new Vector3(0, 0, 1), angularVelocity);
                Vector3 eccentVector = OrbitMath.EccentricityVector(sgp, pos, (Vector3)vel);

                
                var ω1 = OrbitMath.GetArgumentOfPeriapsis1(nodeVector, eccentVector, pos, (Vector3)vel);
                var ω2 = OrbitMath.GetArgumentOfPeriapsis2(pos, o_i, o_Ω, o_ν);
                var ω3 = OrbitMath.GetArgumentOfPeriapsis3(nodeVector, eccentVector, pos, (Vector3)vel, o_Ω);
                var ω4 = OrbitMath.GetArgumentOfPeriapsis4(o_i, eccentVector, nodeVector);

                Assert.Multiple(() =>
                {
                    Assert.AreEqual(o_ω, ω1, 1.0E-7, "i"+i+" AoP ω1 expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω1));
                    Assert.AreEqual(o_ω, ω2, 1.0E-7, "i"+i+" AoP ω2 expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω2));
                    Assert.AreEqual(o_ω, ω3, 1.0E-7, "i"+i+" AoP ω3 expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω3));
                    Assert.AreEqual(o_ω, ω4, 1.0E-7, "i"+i+" AoP ω4 expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω4));
                });

            }
        }



        [Test, TestCaseSource(nameof(_allTestOrbitData))]
        public void TestingKeplerConversions((OrbitDB orbitDB, string TestName) testData)
        {
            var orbitDB = testData.orbitDB;
            
            double sgp = orbitDB.GravitationalParameterAU; 
            double o_a = orbitDB.SemiMajorAxis; 
            double o_e = orbitDB.Eccentricity; 
            double o_i = Angle.ToRadians(orbitDB.Inclination); 
            double o_Ω = Angle.ToRadians(orbitDB.LongitudeOfAscendingNode); 
            double o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch); 
            double o_n = Angle.ToRadians(orbitDB.MeanMotion); 
            double o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis); 
            double o_lop = OrbitMath.LonditudeOfPeriapsis2d(o_Ω, o_ω, o_i);
        
            DateTime o_epoch = orbitDB.Epoch; 

            double periodInSeconds = OrbitMath.GetOrbitalPeriodInSeconds(sgp, o_a);
            Assert.AreEqual(periodInSeconds, orbitDB.OrbitalPeriod.TotalSeconds, 0.1);

            //lets break the orbit up and check the rest of the paremeters at different points of the orbit:
            double segmentTime = periodInSeconds / 16;

            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;

                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);

                var pos = OrbitProcessor.GetPosition_AU(orbitDB, segmentDatetime);
                var vel = (Vector3)OrbitMath.InstantaneousOrbitalVelocityVector(sgp, pos, o_a, o_e, o_ν, o_ω);

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
