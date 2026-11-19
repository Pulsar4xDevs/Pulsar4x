using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Pin set and Queue visibility for the Missing Inputs panel. No ImGui and no game.</summary>
[TestFixture]
public class MissingInputsPinTests
{
    const int Colony = 10;
    const int OtherColony = 11;

    static MissingInputView Input(
        string itemId,
        string name,
        long required,
        long inStock,
        long shortfall,
        bool canBuild = false,
        string designId = "",
        string lineId = "",
        string lineName = "",
        int queueQuantity = 0,
        long queued = 0)
        => new(itemId, name, required, inStock, shortfall)
        {
            CanBuild = canBuild,
            DesignId = designId,
            LineId = lineId,
            LineName = lineName,
            LineKind = canBuild ? IndustryLineKinds.Factory : "",
            QueueQuantity = queueQuantity,
            Queued = queued,
        };

    static IndustryJobView Job(string jobId, string name, params MissingInputView[] missing)
        => new(jobId, name, 0, 1, false, missing.Length > 0 ? "MissingResources" : "Processing", missing.Length > 0, 0, 10)
        {
            MissingInputs = missing,
        };

    static ProductionLineView Line(string id, string name, params IndustryJobView[] jobs)
        => new(id, name, 0) { Jobs = jobs };

    static IndustryView Industry(int headId, string? headName, params ProductionLineView[] lines)
        => new(lines) { IndustryHeadId = headId, IndustryHeadName = headName };

    [Test]
    public void Toggle_AddsThenRemovesTheSameJob()
    {
        var pins = new MissingInputsPin();
        Assert.That(pins.Toggle(Colony, "job-a"), Is.True);
        Assert.That(pins.Contains(Colony, "job-a"), Is.True);
        Assert.That(pins.Toggle(Colony, "job-a"), Is.False);
        Assert.That(pins.Contains(Colony, "job-a"), Is.False);
        Assert.That(pins.Count, Is.EqualTo(0));
    }

    [Test]
    public void Add_IsIdempotent_AndIgnoresAnEmptyId()
    {
        var pins = new MissingInputsPin();
        pins.Add(Colony, "job-a");
        pins.Add(Colony, "job-a");
        pins.Add(Colony, "");
        pins.Add(Colony, null!);
        Assert.That(pins.Count, Is.EqualTo(1));
        Assert.That(pins.Toggle(Colony, ""), Is.False);
        Assert.That(pins.Count, Is.EqualTo(1));
    }

    [Test]
    public void Prune_DropsJobsThatFinishedOrWereCancelled_AndKeepsAJobThatMovedLines()
    {
        var pins = new MissingInputsPin();
        pins.Add(Colony, "keep");
        pins.Add(Colony, "moved");
        pins.Add(Colony, "done");
        pins.Add(OtherColony, "done");

        var after = Industry(-1, null,
            Line("factory", "Factory", Job("keep", "Hull")),
            Line("refinery", "Refinery", Job("moved", "Plastic")));

        Assert.That(pins.Prune(Colony, after), Is.EqualTo(1));
        Assert.That(pins.Contains(Colony, "keep"), Is.True);
        Assert.That(pins.Contains(Colony, "moved"), Is.True);
        Assert.That(pins.Contains(Colony, "done"), Is.False);
        Assert.That(pins.Contains(OtherColony, "done"), Is.True, "a pin on another colony is not pruned from this view");

        Assert.That(pins.Prune(Colony, null), Is.EqualTo(2), "with no industry, every pin on this colony is gone");
        Assert.That(pins.Contains(OtherColony, "done"), Is.True);
    }

    [Test]
    public void PinnedJobs_FollowLineThenQueueOrder()
    {
        var pins = new MissingInputsPin();
        pins.Add(Colony, "c");
        pins.Add(Colony, "a");
        pins.Add(Colony, "b");
        pins.Add(OtherColony, "other");

        var industry = Industry(-1, null,
            Line("factory", "Factory", Job("b", "B"), Job("a", "A")),
            Line("refinery", "Refinery", Job("c", "C")));

        Assert.That(pins.PinnedJobs(Colony, industry).Select(j => j.JobId), Is.EqualTo(new[] { "b", "a", "c" }));
        Assert.That(pins.PinnedJobs(OtherColony, industry), Is.Empty, "a pin for another colony is not listed against this industry");
        Assert.That(pins.PinnedJobs(Colony, null), Is.Empty);
    }

