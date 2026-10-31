namespace Pulsar4X.Api;

/// <summary>One crate-able design with finished stock, and how many the ship could take.</summary>
public sealed record BootstrapCrateOption(string DesignId, string Name, long Available);

/// <summary>
/// The option lists and checks behind the Bootstrap colony picker. It reads snapshots only, so the
/// client draws it and tests run it without a window. The checks mirror the translator's, in order.
/// </summary>
public static class BootstrapPicker
{
    public const string NoCaptain = "This ship has no captain. Plot an action instead.";
    public const string NoBay = "The ship has no Construction Bay.";
    public const string OutOfReach = "The target is outside this ship's reach.";

    /// <summary>Whether the ship can take this order at all. Null when it can.</summary>
    public static string? CheckShip(ShipSnapshot ship)
    {
        if (ship.CommanderId == null)
            return NoCaptain;
        if (!ship.HasConstructionBay)
            return NoBay;
        return null;
    }

    /// <summary>The three reach bands the picker groups colonies under, narrowest first.</summary>
    public enum ReachBand
    {
        Ship = 0,
        Well = 1,
        System = 2,
    }

    /// <summary>The widest band the ship's effective span reaches.</summary>
    public static ReachBand ReachOf(ShipSnapshot ship)
        => ship.CommandSpan == CommandSpanLabels.System || ship.CommandSpan == CommandSpanLabels.Neighbor ? ReachBand.System
            : ship.CommandSpan == CommandSpanLabels.Well ? ReachBand.Well
            : ReachBand.Ship;

    static string BandTitle(ReachBand band) => band switch
    {
        ReachBand.Well => "This well",
        ReachBand.System => "This system",
        _ => "This ship",
    };

    /// <summary>
    /// Why a grayed colony row cannot be picked, and what to change. Null when the row is not blocked.
    /// <paramref name="pair"/> is the band the row and the other chosen colony share, null when no other
    /// colony is chosen. <paramref name="shown"/> is the band the row is listed under.
    /// A band the ship's reach does not cover names the part to change (bridge, captain's rank, empty
    /// chair). A pair that sits in another band says which one to pick under.
    /// </summary>
    public static string? Blocked(ShipSnapshot ship, ReachBand? pair, ReachBand shown)
    {
        if(pair == shown)
            return null;
        var needed = pair ?? shown;
        if(needed > ReachOf(ship))
        {
            string fix = needed == ReachBand.System ? ship.ReachFixSystem : ship.ReachFixWell;
            if(string.IsNullOrEmpty(fix))
                return ship.SpanLine;
            return pair == null ? fix : fix + " Then pick under " + BandTitle(needed) + ".";
        }
        if(pair != null)
            return "These two colonies sit in " + BandTitle(pair.Value).ToLowerInvariant() + ". Pick them under " + BandTitle(pair.Value) + ".";
        return "No other colony of yours with a warehouse sits in " + BandTitle(shown).ToLowerInvariant() + ".";
    }

    /// <summary>The colonies the picker offers: owned, with a warehouse.</summary>
    public static bool IsPickable(EntitySnapshot colony)
        => colony.Relation == OwnerRelation.Owned
            && colony.Kind == BodyKind.Colony
            && colony.HasView<CargoStorageView>();

    /// <summary>Destinations once the source is chosen. The source itself is left out.</summary>
    public static bool IsPickableDestination(EntitySnapshot colony, int sourceId)
        => colony.Id != sourceId && IsPickable(colony);

    /// <summary>
    /// Crate designs with finished stock at the source, counting crates already in the ship's hold
    /// (the planner takes the hold first). Sorted by name. Designs with no stock are not listed.
    /// </summary>
    public static IReadOnlyList<BootstrapCrateOption> CrateOptions(EntitySnapshot? source, EntitySnapshot? ship)
    {
        var stock = new Dictionary<string, (string Name, long Units)>(StringComparer.Ordinal);
        foreach (var holder in new[] { source, ship })
        {
            if (holder?.GetView<CargoStorageView>() is not { } storage)
                continue;
            foreach (var store in storage.Stores)
            {
                foreach (var item in store.Items)
                {
                    if (!item.IsCrate || item.Units <= 0 || string.IsNullOrEmpty(item.DesignId))
                        continue;
                    stock.TryGetValue(item.DesignId, out var held);
                    stock[item.DesignId] = (item.Name, held.Units + item.Units);
                }
            }
        }
        return stock
            .Select(pair => new BootstrapCrateOption(pair.Key, pair.Value.Name, pair.Value.Units))
            .OrderBy(option => option.Name, StringComparer.Ordinal)
            .ThenBy(option => option.DesignId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Keeps the typed counts in step with the live options: a new design starts at zero, a count never
    /// passes the stock or goes below zero, and a design that left the list is dropped.
    /// </summary>
    public static void SyncCounts(Dictionary<string, int> counts, IReadOnlyList<BootstrapCrateOption> options)
    {
        var live = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in options)
        {
            live.Add(option.DesignId);
            int cap = Cap(option);
            if (!counts.TryGetValue(option.DesignId, out var count))
                counts[option.DesignId] = 0;
            else if (count > cap)
                counts[option.DesignId] = cap;
            else if (count < 0)
                counts[option.DesignId] = 0;
        }
        foreach (var id in counts.Keys.Where(id => !live.Contains(id)).ToList())
            counts.Remove(id);
    }

    /// <summary>The lines to send: designs with a count above zero, held to the stock.</summary>
    public static List<BootstrapCrateRequest> Requests(
        IReadOnlyList<BootstrapCrateOption> options, IReadOnlyDictionary<string, int> counts)
    {
        var lines = new List<BootstrapCrateRequest>();
        foreach (var option in options)
        {
            if (!counts.TryGetValue(option.DesignId, out var count) || count <= 0)
                continue;
            lines.Add(new BootstrapCrateRequest(option.DesignId, Math.Min(count, Cap(option))));
        }
        return lines;
    }

    /// <summary>
    /// Why the order cannot be sent yet, or null. <paramref name="inReach"/> is the client's span test of
    /// the pair, the same one the Move goods picker uses.
    /// </summary>
    public static string? Validate(
        ShipSnapshot ship, int sourceId, int destId, IReadOnlyList<BootstrapCrateRequest> lines, bool inReach)
    {
        if (CheckShip(ship) is { } shipProblem)
            return shipProblem;
        if (sourceId < 0)
            return "Pick a source colony.";
        if (destId < 0)
            return "Pick a destination colony.";
        if (sourceId == destId)
            return "Pick two different colonies.";
        if (lines.Count == 0)
            return "Nothing to build.";
        foreach (var line in lines)
        {
            if (line.Count <= 0)
                return "Crate counts must be positive.";
        }
        if (!inReach)
            return OutOfReach;
        return null;
    }

    static int Cap(BootstrapCrateOption option)
        => option.Available > int.MaxValue ? int.MaxValue : (int)option.Available;
}
