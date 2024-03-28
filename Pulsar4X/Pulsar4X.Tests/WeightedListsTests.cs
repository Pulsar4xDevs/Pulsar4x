using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Modding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Tests
{
    public class WeightedListTests
    {
        private Random R = new Random(22367);

        /// <summary>
        /// TODO: Isolate this method. Any changes to the default data will break these tests.
        /// </summary>
        private SystemGenSettingsBlueprint GetSystemGenSettingsSD()
        {
            R = new Random(22367);
            var startDate = new DateTime(2050, 1, 1);

            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();

            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);

            var settings = new NewGameSettings { GameName = "Unit Test Game", StartDateTime = startDate, MaxSystems = 0, CreatePlayerFaction = false };
            var game  = new Game(settings, modDataStore);
            return game.GalaxyGen.Settings;
        }

        [Test]
        public void TestInnerBandTypeWeights()
        {
            var underTest = GetSystemGenSettingsSD().InnerBandTypeWeights;

            List<BodyType> results = new List<BodyType>();
            for (int i = 0; i < 1000; i++)
            {
                results.Add(underTest.Select(R.NextDouble()));
            }

            Assert.AreEqual(358, results.Count(x => x == BodyType.Asteroid), "Asteroid");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Comet), "Comet");
            Assert.AreEqual(0, results.Count(x => x == BodyType.DwarfPlanet), "DwarfPlanet");
            Assert.AreEqual(110, results.Count(x => x == BodyType.GasDwarf), "GasDwarf");
            Assert.AreEqual(51, results.Count(x => x == BodyType.GasGiant), "GasGiant");
            Assert.AreEqual(0, results.Count(x => x == BodyType.IceGiant), "IceGiant");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Moon), "Moon");
            Assert.AreEqual(481, results.Count(x => x == BodyType.Terrestrial), "Terrestrial");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Unknown), "Unknown");
        }

        [Test]
        public void TestHabitableBandTypeWeights()
        {
            var underTest = GetSystemGenSettingsSD().HabitableBandTypeWeights;

            List<BodyType> results = new List<BodyType>();
            for (int i = 0; i < 1000; i++)
            {
                results.Add(underTest.Select(R.NextDouble()));
            }

            Assert.AreEqual(257, results.Count(x => x == BodyType.Asteroid), "Asteroid");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Comet), "Comet");
            Assert.AreEqual(0, results.Count(x => x == BodyType.DwarfPlanet), "DwarfPlanet");
            Assert.AreEqual(101, results.Count(x => x == BodyType.GasDwarf), "GasDwarf");
            Assert.AreEqual(57, results.Count(x => x == BodyType.GasGiant), "GasGiant");
            Assert.AreEqual(0, results.Count(x => x == BodyType.IceGiant), "IceGiant");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Moon), "Moon");
            Assert.AreEqual(585, results.Count(x => x == BodyType.Terrestrial), "Terrestrial");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Unknown), "Unknown");
        }

        [Test]
        public void TestOuterBandTypeWeights()
        {
            var underTest = GetSystemGenSettingsSD().OuterBandTypeWeights;

            List<BodyType> results = new List<BodyType>();
            for (int i = 0; i < 1000; i++)
            {
                results.Add(underTest.Select(R.NextDouble()));
            }

            Assert.AreEqual(180, results.Count(x => x == BodyType.Asteroid), "Asteroid");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Comet), "Comet");
            Assert.AreEqual(0, results.Count(x => x == BodyType.DwarfPlanet), "DwarfPlanet");
            Assert.AreEqual(223, results.Count(x => x == BodyType.GasDwarf), "GasDwarf");
            Assert.AreEqual(285, results.Count(x => x == BodyType.GasGiant), "GasGiant");
            Assert.AreEqual(211, results.Count(x => x == BodyType.IceGiant), "IceGiant");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Moon), "Moon");
            Assert.AreEqual(101, results.Count(x => x == BodyType.Terrestrial), "Terrestrial");
            Assert.AreEqual(0, results.Count(x => x == BodyType.Unknown), "Unknown");
        }
    }
}