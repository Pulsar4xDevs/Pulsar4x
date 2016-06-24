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
            Entity mineEntity = GenericComponentFactory.DesignToEntity(game, factionEntity, mineDesign);


            ComponentTemplateSD RefinerySD = game.StaticData.Components[new Guid("90592586-0BD6-4885-8526-7181E08556B5")];
            ComponentDesign RefineryDesign = GenericComponentFactory.StaticToDesign(RefinerySD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity RefineryEntity = GenericComponentFactory.DesignToEntity(game, factionEntity, RefineryDesign);

            ComponentTemplateSD labSD = game.StaticData.Components[new Guid("c203b7cf-8b41-4664-8291-d20dfe1119ec")];
            ComponentDesign labDesign = GenericComponentFactory.StaticToDesign(labSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity labEntity = GenericComponentFactory.DesignToEntity(game, factionEntity, labDesign);

            ComponentTemplateSD facSD = game.StaticData.Components[new Guid("{07817639-E0C6-43CD-B3DC-24ED15EFB4BA}")];
            ComponentDesign facDesign = GenericComponentFactory.StaticToDesign(facSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity facEntity = GenericComponentFactory.DesignToEntity(game, factionEntity, facDesign);

            Entity scientistEntity = CommanderFactory.CreateScientist(game.GlobalManager, factionEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Scientists.Add(scientistEntity);

            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            //TechProcessor.ApplyTech(factionTech, game.StaticData.Techs[new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c")]); //add conventional engine for testing. 
            TechProcessor.MakeResearchable(factionTech);

            ShipAndColonyInfoProcessor.AddComponentDesignToEntity(mineEntity, colonyEntity);
            ShipAndColonyInfoProcessor.AddComponentDesignToEntity(RefineryEntity, colonyEntity);
            ShipAndColonyInfoProcessor.AddComponentDesignToEntity(labEntity, colonyEntity);
            ShipAndColonyInfoProcessor.AddComponentDesignToEntity(facEntity, colonyEntity);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            
            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(sol.Guid);

            Entity shipClass = DefaultShipDesign(game, factionEntity);
            Vector4 position = earth.GetDataBlob<PositionDB>().Position;
            Entity ship = ShipFactory.CreateShip(shipClass, sol.SystemManager, factionEntity, position, sol, "Serial Peacemaker");
            sol.SystemManager.SetDataBlob(ship.ID, new TransitableDB());

            return factionEntity;
        }


        static Entity DefaultShipDesign(Game game, Entity faction)
        {
            var shipDesign = ShipFactory.CreateNewShipClass(game, faction, "Ob'enn dropship");
            Entity engine = DefaultEngineDesign(game, faction);
            Entity laser = DefaultSimpleLaser(game, faction);
            ShipFactory.AddShipComponent(shipDesign, engine);
            ShipFactory.AddShipComponent(shipDesign, engine);
            ShipFactory.AddShipComponent(shipDesign, laser);
            return shipDesign;
        }

        static Entity DefaultEngineDesign(Game game, Entity faction)
        {
            ComponentDesign engineDesign;

            ComponentTemplateSD engineSD = game.StaticData.Components[new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6")];
            engineDesign = GenericComponentFactory.StaticToDesign(engineSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            engineDesign.ComponentDesignAbilities[0].SetValueFromInput(5); //size
            engineDesign.Name = "DefaultEngine1";
            //engineDesignDB.ComponentDesignAbilities[1]
            return GenericComponentFactory.DesignToEntity(game, faction, engineDesign);
        }

        static Entity DefaultSimpleLaser(Game game, Entity faction)
        {
            ComponentDesign laserDesign;
            ComponentTemplateSD laserSD = game.StaticData.Components[new Guid("8923f0e1-1143-4926-a0c8-66b6c7969425")];
            laserDesign = GenericComponentFactory.StaticToDesign(laserSD, faction.GetDataBlob<FactionTechDB>(), game.StaticData);
            laserDesign.ComponentDesignAbilities[0].SetValueFromInput(10);
            laserDesign.ComponentDesignAbilities[0].SetValueFromInput(5000);
            laserDesign.ComponentDesignAbilities[0].SetValueFromInput(5);

            return GenericComponentFactory.DesignToEntity(game, faction, laserDesign);

        }
    }

}