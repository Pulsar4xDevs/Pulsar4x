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

/// <summary>A labelled box in the diagram, in the same coordinate space as the content region.</summary>
public sealed record LedgerSankeyNode(
    string Id,
    string Label,
    decimal Amount,
    LedgerSankeyNodeKind Kind,
    float X,
    float Y,
    float Width,
    float Height);

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
    public const int BandSamples = 24;

    public static float NodeWidthFor(float width)
        => Math.Clamp(width * 0.18f, 72f, 140f);

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
        float height)
    {
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

        float nodeW = NodeWidthFor(innerW);
        if (nodeW * 3 > innerW)
            nodeW = innerW / 3f;

        float leftX = innerX;
        float rightX = innerX + innerW - nodeW;
        float midX = innerX + (innerW - nodeW) * 0.5f;
        float ppu = (float)((double)innerH / (double)throughput);

        var nodes = new List<LedgerSankeyNode>();
        var bands = new List<LedgerSankeyBand>();

        float incomeTop = innerY;
        float cursor = incomeTop;
        var incomeNodes = new List<(LedgerSankeySlice Slice, LedgerSankeyNode Node)>();
        foreach (var slice in incomeSlices)
        {
            float h = (float)((double)slice.Amount * ppu);
            var node = new LedgerSankeyNode(
                "in-" + slice.Label, slice.Label, slice.Amount, LedgerSankeyNodeKind.Income,
                leftX, cursor, nodeW, h);
            nodes.Add(node);
            incomeNodes.Add((slice, node));
            cursor += h;
        }

        LedgerSankeyNode? shortfallNode = null;
        if (hasShortfall)
        {
            float h = (float)((double)shortfall * ppu);
            shortfallNode = new LedgerSankeyNode(
                "shortfall", ShortfallLabel, shortfall, LedgerSankeyNodeKind.Shortfall,
                leftX, cursor, nodeW, h);
            nodes.Add(shortfallNode);
        }

        float treasuryH = (float)((double)throughput * ppu);
        var treasury = new LedgerSankeyNode(
            "treasury", TreasuryLabel, throughput, LedgerSankeyNodeKind.Treasury,
            midX, innerY, nodeW, treasuryH);
        nodes.Add(treasury);

        cursor = innerY;
        var expenseNodes = new List<(LedgerSankeySlice Slice, LedgerSankeyNode Node)>();
        foreach (var slice in expenseSlices)
        {
            float h = (float)((double)slice.Amount * ppu);
            var node = new LedgerSankeyNode(
                "ex-" + slice.Label, slice.Label, slice.Amount, LedgerSankeyNodeKind.Expense,
                rightX, cursor, nodeW, h);
            nodes.Add(node);
            expenseNodes.Add((slice, node));
            cursor += h;
        }

        LedgerSankeyNode? netNode = null;
        if (hasNet)
        {
            float h = (float)((double)saved * ppu);
            netNode = new LedgerSankeyNode(
                "net", NetSavedLabel, saved, LedgerSankeyNodeKind.NetSaved,
                rightX, cursor, nodeW, h);
            nodes.Add(netNode);
        }

        float treasuryLeft = midX;
        float treasuryRight = midX + nodeW;
        float leftEdge = leftX + nodeW;
        float rightEdge = rightX;
        float tIn = innerY;
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

        float tOut = innerY;
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
