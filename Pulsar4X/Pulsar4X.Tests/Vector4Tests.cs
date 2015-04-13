using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Tests for the Vector4 Struct.")]
    class Vector4Tests
    {
        private Vector4 vec1 = new Vector4(0, 1, 2, 3);
        private Vector4 vec2 = new Vector4(3, 2, 1, 0);

        [SetUp]
        public void Init()
        {
            vec1 = new Vector4(0, 1, 2, 3);
            vec2 = new Vector4(3, 2, 1, 0);
        }

        [Test]
        public void Addition()
        {
            Assert.That(vec1 + vec2, Is.EqualTo(new Vector4(3, 3, 3, 3)).Within(1).Ulps);

            Assert.That(Vector4.Add(vec1, vec2), Is.EqualTo(new Vector4(3, 3, 3, 3)).Within(1).Ulps);

            Assert.That(Vector4.One + Vector4.Zero, Is.EqualTo(Vector4.One).Within(1).Ulps);
        }

        [Test]
        public void Subtraction()
        {
            Assert.That(vec1 - vec2, Is.EqualTo(new Vector4(-3, -1, 1, 3)).Within(1).Ulps);

            Assert.That(Vector4.Subtract(vec1, vec2), Is.EqualTo(new Vector4(-3, -1, 1, 3)).Within(1).Ulps);

            Assert.That(Vector4.Zero - Vector4.One, Is.EqualTo(-Vector4.One).Within(1).Ulps);
        }

        [Test]
        public void Multiplication()
        {
            Assert.That(vec1 * vec2, Is.EqualTo(new Vector4(0.0, 2.0, 2.0, 0.0)).Within(1).Ulps);

            Assert.That(vec1 * 2, Is.EqualTo(new Vector4(0.0, 2.0, 4.0, 6.0)).Within(1).Ulps);

            Assert.That(3 * vec1, Is.EqualTo(new Vector4(0.0, 3.0, 6.0, 9.0)).Within(1).Ulps);

            Assert.That(Vector4.Multiply(vec1, vec2), Is.EqualTo(new Vector4(0.0, 2.0, 2.0, 0.0)).Within(1).Ulps);

            Assert.That(Vector4.Multiply(vec1, 2), Is.EqualTo(new Vector4(0.0, 2.0, 4.0, 6.0)).Within(1).Ulps);

            Assert.That(Vector4.Multiply(3, vec1), Is.EqualTo(new Vector4(0.0, 3.0, 6.0, 9.0)).Within(1).Ulps);
        }

        [Test]
        public void Division()
        {
            vec1.X = 1;
            vec2.W = 1;

            Assert.That(vec1 / vec2, Is.EqualTo(new Vector4(1.0 / 3.0, 0.5, 2, 3)).Within(1).Ulps);

            Assert.That(vec1 / 2, Is.EqualTo(new Vector4(0.5, 0.5, 1, 3.0 / 2.0)).Within(1).Ulps);

            Assert.That(Vector4.Divide(vec1, vec2), Is.EqualTo(new Vector4(1.0 / 3.0, 0.5, 2, 3)).Within(1).Ulps);

            Assert.That(Vector4.Divide(vec1, 2), Is.EqualTo(new Vector4(0.5, 0.5, 1, 3.0 / 2.0)).Within(1).Ulps);
        }

        [Test]
        public void Length()
        {
            vec1 = new Vector4(1);

            Assert.That(vec1.Length(), Is.EqualTo(Math.Sqrt(4.0)).Within(1).Ulps);
            Assert.That(vec1.LengthSquared(), Is.EqualTo(4.0).Within(1).Ulps);

            Assert.That(Vector4.UnitX.Length(), Is.EqualTo(Math.Sqrt(1)).Within(1).Ulps);
            Assert.That(Vector4.UnitY.Length(), Is.EqualTo(Math.Sqrt(1)).Within(1).Ulps);
            Assert.That(Vector4.UnitZ.LengthSquared(), Is.EqualTo(1.0).Within(1).Ulps);

            Assert.That(vec2.LengthSquared(), Is.EqualTo(14.0).Within(1).Ulps);
            Assert.That(vec2.Length(), Is.EqualTo(Math.Sqrt(14)).Within(1).Ulps);

            vec1 = new Vector4(1);
        }
    }
}
