using System;
using System.Collections.Generic;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Factions;

namespace Pulsar4X.Engine.Api
{
    /// <summary>
    /// Converts between a live <see cref="ComponentDesigner"/> and the serializable
    /// <see cref="DesignerInput"/> form of its player-settable state. The interactive designer runs
    /// client-side; this is the shared seam on either end of the boundary: the client
    /// <see cref="Extract"/>s its designer state to submit a <c>CreateComponentDesignCommand</c> (and
    /// <see cref="Apply"/>s a saved design's inputs to reload it), and the server <see cref="Build"/>s
    /// a fresh designer from the submitted inputs to validate and register the design.
    /// </summary>
    public static class DesignerInputs
    {
        /// <summary>Constructs a designer for a template and replays inputs onto it (in order —
        /// properties may depend on earlier ones).</summary>
        public static ComponentDesigner Build(FactionDataStore data, FactionTechDB techs,
            ComponentTemplateBlueprint template, IReadOnlyList<DesignerInput> inputs)
        {
            var designer = new ComponentDesigner(template, data, techs);
            Apply(designer, inputs);
            return designer;
        }

        public static void Apply(ComponentDesigner designer, IReadOnlyList<DesignerInput> inputs)
        {
            foreach (var input in inputs)
            {
                if (designer.ComponentDesignProperties.TryGetValue(input.PropertyName, out var property))
                    Apply(property, input);
            }

            designer.EvalAll();
        }

        // Numeric inputs (sliders/ranges/enums) clamp via SetValueFromInput; string inputs resolve
        // per the property's data source.
        private static void Apply(ComponentDesignProperty property, DesignerInput input)
        {
            if (input.NumericValue is { } numeric)
            {
                property.SetValueFromInput(numeric);
                return;
            }

            if (string.IsNullOrEmpty(input.StringValue)) return;

            switch (property.GuiHint)
            {
                case GuiHint.GuiTextSelectionFormula:
                    // Saved designs persist the formula's *result* rather than its key (engine
                    // legacy); treat the value as a key when it is one, else replay it literally.
                    if (property.GuidDictionary != null && property.GuidDictionary.ContainsKey(input.StringValue))
                        property.SetValueFromDictionaryExpression(input.StringValue);
                    else
                        property.SetValueFromString(input.StringValue);
                    break;

                case GuiHint.GuiEnumSelectionList:
                    if (property.EnumType != null && Enum.TryParse(property.EnumType, input.StringValue, out var enumValue))
                        property.SetValueFromInput(Convert.ToInt32(enumValue));
                    break;

                default:
                    property.SetValueFromString(input.StringValue);
                    break;
            }
        }

        /// <summary>The designer's player-settable state as inputs: everything <see cref="Build"/>
        /// needs to reproduce the design. Mirrors the engine's own
        /// <c>ComponentDesigner.CreateDesign</c> persistence switch, plus the GuiHint-less upper
        /// bound of each range-slider pair (settable, but never persisted by the engine).</summary>
        public static List<DesignerInput> Extract(ComponentDesigner designer)
        {
            // The upper bounds of range pairs render with GuiHint.None like untouchable bookkeeping
            // properties (attribute-constructor args etc.), but ARE player-set — include them.
            var rangePartners = new HashSet<string>();
            foreach (var property in designer.ComponentDesignProperties.Values)
                if (property.GuiHint == GuiHint.GuiSelectionMinMaxRange && !string.IsNullOrEmpty(property.PairedPropertyName))
                    rangePartners.Add(property.PairedPropertyName);

            var inputs = new List<DesignerInput>();
            foreach (var property in designer.ComponentDesignProperties.Values)
            {
                switch (property.GuiHint)
                {
                    case GuiHint.GuiSelectionMaxMin:
                    case GuiHint.GuiSelectionMaxMinInt:
                    case GuiHint.GuiSelectionMinMaxRange:
                    case GuiHint.GuiEnumSelectionList:
                        if (TryGetNumeric(property, out double value))
                            inputs.Add(new DesignerInput(property.Name, value));
                        break;

                    case GuiHint.GuiTextDisplay:
                    case 0:
                        break; // derived, not player-set

                    case GuiHint.None:
                        if (rangePartners.Contains(property.Name) && TryGetNumeric(property, out double high))
                            inputs.Add(new DesignerInput(property.Name, high));
                        break; // any other GuiHint.None property is bookkeeping — replaying it would
                               // overwrite its formula with a constant

                    default:
                        if (TryGetString(property, out string str))
                            inputs.Add(new DesignerInput(property.Name, StringValue: str));
                        break;
                }
            }
            return inputs;
        }

        // ChainedExpression's typed accessors throw when the result is the other type; an
        // unset/mistyped value just isn't part of the player's input state.
        private static bool TryGetNumeric(ComponentDesignProperty property, out double value)
        {
            try { value = property.Value; return true; }
            catch { value = 0; return false; }
        }

        private static bool TryGetString(ComponentDesignProperty property, out string value)
        {
            try
            {
                value = property.ValueString ?? "";
                return value.Length > 0;
            }
            catch { value = ""; return false; }
        }
    }
}
