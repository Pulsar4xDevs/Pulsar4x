using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;

namespace Pulsar4X.Tests;

[TestFixture]
public class BootstrapPickerTests
{
    static CargoItemView Item(int id, string name, string designId, long units, bool crate = true)
        => new(id, name, "Component", "", units, 0, 0, 0, 0, 0, 0, false) { DesignId = designId, IsCrate = crate };

    static EntitySnapshot Holder(int id, OwnerRelation relation, BodyKind kind, params CargoItemView[] items)
        => new()
        {
            Id = id,
            Relation = relation,
            Kind = kind,
            Views = new IComponentView[]
            {
                new CargoStorageView(0, 0, 0) { Stores = new[] { new CargoTypeStoreView("general", "General", 0, 0) { Items = items } } },
            },
        };

    static EntitySnapshot Colony(int id, params CargoItemView[] items) => Holder(id, OwnerRelation.Owned, BodyKind.Colony, items);

    static ShipSnapshot Ship(int? captain = 7, bool bay = true) => new(1, "Ship", "sys", "Design", null) { CommanderId = captain, HasConstructionBay = bay };

    [Test]
    public void ColonyChoices_AreOwnedWarehouses_AndTheDestinationLeavesOutTheSource()
    {
        var owned = Colony(10);
        var foreign = Holder(11, OwnerRelation.Friendly, BodyKind.Colony);
        var planet = Holder(12, OwnerRelation.Owned, BodyKind.Planet);
        var noWarehouse = new EntitySnapshot { Id = 13, Relation = OwnerRelation.Owned, Kind = BodyKind.Colony };

        Assert.That(new[] { owned, foreign, planet, noWarehouse }.Where(BootstrapPicker.IsPickable), Is.EqualTo(new[] { owned }));
        Assert.That(BootstrapPicker.IsPickableDestination(owned, 10), Is.False);
        Assert.That(BootstrapPicker.IsPickableDestination(owned, -1), Is.True);
        Assert.That(BootstrapPicker.IsPickableDestination(owned, 99), Is.True);
    }

    [Test]
    public void CrateOptions_ListOnlyCratesWithStock_AndSumTheSourceAndTheHold()
    {
        var source = Colony(10,
            Item(1, "Mine", "mine", 2),
            Item(2, "Mine", "mine", 1),
            Item(3, "Refinery", "refinery", 1),
            Item(4, "Ore", "ore", 500, crate: false),
            Item(5, "Spent", "spent", 0));
        var hold = Holder(1, OwnerRelation.Owned, BodyKind.Ship, Item(6, "Mine", "mine", 2), Item(7, "Lab", "lab", 1));

        var options = BootstrapPicker.CrateOptions(source, hold);

        Assert.That(options.Select(o => (o.DesignId, o.Available)),
            Is.EqualTo(new[] { ("lab", 1L), ("mine", 5L), ("refinery", 1L) }));
        Assert.That(BootstrapPicker.CrateOptions(null, null), Is.Empty);
        Assert.That(BootstrapPicker.CrateOptions(source, null).Single(o => o.DesignId == "mine").Available, Is.EqualTo(3));
    }

    [Test]
    public void SyncCounts_HoldsCountsToTheStock_AndDropsDesignsThatLeft()
    {
        var options = new[] { new BootstrapCrateOption("mine", "Mine", 3), new BootstrapCrateOption("lab", "Lab", 1) };
        var counts = new Dictionary<string, int> { ["mine"] = 9, ["gone"] = 2, ["lab"] = -4 };

        BootstrapPicker.SyncCounts(counts, options);

        Assert.That(counts, Is.EquivalentTo(new Dictionary<string, int> { ["mine"] = 3, ["lab"] = 0 }));

        counts.Clear();
        BootstrapPicker.SyncCounts(counts, options);
        Assert.That(counts.Values, Is.All.EqualTo(0));
    }

    [Test]
    public void Requests_SkipZeroCounts_AndNeverAskForMoreThanTheStock()
    {
        var options = new[]
        {
            new BootstrapCrateOption("lab", "Lab", 1),
            new BootstrapCrateOption("mine", "Mine", 3),
            new BootstrapCrateOption("refinery", "Refinery", 2),
        };
        var counts = new Dictionary<string, int> { ["lab"] = 0, ["mine"] = 5, ["refinery"] = 2 };

        var lines = BootstrapPicker.Requests(options, counts);

        Assert.That(lines.Select(l => (l.DesignId, l.Count)), Is.EqualTo(new[] { ("mine", 3), ("refinery", 2) }));
        Assert.That(BootstrapPicker.Requests(options, new Dictionary<string, int>()), Is.Empty);
    }

