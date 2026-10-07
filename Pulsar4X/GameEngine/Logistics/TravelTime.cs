using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Movement;

namespace Pulsar4X.Logistics;

/// <summary>
/// Least travel time from a ship to one entity of a chosen set.
/// Same system is straight-line warp time. Another system adds the known-jump path.
/// </summary>
public static class TravelTime
{
    /// <summary>
    /// The candidate that takes the least time to reach.
    /// Unreachable candidates are skipped. A tie goes to the lower entity id.
    /// No warp drive returns none and <paramref name="reason"/> "no warp".
    /// </summary>
    public static bool TryNearest(
        Entity ship,
        IEnumerable<Entity> candidates,
        out Entity nearest,
        out double hours,
        out string reason)
    {
        nearest = null!;
        hours = 0;
        reason = "";

        if (ship == null)
        {
            reason = "no ship";
            return false;
        }

        if (!ship.TryGetDataBlob<WarpAbilityDB>(out var warp) || warp.MaxSpeed <= 0)
        {
            reason = "no warp";
            return false;
        }

        Entity best = null!;
        double bestHours = 0;
        bool any = false;

        if (candidates != null)
        {
            foreach (var candidate in candidates)
            {
                if (candidate == null || !TryHours(ship, candidate, out var candidateHours))
                    continue;

                if (!any || candidateHours < bestHours || (candidateHours == bestHours && candidate.Id < best.Id))
                {
                    any = true;
                    best = candidate;
                    bestHours = candidateHours;
                }
            }
        }

        if (!any)
        {
            reason = "none reachable";
            return false;
        }

        nearest = best;
        hours = bestHours;
        return true;
    }

    /// <summary>
    /// Hours from <paramref name="ship"/> to <paramref name="place"/>.
    /// Same system matches <see cref="TradePlan"/> warp time. Another system uses <see cref="JumpRoute"/>.
    /// </summary>
    public static bool TryHours(Entity ship, Entity place, out double hours)
    {
        hours = 0;
        if (ship?.Manager == null || place?.Manager == null)
            return false;
        if (!ship.TryGetDataBlob<WarpAbilityDB>(out var warp) || warp.MaxSpeed <= 0)
            return false;

        if (place.Manager == ship.Manager)
        {
            if (!ship.TryGetDataBlob<PositionDB>(out var from) || !place.TryGetDataBlob<PositionDB>(out var to))
                return false;

            double meters = from.GetDistanceTo_m(to);
            if (!double.IsFinite(meters) || meters < 0)
                return false;

            hours = meters / warp.MaxSpeed / 3600.0;
            return double.IsFinite(hours) && hours >= 0;
        }

        return JumpRoute.TryTravelHours(ship, ship, place, out hours);
    }
}
