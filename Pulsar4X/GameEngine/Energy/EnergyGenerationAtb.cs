using Pulsar4X.Colonies;
using System;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.Factions;
using Pulsar4X.Storage;
using Newtonsoft.Json;

namespace Pulsar4X.Energy
{
    public class EnergyGenerationAtb : IComponentDesignAttribute
    {
        public string FuelType; //min or mat.

        public double FuelUsedAtMax;  //KgPerS

        public string EnergyTypeID;

        public double PowerOutputMax; //Kw

        public double Lifetime;

        /// <summary>
        /// Seconds for the output to halve (an isotope generator, RTG). 0: no decay.
        /// Such a source burns no fuel; its output just falls with age.
        /// </summary>
        public double HalfLife_s;

        /// <summary>
        /// 1: a fuel cell, burning FuelType from the ship's hold rather than fuel built into it.
        /// </summary>
        public double TankFed;

        [JsonConstructor]
        private EnergyGenerationAtb() { FuelType = ""; EnergyTypeID = ""; }

        public EnergyGenerationAtb(string fueltype, double fuelUsedAtMax, string energyTypeID, double powerOutputMax, double lifetime)
        {
            FuelType = fueltype;
            PowerOutputMax = powerOutputMax;
            FuelUsedAtMax = fuelUsedAtMax;
            EnergyTypeID = energyTypeID;
            Lifetime = lifetime;
        }

        /// <summary>
        /// Energy phase 6: also a half-life (s, isotope generators) and tankFed (1 for a fuel cell
        /// burning fueltype from the hold at fuelUsedAtMax kg/s).
        /// </summary>
        public EnergyGenerationAtb(string fueltype, double fuelUsedAtMax, string energyTypeID, double powerOutputMax, double lifetime, double halfLife_s, double tankFed)
            : this(fueltype, fuelUsedAtMax, energyTypeID, powerOutputMax, lifetime)
        {
            HalfLife_s = double.IsFinite(halfLife_s) && halfLife_s > 0 ? halfLife_s : 0;
            TankFed = tankFed > 0 ? 1 : 0;
        }

        bool IsCell => TankFed > 0;
        bool IsIsotope => !IsCell && HalfLife_s > 0;

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            // A colony's generators belong to its grid; ColonyPowerProcessor reads the installed designs.
            if (parentEntity.HasDataBlob<ColonyInfoDB>())
                return;
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
                EnergyGenProcessor.CatchUp(parentEntity, genDB);

                if (genDB.EnergyType == null)
                    genDB.EnergyType = energyCargoable;
                else if(genDB.EnergyType != energyCargoable)//this is just to reduce complexity. we can add this ability later.
                    throw new Exception("PrimeEntity cannot use two different energy types");
            }

            if (IsCell)
            {
                // Fuel cells are their own group: one propellant from the hold.
                if (string.IsNullOrEmpty(genDB.CellFuelUseAtMax.type))
                    genDB.CellFuelUseAtMax.type = FuelType;
                else if (genDB.CellFuelUseAtMax.type != FuelType)
                    throw new Exception("PrimeEntity cannot have fuel cells that use different fuel types");
                genDB.MaxOutputFromCells += PowerOutputMax;
                genDB.CellFuelUseAtMax = (FuelType, genDB.CellFuelUseAtMax.maxUse + EnergyGenAbilityDB.SaneFuel(FuelUsedAtMax));
                AddStore(genDB);
                return;
            }
            if (IsIsotope)
            {
                // Isotope generators burn nothing; their output halves every half-life from now.
                genDB.DecaySources ??= new();
                genDB.DecaySources.Add(new DecaySource(PowerOutputMax, HalfLife_s, parentEntity.StarSysDateTime));
                AddStore(genDB);
                return;
            }

            {
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
            AddStore(genDB);
        }

        void AddStore(EnergyGenAbilityDB genDB)
        {
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
            // The buffer takes and gives up to the generator's own output, without loss, so a
            // generator-only ship charges and drains its buffer as before.
            genDB.AddStoreRates(PowerOutputMax, PowerOutputMax, PowerOutputMax, 1, +1);
        }

        /// <summary>
        /// Fuel built into one reactor, in kg.
        /// </summary>
        internal double ReactorFuel() => EnergyGenAbilityDB.SaneFuel(FuelUsedAtMax * Lifetime);

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (parentEntity.HasDataBlob<ColonyInfoDB>())
                return;
            if (!parentEntity.TryGetDataBlob<EnergyGenAbilityDB>(out var genDB))
                return;
            EnergyGenProcessor.CatchUp(parentEntity, genDB);

            if (IsCell)
            {
                genDB.MaxOutputFromCells = Math.Max(0, genDB.MaxOutputFromCells - PowerOutputMax);
                double cellUse = Math.Max(0, genDB.CellFuelUseAtMax.maxUse - EnergyGenAbilityDB.SaneFuel(FuelUsedAtMax));
                bool anyLeft = genDB.MaxOutputFromCells > 1e-12;
                genDB.CellFuelUseAtMax = (anyLeft ? genDB.CellFuelUseAtMax.type : "", anyLeft ? cellUse : 0);
                if (!anyLeft)
                    genDB.CellFuelDebt_kg = 0;
                EnergyGenAbilityDB.RemoveStore(genDB, EnergyTypeID, PowerOutputMax);
                genDB.AddStoreRates(PowerOutputMax, PowerOutputMax, PowerOutputMax, 1, -1);
                return;
            }
            if (IsIsotope)
            {
                // Take out the oldest matching generator.
                var sources = genDB.DecaySources;
                if (sources != null)
                {
                    int idx = -1;
                    for (int i = 0; i < sources.Count; i++)
                    {
                        if (Math.Abs(sources[i].Output_kW - PowerOutputMax) < 1e-9 && Math.Abs(sources[i].HalfLife_s - HalfLife_s) < 1e-6
                            && (idx < 0 || sources[i].Since < sources[idx].Since))
                            idx = i;
                    }
                    if (idx >= 0)
                        sources.RemoveAt(idx);
                }
                EnergyGenAbilityDB.RemoveStore(genDB, EnergyTypeID, PowerOutputMax);
                genDB.AddStoreRates(PowerOutputMax, PowerOutputMax, PowerOutputMax, 1, -1);
                return;
            }

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
            genDB.AddStoreRates(PowerOutputMax, PowerOutputMax, PowerOutputMax, 1, -1);
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