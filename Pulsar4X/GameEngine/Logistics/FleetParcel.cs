using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Splits one fleet trade or freight order into one-ship goals.
/// Candidates stay in the fleet's star system. A contract ignores command span.
/// </summary>
static class FleetParcel
{
    const double NoWarpHours = 1e12;

    readonly struct PairKey : IEquatable<PairKey>
    {
        public readonly string Cargo;
        public readonly int Source;
        public readonly int Dest;
        public PairKey(string cargo, int source, int dest)
        {
            Cargo = cargo;
            Source = source;
            Dest = dest;
        }
        public bool Equals(PairKey other) => Source == other.Source && Dest == other.Dest && Cargo == other.Cargo;
        public override bool Equals(object obj) => obj is PairKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Cargo, Source, Dest);
    }

    sealed class Pair
    {
        public Entity Source;
        public Entity Dest;
        public string Cargo;
        public PairKey Key;
    }

    public static PlanResult Plan(Entity fleet, Goal goal, bool freight)
    {
        if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB))
            return PlanResult.Fail("We have no subordinates to manage");

        var cargoShips = fleetDB.Children.Where(IsCargoShip).ToList();
        if (cargoShips.Count == 0)
            return PlanResult.Fail("We have no subordinates to manage");

        // A failed child of this order stays in place so the fleet rollup can see it.
        // Replacing it here would clear the failure before that check.
        if (cargoShips.Any(ship => OurGoal(ship, goal) is { Status: GoalStatus.Failed }))
            return PlanResult.Continue(new List<(Entity, Goal)>());

        var free = cargoShips.Where(ship => IsFree(ship, goal)).ToList();
        var claimedPairs = new HashSet<PairKey>();
        var claimedUnits = new Dictionary<PairKey, long>();
        foreach (var ship in cargoShips)
        {
            var active = OurGoal(ship, goal);
            if (active == null || active.Status is GoalStatus.Completed or GoalStatus.Failed)
                continue;
            if (!MarketRun.HasRoute(active))
                continue;
            var key = new PairKey(active.CargoId, active.SourceEntityId, active.DestEntityId);
            claimedPairs.Add(key);
            claimedUnits[key] = claimedUnits.GetValueOrDefault(key) + Math.Max(0, active.UnitShare);
        }

        bool contract = freight && MarketRun.HasRoute(goal);
        var markets = Markets(fleet, goal, freight, contract);
        var subGoals = new List<(Entity, Goal)>();
        if (freight)
            AssignFreight(fleet, goal, free, markets, claimedUnits, subGoals);
        else
            AssignTrade(fleet, goal, free, markets, claimedPairs, subGoals);

        bool anyoneOut = cargoShips.Any(ship => OurGoal(ship, goal) is { } active
            && active.Status is not (GoalStatus.Completed or GoalStatus.Failed));
        TryGetFleetTanker(fleetDB, out var tanker);
        int anchorId = goal.TargetEntityID;
        bool tankerOut = tanker != null && TankerAlreadyTasked(tanker, goal, anchorId);
        bool finished = cargoShips.Any(ship => OurGoal(ship, goal) is { Status: GoalStatus.Completed });
        bool work = WorkRemains(fleet, freight, markets, cargoShips, goal, claimedPairs, claimedUnits);
        if (subGoals.Count == 0 && !anyoneOut && !tankerOut && !work)
            return finished ? PlanResult.Done() : PlanResult.Fail(freight ? "no haul" : "no route");

        if (tanker != null && !tankerOut && anchorId >= 0
            && fleet.Manager.TryGetEntityById(anchorId, out _)
            && MovePlanner.CanMove(tanker, out _))
        {
            subGoals.Add((tanker, new Goal(GoalType.MoveTo)
            {
                ParentGoalId = goal.Id,
                TargetEntityID = anchorId,
            }));
        }

        return PlanResult.Continue(subGoals);
    }

    static void AssignTrade(
        Entity fleet, Goal goal, List<Entity> free, List<Entity> markets,
        HashSet<PairKey> claimed, List<(Entity, Goal)> subGoals)
    {
        var taken = new HashSet<PairKey>(claimed);
        while (free.Count > 0)
        {
            Entity bestShip = null;
            Pair bestPair = null;
            double bestScore = 0;
            double bestDist = double.MaxValue;
            foreach (var ship in free)
            {
                foreach (var pair in TradePairs(ship, markets))
                {
                    if (FreeSpace(ship, pair.Cargo) <= 0)
                        continue;
                    if (taken.Contains(pair.Key))
                        continue;
                    double score = TradeScore(ship, pair);
                    if (score <= 0)
                        continue;
                    double dist = MarketRun.DistanceMeters(ship, pair.Source);
                    if (score < bestScore)
                        continue;
                    if (score == bestScore && (dist > bestDist || (dist == bestDist && bestShip != null && ship.Id > bestShip.Id)))
                        continue;
                    bestScore = score;
                    bestDist = dist;
                    bestShip = ship;
                    bestPair = pair;
                }
            }

            if (bestShip == null || bestPair == null)
                break;

            long space = FreeSpace(bestShip, bestPair.Cargo);
            if (space <= 0 || !MarketBook.TryGet(bestPair.Source, bestPair.Cargo, out var sell))
                break;
            free.Remove(bestShip);
            taken.Add(bestPair.Key);
            subGoals.Add((bestShip, Child(goal, GoalType.Trade, bestPair, Math.Min(sell.SellQuantity, space))));
        }
    }

    static void AssignFreight(
        Entity fleet, Goal goal, List<Entity> free, List<Entity> markets,
        Dictionary<PairKey, long> claimed, List<(Entity, Goal)> subGoals)
    {
        var blocked = new HashSet<PairKey>();
        while (free.Count > 0)
        {
            Pair best = null;
            long bestUnits = 0;
            double bestDist = double.MaxValue;
            foreach (var pair in FreightPairs(markets))
            {
                if (blocked.Contains(pair.Key))
                    continue;
                long remaining = Remaining(fleet, pair, claimed);
                if (remaining <= 0)
                    continue;
                double dist = MarketRun.DistanceMeters(pair.Source, pair.Dest);
                if (!double.IsFinite(dist))
                    continue;
                if (remaining < bestUnits)
                    continue;
                if (remaining == bestUnits && (dist > bestDist || (dist == bestDist && best != null && LosesTie(pair, best))))
                    continue;
                bestUnits = remaining;
                bestDist = dist;
                best = pair;
            }

            if (best == null)
                break;

            Entity ship = Closest(free, best);
            if (ship == null)
            {
                blocked.Add(best.Key);
                continue;
            }

            long share = Math.Min(Remaining(fleet, best, claimed), FreeSpace(ship, best.Cargo));
            if (share <= 0)
            {
                blocked.Add(best.Key);
                continue;
            }

            free.Remove(ship);
            claimed[best.Key] = claimed.GetValueOrDefault(best.Key) + share;
            subGoals.Add((ship, Child(goal, GoalType.Freighter, best, share)));
        }
    }

    static bool WorkRemains(
        Entity fleet, bool freight, List<Entity> markets, List<Entity> ships, Goal goal,
        HashSet<PairKey> claimedPairs, Dictionary<PairKey, long> claimedUnits)
    {
        if (freight)
        {
            foreach (var pair in FreightPairs(markets))
            {
                if (Remaining(fleet, pair, claimedUnits) <= 0)
                    continue;
                if (ships.Any(ship => OurGoal(ship, goal) is not { Status: GoalStatus.Failed } && FreeSpace(ship, pair.Cargo) > 0))
                    return true;
            }
            return false;
        }

        foreach (var ship in ships)
        {
            if (OurGoal(ship, goal) is { Status: GoalStatus.Failed })
                continue;
            foreach (var pair in TradePairs(ship, markets))
            {
                if (claimedPairs.Contains(pair.Key))
                    continue;
                if (TradeScore(ship, pair) > 0)
                    return true;
            }
        }
        return false;
    }

    static Goal Child(Goal parent, GoalType type, Pair pair, long share) => new(type)
    {
        ParentGoalId = parent.Id,
        CargoId = pair.Cargo,
        SourceEntityId = pair.Source.Id,
        DestEntityId = pair.Dest.Id,
        UnitShare = share,
    };

    static List<Entity> Markets(Entity fleet, Goal goal, bool freight, bool contract)
    {
        var list = new List<Entity>();
        if (fleet.Manager == null)
            return list;
        foreach (var market in fleet.Manager.GetAllEntitiesWithDataBlob<LogiBaseDB>())
        {
            if (market.Id == fleet.Id)
                continue;
            if (freight)
            {
                if (market.FactionOwnerID != fleet.FactionOwnerID || !market.HasDataBlob<ColonyInfoDB>())
                    continue;
                if (contract && market.Id != goal.SourceEntityId && market.Id != goal.DestEntityId)
                    continue;
            }
            else if (!FactionStanceRules.CanTrade(fleet.Manager.Game, fleet.FactionOwnerID, market.FactionOwnerID))
                continue;

            if (!contract && !InSpan(fleet, goal.TargetEntityID, market))
                continue;
            list.Add(market);
        }
        list.Sort((a, b) => a.Id.CompareTo(b.Id));
        return list;
    }

    static bool InSpan(Entity fleet, int anchorId, Entity market)
    {
        if (fleet.Manager == null || !fleet.Manager.TryGetEntityById(anchorId, out var anchor))
            return false;
        var span = CommandSpan.Of(fleet);
        if (span == CommandSpanKind.System)
            return market.Manager == fleet.Manager;
        if (market.Id == anchor.Id)
            return true;
        if (!market.TryGetDataBlob<PositionDB>(out var pos))
            return false;
        if (pos.Parent == anchor)
            return true;
        if (span != CommandSpanKind.Well || pos.Parent == null)
            return false;
        return anchor.TryGetDataBlob<PositionDB>(out var anchorPos) && anchorPos.Children.Contains(pos.Parent);
    }

    static IEnumerable<Pair> TradePairs(Entity ship, List<Entity> markets)
    {
        foreach (var from in markets)
        {
            if (!from.TryGetDataBlob<LogiBaseDB>(out var book))
                continue;
            foreach (var sell in book.Listings.Values.OrderBy(listing => listing.CargoId, StringComparer.Ordinal))
            {
                if (sell.SellQuantity <= 0 || string.IsNullOrEmpty(sell.CargoId))
                    continue;
                if (!MarketRun.TryShipGood(ship, sell.CargoId, out _))
                    continue;
                foreach (var to in markets)
                {
                    if (to.Id == from.Id)
                        continue;
                    if (!MarketBook.TryGet(to, sell.CargoId, out var buy) || buy.BuyQuantity <= 0)
                        continue;
                    yield return new Pair
                    {
                        Source = from,
                        Dest = to,
                        Cargo = sell.CargoId,
                        Key = new PairKey(sell.CargoId, from.Id, to.Id),
                    };
                }
            }
        }
    }

    static IEnumerable<Pair> FreightPairs(List<Entity> markets)
    {
        foreach (var from in markets)
        {
            if (!from.TryGetDataBlob<LogiBaseDB>(out var book))
                continue;
            foreach (var sell in book.Listings.Values.OrderBy(listing => listing.CargoId, StringComparer.Ordinal))
            {
                if (sell.SellQuantity <= 0 || string.IsNullOrEmpty(sell.CargoId))
                    continue;
                foreach (var to in markets)
                {
                    if (to.Id == from.Id)
                        continue;
                    if (!MarketBook.TryGet(to, sell.CargoId, out var buy) || buy.BuyQuantity <= 0)
                        continue;
                    yield return new Pair
                    {
                        Source = from,
                        Dest = to,
                        Cargo = sell.CargoId,
                        Key = new PairKey(sell.CargoId, from.Id, to.Id),
                    };
                }
            }
        }
    }

    static double TradeScore(Entity ship, Pair pair)
    {
        if (!MarketBook.TryGet(pair.Source, pair.Cargo, out var sell)
            || !MarketBook.TryGet(pair.Dest, pair.Cargo, out var buy))
            return 0;
        return (double)(buy.Bid - sell.Ask) - Hours(ship, pair.Source, pair.Dest);
    }

    static double Hours(Entity ship, Entity source, Entity dest)
    {
        if (!ship.TryGetDataBlob<WarpAbilityDB>(out var warp) || warp.MaxSpeed <= 0)
            return NoWarpHours;
        double meters = MarketRun.DistanceMeters(source, dest);
        if (!double.IsFinite(meters) || meters < 0 || meters >= double.MaxValue)
            return NoWarpHours;
        return meters / warp.MaxSpeed / 3600.0;
    }

    static long Remaining(Entity fleet, Pair pair, Dictionary<PairKey, long> claimed)
    {
        if (!MarketBook.TryGet(pair.Source, pair.Cargo, out var sell)
            || !MarketBook.TryGet(pair.Dest, pair.Cargo, out var buy))
            return 0;
        if (!TryCargo(fleet, pair.Cargo, out var good))
            return 0;
        long available = Math.Min(sell.SellQuantity, MarketBook.Sellable(pair.Source, sell, good));
        long posted = Math.Min(available, buy.BuyQuantity);
        long left = posted - claimed.GetValueOrDefault(pair.Key);
        return left > 0 ? left : 0;
    }

    static bool TryCargo(Entity entity, string cargoId, out ICargoable good)
    {
        good = null!;
        var library = entity.GetFactionCargoDefinitions();
        if (library == null || !library.Contains(cargoId))
            return false;
        var found = library.GetAny(cargoId);
        if (found == null)
            return false;
        good = found;
        return true;
    }

    static long FreeSpace(Entity ship, string cargoId)
    {
        if (!MarketRun.TryShipGood(ship, cargoId, out var good))
            return 0;
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var store))
            return 0;
        return Math.Max(0, store.GetFreeUnitSpace(good));
    }

    static Entity Closest(List<Entity> free, Pair pair)
    {
        Entity best = null;
        double bestDist = double.MaxValue;
        foreach (var ship in free)
        {
            if (FreeSpace(ship, pair.Cargo) <= 0)
                continue;
            double dist = MarketRun.DistanceMeters(ship, pair.Source);
            if (best == null || dist < bestDist || (dist == bestDist && ship.Id < best.Id))
            {
                best = ship;
                bestDist = dist;
            }
        }
        return best;
    }

    static bool LosesTie(Pair pair, Pair best)
    {
        int source = pair.Source.Id.CompareTo(best.Source.Id);
        if (source != 0)
            return source > 0;
        int dest = pair.Dest.Id.CompareTo(best.Dest.Id);
        if (dest != 0)
            return dest > 0;
        return string.CompareOrdinal(pair.Cargo, best.Cargo) > 0;
    }

    static bool IsCargoShip(Entity ship)
    {
        if (!ship.TryGetDataBlob<ShipInfoDB>(out var info) || info.Tanker)
            return false;
        if (!ship.HasDataBlob<CargoStorageDB>())
            return false;
        return ship.HasDataBlob<WarpAbilityDB>() || ship.HasDataBlob<NewtonThrustAbilityDB>();
    }

    static bool IsFree(Entity ship, Goal parent)
    {
        var active = ship.TryGetDataBlob<GoalsDB>(out var goals) ? goals.ActiveGoal : null;
        if (active == null || active.Status == GoalStatus.Completed)
            return true;
        if (active.Status == GoalStatus.Failed)
            return active.ParentGoalId != parent.Id;
        return false;
    }

    static Goal OurGoal(Entity ship, Goal parent)
    {
        if (!ship.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return null;
        return goals.ActiveGoal.ParentGoalId == parent.Id ? goals.ActiveGoal : null;
    }

    static bool TryGetFleetTanker(FleetDB fleetDB, out Entity tanker)
    {
        tanker = null;
        foreach (var child in fleetDB.Children)
        {
            if (!child.TryGetDataBlob<ShipInfoDB>(out var info) || !info.Tanker)
                continue;
            tanker = child;
            return true;
        }
        return false;
    }

    static bool TankerAlreadyTasked(Entity tanker, Goal parent, int anchorId)
    {
        if (!tanker.TryGetDataBlob<GoalsDB>(out var goals) || goals.ActiveGoal == null)
            return false;
        var active = goals.ActiveGoal;
        if (active.ParentGoalId != parent.Id || active.Status is GoalStatus.Completed or GoalStatus.Failed)
            return false;
        return active.Type == GoalType.MoveTo && active.TargetEntityID == anchorId;
    }
}
