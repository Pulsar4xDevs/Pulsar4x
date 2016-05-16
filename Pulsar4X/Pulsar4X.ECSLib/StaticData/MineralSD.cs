using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ID")]
    public struct MineralSD
    {
        public string Name;
        public string Description;
        public Guid ID;
        //public CargoType CargoType;
        public float Weight;

        public Dictionary<BodyType, double> Abundance;
    }
}
