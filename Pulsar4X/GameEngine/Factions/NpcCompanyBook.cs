using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.Factions
{
    /// <summary>Multi-faction phase 9: persisted state for runtime NPC companies (clocks, caps, and charters).</summary>
    public sealed class NpcCompanyBook
    {
        /// <summary>Last charter in each system, by system id. Caps one spawn per system per six months.</summary>
        [JsonProperty]
        public Dictionary<string, DateTime> LastSpawnBySystem { get; set; } = new();

        /// <summary>When unmet full-premium demand for a good in a system started, keyed by "systemId:cargoId".</summary>
        [JsonProperty]
        public Dictionary<string, DateTime> UnmetSince { get; set; } = new();

        [JsonProperty]
        public List<SpawnedCompany> Spawned { get; set; } = new();
    }

    public sealed class SpawnedCompany
    {
        public int FactionId { get; set; }
        public int SponsorFactionId { get; set; }
        public int ColonyId { get; set; }
        public string SystemId { get; set; } = "";
        public string CargoId { get; set; } = "";
        public DateTime SpawnedAt { get; set; }
        /// <summary>When the company first went broke with no stock; null while it is not in that state.</summary>
        public DateTime? BrokeSince { get; set; }
    }
}
