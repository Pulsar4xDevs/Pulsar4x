using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Test the AtmosphereDBExtension methods")]
    class AtmosphereDBExtensionsTests : TestHelper
    {
        [SetUp]
        public void Init()
        {
            var _modLoader = new ModLoader();
            var _modDataStore = new ModDataStore();

            _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);

            var _settings = new NewGameSettings() {
                MaxSystems = 1
            };

            _game  = new Game(_settings, _modDataStore);

            _entityManager = new EntityManager();
            _entityManager.Initialize(_game);

            _humans = new SpeciesDB(
                baseGravity: 1, minGravity: 0.1, maxGravity: 1.9,
                basePressure: 1, minPressure: 0, maxPressure: 4,
                baseTemp: 14, minTemp: -10, maxTemp: 38);
            _humans.BreathableGasSymbol = "O2";

            _gasDictionary = new Dictionary<string, GasBlueprint>();
            foreach (var (id, gas) in _game.AtmosphericGases)
            {
                _gasDictionary.Add(gas.ChemicalSymbol, gas);
            }

            _atmosphere = new(
                pressure: 0f,
                hydrosphere: false,
                hydroExtent: 0m,
                greenhouseFactor: 1.0f,
                greenhousePressue: 0.0f,
                surfaceTemp: 100f,
                new Dictionary<string, float>()
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
            GasBlueprint gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 10
            };
            Assert.AreEqual(true, gas.WouldBeFrozenAtGivenTemperature(5f), "WouldBeFrozenAtGivenTemperature (Frozen)");
        }

        [Test]
        public void TestNotFrozenWouldBeFrozenAtGiveTemperature()
        {
            GasBlueprint gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0
            };
            Assert.AreEqual(false, gas.WouldBeFrozenAtGivenTemperature(5f), "WouldBeFrozenAtGivenTemperature (Not Frozen)");
        }

        [Test]
        public void TestNoFrozenGasGetAtmosphericPressure()
        {
            float pressure = 1f;
            GasBlueprint gas = new() {
                UniqueID = "NOT",
                ChemicalSymbol = "NOT",
                MeltingPoint = 0
            };
            _game.AtmosphericGases.Add(gas.UniqueID, gas);
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas.UniqueID, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetAtmosphericPressure(), 0.1, "GetAtmosphericPressure (No Frozen Gasses)");
            _game.AtmosphericGases.Remove(gas.UniqueID);
        }

        [Test]
        public void TestFrozenGasGetAtmosphericPressure()
        {
            float pressure = 1f;
            GasBlueprint gas = new() {
                UniqueID = "NOT",
                ChemicalSymbol = "NOT",
                MeltingPoint = 0
            };
            GasBlueprint frozen = new() {
                UniqueID = "FRO",
                ChemicalSymbol = "FRO",
                MeltingPoint = 20
            };
            _game.AtmosphericGases.Add(gas.UniqueID, gas);
            _game.AtmosphericGases.Add(frozen.UniqueID, frozen);
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas.UniqueID, pressure);
            _atmosphere.Composition.Add(frozen.UniqueID, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetAtmosphericPressure(), 0.1, "GetAtmosphericPressure (With Frozen Gasses)");
            _game.AtmosphericGases.Remove(gas.UniqueID);
            _game.AtmosphericGases.Remove(frozen.UniqueID);
        }

        [Test]
        public void TestNoFrozenGasGetGreenhousePressure()
        {
            float pressure = 1f;
            GasBlueprint gas = new() {
                UniqueID = "NOT",
                ChemicalSymbol = "NOT",
                MeltingPoint = 0,
                GreenhouseEffect = 1,
            };
            _game.AtmosphericGases.Add(gas.UniqueID, gas);
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas.UniqueID, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetGreenhousePressure(), 0.1, "GetGreenhousePressure (No Frozen Gasses)");
            _game.AtmosphericGases.Remove(gas.UniqueID);
        }

        [Test]
        public void TestFrozenGasGetGreenhousePressure()
        {
            float pressure = 1f;
            GasBlueprint gas1 = new() {
                UniqueID = "NOT",
                ChemicalSymbol = "NOT",
                MeltingPoint = 0,
                GreenhouseEffect = 1
            };
            GasBlueprint gas2 = new() {
                UniqueID = "FRO",
                ChemicalSymbol = "FRO",
                MeltingPoint = 20,
                GreenhouseEffect = 0
            };
            GasBlueprint gas3 = new() {
                UniqueID = "FG",
                ChemicalSymbol = "FG",
                MeltingPoint = 20,
                GreenhouseEffect = 1
            };
            _game.AtmosphericGases.Add(gas1.UniqueID, gas1);
            _game.AtmosphericGases.Add(gas2.UniqueID, gas2);
            _game.AtmosphericGases.Add(gas3.UniqueID, gas3);

            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas1.UniqueID, pressure);
            _atmosphere.Composition.Add(gas2.UniqueID, pressure);
            _atmosphere.Composition.Add(gas3.UniqueID, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetGreenhousePressure(), 0.1, "GetGreenhousePressure (With Frozen Gasses)");

            _game.AtmosphericGases.Remove(gas1.UniqueID);
            _game.AtmosphericGases.Remove(gas2.UniqueID);
            _game.AtmosphericGases.Remove(gas3.UniqueID);
        }

        [Test]
        public void TestNoFrozenGasGetAntiGreenhousePressure()
        {
            float pressure = 1f;
            GasBlueprint gas = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0,
                GreenhouseEffect = -1,
            };
            _game.AtmosphericGases.Add(gas.UniqueID, gas);
            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas.UniqueID, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetAntiGreenhousePressure(), 0.1, "GetAntiGreenhousePressure (No Frozen Gasses)");
            _game.AtmosphericGases.Remove(gas.UniqueID);
        }

        [Test]
        public void TestFrozenGasGetAntiGreenhousePressure()
        {
            float pressure = 1f;
            GasBlueprint gas1 = new() {
                ChemicalSymbol = "NOT",
                MeltingPoint = 0,
                GreenhouseEffect = -1
            };
            GasBlueprint gas2 = new() {
                ChemicalSymbol = "G2",
                MeltingPoint = 20,
                GreenhouseEffect = -1
            };
            GasBlueprint gas3 = new() {
                ChemicalSymbol = "G3",
                MeltingPoint = 20,
                GreenhouseEffect = 1
            };
            GasBlueprint gas4 = new() {
                ChemicalSymbol = "G4",
                MeltingPoint = 0,
                GreenhouseEffect = 1
            };
            _game.AtmosphericGases.Add(gas1.UniqueID, gas1);
            _game.AtmosphericGases.Add(gas2.UniqueID, gas2);
            _game.AtmosphericGases.Add(gas3.UniqueID, gas3);
            _game.AtmosphericGases.Add(gas4.UniqueID, gas4);

            _atmosphere.Composition.Clear();
            _atmosphere.Composition.Add(gas1.UniqueID, pressure);
            _atmosphere.Composition.Add(gas2.UniqueID, pressure);
            _atmosphere.Composition.Add(gas3.UniqueID, pressure);
            _atmosphere.Composition.Add(gas4.UniqueID, pressure);
            Entity planet = GetPlanet(10, 1f, 1, _atmosphere);
            Assert.AreEqual(pressure, _atmosphere.GetAntiGreenhousePressure(), 0.1, "GetAntiGreenhousePressure (With Frozen Gasses)");

            _game.AtmosphericGases.Remove(gas1.UniqueID);
            _game.AtmosphericGases.Remove(gas2.UniqueID);
            _game.AtmosphericGases.Remove(gas3.UniqueID);
            _game.AtmosphericGases.Remove(gas4.UniqueID);
        }
    }
}