using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using System.IO;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Factories;

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

            // add a faction:
            Entity humanFaction = FactionFactory.CreateFaction(game.GlobalManager, "New Terran Utopian Empire");

            // add a species:
            Entity humanSpecies = SpeciesFactory.CreateSpeciesHuman(humanFaction, game.GlobalManager);

            // add another faction:
            Entity greyAlienFaction = FactionFactory.CreateFaction(game.GlobalManager, "The Grey Empire");
            // Add another species:
            Entity greyAlienSpecies = SpeciesFactory.CreateSpeciesHuman(greyAlienFaction, game.GlobalManager);

            // Greys Name the Humans.
            humanSpecies.GetDataBlob<NameDB>().Name.Add(greyAlienFaction, "Stupid Terrans");
            // Humans name the Greys.
            greyAlienSpecies.GetDataBlob<NameDB>().Name.Add(humanFaction, "Space bugs");
            
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
                //Assert.AreEqual("Human", refDB.SpeciesName);
                Assert.AreEqual(1.0, refDB.BaseGravity);
                Assert.AreEqual(1.0, refDB.BasePressure);
            }
        }
    }
}
