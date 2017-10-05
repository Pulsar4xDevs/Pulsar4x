using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class EntityChangeListnerDB : BaseDataBlob
    {

        public List<EntityChangeData> EntityChanges { get; } = new List<EntityChangeData>();
        internal HashSet<Entity> ListningToEntites { get; } = new HashSet<Entity>();

        public EntityChangeListnerDB()
        {
        }

        public EntityChangeListnerDB(EntityChangeListnerDB db)
        {
            EntityChanges = new List<EntityChangeData>(db.EntityChanges);
            ListningToEntites = new HashSet<Entity>(db.ListningToEntites);
        }

        public override object Clone()
        {
            return new EntityChangeListnerDB(this);
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
}
