using GameEngine.Damage;
using Pulsar4X.Sensors;

namespace Pulsar4X.Blueprints;

//defaults from stainless steel
public class ParticleMaterialBlueprint : Blueprint
{
    public uint PartMatID;
    public float Elasticity = 0.5f; //0 to 1 this is how elastic collisions will be
    public float TensileStrength = 110; //"ultimate" material tensile strenghth here is probibly fine.  
    public float ThermalCapacity = 500;
    public float ThermalConductivity = 16.3f;
    public float MeltingZeroPoint = 1673;
    public PhasePoint TriplePoint = new PhasePoint(0.00001f, 1673);
    public PhasePoint CriticalPoint = new PhasePoint(30000, 20000);
    public EMWaveForm PhotonReflectivity = new EMWaveForm(300, 550, 750);
    public float PhotonReflectivityPeak = 0.85f;
    public EMWaveForm PhotonTransparency = new EMWaveForm(800, 1500, 2000);
    public float PhotonTransparencyPeak = 0.01f;
}