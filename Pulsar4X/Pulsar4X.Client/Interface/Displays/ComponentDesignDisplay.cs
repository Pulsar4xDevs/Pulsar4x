using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;
using Pulsar4X.Industry;
using Pulsar4X.Factions;
using Pulsar4X.Storage;
using Pulsar4X.Technology;
using Pulsar4X.Weapons;

namespace Pulsar4X.Client
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
        private static ComponentDesignDisplay? instance = null;
        private static readonly object padlock = new object();

        private NoTemplateState NoTemplateState = NoTemplateState.PleaseSelect;
        private ComponentDesigner? _componentDesigner;
        public ComponentTemplateBlueprint? Template { get; private set;}
        private string[]? _designTypes;
        private ComponentTemplateBlueprint[]? _designables;
        private static byte[] _nameInputBuffer = new byte[128];
        private static Tech[]? _techSDs;
        private static string[]? _techNames;
        private static int _techSelectedIndex = -1;
        //private TechSD[] _techSDs;
        private static string[]? _listNames;


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

        public void SetTemplate(ComponentTemplateBlueprint template, GlobalUIState state)
        {
            Template = template;

            var factionData = state.Faction.GetDataBlob<FactionInfoDB>().Data;
            var factionTech = state.Faction.GetDataBlob<FactionTechDB>();
            _componentDesigner = new ComponentDesigner(Template, factionData, factionTech);

            NoTemplateState = NoTemplateState.Created;
        }

        public void SetFromComponent(ComponentDesign component, GlobalUIState state)
        {


            var factionData = state.Faction.GetDataBlob<FactionInfoDB>().Data;
            var factionTech = state.Faction.GetDataBlob<FactionTechDB>();
            Template = factionData.ComponentTemplates[component.TemplateID];
            _componentDesigner = new ComponentDesigner(Template, factionData, factionTech);

            NoTemplateState = NoTemplateState.Created;

            var templateProperties = component.TemplatePropertyValues;
            //_componentDesigner.Name = component.Name;
            _nameInputBuffer = Utils.BytesFromString(component.Name);
            foreach (var ptup in templateProperties)
            {
                var tprop = _componentDesigner.ComponentDesignProperties[ptup.propName];
                if (tprop.GuiHint == GuiHint.GuiFuelTypeSelection)
                {
                    var cargoTypesToDisplay = GetFuelTypes(tprop, state);
                    var strfuel = (string)ptup.propValue;
                    var index = cargoTypesToDisplay.FindIndex(item => item.UniqueID == strfuel);
                    tprop.SetValueFromString((string)ptup.propValue);
                    tprop.ListSelection = index;
                }
                else if (ptup.propValue is string)
                {
                    tprop.SetValueFromString((string)ptup.propValue);
                }
                else if (ptup.propValue is Int32 || ptup.propValue is float || ptup.propValue is double )
                {
                    tprop.SetValueFromInput((double)ptup.propValue);
                }
            }
        }

        internal void Display(GlobalUIState uiState)
        {
            if(Template == null)
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
            if (ImGui.BeginChild("ComponentDesignChildWindow", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y), ImGuiChildFlags.Borders))
            {
                DisplayHelpers.Header("Specifications",
                    "Configure the specifications for the component below.\n\n" +
                    "Different settings will determine the statistics and capabilities\n" +
                    "of the component.");

                GuiDesignUI(uiState); //Part design
            }
            ImGui.EndChild();
            ImGui.SameLine();
            //ImGui.SetCursorPosY(27f);

            var position = ImGui.GetCursorPos();
            if (ImGui.BeginChild("ComponentDesignChildWindow2", new Vector2(windowContentSize.X * 0.49f, windowContentSize.Y * 0.65f), ImGuiChildFlags.Borders))
            {
                GuiCostText(uiState); //Print cost
            }
            ImGui.EndChild();

            ImGui.SetCursorPos(new Vector2(position.X, position.Y + windowContentSize.Y * 0.662f));
            if (ImGui.BeginChild("ComponentDesignChildWindow3", new Vector2(windowContentSize.X * 0.49f, windowContentSize.Y * 0.34f), ImGuiChildFlags.Borders))
            {
                var sizeAvailable = ImGui.GetContentRegionAvail();

                DisplayHelpers.Header("Finalize the Design");
                ImGui.Text("Name");
                ImGui.InputText("###designname", _nameInputBuffer, 32);
                ImGui.SetCursorPosY(sizeAvailable.Y - 12f);
                if(ImGui.Button("Save", new Vector2(sizeAvailable.X, 0)))
                {
                    if(!_nameInputBuffer.All(b => b == 0))
                    {
                        if(_componentDesigner != null)
                        {
                            string name = Utils.StringFromBytes(_nameInputBuffer);
                            _componentDesigner.Name = name;
                            _componentDesigner.CreateDesign(uiState.Faction);
                        }

                        //we reset the designer here, so we don't end up trying to edit the previous design.
                        var factionData = uiState.Faction.GetDataBlob<FactionInfoDB>().Data;
                        var factionTech = uiState.Faction.GetDataBlob<FactionTechDB>();
                        _componentDesigner = new ComponentDesigner(Template, factionData, factionTech);

                        NoTemplateState = NoTemplateState.Created;
                        Template = null;
                        _nameInputBuffer = new byte[128];
                    }
                }
            }
            ImGui.EndChild();
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
                // Build the set of properties consumed as "partners" by a range slider — don't render them on their own.
                var pairedPartners = new HashSet<string>();
                foreach (var p in _componentDesigner.ComponentDesignProperties.Values)
                {
                    if (p.GuiHint == GuiHint.GuiSelectionMinMaxRange && !string.IsNullOrEmpty(p.PairedPropertyName))
                        pairedPartners.Add(p.PairedPropertyName);
                }

                foreach (ComponentDesignProperty attribute in _componentDesigner.ComponentDesignProperties.Values) //For each property of the comp type
                {
                    if (pairedPartners.Contains(attribute.Name)) continue;

                    ImGui.PushID(attribute.Name);

                    if (attribute.IsEnabled)
                    {
                        switch (attribute.GuiHint) //Either
                        {
                            case 0:
                                break;
                            case GuiHint.None:
                                break;
                            case GuiHint.GuiTechSelectionList: //Let the user pick a type from a list
                                GuiHintTechSelection(attribute, uiState);
                                break;
                            case GuiHint.GuiSelectionMaxMin: //Set a value
                                GuiHintMaxMin(attribute);
                                break;
                            case GuiHint.GuiSelectionMaxMinInt:
                                GuiHintMaxMinInt(attribute);
                                break;
                            case GuiHint.GuiSelectionMinMaxRange:
                                GuiHintMinMaxRange(attribute);
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
                            case GuiHint.GuiTextSelectionFormula:
                                GuiHintTextSelectionFormula(attribute);
                                break;
                            case GuiHint.GuiFuelTypeSelection:
                                GuiHintFuelTypeSelection(attribute, uiState);
                                break;
                            case GuiHint.GuiTechCategorySelection:
                                GuiHintTechCategorySelection(attribute, uiState);
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
                DisplayHelpers.Header("Statistics");

                if(ImGui.BeginTable("DesignStatsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Type");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.ComponentType);

                    var activeMountTypes = _componentDesigner.GetActiveMountTypes();
                    if(activeMountTypes.Count > 0)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text("");
                        ImGui.SameLine();
                        ImGui.Text("Installs On or In");
                        ImGui.TableNextColumn();
                        for(int i = 0; i < activeMountTypes.Count; i++)
                        {
                            if(i < activeMountTypes.Count - 1)
                            {
                                ImGui.Text(activeMountTypes[i].ToDescription() +  ",");
                                ImGui.SameLine();
                            }
                            else
                            {
                                ImGui.Text(activeMountTypes[i].ToDescription());
                            }
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Mass");
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.Mass(_componentDesigner.MassValue));

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Volume");
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.VolumeLtr(_componentDesigner.VolumeM3Value));

                    if(_componentDesigner.CrewReqValue > 0)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text("");
                        ImGui.SameLine();
                        ImGui.Text("Crew Required");
                        ImGui.TableNextColumn();
                        ImGui.Text(_componentDesigner.CrewReqValue.ToString(Styles.IntFormat));
                    }

                    foreach (ComponentDesignProperty attribute in _componentDesigner.ComponentDesignProperties.Values) //For each property of the comp type
                    {
                        if(attribute.IsEnabled && attribute.GuiHint == GuiHint.GuiTextDisplay)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text("");
                            ImGui.SameLine();
                            ImGui.Text(attribute.Name);
                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip(attribute.Description);
                            ImGui.TableNextColumn();

                            if(attribute.Unit.IsNotNullOrEmpty())
                            {
                                var value = attribute.Value;
                                var strUnit = attribute.Unit;
                                var displayStr = "";

                                switch (strUnit)
                                {
                                    case "KJ":
                                    {
                                        displayStr = Stringify.Energy(value);
                                        break;
                                    }
                                    case "KW":
                                    {
                                        displayStr = Stringify.Power(value);
                                        break;
                                    }
                                    case "m^2":
                                    {
                                        displayStr = Stringify.Area(value);
                                        break;
                                    }
                                    case "nm":
                                    {
                                        displayStr = Stringify.DistanceSmall(value);
                                        break;
                                    }
                                    case "kg":
                                    {
                                        displayStr = Stringify.Mass(value);
                                        break;
                                    }
                                    case "m":
                                    {
                                        displayStr = Stringify.Distance(value);
                                        break;
                                    }
                                    case "N":
                                    {
                                        displayStr = Stringify.Thrust(value);
                                        break;
                                    }
                                    case "m/s":
                                        displayStr = Stringify.Velocity(value);
                                        break;
                                    case "s":
                                        displayStr = TimeSpan.FromSeconds(value).ToString() ;
                                        break;
                                    default:
                                    {
                                        displayStr = attribute.Value.ToString(Styles.DecimalFormat) + " " + attribute.Unit;
                                        break;
                                    }
                                }

                                ImGui.TextUnformatted(displayStr);
                                if(ImGui.IsItemHovered())
                                    ImGui.SetTooltip(@attribute.Value.ToString(Styles.IntFormat) + " " + attribute.Unit.Replace("%", "%%"));

                            }
                            else
                            {
                                ImGui.Text(attribute.Value.ToString(Styles.IntFormat));
                                if(ImGui.IsItemHovered())
                                    ImGui.SetTooltip(attribute.Value.ToString(Styles.DecimalFormat));
                            }
                        }
                        else if(attribute.IsEnabled && attribute.GuiHint == GuiHint.GuiFuelTypeSelection)
                        {
                            var cargo = (ProcessedMaterial)uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMaterial(attribute.ValueString);
                            ImGui.TableNextColumn();
                            ImGui.Text("");
                            ImGui.SameLine();
                            ImGui.Text("Fuel Type");
                            ImGui.TableNextColumn();
                            ImGui.Text(cargo.Name);
                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip(cargo.Description);
                        }
                    }
                    ImGui.EndTable();
                }

                ImGui.NewLine();
                DisplayHelpers.Header("Costs");

                if(ImGui.BeginTable("DesignCostsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Cost");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.CreditCostValue.ToString(Styles.IntFormat));

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Research");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.ResearchCostValue.ToString(Styles.IntFormat) + " RP");

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Production");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.IndustryPointCostsValue.ToString(Styles.IntFormat) + " IP");

                    ImGui.EndTable();
                }

                ImGui.NewLine();
                DisplayHelpers.Header("Resources Required");

                if(ImGui.BeginTable("DesignResourceCostsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Resource", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Quantity Needed", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    foreach (var kvp in _componentDesigner.ResourceCostValues)
                    {
                        var resource = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(kvp.Key);
                        if (resource == null)
                            resource = (ICargoable)uiState.Faction.GetDataBlob<FactionInfoDB>().IndustryDesigns[kvp.Key];

                        ImGui.TableNextColumn();
                        ImGui.Text("");
                        ImGui.SameLine();
                        ImGui.Text(resource.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(kvp.Value.ToString(Styles.IntFormat));
                    }
                    ImGui.EndTable();
                }
            }
        }

        private void GuiHintText(ComponentDesignProperty property)
        {
            var value = property.Value;
            var strUnit = property.Unit;
            var displayStr = "";
            switch (strUnit)
            {
                case "KJ":
                {
                    displayStr = Stringify.Energy(value);
                    break;
                }
                default:
                {
                    displayStr = property.Value.ToString() + " " + property.Unit;
                    break;
                }


            }

            Title(property.Name, displayStr);
        }

        private void GuiHintMaxMin(ComponentDesignProperty property)
        {
            Title(property.Name, property.Description);

            property.SetMax();
            property.SetMin();
            //attribute.SetValue();
            property.SetStep();

            var max = property.MaxValue;
            var min = property.MinValue;
            double val = property.Value;
            double step = property.StepValue;
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

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.SliderScalar("##scaler" + property.Name, ImGuiDataType.Double, valPtr, minPtr, maxPtr))
            {
                property.SetValueFromInput(val);
            }
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.InputScalar("##input" + property.Name, ImGuiDataType.Double, valPtr, stepPtr, fstepPtr))
                property.SetValueFromInput(val);
            ImGui.NewLine();
        }

        private void GuiHintMaxMinInt(ComponentDesignProperty property)
        {
            Title(property.Name, property.Description);

            property.SetMax();
            property.SetMin();
            //attribute.SetValue();
            property.SetStep();

            var max = property.MaxValue;
            var min = property.MinValue;
            int val = (int)property.Value;
            double step = property.StepValue;
            double fstep = step * 10;

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if(ImGui.SliderInt("##scaler" + property.Name, ref val, (int)min, (int)max))
            {
                property.SetValueFromInput(val);
            }

            ImGui.SetNextItemWidth(sizeAvailable.X);
            if(ImGui.InputInt("##input" + property.Name, ref val, (int)step, (int)fstep))
            {
                property.SetValueFromInput(val);
            }
            ImGui.NewLine();
        }

        private void GuiHintMinMaxRange(ComponentDesignProperty lowProperty)
        {
            // Resolve the paired (upper-bound) property.
            if (string.IsNullOrEmpty(lowProperty.PairedPropertyName)
                || !_componentDesigner.ComponentDesignProperties.TryGetValue(
                    lowProperty.PairedPropertyName, out var highProperty))
            {
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f),
                    $"[range slider misconfigured: '{lowProperty.Name}' has no PairedPropertyName]");
                return;
            }

            lowProperty.SetMin(); lowProperty.SetMax(); lowProperty.SetStep();
            lowProperty.SetMaxRange();
            highProperty.SetMin(); highProperty.SetMax();

            // The two properties share the same allowed band — the wider of the two ends wins.
            double axisMin = Math.Min(lowProperty.MinValue, highProperty.MinValue);
            double axisMax = Math.Max(lowProperty.MaxValue, highProperty.MaxValue);
            double maxGap = lowProperty.MaxRangeValue;
            if (axisMax <= axisMin)
            {
                ImGui.Text(lowProperty.Name + " / " + highProperty.Name + ": no range available");
                return;
            }

            double lowVal = lowProperty.Value;
            double highVal = highProperty.Value;

            // Anchor X / width come from window state, not the cursor — cursor-based reads
            // can drift between two consecutive widgets when something upstream leaves the
            // cursor at a slightly different X, producing visibly misaligned tracks between
            // the paired gravity and pressure sliders. Window pos/size are frame-stable.
            float padX = ImGui.GetStyle().WindowPadding.X;
            float widgetLeftAnchorX = ImGui.GetWindowPos().X + padX;
            float widgetAvailWidth = ImGui.GetWindowSize().X - padX * 2f;

            Title(lowProperty.Name + " ↔ " + highProperty.Name, lowProperty.Description);

            float width = widgetAvailWidth;
            const float TrackHeight = 8f;
            const float HandleRadius = 8f;
            // Edge padding leaves room for the value label to stay centered under a handle
            // when the handle sits at the very end of the track.
            const float Padding = 28f;
            float trackY = ImGui.GetCursorScreenPos().Y + HandleRadius + 4f;
            float trackLeftX = widgetLeftAnchorX + Padding;
            float trackRightX = widgetLeftAnchorX + width - Padding;
            float trackWidth = trackRightX - trackLeftX;
            if (trackWidth < 1f) { ImGui.Dummy(new Vector2(width, 1f)); return; }

            double ValueToPx(double v) => trackLeftX + (v - axisMin) / (axisMax - axisMin) * trackWidth;
            double PxToValue(float x) => axisMin + (x - trackLeftX) / trackWidth * (axisMax - axisMin);
            double PxDeltaToValue(float dx) => dx / trackWidth * (axisMax - axisMin);

            float lowPx = (float)ValueToPx(lowVal);
            float highPx = (float)ValueToPx(highVal);

            var drawList = ImGui.GetWindowDrawList();
            uint colTrack = ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.20f, 0.24f, 1.0f));
            uint colFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.30f, 0.65f, 1.00f, 0.85f));
            uint colHandle = ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.90f, 1.00f, 1.00f));
            uint colHandleHover = ImGui.ColorConvertFloat4ToU32(new Vector4(1.00f, 1.00f, 1.00f, 1.00f));
            uint colHandleOutline = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.15f, 0.25f, 1.00f));

            drawList.AddRectFilled(
                new Vector2(trackLeftX, trackY - TrackHeight * 0.5f),
                new Vector2(trackRightX, trackY + TrackHeight * 0.5f),
                colTrack, 2f);

            drawList.AddRectFilled(
                new Vector2(lowPx, trackY - TrackHeight * 0.5f),
                new Vector2(highPx, trackY + TrackHeight * 0.5f),
                colFill, 2f);

            // Three invisible buttons for the three drag zones. Middle button sits BEHIND the handles
            // so handle clicks win when they overlap.
            var startCursor = ImGui.GetCursorPos();

            // Mid (fill) drag — full track region between handles.
            float midLeft = lowPx + HandleRadius;
            float midRight = highPx - HandleRadius;
            if (midRight > midLeft)
            {
                ImGui.SetCursorScreenPos(new Vector2(midLeft, trackY - TrackHeight));
                ImGui.InvisibleButton("##midDrag" + lowProperty.Name,
                    new Vector2(midRight - midLeft, TrackHeight * 2f));
                if (ImGui.IsItemActive())
                {
                    float dx = ImGui.GetIO().MouseDelta.X;
                    if (dx != 0)
                    {
                        double delta = PxDeltaToValue(dx);
                        // Clamp so neither bound crosses the shared axis limits.
                        double maxUp = axisMax - highVal;
                        double maxDown = axisMin - lowVal;
                        delta = Math.Clamp(delta, maxDown, maxUp);
                        if (Math.Abs(delta) > 0)
                        {
                            lowProperty.SetValueFromInput(lowVal + delta);
                            highProperty.SetValueFromInput(highVal + delta);
                        }
                    }
                }
                else if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Drag to shift the whole range");
                }
            }

            // Left handle drag
            ImGui.SetCursorScreenPos(new Vector2(lowPx - HandleRadius, trackY - HandleRadius));
            ImGui.InvisibleButton("##lowHandle" + lowProperty.Name,
                new Vector2(HandleRadius * 2f, HandleRadius * 2f));
            bool lowHover = ImGui.IsItemHovered();
            if (ImGui.IsItemActive())
            {
                float dx = ImGui.GetIO().MouseDelta.X;
                if (dx != 0)
                {
                    // low must stay >= highVal - maxGap (gap constraint) AND >= axisMin AND <= highVal
                    double lowFloor = Math.Max(axisMin, highVal - maxGap);
                    double newLow = Math.Clamp(lowVal + PxDeltaToValue(dx), lowFloor, highVal);
                    lowProperty.SetValueFromInput(newLow);
                }
            }

            // Right handle drag
            ImGui.SetCursorScreenPos(new Vector2(highPx - HandleRadius, trackY - HandleRadius));
            ImGui.InvisibleButton("##highHandle" + lowProperty.Name,
                new Vector2(HandleRadius * 2f, HandleRadius * 2f));
            bool highHover = ImGui.IsItemHovered();
            if (ImGui.IsItemActive())
            {
                float dx = ImGui.GetIO().MouseDelta.X;
                if (dx != 0)
                {
                    // high must stay <= lowVal + maxGap (gap constraint) AND >= lowVal AND <= axisMax
                    double highCeil = Math.Min(axisMax, lowVal + maxGap);
                    double newHigh = Math.Clamp(highVal + PxDeltaToValue(dx), lowVal, highCeil);
                    highProperty.SetValueFromInput(newHigh);
                }
            }

            // Draw handles last so they sit on top of the fill.
            drawList.AddCircleFilled(new Vector2(lowPx, trackY), HandleRadius,
                lowHover ? colHandleHover : colHandle);
            drawList.AddCircle(new Vector2(lowPx, trackY), HandleRadius, colHandleOutline, 16, 1.5f);
            drawList.AddCircleFilled(new Vector2(highPx, trackY), HandleRadius,
                highHover ? colHandleHover : colHandle);
            drawList.AddCircle(new Vector2(highPx, trackY), HandleRadius, colHandleOutline, 16, 1.5f);

            // Peaked value labels under each handle.
            uint colLabelBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.14f, 0.22f, 0.95f));
            uint colLabelBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(0.55f, 0.75f, 1.00f, 1.00f));
            uint colLabelText = ImGui.ColorConvertFloat4ToU32(new Vector4(0.92f, 0.96f, 1.00f, 1.00f));
            float labelGap = 6f;
            float labelTopY = trackY + HandleRadius + labelGap;
            float peakOverhang = 7f;
            float labelPadX = 6f;
            float labelPadY = 3f;

            // Labels may extend into the edge padding (full content region), so the peak can
            // stay centered under a handle that sits at the very end of the track.
            float widgetLeftX = widgetLeftAnchorX;
            float widgetRightX = widgetLeftAnchorX + width;
            DrawPeakedLabel(drawList, lowPx, labelTopY, peakOverhang, labelPadX, labelPadY,
                widgetLeftX, widgetRightX, lowVal.ToString("0.0"),
                colLabelBg, colLabelBorder, colLabelText, out float lowLabelBottomY);
            DrawPeakedLabel(drawList, highPx, labelTopY, peakOverhang, labelPadX, labelPadY,
                widgetLeftX, widgetRightX, highVal.ToString("0.0"),
                colLabelBg, colLabelBorder, colLabelText, out float highLabelBottomY);

            float labelsBottomY = Math.Max(lowLabelBottomY, highLabelBottomY);
            float widgetHeight = labelsBottomY - ImGui.GetCursorScreenPos().Y + 14f;
            ImGui.SetCursorPos(startCursor);
            ImGui.Dummy(new Vector2(width, widgetHeight));
            ImGui.NewLine();
        }

        private static void DrawPeakedLabel(
            ImDrawListPtr drawList, float peakX, float labelTopY,
            float peakOverhang, float padX, float padY,
            float clampLeftX, float clampRightX,
            string text, uint colBg, uint colBorder, uint colText,
            out float bottomY)
        {
            var textSize = ImGui.CalcTextSize(text);
            float boxW = MathF.Max(textSize.X + padX * 2f, 26f);
            float boxH = textSize.Y + padY * 2f;
            float boxTopY = labelTopY;
            float boxBottomY = boxTopY + boxH;
            float peakY = boxTopY - peakOverhang;
            float boxLeftX = MathF.Max(clampLeftX, MathF.Min(clampRightX - boxW, peakX - boxW * 0.5f));
            float boxRightX = boxLeftX + boxW;
            // Keep the peak inside the box (with a small inset so the slope reads).
            float peakInset = MathF.Min(8f, boxW * 0.5f - 2f);
            float clampedPeakX = MathF.Max(boxLeftX + peakInset, MathF.Min(boxRightX - peakInset, peakX));

            // Fill: rectangle body + triangle pointer share the same color, so the seam is invisible.
            drawList.AddRectFilled(new Vector2(boxLeftX, boxTopY), new Vector2(boxRightX, boxBottomY), colBg, 2f);
            drawList.AddTriangleFilled(
                new Vector2(clampedPeakX, peakY),
                new Vector2(boxLeftX, boxTopY),
                new Vector2(boxRightX, boxTopY),
                colBg);

            // Border: only the outer pentagon edges (no internal box-top line).
            float t = 1.2f;
            drawList.AddLine(new Vector2(boxLeftX, boxBottomY), new Vector2(boxLeftX, boxTopY), colBorder, t);
            drawList.AddLine(new Vector2(boxLeftX, boxTopY), new Vector2(clampedPeakX, peakY), colBorder, t);
            drawList.AddLine(new Vector2(clampedPeakX, peakY), new Vector2(boxRightX, boxTopY), colBorder, t);
            drawList.AddLine(new Vector2(boxRightX, boxTopY), new Vector2(boxRightX, boxBottomY), colBorder, t);
            drawList.AddLine(new Vector2(boxRightX, boxBottomY), new Vector2(boxLeftX, boxBottomY), colBorder, t);

            // Centered text.
            drawList.AddText(
                new Vector2(boxLeftX + (boxW - textSize.X) * 0.5f, boxTopY + padY),
                colText, text);

            bottomY = boxBottomY;
        }

        private void GuiHintTechSelection(ComponentDesignProperty property, GlobalUIState uiState)
        {
            Title(property.Name, property.Description);

            int i = 0;
            _techSDs = new Tech[property.GuidDictionary.Count];
            _techNames = new string[property.GuidDictionary.Count];
            foreach (var kvp in property.GuidDictionary)
            {
                Tech sd = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.Techs[(string)kvp.Key];
                _techSDs[i] = sd;
                _techNames[i] = sd.Name;
                i++;
            }

            ImGui.TextWrapped(property.Value.ToString());

            if (ImGui.Combo("Select Tech", ref _techSelectedIndex, _techNames, _techNames.Length))
            {
                property.SetValueFromString(_techSDs[_techSelectedIndex].UniqueID);
            }

            ImGui.NewLine();
        }

        private void GuiHintEnumSelection(ComponentDesignProperty property)
        {
            _listNames = Enum.GetNames(property.EnumType);

            int maxValue = Math.Min((int)property.MaxValue, _listNames.Length);
            int minValue = Math.Max((int)property.MinValue, 0);
            int length = maxValue - minValue;

            _listNames = new string[length];
            for (int i = 0; i < length; i++)
            {
                _listNames[i] = Enum.GetName(property.EnumType, i + minValue) ?? string.Empty;
            }

            Title(property.Name, property.Description);

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.Combo("###Select", ref property.ListSelection, _listNames, length))
            {
                int enumVal = (int)Enum.Parse(property.EnumType, _listNames[property.ListSelection]);
                property.SetValueFromInput(enumVal);
            }

            ImGui.NewLine();
        }

        private void GuiHintOrdnanceSelection(ComponentDesignProperty property, GlobalUIState uiState)
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

            Title(property.Name, property.Description);

            ImGui.TextWrapped(property.Value.ToString());

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.Combo("###Select", ref property.ListSelection, _listNames, _listNames.Length))
            {
                property.SetValueFromString(ordnances[property.ListSelection].UniqueID);
            }

            ImGui.NewLine();
        }

        private void GuiHintFuelTypeSelection(ComponentDesignProperty property, GlobalUIState uiState)
        {

            var cargoTypesToDisplay = GetFuelTypes(property, uiState);
            var names = new List<string>();
            foreach (var cargoType in cargoTypesToDisplay)
            {
                names.Add(cargoType.Name);
            }

            string[] arrayNames = names.ToArray();

            Title(property.Name, property.Description);

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if(ImGui.Combo("###cargotypeselection", ref property.ListSelection, arrayNames, arrayNames.Length))
            {
                property.SetValueFromString(cargoTypesToDisplay[property.ListSelection].UniqueID);
            }
        }

        List<ICargoable> GetFuelTypes(ComponentDesignProperty property, GlobalUIState uiState)
        {
            var cargoTypesToDisplay = new List<ICargoable>();

            foreach(string cargoType in property.GuidDictionary.Keys)
            {
                var fuelType = property.GuidDictionary[cargoType].StrResult;
                string cargoTypeID = cargoType.ToString();
                var cargos = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAll().Where(c => c.Value.CargoTypeID.Equals(cargoTypeID));
                foreach(var cargo in cargos)
                {
                    if(cargo.Value is ProcessedMaterial
                       && ((ProcessedMaterial)cargo.Value).Formulas != null
                       && ((ProcessedMaterial)cargo.Value).Formulas.ContainsKey("ExhaustVelocity")
                       && ((ProcessedMaterial)cargo.Value).Formulas["ExhaustVelocity"].IsNotNullOrEmpty()
                       && ((ProcessedMaterial)cargo.Value).Formulas.ContainsKey("FuelType")
                       && ((ProcessedMaterial)cargo.Value).Formulas["FuelType"] == fuelType)
                    {
                        cargoTypesToDisplay.Add(cargo.Value);
                    }
                }
            }
            return cargoTypesToDisplay;
        }

        private void GuiHintTextSelectionFormula(ComponentDesignProperty property)
        {
            _listNames = new string[property.GuidDictionary.Count];

            int i = 0;
            foreach (var kvp in property.GuidDictionary)
            {
                _listNames[i] = (string)kvp.Key;
                i++;
            }

            Title(property.Name, property.Description);

            ImGui.TextWrapped(property.Value.ToString());

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.Combo("###Select", ref property.ListSelection, _listNames, _listNames.Length))
            {
                var key = _listNames[property.ListSelection];
                var value = property.GuidDictionary[key];
                property.SetValueFromDictionaryExpression(_listNames[property.ListSelection]);
            }
        }

        private void GuiHintTechCategorySelection(ComponentDesignProperty property, GlobalUIState uiState)
        {
            _listNames = new string[uiState.Game.TechCategories.Count];

            int i = 0;
            foreach (var kvp in uiState.Game.TechCategories)
            {
                _listNames[i] = (string)kvp.Value.Name;
                i++;
            }

            Title(property.Name, property.Description);
            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.Combo("###Select", ref property.ListSelection, _listNames, _listNames.Length))
            {
                var name = _listNames[property.ListSelection];
                var value = uiState.Game.TechCategories.Where(c => c.Value.Name.Equals(name)).First();
                property.SetValueFromString(value.Key);
            }
        }

        private void DisplayPleaseSelectTemplate()
        {
            var windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ComponentDesignSelectTemplate", windowContentSize, ImGuiChildFlags.None))
            {
                string message = "Please select a template on the left.";
                var size = ImGui.GetContentRegionAvail();
                var textSize = ImGui.CalcTextSize(message);
                ImGui.SetCursorPos(new Vector2(size.X / 2 - textSize.X / 2, size.Y / 2 - textSize.Y / 2));
                ImGui.Text(message);
            }
            ImGui.EndChild();
        }

        private void DisplayCreatedTemplate()
        {
            var windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ComponentDesignCreated", windowContentSize, ImGuiChildFlags.None))
            {
                string message = "Design has been created, it will now be availble to Research.";
                var size = ImGui.GetContentRegionAvail();
                var textSize = ImGui.CalcTextSize(message);
                ImGui.SetCursorPos(new Vector2(size.X / 2 - textSize.X / 2, size.Y / 2 - textSize.Y / 2));
                ImGui.Text(message);
            }
            ImGui.EndChild();
        }

        private void Title(string title, string tooltip)
        {
            ImGui.Text(title);

            if(tooltip.IsNullOrEmpty()) return;

            ImGui.SameLine();
            ImGui.Text("[?]");
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }
    }
}


