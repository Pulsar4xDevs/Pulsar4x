using System;
using System.Collections.Generic;
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
            tech1.Id = Guid.NewGuid();

            TechSD tech2 = new TechSD();
            tech2.Name = "Construction Rate";
            tech2.Requirements = new List<Guid>();
            tech2.Requirements.Add(tech1.Id);
            tech2.Description = "Boosts Construction Rate by 12 BP";
            tech2.Cost = 3000;
            tech2.Category = ResearchCategories.ConstructionProduction;
            tech2.Id = Guid.NewGuid();

            techs.Add(tech1.Id, tech1);
            techs.Add(tech2.Id, tech2);

            StaticDataManager.ExportStaticData(techs, "./TechnologyDataExportTest.json");

            List<InstallationSD> installations = new List<InstallationSD>();
            InstallationSD install = new InstallationSD();
            install.Name = "Mine";
            install.Description = "Employs population to mine transnewtonian resources.";
            install.PopulationRequired = 1;
            install.CargoSize = 1;
            install.BaseAbilityAmounts = new JDictionary<InstallationAbilityType, int>();
            install.BaseAbilityAmounts.Add(InstallationAbilityType.Mine, 1);
            install.Requirements = new List<Guid>();
            install.Requirements.Add(tech1.Id); //use trans-newtonian techology you just added to the tech list
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
            int mineralsNum;
            StaticDataManager.LoadFromDefaultDataDirectory();
            mineralsNum = StaticDataManager.StaticDataStore.Minerals.Count;
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.AtmosphericGases);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.CommanderNameThemes);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.Minerals);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.Techs);
            Assert.IsNotEmpty(StaticDataManager.StaticDataStore.Installations);




            Assert.AreEqual(mineralsNum, StaticDataManager.StaticDataStore.Minerals.Count);
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