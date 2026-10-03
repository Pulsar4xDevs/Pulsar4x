using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Logistics;

/// <summary>
/// One refining batch for a line that is already built. Called by Offer stock.
/// Does not read personality, does not set a bid, and does not assign its own goal.
/// </summary>
public class RunIndustryPlan : IGoalPlanner
{
    public GoalType Type => GoalType.RunIndustry;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        _ = goal;
        if (!managedEntity.TryGetDataBlob<IndustryAbilityDB>(out var industry))
            return PlanResult.Continue(new List<EntityAction>());

        var info = FactionOf(managedEntity);
        if (info == null)
            return PlanResult.Continue(new List<EntityAction>());

        var candidates = new List<Candidate>();
        foreach (var (lineId, line) in industry.ProductionLines)
        {
            if (line.IndustryTypeRates == null || LineIsBusy(line))
                continue;

            foreach (var design in info.IndustryDesigns.Values)
            {
                if (design is not ProcessedMaterial material)
                    continue;
                if (string.IsNullOrEmpty(material.IndustryTypeID))
                    continue;
                if (!line.IndustryTypeRates.ContainsKey(material.IndustryTypeID))
                    continue;
                if (material.OutputAmount < 1 || material.VolumePerUnit <= 0)
                    continue;
                if (material.ResourceCosts == null || material.ResourceCosts.Count == 0)
                    continue;
                if (!CanPay(managedEntity, info, material))
                    continue;

                long stock = StockOf(managedEntity, info, material.UniqueID);
                if (managedEntity.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy)
                    && policy.Rows.TryGetValue(design.UniqueID, out var row))
                {
                    if (row.AutoProduce || stock >= row.Max)
                        continue;
                }

                candidates.Add(new Candidate
                {
                    CargoId = material.UniqueID,
                    LineId = lineId,
                    Stock = stock,
                    OutputAmount = material.OutputAmount,
                    Batches = stock / material.OutputAmount,
                });
            }
        }

        if (candidates.Count == 0)
            return PlanResult.Continue(new List<EntityAction>());

        // Top up a refined good already in the warehouse. A recipe with no pile
        // waits. Earth also unlocks plastic, ntp, fissile fuels, and electricity
        // with none stored; stainless steel is the thinnest pile it does have.
        // When nothing refined is stored, every payable recipe stays in the list.
        if (candidates.Exists(candidate => candidate.Stock > 0))
            candidates.RemoveAll(candidate => candidate.Stock <= 0);

        candidates.Sort(static (a, b) =>
        {
            int byBatches = a.Batches.CompareTo(b.Batches);
            if (byBatches != 0)
                return byBatches;
            int byCargo = string.CompareOrdinal(a.CargoId, b.CargoId);
            if (byCargo != 0)
                return byCargo;
            return string.CompareOrdinal(a.LineId, b.LineId);
        });

        var chosen = candidates[0];
        if (!HasRow(managedEntity, chosen.CargoId))
        {
            int capacity = CapacityOf(managedEntity);
            if (!TryAddRow(managedEntity, chosen, capacity))
                return PlanResult.Continue(new List<EntityAction>(), $"{chosen.CargoId} skipped for capacity");
        }

        var job = new IndustryJob(info, chosen.CargoId);
        job.InitialiseJob(1, false);
        var order = IndustryOrder2.CreateNewJobOrder(
            managedEntity.FactionOwnerID, managedEntity, chosen.LineId, job);
        order.AutoAddSubJobs = false;
        order.UseActionLanes = true;
        order.ActionOnDate = managedEntity.StarSysDateTime;
        return PlanResult.Continue(new List<EntityAction> { order });
    }

    static bool LineIsBusy(IndustryAbilityDB.ProductionLine line)
    {
        foreach (var job in line.Jobs)
        {
            if (job.NumberCompleted < job.NumberOrdered)
                return true;
        }
        return false;
    }

    static bool CanPay(Entity colony, FactionInfoDB info, IConstructableDesign design)
    {
        if (design.ResourceCosts == null)
            return true;
        foreach (var (cargoId, amount) in design.ResourceCosts)
        {
            if (amount > 0 && StockOf(colony, info, cargoId) < amount)
                return false;
        }
        return true;
    }

    static int CapacityOf(Entity colony)
    {
        if (!colony.TryGetDataBlob<LogiBaseDB>(out var office))
            return 0;
        return office.Capacity < 0 ? 0 : office.Capacity;
    }

    static bool HasRow(Entity colony, string cargoId)
        => colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy) && policy.Rows.ContainsKey(cargoId);

    static bool TryAddRow(Entity colony, Candidate chosen, int capacity)
    {
        if (!colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
        {
            if (capacity <= 0)
                return false;
            policy = new ColonyMarketPolicyDB();
            colony.SetDataBlob(policy);
        }
        if (policy.Rows.ContainsKey(chosen.CargoId))
            return true;
        if (policy.Rows.Count >= capacity)
            return false;

        policy.Rows[chosen.CargoId] = new MarketPolicyRow
        {
            CargoId = chosen.CargoId,
            Min = 0,
            Max = chosen.Stock + chosen.OutputAmount,
            AutoProduce = false,
            Ask = 0,
            Bid = 0,
        };
        return true;
    }

    static long StockOf(Entity colony, FactionInfoDB info, string cargoId)
    {
        if (info.Data?.CargoGoods == null || !info.Data.CargoGoods.Contains(cargoId))
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

    sealed class Candidate
    {
        public string CargoId = "";
        public string LineId = "";
        public long Stock;
        public long OutputAmount;
        public long Batches;
    }
}
