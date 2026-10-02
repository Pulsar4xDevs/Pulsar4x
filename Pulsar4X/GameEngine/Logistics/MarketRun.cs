using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Movement;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Markets in the ship's system and in systems the faction knows, and the current buy or sell leg.
/// Trade and Freighter each score their own pairs.
/// </summary>
static class MarketRun
{
    public static bool HasRoute(Goal goal)
        => !string.IsNullOrEmpty(goal.CargoId) && goal.SourceEntityId >= 0 && goal.DestEntityId >= 0;

    public static bool TryMarket(Entity ship, int id, out Entity market)
        => ship.Manager.TryGetGlobalEntityById(id, out market) && market.HasDataBlob<LogiBaseDB>();

    public static List<Entity> Markets(Entity ship)
    {
        var seen = new HashSet<int>();
        var list = new List<Entity>();

        void AddFrom(EntityManager manager)
        {
            if (manager == null)
                return;
            foreach (var market in manager.GetAllEntitiesWithDataBlob<LogiBaseDB>())
            {
                if (market.Id == ship.Id || !seen.Add(market.Id))
                    continue;
                list.Add(market);
            }
        }

        AddFrom(ship.Manager);
        if (ship.Manager.Game.Factions.TryGetValue(ship.FactionOwnerID, out var faction)
            && faction.TryGetDataBlob<FactionInfoDB>(out var info))
        {
            foreach (var systemId in info.KnownSystems)
            {
                foreach (var system in ship.Manager.Game.Systems)
                {
                    if (system.ID == systemId && system != ship.Manager)
                        AddFrom(system);
                }
            }
        }

        list.Sort((a, b) => a.Id.CompareTo(b.Id));
        return list;
    }

    public static bool TryShipGood(Entity ship, string cargoId, out ICargoable good)
    {
        good = null!;
        var library = ship.GetFactionCargoDefinitions();
        if (library == null || !library.Contains(cargoId))
            return false;
        var found = library.GetAny(cargoId);
        if (found == null || !ship.TryGetDataBlob<CargoStorageDB>(out var store))
            return false;
        if (!store.TypeStores.ContainsKey(found.CargoTypeID))
            return false;
        good = found;
        return true;
    }

    public static PlanResult NextLeg(Entity ship, Entity source, Entity dest, string cargoId, ICargoable good, bool capBuyAtRequest = false)
    {
        if (!MarketBook.TryGet(source, cargoId, out var sell) || !MarketBook.TryGet(dest, cargoId, out var buy))
            return PlanResult.Fail("listing gone");

        var store = ship.GetDataBlob<CargoStorageDB>();
        long held = store.GetUnitsStored(good, includeEscro: false);
        if (held > 0)
        {
            long units = Math.Min(held, buy.BuyQuantity);
            return ExchangeOrMove(ship, dest, cargoId, MarketSide.SellToMarket, units);
        }

        long free = store.GetFreeUnitSpace(good);
        long buyUnits = Math.Min(sell.SellQuantity, Math.Max(0, free));
        if (capBuyAtRequest)
            buyUnits = Math.Min(buyUnits, Math.Max(0, buy.BuyQuantity));
        return ExchangeOrMove(ship, source, cargoId, MarketSide.BuyFromMarket, buyUnits);
    }

    public static PlanResult ExchangeOrMove(Entity ship, Entity market, string cargoId, MarketSide side, long units)
    {
        if (market.Manager != ship.Manager)
            return JumpRoute.LegToward(ship, market);

        if (MarketExchangeAction.InRange(ship, market))
            return PlanResult.Continue(MarketExchangeAction.Create(ship, market.Id, cargoId, side, units));

        if (!MovePlanner.TryBuildMoveActions(ship, market, out var actions, out var reason) || actions.Count == 0)
            return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "Out of range" : reason);
        return PlanResult.Continue(actions);
    }

    public static double DistanceMeters(Entity from, Entity to)
    {
        if (!from.TryGetDataBlob<PositionDB>(out var source) || !to.TryGetDataBlob<PositionDB>(out var dest))
            return double.MaxValue;
        double meters = source.GetDistanceTo_m(dest);
        if (!double.IsFinite(meters) || meters < 0)
            return double.MaxValue;
        return meters;
    }
}
