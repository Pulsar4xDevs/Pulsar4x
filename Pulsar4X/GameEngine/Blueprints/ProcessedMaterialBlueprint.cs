using System.Collections.Generic;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Blueprints
{
    public class ProcessedMaterialBlueprint : Blueprint
    {
        public string Name { get; set; }
        public Dictionary<string, string> Formulas { get; set;}
        public Dictionary<string, long> ResourceCosts { get; set; }
        public long IndustryPointCosts { get; set; }
        public string IndustryTypeID { get; set; }
        public string Description { get; set; }
        public ConstructableGuiHints GuiHints { get; set; }
        public ushort WealthCost { get; set;}
        public ushort OutputAmount { get; set; }
        public string CargoTypeID { get; set; }
        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; set; }
    }
}