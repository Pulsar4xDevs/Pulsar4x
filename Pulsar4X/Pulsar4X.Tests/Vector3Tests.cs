using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using System;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

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
            Assert.True(vec1 == new Vector3(vec1));
            Assert.True(vec2 == new Vector3(vec2.X, vec2.Y, vec2.Z));
        }

        [Test]
        public void EqualCorrectlyReturnsFalse() 
        {
            Assert.False(vec1 == new Vector3(vec1.X, vec1.Y, vec1.Z + 1));
            Assert.False(vec1 == new Vector3(vec1.X, vec1.Y + 1, vec1.Z));
            Assert.False(vec1 == new Vector3(vec1.X + 1, vec1.Y, vec1.Z));
        }

        [Test]
        public void NotEqualCorrectlyReturnsTrue() 
        {
			Assert.True(vec1 != new Vector3(vec1.X, vec1.Y, vec1.Z + 1));
			Assert.True(vec1 != new Vector3(vec1.X, vec1.Y + 1, vec1.Z));
			Assert.True(vec1 != new Vector3(vec1.X + 1, vec1.Y, vec1.Z));
		}

        [Test]
        public void NotEqualCorrectlyReturnsFalse() 
        {
			Assert.False(vec1 != new Vector3(vec1));
			Assert.False(vec2 != new Vector3(vec2.X, vec2.Y, vec2.Z));
		}

        [Test]
        public void Addition()
        {
            Assert.That(vec1 + vec2, Is.EqualTo(new Vector3(2, 2, 2)).Within(1).Ulps);

            Assert.That(Vector3.Add(vec1, vec2), Is.EqualTo(new Vector3(2, 2, 2)).Within(1).Ulps);

            Assert.That(Vector3.One + Vector3.Zero, Is.EqualTo(Vector3.One).Within(1).Ulps);
        }

        [Test]
        public void Subtraction()
        {
            Assert.That(vec1 - vec2, Is.EqualTo(new Vector3(-2, 0, 2)).Within(1).Ulps);

            Assert.That(Vector3.Subtract(vec1, vec2), Is.EqualTo(new Vector3(-2, 0, 2)).Within(1).Ulps);

            Assert.That(Vector3.Zero - Vector3.One, Is.EqualTo(-Vector3.One).Within(1).Ulps);
        }

        [Test]
        public void Multiplication()
        {
            Assert.That(vec1 * vec2, Is.EqualTo(new Vector3(0.0, 1.0, 0.0)).Within(1).Ulps);

            Assert.That(vec1 * 2, Is.EqualTo(new Vector3(0.0, 2.0, 4.0)).Within(1).Ulps);

            Assert.That(3 * vec1, Is.EqualTo(new Vector3(0.0, 3.0, 6.0)).Within(1).Ulps);

            Assert.That(Vector3.Multiply(vec1, vec2), Is.EqualTo(new Vector3(0.0, 1.0, 0.0)).Within(1).Ulps);

            Assert.That(Vector3.Multiply(vec1, 2), Is.EqualTo(new Vector3(0.0, 2.0, 4.0)).Within(1).Ulps);

            Assert.That(Vector3.Multiply(3, vec1), Is.EqualTo(new Vector3(0.0, 3.0, 6.0)).Within(1).Ulps);
        }

        [Test]
        public void Division()
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
    }
}
