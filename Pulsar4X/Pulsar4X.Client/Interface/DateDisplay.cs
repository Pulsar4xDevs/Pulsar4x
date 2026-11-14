using System;
using System.Globalization;

namespace Pulsar4X.Client
{
    /// <summary>How the client writes a game date (Settings > UI > Date format).</summary>
    public enum DateStyle
    {
        /// <summary>"15 Nov 2026" (d MMM yyyy), the default.</summary>
        DayMonthName = 0,
        /// <summary>"2026-11-15" (ISO yyyy-MM-dd).</summary>
        Iso = 1,
        /// <summary>"15/11/2026" (dd/MM/yyyy).</summary>
        DayFirst = 2,
        /// <summary>"11/15/2026" (MM/dd/yyyy).</summary>
        MonthFirst = 3,
    }

    /// <summary>How the client writes a game time of day (Settings > UI > Time format).</summary>
    public enum TimeStyle
    {
        /// <summary>"14:05" / "14:05:09" (HH:mm[:ss]), the default.</summary>
        TwentyFourHour = 0,
        /// <summary>"2:05 PM" / "2:05:09 PM" (h:mm[:ss] tt).</summary>
        TwelveHour = 1,
    }

    /// <summary>
    /// The one formatter for game dates and times in the client UI. Every window writes dates through this, so the
    /// user's date and time format (<see cref="GameSettings.DateStyle"/>, <see cref="GameSettings.TimeStyle"/>) apply
    /// everywhere. Invariant culture, so month names and AM/PM never depend on the machine's locale.
    /// Display only: save files, serialization and engine logs keep their own formats.
    /// </summary>
    public static class DateDisplay
    {
        /// <summary>The date style in use; set from the client settings on load and on Apply.</summary>
        public static DateStyle Date { get; set; } = DateStyle.DayMonthName;

        /// <summary>The time style in use; set from the client settings on load and on Apply.</summary>
        public static TimeStyle Time { get; set; } = TimeStyle.TwentyFourHour;

        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        /// <summary>Labels for the settings combo, in enum order.</summary>
        public static readonly string[] DateStyleLabels =
        {
            "d MMM yyyy (15 Nov 2026)",
            "ISO yyyy-MM-dd (2026-11-15)",
            "dd/MM/yyyy (15/11/2026)",
            "MM/dd/yyyy (11/15/2026)",
        };

        /// <summary>Labels for the settings combo, in enum order.</summary>
        public static readonly string[] TimeStyleLabels =
        {
            "24-hour (14:05)",
            "12-hour (2:05 PM)",
        };

        /// <summary>The .NET format string for a date style.</summary>
        public static string DateFormat(DateStyle style) => style switch
        {
            DateStyle.Iso => "yyyy-MM-dd",
            DateStyle.DayFirst => "dd/MM/yyyy",
            DateStyle.MonthFirst => "MM/dd/yyyy",
            _ => "d MMM yyyy",
        };

        /// <summary>The .NET format string for a time style, with or without seconds.</summary>
        public static string TimeFormat(TimeStyle style, bool seconds = false) => style switch
        {
            TimeStyle.TwelveHour => seconds ? "h:mm:ss tt" : "h:mm tt",
            _ => seconds ? "HH:mm:ss" : "HH:mm",
        };

        public static string FormatDate(DateTime value, DateStyle style) => value.ToString(DateFormat(style), Invariant);

        public static string FormatTime(DateTime value, TimeStyle style, bool seconds = false) => value.ToString(TimeFormat(style, seconds), Invariant);

        public static string FormatDateTime(DateTime value, DateStyle date, TimeStyle time, bool seconds = false)
            => FormatDate(value, date) + " " + FormatTime(value, time, seconds);

        /// <summary>A game date in the user's format: "15 Nov 2026" by default.</summary>
        public static string FormatDate(DateTime value) => FormatDate(value, Date);

        /// <summary>A game time of day in the user's format: "14:05" by default ("14:05:09" with seconds).</summary>
        public static string FormatTime(DateTime value, bool seconds = false) => FormatTime(value, Time, seconds);

        /// <summary>A game date and time in the user's format: "15 Nov 2026 14:05" by default.</summary>
        public static string FormatDateTime(DateTime value, bool seconds = false) => FormatDateTime(value, Date, Time, seconds);

        /// <summary><see cref="FormatDate(DateTime)"/>, or <paramref name="none"/> for a missing date.</summary>
        public static string FormatDate(DateTime? value, string none) => value is { } v ? FormatDate(v) : none;

        /// <summary>The widest date-time the current styles can show (28 Dec, 23:59:59), for sizing columns so no date is clipped.</summary>
        public static string WidestDateTimeSample(bool seconds = false) => FormatDateTime(new DateTime(2050, 12, 28, 23, 59, 59), seconds);
    }
}
