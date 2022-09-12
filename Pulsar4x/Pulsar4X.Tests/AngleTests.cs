using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests
{

	using NUnit.Framework;

	[TestFixture, Description("Tests for the Angle helper class.")]
	internal class AngleTests
	{

		private double epsilon = 1e-12d;

		[Test]
		public void CorrectlyConvertsBetweenDegreeAndRadian(
			[Random(-360, 360, 10)]double degrees
		)
		{
			double radians = Angle.ToRadians(degrees);
			Assert.That(radians, Is.EqualTo(degrees*Math.PI/180).Within(epsilon),
				"Conversion to radians incorrect");

			double newDegrees = Angle.ToDegrees(radians);
			Assert.That(newDegrees, Is.EqualTo(degrees).Within(epsilon),
				"Conversion to degrees incorrect");
		}

		[TestCase(0, 0)]
		[TestCase(30, 30)]
		[TestCase(-30, -30)]
		[TestCase(179, 179)]
		[TestCase(-179, -179)]
		[TestCase(180, 180)]
		[TestCase(-180, -180)]
		[TestCase(181, 181)]
		[TestCase(-181, -181)]
		[TestCase(359, 359)]
		[TestCase(-359, -359)]
		[TestCase(360, 0)]
		[TestCase(-360, 0)]
		[TestCase(361, 1)]
		[TestCase(-361, -1)]
		[TestCase(721, 1)]
		[TestCase(-721, -1)]
		public void DegreesCorrectlyNormalized(double input, double output)
		{
			Assert.That(Angle.NormaliseDegrees(input), Is.EqualTo(output));
		}

		[TestCase(0, 0)]
		[TestCase(1/6*Math.PI, 1/6*Math.PI)]
		[TestCase(-1/6*Math.PI, -1/6*Math.PI)]
		[TestCase(11/12*Math.PI, 11/12*Math.PI)]
		[TestCase(-11/12*Math.PI, -11/12*Math.PI)]
		[TestCase(Math.PI, Math.PI)]
		[TestCase(-Math.PI, -Math.PI)]
		[TestCase(13/12*Math.PI, 1/12*Math.PI)]
		[TestCase(-13/12*Math.PI, -1/12*Math.PI)]
		[TestCase(23/12*Math.PI, 11/12*Math.PI)]
		[TestCase(-23/12*Math.PI, -11/12*Math.PI)]
		[TestCase(2*Math.PI, 0)]
		[TestCase(-2*Math.PI, 0)]
		[TestCase(25/12*Math.PI, 1/12*Math.PI)]
		[TestCase(-25/12*Math.PI, -1/12*Math.PI)]
		public void RadiansCorrectlyNormalized(double input, double output)
		{
			Assert.That(Angle.NormaliseRadians(input), Is.EqualTo(output));
		}

		[Test]
		public void CorrectlyNormalisesAngles(
			[Random(-1080d, 1080d, 10)] double degrees
		)
		{
			double normalized = Angle.NormaliseDegrees(degrees);

			Assert.That(normalized, Is.InRange(-360, 360), 
				"Degrees out of range");
			Assert.That(normalized, Is.EqualTo(degrees % 360),
				"Degrees incorrect value");

			double radians = Angle.ToRadians(degrees);
			normalized = Angle.NormaliseRadians(radians);

			Assert.That(normalized, Is.InRange(-2*Math.PI, 2*Math.PI), 
				"Radians out of range");
			Assert.That(normalized, Is.EqualTo(radians % (2*Math.PI)),
				"Radians incorrect value");

			normalized = Angle.NormaliseRadiansPositive(radians);
			double check = radians % (2*Math.PI);
			check += (check < 0) ? 2*Math.PI : 0;

			Assert.That(normalized, Is.InRange(0, 2*Math.PI),
				"PositiveRadians out of range");
			Assert.That(normalized, Is.EqualTo(check),
				"PositiveRadians incorrect value");

		}

		[TestCase(10, 5, 5)]
		[TestCase(-10, 5, 15)]
		[TestCase(5, 10, 5)]
		[TestCase(720, 240, 120)]
		[TestCase(720, -360, 0)]
		[TestCase(361, 1, 0)]
		public void DegreeSubtractionReturnCorrectValue(double a1, double a2, double diff)
		{

			Assert.That(Angle.DifferenceBetweenDegrees(a1, a2), Is.EqualTo(diff));

		}

	}
}
