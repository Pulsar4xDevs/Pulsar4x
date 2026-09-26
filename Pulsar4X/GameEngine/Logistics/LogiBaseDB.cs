using System.Collections.Generic;
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
    }

    public override object Clone()
    {
        return new LogiBaseDB(this);
    }
}
