using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Industry;

/// <summary>
/// Marks a component design as "infrastructure": the underlying support a body
/// provides to everything built on it. Each installed unit contributes
/// <see cref="Capacity"/> to the colony's <see cref="InfrastructureDB.CapacityProvided"/>,
/// gated by the design's gravity/pressure tolerances exactly like population support.
/// </summary>
public class InfrastructureCapacityAtb : IComponentDesignAttribute
{
    /// <summary>Support capacity a single unit of this infrastructure provides.</summary>
    public long Capacity { get; set; }

    public InfrastructureCapacityAtb() { }

    public InfrastructureCapacityAtb(double capacity)
    {
        Capacity = (long)capacity;
    }

    // Provided/required totals are summed by InfrastructureProcessor.RecalcCapacity,
    // which is driven off the colony's ComponentInstancesDB recalc, so these are no-ops.
    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance) { }
    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance) { }

    public string AtbName() => "Infrastructure";

    public string AtbDescription()
        => "Provides underlying support capacity for the colony's installations.";
}
