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

    public MarketPolicyRow Copy() => new()
    {
        CargoId = CargoId,
        Min = Min,
        Max = Max,
        AutoProduce = AutoProduce,
        Ask = Ask,
        Bid = Bid,
    };
}

/// <summary>
/// What a colony wants its market to do. Separate from <see cref="MarketListing"/>:
/// the book is what other factions can see, this blob is the colony's own targets.
/// </summary>
public class ColonyMarketPolicyDB : BaseDataBlob
{
    public Dictionary<string, MarketPolicyRow> Rows { get; } = new();

    public override object Clone()
    {
        var copy = new ColonyMarketPolicyDB();
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
