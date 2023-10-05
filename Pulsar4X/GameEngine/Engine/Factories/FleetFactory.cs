
using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine
{
    public static class FleetFactory
    {
        public static Entity Create(EntityManager manager, int factionID, string name)
        {
            var dataBlobs = new List<BaseDataBlob>();
            var nameDB = new NameDB(name, factionID, name);
            dataBlobs.Add(nameDB);

            var fleetDB = new FleetDB();
            dataBlobs.Add(fleetDB);

            var orderableDB = new OrderableDB();
            dataBlobs.Add(orderableDB);

            var entity = Entity.Create();
            entity.FactionOwnerID = factionID;
            manager.AddEntity(entity, dataBlobs);

            return entity;
        }
    }
}
