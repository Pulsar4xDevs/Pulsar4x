using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pulsar4X.Tests
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using NUnit.Framework;

    [TestFixture]
    [Description("Tests the SerialzationManagers Import/Export capabilities.")]
    internal class SerializationManagerTests
    {
        //private AuthenticationToken _smAuthToken;
        //private const string File = "testSave.json";
        //private const string File2 = "testSave2.json";
        private readonly DateTime _testTime = DateTime.Now;
        private string _testFilename = null;

        [SetUp]
        public void PerTestSetup() 
        {
            _testFilename = Path.GetTempFileName();
            if (File.Exists(_testFilename))
                File.Delete(_testFilename);
            string filePart = Path.ChangeExtension(Path.GetFileNameWithoutExtension(_testFilename), "json");
            _testFilename = Path.Combine(SerializationManager.GetWorkingDirectory(), filePart);
        }

        [TearDown]
        public void PerTestTearDown() 
        {
            if (File.Exists(_testFilename))
                File.Delete(_testFilename);
        }

        #region Check default nulls throw

        [Test]
        public void ExportingNullGameThrowsArgumentNullException()
        {
            Assert.Catch<ArgumentNullException>(() => SerializationManager.Export(null, _testFilename));
        }
        
        [Test]
        public void ExportingToNullFilenameThrowsArgumentException()
        {
            Assert.Catch<ArgumentException>(() => SerializationManager.Export(TestingUtilities.CreateTestUniverse(0), (string)null));
        }
        
        [Test]
        public void ExportingToEmptyFilenameThrowsArgumentException()
        {
            Assert.Catch<ArgumentException>(() => SerializationManager.Export(TestingUtilities.CreateTestUniverse(0), string.Empty));
        }
        
        [Test]
        public void ImportingNullFilenameThrowsArgumentException()
        {
            Assert.Catch<ArgumentException>(() => SerializationManager.ImportGame((string)null));
        }
        
        [Test]
        public void ImportingEmptyFilenameThrowsArgumentException()
        {
            Assert.Catch<ArgumentException>(() => SerializationManager.ImportGame(string.Empty));
        }

        [Test]
        public void ImportingNullStreamThrowsArgumentNullException()
        {
            Assert.Catch<ArgumentNullException>(() => SerializationManager.ImportGame((Stream)null));
        }

        #endregion

        #region Bad save game tests

        //TODO

        #endregion

        /// <param name="numSystems">Nubmer of systems to generate for this test. Configurable.</param>
        /// <param name="generateSol">Whether Sol is in the game, in addition to any generated systems. Configurable.</param>
        [TestCase(10, false)]
        public void ExportCreatesFile(int numSystems, bool generateSol)
        {
            Game game = TestingUtilities.CreateTestUniverse(numSystems, _testTime, generateSol);

            SerializationManager.Export(game, _testFilename);

            Assert.IsTrue(File.Exists(_testFilename));
            Assert.IsFalse(new FileInfo(_testFilename).Length == 0);
        }

        /// <param name="numSystems">Nubmer of systems to generate for this test. Configurable.</param>
        /// <param name="generateSol">Whether Sol is in the game, in addition to any generated systems. Configurable.</param>
        [TestCase(10, false)]
        public void ExportedFileCanBeImported(int numSystems, bool generateSol)
        {
            Game game = TestingUtilities.CreateTestUniverse(numSystems, _testTime, generateSol);

            SerializationManager.Export(game, _testFilename);
            
            Assert.NotNull(SerializationManager.ImportGame(_testFilename));
        }

        #region Imported Game is same as exported Game

        /// <param name="numSystems">Nubmer of systems to generate for this test. Configurable.</param>
        /// <param name="generateSol">Whether Sol is in the game, in addition to any generated systems. Configurable.</param>
        [TestCase(10, false)]
        public void ExportImportPreservesSMAuthToken(int numSystems, bool generateSol)
        {
            Game exportedGame = TestingUtilities.CreateTestUniverse(numSystems, _testTime, generateSol);
            AuthenticationToken exportedAuthToken = new AuthenticationToken(exportedGame.SpaceMaster);
            SerializationManager.Export(exportedGame, _testFilename);

            Game importedGame = SerializationManager.ImportGame(_testFilename);
            AuthenticationToken importedAuthToken = new AuthenticationToken(importedGame.SpaceMaster);

            Assert.AreEqual(exportedAuthToken.PlayerID, importedAuthToken.PlayerID);
            Assert.AreEqual(exportedAuthToken.Password, importedAuthToken.Password);
        }

        /// <param name="numSystems">Nubmer of systems to generate for this test. Configurable.</param>
        /// <param name="generateSol">Whether Sol is in the game, in addition to any generated systems. Configurable.</param>
        [TestCase(10, false)]
        public void ExportImportPreservesTotalSystemCount(int numSystems, bool generateSol)
        {
            Game exportedGame = TestingUtilities.CreateTestUniverse(numSystems, _testTime, generateSol);
            AuthenticationToken smAuthToken = new AuthenticationToken(exportedGame.SpaceMaster);
            SerializationManager.Export(exportedGame, _testFilename);

            Game importedGame = SerializationManager.ImportGame(_testFilename);

            Assert.AreEqual(exportedGame.GetSystems(smAuthToken).Count, importedGame.GetSystems(smAuthToken).Count);
        }

        /// <param name="numSystems">Nubmer of systems to generate for this test. Configurable.</param>
        /// <param name="generateSol">Whether Sol is in the game, in addition to any generated systems. Configurable.</param>
        [TestCase(10, false)]
        public void ExportImportPreservesGameTime(int numSystems, bool generateSol)
        {
            Game exportedGame = TestingUtilities.CreateTestUniverse(numSystems, _testTime, generateSol);
            SerializationManager.Export(exportedGame, _testFilename);

            Game gameToChangeTime = TestingUtilities.CreateTestUniverse(0, DateTime.UnixEpoch, false);
            Assert.AreNotEqual(_testTime, StaticRefLib.CurrentDateTime);

            Game importedGame = SerializationManager.ImportGame(_testFilename);
            Assert.AreEqual(_testTime, StaticRefLib.CurrentDateTime);
        }

        /// <param name="numSystems">Nubmer of systems to generate for this test. Configurable.</param>
        /// <param name="generateSol">Whether Sol is in the game, in addition to any generated systems. Configurable.</param>
        [TestCase(10, false)]
        public void ExportImportPreservesFactionCount(int numSystems, bool generateSol)
        {
            Game exportedGame = TestingUtilities.CreateTestUniverse(numSystems, _testTime, generateSol);
            int exportedCount = exportedGame.GlobalManager.GetAllEntitiesWithDataBlob<FactionInfoDB>(new AuthenticationToken(exportedGame.SpaceMaster)).Count;
            SerializationManager.Export(exportedGame, _testFilename);
            
            Game importedGame = SerializationManager.ImportGame(_testFilename);
            int importedCount = importedGame.GlobalManager.GetAllEntitiesWithDataBlob<FactionInfoDB>(new AuthenticationToken(importedGame.SpaceMaster)).Count;

            Assert.AreEqual(exportedCount, importedCount);
        }

        /// <param name="numSystems">Nubmer of systems to generate for this test. Configurable.</param>
        /// <param name="generateSol">Whether Sol is in the game, in addition to any generated systems. Configurable.</param>
        [TestCase(10, false)]
        public void ExportImportPreservesSpeciesCount(int numSystems, bool generateSol)
        {
            Game exportedGame = TestingUtilities.CreateTestUniverse(numSystems, _testTime, generateSol);
            int exportedCount = exportedGame.GlobalManager.GetAllEntitiesWithDataBlob<SpeciesDB>(new AuthenticationToken(exportedGame.SpaceMaster)).Count;
            SerializationManager.Export(exportedGame, _testFilename);
            
            Game importedGame = SerializationManager.ImportGame(_testFilename);
            int importedCount = importedGame.GlobalManager.GetAllEntitiesWithDataBlob<SpeciesDB>(new AuthenticationToken(importedGame.SpaceMaster)).Count;

            Assert.AreEqual(exportedCount, importedCount);
        }

        // <?TODO: Expand this region out to cover many more DBs, entities, and cases.

        #endregion Imported Game is same as exported Game

        #region Imported Games are Valid Games

        /// <param name="numSystems">Nubmer of systems to generate for this test. Configurable.</param>
        /// <param name="generateSol">Whether Sol is in the game, in addition to any generated systems. Configurable.</param>
        [TestCase(10, false)]
        public void ExportImportSpeciesAndNameBiderectionally(int numSystems, bool generateSol)
        {
            Game exportedGame = TestingUtilities.CreateTestUniverse(numSystems, _testTime, generateSol);
            SerializationManager.Export(exportedGame, _testFilename);
            
            Game importedGame = SerializationManager.ImportGame(_testFilename);
            Entity species = importedGame.GlobalManager.GetFirstEntityWithDataBlob<SpeciesDB>(new AuthenticationToken(importedGame.SpaceMaster));
            NameDB speciesName = species.GetDataBlob<NameDB>();
            
            Assert.AreSame(speciesName.OwningEntity, species);
        }

        // <?TODO: Expand this region out to cover many more DBs, entities, and cases.

        #endregion Imported Games are Valid Games

        [Test]
        public void CompareLoadedGameWithOriginal() //TODO do this after a few game ticks and check the comparitiveTests again. 
        {
            //create a new game
            Game newGame = TestingUtilities.CreateTestUniverse(10, _testTime, true);

            Entity ship = newGame.GlobalManager.GetFirstEntityWithDataBlob<TransitableDB>();
            StarSystem firstSystem = newGame.Systems.First().Value;
            DateTime jumpTime = StaticRefLib.CurrentDateTime + TimeSpan.FromMinutes(1);

            //insert a jump so that we can compair timeloop dictionary
            InterSystemJumpProcessor.SetJump(newGame, jumpTime,  firstSystem, jumpTime, ship);

            // lets create a good saveGame
            SerializationManager.Export(newGame, _testFilename);
            //then load it:
            Game loadedGame = SerializationManager.ImportGame(_testFilename);

            //run some tests
            ComparitiveTests(newGame, loadedGame);
        }


        void ComparitiveTests(Game original, Game loadedGame)
        {
            StarSystem firstOriginal = original.Systems.First().Value;
            StarSystem firstLoaded = loadedGame.Systems.First().Value;

            Assert.AreEqual(firstOriginal.Guid, firstLoaded.Guid);
            Assert.AreEqual(firstOriginal.NameDB.DefaultName, firstLoaded.NameDB.DefaultName);
  
            Assert.AreEqual(original.GamePulse, loadedGame.GamePulse);

            Assert.AreEqual(firstOriginal.ManagerSubpulses.GetTotalNumberOfProceses(), firstLoaded.ManagerSubpulses.GetTotalNumberOfProceses());
            Assert.AreEqual(firstOriginal.ManagerSubpulses.GetInteruptDateTimes(), firstLoaded.ManagerSubpulses.GetInteruptDateTimes());

            Assert.AreEqual(original.GlobalManager.NumberOfGlobalEntites, loadedGame.GlobalManager.NumberOfGlobalEntites);

            var originalEntitiesList = original.GlobalManager.Entities.Where(x => x != null).ToList();
            var loadedEntitiesList = loadedGame.GlobalManager.Entities.Where(x => x != null).ToList();

            Assert.AreEqual(originalEntitiesList.Count, loadedEntitiesList.Count);
            Assert.AreEqual(originalEntitiesList.Select(x => x.Guid).OrderBy(x => x).ToList(), loadedEntitiesList.Select(x => x.Guid).OrderBy(x => x).ToList());
        }


        [Test]
        public void EntityImportExport()
        {
            // Ensure we have a test universe.
            Game game = TestingUtilities.CreateTestUniverse(10);
            var smAuthToken = new AuthenticationToken(game.SpaceMaster);

            Assert.NotNull(game);

            // Choose a random system.
            var rand = new Random();
            List<StarSystem> systems = game.GetSystems(smAuthToken);
            int systemIndex = rand.Next(systems.Count - 1);
            StarSystem system = systems[systemIndex];

            // Export/Reinport all system bodies in that system.

            foreach (Entity entity in system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(smAuthToken))
            {
                string jsonString = SerializationManager.Export(game, entity);

                // Clone the entity for later comparison.
                ProtoEntity clone = entity.Clone();

                // Destroy the entity.
                entity.Destroy();

                // Ensure the entity was destroyed.
                Entity foundEntity;
                Assert.IsFalse(system.FindEntityByGuid(clone.Guid, out foundEntity));

                // Import the entity back into the manager.
                Entity importedEntity = SerializationManager.ImportEntityJson(game, jsonString, system);

                // Ensure the imported entity is valid
                Assert.IsTrue(importedEntity.IsValid);
                // Check to find the guid.
                Assert.IsTrue(system.FindEntityByGuid(clone.Guid, out foundEntity));
                // Check the ID imported correctly.
                Assert.AreEqual(clone.Guid, importedEntity.Guid);
                // Check the datablobs imported correctly.
                Assert.AreEqual(clone.DataBlobs.Where(dataBlob => dataBlob != null).ToList().Count, importedEntity.DataBlobs.Count);
                // Check the manager is the same.
                Assert.AreEqual(system, importedEntity.Manager);
            }
        }

        [Test]
        public void TestProceduralStarSystemImportExport()
        {
            Game game = TestingUtilities.CreateTestUniverse(1);
            var smAuthToken = new AuthenticationToken(game.SpaceMaster);

            Assert.NotNull(game);

            // Test each procedural system.
            List<StarSystem> systems = game.GetSystems(smAuthToken);
            ImportExportSystem(systems.First(), "Procedural");
        }

        [Test]
        public void SolStarSystemImportExport()
        {
            Game game = TestingUtilities.CreateTestUniverse(1, true);
            var smAuthToken = new AuthenticationToken(game.SpaceMaster);

            Assert.NotNull(game);

            //Test with Sol.
            List<StarSystem> systems = game.GetSystems(smAuthToken);
            ImportExportSystem(systems.First(), "Sol");
        }

        private void ImportExportSystem(StarSystem system, string testFileSuffix = "")
        {
            Game game = TestingUtilities.CreateTestUniverse(1, true);
            var smAuthToken = new AuthenticationToken(game.SpaceMaster);

            //TODO: need to be able to export a system that will only save systemBodies. 
            //for a generated one, just saving the seed should suffice. 
            //for a created one we'll have to do more. 
            //also need to be able to export a players view of a system. but that would likely be a different function altogether. 
            var filename = "testSystemExport" + testFileSuffix + ".json";
            SerializationManager.Export(game, filename, system);
            string jsonString = SerializationManager.Export(game, system);

            int entityCount = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(smAuthToken).Count;
            int nameCount = system.GetAllEntitiesWithDataBlob<NameDB>(smAuthToken).Count;
            int orbitCount = system.GetAllEntitiesWithDataBlob<OrbitDB>(smAuthToken).Count;

            game = TestingUtilities.CreateTestUniverse(0);
            smAuthToken = new AuthenticationToken(game.SpaceMaster);

            StarSystem importedSystem = SerializationManager.ImportSystemJson(game, jsonString);
            Assert.AreEqual(system.Guid, importedSystem.Guid);

            // See that the entities were imported.
            Assert.AreEqual(entityCount, importedSystem.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(smAuthToken).Count);
            Assert.AreEqual(nameCount, importedSystem.GetAllEntitiesWithDataBlob<NameDB>(smAuthToken).Count);
            Assert.AreEqual(orbitCount, importedSystem.GetAllEntitiesWithDataBlob<OrbitDB>(smAuthToken).Count);

            // Ensure the system was added to the game's system list.
            List<StarSystem> systems = game.GetSystems(smAuthToken);
            Assert.AreEqual(1, systems.Count);

            // Ensure the returned value references the same system as the game's system list
            system = game.GetSystem(smAuthToken, system.Guid);
            Assert.AreEqual(importedSystem, system);
        }

        /// <summary>
        /// apears to test two saves to confirm that they are the same
        /// </summary>
        [Test]        
        public void SaveGameConsistency()
        {
            const int maxTries = 10;
            string testFilename2 = null; 
            try
            {
                testFilename2 = Path.GetTempFileName();
                if (File.Exists(testFilename2))
                    File.Delete(testFilename2);
                string filePart = Path.ChangeExtension(Path.GetFileNameWithoutExtension(testFilename2), "json");
                testFilename2 = Path.Combine(SerializationManager.GetWorkingDirectory(), filePart);

                for (int numTries = 0; numTries < maxTries; numTries++)
                {
                    Game _game = TestingUtilities.CreateTestUniverse(10);
                    SerializationManager.Export(_game, _testFilename);
                    _game = SerializationManager.ImportGame(_testFilename);
                    SerializationManager.Export(_game, testFilename2);

                    var fs1 = new FileStream(Path.Combine(SerializationManager.GetWorkingDirectory(), _testFilename), FileMode.Open);
                    var fs2 = new FileStream(Path.Combine(SerializationManager.GetWorkingDirectory(), testFilename2), FileMode.Open);

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
            finally
            {
                if (File.Exists(testFilename2))
                    File.Delete(testFilename2);
            }
        }

        [Test]
        public void TestSingleSystemSave()
        {
            Game game = TestingUtilities.CreateTestUniverse(1);
            var smAuthToken = new AuthenticationToken(game.SpaceMaster);

            StarSystemFactory starsysfac = new StarSystemFactory(game);
            StarSystem sol  = starsysfac.CreateSol(game);
            StaticDataManager.ExportStaticData(sol, "solsave.json");
        }




    }
}
