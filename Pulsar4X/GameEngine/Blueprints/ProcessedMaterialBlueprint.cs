using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Blueprints
{
    public class ProcessedMaterialBlueprint : Blueprint
    {
        public string Name { get; init; }
        public Dictionary<string, string> Formulas { get; set;}
        public Dictionary<string, long> ResourceCosts { get; set; }
        public long IndustryPointCosts { get; set; }
        public string IndustryTypeID { get; set; }
        public string Description { get; set; }
        public ConstructableGuiHints GuiHints { get; set; }
        public Dictionary<string, long> MineralsRequired { get; set;}
        public Dictionary<string, long> MaterialsRequired { get; set;}
        public ushort WealthCost { get; set;}
        public ushort OutputAmount { get; set; }
        public string CargoTypeID { get; set; }
        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; set; }
    }
}