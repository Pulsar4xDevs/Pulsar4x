
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class FleetFactory
    {
        public static Entity Create(EntityManager manager, Guid factionID, string name)
        {
            var dataBlobs = new List<BaseDataBlob>();
            var nameDB = new NameDB(name, factionID, name);
            dataBlobs.Add(nameDB);

            var fleetDB = new NavyDB();
            dataBlobs.Add(fleetDB);

            var orderableDB = new OrderableDB();
            dataBlobs.Add(orderableDB);

            return Entity.Create(manager, factionID, dataBlobs);
        }
    }
}
