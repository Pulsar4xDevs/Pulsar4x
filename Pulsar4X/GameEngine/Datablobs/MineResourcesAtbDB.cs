using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;

namespace Pulsar4X.Datablobs
{
    public class MineResourcesAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        public Dictionary<string, long> ResourcesPerEconTick { get; internal set; }

        public MineResourcesAtbDB() { }

        /// <summary>
        /// Component factory constructor.
        /// </summary>
        /// <param name="resources">values will be cast to longs!</param>
        public MineResourcesAtbDB(Dictionary<string, double> resources)
        {
            ResourcesPerEconTick = new Dictionary<string, long>();
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
            // FIXME:
            //string time = StaticRefLib.Game.Settings.EconomyCycleTime.ToString();
            string desc = "Adds to Resource Mining Ability at Rates of: \n";
            foreach (var kvp in ResourcesPerEconTick)
            {
                //string resourceName = StaticRefLib.StaticData.CargoGoods.GetMineral(kvp.Key).Name;
                //desc += resourceName + "\t" + Stringify.Number(kvp.Value) + "\n";
            }

            return desc;// + "per " + time;
        }
    }
}