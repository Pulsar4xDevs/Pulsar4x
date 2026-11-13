using System;
using Pulsar4X.Engine;
using Pulsar4X.Factions;

namespace Pulsar4X.Energy
{
    /// <summary>
    /// The energy techs a colony applies at run time (§7 of the energy plan). Plant efficiency and
    /// reactor heat are design-time (template formulas); grid loss and industrial energy apply to
    /// every colony as soon as the tech is researched. A tech the faction has not opened counts at
    /// level 0, so a tech-0 colony gets exactly the phase 3 numbers.
    /// </summary>
    public static class EnergyTech
    {
        public const string ThermalEfficiency = "tech-thermal-efficiency";
        public const string CombinedCycle = "tech-combined-cycle";
        public const string FissionPowerPlant = "tech-fission-power-plant";
        public const string ReactorPowerDensity = "tech-reactor-power-density";
        public const string PowerConversion = "tech-power-conversion";
        public const string GridDistribution = "tech-grid-distribution";
        public const string IndustrialEnergyEfficiency = "tech-industrial-energy-efficiency";
        public const string FuelCells = "tech-fuel-cells";

        /// <summary>
        /// The tech's data value at the faction's level: open techs at their level, locked ones at
        /// level 0. <paramref name="fallback"/> if the faction's data has no such tech (old saves).
        /// </summary>
        public static double Value(FactionDataStore? data, string techId, double fallback)
        {
            if (data == null)
                return fallback;
            var tech = data.Techs.TryGetValue(techId, out var open) && open != null ? open
                : data.LockedTechs.TryGetValue(techId, out var locked) ? locked : null;
            if (tech == null)
                return fallback;
            double v = tech.TechDataFormula();
            return double.IsFinite(v) ? v : fallback;
        }

        /// <summary>Delivered / generated on the faction's grids, 0..1. 0.92 at level 0.</summary>
        public static double GridEfficiency(FactionDataStore? data)
            => Math.Clamp(Value(data, GridDistribution, ColonyPowerDB.DefaultGridEfficiency), 0, 1);

        /// <summary>Multiplier on every industry point's kWh, 0..1. 1 at level 0.</summary>
        public static double IndustrialEnergyFactor(FactionDataStore? data)
            => Math.Clamp(Value(data, IndustrialEnergyEfficiency, 1), 0, 1);

        public static double GridEfficiency(Entity colony) => GridEfficiency(Data(colony));

        public static double IndustrialEnergyFactor(Entity colony) => IndustrialEnergyFactor(Data(colony));

        static FactionDataStore? Data(Entity? colony)
        {
            if (colony?.Manager?.Game == null
                || !colony.Manager.Game.Factions.TryGetValue(colony.FactionOwnerID, out var faction)
                || faction == null
                || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
                return null;
            return info.Data;
        }
    }
}