    [Test]
    public void ShowQueue_OnlyWhenTheSeatIsEmpty_AndTheInputCanBeBuilt()
    {
        var buildable = Input("p", "Plastic", 40, 0, 40, canBuild: true, designId: "d-p", lineId: "ref", lineName: "Refinery", queueQuantity: 40);
        var raw = Input("ore", "Iron Ore", 12, 2, 10);

        Assert.That(MissingInputsPin.ShowQueue(-1, buildable), Is.True);
        Assert.That(MissingInputsPin.ShowQueue(-1, true), Is.True);
        Assert.That(MissingInputsPin.ShowQueue(-1, raw), Is.False);
        Assert.That(MissingInputsPin.ShowQueue(-1, false), Is.False);
        Assert.That(MissingInputsPin.ShowQueue(7, buildable), Is.False, "a seated head hides Queue");
        Assert.That(MissingInputsPin.ShowQueue(0, buildable), Is.False, "only -1 is an empty seat");
        Assert.That(MissingInputsPin.ShowQueue(-1, (MissingInputView?)null), Is.False);
    }

    [Test]
    public void StockAndQueueText_AndTheCommand_UseTheViewFields()
    {
        var input = Input("p", "Plastic", 40000, 12, 39988, canBuild: true, designId: "d-p", lineId: "ref-1", lineName: "Refinery", queueQuantity: 39988);
        Assert.That(MissingInputsPin.StockText(input), Is.EqualTo("39,988 short / 40,000 required (12 in stock)"));
        Assert.That(MissingInputsPin.LineText(input), Is.EqualTo("Refinery"));
        Assert.That(MissingInputsPin.LineText(Input("ore", "Iron Ore", 1, 0, 1)), Is.EqualTo("—"));
        Assert.That(MissingInputsPin.QueueButtonText(input), Is.EqualTo("Queue 39,988 jobs on Refinery"));
        Assert.That(MissingInputsPin.HeadText(-1, null), Is.EqualTo("Industry head: Empty"));
        Assert.That(MissingInputsPin.HeadText(7, "Lee"), Is.EqualTo("Industry head: Lee"));

        var command = MissingInputsPin.QueueCommand(Colony, input);
        Assert.That(command.TargetEntityId, Is.EqualTo(Colony));
        Assert.That(command.ProductionLineId, Is.EqualTo("ref-1"));
        Assert.That(command.DesignId, Is.EqualTo("d-p"));
        Assert.That(command.Quantity, Is.EqualTo(39988));
        Assert.That(command.Repeat, Is.False);
        Assert.That(command.AutoInstall, Is.False);
    }

    [Test]
    public void QueuedFeeders_ShowQueuedN_AndHideQueueWhenTheShortfallIsCovered()
    {
        var partial = Input("p", "Plastic", 40, 0, 40, canBuild: true, designId: "d-p", lineId: "ref", lineName: "Refinery", queueQuantity: 15, queued: 25);
        Assert.That(MissingInputsPin.QueuedText(partial), Is.EqualTo("Queued 25"));
        Assert.That(MissingInputsPin.ShowQueue(-1, partial), Is.True);
        Assert.That(MissingInputsPin.QueueButtonText(partial), Is.EqualTo("Queue 15 jobs on Refinery"));
        Assert.That(MissingInputsPin.QueueCommand(Colony, partial).Quantity, Is.EqualTo(15));

        var covered = Input("p", "Plastic", 40, 0, 40, canBuild: true, designId: "d-p", lineId: "ref", lineName: "Refinery", queueQuantity: 0, queued: 40);
        Assert.That(MissingInputsPin.QueuedText(covered), Is.EqualTo("Queued 40"));
        Assert.That(MissingInputsPin.ShowQueue(-1, covered), Is.False, "Queue stays hidden when queued jobs already cover the shortfall, even if CanBuild is true");

        var none = Input("p", "Plastic", 40, 0, 40, canBuild: true, designId: "d-p", lineId: "ref", lineName: "Refinery", queueQuantity: 40);
        Assert.That(MissingInputsPin.QueuedText(none), Is.EqualTo(""));
        Assert.That(MissingInputsPin.ShowQueue(-1, none), Is.True);
        Assert.That(MissingInputsPin.StockText(none), Is.EqualTo("40 short / 40 required (0 in stock)"));
    }

    [Test]
    public void RefusalMessage_ShowsTheEngineReason_WhenTheCommandIsRejected()
    {
        Assert.That(MissingInputsPin.RefusalMessage(null), Is.Null);
        Assert.That(MissingInputsPin.RefusalMessage(CommandResult.Ok("ok")), Is.Null);
        Assert.That(MissingInputsPin.RefusalMessage(CommandResult.Reject("That hull is too large for the slip.")),
            Is.EqualTo("That hull is too large for the slip."));
        Assert.That(MissingInputsPin.RefusalMessage(new CommandResult(false)), Is.EqualTo("The command was refused."));
    }
}
