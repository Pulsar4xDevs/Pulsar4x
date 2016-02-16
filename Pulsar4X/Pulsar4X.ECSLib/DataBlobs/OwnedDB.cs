using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OwnedDB : BaseDataBlob
    {
        [PublicAPI]
        public Entity Faction { get; internal set; }

        [JsonConstructor]
        private OwnedDB()
        {
            // JsonConstructor
        }

        internal OwnedDB(Entity faction)
        {
            Faction = faction;
        }

        public OwnedDB(OwnedDB ownedDB)
        {
            Faction = ownedDB.Faction;
        }

        public override object Clone()
        {
            return new OwnedDB(this);
        }
    }
}
