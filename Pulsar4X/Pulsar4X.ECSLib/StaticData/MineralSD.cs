using System;

namespace Pulsar4X.ECSLib
{
    public struct MineralSD
    {
        public string Name;
        public Guid ID;
        public JDictionary<BodyType, double> Abundance; 
    }
}
