using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Client;

public enum StatDirection
{
    LowerIsBetter,
    HigherIsBetter,
    Neutral,
}

public enum StatChange
{
    Unchanged,
    Better,
    Worse,
    Neutral,
}

public readonly record struct StatDelta(
    string Name,
    double OldValue,
    double NewValue,
    StatDirection Direction,
    StatChange Change,
    string? Unit = null)
{
    public double Delta => NewValue - OldValue;

    public string FormatSignedDelta()
        => ComponentUpgradeHelper.FormatSignedDelta(Delta, Unit);
}

/// <summary>
/// Pure helpers for component-design upgrade indicators: classify a stat, compare values with
/// a relative tolerance, and format signed deltas for tooltips.
/// </summary>
public static class ComponentUpgradeHelper
{
    public static double Tolerance(double oldVal)
        => Math.Max(1e-9, Math.Abs(oldVal) * 1e-6);

    public static bool IsChanged(double oldVal, double newVal)
        => Math.Abs(newVal - oldVal) > Tolerance(oldVal);

    public static StatDirection ClassifyStat(string? name, bool isResourceCost = false)
    {
        if (isResourceCost)
            return StatDirection.LowerIsBetter;

        string key = (name ?? "").Trim();
        if (key.Length == 0)
            return StatDirection.Neutral;
        if (LowerIsBetter.Contains(key))
            return StatDirection.LowerIsBetter;
        if (HigherIsBetter.Contains(key))
            return StatDirection.HigherIsBetter;
        return StatDirection.Neutral;
    }

    public static StatChange ClassifyChange(double oldVal, double newVal, StatDirection direction)
    {
        if (!IsChanged(oldVal, newVal))
            return StatChange.Unchanged;
        if (direction == StatDirection.Neutral)
            return StatChange.Neutral;

        bool increased = newVal > oldVal;
        if (direction == StatDirection.HigherIsBetter)
            return increased ? StatChange.Better : StatChange.Worse;
        return increased ? StatChange.Worse : StatChange.Better;
    }

    public static StatDelta CompareValues(string name, double oldVal, double newVal, string? unit = null, bool isResourceCost = false)
    {
        var direction = ClassifyStat(name, isResourceCost);
        var change = ClassifyChange(oldVal, newVal, direction);
        return new StatDelta(name, oldVal, newVal, direction, change, unit);
    }

    public static bool IsUpgradeable(IEnumerable<StatDelta> deltas)
        => deltas.Any(d => d.Change == StatChange.Better);

    public static List<StatDelta> ImprovedDeltas(IEnumerable<StatDelta> deltas)
        => deltas.Where(d => d.Change == StatChange.Better).ToList();

    /// <summary>
    /// Compare a saved design against a designer replayed at current tech. Display-stat keys
    /// missing from the saved design (older saves had no DisplayStats) are skipped so they
    /// are not treated as upgrades from zero.
    /// </summary>
    public static List<StatDelta> Compare(ComponentDesignSummary design, ComponentDesigner designer)
    {
        var deltas = new List<StatDelta>();

        Add(deltas, "Mass", design.MassPerUnit, designer.MassValue, "kg");
        Add(deltas, "Volume", design.VolumePerUnit, designer.VolumeM3Value, "m^3");
        Add(deltas, "Crew Required", design.CrewRequired, designer.CrewReqValue, null);
        Add(deltas, "Cost", design.CreditCost, designer.CreditCostValue, null);
        Add(deltas, "Research", design.ResearchCost, designer.ResearchCostValue, "RP");
        Add(deltas, "Production", design.IndustryPoints, designer.IndustryPointCostsValue, "IP");
        Add(deltas, "Build energy", design.EnergyCost, designer.EnergyCostValue, "kWh");

        var displayUnits = DisplayUnits(designer);
        var displayValues = DisplayValues(designer);
        foreach (var (name, newVal) in displayValues)
        {
            if (design.DisplayStats == null || !design.DisplayStats.TryGetValue(name, out var oldVal))
                continue;
            displayUnits.TryGetValue(name, out var unit);
            Add(deltas, name, oldVal, newVal, unit);
        }

        if (design.ResourceCosts != null)
        {
            foreach (var line in design.ResourceCosts)
            {
                if (!designer.ResourceCostValues.TryGetValue(line.CargoId, out var newAmount))
                    continue;
                Add(deltas, line.Name, line.Amount, newAmount, null, isResourceCost: true);
            }
        }

        return deltas;
    }

