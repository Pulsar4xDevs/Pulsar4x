using NUnit.Framework;
using Pulsar4X.Engine;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SavingAndLoadingTests
    {
        ModLoader _modLoader;
        ModDataStore _modDataStore;

        NewGameSettings _settings;
        Game _game;

        [SetUp]
        public void Setup()
        {
            _modLoader = new ModLoader();
            _modDataStore = new ModDataStore();

            _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);

            _settings = new NewGameSettings() {
                StartDateTime = new System.DateTime(2069, 4, 20)
            };

            _game  = new Game(_settings, _modDataStore);
        }

        [Test]
        public void VerifySaveAndLoad()
        {
            DefaultStartFactory.DefaultHumans(_game, "Test Humans");

            var gameJson = Game.Save(_game);

            var loadedGame = Game.Load(gameJson);

            Assert.NotNull(loadedGame.TimePulse);
            Assert.NotNull(loadedGame.ProcessorManager);
            Assert.NotNull(loadedGame.GlobalManager);
            Assert.NotNull(loadedGame.GameMasterFaction);

            Assert.AreEqual(_game.AtmosphericGases.Count, loadedGame.AtmosphericGases.Count);
            Assert.AreEqual(_game.Themes.Count, loadedGame.Themes.Count);
            Assert.AreEqual(_game.TechCategories.Count, loadedGame.TechCategories.Count);
            Assert.AreEqual(_game.StartingGameData.Armor.Count, loadedGame.StartingGameData.Armor.Count);

            Assert.AreEqual(_game.TimePulse.GameGlobalDateTime, loadedGame.TimePulse.GameGlobalDateTime);
            Assert.AreEqual(_game.TimePulse.TimeMultiplier, loadedGame.TimePulse.TimeMultiplier);
            Assert.AreEqual(_game.TimePulse.TickFrequency, loadedGame.TimePulse.TickFrequency);
            Assert.AreEqual(_game.TimePulse.Ticklength, loadedGame.TimePulse.Ticklength);
            Assert.AreEqual(_game.TimePulse.LastProcessingTime, loadedGame.TimePulse.LastProcessingTime);
            Assert.AreEqual(_game.TimePulse.LastSubtickTime, loadedGame.TimePulse.LastSubtickTime);

            Assert.AreEqual(_game.ProcessorManager.HotloopCount, loadedGame.ProcessorManager.HotloopCount);
            Assert.AreEqual(_game.ProcessorManager.RecalcCount, loadedGame.ProcessorManager.RecalcCount);
            Assert.AreEqual(_game.ProcessorManager.InstanceCount, loadedGame.ProcessorManager.InstanceCount);

            Assert.AreEqual(_game.GlobalManager.Entities.Count, loadedGame.GlobalManager.Entities.Count, "Global Manager Entity Count");

            Assert.AreEqual(_game.GameMasterFaction.Guid, loadedGame.GameMasterFaction.Guid);
        }

    }
}