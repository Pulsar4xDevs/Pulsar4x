using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests;

/// <summary>Stock-keeping field and idle-job text. No ImGui and no game.</summary>
[TestFixture]
public class StockOrderUiTests
{
    static ConstructibleItemView Design(bool ship)
        => new("d", ship ? "Hull" : "Plastic", 1, 1, false) { IsShipDesign = ship };

    static IndustryJobView Job(
        bool repeat = true,
        bool idle = false,
        bool missing = false,
        long? stockBelow = null,
        long? stockOnHand = null,
        string idleReason = "",
        string status = "Processing",
        RefitJobInfo? refit = null,
        RecycleJobInfo? recycle = null)
        => new("job", "Gizmo", 0, 4, repeat, idle ? "Idle" : status, missing, 0, 10)
        {
            StockBelow = stockBelow,
            StockOnHand = stockOnHand,
            IsIdle = idle,
            IdleReason = idleReason,
            Refit = refit,
            Recycle = recycle,
        };

    [Test]
    public void ShowNewJobField_OnlyWhenAHeadIsSeated_RepeatIsOn_AndTheDesignIsNotAShip()
    {
        Assert.That(StockOrderUi.ShowNewJobField(7, true, false), Is.True);
        Assert.That(StockOrderUi.ShowNewJobField(7, true, Design(ship: false)), Is.True);
        Assert.That(StockOrderUi.ShowNewJobField(-1, true, false), Is.False, "hidden when the seat is empty, not merely disabled");
        Assert.That(StockOrderUi.ShowNewJobField(7, false, false), Is.False, "hidden until Repeat is ticked");
        Assert.That(StockOrderUi.ShowNewJobField(7, true, true), Is.False, "ships have no stock level");
        Assert.That(StockOrderUi.ShowNewJobField(7, true, Design(ship: true)), Is.False);
        Assert.That(StockOrderUi.ShowNewJobField(7, true, (ConstructibleItemView?)null), Is.False);
        Assert.That(StockOrderUi.ShowNewJobField(0, true, false), Is.True, "any seated id, not only positive-looking ones");
    }

    [Test]
    public void ShowEditField_OnlyForARepeatJob_WithAHead_AndNeverForAShipOrRefit()
    {
        var repeat = Job(repeat: true, stockBelow: 10);
        var oneShot = Job(repeat: false);
        var refit = Job(repeat: true, refit: new RefitJobInfo(1, "Ship", "A", "B"));

        Assert.That(StockOrderUi.ShowEditField(7, repeat, lineIsShipyard: false), Is.True);
        Assert.That(StockOrderUi.ShowEditField(-1, repeat, lineIsShipyard: false), Is.False);
        Assert.That(StockOrderUi.ShowEditField(7, oneShot, lineIsShipyard: false), Is.False);
        Assert.That(StockOrderUi.ShowEditField(7, refit, lineIsShipyard: false), Is.False);
        Assert.That(StockOrderUi.ShowEditField(7, repeat, lineIsShipyard: true), Is.False);
        Assert.That(StockOrderUi.ShowEditField(7, true, false, false), Is.True);
    }

    [Test]
    public void TryParseLevel_AcceptsABlankClear_AndRejectsZeroOrJunk()
    {
        Assert.That(StockOrderUi.TryParseLevel("", out var none, out var error), Is.True);
        Assert.That(none, Is.Null);
        Assert.That(error, Is.Null);

        Assert.That(StockOrderUi.TryParseLevel("   ", out none, out error), Is.True);
        Assert.That(none, Is.Null);

        Assert.That(StockOrderUi.TryParseLevel("100", out var level, out error), Is.True);
        Assert.That(level, Is.EqualTo(100));
        Assert.That(StockOrderUi.TryParseLevel(" 1,000 ", out level, out error), Is.True);
        Assert.That(level, Is.EqualTo(1000));
        Assert.That(StockOrderUi.FormatLevel(1000), Is.EqualTo("1000"));
        Assert.That(StockOrderUi.FormatLevel(null), Is.EqualTo(""));

        Assert.That(StockOrderUi.TryParseLevel("0", out level, out error), Is.False);
        Assert.That(level, Is.Null);
        Assert.That(error, Is.EqualTo(StockOrderUi.LevelMustBeOneOrMore));
        Assert.That(error, Is.EqualTo("A stock level must be 1 or more."));

        Assert.That(StockOrderUi.TryParseLevel("-3", out level, out error), Is.False);
        Assert.That(error, Is.EqualTo(StockOrderUi.LevelMustBeOneOrMore));

        Assert.That(StockOrderUi.TryParseLevel("abc", out level, out error), Is.False);
        Assert.That(error, Is.EqualTo(StockOrderUi.InvalidLevel));
    }

