using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Per-colony, per-item warehouse rules (<see cref="ColonyStockRulesDB"/>).
/// Min: keep at least this much. It is held back from sale, and with the industry seat filled
/// (<see cref="IndustrySeat.HasHead"/>) production is queued toward it each day, counting jobs already queued as supply.
/// Max: sell if above. With the market seat filled (<see cref="MarketSeat.ManagesMarket"/>) the market lists the
/// stock above it for sale (<see cref="RunMarketPlan"/>).
/// </summary>
public static class StockRules
{
    /// <summary>The rule for this item, or null.</summary>
    public static StockRule? Get(Entity colony, string cargoId)
        => colony.TryGetDataBlob<ColonyStockRulesDB>(out var db) && db.Rules.TryGetValue(cargoId, out var rule) ? rule : null;

    /// <summary>Why this rule cannot be set, or null when it can. Both bounds null is a clear and is fine.</summary>
    public static string? Refusal(Entity colony, string? cargoId, long? min, long? max)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>())
            return "Stock rules belong to a colony.";
        if (string.IsNullOrEmpty(cargoId))
            return "Cargo id is required.";
        if (min < 0 || max < 0)
            return "Min and max cannot be negative.";
        if (min != null && max != null && max < min)
            return "Max cannot be below min.";
        if (min == null && max == null)
            return null;
        if (Item(colony, cargoId) == null)
            return $"Unknown item '{cargoId}'.";
        return null;
    }

    /// <summary>Sets (or, with both bounds null, clears) the item's rule. Returns the refusal, or null when done.</summary>
    public static string? Set(Entity colony, string cargoId, long? min, long? max)
    {
        var refusal = Refusal(colony, cargoId, min, max);
        if (refusal != null)
            return refusal;
        if (min == null && max == null)
        {
            Clear(colony, cargoId);
            return null;
        }
        if (!colony.TryGetDataBlob<ColonyStockRulesDB>(out var db))
        {
            db = new ColonyStockRulesDB();
            colony.SetDataBlob(db);
        }
        bool hadMax = db.Rules.TryGetValue(cargoId, out var old) && old.Max != null;
        db.Rules[cargoId] = new StockRule { CargoId = cargoId, Min = min, Max = max };
        // A dropped Max no longer sells the surplus: its auto-priced row goes too (the next market pass re-adds one if due).
        if (hadMax && max == null)
            WithdrawAutoRow(colony, cargoId);
        return null;
    }

    /// <summary>
    /// Removes the item's rule. Its auto-priced market row and listing are withdrawn, and the jobs
    /// <see cref="QueueTowardMin"/> queued for it are cancelled while not started (<see cref="NotStarted"/>).
    /// A missing rule is fine. Returns the number of jobs cancelled.
    /// </summary>
    public static int Clear(Entity colony, string cargoId)
    {
        if (colony.TryGetDataBlob<ColonyStockRulesDB>(out var db))
            db.Rules.Remove(cargoId);
        WithdrawAutoRow(colony, cargoId);
        return CancelMinJobs(colony, cargoId);
    }

    /// <summary>Drops the item's auto-priced market row and its listing. The player's own priced row stays.</summary>
    static void WithdrawAutoRow(Entity colony, string cargoId)
    {
        if (!colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy)
            || !policy.Rows.TryGetValue(cargoId, out var row) || !row.AutoPriced)
            return;
        policy.Rows.Remove(cargoId);
        MarketBook.RemoveListing(colony, cargoId);
    }

    /// <summary>True for a job with nothing built, spent or consumed yet: cancelling it loses nothing.</summary>
    internal static bool NotStarted(IndustryJob job)
        => job.NumberCompleted == 0
           && job.ProductionPointsLeft >= job.ProductionPointsCost
           && job.ResourcesRequiredRemaining.Values.Sum() >= (job.ResourcesCosts?.Values.Sum() ?? 0)
           && job.EnergyUsed <= 0;

    /// <summary>Cancels this colony's not-started jobs queued for the item's Min rule. Feeder jobs are left: other jobs may count on them.</summary>
    static int CancelMinJobs(Entity colony, string cargoId)
    {
        if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
            return 0;
        int cancelled = 0;
        foreach (var (lineId, line) in industry.ProductionLines)
        {
            foreach (var job in line.Jobs.Where(j => j.StockRuleCargoId == cargoId && NotStarted(j)).ToList())
            {
                IndustryTools.CancelExsistingJob(colony, lineId, job.JobID);
                ShipLogBook.NoteIndustryChange(colony, $"Cancelled {job.Name} x{job.NumberOrdered}: its warehouse minimum was cleared");
                cancelled++;
            }
        }
        return cancelled;
    }

    /// <summary>
    /// The industry head's work for the Min rules: for each item below its Min (stock plus what queued jobs will make),
    /// one job for the gap goes on the fastest line that builds it, with its feeders. Nothing without a seated
    /// industry head. Returns the number of jobs queued.
    /// </summary>
    public static int QueueTowardMin(Entity colony)
    {
        if (!colony.TryGetDataBlob<ColonyStockRulesDB>(out var db) || db.Rules.Count == 0
            || !IndustrySeat.HasHead(colony)
            || !colony.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            || !colony.TryGetDataBlob<CargoStorageDB>(out var storage)
            || Info(colony) is not { } info)
            return 0;
        int queued = 0;
        foreach (var rule in db.Rules.Values.OrderBy(r => r.CargoId, StringComparer.Ordinal).ToList())
        {
            if (rule.Min is not long min || min <= 0)
                continue;
            if (!info.IndustryDesigns.TryGetValue(rule.CargoId, out var design) || design == null)
                continue; // nothing here builds it
            var item = Item(colony, rule.CargoId);
            if (item == null)
                continue;
            long toMake = min - storage.GetUnitsStoredByDesign(item, false) - IndustryTools.QueuedOutput(colony, design);
            if (toMake <= 0)
                continue;
            long perBatch = IndustryTools.OutputPerItem(design);
            var job = new IndustryJob(design) { StockRuleCargoId = rule.CargoId };
            job.InitialiseJob((ushort)Math.Min((toMake + perBatch - 1) / perBatch, ushort.MaxValue), false);
            if (!IndustryTools.SetJobToFastest(industry, job))
                continue;
            queued++;
            ShipLogBook.NoteIndustryQueued(colony, IndustryTools.HeadName(colony), job.NumberOrdered, job.Name, "the warehouse minimum");
            IndustryTools.AutoAddSubJobs(colony, job);
        }
        return queued;
    }

    /// <summary>Units of the item the rule keeps from sale: its Min, and its Max when the market manages sales.</summary>
    internal static long SellHold(Entity colony, string cargoId, long hold, bool managesMarket)
    {
        var rule = Get(colony, cargoId);
        if (rule == null)
            return hold;
        hold = Math.Max(hold, rule.Min ?? 0);
        if (managesMarket && rule.Max is long max)
            hold = Math.Max(hold, max);
        return hold;
    }

    /// <summary>
    /// With the market managed, an item with a Max and no market row gets an auto-priced row (the good's seed ask, no
    /// bid), so its surplus is listed.
    /// </summary>
    internal static void AddSellRows(Entity colony, ColonyMarketPolicyDB policy)
    {
        if (!colony.TryGetDataBlob<ColonyStockRulesDB>(out var db) || Info(colony) is not { } info)
            return;
        foreach (var rule in db.Rules.Values)
        {
            if (rule.Max == null || policy.Rows.ContainsKey(rule.CargoId))
                continue;
            policy.Rows[rule.CargoId] = new MarketPolicyRow
            {
                CargoId = rule.CargoId,
                Ask = Pulsar4X.Energy.EnergyPricing.SeedAsk(colony.Manager?.Game, info.Data, rule.CargoId),
                AutoPriced = true,
            };
        }
    }

    /// <summary>The faction's cargo good or industry design for this id, or null.</summary>
    internal static ICargoable? Item(Entity colony, string cargoId)
    {
        var info = Info(colony);
        if (info == null)
            return null;
        if (info.Data.CargoGoods.Contains(cargoId) && info.Data.CargoGoods.GetAny(cargoId) is { } good)
            return good;
        if (info.IndustryDesigns.TryGetValue(cargoId, out var design) && design is ICargoable buildable)
            return buildable;
        return null;
    }

    static FactionInfoDB? Info(Entity colony)
    {
        if (colony.Manager?.Game is not { } game || !game.Factions.TryGetValue(colony.FactionOwnerID, out var faction))
            return null;
        return faction.TryGetDataBlob<FactionInfoDB>(out var info) ? info : null;
    }
}