    [Test]
    public void Validate_FollowsTheTranslatorsOrder()
    {
        var one = new List<BootstrapCrateRequest> { new("mine", 1) };

        Assert.That(BootstrapPicker.Validate(Ship(captain: null), 10, 20, one, true), Is.EqualTo(BootstrapPicker.NoCaptain));
        Assert.That(BootstrapPicker.Validate(Ship(bay: false), 10, 20, one, true), Is.EqualTo(BootstrapPicker.NoBay));
        Assert.That(BootstrapPicker.Validate(Ship(), -1, 20, one, true), Is.EqualTo("Pick a source colony."));
        Assert.That(BootstrapPicker.Validate(Ship(), 10, -1, one, true), Is.EqualTo("Pick a destination colony."));
        Assert.That(BootstrapPicker.Validate(Ship(), 10, 10, one, true), Is.EqualTo("Pick two different colonies."));
        Assert.That(BootstrapPicker.Validate(Ship(), 10, 20, new List<BootstrapCrateRequest>(), true), Is.EqualTo("Nothing to build."));
        Assert.That(BootstrapPicker.Validate(Ship(), 10, 20, new List<BootstrapCrateRequest> { new("mine", 0) }, true),
            Is.EqualTo("Crate counts must be positive."));
        Assert.That(BootstrapPicker.Validate(Ship(), 10, 20, one, false), Is.EqualTo(BootstrapPicker.OutOfReach));
        Assert.That(BootstrapPicker.Validate(Ship(), 10, 20, one, true), Is.Null);
    }

    static ShipSnapshot Reach(string span, string wellFix = "", string systemFix = "")
        => new(1, "Ship", "sys", "Design", null) { CommandSpan = span, SpanLine = "line", ReachFixWell = wellFix, ReachFixSystem = systemFix };

    [Test]
    public void Blocked_PairBeyondReach_NamesThePartToChange_ThenTheBand()
    {
        var body = Reach(CommandSpanLabels.Body, wellFix: "The bridge limits reach. Fit a Planetary Command Bridge.");

        Assert.That(BootstrapPicker.Blocked(body, BootstrapPicker.ReachBand.Well, BootstrapPicker.ReachBand.Ship),
            Is.EqualTo("The bridge limits reach. Fit a Planetary Command Bridge. Then pick under This well."));
        Assert.That(BootstrapPicker.Blocked(body, null, BootstrapPicker.ReachBand.Well),
            Is.EqualTo("The bridge limits reach. Fit a Planetary Command Bridge."));

        var rank = Reach(CommandSpanLabels.Body, systemFix: "The captain's rank limits reach. Seat an officer of rank 6+.");
        Assert.That(BootstrapPicker.Blocked(rank, BootstrapPicker.ReachBand.System, BootstrapPicker.ReachBand.Well),
            Does.StartWith("The captain's rank limits reach."));
        Assert.That(BootstrapPicker.Blocked(Reach(CommandSpanLabels.Body), BootstrapPicker.ReachBand.Well, BootstrapPicker.ReachBand.Ship),
            Is.EqualTo("line"));
    }

    [Test]
    public void Blocked_PairInReach_SaysWhichBandToPickUnder()
    {
        var well = Reach(CommandSpanLabels.Well);

        Assert.That(BootstrapPicker.Blocked(well, BootstrapPicker.ReachBand.Well, BootstrapPicker.ReachBand.Ship),
            Does.Contain("Pick them under This well."));
        Assert.That(BootstrapPicker.Blocked(well, BootstrapPicker.ReachBand.Ship, BootstrapPicker.ReachBand.Well),
            Does.Contain("Pick them under This ship."));
        Assert.That(BootstrapPicker.Blocked(well, null, BootstrapPicker.ReachBand.Well),
            Does.StartWith("No other colony of yours"));
        Assert.That(BootstrapPicker.Blocked(well, BootstrapPicker.ReachBand.Well, BootstrapPicker.ReachBand.Well), Is.Null);
    }

    [Test]
    public void ReachOf_ReadsTheEffectiveSpanLabel()
    {
        Assert.That(BootstrapPicker.ReachOf(Reach(CommandSpanLabels.Body)), Is.EqualTo(BootstrapPicker.ReachBand.Ship));
        Assert.That(BootstrapPicker.ReachOf(Reach(CommandSpanLabels.Well)), Is.EqualTo(BootstrapPicker.ReachBand.Well));
        Assert.That(BootstrapPicker.ReachOf(Reach(CommandSpanLabels.System)), Is.EqualTo(BootstrapPicker.ReachBand.System));
        Assert.That(BootstrapPicker.ReachOf(Reach(CommandSpanLabels.Neighbor)), Is.EqualTo(BootstrapPicker.ReachBand.System));
    }
}
