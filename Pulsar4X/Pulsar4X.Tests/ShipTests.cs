using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Ship Entity Tests")]
    internal class ShipEntityTests
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _colonyEntity;
        private MineralSD _duraniumSD;
        private MineralSD _corundiumSD;
        private StarSystem _starSystem;
        private ShipFactory.ShipClass _shipClass;
        private Entity _ship;
        private ComponentDesign _engineComponentDesign;
        private ComponentTemplateSD _engineSD;
        private Entity _sol;

        [SetUp]
        public void Init()
        {
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = 1 });

            _faction = FactionFactory.CreateFaction(_game, "Terran");
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e"), 3);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de"), 3);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc"), 5);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"), 1);
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"), 1); //Nuclear Thermal Engine Technology
            _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"), 1);
            _starSystem = new StarSystem(_game, "Sol", -1);
            _sol = TestingUtilities.BasicSol(_starSystem);
            /////Ship Class/////




        }


        [Test]
        public void TestShipCreation()
        {

            ComponentDesigner engineDesigner;// = DefaultStartFactory.DefaultEngineDesign(_game, _faction);
      
            _engineSD = NameLookup.GetTemplateSD(_game, "Engine");
            engineDesigner = new ComponentDesigner(_engineSD, _faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Engine Size"].SetValueFromInput(5); //size = 25 power.
            
            
            
            _engineComponentDesign = engineDesigner.CreateDesign(_faction);

            
            
            _shipClass = DefaultStartFactory.DefaultShipDesign(_game, _faction);
            _ship = ShipFactory.CreateShip(_shipClass, _faction, _sol, _starSystem, "Testship");

            
            ComponentInstancesDB instancesdb = _ship.GetDataBlob<ComponentInstancesDB>();
            instancesdb.TryGetComponentsByAttribute<WarpDriveAtb>(out var instances1);
            int origionalEngineNumber = instances1.Count;
            
            EntityManipulation.AddComponentToEntity(_ship, _engineComponentDesign);
            EntityManipulation.AddComponentToEntity(_ship, _engineComponentDesign);
            
            
            instancesdb.TryGetComponentsByAttribute<WarpDriveAtb>(out var instances2);
            int add2engineNumber = instances2.Count;
            
                
            Assert.AreEqual(origionalEngineNumber + 2, add2engineNumber);

            PropulsionAbilityDB propulsion = _ship.GetDataBlob<PropulsionAbilityDB>();
            ShipInfoDB shipInfo = _ship.GetDataBlob<ShipInfoDB>();

            Assert.AreEqual(500000, propulsion.TotalEnginePower, "Incorrect TotalEnginePower");
            float tonnage1 = _ship.GetDataBlob<ShipInfoDB>().Tonnage;
            int expectedSpeed1 = ShipMovementProcessor.MaxSpeedCalc(propulsion.TotalEnginePower, tonnage1);
            Assert.AreEqual(expectedSpeed1, propulsion.MaximumSpeed_MS, "Incorrect Max Speed");

            EntityManipulation.AddComponentToEntity(_ship, _engineComponentDesign); //add second engine
            Assert.AreEqual(750000, propulsion.TotalEnginePower, "Incorrect TotalEnginePower 2nd engine added");
            float tonnage2 = _ship.GetDataBlob<ShipInfoDB>().Tonnage;
            int expectedSpeed2 = ShipMovementProcessor.MaxSpeedCalc(propulsion.TotalEnginePower, tonnage2);
            Assert.AreEqual(expectedSpeed2, propulsion.MaximumSpeed_MS, "Incorrect Max Speed 2nd engine");


    }
  }
}
