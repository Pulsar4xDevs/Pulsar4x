using System;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Movement;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

public enum MarketSide
{
    BuyFromMarket,
    SellToMarket,
}

/// <summary>
/// One in-range exchange. The ship is the commanded entity. The market is mutated only after
/// <see cref="FactionStanceRules.CanTrade"/> succeeds. Does not move the ship and does not
/// enqueue a cargo transfer.
/// </summary>
public class MarketExchangeAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => "Market Exchange";

    public override string Details => _details;

    string _details = "";

    [JsonProperty]
    public int MarketEntityId { get; set; }

    [JsonProperty]
    public string CargoId { get; set; } = "";

    [JsonProperty]
    public MarketSide Side { get; set; }

    [JsonProperty]
    public long RequestedUnits { get; set; }

    Entity? _entityCommanding;

    internal override Entity EntityCommanding => _entityCommanding!;

    public static MarketExchangeAction Create(Entity ship, int marketEntityId, string cargoId, MarketSide side, long units)
    {
        return new MarketExchangeAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            MarketEntityId = marketEntityId,
            CargoId = cargoId,
            Side = side,
            RequestedUnits = units,
            CreatedDate = ship.Manager.ManagerSubpulses.StarSysDateTime,
        };
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (_isFinished)
            return;
        if (_entityCommanding == null)
        {
            Fail("Cannot trade");
            return;
        }

        var game = _entityCommanding.Manager.Game;
        if (!game.GlobalManager.TryGetGlobalEntityById(MarketEntityId, out var market))
        {
            Fail("No listing");
            return;
        }

        if (!MarketBook.TryGet(market, CargoId, out var listing))
        {
            Fail("No listing");
            return;
        }

        int shipFaction = _entityCommanding.FactionOwnerID;
        int marketFaction = market.FactionOwnerID;
        if (!FactionStanceRules.CanTrade(game, shipFaction, marketFaction))
        {
            Fail("Cannot trade");
            return;
        }

        if (!InRange(_entityCommanding, market))
        {
            Fail("Out of range");
            return;
        }

        if (!TryResolve(game, shipFaction, CargoId, out var shipGood)
            || !TryResolve(game, marketFaction, CargoId, out var marketGood))
        {
            Fail("Unknown good");
            return;
        }

        long requested = RequestedUnits < 0 ? 0 : RequestedUnits;
        long quantity;
        decimal price;
        Entity source;
        Entity dest;
        ICargoable sourceGood;
        ICargoable destGood;

        if (Side == MarketSide.BuyFromMarket)
        {
            price = listing.Ask;
            quantity = Min(
                requested,
                listing.SellQuantity,
                MarketBook.Sellable(market, listing, marketGood),
                FreeUnits(_entityCommanding, shipGood));
            source = market;
            dest = _entityCommanding;
            sourceGood = marketGood;
            destGood = shipGood;
        }
        else if (Side == MarketSide.SellToMarket)
        {
            price = listing.Bid;
            quantity = Min(
                requested,
                listing.BuyQuantity,
                MarketBook.Stock(_entityCommanding, shipGood),
                FreeUnits(market, marketGood));
            source = _entityCommanding;
            dest = market;
            sourceGood = shipGood;
            destGood = marketGood;
        }
        else
        {
            Fail("Nothing to exchange");
            return;
        }

        bool crossFaction = shipFaction != marketFaction;
        if (crossFaction && price > 0)
        {
            int payer = Side == MarketSide.BuyFromMarket ? shipFaction : marketFaction;
            quantity = Math.Min(quantity, Affordable(game, payer, price));
        }

        if (quantity <= 0)
        {
            Fail("Nothing to exchange");
            return;
        }

        if (!source.TryGetDataBlob<CargoStorageDB>(out var sourceStore)
            || !dest.TryGetDataBlob<CargoStorageDB>(out var destStore))
        {
            Fail("Nothing to exchange");
            return;
        }

        long removed = sourceStore.RemoveCargoByUnit(sourceGood, quantity);
        long added = destStore.AddCargoByUnit(destGood, removed);
        if (added < removed)
            sourceStore.AddCargoByUnit(sourceGood, removed - added);
        long moved = added;
        if (moved <= 0)
        {
            Fail("Nothing to exchange");
            return;
        }

        CargoTransferProcessor.UpdateMassFuelAndDeltaV(source);
        CargoTransferProcessor.UpdateMassFuelAndDeltaV(dest);

        if (Side == MarketSide.BuyFromMarket)
            listing.SellQuantity -= moved;
        else
            listing.BuyQuantity -= moved;

        if (crossFaction
            && game.Factions.TryGetValue(shipFaction, out var shipFactionEntity)
            && game.Factions.TryGetValue(marketFaction, out var marketFactionEntity)
            && shipFactionEntity.TryGetDataBlob<FactionInfoDB>(out var shipInfo)
            && marketFactionEntity.TryGetDataBlob<FactionInfoDB>(out var marketInfo))
        {
            decimal amount = price * moved;
            string forShip = $"{CargoId} {moved} {marketFaction}";
            string forMarket = $"{CargoId} {moved} {shipFaction}";
            if (Side == MarketSide.BuyFromMarket)
            {
                shipInfo.Money.AddExpense(atDateTime, TransactionCategory.Trade, forShip, amount);
                marketInfo.Money.AddIncome(atDateTime, TransactionCategory.Trade, forMarket, amount);
            }
            else
            {
                marketInfo.Money.AddExpense(atDateTime, TransactionCategory.Trade, forMarket, amount);
                shipInfo.Money.AddIncome(atDateTime, TransactionCategory.Trade, forShip, amount);
            }
        }

        _details = $"{Side} {moved} {CargoId}";
        _isFinished = true;
    }

    internal override bool IsFinished() => _isFinished;

    internal override bool IsValidCommand(Game game)
    {
        return CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _, out _entityCommanding);
    }

    public override EntityAction Clone()
    {
        return new MarketExchangeAction
        {
            MarketEntityId = MarketEntityId,
            CargoId = CargoId,
            Side = Side,
            RequestedUnits = RequestedUnits,
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ParentGoalId = ParentGoalId,
            UseActionLanes = UseActionLanes,
        };
    }

    void Fail(string message)
    {
        _details = message;
        Status = ActionStatus.Failed;
        _isFinished = true;
    }

    /// <summary>
    /// In range when the Hohmann Δv is within the larger of the two transfer ranges.
    /// <see cref="CargoTransferProcessor"/> stops a transfer when Δv is past that range.
    /// </summary>
    internal static bool InRange(Entity ship, Entity market)
    {
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var shipStore)
            || !market.TryGetDataBlob<CargoStorageDB>(out var marketStore))
            return false;
        if (!ship.HasDataBlob<PositionDB>() || !market.HasDataBlob<PositionDB>())
            return false;

        double dv = CargoTransferProcessor.CalcDVDifference_m(ship, market);
        double maxRange = Math.Max(shipStore.TransferRangeDv_mps, marketStore.TransferRangeDv_mps);
        return dv <= maxRange;
    }

    static bool TryResolve(Game game, int factionId, string cargoId, out ICargoable cargo)
    {
        cargo = null!;
        if (string.IsNullOrEmpty(cargoId))
            return false;
        if (!game.Factions.TryGetValue(factionId, out var faction))
            return false;
        if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return false;
        if (!info.Data.CargoGoods.Contains(cargoId))
            return false;
        var found = info.Data.CargoGoods.GetAny(cargoId);
        if (found == null)
            return false;
        cargo = found;
        return true;
    }

    static long FreeUnits(Entity entity, ICargoable cargo)
    {
        if (!entity.TryGetDataBlob<CargoStorageDB>(out var store))
            return 0;
        long space = store.GetFreeUnitSpace(cargo);
        return space < 0 ? 0 : space;
    }

    static long Affordable(Game game, int factionId, decimal price)
    {
        if (price <= 0)
            return 0;
        if (!game.Factions.TryGetValue(factionId, out var faction))
            return 0;
        if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return 0;
        decimal funds = info.Money.GetCurrentFunds();
        if (funds <= 0)
            return 0;
        decimal raw = Math.Floor(funds / price);
        if (raw >= long.MaxValue)
            return long.MaxValue;
        return (long)raw;
    }

    static long Min(long a, long b, long c, long d)
    {
        long m = a < b ? a : b;
        if (c < m) m = c;
        if (d < m) m = d;
        return m < 0 ? 0 : m;
    }
}
