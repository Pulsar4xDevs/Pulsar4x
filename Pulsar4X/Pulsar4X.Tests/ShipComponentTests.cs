using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Linq;

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
            _shipClass = ShipFactory.CreateNewShipClass(_game, _faction, "TestClass");
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
            ComponentTemplateSD engine = EngineComponentSD();

            ComponentDesign design = GenericComponentFactory.StaticToDesign(engine, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);

            foreach (var ability in design.ComponentDesignAbilities)
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

            design.ComponentDesignAbilities[0].SetValueFromInput(250);

            Entity engineEntity = GenericComponentFactory.DesignToDesignEntity(_game, _faction, design);

            Assert.AreEqual(250, engineEntity.GetDataBlob<ComponentInfoDB>().SizeInTons);

            Dictionary<Guid, ComponentTemplateSD> componentsDict = new Dictionary<Guid, ComponentTemplateSD>();
            componentsDict.Add(engine.ID, engine);
            StaticDataManager.ExportStaticData(componentsDict, "EngineComponentTest.json");

        }

        [Test]
        public void TestMineInstalationFactory()
        {
            ComponentTemplateSD mine = MineInstallation();

            ComponentDesign mineDesign = GenericComponentFactory.StaticToDesign(mine, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);
            mineDesign.ComponentDesignAbilities[0].SetValue();
            Entity mineEntity = GenericComponentFactory.DesignToDesignEntity(_game, _faction, mineDesign);

            Assert.AreEqual(10, mineEntity.GetDataBlob<MineResourcesAtbDB>().ResourcesPerEconTick.Values.ElementAt(0));

            Dictionary<Guid, ComponentTemplateSD> componentsDict = new Dictionary<Guid, ComponentTemplateSD>();
            componentsDict.Add(mine.ID, mine);
            StaticDataManager.ExportStaticData(componentsDict, "MineComponentTest.json");

        }

        [Test]
        public void TestCargoComponentCreation()
        {
            ComponentTemplateSD cargo = GeneralCargo();

            ComponentDesign cargoDesign = GenericComponentFactory.StaticToDesign(cargo, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);
            cargoDesign.ComponentDesignAbilities[0].SetValue();
            Entity cargoEntity = GenericComponentFactory.DesignToDesignEntity(_game, _faction, cargoDesign);

            CargoStorageAtbDB attributeDB = cargoEntity.GetDataBlob<CargoStorageAtbDB>();

            
            CargoTypeSD cargotype = _game.StaticData.CargoTypes[attributeDB.CargoTypeGuid];

            Assert.AreEqual(100, attributeDB.StorageCapacity);

            Dictionary<Guid, ComponentTemplateSD> componentsDict = new Dictionary<Guid, ComponentTemplateSD>();
            componentsDict.Add(cargo.ID, cargo);
            StaticDataManager.ExportStaticData(componentsDict, "CargoComponentTest.json");

        }

        [Test]
        public void TestFactoryComponentCreation()
        {
            ComponentTemplateSD factory = Factory();

            ComponentDesign facDesign = GenericComponentFactory.StaticToDesign(factory, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);
            facDesign.ComponentDesignAbilities[0].SetValue();
            Entity facDesignEntity = GenericComponentFactory.DesignToDesignEntity(_game, _faction, facDesign);

            ConstructionAtbDB attributeDB = facDesignEntity.GetDataBlob<ConstructionAtbDB>();

            Assert.AreEqual(100, attributeDB.ConstructionPoints[ConstructionType.ShipComponents]);

            Dictionary<Guid, ComponentTemplateSD> componentsDict = new Dictionary<Guid, ComponentTemplateSD>();
            componentsDict.Add(factory.ID, factory);
            StaticDataManager.ExportStaticData(componentsDict, "FactoryComponentTest.json");

        }

        public static ComponentTemplateSD EngineComponentSD()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Engine";
            component.Description = "Moves a ship";
            component.ID = new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6");

            component.MassFormula = "Ability(0)";
            component.VolumeFormula = "[Mass] / 2";
            component.HTKFormula = "Max(1, [Mass] / 100)";

            component.CrewReqFormula = "[Mass]";

            component.ResearchCostFormula = "[Mass] * 10";

            component.BuildPointCostFormula = "[Mass]";

            component.MineralCostFormula = new Dictionary<Guid, string> { { new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"), "[Mass] * 8" } };

            component.CreditCostFormula = "[Mass]";

            component.MountType = ComponentMountType.ShipComponent | ComponentMountType.ShipCargo | ComponentMountType.Fighter;

            component.ComponentAbilitySDs = new List<ComponentTemplateAbilitySD>();

            ComponentTemplateAbilitySD SizeFormula0 = new ComponentTemplateAbilitySD();
            SizeFormula0.Name = "Engine Size";
            SizeFormula0.Description = "Size of this engine in Tons";
            SizeFormula0.GuiHint = GuiHint.GuiSelectionMaxMin;
            SizeFormula0.AbilityFormula = "250";
            SizeFormula0.MaxFormula = "2500";
            SizeFormula0.MinFormula = "1";
            SizeFormula0.StepFormula = "1";
            component.ComponentAbilitySDs.Add(SizeFormula0);

            ComponentTemplateAbilitySD engineTypeAbility1 = new ComponentTemplateAbilitySD();
            engineTypeAbility1.Name = "Engine Type";
            engineTypeAbility1.Description = "Type of engine Tech";
            engineTypeAbility1.GuiHint = GuiHint.GuiTechSelectionList;
            engineTypeAbility1.GuidDictionary = new Dictionary<object, string>
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

            ComponentTemplateAbilitySD enginePowerEfficency2 = new ComponentTemplateAbilitySD();
            enginePowerEfficency2.Name = "Engine Consumption vs Power";
            enginePowerEfficency2.Description = "More Powerfull engines are less efficent for a given size";
            enginePowerEfficency2.GuiHint = GuiHint.GuiSelectionMaxMin;
            enginePowerEfficency2.AbilityFormula = "1";
            enginePowerEfficency2.MaxFormula = "TechData('b8ef73c7-2ef0-445e-8461-1e0508958a0e')";
            enginePowerEfficency2.MinFormula = "TechData('08fa4c4b-0ddb-4b3a-9190-724d715694de')";
            enginePowerEfficency2.StepFormula = "0.1";
            component.ComponentAbilitySDs.Add(enginePowerEfficency2);

            ComponentTemplateAbilitySD enginePowerAbility3 = new ComponentTemplateAbilitySD();
            enginePowerAbility3.Name = "Engine Power";
            enginePowerAbility3.Description = "Move Power for ship";
            enginePowerAbility3.GuiHint = GuiHint.GuiTextDisplay;
            enginePowerAbility3.AbilityFormula = "Ability(1) * [Mass] * Ability(2)";
            component.ComponentAbilitySDs.Add(enginePowerAbility3);

            ComponentTemplateAbilitySD enginePowerDBArgs4 = new ComponentTemplateAbilitySD();
            enginePowerDBArgs4.Name = "Engine Power";
            enginePowerDBArgs4.Description = "Move Power for ship";
            enginePowerDBArgs4.GuiHint = GuiHint.None;
            enginePowerDBArgs4.AbilityDataBlobType = typeof(EnginePowerAtbDB).ToString();
            enginePowerDBArgs4.AbilityFormula = "DataBlobArgs(Ability(3))";
            component.ComponentAbilitySDs.Add(enginePowerDBArgs4);

            ComponentTemplateAbilitySD fuelConsumptionTechMod5 = new ComponentTemplateAbilitySD();
            fuelConsumptionTechMod5.Name = "Fuel Consumption";
            fuelConsumptionTechMod5.Description = "From Tech";
            fuelConsumptionTechMod5.GuiHint = GuiHint.None;
            fuelConsumptionTechMod5.AbilityFormula = "TechData('8557acb9-c764-44e7-8ee4-db2c2cebf0bc') * Pow(Ability(2), 2.25)";
            component.ComponentAbilitySDs.Add(fuelConsumptionTechMod5);

            ComponentTemplateAbilitySD fuelConsumptionFinalCalc6 = new ComponentTemplateAbilitySD();
            fuelConsumptionFinalCalc6.Name = "Fuel Consumption";
            fuelConsumptionFinalCalc6.Description = "Fuel Consumption Calc";
            fuelConsumptionFinalCalc6.GuiHint = GuiHint.GuiTextDisplay;
            fuelConsumptionFinalCalc6.AbilityFormula = "Ability(3) - Ability(3) * [Mass] * 0.002 * Ability(5)";
            component.ComponentAbilitySDs.Add(fuelConsumptionFinalCalc6);

            ComponentTemplateAbilitySD fuelConsumptionArgsDB7 = new ComponentTemplateAbilitySD();
            fuelConsumptionArgsDB7.Name = "Fuel Consumption";
            fuelConsumptionArgsDB7.Description = "";
            fuelConsumptionArgsDB7.GuiHint = GuiHint.None;
            fuelConsumptionArgsDB7.AbilityDataBlobType = typeof(ResourceConsumptionAtbDB).ToString();
            fuelConsumptionArgsDB7.AbilityFormula = "DataBlobArgs(GuidString('33e6ac88-0235-4917-a7ff-35c8886aad3a'), Ability(6), 1)";
            component.ComponentAbilitySDs.Add(fuelConsumptionArgsDB7);

            ComponentTemplateAbilitySD thermalReduction8 = new ComponentTemplateAbilitySD();
            thermalReduction8.Name = "Thermal Signature Reduction";
            thermalReduction8.Description = "";
            thermalReduction8.GuiHint = GuiHint.GuiSelectionMaxMin;
            thermalReduction8.AbilityFormula = "0";
            thermalReduction8.MinFormula = "0";
            thermalReduction8.MaxFormula = "0.5";
            thermalReduction8.StepFormula = "0.1";
            component.ComponentAbilitySDs.Add(thermalReduction8);

            ComponentTemplateAbilitySD sensorSigDisplay9 = new ComponentTemplateAbilitySD();
            sensorSigDisplay9.Name = "Thermal Signature";
            sensorSigDisplay9.Description = "";
            sensorSigDisplay9.GuiHint = GuiHint.GuiTextDisplay;
            sensorSigDisplay9.AbilityFormula = "Ability(3) * Ability(8)";
            component.ComponentAbilitySDs.Add(sensorSigDisplay9);

            ComponentTemplateAbilitySD sensorSigDBArgs10 = new ComponentTemplateAbilitySD();
            sensorSigDBArgs10.Name = "Sensor Signature";
            sensorSigDBArgs10.Description = "";
            sensorSigDBArgs10.GuiHint = GuiHint.None;
            sensorSigDBArgs10.AbilityDataBlobType = typeof(SensorSignatureAtbDB).ToString();
            sensorSigDBArgs10.AbilityFormula = "DataBlobArgs(Ability(9),0)";
            component.ComponentAbilitySDs.Add(sensorSigDBArgs10);
            
            return component;
        }

        public static ComponentTemplateSD MineInstallation()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Mine";
            component.Description = "Mines Resources";
            component.ID = new Guid("F7084155-04C3-49E8-BF43-C7EF4BEFA550");

            component.MassFormula = "25000";

            component.VolumeFormula = "[Mass] / 2";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "50000";

            component.ResearchCostFormula = "0";
            
            component.BuildPointCostFormula = "[Mass]";

            component.MineralCostFormula = new Dictionary<Guid, string> {{new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"), "60"}, 
            {new Guid("2ae2a928-3e14-45d5-befc-5bd6ed16ec0a"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            component.ComponentAbilitySDs = new List<ComponentTemplateAbilitySD>();


            ComponentTemplateAbilitySD mineAbility = new ComponentTemplateAbilitySD();
            mineAbility.Name = "MiningAmount";
            mineAbility.Description = "";
            mineAbility.GuiHint = GuiHint.None;
            mineAbility.GuidDictionary = new Dictionary<object, string>
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
            mineAbility.AbilityDataBlobType = typeof(MineResourcesAtbDB).ToString();
            mineAbility.AbilityFormula = "DataBlobArgs([GuidDict])";
            component.ComponentAbilitySDs.Add(mineAbility);
            
            return component;

        }

        public static ComponentTemplateSD ResearchLab()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "ResearchLab";
            component.Description = "Creates Research Points";
            component.ID = new Guid("C203B7CF-8B41-4664-8291-D20DFE1119EC");

            component.MassFormula = "500000";

            component.VolumeFormula = "[Mass] / 2";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Mass]";

            component.MineralCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            component.ComponentAbilitySDs = new List<ComponentTemplateAbilitySD>();


            ComponentTemplateAbilitySD researchPointsAbility = new ComponentTemplateAbilitySD();
            researchPointsAbility.Name = "RP Amount Per EconTick";
            researchPointsAbility.Description = "";
            researchPointsAbility.GuiHint = GuiHint.None;
            researchPointsAbility.AbilityDataBlobType = typeof(ResearchPointsAtbDB).ToString();
            researchPointsAbility.AbilityFormula = "DataBlobArgs(20)";
            component.ComponentAbilitySDs.Add(researchPointsAbility);

            return component;

        }

        public static ComponentTemplateSD Refinery()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Refinery";
            component.Description = "Creates Research Points";
            component.ID = new Guid("{90592586-0BD6-4885-8526-7181E08556B5}");

            component.MassFormula = "500000";

            component.VolumeFormula = "[Mass] / 2";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Mass]";

            component.MineralCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            component.ComponentAbilitySDs = new List<ComponentTemplateAbilitySD>();
            
            ComponentTemplateAbilitySD refinePointsAbility = new ComponentTemplateAbilitySD();
            refinePointsAbility.Name = "RP Amount Per EconTick";
            refinePointsAbility.Description = "";
            refinePointsAbility.GuiHint = GuiHint.None;
            refinePointsAbility.AbilityDataBlobType = typeof(ResearchPointsAtbDB).ToString();
            refinePointsAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(refinePointsAbility);

            ComponentTemplateAbilitySD refineJobsAbility = new ComponentTemplateAbilitySD();
            refineJobsAbility.Name = "RefineAbilitys";
            refineJobsAbility.Description = "";
            refineJobsAbility.GuiHint = GuiHint.None;
            refineJobsAbility.GuidDictionary = new Dictionary<object, string>
            {
                { new Guid("33E6AC88-0235-4917-A7FF-35C8886AAD3A"),"0"},
                { new Guid("6DA93677-EE08-4853-A8A5-0F46D93FE0EB"),"0"}
            };
            refineJobsAbility.AbilityDataBlobType = typeof(RefineResourcesAtbDB).ToString();
            refineJobsAbility.AbilityFormula = "DataBlobArgs([GuidDict], Ability(0))";
            component.ComponentAbilitySDs.Add(refineJobsAbility);
            return component;
        }

        public static ComponentTemplateSD Factory()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Factory";
            component.Description = "Constructs Facilities, Fighters Ammo and Components";
            component.ID = new Guid("{07817639-E0C6-43CD-B3DC-24ED15EFB4BA}");

            component.MassFormula = "500000";

            component.VolumeFormula = "[Mass] / 2";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Mass]";

            component.MineralCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"}, 
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            component.ConstructionType = ConstructionType.Installations;

            component.ComponentAbilitySDs = new List<ComponentTemplateAbilitySD>();


            ComponentTemplateAbilitySD instalationConstructionAbility = new ComponentTemplateAbilitySD();
            instalationConstructionAbility.Name = "Instalation Construction Points";
            instalationConstructionAbility.Description = "";
            instalationConstructionAbility.GuiHint = GuiHint.GuiTextDisplay;
            instalationConstructionAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(instalationConstructionAbility);

            ComponentTemplateAbilitySD shipComponentsConstructionAbility = new ComponentTemplateAbilitySD();
            shipComponentsConstructionAbility.Name = "Component Construction Points";
            shipComponentsConstructionAbility.Description = "";
            shipComponentsConstructionAbility.GuiHint = GuiHint.GuiTextDisplay;
            shipComponentsConstructionAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(shipComponentsConstructionAbility);


            ComponentTemplateAbilitySD shipConstructionAbility = new ComponentTemplateAbilitySD();
            shipConstructionAbility.Name = "Ship Construction Points";
            shipConstructionAbility.Description = "";
            shipConstructionAbility.GuiHint = GuiHint.GuiTextDisplay;
            shipConstructionAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(shipConstructionAbility);

            ComponentTemplateAbilitySD fighterConstructionAbility = new ComponentTemplateAbilitySD();
            fighterConstructionAbility.Name = "Fighter Construction Points";
            fighterConstructionAbility.Description = "";
            fighterConstructionAbility.GuiHint = GuiHint.GuiTextDisplay;
            fighterConstructionAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(fighterConstructionAbility);

            ComponentTemplateAbilitySD ammoConstructionAbility = new ComponentTemplateAbilitySD();
            ammoConstructionAbility.Name = "Ordnance Construction Points";
            ammoConstructionAbility.Description = "";
            ammoConstructionAbility.GuiHint = GuiHint.GuiTextDisplay;
            ammoConstructionAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(ammoConstructionAbility);

            ComponentTemplateAbilitySD atbconstructor = new ComponentTemplateAbilitySD();
            atbconstructor.Name = "Construction Points";
            atbconstructor.Description = "";
            atbconstructor.GuiHint = GuiHint.None;
            atbconstructor.GuidDictionary = new Dictionary<object, string>() {
                { "Installations", "Ability(0)" },
                { "ShipComponents", "Ability(1)" },
                { "Ships", "Ability(2)" },
                { "Fighters", "Ability(3)" },
                { "Ordnance", "Ability(4)" }
            };
            atbconstructor.AbilityDataBlobType = typeof(ConstructionAtbDB).ToString();
            atbconstructor.AbilityFormula = "DataBlobArgs(EnumDict('Pulsar4X.ECSLib.ConstructionType'))";
            component.ComponentAbilitySDs.Add(atbconstructor);

            return component;
        }

        public static ComponentTemplateSD GeneralCargo()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "General Storage";
            component.Description = "Stores General Cargo";
            component.ID = new Guid("{30CD60F8-1DE3-4FAA-ACBA-0933EB84C199}");

            component.MassFormula = "500000";

            component.VolumeFormula = "[Mass] / 100";

            component.HTKFormula = "[Mass]";

            component.CrewReqFormula = "1000000";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Mass]";

            component.MineralCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "60"},
            {new Guid("c3bcb597-a2d1-4b12-9349-26586c8a921c"), "60"}};

            component.CreditCostFormula = "120";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;
            component.ConstructionType = ConstructionType.Installations | ConstructionType.ShipComponents;
            component.ComponentAbilitySDs = new List<ComponentTemplateAbilitySD>();

            ComponentTemplateAbilitySD genralCargoAbility = new ComponentTemplateAbilitySD();
            genralCargoAbility.Name = "Storage Capacity";
            genralCargoAbility.Description = "";
            genralCargoAbility.GuiHint = GuiHint.None;
            genralCargoAbility.AbilityFormula = "100";
            component.ComponentAbilitySDs.Add(genralCargoAbility);

            ComponentTemplateAbilitySD generalCargoCapacityAbility = new ComponentTemplateAbilitySD();
            generalCargoCapacityAbility.Name = "Construction Points";
            generalCargoCapacityAbility.Description = "";
            generalCargoCapacityAbility.GuiHint = GuiHint.None;
            generalCargoCapacityAbility.AbilityDataBlobType = typeof(CargoStorageAtbDB).ToString();
            generalCargoCapacityAbility.AbilityFormula = "DataBlobArgs(Ability(0), GuidString('16b4c4f0-7292-4f4d-8fea-22103c70b288'))";
            component.ComponentAbilitySDs.Add(generalCargoCapacityAbility);

            return component;
        }

    }
}
