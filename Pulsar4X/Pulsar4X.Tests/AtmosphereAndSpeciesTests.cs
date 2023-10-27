using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Engine;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Modding;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Atmosphere and Species Habitablilty Tests based on entities from runs in Aurora")]
    class AtmosphereAndSpeciesTests
    {
        ModLoader _modLoader;
        ModDataStore _modDataStore;

        NewGameSettings _settings;
        Game _game;

        private EntityManager _entityManager;
        private Entity _planet;
        private Dictionary<string, GasBlueprint> _gasDictionary;
        private SpeciesDB _humans;

        [SetUp]
        public void Init()
        {
            _modLoader = new ModLoader();
            _modDataStore = new ModDataStore();

            _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);

            _settings = new NewGameSettings() {
                MaxSystems = 1
            };

            _game  = new Game(_settings, _modDataStore);

            _entityManager = new EntityManager();
            _entityManager.Initialize(_game);

            _humans = new SpeciesDB(1, 0.1, 1.9, 1, 0, 4, 14, -10, 38);

            _gasDictionary = new Dictionary<string, GasBlueprint>();
            foreach (var (id, gas) in _game.AtmosphericGases)
            {
                _gasDictionary.Add(gas.ChemicalSymbol, gas);
            }
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
            _planet = null;
            _gasDictionary = null;
            _humans = null;
        }

        [Test]
        public void TestMercury()
        {
            var atmoGasses = new Dictionary<string, float>
            {
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(0f, false, 0m, 1.0f, 0.0f, 423.751f, atmoGasses);
            _planet = getPlanet(136.853f, 1.7f, 0.38, atmosphereDB);

            Assert.AreEqual(0.0f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.0f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.7f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(3.215, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(0, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestEarth()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["N2"].UniqueID, 0.79f },
                { _gasDictionary["O2"].UniqueID, 0.2f },
                { _gasDictionary["Ar"].UniqueID, 0.01f },
                { _gasDictionary["H2O"].UniqueID, 0.007f }
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(1.007f, true, 69.72m, 1.007f, 0.0f, 14.185f, atmoGasses);
            _planet = getPlanet(-18.0f, 1.023f, 1.0, atmosphereDB);

            Assert.AreEqual(1.007f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.101f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.023f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(0, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(0, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(0, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestLuna()
        {
            var atmoGasses = new Dictionary<string, float>
            {
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(0f, false, 0m, 1.0f, 0.0f, -18.0f, atmoGasses);
            _planet = getPlanet(-18.0f, 1.0f, 0.17, atmosphereDB);

            Assert.AreEqual(0.0f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.0f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.0f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(0.333, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(0, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestMars()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["CO2"].UniqueID, 0.0057f },
                { _gasDictionary["N2"].UniqueID, 0.0002f },
                { _gasDictionary["Ar"].UniqueID, 0.0001f }
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(0.010f, true, 10m, 1.006f, 0.006f, -60.981f, atmoGasses);
            _planet = getPlanet(-66.439f, 1.02f, 0.38, atmosphereDB);

            Assert.AreEqual(0.006f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.006f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.006f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.02f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(2.124, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(2, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestVenus()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["CO2"].UniqueID, 89.745f },
                { _gasDictionary["N2"].UniqueID, 3.255f }
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(100.0f, false, 0m, 3.0f, 89.745f, 626.689f, atmoGasses);
            _planet = getPlanet(26.896f, 1.0f, 0.91, atmosphereDB);

            Assert.AreEqual(93.0f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(89.745f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(3.0f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.0f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(24.529, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(23.250, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(2, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestTitan()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["N2"].UniqueID, 1.4268f },
                { _gasDictionary["H2"].UniqueID, 0.0029f }
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(1.43f, false, 0m, 1.158f, 0.000f, -178.623f, atmoGasses);
            _planet = getPlanet(-190.428f, 1.000f, 0.14, atmosphereDB);

            Assert.AreEqual(1.430f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.143f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.0f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(7.026, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(2, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestIo()
        {
            var atmoGasses = new Dictionary<string, float>
            {
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(0f, false, 0m, 1.0f, 0.0f, -161.207f, atmoGasses);
            _planet = getPlanet(-161.207f, 1.0f, 0.18, atmosphereDB);

            Assert.AreEqual(0.0f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.0f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.0f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(6.3, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(0, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestMinervaM2()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["He"].UniqueID, 0.4073f },
                { _gasDictionary["H2"].UniqueID, 0.1217f },      // FROZEN
                { _gasDictionary["Ne"].UniqueID, 0.0049f }       // FROZEN
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(0.276f, false, 0m, 1.041f, 0.000f, Temperature.ToCelsius(18.086f), atmoGasses);
            _planet = getPlanet(Temperature.ToCelsius(20.706f), 0.85f, 0.73,  atmosphereDB);

            Assert.AreEqual(0.407f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.028f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(0.85f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(10.195, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(0, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestMinervaM8()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["H2"].UniqueID, 1.7006f }
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(1.701f, false, 0m, 1.170f, 0.000f, -242.958f, atmoGasses);
            _planet = getPlanet(-252.294f, 1.24f, 1.3, atmosphereDB);

            Assert.AreEqual(1.701f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.17f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.24f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(9.707, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(2, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestMinervaM18()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["H2"].UniqueID, 0.5268f },
                { _gasDictionary["He"].UniqueID, 0.2714f },
                { _gasDictionary["Ar"].UniqueID, 0.1004f }      // FROZEN
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(0.597f, false, 0m, 1.08f, 0.000f, -247.985f, atmoGasses);
            _planet = getPlanet(-252.294f, 1.140f, 0.89, atmosphereDB);

            Assert.AreEqual(0.798f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.06f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(1.14f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(9.896, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(2, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestOrpheus()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["CO2"].UniqueID, 0.3155f },
                { _gasDictionary["N2"].UniqueID, 0.0132f },
                { _gasDictionary["H2O"].UniqueID, 0.0000329f }
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(0.329f, false, 0m, 1.348f, 0.316f, Temperature.ToCelsius(325.965f), atmoGasses);
            _planet = getPlanet(Temperature.ToCelsius(309.93f), 0.78f, 0.57, atmosphereDB);

            Assert.AreEqual(0.329f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.316f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.348f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(0.78f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(1.992, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(2, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        [Test]
        public void TestSolstice_B_III()
        {
            var atmoGasses = new Dictionary<string, float>
            {
                { _gasDictionary["H2"].UniqueID, 0.0578f },
                { _gasDictionary["He"].UniqueID, 0.0385f },
                { _gasDictionary["N2"].UniqueID, 0.0027f }              // FROZEN
            };
            AtmosphereDB atmosphereDB = new AtmosphereDB(0.094f, true, 8m, 1.009f, 0.0f, Temperature.ToCelsius(51.655f), atmoGasses);
            _planet = getPlanet(Temperature.ToCelsius(64.78f), 0.79f, .31, atmosphereDB);

            Assert.AreEqual(0.096f, atmosphereDB.GetAtmosphericPressure(), 0.001, "Atmospheric Pressure");
            Assert.AreEqual(0.0f, atmosphereDB.GetGreenhousePressure(), 0.01, "Greenhouse Gas Pressure");
            Assert.AreEqual(1.009f, atmosphereDB.CalculatedGreenhouseFactor(), 0.001, "Greenhouse Factor");

            var calcSurfaceTemp = _planet.GetDataBlob<AtmosphereDB>().CalulatedSurfaceTemperature(0.79f);
            Assert.AreEqual(atmosphereDB.SurfaceTemperature, calcSurfaceTemp, 0.5, "Surface Temperature");
            atmosphereDB.SurfaceTemperature = calcSurfaceTemp;

            // Test Humans habitability
            Assert.IsTrue(_humans.CanSurviveGravityOn(_planet));
            Assert.AreEqual(1.992, _humans.ColonyTemperatureCost(_planet), 0.1, "Temperature");
            Assert.AreEqual(0, _humans.ColonyPressureCost(_planet), 0.1, "Pressure");
            Assert.AreEqual(2, _humans.ColonyGasCost(_planet), 0.1, "Breathability");
            Assert.AreEqual(2, _humans.ColonyToxicityCost(_planet), 0.1, "Toxicity");
        }

        // Sets a planet entity to earth normal
        private Entity getPlanet(float baseTemperature, float albedo, double gravity, AtmosphereDB atmos)
        {
            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true };
            planetBodyDB.Gravity = gravity;
            planetBodyDB.BaseTemperature = baseTemperature;
            planetBodyDB.Albedo = new PercentValue(albedo);

            NameDB planetNameDB = new NameDB("Test Planet");

            Entity resultPlanet = Entity.Create();
            _entityManager.AddEntity(resultPlanet, new List<BaseDataBlob> { planetBodyDB, planetNameDB, atmos });

            return resultPlanet;
        }
    }
}
