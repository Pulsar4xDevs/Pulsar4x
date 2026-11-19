using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace Pulsar4X.Client;

/// <summary>Which column a Sankey node sits in.</summary>
public enum LedgerSankeyNodeKind
{
    Income,
    Expense,
    Treasury,
    NetSaved,
    Shortfall,
}

/// <summary>A flow band between two nodes.</summary>
public enum LedgerSankeyBandKind
{
    Income,
    Expense,
    NetSaved,
    Shortfall,
}

/// <summary>A category (or the Other rollup) after the 3% grouping.</summary>
public sealed record LedgerSankeySlice(
    string Label,
    decimal Amount,
    IReadOnlyList<(string Label, decimal Amount)> Parts);

/// <summary>A selectable year or month already present in <see cref="LedgerTree"/>.</summary>
public sealed record LedgerSankeyPeriod(
    string Id,
    string Label,
    bool IsYear,
    int Year,
    int Month,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    IReadOnlyList<LedgerCategoryTotal> IncomeCategories,
    IReadOnlyList<LedgerCategoryTotal> ExpenseCategories);

/// <summary>Which side of its bar a node's label sits on.</summary>
public enum LedgerSankeyLabelSide
{
    Left,
    Right,
    Above,
}

/// <summary>A labelled bar in the diagram, in the same coordinate space as the content region.</summary>
public sealed record LedgerSankeyNode(
    string Id,
    string Label,
    decimal Amount,
    LedgerSankeyNodeKind Kind,
    float X,
    float Y,
    float Width,
    float Height)
{
    /// <summary>Left edge of the label box (name and amount, two lines).</summary>
    public float LabelX { get; init; }

    /// <summary>Top edge of the label box.</summary>
    public float LabelY { get; init; }

    /// <summary>Width reserved for the label; text is ellipsized only if it would exceed this.</summary>
    public float LabelMaxWidth { get; init; }

    /// <summary>Line height used to place the amount under the name.</summary>
    public float LabelLineHeight { get; init; }

    /// <summary>True when the two lines should be right-aligned in the label box (left-column nodes).</summary>
    public bool LabelAlignRight { get; init; }

    public LedgerSankeyLabelSide LabelSide { get; init; }

    public string AmountText { get; init; } = "";

    /// <summary>Name to draw; equal to <see cref="Label"/> unless the window was too narrow.</summary>
    public string FittedLabel { get; init; } = "";

    /// <summary>Amount to draw; equal to <see cref="AmountText"/> unless the window was too narrow.</summary>
    public string FittedAmount { get; init; } = "";
}

/// <summary>
/// A curved band from one node edge to another. <see cref="Share"/> is the amount
/// as a fraction of the flow through the treasury (the larger of income and expenses).
/// <see cref="Parts"/> is populated only for an Other rollup.
/// </summary>
public sealed class LedgerSankeyBand
{
    public LedgerSankeyBand(
        string id,
        string label,
        decimal amount,
        decimal share,
        LedgerSankeyBandKind kind,
        IReadOnlyList<(string Label, decimal Amount)> parts,
        float leftX,
        float leftY0,
        float leftY1,
        float rightX,
        float rightY0,
        float rightY1)
    {
        Id = id;
        Label = label;
        Amount = amount;
        Share = share;
        Kind = kind;
        Parts = parts;
        LeftX = leftX;
        LeftY0 = leftY0;
        LeftY1 = leftY1;
        RightX = rightX;
        RightY0 = rightY0;
        RightY1 = rightY1;
    }

    public string Id { get; }
    public string Label { get; }
    public decimal Amount { get; }
    public decimal Share { get; }
    public LedgerSankeyBandKind Kind { get; }
    public IReadOnlyList<(string Label, decimal Amount)> Parts { get; }
    public float LeftX { get; }
    public float LeftY0 { get; }
    public float LeftY1 { get; }
    public float RightX { get; }
    public float RightY0 { get; }
    public float RightY1 { get; }
}

/// <summary>The complete geometry of a Flow-tab Sankey, with no ImGui types.</summary>
public sealed class LedgerSankeyDiagram
{
    public LedgerSankeyDiagram(
        IReadOnlyList<LedgerSankeyNode> nodes,
        IReadOnlyList<LedgerSankeyBand> bands,
        string? emptyMessage)
    {
        Nodes = nodes;
        Bands = bands;
        EmptyMessage = emptyMessage;
    }

