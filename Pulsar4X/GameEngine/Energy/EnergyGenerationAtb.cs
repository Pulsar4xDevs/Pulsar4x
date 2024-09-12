using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Atb
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
            ICargoable? energyCargoable = parentEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(resourceID);
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

            genDB.TotalOutputMax += PowerOutputMax;
            double maxUse = genDB.TotalFuelUseAtMax.maxUse + FuelUsedAtMax;
            genDB.TotalFuelUseAtMax = (FuelType, maxUse);
            genDB.LocalFuel = maxUse * Lifetime;

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

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {

        }

        public string AtbName()
        {
            return "Energy Generation";
        }

        public string AtbDescription()
        {
            //string fuelName = StaticRefLib.StaticData.CargoGoods.GetAny(FuelType).Name;
            return "Generates " + PowerOutputMax + " Mw, using: " + FuelUsedAtMax + "kg/s of ";// + fuelName;
        }
    }
}