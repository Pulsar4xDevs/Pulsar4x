using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Security.Cryptography.X509Certificates;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;

namespace Pulsar4X.ECSLib
{
    public static class DefaultStartFactory
    {
        private static ComponentDesign _thruster500;
        private static ComponentDesign _warpDrive;
        private static ComponentDesign _fuelTank_500;
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
        private static ComponentDesign _spacePort;
        private static ShipDesign _defaultShipDesign;
        

        // this code is a test for multiple systems, worth mentioning it utterly failed, modularity is good when you have it huh.ç
        //TODO: try further tests at smaller distances between systems, create own starSystemFactory function for testing.
        private static Entity completeTest(Game game, string name){
            var log = StaticRefLib.EventLog;
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem solSys = starfac.CreateSol(game);
            //sol.ManagerSubpulses.Init(sol);
            Entity earth = solSys.Entities[3]; //should be fourth entity created 
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);
            Entity factionEntity = FactionFactory.CreateFaction(game, name);
            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);


            /*
            GalaxyFactory GalaxyGen = game.GalaxyGen;
            SystemBodyFactory _systemBodyFactory = new SystemBodyFactory(GalaxyGen);

            SystemBodyInfoDB halleysBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Comet, SupportsPopulations = false, Albedo = 0.04f  }; //Albedo = 0.04f 
            MassVolumeDB halleysMVDB = MassVolumeDB.NewFromMassAndRadius_AU(2.2e14, Distance.KmToAU(11));
            NameDB halleysNameDB = new NameDB("testName");
            double halleysSemiMajAxis = 17.834; //AU
            double halleysEccentricity = 0.96714;
            double halleysInclination = 180; //162.26° note retrograde orbit.
            double halleysLoAN = 58.42; //°
            double halleysAoP = 111.33;//°
            double halleysMeanAnomaly = 38.38;//°
            OrbitDB halleysOrbitDB = OrbitDB.FromAsteroidFormat(solSys.Entities[0], solSys.Entities[0].GetDataBlob<MassVolumeDB>().Mass, halleysMVDB.Mass, halleysSemiMajAxis, halleysEccentricity, halleysInclination, halleysLoAN, halleysAoP, halleysMeanAnomaly, new System.DateTime(1994, 2, 17));
            PositionDB halleysPositionDB = new PositionDB(0,0,0, solSys.ID, solSys.Entities[0]); // + earthPositionDB.AbsolutePosition_AU, sol.ID);
            SensorProfileDB sensorProfile = new SensorProfileDB();
            Entity halleysComet = new Entity(solSys, new List<BaseDataBlob> { halleysPositionDB, halleysNameDB });
            //_systemBodyFactory.MineralGeneration(game.StaticData, solSys, halleysComet);
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, halleysBodyDB, halleysMVDB);*/

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
            DefaultCargoInstalation(game, factionEntity);
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
            EntityManipulation.AddComponentToEntity(colonyEntity, _sensorInstalation);
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
            EntityManipulation.AddComponentToEntity(colonyEntity2, _sensorInstalation);
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
            var log = StaticRefLib.EventLog;
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem solSys = starfac.CreateSol(game);
            //sol.ManagerSubpulses.Init(sol);
            Entity solStar = solSys.Entities[0];
            Entity earth = solSys.Entities[3]; //should be fourth entity created 
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);
            Entity factionEntity = FactionFactory.CreateFaction(game, name);
            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);

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
            DefaultWarpDesign(game, factionEntity);
            DefaultFuelTank(game, factionEntity);
            DefaultCargoInstalation(game, factionEntity);
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
            
            EntityManipulation.AddComponentToEntity(colonyEntity, mineDesign);
            EntityManipulation.AddComponentToEntity(colonyEntity, refinaryDesign);
            EntityManipulation.AddComponentToEntity(colonyEntity, labEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, facEntity);
           
            EntityManipulation.AddComponentToEntity(colonyEntity, _fuelTank_500);
            
            EntityManipulation.AddComponentToEntity(colonyEntity, _cargoInstalation);
            EntityManipulation.AddComponentToEntity(marsColony, _cargoInstalation);
            
            EntityManipulation.AddComponentToEntity(colonyEntity, _sensorInstalation);
            EntityManipulation.AddComponentToEntity(colonyEntity, SpacePort(factionEntity));
            ReCalcProcessor.ReCalcAbilities(colonyEntity);


            colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            var rawSorium = NameLookup.GetMineralSD(game, "Sorium");
            StorageSpaceProcessor.AddCargo(colonyEntity.GetDataBlob<CargoStorageDB>(), rawSorium, 5000);
            var iron = NameLookup.GetMineralSD(game, "Iron");
            StorageSpaceProcessor.AddCargo(colonyEntity.GetDataBlob<CargoStorageDB>(), iron, 5000);
            var hydrocarbon = NameLookup.GetMineralSD(game, "Hydrocarbons");
            StorageSpaceProcessor.AddCargo(colonyEntity.GetDataBlob<CargoStorageDB>(), hydrocarbon, 5000);
            var stainless = NameLookup.GetMaterialSD(game, "Stainless Steel");
            StorageSpaceProcessor.AddCargo(colonyEntity.GetDataBlob<CargoStorageDB>(), stainless, 1000);
            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(solSys.Guid);

            

            //test systems
            //factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateEccTest(game).ID);
            //factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateLongitudeTest(game).ID);


            factionEntity.GetDataBlob<NameDB>().SetName(factionEntity.Guid, "UEF");


            // Todo: handle this in CreateShip
            ShipDesign shipDesign = DefaultShipDesign(game, factionEntity);
            ShipDesign gunShipDesign = GunShipDesign(game, factionEntity);

            Entity ship1 = ShipFactory.CreateShip(shipDesign, factionEntity, earth, solSys, "Serial Peacemaker");
            Entity ship2 = ShipFactory.CreateShip(shipDesign, factionEntity, earth, solSys, "Ensuing Calm");
            Entity ship3 = ShipFactory.CreateShip(shipDesign, factionEntity, earth, solSys, "Touch-and-Go");
            Entity gunShip = ShipFactory.CreateShip(gunShipDesign, factionEntity, earth, solSys, "Prevailing Stillness");
            Entity courier = ShipFactory.CreateShip(CargoShipDesign(game, factionEntity), factionEntity, earth, solSys, "Planet Express Ship");
            var fuel = NameLookup.GetMaterialSD(game, "Sorium Fuel");
            var rp1 = NameLookup.GetMaterialSD(game, "LOX/Hydrocarbon");
            StorageSpaceProcessor.AddCargo(ship1.GetDataBlob<CargoStorageDB>(), rp1, 15000);
            StorageSpaceProcessor.AddCargo(ship2.GetDataBlob<CargoStorageDB>(), rp1, 15000);
            StorageSpaceProcessor.AddCargo(ship3.GetDataBlob<CargoStorageDB>(), rp1, 15000);
            StorageSpaceProcessor.AddCargo(gunShip.GetDataBlob<CargoStorageDB>(), rp1, 15000);
            StorageSpaceProcessor.AddCargo(courier.GetDataBlob<CargoStorageDB>(), rp1, 15000);
            var elec = NameLookup.GetMaterialSD(game, "Electrical Energy");
            ship1.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            ship2.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            ship3.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            gunShip.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            courier.GetDataBlob<EnergyGenAbilityDB>().EnergyStored[elec.ID] = 2750;
            
            
            
            NewtonionMovementProcessor.CalcDeltaV(ship1);
            NewtonionMovementProcessor.CalcDeltaV(ship2);
            NewtonionMovementProcessor.CalcDeltaV(ship3);
            NewtonionMovementProcessor.CalcDeltaV(gunShip);
            NewtonionMovementProcessor.CalcDeltaV(courier);
            //StorageSpaceProcessor.AddCargo(ship1.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            //StorageSpaceProcessor.AddCargo(ship2.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            //StorageSpaceProcessor.AddCargo(ship3.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            

            double test_a = 0.5; //AU
            double test_e = 0;
            double test_i = 0;      //°
            double test_loan = 0;   //°
            double test_aop = 0;    //°
            double test_M0 = 0;     //°
            double test_bodyMass = ship2.GetDataBlob<MassVolumeDB>().Mass;
            OrbitDB testOrbtdb_ship2 = OrbitDB.FromAsteroidFormat(solStar, solStar.GetDataBlob<MassVolumeDB>().Mass, test_bodyMass, test_a, test_e, test_i, test_loan, test_aop, test_M0, StaticRefLib.CurrentDateTime);
            ship2.RemoveDataBlob<OrbitDB>();
            ship2.SetDataBlob(testOrbtdb_ship2);
            ship2.GetDataBlob<PositionDB>().SetParent(solStar);
            StaticRefLib.ProcessorManager.RunProcessOnEntity<OrbitDB>(ship2, 0);

            test_a = 0.51;
            test_i = 180;
            test_aop = 0;
            OrbitDB testOrbtdb_ship3 = OrbitDB.FromAsteroidFormat(solStar, solStar.GetDataBlob<MassVolumeDB>().Mass, test_bodyMass, test_a, test_e, test_i, test_loan, test_aop, test_M0, StaticRefLib.CurrentDateTime);
            ship3.RemoveDataBlob<OrbitDB>();
            ship3.SetDataBlob(testOrbtdb_ship3);
            ship3.GetDataBlob<PositionDB>().SetParent(solStar);
            StaticRefLib.ProcessorManager.RunProcessOnEntity<OrbitDB>(ship3, 0);

            
            gunShip.GetDataBlob<PositionDB>().RelativePosition_AU = new Vector3(8.52699302490434E-05, 0, 0);
            //give the gunship a hypobolic orbit to test:
            //var orbit = OrbitDB.FromVector(earth, gunShip, new Vector4(0, velInAU, 0, 0), game.CurrentDateTime);
            gunShip.RemoveDataBlob<OrbitDB>();
            var nmdb = new NewtonMoveDB(earth, new Vector3(0, -10000.0, 0));
            gunShip.SetDataBlob<NewtonMoveDB>(nmdb);



            solSys.SetDataBlob(ship1.ID, new TransitableDB());
            solSys.SetDataBlob(ship2.ID, new TransitableDB());
            solSys.SetDataBlob(gunShip.ID, new TransitableDB());
            solSys.SetDataBlob(courier.ID, new TransitableDB());

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

        public static ShipDesign GunShipDesign(Game game, Entity faction)
        {
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (_sensor_50, 1), 
                (_laser, 4),     
                (_fireControl, 2),
                (_fuelTank_500, 2),
                (_warpDrive, 4),
                (_battery, 3),
                (_reactor, 1),
                (_thruster500, 4),
            };
            ArmorSD plastic = game.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            var shipdesign = new ShipDesign(factionInfo, "Sanctum Adroit GunShip", components2, (plastic, 3));
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
                (DefaultFuelTank(game, faction), 2),
                (_cargoHold, 1),   
                (_warpDrive, 4),
                (_battery, 3),
                (_reactor, 1),
                (_thruster500, 4),
            };
            ArmorSD plastic = game.StaticData.ArmorTypes[new Guid("207af637-95a0-4b89-ac4a-6d66a81cfb2f")];
            var shipdesign = new ShipDesign(factionInfo, "Cargo Courier", components2, (plastic, 3));
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
        }

        public static ComponentDesign SpacePort(Entity faction)
        {
            if (_spacePort != null)
                return _spacePort;
            ComponentDesigner spacePortDesigner;
            ComponentTemplateSD spaceportSD = StaticRefLib.StaticData.ComponentTemplates[new Guid("0BD304FF-FDEA-493C-8979-15FE86B7123E")];
            spacePortDesigner = new ComponentDesigner(spaceportSD, faction.GetDataBlob<FactionTechDB>());
            spacePortDesigner.Name = "Space Port";
            _spacePort = spacePortDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_spacePort.TechID);
            return _spacePort;
        }

        public static ComponentDesign DefaultThrusterDesign(Game game, Entity faction)
        {
            if (_thruster500 != null)
                return _thruster500;
            
            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("b12f50f6-ac68-4a49-b147-281a9bb34b9b")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(500); 
            engineDesigner.Name = "Thruster 500";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput
   
            _thruster500 = engineDesigner.CreateDesign(faction);
            
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_thruster500.TechID);
            return _thruster500;
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

        public static ComponentDesign DefaultFuelTank(Game game, Entity faction)
        {
            if (_fuelTank_500 != null)
                return _fuelTank_500;
            ComponentDesigner fuelTankDesigner;
            ComponentTemplateSD tankSD = game.StaticData.ComponentTemplates[new Guid("E7AC4187-58E4-458B-9AEA-C3E07FC993CB")];
            fuelTankDesigner = new ComponentDesigner(tankSD, faction.GetDataBlob<FactionTechDB>());
            fuelTankDesigner.ComponentDesignAttributes["Tank Size"].SetValueFromInput(2500);
            fuelTankDesigner.Name = "Tank-500";
            _fuelTank_500 = fuelTankDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_fuelTank_500.TechID);
            return _fuelTank_500;
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
            suiteDesigner.ComponentDesignAttributes["Sensor Mass"].SetValueFromInput(10);
            suiteDesigner.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(600);
            suiteDesigner.ComponentDesignAttributes["Detection Wavelength Width"].SetValueFromInput(100);
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
            srbDesigner.ComponentDesignAttributes["Engine Mass"].SetValueFromInput(1);
            srbDesigner.ComponentDesignAttributes["Fuel Mass"].SetValueFromInput(199);
            srbDesigner.Name = "SRB 200";
            _missileSRB = srbDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_missileSRB.TechID);
            return _missileSRB;
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

        public static ComponentDesign DefaultCargoInstalation(Game game, Entity faction)
        {
            ComponentDesigner componentDesigner;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{30cd60f8-1de3-4faa-acba-0933eb84c199}")];
            componentDesigner = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes["Warehouse Size"].SetValueFromInput(1000000);
            componentDesigner.Name = "CargoInstalation1";
            //return cargoInstalation.CreateDesign(faction);
            _cargoInstalation = componentDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_cargoInstalation.TechID);
            return _cargoInstalation;
        }
        
        public static ComponentDesign DefaultFisionReactor(Game game, Entity faction)
        {
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
            ComponentDesigner componentDesigner;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{1de23a8b-d44b-4e0f-bacd-5463a8eb939d}")];
            componentDesigner = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes["Mass"].SetValueFromInput(1000);
            componentDesigner.Name = "Battery900";
            //return cargoInstalation.CreateDesign(faction);
            _battery = componentDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_battery.TechID);
            return _battery;
        }

        public static ComponentDesign ShipDefaultCargoHold(Game game, Entity faction)
        {
            if (_cargoHold != null)
                return _cargoHold;
            ComponentDesigner cargoComponent;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{30cd60f8-1de3-4faa-acba-0933eb84c199}")];
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
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{30cd60f8-1de3-4faa-acba-0933eb84c199}")];
            cargoComponent = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            cargoComponent.ComponentDesignAttributes["Warehouse Size"].SetValueFromInput(1000); //5t component
            cargoComponent.ComponentDesignAttributes["Cargo Transfer Rate"].SetValueFromInput(500);
            cargoComponent.ComponentDesignAttributes["Transfer Range"].SetValueFromInput(100);
            cargoComponent.Name = "CargoComponent1t";
            _cargoCompartment = cargoComponent.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_cargoCompartment.TechID);
            return _cargoCompartment;
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