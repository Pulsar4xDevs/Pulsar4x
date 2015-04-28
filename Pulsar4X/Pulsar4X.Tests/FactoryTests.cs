using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Factories;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Test for all exists factories")]
    public class FactoryTests
    {
        private Game _game;

        [SetUp]
        public void Init()
        {
            _game = new Game();
        }

        [Test]
        [Description("FactionFactory test")]
        public void CreateNewFaction()
        {
            string factionName = "Terran";

            var requiredDataBlobs = new List<Type>()
            {
                typeof(FactionDB),
                typeof(FactionAbilitiesDB),
                typeof(NameDB),
                typeof(TechDB)
            };

            Entity faction = FactionFactory.CreateFaction(_game.GlobalManager, factionName);
            NameDB nameDB = faction.GetDataBlob<NameDB>();
            //FactionDB factionDB = faction.GetDataBlob<FactionDB>();
            Entity factioncopy = faction.Clone(faction.Manager);

            Assert.IsTrue(HasAllRequiredDatablobs(faction, requiredDataBlobs));
            Assert.IsTrue(nameDB.Name[faction] == factionName);
        }

        [Test]
        [Description("ColonyFactory test. This one use FactionFactory.CreateFaction")]
        public void CreateNewColony()
        {
            Entity faction = FactionFactory.CreateFaction(_game.GlobalManager, "Terran");
            Entity starSystem = Entity.Create(_game.GlobalManager);
            Entity planet = Entity.Create(starSystem.Manager, new List<BaseDataBlob>());

            var requiredDataBlobs = new List<Type>()
            {
                typeof(ColonyInfoDB), 
                typeof(NameDB),
                typeof(InstallationsDB)

            };

            //Entity colony = ColonyFactory.CreateColony(faction, planet);
            ColonyFactory.CreateColony(faction, planet);
            Entity colony = faction.GetDataBlob<FactionDB>().Colonies[0];
            ColonyInfoDB colonyInfoDB = colony.GetDataBlob<ColonyInfoDB>();
            //NameDB nameDB = colony.GetDataBlob<NameDB>();

            Assert.IsTrue(HasAllRequiredDatablobs(colony, requiredDataBlobs), "Colony Entity doesn't contains all required datablobs");
            Assert.IsTrue(colonyInfoDB.PlanetEntity == planet, "ColonyInfoDB.PlanetEntity refs to wrong entity");
        }

        [Test]
        [Description("CommanderFactory test. This one use FactionFactory.CreateFaction")]
        public void CreateScientist()
        {
            Entity faction = FactionFactory.CreateFaction(_game.GlobalManager, "Terran");

            var requiredDataBlobs = new List<Type>()
            {
                typeof(CommanderDB),
                typeof(ScientistBonusDB)
            };

            Entity scientist = CommanderFactory.CreateScientist(_game.GlobalManager, faction);

            Assert.IsTrue(HasAllRequiredDatablobs(scientist, requiredDataBlobs), "Scientist Entity doesn't contains all required datablobs");
        }

        [Test]
        [Description("ShipFactory test. This one use FactionFactory.CreateFaction")]
        public void CreateClassAndShip()
        {
            Entity faction = FactionFactory.CreateFaction(_game.GlobalManager, "Terran");
            StarSystem starSystem = new StarSystem();

            string shipClassName = "M6 Corvette"; //X Universe ;3
            string shipName = "USC Winterblossom"; //Still X Universe

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

            Entity shipClass = ShipFactory.CreateNewShipClass(faction, shipClassName);
            ShipInfoDB shipClassInfo = shipClass.GetDataBlob<ShipInfoDB>();
            NameDB shipClassNameDB = shipClass.GetDataBlob<NameDB>();

            Assert.IsTrue(HasAllRequiredDatablobs(shipClass, requiredDataBlobs), "ShipClass Entity doesn't contains all required datablobs");
            Assert.IsTrue(shipClassInfo.ShipClassDefinition == Guid.Empty, "Ship Class ShipInfoDB must have empty ShipClassDefinition Guid");
            Assert.IsTrue(shipClassNameDB.Name[faction] == shipClassName);

            /////Ship/////

            Entity ship = ShipFactory.CreateShip(shipClass, starSystem.SystemManager, faction, shipName);
            ShipInfoDB shipInfo = ship.GetDataBlob<ShipInfoDB>();
            NameDB shipNameDB = ship.GetDataBlob<NameDB>();

            Assert.IsTrue(HasAllRequiredDatablobs(ship, requiredDataBlobs), "Ship Entity doesn't contains all required datablobs");
            Assert.IsTrue(shipInfo.ShipClassDefinition == shipClass.Guid, "ShipClassDefinition guid must be same as ship class entity guid");
            Assert.IsTrue(shipNameDB.Name[faction] == shipName);
        }

        [Test]
        [Description("SystemBodyFactory tests")]
        public void CreateAndFillStarSystem()
        {
            StarSystem starSystem = new StarSystem();

            var starDataBlobTypes = new List<Type>()
            {
                typeof(NameDB),
                typeof(StarInfoDB),
                typeof(MassVolumeDB),
                typeof(OrbitDB)
            };

            var planetDataBlobTypes = new List<Type>()
            {
                typeof(NameDB),
                typeof(SystemBodyDB),
                typeof(MassVolumeDB),
                typeof(OrbitDB),
                typeof(AtmosphereDB)
            };

            Entity mainStar = SystemBodyFactory.CreateMainStar(starSystem.SystemManager, starSystem, "Sonra");
            Entity subStar = SystemBodyFactory.CreateSubStar(starSystem.SystemManager, mainStar, "Another little star");
            Entity planet = SystemBodyFactory.CreatePlanet(starSystem.SystemManager, mainStar, "Argon Prime");

            Assert.IsTrue(HasAllRequiredDatablobs(mainStar, starDataBlobTypes), "Main Star doesn't contains all required datablobs");
            Assert.IsTrue(HasAllRequiredDatablobs(subStar, starDataBlobTypes), "Sub Star doesn't contains all required datablobs");
            Assert.IsTrue(HasAllRequiredDatablobs(planet, planetDataBlobTypes), "Planet doesn't contains all required datablobs");

            OrbitDB mainStarOrbitDB = mainStar.GetDataBlob<OrbitDB>();
            OrbitDB subStarOrbitDB = subStar.GetDataBlob<OrbitDB>();
            OrbitDB planetOrbitDB = subStar.GetDataBlob<OrbitDB>();

            Assert.IsTrue(mainStarOrbitDB.Parent == null);
            Assert.IsTrue(subStarOrbitDB.Parent == mainStar);
            Assert.IsTrue(planetOrbitDB.Parent == mainStar);


        }

        private static bool HasAllRequiredDatablobs(Entity toCheck, List<Type> datablobTypes)
        {
            foreach (BaseDataBlob datablob in toCheck.GetAllDataBlobs())
                if (!datablobTypes.Contains(datablob.GetType()))
                    return false;
            return true;
        }
    }
}
