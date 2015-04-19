using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class ScientistBonusDB : BaseDataBlob
    {
        public Dictionary<ResearchCategories, int> Bonuses { get; set; }
        public int MaxTeamSize { get; set; }

        public ScientistBonusDB(Dictionary<ResearchCategories,int> bonuses, int maxTeamSize )
        {
            Bonuses = bonuses;
            MaxTeamSize = maxTeamSize;
        }

        public ScientistBonusDB(ScientistBonusDB scientistBonusDB)
        {
            Bonuses = new Dictionary<ResearchCategories, int>();
        }

        public override object Clone()
        {
            return new ScientistBonusDB(this);
        }
    }
}