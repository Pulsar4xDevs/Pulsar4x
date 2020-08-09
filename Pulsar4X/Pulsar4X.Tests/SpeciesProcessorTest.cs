using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Species Processor Test")]
    class SpeciesProcessorTest
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _atmosPlanet, _coldPlanet, _hotPlanet, _lowGravPlanet, _highGravPlanet, _noatmosPlanet, _weirdatmosPlanet;
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
            var gameSettings = new NewGameSettings();
            gameSettings.MaxSystems = 10;
            _game = new Game(gameSettings);
            StaticDataManager.LoadData("Pulsar4x", _game);
            _entityManager = new EntityManager(_game);
            _faction = FactionFactory.CreateFaction(_game, "Terran");  // Terrian?


            /*_galaxyFactory = new GalaxyFactory();
            _starSystemFactory = new StarSystemFactory(_galaxyFactory);

            _starSystem = _starSystemFactory.CreateSol(_game);*/  // Seems unnecessary for now

            _gasDictionary = new Dictionary<string, AtmosphericGasSD>();

            foreach (WeightedValue<AtmosphericGasSD> atmos in _game.StaticData.AtmosphericGases)
            {
                _gasDictionary.Add(atmos.Value.ChemicalSymbol, atmos.Value);
            }


            // Create Earth
            _earthPlanet = setEarthPlanet();
            _coldPlanet = setEarthPlanet();
            _hotPlanet = setEarthPlanet();
            _lowGravPlanet = setEarthPlanet();
            _highGravPlanet = setEarthPlanet();


            _coldPlanet.GetDataBlob<SystemBodyInfoDB>().BaseTemperature = -20.0f;
            _hotPlanet.GetDataBlob<SystemBodyInfoDB>().BaseTemperature = 120.0f;
            _lowGravPlanet.GetDataBlob<SystemBodyInfoDB>().Gravity = 0.05;
            _highGravPlanet.GetDataBlob<SystemBodyInfoDB>().Gravity = 5.0;

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
            _lowGravSpecies.GetDataBlob<SpeciesDB>().MinimumGravityConstraint = 0.01;


            _highGravSpecies.GetDataBlob<SpeciesDB>().BaseGravity = 4.0;
            _highGravSpecies.GetDataBlob<SpeciesDB>().MaximumGravityConstraint = 5.5;
            _highGravSpecies.GetDataBlob<SpeciesDB>().MinimumGravityConstraint = 4.5;

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
            // @todo: set up two nested loops - one for list of species, one for list of gravities
            // test a large number of different inputs

            // set _earthPlanet to have earth gravity (1.0)
            _earthPlanet.GetDataBlob<SystemBodyInfoDB>().Gravity = 1.0;

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
            SystemBodyInfoDB earthBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true };
            NameDB earthNameDB = new NameDB("Earth");
            double expectedCost;
            string gasSym;

            var lowToxicGases = new Dictionary<string, AtmosphericGasSD>();
            var highToxicGases = new Dictionary<string, AtmosphericGasSD>();
            var benignGases = new Dictionary<string, AtmosphericGasSD>();
            AtmosphericGasSD oxygenGas = new AtmosphericGasSD();

            //Separate all the gases into the lists above
            foreach (KeyValuePair<string, AtmosphericGasSD> kvp in _gasDictionary)
            {
                gasSym = kvp.Key;
                if (kvp.Value.IsHighlyToxic)
                {
                    highToxicGases.Add(gasSym, kvp.Value);
                }
                else if (kvp.Value.IsToxic)
                {
                    lowToxicGases.Add(gasSym, kvp.Value);
                }
                else if (gasSym == "O2")
                {
                    oxygenGas = kvp.Value;
                }
                else
                {
                    benignGases.Add(gasSym, kvp.Value);
                }
            }

            // @todo: set up two nested loops - one for list of species, one for list of gases
            // test a large number of different inputs
            AtmosphereDB weirdAtmosphereDB;
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>();
            var humans = _humanSpecies.GetDataBlob<SpeciesDB>();

            // Test the "low" toxic gases (colony cost 2.0 minimum
            foreach (KeyValuePair<string, AtmosphericGasSD> kvp in lowToxicGases)
            {
                expectedCost = 2.0;
                gasSym = kvp.Key;
                atmoGasses.Clear();
                atmoGasses.Add(_gasDictionary[gasSym], 0.1f);
                weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
                _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);

                Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));
            }

            // Test the "high" toxic gases (colony cost 3.0 minimum)
            foreach (KeyValuePair<string, AtmosphericGasSD> kvp in highToxicGases)
            {
                expectedCost = 3.0;
                gasSym = kvp.Key;
                atmoGasses.Clear();
                atmoGasses.Add(_gasDictionary[gasSym], 0.1f);
                weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
                _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);

                Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));
            }

            // Test the "benign" toxic gases (no affect on colony cost, but no oxygen means 2.0)
            foreach (KeyValuePair<string, AtmosphericGasSD> kvp in lowToxicGases)
            {
                expectedCost = 2.0;
                gasSym = kvp.Key;
                atmoGasses.Clear();
                atmoGasses.Add(_gasDictionary[gasSym], 0.1f);
                weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
                _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);

                Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));
            }

            // test with atmposheres composed of two toxic gases that have the same colony cost
            foreach (KeyValuePair<string, AtmosphericGasSD> kvp1 in lowToxicGases)
            {
                foreach (KeyValuePair<string, AtmosphericGasSD> kvp2 in lowToxicGases)
                {
                    expectedCost = 2.0;
                    string gasSym1 = kvp1.Key;
                    string gasSym2 = kvp2.Key;
                    if (gasSym1 == gasSym2)
                        continue;

                    atmoGasses.Clear();
                    atmoGasses.Add(lowToxicGases[gasSym1], 0.1f);
                    atmoGasses.Add(lowToxicGases[gasSym2], 0.1f);
                    weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
                    _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);

                    Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));
                }
            }

            // test with atmposheres composed of two toxic gases that have the same colony cost
            foreach (KeyValuePair<string, AtmosphericGasSD> kvp1 in highToxicGases)
            {
                foreach (KeyValuePair<string, AtmosphericGasSD> kvp2 in highToxicGases)
                {
                    expectedCost = 3.0;
                    string gasSym1 = kvp1.Key;
                    string gasSym2 = kvp2.Key;
                    if (gasSym1 == gasSym2)
                        continue;

                    atmoGasses.Clear();
                    atmoGasses.Add(highToxicGases[gasSym1], 0.1f);
                    atmoGasses.Add(highToxicGases[gasSym2], 0.1f);
                    weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
                    _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);

                    Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));
                }
            }

            // test with atmospheres composed of two toxic gases that have different colony costs 
            
            foreach (KeyValuePair<string, AtmosphericGasSD> kvp1 in lowToxicGases)
            {
                foreach (KeyValuePair<string, AtmosphericGasSD> kvp2 in highToxicGases)
                {
                    expectedCost = 3.0;
                    string gasSym1 = kvp1.Key;
                    string gasSym2 = kvp2.Key;
                    if (gasSym1 == gasSym2)
                        continue;

                    atmoGasses.Clear();
                    atmoGasses.Add(lowToxicGases[gasSym1], 0.1f);
                    atmoGasses.Add(highToxicGases[gasSym2], 0.1f);
                    weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
                    _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);

                    Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));
                }
            }


            //Toxic Gasses(CC = 2): Hydrogen(H2), Methane(CH4), Ammonia(NH3), Carbon Monoxide(CO), Nitrogen Monoxide(NO), Hydrogen Sulfide(H2S), Nitrogen Dioxide(NO2), Sulfur Dioxide(SO2)

            //Toxic Gasses(CC = 3): Chlorine(Cl2), Florine(F2), Bromine(Br2), and Iodine(I2)


            //Toxic Gasses at trigger high colony cost when meeting their toxicity threshold. *
            var gasesThatCanBecomeToxic = _gasDictionary.Values.Where(x => x.IsToxicAtPercentage.HasValue).ToList();
            if (gasesThatCanBecomeToxic.Any())
            {
                var firstBenignGas = _gasDictionary.Values.FirstOrDefault(x => !x.IsToxic && !x.IsHighlyToxic && !x.IsToxicAtPercentage.HasValue);
                Assert.IsNotNull(firstBenignGas, "No Benign Gases Available To Enable Testing");

                foreach (AtmosphericGasSD gasUnderTest in gasesThatCanBecomeToxic)
                {
                    var toxicityLevel = gasUnderTest.IsToxicAtPercentage.Value / 100.0f;

                    // Test when gas under test is Exactly on the Toxicity Threshold
                    expectedCost = 2.0;
                    atmoGasses.Clear();
                    atmoGasses.Add(firstBenignGas, 1.0f - toxicityLevel);
                    atmoGasses.Add(gasUnderTest, toxicityLevel);
                    weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses);
                    _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);
                    Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));

                    // Test when gas under test is Less than on the Toxicity Threshold
                    expectedCost = gasUnderTest.ChemicalSymbol.Equals("O2") ? 1 : 2;
                    atmoGasses.Clear();
                    atmoGasses.Add(firstBenignGas, 1.0f - toxicityLevel + 0.01f);
                    atmoGasses.Add(gasUnderTest, toxicityLevel - 0.01f);
                    weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses);
                    _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);
                    Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));


                    // Test when gas under test is Over the Toxicity Threshold
                    expectedCost = 2.0;
                    atmoGasses.Clear();
                    atmoGasses.Add(firstBenignGas, 1.0f - toxicityLevel - 0.01f);
                    atmoGasses.Add(gasUnderTest, toxicityLevel + 0.01f);
                    weirdAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses);
                    _weirdatmosPlanet = setAtmosphere(weirdAtmosphereDB);
                    Assert.AreEqual(expectedCost, humans.ColonyCost(_weirdatmosPlanet));
                }
            }


            Assert.AreEqual(1.0, humans.ColonyCost(_earthPlanet));
        }

        [Test]
        public void testColonyPressureCost()
        {
            Entity testPlanet;
            double idealPressure;
            double maxPressure;
            double expected;
            float totalPressure;

            var humans = _humanSpecies.GetDataBlob<SpeciesDB>();
            idealPressure = humans.BasePressure;
            maxPressure = humans.MaximumPressureConstraint;

            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>();

            for (float i = 0.3f; i < 10.0; i += 0.1f)
            {
                AtmosphereDB testAtmosphereDB;
                atmoGasses.Clear();
                
                // Keep atmosphere breathable
                atmoGasses.Add(_gasDictionary["N2"], (float)(2.0f * i));
                totalPressure = 2.0f * i;
                atmoGasses.Add(_gasDictionary["O2"], 0.1f);
                totalPressure += 0.1f;

                    

                testAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses); //TODO what's our greenhouse factor an pressure?
                testPlanet = setAtmosphere(testAtmosphereDB);

                if (totalPressure > 4.0)
                    expected = 2.0;
                else
                    expected = 1.0;
                Assert.AreEqual(expected, humans.ColonyCost(testPlanet));

            }

            // Check with pressure from just one gas and multiple gases

            //throw (new NotImplementedException());
            Assert.AreEqual(1.0, humans.ColonyCost(_earthPlanet));
        }

        [Test]
        public void testColonyTemperatureCost()
        {
            Entity tempPlanet;
            double idealTemp;
            double tempRange;
            double expected;

            tempPlanet = setEarthPlanet();

            List<Entity> species = new List<Entity>();

            species.Add(_lowTempSpecies);
            species.Add(_highTempSpecies);
            species.Add(_humanSpecies);

            // Check each species - more can be added above
            foreach (Entity spec in species)
            {
                var specDb = spec.GetDataBlob<SpeciesDB>();
                idealTemp = specDb.BaseTemperature;
                tempRange = specDb.TemperatureToleranceRange;

                // Check a wide variety of temperatures
                for (float temp = -200; temp < 500; temp++)
                {
                    tempPlanet.GetDataBlob<SystemBodyInfoDB>().BaseTemperature = temp;
                    expected = Math.Abs((idealTemp + 273.15) - (temp + 273.15)) / tempRange;
                    expected = Math.Max(1.0, expected);
                    Assert.AreEqual(expected, specDb.ColonyCost(tempPlanet));
                }
            }

            //throw (new NotImplementedException());
            Assert.AreEqual(1.0, _humanSpecies.GetDataBlob<SpeciesDB>().ColonyCost(_earthPlanet));
        }

        [Test]
        public void testColonyGasCost()
        {
            Entity testPlanet;
            double expected;
            float oRatio;

            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>();

            var nitrogen = _gasDictionary["N2"];
            var oxygen = _gasDictionary["O2"];
            for (int i = 0; i < 40; i++)
            {
                for (int j = 0; j < 20 ; j++)
                {
                    if (i + j > 40) // pressure too high, would fail the pressure test
                        continue;

                    AtmosphereDB testAtmosphereDB;
                    atmoGasses.Clear();

                    atmoGasses.Add(nitrogen, i * 0.1f);
                    atmoGasses.Add(oxygen, j * 0.1f);

                    testAtmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses);
                    testPlanet = setAtmosphere(testAtmosphereDB);

                    if (i + j == 0)
                        oRatio = 0.0f;
                    else
                        oRatio = j * 1.0f / (i + j);

                    if (j < 1|| j > 3)
                        expected = 2.0;
                    else if ((oRatio <= 0.30f && j >= 1) && oRatio < (oxygen.IsToxicAtPercentage / 100.0f))
                        expected = 1.0;
                    else
                        expected = 2.0;

                    Assert.AreEqual(expected, _humanSpecies.GetDataBlob<SpeciesDB>().ColonyCost(testPlanet));
                }
            }

            //throw (new NotImplementedException());
        }

        [Test]
        public void testColonyCost()
        {
            // @todo
            // test for humans on earth

            Assert.AreEqual(1.0, _humanSpecies.GetDataBlob<SpeciesDB>().ColonyCost(_earthPlanet));
        }

        // Sets a planet entity to earth normal
        private Entity setEarthPlanet()
        {
            Entity resultPlanet;
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>();

            atmoGasses.Add(_gasDictionary["N2"], 0.79f);
            atmoGasses.Add(_gasDictionary["O2"], 0.20f);
            atmoGasses.Add(_gasDictionary["Ar"], 0.01f);
            AtmosphereDB atmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses);
            resultPlanet = setAtmosphere(atmosphereDB);

            return resultPlanet;
        }

        // Sets an entity to earth normal aside from the atmosphere
        private Entity setAtmosphere(AtmosphereDB atmosDB)
        {
            SystemBodyInfoDB earthBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true };
            NameDB earthNameDB = new NameDB("Earth");
            earthBodyDB.Gravity = 1.0;
            earthBodyDB.BaseTemperature = 20.0f;

            Entity resultPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, atmosDB });
            return resultPlanet;
        }
    }
}