    [Test]
    public void QueueAndEditCommands_PassTheCurrentBatchAndRepeat_AndNullClearsTheLevel()
    {
        Assert.That(
            StockOrderUi.TryQueueStockBelow(7, true, false, "50", out var below, out var error),
            Is.True);
        Assert.That(below, Is.EqualTo(50));
        Assert.That(error, Is.Null);

        Assert.That(StockOrderUi.TryQueueStockBelow(-1, true, false, "50", out below, out error), Is.True);
        Assert.That(below, Is.Null, "a hidden field is not sent, even if text is left over");

        Assert.That(StockOrderUi.TryQueueStockBelow(7, true, true, "50", out below, out error), Is.True);
        Assert.That(below, Is.Null, "a ship design does not send a level");

        Assert.That(StockOrderUi.TryQueueStockBelow(7, false, false, "50", out below, out error), Is.True);
        Assert.That(below, Is.Null);

        Assert.That(StockOrderUi.TryQueueStockBelow(7, true, false, "0", out below, out error), Is.False);
        Assert.That(error, Is.EqualTo(StockOrderUi.LevelMustBeOneOrMore));

        var queue = StockOrderUi.QueueCommand(10, "factory", "d-p", 4, true, false, 50);
        Assert.That(queue.TargetEntityId, Is.EqualTo(10));
        Assert.That(queue.ProductionLineId, Is.EqualTo("factory"));
        Assert.That(queue.DesignId, Is.EqualTo("d-p"));
        Assert.That(queue.Quantity, Is.EqualTo(4));
        Assert.That(queue.Repeat, Is.True);
        Assert.That(queue.AutoInstall, Is.False);
        Assert.That(queue.StockBelow, Is.EqualTo(50));

        var edit = StockOrderUi.EditCommand(10, "factory", "job-1", 4, true, 80);
        Assert.That(edit.TargetEntityId, Is.EqualTo(10));
        Assert.That(edit.ProductionLineId, Is.EqualTo("factory"));
        Assert.That(edit.JobId, Is.EqualTo("job-1"));
        Assert.That(edit.Quantity, Is.EqualTo(4));
        Assert.That(edit.Repeat, Is.True);
        Assert.That(edit.StockBelow, Is.EqualTo(80));

        var clear = StockOrderUi.EditCommand(10, "factory", "job-1", 4, true, null);
        Assert.That(clear.Quantity, Is.EqualTo(4));
        Assert.That(clear.Repeat, Is.True);
        Assert.That(clear.StockBelow, Is.Null);
    }

    [Test]
    public void IdleStatusAndTooltip_DoNotUseTheStalledColour()
    {
        var idle = Job(idle: true, missing: true, stockBelow: 100, stockOnHand: 120, idleReason: "Stock 120 is at or above 100");
        Assert.That(StockOrderUi.IsIdle(idle), Is.True);
        Assert.That(StockOrderUi.IsStalled(idle), Is.False, "idle must not look stalled even if MissingResources is set");
        Assert.That(StockOrderUi.StatusText(idle), Is.EqualTo("Idle"));
        Assert.That(StockOrderUi.StockSummary(idle), Is.EqualTo("120 / below 100"));
        Assert.That(StockOrderUi.IdleTooltip(idle), Is.EqualTo("Stock 120 is at or above 100\n120 / below 100"));

        var running = Job(missing: true, stockBelow: 100, stockOnHand: 40, status: "MissingResources");
        Assert.That(StockOrderUi.IsStalled(running), Is.True);
        Assert.That(StockOrderUi.StatusText(running), Is.EqualTo("MissingResources"));
        Assert.That(StockOrderUi.IdleTooltip(running), Is.EqualTo(""));

        var stalledRefit = Job(repeat: false, refit: new RefitJobInfo(1, "Ship", "A", "B") { WaitReason = "Waiting for a part" });
        Assert.That(StockOrderUi.IsStalled(stalledRefit), Is.True);

        var stalledRecycle = Job(repeat: false, recycle: new RecycleJobInfo("Ship", 1, "hull") { WaitReason = "That ship is not in transfer range." });
        Assert.That(StockOrderUi.IsStalled(stalledRecycle), Is.True);

        var busy = Job(status: "Processing");
        Assert.That(StockOrderUi.IsStalled(busy), Is.False);
        Assert.That(StockOrderUi.StockSummary(busy), Is.EqualTo(""));
    }

    [Test]
    public void RefusalMessage_ShowsTheEngineReason_WhenTheCommandIsRejected()
    {
        Assert.That(StockOrderUi.RefusalMessage(null), Is.Null);
        Assert.That(StockOrderUi.RefusalMessage(CommandResult.Ok("ok")), Is.Null);
        Assert.That(
            StockOrderUi.RefusalMessage(CommandResult.Reject("A stock order needs an industry head. Seat one on this colony first.")),
            Is.EqualTo("A stock order needs an industry head. Seat one on this colony first."));
        Assert.That(StockOrderUi.RefusalMessage(new CommandResult(false)), Is.EqualTo("The command was refused."));
    }
}
