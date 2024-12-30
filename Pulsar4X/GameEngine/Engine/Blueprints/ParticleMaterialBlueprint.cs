using GameEngine.Damage;

namespace Pulsar4X.Blueprints;

public class ParticleMaterialBlueprint : Blueprint
{
    public uint PartMatID;
    public float ThermalCapacity;
    public float ThermalConductivity;
    public float MeltingZeroPoint;
    public PhasePoint TriplePoint;
    public PhasePoint CriticalPoint;
}