using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>
/// Rendezvous onto another object's orbit around the shared SOI parent (Phobos, a ship,
/// a station). Circularise leftover first; this action then Hohmanns or phases from the
/// current circular orbit. Burns are sequenced internally so the queue shows one order.
/// </summary>
public class MatchOrbitAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
    public override bool IsBlocking => true;
    public override string Name => _name;
    string _name = "Match Orbit";
    string _details = "Match orbit";
    public override string Details => _details;

    Entity _factionEntity;
    Entity _entityCommanding;
    Entity _targetEntity;
    public int TargetEntityGuid { get; private set; }
    internal override Entity EntityCommanding => _entityCommanding;

    const double MaxEccentricityForTransfer = 0.01;
    const double CoOrbitalRadiusTolerance = 0.001;
    const double ArrivedAlongOrbitFraction = 0.01;
    const double ArrivedWithinLowOrbits = 3.0;
    const double ArrivedWithin_m = 10_000;

    readonly List<(KeplerElements start, KeplerElements end, DateTime at)> _burns = new();
    int _next;
    NewtonSimpleMoveDB? _db;

    public static MatchOrbitAction CreateCommand(Entity ship, Entity target, DateTime? actionOnDate = null)
    {
        var cmd = new MatchOrbitAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            TargetEntityGuid = target.Id,
            _entityCommanding = ship,
            _targetEntity = target,
            CreatedDate = ship.StarSysDateTime,
            ActionOnDate = actionOnDate ?? ship.StarSysDateTime,
            _name = "Match orbit of " + target.GetName(ship.FactionOwnerID),
        };
        cmd.UpdateDetailString();
        return cmd;
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (atDateTime < ActionOnDate)
            return;

        if (!IsRunning)
        {
            if (_targetEntity == null
                && !_entityCommanding.Manager.TryGetGlobalEntityById(TargetEntityGuid, out _targetEntity))
            {
                Status = ActionStatus.Failed;
                _isFinished = true;
                _details = "target gone";
                return;
            }

            if (!TryPlanBurns(_entityCommanding, _targetEntity, atDateTime, out var burns, out var reason))
            {
                Status = ActionStatus.Failed;
                _isFinished = true;
                _details = reason;
                return;
            }

            _burns.Clear();
            _burns.AddRange(burns);
            _next = 0;
            IsRunning = true;
            if (_burns.Count == 0)
            {
                _isFinished = true;
                _details = "Already matching";
                return;
            }
        }

        TryAdvanceBurn(atDateTime);
        UpdateDetailString();
    }

    void TryAdvanceBurn(DateTime atDateTime)
    {
        if (_db != null && !_db.IsComplete)
            return;
        if (_next >= _burns.Count)
        {
            _isFinished = true;
            return;
        }

        var burn = _burns[_next];
        if (atDateTime < burn.at)
            return;

        var parent = _entityCommanding.GetSOIParentEntity();
        if (parent == null)
        {
            Status = ActionStatus.Failed;
            _isFinished = true;
            return;
        }

        _db = new NewtonSimpleMoveDB(parent, burn.start, burn.end, burn.at);
        _entityCommanding.SetDataBlob(_db);
        NewtonSimpleProcessor.ProcessEntity(_entityCommanding, atDateTime);
        _next++;
        if (_db.IsComplete && _next >= _burns.Count)
            _isFinished = true;
    }

    internal static bool TryPlanBurns(
        Entity ship, Entity target, DateTime now,
        out List<(KeplerElements start, KeplerElements end, DateTime at)> burns,
        out string reason)
    {
        burns = new List<(KeplerElements, KeplerElements, DateTime)>();
        reason = string.Empty;

        if (!ship.TryGetDataBlob<OrbitDB>(out var shipOrbit) || shipOrbit.Parent == null)
        {
            reason = "ship is not in a stable orbit";
            return false;
        }
        if (!target.TryGetDataBlob<OrbitDB>(out var targetOrbit) || targetOrbit.Parent == null)
        {
            reason = "target is not in a stable orbit";
            return false;
        }
        if (shipOrbit.Parent != targetOrbit.Parent)
        {
            reason = "target is under a different SOI parent";
            return false;
        }
        if (shipOrbit.Eccentricity > MaxEccentricityForTransfer)
        {
            reason = "circularise first";
            return false;
        }

        var parent = shipOrbit.Parent;
        double sgp = OrbitMath.SGP(parent, ship);
        double shipRadius = shipOrbit.SemiMajorAxis;
        double targetRadius = targetOrbit.SemiMajorAxis;
        double shipAngle = AngleOf(ship, now);
        double targetAngle = AngleOf(target, now);
        double phaseAngle = NormaliseAngle(targetAngle - shipAngle);
        double along = AlongTrack(target, targetRadius);
        double phaseTol = targetRadius > 0 ? along / targetRadius : 0;
        double separation = ((Vector3)MoveMath.GetAbsoluteFuturePosition(ship, now)
                             - (Vector3)MoveMath.GetAbsoluteFuturePosition(target, now)).Length();

        // Warp drop-in leaves us dest+offset; that is arrived even if leftover SMA ≠ a.
        if (separation <= along)
            return true;

        if (IsCoOrbital(shipRadius, targetRadius, target) && Math.Abs(phaseAngle) <= phaseTol)
            return true;

        (Vector3 deltaV, double timeInSeconds)[] manuvers;
        if (IsCoOrbital(shipRadius, targetRadius, target))
            manuvers = OrbitalMath.OrbitPhasingManuvers(shipOrbit.GetElements(), sgp, now, phaseAngle);
        else
        {
            manuvers = OrbitalMath.HohmannOE(sgp, shipRadius, shipAngle, targetRadius, targetAngle);
            if (manuvers.Length > 1)
                manuvers[1].timeInSeconds += manuvers[0].timeInSeconds;
        }

        var startKE = shipOrbit.GetElements();
        foreach (var manuver in manuvers)
        {
            DateTime nodeTime = now + TimeSpan.FromSeconds(manuver.timeInSeconds);
            var state = OrbitalMath.GetStateVectors(startKE, nodeTime);
            var position = state.position;
            var velocity = (Vector3)state.velocity;
            var deltaV = OrbitalMath.ProgradeToStateVector(sgp, manuver.deltaV, position, velocity);
            var endKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity + deltaV, nodeTime);
            if (!double.IsFinite(endKE.SemiMajorAxis) || !double.IsFinite(endKE.Eccentricity))
            {
                reason = "transfer solution did not converge";
                return false;
            }
            burns.Add((startKE, endKE, nodeTime));
            startKE = endKE;
        }

        return burns.Count > 0;
    }

    static bool IsCoOrbital(double shipRadius, double targetRadius, Entity target)
    {
        if (!double.IsFinite(shipRadius) || !double.IsFinite(targetRadius) || targetRadius <= 0)
            return false;
        double close = target.HasDataBlob<MassVolumeDB>()
            ? OrbitMath.LowOrbitRadius(target) * 2
            : ArrivedWithin_m;
        double tol = Math.Max(targetRadius * CoOrbitalRadiusTolerance, close);
        return Math.Abs(shipRadius - targetRadius) <= tol;
    }

    static double AlongTrack(Entity target, double targetRadius)
    {
        double close = target.HasDataBlob<MassVolumeDB>()
            ? OrbitMath.LowOrbitRadius(target) * ArrivedWithinLowOrbits
            : ArrivedWithin_m;
        if (!double.IsFinite(targetRadius) || targetRadius <= 0)
            return close;
        return Math.Max(close, targetRadius * ArrivedAlongOrbitFraction);
    }

    static double AngleOf(Entity entity, DateTime at)
    {
        var pos = MoveMath.GetRelativeFuturePosition(entity, at);
        return Math.Atan2(pos.Y, pos.X);
    }

    static double NormaliseAngle(double radians)
    {
        while (radians > Math.PI) radians -= 2 * Math.PI;
        while (radians <= -Math.PI) radians += 2 * Math.PI;
        return radians;
    }

    public override void UpdateDetailString()
    {
        if (_burns.Count == 0)
            _details = _name;
        else if (_next >= _burns.Count)
            _details = "Matched orbit";
        else
            _details = $"{_name} (burn {_next + 1}/{_burns.Count})";
    }

    internal override bool IsFinished()
    {
        if (_isFinished)
            return true;
        if (IsRunning && _db != null && _db.IsFailed)
        {
            Status = ActionStatus.Failed;
            return _isFinished = true;
        }
        if (IsRunning && _next >= _burns.Count && (_db == null || _db.IsComplete))
            return _isFinished = true;
        return false;
    }

    internal override bool IsValidCommand(Game game)
    {
        if (!CommandHelpers.IsCommandValid(
                game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid,
                out _factionEntity, out _entityCommanding))
            return false;
        return game.GlobalManager.TryGetGlobalEntityById(TargetEntityGuid, out _targetEntity);
    }

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }
}
