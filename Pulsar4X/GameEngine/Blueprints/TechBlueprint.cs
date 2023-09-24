using System.Collections.Generic;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Blueprints
{
    public class TechBlueprint : Blueprint
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxLevel { get; set; }
        public string CostFormula { get; set; }
        public string DataFormula { get; set; }
        public ResearchCategories Category { get; set; }
        public List<string> Unlocks { get; set; } = new ();
    }
}