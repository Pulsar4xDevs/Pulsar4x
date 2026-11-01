using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Industry;
using Pulsar4X.Movement;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>Who mounts a Bootstrap crate package's crates, and the capacity each side had when that was decided.</summary>
internal readonly record struct MountChoice(bool Possible, bool ByShip, long ShipFree, long ColonyFree, bool ShipCanBuild, bool ColonyCanBuild)
{
    /// <summary>One line for the order's message and the trip's name.</summary>
    public string Describe()
    {
        if (!Possible)
            return "Neither the colony nor the ship can mount crates";
        if (ShipCanBuild && ColonyCanBuild)
            return ByShip
                ? $"The ship's Construction Bay mounts the crates ({ShipFree} free build points a day against the colony's {ColonyFree})"
                : $"The colony mounts the crates ({ColonyFree} free build points a day against the ship's {ShipFree})";
        return ByShip
            ? "The ship's Construction Bay mounts the crates (the colony has no construction capacity)"
            : "The colony mounts the crates (the ship has no Construction Bay)";
    }
}

/// <summary>
/// What the fleet Bootstrap order puts in its sealed packages and how the crates get mounted (decisions 15 to 17).
/// The order seals the crates first, then the resources the site builds cost, each in its own
/// <see cref="ShipmentBook.TrySeal"/> call so a package never mixes the two. Crates arrive before resources.
/// </summary>
internal static class BootstrapPackage
{
    /// <summary><see cref="ShipmentCargo.FleetOrder"/> of every package this order seals, followed by the goal id.</summary>
    public const string OrderPrefix = "bootstrap ";

    // ------------------------------------------------------------------ capacity

    /// <summary>
    /// Free construction capacity of a colony or a ship's Construction Bay, in build points a day: the points it
    /// makes in a day less the points still owed on its queued jobs. It can be negative, so a longer backlog
    /// ranks lower. <paramref name="canBuild"/> is false when it makes no points at all.
    /// </summary>
    public static long FreeBuildPoints(Entity builder, out bool canBuild)
    {
        canBuild = false;
        if (!builder.TryGetDataBlob<LocalConstructionDB>(out var bay) || bay.PointsPerDay <= 0)
            return 0;
        canBuild = true;
        long owed = 0;
        foreach (var job in bay.BuildQueue)
            owed += Math.Max(0, job.PointCost - job.PointsAccumulated);
        return bay.PointsPerDay - owed;
    }

    /// <summary>
    /// Whichever has more free construction capacity mounts the crates: this ship's Construction Bay or the
    /// destination colony. A tie goes to the colony. A side that makes no build points never mounts.
    /// </summary>
    public static MountChoice Choose(Entity ship, Entity dest)
    {
        long shipFree = FreeBuildPoints(ship, out bool shipCan);
        long colonyFree = FreeBuildPoints(dest, out bool colonyCan);
        bool byShip = shipCan && (!colonyCan || shipFree > colonyFree);
        return new MountChoice(shipCan || colonyCan, byShip, shipFree, colonyFree, shipCan, colonyCan);
    }

    // ------------------------------------------------------------------ seal

