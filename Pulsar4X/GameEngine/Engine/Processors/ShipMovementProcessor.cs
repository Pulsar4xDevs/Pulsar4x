using System.Collections.Generic;
using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine
{

    public static class ShipMovementProcessor
    {
        public static void Initialize()
        {
        }

        
        /*
         This was used when warp drive used fuel instead of energy. 
         keeping it around incase we do a simular non newtonion drive that uses fuel. 
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
        }*/

        /// <summary>
        /// recalculates a shipsMaxSpeed.
        /// </summary>
        /// <param name="ship"></param>
        public static void CalcMaxWarpAndEnergyUsage(Entity ship)
        {
            Dictionary<string, double> totalFuelUsage = new Dictionary<string, double>();
            var instancesDB = ship.GetDataBlob<ComponentInstancesDB>();
            int totalEnginePower = instancesDB.GetTotalEnginePower(out totalFuelUsage);
            
            //Note: TN aurora uses the TCS for max speed calcs. 
            WarpAbilityDB warpDB = ship.GetDataBlob<WarpAbilityDB>();
            warpDB.TotalWarpPower = totalEnginePower;
            //propulsionDB.FuelUsePerKM = totalFuelUsage;

            var mass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var maxSpeed = MaxSpeedCalc(totalEnginePower, mass);
            warpDB.MaxSpeed = maxSpeed;
            
        }

        /// <summary>
        /// Calculates max ship speed based on engine power and ship mass
        /// </summary>
        /// <param name="power">TotalEnginePower</param>
        /// <param name="tonage">HullSize</param>
        /// <returns>Max speed in km/s</returns>
        public static int MaxSpeedCalc(double power, double tonage)
        {
          // From Aurora4x wiki:  Speed = (Total Engine Power / Total Class Size in HS) * 1000 km/s
          return (int)((power / tonage) * 1000);
        }
    }
}