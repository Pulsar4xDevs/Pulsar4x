using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Storage;
using Pulsar4X.Entities;
using System.ComponentModel;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class StorageTests
    {
        private GameState _gameState;
        private List<CommanderNameTheme> _nameThemes;

        private string _appPath;
        private string _saveFolder;
        private const string SAVE_GAME_FILE_NAME = "TestGame.db";

        [SetUp]
        public void TestSetup()
        {
            _gameState = GameState.Instance;
            _gameState.Name = "Test Game";
            _gameState.Species = new BindingList<Species>();
            _gameState.Factions = new BindingList<Faction>();
            _gameState.StarSystems = new BindingList<StarSystem>();
            _gameState.Stars = new BindingList<Star>();
            _gameState.Planets = new BindingList<SystemBody>();

            var species = new Species { Id = Guid.NewGuid(), Name = "Test Humans" };
            _gameState.Species.Add(species);
            var theme = new FactionTheme { Id = Guid.NewGuid(), Name = "Test Theme" };
            _gameState.Factions.Add(new Faction(0) { Id = Guid.NewGuid(), Name = "Test Faction", Species = species, Title = "Mighty Humans", FactionTheme = theme });

            var ss = SystemGen.CreateSystem("Test Sol");
            GameState.Instance.StarSystemCurrentIndex++;
            ss.Stars.ToList().ForEach(x => _gameState.Stars.Add(x));
            ss.Stars.ToList().SelectMany(x => x.Planets).ToList().ForEach(p => _gameState.Planets.Add(p));

            UriBuilder uri = new UriBuilder(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            _appPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            _saveFolder = Path.Combine(_appPath, "Test");

            _nameThemes = new List<CommanderNameTheme>();
            var ct1 = new CommanderNameTheme()
                            {
                                Id = Guid.NewGuid(),
                                Name = "Test Theme 1",
                                NameEntries =
                                    {
                                        new NameEntry() {IsFemale = false, Name = "Bob", NamePosition = NamePosition.FirstName}, 
                                        new NameEntry() {IsFemale = false, Name = "Smith", NamePosition = NamePosition.LastName}
                                    }
                            };
            _nameThemes.Add(ct1);
            var ct2 = new CommanderNameTheme()
                          {
                              Id = Guid.NewGuid(),
                              Name = "Test Theme 2",
                              NameEntries =
                                  {
                                      new NameEntry()
                                          {IsFemale = true, Name = "Sarah", NamePosition = NamePosition.FirstName},
                                      new NameEntry()
                                          {IsFemale = false, Name = "Connor", NamePosition = NamePosition.LastName}
                                  }
                          };
            _nameThemes.Add(ct2);
        }


        [Test, ExpectedException]
        public void Save_Throws_Without_GameName()
        {
            var s = new Store("", _saveFolder);
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

        [Test]
        public void Save_And_Load_CommanderNameThemes_To_JSON()
        {
            var bs = new Bootstrap();
            bs.SaveCommanderNameTheme(_nameThemes);

            var nt = bs.LoadCommanderNameTheme();
            Assert.IsNotNull(nt);
            Assert.IsNotEmpty(nt);
        }
    }
}
