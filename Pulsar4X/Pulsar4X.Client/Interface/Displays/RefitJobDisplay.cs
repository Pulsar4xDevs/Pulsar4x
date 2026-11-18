using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Client-side wording for a shipyard refit shown as a normal industry-job row.
/// Progress and status come from the job itself; this formats the name, batch, ETA and parts.
/// </summary>
public static class RefitJobDisplay
{
    /// <summary>
    /// 'Refit &lt;ship&gt; to &lt;design&gt;', or 'Refit &lt;ship&gt;' when ToDesign is empty (a manual refit).
    /// Non-refit jobs keep <see cref="IndustryJobView.Name"/>.
    /// </summary>
    public static string Name(IndustryJobView job)
        => job.Refit is { } info ? Name(info) : job.Name;

    public static string Name(RefitJobInfo info)
    {
        string ship = string.IsNullOrWhiteSpace(info.ShipName) ? "ship" : info.ShipName;
        if (string.IsNullOrWhiteSpace(info.ToDesign))
            return "Refit " + ship;
        return "Refit " + ship + " to " + info.ToDesign;
    }

    /// <summary>A refit is one item, shown as completed/ordered. The engine queues it as 0/1.</summary>
    public static string Batch(IndustryJobView job)
        => job.NumberCompleted.ToString() + "/" + job.NumberOrdered.ToString();

    /// <summary>
    /// About ProductionPointsLeft / PointsPerDay. Null when there is no daily rate (no division by zero).
    /// </summary>
    public static string? Eta(long productionPointsLeft, long pointsPerDay)
    {
        if (pointsPerDay <= 0)
            return null;
        double days = Math.Round((double)productionPointsLeft / pointsPerDay, 1, MidpointRounding.AwayFromZero);
        string n = days.ToString("0.#", CultureInfo.InvariantCulture);
        string unit = n == "1" ? "day" : "days";
        return "About " + n + " " + unit;
    }

    public static string? Eta(IndustryJobView job)
        => job.Refit == null ? null : Eta(job.ProductionPointsLeft, job.Refit.PointsPerDay);

    /// <summary>The yard's daily rate for the head job: refit points when the head is a refit, otherwise the line rate.</summary>
    public static double ProgressPerDay(ProductionLineView line)
    {
        if (line.Jobs.Count > 0 && line.Jobs[0].Refit is { PointsPerDay: > 0 } refit)
            return refit.PointsPerDay;
        return line.CurrentRatePerDay;
    }

    /// <summary>
    /// Design ids for <see cref="QueueShipRefitCommand.MountDesignIds"/>: each missing part's
    /// design id is repeated <see cref="RefitMissingPartView.Count"/> times. Warehouse instances
    /// stay in MountInstanceIds and are not included here.
    /// </summary>
    public static IReadOnlyList<string> MountDesignIds(IEnumerable<RefitMissingPartView>? missing)
    {
        var ids = new List<string>();
        if (missing == null)
            return ids;
        foreach (var part in missing)
        {
            if (string.IsNullOrEmpty(part.DesignId) || part.Count <= 0)
                continue;
            for (int i = 0; i < part.Count; i++)
                ids.Add(part.DesignId);
        }
        return ids;
    }

    /// <summary>True when there is something to queue: parts to remove, warehouse mounts, or awaited designs.</summary>
    public static bool HasWork(IReadOnlyCollection<int> removeIds, IReadOnlyCollection<int> mountIds, IEnumerable<RefitMissingPartView>? missing)
        => (removeIds != null && removeIds.Count > 0)
            || (mountIds != null && mountIds.Count > 0)
            || MountDesignIds(missing).Count > 0;

    /// <summary>
    /// The engine joins several stall reasons with "; ". Split them so the row and tooltip
    /// can show one reason per line.
    /// </summary>
    public static string WaitReasonText(string? waitReason)
    {
        if (string.IsNullOrWhiteSpace(waitReason))
            return "";
        var parts = waitReason.Split("; ", StringSplitOptions.None)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();
        return string.Join("\n", parts);
    }

    public static string PartsText(RefitJobInfo info)
    {
        var lines = new List<string>();
        lines.Add("Removing: " + JoinParts(info.Removing));
        // Mounting already includes awaited names from the engine; list it as-is and do not append WaitingFor again.
        lines.Add("Mounting: " + JoinParts(info.Mounting));
        if (info.WaitingFor is { Count: > 0 } waiting && waiting.Any(p => !string.IsNullOrWhiteSpace(p)))
            lines.Add("Waiting for: " + JoinParts(info.WaitingFor));
        return string.Join("\n", lines);
    }

    /// <summary>Status, wait reason, points left, ETA and the parts list, for a tooltip or detail view.</summary>
    public static string Tooltip(IndustryJobView job)
    {
        var lines = new List<string>();
        if (job.Refit is { } info)
        {
            if (WaitReasonText(info.WaitReason) is { Length: > 0 } reason)
                lines.Add(reason);
            lines.Add(PartsText(info));
        }
        lines.Add("Industry points left: " + job.ProductionPointsLeft.ToString("N0", CultureInfo.InvariantCulture));
        if (Eta(job) is { } eta)
            lines.Add("ETA " + eta);
        return string.Join("\n", lines);
    }

    static string JoinParts(IReadOnlyList<string> parts)
    {
        var names = (parts ?? Array.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        return names.Count == 0 ? "none" : string.Join(", ", names);
    }
}
