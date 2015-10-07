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
        private readonly DateTime testTime = DateTime.Now;

        [SetUp]
        public void Init()
        {
            game = Game.NewGame("Unit Test Game", testTime, 1);

            // add a faction:
            Entity humanFaction = FactionFactory.CreateFaction(game, "New Terran Utopian Empire");

            // add a species:
            Entity humanSpecies = SpeciesFactory.CreateSpeciesHuman(humanFaction, game.GlobalManager);

            // add another faction:
            Entity greyAlienFaction = FactionFactory.CreateFaction(game, "The Grey Empire");
            // Add another species:
            Entity greyAlienSpecies = SpeciesFactory.CreateSpeciesHuman(greyAlienFaction, game.GlobalManager);

            // Greys Name the Humans.
            humanSpecies.GetDataBlob<NameDB>().SetName(greyAlienFaction, "Stupid Terrans");
            // Humans name the Greys.
            greyAlienSpecies.GetDataBlob<NameDB>().SetName(humanFaction, "Space bugs");
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

            // Check default nulls throw:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SaveGame.Save(null, file);
            });
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SaveGame.Save(game, (string)null);
            }); 
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SaveGame.Load(null);
            });

            // check provided empty string throws:
            const string emptyString = "";
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SaveGame.Save(game, emptyString);
            });
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SaveGame.Load(emptyString);
            });

            // lets create a good saveGame
            SaveGame.Save(game, file);

            Assert.IsTrue(File.Exists(file));

            // now lets give ourselves a clean game:
            game = null;

            //and load the saved data:
            game = SaveGame.Load(file);

            Assert.AreEqual(1, game.Systems.Count);
            Assert.AreEqual(testTime, game.CurrentDateTime);
            List<Entity> entities = game.GlobalManager.GetAllEntitiesWithDataBlob<FactionInfoDB>();
            Assert.AreEqual(3, entities.Count);
            entities = game.GlobalManager.GetAllEntitiesWithDataBlob<SpeciesDB>();
            Assert.AreEqual(2, entities.Count);

            // lets check the the refs were hocked back up:
            Entity species = game.GlobalManager.GetFirstEntityWithDataBlob<SpeciesDB>();
            NameDB speciesName = species.GetDataBlob<NameDB>();
            Assert.AreSame(speciesName.OwningEntity, species);

            // <?TODO: Expand this out to cover many more DBs, entities, and cases.
        }

        [Test]
        public void TestSingleSystemSave()
        {
            StarSystemFactory starsysfac = new StarSystemFactory(game);
            StarSystem sol  = starsysfac.CreateSol(game);
            StaticDataManager.ExportStaticData(sol, "./solsave.json");
        }
    }
}
