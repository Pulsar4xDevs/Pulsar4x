using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib.Helpers;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Tests for the unit conversion helpers in Pulsar4X.ECSLib.Helpers.GameMath")]
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
                Assert.That(to, Is.EqualTo(Angle.ToRadians(from)).Within(0.0000000001));
            }
            else
            {
                Assert.That(to, Is.EqualTo(Angle.ToDegrees(from)).Within(0.0000000001));
            }
        }


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
                Assert.That(to, Is.EqualTo(Distance.ToAU(from)).Within(1).Ulps);  // ulps mean units in the last place.
            }
            else
            {
                Assert.That(to, Is.EqualTo(Distance.ToKm(from)).Within(1).Ulps);
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
                Assert.That(to, Is.EqualTo(Temperature.ToKelvin(from)).Within(0.000000001)); 
            }
            else
            {
                Assert.That(to, Is.EqualTo(Temperature.ToCelsius(from)).Within(0.000000001));
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
                Assert.That(to, Is.EqualTo(Temperature.ToKelvin(from)).Within(0.0001)); 
            }
            else
            {
                Assert.That(to, Is.EqualTo(Temperature.ToCelsius(from)).Within(0.0001));
            }
        }
    }
}
