using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OwnedDB : BaseDataBlob
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
                    if (_factionOwner != null)
                    {
                        _factionOwner.GetDataBlob<OwnerDB>().OwnedEntities.Remove(this.OwningEntity.Guid);
                    }
                    _factionOwner = newOwner;
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

        internal OwnedDB(Entity ownerFaction)
        {
            OwnedByFaction = ownerFaction;
            ObjectOwner = ownerFaction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.OwnedDB"/> class.
        /// Use this one if Faction entity is not yet properly initialised. 
        /// </summary>
        /// <param name="ownerFaction">Owner faction.</param>
        /// <param name="ownerDB">Owner db.</param>
        public OwnedDB(Entity ownerFaction, OwnerDB ownerDB) : this(ownerFaction, ownerFaction, ownerDB) { }

        internal OwnedDB(Entity entityOwner, Entity objectOwner, OwnerDB ownerDB)
        {
            _factionOwner = entityOwner;
            ownerDB.OwnedEntities[OwningEntity.Guid] = OwningEntity;

            ObjectOwner = objectOwner;
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
    }

    public class OwnerDB : BaseDataBlob, IGetValuesHash
    {
        [JsonProperty]
        internal Dictionary<Guid, Entity> OwnedEntities { get; } = new Dictionary<Guid, Entity>();

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
                //hash *= Misc.ValueHash(item.Value.Guid);
            }

            return hash;
        }
    }
}
