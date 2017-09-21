using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class FactionFactory
    {
        public static Entity CreateFaction(Game game, string factionName)
        {
            var name = new NameDB(factionName);
            var factionDB = new FactionInfoDB();
            var factionAbilitiesDB = new FactionAbilitiesDB();
            var techDB = new FactionTechDB(game.StaticData.Techs.Values.ToList());
            var factionOwned = new FactionOwnedEntitesDB();

            var blobs = new List<BaseDataBlob> { name, factionDB, factionAbilitiesDB, techDB, factionOwned };
            var factionEntity = new Entity(game.GlobalManager, blobs);

            // Add this faction to the SM's access list.
            game.SpaceMaster.SetAccess(factionEntity, AccessRole.SM);

            return factionEntity;
        }

        public static Entity CreatePlayerFaction(Game game, Player owningPlayer, string factionName)
        {
            Entity faction = CreateFaction(game, factionName);

            if (!Equals(owningPlayer, game.SpaceMaster))
            {
                owningPlayer.SetAccess(faction, AccessRole.Owner);
            }

            return faction;
        }
    }
}