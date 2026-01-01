using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Cargo Transfer Processor Tests")]
    public class CargoTransferTests
    {
        private Game _game;
        private EntityManager _manager;
        private Entity _station;
        private Entity _ship;
        private Entity _parentStar;
        private TestCargoItem _lightCargo;
        private TestCargoItem _heavyCargo;
        private Entity _faction;
        private const string CARGO_TYPE_ID = "test-cargo-type";

        [SetUp]
        public void Init()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);

            var settings = new NewGameSettings() { MaxSystems = 1 };
            _game = new Game(settings, modDataStore);
            _manager = new EntityManager();
            _manager.Initialize(_game);

            // Create a faction for the tests
            _faction = FactionFactory.CreateFaction(_game, "Test Faction");

            // Create a parent body (star) for SOI calculations
            var parentBlobs = new BaseDataBlob[]
            {
                new PositionDB { AbsolutePosition = new Vector3(0, 0, 0) },
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 696342000.0), // Sun mass and radius
                new OrbitDB(),
                new NameDB("Test Star")
            };
            _parentStar = Entity.Create();
            _manager.AddEntity(_parentStar, parentBlobs);

            // Create test cargo items
            _lightCargo = new TestCargoItem
            {
                ID = 1,
                UniqueID = Guid.NewGuid().ToString(),
                Name = "Light Cargo",
                CargoTypeID = CARGO_TYPE_ID,
                MassPerUnit = 1,  // 1 kg per unit
                VolumePerUnit = 1.0
            };

            _heavyCargo = new TestCargoItem
            {
                ID = 2,
                UniqueID = Guid.NewGuid().ToString(),
                Name = "Heavy Components",
                CargoTypeID = CARGO_TYPE_ID,
                MassPerUnit = 50,  // 50 kg per unit - tests the math bug fix
                VolumePerUnit = 10.0
            };

            // Create a station entity with cargo storage
            var stationStorage = new CargoStorageDB(CARGO_TYPE_ID, 10000.0); // 10,000 volume
            stationStorage.TransferRate = 100; // 100 kg/s
            stationStorage.TransferRangeDv_mps = 1000; // 1000 m/s range

            var stationOrbit = new OrbitDB(_parentStar);
            var stationBlobs = new BaseDataBlob[]
            {
                stationStorage,
                new PositionDB { AbsolutePosition = new Vector3(1000000, 0, 0) },
                new MassVolumeDB { MassDry = 100000 },
                stationOrbit,
                new NameDB("Test Station"),
                new OrderableDB()
            };
            _station = Entity.Create();
            _manager.AddEntity(_station, stationBlobs);

            // Create a ship entity with cargo storage (very close to station)
            var shipStorage = new CargoStorageDB(CARGO_TYPE_ID, 5000.0); // 5,000 volume
            shipStorage.TransferRate = 50; // 50 kg/s
            shipStorage.TransferRangeDv_mps = 1000; // 1000 m/s range

            var shipOrbit = new OrbitDB(_parentStar);
            var shipBlobs = new BaseDataBlob[]
            {
                shipStorage,
                new PositionDB { AbsolutePosition = new Vector3(1000100, 0, 0) },
                new MassVolumeDB { MassDry = 10000 },
                shipOrbit,
                new NameDB("Test Ship"),
                new OrderableDB()
            };
            _ship = Entity.Create();
            _manager.AddEntity(_ship, shipBlobs);

            // Add some initial cargo to the station
            var stationCargoDb = _station.GetDataBlob<CargoStorageDB>();
            stationCargoDb.AddCargoByUnit(_lightCargo, 100);
            stationCargoDb.AddCargoByUnit(_heavyCargo, 20);
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _manager = null;
            _station = null;
            _ship = null;
        }

        private void ExecuteOrders(params Entity[] entities)
        {
            foreach (var entity in entities)
            {
                if (entity.TryGetDataBlob<OrderableDB>(out var orderable))
                {
                    foreach (var order in orderable.ActionList)
                    {
                        if (!order.IsRunning)
                            order.Execute(_manager.ManagerSubpulses.StarSysDateTime);
                    }
                }
            }
        }

        [Test]
        [Description("Test basic cargo transfer with light items (MassPerUnit = 1)")]
        public void CargoTransfer_LightItems_TransfersCorrectly()
        {
            var stationCargo = _station.GetDataBlob<CargoStorageDB>();
            var shipCargo = _ship.GetDataBlob<CargoStorageDB>();

            // Initial state
            Assert.AreEqual(100, stationCargo.GetUnitsStored(_lightCargo, true), "Station should have 100 light cargo initially");
            Assert.AreEqual(0, shipCargo.GetUnitsStored(_lightCargo, true), "Ship should have 0 light cargo initially");

            // Create transfer data directly (bypass order system for simpler testing)
            var itemsToMove = new List<(ICargoable, long)> { (_lightCargo, 50) };
            var transferData = new CargoTransferDataDB(_ship, _station, itemsToMove);

            // Create CargoTransferDB on both entities
            var shipTransferDB = new CargoTransferDB(transferData);
            shipTransferDB.ParentStorageDB = shipCargo;
            _ship.SetDataBlob(shipTransferDB);

            var stationTransferDB = new CargoTransferDB(transferData);
            stationTransferDB.ParentStorageDB = stationCargo;
            _station.SetDataBlob(stationTransferDB);

            // Process the transfer (simulate 1 second at 150 kg/s total = can transfer 150 kg)
            var processor = new CargoTransferProcessor();
            processor.ProcessManager(_manager, 1);

            // After 1 second, should transfer all 50 units (50 kg total, well under 150 kg/s capacity)
            var stationUnits = stationCargo.GetUnitsStored(_lightCargo, false);  // false = don't include escrow
            var shipUnits = shipCargo.GetUnitsStored(_lightCargo, false);

            Assert.AreEqual(50, stationUnits, "Station should have 50 light cargo after transfer (started with 100)");
            Assert.AreEqual(50, shipUnits, "Ship should have 50 light cargo after transfer");
        }

        [Test]
        [Description("Test cargo transfer with heavy items (MassPerUnit = 50) - validates math bug fix")]
        public void CargoTransfer_HeavyItems_TransfersCorrectly()
        {
            var stationCargo = _station.GetDataBlob<CargoStorageDB>();
            var shipCargo = _ship.GetDataBlob<CargoStorageDB>();

            // Initial state
            Assert.AreEqual(20, stationCargo.GetUnitsStored(_heavyCargo, true), "Station should have 20 heavy cargo initially");
            Assert.AreEqual(0, shipCargo.GetUnitsStored(_heavyCargo, true), "Ship should have 0 heavy cargo initially");

            // Create transfer data directly: move 10 heavy cargo from station to ship (500 kg total)
            var itemsToMove = new List<(ICargoable, long)> { (_heavyCargo, 10) };
            var transferData = new CargoTransferDataDB(_ship, _station, itemsToMove);

            // Create CargoTransferDB on both entities
            var shipTransferDB = new CargoTransferDB(transferData);
            shipTransferDB.ParentStorageDB = shipCargo;
            _ship.SetDataBlob(shipTransferDB);

            var stationTransferDB = new CargoTransferDB(transferData);
            stationTransferDB.ParentStorageDB = stationCargo;
            _station.SetDataBlob(stationTransferDB);

            var processor = new CargoTransferProcessor();

            // First tick: 1 second
            // Both ship and station will process, each with their own transfer rate
            // Ship: 50 kg/s, Station: 100 kg/s, so total 150 kg/s = 3 units (150/50)
            processor.ProcessManager(_manager, 1);
            var shipUnits1 = shipCargo.GetUnitsStored(_heavyCargo, false);  // false = don't include escrow
            Assert.AreEqual(3, shipUnits1, "Ship should have 3 heavy cargo after 1 second (150 kg = 50+100 from both entities)");

            // Second tick: another 150 kg = 3 more units
            processor.ProcessManager(_manager, 1);
            var shipUnits2 = shipCargo.GetUnitsStored(_heavyCargo, false);
            Assert.AreEqual(6, shipUnits2, "Ship should have 6 heavy cargo after 2 seconds (300 kg)");

            // Third tick: another 150 kg transfers 3 more units = 9 total, leaving 50 kg (1 unit) in escrow
            processor.ProcessManager(_manager, 1);
            var shipUnits3 = shipCargo.GetUnitsStored(_heavyCargo, false);
            Assert.AreEqual(9, shipUnits3, "Ship should have 9 units after 3 seconds (450 kg)");

            // Fourth tick: final 50 kg = 1 unit (completes the transfer)
            // Total time needed: 500 kg / 150 kg/s = 3.33 seconds
            processor.ProcessManager(_manager, 1);
            var shipUnitsFinal = shipCargo.GetUnitsStored(_heavyCargo, false);
            Assert.AreEqual(10, shipUnitsFinal, "Ship should have all 10 units after 4 seconds");

            var stationFinal = stationCargo.GetUnitsStored(_heavyCargo, false);
            Assert.AreEqual(10, stationFinal, "Station should have 10 heavy cargo remaining (started with 20, transferred 10)");
        }

        [Test]
        [Description("Test that transfer data escrow lists are properly processed")]
        public void CargoTransfer_EscrowSystem_WorksCorrectly()
        {
            var stationCargo = _station.GetDataBlob<CargoStorageDB>();
            var shipCargo = _ship.GetDataBlob<CargoStorageDB>();

            // Initial state
            Assert.AreEqual(100, stationCargo.GetUnitsStored(_lightCargo, false), "Station should have 100 light cargo initially");
            Assert.AreEqual(0, shipCargo.GetUnitsStored(_lightCargo, false), "Ship should have 0 light cargo initially");

            // Create transfer data directly
            var itemsToMove = new List<(ICargoable, long)> { (_lightCargo, 50) };
            var transferData = new CargoTransferDataDB(_ship, _station, itemsToMove);

            // After creating transfer, items should be in escrow
            Assert.AreEqual(50, stationCargo.GetUnitsStored(_lightCargo, false), "Station should have 50 in storage (50 moved to escrow)");
            Assert.Greater(transferData.EscroHeldInSecondary.Count, 0, "Escrow should have items");

            // Create CargoTransferDB on both entities
            var shipTransferDB = new CargoTransferDB(transferData);
            shipTransferDB.ParentStorageDB = shipCargo;
            _ship.SetDataBlob(shipTransferDB);

            var stationTransferDB = new CargoTransferDB(transferData);
            stationTransferDB.ParentStorageDB = stationCargo;
            _station.SetDataBlob(stationTransferDB);

            // Process transfer to completion
            var processor = new CargoTransferProcessor();
            for (int i = 0; i < 10; i++)
            {
                processor.ProcessManager(_manager, 1);
            }

            // After transfer completes, cargo should have moved
            var stationFinal = stationCargo.GetUnitsStored(_lightCargo, false);
            var shipFinal = shipCargo.GetUnitsStored(_lightCargo, false);

            Assert.AreEqual(50, stationFinal, "Station should have 50 light cargo after transfer (started with 100)");
            Assert.AreEqual(50, shipFinal, "Ship should have 50 light cargo after transfer");
        }

        [Test]
        [Description("Test transfer rate calculation based on both entities' capabilities")]
        public void CargoTransfer_TransferRate_CalculatesCorrectly()
        {
            var stationCargo = _station.GetDataBlob<CargoStorageDB>();
            var shipCargo = _ship.GetDataBlob<CargoStorageDB>();

            // Station has 100 kg/s, Ship has 50 kg/s
            // Expected transfer rate at best range: 100 + 50 = 150 kg/s

            var dvDiff = CargoTransferProcessor.CalcDVDifference_m(_station, _ship);
            var transferRate = CargoTransferProcessor.CalcTransferRate(dvDiff, stationCargo, shipCargo);

            // At same position/orbit, DV difference should be minimal
            // Transfer rate should be sum of both rates (150 kg/s)
            Assert.AreEqual(150, transferRate, "Transfer rate should be sum of both entities' rates");
        }

        [Test]
        [Description("Test that fractional mass transfers correctly calculate remaining items")]
        public void CargoTransfer_FractionalMass_CalculatesItemsCorrectly()
        {
            var stationCargo = _station.GetDataBlob<CargoStorageDB>();
            var shipCargo = _ship.GetDataBlob<CargoStorageDB>();

            // Heavy cargo: 50 kg per unit
            // Transfer rate: 100 kg/s
            // Transferring 10 units = 500 kg total

            var itemsToMove = new List<(ICargoable, long)> { (_heavyCargo, 10) };
            var transferData = new CargoTransferDataDB(_ship, _station, itemsToMove);

            // Create CargoTransferDB on both entities
            var shipTransferDB = new CargoTransferDB(transferData);
            shipTransferDB.ParentStorageDB = shipCargo;
            _ship.SetDataBlob(shipTransferDB);

            var stationTransferDB = new CargoTransferDB(transferData);
            stationTransferDB.ParentStorageDB = stationCargo;
            _station.SetDataBlob(stationTransferDB);

            var processor = new CargoTransferProcessor();

            // After 1 second: 150 kg transferred (50 from ship + 100 from station)
            // Items transferred should be floor(150/50) = 3 units
            // Remaining mass in escrow: 350 kg
            // Remaining items: ceiling(350/50) = 7 units
            processor.ProcessManager(_manager, 1);

            var shipUnits = shipCargo.GetUnitsStored(_heavyCargo, false);  // false = don't include escrow
            Assert.AreEqual(3, shipUnits, "Should transfer exactly 3 units (150 kg / 50 kg per unit)");

            // After another second: 150 more kg = 3 more units
            processor.ProcessManager(_manager, 1);
            shipUnits = shipCargo.GetUnitsStored(_heavyCargo, false);
            Assert.AreEqual(6, shipUnits, "Should have 6 units total after 300 kg transferred");
        }

        [Test]
        [Description("Test that transfer respects cargo capacity limits")]
        public void CargoTransfer_CapacityLimit_PreventsOverfill()
        {
            // Create a small ship with limited capacity
            var smallStorage = new CargoStorageDB(CARGO_TYPE_ID, 100.0); // Only 100 volume
            smallStorage.TransferRate = 50;
            smallStorage.TransferRangeDv_mps = 1000;

            var smallShipOrbit = new OrbitDB(_parentStar);
            var smallShipBlobs = new BaseDataBlob[]
            {
                smallStorage,
                new PositionDB { AbsolutePosition = new Vector3(1000100, 0, 0) },
                new MassVolumeDB { MassDry = 5000 },
                smallShipOrbit,
                new NameDB("Small Ship"),
                new OrderableDB()
            };
            var smallShip = Entity.Create();
            _manager.AddEntity(smallShip, smallShipBlobs);

            // Try to transfer 150 units (150 volume), but ship can only hold 100 volume
            var itemsToMove = new List<(ICargoable, long)> { (_lightCargo, 150) };
            var transferData = new CargoTransferDataDB(smallShip, _station, itemsToMove);

            var stationCargo = _station.GetDataBlob<CargoStorageDB>();

            // Create CargoTransferDB on both entities
            var smallShipTransferDB = new CargoTransferDB(transferData);
            smallShipTransferDB.ParentStorageDB = smallStorage;
            smallShip.SetDataBlob(smallShipTransferDB);

            var stationTransferDB = new CargoTransferDB(transferData);
            stationTransferDB.ParentStorageDB = stationCargo;
            _station.SetDataBlob(stationTransferDB);

            // Process transfer to completion
            var processor = new CargoTransferProcessor();
            for (int i = 0; i < 10; i++)
            {
                processor.ProcessManager(_manager, 1);
            }

            var smallShipCargo = smallShip.GetDataBlob<CargoStorageDB>();
            var transferred = smallShipCargo.GetUnitsStored(_lightCargo, false);  // false = don't include escrow

            // Should only transfer what fits (100 units max for 100 volume)
            Assert.LessOrEqual(transferred, 100, "Should not exceed cargo capacity");
        }
    }

    /// <summary>
    /// Simple test implementation of ICargoable for testing purposes
    /// </summary>
    public class TestCargoItem : ICargoable
    {
        public int ID { get; set; }
        public string UniqueID { get; set; }
        public string Name { get; set; }
        public string CargoTypeID { get; set; }
        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; set; }
    }
}
