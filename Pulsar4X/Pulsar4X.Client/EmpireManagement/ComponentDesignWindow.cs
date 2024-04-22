using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{
    public class ComponentDesignWindow : PulsarGuiWindow
    {
        private static List<ComponentTemplateBlueprint> templates = new();
        private static List<ComponentTemplateBlueprint> filteredTemplates = new ();
        private static string[]? sortedGroupNames;
        private static int selectedFilterIndex = 0;
        private ComponentDesignWindow() { }

        internal static ComponentDesignWindow GetInstance()
        {
            ComponentDesignWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ComponentDesignWindow)))
            {
                thisitem = new ComponentDesignWindow();

                // FIXME: doing this here is efficient but it will never update the list if new templates are available
                templates = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data.ComponentTemplates.Select(kvp => kvp.Value).ToList();
                templates.Sort((a, b) => a.Name.CompareTo(b.Name));

                var templatesByGroup = templates.GroupBy(t => t.ComponentType);
                var groupNames = templatesByGroup.Select(g => g.Key).ToList();
                var sortedTempGroupNames = groupNames.OrderBy(name => name).ToArray();
                sortedGroupNames = new string[sortedTempGroupNames.Length + 1];
                sortedGroupNames[0] = "All";
                Array.Copy(sortedTempGroupNames, 0, sortedGroupNames, 1, sortedTempGroupNames.Length);

                filteredTemplates = new List<ComponentTemplateBlueprint>(templates);
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

                    var availableSize = ImGui.GetContentRegionAvail();
                    ImGui.SetNextItemWidth(availableSize.X);
                    if(ImGui.Combo("###template-filter", ref selectedFilterIndex, sortedGroupNames, sortedGroupNames?.Length ?? 0))
                    {
                        if(selectedFilterIndex == 0)
                        {
                            filteredTemplates = new List<ComponentTemplateBlueprint>(templates);
                        }
                        else
                        {
                            filteredTemplates = templates.Where(t => t.ComponentType.Equals(sortedGroupNames?[selectedFilterIndex])).ToList();
                        }
                    }

                    foreach(var template in filteredTemplates)
                    {
                        var selected = ComponentDesignDisplay.GetInstance().Template?.Name.Equals(template.Name);

                        if (ImGui.Selectable(template.Name + "###component-" + template.UniqueID, selected.HasValue && selected.Value))
                        {
                            ComponentDesignDisplay.GetInstance().SetTemplate(template, _uiState);
                        }
                        DisplayHelpers.DescriptiveTooltip(template.Name, template.ComponentType, template.Formulas["Description"]);
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
    }
}