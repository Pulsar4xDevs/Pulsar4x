using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// Staffing priority when a colony is short of staff (multi-faction phase 5, plan §7.1), highest first.
    /// Like power tiers: each tier gets min(1, staff left ÷ its demand), lower tiers get what is left. No floor.
    /// </summary>
    public enum StaffTier
    {
        /// <summary>Power plants, solar, batteries, infrastructure, life support.</summary>
        Essential = 1,
        /// <summary>City hall, logistics office, academy, spaceport (and anything without a power tier).</summary>
        Control = 2,
        /// <summary>Warehouses, fuel farms.</summary>
        Storage = 3,
        /// <summary>Shipyards, factories, refineries, construction sites.</summary>
        Industry = 4,
        /// <summary>Mines.</summary>
        Mining = 5,
        /// <summary>Research labs.</summary>
        Research = 6,
    }

    /// <summary>
    /// Civilians vs employees (multi-faction phase 5). <c>Population</c> stays the residents; <see cref="Employed"/>
    /// is the subset of residents at work. Contract staff (commuters) from a same-body bloc count as staff here but
    /// stay in the bloc's population. Civilians = Population − Employed (academy trainees have already left the
    /// population). Required, the tier split and the staffing shares are derived and refreshed by
    /// <see cref="Workforce.Recalc"/>.
    /// </summary>
    public class ColonyWorkforceDB : BaseDataBlob
    {
        /// <summary>Own residents at work.</summary>
        [JsonProperty]
        public long Employed { get; internal set; }

        /// <summary>Wanted headcount (own + contract); null means "Required".</summary>
        [JsonProperty]
        public long? Target { get; internal set; }

        /// <summary>Contract staff by the bloc faction that supplies them (commuters stay that bloc's residents).</summary>
        [JsonProperty]
        public Dictionary<int, long> Commuters { get; internal set; } = new();

        /// <summary>Σ CrewReq × count over enabled installations.</summary>
        [JsonProperty]
        public long Required { get; internal set; }

        [JsonProperty]
        public Dictionary<StaffTier, long> RequiredByTier { get; internal set; } = new();

        /// <summary>Share of each tier's requirement that is staffed, 0..1 (1 for tiers with no demand).</summary>
        [JsonProperty]
        public Dictionary<StaffTier, double> TierStaffing { get; internal set; } = new();

        /// <summary>Last day the hire ramp ran.</summary>
        [JsonProperty]
        public DateTime? LastHireDay { get; internal set; }

        public long CommuterTotal
        {
            get
            {
                long total = 0;
                foreach (var n in Commuters.Values)
                    total += n;
                return total;
            }
        }

        public ColonyWorkforceDB() { }

        public ColonyWorkforceDB(ColonyWorkforceDB db)
        {
            Employed = db.Employed;
            Target = db.Target;
            Commuters = new Dictionary<int, long>(db.Commuters);
            Required = db.Required;
            RequiredByTier = new Dictionary<StaffTier, long>(db.RequiredByTier);
            TierStaffing = new Dictionary<StaffTier, double>(db.TierStaffing);
            LastHireDay = db.LastHireDay;
        }

        public override object Clone() => new ColonyWorkforceDB(this);
    }
}
