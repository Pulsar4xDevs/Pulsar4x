
using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Names;

namespace Pulsar4X.Fleets
{
    public static class FleetFactory
    {
        public static Entity Create(EntityManager? manager, int factionID, string name)
        {
            if(manager == null) throw new ArgumentNullException("manager cannot be null");

            var dataBlobs = new List<BaseDataBlob>();
            var nameDB = new NameDB(name, factionID, name);
            dataBlobs.Add(nameDB);

            var fleetDB = new FleetDB();
            dataBlobs.Add(fleetDB);

            var orderableDB = new ActionQueueDB();
            dataBlobs.Add(orderableDB);

            var entity = Entity.Create();
            entity.FactionOwnerID = factionID;
            manager.AddEntity(entity, dataBlobs);

            return entity;
        }
    }
}
