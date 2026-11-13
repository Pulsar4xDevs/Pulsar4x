using System;
using System.Linq;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// A ship's power totals with life support in them, for the Power tab summary, its header state, the
/// Summary tab's power line and the rings, so they agree with the Loads rows. The Api keeps life support
/// out of <see cref="ShipPowerView.Demand_kW"/>, <see cref="ShipPowerView.TotalDemand_kW"/> and
/// <see cref="ShipPowerView.Delivered_kW"/>; it is added back here once, from the same figures as the
/// Life support row (<see cref="LifeSupportRows.Demand_kW"/>).
/// </summary>
public static class ShipPowerTotals
{
    /// <summary>All demand, kW: warp and other loads (<see cref="ShipPowerView.TotalDemand_kW"/>) plus life support.</summary>
    public static double Demand_kW(ShipPowerView p) => p.TotalDemand_kW + LifeSupportRows.Demand_kW(p);

    /// <summary>Must-run demand, kW: warp plus life support (both served before charging and other loads).</summary>
    public static double MustRun_kW(ShipPowerView p) => p.Demand_kW + LifeSupportRows.Demand_kW(p);

    /// <summary>Generation over the must-run demand, 0..1 (the entity card's ring); 1 when nothing must run.</summary>
    public static double GenerationShare(ShipPowerView p)
    {
        double mustRun = MustRun_kW(p);
        return mustRun > 0 ? Math.Clamp(p.Generation_kW / mustRun, 0, 1) : 1;
    }

    /// <summary>Life support powered now, kW: the engine's figure (or its load group's), never above the life-support demand.</summary>
    public static double LifeSupportServed_kW(ShipPowerView p)
    {
        double demand = LifeSupportRows.Demand_kW(p);
        double served = p.LifeSupport_kW > 0
            ? p.LifeSupportServed_kW
            : p.LoadGroups.FirstOrDefault(LifeSupportRows.IsLifeSupportGroup)?.Served_kW ?? 0;
        return Math.Clamp(served, 0, demand);
    }

    /// <summary>
    /// Warp and other loads served now, kW, without life support. Rebuilt from its parts rather than read
    /// from <see cref="ShipPowerView.Delivered_kW"/>, so life support cannot be counted twice should that
    /// property ever include it. Warp shares the must-run supply with life support alike (the engine's rule,
    /// see <see cref="ShipPowerView.LifeSupportServedFraction"/>); without life support it is warp up to generation.
    /// </summary>
    public static double OtherServed_kW(ShipPowerView p)
    {
        double warp = LifeSupportRows.Demand_kW(p) > 0
            ? p.Demand_kW * Math.Clamp(p.LifeSupportServedFraction, 0, 1)
            : Math.Min(p.Demand_kW, p.Generation_kW);
        return Math.Max(0, warp) + Math.Clamp(p.AuxServed_kW, 0, Math.Max(0, p.AuxDemand_kW));
    }

    /// <summary>What reaches the loads now, kW: life support, warp and the other loads served. Charging is not counted.</summary>
    public static double Served_kW(ShipPowerView p) => Math.Min(Demand_kW(p), OtherServed_kW(p) + LifeSupportServed_kW(p));

    /// <summary>Served over all demand, 0..1; 1 when nothing draws power.</summary>
    public static double Share(ShipPowerView p)
    {
        double demand = Demand_kW(p);
        return demand > 0 ? Math.Clamp(Served_kW(p) / demand, 0, 1) : 1;
    }
}
