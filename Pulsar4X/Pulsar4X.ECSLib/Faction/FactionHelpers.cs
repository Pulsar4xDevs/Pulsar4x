using System;
namespace Pulsar4X.ECSLib
{
    public static class FactionHelpers
    {
        public static void SetOwnership(Entity objectToOwn, Entity factionOwner)
        {
            OwnedDB ownedDB = objectToOwn.GetDataBlob<OwnedDB>();
            if (ownedDB.OwnedByFaction != null || ownedDB.OwnedByFaction.IsValid) 
            { 
                Entity originalFaction = objectToOwn.GetDataBlob<OwnedDB>().OwnedByFaction;
                var originalOwnedEntites = originalFaction.GetDataBlob<FactionOwnedEntitesDB>().OwnedEntites;
                if (originalOwnedEntites.ContainsKey(objectToOwn.Guid))
                {
                    originalOwnedEntites.Remove(objectToOwn.Guid);
                }
            }

            var factionOwnedEntites = factionOwner.GetDataBlob<FactionOwnedEntitesDB>().OwnedEntites;
            ownedDB.OwnedByFaction = factionOwner;
            factionOwnedEntites.Add(objectToOwn.Guid, objectToOwn);

        }
    }
}
