using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;

namespace Pulsar4X.Storage;

/// <summary>
/// Task planner for <see cref="GoalType.RefuelAt"/>.
/// TargetEntityID = depot, colony, or fleet tanker to take fuel from.
/// Leaf: move (via MovePlanner) then cargo transfer WaitTillFull.
/// Fleet: hand RefuelAt to ships that are low on fuel (source stays the target).
/// Does not mutate the goal — agent applies <see cref="PlanResult"/> status.
/// </summary>
public class RefuelShipPlanner : IGoalPlanner
{
    public GoalType Type => GoalType.RefuelAt;

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
        if (!FuelSituation.TryGetFuelMass(ship, out var fuel, out _, out _))
            return PlanResult.Fail("ship has no newton fuel system / cargo for fuel");

        // Resolve source: explicit goal target, else fleet tanker from Observe.
        var snap = FuelSituation.Observe(ship, goal.TargetEntityID > 0 ? goal.TargetEntityID : null);
        int sourceId = goal.TargetEntityID > 0 ? goal.TargetEntityID : (snap.PreferredSourceId ?? -1);

        if (sourceId <= 0 || !ship.Manager.TryGetGlobalEntityById(sourceId, out var source))
            return PlanResult.Fail(snap.SourceReason ?? "no refuel source");

        if (!source.TryGetDataBlob<CargoStorageDB>(out _))
            return PlanResult.Fail("refuel source has no cargo storage");

        // Already full enough — nothing to do.
        if (snap.Fraction >= 0.98f)
            return PlanResult.Done("tanks full");

        // In-flight work for this goal.
        if (ship.TryGetDataBlob<ActionQueueDB>(out var actionQueue))
        {
            var actionsForGoal = actionQueue.ActionsFor(goal);
            if (actionsForGoal.Count > 0)
            {
                if (actionsForGoal.Exists(a => a.Status == ActionStatus.Failed))
                    return PlanResult.Fail("a refuel action failed");
                if (actionsForGoal.TrueForAll(a => a.Status == ActionStatus.Succeeded))
                    return PlanResult.Done();
                return PlanResult.Continue(new List<EntityAction>());
            }
        }

        var plan = new List<EntityAction>();

        // 1. Get alongside the source (empty if already there).
        if (!MovePlanner.TryBuildMoveActions(ship, sourceId, out var moveActions, out var moveReason))
            return PlanResult.Fail(moveReason);
        plan.AddRange(moveActions);

        // 2. Transfer: receiver is primary. Secondary must also run on the supplier;
        //    agent only enqueues on the planning unit, so submit secondary here.
        var (primary, secondary) = CargoTransferOrder.CreateRefuelPair(
            ship.FactionOwnerID,
            receiver: ship,
            supplier: source,
            fuel: fuel,
            CargoTransferOrder.Conditionals.WaitTillFull);

        plan.Add(primary);
        ship.Manager.Game.OrderHandler.HandleOrder(secondary);

        return PlanResult.Continue(plan);
    }

    public PlanResult PlanSubGoals(Entity fleet, Goal goal)
    {
        if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB))
            return PlanResult.Fail("no subordinates");

        // Target is the fuel source (tanker or depot). If unset, pick first child with storage.
        int sourceId = goal.TargetEntityID;
        if (sourceId <= 0)
        {
            foreach (var child in fleetDB.Children)
            {
                if (!child.HasDataBlob<ShipInfoDB>())
                    continue;
                if (!child.TryGetDataBlob<CargoStorageDB>(out _))
                    continue;
                sourceId = child.Id;
                break;
            }
        }

        if (sourceId <= 0)
            return PlanResult.Fail("no refuel source for fleet");

        var subGoals = new List<(Entity subordinate, Goal goal)>();
        int alreadyWorking = 0;

        foreach (var subunit in fleetDB.Children)
        {
            if (subunit.Id == sourceId)
                continue;
            if (!subunit.HasDataBlob<ShipInfoDB>())
                continue;
            if (!FuelSituation.TryGetFuelMass(subunit, out _, out _, out _))
                continue;

            if (subunit.TryGetDataBlob<GoalsDB>(out var childGoals)
                && childGoals.ActiveGoal != null
                && childGoals.ActiveGoal.ParentGoalId == goal.Id
                && childGoals.ActiveGoal.Status is not (GoalStatus.Completed or GoalStatus.Failed))
            {
                alreadyWorking++;
                continue;
            }

            var snap = FuelSituation.Observe(subunit, sourceId);
            if (snap.Fraction >= 0.9f)
                continue;

            subGoals.Add((subunit, new Goal(GoalType.RefuelAt)
            {
                TargetEntityID = sourceId,
                ParentGoalId = goal.Id,
            }));
        }

        if (subGoals.Count == 0)
        {
            if (alreadyWorking > 0)
                return PlanResult.Continue(new List<(Entity subordinate, Goal goal)>());
            return PlanResult.Done("no ships need fuel");
        }

        return PlanResult.Continue(subGoals);
    }
}
