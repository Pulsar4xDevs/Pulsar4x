using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Pure client helpers for a shipyard refit shown as a normal industry-job row.</summary>
[TestFixture]
public class RefitJobDisplayTests
{
    static IndustryJobView Job(
        string name = "queued name",
        int completed = 0,
        int ordered = 1,
        string status = "Processing",
        bool missing = false,
        double percent = 25,
        long pointsLeft = 75,
        RefitJobInfo? refit = null)
        => new( "job-1", name, completed, ordered, false, status, missing, percent, pointsLeft) { Refit = refit };

    static RefitJobInfo Info(
        string ship = "SensorSat I",
        string from = "Sensor Sat",
        string to = "Sensor Sat Mk2",
        string[]? removing = null,
        string[]? mounting = null,
        string[]? waitingFor = null,
        string wait = "",
        long perDay = 25)
        => new(42, ship, from, to)
        {
            Removing = removing ?? Array.Empty<string>(),
            Mounting = mounting ?? Array.Empty<string>(),
            WaitingFor = waitingFor ?? Array.Empty<string>(),
            WaitReason = wait,
            PointsPerDay = perDay,
        };

    [Test]
    public void Name_RefitToADesign_AndManualRefitWithEmptyToDesign()
    {
        Assert.That(RefitJobDisplay.Name(Info()), Is.EqualTo("Refit SensorSat I to Sensor Sat Mk2"));
        Assert.That(RefitJobDisplay.Name(Job(refit: Info())), Is.EqualTo("Refit SensorSat I to Sensor Sat Mk2"));

        var manual = Info(to: "");
        Assert.That(RefitJobDisplay.Name(manual), Is.EqualTo("Refit SensorSat I"));
        Assert.That(RefitJobDisplay.Name(Job(name: "Refit SensorSat I", refit: manual)), Is.EqualTo("Refit SensorSat I"));

        Assert.That(RefitJobDisplay.Name(Job(name: "Planet Express Ship")), Is.EqualTo("Planet Express Ship"),
            "A non-refit job keeps its own name.");
    }

    [Test]
    public void Batch_ShowsCompletedOverOrdered_AsZeroOfOneForARefit()
    {
        Assert.That(RefitJobDisplay.Batch(Job(refit: Info(), completed: 0, ordered: 1)), Is.EqualTo("0/1"));
        Assert.That(RefitJobDisplay.Batch(Job(name: "Hull", completed: 2, ordered: 5)), Is.EqualTo("2/5"));
    }

    [Test]
    public void Eta_IsPointsLeftOverPointsPerDay_AndGuardsDivisionByZero()
    {
        Assert.That(RefitJobDisplay.Eta(10, 5), Is.EqualTo("About 2 days"));
        Assert.That(RefitJobDisplay.Eta(5, 5), Is.EqualTo("About 1 day"));
        Assert.That(RefitJobDisplay.Eta(1, 4), Is.EqualTo("About 0.3 days"));
        Assert.That(RefitJobDisplay.Eta(0, 5), Is.EqualTo("About 0 days"));
        Assert.That(RefitJobDisplay.Eta(20, 0), Is.Null);
        Assert.That(RefitJobDisplay.Eta(20, -1), Is.Null);
        Assert.That(RefitJobDisplay.Eta(Job(pointsLeft: 50, refit: Info(perDay: 25))), Is.EqualTo("About 2 days"));
        Assert.That(RefitJobDisplay.Eta(Job(pointsLeft: 50, refit: Info(perDay: 0))), Is.Null);
        Assert.That(RefitJobDisplay.Eta(Job(name: "Hull", pointsLeft: 50)), Is.Null);
    }

