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
        public void Length()
        {
            vec1 = new Vector3(1);

            Assert.That(vec1.Length(), Is.EqualTo(Math.Sqrt(3.0)).Within(1).Ulps);
            Assert.That(vec1.LengthSquared(), Is.EqualTo(3.0).Within(1).Ulps);

            Assert.That(Vector3.UnitX.Length(), Is.EqualTo(Math.Sqrt(1)).Within(1).Ulps);
            Assert.That(Vector3.UnitY.Length(), Is.EqualTo(Math.Sqrt(1)).Within(1).Ulps);
            Assert.That(Vector3.UnitZ.LengthSquared(), Is.EqualTo(1.0).Within(1).Ulps);

            Assert.That(vec2.LengthSquared(), Is.EqualTo(5.0).Within(1).Ulps);
            Assert.That(vec2.Length(), Is.EqualTo(Math.Sqrt(5)).Within(1).Ulps);

            vec1 = new Vector3(1);
        }
    }
}
