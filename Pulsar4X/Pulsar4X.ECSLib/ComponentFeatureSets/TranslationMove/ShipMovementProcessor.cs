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



        /// <summary>
        /// Creates orbit here using the current distance between the two entites as aphelion(furthest distance) and a given semiMajorAxis
        /// *NOTE BUG* this only returns a correct orbit DB if the position is y=0 and is +x (ie the position is in the reference direction)
        /// </summary>
        /// <returns>An OrbitDB. Does Not set DB to Entity.</returns>
        /// <param name="shipEntity">Ship entity.</param>
        /// <param name="parentEntity">The Entity to orbit</param>
        /// <param name="semiMajorAxsis">Largest Radius</param>
        public static OrbitDB CreateOrbitHereWithSemiMajAxis(Entity shipEntity, Entity parentEntity, double semiMajAxsisKM, DateTime time)
        {
            PositionDB parentPosition = parentEntity.GetDataBlob<PositionDB>();
            PositionDB myPosition = shipEntity.GetDataBlob<PositionDB>();
            double parentMass = parentEntity.GetDataBlob<MassVolumeDB>().Mass;
            double myMass = shipEntity.GetDataBlob<MassVolumeDB>().Mass;
            double aphelionAU = PositionDB.GetDistanceBetween(parentPosition, myPosition);
            double semiMajAxisAU = semiMajAxsisKM / GameConstants.Units.KmPerAu;
            double linierEcentricity = aphelionAU - semiMajAxisAU;
            double semiMinorAxsis =  Math.Sqrt(Math.Pow(semiMajAxisAU, 2) - Math.Pow(linierEcentricity, 2));
            double ecentricity = linierEcentricity / semiMajAxisAU;
            Vector4 ralitivePos = myPosition.RelativePosition_AU; 
             
            double angle = Math.Atan2(ralitivePos.Y, ralitivePos.X); 
            var theta = Angle.ToDegrees(angle);
            OrbitDB newOrbit = OrbitDB.FromAsteroidFormat(parentEntity, parentMass, myMass, semiMajAxisAU, ecentricity, 0, 0, angle, angle, time);
            var pos = OrbitProcessor.GetPosition_AU(newOrbit, time);
            var pos2 = Distance.AuToKm(pos);
            return newOrbit;
        }

        /// <summary>
        /// Creates orbit here using the current distance between the two entites as aphelion(furthest distance) and a given perihelion
        /// </summary>
        /// <returns>An OrbitDB. Does Not set DB to Entity.</returns>
        /// <param name="shipEntity">Ship entity.</param>
        /// <param name="parentEntity">The Entity to orbit</param>
        /// <param name="perihelionKM">closest distance to the parent in KM</param>
        public static OrbitDB CreateOrbitHereWithPerihelion(Entity shipEntity, Entity parentEntity, double perihelionKM, DateTime time)
        {
            PositionDB parentPosition = parentEntity.GetDataBlob<PositionDB>();
            PositionDB myPosition = shipEntity.GetDataBlob<PositionDB>();
            double parentMass = parentEntity.GetDataBlob<MassVolumeDB>().Mass;
            double myMass = shipEntity.GetDataBlob<MassVolumeDB>().Mass;
            double aphelionAU = PositionDB.GetDistanceBetween(parentPosition, myPosition);
            double perihelionAU = perihelionKM / GameConstants.Units.KmPerAu;
            double semiMajorAxsis = (perihelionAU + aphelionAU) / 2 ;
            double linierEcentricity = aphelionAU - semiMajorAxsis;
            double ecentricity = linierEcentricity / semiMajorAxsis;
            Vector4 ralitivePos = (myPosition.AbsolutePosition_AU - parentPosition.AbsolutePosition_AU);
            double inclination = 0;
            double loAN = 0; //longditude of Acending Node
            double aoP = Math.Tan(ralitivePos.X / ralitivePos.Y); ; //arguemnt of Periapsis
            //double ecentricAnomaly = 0;
            double meanAnomaly = 0; //ecentricAnomaly - ecentricity * Math.Sin(ecentricAnomaly);
            OrbitDB newOrbit = OrbitDB.FromAsteroidFormat(parentEntity, parentMass, myMass, semiMajorAxsis, ecentricity, inclination, loAN, aoP, meanAnomaly, time);
            return newOrbit;
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
            var designs = instancesDB.GetDesignsByType(typeof(EnginePowerAtbDB));
            foreach (var design in designs)
            {
                foreach (var instanceInfo in instancesDB.GetComponentsBySpecificDesign(design.Guid))
                {
                    var power = instanceInfo.DesignEntity.GetDataBlob<EnginePowerAtbDB>();
                    var fuelUsage = instanceInfo.DesignEntity.GetDataBlob<ResourceConsumptionAtbDB>();
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