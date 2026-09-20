using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Galaxy;

/// <summary>
/// Declares the atmospheric pressure range (in Earth atmospheres, 1.0 = Earth)
/// a piece of infrastructure can operate within. A body whose surface pressure
/// falls outside [MinPressure, MaxPressure] receives no population support from
/// this design.
/// </summary>
public class PressureToleranceAtb : IComponentDesignAttribute
{
    public double MinPressure;
    public double MaxPressure;

    public PressureToleranceAtb() { }

    public PressureToleranceAtb(double minPressure, double maxPressure)
    {
        MinPressure = minPressure;
        MaxPressure = maxPressure;
    }

    public bool SupportsBodyPressure(double bodyPressureAtm)
    {
        return bodyPressureAtm >= MinPressure && bodyPressureAtm <= MaxPressure;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance) { }
    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance) { }

    public string AtbName() => "Atmospheric Pressure Tolerance";

    public string AtbDescription()
        => "Operates between " + MinPressure.ToString("0.00") + " atm and " + MaxPressure.ToString("0.00") + " atm";
}
