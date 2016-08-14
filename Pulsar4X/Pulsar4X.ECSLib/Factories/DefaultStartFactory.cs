using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class DefaultStartFactory
    {
        public static Entity DefaultHumans(Game game, Player owner, string name)
        {
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem sol = starfac.CreateSol(game);
            Entity earth = sol.SystemManager.Entities[3]; //should be fourth entity created 
            Entity factionEntity = FactionFactory.CreatePlayerFaction(game, owner, name);
            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);
            Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth);

            ComponentTemplateSD mineSD = game.StaticData.Components[new Guid("f7084155-04c3-49e8-bf43-c7ef4befa550")];
            ComponentDesign mineDesign = GenericComponentFactory.StaticToDesign(mineSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity mineEntity = GenericComponentFactory.DesignToDesignEntity(game, factionEntity, mineDesign);


            ComponentTemplateSD RefinerySD = game.StaticData.Components[new Guid("90592586-0BD6-4885-8526-7181E08556B5")];
            ComponentDesign RefineryDesign = GenericComponentFactory.StaticToDesign(RefinerySD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity RefineryEntity = GenericComponentFactory.DesignToDesignEntity(game, factionEntity, RefineryDesign);

            ComponentTemplateSD labSD = game.StaticData.Components[new Guid("c203b7cf-8b41-4664-8291-d20dfe1119ec")];
            ComponentDesign labDesign = GenericComponentFactory.StaticToDesign(labSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity labEntity = GenericComponentFactory.DesignToDesignEntity(game, factionEntity, labDesign);

            ComponentTemplateSD facSD = game.StaticData.Components[new Guid("{07817639-E0C6-43CD-B3DC-24ED15EFB4BA}")];
            ComponentDesign facDesign = GenericComponentFactory.StaticToDesign(facSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity facEntity = GenericComponentFactory.DesignToDesignEntity(game, factionEntity, facDesign);

            Entity scientistEntity = CommanderFactory.CreateScientist(game.GlobalManager, factionEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Scientists.Add(scientistEntity);

            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            //TechProcessor.ApplyTech(factionTech, game.StaticData.Techs[new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c")]); //add conventional engine for testing. 
            TechProcessor.MakeResearchable(factionTech);

            EntityManipulation.AddComponentToEntity(colonyEntity, mineEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, RefineryEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, labEntity);
            EntityManipulation.AddComponentToEntity(colonyEntity, facEntity);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            


            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(sol.Guid);
            //test systems
            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateEccTest(game).Guid);
            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(starfac.CreateLongitudeTest(game).Guid);



            // Todo: handle this in CreateShip
            Entity shipClass = DefaultShipDesign(game, factionEntity);
            Entity gunShipClass = GunShipDesign(game, factionEntity);

            Vector4 position = earth.GetDataBlob<PositionDB>().AbsolutePosition;


            // Problem - the component instances, both the design and the instances themselves, are the same entities on each ship
            // IE, the fire control on ship1 is the same entity as on ship2
            // Both the design and instances should be unique

            Entity ship1 = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Serial Peacemaker");
            Entity ship2 = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Ensuing Calm");
            Entity gunShip = ShipFactory.CreateShip(gunShipClass, sol.SystemManager, factionEntity, position, sol, "Prevailing Stillness");

            sol.SystemManager.SetDataBlob(ship1.ID, new TransitableDB());
            sol.SystemManager.SetDataBlob(ship2.ID, new TransitableDB());
            sol.SystemManager.SetDataBlob(gunShip.ID, new TransitableDB());

            //Entity ship = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Serial Peacemaker");
            //ship.SetDataBlob(earth.GetDataBlob<PositionDB>()); //first ship reference PositionDB

            //Entity ship3 = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Contiual Pacifier");
            //ship3.SetDataBlob((OrbitDB)earth.GetDataBlob<OrbitDB>().Clone());//second ship clone earth OrbitDB


            //sol.SystemManager.SetDataBlob(ship.ID, new TransitableDB());

            Entity rock = AsteroidFactory.CreateAsteroid(sol, earth, game.CurrentDateTime + TimeSpan.FromDays(365));

            return factionEntity;
        }


        public static Entity DefaultShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Ob'enn dropship");
            Entity engine = DefaultEngineDesign(game, faction);
            Entity laser = DefaultSimpleLaser(game, faction);
            Entity bfc = DefaultBFC(game, faction);
            EntityManipulation.AddComponentToEntity(shipDesign, engine);
            EntityManipulation.AddComponentToEntity(shipDesign, engine);
            EntityManipulation.AddComponentToEntity(shipDesign, laser);
            EntityManipulation.AddComponentToEntity(shipDesign, bfc);
            return shipDesign;
        }

        public static Entity GunShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Ob'enn dropship");
            Entity engine = DefaultEngineDesign(game, faction);
            Entity laser = DefaultSimpleLaser(game, faction);
            Entity bfc = DefaultBFC(game, faction);
            EntityManipulation.AddComponentToEntity(shipDesign, engine);
            EntityManipulation.AddComponentToEntity(shipDesign, engine);
            EntityManipulation.AddComponentToEntity(shipDesign, laser);
            EntityManipulation.AddComponentToEntity(shipDesign, laser);
            EntityManipulation.AddComponentToEntity(shipDesign, laser);
            EntityManipulation.AddComponentToEntity(shipDesign, laser);
            EntityManipulation.AddComponentToEntity(shipDesign, bfc);
            EntityManipulation.AddComponentToEntity(shipDesign, bfc);
            return shipDesign;
        }

        public static Entity DefaultEngineDesign(Game game, Entity faction)
        {
            ComponentDesign engineDesign;

            ComponentTemplateSD engineSD = game.StaticData.Components[new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6")];
            engineDesign = GenericComponentFactory.StaticToDesign(engineSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            engineDesign.ComponentDesignAbilities[0].SetValueFromInput(10); //size
            engineDesign.Name = "DefaultEngine1";
            //engineDesignDB.ComponentDesignAbilities[1]
            return GenericComponentFactory.DesignToDesignEntity(game, faction, engineDesign);
        }

        public static Entity DefaultSimpleLaser(Game game, Entity faction)
        {
            ComponentDesign laserDesign;
            ComponentTemplateSD laserSD = game.StaticData.Components[new Guid("8923f0e1-1143-4926-a0c8-66b6c7969425")];
            laserDesign = GenericComponentFactory.StaticToDesign(laserSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            laserDesign.ComponentDesignAbilities[0].SetValueFromInput(10);
            laserDesign.ComponentDesignAbilities[1].SetValueFromInput(5000);
            laserDesign.ComponentDesignAbilities[2].SetValueFromInput(5);

            return GenericComponentFactory.DesignToDesignEntity(game, faction, laserDesign);

        }

        public static Entity DefaultBFC(Game game, Entity faction)
        {
            ComponentDesign fireControlDesign;
            ComponentTemplateSD bfcSD = game.StaticData.Components[new Guid("33fcd1f5-80ab-4bac-97be-dbcae19ab1a0")];
            fireControlDesign = GenericComponentFactory.StaticToDesign(bfcSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            fireControlDesign.ComponentDesignAbilities[0].SetValueFromInput(10);
            fireControlDesign.ComponentDesignAbilities[1].SetValueFromInput(5000);
            fireControlDesign.ComponentDesignAbilities[2].SetValueFromInput(1);

            return GenericComponentFactory.DesignToDesignEntity(game, faction, fireControlDesign);

        }
    }

}