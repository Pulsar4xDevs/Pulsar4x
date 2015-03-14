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

            // Create entity with existing datablobs, but provide a null list:
            Assert.Catch(typeof(ArgumentNullException), () =>
                {
                    entityManager.CreateEntity(null); // should throw ArgumentNullException
                });
        }

        [Test]
        public void SetDataBlobs()
        {
            int testEntity = entityManager.CreateEntity();
            entityManager.SetDataBlob(testEntity, OrbitDB.FromStationary(5));
            entityManager.SetDataBlob(testEntity, new PopulationDB(10));

            // test bad input:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                OrbitDB bad = null;
                entityManager.SetDataBlob(testEntity, bad); // should throw ArgumentNullException
            });
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
        public void GetDataBlobByEntity()
        {
            int testEntity = PopulateEntityManager();

            // Get the Population DB of a specific entity.
            PopulationDB popDB = entityManager.GetDataBlob<PopulationDB>(testEntity);
            Assert.IsNotNull(popDB);

            // get a DB we know the entity does not have:
            AtmosphereDB AtmoDB = entityManager.GetDataBlob<AtmosphereDB>(testEntity);
            Assert.IsNull(AtmoDB);

            // test with invalid entity ID:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                entityManager.GetDataBlob<PopulationDB>(42);
            });

            // test with invalid data blob type
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                entityManager.GetDataBlob<BaseDataBlob>(testEntity);
            });

            // and again for the second lookup type:
            // Get the Population DB of a specific entity.
            int typeIndex = entityManager.GetDataBlobTypeIndex<PopulationDB>();
            popDB = entityManager.GetDataBlob<PopulationDB>(testEntity, typeIndex);
            Assert.IsNotNull(popDB);

            // get a DB we know the entity does not have:
            typeIndex = entityManager.GetDataBlobTypeIndex<AtmosphereDB>();
            AtmoDB = entityManager.GetDataBlob<AtmosphereDB>(testEntity, typeIndex);
            Assert.IsNull(AtmoDB);

            // test with invalid entity ID:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                entityManager.GetDataBlob<AtmosphereDB>(42, typeIndex);
            });

            // test with invalid type index:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                entityManager.GetDataBlob<AtmosphereDB>(testEntity, -42);
            });

            // test with invalid T vs type at typeIndex
            typeIndex = entityManager.GetDataBlobTypeIndex<PopulationDB>();
            Assert.Catch(typeof(InvalidCastException), () =>
            {
                entityManager.GetDataBlob<PlanetInfoDB>(testEntity, typeIndex);
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

            Assert.IsFalse(entityManager.IsValidEntity(testEntity));

            // now lets remove an entity that does not exist:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
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

        [Test]
        public void RemoveDataBlobs()
        {
            // a little setup:
            int testEntity = entityManager.CreateEntity();
            entityManager.SetDataBlob(testEntity, new PopulationDB(10));

            Assert.IsTrue(entityManager.GetDataBlob<PopulationDB>(testEntity) != null);  // check that it has the data blob
            entityManager.RemoveDataBlob<PopulationDB>(testEntity);                     // Remove a data blob
            Assert.IsTrue(entityManager.GetDataBlob<PopulationDB>(testEntity) == null); // now check that it doesn't

            // now lets try remove it again:
            entityManager.RemoveDataBlob<PopulationDB>(testEntity); 

            // now lets try removal for an entity that does not exist:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
                {
                    entityManager.RemoveDataBlob<PopulationDB>(42);  
                });

            // cannot remove baseDataBlobs, invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
                {
                    entityManager.RemoveDataBlob<BaseDataBlob>(testEntity);  
                });


            // reset:
            entityManager.SetDataBlob(testEntity, new PopulationDB(10));
            int typeIndex = entityManager.GetDataBlobTypeIndex<PopulationDB>();

            Assert.IsTrue(entityManager.GetDataBlob<PopulationDB>(testEntity) != null);  // check that it has the data blob
            entityManager.RemoveDataBlob(testEntity, typeIndex);              // Remove a data blob
            Assert.IsTrue(entityManager.GetDataBlob<PopulationDB>(testEntity) == null); // now check that it doesn't

            // now lets try remove it again:
            entityManager.RemoveDataBlob(testEntity, typeIndex);

            // now lets try an invlaid entity:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                entityManager.RemoveDataBlob(42, typeIndex);
            });

            // and an invalid typeIndex:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                entityManager.RemoveDataBlob(testEntity, -42);
            });
        }

        [Test]
        public void EntityLookup()
        {
            PopulateEntityManager();

            // Find all entities with a specific DataBlob.
            List<int> entities = entityManager.GetAllEntitiesWithDataBlob<PopulationDB>();
            Assert.AreEqual(2, entities.Count);

            // again, but look for a datablob that no entity has:
            entities = entityManager.GetAllEntitiesWithDataBlob<AtmosphereDB>();
            Assert.AreEqual(0, entities.Count);

            // check with invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
                {
                    entityManager.GetAllEntitiesWithDataBlob<BaseDataBlob>();
                });

            // now lets lookup using a mask:
            Pulsar4X.Helpers.ComparableBitArray dataBlobMask = entityManager.BlankDataBlobMask();
            dataBlobMask.Set(entityManager.GetDataBlobTypeIndex<PopulationDB>(), true);
            dataBlobMask.Set(entityManager.GetDataBlobTypeIndex<OrbitDB>(), true);
            entities = entityManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
            Assert.AreEqual(2, entities.Count);

            // and with a mask that will not match any entities:
            dataBlobMask.Set(entityManager.GetDataBlobTypeIndex<AtmosphereDB>(), true);
            entities = entityManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
            Assert.AreEqual(0, entities.Count);

            // and an empty mask:
            dataBlobMask = entityManager.BlankDataBlobMask();
            entities = entityManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
            Assert.AreEqual(0, entities.Count);

            // test bad mask:
            Pulsar4X.Helpers.ComparableBitArray badMask = new Helpers.ComparableBitArray(4242); // use a big number so we never rach that many data blobs.
            Assert.Catch(typeof(ArgumentException), () =>
                {
                    entityManager.GetAllEntitiesWithDataBlobs(badMask);
                });

            Assert.Catch(typeof(NullReferenceException), () =>
                {
                    entityManager.GetAllEntitiesWithDataBlobs(null);
                });
        }

        [Test]
        public void EntityValidity()
        {
            int testEntity = entityManager.CreateEntity();
            Assert.IsTrue(entityManager.IsValidEntity(testEntity));

            // now test invalid input:
            Assert.IsFalse(entityManager.IsValidEntity(-42));
            Assert.IsFalse(entityManager.IsValidEntity(42));

            // and a removed entity:
            entityManager.RemoveEntity(testEntity);
            Assert.IsFalse(entityManager.IsValidEntity(testEntity));
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
