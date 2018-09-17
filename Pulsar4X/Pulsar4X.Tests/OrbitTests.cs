using System;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Tests kepler form velocity")]
    public class OrbitTests
    {


        [Test]
        public void TestPreciseOrbitalSpeed()
        { 
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgpKm = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 1000000000;
            var speedKm = OrbitProcessor.PreciseOrbitalSpeed(sgpKm, 405400, 384399);
            Assert.AreEqual(0.97, speedKm, 0.01);
        }



        [Test]
        public void TestKeplerElementsFromVectors()
        {
            Vector4 position = new Vector4() { X = Distance.KmToAU(405400) }; //moon at apoapsis
            Vector4 velocity = new Vector4() { Y = Distance.KmToAU(0.97) }; //approx velocity of moon at apoapsis
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;
            KeplerElements elements = OrbitMath.KeplerFromVelocityAndPosition(sgp, position, velocity);

            Vector4 postionKm = new Vector4() { X = 405400 };
            Vector4 velocityKm = new Vector4() { Y = 0.97 };
            double sgpKm = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 1000000000;

            KeplerElements elementsKm = OrbitMath.KeplerFromVelocityAndPosition(sgpKm, postionKm, velocityKm);

            //check that the function is unit agnostic.
            Assert.AreEqual(Distance.AuToKm(elements.SemiMajorAxis), elementsKm.SemiMajorAxis, 0.001);
            Assert.AreEqual(elements.Eccentricity, elementsKm.Eccentricity, 1.0e-9); //this is where inacuarcy from units stars creaping in, not much can do about that.
            Assert.AreEqual(Distance.AuToKm(elements.Apoapsis), elementsKm.Apoapsis, 0.001);
            Assert.AreEqual(Distance.AuToKm(elements.Periapsis), elementsKm.Periapsis, 0.001);


            var speedAU = OrbitProcessor.PreciseOrbitalSpeed(sgp, position.Length(), elements.SemiMajorAxis);
            var speedVectorAU = OrbitProcessor.PreciseOrbitalVector(sgp, position, elements.SemiMajorAxis);
            Assert.AreEqual(speedAU, speedVectorAU.Length());

            Assert.AreEqual(elementsKm.Apoapsis + elementsKm.Periapsis, elementsKm.SemiMajorAxis * 2, 0.001);


            var speedKm = velocityKm.Length();
            var speedKm2 = OrbitProcessor.PreciseOrbitalSpeed(sgpKm, postionKm.Length(), elementsKm.SemiMajorAxis);
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
        public void TestOrbitDBFromVectors()
        {
            Vector4 position = new Vector4() { X = Distance.KmToAU(405400) }; //moon at apoapsis
            Vector4 velocity = new Vector4() { Y = Distance.KmToAU(0.97) }; //approx velocity of moon at apoapsis
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;
            KeplerElements ke = OrbitMath.KeplerFromVelocityAndPosition(sgp, position, velocity);

            Game game = new Game();
            EntityManager man = new EntityManager(game, false);

            BaseDataBlob[] parentblobs = new BaseDataBlob[2];
            parentblobs[0] = new PositionDB(man.ManagerGuid) {X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            Entity parentEntity = new Entity(man, parentblobs);


            OrbitDB objOrbit = OrbitDB.FromVector(parentEntity, objMass, parentMass, sgp, position, velocity, new DateTime());
            Vector4 resultPos = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());

            //check trueAnomaly 
            var orbTrueAnom = OrbitProcessor.GetTrueAnomaly(objOrbit, new DateTime());
            Assert.AreEqual(ke.TrueAnomaly, orbTrueAnom);

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

        }

        [Test]
        public void TestOrbitDBFromVectorsInKM()
        {
            Vector4 position = new Vector4() { X = 405400 }; //moon at apoapsis
            Vector4 velocity = new Vector4() { Y = 0.97 }; //approx velocity of moon at apoapsis
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 1000000000;
            KeplerElements ke = OrbitMath.KeplerFromVelocityAndPosition(sgp, position, velocity);

            Game game = new Game();
            EntityManager man = new EntityManager(game, false);

            BaseDataBlob[] parentblobs = new BaseDataBlob[2];
            parentblobs[0] = new PositionDB(man.ManagerGuid) { X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            Entity parentEntity = new Entity(man, parentblobs);

            OrbitDB objOrbit = OrbitDB.FromVectorKM(parentEntity, objMass, parentMass, sgp, position, velocity, new DateTime());
            Vector4 resultPos = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());


            Assert.AreEqual(ke.Eccentricity, objOrbit.Eccentricity);
            Assert.AreEqual(ke.SemiMajorAxis, Distance.AuToKm(objOrbit.SemiMajorAxis)); 


            var lenke1 = ke.SemiMajorAxis * 2;
            var lenke2 = ke.Apoapsis + ke.Periapsis;
            Assert.AreEqual(lenke1, lenke2);
            var lendb1 = objOrbit.SemiMajorAxis * 2;
            var lendb2 = objOrbit.Apoapsis + objOrbit.Periapsis;

            //check lengths of ellipse are the same;
            Assert.AreEqual(lendb1, lendb2);
            Assert.AreEqual(lenke1, Distance.AuToKm( lendb1));
            Assert.AreEqual(lenke2, Distance.AuToKm( lendb2));

            var sma1 = ke.SemiMinorAxis;
            var sma2 = EllipseMath.SemiMinorAxisFromApsis(ke.Apoapsis, ke.Periapsis);
            var sma3 = EllipseMath.SemiMinorAxis(ke.SemiMajorAxis, ke.Eccentricity);

            Assert.AreEqual(sma1, sma2);
            var dif = sma2 - sma3;
            Assert.AreEqual(sma2, sma3, double.Epsilon);

            var sma4 = EllipseMath.SemiMinorAxis(objOrbit.SemiMajorAxis, objOrbit.Eccentricity);
            var sma5 = EllipseMath.SemiMinorAxisFromApsis(objOrbit.Apoapsis, objOrbit.Periapsis);

            var dif2 = sma4 - sma5;
            Assert.AreEqual(sma4, sma5, 1.0e-15);
            //check the orbitWidths are the same;
            Assert.AreEqual(sma1, Distance.AuToKm(sma4), 1.0e-9);

            Assert.GreaterOrEqual(ke.Apoapsis, ke.Periapsis);
            Assert.GreaterOrEqual(objOrbit.Apoapsis, objOrbit.Periapsis);


            var db_apkm = Distance.AuToKm(objOrbit.Apoapsis);
            var db_pekm = Distance.AuToKm(objOrbit.Periapsis);
            var differnce = ke.Apoapsis - db_apkm;
            var peDif = ke.Periapsis - db_pekm;
            Assert.AreEqual(ke.Apoapsis, db_apkm, 1.0e-10); 
            Assert.AreEqual(ke.Periapsis, db_pekm, 1.0e-10);



            //check trueAnomaly 
            var orbTrueAnom = OrbitProcessor.GetTrueAnomaly(objOrbit, new DateTime());
            Assert.AreEqual(ke.TrueAnomaly, orbTrueAnom);

            Vector4 resultKM = Distance.AuToKm(resultPos);
            var diference = position.Length() - resultKM.Length();
            Assert.AreEqual(position.Length(), resultKM.Length(), 0.01);



            Assert.AreEqual(position.X, resultKM.X, 0.01);
            Assert.AreEqual(position.Y, resultKM.Y, 0.01);
            Assert.AreEqual(position.Z, resultKM.Z, 0.01);
        }

        [Test]
        public void TestOrbitDBFromVectorsHighEccent()
        {
            Vector4 position = new Vector4() { X = 0.57 }; //Halley's Comet at periapse aprox
            Vector4 velocity = new Vector4() { Y = Distance.KmToAU(54) }; 
            double parentMass = 1.989e30;
            double objMass = 2.2e+15;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 3.347928976e33;
            KeplerElements ke = OrbitMath.KeplerFromVelocityAndPosition(sgp, position, velocity);

            Game game = new Game();
            EntityManager man = new EntityManager(game, false);

            BaseDataBlob[] parentblobs = new BaseDataBlob[2];
            parentblobs[0] = new PositionDB(man.ManagerGuid) { X = 0, Y = 0, Z = 0 };
            parentblobs[1] = new MassVolumeDB() { Mass = parentMass };
            Entity parentEntity = new Entity(man, parentblobs);


            OrbitDB objOrbit = OrbitDB.FromVector(parentEntity, objMass, parentMass, sgp, position, velocity, new DateTime());
            Vector4 resultPos = OrbitProcessor.GetPosition_AU(objOrbit, new DateTime());

            //check trueAnomaly 
            var orbTrueAnom = OrbitProcessor.GetTrueAnomaly(objOrbit, new DateTime());
            var degreeDifference = Angle.ToDegrees( orbTrueAnom - ke.TrueAnomaly);
            Assert.AreEqual(ke.TrueAnomaly, orbTrueAnom, 1.0e15);

            Assert.AreEqual(ke.Eccentricity, objOrbit.Eccentricity);
            Assert.AreEqual(ke.SemiMajorAxis, objOrbit.SemiMajorAxis);


            var lenke1 = ke.SemiMajorAxis * 2;
            var lenke2 = ke.Apoapsis + ke.Periapsis;
            Assert.AreEqual(lenke1, lenke2);
            var lendb1 = objOrbit.SemiMajorAxis * 2;
            var lendb2 = objOrbit.Apoapsis + objOrbit.Periapsis;
            Assert.AreEqual(lendb1, lendb2);
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

        }
    }
}
