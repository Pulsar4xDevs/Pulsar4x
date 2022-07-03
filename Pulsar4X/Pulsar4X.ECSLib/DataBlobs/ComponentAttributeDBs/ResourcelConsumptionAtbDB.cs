using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ResourceConsumptionAtbDB : IComponentDesignAttribute
    {
        [JsonProperty]
        public Dictionary<Guid, int> MaxUsage { get; internal set; } = new Dictionary<Guid, int>();
        [JsonProperty]
        public Dictionary<Guid, int> MinUsage { get; internal set; } = new Dictionary<Guid, int>();

        public ResourceConsumptionAtbDB()
        {
        }

        public ResourceConsumptionAtbDB(Guid resourcetype, double maxUsage, double minUsage)
        {
            MaxUsage.Add(resourcetype, (int)maxUsage);
            MinUsage.Add(resourcetype, (int)minUsage);
        }

        public ResourceConsumptionAtbDB(Dictionary<Guid, double> maxUsage, Dictionary<Guid, double> minUsage)
        {
            foreach (var kvp in maxUsage)
            {
                MaxUsage.Add(kvp.Key, (int)kvp.Value);
            }
            foreach (var kvp in minUsage)
            {
                MinUsage.Add(kvp.Key, (int)kvp.Value);
            }       
        }

        public ResourceConsumptionAtbDB(Dictionary<Guid,int> maxUsage, Dictionary<Guid,int> minUsage)
        {
            MaxUsage = maxUsage;
            MinUsage = minUsage;
        }

        public ResourceConsumptionAtbDB(ResourceConsumptionAtbDB db)
        {
            MaxUsage = new Dictionary<Guid, int>(db.MaxUsage);
            MinUsage = new Dictionary<Guid, int>(db.MinUsage);
        }

        public object Clone()
        {
            return new ResourceConsumptionAtbDB(this);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            
        }
        
        public string AtbName()
        {
            return "Resource Consumption";
        }

        public string AtbDescription()
        {

            return " ";
        }
    }
}