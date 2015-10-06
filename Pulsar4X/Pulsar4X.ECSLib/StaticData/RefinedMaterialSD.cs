using System;

namespace Pulsar4X.ECSLib
{

    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct RefinedMaterialSD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public JDictionary<Guid, int> RawMineralCosts;
        public JDictionary<Guid, int> RefinedMateraialsCosts;
        public int RefinaryPointCost;
        public int WealthCost;
        public int OutputAmount;
    }
}