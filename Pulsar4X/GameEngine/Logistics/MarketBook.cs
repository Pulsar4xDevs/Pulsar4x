using Pulsar4X.Engine;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One cargo id at one market. <see cref="CargoId"/> is <see cref="ICargoable.UniqueID"/>.
/// </summary>
public sealed class MarketListing
{
    public string CargoId = "";
    public long SellQuantity;
    public decimal Ask;
    public long BuyQuantity;
    public decimal Bid;
    public long Reserve;

    public MarketListing Copy() => new()
    {
        CargoId = CargoId,
        SellQuantity = SellQuantity,
        Ask = Ask,
        BuyQuantity = BuyQuantity,
        Bid = Bid,
        Reserve = Reserve,
    };
}

/// <summary>
/// Read and write a logistics office's book. Does not move cargo and does not run a processor.
/// </summary>
public static class MarketBook
{
    public static bool TryGet(Entity entity, string cargoId, out MarketListing listing)
    {
        listing = null!;
        if (string.IsNullOrEmpty(cargoId))
            return false;
        if (!entity.TryGetDataBlob<LogiBaseDB>(out var book))
            return false;
        if (!book.Listings.TryGetValue(cargoId, out var found))
            return false;
        listing = found;
        return true;
    }

    /// <summary>
    /// Replace the listing for <see cref="MarketListing.CargoId"/>.
    /// Rejects a negative quantity or price, an empty id, and a new id when the book is at capacity.
    /// </summary>
    public static bool SetListing(Entity entity, MarketListing listing)
    {
        if (listing == null || string.IsNullOrEmpty(listing.CargoId))
            return false;
        if (listing.SellQuantity < 0 || listing.BuyQuantity < 0 || listing.Reserve < 0)
            return false;
        if (listing.Ask < 0 || listing.Bid < 0)
            return false;
        if (!entity.TryGetDataBlob<LogiBaseDB>(out var book))
            return false;

        bool exists = book.Listings.ContainsKey(listing.CargoId);
        if (!exists && book.Listings.Count >= book.Capacity)
            return false;

        book.Listings[listing.CargoId] = listing.Copy();
        return true;
    }

    public static long Stock(Entity entity, ICargoable cargo)
    {
        if (cargo == null || !entity.TryGetDataBlob<CargoStorageDB>(out var storage))
            return 0;
        return storage.GetUnitsStored(cargo, includeEscro: false);
    }

    public static long Sellable(Entity entity, MarketListing listing, ICargoable cargo)
    {
        if (listing == null)
            return 0;
        long stock = Stock(entity, cargo);
        long sellable = stock - listing.Reserve;
        return sellable > 0 ? sellable : 0;
    }
}
