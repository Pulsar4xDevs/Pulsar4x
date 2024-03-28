using Pulsar4X.Modding;
using NUnit.Framework;
using System.Data;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Tests the ModLoader class")]
    internal class ModLoaderTests
    {
        ModLoader _modLoader;
        ModDataStore _modDataStore;

        [SetUp]
        public void Setup()
        {
            _modLoader = new ModLoader();
            _modDataStore = new ModDataStore();
        }

        [Test]
        [Description("Tests loading a mod")]
        public void TestModLoader()
        {
            _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);

            Assert.AreEqual(1, _modLoader.LoadedMods.Count);
        }

        [Test]
        public void TestDuplicateMods()
        {
            _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);

            var ex = Assert.Throws<DuplicateNameException>(() => {
                // Load the same mod again
                _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);
            });
        }
    }
}