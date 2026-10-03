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

            // Cargo mass is kg and volume is m³. Density outside this band is a unit slip
            // (g/cm³ written as m³/kg, or the reverse).
            foreach (var mineral in _modDataStore.Minerals.Values)
            {
                Assert.AreEqual(1L, mineral.MassPerUnit, mineral.UniqueID);
                double density = mineral.MassPerUnit / mineral.VolumePerUnit;
                Assert.GreaterOrEqual(density, 100d, mineral.UniqueID);
                Assert.LessOrEqual(density, 25000d, mineral.UniqueID);
            }

            var electricity = _modDataStore.ProcessedMaterials["electricity"];
            Assert.AreEqual(0L, electricity.MassPerUnit);
            Assert.AreEqual(0d, electricity.VolumePerUnit);

            foreach (var material in _modDataStore.ProcessedMaterials.Values)
            {
                if (material.UniqueID == "electricity")
                    continue;

                Assert.AreEqual(1L, material.MassPerUnit, material.UniqueID);
                double density = material.MassPerUnit / material.VolumePerUnit;
                Assert.GreaterOrEqual(density, 100d, material.UniqueID);
                Assert.LessOrEqual(density, 25000d, material.UniqueID);
            }
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