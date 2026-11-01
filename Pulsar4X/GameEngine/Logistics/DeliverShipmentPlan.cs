using Pulsar4X.Industry;
using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One ship carries a sealed shipment, or a part of whole lines, from its source colony to its destination colony.
/// The goods are already paid, so this does not call <see cref="MarketExchangeAction"/> and does not score a spread.
/// <c>Goal.CargoId</c> is the shipment's unique id, and <c>SourceEntityId</c> / <c>DestEntityId</c> are the colonies.
/// The planner is stateless: each wake reads the shipment and the hold to see what is left to do.
/// Neither colony has to belong to the ship's faction. The claim (market path) checks that it can trade with both.
/// </summary>
public class DeliverShipmentPlan : IGoalPlanner
{
    public GoalType Type => GoalType.DeliverShipment;

    public PlanResult Plan(Entity ship, Goal goal, DateTime atDateTime)
    {
        if (ship.HasDataBlob<FleetDB>())
            return PlanResult.Fail("Deliver shipment is one ship");
        if (!ship.HasDataBlob<ShipInfoDB>())
            return PlanResult.Fail("Non supported entity");
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var hold))
            return PlanResult.Fail("The ship has no cargo storage.");
        var game = ship.Manager?.Game;
        if (game == null)
            return PlanResult.Fail("Non supported entity");

        bool child = !string.IsNullOrEmpty(goal.ParentGoalId);

        if (ship.TryGetDataBlob<GoalsDB>(out var goals)
            && GoalWeighting.ShouldInterruptForRefuel(goals, goal))
        {
            Unclaim(game, ship, goal);
            return PlanResult.Fail("low on fuel");
        }

        // A direct second plan while a transfer is queued must not complete the trip.
        if (TransferQueued(ship, goal))
            return PlanResult.Continue(new List<EntityAction>());

        if (!Lookup(game, goal.SourceEntityId, out var source) || !Lookup(game, goal.DestEntityId, out var dest))
        {
            Unclaim(game, ship, goal);
            return PlanResult.Fail("the colony is gone.");
        }

        if (!ShipmentBook.TryFind(source, goal.CargoId, out var shipment))
        {
            Unclaim(game, ship, goal);
            return PlanResult.Done("shipment closed");
        }

        if (!EnsureClaim(ship, dest, goal, shipment, child, out var reason))
            return PlanResult.Fail(reason);

        // Whatever has landed here is unpacked first, so the count below is true.
        ShipmentBook.Unpack(dest, shipment);
        if (shipment.AllArrived)
        {
            ShipmentBook.Complete(game, shipment, source, ship.FactionOwnerID, atDateTime);
            return PlanResult.Done("delivered");
        }

        var aboard = Aboard(ship, hold, shipment);
        if (aboard.Count > 0)
            return PlanUnload(ship, goal, dest, aboard);

        bool open = shipment.Lines.Any(line => !line.Released);
        bool carriedBefore = shipment.Lines.Any(line => line.Released && line.CarrierId == ship.Id);
        if (!open)
            return PlanResult.Done("nothing left to load");
        // A fleet ship delivers its own load and is done. The fleet hands the rest to a free ship.
        if (child && carriedBefore)
            return PlanResult.Done("trip delivered");

        return PlanLoad(ship, goal, source, shipment);
    }

    public void Cancel(Entity unit, Goal goal, DateTime at, CancelTiming timing)
    {
        GoalCancel.Apply(unit, goal, at, timing);
        var game = unit.Manager?.Game;
        if (game != null && string.IsNullOrEmpty(goal.ParentGoalId))
            Unclaim(game, unit, goal);
    }

    // ------------------------------------------------------------------ legs

    static PlanResult PlanLoad(Entity ship, Goal goal, Entity source, ShipmentCargo shipment)
    {
        var game = ship.Manager.Game;
        if (!MarketExchangeAction.InRange(ship, source))
        {
            if (!MoveGoodsPlan.TryMoveIntoRange(ship, source, out var moves, out var reason, goal))
            {
                Unclaim(game, ship, goal);
                return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "out of range" : reason);
            }
            return PlanResult.Continue(moves);
        }

        // Whole lines only. Earlier lines get the hold first.
        var db = ship.GetDataBlob<CargoStorageDB>();
        var room = new Dictionary<string, double>(StringComparer.Ordinal);
        var picked = new List<ShipmentLine>();
        foreach (var line in shipment.Lines)
        {
            if (line.Released || line.Units <= 0 || line.Held.VolumePerUnit <= 0 || !db.Accepts(line.Held))
                continue;
            string type = line.Held.CargoTypeID;
            if (!db.TypeStores.ContainsKey(type))
                continue;
            if (!room.TryGetValue(type, out var left))
                left = db.GetFreeVolume(line.Held);
            if (line.Volume > left + 1e-9 || db.GetFreeUnitSpace(line.Held) < line.Units)
                continue;
            room[type] = left - line.Volume;
            picked.Add(line);
        }

        if (picked.Count == 0)
        {
            Unclaim(game, ship, goal);
            return PlanResult.Fail("nothing fits");
        }

        foreach (var line in picked)
            ShipmentBook.ReleaseLine(source, shipment, line, ship.Id);

        var items = picked.Select(line => (line.Held, line.Units)).ToList();
        var (primary, secondary) = CargoTransferOrder.CreateLoadPair(ship.FactionOwnerID, ship, source, items);
        // The warehouse's own faction orders its side, so a ship of another faction can still draw from it.
        secondary.RequestingFactionGuid = source.FactionOwnerID;
        bool queued = game.OrderHandler.HandleOrder(secondary);
        bool whole = queued && picked.All(line => primary.Taken(line.Held.ID) >= line.Units);
        if (!whole)
        {
            primary.Stop(ship.StarSysDateTime);
            foreach (var line in picked)
                ShipmentBook.Rehold(source, shipment, line);
            Unclaim(game, ship, goal);
            return PlanResult.Fail(queued ? "no room aboard" : "nothing to load");
        }

        return PlanResult.Continue(new List<EntityAction> { primary }, "Loading");
    }

    static PlanResult PlanUnload(Entity ship, Goal goal, Entity dest, List<(ICargoable item, long units)> aboard)
    {
        if (!MarketExchangeAction.InRange(ship, dest))
        {
            if (!MoveGoodsPlan.TryMoveIntoRange(ship, dest, out var moves, out var reason, goal))
                return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "out of range" : reason);
            return PlanResult.Continue(moves);
        }

        // As much as the destination can take. What stays aboard is retried on the next wake.
        var db = dest.GetDataBlob<CargoStorageDB>();
        var room = new Dictionary<string, double>(StringComparer.Ordinal);
        var picked = new List<(ICargoable item, long amount)>();
        foreach (var (item, units) in aboard)
        {
            if (item.VolumePerUnit <= 0 || !db.TypeStores.ContainsKey(item.CargoTypeID))
                continue;
            if (!room.TryGetValue(item.CargoTypeID, out var left))
                left = db.GetFreeVolume(item);
            long byVolume = left <= 0 ? 0 : (long)Math.Floor((left + 1e-9) / item.VolumePerUnit);
            long take = Math.Min(units, Math.Min(byVolume, Math.Max(0, db.GetFreeUnitSpace(item))));
            if (take <= 0)
                continue;
            room[item.CargoTypeID] = left - take * item.VolumePerUnit;
            picked.Add((item, -take));
        }

        if (picked.Count == 0)
            return Wait(ship);

        var (primary, secondary) = CargoTransferOrder.CreateUnloadPair(ship.FactionOwnerID, ship, dest, picked);
        secondary.RequestingFactionGuid = dest.FactionOwnerID;
        if (!ship.Manager.Game.OrderHandler.HandleOrder(secondary))
        {
            primary.Stop(ship.StarSysDateTime);
            return Wait(ship);
        }
        return PlanResult.Continue(new List<EntityAction> { primary }, "Unloading");
    }

    static PlanResult Wait(Entity ship)
        => PlanResult.Continue(new List<EntityAction> { new UnloadWaitOrder(ship) }, "Waiting to unload");

    // ------------------------------------------------------------------ helpers

    /// <summary>What of this shipment is on the ship: the sealed resource lines and any crate in a line.</summary>
    internal static List<(ICargoable item, long units)> Aboard(Entity ship, CargoStorageDB hold, ShipmentCargo shipment)
    {
        var found = new List<(ICargoable item, long units)>();
        foreach (var line in shipment.Lines)
        {
            if (!line.Released || line.Arrived)
                continue;
            long units = hold.GetUnitsStored(line.Held, false);
            if (units > 0)
                found.Add((line.Held, units));
        }
        return found;
    }

    static bool Lookup(Game game, int id, out Entity colony)
    {
        colony = null!;
        return id >= 0
            && game.GlobalManager.TryGetGlobalEntityById(id, out colony)
            && colony.HasDataBlob<CargoStorageDB>();
    }

    /// <summary>
    /// A market package has a haul row. The first ship to plan it claims the row when nobody has, so a ship
    /// that was given the goal with no command still holds the claim. A fleet's ship works under its fleet's claim.
    /// </summary>
    static bool EnsureClaim(Entity ship, Entity dest, Goal goal, ShipmentCargo shipment, bool child, out string reason)
    {
        reason = "";
        if (!shipment.IsMarket || child)
            return true;
        if (!dest.TryGetDataBlob<LogiBaseDB>(out var book) || book.Intel == null
            || !book.Intel.TryGetValue(IntelBook.Key(IntelKind.Haul, shipment.UniqueID), out var row))
            return true;
        if (row.ClaimantId == ship.Id)
            return true;
        if (row.ClaimantId >= 0)
        {
            reason = "That haul is already claimed.";
            return false;
        }
        return IntelBook.TryClaim(dest, ship, shipment.UniqueID, out reason);
    }

    /// <summary>
    /// A claim that fails before loading clears the claimant so another free ship can take the row.
    /// Not once anything of the shipment is out of the hold.
    /// </summary>
    static void Unclaim(Game game, Entity ship, Goal goal)
    {
        if (!string.IsNullOrEmpty(goal.ParentGoalId) || !Lookup(game, goal.DestEntityId, out var dest))
            return;
        if (Lookup(game, goal.SourceEntityId, out var source)
            && ShipmentBook.TryFind(source, goal.CargoId, out var shipment)
            && shipment.Lines.Any(line => line.Released && !line.Arrived))
            return;
        IntelBook.ReleaseClaim(dest, goal.CargoId, ship.Id);
    }

    static bool TransferQueued(Entity ship, Goal goal)
    {
        if (!ship.TryGetDataBlob<ActionQueueDB>(out var queue))
            return false;
        foreach (var action in queue.ActionsFor(goal))
        {
            if (action is CargoTransferOrder or UnloadWaitOrder
                && action.Status is not (ActionStatus.Succeeded or ActionStatus.Failed))
                return true;
        }
        return false;
    }
}

