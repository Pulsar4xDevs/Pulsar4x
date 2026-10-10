using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// A ship buys its newton fuel from Ceres Depot at the listed ask.
/// The standing fleet job hands this. The ship does not assign it.
/// One purchase, capped by cash and free tank space. <see cref="GoalType.RefuelAt"/> stays free.
/// </summary>
public class BuyFuelPlan : IGoalPlanner
{
    public GoalType Type => GoalType.BuyFuel;

    public readonly struct FuelGate
    {
        public bool Postpone { get; init; }
        public Entity? Ship { get; init; }
        public Goal? Child { get; init; }
    }

    /// <summary>
    /// Low flagship: a fuel child, or <see cref="FuelGate.Postpone"/> when no purchase is possible.
    /// A buy that just finished does not block the rock or the mine it postponed.
    /// A ship already working this goal is left alone.
    /// </summary>
    public static FuelGate Consider(Entity fleet, Goal standing)
    {
        if (!TryFlagship(fleet, out var ship))
            return default;
        if (JustBought(ship, standing))
            return default;
        if (FleetChildDuty.WorkingThisGoal(ship, standing) || FleetChildDuty.BusyWithOwnWork(ship, standing))
            return default;

        var snap = FuelSituation.Observe(ship);
        if (!snap.IsLow)
            return default;
        if (!TryQuote(ship, -1, out var market, out var cargoId, out var units))
            return new FuelGate { Postpone = true };

        return new FuelGate
        {
            Ship = ship,
            Child = new Goal(GoalType.BuyFuel)
            {
                ParentGoalId = standing.Id,
                TargetEntityID = market.Id,
                CargoId = cargoId,
                UnitShare = units,
            },
        };
    }

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (!managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanResult.Fail("ship only");

        if (managedEntity.TryGetDataBlob<ActionQueueDB>(out var queue))
        {
            var actions = queue.ActionsFor(goal);
            if (actions.Exists(a => a.Status == ActionStatus.Failed))
                return PlanResult.Fail("a fuel buy failed");
            if (actions.Exists(a => a.Status != ActionStatus.Succeeded))
                return PlanResult.Continue(new List<EntityAction>());
        }

        if (!TryQuote(managedEntity, goal.TargetEntityID, out var market, out var cargoId, out var units))
        {
            // The purchase was already handed off. This wake is the one after it finished.
            if (goal.Status == GoalStatus.Active && goal.DestEntityId > 0)
                return PlanResult.Done("fuel bought");
            return PlanResult.Fail("nothing to buy");
        }

        // DestEntityId marks the market this goal has already bought from, so the next wake stops.
        if (goal.Status == GoalStatus.Active && goal.DestEntityId == market.Id)
            return PlanResult.Done("fuel bought");

        if (!MarketExchangeAction.InRange(managedEntity, market))
        {
            if (!TryMoveIntoRange(managedEntity, market, out var moves, out var reason))
                return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "Out of range" : reason);
            return PlanResult.Continue(moves);
        }

        goal.DestEntityId = market.Id;
        var action = MarketExchangeAction.Create(
            managedEntity, market.Id, cargoId, MarketSide.BuyFromMarket, units);
        return PlanResult.Continue(new List<EntityAction> { action }, $"Buying {units} {cargoId}");
    }

    /// <summary>
    /// Spawn orbit counts as already at the body, and it sits just outside cargo range.
    /// Low orbit is inside that range, so drop there when movement has nothing to fly.
    /// </summary>
    static bool TryMoveIntoRange(Entity ship, Entity market, out List<EntityAction> moves, out string reason)
    {
        if (!MovePlanner.TryBuildMoveActions(ship, market, out moves, out reason))
            return false;
        if (moves.Count > 0)
            return true;

        var body = market;
        if (market.TryGetDataBlob<ColonyInfoDB>(out var colony))
            body = colony.PlanetEntity;
        if (ship.GetSOIParentEntity() != body)
        {
            reason = string.IsNullOrEmpty(reason) ? "Out of range" : reason;
            return false;
        }

        double parking = OrbitMath.LowOrbitRadius(body);
        if (!ChangeOrbitalAltitudeAction.TryPlanBurns(ship, parking, ship.StarSysDateTime, out var burns, out var burnReason)
            || burns.Count == 0)
        {
            reason = string.IsNullOrEmpty(burnReason) ? "Out of range" : burnReason;
            return false;
        }

        moves = new List<EntityAction>
        {
            ChangeOrbitalAltitudeAction.CreateCommand(ship, parking, ship.StarSysDateTime),
        };
        return true;
    }

    static bool JustBought(Entity ship, Goal standing)
    {
        if (!ship.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return false;
        var active = goals.ActiveGoal;
        return active.Type == GoalType.BuyFuel
            && active.ParentGoalId == standing.Id
            && active.Status == GoalStatus.Completed;
    }

    static bool TryFlagship(Entity fleet, out Entity ship)
    {
        ship = null!;
        if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB) || fleet.Manager == null || fleetDB.FlagShipID < 0)
            return false;
        return fleet.Manager.TryGetEntityById(fleetDB.FlagShipID, out ship)
            && ship.HasDataBlob<ShipInfoDB>();
    }

    /// <summary>
    /// Units are the minimum of free tank space, the depot's sell quantity, and what the purse can pay.
    /// </summary>
    static bool TryQuote(Entity ship, int marketId, out Entity market, out string cargoId, out long units)
    {
        market = null!;
        cargoId = "";
        units = 0;

        if (!ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) || string.IsNullOrEmpty(thrust.FuelType))
            return false;
        if (!FuelSituation.TryGetFuelMass(ship, out var fuel, out _, out _))
            return false;
        if (ship.Manager == null)
            return false;

        cargoId = thrust.FuelType;
        if (marketId > 0)
        {
            if (!ship.Manager.TryGetGlobalEntityById(marketId, out market))
                return false;
        }
        else
        {
            var found = CeresOffice.Find(ship.Manager);
            if (found == null)
                return false;
            market = found;
        }

        if (!MarketBook.TryGet(market, cargoId, out var listing) || listing.Ask <= 0 || listing.SellQuantity <= 0)
            return false;

        var game = ship.Manager.Game;
        if (!game.Factions.TryGetValue(market.FactionOwnerID, out var seller))
            return false;
        if (!seller.TryGetDataBlob<FactionInfoDB>(out var sellerInfo))
            return false;
        var marketGood = sellerInfo.Data.CargoGoods.GetAny(cargoId);
        if (marketGood == null)
            return false;
        if (!game.Factions.TryGetValue(ship.FactionOwnerID, out var buyer))
            return false;
        if (!buyer.TryGetDataBlob<FactionInfoDB>(out var buyerInfo))
            return false;

        long space = 0;
        if (ship.TryGetDataBlob<CargoStorageDB>(out var store))
            space = store.GetFreeUnitSpace(fuel);
        long sellable = MarketBook.Sellable(market, listing, marketGood);
        long affordable = Affordable(buyerInfo.Money.GetCurrentFunds(), listing.Ask);
        units = Min(space, listing.SellQuantity, sellable, affordable);
        return units > 0;
    }

    static long Affordable(decimal funds, decimal price)
    {
        if (price <= 0 || funds <= 0)
            return 0;
        decimal raw = Math.Floor(funds / price);
        if (raw >= long.MaxValue)
            return long.MaxValue;
        return (long)raw;
    }

    static long Min(long a, long b, long c, long d)
    {
        long m = a < b ? a : b;
        if (c < m) m = c;
        if (d < m) m = d;
        return m < 0 ? 0 : m;
    }
}
