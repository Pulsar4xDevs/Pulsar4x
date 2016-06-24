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
        private StarSystem _starSystem;
        private Player _player;
        private Entity _ship, _target;
        private Entity _earth;
        private List<Entity> _planets;
        private List<StarSystem> _systems;
        private OrderProcessor _orderProcessor;


        [SetUp]
        public void Init()
        {
            NewGameSettings settings = new NewGameSettings();
            settings.MaxSystems = 10;
            
            _game = new Game(settings);
            StaticDataManager.LoadData("Pulsar4x", _game);
            _player = _game.AddPlayer("Test Player");
            _faction = DefaultStartFactory.DefaultHumans(_game, _player, "Test Faction");

            _starSystem = _game.Systems.First<KeyValuePair<Guid, StarSystem>>().Value;
            _planets = _starSystem.SystemManager.GetAllEntitiesWithDataBlob<SystemBodyDB>();

            _earth = _planets.Where<Entity>(planet => planet.GetDataBlob<NameDB>().GetName(_faction) == "Earth").First<Entity>();

            _ship = _starSystem.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>().First<Entity>();
            _target = _ship.Clone(_starSystem.SystemManager);

            _orderProcessor = new OrderProcessor();

            _systems = new List<StarSystem>();

            foreach (KeyValuePair<Guid, StarSystem> kvp in _game.Systems)
            {
                _systems.Add(kvp.Value);
            }
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
            PositionDB position;
            BaseOrder order;
            Vector4 speed;
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

            _ship.GetDataBlob<ShipInfoDB>().ProcessOrder();

            // Check for ship speed
            speed = _ship.GetDataBlob<PropulsionDB>().CurrentSpeed;

            Assert.AreEqual(100, speed.X);
            Assert.AreEqual(0, speed.Y);

            _ship.GetDataBlob<ShipInfoDB>().ClearOrders();
            Assert.AreEqual(0, _ship.GetDataBlob<ShipInfoDB>().NumOrders());

            _game.CurrentDateTime.AddSeconds(5.0);

            newPosition.X -= 0.1;
            newPosition.Y += 0.4;

            _player.Orders.MoveOrder(_ship, _starSystem, newPosition.X, newPosition.Y);
            order = _player.Orders.PeekNextOrder();

            Assert.AreEqual(1, _player.Orders.NumOrders());

            _orderProcessor.Process(_game, _systems, 5);

            Assert.AreEqual(0, _player.Orders.NumOrders());
            Assert.AreEqual(1, _ship.GetDataBlob<ShipInfoDB>().NumOrders());

            Assert.Contains(order, _ship.GetDataBlob<ShipInfoDB>().Orders);

            _ship.GetDataBlob<ShipInfoDB>().ProcessOrder();

            // Check speed 
            speed = _ship.GetDataBlob<PropulsionDB>().CurrentSpeed;

            double length = Math.Sqrt(0.1 * 0.1 + 0.4 * 0.4);
            double speedX, speedY;
            speedX = (0.1 / length) * 100;
            speedY = (0.4 / length) * 100;

            // Allowing for very small discrepancies
            Assert.LessOrEqual(Math.Abs(speedX - speed.X), 0.0001);
            Assert.LessOrEqual(Math.Abs(speedY - speed.Y), 0.0001);


        }
    }
}
