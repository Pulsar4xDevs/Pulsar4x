using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Tests the DataBlobReference class. Note that it does not test the save/load/PostLoad functions, these are runtime only tests.")]
    class DataBlobRefTests
    {
        [Test]
        public void EqulityTest()
        {
            // lets start by creating a couple of data blobs:
            SpeciesDB species = new SpeciesDB();
            SpeciesDB nullSpecies = null;
            PositionDB position1 = new PositionDB(42, 42);
            PositionDB position2 = new PositionDB(42, 42);

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
            PositionDB position1 = new PositionDB(42, 42);
            PositionDB position2 = new PositionDB(42, 42);
            DataBlobRef<PositionDB> posRef1 = new DataBlobRef<PositionDB>(position1);
            DataBlobRef<PositionDB> posRef2 = new DataBlobRef<PositionDB>(position2);
            DataBlobRef<PositionDB> posRef3 = new DataBlobRef<PositionDB>(position1);

            // ref should have the same has as the actual DB:
            Assert.AreEqual(posRef1.GetHashCode(), position1.GetHashCode());
            Assert.AreEqual(posRef2.GetHashCode(), position2.GetHashCode());

            // refs to different dbs should have different hashes:
            Assert.IsTrue(posRef1.GetHashCode() != posRef2.GetHashCode());

            // ref to the same dbs should have the same hash:
            Assert.IsTrue(posRef1.GetHashCode() == posRef3.GetHashCode());
        }
    }
}
