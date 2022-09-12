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
	}
}
