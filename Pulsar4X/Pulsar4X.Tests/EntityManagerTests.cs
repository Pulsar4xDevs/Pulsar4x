﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Entity Manager Tests")]
    class EntityManagerTests
    {
        EntityManager _entityManager;
        private Entity _species1;
        private JDictionary<Entity, double> _pop1;
        private JDictionary<Entity, double> _pop2;

        [SetUp]
        public void Init()
        {
            _entityManager = new EntityManager();
            _species1 = _entityManager.CreateEntity(new List<BaseDataBlob> { new SpeciesDB("Human", 1, 0.1, 1.9, 1.0, 0.4, 4, 14, -15, 45) });
            _pop1 = new JDictionary<Entity, double> { { _species1, 10 } };
            _pop2 = new JDictionary<Entity, double> { { _species1, 5 } };
        }

        [TearDown]
        public void Cleanup()
        {
            _entityManager = null;
        }


        [Test]
        public void CreateEntity()
        {
            // create entity with no data blobs:
            Entity testEntity = _entityManager.CreateEntity();
            Assert.IsTrue(testEntity.IsValid);
            Assert.AreEqual(1, testEntity.ID);
            Assert.AreSame(_entityManager, testEntity.Manager);

            // Create entity with existing datablobs:
            var dataBlobs = new List<BaseDataBlob> {OrbitDB.FromStationary(2), new ColonyInfoDB(_pop1)};
            testEntity = _entityManager.CreateEntity(dataBlobs);
            Assert.IsTrue(testEntity.IsValid);
            Assert.AreEqual(2, testEntity.ID);

            // Create entity with existing datablobs, but provide an empty list:
            dataBlobs.Clear();
            testEntity = _entityManager.CreateEntity(dataBlobs);
            Assert.IsTrue(testEntity.IsValid);
            Assert.AreEqual(3, testEntity.ID);

            // Create entity with existing datablobs, but provide a null list:
            Assert.Catch(typeof(ArgumentNullException), () =>
                {
                    _entityManager.CreateEntity(null); // should throw ArgumentNullException
                });

        }

        [Test]
        public void SetDataBlobs()
        {
            Entity testEntity = _entityManager.CreateEntity();
            testEntity.SetDataBlob(OrbitDB.FromStationary(5));
            testEntity.SetDataBlob(new ColonyInfoDB(_pop1));

            // test bad input:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                testEntity.SetDataBlob((OrbitDB)null); // should throw ArgumentNullException
            });
        }

        [Test]
        public void GetDatablobsByType()
        {
            // lets try get something from an empty entity manager:
            List<ColonyInfoDB> populations = _entityManager.GetAllDataBlobsOfType<ColonyInfoDB>();
            Assert.AreEqual(0, populations.Count);  // should get an empty list

            PopulateEntityManager(); // make sure we have something in there.

            // Get all DataBlobs of a specific type.
            populations = _entityManager.GetAllDataBlobsOfType<ColonyInfoDB>();
            Assert.AreEqual(2, populations.Count);

            // and of a different type:
            List<OrbitDB> orbits = _entityManager.GetAllDataBlobsOfType<OrbitDB>();
            Assert.AreEqual(3, orbits.Count);

            // and of a type we know is not in the entity manager:
            List<PlanetInfoDB> planetBlobs = _entityManager.GetAllDataBlobsOfType<PlanetInfoDB>();
            Assert.AreEqual(0, planetBlobs.Count);  // shoul be 0 as there are none of them.

            // and of all types, should throw as you cannot do this:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                _entityManager.GetAllDataBlobsOfType<BaseDataBlob>();
            });
        }

        [Test]
        public void GetDataBlobsByEntity()
        {
            Entity testEntity = PopulateEntityManager();  // make sure we have something in there.

            // Get all DataBlobs of a specific entity.
            List<BaseDataBlob> dataBlobs = testEntity.GetAllDataBlobs();
            Assert.AreEqual(2, dataBlobs.Count);

            // empty entity mean empty list.
            testEntity = _entityManager.CreateEntity();  // create empty entity.
            dataBlobs = testEntity.GetAllDataBlobs();
            Assert.AreEqual(0, dataBlobs.Count);
        }

        [Test]
        public void GetDataBlobByEntity()
        {
            Entity testEntity = PopulateEntityManager();

            // Get the Population DB of a specific entity.
            ColonyInfoDB popDB = testEntity.GetDataBlob<ColonyInfoDB>();
            Assert.IsNotNull(popDB);

            // get a DB we know the entity does not have:
            AtmosphereDB atmoDB = testEntity.GetDataBlob<AtmosphereDB>();
            Assert.IsNull(atmoDB);

            // test with invalid data blob type
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                testEntity.GetDataBlob<BaseDataBlob>();
            });

            // and again for the second lookup type:
            // Get the Population DB of a specific entity.
            int typeIndex = EntityManager.GetTypeIndex<ColonyInfoDB>();
            popDB = testEntity.GetDataBlob<ColonyInfoDB>(typeIndex);
            Assert.IsNotNull(popDB);

            // get a DB we know the entity does not have:
            typeIndex = EntityManager.GetTypeIndex<AtmosphereDB>();
            atmoDB = testEntity.GetDataBlob<AtmosphereDB>(typeIndex);
            Assert.IsNull(atmoDB);

            // test with invalid type index:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                testEntity.GetDataBlob<AtmosphereDB>(-42);
            });

            // test with invalid T vs type at typeIndex
            typeIndex = EntityManager.GetTypeIndex<ColonyInfoDB>();
            Assert.Catch(typeof(InvalidCastException), () =>
            {
                testEntity.GetDataBlob<PlanetInfoDB>(typeIndex);
            });
        }

        [Test]
        public void RemoveEntities()
        {
            Entity testEntity = PopulateEntityManager();

            // lets check the entity at index testEntity
            List<BaseDataBlob> testList = testEntity.GetAllDataBlobs();
            Assert.AreEqual(2, testList.Count);  // should have 2 datablobs.

            // Remove an entity.
            testEntity.DeleteEntity();

            // now lets see if the entity is still there:
            Assert.Catch(typeof (ArgumentException), () =>
            {
                testList = testEntity.GetAllDataBlobs();
            });
   
            Assert.IsFalse(_entityManager.IsValidEntity(testEntity));

            // add a new entity:
            testEntity = _entityManager.CreateEntity();

            // now lets clear the entity manager:
            _entityManager.Clear();
            _entityManager.Clear(); // just to make sure we can clear a empty entity manager.

            // now lets see if that entity is still there:
            Assert.Catch(typeof(ArgumentException), () =>
            {
                testEntity.GetAllDataBlobs();  // should throw this time
            });
        }

        [Test]
        public void RemoveDataBlobs()
        {
            // a little setup:
            Entity testEntity = _entityManager.CreateEntity();
            testEntity.SetDataBlob(new ColonyInfoDB(_pop1));

            Assert.IsTrue(testEntity.GetDataBlob<ColonyInfoDB>() != null);  // check that it has the data blob
            testEntity.RemoveDataBlob<ColonyInfoDB>();                     // Remove a data blob
            Assert.IsTrue(testEntity.GetDataBlob<ColonyInfoDB>() == null); // now check that it doesn't

            // now lets try remove it again:
            testEntity.RemoveDataBlob<ColonyInfoDB>(); 

            // cannot remove baseDataBlobs, invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                testEntity.RemoveDataBlob<BaseDataBlob>();  
            });


            // reset:
            testEntity.SetDataBlob(new ColonyInfoDB(_pop1));
            int typeIndex = EntityManager.GetTypeIndex<ColonyInfoDB>();

            Assert.IsTrue(testEntity.GetDataBlob<ColonyInfoDB>() != null);  // check that it has the data blob
            testEntity.RemoveDataBlob(typeIndex);              // Remove a data blob
            Assert.IsTrue(testEntity.GetDataBlob<ColonyInfoDB>() == null); // now check that it doesn't

            // now lets try remove it again:
            testEntity.RemoveDataBlob(typeIndex);

            // and an invalid typeIndex:
            Assert.Catch(typeof(ArgumentException), () =>
            {
                testEntity.RemoveDataBlob(-42);
            });

            // now lets try an invlaid entity:
            testEntity.DeleteEntity();
            Assert.Catch(typeof(ArgumentException), () =>
            {
                testEntity.RemoveDataBlob(typeIndex);
            });

        }

        [Test]
        public void EntityLookup()
        {
            PopulateEntityManager();

            // Find all entities with a specific DataBlob.
            List<Entity> entities = _entityManager.GetAllEntitiesWithDataBlob<ColonyInfoDB>();
            Assert.AreEqual(2, entities.Count);

            // again, but look for a datablob that no entity has:
            entities = _entityManager.GetAllEntitiesWithDataBlob<AtmosphereDB>();
            Assert.AreEqual(0, entities.Count);

            // check with invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
                {
                    _entityManager.GetAllEntitiesWithDataBlob<BaseDataBlob>();
                }
            );

            // now lets lookup using a mask:
            ComparableBitArray dataBlobMask = EntityManager.BlankDataBlobMask();
            dataBlobMask.Set(EntityManager.GetTypeIndex<ColonyInfoDB>(), true);
            dataBlobMask.Set(EntityManager.GetTypeIndex<OrbitDB>(), true);
            entities = _entityManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
            Assert.AreEqual(2, entities.Count);

            // and with a mask that will not match any entities:
            dataBlobMask.Set(EntityManager.GetTypeIndex<AtmosphereDB>(), true);
            entities = _entityManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
            Assert.AreEqual(0, entities.Count);

            // and an empty mask:
            dataBlobMask = EntityManager.BlankDataBlobMask();
            entities = _entityManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
            Assert.AreEqual(3, entities.Count); // this is counter intuitive... but it is what happens.

            // test bad mask:
            ComparableBitArray badMask = new ComparableBitArray(4242); // use a big number so we never rach that many data blobs.
            Assert.Catch(typeof(ArgumentException), () =>
                {
                    _entityManager.GetAllEntitiesWithDataBlobs(badMask);
                }
            );

            Assert.Catch(typeof(NullReferenceException), () =>
                {
                    _entityManager.GetAllEntitiesWithDataBlobs(null);
                }
            );


            // now lets just get the one entity:
            Entity testEntity = _entityManager.GetFirstEntityWithDataBlob<ColonyInfoDB>();
            Assert.IsTrue(testEntity.IsValid);

            // lookup an entity that does not exist:
            testEntity = _entityManager.GetFirstEntityWithDataBlob<AtmosphereDB>();
            Assert.IsFalse(testEntity.IsValid);

            // try again with incorrect type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                _entityManager.GetFirstEntityWithDataBlob<BaseDataBlob>();
            });


            // now lets just get the one entity, but use a different function to do it:
            int type = EntityManager.GetTypeIndex<ColonyInfoDB>();
            testEntity = _entityManager.GetFirstEntityWithDataBlob(type);
            Assert.IsTrue(testEntity.IsValid);
            Assert.AreEqual(0, testEntity.ID);

            // lookup an entity that does not exist:
            type = EntityManager.GetTypeIndex<AtmosphereDB>();
            testEntity = _entityManager.GetFirstEntityWithDataBlob(type);
            Assert.IsFalse(testEntity.IsValid);

            // try again with incorrect type index:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                _entityManager.GetFirstEntityWithDataBlob(4242);
            });
        }

        [Test]
        public void EntityGuid()
        {
            Entity foundEntity;
            Entity testEntity = _entityManager.CreateEntity();

            Assert.IsTrue(testEntity.IsValid);
            // Check Guid local lookup.
            Assert.IsTrue(_entityManager.TryGetEntityByID(testEntity.ID, out foundEntity));
            Assert.IsTrue(testEntity == foundEntity);

            // Check Guid local reverse lookup
            Assert.IsTrue(_entityManager.TryGetEntityByGuid(testEntity.Guid, out foundEntity));
            Assert.AreEqual(testEntity, foundEntity);
            
            // Check Guid global lookup.
            Assert.IsTrue(EntityManager.FindEntityByGuid(testEntity.Guid, out foundEntity));
            Assert.AreEqual(testEntity, foundEntity);

            // and a removed entity:
            testEntity.DeleteEntity();
            Assert.IsFalse(testEntity.IsValid);

            // Check back Guid lookups.
            Assert.IsFalse(_entityManager.TryGetEntityByGuid(testEntity.Guid, out foundEntity));
            Assert.IsFalse(_entityManager.TryGetEntityByID(-1, out foundEntity));
            Assert.IsFalse(EntityManager.FindEntityByGuid(Guid.Empty, out foundEntity));
        }

        [Test]
        public void EntityTransfer()
        {
            EntityManager manager2 = new EntityManager();

            PopulateEntityManager();

            // Get an entity from the manager.
            Entity testEntity = _entityManager.GetFirstEntityWithDataBlob<OrbitDB>();
            // Ensure we got a valid entity.
            Assert.IsTrue(testEntity.IsValid);
            // Store it's datablobs for later.
            List<BaseDataBlob> testEntityDataBlobs = testEntity.GetAllDataBlobs();
            
            // Store the current GUID.
            Guid entityGuid = testEntity.Guid;

            // Transfer the entity to a new EntityManager
            testEntity.TransferEntity(manager2);

            // Ensure the original manager no longer has the entity.
            Assert.IsFalse(_entityManager.IsValidEntity(testEntity));
            Entity foundEntity;
            Assert.IsFalse(_entityManager.TryGetEntityByGuid(entityGuid, out foundEntity));

            // Ensure the new manager has the entity.
            Assert.IsTrue(manager2.IsValidEntity(testEntity));
            Assert.IsTrue(manager2.TryGetEntityByGuid(entityGuid, out foundEntity));
            Assert.AreSame(testEntity, foundEntity);

            // Get the transferredEntity's datablobs.
            List<BaseDataBlob> transferredEntityDataBlobs = testEntity.GetAllDataBlobs();
            
            // Compare the old datablobs with the new datablobs.
            foreach (BaseDataBlob testEntityDataBlob in testEntityDataBlobs)
            {
                bool matchFound = false;
                foreach (BaseDataBlob transferredDataBlob in transferredEntityDataBlobs)
                {
                    if (ReferenceEquals(testEntityDataBlob, transferredDataBlob))
                    {
                        matchFound = true;
                        break;
                    }
                }
                Assert.IsTrue(matchFound);
            }

        }

        [Test]
        public void TypeIndexTest()
        {
            int typeIndex;
            Assert.Catch<ArgumentNullException>(() => EntityManager.TryGetTypeIndex(null, out typeIndex));
            Assert.Catch<KeyNotFoundException>(() => EntityManager.GetTypeIndex<BaseDataBlob>());

            Assert.IsTrue(EntityManager.TryGetTypeIndex(typeof(OrbitDB), out typeIndex));
            Assert.AreEqual(EntityManager.GetTypeIndex<OrbitDB>(), typeIndex);
        }

        #region Extra Init Stuff

        /// <summary>
        /// This functions creates 3 entities with a total of 5 data blobs (3 orbits and 2 populations).
        /// </summary>
        /// <returns>It returns a reference to the first entity (containing 1 orbit and 1 pop)</returns>
        private Entity PopulateEntityManager()
        {
            // Clear out any previous test results.
            _entityManager.Clear();

            // Create an entity with individual DataBlobs.
            Entity testEntity = _entityManager.CreateEntity();
            testEntity.SetDataBlob(OrbitDB.FromStationary(5));
            testEntity.SetDataBlob(new ColonyInfoDB(_pop1));

            // Create an entity with a DataBlobList.
            var dataBlobs = new List<BaseDataBlob> {OrbitDB.FromStationary(2)};
            _entityManager.CreateEntity(dataBlobs);

            // Create one more, just for kicks.
            dataBlobs = new List<BaseDataBlob> {OrbitDB.FromStationary(5), new ColonyInfoDB(_pop2)};
            _entityManager.CreateEntity(dataBlobs);

            return testEntity;
        }

        #endregion

    }
}
