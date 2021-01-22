using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class MineResourcesAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        public Dictionary<Guid, long> ResourcesPerEconTick { get; internal set; }

        public MineResourcesAtbDB() { }

        /// <summary>
        /// Component factory constructor.
        /// </summary>
        /// <param name="resources">values will be cast to longs!</param>
        public MineResourcesAtbDB(Dictionary<Guid, double> resources)
        {
            ResourcesPerEconTick = new Dictionary<Guid, long>();
            foreach (var kvp in resources)
            {
                ResourcesPerEconTick.Add(kvp.Key,(long)kvp.Value);
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
        
        public string AtbName()
        {
            return "Resource Mining";
        }

        public string AtbDescription()
        {
            
            string resources = "";
            foreach (var kvp in ResourcesPerEconTick)
            {
                var name =StaticRefLib.StaticData.CargoGoods.GetAny(kvp.Key).Name;
                var amount = kvp.Value;

                resources += name + ", amount. ";
            }
            
            return "Gathers " + resources;
        }
    }
}