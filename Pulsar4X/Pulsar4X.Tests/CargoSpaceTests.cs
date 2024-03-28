using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Modding;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Cargo Space Tests")]
    class CargoSpaceTests
    {
        private Game _game;
        private EntityManager _entityManager;

        [SetUp]
        public void Init()
        {
             var _modLoader = new ModLoader();
            var _modDataStore = new ModDataStore();

            _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);

            var _settings = new NewGameSettings() {
                MaxSystems = 10
            };

            _game  = new Game(_settings, _modDataStore);

            _entityManager = new EntityManager();
            _entityManager.Initialize(_game);
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
        }

        [Test]
        public void CargoDefinitionsLibrary_When_AskedIfSomethingIsAMineral_Should_CorrectlyRespond()
        {
            var minerals = new List<Mineral>();
            var mineralCargoTypeId = Guid.NewGuid().ToString();

            var otherJunk = new List<ICargoable>();
            var otherCargoTypeId = Guid.NewGuid().ToString();;

            var theDice = new Random();

            var diceRollMinerals = theDice.Next(15, 20);
            for (var i = 0; i < diceRollMinerals; i++)
            {
                var randomMineral = new Mineral()
                {
                    UniqueID = Guid.NewGuid().ToString(),
                    CargoTypeID = mineralCargoTypeId,
                    MassPerUnit = 1000,
                    Name = "RandomMineral_" + i.ToString(),
                    Description = "A random mineral."
                };
                minerals.Add(randomMineral);
            }

            var diceRoll = theDice.Next(15, 20);
            for (var i = 0; i < diceRoll; i++)
            {
                var randomCargoThing = new JustSomeCargoThing()
                {
                    UniqueID = Guid.NewGuid().ToString(),
                    CargoTypeID = otherCargoTypeId,
                    MassPerUnit = 1000,
                    Name = "AThing_" + i.ToString()
                };
                otherJunk.Add(randomCargoThing);
            }

            var library = new CargoDefinitionsLibrary();
            library.LoadMineralDefinitions(minerals);
            library.LoadOtherDefinitions(otherJunk);

            for (var i = 0; i < diceRollMinerals; i++)
            {
                Assert.IsTrue(library.IsMineral(minerals[i].ID));
            }
            for (var i = 0; i < diceRoll; i++)
            {
                Assert.IsFalse(library.IsMineral(otherJunk[i].ID));
            }
        }

        [Test]
        public void CargoDefinitionsLibrary_When_AskedIfSomethingIsAMaterial_Should_CorrectlyRespond()
        {
            var materials = new List<ProcessedMaterial>();
            var materialCargoTypeId = Guid.NewGuid().ToString();

            var otherJunk = new List<ICargoable>();
            var otherCargoTypeId = Guid.NewGuid().ToString();

            var theDice = new Random();

            var diceRollMaterials = theDice.Next(15, 20);
            for (var i = 0; i < diceRollMaterials; i++)
            {
                var randomMaterial = new ProcessedMaterial()
                {
                    UniqueID = Guid.NewGuid().ToString(),
                    CargoTypeID = materialCargoTypeId,
                    MassPerUnit = 1000,
                    Name = "RandomMaterial_" + i.ToString(),
                    Description = "A random material."
                };
                materials.Add(randomMaterial);
            }

            var diceRoll = theDice.Next(15, 20);
            for (var i = 0; i < diceRoll; i++)
            {
                var randomCargoThing = new JustSomeCargoThing()
                {
                    UniqueID = Guid.NewGuid().ToString(),
                    CargoTypeID = otherCargoTypeId,
                    MassPerUnit = 1000,
                    Name = "AThing_" + i.ToString()
                };
                otherJunk.Add(randomCargoThing);
            }

            var library = new CargoDefinitionsLibrary();
            library.LoadMaterialsDefinitions(materials);
            library.LoadOtherDefinitions(otherJunk);

            for (var i = 0; i < diceRollMaterials; i++)
            {
                Assert.IsTrue(library.IsMaterial(materials[i].ID));
            }
            for (var i = 0; i < diceRoll; i++)
            {
                Assert.IsFalse(library.IsMaterial(otherJunk[i].ID));
            }
        }

        [Test]
        public void CargoDefinitionsLibrary_When_AskedIfSomethingIsOtherCargo_Should_CorrectlyRespond()
        {
            var materials = new List<ProcessedMaterial>();
            var materialCargoTypeId = Guid.NewGuid().ToString();

            var otherJunk = new List<ICargoable>();
            var otherCargoTypeId = Guid.NewGuid().ToString();

            var theDice = new Random();

            var diceRollMaterials = theDice.Next(15, 20);
            for (var i = 0; i < diceRollMaterials; i++)
            {
                var randomMaterial = new ProcessedMaterial()
                {
                    UniqueID = Guid.NewGuid().ToString(),
                    CargoTypeID = materialCargoTypeId,
                    MassPerUnit = 1000,
                    Name = "RandomMaterial_" + i.ToString(),
                    Description = "A random material."
                };
                materials.Add(randomMaterial);
            }

            var diceRoll = theDice.Next(15, 20);
            for (var i = 0; i < diceRoll; i++)
            {
                var randomCargoThing = new JustSomeCargoThing()
                {
                    UniqueID = Guid.NewGuid().ToString(),
                    CargoTypeID = otherCargoTypeId,
                    MassPerUnit = 1000,
                    Name = "AThing_" + i.ToString()
                };
                otherJunk.Add(randomCargoThing);
            }

            var library = new CargoDefinitionsLibrary();
            library.LoadMaterialsDefinitions(materials);
            library.LoadOtherDefinitions(otherJunk);

            for (var i = 0; i < diceRollMaterials; i++)
            {
                Assert.IsFalse(library.IsOther(materials[i].ID));
            }
            for (var i = 0; i < diceRoll; i++)
            {
                Assert.IsTrue(library.IsOther(otherJunk[i].ID));
            }
        }

        [Test]
        public void CargoDefinitionsLibrary_When_GettingADefinitionFromTheLibraryThatDoesnotExist_Should_ReturnNull()
        {
            var library = new CargoDefinitionsLibrary();
            Assert.IsNull(library.GetAny(Guid.NewGuid().ToString()));
        }

        [Test]
        public void VolumeStorage_BasicChecks()
        {
            var cookies = SetupCookieTradeGood();

            var cookiePile = new VolumeStorageDB();
            cookiePile.TypeStores.Add(cookies.CargoTypeID, new TypeStore(100));
            var added = cookiePile.AddCargoByUnit(cookies, 99);


            var storedCookies = cookiePile.GetUnitsStored(cookies);
            var storedCookieMass = cookiePile.GetMassStored(cookies);
            var storedCookieVolume = cookiePile.GetVolumeStored(cookies);


            Assert.AreEqual( 99, added);
            Assert.AreEqual( 99, storedCookies);
            Assert.AreEqual(99, storedCookieMass);
            Assert.AreEqual(99, storedCookieVolume);

            var addMore = cookiePile.AddCargoByUnit(cookies, 100);
            var storedCookies2 = cookiePile.GetUnitsStored(cookies);
            var storedCookieMass2 = cookiePile.GetMassStored(cookies);
            var storedCookieVolume2 = cookiePile.GetVolumeStored(cookies);
            Assert.AreEqual(1, addMore);
            Assert.AreEqual( 100, storedCookies2);
            Assert.AreEqual(100, storedCookieMass2);
            Assert.AreEqual(100, storedCookieVolume2);

        }



        private ProcessedMaterial SetupCookieTradeGood()
        {
            var cookies = new ProcessedMaterial
            {
                Name = "Clicked Cookies",
                Description = "Tastes like carpal tunnel and time.",
                UniqueID = Guid.NewGuid().ToString(),
                CargoTypeID = Guid.NewGuid().ToString(),
                MassPerUnit = 1,
                VolumePerUnit = 1//these are some really really big cookies.
            };

            return cookies;
        }

        private ProcessedMaterial SetupRockTradeGood()
        {
            var rock = new ProcessedMaterial
            {
                Name = "Rock",
                Description = "A pile of heavy rocks. Very useful. Trust me.",
                UniqueID = Guid.NewGuid().ToString(),
                CargoTypeID = Guid.NewGuid().ToString(),
                MassPerUnit = 10,
                VolumePerUnit = 0.0001,
            };

            return rock;
        }
    }

    public class JustSomeCargoThing : ICargoable
    {
        public int ID { get; set; }
        public string UniqueID { get; set; }

        public string Name { get; set; }

        public string CargoTypeID { get; set; }

        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; }
        public double Density { get; set; }
    }
}
