using System;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One ship hauls one posted good between owned colonies in systems the faction knows.
/// The route is returned on <see cref="PlanResult"/>; the agent stores it on the goal.
/// </summary>
public class FreighterPlan : IGoalPlanner
{
    public GoalType Type => GoalType.Freighter;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (managedEntity.HasDataBlob<FleetDB>())
            return PlanResult.Fail("Freighter is one ship");
        if (!managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanResult.Fail("Non supported entity");

        if (managedEntity.TryGetDataBlob<GoalsDB>(out var goals)
            && GoalWeighting.ShouldInterruptForRefuel(goals, goal))
            return PlanResult.Fail("low on fuel");

        if (MarketRun.HasRoute(goal))
            return FollowRoute(managedEntity, goal);

        if (!TryChooseRoute(managedEntity, out var route, out var source, out var dest, out var good))
            return PlanResult.Fail("no haul");

        var leg = MarketRun.NextLeg(managedEntity, source, dest, route.CargoId, good, capBuyAtRequest: true);
        if (leg.Status != GoalStatus.Active)
            return leg;
        return leg.WithRoute(route);
    }

    static PlanResult FollowRoute(Entity ship, Goal goal)
    {
        if (!MarketRun.TryMarket(ship, goal.SourceEntityId, out var source)
            || !MarketRun.TryMarket(ship, goal.DestEntityId, out var dest))
            return PlanResult.Fail("listing gone");

        if (source.FactionOwnerID != ship.FactionOwnerID || dest.FactionOwnerID != ship.FactionOwnerID)
            return PlanResult.Fail("listing gone");

        if (!MarketBook.TryGet(source, goal.CargoId, out _)
            || !MarketBook.TryGet(dest, goal.CargoId, out _))
            return PlanResult.Fail("listing gone");

        if (!MarketRun.TryShipGood(ship, goal.CargoId, out var good))
            return PlanResult.Fail("listing gone");

        return MarketRun.NextLeg(ship, source, dest, goal.CargoId, good, capBuyAtRequest: true);
    }

    static bool TryChooseRoute(Entity ship, out PlannedRoute route, out Entity source, out Entity dest, out ICargoable good)
    {
        route = null!;
        source = null!;
        dest = null!;
        good = null!;

        long bestUnits = 0;
        double bestDistance = double.MaxValue;
        var markets = MarketRun.Markets(ship)
            .Where(market => market.FactionOwnerID == ship.FactionOwnerID && market.HasDataBlob<ColonyInfoDB>())
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

                long available = Math.Min(sell.SellQuantity, MarketBook.Sellable(from, sell, cargo));
                if (available <= 0)
                    continue;

                foreach (var to in markets)
                {
                    if (to.Id == from.Id)
                        continue;
                    if (!MarketBook.TryGet(to, sell.CargoId, out var buy) || buy.BuyQuantity <= 0)
                        continue;

                    long free = Math.Max(0, ship.GetDataBlob<CargoStorageDB>().GetFreeUnitSpace(cargo));
                    long units = Math.Min(buy.BuyQuantity, Math.Min(available, free));
                    if (units <= 0)
                        continue;

                    if (!TryDistance(ship, from, to, out var distance))
                        continue;
                    if (units < bestUnits)
                        continue;
                    if (units == bestUnits && distance >= bestDistance)
                        continue;

                    bestUnits = units;
                    bestDistance = distance;
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

        return bestUnits > 0;
    }

    /// <summary>
    /// Same system keeps the straight-line distance. A colony the ship cannot reach is skipped.
    /// Another system uses the jump path plus one hour of warp per hop.
    /// </summary>
    static bool TryDistance(Entity ship, Entity from, Entity to, out double distance)
    {
        if (!JumpRoute.CanReach(ship, from) || !JumpRoute.CanReach(ship, to))
        {
            distance = double.MaxValue;
            return false;
        }

        if (from.Manager == to.Manager)
        {
            distance = MarketRun.DistanceMeters(from, to);
            return double.IsFinite(distance);
        }

        distance = double.MaxValue;
        if (!ship.TryGetDataBlob<WarpAbilityDB>(out var warp) || warp.MaxSpeed <= 0)
            return false;
        if (!JumpRoute.TryConnect(ship, from, to, out var hops, out var meters, out _))
            return false;

        distance = meters + hops * warp.MaxSpeed * 3600.0 * JumpRoute.HopHours;
        return double.IsFinite(distance);
    }
}
