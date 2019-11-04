using System.Collections.Generic;
using System.Linq;
using System;

namespace Pulsar4X.ECSLib
{

    public static class ShipMovementProcessor
    {
        public static void Initialize()
        {
        }

        

        public static double CalcMaxFuelDistance_KM(Entity shipEntity)
        {
            CargoStorageDB storedResources = shipEntity.GetDataBlob<CargoStorageDB>();
            PropulsionAbilityDB propulsionDB = shipEntity.GetDataBlob<PropulsionAbilityDB>();
            StaticDataStore staticData = shipEntity.Manager.Game.StaticData;
            ICargoable fuelResource;
            double distance = 0;
            foreach (var fuelAndUsage in propulsionDB.FuelUsePerKM)
            {
                fuelResource = staticData.GetICargoable(fuelAndUsage.Key);
                var usePerKm = fuelAndUsage.Value;
                if (storedResources.StoredCargoTypes.ContainsKey(fuelResource.CargoTypeID))
                {
                    if (storedResources.StoredCargoTypes[fuelResource.CargoTypeID].ItemsAndAmounts.ContainsKey(fuelResource.ID))
                    {
                        long fuelStored = storedResources.StoredCargoTypes[fuelResource.CargoTypeID].ItemsAndAmounts[fuelResource.ID];
                        distance += fuelStored / fuelAndUsage.Value;
                    }
                }
            }
            return distance;
        }




        /// <summary>
        /// recalculates a shipsMaxSpeed.
        /// </summary>
        /// <param name="ship"></param>
        public static void CalcMaxWarpAndEnergyUsage(Entity ship)
        {
            int totalEnginePower = 0;
            Dictionary<Guid, double> totalFuelUsage = new Dictionary<Guid, double>();
            var instancesDB = ship.GetDataBlob<ComponentInstancesDB>();
            var designs = instancesDB.GetDesignsByType(typeof(WarpDriveAtb));
            
            //TODO: this is how fuel was calculated, currently power use is static, but will revisit this.
            
            foreach (var design in designs)
            {
                var warpAtb = design.GetAttribute<WarpDriveAtb>();
                foreach (var instanceInfo in instancesDB.GetComponentsBySpecificDesign(design.Guid))
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
            
            //Note: TN aurora uses the TCS for max speed calcs. 
            WarpAbilityDB warpDB = ship.GetDataBlob<WarpAbilityDB>();
            warpDB.TotalWarpPower = totalEnginePower;
            //propulsionDB.FuelUsePerKM = totalFuelUsage;
            var mass = ship.GetDataBlob<ShipInfoDB>().Tonnage;
            var maxSpeed = MaxSpeedCalc(totalEnginePower, mass);
            warpDB.MaxSpeed = maxSpeed;
            
        }

        public static int MaxSpeedCalc(double power, double tonage)
        {
          // From Aurora4x wiki:  Speed = (Total Engine Power / Total Class Size in HS) * 1000 km/s
          return (int)((power / tonage) * 1000);
        }
    }
}