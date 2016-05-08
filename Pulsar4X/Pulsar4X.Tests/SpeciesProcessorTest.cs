using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using System.Diagnostics;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Population Growth Test")]
    class SpeciesProcessorTest
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _atmosPlanet, _coldPlanet, _hotPlanet, _lowGravPlanet, _highGravPlanet;
        private Entity _earthPlanet;
        private Entity _faction;
        private Entity _colonyEntity;
        private GalaxyFactory _galaxyFactory;
        private StarSystemFactory _starSystemFactory;
        private StarSystem _starSystem;
        private Entity _humanSpecies;
        private Entity _exampleSpecies;
        private Dictionary<AtmosphericGasSD, string> _gasDictionary;

        [SetUp]
        public void Init()
        {
            _game = new Game(new NewGameSettings());
            StaticDataManager.LoadData("", _game);  // TODO: Figure out correct directory
            _entityManager = new EntityManager(_game);
            _faction = FactionFactory.CreateFaction(_game, "Terran");  // Terrian?


            /*_galaxyFactory = new GalaxyFactory();
            _starSystemFactory = new StarSystemFactory(_galaxyFactory);

            _starSystem = _starSystemFactory.CreateSol(_game);*/  // Seems unnecessary for now


            // Create Earth

            SystemBodyDB earthBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
            NameDB earthNameDB = new NameDB("Earth");
            earthBodyDB.Gravity = 1.0;
            earthBodyDB.BaseTemperature = 20.0f;
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>();
            atmoGasses.Add(_game.StaticData.AtmosphericGases.SelectAt(6), 0.78f);
            atmoGasses.Add(_game.StaticData.AtmosphericGases.SelectAt(9), 0.12f);
            atmoGasses.Add(_game.StaticData.AtmosphericGases.SelectAt(11), 0.01f);
            AtmosphereDB earthAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 0.3f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?

            _earthPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, earthAtmosphereDB });

            // Create example planets by copying earth, then edit them
            _coldPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, earthAtmosphereDB });
            _hotPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, earthAtmosphereDB });
            _lowGravPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, earthAtmosphereDB });
            _highGravPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, earthAtmosphereDB });

            _coldPlanet.GetDataBlob<SystemBodyDB>().BaseTemperature = -20.0f;
            _hotPlanet.GetDataBlob<SystemBodyDB>().BaseTemperature = 120.0f;
            _lowGravPlanet.GetDataBlob<SystemBodyDB>().Gravity = 0.05;
            _highGravPlanet.GetDataBlob<SystemBodyDB>().Gravity = 5.0;

            atmoGasses = new Dictionary<AtmosphericGasSD, float>();

            //@todo: figure out how to specify gases by name or symbol
            //_gasDictionary = _game.StaticData.AtmosphericGases.ToDictionary<>

            atmoGasses.Add(_game.StaticData.AtmosphericGases.SelectAt(6), 0.78f);
            atmoGasses.Add(_game.StaticData.AtmosphericGases.SelectAt(9), 0.12f);
            atmoGasses.Add(_game.StaticData.AtmosphericGases.SelectAt(11), 0.01f);
            earthAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 0.3f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?

            _atmosPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, earthAtmosphereDB });

            _faction = FactionFactory.CreateFaction(_game, "Terran");

            _humanSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);
            _exampleSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);  // To be changed in tests
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
            _faction = null;
            _earthPlanet = null;
            _examplePlanet = null;
            _colonyEntity = null;
            _starSystem = null;
            _humanSpecies = null;
            _exampleSpecies = null;
        }

        [Test]
        public void testColonyGravityIsHabitable()
        {
            // set _earthPlanet to have earth gravity (1.0)
            _earthPlanet.GetDataBlob<SystemBodyDB>().Gravity = 1.0;

            // @todo
            // test for humans on earth
            Assert.Equals(1.0, SpeciesProcessor.ColonyCost(_earthPlanet, _humanSpecies.GetDataBlob<SpeciesDB>()));


            // test for humans on a planet with low gravity
            // test for humans on a planet with gravity too low for humans to live on
            // test for humans on a planet with high gravity
            // test for humans on a planet with gravity too high for humans to live on

            // similar tests as above, but for a species with high and low ideal gravity

        }

        [Test]
        public void testColonyToxicityCost()
        {
            // @todo
            // test atmospheres with each toxic gas as the only component of the atmosphere
            // test with atmposheres composed of two toxic gases that have the same colony cost
            // test with atmospheres composed of two toxic gases that have different colony costs 

        }

        [Test]
        public void testColonyPressureCost()
        {

        }

        [Test]
        public void testColonyTemperatureCost()
        {

        }

        [Test]
        public void testColonyGasCost()
        {

        }

        [Test]
        public void testColonyCost()
        {
            // set _earthPlanet to have earth gravity (1.0)
            _earthPlanet.GetDataBlob<SystemBodyDB>().Gravity = 1.0;

            // @todo
            // test for humans on earth
            Assert.Equals(1.0, SpeciesProcessor.ColonyCost(_earthPlanet, _humanSpecies.GetDataBlob<SpeciesDB>()));
        }
    }
}
