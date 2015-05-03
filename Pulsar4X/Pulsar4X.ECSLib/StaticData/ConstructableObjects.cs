using System;

namespace Pulsar4X.ECSLib
{
    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct ConstructableObjSD
    {
        public Guid ID;
        public string Name;
        public string Description;
        public JDictionary<Guid, int> Ingredients;
        public int BuildPoints;
        public int WealthCost;
        public InstallationAbilityType ConstructionTypeRequrement;
    }
}