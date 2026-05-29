using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Galaxy;

/// <summary>
/// Declares the surface-gravity range (in m/s²) a piece of infrastructure can
/// operate within. A body whose gravity falls outside [MinGravity, MaxGravity]
/// receives no population support from this design. Earth standard = 9.81 m/s².
/// </summary>
public class GravityToleranceAtb : IComponentDesignAttribute
{
    public double MinGravity;
    public double MaxGravity;

    public GravityToleranceAtb() { }

    public GravityToleranceAtb(double minGravity, double maxGravity)
    {
        MinGravity = minGravity;
        MaxGravity = maxGravity;
    }

    public bool SupportsBodyGravity(double bodyGravityMps2)
    {
        return bodyGravityMps2 >= MinGravity && bodyGravityMps2 <= MaxGravity;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance) { }
    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance) { }

    public string AtbName() => "Gravity Tolerance";

    public string AtbDescription()
        => "Operates between " + MinGravity.ToString("0.00") + " and " + MaxGravity.ToString("0.00") + " m/s²";
}
