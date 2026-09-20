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
/// Two-burn Hohmann to a new circular radius around the current SOI parent.
/// Requires a near-circular start (leftover hyperbola is CirculariseAction first).
/// End orbit is circular at <see cref="TargetRadius_m"/>.
/// </summary>
public class ChangeOrbitalAltitudeAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
    public override bool IsBlocking => true;
    public override string Name => "Change altitude";
    public override string Details => _details;
    string _details = "Change altitude";

    Entity _factionEntity;
    Entity _entityCommanding;
    internal override Entity EntityCommanding => _entityCommanding;

    public double TargetRadius_m { get; private set; }

    const double MaxEccentricityForTransfer = 0.01;

    readonly List<(KeplerElements start, KeplerElements end, DateTime at)> _burns = new();
    int _next;
    NewtonSimpleMoveDB? _db;

    public static ChangeOrbitalAltitudeAction CreateCommand(Entity ship, double targetRadius_m, DateTime? actionOnDate = null)
    {
        var cmd = new ChangeOrbitalAltitudeAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            _entityCommanding = ship,
            TargetRadius_m = targetRadius_m,
            CreatedDate = ship.StarSysDateTime,
            ActionOnDate = actionOnDate ?? ship.StarSysDateTime,
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
            if (!TryPlanBurns(_entityCommanding, TargetRadius_m, atDateTime, out var burns, out var reason))
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
                _details = "Already at altitude";
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
        Entity ship, double targetRadius_m, DateTime now,
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
        if (shipOrbit.Eccentricity > MaxEccentricityForTransfer)
        {
            reason = "circularise first";
            return false;
        }

        var parent = shipOrbit.Parent;
        if (parent.TryGetDataBlob<MassVolumeDB>(out var parentMass)
            && targetRadius_m <= parentMass.RadiusInM)
        {
            reason = "target radius is inside the parent body";
            return false;
        }

        double r1 = shipOrbit.SemiMajorAxis;
        double r2 = targetRadius_m;
        if (!double.IsFinite(r1) || !double.IsFinite(r2) || r1 < 1 || r2 < 1)
        {
            reason = "no usable radius";
            return false;
        }

        double sgp = OrbitMath.SGP(parent, ship);
        // Already at this altitude (same tolerance as planner co-orbital).
        double tol = Math.Max(r2 * 0.001, 1_000);
        if (Math.Abs(r1 - r2) <= tol)
            return true;

        var manuvers = OrbitalMath.Hohmann2(sgp, r1, r2);
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
                reason = "Hohmann solution did not converge";
                return false;
            }
            burns.Add((startKE, endKE, nodeTime));
            startKE = endKE;
        }

        return burns.Count > 0;
    }

    public override void UpdateDetailString()
    {
        if (_burns.Count == 0)
            _details = Name;
        else if (_next >= _burns.Count)
            _details = "At new altitude";
        else
            _details = $"Change altitude (burn {_next + 1}/{_burns.Count})";
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
        return CommandHelpers.IsCommandValid(
            game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid,
            out _factionEntity, out _entityCommanding);
    }

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }
}
