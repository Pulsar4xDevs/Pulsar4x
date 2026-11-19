using System;
using System.Collections.Generic;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Pin set for the production Missing Inputs panel. No ImGui.
/// A pin stays until the job leaves every line (finished or cancelled) or the player unpins it.
/// </summary>
public sealed class MissingInputsPin
{
    readonly HashSet<(int ColonyId, string JobId)> _pins = new();

    public int Count => _pins.Count;

    public bool Contains(int colonyId, string jobId)
        => !string.IsNullOrEmpty(jobId) && _pins.Contains((colonyId, jobId));

    public void Add(int colonyId, string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
            return;
        _pins.Add((colonyId, jobId));
    }

    /// <summary>Adds the job if it is not pinned; removes it if it is. Returns true when it is pinned afterwards.</summary>
    public bool Toggle(int colonyId, string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
            return false;
        var key = (colonyId, jobId);
        if (_pins.Remove(key))
            return false;
        _pins.Add(key);
        return true;
    }

    public void Unpin(int colonyId, string jobId)
        => _pins.Remove((colonyId, jobId));

    /// <summary>
    /// Drops pins on <paramref name="colonyId"/> whose job is no longer on any production line.
    /// Pins for other colonies are left alone.
    /// </summary>
    public int Prune(int colonyId, IndustryView? industry)
    {
        var present = JobIds(industry);
        return _pins.RemoveWhere(pin => pin.ColonyId == colonyId && !present.Contains(pin.JobId));
    }

    /// <summary>Pinned jobs still present on this colony, in line then queue order.</summary>
    public IReadOnlyList<IndustryJobView> PinnedJobs(int colonyId, IndustryView? industry)
    {
        var list = new List<IndustryJobView>();
        if (industry == null)
            return list;
        foreach (var line in industry.ProductionLines)
        {
            foreach (var job in line.Jobs)
            {
                if (_pins.Contains((colonyId, job.JobId)))
                    list.Add(job);
            }
        }
        return list;
    }

    /// <summary>
    /// Queue is offered only when the industry seat is empty (<see cref="IndustryView.IndustryHeadId"/> is -1),
    /// this input can be built on the colony, and already-queued jobs do not already cover the shortfall
    /// (<see cref="MissingInputView.QueueQuantity"/> is above 0).
    /// </summary>
    public static bool ShowQueue(int industryHeadId, MissingInputView? input)
        => industryHeadId == -1 && input is { CanBuild: true } && input.QueueQuantity > 0;

    public static bool ShowQueue(int industryHeadId, bool canBuild)
        => industryHeadId == -1 && canBuild;

    public static string StockText(MissingInputView input)
        => input.Short.ToString("N0") + " short / " + input.Required.ToString("N0")
            + " required (" + input.InStock.ToString("N0") + " in stock)";

    /// <summary>"Queued N" when jobs already on the colony will still make some of this input; otherwise empty.</summary>
    public static string QueuedText(MissingInputView input)
        => input.Queued > 0 ? "Queued " + input.Queued.ToString("N0") : "";

    public static string LineText(MissingInputView input)
        => string.IsNullOrEmpty(input.LineName) ? "—" : input.LineName;

    public static string QueueButtonText(MissingInputView input)
        => "Queue " + input.QueueQuantity.ToString("N0") + (input.QueueQuantity == 1 ? " job" : " jobs") + " on " + input.LineName;

    public static string HeadText(int industryHeadId, string? industryHeadName)
        => "Industry head: " + IndustrySeatUi.HolderText(industryHeadId, industryHeadName);

    public static QueueIndustryJobCommand QueueCommand(int colonyId, MissingInputView input)
        => new(colonyId, input.LineId, input.DesignId, input.QueueQuantity, false, false);

    public static string? RefusalMessage(CommandResult? result)
    {
        if (result == null || result.Accepted)
            return null;
        return string.IsNullOrEmpty(result.RejectionReason)
            ? "The command was refused."
            : result.RejectionReason;
    }

    static HashSet<string> JobIds(IndustryView? industry)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (industry == null)
            return ids;
        foreach (var line in industry.ProductionLines)
        {
            foreach (var job in line.Jobs)
            {
                if (!string.IsNullOrEmpty(job.JobId))
                    ids.Add(job.JobId);
            }
        }
        return ids;
    }
}
