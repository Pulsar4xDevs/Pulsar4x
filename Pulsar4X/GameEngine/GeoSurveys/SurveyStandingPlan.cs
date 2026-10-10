using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Api;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Movement;

namespace Pulsar4X.GeoSurveys;

/// <summary>
/// Survey Flight keeps this goal. It hands one ship one unscanned asteroid in the
/// 0.2 AU neighborhood, and lists the chart when that child finishes.
/// A ship does not run this planner to give itself the next job.
/// </summary>
public class SurveyStandingPlan : IGoalPlanner
{
    public GoalType Type => GoalType.SurveyStanding;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (!managedEntity.TryGetDataBlob<FleetDB>(out var fleetDB))
            return PlanResult.Fail("fleet only");

        ListFinishedChart(managedEntity, fleetDB, goal);

        Entity? ship = null;
        foreach (var subunit in fleetDB.Children)
        {
            if (!subunit.HasOrChildHasAbility<GeoSurveyAbilityDB>())
                continue;
            if (!MovePlanner.CanMove(subunit, out _))
                continue;
            if (FleetChildDuty.WorkingThisGoal(subunit, goal))
                continue;
            if (FleetChildDuty.BusyWithOwnWork(subunit, goal))
                continue;
            ship = subunit;
            break;
        }

        if (ship == null)
            return Idle();

        var rock = NearestUnscanned(ship);
        if (rock == null)
            return Idle();

        return PlanResult.Continue(new List<(Entity subordinate, Goal goal)>
        {
            (ship, new Goal(GoalType.ServeyBodies)
            {
                ParentGoalId = goal.Id,
                TargetEntityID = rock.Id,
            }),
        });
    }

    /// <summary>The child that just finished, if Strata surveyed that body and the depot has no row yet.</summary>
    static void ListFinishedChart(Entity fleet, FleetDB fleetDB, Goal goal)
    {
        var depot = CeresOffice.Find(fleet.Manager);
        var game = fleet.Manager?.Game;
        if (depot == null || game == null || !game.Factions.TryGetValue(fleet.FactionOwnerID, out var seller))
            return;

        foreach (var subunit in fleetDB.Children)
        {
            if (!subunit.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
                continue;
            var child = goals.ActiveGoal;
            if (child.ParentGoalId != goal.Id || child.Type != GoalType.ServeyBodies)
                continue;
            if (child.Status != GoalStatus.Completed)
                continue;
            if (subunit.Manager == null || !subunit.Manager.TryGetGlobalEntityById(child.TargetEntityID, out var body))
                continue;
            if (!body.TryGetDataBlob<GeoSurveyableDB>(out var geo) || !geo.IsSurveyComplete(seller.Id))
                continue;

            string subject = IntelBook.SubjectOf(body.Id);
            if (IntelBook.TryGet(depot, IntelKind.Geo, subject, out _))
                continue;
            IntelBook.TryConsign(depot, seller, IntelKind.Geo, subject, CeresStart.ChartAsk, out _);
        }
    }

    static Entity? NearestUnscanned(Entity ship)
    {
        if (ship.Manager == null || !ship.TryGetDataBlob<PositionDB>(out var origin))
            return null;

        Entity? nearest = null;
        double best = double.MaxValue;
        int bestId = int.MaxValue;
        foreach (var body in ship.Manager.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>())
        {
            if (!body.TryGetDataBlob<SystemBodyInfoDB>(out var info) || info.BodyType != BodyType.Asteroid)
                continue;
            if (!body.TryGetDataBlob<GeoSurveyableDB>(out var geo) || geo.IsSurveyComplete(ship.FactionOwnerID))
                continue;
            if (!body.TryGetDataBlob<PositionDB>(out var pos))
                continue;
            double distance = pos.GetDistanceTo_m(origin);
            if (distance > ServeyBodyPlanner.AsteroidSurveyRadius_m)
                continue;
            if (distance < best || (distance == best && body.Id < bestId))
            {
                best = distance;
                bestId = body.Id;
                nearest = body;
            }
        }
        return nearest;
    }

    static PlanResult Idle()
        => PlanResult.Continue(new List<(Entity subordinate, Goal goal)>());
}
