using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.ECSLib;

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
            _game = Game.NewGame("Unit Test Game", DateTime.Now, 0);
        }

        [Test]
        [Description("FactionFactory test")]
        public void CreateNewFaction()
        {
            string factionName = "Terran";

            var requiredDataBlobs = new List<Type>()
            {
                typeof(FactionInfoDB),
                typeof(FactionAbilitiesDB),
                typeof(NameDB),
                typeof(FactionTechDB)
            };

            Entity faction = FactionFactory.CreateFaction(_game, factionName);
            NameDB nameDB = faction.GetDataBlob<NameDB>();
            //FactionDB factionDB = faction.GetDataBlob<FactionDB>();
            Entity factioncopy = faction.Clone(faction.Manager);

            //Assert.IsTrue(HasAllRequiredDatablobs(faction, requiredDataBlobs));
            Assert.IsTrue(nameDB.GetName(faction) == factionName);
        }

        [Test]
        [Description("ColonyFactory test. This one use FactionFactory.CreateFaction")]
        public void CreateNewColony()
        {
            Entity faction = FactionFactory.CreateFaction(_game, "Terran");
            StarSystemFactory sysfac = new StarSystemFactory(_game);
            StarSystem sol = sysfac.CreateSol(_game);
            //Entity starSystem = Entity.Create(_game.GlobalManager);
            //Entity planet = Entity.Create(starSystem.Manager, new List<BaseDataBlob>());
            List<Entity> solBodies = sol.SystemManager.GetAllEntitiesWithDataBlob<NameDB>();
            Entity planet = solBodies.Find(item => item.GetDataBlob<NameDB>().DefaultName == "Earth");
            Entity species = SpeciesFactory.CreateSpeciesHuman(faction, _game.GlobalManager);
            var requiredDataBlobs = new List<Type>()
            {
                typeof(ColonyInfoDB), 
                typeof(NameDB),
                typeof(InstallationsDB)

            };

            //Entity colony = ColonyFactory.CreateColony(faction, planet);
            ColonyFactory.CreateColony(faction, species, planet);
            Entity colony = faction.GetDataBlob<FactionInfoDB>().Colonies[0];
            ColonyInfoDB colonyInfoDB = colony.GetDataBlob<ColonyInfoDB>();
            //NameDB nameDB = colony.GetDataBlob<NameDB>();

            //Assert.IsTrue(HasAllRequiredDatablobs(colony, requiredDataBlobs), "Colony Entity doesn't contains all required datablobs");
            Assert.IsTrue(colonyInfoDB.PlanetEntity == planet, "ColonyInfoDB.PlanetEntity refs to wrong entity");
        }

        [Test]
        [Description("CommanderFactory test. This one use FactionFactory.CreateFaction")]
        public void CreateScientist()
        {
            Entity faction = FactionFactory.CreateFaction(_game, "Terran");

            var requiredDataBlobs = new List<Type>()
            {
                typeof(CommanderDB),
                typeof(ScientistDB)
            };

            Entity scientist = CommanderFactory.CreateScientist(_game.GlobalManager, faction);

            //Assert.IsTrue(HasAllRequiredDatablobs(scientist, requiredDataBlobs), "Scientist Entity doesn't contains all required datablobs");
        }

        [Test]
        [Description("ShipFactory test. This one use FactionFactory.CreateFaction")]
        public void CreateClassAndShip()
        {
            Entity faction = FactionFactory.CreateFaction(_game, "Terran");
            StarSystem starSystem = new StarSystem(_game, "Sol", -1);

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

            Entity shipClass = ShipFactory.CreateNewShipClass(_game, faction, shipClassName);
            ShipInfoDB shipClassInfo = shipClass.GetDataBlob<ShipInfoDB>();
            NameDB shipClassNameDB = shipClass.GetDataBlob<NameDB>();

            //Assert.IsTrue(HasAllRequiredDatablobs(shipClass, requiredDataBlobs), "ShipClass Entity doesn't contains all required datablobs");
            Assert.IsTrue(shipClassInfo.ShipClassDefinition == Guid.Empty, "Ship Class ShipInfoDB must have empty ShipClassDefinition Guid");
            Assert.IsTrue(shipClassNameDB.GetName(faction) == shipClassName);

            /////Ship/////

            Entity ship = ShipFactory.CreateShip(shipClass, starSystem.SystemManager, faction, shipName);
            ShipInfoDB shipInfo = ship.GetDataBlob<ShipInfoDB>();
            NameDB shipNameDB = ship.GetDataBlob<NameDB>();

            //Assert.IsTrue(HasAllRequiredDatablobs(ship, requiredDataBlobs), "Ship Entity doesn't contains all required datablobs");
            Assert.IsTrue(shipInfo.ShipClassDefinition == shipClass.Guid, "ShipClassDefinition guid must be same as ship class entity guid");
            Assert.IsTrue(shipNameDB.GetName(faction) == shipName);
        }

        /// <summary>
        /// fuck this test, it doesn't make sense. TODO: somone re-write this so it does what the name implies?
        /// </summary>
        /// <param name="toCheck"></param>
        /// <param name="datablobTypes"></param>
        /// <returns></returns>
        //private static bool HasAllRequiredDatablobs(Entity toCheck, List<Type> datablobTypes)
        //{
        //    var entityDataBlobs = toCheck.DataBlobs;
        //    foreach (BaseDataBlob datablob in toCheck.DataBlobs)
        //        if (!datablobTypes.Contains(datablob.GetType()))
        //            return false;
        //    return true;
        //}
    }
}