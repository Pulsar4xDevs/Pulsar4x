using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /*
    public class OwnedDB : BaseDataBlob, IGetValuesHash
    {
        [JsonIgnore]
        FactionOwnerDB FactionOwnerDB { get; set; }
        
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
                        oldOwner.GetDataBlob<FactionOwnerDB>().OwnedEntities.Remove(this.OwningEntity.ID);
                    }
                    _factionOwner = newOwner;
                    if (!OwningEntity.IsValid)
                        throw new Exception("Invalid Entity, Ownership must be se *after* the entity has been setup (don't include OwnedDB at entity creation, do it after creation)");
                    FactionOwnerDB = _factionOwner.GetDataBlob<FactionOwnerDB>();
                    FactionOwnerDB.OwnedEntities[this.OwningEntity.ID] = this.OwningEntity;
                }
            }
        }

        internal void SetFactionOwner(FactionOwnerDB factionOwnerDB, [System.Runtime.CompilerServices.CallerMemberName] string callerName = null)
        {
            if (callerName != nameof(FactionOwnerDB.SetOwned))
                throw new Exception("SetFactionOwner should be called from FactionOwnerDB.SetOwned");
            _factionOwner = factionOwnerDB.OwningEntity;
            FactionOwnerDB = factionOwnerDB;
            OwningEntity.FactionOwner = factionOwnerDB.OwningEntity.ID;
        }

        /*
        /// <summary>
        /// This is for defining a components ship owner (or a factories colony)
        /// </summary>
        /// <value>The object owner.</value>
        [JsonIgnore]
        ObjectOwnershipDB ObjectOwnerDB { get; set; }
        [JsonProperty]
        private Entity _obectOwner;
        public Entity ObjectOwner
        {
            get
            {
                return _obectOwner;
            }
        }


        internal void SetObjectOwner(ObjectOwnershipDB objectOwnerDB, [System.Runtime.CompilerServices.CallerMemberName] string callerName = null)
        {
            if (callerName != nameof(FactionOwnerDB.SetOwned))
                throw new Exception("SetFactionOwner should be called from FactionOwnerDB.SetOwned");
            _factionOwner = objectOwnerDB.OwningEntity;
            ObjectOwnerDB = objectOwnerDB;
        }
        


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

        }

        OwnedDB(OwnedDB db)
        {
            _factionOwner = db._factionOwner;
            //_obectOwner = db.ObjectOwner;
        }

        public override object Clone()
        {
            return new OwnedDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(_factionOwner.ID, hash);
            //hash = Misc.ValueHash(_obectOwner.ID, hash);
            return hash; 
        }

        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            //_factionOwner.GetDataBlob<OwnerDB>().OwnedEntities[this.OwningEntity.ID] = this.OwningEntity;
        }
    }


    */
    public class ObjectOwnershipDB : TreeHierarchyDB, IGetValuesHash
    {
        /*
        [JsonProperty]
        internal ID ParentStarSystem { get; set; }
        [JsonProperty]
        internal Dictionary<ID, Entity> OwnedEntities { get; set; } = new Dictionary<ID, Entity>();

*/

        public ObjectOwnershipDB() : base(null) { }

        /*
        internal void SetOwned(Entity childEntity)
        {
            OwnedDB ownedDB;
            OwnedEntities[childEntity.ID] = childEntity;
            if (!childEntity.HasDataBlob<OwnedDB>())
            {
                ownedDB = new OwnedDB();
                childEntity.SetDataBlob(ownedDB);
            }
            else
                ownedDB = childEntity.GetDataBlob<OwnedDB>();
            ownedDB.SetObjectOwner(this);

        }
*/

        public override object Clone()
        {
            return new ObjectOwnershipDB(this);
        }

        ObjectOwnershipDB(ObjectOwnershipDB toClone) : base(toClone.Parent)
        {
            //ParentStarSystem = toClone.ParentStarSystem;
            //OwnedEntities = new Dictionary<ID, Entity>(toClone.OwnedEntities);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(Parent.Guid, hash);
            foreach (var item in Children)
            {
                hash = Misc.ValueHash(item.Guid, hash);
            }
            //hash = Misc.ValueHash(ParentStarSystem, hash);
            //foreach (var entityGuid in OwnedEntities.Keys)
            //{
            //    hash = Misc.ValueHash(entityGuid, hash);
            //}
            return hash;
        }
    }

    public class FactionOwnerDB : BaseDataBlob, IGetValuesHash
    {
        [JsonProperty]
        internal Dictionary<Guid, Entity> OwnedEntities { get; set; } = new Dictionary<Guid, Entity>();
        private Dictionary<Guid, List<Entity>> ByStarSystem { get; set; } = new Dictionary<Guid, List<Entity>>();
        public FactionOwnerDB() { }

        public FactionOwnerDB(FactionOwnerDB db)
        {
            OwnedEntities = new Dictionary<Guid, Entity>(db.OwnedEntities);
        }

        internal void SetOwned(Entity entity)
        {
            OwnedEntities[entity.Guid] = entity;
            entity.FactionOwnerID = this.OwningEntity.Guid;
        }

        internal void AddEntity(Entity entity)
        {
            OwnedEntities[entity.Guid] = entity;
            entity.FactionOwnerID = this.OwningEntity.FactionOwnerID;
        }

        internal void RemoveEntity(Entity entity)
        {
            if (OwnedEntities.ContainsKey(entity.Guid))
            {
                OwnedEntities.Remove(entity.Guid);
                entity.FactionOwnerID = Guid.Empty;
            }
        }


        public override object Clone()
        {
            return new FactionOwnerDB(this);
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
