using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;

namespace Pulsar4X.GeoSurveys;

public class ServeyBodyPlanner : IGoalPlanner
{
    public GoalType Type => GoalType.ServeyBodies;

    /// <summary>0.2 AU. Asteroids this close to the one the fleet was sent to.</summary>
    internal const double AsteroidSurveyRadius_m = 0.2 * SbdbSmallBodyImporter.AuInKm * 1000.0;

    /// <summary>0.5 AU. The same neighborhood when the fleet has a tanker that can move.</summary>
    internal const double AsteroidSurveyRadiusWithTanker_m = 0.5 * SbdbSmallBodyImporter.AuInKm * 1000.0;

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
                 !targetEntity.TryGetDataBlob<GeoSurveyableDB>(out var surveyable))
        {
            plan = PlanResult.Fail("Not a valid target");
        }
        else if (surveyable.IsSurveyComplete(ship.FactionOwnerID))
        {
            plan = PlanResult.Done();
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
    /// A planet or moon: the targeted body first, then moons inner-to-outer.
    /// An asteroid: that rock, then other asteroids within
    /// <see cref="AsteroidSurveyRadius_m"/> (or <see cref="AsteroidSurveyRadiusWithTanker_m"/>
    /// when a tanker can move), nearest first. Command span does not clip that sphere.
    /// Closest free ship takes the current POI.
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

        TryGetFleetTanker(fleetDB, out var tanker);
        var pointsOfInterest = IsAsteroid(targetEntity)
            ? CollectAsteroidNeighborhood(targetEntity, AsteroidRadiusFor(tanker), fleet.FactionOwnerID)
            : CollectSurveyPois(targetEntity, CommandSpan.Of(fleet), fleet.FactionOwnerID);

        var claimedPoiIds = new HashSet<int>();
        var freeShips = new List<Entity>();
        bool tankerInFlight = false;

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

        // The sun is only the root of a system-wide order. Do not park the tanker on it.
        if (tanker != null
            && !IsStar(targetEntity)
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

    static bool IsAsteroid(Entity entity)
    {
        return entity.TryGetDataBlob<SystemBodyInfoDB>(out var info)
               && info.BodyType == BodyType.Asteroid;
    }

    static bool IsStar(Entity entity) => entity.HasDataBlob<StarInfoDB>();

    static double AsteroidRadiusFor(Entity? tanker)
    {
        if (tanker != null && MovePlanner.CanMove(tanker, out _))
            return AsteroidSurveyRadiusWithTanker_m;
        return AsteroidSurveyRadius_m;
    }

    /// <summary>
    /// The anchor first, then other unfinished asteroids in its system whose
    /// current distance from the anchor is within <paramref name="radius_m"/>.
    /// </summary>
    static List<Entity> CollectAsteroidNeighborhood(Entity anchor, double radius_m, int factionId)
    {
        var found = new List<(Entity body, double distance_m)>();
        if (anchor.Manager == null || !anchor.TryGetDataBlob<PositionDB>(out var anchorPos))
        {
            if (CanScan(anchor, factionId))
                return new List<Entity> { anchor };
            return new List<Entity>();
        }

        foreach (var entity in anchor.Manager.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>())
        {
            if (!IsAsteroid(entity) || !CanScan(entity, factionId))
                continue;
            if (!entity.TryGetDataBlob<PositionDB>(out var pos))
                continue;

            double distance = entity.Id == anchor.Id ? -1 : pos.GetDistanceTo_m(anchorPos);
            if (distance > radius_m)
                continue;
            found.Add((entity, distance));
        }

        found.Sort((a, b) => a.distance_m.CompareTo(b.distance_m));
        var ordered = new List<Entity>(found.Count);
        foreach (var item in found)
            ordered.Add(item.body);
        return ordered;
    }

    /// <summary>
    /// Target body first if still surveyable, then descendant extras allowed by
    /// command span (Well: direct moons inner-to-outer; System: all descendants).
    /// Parents and siblings of the target are never included.
    /// </summary>
    static List<Entity> CollectSurveyPois(Entity targetEntity, CommandSpanKind span, int factionId)
    {
        var expanded = CommandSpan.Expand(targetEntity, span, e => CanScan(e, factionId));
        var extras = new List<Entity>();
        Entity target = null!;
        bool hasTarget = false;
        foreach (var e in expanded)
        {
            if (e.Id == targetEntity.Id)
            {
                target = e;
                hasTarget = true;
            }
            else
            {
                extras.Add(e);
            }
        }

        if (extras.Count > 1)
        {
            extras.Sort((a, b) => SemiMajorOrDistance_m(a, targetEntity)
                .CompareTo(SemiMajorOrDistance_m(b, targetEntity)));
        }

        var pointsOfInterest = new List<Entity>(extras.Count + (hasTarget ? 1 : 0));
        if (hasTarget)
            pointsOfInterest.Add(target);
        foreach (var extra in extras)
            pointsOfInterest.Add(extra);
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
        if (IsStar(targetEntity))
            return false;
        if (targetEntity.TryGetDataBlob<GeoSurveyableDB>(out var surveyable))
        {
            if (!surveyable.IsSurveyComplete(factionID))
                return true;
        }
        return false;
    }
}
