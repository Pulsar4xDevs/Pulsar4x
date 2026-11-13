using System;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// A colony power plant that burns a fuel. Electric output P_e = Size; fuel burn at full
    /// output = P_e / (efficiency * LHV). Each plant burns one fuel; a colony grid can mix
    /// plants on different fuels. Read by <see cref="ColonyPowerProcessor"/>, whose daily pass
    /// burns the fuel from the colony stockpile.
    /// </summary>
    public class PowerPlantAtb : IComponentDesignAttribute
    {
        /// <summary>Cargo id of the fuel.</summary>
        [JsonProperty]
        public string FuelId { get; internal set; } = "";

        /// <summary>Lower heating value of the fuel, MJ/kg.</summary>
        [JsonProperty]
        public double LHV_MJperKg { get; internal set; }

        /// <summary>Thermal to electric conversion, 0..1.</summary>
        [JsonProperty]
        public double Efficiency { get; internal set; }

        /// <summary>Nameplate electric output, MW.</summary>
        [JsonProperty]
        public double Size_MW { get; internal set; }

        /// <summary>True if the plant takes its oxidiser from the air (needs oxygen in the atmosphere).</summary>
        [JsonProperty]
        public bool NeedsAtmosphericOxygen { get; internal set; }

        [JsonConstructor]
        private PowerPlantAtb() { }

        public PowerPlantAtb(string fuelId, double lhvMJperKg, double efficiency, double sizeMW, double needsAtmosphericOxygen)
        {
            FuelId = fuelId ?? "";
            LHV_MJperKg = Sane(lhvMJperKg);
            Efficiency = Math.Clamp(Sane(efficiency), 0, 1);
            Size_MW = Sane(sizeMW);
            NeedsAtmosphericOxygen = needsAtmosphericOxygen > 0;
        }

        static double Sane(double v) => double.IsFinite(v) ? Math.Max(0, v) : 0;

        /// <summary>Electric output at full load, kW.</summary>
        [JsonIgnore]
        public double Output_kW => Size_MW * 1000;

        /// <summary>Fuel burn at full output, kg/s: P_e / (η · LHV), with kW and kJ/kg.</summary>
        [JsonIgnore]
        public double FuelAtFull_kgPerS => Efficiency > 0 && LHV_MJperKg > 0
            ? Output_kW / (Efficiency * LHV_MJperKg * 1000)
            : 0;

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance) { }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance) { }

        public string AtbName() => "Power Plant";

        public string AtbDescription()
            => "Generates " + Size_MW.ToString("0.###") + " MW from " + FuelId + " at "
               + (Efficiency * 100).ToString("0.#") + "% (" + (FuelAtFull_kgPerS * 86400).ToString("0.###") + " kg/day at full output)";
    }
}
