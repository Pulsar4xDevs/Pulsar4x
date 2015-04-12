using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using System.IO;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Tests the game Save/Load system.")]
    class SaveGameTests
    {
        private Game game;
        private const string file = "./testSave.json";
        private DateTime testTime;

        [SetUp]
        public void Init()
        {
            game = new Game();

            // set date time:
            testTime = DateTime.Now;
            game.CurrentDateTime = testTime;

            // add a species:
            SpeciesDB speciesdb = new SpeciesDB("Human", 1.0, 0.5, 1.5, 1.0, 0.5, 1.5, 22, 0, 44);
            Entity speciesEntity = game.GlobalManager.CreateEntity();
            speciesEntity.SetDataBlob(speciesdb);

            // add a faction:
            var list = new List<BaseDataBlob>();
            Entity sdb = game.GlobalManager.CreateEntity(new List<BaseDataBlob> {speciesdb});
            var pop = new JDictionary<Entity, double> {{sdb, 42}};

            list.Add(new ColonyInfoDB(pop));
            list.Add(new PositionDB(0,0,0));
            list.Add(OrbitDB.FromStationary(0));
            Entity faction = game.GlobalManager.CreateEntity(list);

            // add a star system:
            game.StarSystems.Add(new StarSystem());
        }

        [TearDown]
        public void Cleanup()
        {
            // cleanup the test file:
            if (File.Exists(file))
            {
                //File.Delete(file); 
            }

            game = null;
        }

        [Test]
        public void TestSaveLoad()
        {
            // lets create a bad save game:
            SaveGame bad = new SaveGame();

            // Check default nulls throw:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                bad.Save();
            });
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                bad.Load();
            });

            // check provided empty string throws:
            const string emptyString = "";
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                bad.Save(emptyString);
            });
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                bad.Load(emptyString);
            });

            
            // lets create a good saveGame
            SaveGame save = new SaveGame(file);

            // now lets save the game:
            save.Save();
            Assert.IsTrue(File.Exists(file));

            // now lets give ourselves a clean game:
            game = new Game();

            //and load the saved data:
            save.Load();
            Assert.AreEqual(1, game.StarSystems.Count);
            Assert.AreEqual(testTime, game.CurrentDateTime);
            var entities = game.GlobalManager.GetAllEntitiesWithDataBlob<ColonyInfoDB>();
            Assert.AreEqual(1, entities.Count);

            // lets check the the refs were hocked back up:
            Entity faction = game.GlobalManager.GetFirstEntityWithDataBlob<ColonyInfoDB>();
            var colony = faction.GetDataBlob<ColonyInfoDB>();
            Assert.AreEqual(1, colony.Population.Count);
            foreach (var pop in colony.Population)
            {
                Assert.IsTrue(pop.Key.IsValid);

                SpeciesDB refDB = pop.Key.GetDataBlob<SpeciesDB>();
                Assert.IsNotNull(refDB);

                Assert.AreEqual(42, pop.Value);
                Assert.AreEqual("Human", refDB.SpeciesName);
                Assert.AreEqual(1.0, refDB.BaseGravity);
                Assert.AreEqual(1.0, refDB.BasePressure);
            }
        }
    }
}
