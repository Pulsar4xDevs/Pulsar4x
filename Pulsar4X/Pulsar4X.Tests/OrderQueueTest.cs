using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("OrderQueue Test")]
    public class OrderQueueTest
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        // private List<Entity> _planets;
        private Entity _ship1, _ship2;
        private GalaxyFactory _galaxyFactory;
        private StarSystemFactory _starSystemFactory;
        private StarSystem _starSystem;

        [SetUp]
        public void Init()
        {
            _game = new Game(new NewGameSettings());
            StaticDataManager.LoadData("Pulsar4x", _game);  
            _entityManager = new EntityManager(_game);
            _faction = FactionFactory.CreateFaction(_game, "Terran");

            // Create system
            _starSystem = _starSystemFactory.CreateSol(_game);

            // _planets = _starSystem.SystemManager.GetAllEntitiesWithDataBlob<SystemBodyDB>();


            // Create two ships
            _ship1 = ShipFactory.
        }

        [TearDown]
        public void Cleanup()
        {
        }

        [Test]
        public void testOrderQueue()
        {
        }
    }
}
