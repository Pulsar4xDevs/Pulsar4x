using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One ship, one trip. Own warehouse to own warehouse. No market and no logistics office.
/// </summary>
public class MoveGoodsPlan : IGoalPlanner
{
    public GoalType Type => GoalType.MoveGoods;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (managedEntity.HasDataBlob<FleetDB>())
            return PlanResult.Fail("Move goods is one ship");
        if (!managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanResult.Fail("Non supported entity");
        if (!managedEntity.HasDataBlob<CargoStorageDB>())
            return PlanResult.Fail("The ship has no cargo storage.");

        if (managedEntity.TryGetDataBlob<GoalsDB>(out var goals)
            && GoalWeighting.ShouldInterruptForRefuel(goals, goal))
            return PlanResult.Fail("low on fuel");

        // A direct second plan while the transfer is queued must not complete the trip.
        if (TransferQueued(managedEntity, goal))
            return PlanResult.Continue(new List<EntityAction>());

        if (goal.MoveGoodsUnloaded)
            return PlanResult.Done(Leftover(goal));

        if (!TryColony(managedEntity, goal.SourceEntityId, out var source, out var reason)
            || !TryColony(managedEntity, goal.DestEntityId, out var dest, out reason))
            return PlanResult.Fail(reason);

        // The wake that sees a load must also hand the destination flight. An empty
        // continue would complete the goal on the next non-boundary wake.
        if (goal.MoveGoodsLeg == MoveGoodsLeg.Load && goal.MoveGoodsLines.Exists(line => line.Loaded > 0))
            goal.MoveGoodsLeg = MoveGoodsLeg.Unload;

        if (goal.MoveGoodsLeg == MoveGoodsLeg.Load)
            return PlanLoad(managedEntity, goal, source);
        return PlanUnload(managedEntity, goal, dest);
    }

    static PlanResult PlanLoad(Entity ship, Goal goal, Entity source)
    {
        if (!MarketExchangeAction.InRange(ship, source))
        {
            if (!TryMoveIntoRange(ship, source, out var moves, out var reason, goal))
                return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "out of range" : reason);
            return PlanResult.Continue(moves);
        }

        var clamped = ClampLines(source, ship, goal.MoveGoodsLines, useLoaded: false);
        if (clamped.Count == 0)
            return PlanResult.Fail("nothing to load");

        var (primary, secondary) = CargoTransferOrder.CreateLoadPair(ship.FactionOwnerID, ship, source, clamped);
        if (ship.Manager?.Game == null || !ship.Manager.Game.OrderHandler.HandleOrder(secondary))
        {
            primary.Stop(ship.StarSysDateTime);
            return PlanResult.Fail("nothing to load");
        }

        NoteMoved(primary, goal.MoveGoodsLines, loading: true);
        if (!goal.MoveGoodsLines.Exists(line => line.Loaded > 0))
        {
            primary.Stop(ship.StarSysDateTime);
            return PlanResult.Fail("nothing to load");
        }

