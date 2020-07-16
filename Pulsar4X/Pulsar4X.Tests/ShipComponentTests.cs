using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.ECSLib.Industry;

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
        //private Entity _shipClass;
        private Entity _ship;
        private Entity _engineComponent;
        [SetUp]
        public void Init()
        {
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = 1 });
            //Tech();
            // @todo: change this so we can look up researched techs by name
            _faction = FactionFactory.CreateFaction(_game, "Terran");
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e"),3);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de"), 3);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc"), 5);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"), 1);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"), 1);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"), 1);
            _starSystem = new StarSystem(_game, "Sol", -1);
            /////Ship Class/////
            //_shipClass = ShipFactory.CreateNewShipClass(_game, _faction, "TestClass");
        }

        [Test]
        public void ExportComponents()
        {
            ComponentTemplateSD engine = EngineComponentSD();
            ComponentTemplateSD mine = MineInstallation();
            ComponentTemplateSD lab = ResearchLab();
            ComponentTemplateSD refinery = Refinery();
            ComponentTemplateSD factory = Factory();
            Dictionary<Guid, ComponentTemplateSD> componentsDict = new Dictionary<Guid, ComponentTemplateSD>();
            componentsDict.Add(engine.ID, engine);
            componentsDict.Add(mine.ID, mine);
            componentsDict.Add(lab.ID,lab);
            componentsDict.Add(refinery.ID, refinery);
            componentsDict.Add(factory.ID, factory);
            StaticDataManager.ExportStaticData(componentsDict, "ComponentData.json");
        }

        [Test]
        public void TestEngineComponentFactory()
        {
            ComponentTemplateSD componentTemplateSD = EngineComponentSD();

            ComponentDesigner designer = new ComponentDesigner(componentTemplateSD, _faction.GetDataBlob<FactionTechDB>());

            foreach (var ability in designer.ComponentDesignAttributes.Values)
            {
                if (ability.GuiHint == GuiHint.GuiTechSelectionList)
                {
                    List<Guid> selectionlist = ability.GuidDictionary.Keys.Cast<Guid>().ToList();
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

            designer.ComponentDesignAttributes["Size"].SetValueFromInput(250);

            ComponentDesign engineDesign = designer.CreateDesign(_faction);

            Assert.AreEqual(250, engineDesign.MassPerUnit);

            Dictionary<Guid, ComponentTemplateSD> componentsDict = new Dictionary<Guid, ComponentTemplateSD>();
            componentsDict.Add(componentTemplateSD.ID, componentTemplateSD);
            StaticDataManager.ExportStaticData(componentsDict, "EngineComponentTest.json");

        }

        [Test]
        public void TestMineInstalationFactory()
        {
            ComponentTemplateSD mine = MineInstallation();

            ComponentDesigner mineDesigner = new ComponentDesigner(mine, _faction.GetDataBlob<FactionTechDB>());
            mineDesigner.ComponentDesignAttributes["MiningAmount"].SetValue();
            ComponentDesign mineDesign = mineDesigner.CreateDesign(_faction);

            Assert.AreEqual(10, mineDesign.GetAttribute<MineResourcesAtbDB>().ResourcesPerEconTick.Values.ElementAt(0));

            Dictionary<Guid, ComponentTemplateSD> componentsDict = new Dictionary<Guid, ComponentTemplateSD>();
            componentsDict.Add(mine.ID, mine);
            StaticDataManager.ExportStaticData(componentsDict, "MineComponentTest.json");

        }

        [Test]
        public void TestCargoComponentCreation()
        {
            ComponentTemplateSD cargo = GeneralCargo();

            ComponentDesigner cargoDesigner = new ComponentDesigner(cargo, _faction.GetDataBlob<FactionTechDB>());
            cargoDesigner.ComponentDesignAttributes["Size"].SetValue();
 
            ComponentDesign cargoDesign = cargoDesigner.CreateDesign(_faction);
            
            bool hasAttribute = cargoDesign.TryGetAttribute<CargoStorageAtbDB>(out var attributeDB);
            Assert.IsTrue(hasAttribute);
            

            
            CargoTypeSD cargotype = _game.StaticData.CargoTypes[attributeDB.CargoTypeGuid];

            Assert.AreEqual(100, attributeDB.StorageCapacity);

            Dictionary<Guid, ComponentTemplateSD> componentsDict = new Dictionary<Guid, ComponentTemplateSD>();
            componentsDict.Add(cargo.ID, cargo);
            StaticDataManager.ExportStaticData(componentsDict, "CargoComponentTest.json");

        }



        public static ComponentTemplateSD EngineComponentSD()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Engine";
            component.DescriptionFormula = "Moves a ship";
            component.ID = new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6");

            component.MassFormula = "Ability(0)";
            component.VolumeFormula = "[Mass] / 2";
            component.HTKFormula = "Max(1, [Mass] / 100)";

            component.CrewReqFormula = "[Mass]";

            component.ResearchCostFormula = "[Mass] * 10";

            component.BuildPointCostFormula = "[Mass]";

            component.ResourceCostFormula = new Dictionary<Guid, string> { { new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"), "[Mass] * 8" } };

            component.CreditCostFormula = "[Mass]";

            component.MountType = ComponentMountType.ShipComponent | ComponentMountType.ShipCargo | ComponentMountType.Fighter;

            component.ComponentAtbSDs = new List<ComponentTemplateAttributeSD>();

            ComponentTemplateAttributeSD SizeFormula0 = new ComponentTemplateAttributeSD();
            SizeFormula0.Name = "Size";
            SizeFormula0.DescriptionFormula = "Size of this engine in Tons";
            SizeFormula0.GuiHint = GuiHint.GuiSelectionMaxMin;
            SizeFormula0.AttributeFormula = "250";
            SizeFormula0.MaxFormula = "2500";
            SizeFormula0.MinFormula = "1";
            SizeFormula0.StepFormula = "1";
            component.ComponentAtbSDs.Add(SizeFormula0);

            ComponentTemplateAttributeSD engineTypeAbility1 = new ComponentTemplateAttributeSD();
            engineTypeAbility1.Name = "Engine Type";
            engineTypeAbility1.DescriptionFormula = "Type of engine Tech";
            engineTypeAbility1.GuiHint = GuiHint.GuiTechSelectionList;
            engineTypeAbility1.GuidDictionary = new Dictionary<object, string>
            {
                {new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"),""},
                {new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"),""},
                {new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"),""},
                {new Guid("f3f10e56-9345-40cc-af42-342e7240355d"),""}
                //new ID("58d047e6-c567-4db6-8c76-bfd4a201af94"),
                //new ID("bd75bf88-1dad-4022-b401-acdf05ab73f8"),
                //new ID("042ce9d4-5a2c-4d8e-9ae4-be059920839c"),
                //new ID("93611831-9183-484a-9920-13b39d64e272"),
                //new ID("32eda0ab-c117-4224-b148-6c9d0e474296"),
                //new ID("cbb1a7ce-3c26-4b5b-abd7-9a99c670d68d"),
                //new ID("6e34cc46-0693-4676-b0ca-f076fb36acaf"),
                //new ID("9bb4d1c4-680f-4c98-b927-337654073575"),
                //new ID("c9587310-f7dd-45d0-ac4c-b6f59a1e1897")
            };
            engineTypeAbility1.AttributeFormula = "TechData('f3f10e56-9345-40cc-af42-342e7240355d')";

            component.ComponentAtbSDs.Add(engineTypeAbility1);

            ComponentTemplateAttributeSD enginePowerEfficency2 = new ComponentTemplateAttributeSD();
            enginePowerEfficency2.Name = "Engine Consumption vs Power";
            enginePowerEfficency2.DescriptionFormula = "More Powerfull engines are less efficent for a given size";
            enginePowerEfficency2.GuiHint = GuiHint.GuiSelectionMaxMin;
            enginePowerEfficency2.AttributeFormula = "1";
            enginePowerEfficency2.MaxFormula = "TechData('b8ef73c7-2ef0-445e-8461-1e0508958a0e')";
            enginePowerEfficency2.MinFormula = "TechData('08fa4c4b-0ddb-4b3a-9190-724d715694de')";
            enginePowerEfficency2.StepFormula = "0.1";
            component.ComponentAtbSDs.Add(enginePowerEfficency2);

            ComponentTemplateAttributeSD enginePowerAbility3 = new ComponentTemplateAttributeSD();
            enginePowerAbility3.Name = "Engine Power";
            enginePowerAbility3.DescriptionFormula = "Move Power for ship";
            enginePowerAbility3.GuiHint = GuiHint.GuiTextDisplay;
            enginePowerAbility3.AttributeFormula = "Ability(1) * [Mass] * Ability(2)";
            component.ComponentAtbSDs.Add(enginePowerAbility3);

            ComponentTemplateAttributeSD enginePowerDBArgs4 = new ComponentTemplateAttributeSD();
            enginePowerDBArgs4.Name = "Engine Powerdb";
            enginePowerDBArgs4.DescriptionFormula = "Move Power for ship";
            enginePowerDBArgs4.GuiHint = GuiHint.None;
            enginePowerDBArgs4.AttributeType = typeof(WarpDriveAtb).ToString();
            enginePowerDBArgs4.AttributeFormula = "AtbConstrArgs(Ability(3))";
            component.ComponentAtbSDs.Add(enginePowerDBArgs4);

            ComponentTemplateAttributeSD fuelConsumptionTechMod5 = new ComponentTemplateAttributeSD();
            fuelConsumptionTechMod5.Name = "Fuel Consumption";
            fuelConsumptionTechMod5.DescriptionFormula = "From Tech";
            fuelConsumptionTechMod5.GuiHint = GuiHint.None;
            fuelConsumptionTechMod5.AttributeFormula = "TechData('8557acb9-c764-44e7-8ee4-db2c2cebf0bc') * Pow(Ability(2), 2.25)";
            component.ComponentAtbSDs.Add(fuelConsumptionTechMod5);

            ComponentTemplateAttributeSD fuelConsumptionFinalCalc6 = new ComponentTemplateAttributeSD();
            fuelConsumptionFinalCalc6.Name = "Fuel Consumptioncalc";
            fuelConsumptionFinalCalc6.DescriptionFormula = "Fuel Consumption Calc";
            fuelConsumptionFinalCalc6.GuiHint = GuiHint.GuiTextDisplay;
            fuelConsumptionFinalCalc6.AttributeFormula = "Ability(3) - Ability(3) * [Mass] * 0.002 * Ability(5)";
            component.ComponentAtbSDs.Add(fuelConsumptionFinalCalc6);

            ComponentTemplateAttributeSD fuelConsumptionArgsDB7 = new ComponentTemplateAttributeSD();
            fuelConsumptionArgsDB7.Name = "Fuel Consumptiondb";
            fuelConsumptionArgsDB7.DescriptionFormula = "";
            fuelConsumptionArgsDB7.GuiHint = GuiHint.None;
            fuelConsumptionArgsDB7.AttributeType = typeof(ResourceConsumptionAtbDB).ToString();
            fuelConsumptionArgsDB7.AttributeFormula = "AtbConstrArgs(GuidString('33e6ac88-0235-4917-a7ff-35c8886aad3a'), Ability(6), 1)";
            component.ComponentAtbSDs.Add(fuelConsumptionArgsDB7);

            ComponentTemplateAttributeSD thermalReduction8 = new ComponentTemplateAttributeSD();
            thermalReduction8.Name = "Thermal Signature Reduction";
            thermalReduction8.DescriptionFormula = "";
            thermalReduction8.GuiHint = GuiHint.GuiSelectionMaxMin;
            thermalReduction8.AttributeFormula = "0";
            thermalReduction8.MinFormula = "0";
            thermalReduction8.MaxFormula = "0.5";
            thermalReduction8.StepFormula = "0.1";
            component.ComponentAtbSDs.Add(thermalReduction8);

            ComponentTemplateAttributeSD sensorSigDisplay9 = new ComponentTemplateAttributeSD();
            sensorSigDisplay9.Name = "Thermal Signature";
            sensorSigDisplay9.DescriptionFormula = "";
            sensorSigDisplay9.GuiHint = GuiHint.GuiTextDisplay;
            sensorSigDisplay9.AttributeFormula = "Ability(3) * Ability(8)";
            component.ComponentAtbSDs.Add(sensorSigDisplay9);

            ComponentTemplateAttributeSD sensorSigDBArgs10 = new ComponentTemplateAttributeSD();
            sensorSigDBArgs10.Name = "Sensor Signaturedb";
            sensorSigDBArgs10.DescriptionFormula = "";
            sensorSigDBArgs10.GuiHint = GuiHint.None;
            sensorSigDBArgs10.AttributeType = typeof(SensorSignatureAtbDB).ToString();
            sensorSigDBArgs10.AttributeFormula = "AtbConstrArgs(Ability(9),0)";
            component.ComponentAtbSDs.Add(sensorSigDBArgs10);
            
            return component;
        }

        public static ComponentTemplateSD MineInstallation()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Mine";
            component.DescriptionFormula = "Mines Resources";
            component.ID = new Guid("F7084155-04C3-49E8-BF43-C7EF4BEFA550");

            component.MassFormula = "25000";

            component.VolumeFormula = "[Mass] / 2";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "50000";

            component.ResearchCostFormula = "0";
            
            component.BuildPointCostFormula = "[Mass]";

            component.ResourceCostFormula = new Dictionary<Guid, string> {{new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"), "60"}, 
            {new Guid("2ae2a928-3e14-45d5-befc-5bd6ed16ec0a"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            component.ComponentAtbSDs = new List<ComponentTemplateAttributeSD>();


            ComponentTemplateAttributeSD mineAttribute = new ComponentTemplateAttributeSD();
            mineAttribute.Name = "MiningAmount";
            mineAttribute.DescriptionFormula = "";
            mineAttribute.GuiHint = GuiHint.None;
            mineAttribute.GuidDictionary = new Dictionary<object, string>
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
            mineAttribute.AttributeType = typeof(MineResourcesAtbDB).ToString();
            mineAttribute.AttributeFormula = "AtbConstrArgs([GuidDict])";
            component.ComponentAtbSDs.Add(mineAttribute);
            
            return component;

        }

        public static ComponentTemplateSD ResearchLab()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "ResearchLab";
            component.DescriptionFormula = "Creates Research Points";
            component.ID = new Guid("C203B7CF-8B41-4664-8291-D20DFE1119EC");

            component.MassFormula = "500000";

            component.VolumeFormula = "[Mass] / 2";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Mass]";

            component.ResourceCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            component.ComponentAtbSDs = new List<ComponentTemplateAttributeSD>();


            ComponentTemplateAttributeSD researchPointsAttribute = new ComponentTemplateAttributeSD();
            researchPointsAttribute.Name = "RP Amount Per EconTick";
            researchPointsAttribute.DescriptionFormula = "";
            researchPointsAttribute.GuiHint = GuiHint.None;
            researchPointsAttribute.AttributeType = typeof(ResearchPointsAtbDB).ToString();
            researchPointsAttribute.AttributeFormula = "AtbConstrArgs(20)";
            component.ComponentAtbSDs.Add(researchPointsAttribute);

            return component;

        }

        public static ComponentTemplateSD Refinery()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Refinery";
            component.DescriptionFormula = "Creates Research Points";
            component.ID = new Guid("{90592586-0BD6-4885-8526-7181E08556B5}");

            component.MassFormula = "500000";

            component.VolumeFormula = "[Mass] / 2";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Mass]";

            component.ResourceCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            component.ComponentAtbSDs = new List<ComponentTemplateAttributeSD>();
            
            ComponentTemplateAttributeSD refinePointsAttribute = new ComponentTemplateAttributeSD();
            refinePointsAttribute.Name = "RP Amount Per EconTick";
            refinePointsAttribute.DescriptionFormula = "";
            refinePointsAttribute.GuiHint = GuiHint.None;
            refinePointsAttribute.AttributeType = typeof(ResearchPointsAtbDB).ToString();
            refinePointsAttribute.AttributeFormula = "100";
            component.ComponentAtbSDs.Add(refinePointsAttribute);

            ComponentTemplateAttributeSD refineJobsAttribute = new ComponentTemplateAttributeSD();
            refineJobsAttribute.Name = "RefineAbilitys";
            refineJobsAttribute.DescriptionFormula = "";
            refineJobsAttribute.GuiHint = GuiHint.None;
            refineJobsAttribute.GuidDictionary = new Dictionary<object, string>
            {
                { new Guid("33E6AC88-0235-4917-A7FF-35C8886AAD3A"),"0"},
                { new Guid("6DA93677-EE08-4853-A8A5-0F46D93FE0EB"),"0"}
            };
            refineJobsAttribute.AttributeType = typeof(IndustryAtb).ToString();
            refineJobsAttribute.AttributeFormula = "AtbConstrArgs([GuidDict], Ability(0))";
            component.ComponentAtbSDs.Add(refineJobsAttribute);
            return component;
        }

        public static ComponentTemplateSD Factory()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Factory";
            component.DescriptionFormula = "Constructs Facilities, Fighters Ammo and Components";
            component.ID = new Guid("{07817639-E0C6-43CD-B3DC-24ED15EFB4BA}");

            component.MassFormula = "500000";

            component.VolumeFormula = "[Mass] / 2";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Mass]";

            component.ResourceCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            //component.IndustryTypeID = IndustryType.Installations;

            component.ComponentAtbSDs = new List<ComponentTemplateAttributeSD>();


            ComponentTemplateAttributeSD instalationConstructionAttribute = new ComponentTemplateAttributeSD();
            instalationConstructionAttribute.Name = "Instalation Construction Points";
            instalationConstructionAttribute.DescriptionFormula = "";
            instalationConstructionAttribute.GuiHint = GuiHint.GuiTextDisplay;
            instalationConstructionAttribute.AttributeFormula = "100";
            component.ComponentAtbSDs.Add(instalationConstructionAttribute);

            ComponentTemplateAttributeSD shipComponentsConstructionAttribute = new ComponentTemplateAttributeSD();
            shipComponentsConstructionAttribute.Name = "Component Construction Points";
            shipComponentsConstructionAttribute.DescriptionFormula = "";
            shipComponentsConstructionAttribute.GuiHint = GuiHint.GuiTextDisplay;
            shipComponentsConstructionAttribute.AttributeFormula = "100";
            component.ComponentAtbSDs.Add(shipComponentsConstructionAttribute);


            ComponentTemplateAttributeSD shipConstructionAttribute = new ComponentTemplateAttributeSD();
            shipConstructionAttribute.Name = "Ship Construction Points";
            shipConstructionAttribute.DescriptionFormula = "";
            shipConstructionAttribute.GuiHint = GuiHint.GuiTextDisplay;
            shipConstructionAttribute.AttributeFormula = "100";
            component.ComponentAtbSDs.Add(shipConstructionAttribute);

            ComponentTemplateAttributeSD fighterConstructionAttribute = new ComponentTemplateAttributeSD();
            fighterConstructionAttribute.Name = "Fighter Construction Points";
            fighterConstructionAttribute.DescriptionFormula = "";
            fighterConstructionAttribute.GuiHint = GuiHint.GuiTextDisplay;
            fighterConstructionAttribute.AttributeFormula = "100";
            component.ComponentAtbSDs.Add(fighterConstructionAttribute);

            ComponentTemplateAttributeSD ammoConstructionAttribute = new ComponentTemplateAttributeSD();
            ammoConstructionAttribute.Name = "Ordnance Construction Points";
            ammoConstructionAttribute.DescriptionFormula = "";
            ammoConstructionAttribute.GuiHint = GuiHint.GuiTextDisplay;
            ammoConstructionAttribute.AttributeFormula = "100";
            component.ComponentAtbSDs.Add(ammoConstructionAttribute);

            ComponentTemplateAttributeSD atbconstructor = new ComponentTemplateAttributeSD();
            atbconstructor.Name = "Construction Points";
            atbconstructor.DescriptionFormula = "";
            atbconstructor.GuiHint = GuiHint.None;
            atbconstructor.GuidDictionary = new Dictionary<object, string>() {
                { "Installations", "Ability(0)" },
                { "ShipComponents", "Ability(1)" },
                { "Ships", "Ability(2)" },
                { "Fighters", "Ability(3)" },
                { "Ordnance", "Ability(4)" }
            };
            atbconstructor.AttributeType = typeof(IndustryAtb).ToString();
            atbconstructor.AttributeFormula = "AtbConstrArgs(EnumDict('Pulsar4X.ECSLib.IndustryTypeID'))";
            component.ComponentAtbSDs.Add(atbconstructor);

            return component;
        }

        public static ComponentTemplateSD GeneralCargo()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "General Storage";
            component.DescriptionFormula = "Stores General Cargo";
            component.ID = new Guid("{30CD60F8-1DE3-4FAA-ACBA-0933EB84C199}");

            component.MassFormula = "500000";

            component.VolumeFormula = "[Mass] / 100";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Mass]";

            component.ResourceCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"},
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;
            //component.IndustryTypeID = IndustryType.Installations | IndustryType.ShipComponents;
            component.ComponentAtbSDs = new List<ComponentTemplateAttributeSD>();

            ComponentTemplateAttributeSD genralCargoAttribute = new ComponentTemplateAttributeSD();
            genralCargoAttribute.Name = "Size";
            genralCargoAttribute.DescriptionFormula = "";
            genralCargoAttribute.GuiHint = GuiHint.GuiTextDisplay;
            genralCargoAttribute.AttributeFormula = "100";
            component.ComponentAtbSDs.Add(genralCargoAttribute);

            ComponentTemplateAttributeSD rate = new ComponentTemplateAttributeSD();
            rate.Name = "Transfer Rate";
            rate.DescriptionFormula = "";
            rate.GuiHint = GuiHint.GuiTextDisplay;
            rate.AttributeFormula = "50000";
            component.ComponentAtbSDs.Add(rate);
            
            ComponentTemplateAttributeSD range = new ComponentTemplateAttributeSD();
            range.Name = "Transfer Dv Range";
            range.DescriptionFormula = "";
            range.GuiHint = GuiHint.GuiTextDisplay;
            range.AttributeFormula = "50000";
            component.ComponentAtbSDs.Add(range);
            
            ComponentTemplateAttributeSD generalCargoCapacityAttribute = new ComponentTemplateAttributeSD();
            generalCargoCapacityAttribute.Name = "Construction Points";
            generalCargoCapacityAttribute.DescriptionFormula = "";
            generalCargoCapacityAttribute.GuiHint = GuiHint.None;
            generalCargoCapacityAttribute.AttributeType = typeof(CargoStorageAtbDB).ToString();
            generalCargoCapacityAttribute.AttributeFormula = "AtbConstrArgs(Ability(0), GuidString('16b4c4f0-7292-4f4d-8fea-22103c70b288'), Ability(1), Ability(2))";
            component.ComponentAtbSDs.Add(generalCargoCapacityAttribute);

            return component;
        }

    }
}
