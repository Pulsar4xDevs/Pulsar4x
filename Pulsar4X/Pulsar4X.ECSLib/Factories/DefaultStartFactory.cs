using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Security.Cryptography.X509Certificates;

namespace Pulsar4X.ECSLib
{
    public static class DefaultStartFactory
    {
        private static ComponentDesign Engine250;
        private static ComponentDesign FuelTank_500;
        private static ComponentDesign Laser;
        private static ComponentDesign Sensor_50;
        private static ComponentDesign SensorInstalation;
        private static ComponentDesign FireControl;
        private static ComponentDesign CargoInstalation;
        private static ComponentDesign CargoCompartment;
        
        
        public static Entity DefaultHumans(Game game, string name)
        {
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

            Entity scientistEntity = CommanderFactory.CreateScientist(game.GlobalManager, factionEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Scientists.Add(scientistEntity);

            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            //TechProcessor.ApplyTech(factionTech, game.StaticData.Techs[new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c")]); //add conventional engine for testing. 
            ResearchProcessor.MakeResearchable(factionTech);
            
            DefaultEngineDesign(game, factionEntity);
            DefaultFuelTank(game, factionEntity);
            DefaultCargoInstalation(game, factionEntity);
            DefaultSimpleLaser(game, factionEntity);
            DefaultBFC(game, factionEntity);
            ShipGenericCargo(game, factionEntity);
            ShipPassiveSensor(game, factionEntity);
            FacPassiveSensor(game, factionEntity);
            
            
            EntityManipulation.AddComponentToEntity(colonyEntity, mineDesign);
            EntityManipulation.AddComponentToEntity(colonyEntity, refinaryDesign);
            EntityManipulation.AddComponentToEntity(colonyEntity, labEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, facEntity);
           
            EntityManipulation.AddComponentToEntity(colonyEntity, FuelTank_500);
            
            EntityManipulation.AddComponentToEntity(colonyEntity, CargoInstalation);
            EntityManipulation.AddComponentToEntity(marsColony, CargoInstalation);
            
            EntityManipulation.AddComponentToEntity(colonyEntity, SensorInstalation);
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
            Entity shipClass = DefaultShipDesign(game, factionEntity);
            Entity gunShipClass = GunShipDesign(game, factionEntity);

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
            StorageSpaceProcessor.AddCargo(gunShipClass.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            //give the gunship a hypobolic orbit to test:

            //var orbit = OrbitDB.FromVector(earth, gunShip, new Vector4(0, velInAU, 0, 0), game.CurrentDateTime);
            gunShip.RemoveDataBlob<OrbitDB>();
            var nmdb = new NewtonMoveDB(earth, new Vector3(0, -10000.0, 0));
  
            gunShip.SetDataBlob<NewtonMoveDB>(nmdb);

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
            Entity rock = AsteroidFactory.CreateAsteroid3(solSys, earth, StaticRefLib.CurrentDateTime + TimeSpan.FromDays(365));

            var entitiesWithSensors = solSys.GetAllEntitiesWithDataBlob<SensorAbilityDB>();
            foreach (var entityItem in entitiesWithSensors)
            {
                StaticRefLib.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem, StaticRefLib.CurrentDateTime);
            }



            return factionEntity;
        }


        public static Entity DefaultShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Ob'enn dropship");

;
            List<ComponentDesign> components = new List<ComponentDesign>()
            {
                Engine250,     //50   
                Engine250,     //50  
                Engine250,     //50  
                Engine250,     //50  
                Engine250,     //50  
                Engine250,     //50    300
                FuelTank_500,   //250   550
                FuelTank_500,   //250   800 60%
                Laser,      //10    810
                FireControl,        //10    820
                Sensor_50,     //50    870
                
            };

            EntityManipulation.AddComponentToEntity(shipDesign, components);
            return shipDesign;
        }

        public static Entity GunShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Sanctum Adroit GunShip");
    

