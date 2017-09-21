using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OwnedDB : BaseDataBlob
    {
        
        [JsonProperty]
        internal Entity OwnedByFaction { get; set; }
        [JsonProperty] //TODO: Do we need two entries? maybe remove this. 
        public Entity ObjectOwner { get; internal set; } = Entity.InvalidEntity;
        
        // Json Constructor
        public OwnedDB() { }
        
        public OwnedDB(Entity ownerFaction) : this(ownerFaction, ownerFaction) { }

        internal OwnedDB(Entity entityOwner, Entity objectOwner)
        {
            OwnedByFaction = entityOwner;
            ObjectOwner = objectOwner;
        }

        public override object Clone()
        {
            return new OwnedDB(OwnedByFaction, ObjectOwner);
        }
    }
}
