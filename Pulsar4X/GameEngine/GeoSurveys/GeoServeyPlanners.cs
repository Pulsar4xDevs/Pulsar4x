using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;

namespace Pulsar4X.GeoSurveys;

public class ServeyBodyPlanner : IGoalPlanner
{
    public GoalType Type => GoalType.ServeyBodies;

    public PlanResult Plan(Entity managedEntity, Goal goal)
    {
        if (managedEntity.HasDataBlob<FleetDB>())
            return PlanSubGoals(managedEntity, goal);
        if (managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanActions(managedEntity, goal);

        return PlanResult.Fail("Non supported entity");
    }

    public PlanResult PlanActions(Entity ship, Goal goal)
    {
        PlanResult plan = PlanResult.Fail("unknown fail");

        if (!ship.HasOrChildHasAbility<GeoSurveyAbilityDB>())
        {
            plan = PlanResult.Fail("no geo-survey capability");
        }
        else if (!ship.TryGetDataBlob<ActionQueueDB>(out var actionQueue))
        {
            plan = PlanResult.Fail("no action queue");
        }
        else if (!ship.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out var targetEntity) ||
                 !targetEntity.TryGetDataBlob<GeoSurveyableDB>(out _))
        {
            plan = PlanResult.Fail("Not a valid target");
        }
        else
        {
            var actionsForGoal = actionQueue.ActionsFor(goal);

            if (actionsForGoal.Count > 0)
            {
                if (actionsForGoal.Exists(a => a.Status == ActionStatus.Failed))
                    plan = PlanResult.Fail("a survey action failed");
                else if (actionsForGoal.TrueForAll(a => a.Status == ActionStatus.Succeeded))
                    plan = PlanResult.Done();
                else
                    plan = PlanResult.Continue(new List<EntityAction>()); // still running
            }
            else if (MovePlanner.TryBuildMoveActions(
                         ship,
                         goal.TargetEntityID,
                         out var moveActions,
                         out var moveReason))
            {
                moveActions.Add(new GeoSurveyOrder(ship, targetEntity));
                plan = PlanResult.Continue(moveActions);
            }
            else
            {
                plan = PlanResult.Fail(moveReason);
            }
        }

        return plan;
    }

    /// <summary>
    /// Hand each capable free ship a different unfinished surveyable body (nearest first).
    /// Includes the target if surveyable, plus <see cref="PositionDB.Children"/> (e.g. Earth + Luna).
    /// Does not mutate <paramref name="goal"/> — agent applies the returned status.
    /// Re-entrant: skips ships already working this parent goal; skips POIs already assigned.
    /// </summary>
    public PlanResult PlanSubGoals(Entity fleet, Goal goal)
    {
        if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB))
            return PlanResult.Fail("We have no subordinates to manage");

        if (!fleet.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out var targetEntity))
            return PlanResult.Fail("invalid target");

        // POIs: target itself if surveyable, plus direct children (moons, etc.).
        var pointsOfInterest = new List<Entity>();
        if (CanScan(targetEntity, fleet.FactionOwnerID))
            pointsOfInterest.Add(targetEntity);

        if (targetEntity.TryGetDataBlob<PositionDB>(out var position))
        {
            foreach (var childEntity in position.Children)
            {
                if (CanScan(childEntity, fleet.FactionOwnerID))
                    pointsOfInterest.Add(childEntity);
            }
        }

        if (pointsOfInterest.Count == 0)
            return PlanResult.Done("nothing left to survey");

        var claimedPoiIds = new HashSet<int>();
        var freeShips = new List<Entity>();

        foreach (var subunit in fleetDB.Children)
        {
            if (!subunit.HasOrChildHasAbility<GeoSurveyAbilityDB>())
                continue;
            if (!MovePlanner.CanMove(subunit, out _))
                continue;

            if (subunit.TryGetDataBlob<GoalsDB>(out var childGoals)
                && childGoals.ActiveGoal != null
                && childGoals.ActiveGoal.ParentGoalId == goal.Id
                && childGoals.ActiveGoal.Status is not (GoalStatus.Completed or GoalStatus.Failed))
            {
                claimedPoiIds.Add(childGoals.ActiveGoal.TargetEntityID);
                continue;
            }

            freeShips.Add(subunit);
        }

        var remaining = new List<Entity>();
        foreach (var poi in pointsOfInterest)
        {
            if (!claimedPoiIds.Contains(poi.Id))
                remaining.Add(poi);
        }

        if (remaining.Count == 0)
            return PlanResult.Done("all bodies already assigned or complete");

        if (freeShips.Count == 0)
            return PlanResult.Continue(new List<(Entity subordinate, Goal goal)>());

        var subGoals = new List<(Entity subordinate, Goal goal)>();

        foreach (var ship in freeShips)
        {
            if (remaining.Count == 0)
                break;

            Entity best = remaining[0];
            double bestDist = double.MaxValue;
            foreach (var poi in remaining)
            {
                double d = ship.GetDataBlob<PositionDB>()
                    .GetDistanceTo_m(poi.GetDataBlob<PositionDB>());
                if (d < bestDist)
                {
                    bestDist = d;
                    best = poi;
                }
            }

            remaining.Remove(best);
            subGoals.Add((ship, new Goal(GoalType.ServeyBodies)
            {
                ParentGoalId = goal.Id,
                TargetEntityID = best.Id,
            }));
        }

        return PlanResult.Continue(subGoals);
    }

    bool CanScan(Entity targetEntity, int factionID)
    {
        if (targetEntity.TryGetDataBlob<GeoSurveyableDB>(out var surveyable))
        {
            if (!surveyable.IsSurveyComplete(factionID))
                return true;
        }
        return false;
    }
}
