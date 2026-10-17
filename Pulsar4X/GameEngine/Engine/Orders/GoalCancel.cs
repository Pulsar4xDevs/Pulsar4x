using System;
using System.Collections.Generic;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.People;

namespace GameEngine.Engine.Orders;

/// <summary>
/// Shared cancel. Planners call <see cref="Apply"/>. MoveTo keeps a warp that is already up.
/// </summary>
static class GoalCancel
{
    public static void Apply(Entity unit, Goal goal, DateTime at, CancelTiming timing)
    {
        if (!unit.TryGetDataBlob<ActionQueueDB>(out var queue))
            return;

        var actions = queue.ActionsFor(goal);
        foreach (var action in actions)
        {
            if (timing == CancelTiming.WhenConvenient && Started(action))
                continue;
            action.Stop(at);
            queue.ActionList.Remove(action);
        }
    }

    public static void CancelLine(Entity unit, EntityAction action, DateTime at)
    {
        action.Stop(at);
        if (!unit.TryGetDataBlob<ActionQueueDB>(out var queue))
            return;

        string parent = action.ParentGoalId ?? "";
        queue.ActionList.Remove(action);
        if (action is WarpMoveAction && parent.Length > 0)
        {
            var drop = new List<EntityAction>();
            foreach (var other in queue.ActionList)
            {
                if (other is CirculariseAction
                    && other.ParentGoalId == parent
                    && !Started(other))
                    drop.Add(other);
            }
            foreach (var other in drop)
                queue.ActionList.Remove(other);
        }

        if (parent.Length > 0)
            Note(unit, parent, at);
    }

    /// <summary>
    /// A cancelled goal that still has a line stays active until that line finishes.
    /// A fleet stays active while a child is still finishing.
    /// </summary>
    public static void Settle(Entity unit, Entity agentHost, Goal goal, DateTime at)
    {
        if (unit.TryGetDataBlob<ActionQueueDB>(out var queue))
        {
            queue.ActionList.RemoveAll(a =>
                a.ParentGoalId == goal.Id && a.Status == ActionStatus.Succeeded);
        }

        if (Remaining(unit, goal) || ChildStillFinishing(unit, goal))
        {
            goal.Status = GoalStatus.Active;
            goal.Message = "finishing";
            AgentProcessor.ScheduleAgent(agentHost, at + CommanderSkills.RecheckInterval(unit));
            return;
        }

        goal.Status = GoalStatus.Failed;
        goal.Message = "cancelled";
        // The order is over. Drop it from the chair so the row goes away.
        // GivenGoal stays, so an idle wake does not pick a replacement.
        if (unit.TryGetDataBlob<GoalsDB>(out var goals) && ReferenceEquals(goals.ActiveGoal, goal))
            goals.ActiveGoal = null;
    }

    public static bool Started(EntityAction action)
        => action.Status == ActionStatus.Running || action.IsRunning;

    static void Note(Entity unit, string goalId, DateTime at)
    {
        if (!unit.TryGetDataBlob<GoalsDB>(out var goals))
            return;
        Goal? goal = goals.GivenGoal?.Id == goalId ? goals.GivenGoal
            : goals.ActiveGoal?.Id == goalId ? goals.ActiveGoal
            : null;
        if (goal == null)
            return;
        goal.PlayerCancelled = true;
        Settle(unit, unit, goal, at);
    }

    static bool Remaining(Entity unit, Goal goal)
    {
        if (!unit.TryGetDataBlob<ActionQueueDB>(out var queue))
            return false;
        foreach (var action in queue.ActionsFor(goal))
        {
            if (action.Status is ActionStatus.Succeeded or ActionStatus.Failed)
                continue;
            return true;
        }
        return false;
    }

    static bool ChildStillFinishing(Entity unit, Goal goal)
    {
        if (!unit.TryGetDataBlob<FleetDB>(out var fleet))
            return false;
        foreach (var child in fleet.Children)
        {
            if (!child.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
                continue;
            if (goals.ActiveGoal.ParentGoalId != goal.Id)
                continue;
            if (Remaining(child, goals.ActiveGoal))
                return true;
        }
        return false;
    }
}
