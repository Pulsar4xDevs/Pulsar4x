using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Factories;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("ECS Ship Tests")]
    public class ECSShipTests
    {
        private Game _game;
        private Entity _faction;
        private StarSystem _starSystem;

        [SetUp]
        public void Init()
        {
            _game = new Game();

            var list = new List<BaseDataBlob>();
            Entity sdb = _game.GlobalManager.CreateEntity(new List<BaseDataBlob> {new SpeciesDB("Human", 1.0, 0.5, 1.5, 1.0, 0.5, 1.5, 22, 0, 44),});
            var pop = new JDictionary<Entity, double> { { sdb, 42 } };

            list.Add(new ColonyInfoDB(pop));
            list.Add(new FactionDB("Terran", new List<SpeciesDB>(), new List<StarSystem>(), new List<ColonyInfoDB>()));
            _faction = _game.GlobalManager.CreateEntity(list);
            _game.EngineComms.AddFaction(_faction);

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
                typeof(TroopTransportDB)
            };
            
            Entity shipClass = ShipFactory.CreateNewShipClass(_faction.ID, shipClassName);

            foreach (BaseDataBlob shipClassDataBlob in shipClass.GetAllDataBlobs())
            {
                Assert.IsTrue(requiredDataBlobs.Contains(shipClassDataBlob.GetType()), "Ship Class Entity doesn't contains required datablob: " + shipClassDataBlob.GetType().Name);
            }

            ShipInfoDB shipClassInfo = shipClass.GetDataBlob<ShipInfoDB>();

            Assert.IsTrue(shipClassInfo.ShipClassDefinition == Guid.Empty, "Ship Class ShipInfoDB must have empty ShipClassDefinition Guid");
            Assert.IsTrue(shipClassInfo.ClassName == shipClassName);
            Assert.IsTrue(shipClassInfo.Name == shipClassName);

            string shipName = "USC Winterblossom"; //Still X Universe

            /////Ship/////

            Entity ship = ShipFactory.CreateShip(shipClass.Guid, _starSystem.SystemManager, shipName, -1); //Do something with parentFormation

            foreach (BaseDataBlob shipDataBlob in ship.GetAllDataBlobs())
            {
                Assert.IsTrue(requiredDataBlobs.Contains(shipDataBlob.GetType()), "Ship Entity doesn't contains required datablob: " + shipDataBlob.GetType().Name);
            }

            ShipInfoDB shipInfo = ship.GetDataBlob<ShipInfoDB>();

            Assert.IsTrue(shipInfo.ShipClassDefinition == shipClass.Guid, "ShipClassDefinition guid must be same as ship class entity guid");
            Assert.IsTrue(shipInfo.Name == shipName);
            Assert.IsTrue(shipInfo.ClassName == shipClassName);
        }
    }
}