    public IReadOnlyList<LedgerSankeyNode> Nodes { get; }
    public IReadOnlyList<LedgerSankeyBand> Bands { get; }
    public string? EmptyMessage { get; }
    public bool IsEmpty => EmptyMessage != null;

    /// <summary>The first band whose sampled bezier strip contains <paramref name="x"/>, <paramref name="y"/>.</summary>
    public LedgerSankeyBand? HitTest(float x, float y)
    {
        foreach (var band in Bands)
        {
            if (LedgerSankeyLayout.BandContains(band, x, y))
                return band;
        }
        return null;
    }
}

/// <summary>
/// Pure layout for the ledger Sankey: grouping, net/shortfall bands, node heights,
/// band y-ranges, scaling, and hit-testing. The window only draws what this returns.
/// </summary>
public static class LedgerSankeyLayout
{
    public const decimal OtherThreshold = 0.03m;
    public const string OtherLabel = "Other";
    public const string TreasuryLabel = "Treasury";
    public const string NetSavedLabel = "Net saved";
    public const string ShortfallLabel = "Shortfall (from reserves)";
    public const string EmptyMessage = "No ledger entries for this period";
    public const float Padding = 8f;
    public const float NodeWidth = 14f;
    public const float NodeWidthMin = 12f;
    public const float NodeWidthMax = 16f;
    public const float NodeGap = 5f;
    public const float LabelGap = 6f;
    public const float EstimatedCharWidth = 8f;
    public const float EstimatedLineHeight = 16f;
    public const int BandSamples = 24;

    /// <summary>Narrow bar width, 12-16 px when the plot can fit three bars, smaller only if it cannot.</summary>
    public static float NodeWidthFor(float plotWidth)
    {
        float width = NodeWidth;
        if (width * 3f > plotWidth)
            width = plotWidth / 3f;
        if (width > NodeWidthMax)
            width = NodeWidthMax;
        if (width < 1f)
            width = 1f;
        return width;
    }

    public static Vector2 EstimateTextSize(string text)
        => new((text?.Length ?? 0) * EstimatedCharWidth, EstimatedLineHeight);

