using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Component Tests")]
    internal class ComponentTests
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _colonyEntity;
        private MineralSD _duraniumSD;
        private MineralSD _corundiumSD;
        private StarSystem _starSystem;
        private Entity _shipClass;
        private Entity _ship;
        private Entity _engineComponent;
        [SetUp]
        public void Init()
        {
            _game = Game.NewGame("Test Game", DateTime.Now, 1);
            //Tech();
            _faction = FactionFactory.CreateFaction(_game, "Terran");
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e"),3);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de"), 3);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc"), 5);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"), 1);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"), 1);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"), 1);
            _starSystem = new StarSystem(_game, "Sol", -1);
            /////Ship Class/////
            _shipClass = ShipFactory.CreateNewShipClass(_game, _faction, "TestClass");
        }

        [Test]
        public void ExportComponents()
        {
            ComponentSD engine = EngineComponentSD();
            ComponentSD mine = MineInstallation();
            ComponentSD lab = ResearchLab();
            ComponentSD refinary = Refinary();
            ComponentSD factory = Factory();
            JDictionary<Guid, ComponentSD> componentsDict = new JDictionary<Guid, ComponentSD>();
            componentsDict.Add(engine.ID, engine);
            componentsDict.Add(mine.ID, mine);
            componentsDict.Add(lab.ID,lab);
            componentsDict.Add(refinary.ID, refinary);
            componentsDict.Add(factory.ID, factory);
            StaticDataManager.ExportStaticData(componentsDict, "./ComponentData.json");
        }

        [Test]
        public void TestEngineComponentFactory()
        {
            ComponentSD engine = EngineComponentSD();

            ComponentDesignDB design = GenericComponentFactory.StaticToDesign(engine, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);

            foreach (var ability in design.ComponentDesignAbilities)
            {
                if (ability.GuiHint == GuiHint.GuiTechSelectionList)
                {
                    List<Guid> selectionlist = ability.GuidDictionary.Keys.ToList();
                    ability.SetValueFromGuidList(selectionlist[selectionlist.Count - 1]);
                }
                else if (ability.GuiHint == GuiHint.GuiSelectionMaxMin)
                {
                    ability.SetMax();
                    ability.SetValueFromInput(ability.MaxValue);
                }
                else
                    ability.SetValue();
            }

            design.ComponentDesignAbilities[0].SetValueFromInput(250);

            Entity engineEntity = GenericComponentFactory.DesignToEntity(_game, _faction, design);

            Assert.AreEqual(250, engineEntity.GetDataBlob<ComponentInfoDB>().SizeInTons);

            JDictionary<Guid, ComponentSD> componentsDict = new JDictionary<Guid, ComponentSD>();
            componentsDict.Add(engine.ID, engine);
            StaticDataManager.ExportStaticData(componentsDict, "./EngineComponentTest.json");

        }

        [Test]
        public void TestMineInstalationFactory()
        {
            ComponentSD mine = MineInstallation();

            ComponentDesignDB mineDesign = GenericComponentFactory.StaticToDesign(mine, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);
            mineDesign.ComponentDesignAbilities[0].SetValue();
            Entity mineEntity = GenericComponentFactory.DesignToEntity(_game, _faction, mineDesign);

            Assert.AreEqual(10, mineEntity.GetDataBlob<MineResourcesDB>().ResourcesPerEconTick.Values.ElementAt(0));

            JDictionary<Guid, ComponentSD> componentsDict = new JDictionary<Guid, ComponentSD>();
            componentsDict.Add(mine.ID, mine);
            StaticDataManager.ExportStaticData(componentsDict, "./MineComponentTest.json");

        }


        public static ComponentSD EngineComponentSD()
        {
            ComponentSD component = new ComponentSD();
            component.Name = "Engine";
            component.Description = "Moves a ship";
            component.ID = new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6");

            component.SizeFormula = "Ability(0)";

            component.HTKFormula = "Max(1, [Size] / 100)";

            component.CrewReqFormula = "[Size]";

            component.ResearchCostFormula = "[Size] * 10";

            component.BuildPointCostFormula = "[Size]";

            component.MineralCostFormula = new JDictionary<Guid, string> { { new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"), "[Size] * 8" } };

            component.CreditCostFormula = "[Size]";

            component.MountType = new JDictionary<ComponentMountType, bool>();
            component.MountType.Add(ComponentMountType.ShipComponent, true);
            component.MountType.Add(ComponentMountType.ShipCargo, true);
            component.MountType.Add(ComponentMountType.PlanetFacility, false);
            component.MountType.Add(ComponentMountType.PDS, false);

            component.ComponentAbilitySDs = new List<ComponentAbilitySD>();

            ComponentAbilitySD SizeFormula0 = new ComponentAbilitySD();
            SizeFormula0.Name = "Engine Size";
            SizeFormula0.Description = "Size of this engine in Tons";
            SizeFormula0.GuiHint = GuiHint.GuiSelectionMaxMin;
            SizeFormula0.AbilityFormula = "250";
            SizeFormula0.MaxFormula = "2500";
            SizeFormula0.MinFormula = "1";
            component.ComponentAbilitySDs.Add(SizeFormula0);

            ComponentAbilitySD engineTypeAbility1 = new ComponentAbilitySD();
            engineTypeAbility1.Name = "Engine Type";
            engineTypeAbility1.Description = "Type of engine Tech";
            engineTypeAbility1.GuiHint = GuiHint.GuiTechSelectionList;
            engineTypeAbility1.GuidDictionary = new JDictionary<Guid, string>
            {
                {new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"),""},
                {new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"),""},
                {new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"),""},
                {new Guid("f3f10e56-9345-40cc-af42-342e7240355d"),""}
                //new Guid("58d047e6-c567-4db6-8c76-bfd4a201af94"),
                //new Guid("bd75bf88-1dad-4022-b401-acdf05ab73f8"),
                //new Guid("042ce9d4-5a2c-4d8e-9ae4-be059920839c"),
                //new Guid("93611831-9183-484a-9920-13b39d64e272"),
                //new Guid("32eda0ab-c117-4224-b148-6c9d0e474296"),
                //new Guid("cbb1a7ce-3c26-4b5b-abd7-9a99c670d68d"),
                //new Guid("6e34cc46-0693-4676-b0ca-f076fb36acaf"),
                //new Guid("9bb4d1c4-680f-4c98-b927-337654073575"),
                //new Guid("c9587310-f7dd-45d0-ac4c-b6f59a1e1897")
            };
            engineTypeAbility1.AbilityFormula = "TechData('f3f10e56-9345-40cc-af42-342e7240355d')";

            component.ComponentAbilitySDs.Add(engineTypeAbility1);

            ComponentAbilitySD enginePowerEfficency2 = new ComponentAbilitySD();
            enginePowerEfficency2.Name = "Engine Consumption vs Power";
            enginePowerEfficency2.Description = "More Powerfull engines are less efficent for a given size";
            enginePowerEfficency2.GuiHint = GuiHint.GuiSelectionMaxMin;
            enginePowerEfficency2.AbilityFormula = "1";
            enginePowerEfficency2.MaxFormula = "TechData('b8ef73c7-2ef0-445e-8461-1e0508958a0e')";
            enginePowerEfficency2.MinFormula = "TechData('08fa4c4b-0ddb-4b3a-9190-724d715694de')";
            component.ComponentAbilitySDs.Add(enginePowerEfficency2);

            ComponentAbilitySD enginePowerAbility3 = new ComponentAbilitySD();
            enginePowerAbility3.Name = "Engine Power";
            enginePowerAbility3.Description = "Move Power for ship";
            enginePowerAbility3.GuiHint = GuiHint.GuiTextDisplay;
            enginePowerAbility3.AbilityFormula = "Ability(1) * [Size] * Ability(2)";
            component.ComponentAbilitySDs.Add(enginePowerAbility3);

            ComponentAbilitySD enginePowerDBArgs4 = new ComponentAbilitySD();
            enginePowerDBArgs4.Name = "Engine Power";
            enginePowerDBArgs4.Description = "Move Power for ship";
            enginePowerDBArgs4.GuiHint = GuiHint.None;
            enginePowerDBArgs4.AbilityDataBlobType = typeof(EnginePowerDB).ToString();
            enginePowerDBArgs4.AbilityFormula = "DataBlobArgs(Ability(3))";
            component.ComponentAbilitySDs.Add(enginePowerDBArgs4);

            ComponentAbilitySD fuelConsumptionTechMod5 = new ComponentAbilitySD();
            fuelConsumptionTechMod5.Name = "Fuel Consumption";
            fuelConsumptionTechMod5.Description = "From Tech";
            fuelConsumptionTechMod5.GuiHint = GuiHint.None;
            fuelConsumptionTechMod5.AbilityFormula = "TechData('8557acb9-c764-44e7-8ee4-db2c2cebf0bc') * Pow(Ability(2), 2.25)";
            component.ComponentAbilitySDs.Add(fuelConsumptionTechMod5);

            ComponentAbilitySD fuelConsumptionFinalCalc6 = new ComponentAbilitySD();
            fuelConsumptionFinalCalc6.Name = "Fuel Consumption";
            fuelConsumptionFinalCalc6.Description = "Fuel Consumption Calc";
            fuelConsumptionFinalCalc6.GuiHint = GuiHint.GuiTextDisplay;
            fuelConsumptionFinalCalc6.AbilityFormula = "Ability(3) - Ability(3) * [Size] * 0.002 * Ability(5)";
            component.ComponentAbilitySDs.Add(fuelConsumptionFinalCalc6);

            ComponentAbilitySD fuelConsumptionArgsDB7 = new ComponentAbilitySD();
            fuelConsumptionArgsDB7.Name = "Fuel Consumption";
            fuelConsumptionArgsDB7.Description = "Size Mod";
            fuelConsumptionArgsDB7.GuiHint = GuiHint.None;
            fuelConsumptionArgsDB7.AbilityDataBlobType = typeof(FuelUseDB).ToString();
            fuelConsumptionArgsDB7.AbilityFormula = "DataBlobArgs(Ability(6))";
            component.ComponentAbilitySDs.Add(fuelConsumptionArgsDB7);

            ComponentAbilitySD thermalReduction8 = new ComponentAbilitySD();
            thermalReduction8.Name = "Thermal Signature Reduction";
            thermalReduction8.Description = "";
            thermalReduction8.GuiHint = GuiHint.GuiSelectionMaxMin;
            thermalReduction8.AbilityFormula = "0";
            thermalReduction8.MinFormula = "0";
            thermalReduction8.MaxFormula = "0.5"; 
            component.ComponentAbilitySDs.Add(thermalReduction8);

            ComponentAbilitySD sensorSigDisplay9 = new ComponentAbilitySD();
            sensorSigDisplay9.Name = "Thermal Signature";
            sensorSigDisplay9.Description = "";
            sensorSigDisplay9.GuiHint = GuiHint.GuiTextDisplay;
            sensorSigDisplay9.AbilityFormula = "Ability(3) * Ability(8)";
            component.ComponentAbilitySDs.Add(sensorSigDisplay9);

            ComponentAbilitySD sensorSigDBArgs10 = new ComponentAbilitySD();
            sensorSigDBArgs10.Name = "Sensor Signature";
            sensorSigDBArgs10.Description = "";
            sensorSigDBArgs10.GuiHint = GuiHint.None;
            sensorSigDBArgs10.AbilityDataBlobType = typeof(SensorSignatureDB).ToString();
            sensorSigDBArgs10.AbilityFormula = "DataBlobArgs(Ability(9),0)";
            component.ComponentAbilitySDs.Add(sensorSigDBArgs10);
            
            return component;
        }

        public static ComponentSD MineInstallation()
        {
            ComponentSD component = new ComponentSD();
            component.Name = "Mine";
            component.Description = "Mines Resources";
            component.ID = new Guid("F7084155-04C3-49E8-BF43-C7EF4BEFA550");

            component.SizeFormula = "25000";

            component.HTKFormula = "[Size]";

            component.CrewReqFormula = "50000";

            component.ResearchCostFormula = "0";
            
            component.BuildPointCostFormula = "[Size]";

            component.MineralCostFormula = new JDictionary<Guid, string> {{new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"), "60"}, 
            {new Guid("2ae2a928-3e14-45d5-befc-5bd6ed16ec0a"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = new JDictionary<ComponentMountType, bool>();
            component.MountType.Add(ComponentMountType.ShipComponent, false);
            component.MountType.Add(ComponentMountType.ShipCargo, true);
            component.MountType.Add(ComponentMountType.PlanetFacility, true);
            component.MountType.Add(ComponentMountType.PDS, false);

            component.ComponentAbilitySDs = new List<ComponentAbilitySD>();


            ComponentAbilitySD mineAbility = new ComponentAbilitySD();
            mineAbility.Name = "MiningAmount";
            mineAbility.Description = "";
            mineAbility.GuiHint = GuiHint.None;
            mineAbility.GuidDictionary = new JDictionary<Guid, string>
            {
                {new Guid("08f15d35-ea1d-442f-a2e3-bde04c5c22e9"),"10"},
                {new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"),"10"},
                {new Guid("b12acce2-7c7d-4acf-ad41-1c08093fcad8"),"10"},
                {new Guid("a03863a3-a364-45ff-8c7b-a4f7486bd710"),"10"},
                {new Guid("14a83fc3-04fd-4ea3-82f4-290995c4418e"),"10"},
                {new Guid("29b3797d-f9d7-4e73-a593-2b05bf4ef012"),"10"},
                {new Guid("5fdfb85c-fad2-4a48-8a75-04eb65dc9741"),"10"},
                {new Guid("2ae2a928-3e14-45d5-befc-5bd6ed16ec0a"),"10"},
                {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"),"10"},
                {new Guid("6a38b268-041f-4103-9d23-c13c1041cecd"),"10"},
                {new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"),"10"}

            };
            mineAbility.AbilityDataBlobType = typeof(MineResourcesDB).ToString();
            mineAbility.AbilityFormula = "DataBlobArgs([GuidDict])";
            component.ComponentAbilitySDs.Add(mineAbility);
            
            return component;

        }

        public static ComponentSD ResearchLab()
        {
            ComponentSD component = new ComponentSD();
            component.Name = "ResearchLab";
            component.Description = "Creates Research Points";
            component.ID = new Guid("C203B7CF-8B41-4664-8291-D20DFE1119EC");

            component.SizeFormula = "500000";

            component.HTKFormula = "[Size]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Size]";

            component.MineralCostFormula = new JDictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = new JDictionary<ComponentMountType, bool>();
            component.MountType.Add(ComponentMountType.ShipComponent, false);
            component.MountType.Add(ComponentMountType.ShipCargo, true);
            component.MountType.Add(ComponentMountType.PlanetFacility, true);
            component.MountType.Add(ComponentMountType.PDS, false);

            component.ComponentAbilitySDs = new List<ComponentAbilitySD>();


            ComponentAbilitySD researchPointsAbility = new ComponentAbilitySD();
            researchPointsAbility.Name = "RP Amount Per EconTick";
            researchPointsAbility.Description = "";
            researchPointsAbility.GuiHint = GuiHint.None;
            researchPointsAbility.AbilityDataBlobType = typeof(ResearchPointsAbilityDB).ToString();
            researchPointsAbility.AbilityFormula = "DataBlobArgs(20)";
            component.ComponentAbilitySDs.Add(researchPointsAbility);

            return component;

        }



        public static ComponentSD Refinary()
        {
            ComponentSD component = new ComponentSD();
            component.Name = "Refinary";
            component.Description = "Creates Research Points";
            component.ID = new Guid("{90592586-0BD6-4885-8526-7181E08556B5}");

            component.SizeFormula = "500000";

            component.HTKFormula = "[Size]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Size]";

            component.MineralCostFormula = new JDictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = new JDictionary<ComponentMountType, bool>();
            component.MountType.Add(ComponentMountType.ShipComponent, false);
            component.MountType.Add(ComponentMountType.ShipCargo, true);
            component.MountType.Add(ComponentMountType.PlanetFacility, true);
            component.MountType.Add(ComponentMountType.PDS, false);

            component.ComponentAbilitySDs = new List<ComponentAbilitySD>();



            ComponentAbilitySD refinePointsAbility = new ComponentAbilitySD();
            refinePointsAbility.Name = "RP Amount Per EconTick";
            refinePointsAbility.Description = "";
            refinePointsAbility.GuiHint = GuiHint.None;
            refinePointsAbility.AbilityDataBlobType = typeof(ResearchPointsAbilityDB).ToString();
            refinePointsAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(refinePointsAbility);

            ComponentAbilitySD refineJobsAbility = new ComponentAbilitySD();
            refineJobsAbility.Name = "RefineAbilitys";
            refineJobsAbility.Description = "";
            refineJobsAbility.GuiHint = GuiHint.None;
            refineJobsAbility.GuidDictionary = new JDictionary<Guid, string>
            {
                { new Guid("33E6AC88-0235-4917-A7FF-35C8886AAD3A"),"0"},
                { new Guid("6DA93677-EE08-4853-A8A5-0F46D93FE0EB"),"0"}
            };
            refineJobsAbility.AbilityDataBlobType = typeof(RefineResourcesDB).ToString();
            refineJobsAbility.AbilityFormula = "DataBlobArgs([GuidDict], Ability(0))";
            component.ComponentAbilitySDs.Add(refineJobsAbility);
            return component;
        }

        public static ComponentSD Factory()
        {
            ComponentSD component = new ComponentSD();
            component.Name = "Factory";
            component.Description = "Constructs Facilities, Fighters Ammo and Components";
            component.ID = new Guid("{07817639-E0C6-43CD-B3DC-24ED15EFB4BA}");

            component.SizeFormula = "500000";

            component.HTKFormula = "[Size]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Size]";

            component.MineralCostFormula = new JDictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = new JDictionary<ComponentMountType, bool>();
            component.MountType.Add(ComponentMountType.ShipComponent, false);
            component.MountType.Add(ComponentMountType.ShipCargo, true);
            component.MountType.Add(ComponentMountType.PlanetFacility, true);
            component.MountType.Add(ComponentMountType.PDS, false);

            component.ComponentAbilitySDs = new List<ComponentAbilitySD>();

            ComponentAbilitySD instalationPointsAbility = new ComponentAbilitySD();
            instalationPointsAbility.Name = "Construction Points";
            instalationPointsAbility.Description = "";
            instalationPointsAbility.GuiHint = GuiHint.None;
            instalationPointsAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(instalationPointsAbility);

            ComponentAbilitySD instalationConstructionAbility = new ComponentAbilitySD();
            instalationConstructionAbility.Name = "Construction Points";
            instalationConstructionAbility.Description = "";
            instalationConstructionAbility.GuiHint = GuiHint.None;
            instalationConstructionAbility.AbilityDataBlobType = typeof(ConstructInstationsAbilityDB).ToString();
            instalationConstructionAbility.AbilityFormula = "DataBlobArgs(Ability(0))";
            component.ComponentAbilitySDs.Add(instalationConstructionAbility);

            ComponentAbilitySD shipComponentsConstructionAbility = new ComponentAbilitySD();
            shipComponentsConstructionAbility.Name = "Construction Points";
            shipComponentsConstructionAbility.Description = "";
            shipComponentsConstructionAbility.GuiHint = GuiHint.None;
            shipComponentsConstructionAbility.AbilityDataBlobType = typeof(ConstructShipComponentsAbilityDB).ToString();
            shipComponentsConstructionAbility.AbilityFormula = "DataBlobArgs(Ability(0))";
            component.ComponentAbilitySDs.Add(shipComponentsConstructionAbility);

            ComponentAbilitySD fighterConstructionAbility = new ComponentAbilitySD();
            shipComponentsConstructionAbility.Name = "Construction Points";
            shipComponentsConstructionAbility.Description = "";
            shipComponentsConstructionAbility.GuiHint = GuiHint.None;
            shipComponentsConstructionAbility.AbilityDataBlobType = typeof(ConstructFightersAbilityDB).ToString();
            shipComponentsConstructionAbility.AbilityFormula = "DataBlobArgs(Ability(0))";
            component.ComponentAbilitySDs.Add(fighterConstructionAbility);

            ComponentAbilitySD ammoConstructionAbility = new ComponentAbilitySD();
            shipComponentsConstructionAbility.Name = "Construction Points";
            shipComponentsConstructionAbility.Description = "";
            shipComponentsConstructionAbility.GuiHint = GuiHint.None;
            shipComponentsConstructionAbility.AbilityDataBlobType = typeof(ConstructAmmoAbilityDB).ToString();
            shipComponentsConstructionAbility.AbilityFormula = "DataBlobArgs(Ability(0))";
            component.ComponentAbilitySDs.Add(ammoConstructionAbility);


            return component;
        }
    }
}
