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
        public int MaxLabs
        {
            get { return _maxLabs; }
            internal set { _maxLabs = value; }
        }
        [JsonProperty]
        private int _maxLabs;
        
        [JsonProperty]
        public int AssignedLabs { get; internal set; }

        public List<Guid> ProjectQueue { get; internal set; } 


        [UsedImplicitly]
        public ScientistDB() { } // needed by json

        public ScientistDB(Dictionary<ResearchCategories,float> bonuses, int maxLabs )
        {
            Bonuses = bonuses;
            MaxLabs = maxLabs;
        }

        public ScientistDB(ScientistDB dB)
        {
            Bonuses = new Dictionary<ResearchCategories, float>(dB.Bonuses);
        }

        public override object Clone()
        {
            return new ScientistDB(this);
        }
    }
}