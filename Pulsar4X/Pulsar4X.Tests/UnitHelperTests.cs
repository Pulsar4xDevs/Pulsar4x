using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using System;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Tests for the unit conversion helpers in GameMath.cs")]
    class UnitHelperTests
    {
        [TestCase(90, Math.PI / 2.0, true)]
        [TestCase(180, Math.PI, true)]
        [TestCase(270, Math.PI + Math.PI / 2.0, true)]
        [TestCase(Math.PI / 2.0, 90, false)]
        [TestCase(Math.PI, 180, false)]
        [TestCase(Math.PI + Math.PI / 2.0, 270, false)]
        public void AngleConversionTest(double from, double to, bool toRadians)
        {
            if (toRadians)
            {
                Assert.That(Angle.ToRadians(from), Is.EqualTo(to).Within(0.0000000001));
            }
            else
            {
                Assert.That(Angle.ToDegrees(from), Is.EqualTo(to).Within(0.0000000001));
            }
        }


        /*
        [TestCase]//(new Vector4 { X = 100, Y = 0, Z = 0 }, new Vector4 { X = 0, Y = 1, Z = 0 }, ExpectedResult = 0.01)]
        public void AngleTest()//Vector4 position, Vector4 velocity, double value)
        {
            Vector4 position = new Vector4 { X = 100, Y = 0, Z = 0 };
            Vector4 velocity = new Vector4 { X = 0, Y = 1, Z = 0 };
            double speed = velocity.Length();
            Vector4 angularVelocity = Angle.AngularVelocityVector(position, velocity);
            Assert.That(angularVelocity.Length() / position.Length(), Is.EqualTo(speed).Within(0.0000000001));
        }*/


        [TestCase(1000, 6.6845871222684454959959533702106e-6, true)]
        [TestCase(1000000000000, 6684.5871222684454959959533702106, true)]
        [TestCase(1232539865, 8.2390201092614883053947104074657, true)]
        [TestCase(6.6845871222684454959959533702106e-6, 1000, false)]
        [TestCase(6684.5871222684454959959533702106, 1000000000000, false)]
        [TestCase(8.2390201092614883053947104074657, 1232539865, false)]
        public void DistanceConversionTest(double from, double to, bool toAU)
        {
            if (toAU)
            {
                Assert.That(Distance.KmToAU(from), Is.EqualTo(to).Within(1).Ulps);  // ulps mean units in the last place.
            }
            else
            {
                Assert.That(Distance.AuToKm(from), Is.EqualTo(to).Within(1).Ulps);
            }
        }


        [TestCase(33000, 33273.15, true)]
        [TestCase(0, 273.15, true)]
        [TestCase(-83, 190.15, true)]
        [TestCase(33273.15, 33000, false)]
        [TestCase(273.15, 0, false)]
        [TestCase(190.15, -83, false)]
        public void TemperatureConversionTest(double from, double to, bool toKelvin)
        {
            if (toKelvin)
            {
                Assert.That(Temperature.ToKelvin(from), Is.EqualTo(to).Within(0.000000001)); 
            }
            else
            {
                Assert.That(Temperature.ToCelsius(from), Is.EqualTo(to).Within(0.000000001));
            }
        }

        [TestCase(33000f, 33273.15f, true)]
        [TestCase(0f, 273.15f, true)]
        [TestCase(-83f, 190.15f, true)]
        [TestCase(33273.15f, 33000f, false)]
        [TestCase(273.15f, 0f, false)]
        [TestCase(190.15f, -83f, false)]
        public void TemperatureConversionTest(float from, float to, bool toKelvin)
        {
            if (toKelvin)
            {
                Assert.That(Temperature.ToKelvin(from), Is.EqualTo(to).Within(0.0001)); 
            }
            else
            {
                Assert.That(Temperature.ToCelsius(from), Is.EqualTo(to).Within(0.0001));
            }
        }




    }
}
