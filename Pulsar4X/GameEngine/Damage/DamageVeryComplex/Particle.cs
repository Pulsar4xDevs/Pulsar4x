using Pulsar4X.Orbital;

namespace GameEngine.Damage;

/// <summary>
/// maybe this should be a stuct and only hold non mutable data, constructed from an interface on the mineral/material
/// </summary>
public interface IDamageMaterial
{
    float SpecificHeatCapacity { get; }
    float Density { get; } //as a solid
    float MeltingZeroPoint { get; }
    (float bar, float kelvin) TripplePoint { get; }
    (float bar, float kelvin) CriticalPoint { get; }
}


public enum PhaseState
{
    Solid,
    Liquid,
    Gas,
    Plasma
}

public class Particle
{
    public IDamageMaterial MatType;
    public PhaseState StateOfPhase = PhaseState.Solid;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Temperature;
    public int Life;
    public float Mass;
    public DamageMap _pMap;
    
    public Particle(IDamageMaterial matType, Vector2 position, Vector2 velocity = default)
    {
        MatType = matType;
        Position = position;
        Velocity = velocity;
        Temperature = 293.15f; // Room temperature in Kelvin
        Life = 100; // Arbitrary starting life
    }
}