    /// <summary>Ellipsize <paramref name="text"/> only when it does not fit <paramref name="maxWidth"/>.</summary>
    public static string FitText(string text, float maxWidth, Func<string, Vector2>? measure = null)
    {
        text ??= "";
        measure ??= EstimateTextSize;
        if (maxWidth <= 0f)
            return "";
        if (measure(text).X <= maxWidth)
            return text;
        const string ellipsis = "...";
        if (measure(ellipsis).X > maxWidth)
            return "";
        int lo = 0;
        int hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (measure(text.Substring(0, mid) + ellipsis).X <= maxWidth)
                lo = mid;
            else
                hi = mid - 1;
        }
        return lo == 0 ? ellipsis : text.Substring(0, lo) + ellipsis;
    }

    /// <summary>
    /// Every year, then that year's months, newest first — the periods <see cref="LedgerTree"/> already provides.
    /// </summary>
    public static IReadOnlyList<LedgerSankeyPeriod> Periods(IReadOnlyList<LedgerYearStatement> years)
    {
        var list = new List<LedgerSankeyPeriod>();
        if (years == null)
            return list;
        foreach (var year in years)
        {
            list.Add(new LedgerSankeyPeriod(
                "Y:" + year.Year.ToString(CultureInfo.InvariantCulture),
                LedgerTree.YearLabel(year.Year),
                true,
                year.Year,
                0,
                year.IncomeTotal,
                year.ExpenseTotal,
                year.IncomeCategories,
                year.ExpenseCategories));
            foreach (var month in year.Months)
            {
                list.Add(new LedgerSankeyPeriod(
                    MonthId(month.Year, month.Month),
                    LedgerTree.MonthLabel(month.MonthStart),
                    false,
                    month.Year,
                    month.Month,
                    month.IncomeTotal,
                    month.ExpenseTotal,
                    month.IncomeCategories,
                    month.ExpenseCategories));
            }
        }
        return list;
    }

    public static string MonthId(int year, int month)
        => "M:" + year.ToString(CultureInfo.InvariantCulture) + "-" + month.ToString("00", CultureInfo.InvariantCulture);

    /// <summary>The selected period, or the latest month when <paramref name="selectedId"/> is missing.</summary>
    public static LedgerSankeyPeriod? Resolve(IReadOnlyList<LedgerSankeyPeriod> periods, string? selectedId)
    {
        if (periods == null || periods.Count == 0)
            return null;
        if (!string.IsNullOrEmpty(selectedId))
        {
            foreach (var period in periods)
            {
                if (period.Id == selectedId)
                    return period;
            }
        }
        foreach (var period in periods)
        {
            if (!period.IsYear)
                return period;
        }
        return periods[0];
    }

    /// <summary>Categories under 3% of <paramref name="sideTotal"/> (absolute) are rolled into Other, in input order, Other last.</summary>
    public static IReadOnlyList<LedgerSankeySlice> Group(IReadOnlyList<LedgerCategoryTotal>? categories, decimal sideTotal)
    {
        var kept = new List<LedgerSankeySlice>();
        if (categories == null || categories.Count == 0)
            return kept;

        decimal magnitude = Math.Abs(sideTotal);
        decimal threshold = magnitude * OtherThreshold;
        var otherParts = new List<(string Label, decimal Amount)>();
        decimal otherTotal = 0;

        foreach (var category in categories)
        {
            decimal amount = Math.Abs(category.Total);
            string label = string.IsNullOrEmpty(category.Label)
                ? LedgerWindow.CategoryLabel(category.Category)
                : category.Label;
            if (magnitude > 0 && amount < threshold)
            {
                otherParts.Add((label, amount));
                otherTotal += amount;
            }
            else
            {
                kept.Add(new LedgerSankeySlice(label, amount, Array.Empty<(string, decimal)>()));
            }
        }

        if (otherParts.Count > 0)
            kept.Add(new LedgerSankeySlice(OtherLabel, otherTotal, otherParts));
        return kept;
    }

    public static string PercentLabel(decimal share)
    {
        if (share <= 0)
            return "0%";
        decimal pct = Math.Round(share * 100m, 1, MidpointRounding.AwayFromZero);
        if (pct == decimal.Truncate(pct))
            return ((int)pct).ToString(CultureInfo.InvariantCulture) + "%";
        return pct.ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }

    public static IReadOnlyList<string> TooltipLines(LedgerSankeyBand band, Func<decimal, string>? money = null)
    {
        money ??= static amount => amount.ToString("0.00", CultureInfo.InvariantCulture);
        var lines = new List<string>
        {
            band.Label + "    " + money(band.Amount) + "    " + PercentLabel(band.Share)
        };
        foreach (var part in band.Parts)
            lines.Add(part.Label + "    " + money(part.Amount));
        return lines;
    }

    public static LedgerSankeyDiagram Build(
        IReadOnlyList<LedgerCategoryTotal>? income,
        IReadOnlyList<LedgerCategoryTotal>? expense,
        decimal incomeTotal,
        decimal expenseTotal,
        float x,
        float y,
        float width,
        float height,
        Func<string, Vector2>? measure = null,
        Func<decimal, string>? money = null)
    {
        measure ??= EstimateTextSize;
        money ??= static amount => amount.ToString("0.00", CultureInfo.InvariantCulture);

        decimal incomeMag = incomeTotal < 0 ? -incomeTotal : incomeTotal;
        decimal expenseMag = expenseTotal < 0 ? -expenseTotal : expenseTotal;
        var incomeSlices = Group(income, incomeMag);
        var expenseSlices = Group(expense, expenseMag);

        if (incomeMag == 0 && expenseMag == 0)
            return new LedgerSankeyDiagram(Array.Empty<LedgerSankeyNode>(), Array.Empty<LedgerSankeyBand>(), EmptyMessage);

        decimal throughput = incomeMag > expenseMag ? incomeMag : expenseMag;
        decimal net = incomeMag - expenseMag;
        bool hasNet = net > 0;
        bool hasShortfall = net < 0;
        decimal shortfall = hasShortfall ? -net : 0;
        decimal saved = hasNet ? net : 0;

        float pad = Padding;
        if (width < pad * 2)
            width = pad * 2;
        if (height < pad * 2)
            height = pad * 2;

        float innerX = x + pad;
        float innerY = y + pad;
        float innerW = width - pad * 2;
        float innerH = height - pad * 2;
        if (innerW < 1)
            innerW = 1;
        if (innerH < 1)
            innerH = 1;

        float lineH = Math.Max(1f, measure("Ag").Y);
        string treasuryMoney = money(throughput);
        float treasuryLabelW = Math.Max(measure(TreasuryLabel).X, measure(treasuryMoney).X);
        float topReserve = lineH * 2f + LabelGap;
        if (topReserve > innerH - 1f)
            topReserve = Math.Max(0f, innerH - 1f);
        float plotY = innerY + topReserve;
        float plotH = innerH - topReserve;
        if (plotH < 1f)
            plotH = 1f;

        float leftNeed = 0f;
        foreach (var slice in incomeSlices)
            leftNeed = Math.Max(leftNeed, Math.Max(measure(slice.Label).X, measure(money(slice.Amount)).X));
        if (hasShortfall)
            leftNeed = Math.Max(leftNeed, Math.Max(measure(ShortfallLabel).X, measure(money(shortfall)).X));

        float rightNeed = 0f;
        foreach (var slice in expenseSlices)
            rightNeed = Math.Max(rightNeed, Math.Max(measure(slice.Label).X, measure(money(slice.Amount)).X));
        if (hasNet)
            rightNeed = Math.Max(rightNeed, Math.Max(measure(NetSavedLabel).X, measure(money(saved)).X));

        const float minBand = 24f;
        float minPlot = NodeWidthMin * 3f + minBand * 2f;
        float labelBudget = innerW - 2f * LabelGap - minPlot;
        if (labelBudget < 0f)
        {
            minPlot = Math.Max(3f, innerW - 2f * LabelGap);
            labelBudget = Math.Max(0f, innerW - 2f * LabelGap - minPlot);
        }
        float leftW = leftNeed;
        float rightW = rightNeed;
        float want = leftW + rightW;
        if (want > labelBudget && want > 0f)
        {
            float scale = labelBudget / want;
            leftW *= scale;
            rightW *= scale;
        }

        float plotX = innerX + leftW + LabelGap;
        float plotW = innerW - leftW - rightW - 2f * LabelGap;
        if (plotW < 1f)
            plotW = 1f;

        float nodeW = NodeWidthFor(plotW);
        if (nodeW * 3f > plotW)
            nodeW = plotW / 3f;
        if (nodeW < 1f)
            nodeW = 1f;

        float leftX = plotX;
        float rightX = plotX + plotW - nodeW;
        float midX = plotX + (plotW - nodeW) * 0.5f;

        int leftCount = incomeSlices.Count + (hasShortfall ? 1 : 0);
        int rightCount = expenseSlices.Count + (hasNet ? 1 : 0);
        int gapCount = Math.Max(0, Math.Max(leftCount, rightCount) - 1);
        float gapSpace = gapCount * NodeGap;
        float usableH = plotH - gapSpace;
        if (usableH < 1f)
            usableH = 1f;
        float ppu = (float)((double)usableH / (double)throughput);

        var nodes = new List<LedgerSankeyNode>();
        var bands = new List<LedgerSankeyBand>();

        float cursor = plotY;
        var incomeNodes = new List<(LedgerSankeySlice Slice, LedgerSankeyNode Node)>();
        for (int i = 0; i < incomeSlices.Count; i++)
        {
            var slice = incomeSlices[i];
            float h = (float)((double)slice.Amount * ppu);
            var node = PlaceNode(
                "in-" + slice.Label, slice.Label, slice.Amount, LedgerSankeyNodeKind.Income,
                leftX, cursor, nodeW, h,
                innerX, leftW, plotY, y + height, true, LedgerSankeyLabelSide.Left,
                lineH, measure, money);
            nodes.Add(node);
            incomeNodes.Add((slice, node));
            cursor += h;
            if (i < incomeSlices.Count - 1 || hasShortfall)
                cursor += NodeGap;
        }

        LedgerSankeyNode? shortfallNode = null;
        if (hasShortfall)
        {
            float h = (float)((double)shortfall * ppu);
            shortfallNode = PlaceNode(
                "shortfall", ShortfallLabel, shortfall, LedgerSankeyNodeKind.Shortfall,
                leftX, cursor, nodeW, h,
                innerX, leftW, plotY, y + height, true, LedgerSankeyLabelSide.Left,
                lineH, measure, money);
            nodes.Add(shortfallNode);
        }

        float treasuryH = (float)((double)throughput * ppu);
        var treasury = PlaceTreasury(
            throughput, midX, plotY, nodeW, treasuryH,
            innerX, innerW, innerY, plotY, lineH, measure, money);
        nodes.Add(treasury);

        cursor = plotY;
        var expenseNodes = new List<(LedgerSankeySlice Slice, LedgerSankeyNode Node)>();
        for (int i = 0; i < expenseSlices.Count; i++)
        {
            var slice = expenseSlices[i];
            float h = (float)((double)slice.Amount * ppu);
            var node = PlaceNode(
                "ex-" + slice.Label, slice.Label, slice.Amount, LedgerSankeyNodeKind.Expense,
                rightX, cursor, nodeW, h,
                rightX + nodeW + LabelGap, rightW, plotY, y + height, false, LedgerSankeyLabelSide.Right,
                lineH, measure, money);
            nodes.Add(node);
            expenseNodes.Add((slice, node));
            cursor += h;
            if (i < expenseSlices.Count - 1 || hasNet)
                cursor += NodeGap;
        }

        LedgerSankeyNode? netNode = null;
        if (hasNet)
        {
            float h = (float)((double)saved * ppu);
            netNode = PlaceNode(
                "net", NetSavedLabel, saved, LedgerSankeyNodeKind.NetSaved,
                rightX, cursor, nodeW, h,
                rightX + nodeW + LabelGap, rightW, plotY, y + height, false, LedgerSankeyLabelSide.Right,
                lineH, measure, money);
            nodes.Add(netNode);
        }

        float treasuryLeft = midX;
        float treasuryRight = midX + nodeW;
        float leftEdge = leftX + nodeW;
        float rightEdge = rightX;
        float tIn = plotY;
        foreach (var (slice, node) in incomeNodes)
        {
            decimal share = throughput == 0 ? 0 : slice.Amount / throughput;
            bands.Add(new LedgerSankeyBand(
                "band-in-" + slice.Label, slice.Label, slice.Amount, share, LedgerSankeyBandKind.Income, slice.Parts,
                leftEdge, node.Y, node.Y + node.Height,
                treasuryLeft, tIn, tIn + node.Height));
            tIn += node.Height;
        }
        if (shortfallNode != null)
        {
            decimal share = throughput == 0 ? 0 : shortfall / throughput;
            bands.Add(new LedgerSankeyBand(
                "band-shortfall", ShortfallLabel, shortfall, share, LedgerSankeyBandKind.Shortfall,
                Array.Empty<(string, decimal)>(),
                leftEdge, shortfallNode.Y, shortfallNode.Y + shortfallNode.Height,
                treasuryLeft, tIn, tIn + shortfallNode.Height));
        }

        float tOut = plotY;
        foreach (var (slice, node) in expenseNodes)
        {
            decimal share = throughput == 0 ? 0 : slice.Amount / throughput;
            bands.Add(new LedgerSankeyBand(
                "band-ex-" + slice.Label, slice.Label, slice.Amount, share, LedgerSankeyBandKind.Expense, slice.Parts,
                treasuryRight, tOut, tOut + node.Height,
                rightEdge, node.Y, node.Y + node.Height));
            tOut += node.Height;
        }
        if (netNode != null)
        {
            decimal share = throughput == 0 ? 0 : saved / throughput;
            bands.Add(new LedgerSankeyBand(
                "band-net", NetSavedLabel, saved, share, LedgerSankeyBandKind.NetSaved,
                Array.Empty<(string, decimal)>(),
                treasuryRight, tOut, tOut + netNode.Height,
                rightEdge, netNode.Y, netNode.Y + netNode.Height));
        }

        return new LedgerSankeyDiagram(nodes, bands, null);
    }

    static LedgerSankeyNode PlaceNode(
        string id,
        string label,
        decimal amount,
        LedgerSankeyNodeKind kind,
        float barX,
        float barY,
        float barW,
        float barH,
        float labelBoxX,
        float labelBoxW,
        float plotY,
        float contentBottom,
        bool alignRight,
        LedgerSankeyLabelSide side,
        float lineH,
        Func<string, Vector2> measure,
        Func<decimal, string> money)
    {
        string amountText = money(amount);
        string fittedLabel = FitText(label, labelBoxW, measure);
        string fittedAmount = FitText(amountText, labelBoxW, measure);
        float labelH = lineH * 2f;
        float labelY = barY + (barH - labelH) * 0.5f;
        float minY = plotY;
        float maxY = contentBottom - Padding - labelH;
        if (maxY < minY)
            labelY = minY;
        else if (labelY < minY)
            labelY = minY;
        else if (labelY > maxY)
            labelY = maxY;
        return new LedgerSankeyNode(id, label, amount, kind, barX, barY, barW, barH)
        {
            LabelX = labelBoxX,
            LabelY = labelY,
            LabelMaxWidth = labelBoxW,
            LabelLineHeight = lineH,
            LabelAlignRight = alignRight,
            LabelSide = side,
            AmountText = amountText,
            FittedLabel = fittedLabel,
            FittedAmount = fittedAmount,
        };
    }

    static LedgerSankeyNode PlaceTreasury(
        decimal amount,
        float barX,
        float barY,
        float barW,
        float barH,
        float innerX,
        float innerW,
        float innerY,
        float plotY,
        float lineH,
        Func<string, Vector2> measure,
        Func<decimal, string> money)
    {
        string amountText = money(amount);
        float boxW = Math.Min(innerW, Math.Max(measure(TreasuryLabel).X, measure(amountText).X));
        string fittedLabel = FitText(TreasuryLabel, innerW, measure);
        string fittedAmount = FitText(amountText, innerW, measure);
        boxW = Math.Min(innerW, Math.Max(measure(fittedLabel).X, measure(fittedAmount).X));
        float labelX = barX + barW * 0.5f - boxW * 0.5f;
        if (labelX < innerX)
            labelX = innerX;
        if (labelX + boxW > innerX + innerW)
            labelX = innerX + innerW - boxW;
        float labelH = lineH * 2f;
        float labelY = plotY - LabelGap - labelH;
        if (labelY < innerY)
            labelY = innerY;
        return new LedgerSankeyNode("treasury", TreasuryLabel, amount, LedgerSankeyNodeKind.Treasury, barX, barY, barW, barH)
        {
            LabelX = labelX,
            LabelY = labelY,
            LabelMaxWidth = boxW,
            LabelLineHeight = lineH,
            LabelAlignRight = false,
            LabelSide = LedgerSankeyLabelSide.Above,
            AmountText = amountText,
            FittedLabel = fittedLabel,
            FittedAmount = fittedAmount,
        };
    }

    public static bool BandContains(LedgerSankeyBand band, float x, float y, int samples = BandSamples)
    {
        if (samples < 2)
            samples = 2;
        var top = new Vector2[samples];
        var bot = new Vector2[samples];
        SampleBand(band, samples, top, bot);
        var point = new Vector2(x, y);
        for (int i = 0; i < samples - 1; i++)
        {
            if (PointInTriangle(point, top[i], top[i + 1], bot[i + 1])
                || PointInTriangle(point, top[i], bot[i + 1], bot[i]))
                return true;
        }
        return false;
    }

    /// <summary>Filled-polygon vertices for a band: top edge left-to-right, then bottom edge right-to-left.</summary>
    public static Vector2[] BandPolygon(LedgerSankeyBand band, int samples = BandSamples)
    {
        if (samples < 2)
            samples = 2;
        var top = new Vector2[samples];
        var bot = new Vector2[samples];
        SampleBand(band, samples, top, bot);
        var poly = new Vector2[samples * 2];
        for (int i = 0; i < samples; i++)
            poly[i] = top[i];
        for (int i = 0; i < samples; i++)
            poly[samples + i] = bot[samples - 1 - i];
        return poly;
    }

    /// <summary>Convex quads along the band, each safe to fill on its own.</summary>
    public static IReadOnlyList<(Vector2 A, Vector2 B, Vector2 C, Vector2 D)> BandQuads(LedgerSankeyBand band, int samples = BandSamples)
    {
        if (samples < 2)
            samples = 2;
        var top = new Vector2[samples];
        var bot = new Vector2[samples];
        SampleBand(band, samples, top, bot);
        var quads = new List<(Vector2, Vector2, Vector2, Vector2)>(samples - 1);
        for (int i = 0; i < samples - 1; i++)
            quads.Add((top[i], top[i + 1], bot[i + 1], bot[i]));
        return quads;
    }

    static void SampleBand(LedgerSankeyBand band, int samples, Vector2[] top, Vector2[] bot)
    {
        var t0 = new Vector2(band.LeftX, band.LeftY0);
        var t3 = new Vector2(band.RightX, band.RightY0);
        var b0 = new Vector2(band.LeftX, band.LeftY1);
        var b3 = new Vector2(band.RightX, band.RightY1);
        float midX = (band.LeftX + band.RightX) * 0.5f;
        var t1 = new Vector2(midX, band.LeftY0);
        var t2 = new Vector2(midX, band.RightY0);
        var b1 = new Vector2(midX, band.LeftY1);
        var b2 = new Vector2(midX, band.RightY1);
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            top[i] = Cubic(t0, t1, t2, t3, t);
            bot[i] = Cubic(b0, b1, b2, b3, t);
        }
    }

    public static Vector2 Cubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
    }

    public static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(p, a, b);
        float d2 = Cross(p, b, c);
        float d3 = Cross(p, c, a);
        bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNeg && hasPos);
    }

    static float Cross(Vector2 p, Vector2 a, Vector2 b)
        => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
}
