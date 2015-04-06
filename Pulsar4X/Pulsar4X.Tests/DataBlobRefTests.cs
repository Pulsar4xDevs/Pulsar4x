using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Tests the DataBlobReference class. Note that it does not test the save/load/PostLoad functions, these are runtime only tests.")]
    class DataBlobRefTests
    {
        private Game game;

        [SetUp]
        public void Init()
        {
            game = new Game();
        }

        [TearDown]
        public void Cleanup()
        {
            game = null;
        }

        [Test]
        public void EqulityTest()
        {
            // lets start by creating a couple of data blobs:
            SpeciesDB species = new SpeciesDB();
            SpeciesDB nullSpecies = null;
            PositionDB position1 = new PositionDB(42, 42, 42);
            PositionDB position2 = new PositionDB(42, 42, 42);

            // okay now lets put them into references:
            DataBlobRef<SpeciesDB> speciesRef1 = new DataBlobRef<SpeciesDB>(species);
            DataBlobRef<SpeciesDB> speciesRef2 = new DataBlobRef<SpeciesDB>(species);
            DataBlobRef<PositionDB> posRef1 = new DataBlobRef<PositionDB>(position1);
            DataBlobRef<PositionDB> posRef2 = new DataBlobRef<PositionDB>(position2);

            Assert.IsTrue(speciesRef1.Equals(speciesRef1));
            Assert.IsTrue(speciesRef1.Equals(speciesRef2));
            Assert.IsFalse(speciesRef1.Equals(null));
            Assert.IsTrue(speciesRef1.Equals((object)speciesRef1));
            Assert.IsFalse(speciesRef1.Equals(new object()));
            Assert.IsTrue(speciesRef1.Equals(species));
            Assert.IsFalse(posRef1.Equals(position2));

            Assert.IsTrue(speciesRef1 == speciesRef1);
            Assert.IsTrue(speciesRef1 == speciesRef2);
            Assert.IsFalse(speciesRef1 == nullSpecies);
            Assert.IsFalse(speciesRef1 == (object)null);
            Assert.IsTrue(speciesRef1 == species);
            Assert.IsTrue(species == speciesRef1);
            Assert.IsFalse(posRef1 == posRef2);

            Assert.IsFalse(speciesRef1 != speciesRef1);
            Assert.IsFalse(speciesRef1 != speciesRef2);
            Assert.IsTrue(speciesRef1 != nullSpecies);
            Assert.IsTrue(speciesRef1 != DataBlobRef<SpeciesDB>.Null());
            Assert.IsFalse(speciesRef1 != species);
            Assert.IsFalse(species != speciesRef1);
            Assert.IsTrue(posRef1 != posRef2);

            // Test IsNull too:
            Assert.IsFalse(speciesRef2.IsNull());
            //speciesRef2.Ref = null;
            //Assert.IsTrue(speciesRef2.IsNull());
        }

        [Test]
        public void HashFunctionTest()
        {
            // create the references:
            PositionDB position1 = new PositionDB(42, 42, 42);
            PositionDB position2 = new PositionDB(42, 42, 42);

            var posRef1 = RefGenerator.MakeDataBlobRef(position1);
            var posRef2 = RefGenerator.MakeDataBlobRef(position2);
            var posRef3 = RefGenerator.MakeDataBlobRef(position1);

            // refs to different dbs should have different hashes:
            Assert.IsTrue(posRef1.GetHashCode() != posRef2.GetHashCode());

            // ref to the same dbs should a differnt has as well hash:
            Assert.IsTrue(posRef1.GetHashCode() != posRef3.GetHashCode());

            // the same ref should have the same hash:
            Assert.IsTrue(posRef1.GetHashCode() == posRef1.GetHashCode());

        }
    }
}
