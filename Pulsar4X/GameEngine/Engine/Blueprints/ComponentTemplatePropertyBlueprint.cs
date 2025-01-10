using System.Collections.Generic;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Blueprints
{
    public class ComponentTemplatePropertyBlueprint
    {
        public string Name { get; set; }
        public string DescriptionFormula { get; set; }
        public string Units { get; set; }
        public GuiHint GuiHint { get; set; }
        public string GuiIsEnabledFormula { get; set; }
        public Dictionary<string, string> DataDict { get; set; }
        public string EnumTypeName { get; set; }
        public string MaxFormula { get; set; }
        public string MinFormula { get; set; }
        public string StepFormula { get; set; }
        public string PropertyFormula { get; set; }
        public string AttributeType { get; set; }
    }
}