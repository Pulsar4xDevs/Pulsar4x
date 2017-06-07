using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.ECSLib;


namespace Pulsar4X.Tests
{
    [Description("Ship Entity Tests")]
    internal class OrderTests
    {
        TestGame _testGame;
        private MineralSD _duraniumSD;
        
        private DateTime _currentDateTime {get { return _testGame.Game.CurrentDateTime; }}

        private BaseOrder _cargoOrder;
        
        [SetUp]
        public void Init()
        {
            _testGame = new TestGame(1);
            _duraniumSD = NameLookup.TryGetMineralSD(_testGame.Game, "Duranium");
            OrderableDB orderable = new OrderableDB();
            TestingUtilities.ColonyFacilitys(_testGame, _testGame.EarthColony);
            _testGame.EarthColony.Manager.SetDataBlob(_testGame.DefaultShip.ID, orderable);
            StorageSpaceProcessor.AddItemToCargo(_testGame.EarthColony.GetDataBlob<CargoStorageDB>(), _duraniumSD, 10000); 
            
            _cargoOrder = new CargoOrder(_testGame.DefaultShip.Guid, _testGame.HumanFaction.Guid, 
                                                   _testGame.EarthColony.Guid, CargoOrderTypes.LoadCargo, 
                                                   _duraniumSD.ID, 100);
        }

        [Test]
        public void TestCargoOrder()
        {
           
            
            BaseAction action = _cargoOrder.CreateAction(_testGame.Game, _cargoOrder);
            Assert.NotNull(action.OrderableProcessor);
            
            //enqueue it to the manager for now since the messagepump is still wip. 
            _testGame.EarthColony.Manager.OrderQueue.Enqueue(_cargoOrder); 
            OrderProcessor.ProcessManagerOrders(_testGame.EarthColony.Manager);
            Assert.True(_testGame.DefaultShip.GetDataBlob<OrderableDB>().ActionQueue[0] is CargoAction);

        }

        [Test]
        public void TestCargoMove()
        {
            //_testGame.GameSettings.EnableMultiThreading = false;
            EntityManager entityManager = _testGame.EarthColony.Manager;

            CargoStorageDB cargoStorageDB = _testGame.DefaultShip.GetDataBlob<CargoStorageDB>();
            
            Entity entity;
            Assert.True(_testGame.Game.GlobalManager.FindEntityByGuid(_testGame.DefaultShip.Guid, out entity));       
            
            _testGame.EarthColony.Manager.OrderQueue.Enqueue(_cargoOrder);             

            OrderProcessor.ProcessManagerOrders(_testGame.EarthColony.Manager);

            CargoAction cargoAction = (CargoAction)_testGame.DefaultShip.GetDataBlob<OrderableDB>().ActionQueue[0];
            DateTime eta = cargoAction.EstTimeComplete;
            DateTime nextStep = entityManager.ManagerSubpulses.EntityDictionary.ElementAt(0).Key;
            Assert.AreEqual(nextStep, eta, "check if eta & nextstep are equal");


            
            TimeSpan timeToTake = eta - _currentDateTime;
                                    
            Assert.Greater(nextStep, _currentDateTime, "nextStep should be greater than current datetime");

            long spaceAvailible = StorageSpaceProcessor.RemainingCapacity(cargoStorageDB, _duraniumSD.CargoTypeID);

            _testGame.Game.GameLoop.Ticklength = timeToTake;
            _testGame.Game.GameLoop.TimeStep();
            
            Assert.AreEqual(_currentDateTime, eta);
            long amountInShip = StorageSpaceProcessor.GetAmountOf(cargoStorageDB, _duraniumSD.ID);   
            long amountOnColony = StorageSpaceProcessor.GetAmountOf(_testGame.EarthColony.GetDataBlob<CargoStorageDB>(), _duraniumSD.ID); 
            long spaceRemaining = StorageSpaceProcessor.RemainingCapacity(cargoStorageDB, _duraniumSD.CargoTypeID);
            Assert.Greater(spaceAvailible, spaceRemaining);
            Assert.AreEqual(100, amountInShip, "ship has " + amountInShip.ToString() + " Duranium");
            Assert.AreEqual(9900, amountOnColony, "colony should have duranium removed");

            Assert.AreEqual(0, _testGame.DefaultShip.GetDataBlob<OrderableDB>().ActionQueue.Count, "action should have been removed from queue");

        }

        [Test]
        public void TestOrderSeralisation()
        {            
            string seralisedOrder = OrderSerializer.SerlialiseOrder(_cargoOrder);
            BaseOrder deserailisedOrder = OrderSerializer.DeserializeOrder(seralisedOrder);
            Assert.True(deserailisedOrder is CargoOrder);
        }

        [Test]
        public void TestOrderViaMessagePump()
        {
            string sOrder = OrderSerializer.SerlialiseOrder(_cargoOrder);
            AuthenticationToken auth = new AuthenticationToken(_testGame.Game.SpaceMaster, "");
            _testGame.Game.MessagePump.EnqueueMessage(IncomingMessageType.EntityOrdersWrite, auth, sOrder);

            bool itemFound = false;
            _testGame.Game.GameLoop.Ticklength = TimeSpan.FromSeconds(10);
            _testGame.Game.GameLoop.TimeStep();
            if (_testGame.EarthColony.Manager.OrderQueue.Count > 0 || _testGame.DefaultShip.GetDataBlob<OrderableDB>().ActionQueue.Count > 0)
                itemFound = true;
            
            Assert.True(itemFound, "order or action is lost");
            
        }
    }
}