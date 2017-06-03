using System;
using System.Collections.Generic;
using System.Linq;
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

        [SetUp]
        public void Init()
        {
            _testGame = new TestGame(1);
            _duraniumSD = NameLookup.TryGetMineralSD(_testGame.Game, "Duranium");
            OrderableDB orderable = new OrderableDB();
            TestingUtilities.ColonyFacilitys(_testGame, _testGame.EarthColony);
            _testGame.EarthColony.Manager.SetDataBlob(_testGame.DefaultShip.ID, orderable);
            StorageSpaceProcessor.AddItemToCargo(_testGame.EarthColony.GetDataBlob<CargoStorageDB>(), _duraniumSD, 10000); 
        }

        [Test]
        public void TestCargoOrder()
        {
            CargoOrder cargoOrder = new CargoOrder(_testGame.DefaultShip.Guid, _testGame.HumanFaction.Guid, 
                                                   _testGame.EarthColony.Guid, CargoOrderTypes.LoadCargo, 
                                                   _duraniumSD.CargoTypeID, 100);
            
            CargoAction action = cargoOrder.CreateAction(_testGame.Game, cargoOrder);
            Assert.NotNull(action.OrderableProcessor);
            
            
            _testGame.EarthColony.Manager.OrderQueue.Enqueue(cargoOrder);
            OrderProcessor.ProcessManagerOrders(_testGame.EarthColony.Manager);
            Assert.True(_testGame.DefaultShip.GetDataBlob<OrderableDB>().ActionQueue[0] is CargoAction);

        }

        [Test]
        public void TestCargoMove()
        {
            _testGame.GameSettings.EnableMultiThreading = false;
            EntityManager entityManager = _testGame.EarthColony.Manager;
            CargoOrder cargoOrder = new CargoOrder(_testGame.DefaultShip.Guid, _testGame.HumanFaction.Guid, _testGame.EarthColony.Guid, CargoOrderTypes.LoadCargo, _duraniumSD.CargoTypeID, 100);

            CargoStorageDB cargoStorageDB = _testGame.DefaultShip.GetDataBlob<CargoStorageDB>();
            
            Entity entity;
            Assert.True(_testGame.Game.GlobalManager.FindEntityByGuid(_testGame.DefaultShip.Guid, out entity));       
            //cargoOrder.PreProcessing(_testGame.Game);
            //_testGame.DefaultShip.GetDataBlob<OrderableDB>().ActionQueue.Add(cargoOrder);
            
            //cargoOrder.OrderableProcessor.FirstProcess(cargoOrder);

            //DateTime eta = cargoOrder.EstTimeComplete;
            DateTime nextStep = entityManager.ManagerSubpulses.EntityDictionary.ElementAt(0).Key;
            //Assert.AreEqual(nextStep, eta, "check if eta & nextstep are equal");


            
            //TimeSpan timeToTake = eta - _currentDateTime;
                                    
            Assert.Greater(nextStep, _currentDateTime, "nextStep should be greater than current datetime");
            

            //_testGame.Game.GameLoop.Ticklength = timeToTake;
            _testGame.Game.GameLoop.TimeStep();
            
            long amountInShip = StorageSpaceProcessor.GetAmountOf(cargoStorageDB, _duraniumSD.ID);   
            Assert.AreEqual(100, amountInShip, "ship has " + amountInShip.ToString() + " Duranium");
            
            _testGame.Game.GameLoop.TimeStep();
            
            //check we don't have more than 100 
            amountInShip = StorageSpaceProcessor.GetAmountOf(cargoStorageDB, _duraniumSD.ID);   
            Assert.AreEqual(100, amountInShip, "ship has " + amountInShip.ToString() + " Duranium");
            
        }
    }
}