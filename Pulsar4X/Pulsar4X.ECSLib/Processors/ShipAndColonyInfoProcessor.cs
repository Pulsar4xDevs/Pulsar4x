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
            foreach (var componentDesign in componentInstances.SpecificInstances)
            {
                totalTonnage = componentDesign.Key.GetDataBlob<ComponentInfoDB>().SizeInTons;
                foreach (var componentInstance in componentDesign.Value)
                {
                    totalHTK = componentInstance.GetDataBlob<ComponentInstanceInfoDB>().HTKRemaining;
                }
            }
            if (shipInfo.Tonnage != totalTonnage)
            {
                shipInfo.Tonnage = totalTonnage;
                ShipMovementProcessor.CalcMaxSpeed(shipEntity);
            }
            shipInfo.InternalHTK = totalHTK;
        }
    }
}
