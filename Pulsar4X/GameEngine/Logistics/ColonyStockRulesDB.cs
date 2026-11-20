using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Logistics;

/// <summary>One warehouse rule for one item at one colony. Either bound may be unset (null).</summary>
public sealed class StockRule
{
    /// <summary>Cargo good or industry design id (the same id the warehouse and production use).</summary>
    [JsonProperty]
    public string CargoId { get; set; } = "";

    /// <summary>Keep at least this many units. Never sold; with the industry seat filled, production is queued toward it.</summary>
    [JsonProperty]
    public long? Min { get; set; }

    /// <summary>Sell if above this many units: with the market seat filled, stock above it is listed for sale.</summary>
    [JsonProperty]
    public long? Max { get; set; }

    public StockRule Copy() => new() { CargoId = CargoId, Min = Min, Max = Max };
}

/// <summary>
/// A colony's warehouse stock rules, one Min/Max pair per item, keyed by cargo id. Applied by <see cref="StockRules"/>:
/// Min by the industry head, Max by the market head.
/// </summary>
public class ColonyStockRulesDB : BaseDataBlob
{
    [JsonProperty]
    public Dictionary<string, StockRule> Rules { get; private set; } = new(StringComparer.Ordinal);

    public override object Clone()
    {
        var copy = new ColonyStockRulesDB();
        foreach (var (id, rule) in Rules)
            copy.Rules[id] = rule.Copy();
        return copy;
    }
}
