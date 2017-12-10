using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class EntityChangeListnerDB : BaseDataBlob
    {

        public List<EntityChangeData> EntityChanges { get; } = new List<EntityChangeData>();
        internal HashSet<Entity> ListningToEntites { get; } = new HashSet<Entity>();
        internal Entity ListenForFaction;
        public EntityChangeListnerDB()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.EntityChangeListnerDB"/> class.
        /// </summary>
        /// <param name="factionEntity">will listen for any entites added or removed that are owned by this entity</param>
        internal EntityChangeListnerDB(Entity factionEntity)
        {
            ListenForFaction = factionEntity;
        }

        internal void AddChange(EntityChangeData changeData)
        {
            if (changeData.ChangeType != EntityChangeData.EntityChangeType.EntityAdded)
            {
                if (ListningToEntites.Contains(changeData.Entity))
                {
                    EntityChanges.Add(changeData);
                }
            }
            else 
            {
                /* for some reason, the entity is not considered valid at thit point. so this bit of code fails. 
                bool isvalid = changeData.Entity.IsValid;
                if (changeData.Entity.HasDataBlob<OwnedDB>() && changeData.Entity.GetDataBlob<OwnedDB>().OwnedByFaction == ListenForFaction)
                { 
                    ListningToEntites.Add(changeData.Entity);
                    EntityChanges.Add(changeData);
                }*/
                ListningToEntites.Add(changeData.Entity);
                EntityChanges.Add(changeData);
            }
        }

        public override object Clone()
        {
            return new EntityChangeListnerDB(this);
        }
        EntityChangeListnerDB(EntityChangeListnerDB db)
        {
            EntityChanges = new List<EntityChangeData>(db.EntityChanges);
            ListningToEntites = new HashSet<Entity>(db.ListningToEntites);
            ListenForFaction = db.ListenForFaction;
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
