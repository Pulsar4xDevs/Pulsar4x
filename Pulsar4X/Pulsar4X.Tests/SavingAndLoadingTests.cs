using System.Linq;
using NUnit.Framework;
using Pulsar4X.Engine;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SavingAndLoadingTests
    {
        NewGameSettings? _settings;
        Game? _game;

        [SetUp]
        public void Setup()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();

            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);

            _settings = new NewGameSettings() {
                StartDateTime = new System.DateTime(2100, 9, 1)
            };

            _game  = new Game(_settings, modDataStore);
        }

        [Test]
        public void VerifySaveAndLoad()
        {
            if(_game == null) return;

            DefaultStartFactory.DefaultHumans(_game, "Test Humans");

            var gameJson = Game.Save(_game);

            System.IO.File.WriteAllText("save.json", gameJson);

            var loadedGame = Game.Load(gameJson);

            Assert.NotNull(loadedGame.TimePulse, "TimePulse null");
            Assert.NotNull(loadedGame.ProcessorManager, "ProcessorManager null");
            Assert.NotNull(loadedGame.GlobalManager, "GlobalManager null");
            Assert.NotNull(loadedGame.GameMasterFaction, "GameMasterFaction null");

            Assert.AreEqual(_game.NextEntityID, loadedGame.NextEntityID);

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

            Assert.AreEqual(_game.GlobalManager.ManagerID, loadedGame.GlobalManager.ManagerID, "Global Manager ID");

            var previousEntities = _game.GlobalManager.GetAllEntites();
            var currentEntities = loadedGame.GlobalManager.GetAllEntites();

            Assert.AreEqual(previousEntities.Count, currentEntities.Count, "Global Manager Entity Count");

            for(int i = 0; i < previousEntities.Count; i++)
            {
                Assert.AreEqual(previousEntities[i].Id, currentEntities[i].Id, "Entity ID Check");
            }

            Assert.AreEqual(_game.Systems.Count, loadedGame.Systems.Count, "Star System Count");

            foreach(var system in _game.Systems)
            {
                Assert.NotNull(loadedGame.Systems.Where(s => s.ID.Equals(system.ID)).First(), "Star System Guid Check");

                StarSystem saved = system;
                StarSystem loaded = loadedGame.Systems.Where(s => s.ID.Equals(system.ID)).First();

                var savedEntities = saved.GetAllEntites();
                var loadedEntities = loaded.GetAllEntites();

                Assert.AreEqual(savedEntities.Count, loadedEntities.Count, "Star System Entity Count");

                for(int i = 0; i < savedEntities.Count; i++)
                {
                    Assert.AreEqual(savedEntities[i].Id, loadedEntities[i].Id, "Star System Entity Id Check");
                    Assert.AreEqual(savedEntities[i].FactionOwnerID, loadedEntities[i].FactionOwnerID, "Star System Entity FactionOwnerID Check");

                    var savedEntityDatablobs = saved.GetAllDataBlobsForEntity(savedEntities[i].Id);
                    var loadedEntityDatablobs = loaded.GetAllDataBlobsForEntity(loadedEntities[i].Id);

                    Assert.AreEqual(savedEntityDatablobs.Count, loadedEntityDatablobs.Count, "Entity Datablob Count");

                    for(int j = 0; j < savedEntityDatablobs.Count; j++)
                    {
                        Assert.AreEqual(savedEntityDatablobs[j].GetType(), loadedEntityDatablobs[j].GetType(), "Entity Datablob Type Check");
                    }
                }

                Assert.AreEqual(saved.ManagerSubpulses.InstanceProcessorsQueue.Count, loaded.ManagerSubpulses.InstanceProcessorsQueue.Count, "Star System Queued Processes Count");
                Assert.AreEqual(saved.ManagerSubpulses.GetTotalNumberOfProceses(), loaded.ManagerSubpulses.GetTotalNumberOfProceses(), "Star System Subpulse Count");
            }

            Assert.AreEqual(_game.GameMasterFaction.Id, loadedGame.GameMasterFaction.Id, "Game Master Fation Guid");
        }

    }
}