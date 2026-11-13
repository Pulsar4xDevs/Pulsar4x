using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// The faction's running costs in the Transactions window (crew plan phase 5): wages, the daily payroll
/// (crew on payroll x wage), running costs per day, days until broke and, while funds are negative, a red
/// banner with what the block stops. <see cref="IClientGalaxy.Faction"/> is always the player's own
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

    /// <summary>"1,234 crew x $2.00 = $2,468.00 a day"; before wages are charged, "1,234 crew, no wages charged yet".</summary>
    public static string PayrollText(FactionSnapshot f, CultureInfo? culture = null)
        => f.CrewWagePerDay <= 0 && f.PayrollPerDay <= 0
            ? f.CrewOnPayroll.ToString("N0", culture) + " crew, no wages charged yet"
            : f.CrewOnPayroll.ToString("N0", culture) + " crew x " + Money(f.CrewWagePerDay, culture) + " = " + Money(f.PayrollPerDay, culture) + " a day";

    /// <summary>"$2,500.00 a day (wages $2,468.00, labs $32.00)".</summary>
    public static string RunningCostsText(FactionSnapshot f, CultureInfo? culture = null)
        => Money(f.RunningCostsPerDay, culture) + " a day (wages " + Money(f.PayrollPerDay, culture) + ", labs " + Money(f.LabCostsPerDay, culture) + ")";

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

    /// <summary>The last day's Wages lines: "-$2,000.00 on 2026-11-13", or "none booked yet".</summary>
    public static string LastWagesText(FactionSnapshot f, CultureInfo? culture = null)
        => f.LastWagesDate is { } date ? Money(f.LastWages, culture) + " on " + date.ToString("yyyy-MM-dd") : "none booked yet";

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
            Row("Wages", LastWagesText(f), "The last day's crew pay lines (ledger category " + FactionSnapshot.WagesCategory + ").");
            Row("Payroll", PayrollText(f), "Crew aboard the faction's ships plus trained crew in its colony pools. Academy students are not paid.");
            Row("Running costs", RunningCostsText(f), "Daily costs: crew payroll plus the research labs that research today. Income is not forecast.");
            Row("Days until broke", DaysUntilBrokeText(f.DaysUntilBroke), "Funds / running costs, income not counted.");
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

    /// <summary>"0.80 kW per crew x 12 crew = 9.6 kW", plus the tech's effect when it lowers the 1 kW base.</summary>
    public static string ShipTooltip(ShipPowerView p)
    {
        string text = Kind + ": " + p.LifeSupportPerCrew_kW.ToString("0.##", CultureInfo.InvariantCulture) + " kW per crew x "
            + p.LifeSupportCrew.ToString("N0") + " crew = " + Stringify.Power(p.LifeSupport_kW) + ".";
        text += p.LifeSupportPerCrew_kW > 0 && p.LifeSupportPerCrew_kW < 1
            ? "\nLife-support efficiency tech: " + (1 - p.LifeSupportPerCrew_kW).ToString("0%", CultureInfo.InvariantCulture) + " less than the 1 kW per crew base."
            : "\nBase 1 kW per crew; life-support efficiency tech lowers it.";
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
                ("Crew aboard", p.LifeSupportCrew.ToString("N0")),
                ("Per crew", p.LifeSupportPerCrew_kW.ToString("0.##", CultureInfo.InvariantCulture) + " kW"),
                ("Draw", Stringify.Power(demand)),
                ("Powered", Stringify.Power(p.LifeSupportServed_kW)),
            },
        };
    }

    /// <summary>"Life support 46 kW: Garden world: 0.05 kW per person", or empty with no colonists.</summary>
    public static string ColonyLine(ColonyPowerView p)
        => p.LifeSupportRateLabel.Length == 0 ? "" : Kind + " " + Stringify.Power(p.LifeSupport_kW) + ": " + p.LifeSupportRateLabel;
}
