using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Which resources the fleet Bootstrap order seals and hauls (decisions 12 and 17). The player does not pick them. They are
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
}
