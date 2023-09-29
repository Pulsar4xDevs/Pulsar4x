using System;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Datablobs
{
    public class FleetDB : TreeHierarchyDB
    {
        public string FlagShipID { get; internal set; } = string.Empty;
        public bool InheritOrders { get; internal set; } = true;
        public SafeList<ConditionalOrder> StandingOrders { get; } = new ();

        public FleetDB() : base(null) {}

        public override object Clone()
        {
            return new FleetDB();
        }
    }
}