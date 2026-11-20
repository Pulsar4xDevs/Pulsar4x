using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Multi-faction phase 4: bloc markets that buy. Data-driven demand rows (<see cref="MarketPolicyRow.IsDemand"/>)
/// are drained by a consumption sink each market pass (pro rata to the time since the last pass), bid at
/// seed ask x bid factor x (1 + min(1, days short / 90)), and buy only with funds above <see cref="ReserveCredits"/>.
/// </summary>
public static class BlocDemand
{
    /// <summary>Months of running costs a buying faction keeps back (multi-faction phase 6, replacing phase 4's flat
    /// 10M reserve): demand rows only bid with funds above this; selling is never stopped.</summary>
    public const double ReserveMonths = 3;

    /// <summary>The faction's bid reserve: <see cref="ReserveMonths"/> × its monthly running costs at the game's wages.</summary>
    public static decimal ReserveFor(Entity colony)
    {
        var game = colony.Manager?.Game;
        // Phase 7a: a trade-only NPC (Luna, Ceres) pays no wages, so it keeps no running-cost reserve.
        if (game == null || !FactionSpending.PaysWages(game, colony.FactionOwnerID))
            return 0;
        return FactionSpending.MonthlyRunningCosts(game, colony.FactionOwnerID) * (decimal)ReserveMonths;
    }

    /// <summary>Days short at which the premium reaches its cap (x2 of the base bid, 1.5 x seed ask at factor 0.75).</summary>
    public const double PremiumDays = 90;

