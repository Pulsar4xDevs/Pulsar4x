using System;
using System.Numerics;
using NUnit.Framework;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>
/// Layout helpers behind the 1280 px visual fixes: Commanders year columns, the ship designer's first-use size,
/// cut research names with a full-name tooltip, and the fixed Fleet Orders date column.
/// </summary>
[TestFixture]
[NonParallelizable]
public class VisualFitUiTests
{
    // A monospace stand-in for ImGui.CalcTextSize: 7 px a character.
    static float Mono(string s) => s.Length * 7f;

    [Test]
    public void CommanderYearColumns_FitTheirHeader_AndTheirWidestValue()
    {
        float header = Mono(CommanderWindow.ServiceHeader);
        float value = Mono(CommanderWindow.YearsValueSample);
        Assert.That(CommanderWindow.FitColumnWidth(header, value, 12f), Is.EqualTo(header + 12f), "the header is the wider one");
        Assert.That(CommanderWindow.FitColumnWidth(10f, value, 12f), Is.EqualTo(value + 12f), "a short header still fits the values");
        Assert.That(CommanderWindow.FitColumnWidth(header, value, -5f), Is.EqualTo(header), "negative padding never shrinks below the text");
    }

    [Test]
    public void ShipDesigner_At1280x800_FitsLeftOfTheSelector()
    {
        var fit = ShipDesignWindow.FitDesignerSize(new Vector2(1280, 800), ShipDesignWindow.SelectorWidth);
        Assert.That(ShipDesignWindow.DesignerMargin + fit.X, Is.LessThanOrEqualTo(1280 - ShipDesignWindow.SelectorWidth));
        Assert.That(ShipDesignWindow.DesignerTop + fit.Y, Is.LessThanOrEqualTo(800));
        Assert.That(fit.X, Is.EqualTo(1280 - 256 - 16));
    }

    [Test]
    public void ShipDesigner_PanelShares_FavourStatistics_WhenNarrow_AndNeverOverflow()
    {
        var narrow = ShipDesignWindow.DesignerColumnShares(830f);
        Assert.That(narrow.Z, Is.GreaterThan(narrow.X).And.GreaterThan(narrow.Y));
        Assert.That(narrow.X + narrow.Y + narrow.Z, Is.LessThanOrEqualTo(1.0f + 1e-6f));
        var wide = ShipDesignWindow.DesignerColumnShares(1200f);
        Assert.That(wide.X, Is.EqualTo(wide.Z));
        Assert.That(wide.X + wide.Y + wide.Z, Is.LessThanOrEqualTo(1.0f + 1e-6f));
    }

    [Test]
    public void ShipDesigner_OnABigScreen_KeepsItsPreferredSize()
        => Assert.That(ShipDesignWindow.FitDesignerSize(new Vector2(2560, 1440), 256), Is.EqualTo(new Vector2(1350, 825)));

    [Test]
    public void ResearchName_ThatFits_IsShownWhole_WithNoExtraTooltipLine()
    {
        string name = "Fuel Cells";
        string shown = ResearchWindow.FitText(name, 200f, Mono);
        Assert.That(shown, Is.EqualTo(name));
        Assert.That(ResearchWindow.FullNameLine(name, shown), Is.Empty);
    }

    [Test]
    public void ResearchName_TooLong_IsCutWithDots_AndTheTooltipLeadsWithTheFullName()
    {
        string name = "Alcubierre Gravitational Coupling 1";
        float width = 180f;
        string shown = ResearchWindow.FitText(name, width, Mono);
        Assert.That(shown, Does.EndWith(ResearchWindow.Ellipsis));
        Assert.That(Mono(shown), Is.LessThanOrEqualTo(width));
        Assert.That(name, Does.StartWith(shown.Substring(0, shown.Length - ResearchWindow.Ellipsis.Length)));
        Assert.That(ResearchWindow.FullNameLine(name, shown), Does.StartWith(name));
        Assert.That(ResearchWindow.FitText(name, 1f, Mono), Is.EqualTo(ResearchWindow.Ellipsis), "no room at all still shows dots");
    }

    [Test]
    public void ResearchFullNameLine_EscapesPercent_ForTheFormattedTooltip()
        => Assert.That(ResearchWindow.FullNameLine("Efficiency +10% Long Name", "Efficiency..."), Does.StartWith("Efficiency +10%% Long Name"));

    [Test]
    public void FleetOrdersDateColumn_IsAsWideAsTheLongestDate_PlusPadding()
    {
        var (date, time) = (DateDisplay.Date, DateDisplay.Time);
        try
        {
            DateDisplay.Date = DateStyle.DayMonthName;
            DateDisplay.Time = TimeStyle.TwelveHour;
            float width = DateDisplay.DateTimeColumnWidth(Mono, padding: 8f);
            Assert.That(width, Is.EqualTo(Mono(DateDisplay.WidestDateTimeSample()) + 8f));
            Assert.That(Mono("28 Sep 2026 12:59 PM"), Is.LessThanOrEqualTo(width - 8f));
            Assert.That(DateDisplay.DateTimeColumnWidth(Mono, seconds: true), Is.GreaterThanOrEqualTo(Mono("28 Sep 2026 12:59:59 PM")));
        }
        finally
        {
            (DateDisplay.Date, DateDisplay.Time) = (date, time);
        }
    }
}
