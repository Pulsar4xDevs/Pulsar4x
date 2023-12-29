using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine
{
    public struct EntityChangeData
    {
        public enum EntityChangeType
        {
            EntityAdded,
            EntityRemoved,
            DBAdded,
            DBRemoved,
        }
        //TODO: May need DateTime in here at some point for clients.
        public EntityChangeType ChangeType;
        public Entity Entity;
        public BaseDataBlob? Datablob; //will be null if ChangeType is EntityAdded or EntityRemoved.

        public EntityChangeData(Entity entity, EntityChangeType changeType)
        {
            Entity = entity;
            ChangeType = changeType;
        }
        
    }
}