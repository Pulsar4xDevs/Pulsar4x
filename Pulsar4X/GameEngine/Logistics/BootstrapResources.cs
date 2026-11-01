using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>One resource the fleet Bootstrap order still has to haul, and how much.</summary>
public sealed record BootstrapResourceLine(ICargoable Cargo, long Amount);

/// <summary>
/// Which resources the fleet Bootstrap order hauls (decision 12). The player does not pick them. They are
/// what the site-only facilities the player wants built at the destination cost, times the quantity.
/// </summary>
public static class BootstrapResources
{
    /// <summary>
    /// The construction cost of the listed facilities, summed per resource (cargo id). Designs the faction
    /// does not have, and costs of nothing, are left out.
    /// </summary>
    public static Dictionary<string, long> Derive(FactionInfoDB info, IEnumerable<BootstrapCrateLine> siteBuilds)
    {
        var total = new Dictionary<string, long>();
        foreach (var build in siteBuilds)
        {
            if (build.Count <= 0 || !info.ComponentDesigns.TryGetValue(build.DesignId, out var design))
                continue;
            foreach (var (cargoId, amount) in design.ResourceCosts)
            {
                if (amount <= 0)
                    continue;
                total[cargoId] = total.GetValueOrDefault(cargoId) + amount * build.Count;
            }
        }
        return total;
    }

    /// <summary>
    /// What is still to haul: the derived total less what haulers were already given. Resources this faction
    /// has no definition for are skipped. In the order the facilities are listed.
    /// </summary>
    internal static List<BootstrapResourceLine> Remaining(Entity fleet, Goal goal)
    {
        var lines = new List<BootstrapResourceLine>();
        if (!fleet.Manager.Game.Factions.TryGetValue(fleet.FactionOwnerID, out var faction)
            || !faction.TryGetDataBlob<FactionInfoDB>(out var info)
            || info.Data?.CargoGoods == null)
            return lines;

        foreach (var (cargoId, amount) in Derive(info, goal.BootstrapSiteBuilds))
        {
            if (!info.Data.CargoGoods.Contains(cargoId))
                continue;
            long left = amount - goal.BootstrapHauled.GetValueOrDefault(cargoId);
            if (left > 0)
                lines.Add(new BootstrapResourceLine(info.Data.CargoGoods.GetAny(cargoId)!, left));
        }
        return lines;
    }

    /// <summary>A finished, failed or cancelled haul returns what it did not deliver to what is still needed.</summary>
    internal static void GiveBack(Entity ship, Goal goal, Goal trip)
    {
        if (!ship.Manager.Game.Factions.TryGetValue(ship.FactionOwnerID, out var faction)
            || !faction.TryGetDataBlob<FactionInfoDB>(out var info)
            || info.Data?.CargoGoods == null)
            return;
        foreach (var line in trip.MoveGoodsLines)
        {
            long undelivered = Math.Max(0, line.Requested - line.Delivered);
            if (undelivered == 0 || !info.Data.CargoGoods.Contains(line.ItemId))
                continue;
            var cargo = info.Data.CargoGoods.GetAny(line.ItemId);
            goal.BootstrapHauled[cargo.UniqueID] = Math.Max(0, goal.BootstrapHauled.GetValueOrDefault(cargo.UniqueID) - undelivered);
        }
    }
}
