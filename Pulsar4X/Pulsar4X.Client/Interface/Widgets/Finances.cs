using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// The faction's running costs in the Transactions window (crew plan phase 5): the last monthly wage bill, the
/// payroll (crew on payroll x wage) and running costs per month, what is owed so far this month, the next payday
/// (wages and research accrue daily and are billed on the 1st), days until broke and, while funds are negative,
/// a red banner with what the block stops. <see cref="IClientGalaxy.Faction"/> is always the player's own
/// faction, so this never shows an NPC's books (NPCs pay no wages). Text helpers are pure for tests.
/// </summary>
public static class Finances
{
    public static readonly Vector4 BannerColour = new(1f, 0.35f, 0.35f, 1f);

    /// <summary>Coloured text without printf parsing (TextColored would read a '%' as a format).</summary>
    public static void ColouredText(Vector4 colour, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, colour);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    static string Money(decimal amount, CultureInfo? culture) => amount.ToString("C2", culture ?? CultureInfo.CurrentCulture);

    /// <summary>
    /// "1,234 crew x $2.00 a day = $74,040.00 a month" (this month's days); before wages are charged,
    /// "1,234 crew, no wages charged yet".
    /// </summary>
    public static string PayrollText(FactionSnapshot f, CultureInfo? culture = null)
        => f.CrewWagePerDay <= 0 && f.PayrollPerDay <= 0
            ? f.CrewOnPayroll.ToString("N0", culture) + " crew, no wages charged yet"
            : f.CrewOnPayroll.ToString("N0", culture) + " crew x " + Money(f.CrewWagePerDay, culture) + " a day = " + Money(f.PayrollPerMonth, culture) + " a month";

    /// <summary>"$75,000.00 a month (wages $74,040.00, staff $372,600.00, labs $960.00)".</summary>
    public static string RunningCostsText(FactionSnapshot f, CultureInfo? culture = null)
        => Money(f.RunningCostsPerMonth, culture) + " a month (wages " + Money(f.PayrollPerMonth, culture)
            + ", staff " + Money(f.StaffWagesPerMonth, culture) + ", labs " + Money(f.LabCostsPerMonth, culture) + ")";

    /// <summary>"$12,000.00 (wages $10,000.00, staff $0.00, research $2,000.00)", or "nothing yet".</summary>
    public static string AccruedText(FactionSnapshot f, CultureInfo? culture = null)
        => f.AccruedThisMonth <= 0
            ? "nothing yet"
            : Money(f.AccruedThisMonth, culture) + " (wages " + Money(f.AccruedWagesThisMonth, culture)
                + ", staff " + Money(f.AccruedStaffWagesThisMonth, culture)
                + ", research " + Money(f.AccruedResearchThisMonth, culture) + ")";

    /// <summary>"6,210 staff x $2.00 a day = $372,600.00 a month", or "6,210 staff, no wages charged yet".</summary>
    public static string StaffWagesText(FactionSnapshot f, CultureInfo? culture = null)
    {
        long n = f.StaffOnPayroll + f.CommutersOnPayroll;
        string who = f.CommutersOnPayroll > 0
            ? n.ToString("N0", culture) + " staff (" + f.StaffOnPayroll.ToString("N0", culture) + " own, "
                + f.CommutersOnPayroll.ToString("N0", culture) + " commuters)"
            : n.ToString("N0", culture) + " staff";
        return f.StaffWagePerDay <= 0 && f.StaffWagesPerDay <= 0
            ? who + ", no wages charged yet"
            : who + " x " + Money(f.StaffWagePerDay, culture) + " a day = " + Money(f.StaffWagesPerMonth, culture) + " a month";
    }

    /// <summary>"$2,000.00 a month" (bloc tax; 0 for the company).</summary>
    public static string TaxIncomeText(FactionSnapshot f, CultureInfo? culture = null)
        => Money(f.TaxIncomePerMonth, culture) + " a month";

    /// <summary>"$9,000.00" (three months of running costs a bloc keeps back before it bids; 0 when it doesn't bid).</summary>
    public static string BidReserveText(FactionSnapshot f, CultureInfo? culture = null)
        => Money(f.BidReserve, culture);

    /// <summary>"2050-02-01", or "-" when unknown.</summary>
    public static string NextPaydayText(FactionSnapshot f)
        => DateDisplay.FormatDate(f.NextPayday, "-");

    /// <summary>A daily rate as a month's: "$3,000.00 a month" (30 days unless given).</summary>
    public static string PerMonthText(decimal perDay, int daysInMonth = 30, CultureInfo? culture = null)
        => Money(perDay * Math.Max(1, daysInMonth), culture) + " a month";

