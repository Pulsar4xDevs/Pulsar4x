using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Helpers;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Entity Manager Tests")]
    class EntityManagerTests
    {
        EntityManager _entityManager;
        private SpeciesDB _species1;
        private Dictionary<SpeciesDB, double> _pop1;
        private Dictionary<SpeciesDB, double> _pop2;

        [SetUp]
        public void Init()
        {
            _entityManager = new EntityManager();
            _species1 = new SpeciesDB("Human", 1, 0.1, 1.9, 1.0, 0.4, 4, 14, -15, 45);
            _pop1 = new Dictionary<SpeciesDB, double> {{_species1, 10}};
            _pop2 = new Dictionary<SpeciesDB, double> {{_species1, 5}};
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
            int testEntity = _entityManager.CreateEntity();
            Assert.AreEqual(0, testEntity);

            // Create entity with existing datablobs:
            var dataBlobs = new List<BaseDataBlob> {OrbitDB.FromStationary(2), new PopulationDB(_pop1)};
            testEntity = _entityManager.CreateEntity(dataBlobs);
            Assert.AreEqual(1, testEntity);

            // Create entity with existing datablobs, but provide an empty list:
            dataBlobs.Clear();
            testEntity = _entityManager.CreateEntity(dataBlobs);
            Assert.AreEqual(2, testEntity);

            // Create entity with existing datablobs, but provide a null list:
            Assert.Catch(typeof(ArgumentNullException), () =>
                {
                    _entityManager.CreateEntity(null); // should throw ArgumentNullException
                });
        }

        [Test]
        public void SetDataBlobs()
        {
            int testEntity = _entityManager.CreateEntity();
            _entityManager.SetDataBlob(testEntity, OrbitDB.FromStationary(5));
            _entityManager.SetDataBlob(testEntity, new PopulationDB(_pop1));

            // test bad input:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                _entityManager.SetDataBlob(testEntity, (OrbitDB)null); // should throw ArgumentNullException
            });
        }

        [Test]
        public void GetDatablobsByType()
        {
            // lets try get something from an empty entity manager:
            List<PopulationDB> populations = _entityManager.GetAllDataBlobsOfType<PopulationDB>();
            Assert.AreEqual(0, populations.Count);  // should get an empty list

            PopulateEntityManager(); // make sure we have something in there.

            // Get all DataBlobs of a specific type.
            populations = _entityManager.GetAllDataBlobsOfType<PopulationDB>();
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
            int testEntity = PopulateEntityManager();  // make sure we have something in there.

            // Get all DataBlobs of a specific entity.
            List<BaseDataBlob> dataBlobs = _entityManager.GetAllDataBlobsOfEntity(testEntity);
            Assert.AreEqual(2, dataBlobs.Count);

            // empty entity mean empty list.
            testEntity = _entityManager.CreateEntity();  // create empty entity.
            dataBlobs = _entityManager.GetAllDataBlobsOfEntity(testEntity);
            Assert.AreEqual(0, dataBlobs.Count);

            // and of an entity that does not exist??
            Assert.Catch(typeof(ArgumentException), () =>
            {
                _entityManager.GetAllDataBlobsOfEntity(42);
            });
        }

        [Test]
        public void GetDataBlobByEntity()
        {
            int testEntity = PopulateEntityManager();

            // Get the Population DB of a specific entity.
            PopulationDB popDB = _entityManager.GetDataBlob<PopulationDB>(testEntity);
            Assert.IsNotNull(popDB);

            // get a DB we know the entity does not have:
            AtmosphereDB atmoDB = _entityManager.GetDataBlob<AtmosphereDB>(testEntity);
            Assert.IsNull(atmoDB);

            // test with invalid entity ID:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                _entityManager.GetDataBlob<PopulationDB>(42);
            });

            // test with invalid data blob type
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                _entityManager.GetDataBlob<BaseDataBlob>(testEntity);
            });

            // and again for the second lookup type:
            // Get the Population DB of a specific entity.
            int typeIndex = _entityManager.GetTypeIndex<PopulationDB>();
            popDB = _entityManager.GetDataBlob<PopulationDB>(testEntity, typeIndex);
            Assert.IsNotNull(popDB);

            // get a DB we know the entity does not have:
            typeIndex = _entityManager.GetTypeIndex<AtmosphereDB>();
            atmoDB = _entityManager.GetDataBlob<AtmosphereDB>(testEntity, typeIndex);
            Assert.IsNull(atmoDB);

            // test with invalid entity ID:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                _entityManager.GetDataBlob<AtmosphereDB>(42, typeIndex);
            });

            // test with invalid type index:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                _entityManager.GetDataBlob<AtmosphereDB>(testEntity, -42);
            });

            // test with invalid T vs type at typeIndex
            typeIndex = _entityManager.GetTypeIndex<PopulationDB>();
            Assert.Catch(typeof(InvalidCastException), () =>
            {
                _entityManager.GetDataBlob<PlanetInfoDB>(testEntity, typeIndex);
            });
        }

        [Test]
        public void RemoveEntities()
        {
            int testEntity = PopulateEntityManager();

            // lets check the entity at index testEntity
            List<BaseDataBlob> testList = _entityManager.GetAllDataBlobsOfEntity(testEntity);
            Assert.AreEqual(2, testList.Count);  // should have 2 datablobs.

            // Remove an entity.
            _entityManager.RemoveEntity(testEntity);

            // now lets see if the entity is still there:
            Assert.Catch(typeof (ArgumentException), () =>
            {
                testList = _entityManager.GetAllDataBlobsOfEntity(testEntity);
            });
   
            Assert.IsFalse(_entityManager.IsValidEntity(testEntity));

            // now lets remove an entity that does not exist:
            Assert.Catch(typeof(ArgumentException), () =>
            {
                _entityManager.RemoveEntity(42);
            });

            // add a new entity:
            testEntity = _entityManager.CreateEntity();

            // now lets clear the entity manager:
            _entityManager.Clear();
            _entityManager.Clear(); // just to make sure we can clear a empty entity manager.

            // now lets see if that entity is still there:
            Assert.Catch(typeof(ArgumentException), () =>
            {
                _entityManager.GetAllDataBlobsOfEntity(testEntity);  // should throw this time
            });
        }

        [Test]
        public void RemoveDataBlobs()
        {
            // a little setup:
            int testEntity = _entityManager.CreateEntity();
            _entityManager.SetDataBlob(testEntity, new PopulationDB(_pop1));

            Assert.IsTrue(_entityManager.GetDataBlob<PopulationDB>(testEntity) != null);  // check that it has the data blob
            _entityManager.RemoveDataBlob<PopulationDB>(testEntity);                     // Remove a data blob
            Assert.IsTrue(_entityManager.GetDataBlob<PopulationDB>(testEntity) == null); // now check that it doesn't

            // now lets try remove it again:
            _entityManager.RemoveDataBlob<PopulationDB>(testEntity); 

            // now lets try removal for an entity that does not exist:
            Assert.Catch(typeof(ArgumentException), () =>
            {
                _entityManager.RemoveDataBlob<PopulationDB>(42);  
            });

            // cannot remove baseDataBlobs, invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                _entityManager.RemoveDataBlob<BaseDataBlob>(testEntity);  
            });


            // reset:
            _entityManager.SetDataBlob(testEntity, new PopulationDB(_pop1));
            int typeIndex = _entityManager.GetTypeIndex<PopulationDB>();

            Assert.IsTrue(_entityManager.GetDataBlob<PopulationDB>(testEntity) != null);  // check that it has the data blob
            _entityManager.RemoveDataBlob(testEntity, typeIndex);              // Remove a data blob
            Assert.IsTrue(_entityManager.GetDataBlob<PopulationDB>(testEntity) == null); // now check that it doesn't

            // now lets try remove it again:
            _entityManager.RemoveDataBlob(testEntity, typeIndex);

            // now lets try an invlaid entity:
            Assert.Catch(typeof(ArgumentException), () =>
            {
                _entityManager.RemoveDataBlob(42, typeIndex);
            });

            // and an invalid typeIndex:
            Assert.Catch(typeof(ArgumentException), () =>
            {
                _entityManager.RemoveDataBlob(testEntity, -42);
            });
        }

        [Test]
        public void EntityLookup()
        {
            PopulateEntityManager();

            // Find all entities with a specific DataBlob.
            List<int> entities = _entityManager.GetAllEntitiesWithDataBlob<PopulationDB>();
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
            ComparableBitArray dataBlobMask = _entityManager.BlankDataBlobMask();
            dataBlobMask.Set(_entityManager.GetTypeIndex<PopulationDB>(), true);
            dataBlobMask.Set(_entityManager.GetTypeIndex<OrbitDB>(), true);
            entities = _entityManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
            Assert.AreEqual(2, entities.Count);

            // and with a mask that will not match any entities:
            dataBlobMask.Set(_entityManager.GetTypeIndex<AtmosphereDB>(), true);
            entities = _entityManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
            Assert.AreEqual(0, entities.Count);

            // and an empty mask:
            dataBlobMask = _entityManager.BlankDataBlobMask();
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
            int testEntity = _entityManager.GetFirstEntityWithDataBlob<PopulationDB>();
            Assert.AreEqual(0, testEntity);

            // lookup an entity that does not exist:
            testEntity = _entityManager.GetFirstEntityWithDataBlob<AtmosphereDB>();
            Assert.AreEqual(-1, testEntity);    

            // try again with incorrect type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                _entityManager.GetFirstEntityWithDataBlob<BaseDataBlob>();
            });


            // now lets just get the one entity, but use a different function to do it:
            int type = _entityManager.GetTypeIndex<PopulationDB>();
            testEntity = _entityManager.GetFirstEntityWithDataBlob(type);
            Assert.AreEqual(0, testEntity);

            // lookup an entity that does not exist:
            type = _entityManager.GetTypeIndex<AtmosphereDB>();
            testEntity = _entityManager.GetFirstEntityWithDataBlob(type);
            Assert.AreEqual(-1, testEntity);

            // try again with incorrect type index:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                _entityManager.GetFirstEntityWithDataBlob(4242);
            });
        }

        [Test]
        public void EntityValidity()
        {
            Guid entityGuid;
            EntityManager foundManager;
            int foundID;

            int testEntity = _entityManager.CreateEntity();
            Assert.IsTrue(_entityManager.IsValidEntity(testEntity));

            // Check Guid local lookup.
            Assert.IsTrue(_entityManager.TryGetGuidByEntity(testEntity, out entityGuid));

            // Check Guid local reverse lookup
            Assert.IsTrue(_entityManager.TryGetEntityByGuid(entityGuid, out foundID));
            Assert.AreEqual(testEntity, foundID);
            
            // Check Guid global lookup.
            Assert.IsTrue(EntityManager.FindEntityByGuid(entityGuid, out foundManager, out foundID));
            Assert.AreSame(_entityManager, foundManager);
            Assert.AreEqual(testEntity, foundID);

            // now test invalid input:
            Assert.IsFalse(_entityManager.IsValidEntity(-42));
            Assert.IsFalse(_entityManager.IsValidEntity(42));

            // and a removed entity:
            _entityManager.RemoveEntity(testEntity);
            Assert.IsFalse(_entityManager.IsValidEntity(testEntity));
        }

        [Test]
        public void EntityConvenienceFunctions()
        {
            PopulateEntityManager();

            Dictionary<int, Tuple<OrbitDB, PopulationDB>> entityDBMap = _entityManager.GetEntitiesAndDataBlobs<OrbitDB, PopulationDB>();

            // Make sure we found the rightn number of entities.
            Assert.AreEqual(2, entityDBMap.Count);

            foreach (KeyValuePair<int, Tuple<OrbitDB, PopulationDB>> entityMap in entityDBMap)
            {
                // Make sure we got the same datablobs.
                OrbitDB orbit = _entityManager.GetDataBlob<OrbitDB>(entityMap.Key);
                Assert.AreSame(orbit, entityMap.Value.Item1);

                PopulationDB position = _entityManager.GetDataBlob<PopulationDB>(entityMap.Key);
                Assert.AreSame(position, entityMap.Value.Item2);
            }
        }

        [Test]
        public void EntityTransfer()
        {
            EntityManager manager2 = new EntityManager();

            PopulateEntityManager();

            // Get an entity from the manager.
            int testEntity = _entityManager.GetFirstEntityWithDataBlob<OrbitDB>();
            // Store it's datablobs for later.
            List<BaseDataBlob> testEntityDataBlobs = _entityManager.GetAllDataBlobsOfEntity(testEntity);
            
            // Store the current GUID.
            Guid entityGuid;
            _entityManager.TryGetGuidByEntity(testEntity, out entityGuid);

            // Transfer the entity to a new EntityManager
            int transferredEntity = _entityManager.TransferEntity(testEntity, manager2);

            // Ensure the original manager no longer has the entity.
            Assert.IsFalse(_entityManager.IsValidEntity(testEntity));
            int entityByGuid;
            Assert.IsFalse(_entityManager.TryGetEntityByGuid(entityGuid, out entityByGuid));

            // Ensure the new manager has the entity.
            Assert.IsTrue(manager2.IsValidEntity(transferredEntity));
            Assert.IsTrue(manager2.TryGetEntityByGuid(entityGuid, out entityByGuid));
            Assert.AreEqual(transferredEntity, entityByGuid);

            // Get the transferredEntity's datablobs.
            List<BaseDataBlob> transferredEntityDataBlobs = manager2.GetAllDataBlobsOfEntity(transferredEntity);

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
        public void InvalidGuidLookups()
        {
            PopulateEntityManager();

            int trashEntity;
            Guid trashGuid;
            EntityManager trashManager;

            Assert.IsFalse(_entityManager.TryGetEntityByGuid(Guid.Empty, out trashEntity));
            Assert.IsFalse(_entityManager.TryGetGuidByEntity(-1, out trashGuid));
            Assert.IsFalse(EntityManager.FindEntityByGuid(Guid.Empty, out trashManager, out trashEntity));
        }

        [Test]
        public void TypeIndexTest()
        {
            int typeIndex;
            Assert.Catch<ArgumentNullException>(() => _entityManager.TryGetTypeIndex(null, out typeIndex));
            Assert.Catch<KeyNotFoundException>(() => _entityManager.GetTypeIndex<BaseDataBlob>());

            Assert.IsTrue(_entityManager.TryGetTypeIndex(typeof(OrbitDB), out typeIndex));
            Assert.AreEqual(_entityManager.GetTypeIndex<OrbitDB>(), typeIndex);
        }

        #region Extra Init Stuff

        /// <summary>
        /// This functions creates 3 entities with a total of 5 data blobs (3 orbits and 2 populations).
        /// </summary>
        /// <returns>It returns a reference to the first entity (containing 1 orbit and 1 pop)</returns>
        private int PopulateEntityManager()
        {
            // Clear out any previous test results.
            _entityManager.Clear();

            // Create an entity with individual DataBlobs.
            int testEntity = _entityManager.CreateEntity();
            _entityManager.SetDataBlob(testEntity, OrbitDB.FromStationary(5));
            _entityManager.SetDataBlob(testEntity, new PopulationDB(_pop1));

            // Create an entity with a DataBlobList.
            var dataBlobs = new List<BaseDataBlob> {OrbitDB.FromStationary(2)};
            _entityManager.CreateEntity(dataBlobs);

            // Create one more, just for kicks.
            dataBlobs = new List<BaseDataBlob> {OrbitDB.FromStationary(5), new PopulationDB(_pop2)};
            _entityManager.CreateEntity(dataBlobs);

            return testEntity;
        }

        #endregion

    }
}
