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
            FactionDB factionDB = new FactionDB();
            FactionAbilitiesDB factionAbilitiesDB = new FactionAbilitiesDB();
            TechDB techDB = new TechDB(game.StaticData.Techs.Values.ToList());
            blobs.Add(name);
            blobs.Add(factionDB);
            blobs.Add(factionAbilitiesDB);
            blobs.Add(techDB);
            Entity factionEntity = new Entity(game.GlobalManager, blobs);

            //add this faction to the GM's known faction list.            
            game.GameMasterFaction.GetDataBlob<FactionDB>().KnownFactions.Add(factionEntity);

            return factionEntity;
        }

        public static void CreateGameMaster(Game game)
        {
            var blobs = new List<BaseDataBlob>();
            NameDB name = new NameDB("GameMaster");
            FactionDB factionDB = new FactionDB();
            FactionAbilitiesDB factionAbilitiesDB = new FactionAbilitiesDB();
            TechDB techDB = new TechDB(game.StaticData.Techs.Values.ToList());
            blobs.Add(name);
            blobs.Add(factionDB);
            blobs.Add(factionAbilitiesDB);
            blobs.Add(techDB);
            Entity gm = new Entity(game.GlobalManager, blobs);

            game.GameMasterFaction = gm;
        }

    }
}