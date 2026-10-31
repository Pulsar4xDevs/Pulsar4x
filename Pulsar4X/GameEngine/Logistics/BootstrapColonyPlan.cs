using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Industry;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One captained ship carries finished crates from one colony to another and its Construction Bay
/// mounts them there from its own hold. Nothing is unloaded. Every plan reads the world, so a re-plan
/// after any finished leg picks up where the ship is: load, fly, mount, done.
/// </summary>
public class BootstrapColonyPlan : IGoalPlanner
{
    public GoalType Type => GoalType.BootstrapColony;

    public PlanResult Plan(Entity ship, Goal goal, DateTime atDateTime)
    {
        if (ship.HasDataBlob<FleetDB>())
            return PlanResult.Fail("Bootstrap colony is one ship");
        if (!ship.HasDataBlob<ShipInfoDB>())
            return PlanResult.Fail("Non supported entity");
        if (!ship.TryGetDataBlob<LocalConstructionDB>(out var bay) || bay.PointsPerDay <= 0)
            return PlanResult.Fail("The ship has no Construction Bay.");
        if (!ship.HasDataBlob<CargoStorageDB>())
            return PlanResult.Fail("The ship has no cargo storage.");
        if (!ship.HasDataBlob<WarpAbilityDB>() && !ship.HasDataBlob<NewtonThrustAbilityDB>())
            return PlanResult.Fail("The ship has no drive.");

        // A direct second plan while a leg is queued must not finish the goal or double a load.
        // This runs before the fuel check so a ship that is mounting holds station.
        if (LegQueued(ship, goal))
            return PlanResult.Continue(new List<EntityAction>());

        if (ship.TryGetDataBlob<GoalsDB>(out var goals)
            && GoalWeighting.ShouldInterruptForRefuel(goals, goal))
            return PlanResult.Fail("low on fuel");

        if (!MoveGoodsPlan.TryColony(ship, goal.SourceEntityId, out var source, out var reason)
            || !MoveGoodsPlan.TryColony(ship, goal.DestEntityId, out var dest, out reason))
            return PlanResult.Fail(reason);
        if (source.Id == dest.Id)
            return PlanResult.Fail("Pick two different colonies.");

        if (goal.BootstrapCrateIds.Count == 0 && !TryPick(ship, source, goal, out reason))
            return PlanResult.Fail(reason);

        var atSource = new List<MoveGoodsLine>();
        var aboard = new List<int>();
        int mounted = 0;
        foreach (int id in goal.BootstrapCrateIds)
        {
            if (MountFromHoldAction.Mounted(dest, id))
                mounted++;
            else if (MountFromHoldAction.TryAboard(ship, id, out _))
                aboard.Add(id);
            else if (MountFromHoldAction.TryAboard(source, id, out _))
                atSource.Add(new MoveGoodsLine { ItemId = id, Requested = 1 });
            else
                return PlanResult.Fail("A crate is gone from the source and the hold.");
        }

        if (mounted == goal.BootstrapCrateIds.Count)
            return PlanResult.Done($"Mounted {mounted} {(mounted == 1 ? "crate" : "crates")}");

        if (atSource.Count > 0)
            return PlanLoad(ship, source, atSource);

        if (!LocalConstructionProcessor.InTransferRange(ship, dest))
        {
            if (!MoveGoodsPlan.TryMoveIntoRange(ship, dest, out var moves, out reason))
                return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "The colony is out of transfer range." : reason);
            return PlanResult.Continue(moves);
        }

        return PlanResult.Continue(
            new List<EntityAction> { MountFromHoldAction.Create(ship, dest.Id, aboard) },
            "Mounting crates");
    }

    static PlanResult PlanLoad(Entity ship, Entity source, List<MoveGoodsLine> lines)
    {
        if (!MarketExchangeAction.InRange(ship, source))
        {
            if (!MoveGoodsPlan.TryMoveIntoRange(ship, source, out var moves, out var reason))
                return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "out of range" : reason);
            return PlanResult.Continue(moves);
        }

        // One trip. Crates that do not all fit the hold fail the goal and are not split.
        if (MoveGoodsPlan.ClampLines(source, ship, lines, useLoaded: false).Count < lines.Count)
            return PlanResult.Fail("doesn't fit hold");

        return PlanResult.Continue(
            new List<EntityAction> { LoadGoodsAction.Create(ship, source.Id, 0, lines, plotDestination: false) },
            "Loading crates");
    }

    /// <summary>
    /// Choose crate instances once, ship's hold first, then the source warehouse. Later plans follow
    /// these ids, so Done counts only what this goal picked.
    /// </summary>
    static bool TryPick(Entity ship, Entity source, Goal goal, out string reason)
    {
        reason = "";
        if (goal.BootstrapCrates.Count == 0)
        {
            reason = "Nothing to build.";
            return false;
        }
        if (!ship.Manager.Game.Factions.TryGetValue(ship.FactionOwnerID, out var faction)
            || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
        {
            reason = "The ship has no faction.";
            return false;
        }

        var picked = new List<int>();
        foreach (var line in goal.BootstrapCrates)
        {
            if (!info.ComponentDesigns.TryGetValue(line.DesignId, out var design))
            {
                reason = $"Unknown design {line.DesignId}.";
                return false;
            }
            if (!LocalConstructionJob.IsCrate(design))
            {
                reason = $"{design.Name} is not a crate-able facility.";
                return false;
            }

            int found = 0;
            foreach (var holder in new[] { ship, source })
            {
                foreach (var crate in CratesOf(holder, line.DesignId))
                {
                    if (found >= line.Count)
                        break;
                    if (picked.Contains(crate.ID))
                        continue;
                    picked.Add(crate.ID);
                    found++;
                }
            }
            if (found < line.Count)
            {
                reason = $"no finished crates of {design.Name} at source";
                return false;
            }
        }

        goal.BootstrapCrateIds = picked;
        return true;
    }

    internal static List<ComponentInstance> CratesOf(Entity holder, string designId)
    {
        var found = new List<ComponentInstance>();
        if (!holder.TryGetDataBlob<CargoStorageDB>(out var storage))
            return found;
        foreach (var store in storage.TypeStores.Values)
        {
            foreach (var cargo in store.GetCargoables().Values)
            {
                if (cargo is ComponentInstance crate
                    && crate.Design.UniqueID == designId
                    && storage.GetUnitsStored(crate, false) >= 1)
                    found.Add(crate);
            }
        }
        return found;
    }

    static bool LegQueued(Entity ship, Goal goal)
    {
        if (!ship.TryGetDataBlob<ActionQueueDB>(out var queue))
            return false;
        foreach (var action in queue.ActionsFor(goal))
        {
            if ((action is LoadGoodsAction or MountFromHoldAction or CargoTransferOrder)
                && action.Status is not (ActionStatus.Succeeded or ActionStatus.Failed))
                return true;
        }
        return false;
    }
}
