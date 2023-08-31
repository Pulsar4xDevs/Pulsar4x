
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

            var fleetDB = new FleetDB();
            dataBlobs.Add(fleetDB);

            return Entity.Create(manager, factionID, dataBlobs);
        }
    }
}
