using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
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