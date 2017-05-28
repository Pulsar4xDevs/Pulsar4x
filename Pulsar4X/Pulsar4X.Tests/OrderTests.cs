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
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _planet;
        private Entity _colonyEntity;
        private StarSystem _starSystem;
        private Entity _shipClass;
        private Entity _ship;
        private MineralSD _duraniumSD;
        //private MineralSD _corundiumSD;
        
        StarSystemFactory _starSystemFactory;

        [SetUp]
        public void Init()
        {
            _game = new Game(new NewGameSettings {GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = 1});
            _starSystemFactory = new StarSystemFactory(_game);
            _starSystem = _starSystemFactory.CreateSinglePlanetSystem(_game);
            _entityManager = _starSystem.SystemManager;
            _faction = FactionFactory.CreateFaction(_game, "testFaction");
            _planet = NameLookup.TryGetFirstEntityWithName(_entityManager, "planet");

            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(_faction, _game.GlobalManager);
            _colonyEntity = ColonyFactory.CreateColony(_faction, speciesEntity, _planet);

            _shipClass = DefaultStartFactory.DefaultShipDesign(_game, _faction);

            Entity cargoInstalation = DefaultStartFactory.DefaultCargoInstalation(_game, _faction);


            EntityManipulation.AddComponentToEntity(_colonyEntity, cargoInstalation);
            ReCalcProcessor.ReCalcAbilities(_colonyEntity);
            _colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;

            Vector4 position = _planet.GetDataBlob<PositionDB>().AbsolutePosition;
            _ship = ShipFactory.CreateShip(_shipClass, _entityManager, _faction, position, _starSystem, "Serial Peacemaker");
            OrderableDB orderable = new OrderableDB();
            _entityManager.SetDataBlob(_ship.ID, orderable);

            _duraniumSD = NameLookup.TryGetMineralSD(_game, "Duranium");
            StorageSpaceProcessor.AddItemToCargo(_colonyEntity.GetDataBlob<CargoStorageDB>(), _duraniumSD, 10000);  
        }

   
        [Test]
        public void TestCargoMove()
        {
            CargoOrder cargoOrder = new CargoOrder(_ship.Guid, _faction.Guid, _colonyEntity.Guid, CargoOrder.CargoOrderTypes.LoadCargo, _duraniumSD.CargoTypeID, 100);

            CargoStorageDB cargoStorageDB = _ship.GetDataBlob<CargoStorageDB>();
            
            Entity entity;
            Assert.True(_game.GlobalManager.FindEntityByGuid(_ship.Guid, out entity));       
            cargoOrder.PreProcessing(_game);
            _ship.GetDataBlob<OrderableDB>().ActionQueue.Add(cargoOrder);
            
            cargoOrder.OrderableProcessor.FirstProcess(cargoOrder);

            DateTime eta = cargoOrder.EstTimeComplete;
            DateTime nextStep = _entityManager.ManagerSubpulses.EntityDictionary.ElementAt(0).Key;
            Assert.AreEqual(nextStep, eta, "check if eta is nextstep is correct");

            long amount = StorageSpaceProcessor.GetAmountOf(cargoStorageDB, _duraniumSD.ID);
        }
    }
}