using System;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct ConstructableObjSD
    {
        public string Name;
        public string Description;
        public Guid ID;

        public JDictionary<Guid, int> Ingredients;
        public int BuildPoints;
        public int WealthCost;
        public AbilityType ConstructionTypeRequirement;
    }
}