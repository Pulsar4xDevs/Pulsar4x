using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Orbital;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Atb;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine
{
    public static class DefaultStartFactory
    {
        private static ComponentDesign _merlin;
        private static ComponentDesign _f1;
        private static ComponentDesign _raptor;
        private static ComponentDesign _rs25;
        private static ComponentDesign _warpDrive;
        private static ComponentDesign _largeWarpDrive;
        private static ComponentDesign _fuelTank_1000;
        private static ComponentDesign _fuelTank_2500;
        private static ComponentDesign _fuelTank_3000;
        private static ComponentDesign _laser;
        private static ComponentDesign _payload;
        private static ComponentDesign _missileSRB;
        private static ComponentDesign _missileSuite;
        private static ComponentDesign _sensor_50;
        private static ComponentDesign _sensorInstalation;
        private static ComponentDesign _fireControl;
        private static ComponentDesign _cargoInstalation;
        private static ComponentDesign _reactor;
        private static ComponentDesign _battery;
        private static ComponentDesign _cargoHold;
        private static ComponentDesign _cargoCompartment;
        private static ComponentDesign _shipYard;
        private static ComponentDesign _logiOffice;
        private static ComponentDesign _missileTube;
        private static ComponentDesign _ordnanceStore;
        private static ShipDesign _defaultShipDesign;
        private static ShipDesign _gunshipDesign;
        private static ShipDesign _spaceXStarShipDesign;
        private static OrdnanceDesign _missile;


        // this code is a test for multiple systems, worth mentioning it utterly failed, modularity is good when you have it huh.ç
        //TODO: try further tests at smaller distances between systems, create own starSystemFactory function for testing.
        private static Entity completeTest(Game game, string name){
            //var log = StaticRefLib.EventLog;
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem solSys = starfac.CreateSol(game);
            //sol.ManagerSubpulses.Init(sol);

            var earth = solSys.GetAllEntitiesWithDataBlob<NameDB>().Where(e => e.GetDataBlob<NameDB>().DefaultName.Equals("Earth")).First();
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);
            Entity factionEntity = FactionFactory.CreateFaction(game, name);
            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);
            FactionDataStore factionDataStore = factionEntity.GetDataBlob<FactionInfoDB>().Data;

            var namedEntites = solSys.GetAllEntitiesWithDataBlob<NameDB>();
            foreach (var entity in namedEntites)
            {
                var nameDB = entity.GetDataBlob<NameDB>();
                nameDB.SetName(factionEntity.Id, nameDB.DefaultName);
            }

            //once per game init stuff
            DefaultThrusterDesign(game, factionEntity, factionDataStore);
            DefaultWarpDesign(game, factionEntity, factionDataStore);
            DefaultFuelTank(game, factionEntity, factionDataStore);
            DefaultCargoInstallation(game, factionEntity, factionDataStore);
            DefaultSimpleLaser(game, factionEntity, factionDataStore);
            DefaultBFC(game, factionEntity, factionDataStore);
            ShipDefaultCargoHold(game, factionEntity, factionDataStore);
            ShipSmallCargo(game, factionEntity, factionDataStore);
            ShipPassiveSensor(game, factionEntity, factionDataStore);
            FacPassiveSensor(game, factionEntity, factionDataStore);
            DefaultFisionReactor(game, factionEntity, factionDataStore);
            DefaultBatteryBank(game, factionEntity, factionDataStore);
            DefaultFragPayload(game, factionEntity, factionDataStore);
            DefaultMissileSRB(game, factionEntity, factionDataStore);
            DefaultMissileSensors(game, factionEntity, factionDataStore);
            Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth);
            colonyEntity.AddComponent(_sensorInstalation);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);


            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(solSys.Guid);

            factionEntity.GetDataBlob<NameDB>().SetName(factionEntity.Id, "UEF");




            var entitiesWithSensors = solSys.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
            foreach (var entityItem in entitiesWithSensors)
            {
                game.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem, game.TimePulse.GameGlobalDateTime);
            }


            StarSystemFactory starfac2 = new StarSystemFactory(game);
            StarSystem solSys2 = starfac2.CreateTestSystem(game);



            solSys2.NameDB = new NameDB("other system");
            Entity solStar = solSys2.GetAllEntitiesWithDataBlob<StarInfoDB>().First();
            Entity earth2 = solSys2.GetAllEntitiesWithDataBlob<NameDB>().Where(e => e.GetDataBlob<NameDB>().DefaultName.Equals("Earth")).First();


            //sol.ManagerSubpulses.Init(sol);
            //Entity earth2 = solSys2.Entities[3]; //should be fourth entity created
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);

            var namedEntites2 = solSys2.GetAllEntitiesWithDataBlob<NameDB>();
            foreach (var entity in namedEntites2)
            {
                var nameDB = entity.GetDataBlob<NameDB>();
                nameDB.SetName(factionEntity.Id, nameDB.DefaultName);
            }

            Entity colonyEntity2 = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth2);
            colonyEntity2.AddComponent(_sensorInstalation);
            ReCalcProcessor.ReCalcAbilities(colonyEntity2);

            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(solSys2.Guid);

            var entitiesWithSensors2 = solSys2.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
            foreach (var entityItem in entitiesWithSensors2)
            {
                game.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem, game.TimePulse.GameGlobalDateTime);
            }

            var JPSurveyPoint1 = solSys.GetAllEntitiesWithDataBlob<JPSurveyableDB>()[0];
            JPSurveyPoint1.GetDataBlob<JPSurveyableDB>().SystemToGuid = solSys2.Guid;
            var JPSurveyPoint2 = solSys2.GetAllEntitiesWithDataBlob<JPSurveyableDB>()[0];
            JPSurveyPoint2.GetDataBlob<JPSurveyableDB>().SystemToGuid = solSys.Guid;
            JPSurveyPoint1.GetDataBlob<JPSurveyableDB>().JumpPointTo = JPSurveyPoint2;
            JPSurveyPoint2.GetDataBlob<JPSurveyableDB>().JumpPointTo = JPSurveyPoint1;

            return factionEntity;
        }

        public static Entity DefaultHumans(Game game, string name)
        {

            ComponentDesigner.StartResearched = true;//any components we design should be researched already. turn this off at the end.

            //var log = StaticRefLib.EventLog;
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem solSys = starfac.CreateSol(game);
            //sol.ManagerSubpulses.Init(sol);
            Entity solStar = solSys.GetAllEntitiesWithDataBlob<StarInfoDB>().First();
            Entity earth = NameLookup.GetFirstEntityWithName(solSys, "Earth"); //should be fourth entity created
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);
            Entity factionEntity = FactionFactory.CreateFaction(game, name);
            FactionDataStore factionDataStore = factionEntity.GetDataBlob<FactionInfoDB>().Data;

            Entity targetFaction = FactionFactory.CreateFaction(game, "OpFor");
            FactionDataStore opForDataStore = targetFaction.GetDataBlob<FactionInfoDB>().Data;

            // Set the faction entity to own itself so it can issue orders to itself
            factionEntity.FactionOwnerID = factionEntity.Id;

            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);

            var namedEntites = solSys.GetAllEntitiesWithDataBlob<NameDB>();
            foreach (var entity in namedEntites)
            {
                var nameDB = entity.GetDataBlob<NameDB>();
                nameDB.SetName(factionEntity.Id, nameDB.DefaultName);
            }

            long baseInitialPopulation = 9000000000;
            long variablePopulation = (long)(baseInitialPopulation * .1);
            long initialPopulationOfEarth = game.RNG.NextInt64(baseInitialPopulation - variablePopulation, baseInitialPopulation + variablePopulation);

            Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth, initialPopulationOfEarth);
            Entity marsColony = ColonyFactory.CreateColony(factionEntity, speciesEntity, NameLookup.GetFirstEntityWithName(solSys, "Mars"));

            ComponentTemplateBlueprint mineSD = game.StartingGameData.ComponentTemplates["mine"];
            ComponentDesigner mineDesigner = new ComponentDesigner(mineSD, factionDataStore, factionEntity.GetDataBlob<FactionTechDB>());
            ComponentDesign mineDesign = mineDesigner.CreateDesign(factionEntity);

            ComponentTemplateBlueprint RefinerySD = game.StartingGameData.ComponentTemplates["refinery"];
            ComponentDesigner refineryDesigner = new ComponentDesigner(RefinerySD, factionDataStore, factionEntity.GetDataBlob<FactionTechDB>());
            ComponentDesign refinaryDesign = refineryDesigner.CreateDesign(factionEntity);

            ComponentTemplateBlueprint labSD = game.StartingGameData.ComponentTemplates["university"];
            ComponentDesigner labDesigner = new ComponentDesigner(labSD, factionDataStore, factionEntity.GetDataBlob<FactionTechDB>());
            ComponentDesign labEntity = labDesigner.CreateDesign(factionEntity);

            ComponentTemplateBlueprint facSD = game.StartingGameData.ComponentTemplates["factory"];
            ComponentDesigner facDesigner = new ComponentDesigner(facSD, factionDataStore, factionEntity.GetDataBlob<FactionTechDB>());
            ComponentDesign facEntity = facDesigner.CreateDesign(factionEntity);

            Scientist scientistEntity = CommanderFactory.CreateScientist(factionEntity, colonyEntity);
            colonyEntity.GetDataBlob<TeamsHousedDB>().AddTeam(scientistEntity);

            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            //TechProcessor.ApplyTech(factionTech, factionDataStore.Techs[new ID("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c")]); //add conventional engine for testing.
            //ResearchProcessor.CheckRequrements(factionTech);

            DefaultThrusterDesign(game, factionEntity, factionDataStore);
            F1ThrusterDesign(game, factionEntity, factionDataStore);
            RaptorThrusterDesign(game, factionEntity, factionDataStore);
            RS25ThrusterDesign(game, factionEntity, factionDataStore);
            DefaultWarpDesign(game, factionEntity, factionDataStore);
            DefaultFuelTank(game, factionEntity, factionDataStore);
            LargeFuelTank(game, factionEntity, factionDataStore);
            DefaultCargoInstallation(game, factionEntity, factionDataStore);
            DefaultSimpleLaser(game, factionEntity, factionDataStore);
            DefaultBFC(game, factionEntity, factionDataStore);
            ShipDefaultCargoHold(game, factionEntity, factionDataStore);
            ShipSmallCargo(game, factionEntity, factionDataStore);
            ShipPassiveSensor(game, factionEntity, factionDataStore);
            FacPassiveSensor(game, factionEntity, factionDataStore);
            DefaultFisionReactor(game, factionEntity, factionDataStore);
            DefaultBatteryBank(game, factionEntity, factionDataStore);
            DefaultFragPayload(game, factionEntity, factionDataStore);
            DefaultMissileSRB(game, factionEntity, factionDataStore);
            DefaultMissileSensors(game, factionEntity, factionDataStore);
            DefaultMissileTube(game, factionEntity, factionDataStore);
            MissileDesign250(game, factionEntity);
            ShipSmallOrdnanceStore(game, factionEntity, factionDataStore);

            colonyEntity.AddComponent(mineDesign);
            colonyEntity.AddComponent(refinaryDesign);
            colonyEntity.AddComponent(labEntity);
            colonyEntity.AddComponent(facEntity);
            colonyEntity.AddComponent(_fuelTank_1000);
            colonyEntity.AddComponent(_cargoInstalation);
            colonyEntity.AddComponent(_sensorInstalation);
            colonyEntity.AddComponent(ShipYard(factionEntity, factionDataStore));
            colonyEntity.AddComponent(LogisticsOffice(factionEntity, factionDataStore));
            colonyEntity.AddComponent(_ordnanceStore, 10);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);

            marsColony.AddComponent(_cargoInstalation);
            marsColony.AddComponent(LogisticsOffice(factionEntity, factionDataStore));
            ReCalcProcessor.ReCalcAbilities(marsColony);

            var earthCargo = colonyEntity.GetDataBlob<VolumeStorageDB>();

            //colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            var rawSorium = factionDataStore.CargoGoods["sorium"];

            var iron = factionDataStore.CargoGoods["iron"];
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, iron, 500000);

            var hydrocarbon = factionDataStore.CargoGoods["hydrocarbons"];
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, hydrocarbon, 5000);

            var stainless = factionDataStore.CargoGoods["stainless-steel"];
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, stainless, 100000);

            var chromium = factionDataStore.CargoGoods["chromium"];
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, chromium, 50000);

            var fisiles = factionDataStore.CargoGoods["fissionables"];
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, fisiles, 50000);

            var copper = factionDataStore.CargoGoods["copper"];
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, copper, 50000);

            CargoTransferProcessor.AddCargoItems(colonyEntity, _missile, 100);
            CargoTransferProcessor.AddCargoItems(colonyEntity, _merlin, 5);
            LogiBaseDB earthlogiBase = colonyEntity.GetDataBlob<LogiBaseDB>();
            earthlogiBase.ListedItems.Add(iron, (1000, 1));
            colonyEntity.SetDataBlob(earthlogiBase);

            LogiBaseDB marslogiBase = marsColony.GetDataBlob<LogiBaseDB>();
            marslogiBase.ListedItems.Add(iron, (-1000, 1));
            marsColony.SetDataBlob(marslogiBase);

            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(solSys.Guid);

            //test systems
            //factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateEccTest(game).ID);
            //factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateLongitudeTest(game).ID);


            factionEntity.GetDataBlob<NameDB>().SetName(factionEntity.Id, "UEF");

            var fleetName = NameFactory.GetFleetName(game);
            Entity defaultFleet = FleetFactory.Create(earth.Manager, factionEntity.Id, fleetName);
            defaultFleet.GetDataBlob<FleetDB>().SetParent(factionEntity);

            // Todo: handle this in CreateShip
            ShipDesign shipDesign = DefaultShipDesign(game, factionEntity, factionDataStore);
            ShipDesign gunShipDesign = GunShipDesign(game, factionEntity, factionDataStore);
            ShipDesign pexDesign = CargoShipDesign(game, factionEntity, factionDataStore);
            ShipDesign courierDesign = CargoCourierDesign(game, factionEntity, factionDataStore);

            Entity gunShip0 = ShipFactory.CreateShip(gunShipDesign, factionEntity, earth,  "Serial Peacemaker");
            Entity ship2 = ShipFactory.CreateShip(shipDesign, factionEntity, earth,  "Ensuing Calm");
            Entity ship3 = ShipFactory.CreateShip(shipDesign, factionEntity, earth,  "Touch-and-Go");
            Entity gunShip1 = ShipFactory.CreateShip(gunShipDesign, factionEntity, earth,  "Prevailing Stillness");
            Entity courier = ShipFactory.CreateShip(pexDesign, factionEntity, earth, Math.PI, "Old Bessie");
            Entity courier2 = ShipFactory.CreateShip(pexDesign, factionEntity, earth, 0, "PE2");
            Entity starship = ShipFactory.CreateShip(SpaceXStarShip(game, factionEntity, factionDataStore), factionEntity, earth,  "Starship");
            var fuel = factionDataStore.CargoGoods["sorium-fuel"];
            var rp1 = factionDataStore.CargoGoods["rp-1"];
            var methalox = factionDataStore.CargoGoods["methalox"];
            var hydrolox = factionDataStore.CargoGoods["hydrolox"];

            for(int i = 0; i < 7; i++)
            {
                var commanderDB = CommanderFactory.CreateShipCaptain(game);
                commanderDB.CommissionedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365.25 * 10);
                commanderDB.RankedOn = game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365);
                var entity = CommanderFactory.Create(earth.Manager, factionEntity.Id, commanderDB);

                if(i == 0) gunShip0.GetDataBlob<ShipInfoDB>().CommanderID = entity.Id;
                if(i == 1) ship2.GetDataBlob<ShipInfoDB>().CommanderID = entity.Id;
                if(i == 2) ship3.GetDataBlob<ShipInfoDB>().CommanderID = entity.Id;
                if(i == 3) gunShip1.GetDataBlob<ShipInfoDB>().CommanderID = entity.Id;
                if(i == 4) courier.GetDataBlob<ShipInfoDB>().CommanderID = entity.Id;
                if(i == 5) courier2.GetDataBlob<ShipInfoDB>().CommanderID = entity.Id;
                if(i == 6) starship.GetDataBlob<ShipInfoDB>().CommanderID = entity.Id;
            }

            var fleetDB = defaultFleet.GetDataBlob<FleetDB>();
            fleetDB.AddChild(gunShip0);
            fleetDB.AddChild(ship2);
            fleetDB.AddChild(ship3);
            fleetDB.AddChild(gunShip1);
            fleetDB.AddChild(courier);
            fleetDB.AddChild(courier2);
            fleetDB.AddChild(starship);
            fleetDB.FlagShipID = starship.Id;

            // This can be removed, only for testing orders without having to set them up in game
            ConditionItem conditionItem = new ConditionItem(new FuelCondition(30f, ComparisonType.GreaterThan));
            CompoundCondition compoundCondition = new CompoundCondition(conditionItem);
            SafeList<EntityCommand> actions = new SafeList<EntityCommand>();
            actions.Add(MoveToNearestColonyAction.CreateCommand(factionEntity.Id, defaultFleet));
            var conditionalOrder = new ConditionalOrder(compoundCondition, actions);
            conditionalOrder.Name = "Test";

            fleetDB.StandingOrders.Add(conditionalOrder);

            CargoTransferProcessor.AddCargoItems(colonyEntity, rp1, 10000);
            CargoTransferProcessor.AddCargoItems(colonyEntity, methalox, 10000);
            CargoTransferProcessor.AddCargoItems(colonyEntity, hydrolox, 10000);
            CargoTransferProcessor.AddCargoItems(colonyEntity, rp1, 10000);


            CargoTransferProcessor.AddRemoveCargoVolume(gunShip0, rp1, 2000);
            CargoTransferProcessor.AddRemoveCargoVolume(gunShip1, rp1, 2000);
            CargoTransferProcessor.AddRemoveCargoVolume(ship2, rp1, 2000);
            CargoTransferProcessor.AddRemoveCargoVolume(ship3, rp1, 2000);
            CargoTransferProcessor.AddRemoveCargoVolume(courier, hydrolox, 50000);
            CargoTransferProcessor.AddRemoveCargoVolume(courier2, hydrolox, 50000);
            CargoTransferProcessor.AddRemoveCargoMass(starship, methalox, 1200000);

            CargoTransferProcessor.AddCargoItems(gunShip0, _missile, 20);
            CargoTransferProcessor.AddCargoItems(gunShip1, _missile, 20);
            //gunShip0.GetDataBlob<VolumeStorageDB>().AddCargoByUnit(MissileDesign250(game, factionEntity), 20);
            //gunShip1.GetDataBlob<VolumeStorageDB>().AddCargoByUnit(MissileDesign250(game, factionEntity), 20);

            var elec = factionDataStore.CargoGoods["electricity"];
            gunShip0.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            gunShip1.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            ship2.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            ship3.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            courier.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            courier2.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;


            Entity targetDrone0 = ShipFactory.CreateShip(TargetDrone(game, targetFaction, factionDataStore), targetFaction, earth, (10 * Math.PI / 180), "Target Drone0");
            Entity targetDrone1 = ShipFactory.CreateShip(TargetDrone(game, targetFaction, factionDataStore), targetFaction, earth, (22.5 * Math.PI / 180), "Target Drone1");
            Entity targetDrone2 = ShipFactory.CreateShip(TargetDrone(game, targetFaction, factionDataStore), targetFaction, earth, (45 * Math.PI / 180), "Target Drone2");
            targetDrone0.GetDataBlob<NameDB>().SetName(factionEntity.Id, "TargetDrone0");
            targetDrone1.GetDataBlob<NameDB>().SetName(factionEntity.Id, "TargetDrone1");
            targetDrone2.GetDataBlob<NameDB>().SetName(factionEntity.Id, "TargetDrone2");

            CargoTransferProcessor.AddRemoveCargoVolume(targetDrone1, rp1, 1000);
            CargoTransferProcessor.AddRemoveCargoVolume(targetDrone2, rp1, 1000);


            NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(gunShip0);
            NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(ship2);
            NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(ship3);
            NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(gunShip1);
            NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(courier);
            NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(courier2);
            NewtonionMovementProcessor.UpdateNewtonThrustAbilityDB(starship);

            double test_a = 0.5; //AU
            double test_e = 0;
            double test_i = 0;      //°
            double test_loan = 0;   //°
            double test_aop = 0;    //°
            double test_M0 = 0;     //°
            double test_bodyMass = ship2.GetDataBlob<MassVolumeDB>().MassDry;
            OrbitDB testOrbtdb_ship2 = OrbitDB.FromAsteroidFormat(solStar, solStar.GetDataBlob<MassVolumeDB>().MassDry, test_bodyMass, test_a, test_e, test_i, test_loan, test_aop, test_M0, game.TimePulse.GameGlobalDateTime);
            ship2.RemoveDataBlob<OrbitDB>();
            ship2.SetDataBlob(testOrbtdb_ship2);
            ship2.GetDataBlob<PositionDB>().SetParent(solStar);
            game.ProcessorManager.RunProcessOnEntity<OrbitDB>(ship2, 0);

            test_a = 0.51;
            test_i = 180;
            test_aop = 0;
            OrbitDB testOrbtdb_ship3 = OrbitDB.FromAsteroidFormat(solStar, solStar.GetDataBlob<MassVolumeDB>().MassDry, test_bodyMass, test_a, test_e, test_i, test_loan, test_aop, test_M0, game.TimePulse.GameGlobalDateTime);
            ship3.RemoveDataBlob<OrbitDB>();
            ship3.SetDataBlob(testOrbtdb_ship3);
            ship3.GetDataBlob<PositionDB>().SetParent(solStar);
            game.ProcessorManager.RunProcessOnEntity<OrbitDB>(ship3, 0);


            var pos = Distance.AuToMt(new Vector3(0, 8.52699302490434E-05, 0));
            var vel = new Vector3(10000.0, 0, 0);
            var gunShip1Mass = gunShip1.GetDataBlob<MassVolumeDB>().MassTotal;
            var earthmass = earth.GetDataBlob<MassVolumeDB>().MassTotal;
            
            //give the gunship a hypobolic orbit to test:
            var orbit = OrbitDB.FromVector(earth, gunShip1Mass, earthmass, pos, vel, game.TimePulse.GameGlobalDateTime);
            gunShip1.GetDataBlob<PositionDB>().RelativePosition = pos;
            gunShip1.SetDataBlob<OrbitDB>(orbit);
            var pos2 = gunShip1.GetRelativeFuturePosition(game.TimePulse.GameGlobalDateTime);
            
            // var nmdb = new NewtonMoveDB(earth, new Vector3(-10000.0, 0, 0));
            // gunShip1.SetDataBlob<NewtonMoveDB>(nmdb);



            solSys.SetDataBlob(gunShip0.Id, new TransitableDB());
            solSys.SetDataBlob(ship2.Id, new TransitableDB());
            solSys.SetDataBlob(gunShip1.Id, new TransitableDB());
            solSys.SetDataBlob(courier.Id, new TransitableDB());
            solSys.SetDataBlob(courier2.Id, new TransitableDB());

            //Entity ship = ShipFactory.CreateShip(shipDesign, sol.SystemManager, factionEntity, position, sol, "Serial Peacemaker");
            //ship.SetDataBlob(earth.GetDataBlob<PositionDB>()); //first ship reference PositionDB

            //Entity ship3 = ShipFactory.CreateShip(shipDesign, sol.SystemManager, factionEntity, position, sol, "Contiual Pacifier");
            //ship3.SetDataBlob((OrbitDB)earth.GetDataBlob<OrbitDB>().Clone());//second ship clone earth OrbitDB


            //sol.SystemManager.SetDataBlob(ship.ID, new TransitableDB());

            //Entity rock = AsteroidFactory.CreateAsteroid2(sol, earth, game.CurrentDateTime + TimeSpan.FromDays(365));
            Entity rock = AsteroidFactory.CreateAsteroid(solSys, earth, game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(365));


            var pow = solSys.GetAllEntitiesWithDataBlob<EnergyGenAbilityDB>();
            foreach (var entityItem in pow)
            {
                game.ProcessorManager.GetInstanceProcessor(nameof(EnergyGenProcessor)).ProcessEntity(entityItem,  game.TimePulse.GameGlobalDateTime);

            }

            var entitiesWithSensors = solSys.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
            foreach (var entityItem in entitiesWithSensors)
            {
                game.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem,  game.TimePulse.GameGlobalDateTime);
            }

            ComponentDesigner.StartResearched = false;
            return factionEntity;
        }


        public static ShipDesign DefaultShipDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_defaultShipDesign != null)
                return _defaultShipDesign;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (ShipPassiveSensor(game, faction, factionDataStore), 1),
                (DefaultSimpleLaser(game, faction, factionDataStore), 2),
                (DefaultBFC(game, faction, factionDataStore), 1),
                (ShipSmallCargo(game, faction, factionDataStore), 1),
                (DefaultFuelTank(game, faction, factionDataStore), 2),
                (DefaultWarpDesign(game, faction, factionDataStore), 4),
                (DefaultBatteryBank(game, faction, factionDataStore), 3),
                (DefaultFisionReactor(game, faction, factionDataStore), 1),
                (DefaultThrusterDesign(game, faction, factionDataStore), 3),

            };
            ArmorBlueprint plastic = factionDataStore.Armor["plastic-armor"]; //factionDataStore.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            _defaultShipDesign = new ShipDesign(factionInfo, "Ob'enn Dropship", components2, (plastic, 3));
            _defaultShipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _defaultShipDesign.Armor);
            return _defaultShipDesign;
        }

        public static ShipDesign SpaceXStarShip(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            /*
             * targetStats:
             * dry mass: 120,000 kg
             * gross mass: 1,320,000 kg
             * propellent mass: 1,200,000 kg max
             * 6.9 Dv
             * 3,700 to 3800 isp in vac
             */
            if (_spaceXStarShipDesign != null)
                return _spaceXStarShipDesign;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (ShipPassiveSensor(game, faction, factionDataStore), 1),
                (ShipSmallCargo(game, faction, factionDataStore), 1),
                (LargeFuelTank(game, faction, factionDataStore), 1),
                (RaptorThrusterDesign(game, faction, factionDataStore), 3), //3 for vac

            };
            ArmorBlueprint stainless = factionDataStore.Armor["stainless-steel-armor"];// factionDataStore.ArmorTypes[new Guid("05dce711-8846-488a-b0f3-57fd7924b268")];
            _spaceXStarShipDesign = new ShipDesign(factionInfo, "Starship", components2, (stainless, 12.75f));
            _spaceXStarShipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _spaceXStarShipDesign.Armor);
            return _spaceXStarShipDesign;
        }

        public static ShipDesign GunShipDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_gunshipDesign != null)
                return _gunshipDesign;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (ShipPassiveSensor(game, faction, factionDataStore), 1),
                (DefaultSimpleLaser(game, faction, factionDataStore), 2),
                (DefaultBFC(game, faction, factionDataStore), 1),
                (DefaultMissileTube(game, faction, factionDataStore),1),
                (ShipSmallOrdnanceStore(game, faction, factionDataStore), 2),
                (ShipSmallCargo(game,faction, factionDataStore), 1),
                (DefaultFuelTank(game, faction, factionDataStore), 2),
                (DefaultWarpDesign(game, faction, factionDataStore), 4),
                (DefaultBatteryBank(game, faction, factionDataStore), 3),
                (DefaultFisionReactor(game, faction, factionDataStore), 1),
                (DefaultThrusterDesign(game, faction, factionDataStore), 4),
            };
            ArmorBlueprint plastic = factionDataStore.Armor["plastic-armor"];
            _gunshipDesign = new ShipDesign(factionInfo, "Sanctum Adroit GunShip", components2, (plastic, 3));
            _gunshipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _gunshipDesign.Armor);
            return _gunshipDesign;
        }

        public static ShipDesign TargetDrone(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (_sensor_50, 1),
                (_fuelTank_1000, 2),
                (_warpDrive, 4),
                (_battery, 3),
                (_reactor, 1),
                (_merlin, 4),
            };
            ArmorBlueprint plastic = factionDataStore.Armor["plastic-armor"];
            var shipdesign = new ShipDesign(factionInfo, "TargetDrone", components2, (plastic, 3));
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
        }

        public static ShipDesign CargoShipDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (DefaultSimpleLaser(game, faction, factionDataStore), 1),
                (DefaultBFC(game, faction, factionDataStore), 1),
                (_sensor_50, 1),
                (_fuelTank_2500, 1),
                (_fuelTank_2500, 1),
                (_cargoHold, 1),
                (_warpDrive, 4),
                (_battery, 2),
                (_reactor, 1),
                (_rs25, 4),
            };
            ArmorBlueprint plastic = factionDataStore.Armor["plastic-armor"];
            var shipdesign = new ShipDesign(factionInfo, "Planet Express Ship", components2, (plastic, 3));
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
        }

        public static ShipDesign CargoCourierDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (_sensor_50, 1),
                (VLargeFuelTank(game,faction, factionDataStore), 1),
                (ShipSmallCargo(game,faction, factionDataStore), 1),
                (LargeWarpDesign(game, faction, factionDataStore), 1),
                (_battery, 2),
                (_reactor, 1),
                (_rs25, 1),
            };
            ArmorBlueprint plastic = factionDataStore.Armor["plastic-armor"];
            var shipdesign = new ShipDesign(factionInfo, "Cargo Courier", components2, (plastic, 1));
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
        }

        public static OrdnanceDesign MissileDesign250(Game game, Entity faction)
        {
            if (_missile != null)
                return _missile;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();

            List<(ComponentDesign, int)> components = new List<(ComponentDesign, int)>()
            {
                (DefaultFragPayload(game, faction, factionInfo.Data), 1),
                (DefaultMissileSensors(game, faction, factionInfo.Data), 1),
                (DefaultMissileSRB(game, faction, factionInfo.Data), 1),
            };
            double fuelkg = 225;
            _missile = new OrdnanceDesign(factionInfo, "Missile250", fuelkg, components);
            return _missile;
        }

        public static ComponentDesign ShipYard(Entity faction, FactionDataStore factionDataStore)
        {
            if (_shipYard != null)
                return _shipYard;
            ComponentDesigner spacePortDesigner;
            ComponentTemplateBlueprint spaceportSD = factionDataStore.ComponentTemplates["shipyard"]; //StaticRefLib.StaticData.ComponentTemplates[new Guid("0BD304FF-FDEA-493C-8979-15FE86B7123E")];
            spacePortDesigner = new ComponentDesigner(spaceportSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            spacePortDesigner.Name = "Ship Yard";
            _shipYard = spacePortDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_shipYard.TechID);
            return _shipYard;
        }
        public static ComponentDesign LogisticsOffice(Entity faction, FactionDataStore factionDataStore)
        {
            if (_logiOffice != null)
                return _logiOffice;
            ComponentDesigner logofficeDesigner;
            ComponentTemplateBlueprint logofficeSD = factionDataStore.ComponentTemplates["logistics-office"];
            logofficeDesigner = new ComponentDesigner(logofficeSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            logofficeDesigner.Name = "Logistics Office";
            _logiOffice = logofficeDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_logiOffice.TechID);
            return _logiOffice;
        }
        public static ComponentDesign DefaultThrusterDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_merlin != null)
                return _merlin;

            ComponentDesigner engineDesigner;

            ComponentTemplateBlueprint engineSD = factionDataStore.ComponentTemplates["conventional-engine"];
            engineDesigner = new ComponentDesigner(engineSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(30);
            engineDesigner.Name = "Merlin";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _merlin = engineDesigner.CreateDesign(faction);

            factionDataStore.IncrementTechLevel(_merlin.TechID);
            return _merlin;
        }

        public static ComponentDesign F1ThrusterDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_f1 != null)
                return _f1;

            ComponentDesigner engineDesigner;

            ComponentTemplateBlueprint engineSD = factionDataStore.ComponentTemplates["conventional-engine"];
            engineDesigner = new ComponentDesigner(engineSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(50);
            engineDesigner.Name = "F1";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _f1 = engineDesigner.CreateDesign(faction);

            factionDataStore.IncrementTechLevel(_f1.TechID);
            return _f1;
        }

        /*
            Target Stats:
            Mass: 1500kg
            Thrust: 2.3 MN
            Isp 380s (3.73)
        */
        public static ComponentDesign RaptorThrusterDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_raptor != null)
                return _raptor;

            ComponentDesigner engineDesigner;

            ComponentTemplateBlueprint engineSD = factionDataStore.ComponentTemplates["conventional-engine"];
            engineDesigner = new ComponentDesigner(engineSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(15);
            engineDesigner.ComponentDesignAttributes["Fuel Type"].SetValueFromString("methalox");
            engineDesigner.Name = "Raptor-Vac";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _raptor = engineDesigner.CreateDesign(faction);

            factionDataStore.IncrementTechLevel(_raptor.TechID);
            return _raptor;
        }

        public static ComponentDesign RS25ThrusterDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_rs25 != null)
                return _rs25;
            ComponentDesigner engineDesigner;
            ComponentTemplateBlueprint engineSD = factionDataStore.ComponentTemplates["conventional-engine"];
            engineDesigner = new ComponentDesigner(engineSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(100);
            engineDesigner.ComponentDesignAttributes["Fuel Type"].SetValueFromString("hydrolox");
            engineDesigner.Name = "RS-25";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _rs25 = engineDesigner.CreateDesign(faction);

            factionDataStore.IncrementTechLevel(_rs25.TechID);
            return _rs25;
        }




        public static ComponentDesign DefaultWarpDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_warpDrive != null)
                return _warpDrive;

            ComponentDesigner engineDesigner;

            ComponentTemplateBlueprint engineSD = factionDataStore.ComponentTemplates["alcubierre-warp-drive"];
            engineDesigner = new ComponentDesigner(engineSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(500); //size 500 = 2500 power
            engineDesigner.Name = "Alcuberi-White 500";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _warpDrive = engineDesigner.CreateDesign(faction);

            factionDataStore.IncrementTechLevel(_warpDrive.TechID);
            return _warpDrive;
        }

        public static ComponentDesign LargeWarpDesign(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_largeWarpDrive != null)
                return _largeWarpDrive;

            ComponentDesigner engineDesigner;

            ComponentTemplateBlueprint engineSD = factionDataStore.ComponentTemplates["alcubierre-warp-drive"];
            engineDesigner = new ComponentDesigner(engineSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(2000);
            engineDesigner.Name = "Alcuberi-White 2k";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _warpDrive = engineDesigner.CreateDesign(faction);

            factionDataStore.IncrementTechLevel(_warpDrive.TechID);
            return _warpDrive;
        }

        public static ComponentDesign DefaultFuelTank(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_fuelTank_1000 != null)
                return _fuelTank_1000;
            ComponentDesigner fuelTankDesigner;
            ComponentTemplateBlueprint tankSD = factionDataStore.ComponentTemplates["stainless-steel-fuel-tank"];
            fuelTankDesigner = new ComponentDesigner(tankSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            fuelTankDesigner.ComponentDesignAttributes["Tank Volume"].SetValueFromInput(1000);
            fuelTankDesigner.Name = "Tank-1000m^3";
            _fuelTank_1000 = fuelTankDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_fuelTank_1000.TechID);
            return _fuelTank_1000;
        }

        public static ComponentDesign LargeFuelTank(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_fuelTank_2500 != null)
                return _fuelTank_2500;
            ComponentDesigner fuelTankDesigner;
            ComponentTemplateBlueprint tankSD = factionDataStore.ComponentTemplates["stainless-steel-fuel-tank"];
            fuelTankDesigner = new ComponentDesigner(tankSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            fuelTankDesigner.ComponentDesignAttributes["Tank Volume"].SetValueFromInput(1500);
            fuelTankDesigner.Name = "Tank-1500m^3";
            _fuelTank_2500 = fuelTankDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_fuelTank_2500.TechID);
            return _fuelTank_2500;
        }

        public static ComponentDesign VLargeFuelTank(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_fuelTank_3000 != null)
                return _fuelTank_3000;
            ComponentDesigner fuelTankDesigner;
            ComponentTemplateBlueprint tankSD = factionDataStore.ComponentTemplates["stainless-steel-fuel-tank"];
            fuelTankDesigner = new ComponentDesigner(tankSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            fuelTankDesigner.ComponentDesignAttributes["Tank Volume"].SetValueFromInput(3000);
            fuelTankDesigner.Name = "Tank-3000m^3";
            _fuelTank_3000 = fuelTankDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_fuelTank_3000.TechID);
            return _fuelTank_3000;
        }

        public static ComponentDesign DefaultSimpleLaser(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_laser != null)
                return _laser;
            ComponentDesigner laserDesigner;
            ComponentTemplateBlueprint laserSD = factionDataStore.ComponentTemplates["laser-gun"];
            laserDesigner = new ComponentDesigner(laserSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            laserDesigner.ComponentDesignAttributes["Range"].SetValueFromInput(100);
            laserDesigner.ComponentDesignAttributes["Damage"].SetValueFromInput(5000);
            laserDesigner.ComponentDesignAttributes["ReloadRate"].SetValueFromInput(5);

            _laser = laserDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_laser.TechID);
            return _laser;

        }
        public static ComponentDesign DefaultFragPayload(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_payload != null)
                return _payload;
            ComponentDesigner payloadDesigner;
            ComponentTemplateBlueprint payloadSD = factionDataStore.ComponentTemplates["missle-payload"];
            payloadDesigner = new ComponentDesigner(payloadSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            payloadDesigner.ComponentDesignAttributes["Trigger Type"].SetValueFromInput(2);
            payloadDesigner.ComponentDesignAttributes["Payload Type"].SetValueFromInput(0);
            payloadDesigner.ComponentDesignAttributes["Explosive Mass"].SetValueFromInput(2);
            payloadDesigner.ComponentDesignAttributes["Frag Mass"].SetValueFromInput(0.1);
            payloadDesigner.ComponentDesignAttributes["Frag Count"].SetValueFromInput(30);
            payloadDesigner.ComponentDesignAttributes["Frag Cone Angle"].SetValueFromInput(180);
            payloadDesigner.Name = "ProxFrag 5kg";
            _payload = payloadDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_payload.TechID);
            return _payload;
        }

        public static ComponentDesign DefaultMissileSensors(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_missileSuite != null)
                return _missileSuite;
            ComponentDesigner suiteDesigner;
            ComponentTemplateBlueprint srbSD = factionDataStore.ComponentTemplates["missle-electronics-suite"];
            suiteDesigner = new ComponentDesigner(srbSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            suiteDesigner.ComponentDesignAttributes["Guidance Type"].SetValueFromInput(2);
            suiteDesigner.ComponentDesignAttributes["Antenna Size"].SetValueFromInput(10);
            suiteDesigner.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(470);
            suiteDesigner.ComponentDesignAttributes["Detection Bandwidth"].SetValueFromInput(2);
            suiteDesigner.ComponentDesignAttributes["Resolution"].SetValueFromInput(1);
            suiteDesigner.Name = "Passive Yellow 1MP ";
            _missileSuite = suiteDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_missileSuite.TechID);
            return _missileSuite;
        }
        public static ComponentDesign DefaultMissileSRB(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_missileSRB != null)
                return _missileSRB;
            ComponentDesigner srbDesigner;
            ComponentTemplateBlueprint srbSD = factionDataStore.ComponentTemplates["missle-srb"];
            srbDesigner = new ComponentDesigner(srbSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            srbDesigner.ComponentDesignAttributes["Engine Mass"].SetValueFromInput(10);

            srbDesigner.Name = "SRB 235";
            _missileSRB = srbDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_missileSRB.TechID);
            return _missileSRB;
        }

        public static ComponentDesign DefaultMissileTube(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_missileTube != null)
                return _missileTube;
            ComponentDesigner tubeDesigner;
            ComponentTemplateBlueprint tubeSD = factionDataStore.ComponentTemplates["missle-launcher"];
            tubeDesigner = new ComponentDesigner(tubeSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            tubeDesigner.Name = "MissileTube 500";
            _missileTube = tubeDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_missileTube.TechID);
            return _missileTube;
        }

        public static ComponentDesign DefaultBFC(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_fireControl != null)
                return _fireControl;
            ComponentDesigner fireControlDesigner;
            ComponentTemplateBlueprint bfcSD = factionDataStore.ComponentTemplates["beam-fire-control"];
            fireControlDesigner = new ComponentDesigner(bfcSD, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            fireControlDesigner.ComponentDesignAttributes["Range"].SetValueFromInput(100);
            fireControlDesigner.ComponentDesignAttributes["Tracking Speed"].SetValueFromInput(5000);
            fireControlDesigner.ComponentDesignAttributes["Size vs Range"].SetValueFromInput(1);

            //return fireControlDesigner.CreateDesign(faction);
            _fireControl = fireControlDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_fireControl.TechID);
            return _fireControl;
        }

        public static ComponentDesign DefaultCargoInstallation(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_cargoInstalation != null)
                return _cargoInstalation;
            ComponentDesigner componentDesigner;
            ComponentTemplateBlueprint template = factionDataStore.ComponentTemplates["general-cargo-hold"];
            componentDesigner = new ComponentDesigner(template, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes["Storage Volume"].SetValueFromInput(1000000);
            componentDesigner.Name = "General Cargo Hold";
            //return cargoInstalation.CreateDesign(faction);
            _cargoInstalation = componentDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_cargoInstalation.TechID);
            return _cargoInstalation;
        }

        public static ComponentDesign DefaultFisionReactor(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_reactor != null)
                return _reactor;
            ComponentDesigner componentDesigner;
            ComponentTemplateBlueprint template = factionDataStore.ComponentTemplates["reactor"];
            componentDesigner = new ComponentDesigner(template, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(1000);
            componentDesigner.Name = "Reactor15k";
            //return cargoInstalation.CreateDesign(faction);
            _reactor = componentDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_reactor.TechID);
            return _reactor;
        }

        public static ComponentDesign DefaultBatteryBank(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_battery != null)
                return _battery;
            ComponentDesigner componentDesigner;
            ComponentTemplateBlueprint template = factionDataStore.ComponentTemplates["battery-bank"];
            componentDesigner = new ComponentDesigner(template, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(2000);
            componentDesigner.Name = "Battery2t";
            _battery = componentDesigner.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_battery.TechID);
            return _battery;
        }

        public static ComponentDesign ShipDefaultCargoHold(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_cargoHold != null)
                return _cargoHold;
            ComponentDesigner cargoComponent;
            ComponentTemplateBlueprint template = factionDataStore.ComponentTemplates["general-cargo-hold"];
            cargoComponent = new ComponentDesigner(template, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            cargoComponent.ComponentDesignAttributes["Storage Volume"].SetValueFromInput(5000); //5t component
            cargoComponent.ComponentDesignAttributes["Cargo Transfer Rate"].SetValueFromInput(500);
            cargoComponent.ComponentDesignAttributes["Transfer Range"].SetValueFromInput(100);
            cargoComponent.Name = "CargoComponent5t";
            _cargoHold = cargoComponent.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_cargoHold.TechID);
            return _cargoHold;
        }

        public static ComponentDesign ShipSmallCargo(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_cargoCompartment != null)
                return _cargoCompartment;
            ComponentDesigner cargoComponent;
            ComponentTemplateBlueprint template = factionDataStore.ComponentTemplates["general-cargo-hold"];
            cargoComponent = new ComponentDesigner(template, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            cargoComponent.ComponentDesignAttributes["Storage Volume"].SetValueFromInput(1000); //5t component
            cargoComponent.ComponentDesignAttributes["Cargo Transfer Rate"].SetValueFromInput(500);
            cargoComponent.ComponentDesignAttributes["Transfer Range"].SetValueFromInput(100);
            cargoComponent.Name = "CargoComponent1t";
            _cargoCompartment = cargoComponent.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_cargoCompartment.TechID);
            return _cargoCompartment;
        }
        public static ComponentDesign ShipSmallOrdnanceStore(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_ordnanceStore != null)
                return _ordnanceStore;
            ComponentDesigner cargoComponent;
            ComponentTemplateBlueprint template = factionDataStore.ComponentTemplates["ordnance-cargo-hold"];
            cargoComponent = new ComponentDesigner(template, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            cargoComponent.ComponentDesignAttributes["Rack Size"].SetValueFromInput(2627); //5t component
            cargoComponent.ComponentDesignAttributes["Cargo Transfer Rate"].SetValueFromInput(100);
            cargoComponent.ComponentDesignAttributes["Transfer Range"].SetValueFromInput(100);
            cargoComponent.Name = "OrdinanceRack-2.5t";
            _ordnanceStore = cargoComponent.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_ordnanceStore.TechID);
            return _ordnanceStore;
        }

        public static ComponentDesign ShipPassiveSensor(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_sensor_50 != null)
                return _sensor_50;
            ComponentDesigner sensor;
            ComponentTemplateBlueprint template = factionDataStore.ComponentTemplates["passive-sensor"];
            sensor = new ComponentDesigner(template, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            sensor.ComponentDesignAttributes["Antenna Size"].SetValueFromInput(5.5);  //size
            sensor.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(479); //best wavelength
            sensor.ComponentDesignAttributes["Detection Bandwidth"].SetValueFromInput(200); //wavelength detection width
            //sensor.ComponentDesignAttributes[3].SetValueFromInput(10);  //best detection magnatude. (Not settable)
            //[4] worst detection magnatude (not settable)
            sensor.ComponentDesignAttributes["Resolution"].SetValueFromInput(1);   //resolution
            sensor.ComponentDesignAttributes["Scan Time"].SetValueFromInput(3600);//Scan Time
            sensor.Name = "PassiveSensor-S50";
            _sensor_50 = sensor.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_sensor_50.TechID);
            return _sensor_50;

        }

        public static ComponentDesign FacPassiveSensor(Game game, Entity faction, FactionDataStore factionDataStore)
        {
            if (_sensorInstalation != null)
                return _sensorInstalation;
            ComponentDesigner sensor;
            ComponentTemplateBlueprint template = factionDataStore.ComponentTemplates["passive-sensor"];
            sensor = new ComponentDesigner(template, factionDataStore, faction.GetDataBlob<FactionTechDB>());
            sensor.ComponentDesignAttributes["Antenna Size"].SetValueFromInput(5000);  //size
            sensor.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(470); //best wavelength
            sensor.ComponentDesignAttributes["Detection Bandwidth"].SetValueFromInput(250); //wavelength detection width
            //sensor.ComponentDesignAttributes[3].SetValueFromInput(10);  //best detection magnatude. (Not settable)
            //[4] worst detection magnatude (not settable)
            sensor.ComponentDesignAttributes["Resolution"].SetValueFromInput(100);   //resolution
            sensor.ComponentDesignAttributes["Scan Time"].SetValueFromInput(3600);//Scan Time
            sensor.Name = "Passive Scanner";
            _sensorInstalation = sensor.CreateDesign(faction);
            factionDataStore.IncrementTechLevel(_sensorInstalation.TechID);
            return _sensorInstalation;

        }
    }

}