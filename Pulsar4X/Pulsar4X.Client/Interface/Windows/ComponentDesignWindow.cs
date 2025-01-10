using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Blueprints;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Factions;

namespace Pulsar4X.SDL2UI
{
    public class ComponentDesignWindow : PulsarGuiWindow
    {
        private static List<ComponentTemplateBlueprint> templates = new();
        private static List<ComponentTemplateBlueprint> filteredTemplates = new ();
        private static string[]? sortedGroupNames;
        private static int selectedFilterIndex = 0;
        private static ComponentTemplateBlueprint? selectedTemplate;
        
        
        private static Dictionary<string, ComponentDesign> componentDesigns = new();
        private static List<ComponentDesign> componentsOfType = new();
        private static string[]? componentNames = new string[0];
        private static ComponentDesign selectedComponent;
        
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
                componentDesigns = _uiState.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.ToDictionary();
            }
            thisitem = (ComponentDesignWindow)_uiState.LoadedWindows[typeof(ComponentDesignWindow)];

            return thisitem;
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if(Window.Begin("Component Designer", ref IsActive, _flags))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X * 0.15f, windowContentSize.Y);
                var secondChildSize = new Vector2(windowContentSize.X * 0.15f, windowContentSize.Y);
                var thirdChildSize = new Vector2(windowContentSize.X * 0.7f - (windowContentSize.X * 0.01f), windowContentSize.Y);

                if(ImGui.BeginChild("ComponentDesignSelection", firstChildSize, true))
                {
                    DisplayTemplateSelection();
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                if (ImGui.BeginChild("ComponentSelection", secondChildSize, true))
                {
                    DisplayComponentList();
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                if (ImGui.BeginChild("ComponentDesign", thirdChildSize, true))
                {
                    if(selectedTemplate != null)
                    {
                        ComponentDesignDisplay.GetInstance().Display(_uiState);
                    }
                    ImGui.EndChild();
                }


                ImGui.SameLine();
                //ImGui.SetCursorPosY(27f); // FIXME: this should somehow be calculated
                

                Window.End();
            }
        }

        void DisplayTemplateSelection()
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
                bool isSelected = selectedTemplate == template;
                if (ImGui.Selectable(template.Name + "###component-" + template.UniqueID, isSelected))
                {
                    selectedTemplate = template;
                    ComponentDesignDisplay.GetInstance().SetTemplate(selectedTemplate, _uiState);
                    componentsOfType = new List<ComponentDesign>();
                    foreach (var cd in componentDesigns)
                    {
                        if(cd.Value.TemplateName == template.Name)
                        {
                            componentsOfType.Add(cd.Value);
                        }
                    }
                    if(componentsOfType.Count > 0)
                    {
                        componentNames = new string[componentsOfType.Count];
                        for (int c = 0; c < componentsOfType.Count; c++)
                        {
                            componentNames[c] = componentsOfType[c].Name;
                        }
                    }
                    
                }
                DisplayHelpers.DescriptiveTooltip(template.Name, template.ComponentType, template.Formulas["Description"]);
            }
        }

        void DisplayComponentList()
        {
            DisplayHelpers.Header("Current Component Designs of this type");

            var availableSize = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(availableSize.X);
            if (componentNames.Length > 0)
            {
                for (int index = 0; index < componentsOfType.Count; index++)
                {
                    ComponentDesign? component = componentsOfType[index];
                    bool isSelected = componentsOfType[index] == component;
                    if (ImGui.Selectable(component.Name + "###component-" + component.UniqueID, isSelected))
                    {
                        ComponentDesignDisplay.GetInstance().SetFromComponent(component, _uiState);
                    }
                }
            }
            
            ImGui.BeginDisabled();
            if(ImGui.Button("Create Template", new Vector2(204f, 0f)))
            {

            }
            ImGui.EndDisabled();
    
        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }
    }
}