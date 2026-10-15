using Pulsar4X.Api;
using Pulsar4X.Engine;

namespace Pulsar4X.Factions;

/// <summary>
/// One-way grants and the settlement check that needs both directions.
/// Same faction may trade with itself. <see cref="Game.NeutralFactionId"/> is never a partner.
/// Status (<see cref="FactionStance"/>) does not open a market or reveal a ship.
/// </summary>
public static class FactionStanceRules
{
    public const FactionGrants OpenDefaults = FactionGrants.Trade | FactionGrants.Positions;

    /// <summary>The granter has given this flag to the receiver.</summary>
    public static bool Granted(Game game, int granterId, int receiverId, FactionGrants flag)
    {
        if (granterId == Game.NeutralFactionId || receiverId == Game.NeutralFactionId)
            return false;
        if (flag == FactionGrants.None)
            return false;
        if (!game.Factions.TryGetValue(granterId, out var granter))
            return false;
        if (!granter.TryGetDataBlob<FactionInfoDB>(out var info) || info.Grants == null)
            return false;
        return info.Grants.TryGetValue(receiverId, out var grants) && grants.HasFlag(flag);
    }

    /// <summary>
    /// A cargo exchange, an intel buy, or a lot sale may finish.
    /// Both sides must have granted <see cref="FactionGrants.Trade"/>, unless they are the same faction.
    /// </summary>
    public static bool CanTrade(Game game, int viewerFactionId, int otherFactionId)
    {
        if (viewerFactionId == Game.NeutralFactionId || otherFactionId == Game.NeutralFactionId)
            return false;
        if (viewerFactionId == otherFactionId)
            return true;
        return Granted(game, viewerFactionId, otherFactionId, FactionGrants.Trade)
            && Granted(game, otherFactionId, viewerFactionId, FactionGrants.Trade);
    }

    /// <summary>Lookup form used by tests. Reads grants, not stances.</summary>
    public static bool CanTrade(FactionInfoDB viewer, int otherFactionId, FactionInfoDB other, int viewerFactionId)
    {
        if (viewerFactionId == Game.NeutralFactionId || otherFactionId == Game.NeutralFactionId)
            return false;
        if (viewer == null || other == null)
            return false;
        if (viewerFactionId == otherFactionId)
            return true;
        if (viewer.Grants == null || other.Grants == null)
            return false;
        return viewer.Grants.TryGetValue(otherFactionId, out var give) && give.HasFlag(FactionGrants.Trade)
            && other.Grants.TryGetValue(viewerFactionId, out var back) && back.HasFlag(FactionGrants.Trade);
    }

    /// <summary>
    /// Placement seed. Friendly or Allied writes Trade and Ship positions.
    /// Other stances write nothing, so a missing key stays "no grants".
    /// </summary>
    public static void SeedFromStance(FactionInfoDB granter, int receiverId, FactionStance stance)
    {
        if (granter == null)
            return;
        granter.Grants ??= new();
        if (stance is FactionStance.Friendly or FactionStance.Allied)
            granter.Grants[receiverId] = OpenDefaults;
    }

    /// <summary>
    /// Old saves stored friendship only as a stance. Write the open grants once per direction
    /// that has no grant entry yet. An entry that is present, even <see cref="FactionGrants.None"/>, stays.
    /// </summary>
    public static void MigrateGrants(FactionInfoDB info)
    {
        if (info == null)
            return;
        info.Grants ??= new();
        if (info.Stances == null)
            return;
        foreach (var (id, stance) in info.Stances)
        {
            if (info.Grants.ContainsKey(id))
                continue;
            if (stance is FactionStance.Friendly or FactionStance.Allied)
                info.Grants[id] = OpenDefaults;
        }
    }

    public static void MigrateGrants(Game game)
    {
        foreach (var faction in game.Factions.Values)
        {
            if (faction.TryGetDataBlob<FactionInfoDB>(out var info))
                MigrateGrants(info);
        }
    }

    public static bool SetGrant(FactionInfoDB info, int receiverId, FactionGrants flag, bool enabled)
    {
        if (info == null || flag == FactionGrants.None || (flag & (flag - 1)) != 0)
            return false;
        info.Grants ??= new();
        var grants = info.Grants.TryGetValue(receiverId, out var existing) ? existing : FactionGrants.None;
        grants = enabled ? grants | flag : grants & ~flag;
        info.Grants[receiverId] = grants;
        return true;
    }
}
