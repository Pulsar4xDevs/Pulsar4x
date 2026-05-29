using ImGuiNET;
using Pulsar4X.Blueprints;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Components;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Technology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Pulsar4X.Client.Interface.Windows;

public class ComponentsWindow : PulsarGuiWindow
{
    private string _selectedItemId = "";
    private object? _selectedItem = null;
    private bool _showTemplates = true;
    private bool _showDesigns = true;
    private string _searchFilter = "";

    public static ComponentsWindow GetInstance()
    {
        ComponentsWindow instance;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(ComponentsWindow)))
        {
            instance = new ComponentsWindow();
        }
        else
        {
            instance = (ComponentsWindow)_uiState.LoadedWindows[typeof(ComponentsWindow)];
        }

        return instance;
    }

    private void DisplayComponentCategory(string label, List<string> templateIds, List<string> designIds)
    {
        if(ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(_showTemplates && templateIds.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.9f, 1.0f, 1.0f)); // Light blue for templates
                ImGui.Text($"📋 Templates ({templateIds.Count})");
                ImGui.PopStyleColor();

                ImGui.Indent();
                foreach(var templateId in templateIds.OrderBy(k => k))
                {
                    var template = _uiState.Faction.GetDataBlob<Pulsar4X.Factions.FactionInfoDB>().Data.ComponentTemplates[templateId];

                    if(string.IsNullOrEmpty(_searchFilter) || template.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        bool isSelected = _selectedItemId.Equals(templateId);
                        if(isSelected)
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.6f, 1.0f));

                        if(ImGui.Selectable($"  {template.Name}##template_{templateId}", isSelected))
                        {
                            _selectedItemId = templateId;
                            _selectedItem = template;
                        }

                        if(isSelected)
                            ImGui.PopStyleColor();
                    }
                }
                ImGui.Unindent();
                ImGui.Spacing();
            }

            if(_showDesigns && designIds.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 1.0f, 0.7f, 1.0f)); // Light green for designs
                ImGui.Text($"⚙️ Designs ({designIds.Count})");
                ImGui.PopStyleColor();

                ImGui.Indent();
                foreach(var designId in designIds.OrderBy(k => k))
                {
                    var design = _uiState.Faction.GetDataBlob<Pulsar4X.Factions.FactionInfoDB>().ComponentDesigns[designId];

                    if(string.IsNullOrEmpty(_searchFilter) || design.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        bool isSelected = _selectedItemId.Equals(designId);
                        if(isSelected)
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.6f, 1.0f));

                        string statusIcon = design.IsValid ? "✅" : "❌";
                        if(ImGui.Selectable($"  {statusIcon} {design.Name}##design_{designId}", isSelected))
                        {
                            _selectedItemId = designId;
                            _selectedItem = design;
                        }

                        if(isSelected)
                            ImGui.PopStyleColor();

                        if(ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"Status: {(design.IsValid ? "Valid" : "Invalid")}");
                            ImGui.Text($"Mass: {design.MassPerUnit} kg");
                            ImGui.Text($"Volume: {design.VolumePerUnit:F2} m³");
                            ImGui.EndTooltip();
                        }
                    }
                }
                ImGui.Unindent();
                ImGui.Spacing();
            }
        }
    }

    internal override void Display()
    {
        if(!IsActive) return;

        if(Window.Begin("Component Library", ref IsActive, ImGuiWindowFlags.MenuBar))
        {
            if(ImGui.BeginMenuBar())
            {
                if(ImGui.BeginMenu("View"))
                {
                    ImGui.MenuItem("Show Templates", "", ref _showTemplates);
                    ImGui.MenuItem("Show Designs", "", ref _showDesigns);
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }

            var factionInfoDB = _uiState.Faction.GetDataBlob<Pulsar4X.Factions.FactionInfoDB>();

            // Header with search
            //ImGui.PushFont(_uiState.Fonts.NotoSans20);
            ImGui.Text("🔧 Component Library");
            //ImGui.PopFont();

            ImGui.Text("Browse available component templates and faction designs");
            ImGui.Separator();

            // Search and filters
            ImGui.SetNextItemWidth(200);
            ImGui.InputTextWithHint("##search", "Search components...", ref _searchFilter, 100);
            ImGui.SameLine();

            if(ImGui.Button("Clear##search"))
                _searchFilter = "";

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();

            // Split view
            if(ImGui.BeginChild("ComponentListSelection", new Vector2(350, windowContentSize.Y), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX))
            {
                var templatesByType = factionInfoDB.Data.ComponentTemplates.GroupBy(kvp => kvp.Value.ComponentType).ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());
                var designsByType = factionInfoDB.ComponentDesigns.GroupBy(kvp => kvp.Value.ComponentType).ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());

                var allTypes = templatesByType.Keys.Union(designsByType.Keys).OrderBy(k => k);

                foreach(var componentType in allTypes)
                {
                    var templates = templatesByType.ContainsKey(componentType) ? templatesByType[componentType] : new List<string>();
                    var designs = designsByType.ContainsKey(componentType) ? designsByType[componentType] : new List<string>();

                    // Filter by search if needed
                    if(!string.IsNullOrEmpty(_searchFilter))
                    {
                        templates = templates.Where(id => factionInfoDB.Data.ComponentTemplates[id].Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                        designs = designs.Where(id => factionInfoDB.ComponentDesigns[id].Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if(templates.Count > 0 || designs.Count > 0)
                        DisplayComponentCategory(componentType, templates, designs);
                }

                ImGui.EndChild();
            }

            ImGui.SameLine();

            // Details panel
            windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("ComponentDetails", windowContentSize, ImGuiChildFlags.Borders))
            {
                if(_selectedItem != null)
                {
                    if(_selectedItem is ComponentTemplateBlueprint template)
                        DisplayComponentTemplate(template);
                    else if(_selectedItem is ComponentDesign design)
                        DisplayComponentDesign(design);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                    ImGui.Text("Select a component from the list to view details...");
                    ImGui.PopStyleColor();
                }

                ImGui.EndChild();
            }

            Window.End();
        }
    }

    private void DisplayKeyValue(string key, string? value, bool important = false)
    {
        if(important)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.6f, 1.0f));

        ImGui.Text(key + ":");
        ImGui.SameLine();

        if(string.IsNullOrEmpty(value))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
            ImGui.Text("(none)");
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.TextUnformatted(value);
        }

        if(important)
            ImGui.PopStyleColor();
    }

    private void DisplayStatBar(string label, float value, float maxValue, Vector4 color, string unit = "", bool showValue = true)
    {
        ImGui.Text($"{label}:");

        if(showValue)
        {
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text($"{value:F1}{unit}");
            ImGui.PopStyleColor();
        }

        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        var size = new Vector2(200, 8);

        // Background
        drawList.AddRectFilled(cursor, cursor + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 0.8f)), 2.0f);

        // Fill
        float fillWidth = maxValue > 0 ? (value / maxValue) * size.X : 0;
        if(fillWidth > 0)
        {
            var gradientStart = ImGui.ColorConvertFloat4ToU32(color);
            var gradientEnd = ImGui.ColorConvertFloat4ToU32(color * 0.7f);
            drawList.AddRectFilledMultiColor(cursor, cursor + new Vector2(fillWidth, size.Y),
                gradientStart, gradientEnd, gradientEnd, gradientStart);
        }

        // Border
        drawList.AddRect(cursor, cursor + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.4f, 0.4f, 1.0f)), 2.0f);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + size.Y + 3);
    }

    private void DisplayPerformanceMetric(string icon, string label, string value, Vector4 color)
    {
        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        var cardSize = new Vector2(110, 35);

        // Background card
        drawList.AddRectFilled(cursor, cursor + cardSize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.15f, 0.9f)), 6.0f);
        drawList.AddRect(cursor, cursor + cardSize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.4f, 0.8f)), 6.0f);

        // Move cursor to inside the card
        ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(6, 3));

        // Icon
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text(icon);
        ImGui.PopStyleColor();

        ImGui.SameLine();

        // Label
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.8f, 1.0f));
        ImGui.Text(label);
        ImGui.PopStyleColor();

        ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(6, 0));

        // Value
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.95f, 1.0f, 1.0f));
        ImGui.Text(value);
        ImGui.PopStyleColor();

        // Move cursor to after the card
        ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(cardSize.X - 6, 3));
    }

    private void DisplayComponentTemplate(ComponentTemplateBlueprint template)
    {
        // Header
        ImGui.Text($"📋 {template.Name}");
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.9f, 1.0f, 1.0f));
        ImGui.Text("Component Template");
        ImGui.PopStyleColor();

        ImGui.Separator();
        ImGui.Spacing();

        // Create a designer to get calculated values
        var factionInfoDB = _uiState.Faction.GetDataBlob<FactionInfoDB>();
        var factionTechDB = _uiState.Faction.GetDataBlob<FactionTechDB>();
        ComponentDesigner designer;

        try
        {
            designer = new ComponentDesigner(template, factionInfoDB.Data, factionTechDB);
        }
        catch
        {
            DisplayKeyValue("Status", "Cannot evaluate template", true);
            return;
        }

        // Component Type Badge
        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        var badgeSize = new Vector2(150, 25);

        drawList.AddRectFilled(cursor, cursor + badgeSize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.4f, 0.8f, 0.3f)), 12.0f);
        drawList.AddRect(cursor, cursor + badgeSize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.6f, 1.0f, 0.8f)), 12.0f);

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);
        ImGui.Text($"🏷️ {template.ComponentType}");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8);

        ImGui.Spacing();

        // Performance metrics in a proper grid
        ImGui.Columns(3, "metrics", false);
        ImGui.SetColumnWidth(0, 120);
        ImGui.SetColumnWidth(1, 120);
        ImGui.SetColumnWidth(2, 120);

        // Row 1
        DisplayPerformanceMetric("⚖️", "Mass", $"{designer.MassValue:F1} kg", new Vector4(0.8f, 0.4f, 0.2f, 1.0f));
        ImGui.NextColumn();
        DisplayPerformanceMetric("📦", "Volume", $"{designer.VolumeM3Value:F1} m³", new Vector4(0.2f, 0.6f, 0.8f, 1.0f));
        ImGui.NextColumn();
        DisplayPerformanceMetric("👥", "Crew", $"{designer.CrewReqValue}", new Vector4(0.6f, 0.8f, 0.3f, 1.0f));
        ImGui.NextColumn();

        // Row 2
        DisplayPerformanceMetric("🔧", "Build Cost", $"{designer.IndustryPointCostsValue:F0} BP", new Vector4(0.9f, 0.6f, 0.1f, 1.0f));
        ImGui.NextColumn();
        DisplayPerformanceMetric("💰", "Credit Cost", $"{designer.CreditCostValue:F0}", new Vector4(0.1f, 0.8f, 0.3f, 1.0f));
        ImGui.NextColumn();
        DisplayPerformanceMetric("🧪", "Research", $"{designer.ResearchCostValue:F0} RP", new Vector4(0.7f, 0.3f, 0.9f, 1.0f));

        ImGui.Columns(1);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Mount type and requirements section
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
        ImGui.Text("🔩 Mounting & Requirements");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Spacing();

        DisplayKeyValue("Mount Type", template.MountType.ToString());
        DisplayKeyValue("Industry Type", template.IndustryTypeID);
        DisplayKeyValue("Cargo Type", template.CargoTypeID);

        ImGui.Spacing();

        // Resource Requirements section
        if(designer.ResourceCostValues.Count > 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
            ImGui.Text("🏭 Resource Requirements");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.Spacing();

            foreach(var kvp in designer.ResourceCostValues.OrderBy(kvp => kvp.Key))
            {
                var resourceName = factionInfoDB.Data.CargoGoods[kvp.Key]?.Name ?? kvp.Key;
                DisplayKeyValue(resourceName, $"{kvp.Value:F0}");
            }
            ImGui.Spacing();
        }

        // Customizable Properties section
        if(template.Properties?.Count > 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
            ImGui.Text("⚙️ Configurable Properties");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.Spacing();

            foreach(var prop in template.Properties.OrderBy(p => p.Name))
            {
                DisplayTemplateProperty(prop, designer);
                ImGui.Spacing();
            }
        }

        // Description section
        if(!string.IsNullOrEmpty(template.Formulas["Description"]))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
            ImGui.Text("📝 Description");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.9f, 1.0f));
            ImGui.TextWrapped(template.Formulas["Description"]);
            ImGui.PopStyleColor();
        }
    }

    private void DisplayTemplateProperty(ComponentTemplatePropertyBlueprint property, ComponentDesigner designer)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.9f, 1.0f, 1.0f));
        ImGui.Text($"🔧 {property.Name}");
        ImGui.PopStyleColor();

        if(designer.ComponentDesignProperties.ContainsKey(property.Name))
        {
            var designProp = designer.ComponentDesignProperties[property.Name];

            ImGui.Indent();

            // Show current/default value
            try
            {
                DisplayKeyValue("Default Value", designProp.Value.ToString() ?? "N/A");
            }
            catch
            {
                DisplayKeyValue("Default Value", "N/A");
            }

            // Show range if applicable
            DisplayKeyValue("Range", $"{designProp.MinValue:F1} - {designProp.MaxValue:F1}");

            try
            {
                if(designProp.Value is double val)
                {
                    float current = (float)val;
                    float min = (float)designProp.MinValue;
                    float max = (float)designProp.MaxValue;

                    if(max > min)
                    {
                        float normalized = (current - min) / (max - min);
                        DisplayStatBar("Value", current, max, new Vector4(0.4f, 0.8f, 0.6f, 1.0f), property.Units ?? "", false);
                    }
                }
            }
            catch
            {
                // Ignore stat bar if value is not a double
            }

            // Show units
            if(!string.IsNullOrEmpty(property.Units))
                DisplayKeyValue("Units", property.Units);

            // Show description if available
            if(!string.IsNullOrEmpty(designProp.Description))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.8f, 1.0f));
                ImGui.TextWrapped(designProp.Description);
                ImGui.PopStyleColor();
            }

            ImGui.Unindent();
        }
    }

    private void DisplayComponentDesign(ComponentDesign design)
    {
        string statusIcon = design.IsValid ? "✅" : "❌";
        Vector4 statusColor = design.IsValid ? new Vector4(0.3f, 0.8f, 0.3f, 1.0f) : new Vector4(0.8f, 0.3f, 0.3f, 1.0f);

        // Header with status indicator
        ImGui.Text($"⚙️ {design.Name}");

        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        var statusSize = new Vector2(80, 20);

        drawList.AddRectFilled(cursor, cursor + statusSize,
            ImGui.ColorConvertFloat4ToU32(statusColor * 0.3f), 10.0f);
        drawList.AddRect(cursor, cursor + statusSize,
            ImGui.ColorConvertFloat4ToU32(statusColor), 10.0f);

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2);
        ImGui.PushStyleColor(ImGuiCol.Text, statusColor);
        ImGui.Text($"{statusIcon} {(design.IsValid ? "VALID" : "INVALID")}");
        ImGui.PopStyleColor();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 1.0f, 0.7f, 1.0f));
        ImGui.Text("Component Design");
        ImGui.PopStyleColor();

        ImGui.Separator();
        ImGui.Spacing();

        // Component Type Badge
        cursor = ImGui.GetCursorScreenPos();
        var badgeSize = new Vector2(150, 25);

        drawList.AddRectFilled(cursor, cursor + badgeSize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.6f, 0.4f, 0.3f)), 12.0f);
        drawList.AddRect(cursor, cursor + badgeSize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.8f, 0.6f, 0.8f)), 12.0f);

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);
        ImGui.Text($"🏷️ {design.ComponentType}");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8);

        ImGui.Spacing();

        // Performance metrics in a proper grid
        ImGui.Columns(3, "designmetrics", false);
        ImGui.SetColumnWidth(0, 120);
        ImGui.SetColumnWidth(1, 120);
        ImGui.SetColumnWidth(2, 120);

        // Row 1 - Physical Properties
        DisplayPerformanceMetric("⚖️", "Mass", $"{design.MassPerUnit:F1} kg", new Vector4(0.8f, 0.4f, 0.2f, 1.0f));
        ImGui.NextColumn();
        DisplayPerformanceMetric("📦", "Volume", $"{design.VolumePerUnit:F1} m³", new Vector4(0.2f, 0.6f, 0.8f, 1.0f));
        ImGui.NextColumn();
        DisplayPerformanceMetric("🎯", "Density", $"{design.Density:F1} kg/m³", new Vector4(0.6f, 0.5f, 0.8f, 1.0f));
        ImGui.NextColumn();

        // Row 2 - Production
        DisplayPerformanceMetric("🔧", "Build Cost", $"{design.IndustryPointCosts:F0} BP", new Vector4(0.9f, 0.6f, 0.1f, 1.0f));
        ImGui.NextColumn();
        DisplayPerformanceMetric("💰", "Credits", $"{design.CreditCost:F0}", new Vector4(0.1f, 0.8f, 0.3f, 1.0f));
        ImGui.NextColumn();
        DisplayPerformanceMetric("👥", "Crew", $"{design.CrewReq}", new Vector4(0.6f, 0.8f, 0.3f, 1.0f));

        ImGui.Columns(1);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Design Information section
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
        ImGui.Text("🔩 Design Information");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Spacing();

        DisplayKeyValue("Based on Template", design.TemplateName);
        DisplayKeyValue("Mount Type", design.ComponentMountType.ToString());
        DisplayKeyValue("Industry Type", design.IndustryTypeID);
        DisplayKeyValue("Cargo Type", design.CargoTypeID);

        if(design.AspectRatio != 1.0f)
            DisplayKeyValue("Aspect Ratio", design.AspectRatio.ToString("F2"));

        if(design.DestructionPercent > 0)
            DisplayKeyValue("Hull Points", $"{design.DestructionPercent * 100:F1} %");

        ImGui.Spacing();

        // Resource Requirements section
        if(design.ResourceCosts?.Count > 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
            ImGui.Text("🏭 Resource Requirements");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.Spacing();

            var factionInfoDB = _uiState.Faction.GetDataBlob<FactionInfoDB>();

            foreach(var kvp in design.ResourceCosts.OrderBy(kvp => kvp.Key))
            {
                var resourceName = factionInfoDB.Data.CargoGoods[kvp.Key]?.Name ?? kvp.Key;

                ImGui.Text($"• {resourceName}");
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.3f, 1.0f));
                ImGui.Text($"{kvp.Value:F0}");
                ImGui.PopStyleColor();
            }
            ImGui.Spacing();
        }

        // Design Parameters section
        if(design.TemplatePropertyValues?.Count > 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
            ImGui.Text("⚙️ Design Parameters");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.Spacing();

            foreach(var prop in design.TemplatePropertyValues.OrderBy(p => p.propName))
            {
                string valueStr = prop.propValue?.ToString() ?? "null";

                ImGui.Text($"🔧 {prop.propName}:");
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.9f, 1.0f, 1.0f));
                ImGui.Text(valueStr);
                ImGui.PopStyleColor();

                if(prop.valueType != typeof(string))
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.7f, 1.0f));
                    ImGui.Text($"({prop.valueType.Name})");
                    ImGui.PopStyleColor();
                }
            }
            ImGui.Spacing();
        }

        // Component Attributes section
        if(design.AttributesByType?.Count > 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
            ImGui.Text("⚡ Component Attributes");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.Spacing();

            foreach(var kvp in design.AttributesByType.OrderBy(kvp => kvp.Value?.AtbName() ?? kvp.Key.Name))
            {
                string label = kvp.Value?.AtbName() ?? kvp.Key.Name;
                string value = kvp.Value?.AtbDescription() ?? "";

                ImGui.Text($"⚡ {label}:");
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.8f, 0.6f, 1.0f));
                ImGui.TextWrapped(string.IsNullOrWhiteSpace(value) ? "—" : value);
                ImGui.PopStyleColor();
            }
            ImGui.Spacing();
        }

        // Description section
        if(!string.IsNullOrEmpty(design.Description))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.7f, 0.4f, 1.0f));
            ImGui.Text("📝 Description");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.9f, 1.0f));
            ImGui.TextWrapped(design.Description);
            ImGui.PopStyleColor();
        }
    }
}