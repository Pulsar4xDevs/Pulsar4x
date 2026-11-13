using System;
using Newtonsoft.Json;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// A battery: capacity, how fast it charges and discharges, and the share of the charging
    /// energy it keeps. The round-trip loss is booked on the charge side (see <see cref="ChargeEfficiency"/>).
    /// </summary>
    public class EnergyStoreAtb : IComponentDesignAttribute
    {
        //<type, amount>
        public string EnergyTypeID;
        /// <summary>
        /// In Kjouls
        /// </summary>
        public double MaxStore;

        /// <summary>
        /// Highest power the battery takes in, kW (energy drawn from the plant or grid). 0 means
        /// unrated: a design from before battery rates, treated as no limit.
        /// </summary>
        public double MaxCharge_kW;

        /// <summary>Highest power the battery gives out, kW. 0 means unrated (no limit).</summary>
        public double MaxDischarge_kW;

        /// <summary>
        /// Share of the charging energy that ends up stored, 0..1. The whole round-trip loss sits
        /// here and discharge is lossless, so a stored kJ is a kJ the loads can draw: the warp
        /// creation check (stored at least the bubble cost) and the time-to-empty stay exact.
        /// 1 for an unrated battery.
        /// </summary>
        public double ChargeEfficiency = 1;

        /// <summary>A battery with no rates (older designs and mods): no limits, no loss.</summary>
        public EnergyStoreAtb(string energyTypeID, double maxStore)
            : this(energyTypeID, maxStore, 0, 0, 1)
        {
        }

        [JsonConstructor]
        public EnergyStoreAtb(string energyTypeID, double maxStore, double maxCharge_kW, double maxDischarge_kW, double chargeEfficiency)
        {
            EnergyTypeID = energyTypeID;
            MaxStore = maxStore;
            MaxCharge_kW = double.IsFinite(maxCharge_kW) && maxCharge_kW > 0 ? maxCharge_kW : 0;
            MaxDischarge_kW = double.IsFinite(maxDischarge_kW) && maxDischarge_kW > 0 ? maxDischarge_kW : 0;
            // A save from before rates has no efficiency (reads 0): that battery is lossless.
            ChargeEfficiency = double.IsFinite(chargeEfficiency) && chargeEfficiency > 0 ? Math.Min(1, chargeEfficiency) : 1;
        }

        /// <summary>True when both rates are set. An unrated battery puts no limit on the store.</summary>
        [JsonIgnore]
        public bool IsRated => MaxCharge_kW > 0 && MaxDischarge_kW > 0;

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            // A colony's batteries belong to its grid; ColonyPowerProcessor reads the installed designs.
            if (parentEntity.HasDataBlob<ColonyInfoDB>())
                return;
            EnergyGenAbilityDB genDB;

            if (!parentEntity.HasDataBlob<EnergyGenAbilityDB>())
            {
                genDB = new EnergyGenAbilityDB(parentEntity.StarSysDateTime);
                parentEntity.SetDataBlob(genDB);
            }
            else
            {
                genDB = parentEntity.GetDataBlob<EnergyGenAbilityDB>();
                EnergyGenProcessor.CatchUp(parentEntity, genDB);
            }
            if (genDB.EnergyStoreMax.ContainsKey(EnergyTypeID))
            {
                genDB.EnergyStoreMax[EnergyTypeID] += MaxStore;
            }
            else
            {
                genDB.EnergyStored[EnergyTypeID] = 0;
                genDB.EnergyStoreMax[EnergyTypeID] = MaxStore;
            }
            genDB.AddStoreRates(this, +1);
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (parentEntity.HasDataBlob<ColonyInfoDB>())
                return;
            if (!parentEntity.TryGetDataBlob<EnergyGenAbilityDB>(out var genDB))
                return;
            EnergyGenProcessor.CatchUp(parentEntity, genDB);
            EnergyGenAbilityDB.RemoveStore(genDB, EnergyTypeID, MaxStore);
            genDB.AddStoreRates(this, -1);
        }

        public string AtbName()
        {
            return "Energy Storage";
        }

        public string AtbDescription()
        {
            if (!IsRated)
                return "Adds " + MaxStore + " kJ Energy Storage to parent";
            return "Adds " + MaxStore + " kJ Energy Storage to parent, charging at up to " + MaxCharge_kW.ToString("0.#")
                + " kW and discharging at up to " + MaxDischarge_kW.ToString("0.#") + " kW ("
                + ChargeEfficiency.ToString("0%") + " round trip)";
        }
    }
}
