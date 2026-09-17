using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;

namespace Pulsar4X.GeoSurveys;

public class ServeyBodyPlanner : IGoalPlanner
{
    public GoalType Type => GoalType.ServeyBodies;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
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
                         targetEntity,
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
    /// Hand each capable free ship a different unfinished surveyable body.
    /// Work order is the targeted parent first, then moons inner-to-outer
    /// (SMA around the parent). Closest free ship takes the current POI.
    /// A flagged fleet tanker is sent to orbit the parent (<see cref="GoalType.MoveTo"/>).
    /// Does not mutate <paramref name="goal"/> — agent applies the returned status.
    /// Re-entrant: skips ships already working this parent goal; skips POIs already assigned.
    /// </summary>
    public PlanResult PlanSubGoals(Entity fleet, Goal goal)
    {
        if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB))
            return PlanResult.Fail("We have no subordinates to manage");

        if (!fleet.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out var targetEntity))
            return PlanResult.Fail("invalid target");

        var pointsOfInterest = CollectSurveyPois(targetEntity, fleet.FactionOwnerID);

        var claimedPoiIds = new HashSet<int>();
        var freeShips = new List<Entity>();
        bool tankerInFlight = false;

        TryGetFleetTanker(fleetDB, out var tanker);

        foreach (var subunit in fleetDB.Children)
        {
            if (tanker != null && subunit.Id == tanker.Id)
            {
                tankerInFlight = TankerAlreadyTasked(subunit, goal, targetEntity.Id)
                                 && HasOpenSubgoal(subunit, goal);
                continue;
            }

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

        var subGoals = new List<(Entity subordinate, Goal goal)>();

        foreach (var poi in remaining)
        {
            if (freeShips.Count == 0)
                break;

            Entity bestShip = freeShips[0];
            double bestDist = double.MaxValue;
            foreach (var ship in freeShips)
            {
                double d = ship.GetDataBlob<PositionDB>()
                    .GetDistanceTo_m(poi.GetDataBlob<PositionDB>());
                if (d < bestDist)
                {
                    bestDist = d;
                    bestShip = ship;
                }
            }

            freeShips.Remove(bestShip);
            subGoals.Add((bestShip, new Goal(GoalType.ServeyBodies)
            {
                ParentGoalId = goal.Id,
                TargetEntityID = poi.Id,
            }));
        }

        if (tanker != null
            && !TankerAlreadyTasked(tanker, goal, targetEntity.Id)
            && MovePlanner.CanMove(tanker, out _))
        {
            subGoals.Add((tanker, new Goal(GoalType.MoveTo)
            {
                ParentGoalId = goal.Id,
                TargetEntityID = targetEntity.Id,
            }));
        }

        if (subGoals.Count > 0)
            return PlanResult.Continue(subGoals);

        if (claimedPoiIds.Count > 0 || tankerInFlight || remaining.Count > 0)
            return PlanResult.Continue(new List<(Entity subordinate, Goal goal)>());

        return PlanResult.Done(pointsOfInterest.Count == 0
            ? "nothing left to survey"
            : "all bodies already assigned or complete");
    }

    static bool TryGetFleetTanker(FleetDB fleetDB, out Entity tanker)
    {
        tanker = null!;
        foreach (var child in fleetDB.Children)
        {
            if (!child.TryGetDataBlob<ShipInfoDB>(out var info) || !info.Tanker)
                continue;
            tanker = child;
            return true;
        }
        return false;
    }

    static bool HasOpenSubgoal(Entity unit, Goal parent)
    {
        return unit.TryGetDataBlob<GoalsDB>(out var goals)
               && goals.ActiveGoal != null
               && goals.ActiveGoal.ParentGoalId == parent.Id
               && goals.ActiveGoal.Status is not (GoalStatus.Completed or GoalStatus.Failed);
    }

    /// <summary>
    /// Already sent to the parent (in flight or arrived). Do not re-issue MoveTo.
    /// </summary>
    static bool TankerAlreadyTasked(Entity tanker, Goal parent, int parentBodyId)
    {
        if (!tanker.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return false;
        var active = goals.ActiveGoal;
        if (active.ParentGoalId != parent.Id)
            return false;
        return active.Type == GoalType.MoveTo && active.TargetEntityID == parentBodyId;
    }

    /// <summary>
    /// Target body first if still surveyable, then direct children inner-to-outer.
    /// </summary>
    static List<Entity> CollectSurveyPois(Entity targetEntity, int factionId)
    {
        var pointsOfInterest = new List<Entity>();
        if (CanScan(targetEntity, factionId))
            pointsOfInterest.Add(targetEntity);

        if (!targetEntity.TryGetDataBlob<PositionDB>(out var position))
            return pointsOfInterest;

        var moons = new List<(Entity body, double radius_m)>();
        foreach (var childEntity in position.Children)
        {
            if (!CanScan(childEntity, factionId))
                continue;
            moons.Add((childEntity, SemiMajorOrDistance_m(childEntity, targetEntity)));
        }

        moons.Sort((a, b) => a.radius_m.CompareTo(b.radius_m));
        foreach (var (body, _) in moons)
            pointsOfInterest.Add(body);

        return pointsOfInterest;
    }

    static double SemiMajorOrDistance_m(Entity body, Entity parent)
    {
        if (body.TryGetDataBlob<OrbitDB>(out var orbit) && orbit.Parent == parent)
            return orbit.SemiMajorAxis;
        if (body.TryGetDataBlob<PositionDB>(out var bodyPos)
            && parent.TryGetDataBlob<PositionDB>(out var parentPos))
            return bodyPos.GetDistanceTo_m(parentPos);
        return double.MaxValue;
    }

    static bool CanScan(Entity targetEntity, int factionID)
    {
        if (targetEntity.TryGetDataBlob<GeoSurveyableDB>(out var surveyable))
        {
            if (!surveyable.IsSurveyComplete(factionID))
                return true;
        }
        return false;
    }
}
