using NUnit.Framework;
using System;
using Pulsar4X;
using Pulsar4X.Entities;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SystemGenTests
    {
        [Test]
        public void testGenerateSystemProducesCorrectNames()
        {
            StarSystem sys = SystemGen.CreateSystem("TestSystem", 2);
            Assert.AreEqual(4, sys.Stars.Count, "wrong number of stars, generation changed and this test should be updated");
            Assert.AreEqual("TestSystem A", sys.Stars[0].Name);
            Assert.AreEqual("TestSystem B", sys.Stars[1].Name);
            Assert.AreEqual("TestSystem C", sys.Stars[2].Name);
            Assert.AreEqual("TestSystem D", sys.Stars[3].Name);
        }
    }
}