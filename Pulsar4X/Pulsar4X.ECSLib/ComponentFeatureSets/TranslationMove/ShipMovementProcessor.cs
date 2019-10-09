using System.Collections.Generic;
using System.Linq;
using System;

namespace Pulsar4X.ECSLib
{

    internal static class ShipMovementProcessor
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
        public static void CalcMaxSpeedAndFuelUsage(Entity ship)
        {
            int totalEnginePower = 0;
            Dictionary<Guid, double> totalFuelUsage = new Dictionary<Guid, double>();
            var instancesDB = ship.GetDataBlob<ComponentInstancesDB>();
            var designs = instancesDB.GetDesignsByType(typeof(WarpEnginePowerAtbDB));
            foreach (var design in designs)
            {
                foreach (var instanceInfo in instancesDB.GetComponentsBySpecificDesign(design.Guid))
                {
                    var power = (WarpEnginePowerAtbDB)instanceInfo.Design.AttributesByType[typeof(WarpEnginePowerAtbDB)];
                    var fuelUsage = (ResourceConsumptionAtbDB)instanceInfo.Design.AttributesByType[typeof(ResourceConsumptionAtbDB)];
                    if (instanceInfo.IsEnabled)
                    {
                        totalEnginePower += (int)(power.EnginePower * instanceInfo.HealthPercent());
                        foreach (var item in fuelUsage.MaxUsage)
                        {
                            totalFuelUsage.SafeValueAdd(item.Key, item.Value);
                        }
                    }
                }
            }



            /*
            List<KeyValuePair<Entity,PrIwObsList<Entity>>> engineEntities = instancesDB.SpecificInstances.GetInternalDictionary().Where(item => item.Key.HasDataBlob<EnginePowerAtbDB>()).ToList();
            foreach (var engineDesign in engineEntities)
            {
                foreach (var engineInstance in engineDesign.Value)
                {
                    //todo check if it's damaged
                    totalEnginePower += engineDesign.Key.GetDataBlob<EnginePowerAtbDB>().EnginePower;
                    foreach (var kvp in engineDesign.Key.GetDataBlob<ResourceConsumptionAtbDB>().MaxUsage)
                    {
                        totalFuelUsage.SafeValueAdd(kvp.Key, kvp.Value);
                    }                    
                }
            }*/

            //Note: TN aurora uses the TCS for max speed calcs. 
            PropulsionAbilityDB propulsionDB = ship.GetDataBlob<PropulsionAbilityDB>();
            propulsionDB.TotalEnginePower = totalEnginePower;
            propulsionDB.FuelUsePerKM = totalFuelUsage;
            var mass = ship.GetDataBlob<ShipInfoDB>().Tonnage;
            var maxSpeed = MaxSpeedCalc(totalEnginePower, mass);
            propulsionDB.MaximumSpeed_MS = maxSpeed;
        }

        public static int MaxSpeedCalc(float power, float tonage)
        {
          // From Aurora4x wiki:  Speed = (Total Engine Power / Total Class Size in HS) * 1000 km/s
          return (int)((power / tonage) * 1000);
        }
    }
}