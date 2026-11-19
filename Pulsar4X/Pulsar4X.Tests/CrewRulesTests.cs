using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Tests;

/// <summary>Pure client helpers for the crew-rules UI: designer totals, warn-only warnings, order gating, tab text.</summary>
[TestFixture]
public class CrewRulesTests
{
    static CrewRules.DesignPart Part(int crew, int bunks, int slots, bool bridge, int count = 1)
        => new(crew, bunks, slots, bridge, count);

    [Test]
    public void Totals_CrewIsZeroWithoutABridge_BunksAndSlotsAlwaysSum()
    {
        var sat = CrewRules.Totals(new[] { Part(0, 0, 0, false), Part(2, 0, 0, false) });
        Assert.That(sat.Crew, Is.EqualTo(0));
        Assert.That(sat.Bunks, Is.EqualTo(0));
        Assert.That(sat.OfficerSlots, Is.EqualTo(0));
        Assert.That(sat.HasBridge, Is.False);
        Assert.That(sat.Automated, Is.True);
        Assert.That(sat.PeopleCapacity, Is.EqualTo(0));
        Assert.That(CrewRules.DesignWarnings(sat), Is.Empty);
    }

    [Test]
    public void Totals_CrewSumsOnlyWhenAnyPartIsABridge()
    {
        var parts = new[]
        {
            Part(18, 0, 4, true),
            Part(5, 0, 0, false, 2),
            Part(0, 10, 0, false, 3),
        };
        var totals = CrewRules.Totals(parts);
        Assert.That(totals.Crew, Is.EqualTo(28));
        Assert.That(totals.Bunks, Is.EqualTo(30));
        Assert.That(totals.OfficerSlots, Is.EqualTo(4));
        Assert.That(totals.HasBridge, Is.True);
        Assert.That(totals.Automated, Is.False);
        Assert.That(totals.PeopleCapacity, Is.EqualTo(29), "min(crew, bunks) plus the captain's slot");
        Assert.That(CrewRules.DesignWarnings(totals), Is.Empty);
    }

    [Test]
    public void DesignWarnings_BunksShortAndBridgeWithNoSlot_NeverForAutomated()
    {
        var shortBunks = CrewRules.DesignWarnings(new[] { Part(18, 0, 4, true), Part(0, 10, 0, false) });
        Assert.That(shortBunks, Is.EqualTo(new[]
        {
            "Crew 18 but only 10 crew bunks: add crew quarters for 8 more.",
        }));

        var noSlot = CrewRules.DesignWarnings(new[] { Part(18, 20, 0, true) });
        Assert.That(noSlot, Is.EqualTo(new[] { "The bridge has no officer slot for a captain." }));

        var both = CrewRules.DesignWarnings(new[] { Part(18, 0, 0, true) });
        Assert.That(both, Is.EqualTo(new[]
        {
            "Crew 18 but only 0 crew bunks: add crew quarters for 18 more.",
            "The bridge has no officer slot for a captain.",
        }));

        Assert.That(CrewRules.DesignWarnings(new[] { Part(5, 0, 0, false) }), Is.Empty,
            "A bridgeless design is automated and has no crew warnings.");
    }

    [Test]
    public void OrderAndAssignRefusals_MatchTheEngineWording()
    {
        var crewed = new ShipSnapshot(1, "Test", "sys") { OfficerSlots = 4 };
        var automated = new ShipSnapshot(1, "Test", "sys") { Automated = true };
        var noSlot = new ShipSnapshot(1, "Test", "sys") { OfficerSlots = 0 };

        Assert.That(CrewRules.OrderRefusal(crewed), Is.Null);
        Assert.That(CrewRules.OrderRefusal(automated), Is.EqualTo(CrewText.AutomatedNoOrders));
        Assert.That(CrewRules.SetFlagshipRefusal(crewed), Is.Null);
        Assert.That(CrewRules.SetFlagshipRefusal(automated), Is.EqualTo(CrewRules.SetFlagshipAutomated));
        Assert.That(CrewRules.AssignCaptainRefusal(crewed), Is.Null);
        Assert.That(CrewRules.AssignCaptainRefusal(automated), Is.EqualTo(CrewRules.AssignCaptainAutomated));
        Assert.That(CrewRules.AssignCaptainRefusal(noSlot), Is.EqualTo(CrewRules.AssignCaptainNoSlot));

        Assert.That(CrewRules.AutomatedShipNoOrders, Is.EqualTo(CrewText.AutomatedNoOrders));
        Assert.That(CrewText.AutomatedNoOrders, Does.Contain("automated").IgnoreCase.And.Contain("follow the flagship"));
        Assert.That(CrewRules.SetFlagshipAutomated, Does.Contain("cannot be a flagship"));
    }

