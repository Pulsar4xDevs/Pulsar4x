using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;

namespace Pulsar4X.GeoSurveys;

public class ServeyBodyPlanner : IGoalPlanner
{
    public GoalType Type => GoalType.ServeyBodies;

    public IEnumerable<EntityAction> PlanActions(Goal goal, Entity ship)
    {
        List<EntityAction> actions = new List<EntityAction>();

        if (!ship.HasOrChildHasAbility<GeoSurveyAbilityDB>())
        {
            goal.Status = GoalStatus.Failed;
            goal.Message = "No geo-survey capability";
        }
        else if (!ship.TryGetDataBlob<ActionQueueDB>(out var actionQueue))
        {
            //need to check, it might be safe to just give a ship an action queue if it doesn't have one.
            goal.Status = GoalStatus.Failed;
            goal.Message = "No action queue";
        }
        else if (!ship.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out var targetEntity) ||
                 !targetEntity.TryGetDataBlob<GeoSurveyableDB>(out var serveyable))
        {
            goal.Status = GoalStatus.Failed;
            goal.Message = "Not a valid target";
        }
        else
        {
            var actionsForGoal = actionQueue.ActionsFor(goal);

            if (actionsForGoal.Count > 0)
            {
                if (actionsForGoal.TrueForAll(x => x.Status == ActionStatus.Succeeded))
                    goal.Status = GoalStatus.Completed;
            }
            else if (MovePlanner.TryBuildMoveActions(
                         ship,
                         goal.TargetEntityID,
                         out var moveActions,
                         out var moveReason))
            {
                actions.AddRange(moveActions);
                actions.Add(new GeoSurveyOrder(ship, targetEntity));
            }
            else
            {
                goal.Status = GoalStatus.Failed;
                goal.Message = moveReason;
            }
        }

        return actions;
    }
    

    /// <summary>
    /// TODO: should scan everything within the target's SOI — target the sun and we survey the whole
    /// system, target earth and we survey earth and luna — by walking the target's PositionDB
    /// children for anything with a GeoSurveyableDB the faction hasn't finished, then giving each
    /// subunit a *different* body (nearest first) and topping up as ships come free.
    ///
    /// Two things are missing before that can work: this only gets one pass (AgentProcessor plans on
    /// Pending and monitors thereafter), so there is nowhere to hand out the next body from; 
    /// </summary>
    public IEnumerable<(Entity subordinate, Goal goal)> PlanSubGoals(Goal goal, Entity fleet)
    {
        if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB))
        {
            goal.Status = GoalStatus.Failed;
            goal.Message = "We have no subordinates to manage";
            yield break;
        }
        
        if (!fleet.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out var targetEntity))
        {
            goal.Status = GoalStatus.Failed;
            goal.Message = "invalid target";
            yield break;
        }
        
        List<Entity> pointsOfInterest = new List<Entity>();
        if(CanScan(targetEntity, fleet.FactionOwnerID))
            pointsOfInterest.Add(targetEntity);
        targetEntity.TryGetDataBlob<PositionDB>(out var position);
        foreach (var childEntity in position.Children)
        {
            if (CanScan(childEntity, fleet.FactionOwnerID))
                pointsOfInterest.Add(childEntity);
        }
        
        List<Entity> fleetChildren = new List<Entity>();
        foreach (var subunit in fleetDB.Children)
        {
            // 1. Can it survey?
            if (!subunit.HasOrChildHasAbility<GeoSurveyAbilityDB>())
                continue;

            // 2. Can it physically get there? (cheap gate; strengthen later if needed)
            if (!MovePlanner.CanMove(subunit, out _))
                continue;
            if (subunit.TryGetDataBlob<GoalsDB>(out var goalsDB))
            {
                fleetChildren.Add(subunit);
            }
            else
            {
                fleetChildren.Add(subunit);
            }
            
        }
        
        if (fleetChildren.Count == 0 || pointsOfInterest.Count == 0)
        {
            goal.Status = GoalStatus.Failed;
            goal.Message = "no capable subordinates or nothing left to survey";
            yield break;
        }

        var remaining = new List<Entity>(pointsOfInterest);

        foreach (var ship in fleetChildren)
        {
            if(ship.TryGetDataBlob<GoalsDB>(out var goalsDB) 
               && (goalsDB.GivenGoal.ParentGoalId == goal.Id
               && goalsDB.GivenGoal.Status  != GoalStatus.Completed 
               || goalsDB.GivenGoal.Status != GoalStatus.Failed)
               )
            {
                continue;//this ship is already doing this goal. 
            }
            if (remaining.Count == 0)
                yield break;

            Entity best = remaining[0];
            double bestDist = double.MaxValue;
            foreach (var poi in remaining)
            {
                double d = ship.GetDataBlob<PositionDB>().GetDistanceTo_m(poi.GetDataBlob<PositionDB>());
                if (d < bestDist)
                {
                    bestDist = d;
                    best = poi;
                }
            }

            remaining.Remove(best);

            yield return (ship, new Goal(GoalType.ServeyBodies)
            {
                ParentGoalId = goal.Id,
                TargetEntityID = best.Id,   // distinct body, not parent system id
            });

        }
    }

    bool CanScan(Entity targetEntity, int factionID)
    {
        if (targetEntity.TryGetDataBlob<GeoSurveyableDB>(out var serveyTarget))
        {
            if (!serveyTarget.IsSurveyComplete(factionID))
            {
                return true;
            }
        }
        return false;
    }
}