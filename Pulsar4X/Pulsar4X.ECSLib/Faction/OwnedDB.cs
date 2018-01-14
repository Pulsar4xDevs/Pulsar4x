using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OwnedDB : BaseDataBlob, IGetValuesHash
    {
        
        [JsonProperty]
        private Entity _factionOwner;
        internal Entity OwnedByFaction
        {
            get
            {
                return _factionOwner;
            }
            set
            {
                if (_factionOwner != value)
                {
                    var newOwner = value;
                    var oldOwner = _factionOwner;
                    if (_factionOwner != null)
                    {
                        oldOwner.GetDataBlob<OwnerDB>().OwnedEntities.Remove(this.OwningEntity.Guid);
                    }
                    _factionOwner = newOwner;
                    if (!OwningEntity.IsValid)
                        throw new Exception("Invalid Entity, Ownership must be se *after* the entity has been setup (don't include OwnedDB at entity creation, do it after creation)");    
                    _factionOwner.GetDataBlob<OwnerDB>().OwnedEntities[this.OwningEntity.Guid] = this.OwningEntity;
                }
            }
        }


        /// <summary>
        /// This is for defining a components ship owner (or a factories colony)
        /// </summary>
        /// <value>The object owner.</value>
        [JsonProperty] 
        public Entity ObjectOwner { get; internal set; } = Entity.InvalidEntity;
        
        // Json Constructor
        public OwnedDB() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.OwnedDB"/> class.
        /// NOTE: this Sets itself to the owningEntity. there is no need to owningEntity.SetDataBlob(ownedDB)
        /// </summary>
        /// <param name="ownerFaction">Owner faction.</param>
        /// <param name="owningEntity">Owning entity.</param>
        internal OwnedDB(Entity ownerFaction, Entity owningEntity)
        {
            this.OwningEntity = owningEntity;
            OwnedByFaction = ownerFaction;

            owningEntity.SetDataBlob(this);

            ObjectOwner = ownerFaction; //get rid of this. 

        }

        OwnedDB(OwnedDB db)
        {
            _factionOwner = db._factionOwner;
            ObjectOwner = db.ObjectOwner;
        }

        public override object Clone()
        {
            return new OwnedDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(_factionOwner.Guid, hash);
            hash = Misc.ValueHash(ObjectOwner.Guid, hash);
            return hash; 
        }

        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            _factionOwner.GetDataBlob<OwnerDB>().OwnedEntities[this.OwningEntity.Guid] = this.OwningEntity;
        }
    }

    public class OwnerDB : BaseDataBlob, IGetValuesHash
    {
        [JsonProperty]
        internal Dictionary<Guid, Entity> OwnedEntities { get; set; } = new Dictionary<Guid, Entity>();

        public OwnerDB() { }

        public OwnerDB(OwnerDB db)
        {
            OwnedEntities = new Dictionary<Guid, Entity>(db.OwnedEntities);
        }

        public override object Clone()
        {
            return new OwnerDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            foreach (var item in OwnedEntities)
            {
                hash = Misc.ValueHash(item.Key, hash);
            }

            return hash;
        }
    }
}
