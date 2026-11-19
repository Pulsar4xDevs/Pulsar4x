using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Pure layout for the ledger Flow-tab Sankey. No ImGui and no game.</summary>
[TestFixture]
public class LedgerSankeyLayoutTests
{
    const float X = 0f;
    const float Y = 0f;
    const float W = 800f;
    const float H = 400f;

    static LedgerCategoryTotal Cat(string category, decimal total, bool income)
        => new(category, LedgerWindow.CategoryLabel(category), total, income, Array.Empty<LedgerEntrySnapshot>());

    static LedgerSankeyDiagram Diagram(
        (string Category, decimal Total)[] income,
        (string Category, decimal Total)[] expense,
        float width = W,
        float height = H)
    {
        var inc = income.Select(c => Cat(c.Category, c.Total, true)).ToArray();
        var exp = expense.Select(c => Cat(c.Category, c.Total, false)).ToArray();
        decimal incomeTotal = inc.Sum(c => c.Total);
        decimal expenseTotal = exp.Sum(c => c.Total);
        return LedgerSankeyLayout.Build(inc, exp, incomeTotal, expenseTotal, X, Y, width, height);
    }

    LedgerSankeyNode Node(LedgerSankeyDiagram d, string label)
        => d.Nodes.Single(n => n.Label == label);

    LedgerSankeyBand Band(LedgerSankeyDiagram d, string label)
        => d.Bands.Single(b => b.Label == label);

    [Test]
    public void NodeHeights_AreProportionalToAmount()
    {
        var d = Diagram(
            new[] { ("Trade", 100m), ("Charter", 50m) },
            new[] { (FactionSnapshot.WagesCategory, -150m) });

        var trade = Node(d, "Trade");
        var charter = Node(d, "Charter");
        var wages = Node(d, "Wages (monthly)");
        var treasury = Node(d, LedgerSankeyLayout.TreasuryLabel);

        Assert.That(trade.Height / charter.Height, Is.EqualTo(2f).Within(1e-4f));
        Assert.That(trade.Height + charter.Height, Is.EqualTo(wages.Height).Within(1e-3f));
        Assert.That(treasury.Height, Is.EqualTo(wages.Height).Within(1e-3f));
        Assert.That(trade.Kind, Is.EqualTo(LedgerSankeyNodeKind.Income));
        Assert.That(wages.Kind, Is.EqualTo(LedgerSankeyNodeKind.Expense));
        Assert.That(treasury.Kind, Is.EqualTo(LedgerSankeyNodeKind.Treasury));
        Assert.That(trade.X, Is.LessThan(treasury.X));
        Assert.That(wages.X, Is.GreaterThan(treasury.X));
        Assert.That(d.IsEmpty, Is.False);
        Assert.That(d.EmptyMessage, Is.Null);
    }

    [Test]
    public void OtherGrouping_RollsUpSlicesUnderThreePercent_AndListsWhatIsInside()
    {
        // Side total 1000: 20 is 2% (grouped), 30 is 3% (kept), 950 is kept.
        var income = new[]
        {
            Cat("Trade", 950m, true),
            Cat("Charter", 30m, true),
            Cat("InitialInvestment", 20m, true),
        };
        var slices = LedgerSankeyLayout.Group(income, 1000m);
        Assert.That(slices.Select(s => s.Label), Is.EqualTo(new[] { "Trade", "Charter", LedgerSankeyLayout.OtherLabel }));
        var other = slices.Single(s => s.Label == LedgerSankeyLayout.OtherLabel);
        Assert.That(other.Amount, Is.EqualTo(20m));
        Assert.That(other.Parts, Is.EqualTo(new[] { ("Initial investment", 20m) }));

        var d = LedgerSankeyLayout.Build(income, Array.Empty<LedgerCategoryTotal>(), 1000m, 0m, X, Y, W, H);
        var band = Band(d, LedgerSankeyLayout.OtherLabel);
        Assert.That(band.Amount, Is.EqualTo(20m));
        Assert.That(band.Parts.Select(p => (p.Label, p.Amount)), Is.EqualTo(new[] { ("Initial investment", 20m) }));
        var tip = LedgerSankeyLayout.TooltipLines(band);
        Assert.That(tip[0], Does.Contain(LedgerSankeyLayout.OtherLabel).And.Contain("20.00").And.Contain("%"));
        Assert.That(tip[0], Does.Contain("2%"));
        Assert.That(tip, Has.Some.Contain("Initial investment").And.Contain("20.00"));
        Assert.That(tip.Any(line => line.Contains("%")), Is.True);
    }

