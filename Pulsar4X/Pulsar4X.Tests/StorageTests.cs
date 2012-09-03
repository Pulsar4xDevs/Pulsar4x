using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Stargen;
using Pulsar4X.Storage;
using Pulsar4X.Entities;
using System.ComponentModel;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class StorageTests
    {
        private GameState _gameState;

        private string _appPath;
        private string _saveFolder;
        private const string SAVE_GAME_FILE_NAME = "TestGame.db";

        [SetUp]
        public void TestSetup()
        {
            _gameState = new GameState();
            _gameState.Name = "Test Game";
            _gameState.Species = new BindingList<Species>();
            _gameState.Factions = new BindingList<Faction>();
            _gameState.StarSystems = new BindingList<StarSystem>();
            _gameState.Stars = new BindingList<Star>();
            _gameState.Planets = new BindingList<Planet>();

            var species = new Species { Id = Guid.NewGuid(), Name = "Test Humans" };
            _gameState.Species.Add(species);
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
        }
        [Test]
        public void Save_GameState_To_JSON()
        {
            var s = new Store("TestGameFile", _saveFolder);
            s.SaveGame(_gameState);
        }

        [Test]
        public void Load_GameState_From_JSON()
        {
            var s = new Store("TestGameFile", _saveFolder);
            s.SaveGame(_gameState);

            var gs = s.LoadGame(Path.Combine(_saveFolder, "TestGameFile"));

            Assert.IsNotNull(gs);
        }

        
    }
}
