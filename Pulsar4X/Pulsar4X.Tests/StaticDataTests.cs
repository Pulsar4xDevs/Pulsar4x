using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;


namespace Pulsar4X.Tests
{
    [TestFixture, Description("Tests the static data import/export/manager/store")]
    public class StaticDataTests
    {
        [Test]
        public void TestExport()
        {
            WeightedList<AtmosphericGasSD> atmoGases = new WeightedList<AtmosphericGasSD>();
            AtmosphericGasSD gas = new AtmosphericGasSD();
            gas.BoilingPoint = 100;
            gas.MeltingPoint = 0;
            gas.ChemicalSymbol = "H20";
            gas.Name = "Water";
            gas.IsToxic = false;

            atmoGases.Add(1.0, gas);

            gas.BoilingPoint = 100;
            gas.MeltingPoint = 0;
            gas.ChemicalSymbol = "H2O";
            gas.Name = "Water Second take";
            gas.IsToxic = false;

            atmoGases.Add(1.0, gas);

            StaticDataManager.ExportStaticData(atmoGases, "./AtmoGasesExportTest.json");

            List<CommanderNameThemeSD> nameThemes = new List<CommanderNameThemeSD>();
            CommanderNameThemeSD nameTheme = new CommanderNameThemeSD();
            nameTheme.NameList = new List<CommanderNameSD>();
            nameTheme.ThemeName = "The Creators";

            CommanderNameSD name = new CommanderNameSD();
            name.First = "Greg";
            name.Last = "Nott";
            name.IsFemale = false;

            nameTheme.NameList.Add(name);

            name.First = "Rod";
            name.Last = "Serling";
            name.IsFemale = false;

            nameTheme.NameList.Add(name);

            nameThemes.Add(nameTheme);

            StaticDataManager.ExportStaticData(nameThemes, "./CommanderNameThemeExportTest.json");

            StaticDataManager.ExportStaticData(VersionInfo.PulsarVersionInfo, "./VersionInfoExportTest.vinfo");

            List<MineralSD> minList = new List<MineralSD>();
            MineralSD min = new MineralSD();
            min.Abundance = new JDictionary<BodyType, double>();
            min.Abundance.Add(BodyType.Asteroid, 0.01);
            min.Abundance.Add(BodyType.Comet, 0.05);
            min.Abundance.Add(BodyType.DwarfPlanet, 0.075);
            min.Abundance.Add(BodyType.GasDwarf, 0.1);
            min.Abundance.Add(BodyType.GasGiant, 1.0);
            min.Abundance.Add(BodyType.IceGiant, 0.5);
            min.Abundance.Add(BodyType.Moon, 0.5);
            min.Abundance.Add(BodyType.Terrestrial, 1.0);
            min.ID = Guid.NewGuid();
            min.Name = "Sorium";
            min.Description = "des";
            minList.Add(min);

            StaticDataManager.ExportStaticData(minList, "./MineralsExportTest.json");

            JDictionary<Guid, TechSD> techs = new JDictionary<Guid, TechSD>();
            TechSD tech1 = new TechSD();
            tech1.Name = "Trans-Newtonian Technology";
            tech1.Requirements = new List<Guid>();
            tech1.Description = "Unlocks almost all other technology.";
            tech1.Cost = 1000;
            tech1.Category = ResearchCategories.ConstructionProduction;
            tech1.ID = Guid.NewGuid();

            TechSD tech2 = new TechSD();
            tech2.Name = "Construction Rate";
            tech2.Requirements = new List<Guid>();
            tech2.Requirements.Add(tech1.ID);
            tech2.Description = "Boosts Construction Rate by 12 BP";
            tech2.Cost = 3000;
            tech2.Category = ResearchCategories.ConstructionProduction;
            tech2.ID = Guid.NewGuid();

            techs.Add(tech1.ID, tech1);
            techs.Add(tech2.ID, tech2);

            StaticDataManager.ExportStaticData(techs, "./TechnologyDataExportTest.json");

            List<InstallationSD> installations = new List<InstallationSD>();
            InstallationSD install = new InstallationSD();
            install.Name = "Mine";
            install.Description = "Employs population to mine transnewtonian resources.";
            install.PopulationRequired = 1;
            install.CargoSize = 1;
            install.BaseAbilityAmounts = new JDictionary<AbilityType, int>();
            install.BaseAbilityAmounts.Add(AbilityType.Mine, 1);
            install.TechRequirements = new List<Guid>();
            install.TechRequirements.Add(tech1.ID); //use trans-newtonian techology you just added to the tech list
            install.ResourceCosts = new JDictionary<Guid, int>();
            install.ResourceCosts.Add(min.ID,60); //use Sorium that you just added to the mineral list
            install.WealthCost = 120;
            install.BuildPoints = 120;

            installations.Add(install);

            StaticDataManager.ExportStaticData(installations, "./InstallationExportTest.json");
        }

