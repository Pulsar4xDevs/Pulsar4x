using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Ships
{
    public class LaunchPad
    {
        [JsonProperty]
        public long MaxTonnage { get; set; }

        [JsonProperty]
        public string? ShipDesignId { get; set; }

        [JsonProperty]
        public string? ShipName { get; set; }

        [JsonProperty]
        public long ShipMass { get; set; }

        [JsonProperty]
        public double TargetOrbitRadius { get; set; }

        [JsonProperty]
        public bool ReadyToLaunch { get; set; }
    }

    public class LaunchQueueEntry
    {
        [JsonProperty]
        public string DesignId { get; set; } = "";

        [JsonProperty]
        public string ShipName { get; set; } = "";
    }

    public class LaunchComplexDB : BaseDataBlob
    {
        [JsonProperty]
        public Dictionary<string, LaunchPad> Pads { get; set; } = new();

        [JsonProperty]
        public List<LaunchQueueEntry> LaunchQueue { get; set; } = new();

        [JsonConstructor]
        public LaunchComplexDB() { }

        public LaunchComplexDB(string padId, LaunchPad pad)
        {
            Pads.Add(padId, pad);
        }

        public LaunchComplexDB(LaunchComplexDB db)
        {
            Pads = new Dictionary<string, LaunchPad>(db.Pads);
            LaunchQueue = new List<LaunchQueueEntry>(db.LaunchQueue);
        }

        public override object Clone()
        {
            return new LaunchComplexDB(this);
        }
    }
}
