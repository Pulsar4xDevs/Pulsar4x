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

public struct TrajectorySegment
{
    public KeplerElements Orbit;
    public Entity ParentBody;
    public IPosition ParentPosition;
    public string ParentName;
    public DateTime StartTime;
    public DateTime EndTime;
    public Orbital.Vector3 StartPosition;
    public Orbital.Vector3 EndPosition;
    public bool EntersSOI;
    public bool ExitsSOI;
    /// <summary>
    /// True if this segment orbits a different body than the ship's original SOI parent.
    /// Rendering must time-sample and add body's predicted position to get original-parent-relative coords.
    /// </summary>
    public bool IsFlybySegment;
    /// <summary>
    /// For flyby segments: the orbit of ParentBody around the original SOI parent.
    /// Used to compute the body's predicted position at each rendering sample time.
    /// </summary>
    public KeplerElements BodyOrbitKE;
}

public struct EncounterPrediction
{
    public Entity Body;
    public string BodyName;
    public Orbital.Vector3 BodyPositionAtEncounter;
    public double SOIRadius_m;
    public double BodyRadius_m;
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
    public TrajectorySegment[] Segments = Array.Empty<TrajectorySegment>();

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
        PredictPatchedConics();
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

        // Clamp so burnEnd + scanSeconds doesn't overflow DateTime.MaxValue
        double maxSeconds = (DateTime.MaxValue - burnEnd).TotalSeconds - 1;
        if (scanSeconds > maxSeconds)
            scanSeconds = Math.Max(0, maxSeconds);

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

            // Track first SOI entry (outside → inside transition)
            bool prevOutside = true;
            DateTime soiEntryTime = DateTime.MaxValue;
            Orbital.Vector3 soiEntryShipPos = Orbital.Vector3.Zero;
            Orbital.Vector3 soiEntryBodyPos = Orbital.Vector3.Zero;

