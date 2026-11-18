using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
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

    static ShipPowerView ShipPower(bool has, int crew = 12, double perCrew = 0.8, double gen = 20, double served = 1)
        => new ShipPowerView(gen, 5, 0, 0, true) with
        {
            HasLifeSupport = has,
            LifeSupportCrew = crew,
            LifeSupportPerCrew_kW = perCrew,
            LifeSupport_kW = has ? crew * perCrew : 0,
            LifeSupportServedFraction = served,
            LifeSupportServed_kW = has ? crew * perCrew * served : 0,
        };

    static ShipPowerLoadGroup LifeGroup(double kW, double share)
        => new("Life support", kW, kW * share, share, share >= 1 ? "Powered" : "Partial",
            new[] { new ShipPowerLoadRow("life-support", "Crew life support", 12, kW, "0.8 kW per crew aboard; must run") });

    static ShipPowerLoadGroup WarpGroup(double kW)
        => new("Engines and warp", kW, kW, 1, kW > 0 ? "Powered" : "Idle", Array.Empty<ShipPowerLoadRow>());

    [Test]
    public void ShipLifeSupportRow_HiddenWithoutLifeSupport()
    {
        Assert.That(LifeSupportRows.ShipRow(ShipPower(false), Vector4.One, 10), Is.Null);
        Assert.That(LifeSupportRows.ShipRow(ShipPower(true, crew: 0), Vector4.One, 10), Is.Null, "nobody aboard, no load, no engine group");
        var row = LifeSupportRows.ShipRow(ShipPower(true), Vector4.One, 14.6);
        Assert.That(row, Is.Not.Null);
        Assert.That(row!.Id, Is.EqualTo("life-support"));
        Assert.That(row.Name, Is.EqualTo("Life support"));
        Assert.That(row.Value, Is.EqualTo(Stringify.Power(9.6)));
        Assert.That(row.Fill, Is.EqualTo(1f));
        Assert.That(row.Trailing, Does.StartWith("must-run"));
        Assert.That(row.Tooltip, Does.Contain("0.8 kW per person x 12 people"));
        Assert.That(row.Tooltip, Does.Contain("20% less than the 1 kW per person base"));
        Assert.That(row.Tooltip, Does.Contain("Must-run"));
    }

    [Test]
    public void ShipLifeSupportRow_UsesTheEnginesServedFraction_NotGeneration()
    {
        // 50 kW generation would cover 10 kW, but the engine says only 40% is served (warp shares the must-run supply).
        var row = LifeSupportRows.ShipRow(ShipPower(true, crew: 10, perCrew: 1, gen: 50, served: 0.4), Vector4.One, 15)!;
        Assert.That(row.Fill, Is.EqualTo(0.4f));
        Assert.That(row.Tooltip, Does.Contain("Powered now: " + Stringify.Power(4) + " of " + Stringify.Power(10) + " (40%)"));
        Assert.That(row.Detail.Single(d => d.Name == "Powered").Value, Is.EqualTo(Stringify.Power(4)));
        Assert.That(row.Tooltip, Does.Contain("Base 1 kW per person"));
    }

    [Test]
    public void ShipLifeSupport_ListedOnce_WhenTheEngineHasALifeSupportGroup()
    {
        var power = ShipPower(true) with { LoadGroups = new[] { LifeGroup(9.6, 1), WarpGroup(5) } };
        Assert.That(LifeSupportRows.ShipRow(power, Vector4.One, LifeSupportRows.LoadScale_kW(power)), Is.Not.Null, "the group becomes the must-run row");
        Assert.That(LifeSupportRows.OtherGroups(power).Select(g => g.Kind), Is.EqualTo(new[] { "Engines and warp" }),
            "and leaves the generic loads list");

        // Found by its row id too, whatever the group is called.
        var renamed = power with { LoadGroups = new[] { LifeGroup(9.6, 1) with { Kind = "Crew" }, WarpGroup(5) } };
        Assert.That(LifeSupportRows.OtherGroups(renamed).Select(g => g.Kind), Is.EqualTo(new[] { "Engines and warp" }));
    }

    [Test]
    public void ShipLoadShares_AreOfDemandPlusLifeSupport()
    {
        // TotalDemand_kW (5 warp + 0 aux) leaves life support (9.6) out; the shares are of 14.6 and add to 100%.
        var power = ShipPower(true) with { LoadGroups = new[] { LifeGroup(9.6, 1), WarpGroup(5) } };
        Assert.That(power.TotalDemand_kW, Is.EqualTo(5));
        double scale = LifeSupportRows.LoadScale_kW(power);
        Assert.That(scale, Is.EqualTo(14.6).Within(1e-9));
        Assert.That(LifeSupportRows.ShipRow(power, Vector4.One, scale)!.Percent, Is.EqualTo(PowerBars.Percent(9.6, 14.6)));

        // The engine group's demand stands in when the view's life-support figure is 0.
        var groupOnly = power with { LifeSupport_kW = 0 };
        Assert.That(LifeSupportRows.Demand_kW(groupOnly), Is.EqualTo(9.6));
        Assert.That(LifeSupportRows.LoadScale_kW(ShipPower(false)), Is.EqualTo(5));
    }

    // Prospector I at game start, as the engine projects it: 17 kW life support served in full, no warp,
    // 55 W of sensors waiting while ~18 kW charges the batteries.
    static ShipPowerView StartShip()
        => new ShipPowerView(36.934, 0, 0, 3_000_000, true) with
        {
            AuxDemand_kW = 0.055,
            AuxServed_kW = 0,
            Charging_kW = 17.941,
            HasLifeSupport = true,
            LifeSupportCrew = 17,
            LifeSupportPerCrew_kW = 1,
            LifeSupport_kW = 17,
            LifeSupportServed_kW = 17,
            LifeSupportServedFraction = 1,
            LoadGroups = new[] { LifeGroup(17, 1), WarpGroup(0) },
        };

    [Test]
    public void ShipPowerTotals_IncludeLifeSupport_LikeTheRows()
    {
        var p = StartShip();
        Assert.That(p.Delivered_kW, Is.EqualTo(0), "the Api's delivered leaves life support out");
        Assert.That(ShipPowerTotals.Demand_kW(p), Is.EqualTo(17.055).Within(1e-9));
        Assert.That(ShipPowerTotals.Served_kW(p), Is.EqualTo(17).Within(1e-9));
        Assert.That(ShipPowerTotals.Share(p), Is.EqualTo(17 / 17.055).Within(1e-9));
        Assert.That(ShipPowerTotals.Demand_kW(p), Is.EqualTo(LifeSupportRows.LoadScale_kW(p)), "summary demand = the rows' total");
        Assert.That(p.LoadsWaitOnCharge, Is.True, "the sensors still wait on the charge, so the header stays 'charging batteries'");
        Assert.That(ShipPowerTotals.GenerationShare(p), Is.EqualTo(1));
    }

    [Test]
    public void ShipPowerTotals_WarpSharesTheMustRunSupply_AndLifeSupportCountsOnce()
    {
        // 10 kW warp + 10 kW life support on 10 kW: the engine serves both at 50%.
        var p = new ShipPowerView(10, 10, 0, 0, true) with
        {
            HasLifeSupport = true, LifeSupport_kW = 10, LifeSupportServed_kW = 5, LifeSupportServedFraction = 0.5,
            LoadGroups = new[] { LifeGroup(10, 0.5), WarpGroup(10) },
        };
        Assert.That(ShipPowerTotals.OtherServed_kW(p), Is.EqualTo(5));
        Assert.That(ShipPowerTotals.Served_kW(p), Is.EqualTo(10));
        Assert.That(ShipPowerTotals.Share(p), Is.EqualTo(0.5));
        Assert.That(ShipPowerTotals.GenerationShare(p), Is.EqualTo(0.5));
        // Read from its parts, not from Delivered_kW (min(warp, generation) = 10 here), so life support is not counted twice.
        Assert.That(p.Delivered_kW, Is.EqualTo(10));

        // Served never runs above demand, whatever the served figure says.
        var over = p with { LifeSupportServed_kW = 50 };
        Assert.That(ShipPowerTotals.LifeSupportServed_kW(over), Is.EqualTo(10));
        Assert.That(ShipPowerTotals.Served_kW(over), Is.LessThanOrEqualTo(ShipPowerTotals.Demand_kW(over)));
    }

    [Test]
    public void ShipPowerTotals_WithoutLifeSupport_MatchTheApi()
    {
        var p = new ShipPowerView(6, 8, 0, 0, true) with { AuxDemand_kW = 2, AuxServed_kW = 0 };
        Assert.That(ShipPowerTotals.Demand_kW(p), Is.EqualTo(p.TotalDemand_kW));
        Assert.That(ShipPowerTotals.Served_kW(p), Is.EqualTo(p.Delivered_kW));
        Assert.That(ShipPowerTotals.Share(p), Is.EqualTo(p.SupplyShare));
        Assert.That(ShipPowerTotals.GenerationShare(p), Is.EqualTo(p.Share));
        Assert.That(ShipPowerTotals.Share(new ShipPowerView(0, 0, 0, 0, false)), Is.EqualTo(1));
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
        var f = Faction(10_000m) with
        {
            CrewOnPayroll = 1234, CrewWagePerDay = 2m, PayrollPerDay = 2468m, LabCostsPerDay = 32m, RunningCostsPerDay = 2500m, DaysUntilBroke = 4.0,
            DaysInMonth = 30, PayrollPerMonth = 74_040m, LabCostsPerMonth = 960m, RunningCostsPerMonth = 75_000m,
        };
        Assert.That(Finances.PayrollText(f, Inv), Is.EqualTo("1,234 crew x ¤2.00 a day = ¤74,040.00 a month"));
        Assert.That(Finances.RunningCostsText(f, Inv), Is.EqualTo("¤75,000.00 a month (wages ¤74,040.00, staff ¤0.00, labs ¤960.00)"));
        Assert.That(Finances.PayrollText(f with { CrewWagePerDay = 0, PayrollPerDay = 0 }, Inv), Is.EqualTo("1,234 crew, no wages charged yet"));
        Assert.That(Finances.DaysUntilBrokeText(null), Is.EqualTo("∞"));
        Assert.That(Finances.DaysUntilBrokeText(0), Is.EqualTo("now"));
        Assert.That(Finances.DaysUntilBrokeText(1.5), Is.EqualTo("1 day"));
        Assert.That(Finances.DaysUntilBrokeText(412.9), Is.EqualTo("412 days"));
        Assert.That(Finances.LastWagesText(f, Inv), Is.EqualTo("none booked yet"));
        Assert.That(Finances.LastWagesText(f with { LastWages = -2000m, LastWagesDate = new DateTime(2026, 11, 13) }, Inv), Is.EqualTo("-¤2,000.00 on 13 Nov 2026"), "the default date format");
    }

    [Test]
    public void MonthlyBilling_OwedThisMonth_NextPayday_AndPerMonthRates()
    {
        var f = Faction(10_000m);
        Assert.That(Finances.AccruedText(f, Inv), Is.EqualTo("nothing yet"));
        Assert.That(Finances.NextPaydayText(f), Is.EqualTo("-"));
        var owing = f with { AccruedThisMonth = 12_000m, AccruedWagesThisMonth = 10_000m, AccruedResearchThisMonth = 2_000m, NextPayday = new DateTime(2050, 2, 1) };
        Assert.That(Finances.AccruedText(owing, Inv), Is.EqualTo("¤12,000.00 (wages ¤10,000.00, staff ¤0.00, research ¤2,000.00)"));
        Assert.That(Finances.NextPaydayText(owing), Is.EqualTo("1 Feb 2050"));
        Assert.That(Finances.PerMonthText(100m, 31, Inv), Is.EqualTo("¤3,100.00 a month"));
        Assert.That(LedgerWindow.CategoryLabel(FactionSnapshot.WagesCategory), Is.EqualTo("Wages (monthly)"));
        Assert.That(LedgerWindow.CategoryLabel(FactionSnapshot.StaffWagesCategory), Is.EqualTo("Staff wages (monthly)"));
        Assert.That(LedgerWindow.CategoryLabel(FactionSnapshot.TaxCategory), Is.EqualTo("Tax"));
        Assert.That(LedgerWindow.CategoryLabel("Research"), Is.EqualTo("Research (monthly)"));
        Assert.That(LedgerWindow.CategoryLabel("Trade"), Is.EqualTo("Trade"));
    }

    [Test]
    public void CandidateWage_AndLabCost_ShowPerMonth()
    {
        var candidate = new CandidateView(1, "Ada", 30, 2, "Earth", 100, "Physics", 3) { WagePerMonth = 3_100 };
        Assert.That(ColonyManagementWindow.CandidateWageText(candidate), Does.EndWith(" a month").And.Contain("100"));
        Assert.That(ColonyManagementWindow.CandidateWageText(candidate with { WagePerMonth = 0 }), Does.EndWith(" a day"));
        var lab = new ResearcherView("Lab", "research-lab", "", 1, "Earth", null, null,
            new ModifiedValue(10_000, 10_000, Array.Empty<ValueModifier>()), new ModifiedValue(5, 5, Array.Empty<ValueModifier>()), 1, Array.Empty<string>())
            { DaysInMonth = 31, CostPerMonth = 310_000m, NextPayday = new DateTime(2050, 2, 1) };
        string tip = ResearchWindow.LabCostTooltip(lab);
        Assert.That(tip, Does.Contain("31 days this month").And.Contain("1 Feb 2050"));
    }

    [Test]
    public void StaffWages_TaxIncome_AndBidReserve()
    {
        var f = Faction(10_000m) with
        {
            StaffOnPayroll = 5210, CommutersOnPayroll = 1000, StaffWagePerDay = 2m, StaffWagesPerDay = 12_420m,
            DaysInMonth = 30, StaffWagesPerMonth = 372_600m, AccruedStaffWagesThisMonth = 8_000m,
            AccruedThisMonth = 8_000m, TaxIncomePerMonth = 2_000_000m, BidReserve = 9_000m,
        };
        Assert.That(Finances.StaffWagesText(f, Inv), Is.EqualTo("6,210 staff (5,210 own, 1,000 commuters) x ¤2.00 a day = ¤372,600.00 a month"));
        Assert.That(Finances.StaffWagesText(f with { StaffWagePerDay = 0, StaffWagesPerDay = 0, CommutersOnPayroll = 0, StaffOnPayroll = 6210 }, Inv),
            Is.EqualTo("6,210 staff, no wages charged yet"));
        Assert.That(Finances.TaxIncomeText(f, Inv), Is.EqualTo("¤2,000,000.00 a month"));
        Assert.That(Finances.BidReserveText(f, Inv), Is.EqualTo("¤9,000.00"));
        Assert.That(Finances.TaxIncomeText(Faction(1m), Inv), Is.EqualTo("¤0.00 a month"));
        Assert.That(Finances.RunningCostsText(f with { PayrollPerMonth = 74_040m, LabCostsPerMonth = 960m, RunningCostsPerMonth = 447_600m }, Inv),
            Is.EqualTo("¤447,600.00 a month (wages ¤74,040.00, staff ¤372,600.00, labs ¤960.00)"));
        Assert.That(Finances.AccruedText(f, Inv), Is.EqualTo("¤8,000.00 (wages ¤0.00, staff ¤8,000.00, research ¤0.00)"));
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
