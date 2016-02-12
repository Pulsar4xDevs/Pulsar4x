using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using System.IO;
using System.Linq;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture]
    [Description("Tests the SerialzationManagers Import/Export capabilities.")]
    internal class SerializationManagerTests
    {
        private Game _game;
        private const string File = "./testSave.json";
        private const string File2 = "./testSave2.json";
        private readonly DateTime _testTime = DateTime.Now;

        [Test]
        public void GameImportExport()
        {
            // Nubmer of systems to generate for this test. Configurable.
            const int numSystems = 10;

            // lets create a bad save game:

            // Check default nulls throw:
            Assert.Catch<ArgumentNullException>(() => SerializationManager.ExportGame(null, File));
            Assert.Catch<ArgumentNullException>(() => SerializationManager.ExportGame(_game, (string)null));
            Assert.Catch<ArgumentNullException>(() => SerializationManager.ExportGame(_game, string.Empty));

            Assert.Catch<ArgumentNullException>(() => SerializationManager.ImportGame((string)null));
            Assert.Catch<ArgumentNullException>(() => SerializationManager.ImportGame(string.Empty));
            Assert.Catch<ArgumentNullException>(() => SerializationManager.ImportGame((Stream)null));

            if (_game == null)
                CreateTestUniverse(numSystems);
            Assert.NotNull(_game);

            // lets create a good saveGame
            SerializationManager.ExportGame(_game, File);

            Assert.IsTrue(System.IO.File.Exists(File));
            Console.WriteLine(Path.GetFullPath(File));
            // now lets give ourselves a clean game:
            _game = null;

            //and load the saved data:
            _game = SerializationManager.ImportGame(File);

            Assert.AreEqual(numSystems, _game.Systems.Count);
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
        public void EntityImportExport()
        {
            // Ensure we have a test universe.
            if (_game == null)
                CreateTestUniverse(10);
            Assert.NotNull(_game);

            // Choose a random system.
            var rand = new Random();
            int systemIndex = rand.Next(_game.Systems.Count - 1);
            StarSystem system = _game.Systems.Values.ToArray()[systemIndex];

            // Export/Reinport all entities in that system.
            EntityManager systemManager = system.SystemManager;
            for (int index = 0; index < systemManager.Entities.Count; index++)
            {
                Entity entity = systemManager.Entities[index];

                if (entity == null || !entity.IsValid)
                {
                    continue;
                }

                string jsonString = SerializationManager.ExportEntity(entity);

                // Clone the entity for later comparison.
                ProtoEntity clone = entity.Clone();

                // Destroy the entity.
                entity.Destroy();

                // Ensure the entity was destroyed.
                Entity foundEntity;
                Assert.IsFalse(systemManager.FindEntityByGuid(clone.Guid, out foundEntity));

                // Import the entity back into the manager.
                Entity importedEntity = SerializationManager.ImportEntity(_game, systemManager, jsonString);

                // Ensure the imported entity is valid
                Assert.IsTrue(importedEntity.IsValid);
                // Check to find the guid.
                Assert.IsTrue(systemManager.FindEntityByGuid(clone.Guid, out foundEntity));
                // Check the Guid imported correctly.
                Assert.AreEqual(clone.Guid, importedEntity.Guid);
                // Check the datablobs imported correctly.
                Assert.AreEqual(clone.DataBlobs.Where(dataBlob => dataBlob != null).ToList().Count, importedEntity.DataBlobs.Count);
                // Check the manager is the same.
                Assert.AreEqual(systemManager, importedEntity.Manager);
            }
        }

        [Test]
        public void StarSystemImportExport()
        {
            // Ensure we have a test universe.
            if (_game == null)
                CreateTestUniverse(10);
            Assert.NotNull(_game);

            // Choose a procedural system.
            var rand = new Random();
            int systemIndex = rand.Next(_game.Systems.Count - 1);
            StarSystem system = _game.Systems.Values.ToArray()[systemIndex];

            ImportExportSystem(system);

            //Now do the same thing, but with Sol.
            DefaultStartFactory.DefaultHumans(_game, "Humans");

            system = _game.Systems.Values.ToArray()[_game.Systems.Count - 1];
            ImportExportSystem(system);

        }
        private void ImportExportSystem(StarSystem system)
        {
            string jsonString = SerializationManager.ExportStarSystem(system);
            _game = Game.NewGame("StarSystem Import Test", DateTime.Now, 0);

            StarSystem importedSystem = SerializationManager.ImportStarSystem(_game, jsonString);
            Assert.AreEqual(system.Guid, importedSystem.Guid);

            // See that the entities were imported.
            Assert.AreEqual(system.SystemManager.Entities.Count, importedSystem.SystemManager.Entities.Count);

            // Ensure the system was added to the game's system list.
            Assert.AreEqual(1, _game.Systems.Count);
            Assert.IsTrue(_game.Systems.TryGetValue(system.Guid, out system));

            // Ensure the returned value references the same system as the game's system list
            Assert.AreEqual(importedSystem, system);
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
