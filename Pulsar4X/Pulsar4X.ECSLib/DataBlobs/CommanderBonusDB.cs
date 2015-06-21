using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ScientistBonusDB : BaseDataBlob
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

        [UsedImplicitly]
        public ScientistBonusDB() { } // needed by json

        public ScientistBonusDB(Dictionary<ResearchCategories,float> bonuses, int maxLabs )
        {
            Bonuses = bonuses;
            MaxLabs = maxLabs;
        }

        public ScientistBonusDB(ScientistBonusDB scientistBonusDB)
        {
            Bonuses = new Dictionary<ResearchCategories, float>();
        }

        public override object Clone()
        {
            return new ScientistBonusDB(this);
        }
    }
}