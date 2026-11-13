using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Storage;
using NameDB = Pulsar4X.Names.NameDB;

namespace Pulsar4X.Tests;

[TestFixture]
public class MarketHistoryTests
{
    [Test]
    public void BuyFromListing_RecordsOneBucketAtAsk()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 7);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 12));

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Open, Is.EqualTo(10m));
        Assert.That(bucket.High, Is.EqualTo(10m));
        Assert.That(bucket.Low, Is.EqualTo(10m));
        Assert.That(bucket.Close, Is.EqualTo(10m));
        Assert.That(bucket.Volume, Is.EqualTo(12));
        Assert.That(bucket.Bid, Is.EqualTo(7m));
        Assert.That(bucket.Ask, Is.EqualTo(10m));
        Assert.That(bucket.Stock, Is.EqualTo(28));
    }

    [Test]
    public void SellToListing_RecordsAtBid()
    {
        var scene = Scene.Build(marketStock: 0, shipStock: 15, sell: 0, buy: 10, bid: 12);
        scene.Run(scene.Exchange(MarketSide.SellToMarket, 6));

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Open, Is.EqualTo(12m));
        Assert.That(bucket.High, Is.EqualTo(12m));
        Assert.That(bucket.Low, Is.EqualTo(12m));
        Assert.That(bucket.Close, Is.EqualTo(12m));
        Assert.That(bucket.Volume, Is.EqualTo(6));
        Assert.That(bucket.Bid, Is.EqualTo(12m));
        Assert.That(bucket.Ask, Is.EqualTo(0m));
        Assert.That(bucket.Stock, Is.EqualTo(6));
    }

    [Test]
    public void TwoTradesSameDay_OpenFirstCloseLast_VolumeSummed()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 4));
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 26,
            Ask = 15,
        }), Is.True);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 2));

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Open, Is.EqualTo(10m));
        Assert.That(bucket.Close, Is.EqualTo(15m));
        Assert.That(bucket.High, Is.EqualTo(15m));
        Assert.That(bucket.Low, Is.EqualTo(10m));
        Assert.That(bucket.Volume, Is.EqualTo(6));
    }

    [Test]
    public void TradeOnTheNextDay_StartsANewBucket()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 4));
        var day0 = scene.Market.StarSysDateTime.Date;

        AdvanceDays(scene.Market, 1);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 3));

        var days = Days(scene, "iron");
        Assert.That(days, Has.Count.EqualTo(2));
        Assert.That(days[0].Day, Is.EqualTo(day0));
        Assert.That(days[0].Volume, Is.EqualTo(4));
        Assert.That(days[0].Close, Is.EqualTo(10m));
        Assert.That(days[1].Day, Is.EqualTo(day0.AddDays(1)));
        Assert.That(days[1].Volume, Is.EqualTo(3));
        Assert.That(days[1].Open, Is.EqualTo(10m));
    }

    [Test]
    public void SetListing_QuoteOnlyDay_VolumeZero()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 7);
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 30,
            Ask = 11,
            Bid = 8,
        }), Is.True);

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Volume, Is.EqualTo(0));
        Assert.That(bucket.Bid, Is.EqualTo(8m));
        Assert.That(bucket.Ask, Is.EqualTo(11m));
        Assert.That(bucket.Stock, Is.EqualTo(40));
    }

    [Test]
    public void LaterSameDayZeroQuote_KeepsTheNonZeroBidAndAsk()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 7);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 5));
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing { CargoId = "iron", SellQuantity = 30 }), Is.True);

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Volume, Is.EqualTo(5));
        Assert.That(bucket.Close, Is.EqualTo(10m));
        Assert.That(bucket.Bid, Is.EqualTo(7m));
        Assert.That(bucket.Ask, Is.EqualTo(10m));

        // A later real (non-zero) quote still replaces it.
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 30,
            Ask = 12,
            Bid = 9,
        }), Is.True);
        bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Bid, Is.EqualTo(9m));
        Assert.That(bucket.Ask, Is.EqualTo(12m));
    }

    [Test]
    public void DayThatOnlySawZeroQuotes_StoresZero()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30);
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing { CargoId = "iron", SellQuantity = 20 }), Is.True);

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Volume, Is.EqualTo(0));
        Assert.That(bucket.Bid, Is.EqualTo(0m));
        Assert.That(bucket.Ask, Is.EqualTo(0m));
    }

    [Test]
    public void CapTrimming_DropsOldestBeyondMaxDays()
    {
        var scene = Scene.Build(list: false, marketStock: 10);
        var start = scene.Market.StarSysDateTime.Date;
        int days = MarketHistory.MaxDays + 5;
        for (int i = 0; i < days; i++)
        {
            if (i > 0)
                AdvanceDays(scene.Market, 1);
            Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
            {
                CargoId = "iron",
                SellQuantity = 1,
                Ask = 1,
            }), Is.True);
        }

        var kept = Days(scene, "iron");
        Assert.That(kept, Has.Count.EqualTo(MarketHistory.MaxDays));
        Assert.That(kept[0].Day, Is.EqualTo(start.AddDays(5)));
        Assert.That(kept[kept.Count - 1].Day, Is.EqualTo(start.AddDays(days - 1)));
    }

    [Test]
    public void CloneAndJson_PreserveHistory_RemoveListingKeepsIt()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 7);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 12));
        var original = scene.Market.GetDataBlob<LogiBaseDB>();
        Assert.That(original.History.ContainsKey("iron"), Is.True);
        long volume = original.History["iron"][0].Volume;
        decimal close = original.History["iron"][0].Close;
        DateTime day = original.History["iron"][0].Day;

        var clone = (LogiBaseDB)original.Clone();
        Assert.That(clone.History.ContainsKey("iron"), Is.True);
        Assert.That(clone.History["iron"], Has.Count.EqualTo(1));
        Assert.That(clone.History["iron"][0].Volume, Is.EqualTo(volume));
        clone.History["iron"][0].Volume = 0;
        clone.History["iron"][0].Close = 0;
        Assert.That(original.History["iron"][0].Volume, Is.EqualTo(volume));
        Assert.That(original.History["iron"][0].Close, Is.EqualTo(close));

        MarketBook.RemoveListing(scene.Market, "iron");
        Assert.That(MarketBook.TryGet(scene.Market, "iron", out _), Is.False);
        Assert.That(original.History.ContainsKey("iron"), Is.True);
        Assert.That(original.History["iron"][0].Volume, Is.EqualTo(volume));

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Objects,
            ContractResolver = new NonPublicResolver(),
        };
        string json = JsonConvert.SerializeObject(original, settings);
        var loaded = JsonConvert.DeserializeObject<LogiBaseDB>(json, settings);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.History, Is.Not.Null);
        Assert.That(loaded.History.ContainsKey("iron"), Is.True);
        Assert.That(loaded.History["iron"], Has.Count.EqualTo(1));
        Assert.That(loaded.History["iron"][0].Volume, Is.EqualTo(volume));
        Assert.That(loaded.History["iron"][0].Close, Is.EqualTo(close));
        Assert.That(loaded.History["iron"][0].Day, Is.EqualTo(day));
        Assert.That(loaded.History["iron"][0].Stock, Is.EqualTo(28));
    }

    [Test]
    public void RelocationListing_NeverCreatesHistory()
    {
        var scene = Scene.Build(list: false, marketStock: 20, sell: 0, ask: 0);
        Assert.That(MarketBook.SetListing(scene.Market, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 20,
            Ask = 10,
            Relocation = true,
        }), Is.True);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 5));

        var book = scene.Market.GetDataBlob<LogiBaseDB>();
        Assert.That(book.History, Is.Empty);
        Assert.That(MarketBook.Stock(scene.Ship, scene.ShipIron), Is.EqualTo(5));
    }

    [Test]
    public void ShipmentSeal_RecordsTradeAtListingPriceOnTheHolder()
    {
        var scene = Scene.Build(
            sameFaction: false,
            mutualFriendly: true,
            marketStock: 100,
            sell: 100,
            ask: 10,
            bid: 0);
        var buyer = scene.ExtraOffice(scene.ShipFaction, funds: 1000);

        Assert.That(ShipmentBook.TrySealMarket(
            buyer, scene.Market, false,
            new List<(ICargoable, long)> { (scene.MarketIron, 30) },
            0m, out _, out var reason), Is.True, reason);

        var bucket = SoleBucket(scene, "iron");
        Assert.That(bucket.Open, Is.EqualTo(10m));
        Assert.That(bucket.High, Is.EqualTo(10m));
        Assert.That(bucket.Low, Is.EqualTo(10m));
        Assert.That(bucket.Close, Is.EqualTo(10m));
        Assert.That(bucket.Volume, Is.EqualTo(30));
        Assert.That(bucket.Ask, Is.EqualTo(10m));
        Assert.That(bucket.Stock, Is.EqualTo(70));
    }

    [Test]
    public void SeedPriorDays_WritesThirtyDaysBeforeStart_NeverToday()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 8);
        var start = scene.Market.StarSysDateTime.Date;
        var today = SoleBucket(scene, "iron");
        Assert.That(today.Day, Is.EqualTo(start));
        Assert.That(today.Volume, Is.EqualTo(0));
        Assert.That(today.Bid, Is.EqualTo(8m));
        Assert.That(today.Ask, Is.EqualTo(10m));

        MarketHistory.SeedPriorDays(scene.Market);

        var days = Days(scene, "iron");
        Assert.That(days, Has.Count.EqualTo(31));
        for (int i = 0; i < 30; i++)
            Assert.That(days[i].Day, Is.EqualTo(start.AddDays(-30 + i)));
        Assert.That(days[30], Is.SameAs(today));
        Assert.That(days[30].Day, Is.EqualTo(start));
        Assert.That(days[30].Volume, Is.EqualTo(0));
        Assert.That(days[30].Bid, Is.EqualTo(8m));
        Assert.That(days[30].Ask, Is.EqualTo(10m));
    }

    [Test]
    public void SeedPriorDays_ValidOhlc()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 8);
        MarketHistory.SeedPriorDays(scene.Market);

        var seeded = Days(scene, "iron").Take(30).ToList();
        Assert.That(seeded, Has.Count.EqualTo(30));
        for (int i = 0; i < seeded.Count; i++)
        {
            var bucket = seeded[i];
            Assert.That(bucket.High, Is.GreaterThanOrEqualTo(Math.Max(bucket.Open, bucket.Close)));
            Assert.That(bucket.Low, Is.LessThanOrEqualTo(Math.Min(bucket.Open, bucket.Close)));
            Assert.That(bucket.Low, Is.GreaterThanOrEqualTo(0.01m));
            Assert.That(bucket.Open, Is.GreaterThanOrEqualTo(0.01m));
            Assert.That(bucket.High, Is.GreaterThanOrEqualTo(0.01m));
            Assert.That(bucket.Close, Is.GreaterThanOrEqualTo(0.01m));
            Assert.That(bucket.Bid, Is.GreaterThanOrEqualTo(0.01m));
            Assert.That(bucket.Ask, Is.GreaterThanOrEqualTo(0.01m));
            Assert.That(bucket.Volume, Is.GreaterThan(0));
            Assert.That(bucket.Stock, Is.EqualTo(40));
            Assert.That(bucket.Ask, Is.GreaterThanOrEqualTo(bucket.Bid));
            Assert.That(bucket.Bid, Is.GreaterThan(0));
            Assert.That(bucket.Close, Is.GreaterThanOrEqualTo(6.29m).And.LessThanOrEqualTo(11.71m));
            if (i >= 1)
                Assert.That(bucket.Open, Is.EqualTo(seeded[i - 1].Close));
        }
        Assert.That(seeded[29].Close, Is.EqualTo(9.00m));
    }

    [Test]
    public void SeedPriorDays_SameSeedSameSeries()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 8);
        scene.Game.Settings.MasterSeed = 12345678;
        var first = SeedAndTakePrior(scene);
        var second = SeedAndTakePrior(scene);

        Assert.That(first, Has.Count.EqualTo(30));
        Assert.That(second, Has.Count.EqualTo(30));
        for (int i = 0; i < 30; i++)
            AssertBucketEquals(second[i], first[i]);
    }

    [Test]
    public void SeedPriorDays_DifferentMasterSeedDifferentSeries()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 8);
        scene.Game.Settings.MasterSeed = 12345678;
        var first = SeedAndTakePrior(scene);
        scene.Game.Settings.MasterSeed = 87654321;
        var second = SeedAndTakePrior(scene);

        Assert.That(second, Has.Count.EqualTo(30));
        bool closeDiffers = false;
        for (int i = 0; i < 30; i++)
        {
            if (first[i].Close != second[i].Close)
            {
                closeDiffers = true;
                break;
            }
        }
        Assert.That(closeDiffers, Is.True);
    }

    [Test]
    public void SeedFor_IsStableAcrossProcesses()
    {
        int IndependentFnv(int masterSeed, int officeId, string cargoId)
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

        Assert.That(MarketHistory.SeedFor(12345678, 7, "iron"), Is.EqualTo(IndependentFnv(12345678, 7, "iron")));
        // Fixed value: the seed must not depend on process-randomized string hashing.
        Assert.That(MarketHistory.SeedFor(12345678, 7, "iron"), Is.EqualTo(-537164315));
        Assert.That(MarketHistory.SeedFor(1, 2, "iron"), Is.Not.EqualTo(MarketHistory.SeedFor(2, 1, "iron")));
        Assert.That(MarketHistory.SeedFor(1, 2, "iron"), Is.Not.EqualTo(MarketHistory.SeedFor(1, 2, "water")));
    }

    [Test]
    public void SeedPriorDays_SecondCallIsNoOp()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 8);
        MarketHistory.SeedPriorDays(scene.Market);
        var book = scene.Market.GetDataBlob<LogiBaseDB>();
        int version = book.HistoryVersion;
        var snapshot = book.History["iron"].Select(b => b.Copy()).ToList();

        MarketHistory.SeedPriorDays(scene.Market);

        Assert.That(book.History["iron"], Has.Count.EqualTo(snapshot.Count));
        Assert.That(book.HistoryVersion, Is.EqualTo(version));
        for (int i = 0; i < snapshot.Count; i++)
            AssertBucketEquals(book.History["iron"][i], snapshot[i]);
    }

    [Test]
    public void SeedPriorDays_KeepsTodayTradeBucket()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 7);
        scene.Run(scene.Exchange(MarketSide.BuyFromMarket, 12));
        var today = SoleBucket(scene, "iron");
        var start = scene.Market.StarSysDateTime.Date;

        MarketHistory.SeedPriorDays(scene.Market);

        var days = Days(scene, "iron");
        Assert.That(days[days.Count - 1], Is.SameAs(today));
        Assert.That(days[days.Count - 1].Day, Is.EqualTo(start));
        Assert.That(days[days.Count - 1].Open, Is.EqualTo(10m));
        Assert.That(days[days.Count - 1].High, Is.EqualTo(10m));
        Assert.That(days[days.Count - 1].Low, Is.EqualTo(10m));
        Assert.That(days[days.Count - 1].Close, Is.EqualTo(10m));
        Assert.That(days[days.Count - 1].Volume, Is.EqualTo(12));
        Assert.That(days[days.Count - 1].Stock, Is.EqualTo(28));
        Assert.That(days[days.Count - 1].Bid, Is.EqualTo(7m));
        Assert.That(days[days.Count - 1].Ask, Is.EqualTo(10m));
    }

    [Test]
    public void SeedPriorDays_DoesNotMoveCargoOrMoney()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 8);
        long stockBefore = MarketBook.Stock(scene.Market, scene.MarketIron);
        decimal moneyBefore = scene.MarketInfo.Money.GetCurrentFunds();
        decimal bid = scene.Listing.Bid;
        decimal ask = scene.Listing.Ask;
        long sell = scene.Listing.SellQuantity;

        MarketHistory.SeedPriorDays(scene.Market);

        Assert.That(MarketBook.Stock(scene.Market, scene.MarketIron), Is.EqualTo(stockBefore));
        Assert.That(scene.MarketInfo.Money.GetCurrentFunds(), Is.EqualTo(moneyBefore));
        Assert.That(scene.Listing.Bid, Is.EqualTo(bid));
        Assert.That(scene.Listing.Ask, Is.EqualTo(ask));
        Assert.That(scene.Listing.SellQuantity, Is.EqualTo(sell));
    }

    [Test]
    public void SeedPriorDays_SkipsZeroZeroAndRelocationAndUnpriced()
    {
        var zeroZero = Scene.Build(marketStock: 40, sell: 30);
        var zeroStart = zeroZero.Market.StarSysDateTime.Date;
        MarketHistory.SeedPriorDays(zeroZero.Market);
        var zeroDays = Days(zeroZero, "iron");
        Assert.That(zeroDays.Any(b => b.Day.Date < zeroStart), Is.False);

        var reloc = Scene.Build(list: false, marketStock: 20);
        Assert.That(MarketBook.SetListing(reloc.Market, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 20,
            Ask = 10,
            Relocation = true,
        }), Is.True);
        MarketHistory.SeedPriorDays(reloc.Market);
        Assert.That(reloc.Market.GetDataBlob<LogiBaseDB>().History, Is.Empty);

        var unpriced = Scene.Build(list: false);
        MarketHistory.SeedPriorDays(unpriced.Market);
        Assert.That(unpriced.Market.GetDataBlob<LogiBaseDB>().History, Is.Empty);

        var policyOnly = Scene.Build(list: false, marketStock: 20);
        var policy = new ColonyMarketPolicyDB();
        policy.Rows["iron"] = new MarketPolicyRow { CargoId = "iron", Ask = 4, Bid = 0 };
        policyOnly.Market.SetDataBlob(policy);
        MarketHistory.SeedPriorDays(policyOnly.Market);
        var policyDays = Days(policyOnly, "iron");
        Assert.That(policyDays, Has.Count.EqualTo(30));
        foreach (var bucket in policyDays)
        {
            Assert.That(bucket.Ask, Is.EqualTo(bucket.Close));
            Assert.That(bucket.Bid, Is.EqualTo(0m));
        }
        Assert.That(policyDays[29].Close, Is.EqualTo(4.00m));
    }

    [Test]
    public void SeedPriorDays_CloneAndJsonRoundTrip()
    {
        var scene = Scene.Build(marketStock: 40, sell: 30, ask: 10, bid: 8);
        MarketHistory.SeedPriorDays(scene.Market);
        var original = scene.Market.GetDataBlob<LogiBaseDB>();
        Assert.That(original.History["iron"], Has.Count.EqualTo(31));

        var clone = (LogiBaseDB)original.Clone();
        Assert.That(clone.History["iron"], Has.Count.EqualTo(31));
        for (int i = 0; i < 31; i++)
            AssertBucketEquals(clone.History["iron"][i], original.History["iron"][i]);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Objects,
            ContractResolver = new NonPublicResolver(),
        };
        string json = JsonConvert.SerializeObject(original, settings);
        var loaded = JsonConvert.DeserializeObject<LogiBaseDB>(json, settings);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.History, Is.Not.Null);
        Assert.That(loaded.History["iron"], Has.Count.EqualTo(31));
        for (int i = 0; i < 31; i++)
            AssertBucketEquals(loaded.History["iron"][i], original.History["iron"][i]);
    }

    [Test]
    public void NewGame_PostNewGameInitialization_SeedsCeresButNotZeroListings()
    {
        var (game, sol) = Place();
        foreach (var system in game.Systems)
        {
            foreach (var office in system.GetAllEntitiesWithDataBlob<LogiBaseDB>())
            {
                DateTime start = office.StarSysDateTime.Date;
                var book = office.GetDataBlob<LogiBaseDB>();
                if (book.History == null)
                    continue;
                foreach (var days in book.History.Values)
                {
                    if (days == null)
                        continue;
                    Assert.That(days.Any(b => b.Day.Date < start), Is.False);
                }
            }
        }

        game.PostNewGameInitialization();

        var depot = sol.GetAllEntitiesWithDataBlob<ColonyInfoDB>()
            .Single(c => c.GetDataBlob<NameDB>().DefaultName == CeresStart.DepotFactionName);
        var depotBook = depot.GetDataBlob<LogiBaseDB>();
        DateTime depotStart = depot.StarSysDateTime.Date;
        // Anchors follow the energy-priced seeds: iron mid of bid and ask, methalox and water ask only.
        decimal ironMid = Math.Round((EnergyPriceOracle.IronBid(game) + EnergyPriceOracle.IronAsk(game)) / 2, 2, MidpointRounding.AwayFromZero);
        AssertPriorSeed(depotBook, "iron", depotStart, ironMid);
        AssertPriorSeed(depotBook, "methalox", depotStart, EnergyPriceOracle.FuelAsk(game));
        AssertPriorSeed(depotBook, "water", depotStart, EnergyPriceOracle.WaterAsk(game));

        bool foundZeroOnEarthOrLuna = false;
        foreach (var office in sol.GetAllEntitiesWithDataBlob<LogiBaseDB>())
        {
            if (office.Id == depot.Id)
                continue;
            var officeBook = office.GetDataBlob<LogiBaseDB>();
            DateTime officeStart = office.StarSysDateTime.Date;
            foreach (var listing in officeBook.Listings.Values)
            {
                if (listing.Relocation || listing.Bid != 0 || listing.Ask != 0)
                    continue;
                if (HasPricedPolicy(office, listing.CargoId))
                    continue;
                bool hasPrior = officeBook.History != null
                    && officeBook.History.TryGetValue(listing.CargoId, out var days)
                    && days != null
                    && days.Any(b => b.Day.Date < officeStart);
                Assert.That(hasPrior, Is.False, listing.CargoId);

                if (PlanetNamed(office, "Earth") || PlanetNamed(office, "Luna"))
                    foundZeroOnEarthOrLuna = true;
            }
        }
        Assert.That(foundZeroOnEarthOrLuna, Is.True);
    }

    /// <summary>Seeds the iron history, returns copies of the prior days and removes them so the office can be seeded again.</summary>
    static List<PriceBucket> SeedAndTakePrior(Scene scene)
    {
        var start = scene.Market.StarSysDateTime.Date;
        MarketHistory.SeedPriorDays(scene.Market);
        var days = Days(scene, "iron");
        var prior = days.Where(b => b.Day.Date < start).Select(b => b.Copy()).ToList();
        days.RemoveAll(b => b.Day.Date < start);
        return prior;
    }

    static void AssertBucketEquals(PriceBucket actual, PriceBucket expected)
    {
        Assert.That(actual.Day, Is.EqualTo(expected.Day));
        Assert.That(actual.Open, Is.EqualTo(expected.Open));
        Assert.That(actual.High, Is.EqualTo(expected.High));
        Assert.That(actual.Low, Is.EqualTo(expected.Low));
        Assert.That(actual.Close, Is.EqualTo(expected.Close));
        Assert.That(actual.Volume, Is.EqualTo(expected.Volume));
        Assert.That(actual.Bid, Is.EqualTo(expected.Bid));
        Assert.That(actual.Ask, Is.EqualTo(expected.Ask));
        Assert.That(actual.Stock, Is.EqualTo(expected.Stock));
    }

    static void AssertPriorSeed(LogiBaseDB book, string cargoId, DateTime start, decimal lastClose)
    {
        Assert.That(book.History.ContainsKey(cargoId), Is.True);
        var prior = book.History[cargoId].Where(b => b.Day.Date < start).ToList();
        Assert.That(prior, Has.Count.EqualTo(30));
        Assert.That(prior[0].Day.Date, Is.EqualTo(start.AddDays(-30)));
        Assert.That(prior[29].Day.Date, Is.EqualTo(start.AddDays(-1)));
        Assert.That(prior[29].Close, Is.EqualTo(lastClose));
    }

    static bool HasPricedPolicy(Entity office, string cargoId)
    {
        if (!office.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) || policy == null)
            return false;
        if (!policy.Rows.TryGetValue(cargoId, out var row) || row == null)
            return false;
        return row.Bid != 0 || row.Ask != 0;
    }

    static bool PlanetNamed(Entity office, string name)
    {
        if (!office.TryGetDataBlob<ColonyInfoDB>(out var colony) || colony == null)
            return false;
        var planet = colony.PlanetEntity;
        if (planet == null || planet.Manager == null)
            return false;
        if (!planet.TryGetDataBlob<NameDB>(out var nameDb) || nameDb == null)
            return false;
        return nameDb.DefaultName == name;
    }

    static (Game game, StarSystem sol) Place()
    {
        var modLoader = new ModLoader();
        var store = new ModDataStore();
        modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
        modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(game, store.Systems["system-sol"]);
        var player = FactionFactory.CreateBasicFaction(game, "United Earth Corp", "UEC", 0);
        player.FactionOwnerID = player.Id;
        player.GetDataBlob<FactionInfoDB>().KnownSystems.Add("system-sol");
        ColonyFactory.PlaceOwnedColonies(game, store, player, store.Species["species-human"]);
        FactionFactory.PlaceFactions(game, store);
        return (game, game.Systems.Single(s => s.ID == "system-sol"));
    }

    static PriceBucket SoleBucket(Scene scene, string cargoId)
    {
        var days = Days(scene, cargoId);
        Assert.That(days, Has.Count.EqualTo(1));
        return days[0];
    }

    static List<PriceBucket> Days(Scene scene, string cargoId)
    {
        var book = scene.Market.GetDataBlob<LogiBaseDB>();
        Assert.That(book.History, Is.Not.Null);
        Assert.That(book.History.ContainsKey(cargoId), Is.True);
        return book.History[cargoId];
    }

    static void AdvanceDays(Entity office, int days)
    {
        var pulse = office.Manager.ManagerSubpulses;
        var field = typeof(ManagerSubPulse).GetField("_systemLocalDateTime", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field!.SetValue(pulse, office.StarSysDateTime.AddDays(days));
    }

    sealed class Scene
    {
        public Game Game = null!;
        public StarSystem System = null!;
        public Entity Star = null!;
        public Entity ShipFaction = null!;
        public Entity MarketFaction = null!;
        public Entity Ship = null!;
        public Entity Market = null!;
        public ICargoable ShipIron = null!;
        public ICargoable MarketIron = null!;
        public FactionInfoDB ShipInfo = null!;
        public FactionInfoDB MarketInfo = null!;
        public MarketListing Listing = null!;

        public MarketExchangeAction Exchange(MarketSide side, long units)
            => MarketExchangeAction.Create(Ship, Market.Id, "iron", side, units);

        public void Run(MarketExchangeAction action)
        {
            Assert.That(action.IsValidCommand(Ship.Manager.Game), Is.True);
            action.Execute(Ship.Manager.ManagerSubpulses.StarSysDateTime);
        }

        public Entity ExtraOffice(Entity faction, decimal funds = 0)
        {
            var office = Entity.Create(faction.Id);
            var store = new CargoStorageDB("general-storage", 1000);
            System.AddEntity(office, new List<BaseDataBlob>
            {
                store,
                new LogiBaseDB { Capacity = 5 },
                new PositionDB(new Vector3(1.496e11, 0, 0), Star),
                new ActionQueueDB(),
            });
            if (funds > 0)
            {
                var info = faction.GetDataBlob<FactionInfoDB>();
                info.Money.AddIncome(Game.TimePulse.GameGlobalDateTime, TransactionCategory.InitialInvestment, "test", funds);
            }
            return office;
        }

        public static Scene Build(
            bool sameFaction = true,
            bool mutualFriendly = false,
            bool inRange = true,
            bool list = true,
            bool unlockShip = true,
            bool unlockMarket = true,
            double shipVolume = 1000,
            double marketVolume = 1000,
            long marketStock = 0,
            long shipStock = 0,
            long sell = 0,
            decimal ask = 0,
            long buy = 0,
            decimal bid = 0,
            long reserve = 0,
            decimal shipFunds = 0,
            decimal marketFunds = 0)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            modLoader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var shipFaction = FactionFactory.CreateFaction(game, "Haulers");
            var marketFaction = sameFaction ? shipFaction : FactionFactory.CreateFaction(game, "Market");
            var shipInfo = shipFaction.GetDataBlob<FactionInfoDB>();
            var marketInfo = marketFaction.GetDataBlob<FactionInfoDB>();
            if (!sameFaction && mutualFriendly)
            {
                shipInfo.Stances[marketFaction.Id] = FactionStance.Friendly;
                marketInfo.Stances[shipFaction.Id] = FactionStance.Friendly;
                FactionStanceRules.SetGrant(shipInfo, marketFaction.Id, FactionGrants.Trade, true);
                FactionStanceRules.SetGrant(marketInfo, shipFaction.Id, FactionGrants.Trade, true);
            }
            if (unlockShip)
                shipInfo.Data.Unlock("iron");
            if (!sameFaction && unlockMarket)
                marketInfo.Data.Unlock("iron");

            ICargoable? shipIron = shipInfo.Data.CargoGoods.Contains("iron") ? shipInfo.Data.CargoGoods.GetAny("iron") : null;
            ICargoable? marketIron = marketInfo.Data.CargoGoods.Contains("iron") ? marketInfo.Data.CargoGoods.GetAny("iron") : null;

            var system = new StarSystem();
            system.Initialize(game, "Exchange", -1);
            var star = Entity.Create();
            system.AddEntity(star, new List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });

            var here = new Vector3(1.496e11, 0, 0);
            var far = new Vector3(3.0e11, 0, 0);

            var market = Entity.Create(marketFaction.Id);
            var marketStore = new CargoStorageDB("general-storage", marketVolume);
            system.AddEntity(market, new List<BaseDataBlob>
            {
                marketStore,
                new LogiBaseDB { Capacity = 5 },
                new PositionDB(here, star),
                new ActionQueueDB(),
            });

            var ship = Entity.Create(shipFaction.Id);
            var shipStore = new CargoStorageDB("general-storage", shipVolume);
            system.AddEntity(ship, new List<BaseDataBlob>
            {
                shipStore,
                new PositionDB(inRange ? here : far, star),
                new ActionQueueDB(),
            });

            if (marketStock > 0)
                Assert.That(marketStore.AddCargoByUnit(marketIron!, marketStock), Is.EqualTo(marketStock));
            if (shipStock > 0)
                Assert.That(shipStore.AddCargoByUnit(shipIron!, shipStock), Is.EqualTo(shipStock));

            MarketListing? listing = null;
            if (list)
            {
                Assert.That(MarketBook.SetListing(market, new MarketListing
                {
                    CargoId = "iron",
                    SellQuantity = sell,
                    Ask = ask,
                    BuyQuantity = buy,
                    Bid = bid,
                    Reserve = reserve,
                }), Is.True);
                Assert.That(MarketBook.TryGet(market, "iron", out listing), Is.True);
            }

            if (shipFunds > 0)
                shipInfo.Money.AddIncome(game.TimePulse.GameGlobalDateTime, TransactionCategory.InitialInvestment, "test", shipFunds);
            if (marketFunds > 0 && !sameFaction)
                marketInfo.Money.AddIncome(game.TimePulse.GameGlobalDateTime, TransactionCategory.InitialInvestment, "test", marketFunds);

            return new Scene
            {
                Game = game,
                System = system,
                Star = star,
                ShipFaction = shipFaction,
                MarketFaction = marketFaction,
                Ship = ship,
                Market = market,
                ShipIron = shipIron!,
                MarketIron = marketIron!,
                ShipInfo = shipInfo,
                MarketInfo = marketInfo,
                Listing = listing!,
            };
        }
    }
}

