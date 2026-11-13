using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// One part of a stacked power bar. Served (0..1) is the share of Value_kW that is met: the met part
/// is drawn in Colour, the rest grey and hatched (amber hatching when Waiting, i.e. not a shortage).
/// </summary>
public readonly record struct PowerSegment(string Label, double Value_kW, double Served, Vector4 Colour, string Tooltip,
    bool Waiting = false);

/// <summary>
/// One compact line under a stacked bar: a colour chip, a name, a value, its share of the total, a thin
/// bar and a short trailing note. Detail lines show in the hover and, when clicked, as an expand.
/// </summary>
public sealed class PowerRow
{
    public required string Id { get; init; }
    public required Vector4 Chip { get; init; }
    public required string Name { get; init; }
    public string Value { get; init; } = "";
    public string Percent { get; init; } = "";
    /// <summary>0..1, or null for no bar.</summary>
    public float? Fill { get; init; }
    public Vector4 FillColour { get; init; } = PowerRing.FullColour;
    /// <summary>Hatch the unfilled part amber (waiting on a charge) instead of grey.</summary>
    public bool Waiting { get; init; }
    public string Trailing { get; init; } = "";
    /// <summary>Null draws the trailing text dimmed.</summary>
    public Vector4? TrailingColour { get; init; }
    public string? Tooltip { get; init; }
    public IReadOnlyList<(string Name, string Value)> Detail { get; init; } = Array.Empty<(string, string)>();
}

/// <summary>
/// The shared look of the ship and colony Power tabs: a stacked horizontal bar split by group, then
/// one compact row per group. Drawn with the window draw list; widths follow the available space so
/// nothing clips.
/// </summary>
public static class PowerBars
{
    public static readonly Vector4 UnservedColour = new(0.32f, 0.32f, 0.36f, 1f);
    static readonly Vector4 HatchColour = new(0.55f, 0.55f, 0.6f, 0.9f);
    static readonly Vector4 WaitingHatchColour = new(1f, 0.75f, 0.1f, 0.85f);
    static readonly Vector4 MarkerColour = new(1f, 1f, 1f, 0.95f);

    /// <summary>Group colours, kept clear of the green / amber / red supply colours.</summary>
    static readonly Vector4[] Palette =
    {
        new(0.35f, 0.62f, 1f, 1f),    // blue
        new(0.72f, 0.5f, 1f, 1f),     // violet
        new(0.3f, 0.82f, 0.85f, 1f),  // teal
        new(1f, 0.5f, 0.72f, 1f),     // pink
        new(0.85f, 0.76f, 0.5f, 1f),  // sand
        new(0.55f, 0.58f, 0.95f, 1f), // periwinkle
        new(0.7f, 0.72f, 0.78f, 1f),  // silver
    };

    public static Vector4 GroupColour(int index) => Palette[((index % Palette.Length) + Palette.Length) % Palette.Length];

    /// <summary>Source colours by kind: ship "Solar", "RTG", "Reactor", "Fuel cell"; colony "Fuel", "Battery".</summary>
    public static Vector4 SourceColour(string kind) => kind switch
    {
        "Solar" => new Vector4(1f, 0.84f, 0.3f, 1f),
        "RTG" => new Vector4(0.8f, 0.5f, 1f, 1f),
        "Reactor" or "Fuel" => new Vector4(0.3f, 0.78f, 1f, 1f),
        "Fuel cell" => new Vector4(0.4f, 0.9f, 0.72f, 1f),
        "Battery" => new Vector4(0.55f, 0.6f, 0.75f, 1f),
        _ => new Vector4(0.7f, 0.72f, 0.78f, 1f),
    };

