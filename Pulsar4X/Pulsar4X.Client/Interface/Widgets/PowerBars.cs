using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// One part of a stacked power bar. Served (0..1) is the share of Value_kW that is met: the met part
/// is drawn in Colour, the rest grey and hatched (amber hatching when Waiting, i.e. not a shortage).
/// ShowLabel writes Label inside the segment when it fits (the battery slices, which have no row).
/// </summary>
public readonly record struct PowerSegment(string Label, double Value_kW, double Served, Vector4 Colour, string Tooltip,
    bool Waiting = false, bool ShowLabel = false);

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
                if (s.ShowLabel)
                {
                    var ts = ImGui.CalcTextSize(s.Label);
                    if (ts.X + 8f <= w)
                        draw.AddText(new Vector2(x + (w - ts.X) * 0.5f, min.Y + (height - ts.Y) * 0.5f),
                            ImGui.GetColorU32(new Vector4(0.05f, 0.07f, 0.1f, 1f)), s.Label);
                }
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

    /// <summary>Charging (blue, right of zero) and discharging (amber, left of zero) on the Storage bar.</summary>
    public static readonly Vector4 ChargeColour = new(0.35f, 0.75f, 1f, 1f);
    public static readonly Vector4 DischargeColour = PowerRing.PartialColour;

    /// <summary>
    /// The Storage section shared by the ship and colony Power tabs: a two-way bar centred on zero
    /// (blue to the right while charging, amber to the left while discharging, each half scaled to its
    /// max rate), a thin charge-level gauge under it, and the stored % with "full in" / "empty in"
    /// beside it. Capacity and rates show on hover. Greyed when there is no storage.
    /// </summary>
    /// <param name="battery_kW">+charge / -discharge.</param>
    /// <param name="detail">Optional lines for the hover, e.g. the colony's battery designs.</param>
    public static void Storage(string id, double battery_kW, double storedFraction, double capacity_kJ,
        double maxCharge_kW, double maxDischarge_kW, double? timeToFullOrEmpty_s,
        IReadOnlyList<(string Name, string Value)>? detail = null)
    {
        ImGui.SeparatorText("Storage");
        const double eps = 1e-6;
        if (capacity_kJ <= 0)
        {
            ImGui.TextDisabled("No batteries");
            return;
        }

        double level = Math.Clamp(storedFraction, 0, 1);
        bool charging = battery_kW > eps, discharging = battery_kW < -eps;
        string when = timeToFullOrEmpty_s is { } t && (charging || discharging) ? ShortDuration(t) : "";
        string note = charging ? (when != "" ? "full in " + when : "charging")
            : discharging ? (when != "" ? "empty in " + when : "discharging")
            : level >= 0.999 ? "full" : level <= 0.001 ? "empty" : "idle";
        string pct = $"{level * 100:0}%";
        string flow = charging ? "+" + Stringify.Power(battery_kW) : discharging ? "-" + Stringify.Power(-battery_kW) : "0 kW";

        // Text block on the right: flow, stored %, then the note. Fixed widths so the bar does not jump.
        float gap = ImGui.GetStyle().ItemSpacing.X * 2f;
        float flowW = Math.Max(ImGui.CalcTextSize(flow).X, ImGui.CalcTextSize("-000.0 kW").X);
        float pctW = ImGui.CalcTextSize("100%").X;
        float noteW = Math.Max(ImGui.CalcTextSize(note).X, ImGui.CalcTextSize("empty in 00 d").X);
        float avail = ImGui.GetContentRegionAvail().X;
        float barW = Math.Max(80f, avail - flowW - pctW - noteW - gap * 3f);
        float line = ImGui.GetTextLineHeight();
        const float barH = 14f, gaugeH = 3f;

        var draw = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton(id, new Vector2(barW, Math.Max(line, barH + gaugeH + 2f)));
        bool hovered = ImGui.IsItemHovered();
        var min = origin;
        var max = origin + new Vector2(barW, barH);
        float mid = min.X + barW * 0.5f;
        // Faint halves so the direction reads even when idle.
        draw.AddRectFilled(min, new Vector2(mid, max.Y), ImGui.GetColorU32(new Vector4(0.16f, 0.13f, 0.08f, 1f)), 3f);
        draw.AddRectFilled(new Vector2(mid, min.Y), max, ImGui.GetColorU32(new Vector4(0.08f, 0.12f, 0.17f, 1f)), 3f);
        float half = barW * 0.5f - 1f;
        if (charging)
        {
            float f = maxCharge_kW > eps ? (float)Math.Clamp(battery_kW / maxCharge_kW, 0, 1) : 1f;
            draw.AddRectFilled(new Vector2(mid, min.Y), new Vector2(mid + Math.Max(2f, half * f), max.Y), ImGui.GetColorU32(ChargeColour));
        }
        else if (discharging)
        {
            float f = maxDischarge_kW > eps ? (float)Math.Clamp(-battery_kW / maxDischarge_kW, 0, 1) : 1f;
            draw.AddRectFilled(new Vector2(mid - Math.Max(2f, half * f), min.Y), new Vector2(mid, max.Y), ImGui.GetColorU32(DischargeColour));
        }
        draw.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border), 3f);
        draw.AddLine(new Vector2(mid, min.Y - 3f), new Vector2(mid, max.Y + 3f), ImGui.GetColorU32(MarkerColour), 2f);
        // Small end labels inside the halves.
        uint dim = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        float small = ImGui.GetFontSize();
        if (!discharging)
            draw.AddText(new Vector2(min.X + 4f, min.Y + (barH - small) * 0.5f), dim, "discharge");
        if (!charging)
        {
            float cw = ImGui.CalcTextSize("charge").X;
            draw.AddText(new Vector2(max.X - cw - 4f, min.Y + (barH - small) * 0.5f), dim, "charge");
        }
        // Charge level gauge under the bar.
        var gMin = new Vector2(min.X, max.Y + 2f);
        draw.AddRectFilled(gMin, new Vector2(max.X, gMin.Y + gaugeH), ImGui.GetColorU32(UnservedColour));
        if (level > 0)
            draw.AddRectFilled(gMin, new Vector2(min.X + barW * (float)level, gMin.Y + gaugeH), ImGui.GetColorU32(SourceColour("Battery") * new Vector4(1.2f, 1.2f, 1.2f, 1f)));

        // Text beside the bar, on the bar's line.
        ImGui.SameLine(0f, gap);
        ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().X, origin.Y + (barH - line) * 0.5f));
        RightAligned(flow, flowW, charging ? ChargeColour : discharging ? DischargeColour : (Vector4?)null);
        ImGui.SameLine(0f, gap);
        RightAligned(pct, pctW, Styles.NeutralColor);
        ImGui.SameLine(0f, gap);
        Plain(note, charging ? ChargeColour : discharging ? DischargeColour : ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
        bool textHovered = ImGui.IsMouseHoveringRect(new Vector2(origin.X + barW, origin.Y - 2f),
            new Vector2(origin.X + avail, origin.Y + barH + gaugeH + 4f));

        if (hovered || textHovered)
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 32f);
            ImGui.TextUnformatted($"Batteries: {Stringify.Energy(capacity_kJ * level)} of {Stringify.Energy(capacity_kJ)} ({level * 100:0.#}%)");
            ImGui.TextUnformatted(charging ? $"Charging at {Stringify.Power(battery_kW)}"
                : discharging ? $"Discharging at {Stringify.Power(-battery_kW)}" : "Idle: not charging or discharging");
            if (when != "" && timeToFullOrEmpty_s is { } s2)
                ImGui.TextUnformatted($"{(charging ? "Full" : "Empty")} in {LongDuration(s2)}");
            ImGui.TextUnformatted($"Max charge {Stringify.Power(maxCharge_kW)}, max discharge {Stringify.Power(maxDischarge_kW)}");
            if (detail is { Count: > 0 } && ImGui.BeginTable("##storageTipDetail", 2, ImGuiTableFlags.SizingFixedFit))
            {
                foreach (var (name, value) in detail)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(name);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(value);
                }
                ImGui.EndTable();
            }
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    /// <summary>The generation bar's battery slice while it discharges, or null.</summary>
    public static PowerSegment? DischargeSegment(double battery_kW)
        => battery_kW < -1e-6
            ? new PowerSegment("Batteries", -battery_kW, 1, DischargeColour, $"Batteries: discharging {Stringify.Power(-battery_kW)}", ShowLabel: true)
            : null;

    /// <summary>The demand bar's battery slice while it charges, or null.</summary>
    public static PowerSegment? ChargeSegment(double battery_kW)
        => battery_kW > 1e-6
            ? new PowerSegment("To batteries", battery_kW, 1, ChargeColour, $"Charging the batteries: {Stringify.Power(battery_kW)}", ShowLabel: true)
            : null;

    /// <summary>"15,210 y", "40 d", "5 h", "12 min".</summary>
    public static string ShortDuration(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) return "-";
        double days = seconds / 86400.0;
        if (days >= 730) return $"{days / 365.25:N0} y";
        if (days >= 2) return $"{days:N0} d";
        double hours = seconds / 3600.0;
        return hours >= 1 ? $"{hours:N0} h" : $"{Math.Max(1, seconds / 60.0):N0} min";
    }

    /// <summary>"1.5 years", "40 days", "5.0 hours", "12 min".</summary>
    public static string LongDuration(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) return "-";
        double days = seconds / 86400.0;
        if (days >= 730) return $"{days / 365.25:N1} years";
        if (days >= 2) return $"{days:N0} days";
        double hours = seconds / 3600.0;
        return hours >= 1 ? $"{hours:N1} hours" : $"{Math.Max(1, seconds / 60.0):N0} min";
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
