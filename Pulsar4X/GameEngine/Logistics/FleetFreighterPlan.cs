using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;

namespace Pulsar4X.Logistics;

/// <summary>
/// One fleet parcels <see cref="GoalType.Freighter"/> across its cargo ships.
/// The ship planner still rejects a fleet.
/// </summary>
public class FleetFreighterPlan : IGoalPlanner
{
    public GoalType Type => GoalType.FleetFreighter;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
        => FleetParcel.Plan(managedEntity, goal, freight: true);
}
