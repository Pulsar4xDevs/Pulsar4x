using System;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Datablobs
{
    public class FleetDB : TreeHierarchyDB
    {
        public int FlagShipID { get; internal set; } = -1;
        public bool InheritOrders { get; internal set; } = true;
        public SafeList<ConditionalOrder> StandingOrders { get; } = new ();

        public FleetDB() : base(null) {}

        public override object Clone()
        {
            return new FleetDB();
        }
    }
}