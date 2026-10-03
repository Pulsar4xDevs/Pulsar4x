using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;

namespace Pulsar4X.Logistics;

/// <summary>
/// One fleet parcels <see cref="GoalType.Trade"/> across its cargo ships.
/// The ship planner still rejects a fleet.
/// </summary>
public class FleetTradePlan : IGoalPlanner
{
    public GoalType Type => GoalType.FleetTrade;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
        => FleetParcel.Plan(managedEntity, goal, freight: false);
}
