using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

[TestFixture]
public class MarketChartDataTests
{
    static readonly DateTime Now = new DateTime(2045, 11, 12);

    [Test]
    public void PresetRanges_AreInclusiveOfToday()
    {
        var week = MarketChartData.Preset(MarketChartPreset.Week, Now.AddHours(6), null);
        Assert.That(week.From, Is.EqualTo(new DateTime(2045, 11, 6)));
        Assert.That(week.To, Is.EqualTo(Now));

        var month = MarketChartData.Preset(MarketChartPreset.Month, Now, null);
        Assert.That(month.From, Is.EqualTo(new DateTime(2045, 10, 12)));
        Assert.That(month.To, Is.EqualTo(Now));

        var threeMonths = MarketChartData.Preset(MarketChartPreset.ThreeMonths, Now, null);
        Assert.That(threeMonths.From, Is.EqualTo(new DateTime(2045, 8, 12)));
        Assert.That(threeMonths.To, Is.EqualTo(Now));

        var year = MarketChartData.Preset(MarketChartPreset.Year, Now, null);
        Assert.That(year.From, Is.EqualTo(new DateTime(2044, 11, 12)));
        Assert.That(year.To, Is.EqualTo(Now));

        var earliest = new DateTime(2040, 1, 5, 15, 0, 0);
        var all = MarketChartData.Preset(MarketChartPreset.All, Now, earliest);
        Assert.That(all.From, Is.EqualTo(new DateTime(2040, 1, 5)));
        Assert.That(all.To, Is.EqualTo(Now));

        var allEmpty = MarketChartData.Preset(MarketChartPreset.All, Now, null);
        Assert.That(allEmpty.From, Is.EqualTo(Now));
        Assert.That(allEmpty.To, Is.EqualTo(Now));

        var future = MarketChartData.Preset(MarketChartPreset.All, Now, Now.AddDays(3));
        Assert.That(future.From, Is.EqualTo(Now));
        Assert.That(future.To, Is.EqualTo(Now));
    }

    [Test]
    public void BucketSize_FollowsTheSpan()
    {
        DateTime origin = new DateTime(2045, 1, 1);
        Assert.That(MarketChartData.BucketSizeFor(origin, origin), Is.EqualTo(MarketBucketSize.Day));
        Assert.That(MarketChartData.BucketSizeFor(origin, origin.AddDays(92)), Is.EqualTo(MarketBucketSize.Day));
        Assert.That(MarketChartData.BucketSizeFor(origin, origin.AddDays(93)), Is.EqualTo(MarketBucketSize.Week));
        Assert.That(MarketChartData.BucketSizeFor(origin, origin.AddDays(731)), Is.EqualTo(MarketBucketSize.Week));
        Assert.That(MarketChartData.BucketSizeFor(origin, origin.AddDays(732)), Is.EqualTo(MarketBucketSize.Month));
    }

    [Test]
    public void DailyBuckets_KeepThatDaysOhlcAndVolume()
    {
        var days = new List<PricePointView>
        {
            Point(new DateTime(2045, 10, 1), 10m, 12m, 9m, 11m, 4, 10m, 12m, 50),
            Point(new DateTime(2045, 10, 2), 11m, 14m, 10m, 13m, 7, 12m, 14m, 40),
        };

        var buckets = MarketChartData.Aggregate(
            days, new DateTime(2045, 10, 1), new DateTime(2045, 10, 2), MarketBucketSize.Day);

        Assert.That(buckets, Has.Count.EqualTo(2));
        AssertCandle(buckets[0], 10m, 12m, 9m, 11m, 4);
        Assert.That(buckets[0].Bid, Is.EqualTo(10m));
        Assert.That(buckets[0].Ask, Is.EqualTo(12m));
        Assert.That(buckets[0].Stock, Is.EqualTo(50));
        AssertCandle(buckets[1], 11m, 14m, 10m, 13m, 7);
        Assert.That(buckets[1].Line, Is.EqualTo(13m));
        Assert.That(buckets[1].QuoteMid, Is.False);
    }

