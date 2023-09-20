using System.Collections.Generic;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Modding
{
    public class TechBlueprint : SerializableGameData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxLevel { get; set; }
        public string DataFormula { get; set; }
        public ResearchCategories Category { get; set; }
        public Dictionary<int, List<string>> Unlocks { get; set; }
        public string CostFormula { get; set; }
    }
}