[TestFixture]
public class MarketHistoryApiTests : ApiTestBase
{
    [Test]
    public void ProjectEntity_OwnerSeesHistory_ViewerWithoutGrantDoesNot_EmptyIsNull_CacheReused()
    {
        var session = Connect();
        var colony = MakeColony(session.FactionId);
        OfficeWithIron(colony, session.FactionId, stock: 250);

        Assert.That(_projector.ProjectEntity(colony, session.FactionId).GetView<MarketHistoryView>(), Is.Null);

        Assert.That(MarketBook.SetListing(colony, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 150,
            Ask = 11m,
            BuyQuantity = 40,
            Bid = 7m,
            Reserve = 100,
        }), Is.True);

        var owned = _projector.ProjectEntity(colony, session.FactionId);
        var history = owned.GetView<MarketHistoryView>();
        Assert.That(history, Is.Not.Null);
        Assert.That(history!.Goods, Has.Count.EqualTo(1));
        Assert.That(history.Goods[0].CargoId, Is.EqualTo("iron"));
        Assert.That(history.Goods[0].Days, Has.Count.EqualTo(1));
        var point = history.Goods[0].Days[0];
        Assert.That(point.Volume, Is.EqualTo(0));
        Assert.That(point.Ask, Is.EqualTo(11m));
        Assert.That(point.Bid, Is.EqualTo(7m));
        Assert.That(point.Stock, Is.EqualTo(250));

