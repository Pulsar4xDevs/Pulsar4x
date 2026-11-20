using System;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Client logic for the Recycle section and queued recycle jobs. No ImGui and no game.</summary>
[TestFixture]
public class RecycleUiTests
{
    static RecycleCandidateView Ship(string name = "Endeavour", int id = 9, string? refusal = "", string? lineId = "yard")
        => new(RecycleUi.ShipKind, id, "hull-a", name, 1, lineId)
        {
            Returns = new[] { new ResourceRequirement("Steel", 10), new ResourceRequirement("Titanium", 2) },
            PointCost = 40,
            Refusal = refusal ?? "",
        };

    static RecycleCandidateView Item(string name = "Sensor", string designId = "sensor", long available = 5, string? refusal = "", string? lineId = "factory")
        => new(RecycleUi.ComponentKind, -1, designId, name, available, lineId)
        {
            Returns = new[] { new ResourceRequirement("Electronics", 3) },
            PointCost = 8,
            Refusal = refusal ?? "",
        };

    static IndustryJobView Job(string name, RecycleJobInfo? recycle, string status = "Processing", bool missing = false)
        => new("job", name, 0, 1, false, status, missing, 0, 10) { Recycle = recycle };

    [Test]
    public void ReturnFraction_MatchesTheEngineSetting()
    {
        Assert.That(RecycleUi.ReturnFraction, Is.EqualTo(0.5));
        Assert.That(RecycleUi.ReturnFractionText(), Is.EqualTo("50%"));
    }

    [Test]
    public void CanQueue_IsFalseWhenThereIsARefusal()
    {
        Assert.That(RecycleUi.CanQueue(Ship()), Is.True);
        Assert.That(RecycleUi.CanQueue(Ship(refusal: "That ship is in refit.")), Is.False);
        Assert.That(RecycleUi.CanQueue(Item(refusal: "That line cannot build it, so it cannot take it apart.")), Is.False);
    }

    [Test]
    public void Command_QueuesAShipOrAnItem_AndIsNullWhenRefused()
    {
        var ship = RecycleUi.Command(4, Ship(), 99);
        Assert.That(ship, Is.TypeOf<RecycleShipCommand>());
        var shipCmd = (RecycleShipCommand)ship!;
        Assert.That((shipCmd.TargetEntityId, shipCmd.ShipEntityId, shipCmd.ProductionLineId), Is.EqualTo((4, 9, "yard")));

        var item = RecycleUi.Command(4, Item(available: 5), 3);
        Assert.That(item, Is.TypeOf<RecycleComponentCommand>());
        var itemCmd = (RecycleComponentCommand)item!;
        Assert.That((itemCmd.TargetEntityId, itemCmd.DesignId, itemCmd.Quantity, itemCmd.ProductionLineId), Is.EqualTo((4, "sensor", 3, "factory")));

        Assert.That(RecycleUi.Command(4, Ship(refusal: "That ship is already queued for recycling."), 1), Is.Null);
        Assert.That(RecycleUi.ClampQuantity(0, 5), Is.EqualTo(1));
        Assert.That(RecycleUi.ClampQuantity(9, 5), Is.EqualTo(5));
        var clamped = (RecycleComponentCommand)RecycleUi.Command(4, Item(available: 2), 99)!;
        Assert.That(clamped.Quantity, Is.EqualTo(2));
    }

    [Test]
    public void ReturnsAndPointCost_FormatExpectedYield()
    {
        var ship = Ship();
        Assert.That(RecycleUi.ReturnsText(ship.Returns), Is.EqualTo("10 Steel, 2 Titanium"));
        Assert.That(RecycleUi.PointCostText(ship.PointCost), Is.EqualTo("40 points"));
        Assert.That(RecycleUi.ReturnsText(RecycleUi.ReturnsForQuantity(Item().Returns, 3)), Is.EqualTo("9 Electronics"));
        Assert.That(RecycleUi.PointCostText(8, 3), Is.EqualTo("24 points (8 points each)"));
        Assert.That(RecycleUi.ReturnsText(Array.Empty<ResourceRequirement>()), Is.EqualTo("No returns"));
    }

    [Test]
    public void JobName_LabelsAQueuedJobAsRecycling()
    {
        var scrap = Job("Scrap Endeavour", new RecycleJobInfo(RecycleUi.ShipKind, 9, "hull-a"));
        Assert.That(RecycleUi.Name(scrap), Is.EqualTo("Recycling Endeavour"));
        var recycle = Job("Recycle Sensor", new RecycleJobInfo(RecycleUi.ComponentKind, -1, "sensor"));
        Assert.That(RecycleUi.Name(recycle), Is.EqualTo("Recycling Sensor"));
        var already = Job("Recycling Sensor", new RecycleJobInfo(RecycleUi.ComponentKind, -1, "sensor"));
        Assert.That(RecycleUi.Name(already), Is.EqualTo("Recycling Sensor"));
        var ordinary = new IndustryJobView("job", "Plastic", 0, 4, false, "Processing", false, 0, 10);
        Assert.That(RecycleUi.Name(ordinary), Is.EqualTo("Plastic"));
        var refit = new IndustryJobView("job", "Hull", 0, 1, false, "Processing", false, 0, 10)
        {
            Refit = new RefitJobInfo(1, "Endeavour", "Old", "New"),
        };
        Assert.That(RecycleUi.Name(refit), Is.EqualTo("Refit Endeavour to New"));
    }

    [Test]
    public void TooltipAndShipLock_UseReturnsAndWaitReason()
    {
        var recycle = new RecycleJobInfo(RecycleUi.ShipKind, 9, "hull-a")
        {
            Returns = new[] { new ResourceRequirement("Steel", 10) },
            WaitReason = "That ship is not in transfer range.",
        };
        var job = Job("Scrap Endeavour", recycle, status: "MissingResources", missing: true);
        Assert.That(RecycleUi.Tooltip(job), Is.EqualTo("Returns: 10 Steel\nThat ship is not in transfer range."));

        var line = new ProductionLineView("yard", "Earth Shipyard", 12)
        {
            Jobs = new[] { job },
            IsShipyard = true,
        };
        var industry = new IndustryView(new[] { line });
        var locked = RecycleUi.FindShipLock(9, industry);
        Assert.That(locked, Is.Not.Null);
        Assert.That(locked!.Value.YardName, Is.EqualTo("Earth Shipyard"));
        Assert.That(locked.Value.JobName, Is.EqualTo("Recycling Endeavour"));
        Assert.That(locked.Value.WaitReason, Is.EqualTo("That ship is not in transfer range."));
        Assert.That(RecycleUi.FindShipLock(8, industry), Is.Null);
        Assert.That(RecycleUi.FindShipLock(9, (IndustryView?)null), Is.Null);
    }

    [Test]
    public void RefusalMessage_ShowsTheEngineReason_WhenTheCommandIsRejected()
    {
        Assert.That(RecycleUi.RefusalMessage(null), Is.Null);
        Assert.That(RecycleUi.RefusalMessage(CommandResult.Ok("ok")), Is.Null);
        Assert.That(RecycleUi.RefusalMessage(CommandResult.Reject("That ship is in refit.")), Is.EqualTo("That ship is in refit."));
        Assert.That(RecycleUi.RefusalMessage(new CommandResult(false)), Is.EqualTo("The command was refused."));
    }
}
