using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ScientistDB : BaseDataBlob
    {
        [PublicAPI]
        [JsonProperty]
        public Dictionary<ResearchCategories, float> Bonuses { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public byte MaxLabs { get; internal set; }

        [JsonProperty]
        public byte AssignedLabs { get; internal set; }

        public List<Guid> ProjectQueue { get; internal set; } 

        public ScientistDB() { } // needed by json

        public ScientistDB(Dictionary<ResearchCategories,float> bonuses, byte maxLabs )
        {
            Bonuses = bonuses;
            MaxLabs = maxLabs;
            AssignedLabs = 0;
            ProjectQueue = new List<Guid>();
        }

        public ScientistDB(ScientistDB dB)
        {
            Bonuses = new Dictionary<ResearchCategories, float>(dB.Bonuses);
            MaxLabs = dB.MaxLabs;
            AssignedLabs = dB.AssignedLabs;
            ProjectQueue = dB.ProjectQueue;
        }

        public override object Clone()
        {
            return new ScientistDB(this);
        }
    }
}