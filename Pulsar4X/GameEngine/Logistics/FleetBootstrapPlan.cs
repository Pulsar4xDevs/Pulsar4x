using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
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
/// The fleet Bootstrap order. The fleet does not fly. Its captained ships that carry a Construction
/// Bay, sub-fleets included, each get a <see cref="GoalType.BootstrapColony"/> goal for as many of the
/// player's crates as fit their hold, taken in the player's priority order. A ship that finishes its trip
/// is handed the next batch, so a list bigger than one hold goes over in several trips.
///
/// Every plan reads the world. The goal keeps three things: the priority list (<see cref="Goal.BootstrapCrates"/>),
/// what ships have been given so far (<see cref="Goal.BootstrapAssigned"/>), and the crate instances they were
/// given (<see cref="Goal.BootstrapCrateIds"/>). A ship with its own order, a cancelled order, or a failed trip
/// of this goal is left alone, so the fleet order never doubles up on a ship that took over.
/// </summary>
public class FleetBootstrapPlan : IGoalPlanner
{
    public GoalType Type => GoalType.FleetBootstrap;

    public PlanResult Plan(Entity fleetEntity, Goal goal, DateTime atDateTime)
    {
        if (!fleetEntity.HasDataBlob<FleetDB>())
            return PlanResult.Fail("We have no subordinates to manage");
        if (goal.BootstrapCrates.Count == 0)
            return PlanResult.Fail("Nothing to build.");

        if (!MoveGoodsPlan.TryColony(fleetEntity, goal.SourceEntityId, out var source, out var reason)
            || !MoveGoodsPlan.TryColony(fleetEntity, goal.DestEntityId, out var dest, out reason))
            return PlanResult.Fail(reason);
        if (source.Id == dest.Id)
            return PlanResult.Fail("Pick two different colonies.");

        var ships = FleetChildDuty.Descendants(fleetEntity)
            .Where(entity => entity.HasDataBlob<ShipInfoDB>() && !entity.HasDataBlob<FleetDB>())
            .ToList();
        if (ships.Count == 0)
            return PlanResult.Fail("We have no subordinates to manage");

        string failedWhy = TakeBack(goal, ships, dest);

        bool Live(Goal? trip, GoalType type)
            => trip is { PlayerCancelled: false, Status: not (GoalStatus.Completed or GoalStatus.Failed) } && trip.Type == type;
        bool cratesInFlight = ships.Any(ship => Live(OwnTrip(ship, goal), GoalType.BootstrapColony));
        bool haulsInFlight = ships.Any(ship => Live(OwnTrip(ship, goal), GoalType.MoveGoods));

        var remaining = Remaining(goal);
        var handed = new List<(Entity, Goal)>();
        bool anyEligible = false;
        bool tooBig = false;
        if (remaining.Count > 0)
        {
            var reserved = new HashSet<int>(goal.BootstrapCrateIds);
            foreach (var ship in ships.OrderBy(ship => MarketRun.DistanceMeters(ship, source)).ThenBy(ship => ship.Id))
            {
                if (!Eligible(ship, goal, source, dest, needsBay: true))
                    continue;
                anyEligible = true;
                var trip = Batch(ship, source, goal, remaining, reserved, out bool bigForThisShip);
                tooBig |= bigForThisShip;
                if (trip == null)
                    continue;
                handed.Add((ship, trip));
                foreach (var line in trip.BootstrapCrates)
                    Credit(goal.BootstrapAssigned, line.DesignId, line.Count);
                goal.BootstrapCrateIds.AddRange(trip.BootstrapCrateIds);
                remaining = Remaining(goal);
                if (remaining.Count == 0)
                    break;
            }

            if (handed.Count > 0 || cratesInFlight || haulsInFlight)
                return PlanResult.Continue(handed);

            // Nothing in flight and nothing handed, so the rest cannot ship.
            if (!anyEligible)
                return PlanResult.Fail(string.IsNullOrEmpty(failedWhy) ? "no ship" : failedWhy);
            if (tooBig)
                return PlanResult.Fail("doesn't fit hold");
            return PlanResult.Fail(ShortMessage(fleetEntity, remaining));
        }

        // Resources go after the crates: the crates are what let the destination use them, so every crate
        // has arrived before the first haul leaves.
        if (cratesInFlight)
            return PlanResult.Continue(handed);

        var needs = BootstrapResources.Remaining(fleetEntity, goal);
        if (needs.Count == 0)
        {
            if (haulsInFlight)
                return PlanResult.Continue(handed);
            int total = goal.BootstrapAssigned.Sum(line => line.Count);
            string crates = $"Mounted {total} {(total == 1 ? "crate" : "crates")}";
            return PlanResult.Done(goal.BootstrapSiteBuilds.Count == 0 ? crates : crates + ", hauled the resources");
        }

        // One round of hauls at a time, so every pass reads the stock the source really holds.
        if (haulsInFlight)
            return PlanResult.Continue(handed);

        var hauls = HandOutHauls(ships, goal, source, dest, needs, out string haulWhy);
        if (hauls.Count > 0)
            return PlanResult.Continue(hauls);
        return PlanResult.Fail(string.IsNullOrEmpty(failedWhy) ? haulWhy : failedWhy);
    }

