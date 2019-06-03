using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Tests kepler form velocity")]
    public class OrbitTests
    {

        public void TestOrbitEpoch()
        {
            Game game = new Game();
            EntityManager man = new EntityManager(game, false);

            double parentMass = 1.989e30;
            double objMass = 2.2e+15;
            Vector4 position = new Vector4() { X = 0.57 }; //Halley's Comet at periapse aprox
            Vector4 velocity = new Vector4() { Y = Distance.KmToAU(54) };

            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(man.ManagerGuid) { X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(man, parentblobs);
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;

            OrbitDB objOrbit = OrbitDB.FromVector(parentEntity, objMass, parentMass, sgp, position, velocity, new DateTime());
            Vector4 resultPos = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());
        }


        [Test]
        public void TestPreciseOrbitalSpeed()
        {
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgpKm = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 1000000000;
            var speedKm = OrbitMath.PreciseOrbitalSpeed(sgpKm, 405400, 384399);
            Assert.AreEqual(0.97, speedKm, 0.01);
        }

        [Test]
        public void TestEccentricAnomalyCalc()
        {
            double parentMass = 1.989e30;
            double objMass = 2.2e+15;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;
            Vector4 pos = new Vector4() { X = 0.25, Y = 0.25 };
            Vector4 vel = new Vector4() { Y = Distance.KmToAU(54) };
            Vector4 ev = OrbitMath.EccentricityVector(sgp, pos, vel);
            double e = ev.Length();
            double specificOrbitalEnergy = Math.Pow(vel.Length(), 2) * 0.5 - sgp / pos.Length();
            double a = -sgp / (2 * specificOrbitalEnergy);
            double ae = e * a;
            double aop = Math.Atan2(ev.Y, ev.X);
            var ν = OrbitMath.TrueAnomaly(sgp, pos, vel);

            var E1 = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, a, ae, aop);
            var meanAnomaly = E1 - e * Math.Sin(E1);
            var E2 = OrbitMath.GetEccentricAnomalyNewtonsMethod(e, meanAnomaly);
            var E3 = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(ν, e);

            Assert.AreEqual(E1, E2, 0.00000001);
            Assert.AreEqual(E1, E3, 0.00000001);
        }


        [Test]
        public void TestTrueAnomalyCalcs()
        {
            Vector4 pos;// = new Vector4() { X = 0.25, Y = 0.25 };
            Vector4 vel;// = new Vector4() { X = Distance.KmToAU(25), Y = Distance.KmToAU(25) };
                        //TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = 0.25, Y = 0 };
            vel = new Vector4() { X = Distance.KmToAU(0), Y = Distance.KmToAU(25) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = 0, Y = 0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(0) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = -0.25, Y = 0 };
            vel = new Vector4() { X = Distance.KmToAU(0), Y = Distance.KmToAU(-25) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = 0, Y = -0.25 };
            vel = new Vector4() { X = Distance.KmToAU(25), Y = Distance.KmToAU(0) };
            TrueAnomalyCalcs(pos, vel);

            pos = new Vector4() { X = 0.25, Y = 0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(25) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = 0.25, Y = -0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(25) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = -0.25, Y = 0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(25) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = -0.25, Y = -0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(25) };
            TrueAnomalyCalcs(pos, vel);

            pos = new Vector4() { X = 0.25, Y = 0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(-25) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = 0.25, Y = -0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(-25) };
            TrueAnomalyCalcs(pos, vel); 
            pos = new Vector4() { X = -0.25, Y = 0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(-25) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = -0.25, Y = -0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-25), Y = Distance.KmToAU(-25) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = -0.208994076275941, Y = 0.955838328099748 };
            vel = new Vector4() { X = -2.1678187689294E-07, Y = -7.93096769486992E-08 };
            TrueAnomalyCalcs(pos, vel);
        }

        private void TrueAnomalyCalcs(Vector4 pos, Vector4 vel)
        {
            double angleΔ = 0.0000001;
            double parentMass = 1.989e30;
            double objMass = 2.2e+15;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;

            Vector4 ev = OrbitMath.EccentricityVector(sgp, pos, vel);
            double e = ev.Length();
            double specificOrbitalEnergy = Math.Pow(vel.Length(), 2) * 0.5 - sgp / pos.Length();
            double a = -sgp / (2 * specificOrbitalEnergy);
            double ae = e * a;
            Vector4 angularVelocity = Vector4.Cross(pos, vel);
            Vector4 nodeVector = Vector4.Cross(new Vector4(0, 0, 1, 0), angularVelocity);
            double aop = OrbitMath.ArgumentOfPeriapsis(nodeVector, ev, pos, vel);

            double eccentricAnomaly = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, a, ae, aop);
            //double aopD = Angle.ToDegrees(aop);
            double directAngle = Math.Atan2(pos.Y, pos.X);

            var θ1 = OrbitMath.TrueAnomaly(sgp, pos, vel);
            var θ2 = OrbitMath.TrueAnomaly(ev, pos, vel);
            var θ3 = OrbitMath.TrueAnomalyFromEccentricAnomaly(e, eccentricAnomaly);
            var θ4 = OrbitMath.TrueAnomalyFromEccentricAnomaly2(e, eccentricAnomaly);
            //var θ5 = OrbitMath.TrueAnomaly2(ev, pos, vel);
            //var θ6 = OrbitMath.TrueAnomaly(pos, aop);
            var d1 = Angle.ToDegrees(θ1);
            var d2 = Angle.ToDegrees(θ2);
            var d3 = Angle.ToDegrees(θ3);
            var d4 = Angle.ToDegrees(θ4);
            //var d5 = Angle.ToDegrees(θ5);
            //var d6 = Angle.ToDegrees(θ6);

            Assert.AreEqual(0, Angle.DifferenceBetweenRadians(directAngle, aop - θ1), angleΔ);
            Assert.AreEqual(0, Angle.DifferenceBetweenRadians(directAngle, aop - θ2), angleΔ);
            Assert.AreEqual(0, Angle.DifferenceBetweenRadians(directAngle, aop - θ3), angleΔ);
            Assert.AreEqual(0, Angle.DifferenceBetweenRadians(directAngle, aop - θ4), angleΔ);
            //Assert.AreEqual(0, Angle.DifferenceBetweenRadians(directAngle, aop - θ5), angleΔ);
            //Assert.AreEqual(0, Angle.DifferenceBetweenRadians(directAngle, aop - θ6), angleΔ);

        }

        [Test]
        public void OrbitsFromVectorTests2()
        {
            Game game = new Game();
            StarSystem sol = new StarSystem(game, "Sol", -1);
            var sun = TestingUtilities.BasicSol(sol);
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            //first test something easy:
            double test_a = 1; //AU
            double test_e = 0;
            double test_i = 0;      //°
            double test_loan = 0;   //°
            double test_aop = 0;    //°
            double test_M0 = 0;     //°
            OrbitDB test_db = OrbitDB.FromAsteroidFormat(sun, sunMVDB.Mass, 1000, test_a, test_e, test_i, test_loan, test_aop, test_M0, new System.DateTime());

            TestingKeplerConversions(test_db);


            DateTime timeNow = new DateTime(1994, 2, 17);

            SystemBodyInfoDB halleysBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Comet, SupportsPopulations = false, Albedo = 0.04f }; //Albedo = 0.04f 
            MassVolumeDB halleysMVDB = MassVolumeDB.NewFromMassAndRadius(2.2e14, Distance.KmToAU(11));
            NameDB halleysNameDB = new NameDB("Halleys Comet");
            double halleysSemiMajAxis = 17.834; //AU
            double halleysEccentricity = 0.96714;
            double halleysInclination = 0; //180; //162.26° note retrograde orbit.
            double halleysLoAN = 0; //58.42; //°
            double halleysAoP = 111.33;//°
            double halleysMeanAnomaly = 38.38;//°
            OrbitDB halleysOrbitDB = OrbitDB.FromAsteroidFormat(sun, sunMVDB.Mass, halleysMVDB.Mass, halleysSemiMajAxis, halleysEccentricity, halleysInclination, halleysLoAN, halleysAoP, halleysMeanAnomaly, new System.DateTime(1994, 2, 17));
            PositionDB halleysPositionDB = new PositionDB(OrbitProcessor.GetPosition_AU(halleysOrbitDB, timeNow), sol.Guid, sun);
            Entity halleysComet = new Entity(sol, new List<BaseDataBlob> {halleysPositionDB, halleysBodyDB, halleysMVDB, halleysNameDB, halleysOrbitDB });

            TestingKeplerConversions(halleysOrbitDB);




        }

        void TestingKeplerConversions(OrbitDB orbitDB)
        {
            var sgp = orbitDB.GravitationalParameterAU;
            var o_epoch = orbitDB.Epoch;
            var o_a = orbitDB.SemiMajorAxis;
            var o_e = orbitDB.Eccentricity;
            var o_i = Angle.ToRadians(orbitDB.Inclination);
            var o_Ω = Angle.ToRadians( orbitDB.LongitudeOfAscendingNode);
            var o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch);
            var o_n = Angle.ToRadians(orbitDB.MeanMotion);
            var o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis);

            double periodInSeconds = OrbitMath.GetOrbitalPeriodInSeconds(sgp, o_a);            
            Assert.AreEqual(periodInSeconds, orbitDB.OrbitalPeriod.TotalSeconds, 0.1);

            //lets break the orbit up and check the rest of the paremeters at different points of the orbit:
            double segmentTime = periodInSeconds / 16;

            for (int i = 0; i < 16; i++)
            {
                TimeSpan timeSinceEpoch = TimeSpan.FromSeconds(segmentTime * i);
                DateTime segmentDatetime = o_epoch + timeSinceEpoch;

                var posi = OrbitProcessor.GetPosition_AU(orbitDB, segmentDatetime);
                var veli = OrbitMath.PreciseOrbitalVelocityVector(sgp, posi, o_a, o_e, o_Ω);

                var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, posi, veli, segmentDatetime);

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

                double o_M = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, timeSinceEpoch.TotalSeconds); //orbitProcessor uses this calc directly
                double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M);
                double o_ν = OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);

                TestMeanAnomalyCalcs(orbitDB, o_E, timeSinceEpoch.TotalSeconds);
                TestAngleOfPeriapsCalcs(orbitDB, posi, veli, o_ν);
                TestEccentricAnomalyCalcs(orbitDB, o_M, o_ν, posi, veli);


                Assert.Multiple(() =>
                {
                    //these should not change (other than floating point errors) between each itteration
                    Assert.AreEqual(o_a, ke_a, 0.001,   "SemiMajorAxis a"); //should be more accurate than this, though if testing from a given set of ke to state, and back, the calculated could be more acurate...
                    Assert.AreEqual(o_e, ke_e, 0.00001, "Eccentricity e");
                    Assert.AreEqual(o_i, ke_i, 1.0E-7,  "Inclination i expected: " + Angle.ToDegrees(o_i) + " was: " + Angle.ToDegrees(ke_i));
                    Assert.AreEqual(o_Ω, ke_Ω, 1.0E-7,  "LoAN Ω expected: " + Angle.ToDegrees(o_Ω) + " was: " + Angle.ToDegrees(ke_Ω));
                    Assert.AreEqual(o_ω, ke_ω, 1.0E-7,  "AoP ω expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ke_ω));
                    Assert.AreEqual(o_n, ke_n, 1.0E-7,  "MeanMotion n expected: " + Angle.ToDegrees(o_n) + " was: " + Angle.ToDegrees(ke_n));

                    //these will change between itterations:

                    Assert.AreEqual(o_E, ke_E, 1.0E-7,  "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(ke_E));
                    Assert.AreEqual(o_ν, ke_ν, 1.0E-7,  "True Anomaly ν expected: " + Angle.ToDegrees(o_ν) + " was: " + Angle.ToDegrees(ke_ν));
                    Assert.AreEqual(o_M, ke_M0, 1.0E-7, "MeanAnomaly M expected: " + Angle.ToDegrees(o_M) + " was: " + Angle.ToDegrees(ke_M0));

                });
            }
        }

        public void TestEccentricAnomalyCalcs(OrbitDB orbitDB, double meanAnomaly, double trueAnomaly, Vector4 position, Vector4 velocity)
        {
            var sgp = orbitDB.GravitationalParameterAU;
            var o_e = orbitDB.Eccentricity;
            var o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch);
            var o_n = Angle.ToRadians(orbitDB.MeanMotion);
            var o_a = orbitDB.SemiMajorAxis;
            var o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis);

            var pos = position;
            var vel = velocity;
            double o_M = meanAnomaly;//OrbitMath.CurrentMeanAnomaly(o_M0, o_n, secondsSinceEpoch); //
            double o_ν = trueAnomaly;//OrbitProcessor.GetTrueAnomaly(orbitDB, segmentDatetime);
            double linierEccentricity = o_e * o_a;
            double o_E = OrbitProcessor.GetEccentricAnomaly(orbitDB, o_M); //newtons method
            var E1 = OrbitMath.GetEccentricAnomalyNewtonsMethod(o_e, o_M); //newtons method.
            var E2 = OrbitMath.GetEccentricAnomalyNewtonsMethod2(o_e, o_M); //newtons method. 
            var E3 = OrbitMath.GetEccentricAnomalyFromTrueAnomaly(o_ν, o_e);
            var E4 = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, o_a, linierEccentricity, o_ω);
            var E5 = OrbitMath.GetEccentricAnomalyFromStateVectors2(sgp, o_a, pos, vel);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(o_E, E1, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E1));// these two should be calculatd the same way.  
                Assert.AreEqual(o_E, E2, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E2));
                Assert.AreEqual(o_E, E3, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E3));
                Assert.AreEqual(o_E, E4, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E4));
                Assert.AreEqual(o_E, E5, 1.0E-7, "EccentricAnomaly E expected: " + Angle.ToDegrees(o_E) + " was: " + Angle.ToDegrees(E5));
            });
            
        }

        public void TestMeanAnomalyCalcs(OrbitDB orbitDB, double eccentricAnomaly, double secondsFromEpoch)
        {
            var sgp = orbitDB.GravitationalParameterAU;
            var o_e = orbitDB.Eccentricity;
            var o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch);
            var o_n = Angle.ToRadians(orbitDB.MeanMotion);
            var o_a = orbitDB.SemiMajorAxis;
            var o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis);


            var M1 = OrbitMath.GetMeanAnomaly(o_e, eccentricAnomaly);
            var M2 = OrbitMath.GetMeanAnomalyFromTime(o_M0, o_n, secondsFromEpoch);

            Assert.AreEqual(M1, M2, 1.0E-7, "MeanAnomaly M expected: " + Angle.ToDegrees(M1) + " was: " + Angle.ToDegrees(M2));
        }


        public void TestAngleOfPeriapsCalcs(OrbitDB orbitDB, Vector4 position, Vector4 velocity, double trueAnomaly)
        {
            var sgp = orbitDB.GravitationalParameterAU;
            var o_e = orbitDB.Eccentricity;
            var o_M0 = Angle.ToRadians(orbitDB.MeanAnomalyAtEpoch);
            var o_n = Angle.ToRadians(orbitDB.MeanMotion);
            var o_a = orbitDB.SemiMajorAxis;
            var o_ω = Angle.ToRadians(orbitDB.ArgumentOfPeriapsis);
            var o_Ω = Angle.ToRadians(orbitDB.LongitudeOfAscendingNode);
            var o_i = Angle.ToRadians(orbitDB.Inclination);

            Vector4 angularVelocity = Vector4.Cross(position, velocity);
            Vector4 nodeVector = Vector4.Cross(new Vector4(0, 0, 1, 0), angularVelocity);
            Vector4 eccentVector = OrbitMath.EccentricityVector(sgp, position, velocity);
            double o_ν = trueAnomaly;
            
            var ω1 = OrbitMath.ArgumentOfPeriapsis(nodeVector, eccentVector, position, velocity);
            var ω2 = OrbitMath.ArgumentOfPeriapsis(nodeVector, eccentVector, position, velocity, o_Ω);
            var ω3 = OrbitMath.ArgumentOfPeriapsis2(position, o_i, o_Ω, o_ν);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(o_ω, ω1, 1.0E-7, "AoP ω expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω1));
                Assert.AreEqual(o_ω, ω2, 1.0E-7, "AoP ω expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω2));
                Assert.AreEqual(o_ω, ω3, 1.0E-7, "AoP ω expected: " + Angle.ToDegrees(o_ω) + " was: " + Angle.ToDegrees(ω3));
            });

        }

        [Test]
        public void TestAngles() 
        {
            var e = 0.5;
            var a = 100;
            var p = EllipseMath.SemiLatusRectum(a, e);
            double angleDelta = 0.00001;
            var i = 0;
            for (double angle = 0; angle < Math.PI; angle += 0.0174533)
            {
                var r = OrbitMath.RadiusAtAngle(angle, p, e);
                var theta = OrbitMath.AngleAtRadus(r, p, e);
                Assert.AreEqual(angle, theta, angleDelta,  "inc: " + i + " r: " + r);
                i++;
            }
        }

        [Test]
        public void TestKeplerElementsFromVectors()
        {
            Vector4 position = new Vector4() { X = Distance.KmToAU(405400) }; //moon at apoapsis
            Vector4 velocity = new Vector4() { Y = Distance.KmToAU(0.97) }; //approx velocity of moon at apoapsis
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;
            KeplerElements elements = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity, new DateTime());

            Vector4 postionKm = new Vector4() { X = 405400 };
            Vector4 velocityKm = new Vector4() { Y = 0.97 };
            double sgpKm = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 1000000000;

            KeplerElements elementsKm = OrbitMath.KeplerFromPositionAndVelocity(sgpKm, postionKm, velocityKm, new DateTime());

            //check that the function is unit agnostic.
            Assert.AreEqual(Distance.AuToKm(elements.SemiMajorAxis), elementsKm.SemiMajorAxis, 0.001);
            Assert.AreEqual(elements.Eccentricity, elementsKm.Eccentricity, 1.0e-9); //this is where inacuarcy from units stars creaping in, not much can do about that.
            Assert.AreEqual(Distance.AuToKm(elements.Apoapsis), elementsKm.Apoapsis, 0.001);
            Assert.AreEqual(Distance.AuToKm(elements.Periapsis), elementsKm.Periapsis, 0.001);


            var speedAU = OrbitMath.PreciseOrbitalSpeed(sgp, position.Length(), elements.SemiMajorAxis);
            var speedVectorAU = OrbitMath.PreciseOrbitalVelocityVector(sgp, position, elements.SemiMajorAxis, elements.Eccentricity, elements.LoAN + elements.AoP);
            Assert.AreEqual(speedAU, speedVectorAU.Length());

            Assert.AreEqual(elementsKm.Apoapsis + elementsKm.Periapsis, elementsKm.SemiMajorAxis * 2, 0.001);


            var speedKm = velocityKm.Length();
            var speedKm2 = OrbitMath.PreciseOrbitalSpeed(sgpKm, postionKm.Length(), elementsKm.SemiMajorAxis);
            Assert.AreEqual(speedKm, speedKm2, 0.001);


            Assert.GreaterOrEqual(elements.Apoapsis, elements.Periapsis);
            Assert.GreaterOrEqual(elementsKm.Apoapsis, elementsKm.Periapsis);

            //below was some experimentation with different ways of calculating things, and an attempt to use decimal for Eccentricity.
            //not sure it's worth the minor slowdown or complication, didn't seem to fix the problem I was seeing in anycase. 
            #region experimentation 

            var H = Vector4.Cross(postionKm, velocityKm).Length();
            var p = H * H / sgpKm;
            var sma = 1 / (2 / postionKm.Length() - velocityKm.Length() * velocityKm.Length() / sgpKm); //  semi-major axis


            decimal E;
            double Periapsis;
            double Apoapsis;

            if (sma < (double)decimal.MaxValue)
            {
                decimal smad = (decimal)sma;
                E = GMath.Sqrt(1 - (decimal)p / smad);

                decimal PlusMinus = smad * E;
                Periapsis = (double)(smad - PlusMinus);
                Apoapsis = (double)(smad + PlusMinus);
            }
            else
            {
                E = (decimal)Math.Sqrt(1 - p / sma);  // eccentricity

                double PlusMinus = sma * (double)E;
                Periapsis = sma - PlusMinus;
                Apoapsis = sma + PlusMinus;

            }
            Assert.AreEqual(Periapsis + Apoapsis, sma * 2, 0.0001);
            var peStr = Periapsis.ToString("R");
            var apStr = Apoapsis.ToString("R");
            //Assert.AreEqual(elementsKm.SemiMajorAxis, sma);
            var difference1 = (Periapsis + Apoapsis) - sma * 2;
            var difference2 = (elementsKm.Apoapsis + elementsKm.Periapsis) - elementsKm.SemiMajorAxis * 2;

            #endregion

            if (velocity.Z == 0)
                Assert.IsTrue(elements.Inclination == 0);


        }


        [Test]
        public void OrbitsFromVectorTests()
        {
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            Vector4 position = new Vector4() { X = Distance.KmToAU(405400) }; //moon at apoapsis
            Vector4 velocity = new Vector4() { Y = Distance.KmToAU(0.97) }; //approx velocity of moon at apoapsis
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            //test high eccentricity orbit
            parentMass = 1.989e30;
            objMass = 2.2e+15;
            position = new Vector4() { X = 0.57 }; //Halley's Comet at periapse aprox
            velocity = new Vector4() { Y = Distance.KmToAU(54) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);



            parentMass = 1.989e30;
            objMass = 2.2e+15;
            position = new Vector4() { X = 0.25, Y = 0.25 }; 
            velocity = new Vector4() { Y = Distance.KmToAU(54) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            parentMass = 1.989e30;
            objMass = 10000;
            position = new Vector4() { X = 0.25, Y = 0.25 };
            velocity = new Vector4() { X = Distance.KmToAU(0), Y = Distance.KmToAU(1) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            /*
            //this is something that is breaking ingame
            parentMass = 1.989e30;
            objMass = 10000;
            position = new Vector4() { X = -0.208994076275941, Y = 0.955838328099748 };
            velocity = new Vector4() { X = -2.1678187689294E-07, Y = -7.93096769486992E-08};
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);
            */



        }

        [Test]
        public void FailingOrbitsFromVectorTests()
        {
            double parentMass = 1.989e30;
            double objMass = 10000;
            Vector4 position = new Vector4() { X = 0.25, Y = 0.25 };
            Vector4 velocity = new Vector4() { X = Distance.KmToAU(0), Y = Distance.KmToAU(1) }; //passes
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector4() { X = Distance.KmToAU(0), Y = -Distance.KmToAU(2) }; //fails
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector4() { X = Distance.KmToAU(1), Y = Distance.KmToAU(0) }; //fails
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector4() { X = -Distance.KmToAU(1), Y = Distance.KmToAU(0) }; //fails
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

        }

        public void TestOrbitDBFromVectors(double parentMass, double objMass, Vector4 position, Vector4 velocity)
        {
            double angleΔ = 0.0000000001; 
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;
            KeplerElements ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity, new DateTime());

            Game game = new Game();
            EntityManager man = new EntityManager(game, false);

            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(man.ManagerGuid) { X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(man, parentblobs);


            OrbitDB objOrbit = OrbitDB.FromVector(parentEntity, objMass, parentMass, sgp, position, velocity, new DateTime());
            Vector4 resultPos = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());

            //check LoAN
            var objLoAN = Angle.ToRadians(objOrbit.LongitudeOfAscendingNode);
            var keLoAN = ke.LoAN;
            var loANDifference = objLoAN - keLoAN;
            Assert.AreEqual(keLoAN, objLoAN, angleΔ);

            //check AoP
            var objAoP = Angle.ToRadians(objOrbit.ArgumentOfPeriapsis);
            var keAoP = ke.AoP;
            var difference = objAoP - keAoP;
            Assert.AreEqual(keAoP, objAoP, angleΔ);


            //check MeanAnomalyAtEpoch
            var objM0 = Angle.ToRadians(objOrbit.MeanAnomalyAtEpoch);
            var keM0 = ke.MeanAnomalyAtEpoch;
            Assert.AreEqual(keM0, objM0, angleΔ);
            Assert.AreEqual(objM0, OrbitMath.GetMeanAnomalyFromTime(objM0, objOrbit.MeanMotion, 0), "meanAnomalyError");

            //checkEpoch
            var objEpoch = objOrbit.Epoch;
            var keEpoch = ke.Epoch;
            Assert.AreEqual(keEpoch, objEpoch);

            //check EccentricAnomaly:
            var objE = (OrbitProcessor.GetEccentricAnomaly(objOrbit, objOrbit.MeanAnomalyAtEpoch));
            var keE =   (OrbitMath.GetEccentricAnomalyFromStateVectors(position, ke.SemiMajorAxis, ke.LinierEccentricity, ke.AoP));
            if (objE != keE)
            {
                var dif = objE - keE;
                //Assert.AreEqual(keE, objE, angleΔ);
            }

            //check trueAnomaly 
            var orbTrueAnom = OrbitProcessor.GetTrueAnomaly(objOrbit, new DateTime());
            var orbtaDeg = Angle.ToDegrees(orbTrueAnom);
            var differenceInRadians = orbTrueAnom - ke.TrueAnomalyAtEpoch;
            var differenceInDegrees = Angle.ToDegrees(differenceInRadians);
            if (ke.TrueAnomalyAtEpoch != orbTrueAnom) 
            { 

                Vector4 eccentVector = OrbitMath.EccentricityVector(sgp, position, velocity);
                var tacalc1 = OrbitMath.TrueAnomaly(eccentVector, position, velocity);
                var tacalc2 = OrbitMath.TrueAnomaly(sgp, position, velocity);

                var diffa = differenceInDegrees;
                var diffb = Angle.ToDegrees(orbTrueAnom - tacalc1);
                var diffc = Angle.ToDegrees(orbTrueAnom - tacalc2);

                var ketaDeg = Angle.ToDegrees(tacalc1);
            }

            Assert.AreEqual(0, Angle.DifferenceBetweenRadians(ke.TrueAnomalyAtEpoch, orbTrueAnom), angleΔ,
                "more than " + angleΔ + " radians difference, at " + differenceInRadians + " \n " +
                "(more than " + Angle.ToDegrees(angleΔ) + " degrees difference at " + differenceInDegrees + ")" + " \n " +
                "ke Angle: " + ke.TrueAnomalyAtEpoch + " obj Angle: " + orbTrueAnom + " \n " +
                "ke Angle: " + Angle.ToDegrees(ke.TrueAnomalyAtEpoch) + " obj Angle: " + Angle.ToDegrees(orbTrueAnom));
                
            Assert.AreEqual(ke.Eccentricity, objOrbit.Eccentricity);
            Assert.AreEqual(ke.SemiMajorAxis, objOrbit.SemiMajorAxis);


            var lenke1 = ke.SemiMajorAxis * 2;
            var lenke2 = ke.Apoapsis + ke.Periapsis;
            Assert.AreEqual(lenke1, lenke2);
            var lendb1 = objOrbit.SemiMajorAxis * 2;
            var lendb2 = objOrbit.Apoapsis + objOrbit.Periapsis;
            Assert.AreEqual(lendb1, lendb2 );
            Assert.AreEqual(lenke1, lendb1);
            Assert.AreEqual(lenke2, lendb2);



            var ke_apkm = Distance.AuToKm(ke.Apoapsis);
            var db_apkm = Distance.AuToKm(objOrbit.Apoapsis);
            var differnce = ke_apkm - db_apkm;
            Assert.AreEqual(ke.Apoapsis, objOrbit.Apoapsis); 
            Assert.AreEqual(ke.Periapsis, objOrbit.Periapsis);

            Vector4 posKM = Distance.AuToKm(position);
            Vector4 resultKM = Distance.AuToKm(resultPos);

            double keslr = EllipseMath.SemiLatusRectum(ke.SemiMajorAxis, ke.Eccentricity);
            double keradius = OrbitMath.RadiusAtAngle(ke.TrueAnomalyAtEpoch, keslr, ke.Eccentricity);
            Vector4 kemathPos = OrbitMath.GetRalitivePosition(ke.LoAN, ke.AoP, ke.Inclination, ke.TrueAnomalyAtEpoch, keradius);
            Vector4 kemathPosKM = Distance.AuToKm(kemathPos);
            Assert.AreEqual(kemathPosKM.Length(), posKM.Length(), 0.01);

            Assert.AreEqual(posKM.Length(), resultKM.Length(), 0.01, "TA: " + orbtaDeg);
            Assert.AreEqual(posKM.X, resultKM.X, 0.01, "TA: " + orbtaDeg);
            Assert.AreEqual(posKM.Y, resultKM.Y, 0.01, "TA: " + orbtaDeg);
            Assert.AreEqual(posKM.Z, resultKM.Z, 0.01, "TA: " + orbtaDeg);

            if (velocity.Z == 0)
            {
                Assert.IsTrue(ke.Inclination == 0);
                Assert.IsTrue(objOrbit.Inclination == 0);
            }

            //var speedVectorAU = OrbitProcessor.PreciseOrbitalVector(sgp, position, ke.SemiMajorAxis);
            //var speedVectorAU2 = OrbitProcessor.PreciseOrbitalVector(objOrbit, new DateTime());
            //Assert.AreEqual(speedVectorAU, speedVectorAU2);

    }


        [Test]
        public void TestIntercept()
        {
            double myMass = 10000;
            double parentMass = 1.989e30; //solar mass.
            Game game = new Game();
            EntityManager mgr = new EntityManager(game, false);

            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(mgr.ManagerGuid) { X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(mgr, parentblobs);

            Vector4 currentPos = new Vector4 { X=-0.77473184638034, Y = 0.967145228951685 };
            Vector4 currentVelocity = new Vector4 { Y = Distance.KmToAU(40) };
            double nonNewtSpeed = Distance.KmToAU( 283.018);

            Vector4 targetObjPosition = new Vector4 { X = 0.149246434443459, Y=-0.712107888348067 };
            Vector4 targetObjVelocity = new Vector4 { Y = Distance.KmToAU(35) };


            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + myMass) / 3.347928976e33;
            //KeplerElements ke = OrbitMath.KeplerFromVelocityAndPosition(sgp, targetObjPosition, targetObjVelocity);

            var currentDateTime = new DateTime(2000, 1, 1);

            OrbitDB targetOrbit = OrbitDB.FromVector(parentEntity, myMass, parentMass, sgp, targetObjPosition, targetObjVelocity, currentDateTime);



            var intercept = InterceptCalcs.GetInterceptPosition2(currentPos, nonNewtSpeed, targetOrbit ,currentDateTime);

            var futurePos1 = Distance.AuToKm( OrbitProcessor.GetAbsolutePosition_AU(targetOrbit, intercept.Item2));

            var futurePos2 = Distance.AuToKm( intercept.Item1);




            Assert.AreEqual(futurePos1.Length(), futurePos2.Length(), 0.01);
            Assert.AreEqual(futurePos1.X, futurePos2.X, 0.01);
            Assert.AreEqual(futurePos1.Y, futurePos2.Y, 0.01);
            Assert.AreEqual(futurePos1.Z, futurePos2.Z, 0.01);
            var time = intercept.Item2 - currentDateTime;

            var distance = (currentPos - intercept.Item1).Length();
            var distancekm = Distance.AuToKm(distance);

            var speed = distance / time.TotalSeconds;
            var speed2 = distancekm / time.TotalSeconds;

            var distb = nonNewtSpeed * time.TotalSeconds;
            var distbKM = Distance.AuToKm(distb);
            var timeb = distance / nonNewtSpeed;

            Assert.AreEqual(nonNewtSpeed, speed, 1.0e-10 );

            var dif = distancekm - distbKM;
            Assert.AreEqual(distancekm, distbKM, 0.25);
        }

        [Test]
        public void TestNewtonTrajectory()
        {
            Game game = new Game();
            EntityManager mgr = new EntityManager(game, false);
            Entity parentEntity = TestingUtilities.BasicEarth(mgr);

            PositionDB pos1 = new PositionDB(mgr.ManagerGuid) { X = 0, Y = 8.52699302490434E-05, Z = 0 };
            BaseDataBlob[] objBlobs1 = new BaseDataBlob[3];
            objBlobs1[0] = pos1;
            objBlobs1[1] = new MassVolumeDB() { Mass = 10000 };
            objBlobs1[2] = new NewtonMoveDB(parentEntity)
            {
                CurrentVector_kms = new Vector4(-10.0, 0, 0, 0)
            };
            Entity objEntity1 = new Entity(mgr, objBlobs1);
            PositionDB pos2 = new PositionDB(mgr.ManagerGuid) { X = 0, Y = 8.52699302490434E-05, Z = 0 };
            BaseDataBlob[] objBlobs2 = new BaseDataBlob[3];
            objBlobs2[0] = pos2;
            objBlobs2[1] = new MassVolumeDB() { Mass = 10000 };
            objBlobs2[2] = new NewtonMoveDB(parentEntity)
            {
                CurrentVector_kms = new Vector4(-10.0, 0, 0, 0)
            };
            Entity objEntity2 = new Entity(mgr, objBlobs2);

            var seconds = 100;
            for (int i = 0; i < seconds; i++)
            {
                NewtonionMovementProcessor.NewtonMove(objEntity1, 1);
            }
            NewtonionMovementProcessor.NewtonMove(objEntity2, seconds);
            var distance1 = Distance.AuToKm(pos1.AbsolutePosition_AU.Length());
            var distance2 = Distance.AuToKm(pos2.AbsolutePosition_AU.Length());

            //this test is currently failing and I'm unsure why. right now the code is using a 1s timestep so it should come out exact...
            //it looks ok graphicaly though so I'm not *too* conserned about this one right now. 
            Assert.AreEqual(distance1, distance2); //if we put the variable timstep which is related to the speed of the object in we'll have to give this a delta


        }
    }
}
