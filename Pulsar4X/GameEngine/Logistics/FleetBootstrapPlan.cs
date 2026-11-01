using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Api;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Industry;
using Pulsar4X.Industry.Orders;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// The fleet Bootstrap order. At order build the translator seals the player's crates and the resources the site
/// builds cost into sealed packages at the source (<see cref="BootstrapPackage"/>), and the goal points at them
/// through <see cref="Goal.BootstrapPackageIds"/>. The fleet does not fly. It hands one package to one free
/// captained ship at a time, sub-fleets included, and waits until that ship has loaded.
///
/// Crates go first. Whichever of the ship's Construction Bay or the destination colony has more free construction
/// capacity mounts them, the colony on a tie (<see cref="BootstrapPackage.Choose"/>). A ship that mounts keeps the
/// crates in its hold and gets a <see cref="GoalType.BootstrapColony"/> trip. When the colony mounts, the ship gets a
/// <see cref="GoalType.DeliverShipment"/> trip, unloads the crates and the fleet queues them on the colony.
/// Resources go only once every crate has arrived, as further <see cref="GoalType.DeliverShipment"/> trips of the
/// resource packages. Nothing is hauled with Move goods.
///
/// Every plan reads the world, so the goal holds only the package ids. The order fails or is cancelled with every
/// package released back to open stock. Crates that had already left the hold stay in the cargo they are in, unmarked.
/// </summary>
public class FleetBootstrapPlan : IGoalPlanner
{
    public GoalType Type => GoalType.FleetBootstrap;

    public PlanResult Plan(Entity fleetEntity, Goal goal, DateTime atDateTime)
    {
        var game = fleetEntity.Manager?.Game;
        if (game == null || !fleetEntity.HasDataBlob<FleetDB>())
            return PlanResult.Fail("We have no subordinates to manage");
        if (goal.BootstrapPackageIds.Count == 0)
            return PlanResult.Fail("Nothing to build.");

        if (!MoveGoodsPlan.TryColony(fleetEntity, goal.SourceEntityId, out var source, out var reason)
            || !MoveGoodsPlan.TryColony(fleetEntity, goal.DestEntityId, out var dest, out reason))
            return Stop(game, goal, reason);
        if (source.Id == dest.Id)
            return Stop(game, goal, "Pick two different colonies.");

        // What has landed is unpacked, and a package whose every line has arrived and whose crates are mounted is closed.
        var live = new List<ShipmentCargo>();
        foreach (var package in BootstrapPackage.Open(game, goal))
        {
            ShipmentBook.Unpack(dest, package);
            if (package.AllArrived && !ShipmentBook.AwaitsMount(dest, package))
            {
                ShipmentBook.Complete(game, package, source, fleetEntity.FactionOwnerID, atDateTime);
                continue;
            }
            live.Add(package);
        }

        if (live.Count == 0)
        {
            int total = goal.BootstrapCrates.Sum(line => line.Count);
            string crates = $"Mounted {total} {(total == 1 ? "crate" : "crates")}";
            return PlanResult.Done(goal.BootstrapSiteBuilds.Count == 0 ? crates : crates + ", hauled the resources");
        }

        var ships = FleetChildDuty.Descendants(fleetEntity)
            .Where(entity => entity.HasDataBlob<ShipInfoDB>() && !entity.HasDataBlob<FleetDB>())
            .ToList();
        var workers = ships.Where(ship => FleetChildDuty.WorkingThisGoal(ship, goal)).ToList();

        if (goal.PlayerCancelled)
        {
            if (workers.Count > 0)
                return PlanResult.Continue(new List<(Entity, Goal)>());
            return Stop(game, goal, "cancelled");
        }

        if (fleetEntity.TryGetDataBlob<GoalsDB>(out var goals)
            && GoalWeighting.ShouldInterruptForRefuel(goals, goal))
            return Stop(game, goal, "low on fuel");

        // A crate the ship unloaded is mounted by the colony.
        if (!QueueColonyMounts(game, dest, live, out reason))
            return Stop(game, goal, reason);

        // A line that left the hold and has no ship working on it is stranded: its trip failed or the ship is gone.
        if (live.Any(package => package.Lines.Any(line => line.Released && !line.Arrived
                && !workers.Any(ship => ship.Id == line.CarrierId))))
            return Stop(game, goal, FailedTripMessage(ships, goal) ?? "load stranded");

        // A ship that has been handed a package but has not loaded yet is still on its way to the source.
        // A second ship would race it for the same lines.
        if (workers.Any(ship => !live.Any(package => package.Lines.Any(line => line.Released && line.CarrierId == ship.Id))))
            return PlanResult.Continue(new List<(Entity, Goal)>());

        // Crates arrive before resources (decision 4). While any crate has not arrived the resources wait.
        var cratePackages = live.Where(BootstrapPackage.IsCratePackage).ToList();
        var resourcePackages = live.Where(package => !BootstrapPackage.IsCratePackage(package)).ToList();
        bool crateStillOut = cratePackages.Any(package => package.Lines.Any(line => !line.Arrived));
        var serve = cratePackages.FirstOrDefault(package => package.Lines.Any(line => !line.Released));
        if (serve == null && !crateStillOut)
            serve = resourcePackages.FirstOrDefault(package => package.Lines.Any(line => !line.Released));
        if (serve == null)
            return PlanResult.Continue(new List<(Entity, Goal)>());

        if (ships.Count == 0)
            return Stop(game, goal, "We have no subordinates to manage");
        return HandOut(game, fleetEntity, goal, source, dest, ships, workers, serve);
    }

