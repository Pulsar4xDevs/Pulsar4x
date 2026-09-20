using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NCalc;
using Pulsar4X.Modding;

namespace Pulsar4X.Client.ModFileEditing
{
    public static class FunctionEditWidget
    {
        private static uint _buffSize = 256;
        private static byte[] _strInputBuffer = new byte[256];
        private static string? _editingID;
        private static string _errorMessage = "";
        private static bool _hasError = false;
        private static bool _isComboActive = false;
        private static bool _isSelectActive = false;

        private static readonly Dictionary<string, string> _builtInFunctions = new Dictionary<string, string>
        {
            {"Abs(number)", "Absolute value."},
            {"Acos(number)", "Arccosine."},
            {"Asin(number)", "Arcsine."},
            {"Atan(number)", "Arctangent."},
            {"Ceiling(number)", "Smallest integer >= number."},
            {"Cos(angle)", "Cosine."},
            {"Exp(power)", "e^power."},
            {"Floor(number)", "Largest integer <= number."},
            {"Log(number, base)", "Logarithm."},
            {"Log10(number)", "Base-10 log."},
            {"Max(number1, number2)", "Maximum of two numbers."},
            {"Min(number1, number2)", "Minimum of two numbers."},
            {"Pow(base, exponent)", "base^exponent."},
            {"Round(value, decimals)", "Round to decimals."},
            {"Sign(number)", "Sign of number (-1, 0, 1)."},
            {"Sin(angle)", "Sine."},
            {"Sqrt(number)", "Square root."},
            {"Tan(angle)", "Tangent."},
            {"if(condition, trueValue, falseValue)", "Conditional."},
            {"in(element, values...)", "Check if element in values."}
        };

        private static readonly Dictionary<string, string> _customFunctions = new Dictionary<string, string>
        {
            {"PropertyValue(key)", "Gets the value of a component property by key."},
            {"SetPropertyValue(key, value)", "Sets a component property value by key."},
            {"EnumDict(type)", "Creates a dictionary of enum values for the given type."},
            {"TechData(techID)", "Gets tech data for the given tech ID."},
            {"TechLevel(techID)", "Gets tech level for the given tech ID."},
            {"CargoType(typeID)", "Gets cargo type data for the given ID."},
            {"UniqueID(typeID)", "Returns the ID string for use in datablob args."},
            {"AtbConstrArgs(args...)", "Sets attribute construction args for a datablob."},
            {"ExhaustVelocityLookup(cargoID)", "Looks up exhaust velocity for a cargo material."}
        };

        private static readonly Dictionary<string, string> _pulsarParameters = new Dictionary<string, string>
        {
            {"[Pi]", "Mathematical constant π (3.14159)."},
            {"[Mass]", "Component mass in kg."},
            {"[Volume_km3]", "Component volume in cubic kilometers."},
            {"[Crew]", "Crew requirement for the component."},
            {"[HTK]", "Hull To Kill value, measures component durability."},
            {"[ResearchCost]", "Research cost for the component or tech."},
            {"[ResourceCosts]", "Dictionary of resource costs for the component."},
            {"[MineralCosts]", "Dictionary of mineral costs for the component."},
            {"[CreditCost]", "Credit cost for the component."},
            {"[GuidDict]", "Dictionary of GUID-keyed values for custom component properties."}
        };

        private static readonly List<KeyValuePair<string, string>> _allItems = new List<KeyValuePair<string, string>>();

        static FunctionEditWidget()
        {
            _allItems.AddRange(_builtInFunctions);
            _allItems.AddRange(_customFunctions);
            foreach (var param in _pulsarParameters)
            {
                _allItems.Add(new KeyValuePair<string, string>(param.Key, param.Value));
            }
        }

        public static uint BufferSize
        {
            get => _buffSize;
            set
            {
                _buffSize = value;
                _strInputBuffer = new byte[value];
            }
        }

