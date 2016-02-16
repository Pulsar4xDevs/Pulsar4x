using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct MineralSD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public Dictionary<BodyType, double> Abundance;
    }
}