    public void Cancel(Entity unit, Goal goal, DateTime at, CancelTiming timing)
    {
        GoalCancel.Apply(unit, goal, at, timing);
        var game = unit.Manager?.Game;
        if (game == null)
            return;

        // A child still finishing keeps its load. The next plan wake releases the packages once it is done.
        bool working = FleetChildDuty.Descendants(unit)
            .Any(ship => ship.HasDataBlob<ShipInfoDB>() && FleetChildDuty.WorkingThisGoal(ship, goal));
        if (timing == CancelTiming.WhenConvenient && working)
            return;
        BootstrapPackage.ReleaseAll(game, goal);
    }

    // ------------------------------------------------------------------ hand out

    static PlanResult HandOut(Game game, Entity fleet, Goal goal, Entity source, Entity dest,
        List<Entity> ships, List<Entity> workers, ShipmentCargo serve)
    {
        bool crates = BootstrapPackage.IsCratePackage(serve);
        var open = serve.Lines.Where(line => !line.Released).ToList();

        bool anyShip = false;
        bool anyCanMount = false;
        Entity best = null!;
        MountChoice bestChoice = default;
        double bestDistance = double.MaxValue;
        foreach (var ship in ships)
        {
            if (!ship.TryGetDataBlob<CargoStorageDB>(out var hold)
                || (!ship.HasDataBlob<WarpAbilityDB>() && !ship.HasDataBlob<NewtonThrustAbilityDB>()))
                continue;
            anyShip = true;
            // Whether anything could mount is a fact about the colony and the bays, whatever else keeps a ship idle.
            if (BootstrapPackage.Choose(ship, dest).Possible)
                anyCanMount = true;
            if (ship.GetDataBlob<ShipInfoDB>().CommanderID < 0)
                continue;
            if (OwnTrip(ship, goal) is { Status: GoalStatus.Failed })
                continue;
            if (FleetChildDuty.WorkingThisGoal(ship, goal) || FleetChildDuty.BusyWithOwnWork(ship, goal))
                continue;
            if (!JumpRoute.CanReach(ship, source) || !JumpRoute.CanReach(ship, dest))
                continue;
            if (!open.Any(line => line.Held.VolumePerUnit > 0 && hold.Accepts(line.Held)
                    && hold.TypeStores.ContainsKey(line.Held.CargoTypeID)
                    && line.Volume <= hold.GetFreeVolume(line.Held) + 1e-9))
                continue;

            var choice = BootstrapPackage.Choose(ship, dest);
            if (crates && !choice.Possible)
                continue;

            double distance = MarketRun.DistanceMeters(ship, source);
            if (best != null && (distance > bestDistance || (distance == bestDistance && ship.Id > best.Id)))
                continue;
            best = ship;
            bestChoice = choice;
            bestDistance = distance;
        }

        if (best == null)
        {
            if (workers.Count > 0)
                return PlanResult.Continue(new List<(Entity, Goal)>());
            string why = !anyShip ? "We have no subordinates to manage"
                : crates && !anyCanMount ? "Neither the colony nor a ship's Construction Bay can mount crates"
                : "no ship";
            return Stop(game, goal, why);
        }

        var trip = new Goal(crates && bestChoice.ByShip ? GoalType.BootstrapColony : GoalType.DeliverShipment)
        {
            Name = !crates ? "Deliver resources" : bestChoice.ByShip ? "Bootstrap colony" : "Deliver crates",
            ParentGoalId = goal.Id,
            CargoId = serve.UniqueID,
            SourceEntityId = source.Id,
            DestEntityId = dest.Id,
            TargetEntityID = dest.Id,
        };
        string message = crates ? bestChoice.Describe() : "Hauling resources";
        return PlanResult.Continue(new List<(Entity, Goal)> { (best, trip) }).WithMessage(message);
    }

    /// <summary>
    /// Queue every crate that has landed in the destination cargo on the colony's own construction queue.
    /// False, with the reason, when the colony cannot.
    /// </summary>
    static bool QueueColonyMounts(Game game, Entity dest, List<ShipmentCargo> live, out string reason)
    {
        reason = "";
        dest.TryGetDataBlob<LocalConstructionDB>(out var bay);
        foreach (var package in live)
        {
            foreach (var line in package.Lines)
            {
                if (!line.Released || !line.Arrived || line.Held is not ComponentInstance crate)
                    continue;
                if (MountFromHoldAction.Mounted(dest, crate.ID) || !MountFromHoldAction.TryAboard(dest, crate.ID, out _))
                    continue;
                if (bay != null && MountFromHoldAction.Queued(bay, crate.ID))
                    continue;
                if (bay == null || bay.PointsPerDay <= 0)
                {
                    reason = "The colony has no construction capacity to mount the crates.";
                    return false;
                }
                if (!game.OrderHandler.HandleOrder(AddToConstructionQueueOrder.CreateMount(dest, crate)))
                {
                    reason = "The colony could not queue " + crate.Name + ".";
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>Fail the order and give every package back to open stock.</summary>
    static PlanResult Stop(Game game, Goal goal, string why)
    {
        BootstrapPackage.ReleaseAll(game, goal);
        return PlanResult.Fail(why);
    }

    static string? FailedTripMessage(List<Entity> ships, Goal goal)
    {
        foreach (var ship in ships)
        {
            if (OwnTrip(ship, goal) is { Status: GoalStatus.Failed } trip && !string.IsNullOrEmpty(trip.Message))
                return trip.Message;
        }
        return null;
    }

    /// <summary>This goal's trip on the ship, if its active goal is one. Null otherwise.</summary>
    static Goal? OwnTrip(Entity ship, Goal fleetGoal)
    {
        if (!ship.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return null;
        return goals.ActiveGoal.ParentGoalId == fleetGoal.Id ? goals.ActiveGoal : null;
    }
}
