using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Decision logic for the colony market-head seat. No ImGui.
/// Candidates are unassigned civilians who do not hold a city-hall chair, an industry seat, or another market seat.
/// </summary>
public static class MarketSeatUi
{
    public const string EmptyLabel = "Empty";
    public const string PostLabel = "Market head";

    public static bool IsSeated(int marketHeadId) => marketHeadId >= 0;

    public static string HolderText(int marketHeadId, string? marketHeadName)
        => IsSeated(marketHeadId) && !string.IsNullOrEmpty(marketHeadName)
            ? marketHeadName
            : EmptyLabel;

    /// <summary>
    /// Entity ids already holding a city-hall chair, an industry seat, or a market seat,
    /// from colony views. These people cannot take a market seat (one person, one post).
    /// </summary>
    public static IReadOnlyList<int> BlockedIds(IEnumerable<ColonyView?>? colonies)
    {
        var ids = new List<int>();
        if (colonies == null)
            return ids;
        foreach (var colony in colonies)
        {
            if (colony == null)
                continue;
            if (colony.AdministratorId >= 0)
                ids.Add(colony.AdministratorId);
            if (colony.IndustryHeadId >= 0)
                ids.Add(colony.IndustryHeadId);
            if (colony.MarketHeadId >= 0)
                ids.Add(colony.MarketHeadId);
        }
        return ids;
    }

    /// <summary>
    /// Civilians with <see cref="CommanderSnapshot.IsAssigned"/> false, excluding anyone in
    /// <paramref name="blockedIds"/>, ordered by name.
    /// </summary>
    public static IReadOnlyList<CommanderSnapshot> Candidates(
        IEnumerable<CommanderSnapshot>? commanders,
        IEnumerable<int>? blockedIds = null)
    {
        var blocked = blockedIds == null ? new HashSet<int>() : new HashSet<int>(blockedIds);
        return (commanders ?? Array.Empty<CommanderSnapshot>())
            .Where(person => person.Kind == CommanderKind.Civilian
                && !person.IsAssigned
                && !blocked.Contains(person.Id))
            .OrderBy(person => person.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// The seated market head's portrait, looked up on the roster because
    /// <see cref="ColonyView"/> has no market-head portrait fields.
    /// Empty when the seat is vacant or the person is not in the roster.
    /// </summary>
    public static PortraitRef Portrait(int marketHeadId, IEnumerable<CommanderSnapshot>? commanders)
    {
        if (marketHeadId < 0 || commanders == null)
            return default;
        foreach (var person in commanders)
        {
            if (person.Id == marketHeadId)
                return PortraitIdentity.Of(person);
        }
        return default;
    }

    /// <summary>The message to show after a seat/unseat command, or null when it was accepted.</summary>
    public static string? RefusalMessage(CommandResult? result)
    {
        if (result == null || result.Accepted)
            return null;
        return string.IsNullOrEmpty(result.RejectionReason)
            ? "The command was refused."
            : result.RejectionReason;
    }
}
