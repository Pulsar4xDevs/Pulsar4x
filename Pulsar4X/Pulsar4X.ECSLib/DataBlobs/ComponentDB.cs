using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ComponentDB : BaseDataBlob
    {
        public int HitPoints { get; set; }
        public Dictionary<AbilityType, float> Abilities { get; set; }
        
        public ComponentDB()
        { }

        public ComponentDB(ComponentDB db)
        {
            HitPoints = db.HitPoints;
            Abilities = new Dictionary<AbilityType, float>(db.Abilities);
        }

        public override object Clone()
        {
            return new ComponentDB(this);
        }
    }


}
