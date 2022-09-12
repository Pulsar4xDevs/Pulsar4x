using System;
using System.Diagnostics;
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
		#region LineIntersectsLine

		[Test]
		public void LineIntersectsLineReturnsTrueIfSegmentsIntersetWithinTheirLengths()
		{
			Vector2 start1 = new Vector2(-1, 0);
			Vector2 end1 = new Vector2(1, 0);

			Vector2 start2 = new Vector2(0, -1);
			Vector2 end2 = new Vector2(0, 1);

			Vector2 output;

			bool result = GeneralMath.LineIntersectsLine(start1, end1, start2, end2, out output);

			Assert.That(result, Is.True);

			start1 = new Vector2(1, 0);
			end1 = new Vector2(-1, 0);

			start2 = new Vector2(0, -1);
			end2 = new Vector2(0, 1);

			result = GeneralMath.LineIntersectsLine(start1, end1, start2, end2, out output);

			Assert.That(result, Is.True);
		}

		[Test]
		public void LineIntersectsLineReturnsFalseIfSegmentsParallel()
		{
			Vector2 start1 = new Vector2(-1, 0);
			Vector2 end1 = new Vector2(1, 0);

			Vector2 start2 = new Vector2(-1, 1);
			Vector2 end2 = new Vector2(1, 1);

			Vector2 output;

			bool result = GeneralMath.LineIntersectsLine(start1, end1, start2, end2, out output);

			Assert.That(result, Is.False);
		}

		[Test]
		public void LineIntersectsLineReturnsFalseIfSegmentsIntersectOutsideTheirLength()
		{
			Vector2 start1 = new Vector2(-1, 0);
			Vector2 end1 = new Vector2(1, 0);

			Vector2 start2 = new Vector2(0, 1);
			Vector2 end2 = new Vector2(0, 2);

			Vector2 output;

			bool result = GeneralMath.LineIntersectsLine(start1, end1, start2, end2, out output);

			Assert.That(result, Is.False);
		}

		[Test]
		public void LineIntersectsLineReturnsCorrectIntersectionPoint()
		{
			Vector2 start1 = new Vector2(-1, 0);
			Vector2 end1 = new Vector2(1, 0);

			Vector2 start2 = new Vector2(0, -1);
			Vector2 end2 = new Vector2(0, 1);

			Vector2 output;

			GeneralMath.LineIntersectsLine(start1, end1, start2, end2, out output);

			Assert.That(output, Is.EqualTo(Vector2.Zero));

			start1 = new Vector2(0, 0);
			end1 = new Vector2(2, 0);

			start2 = new Vector2(1, -1);
			end2 = new Vector2(1, 1);

			GeneralMath.LineIntersectsLine(start1, end1, start2, end2, out output);

			Assert.That(output, Is.EqualTo(new Vector2(1, 0)));

			start1 = new Vector2(-1, 0);
			end1 = new Vector2(1, 0);

			start2 = new Vector2(0, 1);
			end2 = new Vector2(0, 2);

			GeneralMath.LineIntersectsLine(start1, end1, start2, end2, out output);

			Assert.That(output, Is.EqualTo(Vector2.Zero));
		}

		[Test]
		public void LineIntersectsLineRunningTimeStudy()
		{
			Stopwatch sw = new Stopwatch();
			Random random = new Random();

			for (int i = 1; i < 1000000; i++)
			{

				Vector2 start1 = Vector2.Random;
				Vector2 end1 = Vector2.Random;

				Vector2 start2 = Vector2.Random;
				Vector2 end2 = Vector2.Random;

				Vector2 output;

				sw.Start();
				GeneralMath.LineIntersectsLine(start1, end1, start2, end2, out output);
				sw.Stop();

			}
			Console.WriteLine("Elapsed Seconds: " + sw.Elapsed);
		}

		#endregion
	}
}
