using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Processors;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Entity Manager Tests")]
    class EntityManagerTests
    {
        EntityManager entityManager;

        [SetUp]
        public void Init()
        {
            entityManager = new EntityManager();
        }

        [TearDown]
        public void Cleanup()
        {
            entityManager = null;
        }


        [Test]
        public void CreateEntity()
        {
            // create entity with no data blobs:
            int testEntity = entityManager.CreateEntity();
            Assert.AreEqual(0, testEntity);

            // Create entity with existing datablobs:
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add(OrbitDB.FromStationary(2));
            dataBlobs.Add(new PopulationDB(9));
            testEntity = entityManager.CreateEntity(dataBlobs);
            Assert.AreEqual(1, testEntity);

            // Create entity with existing datablobs, but provide an empty list:
            dataBlobs.Clear();
            testEntity = entityManager.CreateEntity(dataBlobs);
            Assert.AreEqual(2, testEntity);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateBadEntity()
        {
            // Create entity with existing datablobs, but provide a null list:
            int testEntity = entityManager.CreateEntity(null);  // should throw ArgumentNullException
        }

        [Test]
        public void SetDataBlobs()
        {
            int testEntity = entityManager.CreateEntity();
            entityManager.SetDataBlob(testEntity, OrbitDB.FromStationary(5));
            entityManager.SetDataBlob(testEntity, new PopulationDB(10));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetBadDataBlobs()
        {
            int testEntity = entityManager.CreateEntity();
            OrbitDB bad = null;
            entityManager.SetDataBlob(testEntity, bad);  // should throw ArgumentNullException
        }

        [Test]
        public void GetDatablobsByType()
        {
            // lets try get something from an empty entity manager:
            List<PopulationDB> populations = entityManager.GetAllDataBlobsOfType<PopulationDB>();
            Assert.AreEqual(0, populations.Count);  // should get an empty list

            PopulateEntityManager(); // make sure we have something in there.

            // Get all DataBlobs of a specific type.
            populations = entityManager.GetAllDataBlobsOfType<PopulationDB>();
            Assert.AreEqual(2, populations.Count);

            // and of a different type:
            List<OrbitDB> orbits = entityManager.GetAllDataBlobsOfType<OrbitDB>();
            Assert.AreEqual(3, orbits.Count);

            // and of a type we know is not in the entity manager:
            List<PlanetInfoDB> planetBlobs = entityManager.GetAllDataBlobsOfType<PlanetInfoDB>();
            Assert.AreEqual(0, planetBlobs.Count);  // shoul be 0 as there are none of them.

            // and of all types, should throw as you cannot do this:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                entityManager.GetAllDataBlobsOfType<BaseDataBlob>();
            });
        }

        [Test]
        public void GetDataBlobsByEntity()
        {
            int testEntity = PopulateEntityManager();  // make sure we have something in there.

            // Get all DataBlobs of a specific entity.
            List<BaseDataBlob> dataBlobs = entityManager.GetAllDataBlobsOfEntity(testEntity);
            Assert.AreEqual(2, dataBlobs.Count);

            // empty entity mean empty list.
            testEntity = entityManager.CreateEntity();  // create empty entity.
            dataBlobs = entityManager.GetAllDataBlobsOfEntity(testEntity);
            Assert.AreEqual(0, dataBlobs.Count);

            // and of an entity that does not exist??
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                entityManager.GetAllDataBlobsOfEntity(42);
            });
        }

        [Test]
        public void RemoveEntities()
        {
            int testEntity = PopulateEntityManager();

            // lets check the entity at index testEntity
            var testList = entityManager.GetAllDataBlobsOfEntity(testEntity);
            Assert.AreEqual(2, testList.Count);  // should have 2 datablobs.
            int noOfDataBlobsOfRemovedEntity = testList.Count;

            // Remove an entity.
            entityManager.RemoveEntity(testEntity);

            // now lets see if the entity is still there:
            testList = entityManager.GetAllDataBlobsOfEntity(testEntity);
            // wait that worksed??? of course, becasue there is still an entity at testEntity index, it is not the same of course
            Assert.AreEqual(0, testList.Count); // invlaid entity should have 0 data blobs
            Assert.AreNotEqual(noOfDataBlobsOfRemovedEntity, testList.Count);

            // now lets remove an entity that does not exist:
            Assert.Catch(typeof(IndexOutOfRangeException), () =>
            {
                entityManager.RemoveEntity(42);
            });

            // add a new entity:
            testEntity = entityManager.CreateEntity();

            // now lets clear the entity manager:
            entityManager.Clear();
            entityManager.Clear(); // just to make sure we can clear a empty entity manager.

            // now lets see if that entity is still there:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                entityManager.GetAllDataBlobsOfEntity(testEntity);  // should throw this time
            });
        }


        #region Extra Init Stuff

        /// <summary>
        /// This functions creates 3 entities with a total of 5 data blobs (3 orbits and 2 populations).
        /// </summary>
        /// <returns>It returns a reference to the first entity (containing 1 orbit and 1 pop)</returns>
        private int PopulateEntityManager()
        {
            // Create an entity with individual DataBlobs.
            int testEntity = entityManager.CreateEntity();
            entityManager.SetDataBlob(testEntity, OrbitDB.FromStationary(5));
            entityManager.SetDataBlob(testEntity, new PopulationDB(10));

            // Create an entity with a DataBlobList.
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add(OrbitDB.FromStationary(2));
            entityManager.CreateEntity(dataBlobs);

            // Create one more, just for kicks.
            dataBlobs.Add(new PopulationDB(9));
            entityManager.CreateEntity(dataBlobs);

            return testEntity;
        }

        #endregion

    }
}
