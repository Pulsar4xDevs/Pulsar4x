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
        private static ComponentDesign _engine500;
        private static ComponentDesign _warpDrive;
        private static ComponentDesign _fuelTank_500;
        private static ComponentDesign _laser;
        private static ComponentDesign _sensor_50;
        private static ComponentDesign _sensorInstalation;
        private static ComponentDesign _fireControl;
        private static ComponentDesign _cargoInstalation;
        private static ComponentDesign _reactor;
        private static ComponentDesign _battery;
        private static ComponentDesign _cargoHold;
        private static ComponentDesign _cargoCompartment;
        private static ShipFactory.ShipClass _defaultShipClass;
        
        public static Entity DefaultHumans(Game game, string name)
        {
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
            //TechProcessor.ApplyTech(factionTech, game.StaticData.Techs[new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c")]); //add conventional engine for testing. 
            ResearchProcessor.CheckRequrements(factionTech);
            
            DefaultEngineDesign(game, factionEntity);
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
            
            EntityManipulation.AddComponentToEntity(colonyEntity, mineDesign);
            EntityManipulation.AddComponentToEntity(colonyEntity, refinaryDesign);
            EntityManipulation.AddComponentToEntity(colonyEntity, labEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, facEntity);
           
            EntityManipulation.AddComponentToEntity(colonyEntity, _fuelTank_500);
            
            EntityManipulation.AddComponentToEntity(colonyEntity, _cargoInstalation);
            EntityManipulation.AddComponentToEntity(marsColony, _cargoInstalation);
            
            EntityManipulation.AddComponentToEntity(colonyEntity, _sensorInstalation);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);


            colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            var rawSorium = NameLookup.GetMineralSD(game, "Sorium");
            StorageSpaceProcessor.AddCargo(colonyEntity.GetDataBlob<CargoStorageDB>(), rawSorium, 5000);


            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(solSys.Guid);

            //test systems
            //factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateEccTest(game).Guid);
            //factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateLongitudeTest(game).Guid);


            factionEntity.GetDataBlob<NameDB>().SetName(factionEntity.Guid, "UEF");


            // Todo: handle this in CreateShip
            ShipFactory.ShipClass shipClass = DefaultShipDesign(game, factionEntity);
            ShipFactory.ShipClass gunShipClass = GunShipDesign(game, factionEntity);

            Entity ship1 = ShipFactory.CreateShip(shipClass, factionEntity, earth, solSys, "Serial Peacemaker");
            Entity ship2 = ShipFactory.CreateShip(shipClass, factionEntity, earth, solSys, "Ensuing Calm");
            Entity ship3 = ShipFactory.CreateShip(shipClass, factionEntity, earth, solSys, "Touch-and-Go");
            var fuel = NameLookup.GetMaterialSD(game, "Sorium Fuel");
            StorageSpaceProcessor.AddCargo(ship1.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            StorageSpaceProcessor.AddCargo(ship2.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            StorageSpaceProcessor.AddCargo(ship3.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);



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


            Entity gunShip = ShipFactory.CreateShip(gunShipClass, factionEntity, earth, solSys, "Prevailing Stillness");
            gunShip.GetDataBlob<PositionDB>().RelativePosition_AU = new Vector3(8.52699302490434E-05, 0, 0);
            StorageSpaceProcessor.AddCargo(gunShip.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            //give the gunship a hypobolic orbit to test:

            //var orbit = OrbitDB.FromVector(earth, gunShip, new Vector4(0, velInAU, 0, 0), game.CurrentDateTime);
            gunShip.RemoveDataBlob<OrbitDB>();
            var nmdb = new NewtonMoveDB(earth, new Vector3(0, -10000.0, 0));
  
            gunShip.SetDataBlob<NewtonMoveDB>(nmdb);

            //Entity courier = ShipFactory.CreateShip(CargoShipDesign(game, factionEntity), factionEntity, earth, solSys, "Planet Express Ship");
            Entity courier = ShipFactory.CreateShip(CargoShipDesign(game, factionEntity), factionEntity, earth, solSys, "Planet Express Ship");
            StorageSpaceProcessor.AddCargo(courier.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);

            solSys.SetDataBlob(ship1.ID, new TransitableDB());
            solSys.SetDataBlob(ship2.ID, new TransitableDB());
            solSys.SetDataBlob(gunShip.ID, new TransitableDB());
            solSys.SetDataBlob(courier.ID, new TransitableDB());

            //Entity ship = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Serial Peacemaker");
            //ship.SetDataBlob(earth.GetDataBlob<PositionDB>()); //first ship reference PositionDB

            //Entity ship3 = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Contiual Pacifier");
            //ship3.SetDataBlob((OrbitDB)earth.GetDataBlob<OrbitDB>().Clone());//second ship clone earth OrbitDB


            //sol.SystemManager.SetDataBlob(ship.ID, new TransitableDB());

            //Entity rock = AsteroidFactory.CreateAsteroid2(sol, earth, game.CurrentDateTime + TimeSpan.FromDays(365));
            Entity rock = AsteroidFactory.CreateAsteroid(solSys, earth, StaticRefLib.CurrentDateTime + TimeSpan.FromDays(365));


            var pow = solSys.GetAllEntitiesWithDataBlob<EntityEnergyGenAbilityDB>();
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


        public static ShipFactory.ShipClass DefaultShipDesign(Game game, Entity faction)
        {
            if (_defaultShipClass != null)
                return _defaultShipClass;
            _defaultShipClass = new ShipFactory.ShipClass(faction.GetDataBlob<FactionInfoDB>());
            _defaultShipClass.DesignName = "Ob'enn dropship";
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (ShipPassiveSensor(game, faction), 1), 
                (DefaultSimpleLaser(game, faction), 2),     
                (DefaultBFC(game, faction), 1),
                (ShipSmallCargo(game, faction), 1),
                (_fuelTank_500, 2),
                (_warpDrive, 4),
                (_battery, 3),
                (_reactor, 1),
                (_engine500, 3),
                
            };
            _defaultShipClass.Components = components2;
            _defaultShipClass.Armor = ("Polyprop", 1175f, 3);
            
            _defaultShipClass.DamageProfileDB = new EntityDamageProfileDB(components2, _defaultShipClass.Armor);
            return _defaultShipClass;
        }

        public static ShipFactory.ShipClass GunShipDesign(Game game, Entity faction)
        {

            var shipdesign = new ShipFactory.ShipClass(faction.GetDataBlob<FactionInfoDB>());
            shipdesign.DesignName = "Sanctum Adroit GunShip";
            List<(ComponentDesign, int)> components2 = new List<(ComponentDesign, int)>()
            {
                (_sensor_50, 1), 
                (_laser, 4),     
                (_fireControl, 2),
                (_fuelTank_500, 2),
                (_warpDrive, 4),
                (_battery, 3),
                (_reactor, 1),
                (_engine500, 4),
            };
            shipdesign.Components = components2;
            shipdesign.Armor = ("Polyprop", 1175f, 3);
            
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
            
        }

        public static ShipFactory.ShipClass CargoShipDesign(Game game, Entity faction)
        {
            var shipdesign = new ShipFactory.ShipClass(faction.GetDataBlob<FactionInfoDB>());
            shipdesign.DesignName = "Cargo Courier";
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
                (_engine500, 4),
            };
            shipdesign.Components = components2;
            shipdesign.Armor = ("Polyprop", 1175f, 3);
            
            shipdesign.DamageProfileDB = new EntityDamageProfileDB(components2, shipdesign.Armor);
            return shipdesign;
        }

        public static ComponentDesign DefaultEngineDesign(Game game, Entity faction)
        {
            if (_engine500 != null)
                return _engine500;
            
            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("b12f50f6-ac68-4a49-b147-281a9bb34b9b")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(500); 
            engineDesigner.Name = "Thruster 500";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput
   
            _engine500 = engineDesigner.CreateDesign(faction);
            
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_engine500.TechID);
            return _engine500;
        }
        public static ComponentDesign DefaultWarpDesign(Game game, Entity faction)
        {
            if (_warpDrive != null)
                return _warpDrive;
            
            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("7d0b867f-e239-4b93-9b30-c6d4b769b5e4")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(1000); //size 500 = 2500 power
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
            componentDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(1000);
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
            componentDesigner.ComponentDesignAttributes["Size"].SetValueFromInput(1000);
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
            sensor.ComponentDesignAttributes["Sensor Size"].SetValueFromInput(500);  //size
            sensor.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(600); //best wavelength
            sensor.ComponentDesignAttributes["Detection Wavelength Width"].SetValueFromInput(250); //wavelength detection width 
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
            ComponentDesigner sensorDesigner;
            ComponentTemplateSD template = NameLookup.GetTemplateSD(game, "PassiveSensor");
            sensorDesigner = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            sensorDesigner.ComponentDesignAttributes["Sensor Size"].SetValueFromInput(5000);  //size
            sensorDesigner.ComponentDesignAttributes["Ideal Detection Wavelength"].SetValueFromInput(500); //best wavelength
            sensorDesigner.ComponentDesignAttributes["Detection Wavelength Width"].SetValueFromInput(1000); //wavelength detection width 
            //[3] best detection magnatude. (Not settable)
            //[4] worst detection magnatude (not setta[ble)
            sensorDesigner.ComponentDesignAttributes["Resolution"].SetValueFromInput(5);   //resolution
            sensorDesigner.ComponentDesignAttributes["Scan Time"].SetValueFromInput(3600);//Scan Time
            sensorDesigner.Name = "PassiveSensor-S500";
            //return sensor.CreateDesign(faction);
            _sensorInstalation = sensorDesigner.CreateDesign(faction);
            faction.GetDataBlob<FactionTechDB>().IncrementLevel(_sensorInstalation.TechID);
            return _sensorInstalation;

        }
    }

}