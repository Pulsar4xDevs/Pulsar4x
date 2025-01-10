using GameEngine.Damage;
using Pulsar4X.Sensors;

namespace Pulsar4X.Blueprints;

public class ParticleMaterialBlueprint : Blueprint
{
    public uint PartMatID;
    public float ThermalCapacity;
    public float ThermalConductivity;
    public float MeltingZeroPoint;
    public PhasePoint TriplePoint;
    public PhasePoint CriticalPoint;
    public EMWaveForm PhotonReflectivity;
    public float PhotonReflectivityPeak;
    public EMWaveForm PhotonTransparency;
    public float PhotonTransparencyPeak;
}