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

        /// <summary>
        /// Cargo id of the propellant this pad burns, from its launch complex design. Null in a save
        /// from before the choice: see <see cref="LaunchFuel"/>.
        /// </summary>
        [JsonProperty]
        public string? LaunchFuelId { get; set; }

        /// <summary>The launch fuel, checked by <see cref="LaunchComplexAtb.NormalizeLaunchFuel"/>: rp-1 unless the id is an allowed propellant.</summary>
        [JsonIgnore]
        public string LaunchFuel => LaunchComplexAtb.NormalizeLaunchFuel(LaunchFuelId);
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
        public Dictionary<string, LaunchPad> Pads { get; set; } = new();

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
