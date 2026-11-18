using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Interfaces;
using Pulsar4X.Logistics;
using Pulsar4X.People;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// Multi-faction phase 8, the crew market (plan §7.6 / phase 8). A colony with a crew pool can list its spare
    /// trained crew (the pool above a reserve) at a transfer fee. A buyer on the same body (Trade granted both ways)
    /// pays the fee, the crew move from the seller's pool to the buyer's at once, and the buyer pays their wages
    /// from then on as ordinary pool crew. Supply is the spare pool, which an NPC bloc's academy refills slowly; the
    /// fee is set well above academy tuition so the market is no way to skip the academy.
    /// </summary>
    public static class CrewMarket
    {
        /// <summary>Months of a crew course whose pay stands in for the academy's tuition.</summary>
        public const int TuitionMonths = NavalAcademyAtb.DefaultCrewCourseMonths;
        /// <summary>The default fee is this multiple of the tuition equivalent (1.5x), and no ask may be lower.</summary>
        public const decimal FeeMultiplier = 1.5m;
        /// <summary>The seller receives this share of the fee; the rest is the market's transfer levy (not paid to anyone).</summary>
        public const decimal SellerShare = 0.6m;
        /// <summary>An NPC bloc keeps this many crew back from sale.</summary>
        public const long NpcReserve = 50;
        /// <summary>An NPC academy tops its pool up to reserve + this many.</summary>
        public const long NpcSpareTarget = 200;
        /// <summary>An NPC academy's monthly output into the pool (a class of 200 over 3 months).</summary>
        public const long NpcMonthlyOutput = NavalAcademyAtb.DefaultCrewSeats / NavalAcademyAtb.DefaultCrewCourseMonths;

        /// <summary>One crew's tuition equivalent: the course months at the crew wage, cr.</summary>
        public static decimal TuitionEquivalent(Game? game)
        {
            decimal wage = game != null && game.Settings.CrewWagePerDay > 0 ? game.Settings.CrewWagePerDay : Pulsar4X.Engine.GameSettings.DefaultCrewWagePerDay;
            return TuitionMonths * 30m * wage;
        }

        /// <summary>The fee nobody may undercut: <see cref="FeeMultiplier"/> x tuition.</summary>
        public static decimal DefaultFee(Game? game) => Math.Round(TuitionEquivalent(game) * FeeMultiplier, 0);

        /// <summary>The seller's fee per crew.</summary>
        public static decimal Fee(Entity seller)
        {
            var game = seller.Manager?.Game;
            decimal floor = DefaultFee(game);
            return seller.TryGetDataBlob<ColonyCrewPoolDB>(out var pool) && pool.Ask is decimal ask ? Math.Max(floor, ask) : floor;
        }

        /// <summary>Crew of a species on offer: the listed pool above the reserve (0 when not listed).</summary>
        public static long Available(Entity seller, int speciesId)
        {
            if (!seller.TryGetDataBlob<ColonyCrewPoolDB>(out var pool) || !pool.Listed)
                return 0;
            pool.Trained.TryGetValue(speciesId, out long have);
            return Math.Max(0, Math.Min(have, pool.Total - pool.Reserve));
        }

        /// <summary>Everything the seller lists, by species with stock (ordered by species id).</summary>
        public static List<(int SpeciesId, long Available)> Offers(Entity seller)
        {
            var list = new List<(int, long)>();
            if (!seller.TryGetDataBlob<ColonyCrewPoolDB>(out var pool))
                return list;
            foreach (var id in pool.Trained.Keys.OrderBy(k => k))
            {
                long n = Available(seller, id);
                if (n > 0)
                    list.Add((id, n));
            }
            return list;
        }

        /// <summary>Why <paramref name="buyer"/> cannot buy from <paramref name="seller"/> at all (null when it can).</summary>
        public static string? CannotTrade(Entity buyer, Entity seller)
        {
            var game = buyer.Manager?.Game;
            if (game == null || !buyer.HasDataBlob<ColonyInfoDB>() || !seller.HasDataBlob<ColonyInfoDB>())
                return "Not a colony.";
            if (buyer.FactionOwnerID == seller.FactionOwnerID)
                return "Move your own crew between your colonies instead.";
            if (!SameBody.Is(buyer, seller))
                return "Crew are only sold to a colony on the same body.";
            if (!FactionStanceRules.CanTrade(game, buyer.FactionOwnerID, seller.FactionOwnerID))
                return "Trade must be granted both ways.";
            return null;
        }

        /// <summary>Lists or delists the colony's spare crew. <paramref name="ask"/> null or below the default fee uses the default.</summary>
        public static string? TryList(Entity seller, bool listed, long reserve, decimal? ask)
        {
            if (!seller.HasDataBlob<ColonyInfoDB>())
                return "Not a colony.";
            if (reserve < 0)
                return "The reserve cannot be negative.";
            var pool = ColonyCrewPoolDB.Of(seller);
            pool.Listed = listed;
            pool.Reserve = reserve;
            pool.Ask = ask is decimal a && a > DefaultFee(seller.Manager?.Game) ? a : null;
            pool.NpcSetUp = true;
            return null;
        }

        /// <summary>
        /// Buys <paramref name="count"/> crew of a species from a listed colony on the same body. The fee is charged to the
        /// buyer; the seller banks <see cref="SellerShare"/> of it. Returns null or the refusal.
        /// </summary>
        public static string? TryBuy(Entity buyer, Entity seller, int speciesId, long count)
        {
            var game = buyer.Manager?.Game;
            string? cannot = CannotTrade(buyer, seller);
            if (cannot != null)
                return cannot;
            if (count <= 0)
                return "Buy at least one crew.";
            long available = Available(seller, speciesId);
            if (available <= 0)
                return "No crew of that species are on offer.";
            if (count > available)
                return $"Only {available:N0} are on offer (the seller keeps a reserve).";
            if (!game!.Factions.TryGetValue(buyer.FactionOwnerID, out var buyerFaction) || !buyerFaction.TryGetDataBlob<FactionInfoDB>(out var buyerInfo)
                || !game.Factions.TryGetValue(seller.FactionOwnerID, out var sellerFaction) || !sellerFaction.TryGetDataBlob<FactionInfoDB>(out var sellerInfo))
                return "Unknown faction.";
            if (FactionSpending.IsBlocked(game, buyer.FactionOwnerID))
                return "Spending is blocked while the balance is negative.";
            decimal fee = Fee(seller);
            decimal total = fee * count;
            if (FactionSpending.FundsAfterOwed(buyerInfo) < total)
                return $"The fee is {total:N0} cr; you can spare {Math.Max(0, FactionSpending.FundsAfterOwed(buyerInfo)):N0}.";

            var sellerPool = seller.GetDataBlob<ColonyCrewPoolDB>();
            long moved = 0;
            if (sellerPool.Trained.TryGetValue(speciesId, out long have))
            {
                moved = Math.Min(have, count);
                if (have - moved <= 0) sellerPool.Trained.Remove(speciesId); else sellerPool.Trained[speciesId] = have - moved;
            }
            if (moved <= 0)
                return "No crew of that species are on offer.";
            ColonyCrewPoolDB.Of(buyer).Add(speciesId, moved);

            DateTime now = buyer.StarSysDateTime;
            decimal paid = fee * moved;
            buyerInfo.Money.AddExpense(now, TransactionCategory.CrewTransfer, $"Crew transfer: {moved} crew from {seller.GetName(buyer.FactionOwnerID)}", paid);
            sellerInfo.Money.AddIncome(now, TransactionCategory.CrewTransfer, $"Crew transfer: {moved} crew to {buyer.GetName(seller.FactionOwnerID)}", Math.Round(paid * SellerShare, 2));
            EventManager.Instance.Publish(Event.Create(EventType.CrewTransferred, now,
                $"{moved} trained crew arrived from {seller.GetName(buyer.FactionOwnerID)} for {paid:N0} cr.", buyer.FactionOwnerID, buyer.Manager?.ManagerID, buyer.Id));
            return null;
        }

        /// <summary>
        /// Daily pass on a crew pool: an NPC bloc with an academy lists its spare crew once, and every 30 days its academy
        /// tops the pool up toward reserve + <see cref="NpcSpareTarget"/> by <see cref="NpcMonthlyOutput"/>.
        /// </summary>
        public static void RunDay(Entity colony, DateTime now)
        {
            var game = colony.Manager?.Game;
            if (game == null || !colony.TryGetDataBlob<ColonyCrewPoolDB>(out var pool) || !colony.TryGetDataBlob<ColonyInfoDB>(out var info)
                || !FactionSpending.IsNpcEconomy(game, colony.FactionOwnerID))
                return;
            if (!pool.NpcSetUp)
            {
                pool.NpcSetUp = true;
                pool.Listed = true;
                pool.Reserve = NpcReserve;
            }
            if (!colony.HasDataBlob<NavalAcademyDB>())
                return;
            if (pool.LastRefill is not DateTime last)
            {
                pool.LastRefill = now;
                return;
            }
            if ((now - last).TotalDays < Pulsar4X.Colonies.PopulationProcessor.TickDays - 0.01)
                return;
            pool.LastRefill = now;
            long room = pool.Reserve + NpcSpareTarget - pool.Total;
            if (room <= 0)
                return;
            int species = pool.Trained.Count > 0 ? pool.Trained.OrderByDescending(p => p.Value).ThenBy(p => p.Key).First().Key
                : info.Population.OrderByDescending(p => p.Value).Select(p => p.Key).FirstOrDefault();
            pool.Add(species, Math.Min(room, NpcMonthlyOutput));
        }
    }

    /// <summary>Daily crew market pass (multi-faction phase 8), keyed on the crew pool.</summary>
    public class CrewMarketProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);
        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.5);
        public Type GetParameterType => typeof(ColonyCrewPoolDB);
        public void Init(Game game) { }

        public void ProcessEntity(Entity entity, int deltaSeconds) => CrewMarket.RunDay(entity, entity.StarSysDateTime);

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var colonies = manager.GetAllEntitiesWithDataBlob<ColonyCrewPoolDB>();
            foreach (var colony in colonies)
                CrewMarket.RunDay(colony, manager.StarSysDateTime);
            return colonies.Count;
        }
    }
}
