using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Logistics;
using Pulsar4X.Movement;

namespace Pulsar4X.Industry;

/// <summary>
/// Mining Flight keeps this goal. A free miner buys one chart it can afford and does
/// not already have, then takes the existing mine-and-sell job. Nothing left to buy
/// or dig stays active for a later wake. The ship does not re-issue this goal.
/// </summary>
public class MineStandingPlan : IGoalPlanner
{
    public GoalType Type => GoalType.MineStanding;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (!managedEntity.TryGetDataBlob<FleetDB>(out var fleetDB))
            return PlanResult.Fail("fleet only");

        var fuel = BuyFuelPlan.Consider(managedEntity, goal);
        if (fuel.Postpone)
            return PlanResult.Continue(new List<(Entity subordinate, Goal goal)>());
        if (fuel.Child != null && fuel.Ship != null)
        {
            return PlanResult.Continue(new List<(Entity subordinate, Goal goal)>
            {
                (fuel.Ship, fuel.Child),
            });
        }

        if (HasFreeMiner(fleetDB, goal))
            BuyOneChart(managedEntity);

        var mined = new MineAsteroidsPlan().Plan(managedEntity, goal, atDateTime);
        if (mined.Status == GoalStatus.Active)
            return mined;

        // Done and Fail from the mine planner would end the standing job.
        // An empty belt, or a ship left alone, waits for the next wake.
        return PlanResult.Continue(new List<(Entity subordinate, Goal goal)>());
    }

    static bool HasFreeMiner(FleetDB fleetDB, Goal goal)
    {
        foreach (var subunit in fleetDB.Children)
        {
            if (!subunit.HasDataBlob<AsteroidMineAbilityDB>())
                continue;
            if (!MovePlanner.CanMove(subunit, out _))
                continue;
            if (FleetChildDuty.WorkingThisGoal(subunit, goal))
                continue;
            if (FleetChildDuty.BusyWithOwnWork(subunit, goal))
                continue;
            return true;
        }
        return false;
    }

    /// <summary>One ForSale geo row on Ceres Depot. <see cref="IntelBook.TryBuy"/> pays the seller.</summary>
    static void BuyOneChart(Entity fleet)
    {
        var depot = CeresOffice.Find(fleet.Manager);
        var game = fleet.Manager?.Game;
        if (depot == null || game == null || !depot.TryGetDataBlob<LogiBaseDB>(out var book) || book.Intel == null)
            return;
        if (!game.Factions.TryGetValue(fleet.FactionOwnerID, out var buyer))
            return;

        foreach (var row in book.Intel.Values)
        {
            if (row.Kind != IntelKind.Geo || !row.ForSale)
                continue;
            if (IntelBook.BuyerHas(game, buyer.Id, row.Kind, row.Subject))
                continue;
            if (IntelBook.TryBuy(depot, buyer, row.Kind, row.Subject, out _))
                return;
        }
    }
}
