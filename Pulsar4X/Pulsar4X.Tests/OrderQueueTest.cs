﻿using System;
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
        private Player _player;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _ship, _target;
        private Entity _earth;
        private List<Entity> _planets;
        private StarSystem _starSystem;
        private List<StarSystem> _systems;
        private OrderProcessor _orderProcessor;


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

            StaticDataManager.LoadData("Pulsar4x", _game);

            _player = _game.AddPlayer("Test Player");

            _faction = DefaultStartFactory.DefaultHumans(_game, _player, "Test Faction");

            _systems = new List<StarSystem>();

            foreach(KeyValuePair<Guid, StarSystem> kvp in _game.Systems)
            {
                _systems.Add(kvp.Value);
            }

            _starSystem = _game.Systems.First<KeyValuePair<Guid, StarSystem>>().Value;
            _planets = _starSystem.SystemManager.GetAllEntitiesWithDataBlob<SystemBodyDB>();

            _earth = _planets.Where<Entity>(planet => planet.GetDataBlob<NameDB>().GetName(_faction) == "Earth").First<Entity>();

            _ship = _starSystem.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>().First<Entity>();
            _target = _ship.Clone(_starSystem.SystemManager);

            _orderProcessor = new OrderProcessor();
        }


        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _player = null;
            _faction = null;
            _starSystem = null;
            _ship = null;
            _target = null;

        }

        [Test]
        public void testOrderQueue()
        {
            //@todo: more stringent tests
            BaseOrder order;
            Vector4 newPosition = new Vector4(_earth.GetDataBlob<PositionDB>().Position);

            newPosition.X += 0.2;

            _target.GetDataBlob<PositionDB>().Position = newPosition ;

            // @todo: give the ship some move orders, see if its speed changes correctly
            _player.Orders.MoveOrder(_ship, _target);
            order = _player.Orders.PeekNextOrder();

            Assert.AreEqual(1, _player.Orders.NumOrders());

            _orderProcessor.Process(_game, _systems, 5);

            Assert.AreEqual(0, _player.Orders.NumOrders());
            Assert.AreEqual(1, _ship.GetDataBlob<ShipInfoDB>().NumOrders());

            // The ship should now have the order
            Assert.Contains(order, _ship.GetDataBlob<ShipInfoDB>().Orders);

            _ship.GetDataBlob<ShipInfoDB>().ClearOrders();
        }
    }
}
