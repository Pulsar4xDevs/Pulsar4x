using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ScientistDB : BaseDataBlob
    {
        [PublicAPI]
        public Dictionary<ResearchCategories, float> Bonuses
        {
            get { return _bonuses; }
            internal set { _bonuses = value; }
        }
        [JsonProperty]
        private Dictionary<ResearchCategories, float> _bonuses;

        [PublicAPI]
        public byte MaxLabs
        {
            get { return _maxLabs; }
            internal set { _maxLabs = value; }
        }
        [JsonProperty]
        private byte _maxLabs;
        
        [JsonProperty]
        public byte AssignedLabs { get; internal set; }

        public List<Guid> ProjectQueue { get; internal set; } 


        [UsedImplicitly]
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
            _maxLabs = dB.MaxLabs;
            AssignedLabs = dB.AssignedLabs;
            ProjectQueue = dB.ProjectQueue;
        }

        public override object Clone()
        {
            return new ScientistDB(this);
        }
    }
}