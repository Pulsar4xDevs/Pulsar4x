using System;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One ship, one good, one buy market and one sell market in a known system.
/// The route is returned on <see cref="PlanResult"/>; the agent stores it on the goal.
/// </summary>
public class TradePlan : IGoalPlanner
{
    /// <summary>Hours charged when the ship cannot warp. Large enough that no bid clears it.</summary>
    const double NoWarpHours = 1e12;

    public GoalType Type => GoalType.Trade;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (managedEntity.HasDataBlob<FleetDB>())
            return PlanResult.Fail("Trade is one ship");
        if (!managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanResult.Fail("Non supported entity");

        if (managedEntity.TryGetDataBlob<GoalsDB>(out var goals)
            && GoalWeighting.ShouldInterruptForRefuel(goals, goal))
            return PlanResult.Fail("low on fuel");

        if (MarketRun.HasRoute(goal))
            return FollowRoute(managedEntity, goal);

        if (!TryChooseRoute(managedEntity, out var route, out var source, out var dest, out var good))
            return PlanResult.Fail("no route");

        var leg = MarketRun.NextLeg(managedEntity, source, dest, route.CargoId, good);
        if (leg.Status != GoalStatus.Active)
            return leg;
        return leg.WithRoute(route);
    }

    static PlanResult FollowRoute(Entity ship, Goal goal)
    {
        bool handed = !string.IsNullOrEmpty(goal.ParentGoalId);
        if (!MarketRun.TryMarket(ship, goal.SourceEntityId, out var source)
            || !MarketRun.TryMarket(ship, goal.DestEntityId, out var dest))
            return handed ? PlanResult.Done("share closed") : PlanResult.Fail("listing gone");

        if (!FactionStanceRules.CanTrade(ship.Manager.Game, ship.FactionOwnerID, source.FactionOwnerID)
            || !FactionStanceRules.CanTrade(ship.Manager.Game, ship.FactionOwnerID, dest.FactionOwnerID))
            return PlanResult.Fail("Cannot trade");

        if (!MarketRun.TryShipGood(ship, goal.CargoId, out var good))
            return handed ? PlanResult.Done("share closed") : PlanResult.Fail("listing gone");

        return MarketRun.HandedLeg(ship, goal, source, dest, goal.CargoId, good, capBuyAtRequest: false);
    }

    static bool TryChooseRoute(Entity ship, out PlannedRoute route, out Entity source, out Entity dest, out ICargoable good)
    {
        route = null!;
        source = null!;
        dest = null!;
        good = null!;

        double bestScore = 0;
        var game = ship.Manager.Game;
        var markets = MarketRun.Markets(ship)
            .Where(market => FactionStanceRules.CanTrade(game, ship.FactionOwnerID, market.FactionOwnerID))
            .ToList();

        foreach (var from in markets)
        {
            if (!from.TryGetDataBlob<LogiBaseDB>(out var fromBook))
                continue;
            foreach (var sell in fromBook.Listings.Values.OrderBy(listing => listing.CargoId, StringComparer.Ordinal))
            {
                if (sell.SellQuantity <= 0 || string.IsNullOrEmpty(sell.CargoId))
                    continue;
                if (!MarketRun.TryShipGood(ship, sell.CargoId, out var cargo))
                    continue;

                foreach (var to in markets)
                {
                    if (to.Id == from.Id)
                        continue;
                    if (!MarketBook.TryGet(to, sell.CargoId, out var buy) || buy.BuyQuantity <= 0)
                        continue;

                    if (!TryHours(ship, from, to, out var hours))
                        continue;

                    double score = (double)(buy.Bid - sell.Ask) - hours;
                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    route = new PlannedRoute
                    {
                        CargoId = sell.CargoId,
                        SourceEntityId = from.Id,
                        DestEntityId = to.Id,
                    };
                    source = from;
                    dest = to;
                    good = cargo;
                }
            }
        }

        return bestScore > 0;
    }

    /// <summary>
    /// Same-system hours stay the straight-line warp time.
    /// A market the ship cannot reach is skipped, even when both ends share that system.
    /// Another system adds the known-jump path between the two markets.
    /// </summary>
    static bool TryHours(Entity ship, Entity source, Entity dest, out double hours)
    {
        if (!JumpRoute.CanReach(ship, source) || !JumpRoute.CanReach(ship, dest))
        {
            hours = 0;
            return false;
        }

        if (source.Manager == dest.Manager)
        {
            hours = Hours(ship, source, dest);
            return true;
        }

        return JumpRoute.TryTravelHours(ship, source, dest, out hours);
    }

    /// <summary>Travel time in hours at warp max speed. No warp drive prices the trip out of every route.</summary>
    static double Hours(Entity ship, Entity source, Entity dest)
    {
        if (!ship.TryGetDataBlob<WarpAbilityDB>(out var warp) || warp.MaxSpeed <= 0)
            return NoWarpHours;
        if (!source.TryGetDataBlob<PositionDB>(out var from) || !dest.TryGetDataBlob<PositionDB>(out var to))
            return NoWarpHours;

        double meters = from.GetDistanceTo_m(to);
        if (!double.IsFinite(meters) || meters < 0)
            return NoWarpHours;
        return meters / warp.MaxSpeed / 3600.0;
    }
}
