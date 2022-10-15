using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using Vector3 = System.Numerics.Vector3;

namespace Pulsar4X.SDL2UI.ManuverNodes;

public class ManuverNode
{
    public string NodeName = "";
    /// <summary>
    /// This descibes the center of the burn in DeltaV 
    /// </summary>
    public DateTime NodeTime;
    /// <summary>
    /// This is the time we should start the burn
    /// </summary>
    public DateTime TimeAtStartBurn; 
    
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

    public Vector2 TargetVelocity;

    /// <summary>
    /// Ralitive to parent
    /// </summary>
    internal Orbital.Vector3 NodePosition;

    /// <summary>
    /// Angle of position
    /// </summary>
    public double GetNodeAnomaly
    {
        get { return Angle.RadiansFromVector3(NodePosition); }
    }
    
    internal Entity _orderEntity;
    private NewtonThrustAbilityDB _newtonThrust;
    private double _totalMass;
    private double _dryMass;
    private double _sgp;
    private ICargoable _fuelType;

    private double _burnRate;
    private double _exhaustVelocity;
    public KeplerElements PriorOrbit;
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
        
        PriorOrbit = orderEntity.GetDataBlob<OrbitDB>().GetElements();
        TargetOrbit = PriorOrbit;
        NodePosition = OrbitalMath.GetRelativePosition(PriorOrbit, NodeTime);
        TargetVelocity = OrbitalMath.GetStateVectors(TargetOrbit, nodeTime).velocity;
    }

    /// <summary>
    /// Adds parameters to exsisting node
    /// </summary>
    /// <param name="prograde"></param>
    /// <param name="radial"></param>
    /// <param name="normal"></param>
    /// <param name="time"></param>
    public void ManipulateNode(double prograde, double radial, double normal,  double time = 0)
    {
        Prograde += prograde;
        Radial += radial;
        Normal += normal;
        NodeTime += TimeSpan.FromSeconds(time);
        NodePosition = OrbitalMath.GetRelativePosition(PriorOrbit, NodeTime); //set the position for new time on current orbit
        double dv = Math.Sqrt((normal * normal) + (prograde * prograde) + (radial * radial));
        DeltaVCost += dv;

        FuelCostTotal = OrbitalMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, DeltaVCost);
        FuelCostRemaining = FuelCostTotal;

        BurnTimeTotal = FuelCostTotal / _burnRate;
        BurnTimeRemaining = BurnTimeTotal;

        var firsthalfDvFuel = OrbitalMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, dv * 0.5);
        var firsthalfBurnTime = firsthalfDvFuel / _burnRate;
        TimeAtStartBurn = NodeTime - TimeSpan.FromSeconds(firsthalfBurnTime);
        (Orbital.Vector3 position, Vector2 velocity) stateVectors = OrbitalMath.GetStateVectors(PriorOrbit, NodeTime);
        
        Orbital.Vector3 velocity = new Orbital.Vector3(stateVectors.velocity.X, stateVectors.velocity.Y, 0);
        velocity += OrbitalMath.ProgradeToStateVector(new(radial, prograde, normal), PriorOrbit);
        TargetVelocity = new Vector2(velocity.X, velocity.Y);
        TargetOrbit =  OrbitalMath.KeplerFromPositionAndVelocity(_sgp, NodePosition, velocity, NodeTime);
    }

    /// <summary>
    /// Adds parameters to exsisting node
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
    /// Adds parameters to exsisting node
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
    /// Adds parameters to exsisting node
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
    /// Adds parameters to exsisting node
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

    /// <summary>
    /// Sets the node with given parameters (replaces exsisting parameters)
    /// </summary>
    /// <param name="prograde"></param>
    /// <param name="radial"></param>
    /// <param name="normal"></param>
    /// <param name="time"></param>
    public void SetNode(double prograde, double radial, double normal,  DateTime time)
    {
        Prograde = prograde;
        Radial = radial;
        Normal = normal;
        NodeTime = time;
        NodePosition = OrbitalMath.GetRelativePosition(PriorOrbit, NodeTime); //set the position for new time on current orbit
        double dv = Math.Sqrt((normal * normal) + (prograde * prograde) + (radial * radial));
        DeltaVCost = dv;

        FuelCostTotal = OrbitalMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, DeltaVCost);
        FuelCostRemaining = FuelCostTotal;

        BurnTimeTotal = FuelCostTotal / _burnRate;
        BurnTimeRemaining = BurnTimeTotal;

        var firsthalfDvFuel = OrbitalMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, dv * 0.5);
        var firsthalfBurnTime = firsthalfDvFuel / _burnRate;
        TimeAtStartBurn = NodeTime - TimeSpan.FromSeconds(firsthalfBurnTime);
        (Orbital.Vector3 position, Vector2 velocity) stateVectors = OrbitalMath.GetStateVectors(PriorOrbit, NodeTime);
        
        Orbital.Vector3 velocity = new Orbital.Vector3(stateVectors.velocity.X, stateVectors.velocity.Y, 0);
        velocity += OrbitalMath.ProgradeToStateVector(new(radial, prograde, normal), PriorOrbit);
        
        TargetOrbit =  OrbitalMath.KeplerFromPositionAndVelocity(_sgp, NodePosition, velocity, NodeTime);
        if (TargetOrbit.MeanAnomalyAtEpoch is double.NaN)
            throw new Exception("wtf exception");

    }
    
    public void SetNode(Orbital.Vector3 burn, DateTime time)
    {
        SetNode(burn.Y, burn.X, burn.Z, time);
    }
    
}

public class ManuverSequence
{
    public String SequenceName = "";
    //public bool IsOpen = false;
    //public ManuverSequence ParentSequence;

    /// <summary>
    /// the focal point of orbits in this sequence. 
    /// </summary>
    public ECSLib.IPosition ParentPosition = new zeroPosition();

    class zeroPosition : IPosition
    {
        public Orbital.Vector3 AbsolutePosition { get {return Orbital.Vector3.Zero;} }
        public Orbital.Vector3 RelativePosition { get {return Orbital.Vector3.Zero;} }
    }

    public List<ManuverNode> ManuverNodes = new List<ManuverNode>();
    public List<(double startAngle, double endAngle)> OrbitArcs = new List<(double startAngle, double endAngle)>();
    public List<ManuverSequence> ManuverSequences = new List<ManuverSequence>();
    
    
}