using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Test the AtmosphereDBExtension methods")]
    class AtmosphereDBExtensionsTests
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
            _humans.BreathableGasSymbol = "O2";

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
        public void TestFrozenWouldBeFrozenAtGiveTemperature()
        {
            AtmosphericGasSD gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 10
            };
            Assert.AreEqual(true, gas.WouldBeFrozenAtGivenTemperature(5f), "WouldBeFrozenAtGivenTemperature (Frozen)");
        }

        [Test]
        public void TestNotFrozenWouldBeFrozenAtGiveTemperature()
        {
            AtmosphericGasSD gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0
            };
            Assert.AreEqual(false, gas.WouldBeFrozenAtGivenTemperature(5f), "WouldBeFrozenAtGivenTemperature (Not Frozen)");
        }

        [Test]
        public void TestNoFrozenGasGetAtmosphericPressure()
        {
            float pressure = 1f;
            AtmosphericGasSD gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0
            };
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetAtmosphericPressure(), 0.1, "GetAtmosphericPressure (No Frozen Gasses)");
        }

        [Test]
        public void TestFrozenGasGetAtmosphericPressure()
        {
            float pressure = 1f;
            AtmosphericGasSD gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0
            };
            AtmosphericGasSD frozen = new() {
                ChemicalSymbol = "FRO",
                MeltingPoint = 20
            };
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas, pressure);
            _atmosphere.Composition.Add(frozen, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetAtmosphericPressure(), 0.1, "GetAtmosphericPressure (With Frozen Gasses)");
        }

        [Test]
        public void TestNoFrozenGasGetGreenhousePressure()
        {
            float pressure = 1f;
            AtmosphericGasSD gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0,
                GreenhouseEffect = 1,
            };
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetGreenhousePressure(), 0.1, "GetGreenhousePressure (No Frozen Gasses)");
        }

        [Test]
        public void TestFrozenGasGetGreenhousePressure()
        {
            float pressure = 1f;
            AtmosphericGasSD gas1 = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0,
                GreenhouseEffect = 1
            };
            AtmosphericGasSD gas2 = new() {
                ChemicalSymbol = "FRO",
                MeltingPoint = 20,
                GreenhouseEffect = 0
            };
            AtmosphericGasSD gas3 = new() {
                ChemicalSymbol = "FG",
                MeltingPoint = 20,
                GreenhouseEffect = 1
            };
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas1, pressure);
            _atmosphere.Composition.Add(gas2, pressure);
            _atmosphere.Composition.Add(gas3, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetGreenhousePressure(), 0.1, "GetGreenhousePressure (With Frozen Gasses)");
        }

        [Test]
        public void TestNoFrozenGasGetAntiGreenhousePressure()
        {
            float pressure = 1f;
            AtmosphericGasSD gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0,
                GreenhouseEffect = -1,
            };
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetAntiGreenhousePressure(), 0.1, "GetAntiGreenhousePressure (No Frozen Gasses)");
        }

        [Test]
        public void TestFrozenGasGetAntiGreenhousePressure()
        {
            float pressure = 1f;
            AtmosphericGasSD gas1 = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0,
                GreenhouseEffect = -1
            };
            AtmosphericGasSD gas2 = new() {
                ChemicalSymbol = "G2",
                MeltingPoint = 20,
                GreenhouseEffect = -1
            };
            AtmosphericGasSD gas3 = new() {
                ChemicalSymbol = "G3",
                MeltingPoint = 20,
                GreenhouseEffect = 1
            };
            AtmosphericGasSD gas4 = new() {
                ChemicalSymbol = "G4",
                MeltingPoint = 0,
                GreenhouseEffect = 1
            };
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas1, pressure);
            _atmosphere.Composition.Add(gas2, pressure);
            _atmosphere.Composition.Add(gas3, pressure);
            _atmosphere.Composition.Add(gas4, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetAntiGreenhousePressure(), 0.1, "GetAntiGreenhousePressure (With Frozen Gasses)");
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