            for (int s = 0; s <= steps; s++)
            {
                DateTime sampleTime = burnEnd + TimeSpan.FromSeconds(s * dt);
                var shipPos = OrbitalMath.GetRelativePosition(TargetOrbit, sampleTime);
                var bodyPos = OrbitalMath.GetRelativePosition(bodyKE, sampleTime);

                double dist = (shipPos - bodyPos).Length();
                bool isOutside = dist >= soiRadius;

                // Detect first SOI boundary crossing
                if (prevOutside && !isOutside && s > 0 && soiEntryTime == DateTime.MaxValue)
                {
                    soiEntryTime = sampleTime;
                    soiEntryShipPos = shipPos;
                    soiEntryBodyPos = bodyPos;
                }
                prevOutside = isOutside;

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
                double bodyRadius = 0;
                if (child.TryGetDataBlob<MassVolumeDB>(out var childMVDB))
                    bodyRadius = childMVDB.RadiusInM;

                bool entersSOI = minDist < soiRadius;

                // For SOI entries, show the body at the SOI crossing time (not closest approach)
                // so the encounter icon aligns with where the trajectory enters the SOI
                var displayBodyPos = entersSOI && soiEntryTime != DateTime.MaxValue ? soiEntryBodyPos : minBodyPos;
                var displayShipPos = entersSOI && soiEntryTime != DateTime.MaxValue ? soiEntryShipPos : minShipPos;
                var displayTime = entersSOI && soiEntryTime != DateTime.MaxValue ? soiEntryTime : minTime;

                results.Add(new EncounterPrediction
                {
                    Body = child,
                    BodyName = child.GetDefaultName(),
                    BodyPositionAtEncounter = displayBodyPos,
                    SOIRadius_m = soiRadius,
                    BodyRadius_m = bodyRadius,
                    ClosestApproach_m = minDist,
                    EncounterTime = displayTime,
                    ShipPositionAtEncounter = displayShipPos,
                    EntersSOI = entersSOI
                });
            }
        }

        Encounters = results.ToArray();
    }

    private void PredictPatchedConics()
    {
        var soiParent = _orderEntity.GetSOIParentEntity();
        if (soiParent == null || !soiParent.TryGetDataBlob<PositionDB>(out var parentPosDB))
        {
            Segments = Array.Empty<TrajectorySegment>();
            return;
        }

        var segments = new List<TrajectorySegment>();
        var currentOrbit = TargetOrbit;
        var currentParent = soiParent;
        var currentParentPosDB = parentPosDB;
        DateTime burnEnd = TimeAtStartBurn + TimeSpan.FromSeconds(BurnTimeTotal);
        DateTime currentTime = burnEnd;
        double currentShipMass = _totalMass - FuelCostTotal;
        int maxSegments = 4;

        // Keep track of the original parent for return-from-flyby
        var originalParent = soiParent;
        var originalParentPosDB = parentPosDB;
        double originalParentMass = _parentMass;

        for (int depth = 0; depth < maxSegments; depth++)
        {
            // 1. Scan duration
            double scanSeconds;
            if (currentOrbit.Eccentricity < 1.0 && currentOrbit.Period > 0)
                scanSeconds = currentOrbit.Period;
            else
                scanSeconds = 365.25 * 24 * 3600;

            double maxSeconds = (DateTime.MaxValue - currentTime).TotalSeconds - 1;
            if (scanSeconds > maxSeconds)
                scanSeconds = Math.Max(0, maxSeconds);

            // 2. Find earliest SOI crossing among sibling bodies
            var children = currentParentPosDB.Children.ToArray();
            int steps = 180;
            double dt = scanSeconds / steps;

            Entity crossBody = null;
            KeplerElements crossBodyKE = default;
            double crossSOIRadius = 0;
            int crossStepInside = -1;
            int crossStepOutside = -1;
            double crossEarliestTime = double.MaxValue;

            foreach (var child in children)
            {
                if (child == _orderEntity)
                    continue;
                if (!child.TryGetDataBlob<OrbitDB>(out var childOrbitDB))
                    continue;

                double soiRadius = child.GetSOI_m();
                if (double.IsInfinity(soiRadius) || double.IsNaN(soiRadius) || soiRadius <= 0)
                    continue;

                var bodyKE = childOrbitDB.GetElements();
                bool wasInside = false;

                for (int s = 0; s <= steps; s++)
                {
                    double sampleSec = s * dt;
                    DateTime sampleTime = currentTime + TimeSpan.FromSeconds(sampleSec);
                    var shipPos = OrbitalMath.GetRelativePosition(currentOrbit, sampleTime);
                    var bodyPos = OrbitalMath.GetRelativePosition(bodyKE, sampleTime);
                    double dist = (shipPos - bodyPos).Length();
                    bool isInside = dist < soiRadius;

                    if (!wasInside && isInside && s > 0 && sampleSec < crossEarliestTime)
                    {
                        crossBody = child;
                        crossBodyKE = bodyKE;
                        crossSOIRadius = soiRadius;
                        crossStepOutside = s - 1;
                        crossStepInside = s;
                        crossEarliestTime = sampleSec;
                    }

                    wasInside = isInside;
                }
            }

            if (crossBody == null)
            {
                // No crossing found — terminal segment
                DateTime endTime = currentTime + TimeSpan.FromSeconds(scanSeconds);
                var startPos = OrbitalMath.GetRelativePosition(currentOrbit, currentTime);
                var endPos = OrbitalMath.GetRelativePosition(currentOrbit, endTime);
                segments.Add(new TrajectorySegment
                {
                    Orbit = currentOrbit,
                    ParentBody = currentParent,
                    ParentPosition = currentParentPosDB,
                    ParentName = currentParent.GetDefaultName(),
                    StartTime = currentTime,
                    EndTime = endTime,
                    StartPosition = startPos,
                    EndPosition = endPos,
                    EntersSOI = false,
                    ExitsSOI = false,
                    IsFlybySegment = false,
                    BodyOrbitKE = default,
                });
                break;
            }

            // 3. Refine crossing time with binary search
            double tOutside = crossStepOutside * dt;
            double tInside = crossStepInside * dt;
            for (int iter = 0; iter < 20; iter++)
            {
                double tMid = (tOutside + tInside) / 2;
                DateTime midTime = currentTime + TimeSpan.FromSeconds(tMid);
                var shipMid = OrbitalMath.GetRelativePosition(currentOrbit, midTime);
                var bodyMid = OrbitalMath.GetRelativePosition(crossBodyKE, midTime);
                double distMid = (shipMid - bodyMid).Length();
                if (distMid < crossSOIRadius)
                    tInside = tMid;
                else
                    tOutside = tMid;
            }

            double crossSeconds = (tOutside + tInside) / 2;
            DateTime crossTime = currentTime + TimeSpan.FromSeconds(crossSeconds);

            // 4. Add segment up to SOI boundary
            var segStartPos = OrbitalMath.GetRelativePosition(currentOrbit, currentTime);
            var segEndPos = OrbitalMath.GetRelativePosition(currentOrbit, crossTime);
            segments.Add(new TrajectorySegment
            {
                Orbit = currentOrbit,
                ParentBody = currentParent,
                ParentPosition = currentParentPosDB,
                ParentName = currentParent.GetDefaultName(),
                StartTime = currentTime,
                EndTime = crossTime,
                StartPosition = segStartPos,
                EndPosition = segEndPos,
                EntersSOI = true,
                ExitsSOI = false,
                IsFlybySegment = false,
                BodyOrbitKE = default,
            });

            // 5. Frame conversion at SOI boundary
            Orbital.Vector3 relPos, relVel;
            double bodyMass, newSGP;
            try
            {
                var shipState = OrbitalMath.GetStateVectors(currentOrbit, crossTime);
                var bodyState = OrbitalMath.GetStateVectors(crossBodyKE, crossTime);

                relPos = shipState.position - bodyState.position;
                relVel = new Orbital.Vector3(
                    shipState.velocity.X - bodyState.velocity.X,
                    shipState.velocity.Y - bodyState.velocity.Y,
                    0);

                bodyMass = crossBody.GetDataBlob<MassVolumeDB>().MassTotal;
                newSGP = GeneralMath.StandardGravitationalParameter(currentShipMass + bodyMass);
            }
            catch
            {
                break; // Can't compute state vectors
            }

            if (!crossBody.TryGetDataBlob<PositionDB>(out var crossBodyPosDB))
                break;

            KeplerElements newOrbit;
            try
            {
                newOrbit = OrbitalMath.KeplerFromPositionAndVelocity(newSGP, relPos, relVel, crossTime);
            }
            catch
            {
                break; // degenerate orbit, bail out
            }

            // 6. Determine orbit type: flyby (hyperbolic) or capture (elliptical).
            // The flyby path must fully succeed (exit time + return orbit) or we
            // fall through to the capture path as a safe default.
            bool flybyCompleted = false;

            if (newOrbit.Eccentricity >= 1)
            {
                try
                {
                    double p = EllipseMath.SemiLatusRectum(newOrbit.SemiMajorAxis, newOrbit.Eccentricity);
                    double cosTA = ((p / crossSOIRadius) - 1) / newOrbit.Eccentricity;
                    if (cosTA < -1) cosTA = -1;
                    if (cosTA > 1) cosTA = 1;
                    double exitTA = Math.Acos(cosTA);

                    double exitSeconds = OrbitalMath.TimeFromTrueAnomalyHyperbolic(
                        newSGP, newOrbit.SemiMajorAxis, newOrbit.Eccentricity, exitTA);
                    double epochTA = newOrbit.TrueAnomalyAtEpoch;
                    double epochSeconds = OrbitalMath.TimeFromTrueAnomalyHyperbolic(
                        newSGP, newOrbit.SemiMajorAxis, newOrbit.Eccentricity, epochTA);
                    exitSeconds = exitSeconds - epochSeconds;

                    if (exitSeconds > 0 && !double.IsNaN(exitSeconds) && !double.IsInfinity(exitSeconds))
                    {
                        DateTime exitTime = crossTime + TimeSpan.FromSeconds(exitSeconds);
                        var flybyStartPos = OrbitalMath.GetRelativePosition(newOrbit, crossTime);
                        var flybyEndPos = OrbitalMath.GetRelativePosition(newOrbit, exitTime);

                        segments.Add(new TrajectorySegment
                        {
                            Orbit = newOrbit,
                            ParentBody = crossBody,
                            ParentPosition = crossBodyPosDB,
                            ParentName = crossBody.GetDefaultName(),
                            StartTime = crossTime,
                            EndTime = exitTime,
                            StartPosition = flybyStartPos,
                            EndPosition = flybyEndPos,
                            EntersSOI = false,
                            ExitsSOI = true,
                            IsFlybySegment = true,
                            BodyOrbitKE = crossBodyKE,
                        });

                        // 7. Convert back to original parent frame at exit
                        var shipExitState = OrbitalMath.GetStateVectors(newOrbit, exitTime);
                        var bodyStateAtExit = OrbitalMath.GetStateVectors(crossBodyKE, exitTime);

                        var returnPos = bodyStateAtExit.position + shipExitState.position;
                        var returnVel = new Orbital.Vector3(
                            bodyStateAtExit.velocity.X + shipExitState.velocity.X,
                            bodyStateAtExit.velocity.Y + shipExitState.velocity.Y,
                            0);

                        double returnSGP = GeneralMath.StandardGravitationalParameter(currentShipMass + originalParentMass);
                        currentOrbit = OrbitalMath.KeplerFromPositionAndVelocity(returnSGP, returnPos, returnVel, exitTime);
                        currentParent = originalParent;
                        currentParentPosDB = originalParentPosDB;
                        currentTime = exitTime;
                        flybyCompleted = true;
                    }
                }
                catch
                {
                    // Flyby exit computation failed (near-parabolic, NaN, etc.)
                    // Fall through to capture as safe default
                }
            }

            if (flybyCompleted)
            {
                continue; // Next iteration picks up post-flyby orbit
            }

            // Capture (elliptical) or failed flyby — terminal segment around body
            var captureStartPos = OrbitalMath.GetRelativePosition(newOrbit, crossTime);
            DateTime captureEnd = crossTime + TimeSpan.FromSeconds(
                newOrbit.Period > 0 ? newOrbit.Period : 365.25 * 24 * 3600);
            var captureEndPos = OrbitalMath.GetRelativePosition(newOrbit, captureEnd);
            segments.Add(new TrajectorySegment
            {
                Orbit = newOrbit,
                ParentBody = crossBody,
                ParentPosition = crossBodyPosDB,
                ParentName = crossBody.GetDefaultName(),
                StartTime = crossTime,
                EndTime = captureEnd,
                StartPosition = captureStartPos,
                EndPosition = captureEndPos,
                EntersSOI = false,
                ExitsSOI = false,
                IsFlybySegment = true,
                BodyOrbitKE = crossBodyKE,
            });
            break;
        }

        Segments = segments.ToArray();
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