    /// <summary>
    /// A full-width stacked bar. Each segment is Value_kW / scale_kW of the width. An optional white
    /// marker shows a level such as demand on the generation bar. Hover names the segment.
    /// </summary>
    public static void StackedBar(string id, IReadOnlyList<PowerSegment> segments, double scale_kW, float height = 14f,
        double? marker_kW = null, string? markerTooltip = null)
    {
        var draw = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        float width = Math.Max(60f, ImGui.GetContentRegionAvail().X);
        ImGui.InvisibleButton(id, new Vector2(width, height));
        bool hovered = ImGui.IsItemHovered();
        float mouseX = ImGui.GetIO().MousePos.X;
        var min = origin;
        var max = origin + new Vector2(width, height);
        draw.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0.12f, 0.12f, 0.14f, 1f)), 3f);

        string? tip = null;
        if (scale_kW > 0)
        {
            float[] widths = SegmentWidths(segments, scale_kW, width);
            float x = min.X;
            uint gap = ImGui.GetColorU32(new Vector4(0.06f, 0.06f, 0.08f, 1f));
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                float w = Math.Min(widths[i], max.X - x);
                if (w <= 0)
                    continue;
                float served = (float)Math.Clamp(s.Served, 0, 1);
                float xs = x + w * served;
                if (xs > x)
                    draw.AddRectFilled(new Vector2(x, min.Y), new Vector2(xs, max.Y), ImGui.GetColorU32(s.Colour));
                if (xs < x + w)
                    Hatch(draw, new Vector2(xs, min.Y), new Vector2(x + w, max.Y), s.Waiting);
                if (x > min.X)
                    draw.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), gap, 2f);
                if (hovered && mouseX >= x && mouseX < x + w)
                    tip = s.Tooltip;
                x += w;
            }
        }
        draw.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border), 3f);

        if (marker_kW is { } m && scale_kW > 0)
        {
            float mx = min.X + (float)(width * Math.Clamp(m / scale_kW, 0, 1));
            mx = Math.Clamp(mx, min.X + 1f, max.X - 1f);
            draw.AddLine(new Vector2(mx, min.Y - 3f), new Vector2(mx, max.Y + 3f), ImGui.GetColorU32(MarkerColour), 2f);
            if (hovered && Math.Abs(mouseX - mx) <= 3f && markerTooltip != null)
                tip = markerTooltip;
        }

        if (tip != null)
        {
            // Not SetTooltip: that reads printf formats, and these texts carry percent signs.
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(tip);
            ImGui.EndTooltip();
        }
    }

    /// <summary>
    /// Proportional widths, but every non-zero segment gets at least a few pixels so a small group
    /// still shows; the larger segments give up the difference.
    /// </summary>
    static float[] SegmentWidths(IReadOnlyList<PowerSegment> segments, double scale_kW, float width)
    {
        const float minW = 4f;
        var w = new float[segments.Count];
        float total = 0, small = 0, large = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].Value_kW <= 0)
                continue;
            w[i] = (float)(width * Math.Clamp(segments[i].Value_kW / scale_kW, 0, 1));
            total += w[i];
        }
        float used = Math.Min(total, width);
        for (int i = 0; i < w.Length; i++)
        {
            if (segments[i].Value_kW <= 0)
                continue;
            if (w[i] < minW) { small += minW - w[i]; w[i] = minW; }
            else large += w[i];
        }
        if (small > 0 && large > 0)
        {
            float k = Math.Max(0f, (large - small) / large);
            for (int i = 0; i < w.Length; i++)
                if (w[i] > minW)
                    w[i] = Math.Max(minW, w[i] * k);
        }
        // Keep the bar's filled length where the values put it.
        float sum = 0;
        foreach (float v in w) sum += v;
        if (sum > 0 && used > 0 && Math.Abs(sum - used) > 0.5f && sum > used)
            for (int i = 0; i < w.Length; i++)
                w[i] *= used / sum;
        return w;
    }

    /// <summary>"36%", "&lt;1%" for a small non-zero share, "-" with no total.</summary>
    public static string Percent(double part, double total)
    {
        if (total <= 0)
            return "-";
        double pct = part / total * 100;
        return pct > 0 && pct < 0.5 ? "<1%" : $"{pct:0}%";
    }

    /// <summary>Grey (or amber when waiting) diagonal hatching over the unmet part of a bar.</summary>
    static void Hatch(ImDrawListPtr draw, Vector2 min, Vector2 max, bool waiting)
    {
        draw.AddRectFilled(min, max, ImGui.GetColorU32(UnservedColour));
        uint line = ImGui.GetColorU32(waiting ? WaitingHatchColour : HatchColour);
        float h = max.Y - min.Y;
        draw.PushClipRect(min, max, true);
        for (float x = min.X - h; x < max.X; x += 6f)
            draw.AddLine(new Vector2(x, max.Y), new Vector2(x + h, min.Y), line, 1f);
        draw.PopClipRect();
    }

    static readonly HashSet<string> Expanded = new();

    /// <summary>
    /// The compact rows. Column widths fit the longest value, percent and note, and the name takes the
    /// rest. Hover a row for its detail; click it to expand the detail under it.
    /// </summary>
    public static void Rows(string id, IReadOnlyList<PowerRow> rows, float barWidth = 70f)
    {
        if (rows.Count == 0)
            return;
        float valueW = 0, pctW = 0, trailW = 0;
        foreach (var r in rows)
        {
            valueW = Math.Max(valueW, ImGui.CalcTextSize(r.Value).X);
            pctW = Math.Max(pctW, ImGui.CalcTextSize(r.Percent).X);
            trailW = Math.Max(trailW, ImGui.CalcTextSize(r.Trailing).X);
        }
        if (!ImGui.BeginTable(id, 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoPadOuterX))
            return;
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, Math.Max(valueW, 1f));
        ImGui.TableSetupColumn("Share", ImGuiTableColumnFlags.WidthFixed, Math.Max(pctW, 1f));
        ImGui.TableSetupColumn("Bar", ImGuiTableColumnFlags.WidthFixed, barWidth);
        ImGui.TableSetupColumn("Note", ImGuiTableColumnFlags.WidthFixed, Math.Max(trailW, 1f));

        float line = ImGui.GetTextLineHeight();
        foreach (var r in rows)
        {
            string key = id + "/" + r.Id;
            bool canExpand = r.Detail.Count > 0;
            bool open = canExpand && Expanded.Contains(key);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var draw = ImGui.GetWindowDrawList();
            var p = ImGui.GetCursorScreenPos();
            ImGui.SetNextItemAllowOverlap();
            if (ImGui.Selectable("##" + key, false, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, line)) && canExpand)
            {
                if (!Expanded.Remove(key))
                    Expanded.Add(key);
            }
            if (ImGui.IsItemHovered())
                RowTooltip(r, canExpand, open);

            // Expand arrow (only where there is detail), then the colour chip and the name.
            float ax = p.X + 1f, ay = p.Y + line * 0.5f;
            if (canExpand)
            {
                uint arrow = ImGui.GetColorU32(ImGuiCol.TextDisabled);
                if (open)
                    draw.AddTriangleFilled(new Vector2(ax, ay - 2.5f), new Vector2(ax + 7f, ay - 2.5f), new Vector2(ax + 3.5f, ay + 3f), arrow);
                else
                    draw.AddTriangleFilled(new Vector2(ax + 1f, ay - 4f), new Vector2(ax + 1f, ay + 4f), new Vector2(ax + 6.5f, ay), arrow);
            }
            float cx = p.X + 11f;
            draw.AddRectFilled(new Vector2(cx, ay - 5f), new Vector2(cx + 10f, ay + 5f), ImGui.GetColorU32(r.Chip), 2f);
            ImGui.SameLine(0f, 0f);
            ImGui.SetCursorScreenPos(new Vector2(cx + 16f, p.Y));
            ImGui.TextUnformatted(r.Name);

            ImGui.TableNextColumn();
            RightAligned(r.Value, valueW, null);
            ImGui.TableNextColumn();
            RightAligned(r.Percent, pctW, Styles.NeutralColor);
            ImGui.TableNextColumn();
            if (r.Fill is { } fill)
                ThinBar(fill, r.FillColour, r.Waiting, barWidth, line);
            ImGui.TableNextColumn();
            Plain(r.Trailing, r.TrailingColour ?? ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);

            if (!open)
                continue;
            foreach (var (name, value) in r.Detail)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 28f);
                Plain(name, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
                ImGui.TableNextColumn();
                RightAligned(value, valueW, Styles.NeutralColor);
            }
        }
        ImGui.EndTable();
    }

    static void RowTooltip(PowerRow r, bool canExpand, bool open)
    {
        if (r.Tooltip == null && r.Detail.Count == 0)
            return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 32f);
        if (r.Tooltip != null)
            ImGui.TextUnformatted(r.Tooltip);
        if (r.Detail.Count > 0 && ImGui.BeginTable("##tipDetail", 2, ImGuiTableFlags.SizingFixedFit))
        {
            foreach (var (name, value) in r.Detail)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(name);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(value);
            }
            ImGui.EndTable();
        }
        if (canExpand)
            Plain(open ? "Click to collapse" : "Click to list them here", ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    static void RightAligned(string text, float columnWidth, Vector4? colour)
    {
        float w = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, columnWidth - w));
        if (colour is { } c)
            Plain(text, c);
        else
            ImGui.TextUnformatted(text);
    }

    /// <summary>Coloured text without printf formatting (the rows carry percent signs).</summary>
    static void Plain(string text, Vector4 colour)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, colour);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    /// <summary>A thin satisfaction bar centred on the text line; the unmet part is hatched.</summary>
    static void ThinBar(float fill, Vector4 colour, bool waiting, float width, float line)
    {
        var draw = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();
        float h = 6f;
        var min = new Vector2(p.X, p.Y + (line - h) * 0.5f);
        var max = min + new Vector2(width, h);
        fill = Math.Clamp(fill, 0f, 1f);
        float xs = min.X + width * fill;
        if (xs < max.X)
            Hatch(draw, new Vector2(xs, min.Y), max, waiting);
        if (xs > min.X)
            draw.AddRectFilled(min, new Vector2(xs, max.Y), ImGui.GetColorU32(colour));
        ImGui.Dummy(new Vector2(width, line));
    }
}
