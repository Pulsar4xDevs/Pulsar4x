using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Engine.Factories;

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
        private static ComponentDesign _sensorInstallation;
        private static ComponentDesign _fireControl;
        private static ComponentDesign _cargoInstallation;
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
        private static ComponentDesign _geoSurveyor;
        private static ComponentDesign _jpSurveyor;

        public static Entity DefaultHumans(Game game, string name)
        {

            ComponentDesigner.StartResearched = true;//any components we design should be researched already. turn this off at the end.

            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem startingSystem = starfac.LoadSystemFromJson(game, "Data/basemod/sol/");
            Entity solStar = startingSystem.GetAllEntitiesWithDataBlob<StarInfoDB>().First();
            Entity earth = NameLookup.GetFirstEntityWithName(startingSystem, "Earth");
            Entity luna = NameLookup.GetFirstEntityWithName(startingSystem, "Luna");
            Entity mars = NameLookup.GetFirstEntityWithName(startingSystem, "Mars");

            Entity factionEntity = FactionFactory.CreateFaction(game, name);
            FactionInfoDB factionInfoDB = factionEntity.GetDataBlob<FactionInfoDB>();
            FactionDataStore factionDataStore = factionInfoDB.Data;

            // Set the faction entity to own itself so it can issue orders to itself
            factionEntity.FactionOwnerID = factionEntity.Id;
            factionInfoDB.KnownSystems.Add(startingSystem.Guid);
            factionEntity.GetDataBlob<NameDB>().SetName(factionEntity.Id, "UEF");

            earth.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[factionEntity.Id] = 0;
            luna.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[factionEntity.Id] = 0;
            mars.GetDataBlob<GeoSurveyableDB>().GeoSurveyStatus[factionEntity.Id] = 0;

            Entity targetFaction = FactionFactory.CreateFaction(game, "OpFor");
            FactionDataStore opForDataStore = targetFaction.GetDataBlob<FactionInfoDB>().Data;

            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);

            foreach (var nameDB in startingSystem.GetAllDataBlobsOfType<NameDB>())
            {
                nameDB.SetName(factionEntity.Id, nameDB.DefaultName);
            }

            long baseInitialPopulation = 9000000000;
            long variablePopulation = (long)(baseInitialPopulation * .1);
            long initialPopulationOfEarth = game.RNG.NextInt64(baseInitialPopulation - variablePopulation, baseInitialPopulation + variablePopulation);

            Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth, initialPopulationOfEarth);
            Entity marsColony = ColonyFactory.CreateColony(factionEntity, speciesEntity, NameLookup.GetFirstEntityWithName(startingSystem, "Mars"));

            var mineDesign = ComponentDesignFromJson.Create(factionEntity, factionDataStore, "Data/basemod/componentDesigns/mine.json");
            var refinaryDesign = ComponentDesignFromJson.Create(factionEntity, factionDataStore, "Data/basemod/componentDesigns/refinery.json");
            var labEntity = ComponentDesignFromJson.Create(factionEntity, factionDataStore, "Data/basemod/componentDesigns/university.json");
            var facEntity = ComponentDesignFromJson.Create(factionEntity, factionDataStore, "Data/basemod/componentDesigns/factory.json");

            Scientist scientistEntity = CommanderFactory.CreateScientist(factionEntity, colonyEntity);
            colonyEntity.GetDataBlob<TeamsHousedDB>().AddTeam(scientistEntity);

            DefaultThrusterDesign(factionEntity, factionDataStore);
            F1ThrusterDesign(factionEntity, factionDataStore);
            RaptorThrusterDesign(factionEntity, factionDataStore);
            RS25ThrusterDesign(factionEntity, factionDataStore);
            DefaultWarpDesign(factionEntity, factionDataStore);
            DefaultFuelTank(factionEntity, factionDataStore);
            LargeFuelTank(factionEntity, factionDataStore);
            DefaultCargoInstallation(factionEntity, factionDataStore);
            DefaultSimpleLaser(factionEntity, factionDataStore);
            DefaultBFC(factionEntity, factionDataStore);
            ShipDefaultCargoHold(factionEntity, factionDataStore);
            ShipSmallCargo(factionEntity, factionDataStore);
            ShipPassiveSensor(factionEntity, factionDataStore);
            FacPassiveSensor(factionEntity, factionDataStore);
            DefaultFisionReactor(factionEntity, factionDataStore);
            DefaultBatteryBank(factionEntity, factionDataStore);
            DefaultFragPayload(factionEntity, factionDataStore);
            DefaultMissileSRB(factionEntity, factionDataStore);
            DefaultMissileSensors(factionEntity, factionDataStore);
            DefaultMissileTube(factionEntity, factionDataStore);
            MissileDesign250(factionEntity);
            ShipSmallOrdnanceStore(factionEntity, factionDataStore);

            colonyEntity.AddComponent(mineDesign);
            colonyEntity.AddComponent(refinaryDesign);
            colonyEntity.AddComponent(labEntity);
            colonyEntity.AddComponent(facEntity);
            colonyEntity.AddComponent(_fuelTank_1000);
            colonyEntity.AddComponent(_cargoInstallation);
            colonyEntity.AddComponent(_sensorInstallation);
            colonyEntity.AddComponent(ShipYard(factionEntity, factionDataStore));
            colonyEntity.AddComponent(LogisticsOffice(factionEntity, factionDataStore));
            colonyEntity.AddComponent(_ordnanceStore, 10);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);

            marsColony.AddComponent(_cargoInstallation);
            marsColony.AddComponent(LogisticsOffice(factionEntity, factionDataStore));
            ReCalcProcessor.ReCalcAbilities(marsColony);

            var earthCargo = colonyEntity.GetDataBlob<VolumeStorageDB>();
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

            var fleetName = NameFactory.GetFleetName(game);
            Entity defaultFleet = FleetFactory.Create(earth.Manager, factionEntity.Id, fleetName);
            defaultFleet.GetDataBlob<FleetDB>().SetParent(factionEntity);

            // Todo: handle this in CreateShip
            ShipDesign shipDesign = DefaultShipDesign(factionEntity, factionDataStore);
            ShipDesign gunShipDesign = GunShipDesign(factionEntity, factionDataStore);
            ShipDesign pexDesign = CargoShipDesign(factionEntity, factionDataStore);
            ShipDesign courierDesign = CargoCourierDesign(factionEntity, factionDataStore);

            Entity gunShip0 = ShipFactory.CreateShip(gunShipDesign, factionEntity, earth,  "Serial Peacemaker");
            Entity ship2 = ShipFactory.CreateShip(shipDesign, factionEntity, earth,  "Ensuing Calm");
            Entity ship3 = ShipFactory.CreateShip(shipDesign, factionEntity, earth,  "Touch-and-Go");
            Entity gunShip1 = ShipFactory.CreateShip(gunShipDesign, factionEntity, earth,  "Prevailing Stillness");
            Entity courier = ShipFactory.CreateShip(pexDesign, factionEntity, earth, Math.PI, "Old Bessie");
            Entity courier2 = ShipFactory.CreateShip(pexDesign, factionEntity, earth, 0, "PE2");
            Entity starship = ShipFactory.CreateShip(SpaceXStarShip(factionEntity, factionDataStore), factionEntity, earth,  "Starship");
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

            var elec = factionDataStore.CargoGoods["electricity"];
            gunShip0.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            gunShip1.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            ship2.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            ship3.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            courier.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;
            courier2.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.UniqueID] = 2750000;


            Entity targetDrone0 = ShipFactory.CreateShip(TargetDrone(targetFaction, factionDataStore), targetFaction, earth, (10 * Math.PI / 180), "Target Drone0");
            Entity targetDrone1 = ShipFactory.CreateShip(TargetDrone(targetFaction, factionDataStore), targetFaction, earth, (22.5 * Math.PI / 180), "Target Drone1");
            Entity targetDrone2 = ShipFactory.CreateShip(TargetDrone(targetFaction, factionDataStore), targetFaction, earth, (45 * Math.PI / 180), "Target Drone2");
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


            //var pos = Distance.AuToMt(new Vector3(8.52699302490434E-05, 0, 0));
            //var vel = new Vector3(0, 10000.0, 0);
            // var pos = Distance.AuToMt(new Vector3(0, 8.52699302490434E-05, 0));
            // var vel = new Vector3(-10000.0, 0, 0);

            // var gunShip1Mass = gunShip1.GetDataBlob<MassVolumeDB>().MassTotal;
            // var earthmass = earth.GetDataBlob<MassVolumeDB>().MassTotal;

            // //give the gunship a hypobolic orbit to test:
            // var orbit = OrbitDB.FromVector(earth, gunShip1Mass, earthmass, pos, vel, game.TimePulse.GameGlobalDateTime);
            // gunShip1.GetDataBlob<PositionDB>().RelativePosition = pos;
            // gunShip1.SetDataBlob<OrbitDB>(orbit);
            // var pos2 = gunShip1.GetRelativeFuturePosition(game.TimePulse.GameGlobalDateTime);

            // var nmdb = new NewtonMoveDB(earth, new Vector3(-10000.0, 0, 0));
            // gunShip1.SetDataBlob<NewtonMoveDB>(nmdb);

            startingSystem.SetDataBlob(gunShip0.Id, new TransitableDB());
            startingSystem.SetDataBlob(ship2.Id, new TransitableDB());
            startingSystem.SetDataBlob(gunShip1.Id, new TransitableDB());
            startingSystem.SetDataBlob(courier.Id, new TransitableDB());
            startingSystem.SetDataBlob(courier2.Id, new TransitableDB());

            //Entity ship = ShipFactory.CreateShip(shipDesign, sol.SystemManager, factionEntity, position, sol, "Serial Peacemaker");
            //ship.SetDataBlob(earth.GetDataBlob<PositionDB>()); //first ship reference PositionDB

            //Entity ship3 = ShipFactory.CreateShip(shipDesign, sol.SystemManager, factionEntity, position, sol, "Contiual Pacifier");
            //ship3.SetDataBlob((OrbitDB)earth.GetDataBlob<OrbitDB>().Clone());//second ship clone earth OrbitDB


            //sol.SystemManager.SetDataBlob(ship.ID, new TransitableDB());

            //Entity rock = AsteroidFactory.CreateAsteroid2(sol, earth, game.CurrentDateTime + TimeSpan.FromDays(365));
            Entity rock = AsteroidFactory.CreateAsteroid(startingSystem, earth, game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(365));


            var pow = startingSystem.GetAllEntitiesWithDataBlob<EnergyGenAbilityDB>();
            foreach (var entityItem in pow)
            {
                game.ProcessorManager.GetInstanceProcessor(nameof(EnergyGenProcessor)).ProcessEntity(entityItem,  game.TimePulse.GameGlobalDateTime);

            }

            var entitiesWithSensors = startingSystem.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
            foreach (var entityItem in entitiesWithSensors)
            {
                game.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem,  game.TimePulse.GameGlobalDateTime);
            }

            ComponentDesigner.StartResearched = false;
            return factionEntity;
        }


        public static ShipDesign DefaultShipDesign(Entity faction, FactionDataStore factionDataStore)
        {
            if (_defaultShipDesign != null)
                return _defaultShipDesign;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (ShipPassiveSensor(faction, factionDataStore), 1),
                (DefaultSimpleLaser(faction, factionDataStore), 2),
                (DefaultBFC(faction, factionDataStore), 1),
                (ShipSmallCargo(faction, factionDataStore), 1),
                (DefaultGeoSurveyor(faction, factionDataStore), 1),
                (DefaultJPSurveyor(faction, factionDataStore), 1),
                (DefaultFuelTank(faction, factionDataStore), 2),
                (DefaultWarpDesign(faction, factionDataStore), 4),
                (DefaultBatteryBank(faction, factionDataStore), 3),
                (DefaultFisionReactor(faction, factionDataStore), 1),
                (DefaultThrusterDesign(faction, factionDataStore), 3),

            };
            ArmorBlueprint plastic = factionDataStore.Armor["plastic-armor"]; //factionDataStore.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            _defaultShipDesign = new ShipDesign(factionInfo, "Ob'enn Dropship", components2, (plastic, 3));
            _defaultShipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _defaultShipDesign.Armor);
            return _defaultShipDesign;
        }

        public static ShipDesign SpaceXStarShip(Entity faction, FactionDataStore factionDataStore)
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
                (ShipPassiveSensor(faction, factionDataStore), 1),
                (ShipSmallCargo(faction, factionDataStore), 1),
                (LargeFuelTank(faction, factionDataStore), 1),
                (RaptorThrusterDesign(faction, factionDataStore), 3), //3 for vac

            };
            ArmorBlueprint stainless = factionDataStore.Armor["stainless-steel-armor"];// factionDataStore.ArmorTypes[new Guid("05dce711-8846-488a-b0f3-57fd7924b268")];
            _spaceXStarShipDesign = new ShipDesign(factionInfo, "Starship", components2, (stainless, 12.75f));
            _spaceXStarShipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _spaceXStarShipDesign.Armor);
            return _spaceXStarShipDesign;
        }

        public static ShipDesign GunShipDesign(Entity faction, FactionDataStore factionDataStore)
        {
            if (_gunshipDesign != null)
                return _gunshipDesign;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (ShipPassiveSensor(faction, factionDataStore), 1),
                (DefaultSimpleLaser(faction, factionDataStore), 2),
                (DefaultBFC(faction, factionDataStore), 1),
                (DefaultMissileTube(faction, factionDataStore),1),
                (ShipSmallOrdnanceStore(faction, factionDataStore), 2),
                (ShipSmallCargo(faction, factionDataStore), 1),
                (DefaultFuelTank(faction, factionDataStore), 2),
                (DefaultWarpDesign(faction, factionDataStore), 4),
                (DefaultBatteryBank(faction, factionDataStore), 3),
                (DefaultFisionReactor(faction, factionDataStore), 1),
                (DefaultThrusterDesign(faction, factionDataStore), 4),
            };
            ArmorBlueprint plastic = factionDataStore.Armor["plastic-armor"];
            _gunshipDesign = new ShipDesign(factionInfo, "Sanctum Adroit GunShip", components2, (plastic, 3));
            _gunshipDesign.DamageProfileDB = new EntityDamageProfileDB(components2, _gunshipDesign.Armor);
            return _gunshipDesign;
        }

        public static ShipDesign TargetDrone(Entity faction, FactionDataStore factionDataStore)
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

        public static ShipDesign CargoShipDesign(Entity faction, FactionDataStore factionDataStore)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (DefaultSimpleLaser(faction, factionDataStore), 1),
                (DefaultBFC(faction, factionDataStore), 1),
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

        public static ShipDesign CargoCourierDesign(Entity faction, FactionDataStore factionDataStore)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (_sensor_50, 1),
                (VLargeFuelTank(faction, factionDataStore), 1),
                (ShipSmallCargo(faction, factionDataStore), 1),
                (LargeWarpDesign(faction, factionDataStore), 1),
                (_battery, 2),
                (_reactor, 1),
                (_rs25, 1),
            };
            ArmorBlueprint plastic = factionDataStore.Armor["plastic-armor"];
            var shipdesign = new ShipDesign(factionInfo, "Cargo Courier", components2, (plastic, 1));
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
        }

        public static OrdnanceDesign MissileDesign250(Entity faction)
        {
            if (_missile != null)
                return _missile;
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();

            List<(ComponentDesign, int)> components = new List<(ComponentDesign, int)>()
            {
                (DefaultFragPayload(faction, factionInfo.Data), 1),
                (DefaultMissileSensors(faction, factionInfo.Data), 1),
                (DefaultMissileSRB(faction, factionInfo.Data), 1),
            };
            double fuelkg = 225;
            _missile = new OrdnanceDesign(factionInfo, "Missile250", fuelkg, components);
            return _missile;
        }

        public static ComponentDesign ShipYard(Entity faction, FactionDataStore factionDataStore)
        {
            if (_shipYard != null)
                return _shipYard;
            _shipYard = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/shipyard.json");
            return _shipYard;
        }
        public static ComponentDesign LogisticsOffice(Entity faction, FactionDataStore factionDataStore)
        {
            if (_logiOffice != null)
                return _logiOffice;
            _logiOffice = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/logisticsOffice.json");
            return _logiOffice;
        }
        public static ComponentDesign DefaultThrusterDesign(Entity faction, FactionDataStore factionDataStore)
        {
            if (_merlin != null)
                return _merlin;
            _merlin = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/merlin.json");
            return _merlin;
        }

        public static ComponentDesign F1ThrusterDesign(Entity faction, FactionDataStore factionDataStore)
        {
            if (_f1 != null)
                return _f1;

            _f1 = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/f1.json");
            return _f1;
        }

        /*
            Target Stats:
            Mass: 1500kg
            Thrust: 2.3 MN
            Isp 380s (3.73)
        */
        public static ComponentDesign RaptorThrusterDesign(Entity faction, FactionDataStore factionDataStore)
        {
            if (_raptor != null)
                return _raptor;

            _raptor = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/raptor.json");

            return _raptor;
        }

        public static ComponentDesign RS25ThrusterDesign(Entity faction, FactionDataStore factionDataStore)
        {
            if (_rs25 != null)
                return _rs25;
            _rs25 = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/rs-25.json");
            return _rs25;
        }

        public static ComponentDesign DefaultWarpDesign(Entity faction, FactionDataStore factionDataStore)
        {
            if (_warpDrive != null)
                return _warpDrive;
            _warpDrive = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/alcuberiWhite-500.json");
            return _warpDrive;
        }

        public static ComponentDesign LargeWarpDesign(Entity faction, FactionDataStore factionDataStore)
        {
            if (_largeWarpDrive != null)
                return _largeWarpDrive;
            _largeWarpDrive = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/alcuberiWhite-2k.json");
            return _largeWarpDrive;
        }

        public static ComponentDesign DefaultFuelTank(Entity faction, FactionDataStore factionDataStore)
        {
            if (_fuelTank_1000 != null)
                return _fuelTank_1000;
            _fuelTank_1000 = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/fuelTank-1000.json");
            return _fuelTank_1000;
        }

        public static ComponentDesign LargeFuelTank(Entity faction, FactionDataStore factionDataStore)
        {
            if (_fuelTank_2500 != null)
                return _fuelTank_2500;
            _fuelTank_2500 = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/fuelTank-1500.json");
            return _fuelTank_2500;
        }

        public static ComponentDesign VLargeFuelTank(Entity faction, FactionDataStore factionDataStore)
        {
            if (_fuelTank_3000 != null)
                return _fuelTank_3000;
            _fuelTank_3000 = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/fuelTank-3000.json");
            return _fuelTank_3000;
        }

        public static ComponentDesign DefaultSimpleLaser(Entity faction, FactionDataStore factionDataStore)
        {
            if (_laser != null)
                return _laser;
            _laser = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/laserWeapon.json");
            return _laser;

        }
        public static ComponentDesign DefaultFragPayload(Entity faction, FactionDataStore factionDataStore)
        {
            if (_payload != null)
                return _payload;
            _payload = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/proxFrag-5kg.json");
            return _payload;
        }

        public static ComponentDesign DefaultMissileSensors(Entity faction, FactionDataStore factionDataStore)
        {
            if (_missileSuite != null)
                return _missileSuite;
            _missileSuite = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/passiveYellow-1mp.json");
            return _missileSuite;
        }
        public static ComponentDesign DefaultMissileSRB(Entity faction, FactionDataStore factionDataStore)
        {
            if (_missileSRB != null)
                return _missileSRB;
            _missileSRB = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/srb-235.json");
            return _missileSRB;
        }

        public static ComponentDesign DefaultMissileTube(Entity faction, FactionDataStore factionDataStore)
        {
            if (_missileTube != null)
                return _missileTube;
            _missileTube = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/missileTube-500.json");
            return _missileTube;
        }

        public static ComponentDesign DefaultBFC(Entity faction, FactionDataStore factionDataStore)
        {
            if (_fireControl != null)
                return _fireControl;
            _fireControl = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/beamFireControl.json");
            return _fireControl;
        }

        public static ComponentDesign DefaultCargoInstallation(Entity faction, FactionDataStore factionDataStore)
        {
            if (_cargoInstallation != null)
                return _cargoInstallation;
            _cargoInstallation = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/cargoHold-general.json");
            return _cargoInstallation;
        }

        public static ComponentDesign DefaultFisionReactor(Entity faction, FactionDataStore factionDataStore)
        {
            if (_reactor != null)
                return _reactor;
            _reactor = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/reactor-15k.json");
            return _reactor;
        }

        public static ComponentDesign DefaultBatteryBank(Entity faction, FactionDataStore factionDataStore)
        {
            if (_battery != null)
                return _battery;
            _battery = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/battery-2t.json");
            return _battery;
        }

        public static ComponentDesign ShipDefaultCargoHold(Entity faction, FactionDataStore factionDataStore)
        {
            if (_cargoHold != null)
                return _cargoHold;
            _cargoHold = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/cargoHold-5t.json");
            return _cargoHold;
        }

        public static ComponentDesign ShipSmallCargo(Entity faction, FactionDataStore factionDataStore)
        {
            if (_cargoCompartment != null)
                return _cargoCompartment;
            _cargoCompartment = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/cargoHold-1t.json");
            return _cargoCompartment;
        }
        public static ComponentDesign ShipSmallOrdnanceStore(Entity faction, FactionDataStore factionDataStore)
        {
            if (_ordnanceStore != null)
                return _ordnanceStore;
            _ordnanceStore = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/ordnanceRack-2.5t.json");
            return _ordnanceStore;
        }

        public static ComponentDesign ShipPassiveSensor(Entity faction, FactionDataStore factionDataStore)
        {
            if (_sensor_50 != null)
                return _sensor_50;
            _sensor_50 = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/passiveScannerS50.json");
            return _sensor_50;

        }

        public static ComponentDesign FacPassiveSensor(Entity faction, FactionDataStore factionDataStore)
        {
            if (_sensorInstallation != null)
                return _sensorInstallation;

            _sensorInstallation = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/passiveScanner.json");
            return _sensorInstallation;

        }

        public static ComponentDesign DefaultGeoSurveyor(Entity faction, FactionDataStore factionDataStore)
        {
            if (_geoSurveyor != null)
                return _geoSurveyor;
            _geoSurveyor = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/geoSurveyor.json");
            return _geoSurveyor;
        }

        public static ComponentDesign DefaultJPSurveyor(Entity faction, FactionDataStore factionDataStore)
        {
            if (_jpSurveyor != null)
                return _jpSurveyor;
            _jpSurveyor = ComponentDesignFromJson.Create(faction, factionDataStore, "Data/basemod/componentDesigns/jpSurveyor.json");
            return _jpSurveyor;
        }
    }

}