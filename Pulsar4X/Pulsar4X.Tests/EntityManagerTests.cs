using NUnit.Framework;
using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Modding;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Entity Manager Tests")]
    class EntityManagerTests
    {
        private Game _game;
        private AuthenticationToken _smAuthToken;
        private Entity _species1;
        private Dictionary<int, long> _pop1;
        private Dictionary<int, long> _pop2;

        [SetUp]
        public void Init()
        {
            var settings = new NewGameSettings {GameName = "Test Game", StartDateTime = DateTime.Now, MaxSystems = 1};

            var _modLoader = new ModLoader();
            var _modDataStore = new ModDataStore();

            _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);

            _game  = new Game(settings, _modDataStore);

            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
            //_game.GenerateSystems(_smAuthToken, 1);
            _species1 = Entity.Create();
            _game.GlobalManager.AddEntity(_species1, new List<BaseDataBlob> {new SpeciesDB(1, 0.1, 1.9, 1.0, 0.4, 4, 14, -15, 45)});
            _pop1 = new Dictionary<int, long> { { _species1.Id, 10 } };
            _pop2 = new Dictionary<int, long> { { _species1.Id, 5 } };
        }


        // [Test]
        // public void TestSelfReferencingEntity()
        // {
        //     NameDB name = new NameDB();
        //     Entity testEntity = Entity.Create();
        //     _game.GlobalManager.AddEntity(testEntity);
        //     name.SetName(testEntity.FactionOwnerID, "TestName");
        //     testEntity.SetDataBlob(name);

        //     //serialise the test entity into a mem stream
        //     var mStream = new MemoryStream();
        //     SerializationManager.Export(_game, mStream, testEntity);


        //     //create a second game, we're going to import this entity to here (this would be the case in a network game)
        //     var settings = new NewGameSettings { GameName = "Test Game2", StartDateTime = DateTime.Now, MaxSystems = 1 };
        //     Game game2 = new Game(settings);


        //     //import the entity into the second game.
        //     Entity clonedEntity = SerializationManager.ImportEntity(game2, mStream, game2.GlobalManager);
        //     mStream.Close();

        //     Assert.IsTrue(testEntity.GetValueCompareHash() == clonedEntity.GetValueCompareHash(), "ValueCompareHash should match");//currently valueCompareHash does not check guid of the entity. I'm undecided wheather it should or not.
        //     Entity clonedTest;
        //     Assert.IsTrue(game2.GlobalManager.FindEntityByGuid(testEntity.Guid, out clonedTest), "Game2 should have the test entity");
        //     Assert.IsTrue(testEntity.Guid == clonedEntity.Guid, "ID's need to match, if we get to this assert, then we've got two entities in game2, one of them has the correct guid but no datablobs, the other has a new guid but is complete.");
        //     Assert.IsTrue(ReferenceEquals(clonedTest, clonedEntity),"These should be the same object" );
        //     Assert.IsTrue(testEntity.DataBlobs.Count == clonedTest.DataBlobs.Count);
        //     Assert.IsTrue(testEntity.DataBlobs.Count == clonedEntity.DataBlobs.Count);
        // }


        [Test]
        public void CreateEntity()
        {
            // create entity with no data blobs:
            Entity testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity);
            Assert.IsTrue(testEntity.IsValid);
            Assert.AreSame(_game.GlobalManager, testEntity.Manager);

            // Create entity with existing datablobs:
            var dataBlobs = new List<BaseDataBlob> {new OrbitDB(), new ColonyInfoDB(_pop1, Entity.InvalidEntity)};
            testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity, dataBlobs);
            Assert.IsTrue(testEntity.IsValid);

            // Create entity with existing datablobs, but provide an empty list:
            dataBlobs.Clear();
            testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity, dataBlobs);
            Assert.IsTrue(testEntity.IsValid);
        }

        [Test]
        public void SetDataBlobs()
        {
            Entity testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity);
            testEntity.SetDataBlob(new OrbitDB());
            testEntity.SetDataBlob(new ColonyInfoDB(_pop1, Entity.InvalidEntity));

            // test bad input:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                testEntity.SetDataBlob((OrbitDB)null); // should throw ArgumentNullException
            });
        }

        [Test]
        public void GetDataBlobsByEntity()
        {
            Entity testEntity = PopulateEntityManager();  // make sure we have something in there.

            // Get all DataBlobs of a specific entity.
            var dataBlobs = _game.GlobalManager.GetAllDataBlobsForEntity(testEntity.Id);
            Assert.AreEqual(2, dataBlobs.Count);

            // empty entity mean empty list.
            testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity);  // create empty entity.
            dataBlobs = _game.GlobalManager.GetAllDataBlobsForEntity(testEntity.Id);
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
        }

        [Test]
        public void RemoveEntities()
        {
            Entity testEntity = PopulateEntityManager();

            // lets check the entity at index testEntity
            List<BaseDataBlob> testList = _game.GlobalManager.GetAllDataBlobsForEntity(testEntity.Id);
            Assert.AreEqual(2, testList.Count);  // should have 2 datablobs.

            // Remove an entity.
            testEntity.Destroy();

            // now lets see if the entity is still there:
            testList = _game.GlobalManager.GetAllDataBlobsForEntity(testEntity.Id);
            Assert.AreEqual(0, testList.Count);  // should have 0 datablobs.

            Assert.IsFalse(testEntity.IsValid);

            // Now try to remove the entity. Again.
            Assert.Catch<InvalidOperationException>(testEntity.Destroy);
        }

        [Test]
        public void RemoveDataBlobs()
        {
            // a little setup:
            Entity testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity);
            testEntity.SetDataBlob(new ColonyInfoDB(_pop1, Entity.InvalidEntity));

            Assert.IsTrue(testEntity.GetDataBlob<ColonyInfoDB>() != null);  // check that it has the data blob
            testEntity.RemoveDataBlob<ColonyInfoDB>();                     // Remove a data blob
            Assert.IsTrue(testEntity.GetDataBlob<ColonyInfoDB>() == null); // now check that it doesn't

            // now lets try remove it again:
            Assert.Catch<InvalidOperationException>(testEntity.RemoveDataBlob<ColonyInfoDB>);

            // cannot remove baseDataBlobs, invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                testEntity.RemoveDataBlob<BaseDataBlob>();
            });


            // reset:
            // testEntity.SetDataBlob(new ColonyInfoDB(_pop1, Entity.InvalidEntity));
            // int typeIndex = EntityManager.GetTypeIndex<ColonyInfoDB>();

            // Assert.IsTrue(testEntity.GetDataBlob<ColonyInfoDB>() != null);  // check that it has the data blob
            // testEntity.RemoveDataBlob(typeIndex);              // Remove a data blob
            // Assert.IsTrue(testEntity.GetDataBlob<ColonyInfoDB>() == null); // now check that it doesn't

            // // now lets try remove it again:
            // Assert.Catch<InvalidOperationException>(() => testEntity.RemoveDataBlob(typeIndex));

            // // and an invalid typeIndex:
            // Assert.Catch(typeof(ArgumentException), () => testEntity.RemoveDataBlob(-42));

            // // now lets try an invalid entity:
            // testEntity.Destroy();
            // Assert.Catch<InvalidOperationException>(() => testEntity.RemoveDataBlob(typeIndex));

        }

        [Test]
        public void EntityLookup()
        {
            PopulateEntityManager();

            // Find all entities with a specific DataBlob.
            List<Entity> entities = _game.GlobalManager.GetAllEntitiesWithDataBlob<ColonyInfoDB>(_smAuthToken);
            Assert.AreEqual(2, entities.Count);

            // again, but look for a datablob that no entity has:
            entities = _game.GlobalManager.GetAllEntitiesWithDataBlob<AtmosphereDB>(_smAuthToken);
            Assert.AreEqual(0, entities.Count);

            // check with invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () => _game.GlobalManager.GetAllEntitiesWithDataBlob<BaseDataBlob>(_smAuthToken));


            // now lets just get the one entity:
            Entity testEntity = _game.GlobalManager.GetFirstEntityWithDataBlob<ColonyInfoDB>();
            Assert.IsTrue(testEntity.IsValid);

            // lookup an entity that does not exist:
            testEntity = _game.GlobalManager.GetFirstEntityWithDataBlob<AtmosphereDB>();
            Assert.IsFalse(testEntity.IsValid);

            // try again with incorrect type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                _game.GlobalManager.GetFirstEntityWithDataBlob<BaseDataBlob>();
            });
        }

        [Test]
        public void EntityGuid()
        {
            Entity foundEntity;
            Entity testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity);

            Assert.IsTrue(testEntity.IsValid);
            // Check ID local lookup.
            Assert.IsTrue(_game.GlobalManager.TryGetEntityById(testEntity.Id, out foundEntity));
            Assert.IsTrue(testEntity == foundEntity);

            // Check ID global lookup.
            Assert.IsTrue(_game.GlobalManager.TryGetGlobalEntityById(testEntity.Id, out foundEntity));
            Assert.AreEqual(testEntity, foundEntity);

            // and a removed entity:
            testEntity.Destroy();
            Assert.IsFalse(testEntity.IsValid);

            // Check bad ID lookups.
            Assert.IsFalse(_game.GlobalManager.TryGetEntityById(testEntity.Id, out foundEntity));
            Assert.IsFalse(_game.GlobalManager.TryGetGlobalEntityById(testEntity.Id, out foundEntity));
        }

        [Test]
        public void EntityTransfer()
        {
            EntityManager manager2 = _game.Systems.Last();
            Entity testEntity = PopulateEntityManager();

            // Ensure we got a valid entity.
            Assert.IsTrue(testEntity.IsValid);
            // Store it's datablobs for later.
            List<BaseDataBlob> testEntityDataBlobs = _game.GlobalManager.GetAllDataBlobsForEntity(testEntity.Id);

            // Transfer the entity to a Entity.CreateManager
            manager2.Transfer(testEntity);

            // Ensure the original manager no longer has the entity.
            Entity foundEntity;
            Assert.IsFalse(_game.GlobalManager.TryGetEntityById(testEntity.Id, out foundEntity));

            // Ensure the new manager has the entity.
            Assert.IsTrue(testEntity.Manager == manager2);
            Assert.IsTrue(manager2.TryGetEntityById(testEntity.Id, out foundEntity));
            Assert.AreSame(testEntity, foundEntity);

            // Get the transferredEntity's datablobs.
            List<BaseDataBlob> transferredEntityDataBlobs = manager2.GetAllDataBlobsForEntity(testEntity.Id);

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

            // Try to transfer an invalid entity.
            testEntity.Destroy();
        }

        [Test]
        public void HasDataBlobTests()
        {
            Entity testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity);
            testEntity.SetDataBlob(new OrbitDB());
            testEntity.SetDataBlob(new ColonyInfoDB(_pop1, Entity.InvalidEntity));

            Assert.True(testEntity.HasDataBlob<OrbitDB>(), "This entity should have an OrbitDB");
            Assert.False(testEntity.HasDataBlob<VolumeStorageDB>(), "This entity should NOT have a VolumeStorageDB");
        }

        #region Extra Init Stuff

        /// <summary>
        /// This functions creates 3 entities with a total of 5 data blobs (3 orbits and 2 populations).
        /// </summary>
        /// <returns>It returns a reference to the first entity (containing 1 orbit and 1 pop)</returns>
        private Entity PopulateEntityManager()
        {
            // Clear out any previous test results.
            //_game.GlobalManager.Clear();

            // Create an entity with individual DataBlobs.
            Entity testEntity = Entity.Create();
            _game.GlobalManager.AddEntity(testEntity);
            testEntity.SetDataBlob(new OrbitDB());
            testEntity.SetDataBlob(new ColonyInfoDB(_pop1, Entity.InvalidEntity));

            // Create an entity with a DataBlobList.
            var dataBlobs = new List<BaseDataBlob> { new OrbitDB() };
            var entity = Entity.Create();
            _game.GlobalManager.AddEntity(entity, dataBlobs);

            // Create one more, just for kicks.
            dataBlobs = new List<BaseDataBlob> { new OrbitDB(), new ColonyInfoDB(_pop2, Entity.InvalidEntity) };
            entity = Entity.Create();
            _game.GlobalManager.AddEntity(entity, dataBlobs);

            return testEntity;
        }

        #endregion

    }
}