/// <summary>
/// The fleet does not fly or pay. It hands the package to free captained cargo ships, each child
/// <see cref="GoalType.DeliverShipment"/> taking whole lines, while <see cref="LogisticsSpan.ContractReaches"/> holds.
/// A package held for a fleet order is released back to open stock when the order fails or is cancelled.
/// A market package is not released here: it was paid for, so it stays sealed at the source.
/// </summary>
public class FleetDeliverShipmentPlan : IGoalPlanner
{
    public GoalType Type => GoalType.FleetDeliverShipment;

    public PlanResult Plan(Entity fleetEntity, Goal goal, DateTime atDateTime)
    {
        if (!fleetEntity.HasDataBlob<FleetDB>())
            return PlanResult.Fail("We have no subordinates to manage");
        var game = fleetEntity.Manager?.Game;
        if (game == null)
            return PlanResult.Fail("We have no subordinates to manage");

        if (!Lookup(game, goal.SourceEntityId, out var source) || !Lookup(game, goal.DestEntityId, out var dest))
            return PlanResult.Fail("the colony is gone.");
        if (!ShipmentBook.TryFind(source, goal.CargoId, out var shipment))
            return PlanResult.Done("shipment closed");

        var ships = FleetChildDuty.Descendants(fleetEntity).Where(e => e.HasDataBlob<ShipInfoDB>()).ToList();
        var workers = ships.Where(ship => FleetChildDuty.WorkingThisGoal(ship, goal)).ToList();

        if (goal.PlayerCancelled)
        {
            if (workers.Count > 0)
                return PlanResult.Continue(new List<(Entity, Goal)>());
            return Stop(game, fleetEntity, goal, shipment, dest, "cancelled");
        }

        if (fleetEntity.TryGetDataBlob<GoalsDB>(out var goals)
            && GoalWeighting.ShouldInterruptForRefuel(goals, goal))
            return Stop(game, fleetEntity, goal, shipment, dest, "low on fuel");

        // A ship that has been handed the package but has not loaded yet is still on its way to the source.
        // A second ship would race it for the same lines.
        if (workers.Any(ship => !shipment.Lines.Any(line => line.Released && line.CarrierId == ship.Id)))
            return PlanResult.Continue(new List<(Entity, Goal)>());

        var open = shipment.Lines.Where(line => !line.Released).ToList();
        if (open.Count == 0)
        {
            if (workers.Count > 0)
                return PlanResult.Continue(new List<(Entity, Goal)>());
            return Stop(game, fleetEntity, goal, shipment, dest, "load stranded");
        }

        if (!LogisticsSpan.ContractReaches(fleetEntity, source, dest))
            return Stop(game, fleetEntity, goal, shipment, dest, "out of reach");

        bool anyShip = false;
        Entity best = null!;
        double bestDistance = double.MaxValue;
        foreach (var ship in ships)
        {
            bool drive = ship.HasDataBlob<WarpAbilityDB>() || ship.HasDataBlob<NewtonThrustAbilityDB>();
            if (!ship.TryGetDataBlob<CargoStorageDB>(out var hold) || !drive)
                continue;
            anyShip = true;
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

            double distance = MarketRun.DistanceMeters(ship, source);
            if (best != null && (distance > bestDistance || (distance == bestDistance && ship.Id > best.Id)))
                continue;
            best = ship;
            bestDistance = distance;
        }

        if (best == null)
        {
            if (workers.Count > 0)
                return PlanResult.Continue(new List<(Entity, Goal)>());
            return Stop(game, fleetEntity, goal, shipment, dest, anyShip ? "no ship" : "We have no subordinates to manage");
        }

        var childGoal = new Goal(GoalType.DeliverShipment)
        {
            Name = "Deliver shipment",
            ParentGoalId = goal.Id,
            CargoId = goal.CargoId,
            SourceEntityId = source.Id,
            DestEntityId = dest.Id,
        };
        return PlanResult.Continue(new List<(Entity, Goal)> { (best, childGoal) });
    }

