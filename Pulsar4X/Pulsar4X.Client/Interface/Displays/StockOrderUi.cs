using System;
using System.Globalization;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Decision logic for the "when stock drops below X" field. No ImGui.
/// A stock order is a repeat job with a level; empty input means no level (or clear one).
/// </summary>
public static class StockOrderUi
{
    public const string FieldLabel = "when stock drops below";
    public const string LevelMustBeOneOrMore = "A stock level must be 1 or more.";
    public const string InvalidLevel = "Enter a whole number of 1 or more, or leave it blank to clear.";

    /// <summary>
    /// The new-job field is shown only with a seated head, Repeat ticked, and a non-ship design.
    /// When the seat is empty the field is hidden, not disabled.
    /// </summary>
    public static bool ShowNewJobField(int industryHeadId, bool repeat, bool isShipDesign)
        => industryHeadId != -1 && repeat && !isShipDesign;

    public static bool ShowNewJobField(int industryHeadId, bool repeat, ConstructibleItemView? design)
        => design != null && ShowNewJobField(industryHeadId, repeat, design.IsShipDesign);

    /// <summary>
    /// Edit the level on an existing repeat job when a head is seated. Hidden for ships and refits
    /// (a shipyard line's non-refit jobs are ship designs).
    /// </summary>
    public static bool ShowEditField(int industryHeadId, bool repeat, bool isRefit, bool lineIsShipyard)
        => industryHeadId != -1 && repeat && !isRefit && !lineIsShipyard;

    public static bool ShowEditField(int industryHeadId, IndustryJobView job, bool lineIsShipyard)
        => ShowEditField(industryHeadId, job.Repeat, job.IsRefit, lineIsShipyard);

    /// <summary>
    /// Parses the field. Blank or whitespace is a successful clear (null).
    /// Commas in a thousands grouping are allowed. The level must be 1 or more, matching the engine.
    /// </summary>
    public static bool TryParseLevel(string? text, out long? level, out string? error)
    {
        level = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var trimmed = text.Trim();
        if (trimmed.Contains(','))
            trimmed = trimmed.Replace(",", "", StringComparison.Ordinal);

        if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            error = InvalidLevel;
            return false;
        }
        if (value < 1)
        {
            error = LevelMustBeOneOrMore;
            return false;
        }
        level = value;
        return true;
    }

    public static string FormatLevel(long? level)
        => level is long value ? value.ToString(CultureInfo.InvariantCulture) : "";

    /// <summary>
    /// StockBelow to send on a new job. Null when the field is hidden or the input is blank.
    /// False when the text is present but invalid; <paramref name="error"/> then has the reason.
    /// </summary>
    public static bool TryQueueStockBelow(
        int industryHeadId,
        bool repeat,
        bool isShipDesign,
        string? levelText,
        out long? stockBelow,
        out string? error)
    {
        stockBelow = null;
        error = null;
        if (!ShowNewJobField(industryHeadId, repeat, isShipDesign))
            return true;
        return TryParseLevel(levelText, out stockBelow, out error);
    }

    public static QueueIndustryJobCommand QueueCommand(
        int colonyId,
        string lineId,
        string designId,
        int quantity,
        bool repeat,
        bool autoInstall,
        long? stockBelow)
        => new(colonyId, lineId, designId, quantity, repeat, autoInstall, stockBelow);

    /// <summary>Restates the whole job. A null <paramref name="stockBelow"/> clears the level.</summary>
    public static EditIndustryJobCommand EditCommand(
        int colonyId,
        string lineId,
        string jobId,
        int quantity,
        bool repeat,
        long? stockBelow)
        => new(colonyId, lineId, jobId, quantity, repeat, stockBelow);

    public static bool IsIdle(IndustryJobView job) => job.IsIdle;

    /// <summary>Idle jobs must not use the stalled (missing-resources) colour.</summary>
    public static bool IsStalled(IndustryJobView job)
        => !job.IsIdle && (job.MissingResources || job.Refit is { WaitReason.Length: > 0 });

    public static string StatusText(IndustryJobView job)
        => job.IsIdle ? "Idle" : job.Status;

    /// <summary>IdleReason plus on-hand / below, for the row or tooltip.</summary>
    public static string IdleTooltip(IndustryJobView job)
    {
        if (!job.IsIdle)
            return "";
        string stock = StockSummary(job);
        if (!string.IsNullOrEmpty(job.IdleReason) && stock.Length > 0 && !job.IdleReason.Contains(stock, StringComparison.Ordinal))
            return job.IdleReason + "\n" + stock;
        if (!string.IsNullOrEmpty(job.IdleReason))
            return job.IdleReason;
        return stock.Length > 0 ? stock : "Idle";
    }

    public static string StockSummary(IndustryJobView job)
    {
        if (job.StockOnHand is long have && job.StockBelow is long below)
            return have.ToString("N0") + " / below " + below.ToString("N0");
        if (job.StockBelow is long belowOnly)
            return "below " + belowOnly.ToString("N0");
        return "";
    }

    public static string? RefusalMessage(CommandResult? result)
    {
        if (result == null || result.Accepted)
            return null;
        return string.IsNullOrEmpty(result.RejectionReason)
            ? "The command was refused."
            : result.RejectionReason;
    }
}
