using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class ComponentDesignWindow : PulsarGuiWindow
    {
        private static List<ComponentTemplateSD> templates = new();
        private ComponentDesignWindow() { }

        internal static ComponentDesignWindow GetInstance()
        {
            ComponentDesignWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ComponentDesignWindow)))
            {
                thisitem = new ComponentDesignWindow();

                // FIXME: doing this here is efficient but it will never update the list if new templates are available
                templates = StaticRefLib.StaticData.ComponentTemplates.Values.ToList();
                templates.Sort((a, b) => a.Name.CompareTo(b.Name));
            }
            thisitem = (ComponentDesignWindow)_uiState.LoadedWindows[typeof(ComponentDesignWindow)];

            return thisitem;
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if(ImGui.Begin("Designer", ref IsActive, _flags))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();

                if(ImGui.BeginChild("ComponentDesignSelection", new Vector2(204f, windowContentSize.Y - 24f), true))
                {
                    DisplayHelpers.Header("Select a Template",
                            "Component Templates act as a framework for designing components.\n\n" +
                            "Select a template and then design the attributes of the component to your specification.\n" +
                            "Once the design is created it will be available to produce on the colonies with the appropriate\n" +
                            "installations.");

                    foreach(var template in templates)
                    {
                        var selected = ComponentDesignDisplay.GetInstance().Template?.Name.Equals(template.Name);

                        if (ImGui.Selectable(template.Name + "###component-" + template.ID, selected.HasValue && selected.Value))
                        {
                            ComponentDesignDisplay.GetInstance().SetTemplate(template, _uiState);
                        }
                        if(ImGui.IsItemHovered())
                        {
                            ImGui.SetNextWindowSize(new Vector2(384, 0));
                            ImGui.BeginTooltip();
                            ImGui.Text(template.Name);
                            if(template.ComponentType.IsNotNullOrEmpty())
                            {
                                var size = ImGui.GetContentRegionAvail();
                                var textSize = ImGui.CalcTextSize(template.ComponentType);
                                ImGui.SameLine();
                                ImGui.SetCursorPosX(size.X - textSize.X);
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                                ImGui.Text(template.ComponentType);
                                ImGui.PopStyleColor();
                            }
                            if(template.DescriptionFormula.IsNotNullOrEmpty())
                            {
                                ImGui.Separator();
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                                ImGui.TextWrapped(template.DescriptionFormula);
                                ImGui.PopStyleColor();
                            }
                            ImGui.EndTooltip();
                        }
                    }

                    ImGui.EndChild();
                }

                ImGui.BeginDisabled();
                if(ImGui.Button("Create Template", new Vector2(204f, 0f)))
                {

                }
                ImGui.EndDisabled();

                ImGui.SameLine();
                ImGui.SetCursorPosY(27f); // FIXME: this should somehow be calculated
                ComponentDesignDisplay.GetInstance().Display(_uiState);

                ImGui.End();
            }
        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
            throw new NotImplementedException();
        }
    }
}