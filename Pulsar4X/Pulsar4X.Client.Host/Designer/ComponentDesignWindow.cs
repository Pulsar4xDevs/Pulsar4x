using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class ComponentDesignWindow : UniquePulsarGuiWindow<ComponentDesignWindow>
    {
        // Derived lookups, rebuilt only when a new ComponentDesignsSnapshot is pushed (reference
        // change) — the server pushes one on connect and whenever a design is created.
        private static ComponentDesignsSnapshot? designs;
        private static List<ComponentTemplateSummary> filteredTemplates = new();
        private static string[] sortedGroupNames = Array.Empty<string>();
        private static int selectedFilterIndex = 0;
        private static ComponentTemplateSummary? selectedTemplate;

        private ComponentDesignWindow() { }

        internal static ComponentDesignWindow GetInstance()
        {
            ComponentDesignWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ComponentDesignWindow)))
            {
                thisitem = new ComponentDesignWindow();
            }
            thisitem = (ComponentDesignWindow)_uiState.LoadedWindows[typeof(ComponentDesignWindow)];

            return thisitem;
        }

        private static void RefreshDerivedData(ComponentDesignsSnapshot snapshot)
        {
            designs = snapshot;

            var groupNames = snapshot.Templates
                .Select(t => t.ComponentType)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            sortedGroupNames = new string[groupNames.Count + 1];
            sortedGroupNames[0] = "All";
            groupNames.CopyTo(sortedGroupNames, 1);

            if (selectedFilterIndex >= sortedGroupNames.Length)
                selectedFilterIndex = 0;
            RefreshFilteredTemplates();
        }

        private static void RefreshFilteredTemplates()
        {
            if (designs == null) return;

            filteredTemplates = selectedFilterIndex == 0
                ? designs.Templates.ToList()
                : designs.Templates.Where(t => t.ComponentType.Equals(sortedGroupNames[selectedFilterIndex])).ToList();
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if(Window.Begin("Component Designer", ref IsActive, _flags))
            {
                var snapshot = _uiState.GameClient?.Galaxy?.ComponentDesigns;
                if (snapshot != null && !ReferenceEquals(snapshot, designs))
                    RefreshDerivedData(snapshot);

                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X * 0.15f, windowContentSize.Y);
                var secondChildSize = new Vector2(windowContentSize.X * 0.15f, windowContentSize.Y);
                var thirdChildSize = new Vector2(windowContentSize.X * 0.7f - (windowContentSize.X * 0.01f), windowContentSize.Y);

                if(ImGui.BeginChild("ComponentDesignSelection", firstChildSize, ImGuiChildFlags.Borders))
                {
                    DisplayTemplateSelection();
                }
                ImGui.EndChild();
                ImGui.SameLine();
                if (ImGui.BeginChild("ComponentSelection", secondChildSize, ImGuiChildFlags.Borders))
                {
                    DisplayComponentList();
                }
                ImGui.EndChild();
                ImGui.SameLine();
                if (ImGui.BeginChild("ComponentDesign", thirdChildSize, ImGuiChildFlags.None))
                {
                    if(selectedTemplate != null)
                    {
                        ComponentDesignDisplay.GetInstance().Display(_uiState);
                    }
                }
                ImGui.EndChild();


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
            if(ImGui.Combo("###template-filter", ref selectedFilterIndex, sortedGroupNames, sortedGroupNames.Length))
            {
                RefreshFilteredTemplates();
            }

            foreach(var template in filteredTemplates)
            {
                bool isSelected = selectedTemplate == template;
                if (ImGui.Selectable(template.Name + "###component-" + template.Id, isSelected))
                {
                    selectedTemplate = template;
                    ComponentDesignDisplay.GetInstance().SetTemplate(selectedTemplate, _uiState);
                }
                DisplayHelpers.DescriptiveTooltip(template.Name, template.ComponentType, template.Description);
            }
        }

        void DisplayComponentList()
        {
            DisplayHelpers.Header("Current Component Designs of this type");

            if (designs != null && selectedTemplate != null)
            {
                foreach (var design in designs.Designs.Where(d => d.TemplateId == selectedTemplate.Id))
                {
                    if (ImGui.Selectable(design.Name + "###component-" + design.Id, false))
                    {
                        ComponentDesignDisplay.GetInstance().SetFromComponent(design, _uiState);
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
