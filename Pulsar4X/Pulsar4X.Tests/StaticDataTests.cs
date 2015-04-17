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

            StaticDataManager.ExportStaticData(nameTheme, "./CommanderNameThemeExportTest.json");

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
            min.ID = new Guid();
            min.Name = "Sorium";
            minList.Add(min);

            StaticDataManager.ExportStaticData(minList, "./MineralsExportTest.json");

        }
    }
}