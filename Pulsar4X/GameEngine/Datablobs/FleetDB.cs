using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Datablobs
{
    public class FleetDB : TreeHierarchyDB
    {
        [JsonProperty]
        public int FlagShipID { get; internal set; } = -1;

        [JsonProperty]
        public bool InheritOrders { get; internal set; } = true;

        [JsonProperty]
        public SafeList<ConditionalOrder> StandingOrders { get; internal set; } = new ();

        public FleetDB() : base(null) {}

        public override object Clone()
        {
            return new FleetDB();
        }
    }
}