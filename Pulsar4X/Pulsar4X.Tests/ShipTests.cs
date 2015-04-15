using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Factories;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("ECS Ship Tests")]
    public class ShipTests
    {
        private Game _game;
        private Entity _faction;
        private StarSystem _starSystem;

        [SetUp]
        public void Init()
        {
            _game = new Game();

            _faction = FactionFactory.CreateFaction(_game.GlobalManager, "Terran");

            _starSystem = new StarSystem();
        }

        [Test]
        public void CreateClassAndShip()
        {
            string shipClassName = "M6 Corvette"; //X Universe ;3
            var requiredDataBlobs = new List<Type>()
            {
                typeof(ShipInfoDB),
                typeof(ArmorDB),
                typeof(BeamWeaponsDB),
                typeof(BuildCostDB),
                typeof(CargoDB),
                typeof(CrewDB),
                typeof(DamageDB),
                typeof(HangerDB),
                typeof(IndustryDB),
                typeof(MaintenanceDB),
                typeof(MissileWeaponsDB),
                typeof(PowerDB),
                typeof(PropulsionDB),
                typeof(SensorProfileDB),
                typeof(SensorsDB),
                typeof(ShieldsDB),
                typeof(TractorDB),
                typeof(TroopTransportDB),
                typeof(NameDB)
            };

            Entity shipClass = ShipFactory.CreateNewShipClass(_faction, shipClassName);

            foreach (BaseDataBlob shipClassDataBlob in shipClass.GetAllDataBlobs())
            {
                Assert.IsTrue(requiredDataBlobs.Contains(shipClassDataBlob.GetType()), "Ship Class Entity doesn't contains required datablob: " + shipClassDataBlob.GetType().Name);
            }

            ShipInfoDB shipClassInfo = shipClass.GetDataBlob<ShipInfoDB>();
            Assert.IsTrue(shipClassInfo.ShipClassDefinition == Guid.Empty, "Ship Class ShipInfoDB must have empty ShipClassDefinition Guid");

            NameDB shipClassNameDB = shipClass.GetDataBlob<NameDB>();
            Assert.IsTrue(shipClassNameDB.Name[_faction] == shipClassName);

            string shipName = "USC Winterblossom"; //Still X Universe

            /////Ship/////

            Entity ship = ShipFactory.CreateShip(shipClass, _starSystem.SystemManager, _faction, shipName);

            foreach (BaseDataBlob shipDataBlob in ship.GetAllDataBlobs())
            {
                Assert.IsTrue(requiredDataBlobs.Contains(shipDataBlob.GetType()), "Ship Entity doesn't contains required datablob: " + shipDataBlob.GetType().Name);
            }

            ShipInfoDB shipInfo = ship.GetDataBlob<ShipInfoDB>();
            Assert.IsTrue(shipInfo.ShipClassDefinition == shipClass.Guid, "ShipClassDefinition guid must be same as ship class entity guid");

            NameDB shipNameDB = ship.GetDataBlob<NameDB>();
            Assert.IsTrue(shipNameDB.Name[_faction] == shipName);
        }
    }
}
