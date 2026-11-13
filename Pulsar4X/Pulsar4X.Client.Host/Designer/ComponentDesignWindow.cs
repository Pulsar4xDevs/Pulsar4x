using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine.Api;

namespace Pulsar4X.Client
{
    public class ComponentDesignWindow : UniquePulsarGuiWindow<ComponentDesignWindow>
    {
        // Derived lookups, rebuilt only when a new ComponentDesignsSnapshot is pushed (reference
        // change) — the server pushes one on connect, whenever a design is created, and when
        // research completes (tech can change what a saved design would evaluate to).
        private static ComponentDesignsSnapshot? designs;
        private static List<ComponentTemplateSummary> filteredTemplates = new();
        private static readonly DesignFilterWidget _templateFilter = new();
        private static ComponentTemplateSummary? selectedTemplate;
        private static Dictionary<string, List<StatDelta>> _upgradeDeltas = new();

        private ComponentDesignWindow() { }

        internal static ComponentDesignWindow GetInstance()
        {
            if(_uiState.TryGetUniqueWindow<ComponentDesignWindow>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow(new ComponentDesignWindow());
        }

        private static void RefreshDerivedData(ComponentDesignsSnapshot snapshot)
        {
            designs = snapshot;
            _upgradeDeltas = ComputeUpgradeDeltas(snapshot);
        }

        /// <summary>
        /// Replay each saved design at current tech and cache improved-stat deltas. This is
        /// expensive, so it runs once per snapshot reference change — never in the 60fps list loop.
        /// </summary>
        private static Dictionary<string, List<StatDelta>> ComputeUpgradeDeltas(ComponentDesignsSnapshot snapshot)
        {
            var result = new Dictionary<string, List<StatDelta>>();
            if (_uiState.Lifecycle is not IDesignDataProvider provider
                || !provider.TryGetDesignData(out var info, out var techs))
                return result;

            foreach (var design in snapshot.Designs)
            {
                if (design.PropertyValues == null || design.PropertyValues.Count == 0)
                    continue;
                if (!info.Data.ComponentTemplates.TryGetValue(design.TemplateId, out var blueprint))
                    continue;

                try
                {
                    var designer = DesignerInputs.Build(info.Data, techs, blueprint, design.PropertyValues);
                    var improved = ComponentUpgradeHelper.ImprovedDeltas(
                        ComponentUpgradeHelper.Compare(design, designer));
                    if (improved.Count > 0)
                        result[design.Id] = improved;
                }
                catch
                {
                    // A design that cannot be replayed (missing property, bad formula) is simply not flagged.
                }
            }

            return result;
        }

        internal override void Display()
        {
            if(!IsActive) return;

            ImGui.SetNextWindowSize(new Vector2(1200, 780), ImGuiCond.FirstUseEver);

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

            if (designs != null)
            {
                _templateFilter.Draw("##template-filter", designs.Templates, DesignFilterWidget.DescribeTemplate, out filteredTemplates);
            }
            else
            {
                filteredTemplates = new List<ComponentTemplateSummary>();
            }

            // selectedTemplate is kept by template Id / value equality, not by filtered index, so
            // changing the Category/Type/search filter cannot point at the wrong template or
            // throw if the previous selection is hidden.
            foreach(var template in filteredTemplates)
            {
                bool isSelected = selectedTemplate != null && selectedTemplate.Id == template.Id;
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
                    bool upgradeable = _upgradeDeltas.TryGetValue(design.Id, out var improved) && improved.Count > 0;
                    bool plusHovered = false;
                    if (upgradeable)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.GoodColor);
                        ImGui.TextUnformatted("[+]");
                        ImGui.PopStyleColor();
                        plusHovered = ImGui.IsItemHovered();
                        ImGui.SameLine();
                    }

                    if (ImGui.Selectable(design.Name + "###component-" + design.Id, false))
                    {
                        ComponentDesignDisplay.GetInstance().SetFromComponent(design, _uiState);
                    }

                    if (upgradeable && (plusHovered || ImGui.IsItemHovered()))
                    {
                        var tooltip = FormatUpgradeTooltip(improved!);
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(tooltip);
                        ImGui.EndTooltip();
                    }
                }
            }

            ImGui.BeginDisabled();
            if(ImGui.Button("Create Template", new Vector2(204f, 0f)))
            {

            }
            ImGui.EndDisabled();

        }

        static string FormatUpgradeTooltip(List<StatDelta> improved)
        {
            var sb = new StringBuilder("Upgrade available:");
            foreach (var delta in improved)
            {
                sb.Append('\n');
                sb.Append(delta.Name);
                sb.Append(": ");
                sb.Append(delta.FormatSignedDelta());
            }
            return sb.ToString();
        }
    }
}
