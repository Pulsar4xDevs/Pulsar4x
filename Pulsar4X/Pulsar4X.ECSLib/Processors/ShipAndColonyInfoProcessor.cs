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
            MassVolumeDB mvDB = shipEntity.GetDataBlob<MassVolumeDB>();
            if (mvDB.MassTotal != totalTonnage)
            {
                if (shipEntity.HasDataBlob<WarpAbilityDB>())
                {
                    ShipMovementProcessor.CalcMaxWarpAndEnergyUsage(shipEntity);
                }
            }

            var armor = shipInfo.Design.Armor;
            var r = Math.Cbrt(totalVolume * 3 / 4 / Math.PI);
            var s = 4 * Math.PI * r * r;
            var v = s * armor.thickness * 0.001; //armor thickness is in mm, volume is in m^3
            var m = v * armor.type.Density;
            totalTonnage += (long)Math.Round(m);

            shipInfo.InternalHTK = totalHTK;
            
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