    [Test]
    public void PartsText_ListsRemovingAndMounting_AndWaitingForWithoutDoublingMounting()
    {
        var parts = Info(removing: new[] { "Passive Sensor", "Battery z5kg" }, mounting: new[] { "Better Sensor" });
        Assert.That(RefitJobDisplay.PartsText(parts), Is.EqualTo(
            "Removing: Passive Sensor, Battery z5kg\nMounting: Better Sensor"));

        var empty = Info();
        Assert.That(RefitJobDisplay.PartsText(empty), Is.EqualTo("Removing: none\nMounting: none"));

        // Mounting already includes awaited names; do not append WaitingFor to Mounting.
        // When WaitReason already names those parts, drop the separate Waiting for line.
        var namedInReason = Info(
            removing: new[] { "Old Radio" },
            mounting: new[] { "Warehouse Radio", "Passenger Quarters" },
            waitingFor: new[] { "Passenger Quarters" },
            wait: "Waiting for Passenger Quarters");
        string named = RefitJobDisplay.PartsText(namedInReason);
        Assert.That(named, Is.EqualTo(
            "Removing: Old Radio\nMounting: Warehouse Radio, Passenger Quarters"));
        Assert.That(named, Does.Not.Contain("Waiting for:"));
        Assert.That(Occurrences(named, "Passenger Quarters"), Is.EqualTo(1),
            "The awaited name appears in Mounting once, not again on a Waiting for line.");

        // When WaitReason is empty, WaitingFor still has its own line.
        var noReason = Info(
            removing: new[] { "Old Radio" },
            mounting: new[] { "Warehouse Radio", "Passenger Quarters" },
            waitingFor: new[] { "Passenger Quarters" },
            wait: "");
        string listed = RefitJobDisplay.PartsText(noReason);
        Assert.That(listed, Is.EqualTo(
            "Removing: Old Radio\nMounting: Warehouse Radio, Passenger Quarters\nWaiting for: Passenger Quarters"));
        Assert.That(Occurrences(listed, "Passenger Quarters"), Is.EqualTo(2),
            "With no WaitReason, the part is in Mounting once and in Waiting for once.");
    }

    [Test]
    public void Tooltip_IncludesWaitReasonEtaAndParts()
    {
        var stalled = Job(pointsLeft: 40, status: "MissingResources", missing: true,
            refit: Info(
                removing: new[] { "Old Radio" },
                mounting: new[] { "Warehouse Radio", "Passenger Quarters" },
                waitingFor: new[] { "Passenger Quarters" },
                wait: "Waiting for Passenger Quarters",
                perDay: 20));
        string tip = RefitJobDisplay.Tooltip(stalled);
        Assert.That(tip, Does.StartWith("Waiting for Passenger Quarters"));
        Assert.That(tip, Does.Contain("Removing: Old Radio"));
        Assert.That(tip, Does.Contain("Mounting: Warehouse Radio, Passenger Quarters"));
        Assert.That(tip, Does.Not.Contain("Waiting for:"),
            "WaitReason already names the awaited part, so PartsText must not list it again.");
        Assert.That(Occurrences(tip, "Passenger Quarters"), Is.EqualTo(2),
            "The waiting part appears once in WaitReason and once in Mounting, not a third time.");
        Assert.That(tip, Does.Contain("Industry points left: 40"));
        Assert.That(tip, Does.Contain("ETA About 2 days"));

        // Occupied-hold and no-storage reasons stay intact; if they already name the part,
        // there is still no separate Waiting for line.
        var occupied = Job(pointsLeft: 0, status: "MissingResources", missing: true,
            refit: Info(
                removing: new[] { "Passenger Quarters" },
                mounting: Array.Empty<string>(),
                waitingFor: new[] { "Passenger Quarters" },
                wait: "Waiting for passengers to disembark from Passenger Quarters; Waiting for cargo to be unloaded from Passenger Quarters",
                perDay: 20));
        string occupiedTip = RefitJobDisplay.Tooltip(occupied);
        Assert.That(occupiedTip, Does.StartWith(
            "Waiting for passengers to disembark from Passenger Quarters\n" +
            "Waiting for cargo to be unloaded from Passenger Quarters"));
        Assert.That(occupiedTip, Does.Contain("Mounting: none"));
        Assert.That(occupiedTip, Does.Not.Contain("Waiting for:"));
        Assert.That(occupiedTip, Does.Not.Contain("; "));

        var noStorage = Job(pointsLeft: 0, status: "MissingResources", missing: true,
            refit: Info(
                removing: new[] { "Battery" },
                mounting: Array.Empty<string>(),
                waitingFor: new[] { "Battery" },
                wait: "This yard has no storage for Battery.",
                perDay: 20));
        string storageTip = RefitJobDisplay.Tooltip(noStorage);
        Assert.That(storageTip, Does.StartWith("This yard has no storage for Battery."));
        Assert.That(storageTip, Does.Not.Contain("Waiting for:"));
        Assert.That(Occurrences(storageTip, "Battery"), Is.EqualTo(2),
            "The held part is named in WaitReason once and in Removing once, not again on a Waiting for line.");
    }

