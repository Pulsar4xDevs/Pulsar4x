using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class SystemAsEntityFactory
    {
        public static Entity CreateSystemAsEntity(EntityManager sysMan, StarSystem starSys, Entity factionEntity)
        {
            var sysdb = new StarSystemDB(starSys);

            var ownddb = new OwnedDB(factionEntity);
            var changeListnerDB = new EntityChangeListnerDB();

            List<BaseDataBlob> datablobs = new List<BaseDataBlob>() {
                sysdb,
                ownddb,
                changeListnerDB
            };
            return new Entity(sysMan, datablobs);
        }
    }


    public class WatchedEntityDB : BaseDataBlob
    {
        internal List<EntityChangeData> Changes = new List<EntityChangeData>();


        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }



    public struct EntityChangeData
    {
        public enum EntityChangeType
        {
            EntityAdded,
            EntityRemoved,
            DBAdded,
            DBRemoved,
        }
        public EntityChangeType ChangeType;
        public Entity Entity;
        public BaseDataBlob Datablob; //will be null if ChangeType is EntityAdded or EntityRemoved.
    }



    public class EntityChangeListnerDB : BaseDataBlob
    {

        public List<EntityChangeData> ChangedEntites { get; } = new List<EntityChangeData>();
        internal HashSet<Entity> ListningToEntites { get; } = new HashSet<Entity>();

        public EntityChangeListnerDB() {
        }

        public EntityChangeListnerDB(EntityChangeListnerDB db)
        { }

        public override object Clone()
        {
            return new EntityChangeListnerDB(this);
        }
    }



    public class StarSystemDB : BaseDataBlob
    {
        internal StarSystem StarSystem { get;  set; }

        public StarSystemDB()
        { }

        public StarSystemDB(StarSystem starSys)
        { StarSystem = starSys; }

        public StarSystemDB(StarSystemDB db)
        {
            StarSystem = db.StarSystem;
        }

        public override object Clone()
        {
            return new StarSystemDB(this);
        }
    }



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
                foreach (var change in listnerDB.ChangedEntites)
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
                foreach (var change in listnerDB.ChangedEntites)
                {
                    if (change.ChangeType == EntityChangeData.EntityChangeType.EntityRemoved)
                    {
                        listnerDB.ListningToEntites.Remove(change.Entity);
                    }
                }
                listnerDB.ChangedEntites.Clear();
            }
        }
    }
}
