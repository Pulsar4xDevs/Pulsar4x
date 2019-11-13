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
            
            foreach (KeyValuePair<Guid, List<ComponentInstance>> instance in componentInstances.GetComponentsByDesigns())
            {                
                var componentVolume = componentInstances.AllDesigns[instance.Key].Volume;
                var componentTonnage = componentInstances.AllDesigns[instance.Key].Mass;
                
                foreach (var componentInstance in instance.Value)
                {
                    
                    totalHTK += componentInstance.HTKRemaining; 
                    totalVolume += componentVolume;
                    totalTonnage += componentTonnage;
                }
            }
            if (shipInfo.Tonnage != totalTonnage)
            {
                shipInfo.Tonnage = totalTonnage;
                if(shipEntity.HasDataBlob<NewtonThrustAbilityDB>())
                    ShipMovementProcessor.CalcMaxWarpAndEnergyUsage(shipEntity);
            }
            shipInfo.InternalHTK = totalHTK;
            MassVolumeDB mvDB = shipEntity.GetDataBlob<MassVolumeDB>();
            mvDB.Mass = totalTonnage;
            mvDB.Volume = totalVolume;
            mvDB.Density = MassVolumeDB.CalculateDensity(totalTonnage, totalVolume);
            mvDB.RadiusInAU = MassVolumeDB.CalculateRadius(totalTonnage, mvDB.Density);
            
        }
    }

    public class TonnageAndHTKRecalc : IRecalcProcessor
    {
        public byte ProcessPriority { get; set; } = 0;
        public void RecalcEntity(Entity entity)
        {
            ShipAndColonyInfoProcessor.ReCalculateShipTonnaageAndHTK(entity);
        }
    }
}
