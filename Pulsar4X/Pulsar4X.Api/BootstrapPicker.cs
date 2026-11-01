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

    /// <summary>The destination's planet as the infrastructure check sees it.</summary>
    public sealed record BootstrapClimate(string PlanetName, double GravityMetresPerSec2, double PressureAtm);

    /// <summary>One infrastructure design the source holds a crate of, and its window.</summary>
    public sealed record InfrastructureOption(string DesignId, string Name, InfrastructureWindow Window);

    /// <summary>
    /// The destination colony's planet: gravity from its body and pressure from its atmosphere (none is 0).
    /// Null when the colony or its planet is not known yet.
    /// </summary>
    public static BootstrapClimate? ClimateOf(EntitySnapshot? colony, Func<int, EntitySnapshot?> find)
    {
        if (colony?.GetView<ColonyView>()?.PlanetEntityId is not int planetId || find(planetId) is not { } planet)
            return null;
        var body = planet.GetView<BodyView>();
        if (body == null)
            return null;
        return new BootstrapClimate(planet.GetView<NameView>()?.Name ?? "", body.GravityMetresPerSec2, planet.GetView<AtmosphereView>()?.PressureAtm ?? 0);
    }

    /// <summary>
    /// The infrastructure designs with finished crates at the source. The check reads source cargo only, so the
    /// destination's stock and the ships' holds do not count.
    /// </summary>
    public static IReadOnlyList<InfrastructureOption> InfrastructureAtSource(ComponentDesignsSnapshot designs, EntitySnapshot? source)
    {
        var found = new List<InfrastructureOption>();
        if (source?.GetView<CargoStorageView>() is not { } storage)
            return found;
        foreach (var store in storage.Stores)
        {
            foreach (var item in store.Items)
            {
                if (!item.IsCrate || item.Units <= 0 || string.IsNullOrEmpty(item.DesignId)
                    || found.Any(o => o.DesignId == item.DesignId))
                    continue;
                var design = designs.Designs.FirstOrDefault(d => d.Id == item.DesignId);
                if (design?.Infrastructure != null)
                    found.Add(new InfrastructureOption(design.Id, design.Name, design.Infrastructure));
            }
        }
        return found.OrderBy(o => o.Name, StringComparer.Ordinal).ThenBy(o => o.DesignId, StringComparer.Ordinal).ToList();
    }

    /// <summary>The fitting designs, best fit first, from the source's crates and the destination's climate.</summary>
    public static IReadOnlyList<InfrastructureOption> FittingInfrastructure(
        IReadOnlyList<InfrastructureOption> atSource, BootstrapClimate climate)
    {
        var ranked = InfrastructureFit.Rank(
            atSource.Select(o => (o.DesignId, o.Window)), climate.GravityMetresPerSec2, climate.PressureAtm);
        return ranked.Select(id => atSource.First(o => o.DesignId == id)).ToList();
    }

    /// <summary>
    /// What blocks the order on infrastructure, or null. Only a list with an infrastructure line is checked, and an
    /// unknown climate blocks nothing here, since the server decides.
    /// </summary>
    public static string? CheckInfrastructure(
        BootstrapOrderList list, ComponentDesignsSnapshot designs,
        IReadOnlyList<InfrastructureOption> atSource, BootstrapClimate? climate)
    {
        if (climate == null || list.InfrastructureEntry(designs) == null)
            return null;
        return FittingInfrastructure(atSource, climate).Count == 0 ? InfrastructureFit.NoFitMessage(climate.PlanetName) : null;
    }

    /// <summary>
    /// Puts the best fit on the list, the default the player may then tweak. Call it when the source or the
    /// destination changes. False when nothing fits or the list has no infrastructure line.
    /// </summary>
    public static bool ApplyBestFit(
        BootstrapOrderList list, ComponentDesignsSnapshot designs,
        IReadOnlyList<InfrastructureOption> atSource, BootstrapClimate climate)
    {
        var fits = FittingInfrastructure(atSource, climate);
        return fits.Count > 0 && list.UseInfrastructure(designs, fits[0].DesignId);
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
