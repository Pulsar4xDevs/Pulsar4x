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
        public void StorageSpaceProcessor_When_AskedToCheckIfItHasACargoTypeThatItDoesnotHave_Should_ReturnFalse()
        {
            var cookies = SetupCookieTradeGood();

            var cookiePile = new CargoStorageDB();

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1 }
            };

            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile, cookieCheck);

            Assert.IsFalse(hasCookies);
        }

        [Test]
        public void StorageSpaceProcessor_When_AskedToCheckIfItHasASpecificItemThatItDoesnotHave_Should_ReturnFalse()
        {
            var cookies = SetupCookieTradeGood();
            var biscuits = SetupCookieTradeGood();
            biscuits.CargoTypeID = cookies.CargoTypeID;

            var cookiePile = new CargoStorageDB();
            cookiePile.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            StorageSpaceProcessor.AddCargo(cookiePile, biscuits, 1);

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1 }
            };

            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile, cookieCheck);

            Assert.IsFalse(hasCookies);
        }

        [Test]
        public void StorageSpaceProcessor_When_AskedToCheckIfItHasCargoThatItDoesnotHaveEnoughOf_Should_ReturnFalse()
        {
            var cookies = SetupCookieTradeGood();

            var cookiePile = new CargoStorageDB();
            cookiePile.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            StorageSpaceProcessor.AddCargo(cookiePile, cookies, 6);

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 7 }
            };
            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile, cookieCheck);
            Assert.IsFalse(hasCookies);
        }

        [Test]
        public void StorageSpaceProcessor_When_AskedToCheckIfItHasCargoThatItHasExactlyEnoughOf_Should_ReturnTrue()
        {
            var cookies = SetupCookieTradeGood();

            var cookiePile = new CargoStorageDB();
            cookiePile.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            StorageSpaceProcessor.AddCargo(cookiePile, cookies, 7);

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 7 }
            };
            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile, cookieCheck);
            Assert.IsTrue(hasCookies);
        }

        [Test]
        public void StorageSpaceProcessor_When_AskedToCheckIfItHasCargoThatItHasMoreTHanEnoughOf_Should_ReturnTrue()
        {
            var cookies = SetupCookieTradeGood();

            var cookiePile = new CargoStorageDB();
            cookiePile.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            StorageSpaceProcessor.AddCargo(cookiePile, cookies, 99);

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 7 }
            };
            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile, cookieCheck);
            Assert.IsTrue(hasCookies);
        }

        [Test]
        public void StorageSpaceProcessor_When_AskedToCheckAvailableStorageSpace_Should_ReturnCorrectAnswerForTheRequestedCargoItem()
        {
            var cookies = SetupCookieTradeGood();
            var library = new CargoDefinitionsLibrary();
            library.LoadOtherDefinitions(new List<ICargoable>() { cookies });

            var cookiePile = new CargoStorageDB();
            cookiePile.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 35007, FreeCapacityKg = 32154 });
            cookiePile.StoredCargoTypes.Add(Guid.NewGuid(), new CargoTypeStore() { MaxCapacityKg = 99998, FreeCapacityKg = 99997 });
            cookiePile.StoredCargoTypes.Add(Guid.NewGuid(), new CargoTypeStore() { MaxCapacityKg = 99996, FreeCapacityKg = 99995 });

            var canStoreThisManyItems = StorageSpaceProcessor.GetAvailableSpaceInItemCount(cookiePile, cookies.ID, library);
            Assert.AreEqual(32154, canStoreThisManyItems);

            StorageSpaceProcessor.AddCargo(cookiePile, cookies, 2154);

            canStoreThisManyItems = StorageSpaceProcessor.GetAvailableSpaceInItemCount(cookiePile, cookies.ID, library);
            Assert.AreEqual(30000, canStoreThisManyItems);

        }

        private ProcessedMaterialSD SetupCookieTradeGood()
        {
            var cookies = new ProcessedMaterialSD
            {
                Name = "Clicked Cookies",
                Description = "Tastes like carpal tunnel and time.",
                ID = Guid.NewGuid(),
                CargoTypeID = Guid.NewGuid(),
                Mass = 1
            };

            return cookies;
        }
    }
}
