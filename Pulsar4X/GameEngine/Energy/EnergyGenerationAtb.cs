using System;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.Energy
{
    public class EnergyGenerationAtb : IComponentDesignAttribute
    {
        public string FuelType; //min or mat.

        public double FuelUsedAtMax;  //KgPerS

        public string EnergyTypeID;

        public double PowerOutputMax; //Kw

        public double Lifetime;

        public EnergyGenerationAtb(string fueltype, double fuelUsedAtMax, string energyTypeID, double powerOutputMax, double lifetime)
        {
            FuelType = fueltype;
            PowerOutputMax = powerOutputMax;
            FuelUsedAtMax = fuelUsedAtMax;
            EnergyTypeID = energyTypeID;
            Lifetime = lifetime;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            string resourceID = EnergyTypeID;
            ICargoable? energyCargoable = EnergyGenAbilityDB.FindEnergyType(parentEntity, resourceID);
            EnergyGenAbilityDB genDB;
            if (!parentEntity.HasDataBlob<EnergyGenAbilityDB>())
            {
                genDB = new EnergyGenAbilityDB(parentEntity.StarSysDateTime);
                genDB.EnergyType = energyCargoable;
                parentEntity.SetDataBlob(genDB);
            }
            else
            {
                genDB = parentEntity.GetDataBlob<EnergyGenAbilityDB>();


                if (genDB.EnergyType == null)
                    genDB.EnergyType = energyCargoable;
                else if(genDB.EnergyType != energyCargoable)//this is just to reduce complexity. we can add this ability later.
                    throw new Exception("PrimeEntity cannot use two different energy types");
                if (genDB.TotalFuelUseAtMax.type == String.Empty || genDB.TotalFuelUseAtMax.type == null)
                    genDB.TotalFuelUseAtMax.type = FuelType;
                else if(genDB.TotalFuelUseAtMax.type != FuelType)
                    throw new Exception("PrimeEntity cannot have power plants that use different fuel types");
            }

            genDB.MaxOutputFromReactor += PowerOutputMax;
            double maxUse = genDB.TotalFuelUseAtMax.maxUse + FuelUsedAtMax;
            genDB.TotalFuelUseAtMax = (FuelType, maxUse);
            // Each reactor brings its own fuel: FuelUsedAtMax kg/s for Lifetime s.
            genDB.LocalFuel = EnergyGenAbilityDB.SaneFuel(genDB.LocalFuel) + ReactorFuel();

            //add enough energy store for 1s of running.
            if (genDB.EnergyStoreMax.ContainsKey(EnergyTypeID))
            {
                genDB.EnergyStoreMax[EnergyTypeID] += PowerOutputMax;
            }
            else
            {
                genDB.EnergyStored[EnergyTypeID] = 0;
                genDB.EnergyStoreMax[EnergyTypeID] = PowerOutputMax;
            }

        }

        /// <summary>
        /// Fuel built into one reactor, in kg.
        /// </summary>
        internal double ReactorFuel() => EnergyGenAbilityDB.SaneFuel(FuelUsedAtMax * Lifetime);

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.TryGetDataBlob<EnergyGenAbilityDB>(out var genDB))
                return;
            EnergyGenProcessor.CatchUp(parentEntity, genDB);

            // The reactor takes its share of the fuel left. Reactors burn in proportion to
            // FuelUsedAtMax, so that is its share.
            double maxUseBefore = genDB.TotalFuelUseAtMax.maxUse;
            double maxUse = Math.Max(0, maxUseBefore - FuelUsedAtMax);
            double fuel = EnergyGenAbilityDB.SaneFuel(genDB.LocalFuel);
            if (maxUseBefore > 0 && maxUse > 0)
                genDB.LocalFuel = fuel * (maxUse / maxUseBefore);
            else
                genDB.LocalFuel = 0;

            genDB.MaxOutputFromReactor = Math.Max(0, genDB.MaxOutputFromReactor - PowerOutputMax);
            genDB.TotalFuelUseAtMax = (maxUse > 0 ? genDB.TotalFuelUseAtMax.type : "", maxUse);

            // And the 1 s of output it added to the store.
            EnergyGenAbilityDB.RemoveStore(genDB, EnergyTypeID, PowerOutputMax);
        }

        public string AtbName()
        {
            return "Energy Generation";
        }

        public string AtbDescription()
        {
            //string fuelName = StaticRefLib.StaticData.CargoGoods.GetAny(FuelType).Name;
            return "Generates " + PowerOutputMax + " kW, using: " + FuelUsedAtMax + "kg/s of ";// + fuelName;
        }
    }
}