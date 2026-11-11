using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Factions;

namespace Pulsar4X.Logistics;

/// <summary>
/// One day's prices at one office for one cargo id.
/// Open/High/Low/Close and Volume are from executed trades that day (meaningful when Volume is above 0).
/// Bid, Ask and Stock are the last quote recorded that day. A later 0/0 quote does not wipe a non-zero
/// Bid/Ask already recorded that day; a day that only ever saw 0/0 keeps 0/0.
/// </summary>
public sealed class PriceBucket
{
    public DateTime Day;
    public decimal Open, High, Low, Close;
    public long Volume;
    public decimal Bid, Ask;
    public long Stock;

    public PriceBucket Copy() => new()
    {
        Day = Day,
        Open = Open,
        High = High,
        Low = Low,
        Close = Close,
        Volume = Volume,
        Bid = Bid,
        Ask = Ask,
        Stock = Stock,
    };
}

/// <summary>
/// Per-office daily price history. One list of buckets per cargo id, oldest first, capped at <see cref="MaxDays"/>.
/// </summary>
public static class MarketHistory
{
    public const int MaxDays = 730;

    /// <summary>
    /// Record an executed trade at <paramref name="price"/> for <paramref name="units"/> of the listing.
    /// No-op when units are not positive, the listing is missing or a relocation, or the office has no book.
    /// </summary>
    public static void RecordTrade(Entity office, MarketListing listing, decimal price, long units)
    {
        if (office == null || office.Manager == null || listing == null || listing.Relocation || units <= 0)
            return;
        if (string.IsNullOrEmpty(listing.CargoId))
            return;
        if (!office.TryGetDataBlob<LogiBaseDB>(out var book))
            return;

        book.History ??= new Dictionary<string, List<PriceBucket>>();
        var bucket = BucketForToday(book, listing.CargoId, office.StarSysDateTime.Date);
        if (bucket == null)
            return;

        if (bucket.Volume == 0)
        {
            bucket.Open = price;
            bucket.High = price;
            bucket.Low = price;
            bucket.Close = price;
        }
        else
        {
            if (price > bucket.High)
                bucket.High = price;
            if (price < bucket.Low)
                bucket.Low = price;
            bucket.Close = price;
        }
        bucket.Volume += units;
        KeepQuote(bucket, listing);
        bucket.Stock = StockOf(office, listing.CargoId);
        book.HistoryVersion++;
    }

    /// <summary>
    /// Record the listing's current Bid/Ask/Stock on today's bucket. Does not touch OHLC or Volume.
    /// No-op for a relocation listing or an office with no book.
    /// </summary>
    public static void RecordQuote(Entity office, MarketListing listing)
    {
        if (office == null || office.Manager == null || listing == null || listing.Relocation)
            return;
        if (string.IsNullOrEmpty(listing.CargoId))
            return;
        if (!office.TryGetDataBlob<LogiBaseDB>(out var book))
            return;

        book.History ??= new Dictionary<string, List<PriceBucket>>();
        var bucket = BucketForToday(book, listing.CargoId, office.StarSysDateTime.Date);
        if (bucket == null)
            return;

        KeepQuote(bucket, listing);
        bucket.Stock = StockOf(office, listing.CargoId);
        book.HistoryVersion++;
    }

    /// <summary>
    /// Takes the listing's Bid/Ask, except that a 0/0 listing does not replace a non-zero Bid/Ask the day
    /// already has (from an earlier quote or trade). A one-sided listing (only Bid or only Ask) is a real quote.
    /// </summary>
    static void KeepQuote(PriceBucket bucket, MarketListing listing)
    {
        if (listing.Bid == 0 && listing.Ask == 0 && (bucket.Bid != 0 || bucket.Ask != 0))
            return;
        bucket.Bid = listing.Bid;
        bucket.Ask = listing.Ask;
    }

    static PriceBucket? BucketForToday(LogiBaseDB book, string cargoId, DateTime today)
    {
        today = today.Date;
        if (!book.History.TryGetValue(cargoId, out var days) || days == null)
        {
            days = new List<PriceBucket>();
            book.History[cargoId] = days;
        }

        if (days.Count == 0)
        {
            var created = new PriceBucket { Day = today };
            days.Add(created);
            return created;
        }

        DateTime lastDay = days[days.Count - 1].Day.Date;
        if (lastDay == today)
            return days[days.Count - 1];
        if (today > lastDay)
        {
            var created = new PriceBucket { Day = today };
            days.Add(created);
            while (days.Count > MaxDays)
                days.RemoveAt(0);
            return created;
        }

        for (int i = 0; i < days.Count; i++)
        {
            if (days[i].Day.Date == today)
                return days[i];
        }
        return null;
    }

    static long StockOf(Entity office, string cargoId)
    {
        var game = office.Manager?.Game;
        if (game == null || !game.Factions.TryGetValue(office.FactionOwnerID, out var faction))
            return 0;
        if (!faction.TryGetDataBlob<FactionInfoDB>(out var owner) || owner == null)
            return 0;
        if (!owner.Data.CargoGoods.Contains(cargoId))
            return 0;
        var cargo = owner.Data.CargoGoods.GetAny(cargoId);
        if (cargo == null)
            return 0;
        return MarketBook.Stock(office, cargo);
    }
}