        return PlanResult.Continue(new List<EntityAction> { primary }, "Loading");
    }

    static PlanResult PlanUnload(Entity ship, Goal goal, Entity dest)
    {
        if (!MarketExchangeAction.InRange(ship, dest))
        {
            if (!TryMoveIntoRange(ship, dest, out var moves, out var reason, goal))
                return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "out of range" : reason);
            return PlanResult.Continue(moves);
        }

        var clamped = ClampLines(ship, dest, goal.MoveGoodsLines, useLoaded: true);
        if (clamped.Count == 0)
            return PlanResult.Fail("no place to unload");

        var signed = new List<(ICargoable item, long amount)>(clamped.Count);
        foreach (var (item, amount) in clamped)
            signed.Add((item, -amount));

        var (primary, secondary) = CargoTransferOrder.CreateUnloadPair(ship.FactionOwnerID, ship, dest, signed);
        if (ship.Manager?.Game == null || !ship.Manager.Game.OrderHandler.HandleOrder(secondary))
        {
            primary.Stop(ship.StarSysDateTime);
            return PlanResult.Fail("no place to unload");
        }

        NoteMoved(primary, goal.MoveGoodsLines, loading: false);
        if (!goal.MoveGoodsLines.Exists(line => line.Delivered > 0))
        {
            primary.Stop(ship.StarSysDateTime);
            return PlanResult.Fail("no place to unload");
        }

        goal.MoveGoodsUnloaded = true;
        return PlanResult.Continue(new List<EntityAction> { primary }, "Unloading");
    }

    static bool TransferQueued(Entity ship, Goal goal)
    {
        if (!ship.TryGetDataBlob<ActionQueueDB>(out var queue))
            return false;
        foreach (var action in queue.ActionsFor(goal))
        {
            if (action is CargoTransferOrder
                && action.Status is not (ActionStatus.Succeeded or ActionStatus.Failed))
                return true;
        }
        return false;
    }

    internal static string Leftover(Goal goal)
    {
        long leftSource = 0;
        long leftAboard = 0;
        foreach (var line in goal.MoveGoodsLines)
        {
            leftSource += Math.Max(0, line.Requested - line.Loaded);
            leftAboard += Math.Max(0, line.Loaded - line.Delivered);
        }
        return $"{leftSource} left at the source, {leftAboard} left aboard";
    }

    /// <summary>
    /// Same approach as a fuel buy. Spawn orbit sits just outside cargo range, so drop to low orbit
    /// when movement has nothing left to fly. Another system takes one jump leg.
    /// </summary>
    internal static bool TryMoveIntoRange(Entity ship, Entity colony, out List<EntityAction> moves, out string reason, Goal goal = null)
    {
        moves = new List<EntityAction>();
        reason = "";

        if (colony.Manager != null && ship.Manager != null && colony.Manager != ship.Manager)
        {
            var leg = JumpRoute.LegToward(ship, colony, goal);
            if (leg.Status == GoalStatus.Failed || leg.Actions == null || leg.Actions.Count == 0)
            {
                reason = string.IsNullOrEmpty(leg.Message) ? "no path" : leg.Message;
                return false;
            }
            moves = new List<EntityAction>(leg.Actions);
            return true;
        }

        if (!MovePlanner.TryBuildMoveActions(ship, colony, out moves, out reason, goal))
            return false;
        if (moves.Count > 0)
            return true;

        var body = colony;
        if (colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            body = info.PlanetEntity;
        if (ship.GetSOIParentEntity() != body)
        {
            reason = string.IsNullOrEmpty(reason) ? "out of range" : reason;
            return false;
        }

        double parking = OrbitMath.LowOrbitRadius(body);
        if (!ChangeOrbitalAltitudeAction.TryPlanBurns(ship, parking, ship.StarSysDateTime, out var burns, out var burnReason)
            || burns.Count == 0)
        {
            reason = string.IsNullOrEmpty(burnReason) ? "out of range" : burnReason;
            return false;
        }

        moves = new List<EntityAction>
        {
            ChangeOrbitalAltitudeAction.CreateCommand(ship, parking, ship.StarSysDateTime),
        };
        return true;
    }

    internal static bool TryColony(Entity ship, int id, out Entity colony, out string reason)
    {
        colony = null!;
        reason = "";
        if (ship.Manager == null || !ship.Manager.TryGetGlobalEntityById(id, out colony))
        {
            reason = "the colony is gone.";
            return false;
        }
        if (!colony.HasDataBlob<ColonyInfoDB>() || !colony.HasDataBlob<CargoStorageDB>())
        {
            reason = "The colony has no warehouse.";
            return false;
        }
        if (colony.FactionOwnerID != ship.FactionOwnerID)
        {
            reason = "The colony is not yours.";
            return false;
        }
        return true;
    }

    /// <summary>
    /// Earlier lines get the hold first. Free volume does not shrink inside one constructor call,
    /// so this clamps the batch before escrow. A zero volume per unit takes nothing.
    /// </summary>
    internal static List<(ICargoable item, long amount)> ClampLines(
        Entity source, Entity receiver, IReadOnlyList<MoveGoodsLine> lines, bool useLoaded)
    {
        var picked = new List<(ICargoable item, long amount)>();
        if (!source.TryGetDataBlob<CargoStorageDB>(out var fromStore)
            || !receiver.TryGetDataBlob<CargoStorageDB>(out var toStore))
            return picked;

        var room = new Dictionary<string, double>();
        foreach (var line in lines)
        {
            long cap = useLoaded ? Math.Max(0, line.Loaded - line.Delivered) : line.Requested;
            if (cap <= 0 || line.ItemId == 0)
                continue;
            if (!TryCargo(source, line.ItemId, out var cargo) || cargo.VolumePerUnit <= 0)
                continue;
            long stock = fromStore.GetUnitsStored(cargo, false);
            long take = Fit(toStore, cargo, room, Math.Min(cap, Math.Max(0, stock)));
            if (take > 0)
                picked.Add((cargo, take));
        }
        return picked;
    }

    static long Fit(CargoStorageDB receiver, ICargoable cargo, Dictionary<string, double> room, long want)
    {
        if (want <= 0 || cargo.VolumePerUnit <= 0)
            return 0;
        if (!receiver.TypeStores.ContainsKey(cargo.CargoTypeID))
            return 0;
        if (!room.TryGetValue(cargo.CargoTypeID, out var left))
        {
            left = receiver.GetFreeVolume(cargo);
            room[cargo.CargoTypeID] = left;
        }
        long byVolume = left <= 0 ? 0 : (long)Math.Floor(left / cargo.VolumePerUnit);
        long byStore = Math.Max(0, receiver.GetFreeUnitSpace(cargo));
        long take = Math.Min(want, Math.Min(byVolume, byStore));
        room[cargo.CargoTypeID] = left - take * cargo.VolumePerUnit;
        return take;
    }

    internal static bool TryCargo(Entity entity, int itemId, out ICargoable cargo)
    {
        cargo = null!;
        if (!entity.TryGetDataBlob<CargoStorageDB>(out var storage))
            return false;
        foreach (var store in storage.TypeStores.Values)
        {
            if (store.GetCargoables().TryGetValue(itemId, out var found))
            {
                cargo = found;
                return true;
            }
        }
        return false;
    }

    internal static void NoteMoved(CargoTransferOrder order, List<MoveGoodsLine> lines, bool loading)
    {
        foreach (var line in lines)
        {
            long moved = order.Taken(line.ItemId);
            if (loading)
                line.Loaded = moved;
            else
                line.Delivered = moved;
        }
    }

    internal static List<MoveGoodsLine> CopyLines(IReadOnlyList<MoveGoodsLine> lines)
    {
        var copy = new List<MoveGoodsLine>(lines.Count);
        foreach (var line in lines)
        {
            copy.Add(new MoveGoodsLine
            {
                ItemId = line.ItemId,
                Requested = line.Requested,
                Loaded = line.Loaded,
                Delivered = line.Delivered,
            });
        }
        return copy;
    }
}

