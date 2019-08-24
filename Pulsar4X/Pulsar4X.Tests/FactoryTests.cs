using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Test for all existing factories")]
    public class FactoryTests
    {
        private Game _game;
        private AuthenticationToken _smAuthToken;

        [SetUp]
        public void Init()
        {
            _game = new Game(new NewGameSettings {GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = 0});
            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
        }

        [Test]
        [Description("FactionFactory test")]
        public void CreateNewFaction()
        {
            string factionName = "Terran";

            Entity faction = FactionFactory.CreateFaction(_game, factionName);
            NameDB nameDB = faction.GetDataBlob<NameDB>();
            Assert.IsTrue(nameDB.GetName(faction) == factionName);

            Entity factioncopy = faction.Clone(faction.Manager);

            Assert.IsTrue(faction.GetValueCompareHash() == factioncopy.GetValueCompareHash(), "Hashes don't match");
            

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
            List<Entity> solBodies = sol.GetAllEntitiesWithDataBlob<NameDB>(_smAuthToken);
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

     

    }
}