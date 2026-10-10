using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Logistics;

/// <summary>
/// Market book for a logistics office. Stock stays in <see cref="Pulsar4X.Storage.CargoStorageDB"/>.
/// Capacity is how many distinct cargo ids this office may list.
/// </summary>
public class LogiBaseDB : BaseDataBlob
{
    public int Capacity { get; internal set; }

    public Dictionary<string, MarketListing> Listings { get; internal set; } = new();

    /// <summary>
    /// Intel for sale. Keyed by kind and subject. Does not count toward <see cref="Capacity"/>.
    /// </summary>
    [JsonProperty]
    public Dictionary<string, IntelListing> Intel { get; internal set; } = new();

    public LogiBaseDB()
    {
    }

    internal override void OnSetToEntity()
    {
    }

    private LogiBaseDB(LogiBaseDB db)
    {
        Capacity = db.Capacity;
        Listings = new Dictionary<string, MarketListing>();
        foreach (var (id, listing) in db.Listings)
            Listings[id] = listing.Copy();
        Intel = new Dictionary<string, IntelListing>(StringComparer.Ordinal);
        if (db.Intel != null)
        {
            foreach (var (id, row) in db.Intel)
                Intel[id] = row.Copy();
        }
    }

    public override object Clone()
    {
        return new LogiBaseDB(this);
    }
}