    /// <summary>"∞" when nothing costs anything, "now" at 0 (funds gone), else "412 days" / "1 day".</summary>
    public static string DaysUntilBrokeText(double? days)
    {
        if (days is not { } d || double.IsInfinity(d) || double.IsNaN(d))
            return "∞";
        if (d <= 0)
            return "now";
        int whole = (int)Math.Floor(Math.Min(d, int.MaxValue));
        return whole == 1 ? "1 day" : whole.ToString("N0") + " days";
    }

    /// <summary>The last Wages bill (booked on the 1st for the month before): "-$2,000.00 on 2026-11-01", or "none booked yet".</summary>
    public static string LastWagesText(FactionSnapshot f, CultureInfo? culture = null)
        => f.LastWagesDate is { } date ? Money(f.LastWages, culture) + " on " + DateDisplay.FormatDate(date) : "none booked yet";

    /// <summary>The negative-balance banner, or empty while funds are 0 or more.</summary>
    public static string BannerText(FactionSnapshot f, CultureInfo? culture = null)
    {
        if (!f.FundsNegative && f.Funds >= 0)
            return "";
        // No brackets of our own: some cultures already write negative money as "($3,250.00)".
        string text = "Funds negative: " + Money(f.Funds, culture) + ".";
        if (f.BlockedSpending.Count > 0)
            text += " Blocked: " + string.Join(", ", f.BlockedSpending) + ".";
        else if (f.SpendingBlocked)
            text += " Spending is blocked.";
        return text;
    }

    /// <summary>The summary block above the ledger rows.</summary>
    public static void Draw(FactionSnapshot f)
    {
        string banner = BannerText(f);
        if (banner.Length > 0)
        {
            var p = ImGui.GetCursorScreenPos();
            float w = ImGui.GetContentRegionAvail().X;
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + w - 12f);
            var size = ImGui.CalcTextSize(banner, w - 12f);
            var draw = ImGui.GetWindowDrawList();
            var max = new Vector2(p.X + w, p.Y + size.Y + 8f);
            draw.AddRectFilled(p, max, ImGui.GetColorU32(new Vector4(BannerColour.X, BannerColour.Y, BannerColour.Z, 0.2f)), 4f);
            draw.AddRect(p, max, ImGui.GetColorU32(BannerColour), 4f);
            ImGui.SetCursorScreenPos(new Vector2(p.X + 6f, p.Y + 4f));
            ImGui.PushStyleColor(ImGuiCol.Text, BannerColour);
            ImGui.TextUnformatted(banner);
            ImGui.PopStyleColor();
            ImGui.PopTextWrapPos();
            ImGui.SetCursorScreenPos(new Vector2(p.X, max.Y + 4f));
            ImGui.Dummy(new Vector2(0, 0));
        }

        if (ImGui.BeginTable("##finances", 2, ImGuiTableFlags.SizingFixedFit))
        {
            Row("Last wage bill", LastWagesText(f), "The last monthly crew pay line (ledger category " + FactionSnapshot.WagesCategory + "), booked on the 1st for the month before.");
            Row("Payroll", PayrollText(f), "People aboard the faction's ships (crew and officers) plus trained crew in its colony pools. Academy students are not paid.\nWages accrue daily and are billed once, on the 1st of each month.");
            Row("Staff wages", StaffWagesText(f), "Own staff and commuters, at the staff wage (default 2 cr a day). Commuter wages are paid into their bloc's funds.\nBilled on the 1st with crew wages (ledger category " + FactionSnapshot.StaffWagesCategory + ").");
            Row("Tax income", TaxIncomeText(f), "Bloc tax: residents × tax per person per month. 0 for the company.");
            Row("Bid reserve", BidReserveText(f), "Funds a bloc keeps back before it bids: three months of running costs. 0 for factions that do not bid.");
            Row("Running costs", RunningCostsText(f), "Per month (" + f.DaysInMonth + " days this month): crew payroll, staff wages and the research labs that research today. Income is not forecast.");
            Row("Owed this month", AccruedText(f), "Crew wages, staff wages and research accrued so far this month, not yet billed. Labs can only spend funds less this.");
            Row("Next payday", NextPaydayText(f), "The month's wages and research are billed on the 1st. If funds can't cover the bill, the balance goes negative and spending is blocked.");
            Row("Days until broke", DaysUntilBrokeText(f.DaysUntilBroke), "(Funds - owed this month) / daily running costs, income not counted.");
            ImGui.EndTable();
        }
    }

    static void Row(string name, string value, string tip)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextColored(Styles.DescriptiveColor, name);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(value);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tip);
    }
}

