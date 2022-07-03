using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using System.Diagnostics;
using Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage;

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
            var gameSettings = new NewGameSettings();
            gameSettings.MaxSystems = 10;
            _game = new Game(gameSettings);
            StaticDataManager.LoadData("Pulsar4x", _game);
            _entityManager = new EntityManager(_game);
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
            var minerals = new List<MineralSD>();
            var mineralCargoTypeId = Guid.NewGuid();

            var otherJunk = new List<ICargoable>();
            var otherCargoTypeId = Guid.NewGuid();

            var theDice = new Random();

            var diceRollMinerals = theDice.Next(15, 20);
            for (var i = 0; i < diceRollMinerals; i++)
            {
                var randomMineral = new MineralSD()
                {
                    ID = Guid.NewGuid(),
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
                    ID = Guid.NewGuid(),
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
            var materials = new List<ProcessedMaterialSD>();
            var materialCargoTypeId = Guid.NewGuid();

            var otherJunk = new List<ICargoable>();
            var otherCargoTypeId = Guid.NewGuid();

            var theDice = new Random();

            var diceRollMaterials = theDice.Next(15, 20);
            for (var i = 0; i < diceRollMaterials; i++)
            {
                var randomMaterial = new ProcessedMaterialSD()
                {
                    ID = Guid.NewGuid(),
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
                    ID = Guid.NewGuid(),
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
            var materials = new List<ProcessedMaterialSD>();
            var materialCargoTypeId = Guid.NewGuid();

            var otherJunk = new List<ICargoable>();
            var otherCargoTypeId = Guid.NewGuid();

            var theDice = new Random();

            var diceRollMaterials = theDice.Next(15, 20);
            for (var i = 0; i < diceRollMaterials; i++)
            {
                var randomMaterial = new ProcessedMaterialSD()
                {
                    ID = Guid.NewGuid(),
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
                    ID = Guid.NewGuid(),
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
        public void CargoDefinitionsLibrary_When_GEttingADefinitionFromTheLibraryThatDoesnotExist_Should_ReturnNull()
        {
            var library = new CargoDefinitionsLibrary();
            Assert.IsNull(library.GetAny(Guid.NewGuid()));
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



        private ProcessedMaterialSD SetupCookieTradeGood()
        {
            var cookies = new ProcessedMaterialSD
            {
                Name = "Clicked Cookies",
                Description = "Tastes like carpal tunnel and time.",
                ID = Guid.NewGuid(),
                CargoTypeID = Guid.NewGuid(),
                MassPerUnit = 1,
                VolumePerUnit = 1//these are some really really big cookies. 
            };

            return cookies;
        }

        private ProcessedMaterialSD SetupRockTradeGood()
        {
            var rock = new ProcessedMaterialSD
            {
                Name = "Rock",
                Description = "A pile of heavy rocks. Very useful. Trust me.",
                ID = Guid.NewGuid(),
                CargoTypeID = Guid.NewGuid(),
                MassPerUnit = 10,
                VolumePerUnit = 0.0001,
            };

            return rock;
        }
    }

    public class JustSomeCargoThing : ICargoable
    {
        public Guid ID { get; set; }

        public string Name { get; set; }

        public Guid CargoTypeID { get; set; }

        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; }
        public double Density { get; set; }
    }
}
