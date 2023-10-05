using System.Security.Principal;
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

            Assert.NotNull(loadedGame.TimePulse, "TimePulse null");
            Assert.NotNull(loadedGame.ProcessorManager, "ProcessorManager null");
            Assert.NotNull(loadedGame.GlobalManager, "GlobalManager null");
            Assert.NotNull(loadedGame.GameMasterFaction, "GameMasterFaction null");

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

            Assert.AreEqual(_game.GlobalManager.ManagerGuid, loadedGame.GlobalManager.ManagerGuid, "Global Manager ID");

            var previousEntities = _game.GlobalManager.GetAllEntites();
            var currentEntities = _game.GlobalManager.GetAllEntites();

            Assert.AreEqual(previousEntities.Count, currentEntities.Count, "Global Manager Entity Count");

            for(int i = 0; i < previousEntities.Count; i++)
            {
                Assert.AreEqual(previousEntities[i].Id, currentEntities[i].Id, "Entity ID Check");
            }

            Assert.AreEqual(_game.Systems.Count, loadedGame.Systems.Count, "Star System Count");

            foreach(var (guid, system) in _game.Systems)
            {
                if(!(system is StarSystem)) continue;

                Assert.IsTrue(loadedGame.Systems.ContainsKey(guid), "Star System Guid Check");

                if(!(loadedGame.Systems[guid] is StarSystem)) continue;

                StarSystem saved = (StarSystem)system;
                StarSystem loaded = (StarSystem)loadedGame.Systems[guid];

                var savedEntities = saved.GetAllEntites();
                var loadedEntities = loaded.GetAllEntites();

                Assert.AreEqual(savedEntities.Count, loadedEntities.Count, "Star System Entity Count");

                for(int i = 0; i < savedEntities.Count; i++)
                {
                    Assert.AreEqual(savedEntities[i].Id, loadedEntities[i].Id, "Star System Entity Id Check");
                }
            }

            Assert.AreEqual(_game.GameMasterFaction.Id, loadedGame.GameMasterFaction.Id, "Game Master Fation Guid");
        }

    }
}