namespace Pulsar4X.Blueprints;

public class ParticleMaterialBlueprint : Blueprint
{
    public uint PartMatID;
    public float ThermalCapacity;
    public float ThermalConductivity;
    public float MeltingZeroPoint;
    public (float bar, float kelvin) TriplePoint;
    public (float bar, float kelvin) CriticalPoint;
}