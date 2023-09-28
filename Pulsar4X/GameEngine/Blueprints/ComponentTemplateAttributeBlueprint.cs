using System.Collections.Generic;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Blueprints
{
    public class ComponentTemplateAttributeBlueprint
    {
        public string Name { get; set; }
        public string DescriptionFormula { get; set; }
        public string Units { get; set; }
        public GuiHint GuiHint { get; set; }
        public string GuiIsEnabledFormula { get; set; }
        public Dictionary<object, string> GuidDictionary { get; set; }
        public string EnumTypeName { get; set; }
        public string MaxFormula { get; set; }
        public string MinFormula { get; set; }
        public string StepFormula { get; set; }
        public string AttributeFormula { get; set; }
        public string AttributeType { get; set; }
    }
}