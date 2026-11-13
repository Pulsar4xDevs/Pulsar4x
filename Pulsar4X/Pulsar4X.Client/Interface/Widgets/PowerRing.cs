using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// One supply ring: a colony grid's delivered power against demand, or a ship's generation against
/// its current demand, in the entity card's radial style. The colours and words match the Power
/// tab's tier bars. Anything that draws nothing shows full.
/// </summary>
public static class PowerRing
{
    public static readonly Vector4 FullColour = new(0.4f, 0.9f, 0.4f, 1f);
    public static readonly Vector4 PartialColour = new(1f, 0.75f, 0.1f, 1f);
    public static readonly Vector4 NoneColour = new(0.9f, 0.2f, 0.2f, 1f);

    public static Vector4 ColourFor(double share)
        => share >= 1 ? FullColour : share <= 0 ? NoneColour : PartialColour;

    /// <summary>"fully powered", "running at 60%" or "no power". Plain text, not a printf format.</summary>
    public static string WordsFor(double share)
        => share >= 1 ? "fully powered" : share <= 0 ? "no power" : $"running at {share * 100:0}%";

    /// <summary>Delivered over demand, 0..1. A grid with no demand counts as fully powered.</summary>
    public static double Share(ColonyPowerView power)
        => power.Demand_kW > 0 ? Math.Clamp(power.Delivered_kW / power.Demand_kW, 0, 1) : 1;

    /// <summary>Draws a colony's ring at the cursor and reserves its space.</summary>
    public static void Draw(string id, ColonyPowerView power, Vector4 accentColor, float radius = 22f, float ringThickness = 4f)
    {
        double share = Share(power);
        var cursor = ImGui.GetCursorScreenPos();
        var center = new Vector2(cursor.X + radius + ringThickness, cursor.Y + radius + ringThickness);
        string tooltip = power.Demand_kW > 0
            ? $"Delivered {Stringify.Power(power.Delivered_kW)} of {Stringify.Power(power.Demand_kW)} demand, {WordsFor(share)}"
            : "Nothing draws power";
        DrawAt(ImGui.GetWindowDrawList(), center, radius, ringThickness, share, accentColor, tooltip);
        ImGui.InvisibleButton(id, new Vector2((radius + ringThickness) * 2f, (radius + ringThickness) * 2f + ImGui.GetTextLineHeight() + 2f));
    }

    /// <summary>
    /// Draws a ship's Power-tab ring at the cursor and reserves its space: delivered over all demand
    /// (warp and other loads), amber while the batteries charge and the other loads wait for them.
    /// </summary>
    public static void Draw(string id, ShipPowerView power, Vector4 accentColor, float radius = 22f, float ringThickness = 4f)
    {
        bool charging = power.LoadsWaitOnCharge;
        double share = power.SupplyShare;
        var cursor = ImGui.GetCursorScreenPos();
        var center = new Vector2(cursor.X + radius + ringThickness, cursor.Y + radius + ringThickness);
        string tooltip = charging
            ? $"{Stringify.Power(power.Charging_kW)} is charging the batteries; {Stringify.Power(power.AuxDemand_kW)} of other loads wait until they are full"
            : power.TotalDemand_kW > 0
                ? $"Delivered {Stringify.Power(power.Delivered_kW)} of {Stringify.Power(power.TotalDemand_kW)} demand, {WordsFor(share)}"
                : power.HasPlant ? "Nothing draws power" : "No power plant. Nothing draws power";
        RadialIndicator.Draw(ImGui.GetWindowDrawList(), center, radius, ringThickness, (float)share,
            "POWER", $"{share * 100:0}%", false, accentColor, charging ? PartialColour : ColourFor(share), tooltip);
        ImGui.InvisibleButton(id, new Vector2((radius + ringThickness) * 2f, (radius + ringThickness) * 2f + ImGui.GetTextLineHeight() + 2f));
    }

    /// <summary>Hover text for a ship's ring.</summary>
    public static string TooltipFor(ShipPowerView power)
    {
        if (!power.HasPlant)
            return "No power plant. Nothing draws power";
        if (power.Demand_kW <= 0)
            return $"Generation {Stringify.Power(power.Generation_kW)}. Nothing draws power";
        string text = $"Generation {Stringify.Power(power.Generation_kW)} of {Stringify.Power(power.Demand_kW)} demand, {WordsFor(power.Share)}";
        if (power.Share < 1 && power.Stored_kJ > 0)
            text += $"\nBatteries cover the gap while they last ({Stringify.Energy(power.Stored_kJ)} stored)";
        return text;
    }

    /// <summary>Draws a ring centred on a point, without reserving space (for a row of gauges).</summary>
    public static void DrawAt(ImDrawListPtr drawList, Vector2 center, float radius, float ringThickness,
        double share, Vector4 accentColor, string tooltip)
    {
        RadialIndicator.Draw(drawList, center, radius, ringThickness,
            (float)share, "POWER", $"{share * 100:0}%", false, accentColor, ColourFor(share), tooltip);
    }
}
