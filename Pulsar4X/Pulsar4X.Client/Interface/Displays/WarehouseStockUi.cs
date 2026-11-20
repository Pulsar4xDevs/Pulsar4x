using System;
using System.Collections.Generic;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>One warehouse row the sliders can edit: a stock rule, a cargo item, or both.</summary>
public readonly record struct WarehouseStockRow(
    string CargoId,
    string Name,
    long? Min,
    long? Max,
    long Stock,
    long Queued,
    bool HasRule);

/// <summary>
/// Decision logic for warehouse Min/Max stock sliders and the market auto-pricing banner. No ImGui.
/// Min is shown only with a filled industry seat (it queues production). Max is shown only with a
/// filled market seat (it sells surplus).
/// </summary>
public static class WarehouseStockUi
{
    public const string MinHiddenHint = "Min is hidden: it queues production, which needs an industry head.";
    public const string MaxHiddenHint = "Max is hidden: it sells surplus, which needs a market head.";
    public const string AutoPricingOffHint =
        "Auto-pricing is off without a market head. Auto-priced listings are withdrawn; only prices you set stay listed.";

    public static bool ShowMin(bool industrySeatFilled) => industrySeatFilled;

    public static bool ShowMax(bool marketSeatFilled) => marketSeatFilled;

    public static bool AutoPricingOn(bool marketSeatFilled) => marketSeatFilled;

    public static string? AutoPricingHint(bool marketSeatFilled)
        => marketSeatFilled ? null : AutoPricingOffHint;

    public static string? MinHint(bool industrySeatFilled)
        => industrySeatFilled ? null : MinHiddenHint;

    public static string? MaxHint(bool marketSeatFilled)
        => marketSeatFilled ? null : MaxHiddenHint;

    public static string StockQueuedText(long stock, long queued)
        => "Stock " + stock.ToString("N0") + "   Queued " + queued.ToString("N0");

    public static bool HasRule(long? min, long? max) => min != null || max != null;

    /// <summary>
    /// Stock rules plus cargo items not already covered, ordered by name.
    /// Extra choices (for example market-addable goods) are included when their cargo id is new.
    /// </summary>
    public static IReadOnlyList<WarehouseStockRow> Rows(
        CargoStorageView? storage,
        IEnumerable<MarketGoodChoice>? extraChoices = null)
    {
        var byId = new Dictionary<string, WarehouseStockRow>(StringComparer.Ordinal);
        if (storage != null)
        {
            foreach (var rule in storage.StockRules)
            {
                if (string.IsNullOrEmpty(rule.CargoId))
                    continue;
                byId[rule.CargoId] = new WarehouseStockRow(
                    rule.CargoId, rule.Name, rule.Min, rule.Max, rule.Stock, rule.Queued, HasRule(rule.Min, rule.Max));
            }
            foreach (var store in storage.Stores)
            {
                foreach (var item in store.Items)
                {
                    string id = item.CargoId;
                    if (string.IsNullOrEmpty(id) || byId.ContainsKey(id))
                        continue;
                    byId[id] = new WarehouseStockRow(id, item.Name, null, null, item.Units, 0, false);
                }
            }
        }
        if (extraChoices != null)
        {
            foreach (var choice in extraChoices)
            {
                if (string.IsNullOrEmpty(choice.CargoId) || byId.ContainsKey(choice.CargoId))
                    continue;
                byId[choice.CargoId] = new WarehouseStockRow(choice.CargoId, choice.Name, null, null, 0, 0, false);
            }
        }

        var rows = new List<WarehouseStockRow>(byId.Values);
        rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return rows;
    }

    /// <summary>
    /// Upper end of the Min/Max sliders: at least 100, at least twice the current figures, rounded
    /// up to a 1/2/5 × 10ⁿ step, and never above <see cref="int.MaxValue"/>.
    /// </summary>
    public static int SliderCeiling(long stock, long queued, long? min, long? max)
    {
        long high = 0;
        if (stock > high) high = stock;
        if (queued > high) high = queued;
        if (min is long minValue && minValue > high) high = minValue;
        if (max is long maxValue && maxValue > high) high = maxValue;
        long target = high <= 50 ? 100 : high * 2;
        return (int)Math.Min(int.MaxValue, NiceCeiling(target));
    }

    static long NiceCeiling(long value)
    {
        if (value <= 100)
            return 100;
        long pow = 1;
        while (pow * 10 < value)
            pow *= 10;
        foreach (long step in new[] { 1L, 2L, 5L, 10L })
        {
            long n = step * pow;
            if (n >= value)
                return n;
        }
        return 10 * pow;
    }

    /// <summary>Clamp so Max is never below Min when both are set.</summary>
    public static (long? min, long? max) WithMin(long? min, long? max, long? newMin)
    {
        min = newMin;
        if (min is long a && max is long b && b < a)
            max = a;
        return (min, max);
    }

    public static (long? min, long? max) WithMax(long? min, long? max, long? newMax)
    {
        max = newMax;
        if (min is long a && max is long b && b < a)
            min = b;
        return (min, max);
    }

    /// <summary>Both null clears the rule; otherwise it is set.</summary>
    public static GameCommand Command(int colonyId, string cargoId, long? min, long? max)
        => HasRule(min, max)
            ? new SetStockRuleCommand(colonyId, cargoId, min, max)
            : new ClearStockRuleCommand(colonyId, cargoId);

    public static string? RefusalMessage(CommandResult? result)
    {
        if (result == null || result.Accepted)
            return null;
        return string.IsNullOrEmpty(result.RejectionReason)
            ? "The command was refused."
            : result.RejectionReason;
    }
}