        [Test]
        public void TestLoadDefaultData()
        {
            StaticDataManager.LoadFromDefaultDataDirectory();

            // store counts for later:
            int mineralsNum = StaticDataManager.StaticDataStore.Minerals.Count;  
            int techNum = StaticDataManager.StaticDataStore.Techs.Count;
            int installationsNum = StaticDataManager.StaticDataStore.Installations.Count;
            int constructableObjectsNum = StaticDataManager.StaticDataStore.ConstructableObjects.Count;

            // check that data was loaded:
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.Minerals);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.AtmosphericGases);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.CommanderNameThemes);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.Minerals);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.Techs);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.Installations);

            // now lets re-load the same data, to test that duplicates don't occure as required:
            StaticDataManager.LoadFromDefaultDataDirectory();

            // now check that overwriting occured and that there were no duplicates:
            Assert.AreEqual(mineralsNum, StaticDataManager.StaticDataStore.Minerals.Count);
            Assert.AreEqual(techNum, StaticDataManager.StaticDataStore.Techs.Count);
            Assert.AreEqual(installationsNum, StaticDataManager.StaticDataStore.Installations.Count);
            Assert.AreEqual(constructableObjectsNum, StaticDataManager.StaticDataStore.ConstructableObjects.Count);

            // now lets test some malformed data folders.
            Assert.Catch(typeof(StaticDataLoadException), () =>
            {
                StaticDataManager.LoadFromDirectory("./TestData/MalformedData");
            });

            // now ,lets try for a directory that does not exist.
            Assert.Catch(typeof(StaticDataLoadException), () =>
            {
                StaticDataManager.LoadFromDirectory("./TestData/DoesNotExist");
            });
        }

        [Test]
        public void TestIDLookup()
        {
            // make sure the store is clear:
            StaticDataManager.ClearAllData();

            // test when the store is empty:
            object testNullObj = StaticDataManager.StaticDataStore.FindDataObjectUsingID(Guid.NewGuid());
            Assert.IsNull(testNullObj);

            // Load the default static data to test against:
            StaticDataManager.LoadFromDefaultDataDirectory();

            // test with a guid that is not in the store:
            object testObj = StaticDataManager.StaticDataStore.FindDataObjectUsingID(Guid.Empty);  // empty guid should never be in the store.
            Assert.IsNull(testObj);

            // noew lets test for values that are in the store:
            Guid testID = StaticDataManager.StaticDataStore.Minerals[0].ID;
            testObj = StaticDataManager.StaticDataStore.FindDataObjectUsingID(testID);
            Assert.IsNotNull(testObj);
            Assert.AreEqual(testID, ((MineralSD)testObj).ID);

            testID = StaticDataManager.StaticDataStore.Installations.First().Key;
            testObj = StaticDataManager.StaticDataStore.FindDataObjectUsingID(testID);
            Assert.IsNotNull(testObj);
            Assert.AreEqual(testID, ((InstallationSD)testObj).ID);

            testID = StaticDataManager.StaticDataStore.Techs.First().Key;
            testObj = StaticDataManager.StaticDataStore.FindDataObjectUsingID(testID);
            Assert.IsNotNull(testObj);
            Assert.AreEqual(testID, ((TechSD)testObj).ID);
        }

        //for want of a better place to put it.
        [Test]
        public void TestJdicExtension() 
        {
            JDictionary<int, int> dict = new JDictionary<int, int>();
            dict.Add(1,1);
            dict.SafeValueAdd<int, int>(1, 2); //add to exsisting
            dict.SafeValueAdd<int, int>(2, 5); //add to non exsisting.

            Assert.AreEqual(dict[1], 3);
            Assert.AreEqual(dict[2], 5);
        }
    }
}