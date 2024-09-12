using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Datablobs
{
    public class FactionOwnerDB : BaseDataBlob
    {
        [JsonProperty]
        internal Dictionary<int, Entity> OwnedEntities { get; set; } = new ();
        private Dictionary<string, List<Entity>> ByStarSystem { get; set; } = new ();
        public FactionOwnerDB() { }

        public FactionOwnerDB(FactionOwnerDB db)
        {
            OwnedEntities = new Dictionary<int, Entity>(db.OwnedEntities);
        }

        internal void SetOwned(Entity entity)
        {
            OwnedEntities[entity.Id] = entity;
            // FIXME: was overwriting Id's set from ColonyFactory
            //entity.FactionOwnerID = this.OwningEntity.Id;
        }

        internal void AddEntity(Entity entity)
        {
            OwnedEntities[entity.Id] = entity;
            entity.FactionOwnerID = this.OwningEntity.FactionOwnerID;
        }

        internal void RemoveEntity(Entity entity)
        {
            if (OwnedEntities.ContainsKey(entity.Id))
            {
                OwnedEntities.Remove(entity.Id);
                entity.FactionOwnerID = -1;
            }
        }


        public override object Clone()
        {
            return new FactionOwnerDB(this);
        }
    }
}