/// <summary>Life support rows and lines for the ship and colony Power tabs (crew plan phase 4).</summary>
public static class LifeSupportRows
{
    public const string Kind = "Life support";

    /// <summary>"0.80 kW per person x 12 people = 9.6 kW", plus the tech's effect when it lowers the 1 kW base.</summary>
    public static string ShipTooltip(ShipPowerView p)
    {
        string text = Kind + ": " + p.LifeSupportPerCrew_kW.ToString("0.##", CultureInfo.InvariantCulture) + " kW per person x "
            + p.LifeSupportCrew.ToString("N0") + " people = " + Stringify.Power(p.LifeSupport_kW) + ".";
        text += p.LifeSupportPerCrew_kW > 0 && p.LifeSupportPerCrew_kW < 1
            ? "\nLife-support efficiency tech: " + (1 - p.LifeSupportPerCrew_kW).ToString("0%", CultureInfo.InvariantCulture) + " less than the 1 kW per person base."
            : "\nBase 1 kW per person; life-support efficiency tech lowers it.";
        if (p.LifeSupportMustRun)
            text += "\nMust-run: served before warp and charging, and counted in the warp check.";
        return text;
    }

    /// <summary>The engine's row id for crew life support inside its load group.</summary>
    public const string RowId = "life-support";

    /// <summary>True for the engine's life-support load group (by kind, or by its "life-support" row).</summary>
    public static bool IsLifeSupportGroup(ShipPowerLoadGroup g)
        => g.Kind == Kind || g.Loads.Any(l => l.DesignId == RowId);

    /// <summary>The load groups other than life support, which <see cref="ShipRow"/> lists on its own.</summary>
    public static IEnumerable<ShipPowerLoadGroup> OtherGroups(ShipPowerView p)
        => p.LoadGroups.Where(g => !IsLifeSupportGroup(g));

    /// <summary>Life support demand, kW: the view's figure, or the engine group's when only that is set. 0 without one.</summary>
    public static double Demand_kW(ShipPowerView p)
    {
        if (p.LifeSupport_kW > 0)
            return p.LifeSupport_kW;
        return p.LoadGroups.FirstOrDefault(IsLifeSupportGroup)?.Demand_kW ?? 0;
    }

    /// <summary>
    /// What the Loads shares are taken of: <see cref="ShipPowerView.TotalDemand_kW"/> (which leaves life
    /// support out) plus life support, so the rows' percentages add up to 100% and the bar's slices to its total.
    /// </summary>
    public static double LoadScale_kW(ShipPowerView p) => p.TotalDemand_kW + Demand_kW(p);

    /// <summary>
    /// The ship's one life support row, or null when there is no life support load. It stands in for the
    /// engine's life-support load group, which the caller leaves out (<see cref="OtherGroups"/>). The share
    /// and served power are the engine's (<see cref="ShipPowerView.LifeSupportServedFraction"/>).
    /// </summary>
    public static PowerRow? ShipRow(ShipPowerView p, Vector4 chip, double scale_kW)
    {
        double demand = Demand_kW(p);
        bool group = p.LoadGroups.Any(IsLifeSupportGroup);
        if (!group && (!p.HasLifeSupport || demand <= 0))
            return null;
        double share = Math.Clamp(p.LifeSupportServedFraction, 0, 1);
        Vector4 colour = PowerRing.ColourFor(share);
        return new PowerRow
        {
            Id = RowId,
            Chip = chip,
            Name = Kind,
            Value = Stringify.Power(demand),
            Percent = PowerBars.Percent(demand, scale_kW),
            Fill = (float)share,
            FillColour = colour,
            Trailing = (p.LifeSupportMustRun ? "must-run, " : "") + PowerRing.WordsFor(share),
            TrailingColour = colour,
            Tooltip = ShipTooltip(p) + "\nPowered now: " + Stringify.Power(p.LifeSupportServed_kW) + " of " + Stringify.Power(demand)
                + " (" + PowerBars.Percent(share, 1) + ").",
            Detail = new List<(string, string)>
            {
                ("People aboard", p.LifeSupportCrew.ToString("N0")),
                ("Per person", p.LifeSupportPerCrew_kW.ToString("0.##", CultureInfo.InvariantCulture) + " kW"),
                ("Draw", Stringify.Power(demand)),
                ("Powered", Stringify.Power(p.LifeSupportServed_kW)),
            },
        };
    }

    /// <summary>"Life support 46 kW: Garden world: 0.05 kW per person", or empty with no colonists.</summary>
    public static string ColonyLine(ColonyPowerView p)
        => p.LifeSupportRateLabel.Length == 0 ? "" : Kind + " " + Stringify.Power(p.LifeSupport_kW) + ": " + p.LifeSupportRateLabel;
}
