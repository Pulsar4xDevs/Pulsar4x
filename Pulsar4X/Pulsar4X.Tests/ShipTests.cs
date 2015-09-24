using System;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Ship Entity Tests")]
    internal class ShipEntityTests
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _colonyEntity;
        private MineralSD _duraniumSD;
        private MineralSD _corundiumSD;
        private StarSystem _starSystem;
        private Entity _shipClass;
        private Entity _ship;
        private Entity _engineComponent;
        [SetUp]
        public void Init()
        {
            _game = Game.NewGame("Test Game", DateTime.Now, 1);
            _faction = FactionFactory.CreateFaction(_game, "Terran");
            
            _starSystem = new StarSystem(_game, "Sol", -1);
            /////Ship Class/////
            _shipClass = ShipFactory.CreateNewShipClass(_game, _faction, "TestClass");
            

            

        }

        private void CreateShipFromClass()
        {
            _ship = ShipFactory.CreateShip(_shipClass, _starSystem.SystemManager, _faction, "Serial Peacemaker");
        }

        [Test]
        public void TestEngineComponentFactory()
        {
            int size = 5;

            double consumptionPerHour = 0.5;

            int totalPower = 80;

            int thermalSig = 80;

            int hitTokill = 5;

            JDictionary<Guid, int> costs = new JDictionary<Guid, int>();
            Guid gallicite = new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"); //Gallicite.
            costs.Add(gallicite, 8 * 5);
            
            int crew = 5;
            
            Guid tech = new Guid();
            //_engineComponent = EngineFactory.CreateEngineComponent(_starSystem.SystemManager, 5, hitTokill, costs, tech, crew, totalPower, consumptionPerHour, thermalSig);
        }

        //[Test] //TODO re add this after figuring out how to re-write it.
        //public void TestAddcomponent()
        //{
        //    TestEngineComponentFactory();
        //    ShipFactory.AddShipComponent(_shipClass, _engineComponent);
        //    int expectedSpeed = 320;
        //    int maxSpeed = _shipClass.GetDataBlob<PropulsionDB>().MaximumSpeed;
        //    Assert.AreEqual(expectedSpeed, maxSpeed);
        //}
    }
}