    [Test]
    public void NetSavedBand_AppearsOnTheRight_WhenIncomeExceedsExpenses()
    {
        var d = Diagram(
            new[] { ("Trade", 100m) },
            new[] { (FactionSnapshot.WagesCategory, -40m) });

        Assert.That(d.Nodes.Any(n => n.Kind == LedgerSankeyNodeKind.Shortfall), Is.False);
        var net = Node(d, LedgerSankeyLayout.NetSavedLabel);
        var trade = Node(d, "Trade");
        var wages = Node(d, "Wages (monthly)");
        Assert.That(net.Kind, Is.EqualTo(LedgerSankeyNodeKind.NetSaved));
        Assert.That(net.Amount, Is.EqualTo(60m));
        Assert.That(net.X, Is.EqualTo(wages.X));
        Assert.That(net.Y, Is.GreaterThanOrEqualTo(wages.Y + wages.Height - 1e-3f));
        Assert.That(net.Height / wages.Height, Is.EqualTo(1.5f).Within(1e-4f));
        Assert.That(trade.Height, Is.EqualTo(wages.Height + net.Height).Within(1e-3f));

        var band = Band(d, LedgerSankeyLayout.NetSavedLabel);
        Assert.That(band.Kind, Is.EqualTo(LedgerSankeyBandKind.NetSaved));
        Assert.That(band.Amount, Is.EqualTo(60m));
        Assert.That(band.Share, Is.EqualTo(0.6m));
        Assert.That(LedgerSankeyLayout.PercentLabel(band.Share), Is.EqualTo("60%"));
    }

    [Test]
    public void ShortfallBand_AppearsOnTheLeft_WhenExpensesExceedIncome()
    {
        var d = Diagram(
            new[] { ("Trade", 40m) },
            new[] { (FactionSnapshot.WagesCategory, -100m) });

        Assert.That(d.Nodes.Any(n => n.Kind == LedgerSankeyNodeKind.NetSaved), Is.False);
        var shortfall = Node(d, LedgerSankeyLayout.ShortfallLabel);
        var trade = Node(d, "Trade");
        var wages = Node(d, "Wages (monthly)");
        Assert.That(shortfall.Kind, Is.EqualTo(LedgerSankeyNodeKind.Shortfall));
        Assert.That(shortfall.Amount, Is.EqualTo(60m));
        Assert.That(shortfall.X, Is.EqualTo(trade.X));
        Assert.That(shortfall.Y, Is.GreaterThanOrEqualTo(trade.Y + trade.Height - 1e-3f));
        Assert.That(shortfall.Height / trade.Height, Is.EqualTo(1.5f).Within(1e-4f));
        Assert.That(wages.Height, Is.EqualTo(trade.Height + shortfall.Height).Within(1e-3f));

        var band = Band(d, LedgerSankeyLayout.ShortfallLabel);
        Assert.That(band.Kind, Is.EqualTo(LedgerSankeyBandKind.Shortfall));
        Assert.That(band.Share, Is.EqualTo(0.6m));
    }

    [Test]
    public void EqualIncomeAndExpenses_HasNeitherNetNorShortfall()
    {
        var d = Diagram(
            new[] { ("Trade", 80m) },
            new[] { (FactionSnapshot.WagesCategory, -80m) });

        Assert.That(d.Nodes.Select(n => n.Kind),
            Is.EquivalentTo(new[] { LedgerSankeyNodeKind.Income, LedgerSankeyNodeKind.Expense, LedgerSankeyNodeKind.Treasury }));
        Assert.That(d.Bands.Select(b => b.Kind),
            Is.EquivalentTo(new[] { LedgerSankeyBandKind.Income, LedgerSankeyBandKind.Expense }));
        Assert.That(Node(d, "Trade").Height, Is.EqualTo(Node(d, "Wages (monthly)").Height).Within(1e-3f));
        Assert.That(Node(d, "Trade").Height, Is.EqualTo(Node(d, LedgerSankeyLayout.TreasuryLabel).Height).Within(1e-3f));
        Assert.That(d.IsEmpty, Is.False);
    }

