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

    [TestFixture, Description("Basic Tests for the Main Game Loop.")]
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

            // add a faction:
            List<BaseDataBlob> list = new List<BaseDataBlob>();
            SpeciesDB sdb = new SpeciesDB("Human", 1.0, 0.5, 1.5, 1.0, 0.5, 1.5, 22, 0, 44);
            JDictionary<SpeciesDB, double> pop = new JDictionary<SpeciesDB, double>();
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
        }
    }
}
