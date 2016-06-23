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
            _game = new Game(new NewGameSettings());
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

            for (int i = -5; i < 0; i++)
                for (int j = -5; j < 5; j++)
                {
                    checkMoveEntity(i * .1, j * .1);
                }


            // Check a very large number of different positions

            for (int i = -5; i < 5; i++)
                for (int j = -5; j < 5; j++ )
                {
                    checkMovePosition(i *.1, j * .1);
                }
                    
        }

        private void checkMoveEntity(double x, double y)
        {
            PositionDB target;

            _game.CurrentDateTime.AddSeconds(5.0);

            _ship.GetDataBlob<ShipInfoDB>().ClearOrders();
            _player.Orders.ClearOrders();

            target = new PositionDB(_ship.GetDataBlob<PositionDB>());

            target.X += x;
            target.Y += y;

            _target.GetDataBlob<PositionDB>().X = target.X;
            _target.GetDataBlob<PositionDB>().Y = target.Y;

            _player.Orders.MoveOrder(_ship, _target);
            BaseOrder order = _player.Orders.PeekNextOrder();

            Assert.AreEqual(1, _player.Orders.NumOrders());

            _orderProcessor.Process(_game, _systems, 5);

            Assert.AreEqual(0, _player.Orders.NumOrders());
            Assert.AreEqual(1, _ship.GetDataBlob<ShipInfoDB>().NumOrders());

            Assert.Contains(order, _ship.GetDataBlob<ShipInfoDB>().Orders);

            _ship.GetDataBlob<ShipInfoDB>().ProcessOrder();

            Assert.AreEqual(0, _player.Orders.NumOrders());
            Assert.AreEqual(1, _ship.GetDataBlob<ShipInfoDB>().NumOrders());

            Assert.Contains(order, _ship.GetDataBlob<ShipInfoDB>().Orders);

            _ship.GetDataBlob<ShipInfoDB>().ProcessOrder();

            // Check speed 
            Vector4 speed = _ship.GetDataBlob<PropulsionDB>().CurrentSpeed;

            double length = Math.Sqrt((x * x) + (y * y));
            double speedX, speedY;
            speedX = (x / length) * 100;
            speedY = (y / length) * 100;

            // Allowing for very small discrepancies
            Assert.LessOrEqual(Math.Abs(speedX - speed.X), 0.0001);
            Assert.LessOrEqual(Math.Abs(speedY - speed.Y), 0.0001);
        }

        private void checkMovePosition(double x, double y)
        {
            PositionDB target;

            _game.CurrentDateTime.AddSeconds(5.0);

            _ship.GetDataBlob<ShipInfoDB>().ClearOrders();
            _player.Orders.ClearOrders();

            target = new PositionDB(_ship.GetDataBlob<PositionDB>());

            target.X += x;
            target.Y += y;

            _player.Orders.MoveOrder(_ship, _starSystem, target.X, target.Y);
            BaseOrder order = _player.Orders.PeekNextOrder();

            Assert.AreEqual(1, _player.Orders.NumOrders());

            _orderProcessor.Process(_game, _systems, 5);

            Assert.AreEqual(0, _player.Orders.NumOrders());
            Assert.AreEqual(1, _ship.GetDataBlob<ShipInfoDB>().NumOrders());

            Assert.Contains(order, _ship.GetDataBlob<ShipInfoDB>().Orders);

            _ship.GetDataBlob<ShipInfoDB>().ProcessOrder();

            Assert.AreEqual(0, _player.Orders.NumOrders());
            Assert.AreEqual(1, _ship.GetDataBlob<ShipInfoDB>().NumOrders());

            Assert.Contains(order, _ship.GetDataBlob<ShipInfoDB>().Orders);

            _ship.GetDataBlob<ShipInfoDB>().ProcessOrder();

            // Check speed 
            Vector4 speed = _ship.GetDataBlob<PropulsionDB>().CurrentSpeed;

            double length = Math.Sqrt((x * x) + (y * y));
            double speedX, speedY;
            speedX = (x / length) * 100;
            speedY = (y / length) * 100;

            // Allowing for very small discrepancies
            Assert.LessOrEqual(Math.Abs(speedX - speed.X), 0.0001);
            Assert.LessOrEqual(Math.Abs(speedY - speed.Y), 0.0001);


        }
    }
}