    [Test]
    public void EmptyPeriod_ReturnsTheEmptyMessage_AndNoGeometry()
    {
        var d = LedgerSankeyLayout.Build(
            Array.Empty<LedgerCategoryTotal>(),
            Array.Empty<LedgerCategoryTotal>(),
            0m, 0m, X, Y, W, H);
        Assert.That(d.IsEmpty, Is.True);
        Assert.That(d.EmptyMessage, Is.EqualTo(LedgerSankeyLayout.EmptyMessage));
        Assert.That(d.EmptyMessage, Is.EqualTo("No ledger entries for this period"));
        Assert.That(d.Nodes, Is.Empty);
        Assert.That(d.Bands, Is.Empty);
        Assert.That(d.HitTest(400, 200), Is.Null);

        var tiny = LedgerSankeyLayout.Build(
            Array.Empty<LedgerCategoryTotal>(),
            Array.Empty<LedgerCategoryTotal>(),
            0m, 0m, X, Y, 20f, 10f);
        Assert.That(tiny.IsEmpty, Is.True);
        Assert.That(() => Diagram(new[] { ("Trade", 10m) }, new[] { (FactionSnapshot.WagesCategory, -4m) }, 40f, 20f),
            Throws.Nothing, "a tiny non-empty region still produces a layout");
    }

    [Test]
    public void Periods_CoverEachYearAndMonth_AndDefaultToTheLatestMonth()
    {
        var rows = new[]
        {
            new LedgerEntrySnapshot(new DateTime(2050, 2, 1), FactionSnapshot.WagesCategory, "Feb wages", -200m),
            new LedgerEntrySnapshot(new DateTime(2050, 1, 15), "Trade", "Sold steel", 80m),
            new LedgerEntrySnapshot(new DateTime(2049, 12, 1), "InitialInvestment", "Start-up", 1000m),
        };
        var years = LedgerTree.Build(rows);
        var periods = LedgerSankeyLayout.Periods(years);
        Assert.That(periods.Select(p => (p.IsYear, p.Year, p.Month, p.Label)), Is.EqualTo(new[]
        {
            (true, 2050, 0, "2050"),
            (false, 2050, 2, LedgerTree.MonthLabel(new DateTime(2050, 2, 1))),
            (false, 2050, 1, LedgerTree.MonthLabel(new DateTime(2050, 1, 1))),
            (true, 2049, 0, "2049"),
            (false, 2049, 12, LedgerTree.MonthLabel(new DateTime(2049, 12, 1))),
        }));

        var latest = LedgerSankeyLayout.Resolve(periods, null);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.IsYear, Is.False);
        Assert.That(latest.Year, Is.EqualTo(2050));
        Assert.That(latest.Month, Is.EqualTo(2));
        Assert.That(latest.ExpenseTotal, Is.EqualTo(-200m));
        Assert.That(latest.IncomeTotal, Is.EqualTo(0m));

