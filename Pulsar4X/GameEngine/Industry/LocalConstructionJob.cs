using System;
using System.Collections.Generic;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Industry;

/// <summary>
/// One item on a colony's installation queue. A mount names a warehouse crate.
/// An assemble names a site-only design and keeps its own copy of the recipe.
/// </summary>
public class LocalConstructionJob
{
    /// <summary>
    /// The component design being constructed.
    /// </summary>
    public ComponentDesign Design { get; set; }

    /// <summary>
    /// The warehouse or hold instance this job will mount. Null for an assemble.
    /// </summary>
    public ComponentInstance? Instance { get; set; }

    /// <summary>
    /// Colony this job mounts on. Zero means the entity that owns the queue.
    /// A ship job names the colony. A colony job leaves this at zero and mounts on itself.
    /// </summary>
    public int TargetEntityId { get; set; }

    /// <summary>
    /// Installation points already applied. The total owed is <see cref="PointCost"/>, copied at queue time.
    /// </summary>
    public long PointsAccumulated { get; set; }

    /// <summary>
    /// Installation points this job costs. A mount copies <see cref="MountCost"/>. An assemble copies
    /// <see cref="ComponentDesign.IndustryPointCosts"/>. A later edit to the design does not change it.
    /// </summary>
    public long PointCost { get; set; }

    /// <summary>
    /// Recipe still to consume. Empty for a mount. Copied from the design when an assemble is queued.
    /// </summary>
    public Dictionary<string, long> ResourcesRemaining { get; set; } = new();

    /// <summary>Mount or Assemble.</summary>
    public string Kind => Instance != null ? "Mount" : "Assemble";

    public LocalConstructionJob(ComponentDesign design, ComponentInstance? instance = null, int pointsAccumulated = 0)
    {
        Design = design;
        Instance = instance;
        PointsAccumulated = pointsAccumulated;
        if (instance != null)
        {
            PointCost = MountCost(instance.MassPerUnit);
            ResourcesRemaining = new Dictionary<string, long>();
        }
        else
        {
            PointCost = design.IndustryPointCosts;
            ResourcesRemaining = new Dictionary<string, long>(design.ResourceCosts);
        }
    }

    /// <summary>
    /// Installation points to mount one crate. The divisor lives here.
    /// A default factory (mass 5,000,000) costs 50. Infrastructure (mass 1,000) costs 1.
    /// </summary>
    public static long MountCost(long massPerUnit) => Math.Max(1, massPerUnit / 100_000);

    public static bool IsCrate(ComponentDesign design)
        => design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation)
           && design.ComponentMountType.HasFlag(ComponentMountType.ShipCargo);

    public static bool IsSiteOnly(ComponentDesign design)
        => design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation)
           && !design.ComponentMountType.HasFlag(ComponentMountType.ShipCargo);

    public bool IsComplete => PointCost > 0 && PointsAccumulated >= PointCost;

    /// <summary>
    /// Progress on the current item (0.0 to 1.0).
    /// </summary>
    public double CurrentItemProgress
    {
        get
        {
            if (PointCost <= 0) return 0;
            return (double)PointsAccumulated / PointCost;
        }
    }
}
