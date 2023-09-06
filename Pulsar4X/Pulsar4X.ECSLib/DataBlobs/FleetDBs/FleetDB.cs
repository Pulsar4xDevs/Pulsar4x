using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class FleetDB : TreeHierarchyDB
    {
        public Guid FlagShipID { get; internal set; } = Guid.Empty;
        public bool InheritOrders { get; internal set; } = true;
        public List<ConditionalOrder> StandingOrders { get; } = new ();

        public FleetDB() : base(null) {}

        public override object Clone()
        {
            return new FleetDB();
        }
    }
}