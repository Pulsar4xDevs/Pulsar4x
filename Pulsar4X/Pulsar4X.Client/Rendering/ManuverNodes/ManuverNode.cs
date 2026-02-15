using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;
using Pulsar4X.Factions;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using static Pulsar4X.Movement.NewtonionMovementProcessor;

namespace Pulsar4X.Client;

public struct EncounterPrediction
{
    public Entity Body;
    public string BodyName;
    public Orbital.Vector3 BodyPositionAtEncounter;
    public double SOIRadius_m;
    public double ClosestApproach_m;
    public DateTime EncounterTime;
    public Orbital.Vector3 ShipPositionAtEncounter;
    public bool EntersSOI;
}

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
    private double _parentMass;
    private double _sgp;
    private ICargoable _fuelType;

    private double _burnRate;
    private double _exhaustVelocity;
    public KeplerElements PriorOrbit;
    public KeplerElements TargetOrbit;
    public EncounterPrediction[] Encounters = Array.Empty<EncounterPrediction>();

    public ManuverNode(Entity orderEntity, DateTime nodeTime)
    {
        NodeTime = nodeTime;
        _orderEntity = orderEntity;
        _newtonThrust = _orderEntity.GetDataBlob<NewtonThrustAbilityDB>();
        _totalMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassTotal;
        _dryMass = _orderEntity.GetDataBlob<MassVolumeDB>().MassDry;
        _parentMass = _orderEntity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
        _sgp = GeneralMath.StandardGravitationalParameter(_totalMass + _parentMass);
        var fuelTypeID = _newtonThrust.FuelType;
        _fuelType = orderEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(fuelTypeID);
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
        NodePosition = OrbitalMath.GetRelativePosition(PriorOrbit, NodeTime);
        ComputeTargetOrbit();
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
        NodePosition = OrbitalMath.GetRelativePosition(PriorOrbit, NodeTime);
        ComputeTargetOrbit();
    }

    public void SetNode(Orbital.Vector3 burn, DateTime time)
    {
        SetNode(burn.Y, burn.X, burn.Z, time);
    }

    private void ComputeTargetOrbit()
    {
        double totalDV = Math.Sqrt(Prograde * Prograde + Radial * Radial + Normal * Normal);
        DeltaVCost = totalDV;

        if (totalDV == 0)
        {
            TargetOrbit = PriorOrbit;
            FuelCostTotal = 0;
            FuelCostRemaining = 0;
            BurnTimeTotal = 0;
            BurnTimeRemaining = 0;
            TargetVelocity = OrbitalMath.GetStateVectors(PriorOrbit, NodeTime).velocity;
            return;
        }

        FuelCostTotal = OrbitalMath.TsiolkovskyFuelUse(_totalMass, _exhaustVelocity, totalDV);
        FuelCostRemaining = FuelCostTotal;
        BurnTimeTotal = FuelCostTotal / _burnRate;
        BurnTimeRemaining = BurnTimeTotal;

        // Burn is centered on NodeTime
        TimeAtStartBurn = NodeTime - TimeSpan.FromSeconds(BurnTimeTotal / 2);

        // State vectors at burn start (integration starts here)
        var burnStartState = OrbitalMath.GetStateVectors(PriorOrbit, TimeAtStartBurn);
        Orbital.Vector3 position = burnStartState.position;
        Orbital.Vector3 velocity = new Orbital.Vector3(burnStartState.velocity.X, burnStartState.velocity.Y, 0);

        // Convert prograde/radial/normal to parent-relative delta-V direction at burn center
        // (NodeTime), matching NewtonThrustCommand.Execute which uses _vectorDateTime
        var burnCenterState = OrbitalMath.GetStateVectors(PriorOrbit, NodeTime);
        Orbital.Vector3 centerPos = burnCenterState.position;
        Orbital.Vector3 centerVel = new Orbital.Vector3(burnCenterState.velocity.X, burnCenterState.velocity.Y, 0);
        Orbital.Vector3 manuverDeltaV = OrbitalMath.ProgradeToStateVector(
            _sgp, new Orbital.Vector3(Radial, Prograde, Normal), centerPos, centerVel);

        double mass = _totalMass;
        double dryMass = _totalMass - _newtonThrust.TotalFuel_kg;
        double secondsRemaining = BurnTimeTotal;

        while (secondsRemaining > 0)
        {
            double timeStep = Math.Min(1.0, secondsRemaining);

            var result = IntegrateOneStep(
                position, velocity, manuverDeltaV,
                mass, _parentMass,
                _exhaustVelocity, _burnRate, dryMass,
                timeStep);

            position = result.Position;
            velocity = result.Velocity;
            manuverDeltaV = result.ManuverDeltaV;
            mass = result.Mass;

            secondsRemaining -= timeStep;
        }

        DateTime endTime = TimeAtStartBurn + TimeSpan.FromSeconds(BurnTimeTotal);
        double postBurnSgp = GeneralMath.StandardGravitationalParameter(mass + _parentMass);
        TargetOrbit = OrbitalMath.KeplerFromPositionAndVelocity(postBurnSgp, position, velocity, endTime);
        TargetVelocity = new Vector2(velocity.X, velocity.Y);
        DetectEncounters();
    }

    private void DetectEncounters()
    {
        var soiParent = _orderEntity.GetSOIParentEntity();
        if (soiParent == null || !soiParent.TryGetDataBlob<PositionDB>(out var parentPosDB))
        {
            Encounters = Array.Empty<EncounterPrediction>();
            return;
        }

        var children = parentPosDB.Children.ToArray();
        var results = new List<EncounterPrediction>();

        DateTime burnEnd = TimeAtStartBurn + TimeSpan.FromSeconds(BurnTimeTotal);

        // Determine scan duration: one orbital period for elliptical, capped at 1 year for hyperbolic
        double scanSeconds;
        if (TargetOrbit.Eccentricity < 1.0 && TargetOrbit.Period > 0)
            scanSeconds = TargetOrbit.Period;
        else
            scanSeconds = 365.25 * 24 * 3600;

        int steps = 180;
        double dt = scanSeconds / steps;

        foreach (var child in children)
        {
            if (child == _orderEntity)
                continue;
            if (!child.TryGetDataBlob<OrbitDB>(out var childOrbitDB))
                continue;

            double soiRadius = child.GetSOI_m();
            if (double.IsInfinity(soiRadius) || double.IsNaN(soiRadius))
                continue;

            var bodyKE = childOrbitDB.GetElements();
            double minDist = double.MaxValue;
            DateTime minTime = burnEnd;
            Orbital.Vector3 minShipPos = Orbital.Vector3.Zero;
            Orbital.Vector3 minBodyPos = Orbital.Vector3.Zero;

            for (int s = 0; s <= steps; s++)
            {
                DateTime sampleTime = burnEnd + TimeSpan.FromSeconds(s * dt);
                var shipPos = OrbitalMath.GetRelativePosition(TargetOrbit, sampleTime);
                var bodyPos = OrbitalMath.GetRelativePosition(bodyKE, sampleTime);

                double dist = (shipPos - bodyPos).Length();
                if (dist < minDist)
                {
                    minDist = dist;
                    minTime = sampleTime;
                    minShipPos = shipPos;
                    minBodyPos = bodyPos;
                }
            }

            if (minDist < soiRadius * 5)
            {
                results.Add(new EncounterPrediction
                {
                    Body = child,
                    BodyName = child.GetDefaultName(),
                    BodyPositionAtEncounter = minBodyPos,
                    SOIRadius_m = soiRadius,
                    ClosestApproach_m = minDist,
                    EncounterTime = minTime,
                    ShipPositionAtEncounter = minShipPos,
                    EntersSOI = minDist < soiRadius
                });
            }
        }

        Encounters = results.ToArray();
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
    public IPosition ParentPosition = new zeroPosition();

    class zeroPosition : IPosition
    {
        public Orbital.Vector3 AbsolutePosition { get {return Orbital.Vector3.Zero;} }
        public Orbital.Vector3 RelativePosition { get {return Orbital.Vector3.Zero;} }
    }

    public List<ManuverNode> ManuverNodes = new List<ManuverNode>();
    public List<(double startAngle, double endAngle)> OrbitArcs = new List<(double startAngle, double endAngle)>();
    public List<ManuverSequence> ManuverSequences = new List<ManuverSequence>();


}