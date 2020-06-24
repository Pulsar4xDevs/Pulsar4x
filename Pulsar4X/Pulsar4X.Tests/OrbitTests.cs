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
            Vector3 position = new Vector3() { X = Distance.AuToMt(0.57) }; //Halley's Comet at periapse aprox
            Vector3 velocity = new Vector3() { Y = 54000 };

            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(man.ManagerGuid) { X_AU = 0, Y_AU = 0, Z_AU = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(man, parentblobs);
            double sgp_m = OrbitMath.CalculateStandardGravityParameterInM3S2(parentMass, objMass);

            OrbitDB objOrbit = OrbitDB.FromVector(parentEntity, objMass, parentMass, sgp_m, position, velocity, new DateTime());
            Vector3 resultPos = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());
        }


        [Test]
        public void TestPreciseOrbitalSpeed()
        {
            
            var parentMass = 5.97237e24;
            var objMass = 7.342e22;
            var sgpm = OrbitMath.CalculateStandardGravityParameterInM3S2(parentMass, objMass);
            var speedm = OrbitMath.InstantaneousOrbitalSpeed(sgpm, 405400000, 384399000);
            Assert.AreEqual(970, speedm, 0.025);
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
            Vector3 position = new Vector3() { X = Distance.KmToAU(405400) }; //moon at apoapsis
            Vector3 velocity = new Vector3() { Y = Distance.KmToAU(0.97) }; //approx velocity of moon at apoapsis
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgp = OrbitMath.CalculateStandardGravityParameterInKm3S2(parentMass, objMass);
            KeplerElements elements = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity, new DateTime());

            Vector3 postionKm = new Vector3() { X = 405400 };
            Vector3 velocityKm = new Vector3() { Y = 0.97 };
            double sgpKm = OrbitMath.CalculateStandardGravityParameterInM3S2(parentMass, objMass);

            KeplerElements elementsKm = OrbitMath.KeplerFromPositionAndVelocity(sgpKm, postionKm, velocityKm, new DateTime());

            //check that the function is unit agnostic.
            Assert.AreEqual(Distance.AuToKm(elements.SemiMajorAxis), elementsKm.SemiMajorAxis, 0.001);
            Assert.AreEqual(elements.Eccentricity, elementsKm.Eccentricity, 1.0e-9); //this is where inacuarcy from units stars creaping in, not much can do about that.
            Assert.AreEqual(Distance.AuToKm(elements.Apoapsis), elementsKm.Apoapsis, 0.001);
            Assert.AreEqual(Distance.AuToKm(elements.Periapsis), elementsKm.Periapsis, 0.001);

            //var ta = OrbitMath.TrueAnomalyFromEccentricAnomaly(elements.Eccentricity, elements.)
            var speedAU = OrbitMath.InstantaneousOrbitalSpeed(sgp, position.Length(), elements.SemiMajorAxis);
            //var speedVectorAU = OrbitMath.PreciseOrbitalVelocityVector(sgp, position, elements.SemiMajorAxis, elements.Eccentricity, elements.LoAN + elements.AoP);
            //Assert.AreEqual(speedAU, speedVectorAU.Length());

            Assert.AreEqual(elementsKm.Apoapsis + elementsKm.Periapsis, elementsKm.SemiMajorAxis * 2, 0.001);


            var speedKm = velocityKm.Length();
            var speedKm2 = OrbitMath.InstantaneousOrbitalSpeed(sgpKm, postionKm.Length(), elementsKm.SemiMajorAxis);
            Assert.AreEqual(speedKm, speedKm2, 0.001);


            Assert.GreaterOrEqual(elements.Apoapsis, elements.Periapsis);
            Assert.GreaterOrEqual(elementsKm.Apoapsis, elementsKm.Periapsis);

            //below was some experimentation with different ways of calculating things, and an attempt to use decimal for Eccentricity.
            //not sure it's worth the minor slowdown or complication, didn't seem to fix the problem I was seeing in anycase. 
            #region experimentation 

            var H = Vector3.Cross(postionKm, velocityKm).Length();
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
            Vector3 position = new Vector3() { X = Distance.KmToM(405400) }; //moon at apoapsis
            Vector3 velocity = new Vector3() { Y = 970 }; //approx velocity of moon at apoapsis
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            //test high eccentricity orbit
            parentMass = 1.989e30;
            objMass = 2.2e+15;
            position = new Vector3() { X = Distance.AuToMt(0.57) }; //Halley's Comet at periapse aprox
            velocity = new Vector3() { Y = Distance.KmToM(54) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);



            parentMass = 1.989e30;
            objMass = 2.2e+15;
            position = new Vector3() { X = Distance.AuToMt(0.25), Y = Distance.AuToMt(0.25) }; 
            velocity = new Vector3() { Y = Distance.KmToM(54) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            parentMass = 1.989e30;
            objMass = 10000;
            position = new Vector3() { X = Distance.AuToMt(0.25), Y = Distance.AuToMt(0.25) }; 
            velocity = new Vector3() { X = Distance.KmToM(0), Y = Distance.KmToM(1) };
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

        }

        [Test]
        public void Distance_AuToMt_When_Given1_Should_Give149597870700()
        {
            Assert.AreEqual(149597870700d, Distance.AuToMt(1.0d));
        }

        [Test]
        public void OrbitMath_CalculateAngularMomentum_When_ZeroXPositiveYVelocity_Should_GiveCorrectResults()
        {
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToMt(0.25), Y = Distance.AuToMt(0.25) };
            Vector3 velocity = new Vector3() { X = 0, Y = Distance.KmToM(2) };
            var expectedResult = new Vector3(
                0,
                0,
                74798935350000.0
            );
            var calculatedResult = OrbitMath.CalculateAngularMomentum(position, velocity);
            Assert.IsTrue(TestVectorsAreEqual(expectedResult, calculatedResult, 1.0d));
        }

        [Test]
        public void OrbitMath_CalculateAngularMomentum_When_ZeroXNegativeYVelocity_Should_GiveCorrectResults()
        {
            // To determine what the Kepler Elements should be, use : http://orbitsimulator.com/formulas/OrbitalElements.html
            Vector3 position = new Vector3() { X = Distance.AuToMt(0.25), Y = Distance.AuToMt(0.25) };
            Vector3 velocity = new Vector3() { X = 0, Y = Distance.KmToM(-1) };
            var expectedResult = new Vector3(
                0,
                0,
                -37399467675000.0d
            );
            var calculatedResult = OrbitMath.CalculateAngularMomentum(position, velocity);
            Assert.IsTrue(TestVectorsAreEqual(expectedResult, calculatedResult, 1.0d));
        }

        [Test]
        public void OrbitMath_CalculateLongitudeOfAscendingNode_When_APositiveNodeVector_Should_GiveCorrectResult()
        {
            var nodeVector = new Vector3(
                0,
                0,
                37399467675000.0d
            );
            var calculatedResult = OrbitMath.CalculateLongitudeOfAscendingNode(nodeVector);
            Assert.AreEqual(0, calculatedResult, 0.000000001d);
        }

        [Test]
        public void OrbitMath_CalculateLongitudeOfAscendingNode_When_ANegativeNodeVector_Should_GiveCorrectResult()
        {
            var nodeVector = new Vector3(
                0,
                0,
                -37399467675000.0d
            );
            var calculatedResult = OrbitMath.CalculateLongitudeOfAscendingNode(nodeVector);
            Assert.AreEqual(0.7853981767666225d, calculatedResult, 0.000000001d);
        }

        [Test]
        public void OrbitMath_KeplerFromPositionAndVelocity_When_ZeroXPositiveYVelocity_Should_GiveCorrectResults()
        {
            double parentMass = 1.989e30;
            double objMass = 10000;


        }

        [Test]
        public void FailingOrbitsFromVectorTests()
        {
            double parentMass = 1.989e30;
            double objMass = 10000;
            Vector3 position = new Vector3() { X = Distance.AuToMt(0.25), Y = Distance.AuToMt(0.25) };
            Vector3 velocity = new Vector3() { X = Distance.KmToM(0), Y = Distance.KmToM(1) }; //passes
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector3() { X = Distance.KmToM(0), Y = -Distance.KmToM(2) }; //fails
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector3() { X = Distance.KmToM(1), Y = Distance.KmToM(0) }; //fails
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

            velocity = new Vector3() { X = Distance.KmToM(-1), Y = Distance.KmToM(0) }; //fails
            TestOrbitDBFromVectors(parentMass, objMass, position, velocity);

        }
        public bool TestVectorsAreEqual(Vector3 expected, Vector3 actual, double requiredAccuracy = 0.01)
        {
            Assert.AreEqual(expected.X, actual.X, requiredAccuracy);
            Assert.AreEqual(expected.Y, actual.Y, requiredAccuracy);
            Assert.AreEqual(expected.Z, actual.Z, requiredAccuracy);

            return true;
        }
        public void TestOrbitDBFromVectors(double parentMass, double objMass, Vector3 position_InMeters, Vector3 velocity_InMetersSec)
        {
            double angleΔ = 0.0000000001;
            double sgp_m = OrbitMath.CalculateStandardGravityParameterInM3S2(objMass, parentMass);
            KeplerElements ke_m = OrbitMath.KeplerFromPositionAndVelocity(sgp_m, position_InMeters, velocity_InMetersSec, new DateTime());

            Game game = new Game();
            EntityManager man = new EntityManager(game, false);

            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(man.ManagerGuid) { X_AU = 0, Y_AU = 0, Z_AU = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(man, parentblobs);


            OrbitDB objOrbit = OrbitDB.FromVector(parentEntity, objMass, parentMass, sgp_m, position_InMeters, velocity_InMetersSec, new DateTime());
            Vector3 resultPos_AU = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());

            //check LoAN
            var objLoAN = objOrbit.LongitudeOfAscendingNode;
            var keLoAN = ke_m.LoAN;
            var loANDifference = objLoAN - keLoAN;
            Assert.AreEqual(keLoAN, objLoAN, angleΔ);

            //check AoP
            var objAoP = objOrbit.ArgumentOfPeriapsis;
            var keAoP = ke_m.AoP;
            var difference = objAoP - keAoP;
            Assert.AreEqual(keAoP, objAoP, angleΔ);


            //check MeanAnomalyAtEpoch
            var objM0 = objOrbit.MeanAnomalyAtEpoch;
            var keM0 = ke_m.MeanAnomalyAtEpoch;
            Assert.AreEqual(keM0, objM0, angleΔ);
            Assert.AreEqual(objM0, OrbitMath.GetMeanAnomalyFromTime(objM0, objOrbit.MeanMotion_DegreesSec, 0), "meanAnomalyError");

            //checkEpoch
            var objEpoch = objOrbit.Epoch;
            var keEpoch = ke_m.Epoch;
            Assert.AreEqual(keEpoch, objEpoch);

            
            
            //check EccentricAnomaly:
            var objE = (OrbitProcessor.GetEccentricAnomaly(objOrbit, objOrbit.MeanAnomalyAtEpoch_Degrees));
            //var keE =   (OrbitMath.Gete(position, ke.SemiMajorAxis, ke.LinearEccentricity, ke.AoP));
            /*
            if (objE != keE)
            {
                var dif = objE - keE;
                Assert.AreEqual(keE, objE, angleΔ);
            }
*/
            //check trueAnomaly 
            var orbTrueAnom = OrbitProcessor.GetTrueAnomaly(objOrbit, new DateTime());
            var orbtaDeg = Angle.ToDegrees(orbTrueAnom);
            var differenceInRadians = orbTrueAnom - ke_m.TrueAnomalyAtEpoch;
            var differenceInDegrees = Angle.ToDegrees(differenceInRadians);
            if (ke_m.TrueAnomalyAtEpoch != orbTrueAnom) 
            { 

                Vector3 eccentVector = OrbitMath.EccentricityVector(sgp_m, position_InMeters, velocity_InMetersSec);
                var tacalc1 = OrbitMath.TrueAnomaly(eccentVector, position_InMeters, velocity_InMetersSec);
                var tacalc2 = OrbitMath.TrueAnomaly(sgp_m, position_InMeters, velocity_InMetersSec);

                var diffa = differenceInDegrees;
                var diffb = Angle.ToDegrees(orbTrueAnom - tacalc1);
                var diffc = Angle.ToDegrees(orbTrueAnom - tacalc2);

                var ketaDeg = Angle.ToDegrees(tacalc1);
            }

            Assert.AreEqual(0, Angle.DifferenceBetweenRadians(ke_m.TrueAnomalyAtEpoch, orbTrueAnom), angleΔ,
                "more than " + angleΔ + " radians difference, at " + differenceInRadians + " \n " +
                "(more than " + Angle.ToDegrees(angleΔ) + " degrees difference at " + differenceInDegrees + ")" + " \n " +
                "ke Angle: " + ke_m.TrueAnomalyAtEpoch + " obj Angle: " + orbTrueAnom + " \n " +
                "ke Angle: " + Angle.ToDegrees(ke_m.TrueAnomalyAtEpoch) + " obj Angle: " + Angle.ToDegrees(orbTrueAnom));
                
            Assert.AreEqual(ke_m.Eccentricity, objOrbit.Eccentricity);
            Assert.AreEqual(ke_m.SemiMajorAxis, objOrbit.SemiMajorAxis);


            var majAxisLenke = ke_m.SemiMajorAxis * 2;
            var majAxisLenke2 = ke_m.Apoapsis + ke_m.Periapsis;
            Assert.AreEqual(majAxisLenke, majAxisLenke2, 1.0E-10);
            var majAxisLendb = objOrbit.SemiMajorAxis * 2;
            var majAxisLendb2 = objOrbit.Apoapsis + objOrbit.Periapsis;
            Assert.AreEqual(majAxisLendb, majAxisLendb2, 1.0E-6 );
            Assert.AreEqual(majAxisLenke, majAxisLendb, 1.0E-10 );
            Assert.AreEqual(majAxisLenke2, majAxisLendb2, 1.0E-6 );



            var ke_apm = ke_m.Apoapsis;
            var db_apm = objOrbit.Apoapsis;
            var differnce = ke_apm - db_apm;
            Assert.AreEqual(ke_m.Apoapsis, objOrbit.Apoapsis, 1.0E-6 );
            Assert.AreEqual(ke_m.Periapsis, objOrbit.Periapsis, 1.0E-6 );

            Vector3 pos_m = position_InMeters;
            Vector3 result_m = Distance.AuToMt(resultPos_AU);

            double keslr = EllipseMath.SemiLatusRectum(ke_m.SemiMajorAxis, ke_m.Eccentricity);
            double keradius = OrbitMath.RadiusAtAngle(ke_m.TrueAnomalyAtEpoch, keslr, ke_m.Eccentricity);
            Vector3 kemathPos = OrbitMath.GetRalitivePosition(ke_m.LoAN, ke_m.AoP, ke_m.Inclination, ke_m.TrueAnomalyAtEpoch, keradius);
            
            Assert.AreEqual(kemathPos.Length(), pos_m.Length(), 0.02);

            Assert.AreEqual(pos_m.Length(), result_m.Length(), 0.02, "TA: " + orbtaDeg);
            Assert.AreEqual(pos_m.X, result_m.X, 0.01, "TA: " + orbtaDeg);
            Assert.AreEqual(pos_m.Y, result_m.Y, 0.01, "TA: " + orbtaDeg);
            Assert.AreEqual(pos_m.Z, result_m.Z, 0.01, "TA: " + orbtaDeg);

            if (velocity_InMetersSec.Z == 0)
            {
                Assert.IsTrue(ke_m.Inclination == 0);
                Assert.IsTrue(objOrbit.Inclination_Degrees == 0);
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
            parentblobs[0] = new PositionDB(mgr.ManagerGuid) { X_AU = 0, Y_AU = 0, Z_AU = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            parentblobs[2] = new OrbitDB();
            Entity parentEntity = new Entity(mgr, parentblobs);

            Vector3 currentPos_m = new Vector3 { X= Distance.AuToMt( -0.77473184638034), Y =Distance.AuToMt( 0.967145228951685) };
            Vector3 currentVelocity_m = new Vector3 { Y = Distance.KmToM(40) };
            double nonNewtSpeed_m = Distance.KmToM( 283.018);

            Vector3 targetObjPosition = new Vector3 { X = Distance.AuToMt(0.149246434443459),  Y= Distance.AuToMt(-0.712107888348067) };
            Vector3 targetObjVelocity = new Vector3 { Y = Distance.KmToM(35) };


            double sgp_m = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, parentMass);
            //KeplerElements ke = OrbitMath.KeplerFromVelocityAndPosition(sgp, targetObjPosition, targetObjVelocity);

            var currentDateTime = new DateTime(2000, 1, 1);

            OrbitDB targetOrbit = OrbitDB.FromVector(parentEntity, myMass, parentMass, sgp_m, targetObjPosition, targetObjVelocity, currentDateTime);



            var intercept_m = OrbitMath.GetInterceptPosition_m(currentPos_m, nonNewtSpeed_m, targetOrbit ,currentDateTime);

            var futurePos1_m =  OrbitProcessor.GetAbsolutePosition_m(targetOrbit, intercept_m.Item2);

            var futurePos2_m =  intercept_m.Item1;




            Assert.AreEqual(futurePos1_m.Length(), futurePos2_m.Length(), 0.01);
            Assert.AreEqual(futurePos1_m.X, futurePos2_m.X, 0.01);
            Assert.AreEqual(futurePos1_m.Y, futurePos2_m.Y, 0.01);
            Assert.AreEqual(futurePos1_m.Z, futurePos2_m.Z, 0.01);
            var time = intercept_m.Item2 - currentDateTime;
            var distance_m = (currentPos_m - intercept_m.Item1).Length();
            var speed = distance_m / time.TotalSeconds;
            var distb_m = nonNewtSpeed_m * time.TotalSeconds;

            var timeb = distance_m / nonNewtSpeed_m;

            Assert.AreEqual(nonNewtSpeed_m, speed, 1.0e-4 );

            var dif = distance_m - distb_m;
            Assert.AreEqual(distance_m, distb_m, 100.0, "Out by a difference of " + dif + " meters");
        }

        [Test]
        public void TestNewtonTrajectory()
        {
            Game game = new Game();
            EntityManager mgr = new EntityManager(game, false);
            Entity parentEntity = TestingUtilities.BasicEarth(mgr);

            PositionDB pos1 = new PositionDB(mgr.ManagerGuid, parentEntity) { X_AU = 0, Y_AU = 8.52699302490434E-05, Z_AU = 0 };
            BaseDataBlob[] objBlobs1 = new BaseDataBlob[3];
            objBlobs1[0] = pos1;
            objBlobs1[1] = new MassVolumeDB() { Mass = 10000 };
            objBlobs1[2] = new NewtonMoveDB(parentEntity, new Vector3(-10.0, 0, 0));
            Entity objEntity1 = new Entity(mgr, objBlobs1);
            
            
            PositionDB pos2 = new PositionDB(mgr.ManagerGuid, parentEntity) { X_AU = 0, Y_AU = 8.52699302490434E-05, Z_AU = 0 };
            BaseDataBlob[] objBlobs2 = new BaseDataBlob[3];
            objBlobs2[0] = pos2;
            objBlobs2[1] = new MassVolumeDB() { Mass = 10000 };
            objBlobs2[2] = new NewtonMoveDB(parentEntity, new Vector3(-10.0, 0, 0));
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
