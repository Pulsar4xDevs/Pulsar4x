using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.DataBlobs;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using NUnit.Framework.Constraints;

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
            int speciesEntity = game.GlobalManager.CreateEntity();
            game.GlobalManager.SetDataBlob(speciesEntity, speciesdb);

            // add a faction:
            List<BaseDataBlob> list = new List<BaseDataBlob>();
            DataBlobRef<SpeciesDB> sdb = new DataBlobRef<SpeciesDB>(speciesdb);
            JDictionary<DataBlobRef<SpeciesDB>, double> pop = new JDictionary<DataBlobRef<SpeciesDB>, double>();
            pop.Add(sdb, 42);

            list.Add(new ColonyInfoDB(pop));
            list.Add(new PositionDB(0,0));
            //list.Add(OrbitDB.FromStationary(0));
            int factionID = game.GlobalManager.CreateEntity(list);

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
            int faction = game.GlobalManager.GetFirstEntityWithDataBlob<ColonyInfoDB>();
            var colony = game.GlobalManager.GetDataBlob<ColonyInfoDB>(faction);
            Assert.AreEqual(1, colony.Population.Count);
            foreach (var pop in colony.Population)
            {
                Assert.IsNotNull(pop.Key);
                Assert.AreEqual(42, pop.Value);
                Assert.IsNotNull(pop.Key.Ref);
                Assert.AreEqual("Human", pop.Key.Ref.SpeciesName);
                Assert.AreEqual(1.0, pop.Key.Ref.BaseGravity);
                Assert.AreEqual(1.0, pop.Key.Ref.BasePressure);
            }
        }
    }
}