        var year = LedgerSankeyLayout.Resolve(periods, "Y:2049");
        Assert.That(year!.IsYear, Is.True);
        Assert.That(year.IncomeTotal, Is.EqualTo(1000m));
        Assert.That(LedgerSankeyLayout.Resolve(periods, "missing")!.Month, Is.EqualTo(2),
            "an unknown selection falls back to the latest month");
        Assert.That(LedgerSankeyLayout.Resolve(Array.Empty<LedgerSankeyPeriod>(), null), Is.Null);
        Assert.That(LedgerSankeyLayout.Periods(Array.Empty<LedgerYearStatement>()), Is.Empty);
    }

    [Test]
    public void HitTest_FindsTheBandUnderAPoint_OnTheSampledBezierStrip()
    {
        var d = Diagram(
            new[] { ("Trade", 100m) },
            new[] { (FactionSnapshot.WagesCategory, -40m) });
        var trade = Band(d, "Trade");
        float midX = (trade.LeftX + trade.RightX) * 0.5f;
        float midY = (trade.LeftY0 + trade.LeftY1 + trade.RightY0 + trade.RightY1) * 0.25f;
        Assert.That(LedgerSankeyLayout.BandContains(trade, midX, midY), Is.True);
        Assert.That(d.HitTest(midX, midY)!.Label, Is.EqualTo("Trade"));
        Assert.That(d.HitTest(trade.LeftX - 20f, midY), Is.Null);
        Assert.That(d.HitTest(midX, trade.LeftY0 - 30f), Is.Null);

        var net = Band(d, LedgerSankeyLayout.NetSavedLabel);
        float netX = (net.LeftX + net.RightX) * 0.5f;
        float netY = (net.LeftY0 + net.LeftY1) * 0.5f;
        Assert.That(d.HitTest(netX, netY)!.Label, Is.EqualTo(LedgerSankeyLayout.NetSavedLabel));
    }

    [Test]
    public void PercentLabel_AndTooltip_UseAPercentSign()
    {
        Assert.That(LedgerSankeyLayout.PercentLabel(0), Is.EqualTo("0%"));
        Assert.That(LedgerSankeyLayout.PercentLabel(1), Is.EqualTo("100%"));
        Assert.That(LedgerSankeyLayout.PercentLabel(0.125m), Is.EqualTo("12.5%"));
        Assert.That(LedgerSankeyLayout.PercentLabel(0.03m), Is.EqualTo("3%"));
    }

    [Test]
    public void Nodes_AreNarrowBars_WithGapsAndReservedLabelMargins()
    {
        var d = Diagram(
            new[] { ("Trade", 100m), ("Charter", 50m) },
            new[] { (FactionSnapshot.WagesCategory, -90m), (FactionSnapshot.StaffWagesCategory, -60m) });

        var trade = Node(d, "Trade");
        var charter = Node(d, "Charter");
        var wages = Node(d, "Wages (monthly)");
        var staff = Node(d, "Staff wages (monthly)");
        var treasury = Node(d, LedgerSankeyLayout.TreasuryLabel);

        foreach (var node in d.Nodes)
        {
            Assert.That(node.Width, Is.InRange(LedgerSankeyLayout.NodeWidthMin, LedgerSankeyLayout.NodeWidthMax), node.Label);
            Assert.That(node.Width, Is.EqualTo(LedgerSankeyLayout.NodeWidth).Within(1e-3f), node.Label);
        }

        Assert.That(charter.Y, Is.EqualTo(trade.Y + trade.Height + LedgerSankeyLayout.NodeGap).Within(1e-3f));
        Assert.That(staff.Y, Is.EqualTo(wages.Y + wages.Height + LedgerSankeyLayout.NodeGap).Within(1e-3f));

        var tradeBand = Band(d, "Trade");
        Assert.That(tradeBand.LeftY0, Is.EqualTo(trade.Y).Within(1e-3f));
        Assert.That(tradeBand.LeftY1, Is.EqualTo(trade.Y + trade.Height).Within(1e-3f));
        Assert.That(tradeBand.LeftY1, Is.LessThanOrEqualTo(charter.Y + 1e-3f));
        var charterBand = Band(d, "Charter");
        Assert.That(charterBand.LeftY0, Is.EqualTo(charter.Y).Within(1e-3f));
        Assert.That(charterBand.RightY0, Is.EqualTo(tradeBand.RightY1).Within(1e-3f),
            "treasury-side bands pack tightly, still as thick as their node");
        Assert.That(charterBand.RightY1 - charterBand.RightY0, Is.EqualTo(charter.Height).Within(1e-3f));

        Assert.That(trade.LabelSide, Is.EqualTo(LedgerSankeyLabelSide.Left));
        Assert.That(trade.LabelAlignRight, Is.True);
        Assert.That(trade.LabelX + trade.LabelMaxWidth, Is.LessThanOrEqualTo(trade.X - LedgerSankeyLayout.LabelGap + 1e-3f));
        Assert.That(trade.LabelX, Is.GreaterThanOrEqualTo(X + LedgerSankeyLayout.Padding - 1e-3f));
        Assert.That(trade.FittedLabel, Is.EqualTo("Trade"));

        Assert.That(staff.LabelSide, Is.EqualTo(LedgerSankeyLabelSide.Right));
        Assert.That(staff.LabelAlignRight, Is.False);
        Assert.That(staff.LabelX, Is.GreaterThanOrEqualTo(staff.X + staff.Width + LedgerSankeyLayout.LabelGap - 1e-3f));
        Assert.That(staff.LabelX + staff.LabelMaxWidth, Is.LessThanOrEqualTo(X + W - LedgerSankeyLayout.Padding + 1e-3f));
        Assert.That(staff.FittedLabel, Is.EqualTo("Staff wages (monthly)"));
        Assert.That(staff.LabelMaxWidth, Is.GreaterThanOrEqualTo(LedgerSankeyLayout.EstimateTextSize("Staff wages (monthly)").X - 1e-3f));

        Assert.That(treasury.LabelSide, Is.EqualTo(LedgerSankeyLabelSide.Above));
        Assert.That(treasury.LabelY + treasury.LabelLineHeight * 2f, Is.LessThanOrEqualTo(treasury.Y - LedgerSankeyLayout.LabelGap + 1e-3f));
        Assert.That(treasury.FittedLabel, Is.EqualTo(LedgerSankeyLayout.TreasuryLabel));
    }

    [Test]
    public void NetAndShortfall_KeepTheGapBetweenStackedNodes()
    {
        var net = Diagram(
            new[] { ("Trade", 100m) },
            new[] { (FactionSnapshot.WagesCategory, -40m) });
        var wages = Node(net, "Wages (monthly)");
        var saved = Node(net, LedgerSankeyLayout.NetSavedLabel);
        Assert.That(saved.Y, Is.EqualTo(wages.Y + wages.Height + LedgerSankeyLayout.NodeGap).Within(1e-3f));
        var netBand = Band(net, LedgerSankeyLayout.NetSavedLabel);
        Assert.That(netBand.RightY0, Is.EqualTo(saved.Y).Within(1e-3f));
        Assert.That(netBand.RightY1, Is.EqualTo(saved.Y + saved.Height).Within(1e-3f));

        var deficit = Diagram(
            new[] { ("Trade", 40m) },
            new[] { (FactionSnapshot.WagesCategory, -100m) });
        var trade = Node(deficit, "Trade");
        var shortfall = Node(deficit, LedgerSankeyLayout.ShortfallLabel);
        Assert.That(shortfall.Y, Is.EqualTo(trade.Y + trade.Height + LedgerSankeyLayout.NodeGap).Within(1e-3f));
    }

    [Test]
    public void FitText_EllipsizesOnlyWhenTheWindowIsTooNarrow()
    {
        Assert.That(LedgerSankeyLayout.FitText("Staff wages (monthly)", 1000f), Is.EqualTo("Staff wages (monthly)"));
        string fitted = LedgerSankeyLayout.FitText("Staff wages (monthly)", 10 * LedgerSankeyLayout.EstimatedCharWidth);
        Assert.That(fitted, Is.Not.EqualTo("Staff wages (monthly)"));
        Assert.That(fitted, Does.EndWith("..."));
        Assert.That(LedgerSankeyLayout.EstimateTextSize(fitted).X, Is.LessThanOrEqualTo(10 * LedgerSankeyLayout.EstimatedCharWidth + 1e-3f));

        var tiny = Diagram(
            new[] { ("Trade", 40m) },
            new[] { (FactionSnapshot.StaffWagesCategory, -100m) },
            80f, 80f);
        var staff = Node(tiny, "Staff wages (monthly)");
        Assert.That(staff.FittedLabel, Is.Not.EqualTo("Staff wages (monthly)"),
            "an 80px-wide diagram must ellipsize the long right-hand label");
        Assert.That(staff.LabelX + staff.LabelMaxWidth, Is.LessThanOrEqualTo(X + 80f - LedgerSankeyLayout.Padding + 1e-3f));
        Assert.That(staff.X + staff.Width, Is.LessThanOrEqualTo(staff.LabelX + 1e-3f));
    }
}
