using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{

    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct RefinedMaterialSD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public Dictionary<Guid, int> RawMineralCosts;
        public Dictionary<Guid, int> RefinedMateraialsCosts;
        public ushort RefinaryPointCost;
        public ushort WealthCost;
        public ushort OutputAmount;
    }
}