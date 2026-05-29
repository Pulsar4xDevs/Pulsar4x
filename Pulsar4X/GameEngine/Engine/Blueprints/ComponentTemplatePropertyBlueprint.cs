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

        /// <summary>
        /// For GuiSelectionMinMaxRange: the Name of the partner property
        /// (the upper bound when this property is the lower bound).
        /// </summary>
        public string PairedPropertyName { get; set; }

        /// <summary>
        /// For GuiSelectionMinMaxRange: maximum allowed gap between this property's
        /// value and its partner's value (e.g. "TechData('tech-infra-gravity-range') * 2 * 9.81").
        /// If omitted, no gap constraint is applied.
        /// </summary>
        public string MaxRangeFormula { get; set; }
    }
}