    /// <summary>
    /// Seal the order's crates and resources at the source. Crates go in one package per cargo type, then the
    /// resources, split into hold-sized lines so a ship can take them. Every package is released again when any
    /// part fails, so the stock is untouched. The list is in the order the fleet should hand the packages out.
    /// </summary>
    public static bool TrySeal(Entity fleet, Goal goal, Entity source, Entity dest,
        out List<ShipmentCargo> made, out string reason)
    {
        made = new List<ShipmentCargo>();
        reason = "";
        var game = fleet.Manager.Game;
        if (!game.Factions.TryGetValue(fleet.FactionOwnerID, out var faction)
            || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
        {
            reason = "The fleet has no faction.";
            return false;
        }
        var stock = source.GetDataBlob<CargoStorageDB>();
        string order = OrderPrefix + goal.Id;

        // Crates, in the player's priority order.
        var crates = new List<(ICargoable item, long units)>();
        var used = new HashSet<int>();
        foreach (var line in goal.BootstrapCrates)
        {
            string name = info.ComponentDesigns.TryGetValue(line.DesignId, out var design) ? design.Name : line.DesignId;
            int found = 0;
            foreach (var crate in BootstrapColonyPlan.CratesOf(source, line.DesignId))
            {
                if (found >= line.Count)
                    break;
                if (!used.Add(crate.ID))
                    continue;
                crates.Add((crate, 1));
                found++;
            }
            if (found < line.Count)
            {
                reason = $"no finished crates of {name} at source";
                return false;
            }
        }

        // Resources: what the site builds cost, from source cargo only.
        var wanted = new List<(ICargoable item, long amount)>();
        if (info.Data?.CargoGoods != null)
        {
            foreach (var (cargoId, amount) in BootstrapResources.Derive(info, goal.BootstrapSiteBuilds))
            {
                if (!info.Data.CargoGoods.Contains(cargoId))
                    continue;
                var item = info.Data.CargoGoods.GetAny(cargoId)!;
                if (stock.GetUnitsStored(item, false) < amount)
                {
                    reason = $"not enough {item.Name} at source";
                    return false;
                }
                wanted.Add((item, amount));
            }
        }

        var ships = HaulShips(fleet);
        var rounds = new List<List<(ICargoable item, long units)>>();
        if (crates.Count > 0)
            rounds.Add(crates);
        var chunks = new List<List<(ICargoable item, long units)>>();
        foreach (var (item, amount) in wanted)
        {
            long chunk = amount;
            if (ships.Count > 0)
            {
                chunk = LargestLine(ships, item);
                if (chunk <= 0)
                {
                    reason = item.Name + " does not fit a ship's hold.";
                    return false;
                }
            }
            long placed = 0;
            for (int i = 0; placed < amount; i++)
            {
                long take = Math.Min(chunk, amount - placed);
                while (chunks.Count <= i)
                    chunks.Add(new List<(ICargoable item, long units)>());
                chunks[i].Add((item, take));
                placed += take;
            }
        }
        rounds.AddRange(chunks);

        foreach (var round in rounds)
        {
            if (!ShipmentBook.TrySeal(source, dest, round, order, out var sealed1, out reason))
            {
                foreach (var package in made)
                    ShipmentBook.Release(game, package);
                made = new List<ShipmentCargo>();
                return false;
            }
            made.AddRange(sealed1);
        }
        return true;
    }

    /// <summary>The fleet's ships that could carry a package: captained, with a hold and a drive.</summary>
    static List<Entity> HaulShips(Entity fleet)
        => FleetChildDuty.Descendants(fleet)
            .Where(e => e.HasDataBlob<ShipInfoDB>() && !e.HasDataBlob<FleetDB>() && e.HasDataBlob<CargoStorageDB>()
                && (e.HasDataBlob<WarpAbilityDB>() || e.HasDataBlob<NewtonThrustAbilityDB>()))
            .ToList();

    /// <summary>The most units of a good the biggest hold among these ships could carry in one line. 0 when none can.</summary>
    static long LargestLine(List<Entity> ships, ICargoable item)
    {
        long best = 0;
        if (item.VolumePerUnit <= 0)
            return 0;
        foreach (var ship in ships)
        {
            var hold = ship.GetDataBlob<CargoStorageDB>();
            if (!hold.Accepts(item) || !hold.TypeStores.TryGetValue(item.CargoTypeID, out var store))
                continue;
            best = Math.Max(best, (long)Math.Floor((store.MaxVolume + 1e-9) / item.VolumePerUnit));
        }
        return best;
    }

    // ------------------------------------------------------------------ the order's packages

    /// <summary>The goal's packages that are still held at the source, in the order they were sealed.</summary>
    public static List<ShipmentCargo> Open(Game game, Goal goal)
    {
        var found = new List<ShipmentCargo>();
        if (goal.SourceEntityId < 0 || !game.GlobalManager.TryGetGlobalEntityById(goal.SourceEntityId, out var source))
            return found;
        foreach (var id in goal.BootstrapPackageIds)
        {
            if (ShipmentBook.TryFind(source, id, out var package))
                found.Add(package);
        }
        return found;
    }

    /// <summary>Give every package of the goal back to open stock. Crates already out stay as plain crates, unmarked.</summary>
    public static void ReleaseAll(Game game, Goal goal)
    {
        foreach (var package in Open(game, goal))
            ShipmentBook.Release(game, package);
    }

    public static bool IsCratePackage(ShipmentCargo package)
        => package.Lines.Count > 0 && package.Lines.All(line => line.IsCrate);
}
