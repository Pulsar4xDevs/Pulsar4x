using System;

namespace Pulsar4X.ECSLib
{
    public class NavyDB : TreeHierarchyDB
    {
        public Guid FlagShipID { get; internal set; } = Guid.Empty;

        public NavyDB() : base(null) {}

        public override object Clone()
        {
            return new NavyDB();
        }
    }
}