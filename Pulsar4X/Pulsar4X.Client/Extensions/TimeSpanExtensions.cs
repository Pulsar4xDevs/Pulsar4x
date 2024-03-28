using System;

namespace Pulsar4X.ImGuiNetUI
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