using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Factories;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Processor Tests")]
    class ProcessorTests
    {
        private Game _game;
        EntityManager _entityManager;
        private Entity _faction;


        [SetUp]
        public void Init()
        {
            _game = new Game();
            StaticDataManager.LoadFromDefaultDataDirectory();
            _entityManager = new EntityManager();
            _faction = FactionFactory.CreateFaction(_game.GlobalManager, "Terrian");
            
            Entity starSystem = Entity.Create(_game.GlobalManager);
            Entity planet = Entity.Create(starSystem.Manager, new List<BaseDataBlob>());

            ColonyFactory.CreateColony(_faction, planet);

          


        }
    }
}