/// <summary>
/// The fleet does not fly. One free captained cargo ship takes the whole list.
/// </summary>
public class FleetMoveGoodsPlan : IGoalPlanner
{
    public GoalType Type => GoalType.FleetMoveGoods;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (!managedEntity.TryGetDataBlob<FleetDB>(out var fleet))
            return PlanResult.Fail("We have no subordinates to manage");

        if (managedEntity.TryGetDataBlob<GoalsDB>(out var goals)
            && GoalWeighting.ShouldInterruptForRefuel(goals, goal))
            return PlanResult.Fail("low on fuel");

        Goal done = null;
        foreach (var child in fleet.Children)
        {
            if (!child.TryGetDataBlob<GoalsDB>(out var childGoals) || childGoals.ActiveGoal == null)
                continue;
            var active = childGoals.ActiveGoal;
            if (active.ParentGoalId != goal.Id)
                continue;
            if (active.Status == GoalStatus.Failed)
                return PlanResult.Continue(new List<(Entity, Goal)>());
            if (active.Status != GoalStatus.Completed)
                return PlanResult.Continue(new List<(Entity, Goal)>());
            done = active;
        }
        if (done != null)
            return PlanResult.Done(string.IsNullOrEmpty(done.Message) ? MoveGoodsPlan.Leftover(done) : done.Message);

        if (!MoveGoodsPlan.TryColony(managedEntity, goal.SourceEntityId, out var source, out var reason)
            || !MoveGoodsPlan.TryColony(managedEntity, goal.DestEntityId, out var dest, out reason))
            return PlanResult.Fail(reason);

        bool anyShip = false;
        bool anyCargoHull = false;
        Entity best = null;
        double bestDistance = double.MaxValue;
        foreach (var child in fleet.Children)
        {
            if (!child.HasDataBlob<ShipInfoDB>())
                continue;
            anyShip = true;
            bool drive = child.HasDataBlob<WarpAbilityDB>() || child.HasDataBlob<NewtonThrustAbilityDB>();
            if (!child.HasDataBlob<CargoStorageDB>() || !drive)
                continue;
            anyCargoHull = true;
            if (child.GetDataBlob<ShipInfoDB>().CommanderID < 0)
                continue;
            if (FleetChildDuty.WorkingThisGoal(child, goal) || FleetChildDuty.BusyWithOwnWork(child, goal))
                continue;
            if (!JumpRoute.CanReach(child, source) || !JumpRoute.CanReach(child, dest))
                continue;

            double distance = MarketRun.DistanceMeters(child, source);
            if (best != null && (distance > bestDistance || (distance == bestDistance && child.Id > best.Id)))
                continue;
            best = child;
            bestDistance = distance;
        }

        if (!anyShip || !anyCargoHull)
            return PlanResult.Fail("We have no subordinates to manage");
        if (best == null)
            return PlanResult.Fail("no ship");

        var childGoal = new Goal(GoalType.MoveGoods)
        {
            Name = "Move goods",
            ParentGoalId = goal.Id,
            SourceEntityId = source.Id,
            DestEntityId = dest.Id,
            MoveGoodsLines = MoveGoodsPlan.CopyLines(goal.MoveGoodsLines),
        };
        return PlanResult.Continue(new List<(Entity, Goal)> { (best, childGoal) });
    }
}
