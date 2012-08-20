using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pulsar4X.Stargen;
using Pulsar4X.Storage;
using Pulsar4X.Entities;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class StorageTests
    {
        private GameState _gameState;
        private CreateDatabase _createDatabase;

        private string _appPath;
        private string _saveFolder;
        private const string SAVE_GAME_FILE_NAME = "TestGame.db";

        [SetUp]
        public void TestSetup()
        {
            _gameState = new GameState();
            _gameState.Name = "Test Game";
            _gameState.Factions = new ObservableCollection<Faction>();
            _gameState.StarSystems = new ObservableCollection<StarSystem>();
            _gameState.Stars = new ObservableCollection<Star>();
            _gameState.Planets = new ObservableCollection<Planet>();

            var species = new Species { Id = Guid.NewGuid(), Name = "Test Humans" };
            var theme = new Theme { Id = Guid.NewGuid(), Name = "Test Theme" };
            _gameState.Factions.Add(new Faction { Id = Guid.NewGuid(), Name = "Test Faction", Species = species, SpeciesId = species.Id, Title = "Mighty Humans", Theme = theme, ThemeId = theme.Id });

            var ssf = new StarSystemFactory(true);
            var ss = ssf.Create("Test Sol");
            _gameState.StarSystems.Add(ss);
            ss.Stars.ToList().ForEach(x => _gameState.Stars.Add(x));
            ss.Stars.ToList().SelectMany(x => x.Planets).ToList().ForEach(p => _gameState.Planets.Add(p));

            UriBuilder uri = new UriBuilder(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            _appPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            _saveFolder = Path.Combine(_appPath, "Test");
            _createDatabase = new CreateDatabase(_saveFolder, SAVE_GAME_FILE_NAME);
        }

        [Test]
        public void Create_Saved_Game_DB_File()
        {
            _createDatabase.CreateSaveFile();

            Console.WriteLine(_saveFolder);
            Assert.IsTrue(File.Exists(Path.Combine(_saveFolder, SAVE_GAME_FILE_NAME)));
        }

        [Test]
        public void Create_Saved_Game_DB_Tables()
        {
            _createDatabase.CreateSaveFile();
            _createDatabase.CreateTables();
        }

        [Test]
        public void Create_Saved_Game_Insert_Data()
        {
            _createDatabase.Save(_gameState);
        }
    }
}
