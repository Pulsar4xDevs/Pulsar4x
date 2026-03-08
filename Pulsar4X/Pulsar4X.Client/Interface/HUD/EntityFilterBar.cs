using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using System;

namespace Pulsar4X.Client;

/// <summary>
/// A small borderless toolbar in the upper right that provides toggle buttons
/// for showing/hiding different entity types on the system map.
/// </summary>
public class EntityFilterBar : PulsarGuiWindow
{
    private const string ViewKey = "map";

    private EntityFilterBar()
    {
        _flags = ImGuiWindowFlags.NoScrollbar
               | ImGuiWindowFlags.NoCollapse
               | ImGuiWindowFlags.NoTitleBar
               | ImGuiWindowFlags.AlwaysAutoResize
               | ImGuiWindowFlags.NoBackground
               | ImGuiWindowFlags.NoResize;
    }

    internal static EntityFilterBar GetInstance()
    {
        if (!_uiState.LoadedWindows.ContainsKey(typeof(EntityFilterBar)))
        {
            return new EntityFilterBar();
        }
        return (EntityFilterBar)_uiState.LoadedWindows[typeof(EntityFilterBar)];
    }

    internal override void Display()
    {
        if (!IsActive || _uiState.Faction == null) return;

        var prefs = SystemViewPreferences.GetInstance();

        // Position to the left of the Selector window (which is 256px wide at the right edge)
        float selectorWidth = 256;
        float xPos = ImGui.GetMainViewport().WorkSize.X - selectorWidth - EstimateWidth() - 8;
        ImGui.SetNextWindowPos(new Vector2(xPos, 4));
        ImGui.SetNextWindowBgAlpha(0);

        if (Window.Begin("###entity-filter-bar", _flags))
        {
            foreach (UserOrbitSettings.OrbitBodyType type in Enum.GetValues(typeof(UserOrbitSettings.OrbitBodyType)))
            {
                bool isVisible = prefs.ShouldDisplay(ViewKey, type);

                var idx = (int)type;
                if (idx > 0) ImGui.SameLine(0, 2);

                // Style: bright when active, dim when filtered out
                if (isVisible)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.5f, 0.8f, 0.9f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.6f, 0.9f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 1f));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 0.6f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.8f));
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 0.8f));
                }

                var label = UserOrbitSettings.OrbitBodyTypeShortNames[idx];

                if (ImGui.SmallButton($"{label}##filter-{type}"))
                {
                    prefs.ToggleFilter(ViewKey, type);
                }

                ImGui.PopStyleColor(3);

                if (ImGui.IsItemHovered())
                {
                    var tip = UserOrbitSettings.OrbitBodyTypeTooltips[idx];
                    ImGui.SetTooltip(isVisible ? $"Hide {tip}" : $"Show {tip}");
                }
            }
        }
        Window.End();
    }

    private float EstimateWidth()
    {
        // Rough estimate: each small button is ~16px + 2px spacing
        return Utils.EnumEntries<UserOrbitSettings.OrbitBodyType>() * 18 + 8;
    }
}
