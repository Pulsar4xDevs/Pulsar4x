using System;
using System.Collections.Generic;


namespace Pulsar4X.ECSLib
{
    public static class DefaultStartFactory
    {
        public static Entity DefaultHumans(Game game, string name)
        {
            StarSystemFactory starfac = new StarSystemFactory(game);
            StarSystem sol = starfac.CreateSol(game);
            Entity earth = sol.SystemManager.Entities[3]; //should be fourth entity created 
            Entity factionEntity = FactionFactory.CreateFaction(game, name);
            Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, game.GlobalManager);
            Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth);

            ComponentSD mineSD = game.StaticData.Components[new Guid("f7084155-04c3-49e8-bf43-c7ef4befa550")];
            ComponentDesignDB mineDesign = GenericComponentFactory.StaticToDesign(mineSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity mineEntity = GenericComponentFactory.DesignToEntity(game.GlobalManager, mineDesign, factionEntity.GetDataBlob<FactionTechDB>());


            ComponentSD refinarySD = game.StaticData.Components[new Guid("90592586-0BD6-4885-8526-7181E08556B5")];
            ComponentDesignDB refinaryDesign = GenericComponentFactory.StaticToDesign(refinarySD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity refinaryEntity = GenericComponentFactory.DesignToEntity(game.GlobalManager, refinaryDesign, factionEntity.GetDataBlob<FactionTechDB>());


            ComponentSD labSD = game.StaticData.Components[new Guid("c203b7cf-8b41-4664-8291-d20dfe1119ec")];
            ComponentDesignDB labDesign = GenericComponentFactory.StaticToDesign(labSD, factionEntity.GetDataBlob<FactionTechDB>(), game.StaticData);
            Entity labEntity = GenericComponentFactory.DesignToEntity(game.GlobalManager, labDesign, factionEntity.GetDataBlob<FactionTechDB>());

            colonyEntity.GetDataBlob<ColonyInfoDB>().Installations.Add(mineEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Installations.Add(refinaryEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Installations.Add(labEntity);
            ReCalcProcessor.ReCalcAbilities(colonyEntity);
            colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            factionEntity.GetDataBlob<FactionInfoDB>().KnownSystems.Add(sol); //hack test because currently stuff doesnt get added to knownSystems automaticaly

            return factionEntity;
        }
    }

}