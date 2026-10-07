using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Logistics;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry;

/// <summary>
/// Mine surveyed asteroids in the current system, then haul the ore home.
/// Home is the nearest owned colony, by travel time, that can take a carried mineral.
/// If none can, sell at the nearest friendly colony that is buying.
/// </summary>
public class MineAsteroidsPlan : IGoalPlanner
{
    public GoalType Type => GoalType.MineAsteroids;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (managedEntity.HasDataBlob<FleetDB>())
            return PlanSubGoals(managedEntity, goal);
        if (managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanActions(managedEntity, goal);
        return PlanResult.Fail("Non supported entity");
    }

    public PlanResult PlanActions(Entity ship, Goal goal)
    {
        if (!ship.HasDataBlob<AsteroidMineAbilityDB>())
            return PlanResult.Fail("no asteroid miner");
        if (!ship.TryGetDataBlob<ActionQueueDB>(out var queue))
            return PlanResult.Fail("no action queue");

        var existing = queue.ActionsFor(goal);
        if (existing.Count > 0)
        {
            if (existing.Exists(a => a.Status == ActionStatus.Failed))
                return PlanResult.Fail("a mining action failed");
            if (existing.Exists(a => a.Status != ActionStatus.Succeeded))
                return PlanResult.Continue(new List<EntityAction>());
        }

        var rock = ResolveRock(ship, goal.TargetEntityID);
        if (rock != null && AsteroidMineOrder.CanTakeMore(ship, rock))
            return PlanMine(ship, goal, rock);

        if (HoldHasOre(ship))
        {
            if (rock == null || !RockHasOre(rock))
                goal.TargetEntityID = -1;
            return PlanReturn(ship, goal);
        }

        goal.TargetEntityID = -1;
        var next = NearestRock(ship, ClaimedRocks(ship), out var reason);
        if (next == null)
        {
            if (reason == "no warp")
                return PlanResult.Fail("no warp");
            return PlanResult.Done("nothing left to mine");
        }

        return PlanMine(ship, goal, next);
    }