    /// <summary>
    /// A round of resource hauls: each free captained ship with a hold takes what it can of what is still
    /// needed, in the order <see cref="BootstrapResources.Remaining"/> lists it, from what the source holds.
    /// </summary>
    static List<(Entity, Goal)> HandOutHauls(List<Entity> ships, Goal goal, Entity source, Entity dest,
        List<BootstrapResourceLine> needs, out string why)
    {
        why = "no ship";
        var handed = new List<(Entity, Goal)>();
        var left = needs.ToDictionary(need => need.Cargo.ID, need => need.Amount);
        var stock = source.GetDataBlob<CargoStorageDB>();
        var taken = new Dictionary<int, long>();

        bool anyShip = false;
        foreach (var ship in ships.OrderBy(ship => MarketRun.DistanceMeters(ship, source)).ThenBy(ship => ship.Id))
        {
            if (!Eligible(ship, goal, source, dest, needsBay: false))
                continue;
            anyShip = true;
            var wants = new List<MoveGoodsLine>();
            foreach (var need in needs)
            {
                long onHand = stock.GetUnitsStored(need.Cargo, false) - taken.GetValueOrDefault(need.Cargo.ID);
                long ask = Math.Min(left[need.Cargo.ID], Math.Max(0, onHand));
                if (ask > 0)
                    wants.Add(new MoveGoodsLine { ItemId = need.Cargo.ID, Requested = ask });
            }
            var picked = MoveGoodsPlan.ClampLines(source, ship, wants, useLoaded: false);
            if (picked.Count == 0)
                continue;

            var lines = new List<MoveGoodsLine>();
            foreach (var (item, amount) in picked)
            {
                lines.Add(new MoveGoodsLine { ItemId = item.ID, Requested = amount });
                left[item.ID] -= amount;
                taken[item.ID] = taken.GetValueOrDefault(item.ID) + amount;
                goal.BootstrapHauled[item.UniqueID] = goal.BootstrapHauled.GetValueOrDefault(item.UniqueID) + amount;
            }
            handed.Add((ship, new Goal(GoalType.MoveGoods)
            {
                Name = "Move goods",
                ParentGoalId = goal.Id,
                SourceEntityId = source.Id,
                DestEntityId = dest.Id,
                MoveGoodsLines = lines,
            }));
            if (left.Values.All(amount => amount <= 0))
                break;
        }

        if (handed.Count == 0)
        {
            var first = needs.FirstOrDefault(need => stock.GetUnitsStored(need.Cargo, false) <= 0) ?? needs[0];
            why = anyShip ? $"not enough {first.Cargo.Name} at source" : "no ship";
        }
        return handed;
    }

