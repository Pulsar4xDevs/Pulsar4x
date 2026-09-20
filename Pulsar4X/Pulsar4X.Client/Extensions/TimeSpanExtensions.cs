using System;

namespace Pulsar4X.Client
{
    public static class TimeSpanExtensions
    {
        private const double DaysInAYear = 365.25;

        public static double ToYears(this TimeSpan timespan)
        {
            return timespan.TotalDays / DaysInAYear;
        }
    }
}