    /// <summary>
    /// One rock per free child that carries a miner. The child keeps that goal and
    /// picks the next rock itself after it unloads.
    /// </summary>
    public PlanResult PlanSubGoals(Entity fleet, Goal goal)
    {
        if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB))
            return PlanResult.Fail("We have no subordinates to manage");

        var claimed = ClaimedRocks(fleet);
        var freeShips = new List<Entity>();
        int working = 0;

        foreach (var subunit in fleetDB.Children)
        {
            if (!subunit.HasDataBlob<AsteroidMineAbilityDB>())
                continue;
            if (!MovePlanner.CanMove(subunit, out _))
                continue;
            if (FleetChildDuty.WorkingThisGoal(subunit, goal))
            {
                working++;
                if (subunit.TryGetDataBlob<GoalsDB>(out var childGoals) && childGoals.ActiveGoal != null)
                    claimed.Add(childGoals.ActiveGoal.TargetEntityID);
                continue;
            }
            if (FleetChildDuty.BusyWithOwnWork(subunit, goal))
                continue;
            freeShips.Add(subunit);
        }

        var subGoals = new List<(Entity subordinate, Goal goal)>();
        foreach (var ship in freeShips)
        {
            var rock = NearestRock(ship, claimed, out _);
            if (rock == null)
                continue;
            claimed.Add(rock.Id);
            subGoals.Add((ship, new Goal(GoalType.MineAsteroids)
            {
                ParentGoalId = goal.Id,
                TargetEntityID = rock.Id,
            }));
        }

        if (subGoals.Count > 0)
            return PlanResult.Continue(subGoals);
        if (working > 0)
            return PlanResult.Continue(new List<(Entity subordinate, Goal goal)>());
        if (freeShips.Count == 0 && working == 0)
            return PlanResult.Fail("no asteroid miner");
        return PlanResult.Done("nothing left to mine");
    }

    static PlanResult PlanMine(Entity ship, Goal goal, Entity rock)
    {
        goal.TargetEntityID = rock.Id;
        goal.DestEntityId = -1;

        if (!MovePlanner.TryBuildMoveActions(ship, rock, out var moveActions, out var moveReason))
            return PlanResult.Fail(string.IsNullOrEmpty(moveReason) ? "cannot reach the asteroid" : moveReason);

        moveActions.Add(new AsteroidMineOrder(ship, rock));
        string name = rock.GetOwnersName();
        return PlanResult.Continue(moveActions, moveActions.Count > 1 ? $"Moving to {name}" : $"Mining {name}");
    }

    static PlanResult PlanReturn(Entity ship, Goal goal)
    {
        if (!TryDestination(ship, goal, out var dest, out var selling, out var reason))
        {
            if (reason == "no warp")
                return PlanResult.Fail("no warp");
            return PlanResult.Continue(
                new List<EntityAction> { new UnloadWaitOrder(ship) },
                "no place to unload");
        }

        goal.DestEntityId = dest.Id;
        if (!MovePlanner.TryBuildMoveActions(ship, dest, out var moveActions, out var moveReason))
            return PlanResult.Fail(string.IsNullOrEmpty(moveReason) ? "cannot reach the colony" : moveReason);

        if (moveActions.Count > 0)
        {
            string verb = selling ? "Selling at" : "Unloading at";
            return PlanResult.Continue(moveActions, $"{verb} {dest.GetOwnersName()}");
        }

        if (!selling)
            return PlanUnload(ship, dest);

        var sales = SellActions(ship, dest);
        if (sales.Count == 0)
        {
            goal.DestEntityId = -1;
            return PlanResult.Continue(
                new List<EntityAction> { new UnloadWaitOrder(ship) },
                "no place to unload");
        }

        return PlanResult.Continue(sales, $"Selling at {dest.GetOwnersName()}");
    }

    static PlanResult PlanUnload(Entity ship, Entity colony)
    {
        var items = UnloadAmounts(ship, colony);
        if (items.Count == 0)
        {
            return PlanResult.Continue(
                new List<EntityAction> { new UnloadWaitOrder(ship) },
                "no place to unload");
        }

        var (primary, secondary) = CargoTransferOrder.CreateUnloadPair(
            ship.FactionOwnerID, ship, colony, items);
        ship.Manager.Game.OrderHandler.HandleOrder(secondary);
        return PlanResult.Continue(
            new List<EntityAction> { primary },
            $"Unloading at {colony.GetOwnersName()}");
    }

    static bool TryDestination(Entity ship, Goal goal, out Entity dest, out bool selling, out string reason)
    {
        dest = null!;
        selling = false;
        reason = "";

        if (ship.Manager.TryGetGlobalEntityById(goal.DestEntityId, out var pinned)
            && DestinationStillGood(ship, pinned, out selling))
        {
            dest = pinned;
            return true;
        }

        var owned = new List<Entity>();
        foreach (var colony in Colonies(ship))
        {
            if (colony.FactionOwnerID == ship.FactionOwnerID && CanStoreAny(ship, colony))
                owned.Add(colony);
        }

        if (TravelTime.TryNearest(ship, owned, out dest, out _, out reason))
        {
            selling = false;
            return true;
        }

        if (reason == "no warp")
            return false;

        var buyers = new List<Entity>();
        foreach (var colony in Colonies(ship))
        {
            if (colony.FactionOwnerID == ship.FactionOwnerID)
                continue;
            if (CanSellAny(ship, colony))
                buyers.Add(colony);
        }

        if (TravelTime.TryNearest(ship, buyers, out dest, out _, out reason))
        {
            selling = true;
            return true;
        }

        return false;
    }

    static bool DestinationStillGood(Entity ship, Entity colony, out bool selling)
    {
        selling = false;
        if (!colony.HasDataBlob<ColonyInfoDB>())
            return false;
        if (colony.FactionOwnerID == ship.FactionOwnerID)
            return CanStoreAny(ship, colony);

        if (!CanSellAny(ship, colony))
            return false;
        selling = true;
        return true;
    }

    static Entity? NearestRock(Entity ship, HashSet<int> claimed, out string reason)
    {
        var rocks = new List<Entity>();
        if (ship.Manager != null)
        {
            foreach (var body in ship.Manager.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>())
            {
                if (!IsMineableRock(ship, body) || claimed.Contains(body.Id))
                    continue;
                if (!AsteroidMineOrder.CanTakeMore(ship, body))
                    continue;
                rocks.Add(body);
            }
        }

        if (rocks.Count == 0)
        {
            reason = "";
            return null;
        }

        if (!TravelTime.TryNearest(ship, rocks, out var nearest, out _, out reason))
            return null;
        return nearest;
    }

    static HashSet<int> ClaimedRocks(Entity self)
    {
        var claimed = new HashSet<int>();
        var game = self.Manager?.Game;
        if (game == null)
            return claimed;

        foreach (var system in game.Systems)
        {
            foreach (var entity in system.GetAllEntitiesWithDataBlob<GoalsDB>())
            {
                if (entity.Id == self.Id || entity.FactionOwnerID != self.FactionOwnerID)
                    continue;
                if (!entity.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
                    continue;
                var active = goals.ActiveGoal;
                if (active.Type != GoalType.MineAsteroids)
                    continue;
                if (active.Status is GoalStatus.Completed or GoalStatus.Failed)
                    continue;
                if (active.TargetEntityID > 0)
                    claimed.Add(active.TargetEntityID);
            }
        }

        return claimed;
    }

    static Entity? ResolveRock(Entity ship, int id)
    {
        if (id <= 0 || ship.Manager == null || !ship.Manager.TryGetGlobalEntityById(id, out var body))
            return null;
        if (!IsMineableRock(ship, body) || !RockHasOre(body))
            return null;
        return body;
    }

    static bool IsMineableRock(Entity ship, Entity body)
    {
        if (!body.TryGetDataBlob<SystemBodyInfoDB>(out var info) || info.BodyType != BodyType.Asteroid)
            return false;
        if (!body.TryGetDataBlob<GeoSurveyableDB>(out var survey) || !survey.IsSurveyComplete(ship.FactionOwnerID))
            return false;
        return body.HasDataBlob<MineralsDB>();
    }

    static bool RockHasOre(Entity rock)
    {
        if (!rock.TryGetDataBlob<MineralsDB>(out var minerals))
            return false;
        foreach (var deposit in minerals.Minerals.Values)
        {
            if (deposit != null && deposit.Amount.Actual > 0)
                return true;
        }
        return false;
    }

    static bool HoldHasOre(Entity ship)
    {
        return CarriedMinerals(ship).Count > 0;
    }

    static List<(Mineral mineral, long units)> CarriedMinerals(Entity ship)
    {
        var carried = new List<(Mineral mineral, long units)>();
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var cargo))
            return carried;
        var library = ship.GetFactionCargoDefinitions();
        if (library == null)
            return carried;

        foreach (var store in cargo.TypeStores.Values)
        {
            foreach (var (id, units) in store.CurrentStoreInUnits)
            {
                if (units <= 0)
                    continue;
                if (!library.GetMinerals().TryGetValue(id, out var mineral) || mineral == null)
                    continue;
                carried.Add((mineral, units));
            }
        }

        return carried;
    }

    static bool CanStoreAny(Entity ship, Entity colony)
    {
        if (!colony.TryGetDataBlob<CargoStorageDB>(out var cargo))
            return false;
        foreach (var (mineral, _) in CarriedMinerals(ship))
        {
            if (CargoMath.GetFreeUnitSpace(cargo, mineral) >= 1)
                return true;
        }
        return false;
    }

    static bool CanSellAny(Entity ship, Entity colony)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>() || !colony.HasDataBlob<LogiBaseDB>())
            return false;
        if (!FactionStanceRules.CanTrade(ship.Manager.Game, ship.FactionOwnerID, colony.FactionOwnerID))
            return false;

        foreach (var (mineral, _) in CarriedMinerals(ship))
        {
            if (string.IsNullOrEmpty(mineral.UniqueID))
                continue;
            if (!MarketBook.TryGet(colony, mineral.UniqueID, out var listing) || listing.BuyQuantity <= 0)
                continue;
            if (!colony.TryGetDataBlob<CargoStorageDB>(out var cargo))
                continue;
            if (CargoMath.GetFreeUnitSpace(cargo, mineral) >= 1)
                return true;
        }
        return false;
    }

    static List<(ICargoable item, long amount)> UnloadAmounts(Entity ship, Entity colony)
    {
        var items = new List<(ICargoable item, long amount)>();
        if (!colony.TryGetDataBlob<CargoStorageDB>(out var cargo))
            return items;
        foreach (var (mineral, units) in CarriedMinerals(ship))
        {
            long free = CargoMath.GetFreeUnitSpace(cargo, mineral);
            long move = units < free ? units : free;
            if (move <= 0)
                continue;
            items.Add((mineral, -move));
        }
        return items;
    }

    static List<EntityAction> SellActions(Entity ship, Entity colony)
    {
        var actions = new List<EntityAction>();
        if (!colony.TryGetDataBlob<CargoStorageDB>(out var cargo))
            return actions;
        foreach (var (mineral, units) in CarriedMinerals(ship))
        {
            if (string.IsNullOrEmpty(mineral.UniqueID))
                continue;
            if (!MarketBook.TryGet(colony, mineral.UniqueID, out var listing) || listing.BuyQuantity <= 0)
                continue;
            long free = CargoMath.GetFreeUnitSpace(cargo, mineral);
            long move = units;
            if (move > listing.BuyQuantity)
                move = listing.BuyQuantity;
            if (move > free)
                move = free;
            if (move <= 0)
                continue;
            actions.Add(MarketExchangeAction.Create(
                ship, colony.Id, mineral.UniqueID, MarketSide.SellToMarket, move));
        }
        return actions;
    }

    static IEnumerable<Entity> Colonies(Entity ship)
    {
        var game = ship.Manager?.Game;
        if (game == null)
            yield break;
        foreach (var system in game.Systems)
        {
            foreach (var colony in system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
                yield return colony;
        }
    }
}
