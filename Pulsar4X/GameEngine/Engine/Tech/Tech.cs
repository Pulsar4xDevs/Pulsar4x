using Pulsar4X.Blueprints;
using Pulsar4X.Components;

namespace Pulsar4X.Engine
{
    public class Tech : TechBlueprint
    {
        public ComponentDesign Design { get; set; }
        public Entity Faction { get; set; }

        public Tech() { }

        public Tech(TechBlueprint blueprint)
        {
            Name = blueprint.Name;
            Description = blueprint.Description;
            MaxLevel = blueprint.MaxLevel;
            CostFormula = blueprint.CostFormula;
            DataFormula = blueprint.DataFormula;
            Category = blueprint.Category;
            Unlocks = blueprint.Unlocks;
        }
    }
}