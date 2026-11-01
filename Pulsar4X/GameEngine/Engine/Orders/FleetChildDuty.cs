using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;

namespace GameEngine.Engine.Orders;

/// <summary>
/// Whether a fleet hand-down may task this child.
/// A child already working the fleet goal is skipped so the wake does not stack a second copy.
/// A child with its own in-progress goal, or with actions the player plotted, is left alone.
/// </summary>
public static class FleetChildDuty
{
    /// <summary>
    /// Every entity below this fleet, sub-fleets and their members included, nearest first.
    /// A fleet order that hands work to ships reads this, so a ship in a sub-fleet is not missed.
    /// </summary>
    public static List<Entity> Descendants(Entity fleet)
    {
        var found = new List<Entity>();
        var queue = new Queue<Entity>();
        queue.Enqueue(fleet);
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            if (!next.TryGetDataBlob<FleetDB>(out var db))
                continue;
            foreach (var child in db.Children)
            {
                found.Add(child);
                queue.Enqueue(child);
            }
        }
        return found;
    }

    public static bool WorkingThisGoal(Entity child, Goal fleetGoal)
    {
        if (!child.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return false;
        var active = goals.ActiveGoal;
        return active.ParentGoalId == fleetGoal.Id
            && active.Status is not (GoalStatus.Completed or GoalStatus.Failed);
    }

    /// <summary>
    /// A ship the player took off this fleet order stays busy, so the same job does not hand it again.
    /// A ship's own cancelled order stays busy too. A child of a fleet order that has been replaced does not.
    /// Own in-progress work, and actions the player plotted, stay busy too.
    /// </summary>
    public static bool BusyWithOwnWork(Entity child, Goal fleetGoal)
    {
        if (child.TryGetDataBlob<GoalsDB>(out var goals))
        {
            if (CancelledBlocks(goals.ActiveGoal, fleetGoal) || CancelledBlocks(goals.GivenGoal, fleetGoal))
                return true;
            if (goals.ActiveGoal != null)
            {
                var active = goals.ActiveGoal;
                if (active.Status is not (GoalStatus.Completed or GoalStatus.Failed)
                    && active.ParentGoalId != fleetGoal.Id)
                    return true;
            }
        }

        return HasPlayerPlottedActions(child, fleetGoal);
    }

    /// <summary>
    /// True when this cancelled goal is the ship's own order, or a child of <paramref name="fleetGoal"/>.
    /// A cancelled child of an earlier fleet order must not block the next one.
    /// </summary>
    static bool CancelledBlocks(Goal? goal, Goal fleetGoal)
    {
        if (goal is not { PlayerCancelled: true })
            return false;
        if (string.IsNullOrEmpty(goal.ParentGoalId))
            return true;
        return goal.ParentGoalId == fleetGoal.Id;
    }

    public static bool HasPlayerPlottedActions(Entity child, Goal fleetGoal)
    {
        if (!child.TryGetDataBlob<ActionQueueDB>(out var queue))
            return false;

        string fleetChildGoalId = "";
        if (child.TryGetDataBlob<GoalsDB>(out var goals)
            && goals.ActiveGoal != null
            && goals.ActiveGoal.ParentGoalId == fleetGoal.Id)
            fleetChildGoalId = goals.ActiveGoal.Id;

        foreach (var action in queue.ActionList)
        {
            if (action.Status is ActionStatus.Succeeded or ActionStatus.Failed)
                continue;
            if (action.ParentGoalId == fleetGoal.Id)
                continue;
            if (fleetChildGoalId.Length > 0 && action.ParentGoalId == fleetChildGoalId)
                continue;
            return true;
        }
        return false;
    }
}
