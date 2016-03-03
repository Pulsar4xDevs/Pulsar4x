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

        internal OwnedDB(Entity entityOwner, Entity objectOwner)
        {
            EntityOwner = entityOwner;
            ObjectOwner = objectOwner;
        }

        public OwnedDB(OwnedDB ownedDB) : this(ownedDB.EntityOwner, ownedDB.ObjectOwner) { }

        public OwnedDB(Entity ownerFaction) : this(ownerFaction, ownerFaction) { }

        public override object Clone()
        {
            return new OwnedDB(this);
        }
    }
}