    [Test]
    public void AutomatedShips_BlockOwnOrders_ButAllowTransferRefitFireControlAndCancel()
    {
        var automated = new ShipSnapshot(1, "SensorSat I", "sys") { Automated = true };
        var crewed = new ShipSnapshot(2, "Surveyor I", "sys") { OfficerSlots = 4 };

        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.GoalOrMove, automated), Is.EqualTo(CrewText.AutomatedNoOrders));
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.SetFlagship, automated), Is.EqualTo(CrewRules.SetFlagshipAutomated));
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.AssignCaptain, automated), Is.EqualTo(CrewRules.AssignCaptainAutomated));
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.TopUp, automated), Is.EqualTo(CrewRules.TopUpAutomated));

        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.CargoTransfer, automated), Is.Null);
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.Refit, automated), Is.Null);
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.FireControl, automated), Is.Null);
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.Cancel, automated), Is.Null);

        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.GoalOrMove, crewed), Is.Null);
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.CargoTransfer, crewed), Is.Null);
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.Refit, crewed), Is.Null);
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.FireControl, crewed), Is.Null);
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.Cancel, crewed), Is.Null);
        Assert.That(CrewRules.RefusalFor(CrewRules.ShipAction.GoalOrMove, null), Is.Null);
    }

    [Test]
    public void CrewTabText_FormatsCrewBunksOfficersAndAutomated()
    {
        Assert.That(CrewRules.CrewLine(9, 17, 20), Is.EqualTo("Crew 9/17 (20 bunks)"));
        Assert.That(CrewRules.CrewLine(1, 1, 1), Is.EqualTo("Crew 1/1 (1 bunk)"));
        Assert.That(CrewRules.OfficersLine(1, 4), Is.EqualTo("Officers 1/4"));
        Assert.That(CrewRules.PeopleAboardLine(18), Is.EqualTo("Life support: 18 people aboard"));
        Assert.That(CrewRules.PeopleAboardLine(1), Is.EqualTo("Life support: 1 person aboard"));
        Assert.That(CrewRules.AutomatedState, Is.EqualTo("Automated, no crew needed"));
        Assert.That(CrewRules.AutomatedLabel, Is.EqualTo("Automated"));
    }

    [Test]
    public void FleetCrewWarning_MatchesTheCrewTab_AndIgnoresAStaleUnderCrewedSnapshot()
    {
        // The bug: the fleet tree cached UnderCrewed=true, while the Crew tab's live view was full and green.
        var liveFull = new ShipView("Courier", 17)
        {
            CrewTracked = true,
            CrewAboard = 17,
            CrewBerths = 20,
            UnderCrewed = false,
            OfficersAboard = 1,
            OfficerSlots = 4,
        };
        var staleShort = new ShipSnapshot(1, "Courier", "sys")
        {
            CrewTracked = true,
            CrewAboard = 9,
            CrewRequired = 17,
            UnderCrewed = true,
        };
        Assert.That(CrewRules.IsUnderCrewed(liveFull), Is.False, "Crew tab: full crew is not under-crewed");
        Assert.That(CrewRules.IsUnderCrewed(staleShort), Is.True, "the stale tree would still warn");
        Assert.That(CrewRules.FleetCrewWarning(liveFull, staleShort), Is.False,
            "the fleet list must follow the live Crew tab data, not the stale snapshot");
        Assert.That(CrewRules.FleetCrewWarning(liveFull, staleShort), Is.EqualTo(CrewRules.IsUnderCrewed(liveFull)));

        // Understaffed: both the list and the tab warn, even if a stale snapshot says the ship is full.
        var liveShort = liveFull with { CrewAboard = 9, UnderCrewed = true };
        var staleFull = new ShipSnapshot(1, "Courier", "sys")
        {
            CrewTracked = true,
            CrewAboard = 17,
            CrewRequired = 17,
            UnderCrewed = false,
        };
        Assert.That(CrewRules.IsUnderCrewed(liveShort), Is.True);
        Assert.That(CrewRules.FleetCrewWarning(liveShort, staleFull), Is.True);
        Assert.That(CrewRules.FleetCrewWarning(liveShort, staleFull), Is.EqualTo(CrewRules.IsUnderCrewed(liveShort)));

        // Automated: never a crew warning, even with leftover counts and a stale under-crewed flag.
        var liveAuto = new ShipView("SensorSat I", 0)
        {
            Automated = true,
            CrewTracked = true,
            CrewAboard = 0,
            UnderCrewed = false,
        };
        Assert.That(CrewRules.IsUnderCrewed(liveAuto), Is.False);
        Assert.That(CrewRules.FleetCrewWarning(liveAuto, staleShort), Is.False);
        Assert.That(CrewRules.FleetCrewWarning(liveAuto, staleShort), Is.EqualTo(CrewRules.IsUnderCrewed(liveAuto)));
        Assert.That(CrewRules.IsUnderCrewed(tracked: true, automated: true, crewAboard: 0, crewRequired: 10), Is.False,
            "automated ships never warn, even if leftover required is non-zero");

        // A captain does not count as crew: full crew with no captain is not under-crewed.
        var fullNoCaptain = liveFull with { OfficersAboard = 0, NoCaptain = true };
        Assert.That(CrewRules.IsUnderCrewed(fullNoCaptain), Is.False);

        // Untracked aboard numbers are ignored.
        Assert.That(CrewRules.IsUnderCrewed(tracked: false, automated: false, crewAboard: 0, crewRequired: 17), Is.False);

        // With no live view, recompute from the snapshot's counts, not a leftover UnderCrewed flag.
        Assert.That(CrewRules.FleetCrewWarning(null, staleShort), Is.True);
        var snapshotFullButFlagged = new ShipSnapshot(1, "Courier", "sys")
        {
            CrewTracked = true,
            CrewAboard = 17,
            CrewRequired = 17,
            UnderCrewed = true,
        };
        Assert.That(CrewRules.IsUnderCrewed(snapshotFullButFlagged), Is.False,
            "recompute from counts; do not trust a stale UnderCrewed flag");
        Assert.That(CrewRules.FleetCrewWarning(null, snapshotFullButFlagged), Is.False);
        Assert.That(CrewRules.FleetCrewWarning(null, null), Is.False);
    }
}
