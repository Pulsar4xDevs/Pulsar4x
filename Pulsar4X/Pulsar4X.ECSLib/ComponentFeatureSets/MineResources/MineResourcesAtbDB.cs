using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class MineResourcesAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        public Dictionary<Guid, int> ResourcesPerEconTick { get; internal set; }

        public MineResourcesAtbDB() { }

        /// <summary>
        /// Component factory constructor.
        /// </summary>
        /// <param name="resources">values will be cast to ints!</param>
        public MineResourcesAtbDB(Dictionary<Guid,double> resources)
        {
            ResourcesPerEconTick = new Dictionary<Guid, int>();
            foreach (var kvp in resources)
            {
                ResourcesPerEconTick.Add(kvp.Key,(int)kvp.Value);
            }
        }

        public MineResourcesAtbDB(MineResourcesAtbDB db)
        {
            ResourcesPerEconTick = db.ResourcesPerEconTick;
        }

        public override object Clone()
        {
            return new MineResourcesAtbDB(this);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<MiningDB>())
                parentEntity.SetDataBlob(new MiningDB());
            MineResourcesProcessor.CalcMaxRate(parentEntity);
        }
    }
}