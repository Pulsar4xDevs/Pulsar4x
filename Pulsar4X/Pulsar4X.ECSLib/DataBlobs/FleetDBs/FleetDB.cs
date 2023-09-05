using System;

namespace Pulsar4X.ECSLib
{
    public class FleetDB : TreeHierarchyDB
    {
        public Guid FlagShipID { get; internal set; } = Guid.Empty;

        public FleetDB() : base(null) {}

        public override object Clone()
        {
            return new FleetDB();
        }
    }
}