    [Test]
    public void WeeklyBuckets_CombineTradedDaysAndIgnoreQuoteOhlc()
    {
        var days = new List<PricePointView>
        {
            Point(new DateTime(2045, 10, 6), 10m, 12m, 9m, 11m, 4, 10m, 12m, 50),
            Point(new DateTime(2045, 10, 8), 11m, 15m, 8m, 14m, 6, 13m, 15m, 40),
            Point(new DateTime(2045, 10, 9), 1m, 1m, 1m, 1m, 0, 13m, 16m, 41),
            Point(new DateTime(2045, 10, 13), 14m, 16m, 13m, 15m, 3, 14m, 17m, 30),
        };

        var buckets = MarketChartData.Aggregate(
            days, new DateTime(2045, 10, 6), new DateTime(2045, 10, 19), MarketBucketSize.Week);

        Assert.That(buckets, Has.Count.EqualTo(2));
        Assert.That(buckets[0].Start, Is.EqualTo(new DateTime(2045, 10, 6)));
        Assert.That(buckets[0].End, Is.EqualTo(new DateTime(2045, 10, 12)));
        AssertCandle(buckets[0], 10m, 15m, 8m, 14m, 10);
        Assert.That(buckets[0].Bid, Is.EqualTo(13m));
        Assert.That(buckets[0].Ask, Is.EqualTo(16m));
        Assert.That(buckets[0].Stock, Is.EqualTo(41));
        Assert.That(buckets[0].Line, Is.EqualTo(14m));

        Assert.That(buckets[1].Start, Is.EqualTo(new DateTime(2045, 10, 13)));
        Assert.That(buckets[1].End, Is.EqualTo(new DateTime(2045, 10, 19)));
        AssertCandle(buckets[1], 14m, 16m, 13m, 15m, 3);
        Assert.That(buckets[1].Line, Is.EqualTo(15m));
    }

    [Test]
    public void GapDays_CarryTheCloseFlatWithZeroVolume()
    {
        var days = new List<PricePointView>
        {
            Point(new DateTime(2045, 10, 1), 10m, 10m, 10m, 10m, 5, 9m, 11m, 100),
            Point(new DateTime(2045, 10, 3), 12m, 13m, 11m, 12m, 2, 11m, 13m, 90),
        };

        var buckets = MarketChartData.Aggregate(
            days, new DateTime(2045, 10, 1), new DateTime(2045, 10, 4), MarketBucketSize.Day);

        Assert.That(buckets, Has.Count.EqualTo(4));
        AssertCandle(buckets[0], 10m, 10m, 10m, 10m, 5);

        Assert.That(buckets[1].HasCandle, Is.False);
        Assert.That(buckets[1].Volume, Is.EqualTo(0));
        Assert.That(buckets[1].Line, Is.EqualTo(10m));
        Assert.That(buckets[1].QuoteMid, Is.False);
        Assert.That(buckets[1].Bid, Is.EqualTo(9m));
        Assert.That(buckets[1].Ask, Is.EqualTo(11m));
        Assert.That(buckets[1].Stock, Is.EqualTo(100));

        AssertCandle(buckets[2], 12m, 13m, 11m, 12m, 2);
        Assert.That(buckets[3].HasCandle, Is.False);
        Assert.That(buckets[3].Volume, Is.EqualTo(0));
        Assert.That(buckets[3].Line, Is.EqualTo(12m));
        Assert.That(buckets[3].Bid, Is.EqualTo(11m));
        Assert.That(buckets[3].Ask, Is.EqualTo(13m));
        Assert.That(buckets[3].Stock, Is.EqualTo(90));
    }

    [Test]
    public void QuoteOnlyBucket_ProducesNoCandle()
    {
        var days = new List<PricePointView>
        {
            Point(new DateTime(2045, 10, 1), 10m, 11m, 9m, 10m, 4, 9m, 11m, 20),
            Point(new DateTime(2045, 10, 2), 99m, 99m, 1m, 50m, 0, 8m, 12m, 20),
        };

        var buckets = MarketChartData.Aggregate(
            days, new DateTime(2045, 10, 1), new DateTime(2045, 10, 2), MarketBucketSize.Day);

        Assert.That(buckets[1].HasCandle, Is.False);
        Assert.That(buckets[1].Volume, Is.EqualTo(0));
        Assert.That(buckets[1].Line, Is.EqualTo(10m));
        Assert.That(buckets[1].QuoteMid, Is.False);
        Assert.That(buckets[1].Bid, Is.EqualTo(8m));
        Assert.That(buckets[1].Ask, Is.EqualTo(12m));
    }

    [Test]
    public void NeverTraded_LineIsTheQuoteMid()
    {
        var days = new List<PricePointView>
        {
            Point(new DateTime(2045, 10, 1), 99m, 99m, 1m, 50m, 0, 4m, 6m, 8),
        };

        var buckets = MarketChartData.Aggregate(
            days, new DateTime(2045, 10, 1), new DateTime(2045, 10, 1), MarketBucketSize.Day);

        Assert.That(buckets, Has.Count.EqualTo(1));
        Assert.That(buckets[0].HasCandle, Is.False);
        Assert.That(buckets[0].Volume, Is.EqualTo(0));
        Assert.That(buckets[0].Line, Is.EqualTo(5m));
        Assert.That(buckets[0].QuoteMid, Is.True);
    }

