using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Pulsar4X.ECSLib
{
    public static class DefaultStartFactory
    {
        public static Entity DefaultHumans(Game game, string name)
        {
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem sol = starfac.CreateSol(game);
            //sol.ManagerSubpulses.Init(sol);
            Entity earth = sol.Entities[3]; //should be fourth entity created 
            //Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);
            Entity factionEntity = FactionFactory.CreateFaction(game, name);
            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);

            var namedEntites = sol.GetAllEntitiesWithDataBlob<NameDB>();
            foreach (var entity in namedEntites)
            {
                var nameDB = entity.GetDataBlob<NameDB>();
                nameDB.SetName(factionEntity.Guid, nameDB.DefaultName);
            }

            Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth);
            Entity marsColony = ColonyFactory.CreateColony(factionEntity, speciesEntity, NameLookup.GetFirstEntityWithName(sol, "Mars"));

            ComponentTemplateSD mineSD = game.StaticData.ComponentTemplates[new Guid("f7084155-04c3-49e8-bf43-c7ef4befa550")];
            ComponentDesign mineDesign = GenericComponentFactory.StaticToDesign(mineSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity mineEntity = GenericComponentFactory.DesignToDesignEntity(game, factionEntity, mineDesign);


            ComponentTemplateSD RefinerySD = game.StaticData.ComponentTemplates[new Guid("90592586-0BD6-4885-8526-7181E08556B5")];
            ComponentDesign RefineryDesign = GenericComponentFactory.StaticToDesign(RefinerySD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity RefineryEntity = GenericComponentFactory.DesignToDesignEntity(game, factionEntity, RefineryDesign);

            ComponentTemplateSD labSD = game.StaticData.ComponentTemplates[new Guid("c203b7cf-8b41-4664-8291-d20dfe1119ec")];
            ComponentDesign labDesign = GenericComponentFactory.StaticToDesign(labSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity labEntity = GenericComponentFactory.DesignToDesignEntity(game, factionEntity, labDesign);

            ComponentTemplateSD facSD = game.StaticData.ComponentTemplates[new Guid("{07817639-E0C6-43CD-B3DC-24ED15EFB4BA}")];
            ComponentDesign facDesign = GenericComponentFactory.StaticToDesign(facSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity facEntity = GenericComponentFactory.DesignToDesignEntity(game, factionEntity, facDesign);

            Entity scientistEntity = CommanderFactory.CreateScientist(game.GlobalManager, factionEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Scientists.Add(scientistEntity);

            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            //TechProcessor.ApplyTech(factionTech, game.StaticData.Techs[new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c")]); //add conventional engine for testing. 
            ResearchProcessor.MakeResearchable(factionTech);
            Entity fuelTank = DefaultFuelTank(game, factionEntity);
            Entity cargoInstalation = DefaultCargoInstalation(game, factionEntity);

            EntityManipulation.AddComponentToEntity(colonyEntity, mineEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, RefineryEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, labEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, facEntity);
           
            EntityManipulation.AddComponentToEntity(colonyEntity, fuelTank);
            
            EntityManipulation.AddComponentToEntity(colonyEntity, cargoInstalation);
            EntityManipulation.AddComponentToEntity(marsColony, cargoInstalation);
            Entity colonySensor = FacPassiveSensor(game, factionEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, colonySensor);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);


            colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            var rawSorium = NameLookup.GetMineralSD(game, "Sorium");
            StorageSpaceProcessor.AddCargo(colonyEntity.GetDataBlob<CargoStorageDB>(), rawSorium, 5000);


            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(sol.Guid);

            //test systems
            //factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateEccTest(game).Guid);
            //factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateLongitudeTest(game).Guid);


            factionEntity.GetDataBlob<NameDB>().SetName(factionEntity.Guid, "UEF");


            // Todo: handle this in CreateShip
            Entity shipClass = DefaultShipDesign(game, factionEntity);
            Entity gunShipClass = GunShipDesign(game, factionEntity);

            Entity ship1 = ShipFactory.CreateShip(shipClass, sol, factionEntity, earth, sol, "Serial Peacemaker");
            Entity ship2 = ShipFactory.CreateShip(shipClass, sol, factionEntity, earth, sol, "Ensuing Calm");
            var fuel = NameLookup.GetMaterialSD(game, "Sorium Fuel");
            StorageSpaceProcessor.AddCargo(ship1.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            StorageSpaceProcessor.AddCargo(ship2.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);


            Entity gunShip = ShipFactory.CreateShip(gunShipClass, sol, factionEntity, earth, sol, "Prevailing Stillness");
            gunShip.GetDataBlob<PositionDB>().RelativePosition_AU = new Vector4(8.52699302490434E-05, 0, 0, 0);
            StorageSpaceProcessor.AddCargo(gunShipClass.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);
            //give the gunship a parabolic orbit to test:
            var velInAU = Distance.KmToAU(25);
            //var orbit = OrbitDB.FromVector(earth, gunShip, new Vector4(0, velInAU, 0, 0), game.CurrentDateTime);
            gunShip.RemoveDataBlob<OrbitDB>();
            var nmdb = new NewtonMoveDB(earth);
            nmdb.CurrentVector_kms = new Vector4(0, 5, 0, 0);
            gunShip.SetDataBlob<NewtonMoveDB>(nmdb);

            Entity courier = ShipFactory.CreateShip(CargoShipDesign(game, factionEntity), sol, factionEntity, earth, sol, "Planet Express Ship");
            StorageSpaceProcessor.AddCargo(courier.GetDataBlob<CargoStorageDB>(), fuel, 200000000000);

            sol.SetDataBlob(ship1.ID, new TransitableDB());
            sol.SetDataBlob(ship2.ID, new TransitableDB());
            sol.SetDataBlob(gunShip.ID, new TransitableDB());
            sol.SetDataBlob(courier.ID, new TransitableDB());

            //Entity ship = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Serial Peacemaker");
            //ship.SetDataBlob(earth.GetDataBlob<PositionDB>()); //first ship reference PositionDB

            //Entity ship3 = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Contiual Pacifier");
            //ship3.SetDataBlob((OrbitDB)earth.GetDataBlob<OrbitDB>().Clone());//second ship clone earth OrbitDB


            //sol.SystemManager.SetDataBlob(ship.ID, new TransitableDB());

            //Entity rock = AsteroidFactory.CreateAsteroid2(sol, earth, game.CurrentDateTime + TimeSpan.FromDays(365));
            Entity rock = AsteroidFactory.CreateAsteroid3(sol, earth, game.CurrentDateTime + TimeSpan.FromDays(365));

            var entitiesWithSensors = sol.GetAllEntitiesWithDataBlob<SensorReceverAtbDB>();
            foreach (var entityItem in entitiesWithSensors)
            {
                if(entityItem.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity != null) //don't do the designs, just the actual physical entity components. 
                    game.ProcessorManager.GetInstanceProcessor(nameof(SensorScan)).ProcessEntity(entityItem, game.CurrentDateTime);
            }



            return factionEntity;
        }


        public static Entity DefaultShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Ob'enn dropship");
            Entity engine = DefaultEngineDesign(game, faction);
            Entity fuelTank = DefaultFuelTank(game, faction);
            Entity laser = DefaultSimpleLaser(game, faction);
            Entity bfc = DefaultBFC(game, faction);
            Entity sensor = ShipPassiveSensor(game, faction);
            Entity deadWeight = DeadWeight(game, faction, 1300);
            List<Entity> components = new List<Entity>()
            {
                engine,     //50   
                engine,     //50  
                engine,     //50  
                engine,     //50  
                engine,     //50  
                engine,     //50    300
                fuelTank,   //250   550
                fuelTank,   //250   800 60%
                laser,      //10    810
                bfc,        //10    820
                sensor,     //50    870
                deadWeight  //130   1000
            };

            EntityManipulation.AddComponentToEntity(shipDesign, components, faction.Guid);
            return shipDesign;
        }

        public static Entity GunShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Sanctum Adroit GunShip");
            Entity engine = DefaultEngineDesign(game, faction);
            Entity fuelTank = DefaultFuelTank(game, faction);
            Entity laser = DefaultSimpleLaser(game, faction);
            Entity bfc = DefaultBFC(game, faction);
            Entity deadWeight = DeadWeight(game, faction, 1900);
            Entity sensor = ShipPassiveSensor(game, faction);
            List<Entity> components = new List<Entity>()
            {
                engine,     //50
                engine,     //50
                engine,     //50
                engine,     //50
                fuelTank,   //250
                fuelTank,   //250 60%
                laser,      //10
                laser,      //10
                laser,      //10
                laser,      //10
                bfc,        //10
                bfc,        //10
                sensor,     //50
                deadWeight, //190
            };

            EntityManipulation.AddComponentToEntity(shipDesign, components, faction.Guid);
            return shipDesign;
        }

        public static Entity CargoShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Cargo Courier");
            Entity engine = DefaultEngineDesign(game, faction);
            Entity fuelTank = DefaultFuelTank(game, faction);
            Entity laser = DefaultSimpleLaser(game, faction);
            Entity bfc = DefaultBFC(game, faction);
            Entity sensor = ShipPassiveSensor(game, faction);
            Entity cargo = ShipGenericCargo(game, faction);

            List<Entity> components = new List<Entity>()
            {
                engine,     //50
                engine,     //50
                engine,     //50
                engine,     //50
                fuelTank,   //250
                fuelTank,   //250 60%
                laser,      //10
                bfc,        //10
                sensor,     //50
                cargo,      //190
            };

            EntityManipulation.AddComponentToEntity(shipDesign, components, faction.Guid);
            return shipDesign;
        }

        public static Entity DefaultEngineDesign(Game game, Entity faction)
        {
            ComponentDesign engineDesign;

            ComponentTemplateSD engineSD = game.StaticData.ComponentTemplates[new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6")];
            engineDesign = GenericComponentFactory.StaticToDesign(engineSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            engineDesign.ComponentDesignAttributes[0].SetValueFromInput(500); //size 500 = 2500 power
            engineDesign.Name = "DefaultEngine-250";
            //engineDesignDB.ComponentDesignAbilities[1].SetValueFromInput
            return GenericComponentFactory.DesignToDesignEntity(game, faction, engineDesign);
        }

        public static Entity DefaultFuelTank(Game game, Entity faction)
        {
            ComponentDesign fuelTankDesign;
            ComponentTemplateSD tankSD = game.StaticData.ComponentTemplates[new Guid("E7AC4187-58E4-458B-9AEA-C3E07FC993CB")];
            fuelTankDesign = GenericComponentFactory.StaticToDesign(tankSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            fuelTankDesign.ComponentDesignAttributes[0].SetValueFromInput(2500);
            fuelTankDesign.Name = "Tank-500";
            return GenericComponentFactory.DesignToDesignEntity(game, faction, fuelTankDesign);
        }

        public static Entity DefaultSimpleLaser(Game game, Entity faction)
        {
            ComponentDesign laserDesign;
            ComponentTemplateSD laserSD = game.StaticData.ComponentTemplates[new Guid("8923f0e1-1143-4926-a0c8-66b6c7969425")];
            laserDesign = GenericComponentFactory.StaticToDesign(laserSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            laserDesign.ComponentDesignAttributes[0].SetValueFromInput(100);
            laserDesign.ComponentDesignAttributes[1].SetValueFromInput(5000);
            laserDesign.ComponentDesignAttributes[2].SetValueFromInput(5);

            return GenericComponentFactory.DesignToDesignEntity(game, faction, laserDesign);

        }

        public static Entity DefaultBFC(Game game, Entity faction)
        {
            ComponentDesign fireControlDesign;
            ComponentTemplateSD bfcSD = game.StaticData.ComponentTemplates[new Guid("33fcd1f5-80ab-4bac-97be-dbcae19ab1a0")];
            fireControlDesign = GenericComponentFactory.StaticToDesign(bfcSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            fireControlDesign.ComponentDesignAttributes[0].SetValueFromInput(100);
            fireControlDesign.ComponentDesignAttributes[1].SetValueFromInput(5000);
            fireControlDesign.ComponentDesignAttributes[2].SetValueFromInput(1);

            return GenericComponentFactory.DesignToDesignEntity(game, faction, fireControlDesign);

        }

        public static Entity DefaultCargoInstalation(Game game, Entity faction)
        {
            ComponentDesign cargoInstalation;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{30cd60f8-1de3-4faa-acba-0933eb84c199}")];
            cargoInstalation = GenericComponentFactory.StaticToDesign(template, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            cargoInstalation.ComponentDesignAttributes[0].SetValueFromInput(1000000);
            cargoInstalation.Name = "CargoInstalation1";
            return GenericComponentFactory.DesignToDesignEntity(game, faction, cargoInstalation);
        }

        public static Entity ShipGenericCargo(Game game, Entity faction)
        { 
            ComponentDesign cargoComponent;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{30cd60f8-1de3-4faa-acba-0933eb84c199}")];
            cargoComponent = GenericComponentFactory.StaticToDesign(template, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            cargoComponent.ComponentDesignAttributes[0].SetValueFromInput(5000); //5t component
            cargoComponent.ComponentDesignAttributes[2].SetValueFromInput(500);
            cargoComponent.ComponentDesignAttributes[3].SetValueFromInput(100);
            cargoComponent.Name = "CargoComponent5t";
            return GenericComponentFactory.DesignToDesignEntity(game, faction, cargoComponent);
        }

        public static Entity ShipPassiveSensor(Game game, Entity faction)
        {
            ComponentDesign sensor;
            ComponentTemplateSD template = NameLookup.GetTemplateSD(game, "PassiveSensor");
            sensor = GenericComponentFactory.StaticToDesign(template, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            sensor.ComponentDesignAttributes[0].SetValueFromInput(500);  //size
            sensor.ComponentDesignAttributes[1].SetValueFromInput(600); //best wavelength
            sensor.ComponentDesignAttributes[2].SetValueFromInput(250); //wavelength detection width 
            //sensor.ComponentDesignAttributes[3].SetValueFromInput(10);  //best detection magnatude. (Not settable)
                                                                        //[4] worst detection magnatude (not settable)
            sensor.ComponentDesignAttributes[5].SetValueFromInput(1);   //resolution
            sensor.ComponentDesignAttributes[6].SetValueFromInput(3600);//Scan Time
            sensor.Name = "PassiveSensor-S50";
            return GenericComponentFactory.DesignToDesignEntity(game, faction, sensor);

        }

        public static Entity FacPassiveSensor(Game game, Entity faction)
        {
            ComponentDesign sensor;
            ComponentTemplateSD template = NameLookup.GetTemplateSD(game, "PassiveSensor");
            sensor = GenericComponentFactory.StaticToDesign(template, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            sensor.ComponentDesignAttributes[0].SetValueFromInput(5000);  //size
            sensor.ComponentDesignAttributes[1].SetValueFromInput(500); //best wavelength
            sensor.ComponentDesignAttributes[2].SetValueFromInput(1000); //wavelength detection width 
            //[3] best detection magnatude. (Not settable)
            //[4] worst detection magnatude (not settable)
            sensor.ComponentDesignAttributes[5].SetValueFromInput(5);   //resolution
            sensor.ComponentDesignAttributes[6].SetValueFromInput(3600);//Scan Time
            sensor.Name = "PassiveSensor-S500";
            return GenericComponentFactory.DesignToDesignEntity(game, faction, sensor);

        }

        public static Entity DeadWeight(Game game, Entity faction, int weight)
        {
            ComponentDesign deadTestWeight;
            ComponentTemplateSD template = game.StaticData.ComponentTemplates[new Guid("{57614ddb-0756-44cf-857b-8a6578493792}")];
            deadTestWeight = GenericComponentFactory.StaticToDesign(template, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            deadTestWeight.ComponentDesignAttributes[0].SetValueFromInput(weight);
            deadTestWeight.Name = "DeadWeight-" + weight;
            return GenericComponentFactory.DesignToDesignEntity(game, faction, deadTestWeight);

        }

    }

}