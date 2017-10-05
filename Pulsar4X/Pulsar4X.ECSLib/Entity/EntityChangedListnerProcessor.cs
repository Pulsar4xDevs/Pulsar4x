namespace Pulsar4X.ECSLib
{
    internal static class EntityChangedListnerProcessor
    {
        internal static void SetListners(EntityManager manager, Entity listningEntity)
        {
            //int dbTypeID = EntityManager.GetTypeIndex<OwnedDB>();
            var ownedDBs = manager.GetAllDataBlobsOfType<OwnedDB>();
            var listnerDB = listningEntity.GetDataBlob<EntityChangeListnerDB>();

            foreach (OwnedDB db in ownedDBs)
            {
                if (db.OwnedByFaction == listningEntity || db.OwnedByFaction == listningEntity.GetDataBlob<OwnedDB>().OwnedByFaction)
                {
                    listnerDB.ListningToEntites.Add(db.OwningEntity);
                }
            }
        }

        internal static void PreHandling(EntityChangeListnerDB listnerDB)
        {
            {
                foreach (var change in listnerDB.EntityChanges)
                {
                    if (!listnerDB.ListningToEntites.Contains(change.Entity))
                    {
                        listnerDB.ListningToEntites.Add(change.Entity);
                    }
                }
            }
        }

        internal static void PostHandling(EntityChangeListnerDB listnerDB)
        {
            {
                foreach (var change in listnerDB.EntityChanges)
                {
                    if (change.ChangeType == EntityChangeData.EntityChangeType.EntityRemoved)
                    {
                        listnerDB.ListningToEntites.Remove(change.Entity);
                    }
                }
                listnerDB.EntityChanges.Clear();
            }
        }
    }
}