    [Test]
    public void MountDesignIds_RepeatsEachDesignIdByCount_WarehouseInstancesStaySeparate()
    {
        var missing = new[]
        {
            new RefitMissingPartView("design-quarters", "Passenger Quarters", 3),
            new RefitMissingPartView("design-radio", "Radio", 1),
            new RefitMissingPartView("design-skip", "Skip", 0),
        };
        Assert.That(RefitJobDisplay.MountDesignIds(missing), Is.EqualTo(new[]
        {
            "design-quarters", "design-quarters", "design-quarters",
            "design-radio",
        }));
        Assert.That(RefitJobDisplay.MountDesignIds(null), Is.Empty);
        Assert.That(RefitJobDisplay.MountDesignIds(Array.Empty<RefitMissingPartView>()), Is.Empty);

        var warehouseInstanceIds = new[] { 101, 102 };
        var designIds = RefitJobDisplay.MountDesignIds(missing);
        Assert.That(designIds, Is.Not.EquivalentTo(warehouseInstanceIds.Select(id => id.ToString())),
            "Warehouse instance ids stay in MountInstanceIds; they are not mixed into MountDesignIds.");
        Assert.That(RefitJobDisplay.HasWork(Array.Empty<int>(), warehouseInstanceIds, null), Is.True);
        Assert.That(RefitJobDisplay.HasWork(Array.Empty<int>(), Array.Empty<int>(), missing), Is.True,
            "Missing parts alone are enough to queue: the job waits, it is not blocked.");
        Assert.That(RefitJobDisplay.HasWork(new[] { 7 }, Array.Empty<int>(), null), Is.True);
        Assert.That(RefitJobDisplay.HasWork(Array.Empty<int>(), Array.Empty<int>(), null), Is.False);
    }


    [Test]
    public void WaitReasonText_SplitsJoinedReasonsOntoSeparateLines()
    {
        Assert.That(RefitJobDisplay.WaitReasonText(null), Is.Empty);
        Assert.That(RefitJobDisplay.WaitReasonText(""), Is.Empty);
        Assert.That(RefitJobDisplay.WaitReasonText("This yard has no storage for Battery."),
            Is.EqualTo("This yard has no storage for Battery."));

        string joined =
            "Waiting for passengers to disembark from Passenger Quarters; " +
            "Waiting for cargo to be unloaded from Cargo Hold; " +
            "Waiting for fuel to be unloaded from Fuel Tank";
        string text = RefitJobDisplay.WaitReasonText(joined);
        Assert.That(text, Is.EqualTo(
            "Waiting for passengers to disembark from Passenger Quarters\n" +
            "Waiting for cargo to be unloaded from Cargo Hold\n" +
            "Waiting for fuel to be unloaded from Fuel Tank"));
        Assert.That(text, Does.Not.Contain("; "));
    }

    [Test]
    public void PartsText_EmptyMounting_RendersAsNone_AfterPointsArePaid()
    {
        // After points are paid the engine may hold a part being vacated; Mounting is empty.
        var held = Info(
            removing: new[] { "Passenger Quarters" },
            mounting: Array.Empty<string>(),
            waitingFor: Array.Empty<string>(),
            wait: "Waiting for passengers to disembark from Passenger Quarters; Waiting for cargo to be unloaded from Passenger Quarters");
        Assert.That(RefitJobDisplay.PartsText(held), Is.EqualTo("Removing: Passenger Quarters\nMounting: none"));

        var job = Job(pointsLeft: 0, status: "MissingResources", missing: true, refit: held);
        string tip = RefitJobDisplay.Tooltip(job);
        Assert.That(tip, Does.StartWith(
            "Waiting for passengers to disembark from Passenger Quarters\n" +
            "Waiting for cargo to be unloaded from Passenger Quarters"));
        Assert.That(tip, Does.Contain("Removing: Passenger Quarters"));
        Assert.That(tip, Does.Contain("Mounting: none"));
        Assert.That(tip, Does.Not.Contain("; "));
    }

    [Test]
    public void ProgressPerDay_UsesRefitPointsWhenTheHeadJobIsARefit()
    {
        var line = new ProductionLineView("line", "Shipyard I", 100)
        {
            Jobs = new[]
            {
                Job(pointsLeft: 40, refit: Info(perDay: 5)),
                Job(name: "Planet Express Ship"),
            },
        };
        Assert.That(RefitJobDisplay.ProgressPerDay(line), Is.EqualTo(5));

        var building = new ProductionLineView("line", "Shipyard I", 100)
        {
            Jobs = new[] { Job(name: "Planet Express Ship") },
        };
        Assert.That(RefitJobDisplay.ProgressPerDay(building), Is.EqualTo(100));

        var idle = new ProductionLineView("line", "Shipyard I", 0);
        Assert.That(RefitJobDisplay.ProgressPerDay(idle), Is.EqualTo(0));
    }

    static int Occurrences(string text, string part)
    {
        int count = 0;
        int i = 0;
        while ((i = text.IndexOf(part, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += part.Length;
        }
        return count;
    }
}
