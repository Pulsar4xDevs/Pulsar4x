using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;

namespace Pulsar4X.SDL2UI
{

    /// <summary>
    /// If no component template is selected what should we show?
    /// </summary>
    public enum NoTemplateState
    {
        PleaseSelect,
        Created
    }

    public sealed class ComponentDesignDisplay
    {
        private static ComponentDesignDisplay instance = null;
        private static readonly object padlock = new object();

        private NoTemplateState NoTemplateState = NoTemplateState.PleaseSelect;
        private ComponentDesigner _componentDesigner;
        [CanBeNull]
        public ComponentTemplateSD? Template { get; private set;}
        private string[] _designTypes;
        private ComponentTemplateSD[] _designables;
        private static byte[] _nameInputBuffer = new byte[128];
        public static bool compactmod = false;
        private static TechSD[] _techSDs;
        private static string[] _techNames;
        private static int _techSelectedIndex = -1;
        //private TechSD[] _techSDs;
        private static string[] _listNames;


        private ComponentDesignDisplay() { }

        internal static ComponentDesignDisplay GetInstance() {
            lock(padlock)
            {
                if(instance == null)
                {
                    instance = new ComponentDesignDisplay();
                }
            }

            return instance;
        }

        public void SetTemplate(ComponentTemplateSD template, GlobalUIState state)
        {
            Template = template;

            var factionTech = state.Faction.GetDataBlob<FactionTechDB>();
            _designables = StaticRefLib.StaticData.ComponentTemplates.Values.ToArray();
            int count = _designables.Length;
            _designTypes = new string[count];
            for (int i = 0; i < count; i++)
            {
                _designTypes[i] = _designables[i].Name;
            }

            _componentDesigner = new ComponentDesigner(Template.Value, factionTech);

            NoTemplateState = NoTemplateState.Created;
        }

        internal void Display(GlobalUIState uiState)
        {
            if(!Template.HasValue)
            {
                switch (NoTemplateState)
                {
                    case NoTemplateState.PleaseSelect:
                        DisplayPleaseSelectTemplate();
                        break;
                    case NoTemplateState.Created:
                        DisplayCreatedTemplate();
                        break;
                }
                return;
            }

            var windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ComponentDesignChildWindow", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y), true))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text("Specifications");
                ImGui.SameLine();
                ImGui.Text("[?]");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Configure the specifications for the component below.\n\n" +
                        "Different settings will determine the statistics and capabilities\n" +
                        "of the component.");
                ImGui.PopStyleColor();
                ImGui.Separator();
                GuiDesignUI(uiState); //Part design

                ImGui.EndChild();
            }
            ImGui.SameLine();

            var position = ImGui.GetCursorPos();
            if (ImGui.BeginChild("ComponentDesignChildWindow2", new Vector2(windowContentSize.X * 0.49f, windowContentSize.Y * 0.65f), true))
            {
                GuiCostText(uiState); //Print cost

                ImGui.EndChild();
            }

            ImGui.SetCursorPos(new Vector2(position.X, position.Y + windowContentSize.Y * 0.662f));
            if (ImGui.BeginChild("ComponentDesignChildWindow3", new Vector2(windowContentSize.X * 0.49f, windowContentSize.Y * 0.34f), true))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text("Finalize the Design");
                // ImGui.SameLine();
                // ImGui.Text("[?]");
                // if(ImGui.IsItemHovered())
                //     ImGui.SetTooltip("Configure the specifications for the component below.\n\n" +
                //         "Different settings will determine the statistics and capabilities\n" +
                //         "of the component.");
                ImGui.PopStyleColor();
                ImGui.Separator();

                ImGui.Text("Name");
                ImGui.InputText("", _nameInputBuffer, 32);
                if (ImGui.Button("Create"))
                {
                    string name = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer);
                    if(name.IsNotNullOrEmpty())
                    {
                        _componentDesigner.Name = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer);
                        _componentDesigner.CreateDesign(uiState.Faction);
                        //we reset the designer here, so we don't end up trying to edit the precious design. 
                        var factionTech = uiState.Faction.GetDataBlob<FactionTechDB>();
                        _componentDesigner = new ComponentDesigner(Template.Value, factionTech);

