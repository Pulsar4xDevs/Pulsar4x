using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Tests;

/// <summary>The crew plan phase 4-5 UI text and rows (life support, wages, negative funds), from Api views only.</summary>
[TestFixture]
public class CrewLifeSupportAndWagesUiTests
{
    // Invariant numbers with "-¤n" for negative money, so the expected strings do not depend on the box's culture.
    static readonly CultureInfo Inv = MakeCulture();

    static CultureInfo MakeCulture()
    {
        var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        c.NumberFormat.CurrencyNegativePattern = 1;
        return c;
    }

    static ShipPowerView ShipPower(bool has, int crew = 12, double perCrew = 0.8, double gen = 20)
        => new ShipPowerView(gen, 5, 0, 0, true) with
        {
            HasLifeSupport = has,
            LifeSupportCrew = crew,
            LifeSupportPerCrew_kW = perCrew,
            LifeSupport_kW = crew * perCrew,
        };

    [Test]
    public void ShipLifeSupportRow_HiddenUntilShipsDrawLifeSupport()
    {
        Assert.That(LifeSupportRows.ShipRow(ShipPower(false), Vector4.One, 10), Is.Null);
        var row = LifeSupportRows.ShipRow(ShipPower(true), Vector4.One, 14.6);
        Assert.That(row, Is.Not.Null);
        Assert.That(row!.Name, Is.EqualTo("Life support"));
        Assert.That(row.Value, Is.EqualTo(Stringify.Power(9.6)));
        Assert.That(row.Fill, Is.EqualTo(1f), "20 kW generation covers 9.6 kW");
        Assert.That(row.Trailing, Does.StartWith("must-run"));
        Assert.That(row.Tooltip, Does.Contain("0.8 kW per crew x 12 crew"));
        Assert.That(row.Tooltip, Does.Contain("20% less than the 1 kW per crew base"));
        Assert.That(row.Tooltip, Does.Contain("Must-run"));
    }

    [Test]
    public void ShipLifeSupportRow_ShortWhenThePlantCannotCoverIt_AndBaseTextWithoutTech()
    {
        var row = LifeSupportRows.ShipRow(ShipPower(true, crew: 10, perCrew: 1, gen: 5), Vector4.One, 15)!;
        Assert.That(row.Fill, Is.EqualTo(0.5f));
        Assert.That(row.Tooltip, Does.Contain("Base 1 kW per crew"));
    }

    [Test]
    public void ShipLifeSupportRow_NotDoubled_WhenTheEngineListsItAsALoadGroup()
    {
        var power = ShipPower(true) with
        {
            LoadGroups = new[] { new ShipPowerLoadGroup("Life support", 9.6, 9.6, 1, "Powered", Array.Empty<ShipPowerLoadRow>()) },
        };
        Assert.That(LifeSupportRows.ShipRow(power, Vector4.One, 10), Is.Null);
    }

    [Test]
    public void ColonyLifeSupportLine_CarriesTheRateLabel()
    {
        var power = new ColonyPowerView("electricity", 50, 60, 55, 0.92, 0, 46, 920,
            Array.Empty<ColonyPowerConsumerRow>(), Array.Empty<ColonyPowerPlantRow>(), Array.Empty<ColonyPowerTierRow>(), Array.Empty<ColonyPowerFuelRow>())
            { LifeSupportRateLabel = "Garden world: 0.05 kW per person" };
        Assert.That(LifeSupportRows.ColonyLine(power), Is.EqualTo("Life support " + Stringify.Power(46) + ": Garden world: 0.05 kW per person"));
        Assert.That(LifeSupportRows.ColonyLine(power with { LifeSupportRateLabel = "" }), Is.Empty);
    }

    static FactionSnapshot Faction(decimal funds) => new("Player", "PLR", funds, Array.Empty<LedgerEntrySnapshot>());

    [Test]
    public void Payroll_RunningCosts_AndDaysUntilBroke()
    {
        var f = Faction(10_000m) with { CrewOnPayroll = 1234, CrewWagePerDay = 2m, PayrollPerDay = 2468m, LabCostsPerDay = 32m, RunningCostsPerDay = 2500m, DaysUntilBroke = 4.0 };
        Assert.That(Finances.PayrollText(f, Inv), Is.EqualTo("1,234 crew x ¤2.00 = ¤2,468.00 a day"));
        Assert.That(Finances.RunningCostsText(f, Inv), Is.EqualTo("¤2,500.00 a day (wages ¤2,468.00, labs ¤32.00)"));
        Assert.That(Finances.PayrollText(f with { CrewWagePerDay = 0, PayrollPerDay = 0 }, Inv), Is.EqualTo("1,234 crew, no wages charged yet"));
        Assert.That(Finances.DaysUntilBrokeText(null), Is.EqualTo("∞"));
        Assert.That(Finances.DaysUntilBrokeText(0), Is.EqualTo("now"));
        Assert.That(Finances.DaysUntilBrokeText(1.5), Is.EqualTo("1 day"));
        Assert.That(Finances.DaysUntilBrokeText(412.9), Is.EqualTo("412 days"));
        Assert.That(Finances.LastWagesText(f, Inv), Is.EqualTo("none booked yet"));
        Assert.That(Finances.LastWagesText(f with { LastWages = -2000m, LastWagesDate = new DateTime(2026, 11, 13) }, Inv), Is.EqualTo("-¤2,000.00 on 2026-11-13"));
    }

    [Test]
    public void NegativeBalanceBanner_ListsBlockedSpending()
    {
        Assert.That(Finances.BannerText(Faction(5m), Inv), Is.Empty);
        var broke = Faction(-3000m) with { FundsNegative = true, SpendingBlocked = true, BlockedSpending = new[] { "Building", "Launches" } };
        Assert.That(Finances.BannerText(broke, Inv), Is.EqualTo("Funds negative: -¤3,000.00. Blocked: Building, Launches."));
        Assert.That(Finances.BannerText(broke with { BlockedSpending = Array.Empty<string>() }, Inv), Is.EqualTo("Funds negative: -¤3,000.00. Spending is blocked."));
        Assert.That(Finances.BannerText(Faction(-1m) with { FundsNegative = true }, Inv), Is.EqualTo("Funds negative: -¤1.00."));
    }

    [Test]
    public void TrainCrew_RefusedWithTheFundsReason()
    {
        var academy = new NavalAcademyClassView(7, 8, 24, 0, 50, Array.Empty<AcademyCohortView>())
            { CrewSeats = 40, CrewCourseLengthMonths = 6, FundsReason = FundsText.PausedFundsNegative };
        Assert.That(CrewActions.TrainRefusal(academy, 1000), Is.EqualTo("Paused: funds negative"));
        Assert.That(CrewActions.TrainRefusal(academy with { FundsReason = "" }, 1000), Is.Null);
    }
}
