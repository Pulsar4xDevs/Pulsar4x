using System;
using Pulsar4X.Energy;
using Pulsar4X.Factions;

namespace Pulsar4X.Ships
{
    /// <summary>
    /// The crew techs (crew plan phase 0). Crew is design-time: a template's CrewReq formula reads
    /// the tech through TechData, so a design keeps the crew it was drawn with. A tech the faction
    /// has not opened counts at level 0.
    /// </summary>
    public static class CrewTech
    {
        /// <summary>Construction Automation: multiplies a construction bay's crew, Pow(0.85, level).</summary>
        public const string ConstructionAutomation = "tech-construction-automation";

        /// <summary>Crew multiplier for construction bays designed now, 0..1. 1 at level 0.</summary>
        public static double ConstructionCrewFactor(FactionDataStore? data)
            => Math.Clamp(EnergyTech.Value(data, ConstructionAutomation, 1), 0, 1);

        /// <summary>
        /// Closed-Loop Life Support: Pow(0.85, level). Run-time, read live: it multiplies a ship's kW per crew
        /// aboard and a sealed colony's hostility term, so research helps the existing fleet and colonies.
        /// </summary>
        public const string LifeSupportEfficiency = "tech-life-support-efficiency";

        /// <summary>Life support multiplier, 0..1. 1 at level 0.</summary>
        public static double LifeSupportFactor(FactionDataStore? data)
            => Math.Clamp(EnergyTech.Value(data, LifeSupportEfficiency, 1), 0, 1);

        /// <summary>The owner faction's life support multiplier; 1 without a faction.</summary>
        public static double LifeSupportFactor(Pulsar4X.Engine.Entity entity)
        {
            var game = entity.Manager?.Game;
            if (game == null || !game.Factions.TryGetValue(entity.FactionOwnerID, out var faction)
                || faction == null || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
                return 1;
            return LifeSupportFactor(info.Data);
        }
    }
}
