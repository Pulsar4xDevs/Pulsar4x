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
            int totalHTK = componentInstances.GetTotalHTK();
            float totalTonnage = componentInstances.GetTotalTonnage();
            double totalVolume = componentInstances.GetTotalVolume();
            
            if (shipInfo.Tonnage != totalTonnage)
            {
                shipInfo.Tonnage = totalTonnage;
                if (shipEntity.HasDataBlob<WarpAbilityDB>())
                {
                    ShipMovementProcessor.CalcMaxWarpAndEnergyUsage(shipEntity);
                }
            }

            shipInfo.InternalHTK = totalHTK;
            MassVolumeDB mvDB = shipEntity.GetDataBlob<MassVolumeDB>();
            mvDB.MassDry = totalTonnage;
            mvDB.Volume_m3 = totalVolume;
            mvDB.DensityDry_gcm = MassVolumeDB.CalculateDensity(totalTonnage, totalVolume);
            mvDB.RadiusInAU = MassVolumeDB.CalculateRadius_Au(totalTonnage, mvDB.DensityDry_gcm);
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
