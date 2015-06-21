using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ComponentDB : BaseDataBlob
    {
        public int HitPoints
        {
            get { return _hitPoints; }
            internal set { _hitPoints = value; }
        }
        [JsonProperty]
        private int _hitPoints;

        public Dictionary<AbilityType, float> Abilities
        {
            get { return _abilities; }
            internal set { _abilities = value; }
        }
        [JsonProperty]
        private Dictionary<AbilityType, float> _abilities;

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
