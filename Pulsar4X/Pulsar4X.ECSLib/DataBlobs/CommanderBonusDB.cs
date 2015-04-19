using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class ScientistBonusDB : BaseDataBlob
    {
        public Dictionary<ResearchCategories, float> Bonuses { get; set; }
        public int MaxLabs { get; set; }

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