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
    }
}