    [Test]
    public void Index_RescalesTo100AtTheFirstPoint()
    {
        var indexed = MarketChartData.Index(new decimal?[] { 2m, 2.5m, 1.5m });
        Assert.That(indexed, Has.Count.EqualTo(3));
        Assert.That(indexed[0], Is.EqualTo(100m));
        Assert.That(indexed[1], Is.EqualTo(125m));
        Assert.That(indexed[2], Is.EqualTo(75m));
    }

    [Test]
    public void EmptyHistory_ProducesAnEmptySeries()
    {
        DateTime from = new DateTime(2045, 10, 1);
        DateTime to = new DateTime(2045, 10, 8);
        Assert.That(MarketChartData.Aggregate(null, from, to, MarketBucketSize.Day), Is.Empty);
        Assert.That(MarketChartData.Aggregate(new List<PricePointView>(), from, to, MarketBucketSize.Day), Is.Empty);
        Assert.That(MarketChartData.Index(null), Is.Empty);
        Assert.That(MarketChartData.Index(Array.Empty<decimal?>()), Is.Empty);

        var after = new List<PricePointView>
        {
            Point(new DateTime(2045, 11, 1), 1m, 1m, 1m, 1m, 1, 1m, 2m, 1),
        };
        Assert.That(MarketChartData.Aggregate(after, from, to, MarketBucketSize.Day), Is.Empty);
    }

    [Test]
    public void ToggleCompare_KeepsEarlierChecksAndTogglesTheClickedGood()
    {
        var checkedIds = new HashSet<string> { "iron" };

        Assert.That(MarketChartData.ToggleCompare(checkedIds, "methalox"), Is.True);
        Assert.That(MarketChartData.ToggleCompare(checkedIds, "silicon"), Is.True);
        Assert.That(checkedIds, Is.EquivalentTo(new[] { "iron", "methalox", "silicon" }));

        Assert.That(MarketChartData.ToggleCompare(checkedIds, "iron"), Is.False);
        Assert.That(checkedIds, Is.EquivalentTo(new[] { "methalox", "silicon" }));
    }

    [Test]
    public void ZeroPriceTrade_IsACandleWithItsVolume()
    {
        var days = new List<PricePointView>
        {
            Point(new DateTime(2050, 1, 2), 0m, 0m, 0m, 0m, 10, 0m, 0m, 990),
        };

        var buckets = MarketChartData.Aggregate(
            days, new DateTime(2050, 1, 1), new DateTime(2050, 1, 3), MarketBucketSize.Day);

        Assert.That(buckets[0].HasCandle, Is.False);
        AssertCandle(buckets[1], 0m, 0m, 0m, 0m, 10);
        Assert.That(buckets[2].HasCandle, Is.False);
    }

    [Test]
    public void HoverIndex_SnapsToANearbyTradeOnlyWhenSlotsAreNarrow()
    {
        var days = new List<PricePointView>
        {
            Point(new DateTime(2050, 1, 1), 5m, 5m, 5m, 5m, 10, 4m, 6m, 100),
        };
        var buckets = MarketChartData.Aggregate(
            days, new DateTime(2050, 1, 1), new DateTime(2050, 1, 31), MarketBucketSize.Day);

        // 4 px slots: the mouse over day 3 (x = 10) is within 8 px of the day 1 candle centre (x = 2).
        Assert.That(MarketChartData.HoverIndex(buckets, 10f, 4f, 8f), Is.EqualTo(0));
        // Far from any trade it stays on the day under the mouse.
        Assert.That(MarketChartData.HoverIndex(buckets, 41f, 4f, 8f), Is.EqualTo(10));
        // Wide slots never snap.
        Assert.That(MarketChartData.HoverIndex(buckets, 30f, 20f, 8f), Is.EqualTo(1));
        // Off the plot.
        Assert.That(MarketChartData.HoverIndex(buckets, -1f, 4f, 8f), Is.EqualTo(-1));
        Assert.That(MarketChartData.HoverIndex(buckets, 4f * 31 + 1f, 4f, 8f), Is.EqualTo(-1));
    }

    static PricePointView Point(
        DateTime day, decimal open, decimal high, decimal low, decimal close, long volume,
        decimal bid, decimal ask, long stock)
    {
        return new PricePointView(day, open, high, low, close, volume, bid, ask, stock);
    }

    static void AssertCandle(MarketChartBucket bucket, decimal open, decimal high, decimal low, decimal close, long volume)
    {
        Assert.That(bucket.HasCandle, Is.True);
        Assert.That(bucket.Open, Is.EqualTo(open));
        Assert.That(bucket.High, Is.EqualTo(high));
        Assert.That(bucket.Low, Is.EqualTo(low));
        Assert.That(bucket.Close, Is.EqualTo(close));
        Assert.That(bucket.Volume, Is.EqualTo(volume));
    }
}
