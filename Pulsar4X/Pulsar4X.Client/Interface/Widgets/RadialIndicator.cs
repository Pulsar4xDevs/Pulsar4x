using System;
using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// The entity card's circular gauge: a dim full ring with a coloured arc from 12 o'clock,
/// text in the middle, a label underneath and a hover tooltip.
/// </summary>
public static class RadialIndicator
{
    public static void Draw(
        ImDrawListPtr drawList, Vector2 center, float radius, float ringThickness,
        float value, string label, string centerText, bool isPlaceholder,
        Vector4 accentColor, Vector4 ringColor, string? extraTooltip = null)
    {
        var dimColor = new Vector4(
            accentColor.X * 0.3f, accentColor.Y * 0.3f, accentColor.Z * 0.3f, 0.4f);
        var healthColor = isPlaceholder ? Styles.DescriptiveColor : ringColor;
        var textColor = isPlaceholder ? Styles.DescriptiveColor : healthColor;

        drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(dimColor), 32, ringThickness);

        if (!isPlaceholder && value > 0f)
        {
            float startAngle = -MathF.PI / 2f;
            float endAngle = startAngle + value * 2f * MathF.PI;
            drawList.PathArcTo(center, radius, startAngle, endAngle, 32);
            drawList.PathStroke(ImGui.ColorConvertFloat4ToU32(healthColor), ImDrawFlags.None, ringThickness);
        }

        var centerTextSize = ImGui.CalcTextSize(centerText);
        drawList.AddText(
            new Vector2(center.X - centerTextSize.X * 0.5f, center.Y - centerTextSize.Y * 0.5f),
            ImGui.ColorConvertFloat4ToU32(textColor),
            centerText);

        var labelSize = ImGui.CalcTextSize(label);
        drawList.AddText(
            new Vector2(center.X - labelSize.X * 0.5f, center.Y + radius + ringThickness * 0.5f + 2f),
            ImGui.ColorConvertFloat4ToU32(Styles.DescriptiveColor),
            label);

        var min = new Vector2(center.X - radius - ringThickness, center.Y - radius - ringThickness);
        var max = new Vector2(center.X + radius + ringThickness, center.Y + radius + ringThickness);
        if (ImGui.IsMouseHoveringRect(min, max))
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(label + ": " + (isPlaceholder ? "N/A" : (value * 100f).ToString("0") + "%"));
            if (extraTooltip != null)
                ImGui.TextUnformatted(extraTooltip);
            ImGui.EndTooltip();
        }
    }
}
