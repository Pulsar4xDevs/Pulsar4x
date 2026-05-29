using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// Horizontal pip-and-bar progress indicator: O====O====O====O with labels beneath.
/// Each pip fills as its stage progresses; the bar leading into a pip fills with the prior stage's progress.
/// </summary>
public static class SurveyProgressBar
{
    public readonly struct Stage
    {
        public readonly string Label;
        public readonly float Fill;
        public readonly string? Tooltip;

        public Stage(string label, float fill, string? tooltip = null)
        {
            Label = label;
            Fill = Math.Clamp(fill, 0f, 1f);
            Tooltip = tooltip;
        }
    }

    private const float PipRadius = 7f;
    private const float PipThickness = 1.5f;
    private const float BarHeight = 4f;
    private const float LabelPadding = 4f;
    private const float EdgePadding = 4f;

    public static void Draw(string id, IReadOnlyList<Stage> stages, Vector4? accentColor = null)
    {
        if (stages == null || stages.Count == 0)
            return;

        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        float available = ImGui.GetContentRegionAvail().X;

        Vector4 accent = accentColor ?? new Vector4(0.35f, 0.80f, 0.45f, 1.0f);
        uint fillColor = ImGui.ColorConvertFloat4ToU32(accent);
        uint emptyColor = ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X * 0.25f, accent.Y * 0.25f, accent.Z * 0.25f, 0.55f));
        uint outlineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X * 0.8f, accent.Y * 0.8f, accent.Z * 0.8f, 0.75f));
        uint labelColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.75f, 0.75f, 0.80f, 0.95f));

        int pipCount = stages.Count;

        // Measure labels so we can inset the first/last pip enough that no label overflows.
        var labelSizes = new Vector2[pipCount];
        float maxLabelHeight = 0f;
        for (int i = 0; i < pipCount; i++)
        {
            labelSizes[i] = ImGui.CalcTextSize(stages[i].Label);
            if (labelSizes[i].Y > maxLabelHeight) maxLabelHeight = labelSizes[i].Y;
        }

        float pipCenterY = origin.Y + PipRadius + 2f;
        var pipCenters = new Vector2[pipCount];

        if (pipCount == 1)
        {
            pipCenters[0] = new Vector2(origin.X + available * 0.5f, pipCenterY);
        }
        else
        {
            float firstInset = Math.Max(PipRadius, labelSizes[0].X * 0.5f) + EdgePadding;
            float lastInset = Math.Max(PipRadius, labelSizes[pipCount - 1].X * 0.5f) + EdgePadding;
            float firstCenterX = origin.X + firstInset;
            float lastCenterX = origin.X + available - lastInset;
            if (lastCenterX < firstCenterX) lastCenterX = firstCenterX;

            float step = (lastCenterX - firstCenterX) / (pipCount - 1);
            for (int i = 0; i < pipCount; i++)
                pipCenters[i] = new Vector2(firstCenterX + step * i, pipCenterY);
        }

        for (int i = 0; i < pipCount - 1; i++)
        {
            var left = new Vector2(pipCenters[i].X + PipRadius, pipCenterY - BarHeight * 0.5f);
            var right = new Vector2(pipCenters[i + 1].X - PipRadius, pipCenterY + BarHeight * 0.5f);

            drawList.AddRectFilled(left, right, emptyColor);

            // Bar leads INTO the next stage, so its fill tracks that stage's progress.
            float fill = stages[i + 1].Fill;
            if (fill > 0f)
            {
                float fillEndX = left.X + (right.X - left.X) * fill;
                drawList.AddRectFilled(left, new Vector2(fillEndX, right.Y), fillColor);
            }
        }

        for (int i = 0; i < pipCount; i++)
        {
            drawList.AddCircleFilled(pipCenters[i], PipRadius - 1f, emptyColor);

            if (stages[i].Fill >= 1.0f)
                drawList.AddCircleFilled(pipCenters[i], PipRadius - 1f, fillColor);

            drawList.AddCircle(pipCenters[i], PipRadius, outlineColor, 24, PipThickness);
        }

        float labelY = pipCenterY + PipRadius + LabelPadding;
        for (int i = 0; i < pipCount; i++)
        {
            var labelPos = new Vector2(pipCenters[i].X - labelSizes[i].X * 0.5f, labelY);
            drawList.AddText(labelPos, labelColor, stages[i].Label);

            if (!string.IsNullOrEmpty(stages[i].Tooltip))
            {
                float halfWidth = Math.Max(PipRadius + PipThickness, labelSizes[i].X * 0.5f);
                var hitMin = new Vector2(pipCenters[i].X - halfWidth, pipCenterY - PipRadius - PipThickness);
                var hitMax = new Vector2(pipCenters[i].X + halfWidth, labelY + labelSizes[i].Y);
                if (ImGui.IsMouseHoveringRect(hitMin, hitMax))
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(stages[i].Tooltip);
                    ImGui.EndTooltip();
                }
            }
        }

        float totalHeight = (pipCenterY - origin.Y) + PipRadius + LabelPadding + maxLabelHeight + 4f;
        ImGui.Dummy(new Vector2(available, totalHeight));
    }
}
