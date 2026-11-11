using System;
using System.Collections.Generic;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    public enum MarketChartPreset
    {
        Week,
        Month,
        ThreeMonths,
        Year,
        All,
    }

    public enum MarketBucketSize
    {
        Day,
        Week,
        Month,
    }

    public readonly record struct MarketChartRange(DateTime From, DateTime To);

    /// <summary>
    /// One bucket on the price chart. A bucket with no trades has <see cref="HasCandle"/> false:
    /// its open/high/low/close are meaningless. <see cref="Line"/> is the carried-forward close,
    /// or the bid/ask mid when the good has never traded.
    /// </summary>
    public readonly record struct MarketChartBucket(
        DateTime Start,
        DateTime End,
        bool HasCandle,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        long Volume,
        decimal Bid,
        decimal Ask,
        long Stock,
        bool HasQuote,
        decimal? Line,
        bool QuoteMid);

    public readonly record struct MarketRangeChange(decimal Absolute, decimal Percent, bool HasPercent);

    /// <summary>
    /// Buckets daily price points for the market chart. Days the history omits are carried forward
    /// flat with zero volume. A point with zero volume is a quote only: its open/high/low/close are ignored.
    /// </summary>
    public static class MarketChartData
    {
        public static MarketChartRange Preset(MarketChartPreset preset, DateTime now, DateTime? earliest)
        {
            DateTime today = now.Date;
            DateTime from;
            if (preset == MarketChartPreset.Week)
                from = today.AddDays(-6);
            else if (preset == MarketChartPreset.Month)
                from = today.AddMonths(-1);
            else if (preset == MarketChartPreset.ThreeMonths)
                from = today.AddMonths(-3);
            else if (preset == MarketChartPreset.Year)
                from = today.AddYears(-1);
            else
                from = earliest?.Date ?? today;
            if (from > today)
                from = today;
            return new MarketChartRange(from, today);
        }

        /// <summary>Daily through about three months, weekly through about two years, monthly after that.</summary>
        public static MarketBucketSize BucketSizeFor(DateTime from, DateTime to)
        {
            int days = SpanDays(from, to);
            if (days <= 92)
                return MarketBucketSize.Day;
            if (days <= 731)
                return MarketBucketSize.Week;
            return MarketBucketSize.Month;
        }

        public static IReadOnlyList<MarketChartBucket> Aggregate(
            IReadOnlyList<PricePointView>? days, DateTime from, DateTime to, MarketBucketSize size)
        {
            if (days == null || days.Count == 0)
                return Array.Empty<MarketChartBucket>();

            DateTime start = from.Date;
            DateTime end = to.Date;
            if (end < start)
            {
                DateTime swap = start;
                start = end;
                end = swap;
            }

            var ordered = new List<PricePointView>(days.Count);
            bool anyOnOrBeforeEnd = false;
            foreach (var day in days)
            {
                if (day == null)
                    continue;
                ordered.Add(day);
                if (day.Day.Date <= end)
                    anyOnOrBeforeEnd = true;
            }
            if (!anyOnOrBeforeEnd || ordered.Count == 0)
                return Array.Empty<MarketChartBucket>();
            ordered.Sort(CompareDays);

            var bounds = BuildBounds(start, end, size);
            var result = new List<MarketChartBucket>(bounds.Count);

            decimal? lastClose = null;
            decimal bid = 0;
            decimal ask = 0;
            long stock = 0;
            bool hasQuote = false;
            int index = 0;
            while (index < ordered.Count && ordered[index].Day.Date < start)
            {
                var prior = ordered[index];
                bid = prior.Bid;
                ask = prior.Ask;
                stock = prior.Stock;
                hasQuote = true;
                if (prior.Volume > 0)
                    lastClose = prior.Close;
                index++;
            }

            for (int b = 0; b < bounds.Count; b++)
            {
                DateTime bucketStart = bounds[b].Start;
                DateTime bucketEnd = bounds[b].End;
                bool hasCandle = false;
                decimal open = 0;
                decimal high = 0;
                decimal low = 0;
                decimal close = 0;
                long volume = 0;

                while (index < ordered.Count && ordered[index].Day.Date <= bucketEnd)
                {
                    var point = ordered[index];
                    index++;
                    if (point.Day.Date < bucketStart)
                        continue;
                    bid = point.Bid;
                    ask = point.Ask;
                    stock = point.Stock;
                    hasQuote = true;
                    volume += point.Volume;
                    if (point.Volume <= 0)
                        continue;
                    if (!hasCandle)
                    {
                        open = point.Open;
                        high = point.High;
                        low = point.Low;
                        close = point.Close;
                        hasCandle = true;
                    }
                    else
                    {
                        if (point.High > high)
                            high = point.High;
                        if (point.Low < low)
                            low = point.Low;
                        close = point.Close;
                    }
                    lastClose = point.Close;
                }

                decimal? line = null;
                bool quoteMid = false;
                if (lastClose is decimal traded)
                {
                    line = traded;
                }
                else if (hasQuote)
                {
                    line = (bid + ask) / 2m;
                    quoteMid = true;
                }

                result.Add(new MarketChartBucket(
                    bucketStart,
                    bucketEnd,
                    hasCandle,
                    open,
                    high,
                    low,
                    close,
                    volume,
                    bid,
                    ask,
                    stock,
                    hasQuote,
                    line,
                    quoteMid));
            }

            return result;
        }

        /// <summary>
        /// Rescales each value so the first one in the list is 100.
        /// A zero first value is left unchanged: it cannot be the base of an index.
        /// </summary>
        public static IReadOnlyList<decimal?> Index(IReadOnlyList<decimal?>? values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<decimal?>();

            decimal? first = null;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] != null)
                {
                    first = values[i];
                    break;
                }
            }
            if (first == null)
            {
                var blanks = new decimal?[values.Count];
                return blanks;
            }

            var scaled = new decimal?[values.Count];
            if (first.Value == 0)
            {
                for (int i = 0; i < values.Count; i++)
                    scaled[i] = values[i];
                return scaled;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] is decimal value)
                    scaled[i] = 100m * value / first.Value;
            }
            return scaled;
        }

        /// <summary>
        /// Adds or removes a good from the compare set. Returns true when the good is now checked.
        /// </summary>
        public static bool ToggleCompare(ISet<string> checkedIds, string cargoId)
        {
            if (checkedIds.Remove(cargoId))
                return false;
            checkedIds.Add(cargoId);
            return true;
        }

        /// <summary>
        /// The bucket under the mouse. When slots are narrower than <paramref name="snapPx"/>, a nearby bucket
        /// with a trade wins over an empty one, so a thin candle can be hovered. Returns -1 off the plot.
        /// </summary>
        public static int HoverIndex(IReadOnlyList<MarketChartBucket> buckets, float rel, float slot, float snapPx)
        {
            if (buckets == null || buckets.Count == 0 || slot <= 0f || rel < 0f || rel > slot * buckets.Count)
                return -1;
            int hover = (int)(rel / slot);
            if (hover >= buckets.Count)
                hover = buckets.Count - 1;
            if (buckets[hover].HasCandle || slot >= snapPx)
                return hover;
            int best = hover;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < buckets.Count; i++)
            {
                if (!buckets[i].HasCandle)
                    continue;
                float distance = Math.Abs((i + 0.5f) * slot - rel);
                if (distance <= snapPx && distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }

        public static bool TryLastTrade(IReadOnlyList<PricePointView>? days, out decimal close)
        {
            close = 0;
            if (days == null || days.Count == 0)
                return false;
            bool found = false;
            DateTime best = DateTime.MinValue;
            foreach (var day in days)
            {
                if (day == null || day.Volume <= 0)
                    continue;
                if (!found || day.Day >= best)
                {
                    found = true;
                    best = day.Day;
                    close = day.Close;
                }
            }
            return found;
        }

        /// <summary>First traded close in the range to the last traded close in the range.</summary>
        public static bool TryRangeChange(
            IReadOnlyList<PricePointView>? days, DateTime from, DateTime to, out MarketRangeChange change)
        {
            change = default;
            if (days == null || days.Count == 0)
                return false;

            DateTime start = from.Date;
            DateTime end = to.Date;
            if (end < start)
            {
                DateTime swap = start;
                start = end;
                end = swap;
            }

            bool any = false;
            DateTime firstDay = DateTime.MaxValue;
            DateTime lastDay = DateTime.MinValue;
            decimal firstClose = 0;
            decimal lastClose = 0;
            foreach (var day in days)
            {
                if (day == null || day.Volume <= 0)
                    continue;
                DateTime date = day.Day.Date;
                if (date < start || date > end)
                    continue;
                if (!any || date < firstDay)
                {
                    firstDay = date;
                    firstClose = day.Close;
                }
                if (!any || date >= lastDay)
                {
                    lastDay = date;
                    lastClose = day.Close;
                }
                any = true;
            }
            if (!any)
                return false;

            decimal absolute = lastClose - firstClose;
            bool hasPercent = firstClose != 0;
            decimal percent = hasPercent ? absolute / firstClose * 100m : 0;
            change = new MarketRangeChange(absolute, percent, hasPercent);
            return true;
        }

        static int SpanDays(DateTime from, DateTime to)
        {
            DateTime start = from.Date;
            DateTime end = to.Date;
            if (end < start)
            {
                DateTime swap = start;
                start = end;
                end = swap;
            }
            return (end - start).Days;
        }

        static int CompareDays(PricePointView a, PricePointView b)
        {
            return a.Day.CompareTo(b.Day);
        }

        static List<(DateTime Start, DateTime End)> BuildBounds(DateTime from, DateTime to, MarketBucketSize size)
        {
            var bounds = new List<(DateTime Start, DateTime End)>();
            if (size == MarketBucketSize.Day)
            {
                for (DateTime day = from; day <= to; day = day.AddDays(1))
                    bounds.Add((day, day));
                return bounds;
            }

            if (size == MarketBucketSize.Week)
            {
                DateTime cursor = from;
                while (cursor <= to)
                {
                    DateTime bucketEnd = cursor.AddDays(6);
                    if (bucketEnd > to)
                        bucketEnd = to;
                    bounds.Add((cursor, bucketEnd));
                    cursor = bucketEnd.AddDays(1);
                }
                return bounds;
            }

            DateTime monthCursor = from;
            while (monthCursor <= to)
            {
                int daysInMonth = DateTime.DaysInMonth(monthCursor.Year, monthCursor.Month);
                DateTime monthEnd = new DateTime(monthCursor.Year, monthCursor.Month, daysInMonth);
                if (monthEnd > to)
                    monthEnd = to;
                bounds.Add((monthCursor, monthEnd));
                monthCursor = monthEnd.AddDays(1);
            }
            return bounds;
        }
    }
}