                        NoTemplateState = NoTemplateState.Created;
                        Template = null;
                        _nameInputBuffer = new byte[128];
                    }
                }

                ImGui.EndChild();
            }
        }

        internal void GuiDesignUI(GlobalUIState uiState) //Creates all UI elements need for designing the Component
        {
            // FIXME: compact mode should be an option in the game settings?
            // if (ImGui.Button("Compact"))
            // {
            //     compactmod = !compactmod;
            // }

            //ImGui.NewLine();

            if (_componentDesigner != null) //Make sure comp is selected
            {
                foreach (ComponentDesignAttribute attribute in _componentDesigner.ComponentDesignAttributes.Values) //For each property of the comp type
                {
                    ImGui.PushID(attribute.Name);

                    if (attribute.IsEnabled)
                    {
                        switch (attribute.GuiHint) //Either
                        {
                            case GuiHint.None:
                                break;
                            case GuiHint.GuiTechSelectionList: //Let the user pick a type from a list
                                GuiHintTechSelection(attribute);
                                break;
                            case GuiHint.GuiSelectionMaxMin: //Set a value
                                GuiHintMaxMin(attribute);
                                break;
                            case GuiHint.GuiTextDisplay: //Display a stat
                                //GuiHintText(attribute);
                                break;
                            case GuiHint.GuiEnumSelectionList: //Let the user pick a type from a hard coded list
                                GuiHintEnumSelection(attribute);
                                break;
                            case GuiHint.GuiOrdnanceSelectionList:
                                GuiHintOrdnanceSelection(attribute, uiState);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    ImGui.PopID();
                }

                ImGui.NewLine();
            }
            else //Tell the user they don't have a comp type selected
            {
                ImGui.NewLine();
                ImGui.Text("No component type selected");
                ImGui.NewLine();
            }
        }

        private void GuiCostText(GlobalUIState uiState) //Prints a 2 col table with the costs of the part
        {
            if (_componentDesigner != null) //If a part time is selected
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text("Statistics");
                // ImGui.SameLine();
                // ImGui.Text("[?]");
                // if(ImGui.IsItemHovered())
                //     ImGui.SetTooltip("Configure the specifications for the component below.\n\n" +
                //         "Different settings will determine the statistics and capabilities\n" +
                //         "of the component.");
                ImGui.PopStyleColor();
                ImGui.Separator();

                if(ImGui.BeginTable("DesignStatsTables", 2, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextColumn();
                    ImGui.Text("Mass");
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.Mass(_componentDesigner.MassValue));

                    ImGui.TableNextColumn();
                    ImGui.Text("Volume");
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.Volume(_componentDesigner.VolumeM3Value));

                    if(_componentDesigner.CrewReqValue > 0)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text("Crew Required");
                        ImGui.TableNextColumn();
                        ImGui.Text(_componentDesigner.CrewReqValue.ToString());
                    }

                    foreach (ComponentDesignAttribute attribute in _componentDesigner.ComponentDesignAttributes.Values) //For each property of the comp type
                    {
                        if(attribute.IsEnabled && attribute.GuiHint == GuiHint.GuiTextDisplay)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text(attribute.Name);
                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip(attribute.Description);
                            ImGui.TableNextColumn();
                            ImGui.Text(attribute.Value.ToString());
                            if(attribute.Unit.IsNotNullOrEmpty())
                            {
                                ImGui.SameLine();
                                ImGui.Text(attribute.Unit);
                            }
                        }
                    }
                    ImGui.EndTable();
                }

                ImGui.NewLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text("Costs");
                // ImGui.SameLine();
                // ImGui.Text("[?]");
                // if(ImGui.IsItemHovered())
                //     ImGui.SetTooltip("Configure the specifications for the component below.\n\n" +
                //         "Different settings will determine the statistics and capabilities\n" +
                //         "of the component.");
                ImGui.PopStyleColor();
                ImGui.Separator();

                if(ImGui.BeginTable("DesignCostsTables", 2, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextColumn();
                    ImGui.Text("Cost");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.CreditCostValue.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text("Research");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.ResearchCostValue.ToString() + " RP");

                    ImGui.TableNextColumn();
                    ImGui.Text("Production");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.IndustryPointCostsValue.ToString() + " IP");

                    ImGui.EndTable();
                }

                ImGui.NewLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text("Resources Required");
                // ImGui.SameLine();
                // ImGui.Text("[?]");
                // if(ImGui.IsItemHovered())
                //     ImGui.SetTooltip("Configure the specifications for the component below.\n\n" +
                //         "Different settings will determine the statistics and capabilities\n" +
                //         "of the component.");
                ImGui.PopStyleColor();
                ImGui.Separator();

                if(ImGui.BeginTable("DesignResourceCostsTables", 2, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Resource", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Quantity Needed", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    foreach (var kvp in _componentDesigner.ResourceCostValues)
                    {
                        var resource = StaticRefLib.StaticData.CargoGoods.GetAny(kvp.Key);
                        if (resource == null)
                            resource = (ICargoable)uiState.Faction.GetDataBlob<FactionInfoDB>().IndustryDesigns[kvp.Key];

                        ImGui.TableNextColumn();
                        ImGui.Text(resource.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(kvp.Value.ToString());
                    }
                    ImGui.EndTable();
                }
            }
        }

        private void GuiHintText(ComponentDesignAttribute attribute)
        {
            if (compactmod)
            {
                ImGui.TextWrapped(attribute.Name + ": " + attribute.Value.ToString() + " " + attribute.Unit);
                ImGui.NewLine();
            }
            else
            {
                ImGui.TextWrapped(attribute.Name + ":");
                ImGui.SameLine();
                ImGui.TextWrapped(attribute.Value.ToString() + " " + attribute.Unit);
                ImGui.NewLine();
            }
        }

        private void GuiHintMaxMin(ComponentDesignAttribute attribute)
        {
            if (compactmod)
            {
                ImGui.TextWrapped(attribute.Name + ": " + attribute.Description);
                ImGui.NewLine();
            }
            else
            {
                ImGui.TextWrapped(attribute.Name + ":");
                ImGui.SameLine();
                ImGui.TextWrapped(attribute.Description);
                ImGui.NewLine();
            }

            attribute.SetMax();
            attribute.SetMin();
            //attribute.SetValue();
            attribute.SetStep();

            var max = attribute.MaxValue;
            var min = attribute.MinValue;
            double val = attribute.Value;
            double step = attribute.StepValue;
            double fstep = step * 10;
            IntPtr valPtr;
            IntPtr maxPtr;
            IntPtr minPtr;
            IntPtr stepPtr;
            IntPtr fstepPtr;

            unsafe
            {
                valPtr = new IntPtr(&val);
                maxPtr = new IntPtr(&max);
                minPtr = new IntPtr(&min);
                stepPtr = new IntPtr(&step);
                fstepPtr = new IntPtr(&fstep);
            }
            //ImGui.DragScalar("##slider" + attribute.Name, ImGuiDataType.Double, valPtr, 1f, minPtr, maxPtr);


            if (compactmod)
            {
            }
            else
            {
                //ImGui.PushItemWidth(-1);
                if (ImGui.SliderScalar("##scaler" + attribute.Name, ImGuiDataType.Double, valPtr, minPtr, maxPtr))
                {
                    attribute.SetValueFromInput(val);
                }
            }

            //ImGui.PushItemWidth(-1);
            if (ImGui.InputScalar("##input" + attribute.Name, ImGuiDataType.Double, valPtr, stepPtr, fstepPtr))
                attribute.SetValueFromInput(val);
            ImGui.NewLine();
        }

        private void GuiHintTechSelection(ComponentDesignAttribute attribute)
        {
            if (compactmod)
            {
                ImGui.TextWrapped(attribute.Name + ": " + attribute.Description);
                ImGui.NewLine();
            }
            else
            {
                ImGui.TextWrapped(attribute.Name + ":");
                ImGui.SameLine();
                ImGui.TextWrapped(attribute.Description);
                ImGui.NewLine();
            }

            int i = 0;
            _techSDs = new TechSD[attribute.GuidDictionary.Count];
            _techNames = new string[attribute.GuidDictionary.Count];
            foreach (var kvp in attribute.GuidDictionary)
            {
                TechSD sd = StaticRefLib.StaticData.Techs[Guid.Parse((string)kvp.Key)];
                _techSDs[i] = sd;
                _techNames[i] = sd.Name;
                i++;
            }

            ImGui.TextWrapped(attribute.Value.ToString());



            if (ImGui.Combo("Select Tech", ref _techSelectedIndex, _techNames, _techNames.Length))
            {
                attribute.SetValueFromGuidList(_techSDs[_techSelectedIndex].ID);
            }

            ImGui.NewLine();
        }

        private void GuiHintEnumSelection(ComponentDesignAttribute attribute)
        {
            if (compactmod)
            {
                ImGui.TextWrapped(attribute.Name + ": " + attribute.Description);
                ImGui.NewLine();
            }
            else
            {
                ImGui.TextWrapped(attribute.Name + ":");
                ImGui.SameLine();
                ImGui.TextWrapped(attribute.Description);
                ImGui.NewLine();
            }

            //_techSDs = new TechSD[attribute.GuidDictionary.Count];
            _listNames = Enum.GetNames(attribute.EnumType);


            //ImGui.TextWrapped(attribute.Value.ToString());

            if (ImGui.Combo("Select", ref attribute.ListSelection, _listNames, (int)attribute.MaxValue + 1))
            {
                int enumVal = (int)Enum.Parse(attribute.EnumType, _listNames[attribute.ListSelection]);
                attribute.SetValueFromInput(enumVal);
            }

            ImGui.NewLine();
        }

        private void GuiHintOrdnanceSelection(ComponentDesignAttribute attribute, GlobalUIState uiState)
        {
            var dict = uiState.Faction.GetDataBlob<FactionInfoDB>().MissileDesigns;
            _listNames = new string[dict.Count];
            OrdnanceDesign[] ordnances = new OrdnanceDesign[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                _listNames[i] = kvp.Value.Name;
                ordnances[i] = kvp.Value;
            }



            if (compactmod)
            {
                ImGui.TextWrapped(attribute.Name + ": " + attribute.Description);
                ImGui.NewLine();
            }
            else
            {
                ImGui.TextWrapped(attribute.Name + ":");
                ImGui.SameLine();
                ImGui.TextWrapped(attribute.Description);
                ImGui.NewLine();
            }


            ImGui.TextWrapped(attribute.Value.ToString());

            if (ImGui.Combo("Select", ref attribute.ListSelection, _listNames, _listNames.Length))
            {
                attribute.SetValueFromComponentList(ordnances[attribute.ListSelection].ID);
            }

            ImGui.NewLine();
        }

        private void GuiHintTextSelectionFormula(ComponentDesignAttribute attribute)
        {
            
            Dictionary<string, ChainedExpression> dict = new Dictionary<string, ChainedExpression>();

            _listNames = new string[dict.Count];
            
            int i = 0;
            foreach (var kvp in attribute.GuidDictionary)
            {
                _listNames[i] = (string)kvp.Key;
            }
            
            if (compactmod)
            {
                ImGui.TextWrapped(attribute.Name + ": " + attribute.Description);
                ImGui.NewLine();
            }
            else
            {
                ImGui.TextWrapped(attribute.Name + ":");
                ImGui.SameLine();
                ImGui.TextWrapped(attribute.Description);
                ImGui.NewLine();
            }
            
            ImGui.TextWrapped(attribute.Value.ToString());

            if (ImGui.Combo("Select", ref attribute.ListSelection, _listNames, _listNames.Length))
            {
                var key = _listNames[attribute.ListSelection];
                var value = attribute.GuidDictionary[key];
                attribute.SetValueFromDictionaryExpression(_listNames[attribute.ListSelection]);
            }
        }

        private void DisplayPleaseSelectTemplate()
        {
            var windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ComponentDesignSelectTemplate", windowContentSize, false))
            {
                string message = "Please select a template on the left.";
                var size = ImGui.GetContentRegionAvail();
                var textSize = ImGui.CalcTextSize(message);
                ImGui.SetCursorPos(new Vector2(size.X / 2 - textSize.X / 2, size.Y / 2 - textSize.Y / 2));
                ImGui.Text(message);
                ImGui.EndChild();
            }
        }

        private void DisplayCreatedTemplate()
        {
            var windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ComponentDesignCreated", windowContentSize, false))
            {
                string message = "Design has been created, it will now be availble to Research.";
                var size = ImGui.GetContentRegionAvail();
                var textSize = ImGui.CalcTextSize(message);
                ImGui.SetCursorPos(new Vector2(size.X / 2 - textSize.X / 2, size.Y / 2 - textSize.Y / 2));
                ImGui.Text(message);
                ImGui.EndChild();
            }
        }
    }
}


