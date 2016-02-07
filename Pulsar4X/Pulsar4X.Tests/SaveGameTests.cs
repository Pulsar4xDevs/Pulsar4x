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
        private Game _game;
        private const string File = "./testSave.json";
        private const string File2 = "./testSave2.json";
        private readonly DateTime _testTime = DateTime.Now;

        [Test]
        public void TestSaveLoad()
        {
            // lets create a bad save game:

            // Check default nulls throw:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SerializationManager.ExportGame(null, File);
            });
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SerializationManager.ExportGame(_game, (string)null);
            }); 
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SerializationManager.ImportGame(null);
            });

            // check provided empty string throws:
            const string emptyString = "";
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SerializationManager.ExportGame(_game, emptyString);
            });
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                SerializationManager.ImportGame(emptyString);
            });


            CreateTestUniverse(10);

            // lets create a good saveGame
            SerializationManager.ExportGame(_game, File);

            Assert.IsTrue(System.IO.File.Exists(File));

            // now lets give ourselves a clean game:
            _game = null;

            //and load the saved data:
            _game = SerializationManager.ImportGame(File);

            Assert.AreEqual(10, _game.Systems.Count);
            Assert.AreEqual(_testTime, _game.CurrentDateTime);
            List<Entity> entities = _game.GlobalManager.GetAllEntitiesWithDataBlob<FactionInfoDB>();
            Assert.AreEqual(3, entities.Count);
            entities = _game.GlobalManager.GetAllEntitiesWithDataBlob<SpeciesDB>();
            Assert.AreEqual(2, entities.Count);

            // lets check the the refs were hocked back up:
            Entity species = _game.GlobalManager.GetFirstEntityWithDataBlob<SpeciesDB>();
            NameDB speciesName = species.GetDataBlob<NameDB>();
            Assert.AreSame(speciesName.OwningEntity, species);

            // <?TODO: Expand this out to cover many more DBs, entities, and cases.
        }

        [Test]
        public void SaveGameConsistency()
        {
            const int maxTries = 10;

            for (int numTries = 0; numTries < maxTries; numTries++)
            {
                CreateTestUniverse(10);
                SerializationManager.ExportGame(_game, File);
                _game = SerializationManager.ImportGame(File);
                SerializationManager.ExportGame(_game, File2);

                var fs1 = new FileStream(File, FileMode.Open);
                var fs2 = new FileStream(File2, FileMode.Open);

                if (fs1.Length == fs2.Length)
                {
                    // Read and compare a byte from each file until either a
                    // non-matching set of bytes is found or until the end of
                    // file1 is reached.
                    int file1Byte;
                    int file2Byte;
                    do
                    {
                        // Read one byte from each file.
                        file1Byte = fs1.ReadByte();
                        file2Byte = fs2.ReadByte();
                    } while ((file1Byte == file2Byte) && (file1Byte != -1));

                    // Close the files.
                    fs1.Close();
                    fs2.Close();

                    // Return the success of the comparison. "file1byte" is 
                    // equal to "file2byte" at this point only if the files are 
                    // the same.
                    if (file1Byte - file2Byte == 0)
                    {
                        Assert.Pass("Save Games consistent on try #" + (numTries + 1));
                    }
                }

                fs1.Close();
                fs2.Close();
            }
            Assert.Fail("SaveGameConsistency could not be verified. Please ensure saves are properly loading and saving.");
        }

        [Test]
        public void TestSingleSystemSave()
        {
            CreateTestUniverse(1);

            StarSystemFactory starsysfac = new StarSystemFactory(_game);
            StarSystem sol  = starsysfac.CreateSol(_game);
            StaticDataManager.ExportStaticData(sol, "./solsave.json");
        }

        private void CreateTestUniverse(int numSystems)
        {
            _game = Game.NewGame("Unit Test Game", _testTime, numSystems);

            // add a faction:
            Entity humanFaction = FactionFactory.CreateFaction(_game, "New Terran Utopian Empire");

            // add a species:
            Entity humanSpecies = SpeciesFactory.CreateSpeciesHuman(humanFaction, _game.GlobalManager);

            // add another faction:
            Entity greyAlienFaction = FactionFactory.CreateFaction(_game, "The Grey Empire");
            // Add another species:
            Entity greyAlienSpecies = SpeciesFactory.CreateSpeciesHuman(greyAlienFaction, _game.GlobalManager);

            // Greys Name the Humans.
            humanSpecies.GetDataBlob<NameDB>().SetName(greyAlienFaction, "Stupid Terrans");
            // Humans name the Greys.
            greyAlienSpecies.GetDataBlob<NameDB>().SetName(humanFaction, "Space bugs");

            //TODO Expand the "Test Universe" to cover more datablobs and entities. And ships. Etc.
        }
    }
}
