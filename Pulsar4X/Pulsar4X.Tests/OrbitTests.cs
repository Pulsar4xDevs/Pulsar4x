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
        public void TestKepler()
        {
            Vector4 postion = new Vector4() { X = Distance.KmToAU(405400) }; //moon at apoapsis
            Vector4 velocity = new Vector4() { Y = Distance.KmToAU(0.97) }; //approx velocity of moon at apoapsis
            double parentMass = 5.97237e24;
            double objMass = 7.342e22;
            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + objMass ) / 3.347928976e33;
            KeplerElements elements = OrbitMath.SetParametersFromVelocityAndPosition(sgp, postion, velocity);

            Vector4 postionKm = new Vector4() { X = 405400 };
            Vector4 velocityKm = new Vector4() { Y = 0.97 };

            double sgpKm = GameConstants.Science.GravitationalConstant * (parentMass + objMass) / 1000000000;

            KeplerElements elementsKm = OrbitMath.SetParametersFromVelocityAndPosition(sgpKm, postionKm, velocityKm);

            KeplerElements elemntsKmConverted = new KeplerElements()
            {
                SemiMajorAxis = Distance.AuToKm(elements.SemiMajorAxis),
                SemiMinorAxis = Distance.AuToKm(elements.SemiMinorAxis),
                Eccentricity = elements.Eccentricity,
                LinierEccentricity = Distance.AuToKm(elements.Eccentricity),
                Periapsis = Distance.AuToKm(elements.Periapsis),
                Apoapsis = Distance.AuToKm(elements.Apoapsis),
                LoAN = elements.LoAN,
                AoP = elements.AoP,
                Inclination = elements.Inclination,
                TrueAnomaly = elements.TrueAnomaly
            };

            var speedAU = OrbitProcessor.PreciseOrbitalSpeed(sgp, postion.Length(), elements.SemiMajorAxis);
            var speedVectorAU = OrbitProcessor.PreciseOrbitalVector(sgp, postion, elements.SemiMajorAxis);
            Assert.AreEqual(speedAU, speedVectorAU.Length());

            var speedKm = velocityKm.Length();
            var speedKm2 = OrbitProcessor.PreciseOrbitalSpeed(sgpKm, postionKm.Length(), elementsKm.SemiMajorAxis);
            Assert.AreEqual(speedKm, speedKm2, 0.001);
        }

    }
}
