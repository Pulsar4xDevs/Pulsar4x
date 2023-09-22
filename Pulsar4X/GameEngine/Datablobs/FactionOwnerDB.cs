using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Datablobs
{
    public class FactionOwnerDB : BaseDataBlob, IGetValuesHash
    {
        [JsonProperty]
        internal Dictionary<string, Entity> OwnedEntities { get; set; } = new ();
        private Dictionary<string, List<Entity>> ByStarSystem { get; set; } = new ();
        public FactionOwnerDB() { }

        public FactionOwnerDB(FactionOwnerDB db)
        {
            OwnedEntities = new Dictionary<string, Entity>(db.OwnedEntities);
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
                entity.FactionOwnerID = String.Empty;
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
                hash = ObjectExtensions.ValueHash(item.Key, hash);
            }

            return hash;
        }
    }
}