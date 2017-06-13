using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// ship and colonyInfo processors
    /// </summary>
    public static class ShipAndColonyInfoProcessor
    {
        public static void ReCalculateShipTonnaageAndHTK(Entity shipEntity)
        {
            ShipInfoDB shipInfo = shipEntity.GetDataBlob<ShipInfoDB>();
            ComponentInstancesDB componentInstances = shipEntity.GetDataBlob<ComponentInstancesDB>();
            float totalTonnage = 0;
            int totalHTK = 0;
            double totalVolume = 0;
            foreach (var componentDesign in componentInstances.SpecificInstances)
            {                
                var componentVolume = componentDesign.Key.GetDataBlob<MassVolumeDB>().Volume;
                var componentTonnage = componentDesign.Key.GetDataBlob<ComponentInfoDB>().SizeInTons;
                
                foreach (var componentInstance in componentDesign.Value)
                {
                    totalHTK += componentInstance.GetDataBlob<ComponentInstanceInfoDB>().HTKRemaining; 
                    totalVolume += componentVolume;
                    totalTonnage += componentTonnage;
                    if (!componentInstances.ComponentDictionary.ContainsKey(componentInstance))
                        componentInstances.ComponentDictionary.Add(componentInstance, totalVolume);
                }
            }
            if (shipInfo.Tonnage != totalTonnage)
            {
                shipInfo.Tonnage = totalTonnage;
                PropulsionCalcs.CalcMaxSpeed(shipEntity); 
            }
            shipInfo.InternalHTK = totalHTK;
            MassVolumeDB mvDB = shipEntity.GetDataBlob<MassVolumeDB>();
            mvDB.Volume = totalVolume;
        }
    }
}
