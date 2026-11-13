using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Tests;

/// <summary>The crew UI rows (Client side of crew plan phases 1-3), built from Api views only.</summary>
[TestFixture]
public class CrewUiTests
{
    static ShipView Ship(int required, int berths, bool tracked = false, int aboard = 0, double factor = 1, double engine = 1, bool under = false)
        => new ShipView("Test", required) with
        {
            CrewBerths = berths,
            CrewTracked = tracked,
            CrewAboard = aboard,
            CrewFactor = factor,
            EngineCrewFactor = engine,
            UnderCrewed = under,
            CrewByComponent = new[] { new ShipCrewRow("a", "Laser", 2, 5) },
            BerthsByComponent = new[] { new ShipBerthRow("q", "Crew Quarters", 1, berths) },
        };

    [Test]
    public void ShipRows_HideAboardAndFractions_UntilCrewIsTracked()
    {
        var rows = CrewBars.ShipRows(Ship(28, 10));
        Assert.That(rows.Select(r => r.Id), Is.EqualTo(new[] { "required", "berths" }));

        var tracked = CrewBars.ShipRows(Ship(28, 30, tracked: true, aboard: 14, factor: 0.5, engine: 0.6, under: true));
        Assert.That(tracked.Select(r => r.Id), Is.EqualTo(new[] { "required", "berths", "aboard", "engines" }));
        var aboard = tracked.Single(r => r.Id == "aboard");
        Assert.That(aboard.Value, Is.EqualTo("14/28"));
        Assert.That(aboard.Fill, Is.EqualTo(0.5f));
        Assert.That(aboard.Tooltip, Does.Contain("50%").And.Contain("25%"));
        Assert.That(tracked.Single(r => r.Id == "engines").Value, Is.EqualTo("60%"));
    }

    [Test]
    public void BerthRow_FlagsTooFewBerths()
    {
        var berths = CrewBars.ShipRows(Ship(28, 10)).Single(r => r.Id == "berths");
        Assert.That(berths.Tooltip, Does.Contain("18 berths short"));
        Assert.That(berths.Fill, Is.EqualTo(10f / 28f).Within(1e-6));
        Assert.That(berths.Detail.Single().Value, Is.EqualTo("10"));

        var enough = CrewBars.ShipRows(Ship(28, 30)).Single(r => r.Id == "berths");
        Assert.That(enough.Tooltip, Does.Not.Contain("short"));
        Assert.That(enough.Fill, Is.EqualTo(1f));
    }

    [Test]
    public void Fill_IsFullWhenNothingIsNeeded()
    {
        Assert.That(CrewBars.Fill(0, 0), Is.EqualTo(1f));
        Assert.That(CrewBars.Fill(5, 10), Is.EqualTo(0.5f));
        Assert.That(CrewBars.Fill(50, 10), Is.EqualTo(1f));
        Assert.That(CrewBars.RequiredVsBerths(28, 1), Is.EqualTo("28 crew, 1 berth"));
        Assert.That(CrewBars.BerthShortfall(5, 4), Is.EqualTo("1 berth short"));
        Assert.That(CrewBars.WorkRate(0.5), Is.EqualTo("25%"));
        Assert.That(CrewBars.WorkRate(1), Is.EqualTo("100%"));
    }

    [Test]
    public void CourseRow_ShowsSeatsLengthAndNextGraduation()
    {
        var academy = new NavalAcademyClassView(7, 8, 24, 0, 50, Array.Empty<AcademyCohortView>())
        {
            CrewSeats = 200,
            CrewSeatsInUse = 50,
            CrewCourseLengthMonths = 3,
            NextCrewGraduation = new DateTime(2045, 3, 1),
            NextCrewGraduationCount = 50,
        };
        var row = CrewBars.CourseRow(academy);
        Assert.That(row.Value, Is.EqualTo("50 / 200"));
        Assert.That(row.Percent, Is.EqualTo("25%"));
        Assert.That(row.Trailing, Is.EqualTo("3 mo, next 2045-03-01 (50)"));

        var idle = CrewBars.CourseRow(new NavalAcademyClassView(7, 8, 24, 0, 50, Array.Empty<AcademyCohortView>()));
        Assert.That(idle.Percent, Is.EqualTo("-"));
        Assert.That(idle.Fill, Is.EqualTo(0f));
        Assert.That(idle.Trailing, Does.EndWith("none running"));
    }

    [Test]
    public void PoolRows_AreLargestFirst_WithMatchingSegments()
    {
        var pool = new[] { new CrewPoolRow("Martian", 100), new CrewPoolRow("Human", 300) };
        var rows = CrewBars.PoolRows(pool);
        var segments = CrewBars.PoolSegments(pool);
        Assert.That(rows.Select(r => r.Name), Is.EqualTo(new[] { "Human", "Martian" }));
        Assert.That(rows.Select(r => r.Percent), Is.EqualTo(new[] { "75%", "25%" }));
        Assert.That(segments.Select(s => s.Label), Is.EqualTo(new[] { "Human", "Martian" }));
        Assert.That(segments.Select(s => s.Colour), Is.EqualTo(rows.Select(r => r.Chip)));
    }

    [Test]
    public void SummaryIndicator_ShowsNeededAndBerths_UntilTracked_ThenAboard()
    {
        Assert.That(CrewBars.Indicator(Ship(17, 12)), Is.EqualTo("17 needed, 12 berths"));
        Assert.That(CrewBars.Indicator(Ship(17, 1)), Is.EqualTo("17 needed, 1 berth"));
        Assert.That(CrewBars.Indicator(Ship(17, 20, tracked: true, aboard: 9, under: true)), Is.EqualTo("9/17 aboard"));
    }

    [Test]
    public void CrewTabPartRows_ShareTheTotals()
    {
        var ship = Ship(28, 10) with
        {
            CrewByComponent = new[] { new ShipCrewRow("a", "Laser", 2, 5), new ShipCrewRow("b", "Bridge", 1, 18) },
            BerthsByComponent = new[] { new ShipBerthRow("q", "Crew Quarters", 2, 5) },
        };
        var crew = CrewBars.CrewByComponentRows(ship);
        Assert.That(crew.Select(r => r.Value), Is.EqualTo(new[] { "10", "18" }));
        Assert.That(crew.Select(r => r.Percent), Is.EqualTo(new[] { "36%", "64%" }));
        Assert.That(crew[0].Tooltip, Is.EqualTo("Laser: 5 crew x 2 = 10."));
        var berths = CrewBars.BerthsByComponentRows(ship).Single();
        Assert.That(berths.Value, Is.EqualTo("10"));
        Assert.That(berths.Percent, Is.EqualTo("100%"));
        Assert.That(berths.Fill, Is.EqualTo(1f));
    }
}