    /// <summary>Consumption sink and shortage premium for one colony's demand rows. Safe to call every pass.</summary>
    public static void Run(Entity colony, DateTime now)
    {
        if (!colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
            return;
        var info = InfoOf(colony);
        if (info == null)
            return;
        bool anyDemand = policy.Rows.Values.Any(r => r.IsDemand() || r.OutputPerDay > 0 || r.OutputPerDayPerMillion > 0);
        if (!anyDemand)
            return;

        double days = policy.LastConsumption is DateTime last ? Math.Max(0, (now - last).TotalDays) : 0;
        policy.LastConsumption = now;
        colony.TryGetDataBlob<CargoStorageDB>(out var storage);
        var game = colony.Manager?.Game;

        Produce(policy, info, storage, days, colony);

        foreach (var row in policy.Rows.Values.Where(r => r.IsDemand()).OrderBy(r => r.CargoId, StringComparer.Ordinal))
        {
            var cargo = info.Data.CargoGoods.Contains(row.CargoId) ? info.Data.CargoGoods.GetAny(row.CargoId) : null;
            long stock = cargo != null && storage != null ? storage.GetUnitsStored(cargo, false) : 0;

            if (row.UsePerDay > 0 && days > 0 && cargo != null && storage != null)
            {
                row.UseOwed += row.UsePerDay * days;
                long use = (long)Math.Floor(row.UseOwed);
                long removed = use > 0 ? storage.RemoveCargoByUnit(cargo, Math.Min(use, stock)) : 0;
                // Unmet use is not carried: the shortage shows up as stock below Min and a rising bid.
                row.UseOwed -= use;
                stock -= removed;
            }

            if (stock < row.Min)
                row.ShortSince ??= now;
            else
                row.ShortSince = null;

            // Repricing the bid is the market's own pricing: only a managed market does it.
            if (!GameEngine.People.MarketSeat.ManagesMarket(colony))
                continue;
            decimal ask = EnergyPricing.SeedAsk(game, info.Data, row.CargoId);
            row.Bid = Math.Round(ask * (decimal)(row.BidFactor * Premium(row, now)), 2, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Phase 8: a Sell row with output is the bloc's abstract industry: it adds that many units a day to stock,
    /// held at the row's Max (90 days of output when Max is 0). Output the stock has no room for is not carried.
    /// Production scales with live population when <see cref="MarketPolicyRow.OutputPerDayPerMillion"/> is set.
    /// </summary>
    /// <summary>Units per day at the colony's live population. A row with only <see cref="MarketPolicyRow.OutputPerDay"/>
    /// (tests and rows that are not population-scaled) keeps that rate.</summary>
    public static double LiveOutputPerDay(MarketPolicyRow row, Entity colony)
    {
        if (row.OutputPerDayPerMillion > 0)
        {
            long pop = 0;
            if (colony.TryGetDataBlob<Pulsar4X.Colonies.ColonyInfoDB>(out var info))
            {
                foreach (var n in info.Population.Values)
                    pop += n;
            }
            return row.OutputPerDayPerMillion * (pop / 1_000_000.0);
        }
        return row.OutputPerDay;
    }

    static void Produce(ColonyMarketPolicyDB policy, FactionInfoDB info, Pulsar4X.Storage.CargoStorageDB? storage, double days, Entity colony)
    {
        if (storage == null || days <= 0)
            return;
        foreach (var row in policy.Rows.Values.OrderBy(r => r.CargoId, StringComparer.Ordinal))
        {
            double rate = LiveOutputPerDay(row, colony);
            if (rate <= 0)
                continue;
            if (!info.Data.CargoGoods.Contains(row.CargoId))
                continue;
            var cargo = info.Data.CargoGoods.GetAny(row.CargoId);
            row.OutputOwed += rate * days;
            long whole = (long)Math.Floor(row.OutputOwed);
            row.OutputOwed -= whole;
            long cap = row.Max > 0 ? row.Max : (long)Math.Ceiling(PremiumDays * rate);
            long room = cap - storage.GetUnitsStored(cargo, false);
            long add = Math.Min(whole, room);
            if (add > 0)
                storage.AddCargoByUnit(cargo, add);
        }
    }

    /// <summary>1 when filled, rising linearly to 2 after <see cref="PremiumDays"/> short.</summary>
    public static double Premium(MarketPolicyRow row, DateTime now)
        => row.ShortSince is DateTime since ? 1 + Math.Min(1, Math.Max(0, (now - since).TotalDays) / PremiumDays) : 1;

    /// <summary>Units the colony's faction can pay for at <paramref name="price"/> above the reserve.</summary>
    public static long Affordable(Entity colony, decimal price)
    {
        if (price <= 0)
            return long.MaxValue;
        var info = InfoOf(colony);
        if (info == null)
            return 0;
        decimal spare = info.Money.GetCurrentFunds() - ReserveFor(colony);
        return spare <= 0 ? 0 : (long)Math.Floor(spare / price);
    }

    /// <summary>
    /// Same-body clearing (phase 4 with phase 2's settlement): each want (the colony's policy shortfall at its bid,
    /// computed in the market pass before it posts its buy listing) pulls from
    /// sell listings on other colonies on the same body whose ask is at or below its bid, cheapest first. Instant,
    /// fee 0, mutual trade required. Demand rows only spend funds above the reserve. Returns units bought.
    /// </summary>
    public static long ClearSameBody(Entity buyer, IReadOnlyList<(string cargoId, long units, decimal bid, bool demand)> wants)
    {
        if (!buyer.HasDataBlob<LogiBaseDB>() || !buyer.TryGetDataBlob<Pulsar4X.Colonies.ColonyInfoDB>(out var colonyInfo)
            || !colonyInfo.PlanetEntity.IsValid || !buyer.TryGetDataBlob<LogiBaseDB>(out var book))
            return 0;
        var game = buyer.Manager?.Game;
        if (game == null)
            return 0;
        if (wants.Count == 0)
            return 0;
        var sellers = colonyInfo.PlanetEntity.ColoniesOnBody()
            .Where(c => c.Id != buyer.Id && c.HasDataBlob<LogiBaseDB>()
                && FactionStanceRules.CanTrade(game, buyer.FactionOwnerID, c.FactionOwnerID))
            .OrderBy(c => c.Id).ToList();
        if (sellers.Count == 0)
            return 0;

        long total = 0;
        foreach (var want in wants.Where(w => w.units > 0 && w.bid > 0).OrderBy(w => w.cargoId, StringComparer.Ordinal))
        {
            bool demand = want.demand;
            long left = want.units;
            var offers = new List<(Entity seller, MarketListing listing)>();
            foreach (var seller in sellers)
            {
                if (MarketBook.TryGet(seller, want.cargoId, out var sell) && !sell.Relocation
                    && sell.SellQuantity > 0 && sell.Ask <= want.bid)
                    offers.Add((seller, sell));
            }
            foreach (var (seller, sell) in offers.OrderBy(o => o.listing.Ask).ThenBy(o => o.seller.Id))
            {
                if (left <= 0)
                    break;
                long units = left;
                if (demand && seller.FactionOwnerID != buyer.FactionOwnerID)
                    units = Math.Min(units, Affordable(buyer, sell.Ask));
                if (units <= 0)
                    break;
                var sellerInfo = InfoOf(seller);
                if (sellerInfo == null || !sellerInfo.Data.CargoGoods.Contains(want.cargoId))
                    continue;
                var cargo = sellerInfo.Data.CargoGoods.GetAny(want.cargoId);
                if (cargo == null)
                    continue;
                long before = MarketBook.Stock(buyer, cargo);
                if (!ShipmentBook.TrySealMarket(buyer, seller, false, new List<(ICargoable, long)> { (cargo, units) }, 0m,
                        out _, out _))
                    continue;
                long got = Math.Max(0, MarketBook.Stock(buyer, cargo) - before);
                left -= got;
                total += got;
            }
        }
        return total;
    }

    static FactionInfoDB? InfoOf(Entity colony)
    {
        var game = colony.Manager?.Game;
        if (game == null || !game.Factions.TryGetValue(colony.FactionOwnerID, out var faction))
            return null;
        return faction.TryGetDataBlob<FactionInfoDB>(out var info) ? info : null;
    }
}
