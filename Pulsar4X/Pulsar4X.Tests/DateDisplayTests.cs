using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>The client's one date/time formatter (DateDisplay) and its settings.</summary>
[TestFixture]
[NonParallelizable]
public class DateDisplayTests
{
    static readonly DateTime When = new(2026, 11, 5, 14, 7, 9);

    [Test]
    public void Defaults_AreDayMonthName_And24Hour()
    {
        var settings = new GameSettings();
        Assert.That(settings.DateStyle, Is.EqualTo(DateStyle.DayMonthName));
        Assert.That(settings.TimeStyle, Is.EqualTo(TimeStyle.TwentyFourHour));
        Assert.That(DateDisplay.FormatDate(When, settings.DateStyle), Is.EqualTo("5 Nov 2026"));
        Assert.That(DateDisplay.FormatDateTime(When, settings.DateStyle, settings.TimeStyle), Is.EqualTo("5 Nov 2026 14:07"));
    }

    [TestCase(DateStyle.DayMonthName, "5 Nov 2026")]
    [TestCase(DateStyle.Iso, "2026-11-05")]
    [TestCase(DateStyle.DayFirst, "05/11/2026")]
    [TestCase(DateStyle.MonthFirst, "11/05/2026")]
    public void EachDateStyle(DateStyle style, string expected)
        => Assert.That(DateDisplay.FormatDate(When, style), Is.EqualTo(expected));

    [TestCase(TimeStyle.TwentyFourHour, false, "14:07")]
    [TestCase(TimeStyle.TwentyFourHour, true, "14:07:09")]
    [TestCase(TimeStyle.TwelveHour, false, "2:07 PM")]
    [TestCase(TimeStyle.TwelveHour, true, "2:07:09 PM")]
    public void EachTimeStyle(TimeStyle style, bool seconds, string expected)
        => Assert.That(DateDisplay.FormatTime(When, style, seconds), Is.EqualTo(expected));

    [Test]
    public void MonthNames_AndAmPm_AreInvariant_WhateverTheMachineCulture()
    {
        var saved = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.That(DateDisplay.FormatDate(new DateTime(2050, 3, 1), DateStyle.DayMonthName), Is.EqualTo("1 Mar 2050"), "not 'Mrz'");
            Assert.That(DateDisplay.FormatDate(new DateTime(2050, 3, 1), DateStyle.DayFirst), Is.EqualTo("01/03/2050"), "'/' stays '/', not the culture's '.'");
            Assert.That(DateDisplay.FormatTime(new DateTime(2050, 3, 1, 0, 30, 0), TimeStyle.TwelveHour), Is.EqualTo("12:30 AM"));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Test]
    public void TheSettings_DriveTheSharedFormatter()
    {
        var (date, time) = (DateDisplay.Date, DateDisplay.Time);
        try
        {
            var settings = new GameSettings { DateStyle = DateStyle.MonthFirst, TimeStyle = TimeStyle.TwelveHour };
            settings.ApplyDateDisplay();
            Assert.That(DateDisplay.FormatDate(When), Is.EqualTo("11/05/2026"));
            Assert.That(DateDisplay.FormatDateTime(When, seconds: true), Is.EqualTo("11/05/2026 2:07:09 PM"));
            Assert.That(settings.GetDateTimeFormat(), Is.EqualTo("MM/dd/yyyy h:mm:ss tt"));
            Assert.That(DateDisplay.FormatDate((DateTime?)null, "-"), Is.EqualTo("-"));
            // An unknown value from a hand-edited settings file falls back to the defaults.
            new GameSettings { DateStyle = (DateStyle)42, TimeStyle = (TimeStyle)9 }.ApplyDateDisplay();
            Assert.That(DateDisplay.FormatDateTime(When), Is.EqualTo("5 Nov 2026 14:07"));
        }
        finally
        {
            DateDisplay.Date = date;
            DateDisplay.Time = time;
        }
    }

    [Test]
    public void TheSettingsFile_RoundTripsTheFormats()
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(new GameSettings { DateStyle = DateStyle.Iso, TimeStyle = TimeStyle.TwelveHour });
        var back = Newtonsoft.Json.JsonConvert.DeserializeObject<GameSettings>(json)!;
        Assert.That(back.DateStyle, Is.EqualTo(DateStyle.Iso));
        Assert.That(back.TimeStyle, Is.EqualTo(TimeStyle.TwelveHour));
        var old = Newtonsoft.Json.JsonConvert.DeserializeObject<GameSettings>("{\"EuropeClock\": true}")!;
        Assert.That(old.DateStyle, Is.EqualTo(DateStyle.DayMonthName), "an old settings file loads with the defaults");
    }

    [Test]
    public void TheLongestCombination_IsDayMonthName_With12HourSeconds()
    {
        // For layout checks: the widest date+time string the settings can produce.
        int Widest(DateStyle d, TimeStyle t) => DateDisplay.FormatDateTime(new DateTime(2050, 12, 28, 23, 59, 59), d, t, seconds: true).Length;
        int best = 0;
        foreach (DateStyle d in Enum.GetValues(typeof(DateStyle)))
            foreach (TimeStyle t in Enum.GetValues(typeof(TimeStyle)))
                best = Math.Max(best, Widest(d, t));
        Assert.That(Widest(DateStyle.DayMonthName, TimeStyle.TwelveHour), Is.EqualTo(best));
    }

    [Test]
    public void WidestSample_IsAtLeastAsLongAsAnyDateInTheYear_ForEveryStyle()
    {
        var (date, time) = (DateDisplay.Date, DateDisplay.Time);
        try
        {
            foreach (DateStyle d in Enum.GetValues(typeof(DateStyle)))
                foreach (TimeStyle t in Enum.GetValues(typeof(TimeStyle)))
                {
                    DateDisplay.Date = d;
                    DateDisplay.Time = t;
                    int widest = DateDisplay.WidestDateTimeSample(seconds: true).Length;
                    for (var day = new DateTime(2050, 1, 1, 10, 0, 0); day.Year == 2050; day = day.AddDays(1))
                        Assert.That(DateDisplay.FormatDateTime(day, seconds: true).Length, Is.LessThanOrEqualTo(widest), d + "/" + t + " " + day);
                }
        }
        finally
        {
            (DateDisplay.Date, DateDisplay.Time) = (date, time);
        }
    }
}
