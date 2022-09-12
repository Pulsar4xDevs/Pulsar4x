using System;
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
		[TestCase(-180, 180)]
		[TestCase(181, -179)]
		[TestCase(-181, 179)]
		[TestCase(359, -1)]
		[TestCase(-359, 1)]
		[TestCase(360, 0)]
		[TestCase(-360, 0)]
		[TestCase(361, 1)]
		[TestCase(-361, -1)]
		[TestCase(721, 1)]
		[TestCase(-721, -1)]
		public void DegreesCorrectlyNormalized(double input, double output)
		{
			Assert.That(Angle.NormaliseDegrees(input), Is.EqualTo(output).Within(epsilon));
		}

		[TestCase(0, 0)]
		[TestCase(1d/6d*Math.PI, 1d/6d*Math.PI)]
		[TestCase(-1d/6d*Math.PI, -1d/6d*Math.PI)]
		[TestCase(11d/12d*Math.PI, 11d/12d*Math.PI)]
		[TestCase(-11d/12d*Math.PI, -11d/12d*Math.PI)]
		[TestCase(Math.PI, Math.PI)]
		[TestCase(-Math.PI, Math.PI)]
		[TestCase(13d/12d*Math.PI, -11d/12d*Math.PI)]
		[TestCase(-13d/12d*Math.PI, 11d/12d*Math.PI)]
		[TestCase(23d/12d*Math.PI, -1d/12d*Math.PI)]
		[TestCase(-23d/12d*Math.PI, 1d/12d*Math.PI)]
		[TestCase(2d*Math.PI, 0)]
		[TestCase(-2d*Math.PI, 0)]
		[TestCase(25d/12d*Math.PI, 1d/12d*Math.PI)]
		[TestCase(-25d/12d*Math.PI, -1d/12d*Math.PI)]
		public void RadiansCorrectlyNormalized(double radians, double normalized)
		{
			Console.WriteLine(radians.ToString() + ", " + normalized.ToString());
			radians %= 2 * Math.PI;
			Console.WriteLine(radians);
			radians = (radians + 2 * Math.PI) % (2 * Math.PI);
			Console.WriteLine(radians);
			if (radians > Math.PI)
			{
				radians -= 2 * Math.PI;
			}
			Console.WriteLine(radians);
			Assert.That(Angle.NormaliseRadians(radians), Is.EqualTo(normalized).Within(epsilon));
		}

		[Test]
		public void NormalizedAnglesInCorrectRange(
			[Random(-1080d, 1080d, 10)] double degrees
		)
		{
			double normalized = Angle.NormaliseDegrees(degrees);

			Assert.That(normalized, Is.InRange(-180, 180), 
				"Degrees out of range");

			double radians = Angle.ToRadians(degrees);
			normalized = Angle.NormaliseRadians(radians);

			Assert.That(normalized, Is.InRange(-Math.PI, Math.PI), 
				"Radians out of range");

			normalized = Angle.NormaliseRadiansPositive(radians);

			Assert.That(normalized, Is.InRange(0, 2*Math.PI),
				"PositiveRadians out of range");

		}

		[TestCase(10, 5, 5)]
		[TestCase(5, 5, 0)]
		[TestCase(-10, 5, -15)]
		[TestCase(5, 10, -5)]
		[TestCase(720, 240, 120)]
		[TestCase(720, -360, 0)]
		[TestCase(361, 1, 0)]
		public void DegreeSubtractionReturnCorrectValue(double a1, double a2, double diff)
		{

			Assert.That(Angle.DifferenceBetweenDegrees(a1, a2), Is.EqualTo(diff).Within(epsilon));

		}

	}
}
