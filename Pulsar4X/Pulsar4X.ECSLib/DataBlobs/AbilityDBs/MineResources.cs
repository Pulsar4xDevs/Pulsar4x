using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class MineResourcesDB : BaseDataBlob
    {
        public Dictionary<Guid, int> ResourcesPerEconTick { get; internal set; }

        public MineResourcesDB() { }

        /// <summary>
        /// Component factory constructor.
        /// </summary>
        /// <param name="resources">values will be cast to ints!</param>
        public MineResourcesDB(Dictionary<Guid,double> resources)
        {
            ResourcesPerEconTick = new Dictionary<Guid, int>();
            foreach (var kvp in resources)
            {
                ResourcesPerEconTick.Add(kvp.Key,(int)kvp.Value);
            }
        }

        public MineResourcesDB(MineResourcesDB db)
        {
            ResourcesPerEconTick = db.ResourcesPerEconTick;
        }

        public override object Clone()
        {
            return new MineResourcesDB(this);
        }
    }
}