    public void Cancel(Entity unit, Goal goal, DateTime at, CancelTiming timing)
    {
        GoalCancel.Apply(unit, goal, at, timing);
        var game = unit.Manager?.Game;
        if (game == null || !Lookup(game, goal.SourceEntityId, out var source)
            || !ShipmentBook.TryFind(source, goal.CargoId, out var shipment))
            return;

        // A child still finishing keeps its load. The next plan wake releases the package once it is done.
        bool working = FleetChildDuty.Descendants(unit)
            .Any(ship => ship.HasDataBlob<ShipInfoDB>() && FleetChildDuty.WorkingThisGoal(ship, goal));
        if (timing == CancelTiming.WhenConvenient && working)
            return;
        ShipmentBook.Release(game, shipment);
    }

    /// <summary>Fail the order. The package goes back to open stock when it is not a market sale.</summary>
    static PlanResult Stop(Game game, Entity fleet, Goal goal, ShipmentCargo shipment, Entity dest, string why)
    {
        if (!shipment.IsMarket)
            ShipmentBook.Release(game, shipment);
        else if (!shipment.Lines.Any(line => line.Released && !line.Arrived))
            IntelBook.ReleaseClaim(dest, shipment.UniqueID, fleet.Id);
        return PlanResult.Fail(why);
    }

    static Goal? OwnTrip(Entity ship, Goal fleetGoal)
    {
        if (!ship.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return null;
        return goals.ActiveGoal.ParentGoalId == fleetGoal.Id ? goals.ActiveGoal : null;
    }

    static bool Lookup(Game game, int id, out Entity colony)
    {
        colony = null!;
        return id >= 0
            && game.GlobalManager.TryGetGlobalEntityById(id, out colony)
            && colony.HasDataBlob<CargoStorageDB>();
    }
}
