using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OwnedDB : BaseDataBlob
    {
        
        [JsonProperty]
        private Entity _entityFactionOwner = Entity.InvalidEntity;
        internal Entity OwnedByFaction {
            get {
                return _entityFactionOwner;
            }
            set {
                //TODO: think about moving this to a processor, however that will not guarentee that the factionownedentitiesDB gets populated.
                Entity origionalFaction = _entityFactionOwner;
                Entity newFactionOwner = value;
                _entityFactionOwner = newFactionOwner;
                if(newFactionOwner.IsValid && !newFactionOwner.GetDataBlob<FactionOwnedEntitesDB>().OwnedEntites.ContainsKey(this.OwningEntity.Guid))
                    newFactionOwner.GetDataBlob<FactionOwnedEntitesDB>().OwnedEntites.Add(this.OwningEntity.Guid, this.OwningEntity);
                if(origionalFaction.IsValid)
                {
                    origionalFaction.GetDataBlob<FactionOwnedEntitesDB>().OwnedEntites.Remove(this.OwningEntity.Guid);
                }
            }
        }
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
