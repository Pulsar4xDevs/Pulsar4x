using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Standing colony job. Adds a policy row for warehouse stock that has none,
/// then returns that colony's RunMarket actions. Cargo has no catalog price, so a new row asks and bids 0.
/// </summary>
public class OfferStockPlan : IGoalPlanner
{
    public GoalType Type => GoalType.OfferStock;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        var notes = new List<string>();
        var actions = new List<EntityAction>();

        var industry = new RunIndustryPlan().Plan(managedEntity, goal, atDateTime);
        actions.AddRange(industry.Actions);
        if (!string.IsNullOrEmpty(industry.Message))
            notes.Add(industry.Message);

        if (managedEntity.HasDataBlob<LogiBaseDB>())
            Offer(managedEntity, notes);

        var market = new RunMarketPlan().Plan(managedEntity, goal, atDateTime);
        actions.AddRange(market.Actions);
        return new PlanResult
        {
            Status = GoalStatus.Active,
            Message = Join(market.Message, notes),
            Actions = actions,
            SubGoals = Array.Empty<(Entity, Goal)>(),
        };
    }

    static void Offer(Entity colony, List<string> notes)
    {
        if (!colony.TryGetDataBlob<LogiBaseDB>(out var office))
            return;

        var piles = Piles(colony);
        piles.Sort(static (a, b) =>
        {
            int byStock = b.Stock.CompareTo(a.Stock);
            if (byStock != 0)
                return byStock;
            return string.CompareOrdinal(a.Id, b.Id);
        });

        int capacity = office.Capacity < 0 ? 0 : office.Capacity;
        int skipped = 0;
        foreach (var pile in piles)
        {
            if (HasRow(colony, pile.Id))
                continue;
            if (!TryAddRow(colony, pile.Id, capacity))
                skipped++;
        }
        if (skipped > 0)
            notes.Add($"{skipped} skipped for capacity");
    }

    static List<(string Id, long Stock)> Piles(Entity colony)
    {
        var piles = new List<(string Id, long Stock)>();
        if (!colony.TryGetDataBlob<CargoStorageDB>(out var storage))
            return piles;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var store in storage.TypeStores.Values)
        {
            foreach (var cargo in store.GetCargoables().Values)
            {
                if (string.IsNullOrEmpty(cargo.UniqueID) || !seen.Add(cargo.UniqueID))
                    continue;
                long stock = StockOf(colony, cargo.UniqueID);
                if (stock > 0)
                    piles.Add((cargo.UniqueID, stock));
            }
        }
        return piles;
    }

    static bool TryAddRow(Entity colony, string cargoId, int capacity)
    {
        if (!colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
        {
            policy = new ColonyMarketPolicyDB();
            colony.SetDataBlob(policy);
        }
        if (policy.Rows.ContainsKey(cargoId))
            return true;
        if (policy.Rows.Count >= capacity)
            return false;

        policy.Rows[cargoId] = new MarketPolicyRow
        {
            CargoId = cargoId,
            Min = 0,
            Max = 0,
            AutoProduce = false,
            Ask = 0,
            Bid = 0,
        };
        return true;
    }

    static bool HasRow(Entity colony, string cargoId)
        => colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) && policy.Rows.ContainsKey(cargoId);

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