        public static bool Display(string label, ref string expression, ModDataStore modDataStore, string[] propertyNames, bool exitEditOnFocusLoss = true)
        {
            bool hasChanged = false;
            string uniqueLabel = label.StartsWith("##") ? label : $"##{label}";

            if (uniqueLabel != _editingID)
            {
                ImGui.Text(expression ?? "null");
                if (ImGui.IsItemClicked())
                {
                    _editingID = uniqueLabel;
                    string initExpr = expression ?? "null";
                    _strInputBuffer = Utils.BytesFromString(initExpr);
                    ImGui.SetKeyboardFocusHere(0);
                    ValidateExpression(initExpr);
                    _isSelectActive = true;
                }
            }
            else
            {
                ImGui.InputText(uniqueLabel, _strInputBuffer, _buffSize);
                string tempExpr = Utils.StringFromBytes(_strInputBuffer);
                ValidateExpression(tempExpr);
                if (!_hasError)
                {
                    expression = tempExpr;
                    hasChanged = true;
                }

                ImGui.SameLine();
                string preview = "Select...";
                if (ImGui.BeginCombo("##selector" + uniqueLabel, preview))
                {
                    string searchFilter = "";
                    if (ImGui.InputText("##search" + uniqueLabel, ref searchFilter, 128))
                    {
                    }

                    ImGui.Text("NCalc Built-ins:");
                    ImGui.Separator();
                    foreach (var func in _builtInFunctions)
                    {
                        if (func.Key.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) && ImGui.Selectable(func.Key))
                        {
                            InsertItem(func.Key);
                        }
                        if(ImGui.IsItemActive())
                            _isSelectActive = true;
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(func.Value);
                        }
                    }

                    ImGui.Separator();
                    ImGui.Text("Pulsar4x Custom:");
                    foreach (var func in _customFunctions)
                    {
                        if (func.Key.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            if (func.Key.StartsWith("PropertyValue"))
                            {
                                if(ImGui.BeginMenu("PropertyValue"))
                                {
                                    foreach (var prop in propertyNames)
                                    {
                                        if (ImGui.Selectable(prop))
                                        {
                                            InsertItem($"PropertyValue('{prop}')");
                                        }

                                        if (ImGui.IsItemActive())
                                            _isSelectActive = true;
                                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Component property key.");
                                    }

                                    ImGui.EndMenu();
                                }
                            }
                            else if (func.Key.StartsWith("TechData"))
                            {
                                if(ImGui.BeginMenu("TechData"))
                                {
                                    foreach (var tech in modDataStore.Techs)
                                    {
                                        if (ImGui.Selectable(tech.Key))
                                        {
                                            InsertItem($"TechData('{tech.Key}')");
                                        }

                                        if (ImGui.IsItemActive())
                                            _isSelectActive = true;
                                        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Tech: {tech.Value.Name}");
                                    }

                                    ImGui.EndMenu();
                                }
                            }
                            else if (func.Key.StartsWith("CargoType"))
                            {
                                if (ImGui.BeginMenu("CargoType"))
                                {
                                    foreach (var cargo in modDataStore.CargoTypes)
                                    {
                                        if (ImGui.Selectable(cargo.Key))
                                        {
                                            InsertItem($"CargoType('{cargo.Key}')");
                                        }

                                        if (ImGui.IsItemActive())
                                            _isSelectActive = true;
                                        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Cargo: {cargo.Value.Name}");
                                    }

                                    ImGui.EndMenu();
                                }
                            }
                            
                            else if (func.Key.StartsWith("TechLevel"))
                            {
                                if (ImGui.BeginMenu("TechLevel"))
                                {
                                    foreach (var tech in modDataStore.Techs)
                                    {
                                        if (ImGui.Selectable(tech.Key))
                                        {
                                            InsertItem($"TechLevel('{tech.Key}')");
                                        }

                                        if (ImGui.IsItemActive())
                                            _isSelectActive = true;
                                        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Tech: {tech.Value.Name}");
                                    }

                                    ImGui.EndMenu();
                                }
                            }
                            else 
                            {
                                if(ImGui.Selectable(func.Key))
                                    InsertItem(func.Key);
                            }
                            if(ImGui.IsItemActive())
                                _isSelectActive = true;
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip(func.Value);
                        }
                    }

                    ImGui.Separator();
                    ImGui.Text("Parameters:");
                    foreach (var param in _pulsarParameters)
                    {
                        if (ImGui.Selectable(param.Key))
                        {
                            InsertItem(param.Key);
                        }
                        if(ImGui.IsItemActive())
                            _isSelectActive = true;
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(param.Value);
                        
                    }

                    ImGui.EndCombo();
                }
                _isComboActive = ImGui.IsItemActive();

                if (_hasError)
                {
                    ImGui.TextColored(new System.Numerics.Vector4(1f, 0f, 0f, 1f), _errorMessage);
                }

                if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
                {
                    _editingID = null;
                }
                if (exitEditOnFocusLoss && !ImGui.IsItemActive() && ImGui.IsMouseClicked(0) && !_isComboActive && !_isSelectActive)
                {
                    _editingID = null;
                    _isSelectActive = false;
                    _isComboActive = false;
                }
            }

            return hasChanged;
        }

        private static void ValidateExpression(string expr)
        {
            _hasError = false;
            _errorMessage = "";
            try
            {
                var e = new Expression(expr);
                foreach (var param in _pulsarParameters.Keys)
                {
                    e.Parameters[param.Replace("[", "").Replace("]", "")] = 0.0; // Remove brackets for NCalc
                }
                if (e.HasErrors())
                {
                    throw new Exception(e.Error ?? "Invalid expression");
                }
            }
            catch (Exception ex)
            {
                _hasError = true;
                _errorMessage = $"Error: {ex.Message}";
            }
        }

        private static void InsertItem(string item)
        {
            string current = Utils.StringFromBytes(_strInputBuffer);
            string inserted = current + (current.EndsWith(" ") ? "" : " ");
            inserted += item;
            if (item.Contains("(") && !item.EndsWith(")")) inserted += "()";
            _strInputBuffer = Utils.BytesFromString(inserted);
            ValidateExpression(inserted);
        }
    }
}