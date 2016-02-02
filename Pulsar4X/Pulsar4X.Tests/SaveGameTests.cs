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
        private const string file2 = "./testSave2.json";
        private readonly DateTime testTime = DateTime.Now;

        [SetUp]
        public void Init()
        {
            game = Game.NewGame("Unit Test Game", testTime, 50);

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

            Assert.AreEqual(50, game.Systems.Count);
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
        public void SaveGameConsistency()
        {
            SaveGame.Save(game, file);
            game = SaveGame.Load(file);
            SaveGame.Save(game, file2);

            int file1byte;
            int file2byte;
            FileStream fs1 = new FileStream(file, FileMode.Open);
            FileStream fs2 = new FileStream(file2, FileMode.Open);

            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                Assert.Fail("Savegames are not the same size.");
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            if (file1byte - file2byte != 0)
            {
                Assert.Fail("Save files are different");
            }
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
