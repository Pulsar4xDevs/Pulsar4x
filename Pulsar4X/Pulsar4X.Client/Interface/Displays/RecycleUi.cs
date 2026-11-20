using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>A ship lock while a recycle job names it, shown wherever a refit lock already appears.</summary>
public readonly record struct RecycleShipLock(string YardName, string JobName, IReadOnlyList<ResourceRequirement> Returns, string WaitReason);

/// <summary>
/// Decision and formatting logic for the colony Recycle section and queued recycle jobs. No ImGui.
/// The return fraction matches <c>GameSettings.RecycleReturnFraction</c> (0.5); expected returns
/// and point cost come from the view, already computed.
/// </summary>
public static class RecycleUi
{
    /// <summary>Same value as <c>Pulsar4X.Engine.GameSettings.RecycleReturnFraction</c>.</summary>
    public const double ReturnFraction = 0.5;

    public const string ShipKind = "Ship";
    public const string ComponentKind = "Component";

    public static bool CanQueue(RecycleCandidateView candidate)
        => candidate != null && string.IsNullOrEmpty(candidate.Refusal);

    public static bool IsShip(RecycleCandidateView candidate)
        => candidate != null && string.Equals(candidate.Kind, ShipKind, StringComparison.Ordinal);

    public static bool IsItem(RecycleCandidateView candidate)
        => candidate != null && string.Equals(candidate.Kind, ComponentKind, StringComparison.Ordinal);

    /// <summary>Quantity for a warehouse item: at least 1, at most <see cref="RecycleCandidateView.Available"/>.</summary>
    public static int ClampQuantity(int quantity, long available)
    {
        int max = available <= 0 ? 1 : (int)Math.Min(int.MaxValue, available);
        if (quantity < 1)
            return 1;
        return quantity > max ? max : quantity;
    }

    /// <summary>Null when the candidate cannot be queued (use <see cref="CanQueue"/> and show <see cref="RecycleCandidateView.Refusal"/>).</summary>
    public static GameCommand? Command(int colonyId, RecycleCandidateView candidate, int quantity = 1)
    {
        if (!CanQueue(candidate))
            return null;
        if (IsShip(candidate))
            return new RecycleShipCommand(colonyId, candidate.ShipEntityId, candidate.LineId);
        if (IsItem(candidate))
            return new RecycleComponentCommand(colonyId, candidate.DesignId, ClampQuantity(quantity, candidate.Available), candidate.LineId);
        return null;
    }

    public static string ReturnsText(IEnumerable<ResourceRequirement>? returns)
    {
        if (returns == null)
            return "No returns";
        var parts = returns
            .Where(row => row != null && !string.IsNullOrEmpty(row.Name) && row.Amount > 0)
            .Select(row => row.Amount.ToString("N0") + " " + row.Name)
            .ToList();
        return parts.Count == 0 ? "No returns" : string.Join(", ", parts);
    }

    public static IReadOnlyList<ResourceRequirement> ReturnsForQuantity(IEnumerable<ResourceRequirement>? returns, int quantity)
    {
        int count = Math.Max(1, quantity);
        var rows = new List<ResourceRequirement>();
        if (returns == null)
            return rows;
        foreach (var row in returns)
        {
            if (row == null || string.IsNullOrEmpty(row.Name) || row.Amount <= 0)
                continue;
            long amount = row.Amount;
            if (count > 1 && amount > long.MaxValue / count)
                amount = long.MaxValue;
            else
                amount *= count;
            rows.Add(new ResourceRequirement(row.Name, amount));
        }
        return rows;
    }

    public static string PointCostText(long pointCost, int quantity = 1)
    {
        string points = pointCost.ToString("N0") + (pointCost == 1 ? " point" : " points");
        if (quantity > 1)
            return (pointCost * (long)quantity).ToString("N0") + " points (" + points + " each)";
        return points;
    }

    public static string ReturnFractionText()
        => ((int)Math.Round(ReturnFraction * 100)).ToString() + "%";

    /// <summary>Job-list label: "Recycling &lt;name&gt;". Non-recycle jobs keep <see cref="RefitJobDisplay.Name"/>.</summary>
    public static string Name(IndustryJobView job)
    {
        if (job == null)
            return "";
        if (!job.IsRecycle)
            return RefitJobDisplay.Name(job);
        string item = ItemName(job.Name);
        return string.IsNullOrEmpty(item) ? "Recycling" : "Recycling " + item;
    }

    static string ItemName(string? jobName)
    {
        if (string.IsNullOrWhiteSpace(jobName))
            return "";
        string name = jobName.Trim();
        foreach (var prefix in new[] { "Recycling ", "Recycle ", "Scrap " })
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return name[prefix.Length..].Trim();
        }
        return name;
    }

    public static string WaitReasonText(string? waitReason)
        => RefitJobDisplay.WaitReasonText(waitReason);

    public static string Tooltip(IndustryJobView job)
    {
        if (job?.Recycle is not { } recycle)
            return "";
        var lines = new List<string>();
        string returns = ReturnsText(recycle.Returns);
        if (returns.Length > 0)
            lines.Add("Returns: " + returns);
        string wait = WaitReasonText(recycle.WaitReason);
        if (wait.Length > 0)
            lines.Add(wait);
        return string.Join("\n", lines);
    }

    public static RecycleShipLock? FindShipLock(int shipId, IndustryView? industry)
    {
        if (industry == null || shipId < 0)
            return null;
        foreach (var line in industry.ProductionLines)
        {
            foreach (var job in line.Jobs)
            {
                if (job.IsRecycle && job.Recycle is { ShipEntityId: var id } recycle && id == shipId)
                    return new RecycleShipLock(line.Name, Name(job), recycle.Returns, recycle.WaitReason ?? "");
            }
        }
        return null;
    }

    public static RecycleShipLock? FindShipLock(int shipId, IClientGalaxy? galaxy)
    {
        if (galaxy == null || shipId < 0)
            return null;
        foreach (var summary in galaxy.KnownSystems)
        {
            var system = galaxy.GetSystem(summary.SystemId);
            if (system == null)
                continue;
            foreach (var entity in system.Entities)
            {
                var found = FindShipLock(shipId, entity.GetView<IndustryView>());
                if (found != null)
                    return found;
            }
        }
        return null;
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
