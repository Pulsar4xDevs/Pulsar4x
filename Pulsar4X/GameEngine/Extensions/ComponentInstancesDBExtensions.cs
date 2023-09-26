using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Components;
using Pulsar4X.Atb;

namespace Pulsar4X.Extensions
{
    public static class ComponentInstancesDBExtensions
    {
        public static long GetPopulationSupportValue(this ComponentInstancesDB componentInstances)
        {
            var infrustructureDesigns = componentInstances.GetDesignsByType(typeof(PopulationSupportAtbDB));

            //List<KeyValuePair<Entity, PrIwObsList<Entity>>> infrastructure = instancesDB.ComponentsByDesign.GetInternalDictionary().Where(item => item.Key.HasDataBlob<PopulationSupportAtbDB>()).ToList();
            long popSupportValue = 0;

            //  Pop Cap = Total Population Support Value / Colony Cost
            // Get total popSupport
            popSupportValue = 0;


            foreach (var design in infrustructureDesigns)
            {
                var componentCapacity = design.GetAttribute<PopulationSupportAtbDB>().PopulationCapacity;
                foreach (var component in componentInstances.GetComponentsBySpecificDesign(design.UniqueID).Where(c => c.IsEnabled))
                {
                    popSupportValue += (long)(componentCapacity * component.HealthPercent());
                }
            }

            return popSupportValue;
        }

        public static int GetTotalHTK(this ComponentInstancesDB componentInstances)
        {
            int totalHTK = 0;

            foreach (KeyValuePair<string, List<ComponentInstance>> instance in componentInstances.GetComponentsByDesigns())
            {
                instance.Value.ForEach(x => totalHTK += x.HTKRemaining);
            }

            return totalHTK;
        }

        public static long GetTotalDryMass(this ComponentInstancesDB componentInstances)
        {
            long totalTonnage = 0;

            foreach (KeyValuePair<string, List<ComponentInstance>> instance in componentInstances.GetComponentsByDesigns())
            {
                var componentTonnage = componentInstances.AllDesigns[instance.Key].MassPerUnit;
                instance.Value.ForEach(x => totalTonnage += componentTonnage);
            }

            return totalTonnage;
        }

        public static double GetTotalVolume(this ComponentInstancesDB componentInstances)
        {
            double totalVolume = 0;

            foreach (KeyValuePair<string, List<ComponentInstance>> instance in componentInstances.GetComponentsByDesigns())
            {
                var componentVolume = componentInstances.AllDesigns[instance.Key].VolumePerUnit;
                instance.Value.ForEach(x => totalVolume += componentVolume);
            }

            return totalVolume;
        }

        public static int GetTotalEnginePower(this ComponentInstancesDB instancesDB, out Dictionary<string, double> totalFuelUsage)
        {
            int totalEnginePower = 0;
            totalFuelUsage = new Dictionary<string, double>();
            var designs = instancesDB.GetDesignsByType(typeof(WarpDriveAtb));

            //TODO: this is how fuel was calculated, currently power use is static, but will revisit this.

            foreach (var design in designs)
            {
                var warpAtb = design.GetAttribute<WarpDriveAtb>();
                foreach (var instanceInfo in instancesDB.GetComponentsBySpecificDesign(design.UniqueID))
                {
                    var warpAtb2 = (WarpDriveAtb)instanceInfo.Design.AttributesByType[typeof(WarpDriveAtb)];
                    //var fuelUsage = (ResourceConsumptionAtbDB)instanceInfo.Design.AttributesByType[typeof(ResourceConsumptionAtbDB)];
                    if (instanceInfo.IsEnabled)
                    {
                        totalEnginePower += (int)(warpAtb.WarpPower * instanceInfo.HealthPercent());
                        //foreach (var item in fuelUsage.MaxUsage)
                        //{
                        //    totalFuelUsage.SafeValueAdd(item.Key, item.Value);
                        //}
                    }
                }
            }

            return totalEnginePower;
        }
    }
}
