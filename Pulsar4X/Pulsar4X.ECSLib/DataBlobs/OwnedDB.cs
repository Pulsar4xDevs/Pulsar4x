using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OwnedDB : BaseDataBlob
    {
        [JsonProperty]
        public Entity EntityOwner { get; internal set; } = Entity.InvalidEntity;
        [JsonProperty]
        public Entity ObjectOwner { get; internal set; } = Entity.InvalidEntity;
        
        // Json Constructor
        public OwnedDB() { }
        
        public OwnedDB(Entity ownerFaction) : this(ownerFaction, ownerFaction) { }

        internal OwnedDB(Entity entityOwner, Entity objectOwner)
        {
            EntityOwner = entityOwner;
            ObjectOwner = objectOwner;
        }

        public override object Clone()
        {
            return new OwnedDB(EntityOwner, ObjectOwner);
        }
    }
}
