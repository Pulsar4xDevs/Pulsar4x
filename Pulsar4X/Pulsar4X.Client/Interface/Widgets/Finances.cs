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
        string text = "Funds negative (" + Money(f.Funds, culture) + ").";
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

    /// <summary>Share of life support the plant and batteries can serve now, 0..1 (it is served first).</summary>
    public static double ShipShare(ShipPowerView p)
    {
        if (p.LifeSupport_kW <= 0)
            return 1;
        double supply = p.Generation_kW + (p.StoredFraction > 0 ? p.MaxDischarge_kW : 0);
        return Math.Clamp(supply / p.LifeSupport_kW, 0, 1);
    }

    /// <summary>The ship's life support load row, or null when ships draw no life support yet or a load group already lists it.</summary>
    public static PowerRow? ShipRow(ShipPowerView p, Vector4 chip, double scale_kW)
    {
        if (!p.HasLifeSupport || p.LoadGroups.Any(g => g.Kind == Kind))
            return null;
        double share = ShipShare(p);
        Vector4 colour = PowerRing.ColourFor(share);
        return new PowerRow
        {
            Id = "life-support",
            Chip = chip,
            Name = Kind,
            Value = Stringify.Power(p.LifeSupport_kW),
            Percent = PowerBars.Percent(p.LifeSupport_kW, scale_kW),
            Fill = (float)share,
            FillColour = colour,
            Trailing = (p.LifeSupportMustRun ? "must-run, " : "") + PowerRing.WordsFor(share),
            TrailingColour = colour,
            Tooltip = ShipTooltip(p),
            Detail = new List<(string, string)>
            {
                ("Crew aboard", p.LifeSupportCrew.ToString("N0")),
                ("Per crew", p.LifeSupportPerCrew_kW.ToString("0.##", CultureInfo.InvariantCulture) + " kW"),
                ("Draw", Stringify.Power(p.LifeSupport_kW)),
            },
        };
    }

    /// <summary>"Life support 46 kW: Garden world: 0.05 kW per person", or empty with no colonists.</summary>
    public static string ColonyLine(ColonyPowerView p)
        => p.LifeSupportRateLabel.Length == 0 ? "" : Kind + " " + Stringify.Power(p.LifeSupport_kW) + ": " + p.LifeSupportRateLabel;
}
