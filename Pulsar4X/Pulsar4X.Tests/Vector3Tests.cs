using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;
    using System.Linq;
    using System.Runtime.CompilerServices;

    [TestFixture, Description("Tests for the Vector3 Struct.")]
    class Vector3Tests
    {
        private Vector3 vec1 = new Vector3(0, 1, 2);
        private Vector3 vec2 = new Vector3(2, 1, 0);

        [SetUp]
        public void Init()
        {
            vec1 = new Vector3(0, 1, 2);
            vec2 = new Vector3(2, 1, 0);
        }

        [Test]
        public void ConstructorsCorrectlySetCoordinates()
        {
            Vector3 vector = new Vector3(2d); // Should all be equal to 2d
            Assert.AreEqual(2d, vector.X);
            Assert.AreEqual(2d, vector.Y);
            Assert.AreEqual(2d, vector.Z);

            vector = new Vector3(1d, 3d, -4d); // Should be equal as shown
			Assert.AreEqual(1d, vector.X);
			Assert.AreEqual(3d, vector.Y);
			Assert.AreEqual(-4d, vector.Z);

            vector = new Vector3(vector); // Should be the same as previous case
			Assert.AreEqual(1d, vector.X);
			Assert.AreEqual(3d, vector.Y);
			Assert.AreEqual(-4d, vector.Z);
		}

        [Test]
        public void StaticConstructorsReturnCorrectValues()
        {
            Vector3 vector = Vector3.NaN;
            Assert.AreEqual(double.NaN, vector.X);
            Assert.AreEqual(double.NaN, vector.Y);
            Assert.AreEqual(double.NaN, vector.Z);

			vector = Vector3.One;
			Assert.AreEqual(1d, vector.X);
			Assert.AreEqual(1d, vector.Y);
			Assert.AreEqual(1d, vector.Z);

			vector = Vector3.Zero;
			Assert.AreEqual(0d, vector.X);
			Assert.AreEqual(0d, vector.Y);
			Assert.AreEqual(0d, vector.Z);

			vector = Vector3.UnitX;
			Assert.AreEqual(1d, vector.X);
			Assert.AreEqual(0d, vector.Y);
			Assert.AreEqual(0d, vector.Z);

			vector = Vector3.UnitY;
			Assert.AreEqual(0d, vector.X);
			Assert.AreEqual(1d, vector.Y);
			Assert.AreEqual(0d, vector.Z);

			vector = Vector3.UnitZ;
			Assert.AreEqual(0d, vector.X);
			Assert.AreEqual(0d, vector.Y);
			Assert.AreEqual(1d, vector.Z);
		}

        [Test]
        public void EqualCorrectlyReturnsTrue()
        {
            Assert.That(vec1 == new Vector3(vec1), Is.True);
            Assert.That(vec2 == new Vector3(vec2.X, vec2.Y, vec2.Z), Is.True);
        }

        [Test]
        public void EqualCorrectlyReturnsFalse()
        {
            Assert.That(vec1 == new Vector3(vec1.X, vec1.Y, vec1.Z + 1), Is.False);
            Assert.That(vec1 == new Vector3(vec1.X, vec1.Y + 1, vec1.Z), Is.False);
            Assert.That(vec1 == new Vector3(vec1.X + 1, vec1.Y, vec1.Z), Is.False);
        }

        [Test]
        public void NotEqualCorrectlyReturnsTrue()
        {
			Assert.That(vec1 != new Vector3(vec1.X, vec1.Y, vec1.Z + 1), Is.True);
			Assert.That(vec1 != new Vector3(vec1.X, vec1.Y + 1, vec1.Z), Is.True);
			Assert.That(vec1 != new Vector3(vec1.X + 1, vec1.Y, vec1.Z), Is.True);
		}

        [Test]
        public void NotEqualCorrectlyReturnsFalse()
        {
			Assert.That(vec1 != new Vector3(vec1), Is.False);
			Assert.That(vec2 != new Vector3(vec2.X, vec2.Y, vec2.Z), Is.False);
		}

        [Test]
        public void AdditionReturnsCorrectValue()
        {
            Assert.That(vec1 + vec2, Is.EqualTo(new Vector3(2, 2, 2)).Within(1).Ulps);

            Assert.That(Vector3.Add(vec1, vec2), Is.EqualTo(new Vector3(2, 2, 2)).Within(1).Ulps);

            Assert.That(Vector3.One + Vector3.Zero, Is.EqualTo(Vector3.One).Within(1).Ulps);
        }

        [Test]
        public void SubtractionReturnsCorrectValue()
        {
            Assert.That(vec1 - vec2, Is.EqualTo(new Vector3(-2, 0, 2)).Within(1).Ulps);

            Assert.That(Vector3.Subtract(vec1, vec2), Is.EqualTo(new Vector3(-2, 0, 2)).Within(1).Ulps);

            Assert.That(Vector3.Zero - Vector3.One, Is.EqualTo(-Vector3.One).Within(1).Ulps);
        }

        [Test]
        public void MultiplicationReturnsCorrectValue()
        {
            Assert.That(vec1 * vec2, Is.EqualTo(new Vector3(0.0, 1.0, 0.0)).Within(1).Ulps);

            Assert.That(vec1 * 2, Is.EqualTo(new Vector3(0.0, 2.0, 4.0)).Within(1).Ulps);

            Assert.That(3 * vec1, Is.EqualTo(new Vector3(0.0, 3.0, 6.0)).Within(1).Ulps);

            Assert.That(Vector3.Multiply(vec1, vec2), Is.EqualTo(new Vector3(0.0, 1.0, 0.0)).Within(1).Ulps);

            Assert.That(Vector3.Multiply(vec1, 2), Is.EqualTo(new Vector3(0.0, 2.0, 4.0)).Within(1).Ulps);

            Assert.That(Vector3.Multiply(3, vec1), Is.EqualTo(new Vector3(0.0, 3.0, 6.0)).Within(1).Ulps);
        }

        [Test]
        public void DivisionReturnsCorrectValue()
        {
            vec1.X = 1;
            vec2.Z = 1;

            Assert.That(vec1 / vec2, Is.EqualTo(new Vector3(0.5, 1, 2)).Within(1).Ulps);

            Assert.That(vec1 / 2, Is.EqualTo(new Vector3(0.5, 0.5, 1)).Within(1).Ulps);

            Assert.That(Vector3.Divide(vec1, vec2), Is.EqualTo(new Vector3(0.5, 1, 2)).Within(1).Ulps);

            Assert.That(Vector3.Divide(vec1, 2), Is.EqualTo(new Vector3(0.5, 0.5, 1)).Within(1).Ulps);
        }

        [Test]
        public void LengthSquaredReturnsCorrectValue()
        {

            Vector3 vector = new Vector3(2d, 3d, 4d); // Value should be 4 + 9 + 16 = 29
            Assert.AreEqual(29d, vector.LengthSquared());

            vector = new Vector3(2d, -3d, 4d); // Same value, despite the negative coordinate
			Assert.AreEqual(29d, vector.LengthSquared());
		}

        [Test]
        public void LengthReturnsCorrectValue()
        {

            Vector3 vector = new Vector3(3d, 0d, 0d); // Value should be the same as the non-zero coordinate
            Assert.AreEqual(3d, vector.Length());

            vector = new Vector3(2d, 3d, 4d); // Value should be sqrt(4 + 9 + 16)
            Assert.AreEqual(Math.Sqrt(29d), vector.Length());

            vector = new Vector3(2d, -3d, 4d); // Same value, despite the negative coordinate
            Assert.AreEqual(Math.Sqrt(29d), vector.Length());

        }

        [Test]
        public void DifferenceBetweenCrossAndCrossPrecise()
        {
            Random r = new Random();
            List<double> lengths = new List<Double>();
            double maxElement = 0;

            for (int i = 0; i < 1e3; i++)
            {
                Vector3 v1 = Vector3.Random(r);
                Vector3 v2 = Vector3.Random(r);

                Vector3 cross = Vector3.Cross(v1, v2);
                var (X, Y, Z) = Vector3.CrossPrecise(v1, v2);
                Vector3 crossPrecise = new Vector3((double)X, (double)Y, (double)Z);

                Vector3 diff = cross - crossPrecise;
                if (diff.X > maxElement) maxElement = diff.X;
                if (diff.Y > maxElement) maxElement = diff.Y;
                if (diff.Z > maxElement) maxElement = diff.Z;

                lengths.Add(diff.Length());
            }

            Console.WriteLine("Largest single coordinate error:\t" + maxElement.ToString());
            Console.WriteLine("Max difference in position:\t\t" + lengths.Max());
            Console.WriteLine("Average difference in position:\t" + lengths.Average());
        }
    }
}
