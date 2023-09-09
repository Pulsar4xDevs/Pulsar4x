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
            
            
            long dryMass = componentInstances.GetTotalDryMass();
            double totalVolume = componentInstances.GetTotalVolume();
            MassVolumeDB mvDB = shipEntity.GetDataBlob<MassVolumeDB>();


            var armorMass = ShipDesign.GetArmorMass(shipEntity.GetDataBlob<EntityDamageProfileDB>());
            dryMass += (long)Math.Round(armorMass);
            shipInfo.InternalHTK = totalHTK;
            
            mvDB.MassDry = dryMass;
            mvDB.Volume_m3 = totalVolume;
            mvDB.DensityDry_gcm = MassVolumeDB.CalculateDensity(dryMass, totalVolume);
            mvDB.RadiusInAU = MassVolumeDB.CalculateRadius_Au(dryMass, mvDB.DensityDry_gcm);
            if (shipEntity.TryGetDatablob<VolumeStorageDB>(out var storedb))
                mvDB.UpdateMassTotal(storedb);
            if (shipEntity.HasDataBlob<WarpAbilityDB>())
            {
                ShipMovementProcessor.CalcMaxWarpAndEnergyUsage(shipEntity);
            }
            
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
