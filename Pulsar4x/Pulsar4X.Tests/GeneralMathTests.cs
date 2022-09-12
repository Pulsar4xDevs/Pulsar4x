using Pulsar4X.Orbital;

namespace Pulsar4X.Tests
{

	using NUnit.Framework;

	internal class GeneralMathTests
	{
		#region Clamp
		private void CheckClamp(double test, double min, double max, double value)
		{
			Assert.That(GeneralMath.Clamp(test, min, max), Is.EqualTo(value));

			MinMaxStruct s = new MinMaxStruct(min, max);
			Assert.That(GeneralMath.Clamp(test, s), Is.EqualTo(value));
		}

		[TestCase(1, 0, 4)]
		[TestCase(-1, -1, 4)]
		[TestCase(-4, -5, -4)]
		public void ClampKeepsSameValueWhenInRange(double test, double min, double max)
		{
			CheckClamp(test, min, max, test);
		}

		[TestCase(4.1, 0, 4)]
		[TestCase(5, -1, 4)]
		[TestCase(0, -5, -4)]
		public void ClampReturnsMaxWhenOverRange(double test, double min, double max)
		{
			CheckClamp(test, min, max, max);
		}

		[TestCase(-0.1, 0, 4)]
		[TestCase(-3, -1, 4)]
		[TestCase(-12, -5, -4)]
		public void ClampReturnsMinWhenUnderRange(double test, double min, double max)
		{
			CheckClamp(test, min, max, min);
		}
		#endregion
		#region GetVector

		[TestCase(1, 3, 5, 1, 0, 0, 4)]
		[TestCase(1, 3, 5, 1, 0, 0, 1)]
		[TestCase(1, 3, 5, 1, 0, 0, -1)]
		[TestCase(-4, 13, 24, 1, 1, 0, 4)]
		[TestCase(-4, 13, 24, -8, 5, -5, -12)]
		[TestCase(2, -1, 5, 5, 1, 4, 0)]
		public void GetVectorCorrectlyCalculatesVelocityVector(
			double p1, double p2, double p3,
			double d1, double d2, double d3,
			double magnitude
		)
		{
			Vector3 position = new Vector3(p1, p2, p3);
			Vector3 direction = new Vector3(d1, d2, d3);
			Vector3 target = position + direction;

			Assert.That
			(
				GeneralMath.GetVector(position, target, magnitude), 
				Is.EqualTo(Vector3.Normalise(direction)*magnitude)
			);

		}

		#endregion
	}
}
