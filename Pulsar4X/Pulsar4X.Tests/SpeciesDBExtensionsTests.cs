using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Atmosphere and Species Habitablilty Tests based on entities from runs in Aurora")]
    class SpeciesDBExtensionsTests
    {
        private Game _game;
        private EntityManager _entityManager;
        private Dictionary<string, AtmosphericGasSD> _gasDictionary;
        private SpeciesDB _humans;
        private AtmosphereDB _atmosphere;

        [SetUp]
        public void Init()
        {
            var gameSettings = new NewGameSettings
            {
                MaxSystems = 1
            };

            _game = new Game(gameSettings);
            StaticDataManager.LoadData("Pulsar4x", _game);
            _entityManager = new EntityManager(_game);

            _humans = new SpeciesDB(
                baseGravity: 1, minGravity: 0.1, maxGravity: 1.9,
                basePressure: 1, minPressure: 0, maxPressure: 4,
                baseTemp: 14, minTemp: -10, maxTemp: 38);

            _gasDictionary = new Dictionary<string, AtmosphericGasSD>();
            foreach (WeightedValue<AtmosphericGasSD> atmos in _game.StaticData.AtmosphericGases)
            {
                _gasDictionary.Add(atmos.Value.ChemicalSymbol, atmos.Value);
            }

            _atmosphere = new(
                pressure: 0f,
                hydrosphere: false,
                hydroExtent: 0m,
                greenhouseFactor: 1.0f,
                greenhousePressue: 0.0f,
                surfaceTemp: 100f,
                new Dictionary<AtmosphericGasSD, float>()
            );
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
            _gasDictionary = null;
            _humans = null;
        }

        [Test]
        public void TestHighCanSurviveOnGravity()
        {
            Entity highGravityPlanet = GetPlanet(10, 1f, gravity: 20);
            Assert.AreEqual(false, _humans.CanSurviveGravityOn(highGravityPlanet), "CanSurviveOnGravity (Too High Gravity)");
        }

        [Test]
        public void TestLowCanSurviveOnGravity()
        {
            Entity lowGravityPlanet = GetPlanet(10, 1f, gravity: 0.001);
            Assert.AreEqual(false, _humans.CanSurviveGravityOn(lowGravityPlanet), "CanSurviveOnGravity (Too Low Gravity)");
        }

        [Test]
        public void TestInRangeCanSurviveOnGravity()
        {
            Entity inRangeGravityPlanet = GetPlanet(10, 1f, gravity: 1);
            Assert.AreEqual(true, _humans.CanSurviveGravityOn(inRangeGravityPlanet), "CanSurviveOnGravity (In Range Gravity)");
        }

        [Test]
        public void TestColdColonyTemperatureCost()
        {
            // Human Range: (-10, 38)
            // Cold Planet: -100
            // Deviation: (38 - (-10)) / 2 = 24
            // Diff: -100 - (-10) = 90
            // Cost: 90 / 24 = 3.75
            Entity coldPlanet = GetPlanet(baseTemperature: -100, 1f, 1);
            Assert.AreEqual(3.75, _humans.ColonyTemperatureCost(coldPlanet), 0.1, "ColonyTemperatureCost (Cold Planet)");
        }

        [Test]
        public void TestHotColonyTemperatureCost()
        {
            // Human Range: (-10, 38)
            // Hot Planet: 100
            // Deviation: (38 - (-10)) / 2 = 24
            // Diff: 100 - 38 = 62
            // Cost: 62 / 24 = 2.58
            Entity hotPlanet = GetPlanet(baseTemperature: 100, 1f, 1);
            Assert.AreEqual(2.58, _humans.ColonyTemperatureCost(hotPlanet), 0.1, "ColonyTemperatureCost (Hot Planet)");
        }

        [Test]
        public void TestInRangeColonyTemperatureCost()
        {
            // Entities in the species temperature range return 0 as the cost
            Entity inRangePlanet = GetPlanet(baseTemperature: 10, 1f, 1);
            Assert.AreEqual(0, _humans.ColonyTemperatureCost(inRangePlanet), 0.1, "ColonyTemperatureCost (In Range)");
        }

        private Entity GetPlanet(float baseTemperature, float albedo, double gravity, AtmosphereDB atmosphere = null)
        {
            SystemBodyInfoDB planetBodyDB = new() {
                BodyType = BodyType.Terrestrial,
                SupportsPopulations = true,
                Gravity = gravity,
                BaseTemperature = baseTemperature,
                Albedo = new PercentValue(albedo)
            };
            NameDB planetNameDB = new("Test Planet");

            return new(_entityManager, new List<BaseDataBlob> { planetBodyDB, planetNameDB, atmosphere });
        }
    }
}