    /// <summary>This goal's trip on the ship, if its active goal is one. Null otherwise.</summary>
    static Goal? OwnTrip(Entity ship, Goal fleetGoal)
    {
        if (!ship.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return null;
        return goals.ActiveGoal.ParentGoalId == fleetGoal.Id ? goals.ActiveGoal : null;
    }

    static bool Eligible(Entity ship, Goal goal, Entity source, Entity dest, bool needsBay)
    {
        if (ship.GetDataBlob<ShipInfoDB>().CommanderID < 0)
            return false;
        if (needsBay && (!ship.TryGetDataBlob<LocalConstructionDB>(out var bay) || bay.PointsPerDay <= 0))
            return false;
        if (!ship.HasDataBlob<CargoStorageDB>())
            return false;
        if (!ship.HasDataBlob<WarpAbilityDB>() && !ship.HasDataBlob<NewtonThrustAbilityDB>())
            return false;
        // A failed trip of this goal benches the ship for the rest of the order, or it would fail again.
        if (OwnTrip(ship, goal) is { Status: GoalStatus.Failed })
            return false;
        if (FleetChildDuty.WorkingThisGoal(ship, goal) || FleetChildDuty.BusyWithOwnWork(ship, goal))
            return false;
        return JumpRoute.CanReach(ship, source) && JumpRoute.CanReach(ship, dest);
    }

    /// <summary>
    /// The player's list minus what ships have been given, in priority order. Designs with nothing left are dropped.
    /// </summary>
    internal static List<BootstrapCrateLine> Remaining(Goal goal)
    {
        var left = new List<BootstrapCrateLine>();
        foreach (var asked in goal.BootstrapCrates)
        {
            int given = goal.BootstrapAssigned.Where(line => line.DesignId == asked.DesignId).Sum(line => line.Count);
            if (asked.Count - given > 0)
                left.Add(new BootstrapCrateLine { DesignId = asked.DesignId, Count = asked.Count - given });
        }
        return left;
    }

    static void Credit(List<BootstrapCrateLine> lines, string designId, int count)
    {
        var line = lines.FirstOrDefault(l => l.DesignId == designId);
        if (line == null)
            lines.Add(new BootstrapCrateLine { DesignId = designId, Count = count });
        else
            line.Count += count;
    }

    /// <summary>
    /// One ship's next trip: crates from the source in priority order until the next one would not fit the
    /// hold. A crate too big for an empty hold is skipped and flagged. Null when nothing fits or nothing is left.
    /// </summary>
    static Goal? Batch(Entity ship, Entity source, Goal goal, List<BootstrapCrateLine> remaining,
        HashSet<int> reserved, out bool tooBig)
    {
        tooBig = false;
        var lines = new List<MoveGoodsLine>();
        var ids = new List<int>();
        var perDesign = new List<BootstrapCrateLine>();
        foreach (var want in remaining)
        {
            int need = want.Count;
            bool full = false;
            foreach (var crate in BootstrapColonyPlan.CratesOf(source, want.DesignId))
            {
                if (need == 0)
                    break;
                if (reserved.Contains(crate.ID) || ids.Contains(crate.ID))
                    continue;
                var trial = new List<MoveGoodsLine>(lines) { new MoveGoodsLine { ItemId = crate.ID, Requested = 1 } };
                if (MoveGoodsPlan.ClampLines(source, ship, trial, useLoaded: false).Count < trial.Count)
                {
                    // Priority is strict: nothing lower on the list rides ahead of this crate.
                    // Only a crate no hold of this ship could ever take is skipped.
                    if (lines.Count == 0)
                        tooBig = true;
                    full = true;
                    break;
                }
                lines = trial;
                ids.Add(crate.ID);
                Credit(perDesign, want.DesignId, 1);
                need--;
            }
            if (full && lines.Count > 0)
                break;
        }

        if (ids.Count == 0)
            return null;
        tooBig = false;
        return new Goal(GoalType.BootstrapColony)
        {
            Name = "Bootstrap colony",
            ParentGoalId = goal.Id,
            SourceEntityId = goal.SourceEntityId,
            DestEntityId = goal.DestEntityId,
            TargetEntityID = goal.DestEntityId,
            BootstrapCrates = perDesign,
            BootstrapCrateIds = ids,
        };
    }

    /// <summary>
    /// A ship's trip of this goal that failed or was cancelled gives its undelivered crates back, once, so a
    /// later trip can carry them. A crate already mounted stays counted. Returns that trip's reason, if any.
    /// </summary>
    static string TakeBack(Goal goal, List<Entity> ships, Entity dest)
    {
        string why = "";
        foreach (var ship in ships)
        {
            if (OwnTrip(ship, goal) is not { } trip)
                continue;
            if (trip.Type == GoalType.MoveGoods)
            {
                // A resource haul gives back what it did not deliver, whether it finished, failed or was cancelled.
                if (trip.Status is not (GoalStatus.Completed or GoalStatus.Failed) && !trip.PlayerCancelled)
                    continue;
                if (trip.Status == GoalStatus.Failed && !string.IsNullOrEmpty(trip.Message))
                    why = trip.Message;
                if (goal.BootstrapSettled.Contains(trip.Id))
                    continue;
                goal.BootstrapSettled.Add(trip.Id);
                BootstrapResources.GiveBack(ship, goal, trip);
                continue;
            }
            if (trip.Status != GoalStatus.Failed && !trip.PlayerCancelled)
                continue;
            if (!string.IsNullOrEmpty(trip.Message))
                why = trip.Message;
            if (goal.BootstrapSettled.Contains(trip.Id))
                continue;
            goal.BootstrapSettled.Add(trip.Id);

            int next = 0;
            foreach (var line in trip.BootstrapCrates)
            {
                for (int i = 0; i < line.Count && next < trip.BootstrapCrateIds.Count; i++, next++)
                {
                    int id = trip.BootstrapCrateIds[next];
                    if (MountFromHoldAction.Mounted(dest, id))
                        continue;
                    goal.BootstrapCrateIds.Remove(id);
                    Credit(goal.BootstrapAssigned, line.DesignId, -1);
                }
            }
        }
        return why;
    }

    static string ShortMessage(Entity fleetEntity, List<BootstrapCrateLine> remaining)
    {
        var first = remaining[0];
        string name = first.DesignId;
        if (fleetEntity.Manager.Game.Factions.TryGetValue(fleetEntity.FactionOwnerID, out var faction)
            && faction.TryGetDataBlob<FactionInfoDB>(out var info)
            && info.ComponentDesigns.TryGetValue(first.DesignId, out var design))
            name = design.Name;
        return $"no finished crates of {name} at source";
    }
}
