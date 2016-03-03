using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OwnedDB : BaseDataBlob
    {
        public Entity EntityOwner { get; internal set; }
        public Entity ObjectOwner { get; internal set; }

        [JsonConstructor]
        private OwnedDB()
        {
            // JsonConstructor
        }

        internal OwnedDB(Entity entityOwner, Entity objectOwner)
        {
            EntityOwner = entityOwner;
            ObjectOwner = objectOwner;
        }

        public OwnedDB(OwnedDB ownedDB) : this(ownedDB.EntityOwner, ownedDB.ObjectOwner) { }

        public override object Clone()
        {
            return new OwnedDB(this);
        }
    }
}
