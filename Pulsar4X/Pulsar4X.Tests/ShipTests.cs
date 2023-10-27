using NUnit.Framework;
using Pulsar4X.Atb;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Modding;
using Pulsar4X.Orbital;
using System;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Ship Entity Tests")]
    internal class ShipEntityTests
    {
        private Game _game;
        // private EntityManager _entityManager;
        private Entity _faction;
        private StarSystem _starSystem;
        private ShipDesign _shipDesign;
        private Entity _ship;
        private ComponentDesign _engineComponentDesign;
        // private ComponentTemplateSD _engineSD;
        private Entity _sol;

        [SetUp]
        public void Init()
        {
            var _modLoader = new ModLoader();
            var _modDataStore = new ModDataStore();

            _modLoader.LoadModManifest("Data/basemod/modInfo.json", _modDataStore);

            var _settings = new NewGameSettings() {
                MaxSystems = 1
            };

            _game  = new Game(_settings, _modDataStore);
            _faction = FactionFactory.CreateFaction(_game, "Terran");
            // _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e"), 3);
            // _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de"), 3);
            // _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc"), 5);
            // _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"), 1);
            // _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"), 1); //Nuclear Thermal Engine Technology
            // _faction.GetDataBlob<FactionTechDB>().ResearchedTechs.Add(new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"), 1);
            _starSystem = new StarSystem();
            _starSystem.Initialize(_game);
            _sol = TestingUtilities.BasicSol(_starSystem);
            /////Ship Class/////




        }


        [Test]
        public void TestShipCreation()
        {
            //_engineSD = NameLookup.GetTemplateSD(_game, "Alcubierre Warp Drive");
            //ComponentDesigner engineDesigner = DefaultStartFactory.DefaultEngineDesign(_game, _faction);
            //engineDesigner = new ComponentDesigner(_engineSD, _faction.GetDataBlob<FactionTechDB>());
            //engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(5); //size = 25 power.
            //_engineComponentDesign = engineDesigner.CreateDesign(_faction);
            var factionDataStore = _faction.GetDataBlob<FactionInfoDB>().Data;

            _engineComponentDesign = DefaultStartFactory.DefaultWarpDesign(_game, _faction, factionDataStore);

            _shipDesign = DefaultStartFactory.DefaultShipDesign(_game, _faction, factionDataStore);
            _ship = ShipFactory.CreateShip(_shipDesign, _faction, _sol, "Testship");

            ComponentInstancesDB instancesdb = _ship.GetDataBlob<ComponentInstancesDB>();
            instancesdb.TryGetComponentsByAttribute<WarpDriveAtb>(out var instances1);
            int originalEngineNumber = instances1.Count;

            WarpAbilityDB warpAbility = _ship.GetDataBlob<WarpAbilityDB>();

            MassVolumeDB mvdb = _ship.GetDataBlob<MassVolumeDB>();
            WarpDriveAtb warpAtb = _engineComponentDesign.GetAttribute<WarpDriveAtb>();
            double warpPower = warpAtb.WarpPower;
            Assert.AreEqual(warpPower * originalEngineNumber , warpAbility.TotalWarpPower, "Incorrect TotalEnginePower");
            double tonnage1 = mvdb.MassTotal;
            int expectedSpeed1 = ShipMovementProcessor.MaxSpeedCalc(warpAbility.TotalWarpPower, tonnage1);
            Assert.AreEqual(expectedSpeed1, warpAbility.MaxSpeed, "Incorrect Max Speed");

            _ship.AddComponent(_engineComponentDesign);
            instancesdb.TryGetComponentsByAttribute<WarpDriveAtb>(out var instances2);
            int add2engineNumber = instances2.Count;
            Assert.AreEqual(originalEngineNumber + 1, add2engineNumber);


            Assert.AreEqual(warpPower * add2engineNumber, warpAbility.TotalWarpPower, "Incorrect TotalEnginePower 2nd engine added");
            double tonnage2 = mvdb.MassTotal;
            int expectedSpeed2 = ShipMovementProcessor.MaxSpeedCalc(warpAbility.TotalWarpPower, tonnage2);
            Assert.AreEqual(expectedSpeed2, warpAbility.MaxSpeed, "Incorrect Max Speed 2nd engine");


            var energydb = _ship.GetDataBlob<EnergyGenAbilityDB>();
            var energyMax = energydb.EnergyStoreMax[energydb.EnergyType.UniqueID];
            energydb.EnergyStored[energydb.EnergyType.UniqueID] = energyMax;

            Assert.IsTrue(energyMax >= warpAbility.BubbleCreationCost, "Ship does not store enough energy for a succesfull warp bubble creation");

            Assert.AreEqual(warpAbility.CurrentVectorMS.Length(), 0);

            var posDB = _ship.GetDataBlob<PositionDB>();
            var ralpos = posDB.RelativePosition;
            var targetPos = new Vector3(ralpos.X , ralpos.Y, ralpos.Z);
            targetPos.X += expectedSpeed2 * 60 * 60; //distance for an hours travel.
            WarpMoveCommand.CreateCommand(
                factionDataStore.CargoGoods,
                _faction.Id,
                _ship,
                _sol,
                targetPos,
                _ship.StarSysDateTime,
                new Vector3(0,0,0), tonnage1);

            Assert.AreEqual(warpAbility.CurrentVectorMS.Length(), expectedSpeed2, 1.0E-15);
            // _game.GamePulse.Ticklength = TimeSpan.FromSeconds(1);
            //_game.GamePulse.TimeStep();
            _game.ProcessorManager.GetProcessor<WarpMovingDB>().ProcessEntity(_ship, 1);
            var ralposNow = posDB.RelativePosition;
            var distance = Math.Abs((ralpos - ralposNow).Length());

            Assert.AreEqual(distance, expectedSpeed2, 1.0E-15);

        }
  }
}
