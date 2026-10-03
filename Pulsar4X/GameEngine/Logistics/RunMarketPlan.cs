using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Industry;

namespace Pulsar4X.Logistics;

/// <summary>
/// One pass of <see cref="GoalType.RunMarket"/>. Returns listing and industry actions.
/// Does not write the book and does not enqueue jobs.
/// </summary>
public class RunMarketPlan : IGoalPlanner
{
    public GoalType Type => GoalType.RunMarket;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        // Quality is 1.0 in this plan, including with no administrator. Listings use the policy numbers.
        _ = ColonyAdministrator.Quality(managedEntity);

        if (!managedEntity.TryGetDataBlob<LogiBaseDB>(out var book))
            return PlanResult.Continue(new List<EntityAction>(), "no logistics office");
        if (!managedEntity.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) || policy.Rows.Count == 0)
            return PlanResult.Continue(new List<EntityAction>());

        var info = FactionOf(managedEntity);
        var ranked = new List<RowWork>();
        foreach (var (key, row) in policy.Rows)
        {
            string cargoId = string.IsNullOrEmpty(row.CargoId) ? key : row.CargoId;
            if (string.IsNullOrEmpty(cargoId))
                continue;

            long stock = StockOf(managedEntity, info, cargoId);
            long needed = Needed(managedEntity, cargoId);
            long queued = AlreadyQueued(managedEntity, cargoId);
            long hold = Math.Max(row.Min, needed);
            ranked.Add(new RowWork
            {
                Row = row,
                CargoId = cargoId,
                Stock = stock,
                Queued = queued,
                Hold = hold,
                Deficit = hold - stock,
                Surplus = Math.Max(0, stock - hold),
            });
        }

        ranked.Sort(static (a, b) =>
        {
            int byDeficit = b.Deficit.CompareTo(a.Deficit);
            if (byDeficit != 0)
                return byDeficit;
            int bySurplus = b.Surplus.CompareTo(a.Surplus);
            if (bySurplus != 0)
                return bySurplus;
            return string.CompareOrdinal(a.CargoId, b.CargoId);
        });

        int capacity = book.Capacity < 0 ? 0 : book.Capacity;
        var actions = new List<EntityAction>();
        var notes = new List<string>();

        for (int i = 0; i < ranked.Count; i++)
        {
            var item = ranked[i];
            if (i >= capacity)
            {
                notes.Add($"{item.CargoId} skipped for capacity");
                continue;
            }

            bool needsJob = item.Row.AutoProduce && item.Queued < item.Row.Max && item.Stock < item.Row.Max - item.Queued;
            if (needsJob)
            {
                if (TryFindLine(managedEntity, info, item.CargoId, out var lineId))
                {
                    long gap = item.Row.Max - item.Stock - item.Queued;
                    ushort count = (ushort)Math.Min(gap, ushort.MaxValue);
                    var job = new IndustryJob(info!, item.CargoId);
                    job.InitialiseJob(count, false);
                    var order = IndustryOrder2.CreateNewJobOrder(managedEntity.FactionOwnerID, managedEntity, lineId, job);
                    order.AutoAddSubJobs = false;
                    order.ActionOnDate = managedEntity.StarSysDateTime;
                    actions.Add(order);
                }
                else
                {
                    notes.Add($"{item.CargoId} has no production line");
                }
            }

            var desired = new MarketListing
            {
                CargoId = item.CargoId,
                SellQuantity = Math.Max(0, item.Stock - item.Hold),
                Ask = item.Row.Ask,
                BuyQuantity = Math.Max(0, item.Hold - item.Stock),
                Bid = item.Row.Bid,
                Reserve = item.Row.Min,
            };
            if (!ListingMatches(managedEntity, desired))
                actions.Add(PostMarketListingAction.Create(managedEntity, desired));
        }

        return PlanResult.Continue(actions, notes.Count == 0 ? "" : string.Join("; ", notes));
    }

    static bool ListingMatches(Entity colony, MarketListing desired)
    {
        if (!MarketBook.TryGet(colony, desired.CargoId, out var existing))
            return false;
        return existing.SellQuantity == desired.SellQuantity
            && existing.Ask == desired.Ask
            && existing.BuyQuantity == desired.BuyQuantity
            && existing.Bid == desired.Bid
            && existing.Reserve == desired.Reserve;
    }

    static bool TryFindLine(Entity colony, FactionInfoDB? info, string cargoId, out string lineId)
    {
        lineId = "";
        if (info == null || !info.IndustryDesigns.TryGetValue(cargoId, out var design))
            return false;
        if (string.IsNullOrEmpty(design.IndustryTypeID))
            return false;
        if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
            return false;

        foreach (var (id, line) in industry.ProductionLines.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (line.IndustryTypeRates != null && line.IndustryTypeRates.ContainsKey(design.IndustryTypeID))
            {
                lineId = id;
                return true;
            }
        }
        return false;
    }

    static long Needed(Entity colony, string cargoId)
    {
        if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
            return 0;
        long sum = 0;
        foreach (var line in industry.ProductionLines.Values)
        {
            foreach (var job in line.Jobs)
            {
                if (job.ResourcesRequiredRemaining != null
                    && job.ResourcesRequiredRemaining.TryGetValue(cargoId, out var amount))
                    sum += amount;
            }
        }
        return sum;
    }

    static long AlreadyQueued(Entity colony, string cargoId)
    {
        if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
            return 0;
        long sum = 0;
        foreach (var line in industry.ProductionLines.Values)
        {
            foreach (var job in line.Jobs)
            {
                if (job.ItemGuid == cargoId)
                    sum += job.NumberOrdered - job.NumberCompleted;
            }
        }
        return sum;
    }

    static long StockOf(Entity colony, FactionInfoDB? info, string cargoId)
    {
        if (info == null || !info.Data.CargoGoods.Contains(cargoId))
            return 0;
        var cargo = info.Data.CargoGoods.GetAny(cargoId);
        if (cargo == null)
            return 0;
        return MarketBook.Stock(colony, cargo);
    }

    static FactionInfoDB? FactionOf(Entity colony)
    {
        if (colony.Manager is not { Game: { } game })
            return null;
        if (!game.Factions.TryGetValue(colony.FactionOwnerID, out var faction))
            return null;
        return faction.TryGetDataBlob<FactionInfoDB>(out var info) ? info : null;
    }

    sealed class RowWork
    {
        public MarketPolicyRow Row = null!;
        public string CargoId = "";
        public long Stock;
        public long Queued;
        public long Hold;
        public long Deficit;
        public long Surplus;
    }
}
