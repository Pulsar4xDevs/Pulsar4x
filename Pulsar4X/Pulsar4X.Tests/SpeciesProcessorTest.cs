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
    [TestFixture, Description("Species Processor Test")]
    class SpeciesProcessorTest
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _atmosPlanet, _coldPlanet, _hotPlanet, _lowGravPlanet, _highGravPlanet, _noatmosPlanet;
        private Entity _earthPlanet;
        private Entity _faction;
        private Entity _colonyEntity;
        private GalaxyFactory _galaxyFactory;
        private StarSystemFactory _starSystemFactory;
        private StarSystem _starSystem;
        private Entity _humanSpecies;
        private Entity _lowGravSpecies, _highGravSpecies, _lowTempSpecies, _highTempSpecies;
        private Entity _exampleSpecies;
        private Dictionary<string, AtmosphericGasSD> _gasDictionary;

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

            _gasDictionary = new Dictionary<string, AtmosphericGasSD>();

            foreach(WeightedValue<AtmosphericGasSD> atmos in _game.StaticData.AtmosphericGases)
            {
                _gasDictionary.Add(atmos.Value.ChemicalSymbol, atmos.Value);
            }

            // Empty atmosphere
            atmoGasses = new Dictionary<AtmosphericGasSD, float>();    
            earthAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 0.3f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
            _noatmosPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, earthAtmosphereDB });

            // Nonstandard atmosphere
            atmoGasses = new Dictionary<AtmosphericGasSD, float>();
            atmoGasses.Add(_gasDictionary["N"], 0.05f);
            earthAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 0.3f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
            _noatmosPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, earthAtmosphereDB });


            _faction = FactionFactory.CreateFaction(_game, "Terran");

            _humanSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);
            _exampleSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);  
            _lowGravSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);  
            _highGravSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager); 
            _lowTempSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);
            _highTempSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);

            _humanSpecies.GetDataBlob<SpeciesDB>().TemperatureToleranceRange = 20;
            _exampleSpecies.GetDataBlob<SpeciesDB>().TemperatureToleranceRange = 20;
            _lowGravSpecies.GetDataBlob<SpeciesDB>().TemperatureToleranceRange = 20;
            _highGravSpecies.GetDataBlob<SpeciesDB>().TemperatureToleranceRange = 20;
            _lowTempSpecies.GetDataBlob<SpeciesDB>().TemperatureToleranceRange = 20;
            _highTempSpecies.GetDataBlob<SpeciesDB>().TemperatureToleranceRange = 20;


            _lowGravSpecies.GetDataBlob<SpeciesDB>().BaseGravity = 0.4;
            _lowGravSpecies.GetDataBlob<SpeciesDB>().MaximumGravityConstraint = 0.5;
            _lowGravSpecies.GetDataBlob<SpeciesDB>().MinimumGravityConstraint = 0.1;

            _highGravSpecies.GetDataBlob<SpeciesDB>().BaseGravity = 3.0;
            _highGravSpecies.GetDataBlob<SpeciesDB>().MaximumGravityConstraint = 3.5;
            _highGravSpecies.GetDataBlob<SpeciesDB>().MinimumGravityConstraint = 2.5;

            _lowTempSpecies.GetDataBlob<SpeciesDB>().BaseTemperature = -50.0;
            _lowTempSpecies.GetDataBlob<SpeciesDB>().MaximumTemperatureConstraint = 0.0;
            _lowTempSpecies.GetDataBlob<SpeciesDB>().MinimumTemperatureConstraint = -100.0;

            _highTempSpecies.GetDataBlob<SpeciesDB>().BaseTemperature = 200.0;
            _highTempSpecies.GetDataBlob<SpeciesDB>().MaximumTemperatureConstraint = 300.0;
            _highTempSpecies.GetDataBlob<SpeciesDB>().MinimumTemperatureConstraint = 100.0;
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
            _faction = null;
            _earthPlanet = null;
            _atmosPlanet = null;
            _coldPlanet = null;
            _hotPlanet = null;
            _lowGravPlanet = null;
            _highGravPlanet = null;
            _noatmosPlanet = null;
            _colonyEntity = null;
            _starSystem = null;
            _humanSpecies = null;
            _exampleSpecies = null;
            _lowGravSpecies = null;
            _highGravSpecies = null;
            _lowTempSpecies = null;
            _highTempSpecies = null;
        }

        [Test]
        public void testColonyGravityIsHabitable()
        {
            // set _earthPlanet to have earth gravity (1.0)
            _earthPlanet.GetDataBlob<SystemBodyDB>().Gravity = 1.0;

            // @todo
            // test for humans on earth

            Assert.AreEqual(1.0, SpeciesProcessor.ColonyCost(_earthPlanet, _humanSpecies.GetDataBlob<SpeciesDB>()));

            // test for humans on a planet with low gravity
            Assert.AreEqual(-1.0, SpeciesProcessor.ColonyCost(_lowGravPlanet, _humanSpecies.GetDataBlob<SpeciesDB>()));

            // test for humans on a planet with high gravity
            Assert.AreEqual(-1.0, SpeciesProcessor.ColonyCost(_highGravPlanet, _humanSpecies.GetDataBlob<SpeciesDB>()));

            // similar tests as above, but for a species with high and low ideal gravity
            Assert.AreEqual(-1.0, SpeciesProcessor.ColonyCost(_earthPlanet, _lowGravSpecies.GetDataBlob<SpeciesDB>()));
            Assert.AreEqual(-1.0, SpeciesProcessor.ColonyCost(_highGravPlanet, _lowGravSpecies.GetDataBlob<SpeciesDB>()));
            Assert.AreEqual(1.0, SpeciesProcessor.ColonyCost(_lowGravPlanet, _lowGravSpecies.GetDataBlob<SpeciesDB>()));

            Assert.AreEqual(-1.0, SpeciesProcessor.ColonyCost(_earthPlanet, _highGravSpecies.GetDataBlob<SpeciesDB>()));
            Assert.AreEqual(1.0, SpeciesProcessor.ColonyCost(_highGravPlanet, _highGravSpecies.GetDataBlob<SpeciesDB>()));
            Assert.AreEqual(-1.0, SpeciesProcessor.ColonyCost(_lowGravPlanet, _highGravSpecies.GetDataBlob<SpeciesDB>()));
        }

        [Test]
        public void testColonyToxicityCost()
        {
            // @todo
            // test atmospheres with each toxic gas as the only component of the atmosphere
            // test with atmposheres composed of two toxic gases that have the same colony cost
            // test with atmospheres composed of two toxic gases that have different colony costs 

            //Toxic Gasses(CC = 2): Hydrogen(H2), Methane(CH4), Ammonia(NH3), Carbon Monoxide(CO), Nitrogen Monoxide(NO), Hydrogen Sulfide(H2S), Nitrogen Dioxide(NO2), Sulfur Dioxide(SO2)
            //Toxic Gasses(CC = 3): Chlorine(Cl2), Florine(F2), Bromine(Br2), and Iodine(I2)
            //Toxic Gasses at 30% or greater of atm: Oxygen(O2) *


            Assert.AreEqual(1.0, SpeciesProcessor.ColonyCost(_earthPlanet, _humanSpecies.GetDataBlob<SpeciesDB>()));


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
            // @todo
            // test for humans on earth

            Assert.AreEqual(1.0, SpeciesProcessor.ColonyCost(_earthPlanet, _humanSpecies.GetDataBlob<SpeciesDB>() ));
        }
    }
}
