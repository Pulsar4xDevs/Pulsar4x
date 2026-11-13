using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// One supply ring for a colony grid: delivered power against demand, in the entity card's
/// radial style. The colours and words match the Power tab's tier bars.
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

    /// <summary>Draws the ring at the cursor and reserves its space.</summary>
    public static void Draw(string id, ColonyPowerView power, Vector4 accentColor, float radius = 22f, float ringThickness = 4f)
    {
        double share = Share(power);
        var cursor = ImGui.GetCursorScreenPos();
        var center = new Vector2(cursor.X + radius + ringThickness, cursor.Y + radius + ringThickness);
        string tooltip = power.Demand_kW > 0
            ? $"Delivered {Stringify.Power(power.Delivered_kW)} of {Stringify.Power(power.Demand_kW)} demand, {WordsFor(share)}"
            : "Nothing draws power";
        RadialIndicator.Draw(ImGui.GetWindowDrawList(), center, radius, ringThickness,
            (float)share, "POWER", $"{share * 100:0}%", false, accentColor, ColourFor(share), tooltip);
        ImGui.InvisibleButton(id, new Vector2((radius + ringThickness) * 2f, (radius + ringThickness) * 2f + ImGui.GetTextLineHeight() + 2f));
    }
}
