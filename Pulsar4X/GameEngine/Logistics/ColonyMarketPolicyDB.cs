using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Logistics;

public sealed class MarketPolicyRow
{
    public string CargoId = "";
    public long Min;
    public long Max;
    public bool AutoProduce;
    public decimal Ask;
    public decimal Bid;

    /// <summary>Bloc demand (multi-faction phase 4): units the colony's abstract economy uses per day. 0 = none.</summary>
    public double UsePerDay;
    /// <summary>Bloc demand: bid = seed ask x this x the shortage premium. 0 = not a demand row (bid left alone).</summary>
    public double BidFactor;
    /// <summary>Bloc demand: when stock first fell below <see cref="Min"/>; null when filled.</summary>
    public DateTime? ShortSince;
    /// <summary>Bloc demand: fractional use carried to the next pass.</summary>
    public double UseOwed;

    /// <summary>Bloc output: units the colony's abstract industry adds to stock per day, up to <see cref="Max"/>
    /// (90 days of output when Max is 0). 0 = none. Lets a manufacturing bloc keep its inputs flowing to the others.</summary>
    public double OutputPerDay;
    /// <summary>Bloc output: fractional output carried to the next pass.</summary>
    public double OutputOwed;

    /// <summary>True for a data-driven bloc demand row (bid follows the shortage premium, capped by the reserve).</summary>
    public bool IsDemand() => BidFactor > 0;

    public MarketPolicyRow Copy() => new()
    {
        CargoId = CargoId,
        Min = Min,
        Max = Max,
        AutoProduce = AutoProduce,
        Ask = Ask,
        Bid = Bid,
        UsePerDay = UsePerDay,
        BidFactor = BidFactor,
        ShortSince = ShortSince,
        UseOwed = UseOwed,
        OutputPerDay = OutputPerDay,
        OutputOwed = OutputOwed,
    };
}

/// <summary>
/// What a colony wants its market to do. Separate from <see cref="MarketListing"/>:
/// the book is what other factions can see, this blob is the colony's own targets.
/// </summary>
public class ColonyMarketPolicyDB : BaseDataBlob
{
    public Dictionary<string, MarketPolicyRow> Rows { get; } = new();

    /// <summary>When the bloc consumption sink last ran (multi-faction phase 4). Null before the first pass.</summary>
    public DateTime? LastConsumption { get; set; }

    public override object Clone()
    {
        var copy = new ColonyMarketPolicyDB { LastConsumption = LastConsumption };
        foreach (var (id, row) in Rows)
            copy.Rows[id] = row.Copy();
        return copy;
    }
}

/// <summary>
/// Administrator quality for market orders. This plan returns 1.0, including when the colony has no administrator.
/// </summary>
public static class ColonyAdministrator
{
    public static decimal Quality(Entity colony) => 1.0m;
}
