using Pulsar4X.Api;
using Pulsar4X.Engine;

namespace Pulsar4X.Factions;

/// <summary>
/// Trade permission from stored <see cref="FactionStance"/> values.
/// Same faction may trade with itself. <see cref="Game.NeutralFactionId"/> is never a partner.
/// Two different factions may trade only when each has stored Friendly or Allied toward the other.
/// </summary>
public static class FactionStanceRules
{
    public static bool CanTrade(FactionInfoDB viewer, int otherFactionId, FactionInfoDB other, int viewerFactionId)
    {
        if (viewerFactionId == Game.NeutralFactionId || otherFactionId == Game.NeutralFactionId)
            return false;
        if (viewer == null || other == null)
            return false;
        if (viewerFactionId == otherFactionId)
            return true;

        return IsOpen(viewer, otherFactionId) && IsOpen(other, viewerFactionId);
    }

    /// <summary>Lookup wrapper. A missing faction or info blob cannot trade.</summary>
    public static bool CanTrade(Game game, int viewerFactionId, int otherFactionId)
    {
        if (viewerFactionId == Game.NeutralFactionId || otherFactionId == Game.NeutralFactionId)
            return false;
        if (viewerFactionId == otherFactionId)
            return true;
        if (!game.Factions.TryGetValue(viewerFactionId, out var viewerEntity)
            || !game.Factions.TryGetValue(otherFactionId, out var otherEntity))
            return false;
        if (!viewerEntity.TryGetDataBlob<FactionInfoDB>(out var viewer)
            || !otherEntity.TryGetDataBlob<FactionInfoDB>(out var other))
            return false;

        return CanTrade(viewer, otherFactionId, other, viewerFactionId);
    }

    static bool IsOpen(FactionInfoDB info, int towardFactionId)
        => info.Stances.TryGetValue(towardFactionId, out var stance)
           && stance is FactionStance.Friendly or FactionStance.Allied;
}
