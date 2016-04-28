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
        private Entity _engineComponent;
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

            ComponentDesign engineDesign;

            _engineSD = _game.StaticData.Components[new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6")];
            engineDesign = GenericComponentFactory.StaticToDesign(_engineSD, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);
            engineDesign.ComponentDesignAbilities[0].SetValueFromInput(5); //size
            //engineDesignDB.ComponentDesignAbilities[1]
            _engineComponent = GenericComponentFactory.DesignToEntity(_game, _faction, engineDesign);

            _shipClass = ShipFactory.CreateNewShipClass(_game, _faction, "Ob'enn dropship");
            ShipFactory.AddShipComponent(_shipClass, _engineComponent);


            _ship = ShipFactory.CreateShip(_shipClass, _starSystem.SystemManager, _faction, "Serial Peacemaker");
            PropulsionDB propulsion = _ship.GetDataBlob<PropulsionDB>();
            ShipInfoDB shipInfo = _ship.GetDataBlob<ShipInfoDB>();

            Assert.True(_ship.GetDataBlob<ComponentInstancesDB>().SpecificInstances.ContainsKey(_engineComponent));
            Assert.AreEqual(100, propulsion.MaximumSpeed);
        }
    }
}
