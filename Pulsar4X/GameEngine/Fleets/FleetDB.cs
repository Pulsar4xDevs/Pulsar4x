using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Fleets
{
    public class FleetDB : TreeHierarchyDB
    {
        public int FlagShipID { get; internal set; } = -1;

        public bool InheritOrders { get; internal set; } = true;

        public SafeList<ConditionalOrder> StandingOrders { get; internal set; } = new ();

        public FleetDB() : base(null) {}

        public override object Clone()
        {
            return new FleetDB();
        }
    }
}