    public static Dictionary<string, double> DisplayValues(ComponentDesigner designer)
    {
        var stats = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var property in designer.ComponentDesignProperties.Values)
        {
            if (!property.IsEnabled || property.GuiHint != GuiHint.GuiTextDisplay)
                continue;
            try
            {
                double value = property.Value;
                if (double.IsFinite(value))
                    stats[property.Name] = value;
            }
            catch
            {
                // Non-numeric display text is not a comparable stat.
            }
        }
        return stats;
    }

    public static Dictionary<string, string> DisplayUnits(ComponentDesigner designer)
    {
        var units = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in designer.ComponentDesignProperties.Values)
        {
            if (!property.IsEnabled || property.GuiHint != GuiHint.GuiTextDisplay)
                continue;
            if (!string.IsNullOrEmpty(property.Unit))
                units[property.Name] = property.Unit;
        }
        return units;
    }

    public static string FormatValue(double value, string? unit)
    {
        string u = (unit ?? "").Trim();
        if (u.Length == 0)
            return value.ToString(Styles.DecimalFormat);

        if (EqualsUnit(u, "kW") || EqualsUnit(u, "Kw") || EqualsUnit(u, "KW"))
            return Stringify.Power(value);
        if (EqualsUnit(u, "kJ") || EqualsUnit(u, "KJ"))
            return Stringify.Energy(value);
        if (EqualsUnit(u, "kWh") || EqualsUnit(u, "KWh") || EqualsUnit(u, "KWH"))
            return Stringify.EnergyKwh(value);
        if (EqualsUnit(u, "m") || EqualsUnit(u, "M"))
            return Stringify.Distance(value);
        if (EqualsUnit(u, "W"))
            return value.ToString("G4") + " W";
        if (EqualsUnit(u, "m^2"))
            return Stringify.Area(value);
        if (EqualsUnit(u, "nm"))
            return Stringify.DistanceSmall(value);
        if (EqualsUnit(u, "kg"))
            return Stringify.Mass(value);
        if (EqualsUnit(u, "m^3"))
            return Stringify.Volume(value);
        if (EqualsUnit(u, "N"))
            return Stringify.Thrust(value);
        if (EqualsUnit(u, "m/s"))
            return Stringify.Velocity(value);
        if (EqualsUnit(u, "s"))
            return TimeSpan.FromSeconds(value).ToString();

        return value.ToString(Styles.DecimalFormat) + " " + unit;
    }

    /// <summary>A signed delta such as <c>(+ 15 kW)</c> or <c>(- 200 kg)</c>. Safe to pass to TextUnformatted (may contain %).</summary>
    public static string FormatSignedDelta(double delta, string? unit)
    {
        string sign = delta < 0 ? "-" : "+";
        return "(" + sign + " " + FormatValue(Math.Abs(delta), unit) + ")";
    }

    static void Add(List<StatDelta> deltas, string name, double oldVal, double newVal, string? unit, bool isResourceCost = false)
    {
        var delta = CompareValues(name, oldVal, newVal, unit, isResourceCost);
        if (delta.Change != StatChange.Unchanged)
            deltas.Add(delta);
    }

    static bool EqualsUnit(string actual, string expected)
        => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
           // "m" must not match "m^2" / "m/s" / "m^3"; EqualsUnit is always called with a trimmed exact token.
           && actual.Length == expected.Length;

    static readonly HashSet<string> LowerIsBetter = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mass",
        "Volume",
        "Crew Required",
        "Cost",
        "Research",
        "Production",
        "Build energy",
        "Power Draw",
        "Fuel Consumption",
        "Fuel per kWh",
        "Fuel Burn Rate",
        "Fuel Use",
        "Bubble Creation Energy Cost",
        "Bubble Sustain Energy Cost",
        "Sensitivity at Ideal Wavelength",
        "Sensitivity at Worst Wavelength",
        "Gravitational Coupling",
        "Cost Per Day",
        "Dry Weight",
        "CoreMass",
        "GennyMass",
        "FuelMass",
        "Thermal Output",
    };

    static readonly HashSet<string> HigherIsBetter = new(StringComparer.OrdinalIgnoreCase)
    {
        "Power Output",
        "Output after 20 years",
        "GeneratorOutput",
        "Specific Power",
        "Watt per kg",
        "Heat per kg",
        "Energy Storage",
        "C-Rate",
        "Charge Rate",
        "Discharge Rate",
        "Round-trip Efficiency",
        "Efficiency",
        "Cell Efficiency",
        "Generator Efficiency",
        "Best Efficiency",
        "Worst Efficiency",
        "Size Efficiency",
        "Lense Emission",
        "Conversion Efficency",
        "Exhaust Velocity",
        "Thrust",
        "Engine Power",
        "Bubble Collapse Energy Returned",
        "Bleed Fraction",
        "Research Points",
        "MiningAmount",
        "Refinery Points",
        "Component Construction Points",
        "Installation Construction Points",
        "Fighter Construction Points",
        "Ordnance Construction Points",
        "Ship Assembly Points",
        "Refit Points",
        "Support Colonists",
        "Storage Amount",
        "Support Capacity",
        "Transfer Rate",
        "Transfer Range",
        "Stored Volume",
        "TNT Equivalent",
        "Pulse Energy",
        "Input Light",
        "FuelDuration",
        "Half-life",
        "Tank Volume",
        "Tank Radius",
    };
}
