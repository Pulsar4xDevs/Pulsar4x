using System;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using Vector3 = System.Numerics.Vector3;

namespace Pulsar4X.SDL2UI.ManuverNodes;

public class ManuverNode
{
    /// <summary>
    /// Raises and lowers altitude of orbit 
    /// Positive Prograde,
    /// Negative Retrograde
    /// </summary>
    public double Prograde { get; private set; } = 0;

    /// <summary>
    /// Inclination change
    /// Positive Normal,
    /// Negative Antinormal
    /// </summary>
    public double Normal { get; private set; } = 0;

    /// <summary>
    /// Rotates the orbit around the craft, changing the position of the apoapsis/periapsis along the line of the orbit
    /// normally inefficient way to change orbit, but good for small adjustments. 
    /// This points towards the center of the ellipse not the focal (parent body)
    /// Positive in,
    /// Negative out.
    /// </summary>
    public double Radial { get; private set; } = 0;

    public double DeltaVCost;
    public double DeltaVRemaining;

    public double FuelCostTotal;
    public double FuelCostRemaining;

    public double BurnTimeTotal;
    public double BurnTimeRemaining;

    public DateTime NodeTime;
    public DateTime TimeAtStartBurn;

    private Orbital.Vector3 _nodePosition;

    private Entity _orderEntity;
    private NewtonThrustAbilityDB _newtonThrust;
    private double _totalMass;
    private double _dryMass;
    private double _sgp;
    private ICargoable _fuelType;

    private double _burnRate;
    private double _exhaustVelocity;

    public KeplerElements TargetOrbit;

    public ManuverNode(Entity orderEntity, DateTime nodeTime)
    {
        NodeTime = nodeTime;
        _orderEntity = orderEntity;
        _newtonThrust = _orderEntity.GetDataBlob<NewtonThrustAbilityDB>();
        _totalMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassTotal;
        _dryMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassDry;
        var parentMass = _orderEntity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
        _sgp = GeneralMath.StandardGravitationalParameter(_totalMass + parentMass);
        var fuelTypeID = _newtonThrust.FuelType;
        _fuelType = StaticRefLib.StaticData.CargoGoods.GetAny(fuelTypeID);
        _burnRate = _newtonThrust.FuelBurnRate;
        _exhaustVelocity = _newtonThrust.ExhaustVelocity;
        
        TargetOrbit = orderEntity.GetDataBlob<OrbitDB>().GetElements();
        _nodePosition = OrbitalMath.GetRelativePosition(TargetOrbit, NodeTime);

    }

    public void ManipulateNode(double prograde, double radial, double normal,  double time = 0)
    {
        Prograde += prograde;
        Radial += radial;
        Normal += normal;
        NodeTime += TimeSpan.FromSeconds(time);
        
        double dv = Math.Sqrt((normal * normal) + (prograde * prograde) + (radial * radial));
        DeltaVCost += dv;

        FuelCostTotal = OrbitalMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, DeltaVCost);
        FuelCostRemaining = FuelCostTotal;

        BurnTimeTotal = FuelCostTotal / _burnRate;
        BurnTimeRemaining = BurnTimeTotal;

        var firsthalfDvFuel = OrbitalMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, dv * 0.5);
        var firsthalfBurnTime = firsthalfDvFuel / _burnRate;
        TimeAtStartBurn = NodeTime - TimeSpan.FromSeconds(firsthalfBurnTime);
        (Orbital.Vector3 position, Vector2 velocity) stateVectors = OrbitalMath.GetStateVectors(TargetOrbit, NodeTime);
        
        Orbital.Vector3 velocity = new Orbital.Vector3(stateVectors.velocity.X, stateVectors.velocity.Y, 0);
        velocity += OrbitalMath.ProgradeToStateVector(new(radial, prograde, normal), TargetOrbit);
        
        TargetOrbit =  OrbitalMath.KeplerFromPositionAndVelocity(_sgp, _nodePosition, velocity, NodeTime);
    }

    /// <summary>
    /// Manipulate node with vector3
    /// </summary>
    /// <param name="burn">
    /// x: Radial
    /// y: Prograde
    /// z: normal
    /// </param>
    /// <param name="time"></param>
    public void ManipulateNode(System.Numerics.Vector3 burn, double time)
    {
        ManipulateNode(burn.Y, burn.X, burn.Z, time);
    }

    /// <summary>
    /// Manipulate node with vector3
    /// </summary>
    /// <param name="burn">
    /// x: Radial
    /// y: prograde
    /// z: normal
    /// </param>
    /// <param name="time"></param>
    public void ManipulateNode(Orbital.Vector3 burn, double time)
    {
        ManipulateNode(burn.Y, burn.X, burn.Z, time);
    }

    /// <summary>
    /// Manipulate node with vector2
    /// </summary>
    /// <param name="burn">
    /// x: Radial
    /// y: prograde
    /// </param>
    /// <param name="time"></param>
    public void ManipulateNode(System.Numerics.Vector2 burn, double time)
    {
        ManipulateNode(burn.Y, burn.X, 0, time);
    }
    
    /// <summary>
    /// Manipulate node with vector2
    /// </summary>
    /// <param name="burn">
    /// x: Radial
    /// y: prograde
    /// </param>
    /// <param name="time"></param>
    public void ManipulateNode(Orbital.Vector2 burn, double time)
    {
        ManipulateNode(burn.Y, burn.X, 0, time);
    }

}