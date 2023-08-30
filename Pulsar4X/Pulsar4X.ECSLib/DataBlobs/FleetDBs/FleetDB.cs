using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class FleetDB : TreeHierarchyDB
    {
        public List<Guid> Ships = new ();

        public FleetDB(FleetDB fleetDB) : base(null)
        {
            Ships = new(fleetDB.Ships);
        }

        public override object Clone()
        {
            return new FleetDB(this);
        }
    }
}