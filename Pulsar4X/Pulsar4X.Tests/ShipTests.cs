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
        private Entity _shipClass;
        private Entity _ship;
        private Entity _engineComponentDesign;
        private ComponentTemplateSD _engineSD;

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
            /////Ship Class/////
           


                
        }


        [Test]
        public void TestShipCreation()
        {

            ComponentDesign engineDesign;// = DefaultStartFactory.DefaultEngineDesign(_game, _faction);
      
            _engineSD = NameLookup.GetTemplateSD(_game, "Engine");
            engineDesign = GenericComponentFactory.StaticToDesign(_engineSD, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);
            engineDesign.ComponentDesignAttributes[0].SetValueFromInput(5); //size = 25 power.
                                    
            _engineComponentDesign = GenericComponentFactory.DesignToDesignEntity(_game, _faction, engineDesign);

            _shipClass = ShipFactory.CreateNewShipClass(_game, _faction, "Ob'enn dropship");

            Assert.True(_shipClass.FactionOwner == _faction.Guid);


            EntityManipulation.AddComponentToEntity(_shipClass, _engineComponentDesign);
            EntityManipulation.AddComponentToEntity(_shipClass, _engineComponentDesign);

            Vector4 pos = new Vector4(0, 0, 0, 0);
            int designEngineNumber = _shipClass.GetDataBlob<ComponentInstancesDB>().GetNumberOfComponentsOfDesign(_engineComponentDesign.Guid);
            Assert.AreEqual(2, designEngineNumber);
            _ship = ShipFactory.CreateShip(_shipClass, _starSystem, _faction, pos, _starSystem, "Serial Peacemaker");
            Assert.AreEqual(designEngineNumber, _ship.GetDataBlob<ComponentInstancesDB>().GetNumberOfComponentsOfDesign(_engineComponentDesign.Guid), "Number of engine components not the same as design");

            PropulsionDB propulsion = _ship.GetDataBlob<PropulsionDB>();
            ShipInfoDB shipInfo = _ship.GetDataBlob<ShipInfoDB>();

            Assert.AreEqual(50, propulsion.TotalEnginePower, "Incorrect TotalEnginePower");
            float tonnage1 = _ship.GetDataBlob<ShipInfoDB>().Tonnage;
            int expectedSpeed1 = ShipMovementProcessor.MaxSpeedCalc(propulsion.TotalEnginePower, tonnage1);
            Assert.AreEqual(expectedSpeed1, propulsion.MaximumSpeed_MS, "Incorrect Max Speed");

            EntityManipulation.AddComponentToEntity(_ship, _engineComponentDesign); //add second engine
            Assert.AreEqual(75, propulsion.TotalEnginePower, "Incorrect TotalEnginePower 2nd engine added");
            float tonnage2 = _ship.GetDataBlob<ShipInfoDB>().Tonnage;
            int expectedSpeed2 = ShipMovementProcessor.MaxSpeedCalc(propulsion.TotalEnginePower, tonnage2);
            Assert.AreEqual(expectedSpeed2, propulsion.MaximumSpeed_MS, "Incorrect Max Speed 2nd engine");


    }
  }
}
