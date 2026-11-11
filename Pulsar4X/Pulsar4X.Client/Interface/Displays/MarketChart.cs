using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.Client
{
    public sealed class MarketLineSeries
    {
        public string Name { get; init; } = "";
        public Vector4 Color { get; init; }
        public IReadOnlyList<decimal?> Values { get; init; } = Array.Empty<decimal?>();
        public IReadOnlyList<bool> QuoteMid { get; init; } = Array.Empty<bool>();
    }

    /// <summary>Candle and line charts drawn with the ImGui draw list.</summary>
    public static class MarketChart
    {
        internal static readonly Vector4 Up = new(0.298f, 0.686f, 0.416f, 1f);
        internal static readonly Vector4 Down = new(0.769f, 0.357f, 0.357f, 1f);

        static readonly Vector4 Background = new(0.078f, 0.078f, 0.078f, 1f);
        static readonly Vector4 Grid = new(0.165f, 0.165f, 0.165f, 1f);
        static readonly Vector4 Axis = new(0.541f, 0.541f, 0.541f, 1f);
        static readonly Vector4 Guide = new(0.75f, 0.75f, 0.75f, 0.45f);

        public static void Draw(
            string id,
            IReadOnlyList<MarketChartBucket> buckets,
            bool candles,
            IReadOnlyList<MarketLineSeries> lines,
            bool indexScale,
            float height)
        {
            if (buckets == null || buckets.Count == 0)
                return;

            float width = ImGui.GetContentRegionAvail().X;
            if (width < 48f)
                width = 48f;
            if (height < 48f)
                height = 48f;

            ImGui.InvisibleButton("##market-plot-" + id, new Vector2(width, height));
            bool hovered = ImGui.IsItemHovered();
            Vector2 origin = ImGui.GetItemRectMin();
            Vector2 size = ImGui.GetItemRectSize();
            var draw = ImGui.GetWindowDrawList();

            uint bg = ImGui.ColorConvertFloat4ToU32(Background);
            draw.AddRectFilled(origin, origin + size, bg, 3f);

            bool anyCandle = false;
            decimal min = 0;
            decimal max = 0;
            bool anyValue = false;
            if (candles)
            {
                for (int i = 0; i < buckets.Count; i++)
                {
                    if (!buckets[i].HasCandle)
                        continue;
                    anyCandle = true;
                    Consider(buckets[i].Low, ref min, ref max, ref anyValue);
                    Consider(buckets[i].High, ref min, ref max, ref anyValue);
                }
            }
            else if (lines != null)
            {
                for (int s = 0; s < lines.Count; s++)
                {
                    var values = lines[s].Values;
                    if (values == null)
                        continue;
                    for (int i = 0; i < values.Count; i++)
                    {
                        if (values[i] is decimal value)
                            Consider(value, ref min, ref max, ref anyValue);
                    }
                }
            }

            float padR = 10f;
            float padT = 8f;
            float padB = 22f;
            float volumeH = candles && anyCandle ? size.Y * 0.20f : 0f;
            float volumeGap = volumeH > 0f ? 6f : 0f;

            if (!anyValue)
            {
                string note = candles ? "No trades in this range." : "No trades recorded yet.";
                var noteSize = ImGui.CalcTextSize(note);
                draw.AddText(
                    new Vector2(origin.X + (size.X - noteSize.X) * 0.5f, origin.Y + (size.Y - noteSize.Y) * 0.5f),
                    ImGui.ColorConvertFloat4ToU32(Axis),
                    note);
                return;
            }

            decimal span = max - min;
            decimal pad = span * 0.08m;
            if (pad <= 0)
                pad = max == 0 ? 1m : Math.Abs(max) * 0.08m;
            decimal ymin = min - pad;
            decimal ymax = max + pad;
            if (min >= 0 && ymin < 0)
                ymin = 0;
            if (ymax <= ymin)
                ymax = ymin + 1;

            var ticks = NiceTicks(ymin, ymax, 4);
            float padL = 36f;
            for (int i = 0; i < ticks.Count; i++)
            {
                float labelW = ImGui.CalcTextSize(FormatTick(ticks[i], indexScale)).X;
                if (labelW + 10f > padL)
                    padL = labelW + 10f;
            }
            if (padL > size.X * 0.4f)
                padL = size.X * 0.4f;

            float plotLeft = origin.X + padL;
            float plotRight = origin.X + size.X - padR;
            float plotTop = origin.Y + padT;
            float plotBottom = origin.Y + size.Y - padB - volumeH - volumeGap;
            float plotW = plotRight - plotLeft;
            float plotH = plotBottom - plotTop;
            if (plotW < 8f || plotH < 8f)
                return;

            uint grid = ImGui.ColorConvertFloat4ToU32(Grid);
            uint axis = ImGui.ColorConvertFloat4ToU32(Axis);
            for (int i = 0; i < ticks.Count; i++)
            {
                float y = YAt(ticks[i], ymin, ymax, plotTop, plotH);
                if (y < plotTop - 1f || y > plotBottom + 1f)
                    continue;
                draw.AddLine(new Vector2(plotLeft, y), new Vector2(plotRight, y), grid, 1f);
                string label = FormatTick(ticks[i], indexScale);
                var labelSize = ImGui.CalcTextSize(label);
                draw.AddText(new Vector2(plotLeft - labelSize.X - 6f, y - labelSize.Y * 0.5f), axis, label);
            }

            float slot = plotW / buckets.Count;
            bool longLabels = LongLabels(buckets);

            draw.PushClipRect(new Vector2(plotLeft, plotTop), new Vector2(plotRight, origin.Y + size.Y - padB), true);
            if (candles)
            {
                float bodyW = slot * 0.72f;
                if (bodyW < 1f)
                    bodyW = 1f;
                if (bodyW > slot)
                    bodyW = slot;
                float volumeTop = plotBottom + volumeGap;
                float volumeBottom = origin.Y + size.Y - padB;
                long maxVolume = 0;
                for (int i = 0; i < buckets.Count; i++)
                {
                    if (buckets[i].Volume > maxVolume)
                        maxVolume = buckets[i].Volume;
                }

                for (int i = 0; i < buckets.Count; i++)
                {
                    var bucket = buckets[i];
                    if (!bucket.HasCandle)
                        continue;
                    float x = plotLeft + (i + 0.5f) * slot;
                    bool up = bucket.Close >= bucket.Open;
                    uint color = ImGui.ColorConvertFloat4ToU32(up ? Up : Down);
                    float yHigh = YAt(bucket.High, ymin, ymax, plotTop, plotH);
                    float yLow = YAt(bucket.Low, ymin, ymax, plotTop, plotH);
                    draw.AddLine(new Vector2(x, yHigh), new Vector2(x, yLow), color, 1.2f);
                    float yOpen = YAt(bucket.Open, ymin, ymax, plotTop, plotH);
                    float yClose = YAt(bucket.Close, ymin, ymax, plotTop, plotH);
                    float top = yClose < yOpen ? yClose : yOpen;
                    float bodyH = Math.Abs(yOpen - yClose);
                    if (bodyH < 2f)
                        bodyH = 2f;
                    draw.AddRectFilled(new Vector2(x - bodyW * 0.5f, top), new Vector2(x + bodyW * 0.5f, top + bodyH), color);

                    if (maxVolume > 0 && volumeH > 0f)
                    {
                        float barH = (float)bucket.Volume / maxVolume * (volumeBottom - volumeTop);
                        if (barH < 1f)
                            barH = 1f;
                        var volumeColor = up ? Up : Down;
                        volumeColor.W = 0.35f;
                        draw.AddRectFilled(
                            new Vector2(x - bodyW * 0.5f, volumeBottom - barH),
                            new Vector2(x + bodyW * 0.5f, volumeBottom),
                            ImGui.ColorConvertFloat4ToU32(volumeColor));
                    }
                }
            }
            else if (lines != null)
            {
                for (int s = 0; s < lines.Count; s++)
                {
                    var series = lines[s];
                    if (series.Values == null)
                        continue;
                    uint color = ImGui.ColorConvertFloat4ToU32(series.Color);
                    int count = series.Values.Count;
                    if (count > buckets.Count)
                        count = buckets.Count;
                    bool havePrev = false;
                    Vector2 prev = default;
                    Vector2 last = default;
                    bool haveLast = false;
                    for (int i = 0; i < count; i++)
                    {
                        if (series.Values[i] is not decimal value)
                        {
                            havePrev = false;
                            continue;
                        }
                        float x = plotLeft + (i + 0.5f) * slot;
                        float y = YAt(value, ymin, ymax, plotTop, plotH);
                        var point = new Vector2(x, y);
                        if (havePrev)
                            draw.AddLine(prev, point, color, 2f);
                        prev = point;
                        havePrev = true;
                        last = point;
                        haveLast = true;
                    }
                    if (haveLast)
                        draw.AddCircleFilled(last, 3f, color);
                }
            }
            draw.PopClipRect();

            DrawAxisLabels(draw, buckets, plotLeft, slot, origin.Y + size.Y - 4f, plotRight, axis, longLabels);

            if (!hovered)
                return;
            var mouse = ImGui.GetMousePos();
            int hover = MarketChartData.HoverIndex(buckets, mouse.X - plotLeft, slot, candles ? 8f : 0f);
            if (hover < 0)
                return;
            float guideX = plotLeft + (hover + 0.5f) * slot;
            draw.AddLine(
                new Vector2(guideX, plotTop),
                new Vector2(guideX, origin.Y + size.Y - padB),
                ImGui.ColorConvertFloat4ToU32(Guide),
                1f);
            DrawTooltip(buckets[hover], hover, candles, lines ?? Array.Empty<MarketLineSeries>(), indexScale);
        }

        static void DrawAxisLabels(
            ImDrawListPtr draw,
            IReadOnlyList<MarketChartBucket> buckets,
            float plotLeft,
            float slot,
            float baseline,
            float plotRight,
            uint color,
            bool longLabels)
        {
            float lastRight = float.NegativeInfinity;
            for (int i = 0; i < buckets.Count; i++)
            {
                string text = FormatAxisDate(buckets[i].Start, longLabels);
                var textSize = ImGui.CalcTextSize(text);
                float x = plotLeft + (i + 0.5f) * slot - textSize.X * 0.5f;
                if (x < plotLeft)
                    x = plotLeft;
                if (x + textSize.X > plotRight)
                    x = plotRight - textSize.X;
                if (x < plotLeft)
                    continue;
                if (x < lastRight + 8f)
                    continue;
                draw.AddText(new Vector2(x, baseline - textSize.Y), color, text);
                lastRight = x + textSize.X;
            }
        }

        static void DrawTooltip(
            MarketChartBucket bucket, int index, bool candles, IReadOnlyList<MarketLineSeries> lines, bool indexScale)
        {
            ImGui.BeginTooltip();
            if (bucket.Start.Date == bucket.End.Date)
                ImGui.TextUnformatted(bucket.Start.ToString("d MMM yyyy", CultureInfo.InvariantCulture));
            else
                ImGui.TextUnformatted(
                    bucket.Start.ToString("d MMM yyyy", CultureInfo.InvariantCulture)
                    + " – "
                    + bucket.End.ToString("d MMM yyyy", CultureInfo.InvariantCulture));

            if (candles)
            {
                string volume = bucket.Volume.ToString("N0", CultureInfo.InvariantCulture);
                if (bucket.HasCandle && bucket.Open == bucket.High && bucket.High == bucket.Low && bucket.Low == bucket.Close)
                {
                    ImGui.TextUnformatted("Traded " + volume + " @ " + Money(bucket.Close));
                }
                else if (bucket.HasCandle)
                {
                    ImGui.TextUnformatted("O " + Money(bucket.Open) + "   H " + Money(bucket.High));
                    ImGui.TextUnformatted("L " + Money(bucket.Low) + "   C " + Money(bucket.Close));
                    ImGui.TextUnformatted("Vol " + volume);
                }
                else
                {
                    ImGui.TextUnformatted("No trade");
                }
                if (bucket.HasQuote)
                    ImGui.TextDisabled("Bid " + Money(bucket.Bid) + "   Ask " + Money(bucket.Ask));
                if (bucket.QuoteMid)
                    ImGui.TextUnformatted("quote mid");
            }
            else if (lines != null)
            {
                bool quoteMid = false;
                for (int s = 0; s < lines.Count; s++)
                {
                    var series = lines[s];
                    if (series.Values == null || index >= series.Values.Count)
                        continue;
                    if (series.Values[index] is not decimal value)
                        continue;
                    string shown = indexScale
                        ? value.ToString("0.0", CultureInfo.InvariantCulture)
                        : Money(value);
                    ImGui.TextColored(series.Color, series.Name + "  " + shown);
                    if (series.QuoteMid != null && index < series.QuoteMid.Count && series.QuoteMid[index])
                        quoteMid = true;
                }
                if (quoteMid)
                    ImGui.TextUnformatted("quote mid");
            }
            ImGui.EndTooltip();
        }

        static bool LongLabels(IReadOnlyList<MarketChartBucket> buckets)
        {
            if (buckets.Count == 0)
                return false;
            if ((buckets[0].End - buckets[0].Start).Days >= 27)
                return true;
            return (buckets[buckets.Count - 1].End - buckets[0].Start).Days >= 300;
        }

        static string FormatAxisDate(DateTime day, bool longLabels)
        {
            if (longLabels)
                return day.ToString("MMM yyyy", CultureInfo.InvariantCulture);
            return day.ToString("d MMM", CultureInfo.InvariantCulture);
        }

        static string FormatTick(decimal value, bool indexScale)
        {
            if (indexScale || (Math.Abs(value) >= 10m && Math.Abs(value - decimal.Round(value)) < 0.05m))
                return decimal.Round(value).ToString(CultureInfo.InvariantCulture);
            if (Math.Abs(value) >= 10m)
                return value.ToString("0.0", CultureInfo.InvariantCulture);
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        static string Money(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        static float YAt(decimal value, decimal ymin, decimal ymax, float plotTop, float plotH)
        {
            double t = (double)(value - ymin) / (double)(ymax - ymin);
            if (t < 0)
                t = 0;
            if (t > 1)
                t = 1;
            return plotTop + (float)(1.0 - t) * plotH;
        }

        static void Consider(decimal value, ref decimal min, ref decimal max, ref bool any)
        {
            if (!any)
            {
                min = value;
                max = value;
                any = true;
                return;
            }
            if (value < min)
                min = value;
            if (value > max)
                max = value;
        }

        static List<decimal> NiceTicks(decimal ymin, decimal ymax, int count)
        {
            var ticks = new List<decimal>();
            double span = (double)(ymax - ymin);
            if (span <= 0)
                span = 1;
            if (count < 2)
                count = 2;
            double raw = span / count;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            if (mag <= 0 || double.IsNaN(mag) || double.IsInfinity(mag))
                mag = 1;
            double step = 10 * mag;
            double[] candidates = { 1d, 2d, 2.5d, 5d, 10d };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] * mag >= raw * 0.7)
                {
                    step = candidates[i] * mag;
                    break;
                }
            }
            if (step <= 0)
                step = 1;

            double start = Math.Floor((double)ymin / step) * step;
            double v = start;
            for (int i = 0; i < 20; i++)
            {
                if (v >= (double)ymin - step * 0.05 && v <= (double)ymax + step * 0.05)
                    ticks.Add((decimal)v);
                v = Math.Round(v + step, 10);
                if (v > (double)ymax + step)
                    break;
            }
            if (ticks.Count > 5)
            {
                var trimmed = new List<decimal>();
                trimmed.Add(ticks[0]);
                int inner = ticks.Count - 2;
                int keep = 3;
                for (int i = 1; i <= keep; i++)
                {
                    int index = (int)Math.Round(i * (inner / (double)(keep + 1)));
                    if (index < 1)
                        index = 1;
                    if (index > ticks.Count - 2)
                        index = ticks.Count - 2;
                    if (trimmed[trimmed.Count - 1] != ticks[index])
                        trimmed.Add(ticks[index]);
                }
                if (trimmed[trimmed.Count - 1] != ticks[ticks.Count - 1])
                    trimmed.Add(ticks[ticks.Count - 1]);
                return trimmed;
            }
            if (ticks.Count >= 2)
                return ticks;
            ticks.Clear();
            ticks.Add(ymin);
            ticks.Add((ymin + ymax) / 2m);
            ticks.Add(ymax);
            return ticks;
        }
    }
}
