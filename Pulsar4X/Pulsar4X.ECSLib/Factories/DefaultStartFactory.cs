using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
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
            var log = StaticRefLib.EventLog;
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem solSys = starfac.CreateSol(game);
            //sol.ManagerSubpulses.Init(sol);
            Entity earth = solSys.Entities.FirstOrDefault(x => x.GetDataBlob<NameDB>().DefaultName.Equals("Earth")); //should be fourth entity created
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);
            Entity factionEntity = FactionFactory.CreateFaction(game, name);
            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);

            var namedEntites = solSys.GetAllEntitiesWithDataBlob<NameDB>();
            foreach (var entity in namedEntites)
            {
                var nameDB = entity.GetDataBlob<NameDB>();
                nameDB.SetName(factionEntity.Guid, nameDB.DefaultName);
            }

            //once per game init stuff
            DefaultThrusterDesign(game, factionEntity);
            DefaultWarpDesign(game, factionEntity);
            DefaultFuelTank(game, factionEntity);
            DefaultCargoInstallation(game, factionEntity);
            DefaultSimpleLaser(game, factionEntity);
            DefaultBFC(game, factionEntity);
            ShipDefaultCargoHold(game, factionEntity);
            ShipSmallCargo(game, factionEntity);
            ShipPassiveSensor(game, factionEntity);
            FacPassiveSensor(game, factionEntity);
            DefaultFisionReactor(game, factionEntity);
            DefaultBatteryBank(game, factionEntity);
            DefaultFragPayload(game, factionEntity);
            DefaultMissileSRB(game, factionEntity);
            DefaultMissileSensors(game, factionEntity);
            Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth);
            colonyEntity.AddComponent(_sensorInstalation);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);


            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(solSys.Guid);

            factionEntity.GetDataBlob<NameDB>().SetName(factionEntity.Guid, "UEF");




            var entitiesWithSensors = solSys.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
            foreach (var entityItem in entitiesWithSensors)
            {
                StaticRefLib.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem, StaticRefLib.CurrentDateTime);
            }


            StarSystemFactory starfac2 = new StarSystemFactory(game);
            StarSystem solSys2 = starfac2.CreateTestSystem(game);



            solSys2.NameDB = new NameDB("other system");
            Entity solStar = solSys2.Entities[0];
            Entity earth2 = solSys2.Entities[1];


            //sol.ManagerSubpulses.Init(sol);
            //Entity earth2 = solSys2.Entities[3]; //should be fourth entity created
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);

            var namedEntites2 = solSys2.GetAllEntitiesWithDataBlob<NameDB>();
            foreach (var entity in namedEntites2)
            {
                var nameDB = entity.GetDataBlob<NameDB>();
                nameDB.SetName(factionEntity.Guid, nameDB.DefaultName);
            }

            Entity colonyEntity2 = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth2);
            colonyEntity2.AddComponent(_sensorInstalation);
            ReCalcProcessor.ReCalcAbilities(colonyEntity2);

            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(solSys2.Guid);

            var entitiesWithSensors2 = solSys2.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
            foreach (var entityItem in entitiesWithSensors2)
            {
                StaticRefLib.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem, StaticRefLib.CurrentDateTime);
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
            //USE THIS TO TEST CODE
            //TESTING STUFFF
            //return completeTest(game, name);
           // while(true){

            //}
            //TESTING STUFF

            ComponentDesigner.StartResearched = true;//any components we design should be researched already. turn this off at the end.

            var log = StaticRefLib.EventLog;
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem solSys = starfac.CreateSol(game);
            //sol.ManagerSubpulses.Init(sol);
            Entity solStar = solSys.Entities[0];
            Entity earth = NameLookup.GetFirstEntityWithName(solSys, "Earth"); //should be fourth entity created
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);
            Entity factionEntity = FactionFactory.CreateFaction(game, name);

            // Set the faction entity to own itself so it can issue orders to itself
            factionEntity.FactionOwnerID = factionEntity.Guid;

            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);

            Entity targetFaction = FactionFactory.CreateFaction(game, "OpFor");

            var namedEntites = solSys.GetAllEntitiesWithDataBlob<NameDB>();
            foreach (var entity in namedEntites)
            {
                var nameDB = entity.GetDataBlob<NameDB>();
                nameDB.SetName(factionEntity.Guid, nameDB.DefaultName);
            }

            Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth);
            Entity marsColony = ColonyFactory.CreateColony(factionEntity, speciesEntity, NameLookup.GetFirstEntityWithName(solSys, "Mars"));

            ComponentTemplateSD mineSD = game.StaticData.ComponentTemplates[new Guid("f7084155-04c3-49e8-bf43-c7ef4befa550")];
            ComponentDesigner mineDesigner = new ComponentDesigner(mineSD, factionEntity.GetDataBlob<FactionTechDB>());
            ComponentDesign mineDesign = mineDesigner.CreateDesign(factionEntity);

            ComponentTemplateSD RefinerySD = game.StaticData.ComponentTemplates[new Guid("90592586-0BD6-4885-8526-7181E08556B5")];
            ComponentDesigner refineryDesigner = new ComponentDesigner(RefinerySD, factionEntity.GetDataBlob<FactionTechDB>());
            ComponentDesign refinaryDesign = refineryDesigner.CreateDesign(factionEntity);

            ComponentTemplateSD labSD = game.StaticData.ComponentTemplates[new Guid("c203b7cf-8b41-4664-8291-d20dfe1119ec")];
            ComponentDesigner labDesigner = new ComponentDesigner(labSD, factionEntity.GetDataBlob<FactionTechDB>());
            ComponentDesign labEntity = labDesigner.CreateDesign(factionEntity);

            ComponentTemplateSD facSD = game.StaticData.ComponentTemplates[new Guid("{07817639-E0C6-43CD-B3DC-24ED15EFB4BA}")];
            ComponentDesigner facDesigner = new ComponentDesigner(facSD, factionEntity.GetDataBlob<FactionTechDB>());
            ComponentDesign facEntity = facDesigner.CreateDesign(factionEntity);

            Scientist scientistEntity = CommanderFactory.CreateScientist(factionEntity, colonyEntity);
            colonyEntity.GetDataBlob<TeamsHousedDB>().AddTeam(scientistEntity);

            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            //TechProcessor.ApplyTech(factionTech, game.StaticData.Techs[new ID("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c")]); //add conventional engine for testing.
            ResearchProcessor.CheckRequrements(factionTech);

            DefaultThrusterDesign(game, factionEntity);
            F1ThrusterDesign(game, factionEntity);
            RaptorThrusterDesign(game, factionEntity);
            RS25ThrusterDesign(game, factionEntity);
            DefaultWarpDesign(game, factionEntity);
            DefaultFuelTank(game, factionEntity);
            LargeFuelTank(game, factionEntity);
            DefaultCargoInstallation(game, factionEntity);
            DefaultSimpleLaser(game, factionEntity);
            DefaultBFC(game, factionEntity);
            ShipDefaultCargoHold(game, factionEntity);
            ShipSmallCargo(game, factionEntity);
            ShipPassiveSensor(game, factionEntity);
            FacPassiveSensor(game, factionEntity);
            DefaultFisionReactor(game, factionEntity);
            DefaultBatteryBank(game, factionEntity);
            DefaultFragPayload(game, factionEntity);
            DefaultMissileSRB(game, factionEntity);
            DefaultMissileSensors(game, factionEntity);
            DefaultMissileTube(game, factionEntity);
            MissileDesign250(game, factionEntity);
            ShipSmallOrdnanceStore(game, factionEntity);

            colonyEntity.AddComponent(mineDesign);
            colonyEntity.AddComponent(refinaryDesign);
            colonyEntity.AddComponent(labEntity);
            colonyEntity.AddComponent(facEntity);
            colonyEntity.AddComponent(_fuelTank_1000);
            colonyEntity.AddComponent(_cargoInstalation);
            colonyEntity.AddComponent(_sensorInstalation);
            colonyEntity.AddComponent(ShipYard(factionEntity));
            colonyEntity.AddComponent(LogisticsOffice(factionEntity));
            colonyEntity.AddComponent(_ordnanceStore, 10);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);

            marsColony.AddComponent(_cargoInstalation);
            marsColony.AddComponent(LogisticsOffice(factionEntity));
            ReCalcProcessor.ReCalcAbilities(marsColony);

            var earthCargo = colonyEntity.GetDataBlob<VolumeStorageDB>();

            colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            var rawSorium = NameLookup.GetMineralSD(game, "Sorium");

            var iron = NameLookup.GetMineralSD(game, "Iron");
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, iron, 500000);

            var hydrocarbon = NameLookup.GetMineralSD(game, "Hydrocarbons");
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, hydrocarbon, 5000);

            var stainless = NameLookup.GetMaterialSD(game, "Stainless Steel");
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, stainless, 100000);

            var chromium = NameLookup.GetMineralSD(game, "Chromium");
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, chromium, 50000);

            var fisiles = NameLookup.GetMineralSD(game, "Fissionables");
            CargoTransferProcessor.AddRemoveCargoMass(colonyEntity, fisiles, 50000);

            var copper = NameLookup.GetMineralSD(game, "Copper");
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


            factionEntity.GetDataBlob<NameDB>().SetName(factionEntity.Guid, "UEF");

            var fleetName = NameFactory.GetFleetName();
            Entity defaultFleet = FleetFactory.Create(earth.Manager, factionEntity.Guid, fleetName);
            defaultFleet.GetDataBlob<FleetDB>().SetParent(factionEntity);

            // Todo: handle this in CreateShip
            ShipDesign shipDesign = DefaultShipDesign(game, factionEntity);
            ShipDesign gunShipDesign = GunShipDesign(game, factionEntity);
            ShipDesign pexDesign = CargoShipDesign(game, factionEntity);
            ShipDesign courierDesign = CargoCourierDesign(game, factionEntity);

            Entity gunShip0 = ShipFactory.CreateShip(gunShipDesign, factionEntity, earth,  "Serial Peacemaker");
            Entity ship2 = ShipFactory.CreateShip(shipDesign, factionEntity, earth,  "Ensuing Calm");
            Entity ship3 = ShipFactory.CreateShip(shipDesign, factionEntity, earth,  "Touch-and-Go");
            Entity gunShip1 = ShipFactory.CreateShip(gunShipDesign, factionEntity, earth,  "Prevailing Stillness");
            Entity courier = ShipFactory.CreateShip(pexDesign, factionEntity, earth, Math.PI, "Old Bessie");
            Entity courier2 = ShipFactory.CreateShip(pexDesign, factionEntity, earth, 0, "PE2");
            Entity starship = ShipFactory.CreateShip(SpaceXStarShip(game, factionEntity), factionEntity, earth,  "Starship");
            var fuel = NameLookup.GetMaterialSD(game, "Sorium Fuel");
            var rp1 = NameLookup.GetMaterialSD(game, "RP-1");
            var methalox = NameLookup.GetMaterialSD(game, "Methalox");
            var hydrolox = NameLookup.GetMaterialSD(game, "Hydrolox");

            for(int i = 0; i < 7; i++)
            {
                var commanderDB = CommanderFactory.CreateShipCaptain();
                commanderDB.CommissionedOn = StaticRefLib.CurrentDateTime - TimeSpan.FromDays(365.25 * 10);
                commanderDB.RankedOn = StaticRefLib.CurrentDateTime - TimeSpan.FromDays(365);
                var entity = CommanderFactory.Create(earth.Manager, factionEntity.Guid, commanderDB);

                if(i == 0) gunShip0.GetDataBlob<ShipInfoDB>().CommanderID = entity.Guid;
                if(i == 1) ship2.GetDataBlob<ShipInfoDB>().CommanderID = entity.Guid;
                if(i == 2) ship3.GetDataBlob<ShipInfoDB>().CommanderID = entity.Guid;
                if(i == 3) gunShip1.GetDataBlob<ShipInfoDB>().CommanderID = entity.Guid;
                if(i == 4) courier.GetDataBlob<ShipInfoDB>().CommanderID = entity.Guid;
                if(i == 5) courier2.GetDataBlob<ShipInfoDB>().CommanderID = entity.Guid;
                if(i == 6) starship.GetDataBlob<ShipInfoDB>().CommanderID = entity.Guid;
            }

            var fleetDB = defaultFleet.GetDataBlob<FleetDB>();
            fleetDB.AddChild(gunShip0);
            fleetDB.AddChild(ship2);
            fleetDB.AddChild(ship3);
            fleetDB.AddChild(gunShip1);
            fleetDB.AddChild(courier);
            fleetDB.AddChild(courier2);
            fleetDB.AddChild(starship);
            fleetDB.FlagShipID = starship.Guid;

            // This can be removed, only for testing orders without having to set them up in game
            ConditionItem conditionItem = new ConditionItem(new FuelCondition(30f, ComparisonType.GreaterThan));
            CompoundCondition compoundCondition = new CompoundCondition(conditionItem);
            SafeList<EntityCommand> actions = new SafeList<EntityCommand>();
            actions.Add(MoveToNearestColonyAction.CreateCommand(factionEntity.Guid, defaultFleet));
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

            var elec = NameLookup.GetMaterialSD(game, "Electrical Energy");
            gunShip0.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            gunShip1.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            ship2.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            ship3.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            courier.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            courier2.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;


            Entity targetDrone0 = ShipFactory.CreateShip(TargetDrone(game, targetFaction), targetFaction, earth, (10 * Math.PI / 180), "Target Drone0");
            Entity targetDrone1 = ShipFactory.CreateShip(TargetDrone(game, targetFaction), targetFaction, earth, (22.5 * Math.PI / 180), "Target Drone1");
            Entity targetDrone2 = ShipFactory.CreateShip(TargetDrone(game, targetFaction), targetFaction, earth, (45 * Math.PI / 180), "Target Drone2");
            targetDrone0.GetDataBlob<NameDB>().SetName(factionEntity.Guid, "TargetDrone0");
            targetDrone1.GetDataBlob<NameDB>().SetName(factionEntity.Guid, "TargetDrone1");
            targetDrone2.GetDataBlob<NameDB>().SetName(factionEntity.Guid, "TargetDrone2");

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
            OrbitDB testOrbtdb_ship2 = OrbitDB.FromAsteroidFormat(solStar, solStar.GetDataBlob<MassVolumeDB>().MassDry, test_bodyMass, test_a, test_e, test_i, test_loan, test_aop, test_M0, StaticRefLib.CurrentDateTime);
            ship2.RemoveDataBlob<OrbitDB>();
            ship2.SetDataBlob(testOrbtdb_ship2);
            ship2.GetDataBlob<PositionDB>().SetParent(solStar);
            StaticRefLib.ProcessorManager.RunProcessOnEntity<OrbitDB>(ship2, 0);

            test_a = 0.51;
            test_i = 180;
            test_aop = 0;
            OrbitDB testOrbtdb_ship3 = OrbitDB.FromAsteroidFormat(solStar, solStar.GetDataBlob<MassVolumeDB>().MassDry, test_bodyMass, test_a, test_e, test_i, test_loan, test_aop, test_M0, StaticRefLib.CurrentDateTime);
            ship3.RemoveDataBlob<OrbitDB>();
            ship3.SetDataBlob(testOrbtdb_ship3);
            ship3.GetDataBlob<PositionDB>().SetParent(solStar);
            StaticRefLib.ProcessorManager.RunProcessOnEntity<OrbitDB>(ship3, 0);


            gunShip1.GetDataBlob<PositionDB>().RelativePosition = Distance.AuToMt(new Vector3(0, 8.52699302490434E-05, 0));
            //give the gunship a hypobolic orbit to test:
            //var orbit = OrbitDB.FromVector(earth, gunShip, new Vector4(0, velInAU, 0, 0), game.CurrentDateTime);
            gunShip1.RemoveDataBlob<OrbitDB>();
            var nmdb = new NewtonMoveDB(earth, new Vector3(-10000.0, 0, 0));
            gunShip1.SetDataBlob<NewtonMoveDB>(nmdb);



            solSys.SetDataBlob(gunShip0.ID, new TransitableDB());
            solSys.SetDataBlob(ship2.ID, new TransitableDB());
            solSys.SetDataBlob(gunShip1.ID, new TransitableDB());
            solSys.SetDataBlob(courier.ID, new TransitableDB());
            solSys.SetDataBlob(courier2.ID, new TransitableDB());

            //Entity ship = ShipFactory.CreateShip(shipDesign, sol.SystemManager, factionEntity, position, sol, "Serial Peacemaker");
            //ship.SetDataBlob(earth.GetDataBlob<PositionDB>()); //first ship reference PositionDB

            //Entity ship3 = ShipFactory.CreateShip(shipDesign, sol.SystemManager, factionEntity, position, sol, "Contiual Pacifier");
            //ship3.SetDataBlob((OrbitDB)earth.GetDataBlob<OrbitDB>().Clone());//second ship clone earth OrbitDB


            //sol.SystemManager.SetDataBlob(ship.ID, new TransitableDB());

            //Entity rock = AsteroidFactory.CreateAsteroid2(sol, earth, game.CurrentDateTime + TimeSpan.FromDays(365));
            Entity rock = AsteroidFactory.CreateAsteroid(solSys, earth, StaticRefLib.CurrentDateTime + TimeSpan.FromDays(365));


            var pow = solSys.GetAllEntitiesWithDataBlob<EnergyGenAbilityDB>();
            foreach (var entityItem in pow)
            {
                StaticRefLib.ProcessorManager.GetInstanceProcessor(nameof(EnergyGenProcessor)).ProcessEntity(entityItem, StaticRefLib.CurrentDateTime);

            }

            var entitiesWithSensors = solSys.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
            foreach (var entityItem in entitiesWithSensors)
            {
                StaticRefLib.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem, StaticRefLib.CurrentDateTime);
            }

            ComponentDesigner.StartResearched = false;
            return factionEntity;
        }


        public static ShipDesign DefaultShipDesign(Game game, Entity faction)
        {
            if (_defaultShipDesign != null)
                return _defaultShipDesign;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (ShipPassiveSensor(game, faction), 1),
                (DefaultSimpleLaser(game, faction), 2),
                (DefaultBFC(game, faction), 1),
                (ShipSmallCargo(game, faction), 1),
                (DefaultFuelTank(game, faction), 2),
                (DefaultWarpDesign(game, faction), 4),
                (DefaultBatteryBank(game, faction), 3),
                (DefaultFisionReactor(game, faction), 1),
                (DefaultThrusterDesign(game, faction), 3),

            };
            ArmorSD plastic = game.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            _defaultShipDesign = new ShipDesign(factionInfo, "Ob'enn Dropship", components2, (plastic, 3));
            _defaultShipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _defaultShipDesign.Armor);
            return _defaultShipDesign;
        }

        public static ShipDesign SpaceXStarShip(Game game, Entity faction)
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
                (ShipPassiveSensor(game, faction), 1),
                (ShipSmallCargo(game, faction), 1),
                (LargeFuelTank(game, faction), 1),
                (RaptorThrusterDesign(game, faction), 3), //3 for vac

            };
            ArmorSD stainless = game.StaticData.ArmorTypes[new Guid("05dce711-8846-488a-b0f3-57fd7924b268")];
            _spaceXStarShipDesign = new ShipDesign(factionInfo, "Starship", components2, (stainless, 12.75f));
            _spaceXStarShipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _spaceXStarShipDesign.Armor);
            return _spaceXStarShipDesign;
        }

        public static ShipDesign GunShipDesign(Game game, Entity faction)
        {
            if (_gunshipDesign != null)
                return _gunshipDesign;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (ShipPassiveSensor(game, faction), 1),
                (DefaultSimpleLaser(game, faction), 2),
                (DefaultBFC(game, faction), 1),
                (DefaultMissileTube(game, faction),1),
                (ShipSmallOrdnanceStore(game, faction), 2),
                (ShipSmallCargo(game,faction), 1),
                (DefaultFuelTank(game, faction), 2),
                (DefaultWarpDesign(game, faction), 4),
                (DefaultBatteryBank(game, faction), 3),
                (DefaultFisionReactor(game, faction), 1),
                (DefaultThrusterDesign(game, faction), 4),
            };
            ArmorSD plastic = game.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            _gunshipDesign = new ShipDesign(factionInfo, "Sanctum Adroit GunShip", components2, (plastic, 3));
            _gunshipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _gunshipDesign.Armor);
            return _gunshipDesign;
        }

        public static ShipDesign TargetDrone(Game game, Entity faction)
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
            ArmorSD plastic = game.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            var shipdesign = new ShipDesign(factionInfo, "TargetDrone", components2, (plastic, 3));
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
        }

        public static ShipDesign CargoShipDesign(Game game, Entity faction)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (DefaultSimpleLaser(game, faction), 1),
                (DefaultBFC(game, faction), 1),
                (_sensor_50, 1),
                (_fuelTank_2500, 1),
                (_fuelTank_2500, 1),
                (_cargoHold, 1),
                (_warpDrive, 4),
                (_battery, 2),
                (_reactor, 1),
                (_rs25, 4),
            };
            ArmorSD plastic = game.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            var shipdesign = new ShipDesign(factionInfo, "Planet Express Ship", components2, (plastic, 3));
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
        }

        public static ShipDesign CargoCourierDesign(Game game, Entity faction)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (_sensor_50, 1),
                (VLargeFuelTank(game,faction), 1),
                (ShipSmallCargo(game,faction), 1),
                (LargeWarpDesign(game, faction), 1),
                (_battery, 2),
                (_reactor, 1),
                (_rs25, 1),
            };
            ArmorSD plastic = game.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
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
                (DefaultFragPayload(game, faction), 1),
                (DefaultMissileSensors(game, faction), 1),
                (DefaultMissileSRB(game, faction), 1),
            };
            double fuelkg = 225;
            _missile = new OrdnanceDesign(factionInfo, "Missile250", fuelkg, components);
            return _missile;
        }

        public static ComponentDesign ShipYard(Entity faction)
        {
            if (_shipYard != null)
                return _shipYard;
            ComponentDesigner spacePortDesigner;
            ComponentTemplateSD spaceportSD = StaticRefLib.StaticData.ComponentTemplates[new Guid("0BD304FF-FDEA-493C-8979-15FE86B7123E")];
            spacePortDesigner = new ComponentDesigner(spaceportSD, faction.GetDataBlob<FactionTechDB>());
            spacePortDesigner.Name = "Ship Yard";
            _shipYard = spacePortDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_shipYard.TechID);
            return _shipYard;
        }
        public static ComponentDesign LogisticsOffice(Entity faction)
        {
            if (_logiOffice != null)
                return _logiOffice;
            ComponentDesigner logofficeDesigner;
            ComponentTemplateSD logofficeSD = StaticRefLib.StaticData.ComponentTemplates[new Guid("5B5034C1-8DA1-4645-8F5C-A52B65BB8369")];
            logofficeDesigner = new ComponentDesigner(logofficeSD, faction.GetDataBlob<FactionTechDB>());
            logofficeDesigner.Name = "Logistics Office";
            _logiOffice = logofficeDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_logiOffice.TechID);
            return _logiOffice;
        }
        public static ComponentDesign DefaultThrusterDesign(Game game, Entity faction)
        {
            if (_merlin != null)
                return _merlin;

            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("b12f50f6-ac68-4a49-b147-281a9bb34b9b")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(30);
            engineDesigner.Name = "Merlin";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _merlin = engineDesigner.CreateDesign(faction);

            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_merlin.TechID);
            return _merlin;
        }

        public static ComponentDesign F1ThrusterDesign(Game game, Entity faction)
        {
            if (_f1 != null)
                return _f1;

            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("b12f50f6-ac68-4a49-b147-281a9bb34b9b")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(50);
            engineDesigner.Name = "F1";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _f1 = engineDesigner.CreateDesign(faction);

            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_f1.TechID);
            return _f1;
        }

        /*
            Target Stats:
            Mass: 1500kg
            Thrust: 2.3 MN
            Isp 380s (3.73)
        */
        public static ComponentDesign RaptorThrusterDesign(Game game, Entity faction)
        {
            if (_raptor != null)
                return _raptor;

            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("B03FE82F-EE70-4A9A-AC61-5A7D44A3364E")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(1500);
            engineDesigner.Name = "Raptor-Vac";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _raptor = engineDesigner.CreateDesign(faction);

            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_raptor.TechID);
            return _raptor;
        }

        public static ComponentDesign RS25ThrusterDesign(Game game, Entity faction)
        {
            if (_rs25 != null)
                return _rs25;
            ComponentDesigner engineDesigner;
            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("A0F3E5BB-0AA6-41D0-9873-5A7AC9080B69")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(3527);
            engineDesigner.Name = "RS-25";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _rs25 = engineDesigner.CreateDesign(faction);

            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_rs25.TechID);
            return _rs25;
        }




        public static ComponentDesign DefaultWarpDesign(Game game, Entity faction)
        {
            if (_warpDrive != null)
                return _warpDrive;

            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("7d0b867f-e239-4b93-9b30-c6d4b769b5e4")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(1000); //size 500 = 2500 power
            engineDesigner.Name = "Alcuberi-White 500";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _warpDrive = engineDesigner.CreateDesign(faction);

            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_warpDrive.TechID);
            return _warpDrive;
        }

        public static ComponentDesign LargeWarpDesign(Game game, Entity faction)
        {
            if (_largeWarpDrive != null)
                return _largeWarpDrive;

            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("7d0b867f-e239-4b93-9b30-c6d4b769b5e4")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(4000);
            engineDesigner.Name = "Alcuberi-White 2k";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput

            _warpDrive = engineDesigner.CreateDesign(faction);

            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_warpDrive.TechID);
            return _warpDrive;
        }

        public static ComponentDesign DefaultFuelTank(Game game, Entity faction)
        {
            if (_fuelTank_1000 != null)
                return _fuelTank_1000;
            ComponentDesigner fuelTankDesigner;
            ComponentTemplateSD tankSD = game.StaticData.ComponentTemplates[new Guid("3528600E-3A1C-488C-BAE6-60251D1156AB")];
            fuelTankDesigner = new ComponentDesigner(tankSD, faction.GetDataBlob<FactionTechDB>());
            fuelTankDesigner.ComponentDesignAttributes["Tank Volume"].SetValueFromInput(1000);
            fuelTankDesigner.Name = "Tank-1000m^3";
            _fuelTank_1000 = fuelTankDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_fuelTank_1000.TechID);
            return _fuelTank_1000;
        }

        public static ComponentDesign LargeFuelTank(Game game, Entity faction)
        {
            if (_fuelTank_2500 != null)
                return _fuelTank_2500;
            ComponentDesigner fuelTankDesigner;
            ComponentTemplateSD tankSD = game.StaticData.ComponentTemplates[new Guid("3528600E-3A1C-488C-BAE6-60251D1156AB")];
            fuelTankDesigner = new ComponentDesigner(tankSD, faction.GetDataBlob<FactionTechDB>());
            fuelTankDesigner.ComponentDesignAttributes["Tank Volume"].SetValueFromInput(1500);
            fuelTankDesigner.Name = "Tank-1500m^3";
            _fuelTank_2500 = fuelTankDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_fuelTank_2500.TechID);
            return _fuelTank_2500;
        }

        public static ComponentDesign VLargeFuelTank(Game game, Entity faction)
        {
            if (_fuelTank_3000 != null)
                return _fuelTank_3000;
            ComponentDesigner fuelTankDesigner;
            ComponentTemplateSD tankSD = game.StaticData.ComponentTemplates[new Guid("3528600E-3A1C-488C-BAE6-60251D1156AB")];
            fuelTankDesigner = new ComponentDesigner(tankSD, faction.GetDataBlob<FactionTechDB>());
            fuelTankDesigner.ComponentDesignAttributes["Tank Volume"].SetValueFromInput(3000);
            fuelTankDesigner.Name = "Tank-3000m^3";
            _fuelTank_3000 = fuelTankDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_fuelTank_3000.TechID);
            return _fuelTank_3000;
        }

        public static ComponentDesign DefaultSimpleLaser(Game game, Entity faction)
        {
            if (_laser != null)
                return _laser;
            ComponentDesigner laserDesigner;
            ComponentTemplateSD laserSD = game.StaticData.ComponentTemplates[new Guid("8923f0e1-1143-4926-a0c8-66b6c7969425")];
            laserDesigner = new ComponentDesigner(laserSD, faction.GetDataBlob<FactionTechDB>());
            laserDesigner.ComponentDesignAttributes["Range"].SetValueFromInput(100);
            laserDesigner.ComponentDesignAttributes["Damage"].SetValueFromInput(5000);
            laserDesigner.ComponentDesignAttributes["ReloadRate"].SetValueFromInput(5);

            _laser = laserDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_laser.TechID);
            return _laser;

        }
        public static ComponentDesign DefaultFragPayload(Game game, Entity faction)
        {
            if (_payload != null)
                return _payload;
            ComponentDesigner payloadDesigner;
            ComponentTemplateSD payloadSD = game.StaticData.ComponentTemplates[new Guid("DF9954A7-C5C5-4B49-965C-446B483DA2BE")];
            payloadDesigner = new ComponentDesigner(payloadSD, faction.GetDataBlob<FactionTechDB>());
            payloadDesigner.ComponentDesignAttributes["Trigger Type"].SetValueFromInput(2);
            payloadDesigner.ComponentDesignAttributes["Payload Type"].SetValueFromInput(0);
            payloadDesigner.ComponentDesignAttributes["Explosive Mass"].SetValueFromInput(2);
            payloadDesigner.ComponentDesignAttributes["Frag Mass"].SetValueFromInput(0.1);
            payloadDesigner.ComponentDesignAttributes["Frag Count"].SetValueFromInput(30);
            payloadDesigner.ComponentDesignAttributes["Frag Cone Angle"].SetValueFromInput(180);
            payloadDesigner.Name = "ProxFrag 5kg";
            _payload = payloadDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_payload.TechID);
            return _payload;
        }

        public static ComponentDesign DefaultMissileSensors(Game game, Entity faction)
        {
            if (_missileSuite != null)
                return _missileSuite;
            ComponentDesigner suiteDesigner;
            ComponentTemplateSD srbSD = game.StaticData.ComponentTemplates[new Guid("BBC29A72-C4D3-4389-94DE-36C3BE3B7B0E")];
            suiteDesigner = new ComponentDesigner(srbSD, faction.GetDataBlob<FactionTechDB>());
            suiteDesigner.ComponentDesignAttributes["Guidance Type"].SetValueFromInput(2);
            suiteDesigner.ComponentDesignAttributes["Antenna Size"].SetValueFromInput(10);
            suiteDesigner.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(470);
            suiteDesigner.ComponentDesignAttributes["Detection Bandwidth"].SetValueFromInput(2);
            suiteDesigner.ComponentDesignAttributes["Resolution"].SetValueFromInput(1);
            suiteDesigner.Name = "Passive Yellow 1MP ";
            _missileSuite = suiteDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_missileSuite.TechID);
            return _missileSuite;
        }
        public static ComponentDesign DefaultMissileSRB(Game game, Entity faction)
        {
            if (_missileSRB != null)
                return _missileSRB;
            ComponentDesigner srbDesigner;
            ComponentTemplateSD srbSD = game.StaticData.ComponentTemplates[new Guid("9FDB2A15-4413-40A9-9229-19D05B3765FE")];
            srbDesigner = new ComponentDesigner(srbSD, faction.GetDataBlob<FactionTechDB>());
            srbDesigner.ComponentDesignAttributes["Engine Mass"].SetValueFromInput(10);

            srbDesigner.Name = "SRB 235";
            _missileSRB = srbDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_missileSRB.TechID);
            return _missileSRB;
        }

        public static ComponentDesign DefaultMissileTube(Game game, Entity faction)
        {
            if (_missileTube != null)
                return _missileTube;
            ComponentDesigner tubeDesigner;
            ComponentTemplateSD tubeSD = game.StaticData.ComponentTemplates[new Guid("978DFA9E-411E-4B4F-A618-85D642927503")];
            tubeDesigner = new ComponentDesigner(tubeSD, faction.GetDataBlob<FactionTechDB>());
            tubeDesigner.Name = "MissileTube 500";
            _missileTube = tubeDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_missileTube.TechID);
            return _missileTube;
        }

        public static ComponentDesign DefaultBFC(Game game, Entity faction)
        {
            if (_fireControl != null)
                return _fireControl;
            ComponentDesigner fireControlDesigner;
            ComponentTemplateSD bfcSD = game.StaticData.ComponentTemplates[new Guid("33fcd1f5-80ab-4bac-97be-dbcae19ab1a0")];
            fireControlDesigner = new ComponentDesigner(bfcSD, faction.GetDataBlob<FactionTechDB>());
            fireControlDesigner.ComponentDesignAttributes["Range"].SetValueFromInput(100);
            fireControlDesigner.ComponentDesignAttributes["Tracking Speed"].SetValueFromInput(5000);
            fireControlDesigner.ComponentDesignAttributes["Size vs Range"].SetValueFromInput(1);

            //return fireControlDesigner.CreateDesign(faction);
            _fireControl = fireControlDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_fireControl.TechID);
            return _fireControl;
        }

        public static ComponentDesign DefaultCargoInstallation(Game game, Entity faction)
        {
            if (_cargoInstalation != null)
                return _cargoInstalation;
            ComponentDesigner componentDesigner;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{B8239721-B60E-4C11-8E45-5F64F6BA5FA5}")];
            componentDesigner = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes["Warehouse Size"].SetValueFromInput(1000000);
            componentDesigner.Name = "CargoInstallation1";
            //return cargoInstalation.CreateDesign(faction);
            _cargoInstalation = componentDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_cargoInstalation.TechID);
            return _cargoInstalation;
        }

        public static ComponentDesign DefaultFisionReactor(Game game, Entity faction)
        {
            if (_reactor != null)
                return _reactor;
            ComponentDesigner componentDesigner;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{97cf75a1-5ca3-4037-8832-4d81a89f97fa}")];
            componentDesigner = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(1000);
            componentDesigner.Name = "Reactor15k";
            //return cargoInstalation.CreateDesign(faction);
            _reactor = componentDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_reactor.TechID);
            return _reactor;
        }

        public static ComponentDesign DefaultBatteryBank(Game game, Entity faction)
        {
            if (_battery != null)
                return _battery;
            ComponentDesigner componentDesigner;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{1de23a8b-d44b-4e0f-bacd-5463a8eb939d}")];
            componentDesigner = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(2000);
            componentDesigner.Name = "Battery2t";
            _battery = componentDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_battery.TechID);
            return _battery;
        }

        public static ComponentDesign ShipDefaultCargoHold(Game game, Entity faction)
        {
            if (_cargoHold != null)
                return _cargoHold;
            ComponentDesigner cargoComponent;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{B8239721-B60E-4C11-8E45-5F64F6BA5FA5}")];
            cargoComponent = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            cargoComponent.ComponentDesignAttributes["Warehouse Size"].SetValueFromInput(5000); //5t component
            cargoComponent.ComponentDesignAttributes["Cargo Transfer Rate"].SetValueFromInput(500);
            cargoComponent.ComponentDesignAttributes["Transfer Range"].SetValueFromInput(100);
            cargoComponent.Name = "CargoComponent5t";
            _cargoHold = cargoComponent.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_cargoHold.TechID);
            return _cargoHold;
        }

        public static ComponentDesign ShipSmallCargo(Game game, Entity faction)
        {
            if (_cargoCompartment != null)
                return _cargoCompartment;
            ComponentDesigner cargoComponent;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{B8239721-B60E-4C11-8E45-5F64F6BA5FA5}")];
            cargoComponent = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            cargoComponent.ComponentDesignAttributes["Warehouse Size"].SetValueFromInput(1000); //5t component
            cargoComponent.ComponentDesignAttributes["Cargo Transfer Rate"].SetValueFromInput(500);
            cargoComponent.ComponentDesignAttributes["Transfer Range"].SetValueFromInput(100);
            cargoComponent.Name = "CargoComponent1t";
            _cargoCompartment = cargoComponent.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_cargoCompartment.TechID);
            return _cargoCompartment;
        }
        public static ComponentDesign ShipSmallOrdnanceStore(Game game, Entity faction)
        {
            if (_ordnanceStore != null)
                return _ordnanceStore;
            ComponentDesigner cargoComponent;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{11564F56-D52C-4A16-8434-C9BB50D8EB95}")];
            cargoComponent = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            cargoComponent.ComponentDesignAttributes["Rack Size"].SetValueFromInput(2627); //5t component
            cargoComponent.ComponentDesignAttributes["Cargo Transfer Rate"].SetValueFromInput(100);
            cargoComponent.ComponentDesignAttributes["Transfer Range"].SetValueFromInput(100);
            cargoComponent.Name = "OrdinanceRack-2.5t";
            _ordnanceStore = cargoComponent.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_ordnanceStore.TechID);
            return _ordnanceStore;
        }

        public static ComponentDesign ShipPassiveSensor(Game game, Entity faction)
        {
            if (_sensor_50 != null)
                return _sensor_50;
            ComponentDesigner sensor;
            ComponentTemplateSD template = NameLookup.GetTemplateSD(game, "PassiveSensor");
            sensor = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            sensor.ComponentDesignAttributes["Antenna Size"].SetValueFromInput(5.5);  //size
            sensor.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(479); //best wavelength
            sensor.ComponentDesignAttributes["Detection Bandwidth"].SetValueFromInput(200); //wavelength detection width
            //sensor.ComponentDesignAttributes[3].SetValueFromInput(10);  //best detection magnatude. (Not settable)
            //[4] worst detection magnatude (not settable)
            sensor.ComponentDesignAttributes["Resolution"].SetValueFromInput(1);   //resolution
            sensor.ComponentDesignAttributes["Scan Time"].SetValueFromInput(3600);//Scan Time
            sensor.Name = "PassiveSensor-S50";
            _sensor_50 = sensor.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_sensor_50.TechID);
            return _sensor_50;

        }

        public static ComponentDesign FacPassiveSensor(Game game, Entity faction)
        {
            if (_sensorInstalation != null)
                return _sensorInstalation;
            ComponentDesigner sensor;
            ComponentTemplateSD template = NameLookup.GetTemplateSD(game, "PassiveSensor");
            sensor = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            sensor.ComponentDesignAttributes["Antenna Size"].SetValueFromInput(5000);  //size
            sensor.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(470); //best wavelength
            sensor.ComponentDesignAttributes["Detection Bandwidth"].SetValueFromInput(250); //wavelength detection width
            //sensor.ComponentDesignAttributes[3].SetValueFromInput(10);  //best detection magnatude. (Not settable)
            //[4] worst detection magnatude (not settable)
            sensor.ComponentDesignAttributes["Resolution"].SetValueFromInput(100);   //resolution
            sensor.ComponentDesignAttributes["Scan Time"].SetValueFromInput(3600);//Scan Time
            sensor.Name = "PassiveScannerInstalation";
            _sensorInstalation = sensor.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_sensorInstalation.TechID);
            return _sensorInstalation;

        }
    }

}