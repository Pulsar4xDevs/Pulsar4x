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
            
            return factionEntity;
        }
    }
}