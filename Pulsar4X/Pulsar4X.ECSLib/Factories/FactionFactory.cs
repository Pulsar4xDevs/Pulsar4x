using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class FactionFactory
    {
        public static Entity CreateFaction(Game game, string factionName)
        {
            var blobs = new List<BaseDataBlob>();
            NameDB name = new NameDB(factionName);
            FactionInfoDB factionDB = new FactionInfoDB();
            FactionAbilitiesDB factionAbilitiesDB = new FactionAbilitiesDB();
            FactionTechDB techDB = new FactionTechDB(game.StaticData.Techs.Values.ToList());        
            blobs.Add(name);
            blobs.Add(factionDB);
            blobs.Add(factionAbilitiesDB);
            blobs.Add(techDB);
            Entity factionEntity = new Entity(game.GlobalManager, blobs);

            //add this faction to the GM's known faction list.  
            Entity gameMaster;
            game.GlobalManager.FindEntityByGuid(game.GameMasterFaction, out gameMaster);
            gameMaster.GetDataBlob<FactionInfoDB>().KnownFactions.Add(factionEntity);


            return factionEntity;
        }

        public static void CreateGameMaster(Game game)
        {
            var blobs = new List<BaseDataBlob>();
            NameDB name = new NameDB("GameMaster");
            FactionInfoDB factionDB = new FactionInfoDB();
            FactionAbilitiesDB factionAbilitiesDB = new FactionAbilitiesDB();
            FactionTechDB techDB = new FactionTechDB(game.StaticData.Techs.Values.ToList());

            //add some techs for testing purposes... eventualy the GM should be able to set any factions techlevel from the gui
            techDB.ResearchedTechs.Add(new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e"), 3);
            techDB.ResearchedTechs.Add(new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de"), 3);
            techDB.ResearchedTechs.Add(new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc"), 5);
            techDB.ResearchedTechs.Add(new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"), 1);
            techDB.ResearchedTechs.Add(new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"), 1);
            techDB.ResearchedTechs.Add(new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"), 1);


            blobs.Add(name);
            blobs.Add(factionDB);
            blobs.Add(factionAbilitiesDB);
            blobs.Add(techDB);
            Entity gameMaster = new Entity(game.GlobalManager, blobs);
            game.GameMasterFaction = gameMaster.Guid;
        }

    }
}