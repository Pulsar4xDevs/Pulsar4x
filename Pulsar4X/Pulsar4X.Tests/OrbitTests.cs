using System;
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
            var E2 = OrbitMath.GetEccentricAnomaly(e, meanAnomaly);
            var E3 = OrbitMath.GetEccentricAnomaly2(ν, e);

            Assert.AreEqual(E1, E2, 0.00000001);
            Assert.AreEqual(E1, E3, 0.00000001);
        }


        [Test]
        public void TestTrueAnomalyCalcs()
        {
            Vector4 pos = new Vector4() { X = 0.25, Y = 0.25 };
            Vector4 vel = new Vector4() { Y = Distance.KmToAU(54) };
            TrueAnomalyCalcs(pos, vel);


            pos = new Vector4() { X = 0.25, Y = 0.25 };
            vel = new Vector4() { Y = Distance.KmToAU(54) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = 0.25, Y = -0.25 };
            vel = new Vector4() { X = Distance.KmToAU(54) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = -0.25, Y = 0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-54) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = -0.25, Y = -0.25 };
            vel = new Vector4() { Y = Distance.KmToAU(-54) };
            TrueAnomalyCalcs(pos, vel);

            pos = new Vector4() { X = 0.25, Y = 0.25 };
            vel = new Vector4() { Y = Distance.KmToAU(54) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = 0.25, Y = 0.25 };
            vel = new Vector4() { Y = Distance.KmToAU(-54) };
            TrueAnomalyCalcs(pos, vel); //this one fails currently, however the calcs used in this test are not used in code
            pos = new Vector4() { X = 0.25, Y = 0.25 };
            vel = new Vector4() { X = Distance.KmToAU(54) };
            TrueAnomalyCalcs(pos, vel);
            pos = new Vector4() { X = 0.25, Y = 0.25 };
            vel = new Vector4() { X = Distance.KmToAU(-54) };
            TrueAnomalyCalcs(pos, vel); //this one failes currently, however the calcs used in this test are not used in code

        }

        private void TrueAnomalyCalcs(Vector4 pos, Vector4 vel)
        {
            double angleΔ = 0.0000000001;
            double parentMass = 1.989e30;
            double objMass = 2.2e+15;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;

            Vector4 ev = OrbitMath.EccentricityVector(sgp, pos, vel);
            double e = ev.Length();
            double specificOrbitalEnergy = Math.Pow(vel.Length(), 2) * 0.5 - sgp / pos.Length();
            double a = -sgp / (2 * specificOrbitalEnergy);
            double ae = e * a;
            double aop = Math.Atan2(ev.Y, ev.X);
            double eccentricAnomaly = OrbitMath.GetEccentricAnomalyFromStateVectors(pos, a, ae, aop);

            var θ1 = OrbitMath.TrueAnomaly(sgp, pos, vel);
            var θ2 = OrbitMath.TrueAnomaly(ev, pos, vel);
            var θ3 = OrbitMath.TrueAnomalyFromEccentricAnomaly(e, eccentricAnomaly);
            var θ4 = OrbitMath.TrueAnomalyFromEccentricAnomaly2(e, eccentricAnomaly);
            //var θ5 = OrbitMath.TrueAnomalyFromEccentricAnomaly3(e, eccentricAnomaly);

            Assert.AreEqual(θ1, θ2);
            Assert.AreEqual(θ1, θ3, angleΔ, "Difference of " + Angle.ToDegrees(θ1 - θ3) + "degrees");
            Assert.AreEqual(θ1, θ4, angleΔ, "Difference of " + Angle.ToDegrees(θ1 - θ4) + "degrees");
            //Assert.AreEqual(θ1, θ5, "Difference of " + Angle.ToDegrees(θ1 - θ5) + "degrees"); 
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
            KeplerElements elements = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity);

            Vector4 postionKm = new Vector4() { X = 405400 };
            Vector4 velocityKm = new Vector4() { Y = 0.97 };
            double sgpKm = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 1000000000;

            KeplerElements elementsKm = OrbitMath.KeplerFromPositionAndVelocity(sgpKm, postionKm, velocityKm);

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

        }



        public void TestOrbitDBFromVectors(double parentMass, double objMass, Vector4 position, Vector4 velocity)
        {
            double angleΔ = 0.0000000001; 
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;
            KeplerElements ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity);

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
            Assert.AreEqual(objM0, OrbitMath.CurrentMeanAnomaly(objM0, objOrbit.MeanMotion, 0), "meanAnomalyError");

            //checkEpoch
            var objEpoch = TimeSpan.FromTicks(objOrbit.Epoch.Ticks);
            var keEpoch = TimeSpan.FromSeconds(ke.Epoch);
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
                var orbtaDeg = Angle.ToDegrees(orbTrueAnom);
                var ketaDeg = Angle.ToDegrees(tacalc1);
            }
            Assert.AreEqual(ke.TrueAnomalyAtEpoch, orbTrueAnom, angleΔ, 
                "more than "+ angleΔ + " radians difference, at " + differenceInRadians + " \n " +
                "(more than "+ Angle.ToDegrees(angleΔ) + " degrees difference at "   +differenceInDegrees+")");

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



            Assert.AreEqual(posKM.Length(), resultKM.Length(), 0.01);
            Assert.AreEqual(posKM.X, resultKM.X, 0.01);
            Assert.AreEqual(posKM.Y, resultKM.Y, 0.01);
            Assert.AreEqual(posKM.Z, resultKM.Z, 0.01);

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
    }
}
