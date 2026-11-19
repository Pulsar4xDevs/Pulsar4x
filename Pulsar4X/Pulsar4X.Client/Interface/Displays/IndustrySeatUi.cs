using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Decision logic for the colony industry-head seat. No ImGui.
/// Candidates are unassigned civilians who do not hold a city-hall chair.
/// </summary>
public static class IndustrySeatUi
{
    public const string EmptyLabel = "Empty";

    public static bool IsSeated(int industryHeadId) => industryHeadId >= 0;

    public static string HolderText(int industryHeadId, string? industryHeadName)
        => IsSeated(industryHeadId) && !string.IsNullOrEmpty(industryHeadName)
            ? industryHeadName
            : EmptyLabel;

    /// <summary>Entity ids of city-hall chairs that are currently filled, from colony views.</summary>
    public static IReadOnlyList<int> CityHallIds(IEnumerable<ColonyView?>? colonies)
    {
        var ids = new List<int>();
        if (colonies == null)
            return ids;
        foreach (var colony in colonies)
        {
            if (colony != null && colony.AdministratorId >= 0)
                ids.Add(colony.AdministratorId);
        }
        return ids;
    }

    public static IReadOnlyList<ColonyView> ColonyViews(IClientGalaxy? galaxy)
    {
        var list = new List<ColonyView>();
        if (galaxy == null)
            return list;
        foreach (var summary in galaxy.KnownSystems)
        {
            var system = galaxy.GetSystem(summary.SystemId);
            if (system == null)
                continue;
            foreach (var entity in system.Entities)
            {
                if (entity.GetView<ColonyView>() is { } view)
                    list.Add(view);
            }
        }
        return list;
    }

    /// <summary>
    /// Civilians with <see cref="CommanderSnapshot.IsAssigned"/> false, excluding anyone whose id
    /// is a filled city-hall chair, ordered by name.
    /// </summary>
    public static IReadOnlyList<CommanderSnapshot> Candidates(
        IEnumerable<CommanderSnapshot>? commanders,
        IEnumerable<int>? cityHallIds = null)
    {
        var blocked = cityHallIds == null ? new HashSet<int>() : new HashSet<int>(cityHallIds);
        return (commanders ?? Array.Empty<CommanderSnapshot>())
            .Where(person => person.Kind == CommanderKind.Civilian
                && !person.IsAssigned
                && !blocked.Contains(person.Id))
            .OrderBy(person => person.Name, StringComparer.Ordinal)
            .ToList();
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