            List<ComponentDesign> components = new List<ComponentDesign>()
            {
                Engine250,     //50
                Engine250,     //50
                Engine250,     //50
                Engine250,     //50
                FuelTank_500,   //250
                FuelTank_500,   //250 60%
                Laser,      //10
                Laser,      //10
                Laser,      //10
                Laser,      //10
                FireControl,        //10
                FireControl,        //10
                Sensor_50,     //50
                
            };

            EntityManipulation.AddComponentToEntity(shipDesign, components);
            return shipDesign;
        }

        public static Entity CargoShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Cargo Courier");


            List<ComponentDesign> components = new List<ComponentDesign>()
            {
                Engine250,     //50
                Engine250,     //50
                Engine250,     //50
                Engine250,     //50
                FuelTank_500,   //250
                FuelTank_500,   //250 60%
                Laser,      //10
                FireControl,        //10
                Sensor_50,     //50
                CargoCompartment,      //190
            };

            EntityManipulation.AddComponentToEntity(shipDesign, components);
            return shipDesign;
        }

        public static ComponentDesign DefaultEngineDesign(Game game, Entity faction)
        {
            if (Engine250 != null)
                return Engine250;
            
            ComponentDesigner engineDesigner;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6")];
            engineDesigner = new ComponentDesigner(engineSD, faction.GetDataBlob<FactionTechDB>());
            engineDesigner.ComponentDesignAttributes[0].SetValueFromInput(500); //size 500 = 2500 power
            engineDesigner.Name = "DefaultEngine-250";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput
   
            Engine250 = engineDesigner.CreateDesign(faction);
            return Engine250;
        }

        public static ComponentDesign DefaultFuelTank(Game game, Entity faction)
        {
            if (FuelTank_500 != null)
                return FuelTank_500;
            ComponentDesigner fuelTankDesigner;
            ComponentTemplateSD tankSD = game.StaticData.ComponentTemplates[new Guid("E7AC4187-58E4-458B-9AEA-C3E07FC993CB")];
            fuelTankDesigner = new ComponentDesigner(tankSD, faction.GetDataBlob<FactionTechDB>());
            fuelTankDesigner.ComponentDesignAttributes[0].SetValueFromInput(2500);
            fuelTankDesigner.Name = "Tank-500";

            return FuelTank_500 = fuelTankDesigner.CreateDesign(faction);
        }

        public static ComponentDesign DefaultSimpleLaser(Game game, Entity faction)
        {
            if (Laser != null)
                return Laser;
            ComponentDesigner laserDesigner;
            ComponentTemplateSD laserSD = game.StaticData.ComponentTemplates[new Guid("8923f0e1-1143-4926-a0c8-66b6c7969425")];
            laserDesigner = new ComponentDesigner(laserSD, faction.GetDataBlob<FactionTechDB>());
            laserDesigner.ComponentDesignAttributes[0].SetValueFromInput(100);
            laserDesigner.ComponentDesignAttributes[1].SetValueFromInput(5000);
            laserDesigner.ComponentDesignAttributes[2].SetValueFromInput(5);

            return Laser = laserDesigner.CreateDesign(faction);

        }

        public static void DefaultBFC(Game game, Entity faction)
        {
            ComponentDesigner fireControlDesigner;
            ComponentTemplateSD bfcSD = game.StaticData.ComponentTemplates[new Guid("33fcd1f5-80ab-4bac-97be-dbcae19ab1a0")];
            fireControlDesigner = new ComponentDesigner(bfcSD, faction.GetDataBlob<FactionTechDB>());
            fireControlDesigner.ComponentDesignAttributes[0].SetValueFromInput(100);
            fireControlDesigner.ComponentDesignAttributes[1].SetValueFromInput(5000);
            fireControlDesigner.ComponentDesignAttributes[2].SetValueFromInput(1);

            //return fireControlDesigner.CreateDesign(faction);
            FireControl = fireControlDesigner.CreateDesign(faction);
        }

        public static void DefaultCargoInstalation(Game game, Entity faction)
        {
            ComponentDesigner componentDesigner;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{30cd60f8-1de3-4faa-acba-0933eb84c199}")];
            componentDesigner = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            componentDesigner.ComponentDesignAttributes[0].SetValueFromInput(1000000);
            componentDesigner.Name = "CargoInstalation1";
            //return cargoInstalation.CreateDesign(faction);
            CargoInstalation = componentDesigner.CreateDesign(faction);
        }

        public static void ShipGenericCargo(Game game, Entity faction)
        { 
            ComponentDesigner cargoComponent;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{30cd60f8-1de3-4faa-acba-0933eb84c199}")];
            cargoComponent = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            cargoComponent.ComponentDesignAttributes[0].SetValueFromInput(5000); //5t component
            cargoComponent.ComponentDesignAttributes[2].SetValueFromInput(500);
            cargoComponent.ComponentDesignAttributes[3].SetValueFromInput(100);
            cargoComponent.Name = "CargoComponent5t";
            //return cargoComponent.CreateDesign(faction);
            CargoCompartment = cargoComponent.CreateDesign(faction);
        }

        public static void ShipPassiveSensor(Game game, Entity faction)
        {
            ComponentDesigner sensor;
            ComponentTemplateSD template = NameLookup.GetTemplateSD(game, "PassiveSensor");
            sensor = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            sensor.ComponentDesignAttributes[0].SetValueFromInput(500);  //size
            sensor.ComponentDesignAttributes[1].SetValueFromInput(600); //best wavelength
            sensor.ComponentDesignAttributes[2].SetValueFromInput(250); //wavelength detection width 
            //sensor.ComponentDesignAttributes[3].SetValueFromInput(10);  //best detection magnatude. (Not settable)
                                                                        //[4] worst detection magnatude (not settable)
            sensor.ComponentDesignAttributes[5].SetValueFromInput(1);   //resolution
            sensor.ComponentDesignAttributes[6].SetValueFromInput(3600);//Scan Time
            sensor.Name = "PassiveSensor-S50";
            //return sensor.CreateDesign(faction);
            Sensor_50 = sensor.CreateDesign(faction);

        }

        public static void FacPassiveSensor(Game game, Entity faction)
        {
            ComponentDesigner sensorDesigner;
            ComponentTemplateSD template = NameLookup.GetTemplateSD(game, "PassiveSensor");
            sensorDesigner = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            sensorDesigner.ComponentDesignAttributes[0].SetValueFromInput(5000);  //size
            sensorDesigner.ComponentDesignAttributes[1].SetValueFromInput(500); //best wavelength
            sensorDesigner.ComponentDesignAttributes[2].SetValueFromInput(1000); //wavelength detection width 
            //[3] best detection magnatude. (Not settable)
            //[4] worst detection magnatude (not settable)
            sensorDesigner.ComponentDesignAttributes[5].SetValueFromInput(5);   //resolution
            sensorDesigner.ComponentDesignAttributes[6].SetValueFromInput(3600);//Scan Time
            sensorDesigner.Name = "PassiveSensor-S500";
            //return sensor.CreateDesign(faction);
            SensorInstalation = sensorDesigner.CreateDesign(faction);

        }

        public static ComponentDesign DeadWeight(Game game, Entity faction, int weight)
        {
            ComponentDesigner deadTestWeight;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{57614ddb-0756-44cf-857b-8a6578493792}")];
            deadTestWeight = new ComponentDesigner(template, faction.GetDataBlob<FactionTechDB>());
            deadTestWeight.ComponentDesignAttributes[0].SetValueFromInput(weight);
            deadTestWeight.Name = "DeadWeight-" + weight;
            return deadTestWeight.CreateDesign(faction);

        }

    }

}