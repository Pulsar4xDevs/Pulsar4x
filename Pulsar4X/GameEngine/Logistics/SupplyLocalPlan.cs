using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Movement;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// One colony posts its own book and hands <see cref="GoalType.RunMarket"/> to owned offices
/// its command bridge can see. Balance and Stockpile edit policy before that hand-off.
/// </summary>
public class SupplyLocalPlan : IGoalPlanner
{
    public GoalType Type => GoalType.SupplyLocal;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        var members = Members(managedEntity);
        var notes = new List<string>();
        if (goal.SupplyMode == SupplyMode.Balance)
            Balance(members, notes);
        else if (goal.SupplyMode == SupplyMode.Stockpile)
            Stockpile(managedEntity, members);

        var subgoals = new List<(Entity Sub, Goal Goal)>();
        foreach (var member in members)
        {
            if (member.Id == managedEntity.Id)
                continue;
            if (LeaveAlone(member, goal))
                continue;
            if (!HasPolicy(member))
                continue;
            subgoals.Add((member, new Goal(GoalType.RunMarket) { ParentGoalId = goal.Id }));
        }

        var market = new RunMarketPlan().Plan(managedEntity, goal, atDateTime);
        return new PlanResult
        {
            Status = GoalStatus.Active,
            Message = Join(market.Message, notes),
            Actions = market.Actions,
            SubGoals = subgoals,
        };
    }

    static List<Entity> Members(Entity root)
    {
        var members = new List<Entity>();
        if (root.Manager != null && root.HasDataBlob<ColonyInfoDB>())
        {
            foreach (var colony in root.Manager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            {
                if (colony.FactionOwnerID != root.FactionOwnerID)
                    continue;
                if (!colony.HasDataBlob<LogiBaseDB>())
                    continue;
                members.Add(colony);
            }
        }

        if (members.All(colony => colony.Id != root.Id))
            members.Add(root);

        var span = CommandSpan.Of(root);
        Entity anchor = Anchor(root);
        return members.Where(colony => InSpan(colony, root, anchor, span)).ToList();
    }

    static Entity Anchor(Entity root)
    {
        if (root.TryGetDataBlob<ColonyInfoDB>(out var info) && info.PlanetEntity.IsValid)
            return info.PlanetEntity;
        return Entity.InvalidEntity;
    }

    static bool InSpan(Entity colony, Entity root, Entity anchor, CommandSpanKind span)
    {
        if (span == CommandSpanKind.System)
            return true;
        if (InBody(colony, root, anchor))
            return true;
        if (span != CommandSpanKind.Well || !anchor.IsValid)
            return false;
        if (!anchor.TryGetDataBlob<PositionDB>(out var position))
            return false;
        if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info) || !info.PlanetEntity.IsValid)
            return false;
        return position.Children.Contains(info.PlanetEntity);
    }

    static bool InBody(Entity colony, Entity root, Entity anchor)
    {
        if (colony.Id == root.Id)
            return true;
        return anchor.IsValid
            && colony.TryGetDataBlob<ColonyInfoDB>(out var info)
            && info.PlanetEntity.IsValid
            && info.PlanetEntity.Id == anchor.Id;
    }

    static void Balance(List<Entity> members, List<string> notes)
    {
        var cargoIds = CargoIds(members);
        cargoIds.Sort(StringComparer.Ordinal);
        foreach (var cargoId in cargoIds)
        {
            bool paired = members.Any(shortSide =>
                IsShort(shortSide, cargoId)
                && members.Any(surplusSide => surplusSide.Id != shortSide.Id && IsSurplus(surplusSide, cargoId)));
            if (!paired)
                continue;
            if (!TryPrice(members, cargoId, out var ask, out var bid))
                continue;

            bool skipped = false;
            foreach (var member in members)
            {
                if (!IsSurplus(member, cargoId) || HasRow(member, cargoId))
                    continue;
                if (!TryAddSellRow(member, cargoId, ask, bid))
                    skipped = true;
            }
            if (skipped)
                notes.Add($"{cargoId} skipped for capacity");
        }
    }

    static void Stockpile(Entity root, List<Entity> members)
    {
        if (!root.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) || policy.Rows.Count == 0)
            return;

        var cargoIds = policy.Rows.Keys.Where(id => !string.IsNullOrEmpty(id)).ToList();
        cargoIds.Sort(StringComparer.Ordinal);
        foreach (var cargoId in cargoIds)
        {
            if (!policy.Rows.TryGetValue(cargoId, out var row))
                continue;
            long surplus = 0;
            foreach (var member in members)
            {
                if (member.Id == root.Id)
                    continue;
                surplus += AboveReserve(member, cargoId);
            }
            if (surplus <= 0)
                continue;

            long next = Math.Max(row.Min, StockOf(root, cargoId) + surplus);
            row.Min = next;
            if (row.Max < row.Min)
                row.Max = row.Min;
        }
    }

    static bool LeaveAlone(Entity colony, Goal parent)
    {
        if (!colony.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return false;
        var active = goals.ActiveGoal;
        if (active.Status is not (GoalStatus.Planning or GoalStatus.Active))
            return false;
        if (active.Type != GoalType.RunMarket)
            return true;
        return active.ParentGoalId == parent.Id;
    }

    static bool HasPolicy(Entity colony)
        => colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) && policy.Rows.Count > 0;

    static List<string> CargoIds(List<Entity> members)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in members)
        {
            if (member.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
            {
                foreach (var key in policy.Rows.Keys)
                {
                    if (!string.IsNullOrEmpty(key))
                        ids.Add(key);
                }
            }
            if (!member.TryGetDataBlob<CargoStorageDB>(out var storage))
                continue;
            foreach (var store in storage.TypeStores.Values)
            {
                foreach (var cargo in store.GetCargoables().Values)
                {
                    if (!string.IsNullOrEmpty(cargo.UniqueID))
                        ids.Add(cargo.UniqueID);
                }
            }
        }
        return ids.ToList();
    }

    static bool IsShort(Entity colony, string cargoId)
        => TryGetRow(colony, cargoId, out var row) && StockOf(colony, cargoId) < row.Min;

    static bool IsSurplus(Entity colony, string cargoId)
        => AboveReserve(colony, cargoId) > 0;

    static long AboveReserve(Entity colony, string cargoId)
    {
        long reserve = TryGetRow(colony, cargoId, out var row) ? row.Min : 0;
        long above = StockOf(colony, cargoId) - reserve;
        return above > 0 ? above : 0;
    }

    static bool TryPrice(List<Entity> members, string cargoId, out decimal ask, out decimal bid)
    {
        ask = 0;
        bid = 0;
        Entity? source = null;
        foreach (var member in members)
        {
            if (!TryGetRow(member, cargoId, out var row))
                continue;
            if (row.Ask == 0 && row.Bid == 0)
                continue;
            if (source != null && member.Id >= source.Id)
                continue;
            source = member;
            ask = row.Ask;
            bid = row.Bid;
        }
        return source != null;
    }

    static bool TryAddSellRow(Entity colony, string cargoId, decimal ask, decimal bid)
    {
        if (!colony.TryGetDataBlob<LogiBaseDB>(out var office))
            return false;
        if (!colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
        {
            policy = new ColonyMarketPolicyDB();
            colony.SetDataBlob(policy);
        }
        if (policy.Rows.ContainsKey(cargoId))
            return true;

        int capacity = office.Capacity < 0 ? 0 : office.Capacity;
        if (policy.Rows.Count >= capacity)
            return false;

        policy.Rows[cargoId] = new MarketPolicyRow
        {
            CargoId = cargoId,
            Min = 0,
            Max = 0,
            AutoProduce = false,
            Ask = ask,
            Bid = bid,
        };
        return true;
    }

    static bool HasRow(Entity colony, string cargoId)
        => TryGetRow(colony, cargoId, out _);

    static bool TryGetRow(Entity colony, string cargoId, out MarketPolicyRow row)
    {
        row = null!;
        if (!colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
            return false;
        return policy.Rows.TryGetValue(cargoId, out row!);
    }

    static long StockOf(Entity colony, string cargoId)
    {
        var library = colony.GetFactionCargoDefinitions();
        if (library == null || !library.Contains(cargoId))
            return 0;
        var cargo = library.GetAny(cargoId);
        if (cargo == null)
            return 0;
        return MarketBook.Stock(colony, cargo);
    }

    static string Join(string market, List<string> notes)
    {
        string extra = notes.Count == 0 ? "" : string.Join("; ", notes);
        if (string.IsNullOrEmpty(market))
            return extra;
        if (string.IsNullOrEmpty(extra))
            return market;
        return market + "; " + extra;
    }
}
