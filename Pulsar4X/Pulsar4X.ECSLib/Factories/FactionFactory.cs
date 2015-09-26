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
            game.GameMasterFaction.GetDataBlob<FactionInfoDB>().KnownFactions.Add(factionEntity);

            return factionEntity;
        }

        public static void CreateGameMaster(Game game)
        {
            var blobs = new List<BaseDataBlob>();
            NameDB name = new NameDB("GameMaster");
            FactionInfoDB factionDB = new FactionInfoDB();
            FactionAbilitiesDB factionAbilitiesDB = new FactionAbilitiesDB();
            FactionTechDB techDB = new FactionTechDB(game.StaticData.Techs.Values.ToList());
            blobs.Add(name);
            blobs.Add(factionDB);
            blobs.Add(factionAbilitiesDB);
            blobs.Add(techDB);
            Entity gm = new Entity(game.GlobalManager, blobs);

            game.GameMasterFaction = gm;
        }

    }
}