        var again = _projector.ProjectEntity(colony, session.FactionId).GetView<MarketHistoryView>();
        Assert.That(again, Is.SameAs(history));

        var other = OtherFaction(session.FactionId);
        Assert.That(_projector.ProjectEntity(colony, other.Id).HasView<MarketHistoryView>(), Is.False);

        Assert.That(other.TryGetDataBlob<FactionInfoDB>(out var otherInfo), Is.True);
        otherInfo!.Stances[session.FactionId] = FactionStance.Friendly;
        FactionOf(session.FactionId).Stances[other.Id] = FactionStance.Friendly;
        Assert.That(_projector.ProjectEntity(colony, other.Id).HasView<MarketHistoryView>(), Is.False);

        FactionStanceRules.SetGrant(FactionOf(session.FactionId), other.Id, FactionGrants.Trade, true);
        var granted = _projector.ProjectEntity(colony, other.Id).GetView<MarketHistoryView>();
        Assert.That(granted, Is.SameAs(history));
    }

    void OfficeWithIron(Entity colony, int factionId, long stock)
    {
        var info = FactionOf(factionId);
        info.Data.Unlock("iron");
        var store = new CargoStorageDB("general-storage", 100000);
        colony.SetDataBlob(store);
        var iron = info.Data.CargoGoods.GetAny("iron");
        Assert.That(iron, Is.Not.Null);
        Assert.That(store.AddCargoByUnit(iron!, stock), Is.EqualTo(stock));
        colony.SetDataBlob(new LogiBaseDB { Capacity = 5 });
    }

    FactionInfoDB FactionOf(int factionId)
    {
        Assert.That(_game.Factions[factionId].TryGetDataBlob<FactionInfoDB>(out var info), Is.True);
        return info!;
    }

    Entity OtherFaction(int sessionFactionId)
    {
        return _game.Factions.Values.First(f =>
            f.Id != sessionFactionId
            && f.Id != _game.GameMasterFaction.Id
            && f.Id != Game.NeutralFactionId);
    }

    Entity MakeColony(int factionId)
    {
        var faction = _game.Factions[factionId];
        var species = SpeciesFactory.CreateSpeciesHuman(faction, _game.GlobalManager);
        var planet = _game.Systems[0].GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
            .First(b => b.HasDataBlob<MassVolumeDB>() && b.HasDataBlob<NameDB>());
        return ColonyFactory.CreateColony(faction, species, planet, initialPopulation: 9_000_000);
    }
}
