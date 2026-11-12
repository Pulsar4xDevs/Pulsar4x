using System;
using System.Collections.Generic;
using System.Text;
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
    public const int SeedDays = 30;

    /// <summary>
    /// New-game flavor only. Called from <see cref="Game.PostNewGameInitialization"/>.
    /// Writes <see cref="SeedDays"/> of deterministic daily candles per priced cargo so the Market tab
    /// is not empty on day one. Deterministic per MasterSeed, office, and cargo. Idempotent: a cargo
    /// that already has a bucket before the current day is left unchanged.
    /// </summary>
    public static void SeedPriorDays(Entity office)
    {
        if (office == null || office.Manager == null || office.Manager.Game == null)
            return;
        if (!office.TryGetDataBlob<LogiBaseDB>(out var book))
            return;

        book.History ??= new Dictionary<string, List<PriceBucket>>();
        DateTime startDate = office.StarSysDateTime.Date;
        var game = office.Manager.Game;

        office.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy);

        var cargoIds = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var kv in book.Listings)
        {
            if (!string.IsNullOrEmpty(kv.Key))
                cargoIds.Add(kv.Key);
            if (kv.Value != null && !string.IsNullOrEmpty(kv.Value.CargoId))
                cargoIds.Add(kv.Value.CargoId);
        }
        if (policy != null)
        {
            foreach (var kv in policy.Rows)
            {
                if (!string.IsNullOrEmpty(kv.Key))
                    cargoIds.Add(kv.Key);
                if (kv.Value != null && !string.IsNullOrEmpty(kv.Value.CargoId))
                    cargoIds.Add(kv.Value.CargoId);
            }
        }

        bool wrote = false;
        foreach (var cargoId in cargoIds)
        {
            book.Listings.TryGetValue(cargoId, out var listing);
            if (listing != null && listing.Relocation)
                continue;

            decimal bid = 0;
            decimal ask = 0;
            if (listing != null && (listing.Bid != 0 || listing.Ask != 0))
            {
                bid = listing.Bid;
                ask = listing.Ask;
            }
            else if (policy != null
                     && policy.Rows.TryGetValue(cargoId, out var row)
                     && row != null
                     && (row.Bid != 0 || row.Ask != 0))
            {
                bid = row.Bid;
                ask = row.Ask;
            }
            else
                continue;

            if (book.History.TryGetValue(cargoId, out var existing) && existing != null)
            {
                bool alreadySeeded = false;
                for (int i = 0; i < existing.Count; i++)
                {
                    if (existing[i] != null && existing[i].Day.Date < startDate)
                    {
                        alreadySeeded = true;
                        break;
                    }
                }
                if (alreadySeeded)
                    continue;
            }

            decimal anchor = (bid > 0 && ask > 0) ? (bid + ask) / 2 : (bid > 0 ? bid : ask);
            int seed = SeedFor(game.Settings.MasterSeed, office.Id, cargoId);
            var rng = new System.Random(seed);
            long stock = StockOf(office, cargoId);

            double A = (double)anchor;
            double lo = Math.Max(0.01, 0.7 * A);
            double hi = 1.3 * A;

            var raw = new double[SeedDays];
            raw[0] = ClampRange(A * (1 + 0.03 * Gauss(rng)), lo, hi);
            for (int i = 1; i < SeedDays; i++)
                raw[i] = ClampRange(raw[i - 1] + 0.25 * (A - raw[i - 1]) + A * 0.04 * Gauss(rng), lo, hi);

            double drift = A - raw[SeedDays - 1];
            var close = new decimal[SeedDays];
            for (int i = 0; i < SeedDays; i++)
                close[i] = Round2(ClampRange(raw[i] + drift * (i + 1) / 30.0, lo, hi));
            close[SeedDays - 1] = Math.Round(anchor, 2, MidpointRounding.AwayFromZero);

            var open = new decimal[SeedDays];
            open[0] = Round2(ClampRange(A * (1 + 0.01 * Gauss(rng)), lo, hi));
            for (int i = 1; i < SeedDays; i++)
                open[i] = close[i - 1];

            var seeded = new List<PriceBucket>(SeedDays);
            for (int i = 0; i < SeedDays; i++)
            {
                decimal high = Round2(Math.Max((double)open[i], (double)close[i]) + A * 0.015 * rng.NextDouble());
                decimal low = Round2(Math.Min((double)open[i], (double)close[i]) - A * 0.015 * rng.NextDouble());
                high = Math.Max(high, Math.Max(open[i], close[i]));
                low = Math.Min(low, Math.Min(open[i], close[i]));
                if (low < 0.01m)
                    low = 0.01m;

                long volume = rng.Next(20, 201);

                decimal bucketBid;
                decimal bucketAsk;
                if (bid > 0 && ask > 0)
                {
                    bucketAsk = Math.Max(0.01m, Math.Round(close[i] * (ask / anchor), 2, MidpointRounding.AwayFromZero));
                    bucketBid = Math.Max(0.01m, Math.Round(close[i] * (bid / anchor), 2, MidpointRounding.AwayFromZero));
                }
                else if (ask > 0)
                {
                    bucketAsk = close[i];
                    bucketBid = 0;
                }
                else
                {
                    bucketBid = close[i];
                    bucketAsk = 0;
                }

                seeded.Add(new PriceBucket
                {
                    Day = startDate.AddDays(-SeedDays + i),
                    Open = open[i],
                    High = high,
                    Low = low,
                    Close = close[i],
                    Volume = volume,
                    Bid = bucketBid,
                    Ask = bucketAsk,
                    Stock = stock,
                });
            }

            if (!book.History.TryGetValue(cargoId, out var days) || days == null)
            {
                days = new List<PriceBucket>();
                book.History[cargoId] = days;
            }
            days.InsertRange(0, seeded);
            wrote = true;
        }

        if (wrote)
            book.HistoryVersion++;
    }

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

    internal static int SeedFor(int masterSeed, int officeId, string cargoId)
    {
        unchecked
        {
            ulong h = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            h = (h ^ (byte)(masterSeed >> 0)) * prime;
            h = (h ^ (byte)(masterSeed >> 8)) * prime;
            h = (h ^ (byte)(masterSeed >> 16)) * prime;
            h = (h ^ (byte)(masterSeed >> 24)) * prime;
            h = (h ^ (byte)(officeId >> 0)) * prime;
            h = (h ^ (byte)(officeId >> 8)) * prime;
            h = (h ^ (byte)(officeId >> 16)) * prime;
            h = (h ^ (byte)(officeId >> 24)) * prime;
            byte[] bytes = Encoding.UTF8.GetBytes(cargoId);
            for (int i = 0; i < bytes.Length; i++)
                h = (h ^ bytes[i]) * prime;
            return (int)(h ^ (h >> 32));
        }
    }

    static double Gauss(System.Random rng)
        => rng.NextDouble() + rng.NextDouble() + rng.NextDouble() - 1.5;

    static double ClampRange(double x, double lo, double hi)
        => Math.Min(hi, Math.Max(lo, x));

    static decimal Round2(double x)
        => Math.Round((decimal)x, 2, MidpointRounding.AwayFromZero);
}
