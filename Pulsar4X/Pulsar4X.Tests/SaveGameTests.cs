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
            Dictionary<SpeciesDB, double> pop = new Dictionary<SpeciesDB, double>();
            pop.Add(sdb, 42);

            list.Add(new PopulationDB(pop));
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
            // lets check some invalid strings first:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SaveGame invalidSave = new SaveGame(null);
            });

            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                const string testStr = "";
                SaveGame invalidSave = new SaveGame(testStr);
            });

            // now lets create a good saveGame
            SaveGame save = new SaveGame(file);

            // now lets try to write those bad string again:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                save.File = null;
            });

            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                const string testStr = "";
                save.File = testStr;
            });

            // now lets try to save and load while providing bad strings:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                const string testStr = "";
                save.Load(testStr);
            });

            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                const string testStr = "";
                save.Save(testStr);
            });

            // now lets save the game:
            save.Save();
            Assert.IsTrue(File.Exists(file));

            // now lets give ourselves a clean game:
            game = new Game();

            //and load the saved data:
            save.Load();
            var entities = game.GlobalManager.GetAllEntitiesWithDataBlob<PopulationDB>();
            Assert.AreEqual(1, entities.Count);
            Assert.AreEqual(1, game.StarSystems.Count);
            Assert.AreEqual(testTime, game.CurrentDateTime);
        }
    }
}
