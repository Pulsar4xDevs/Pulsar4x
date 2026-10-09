using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Movement;

/// <summary>
/// Instant circularise around the current SOI parent at current r.
/// Start/target Kepler are built at Execute from leftover state, so callers
/// (MoveTo after warp, CreateCommandEZ) do not have to predict drop-in µ.
/// </summary>
public class CirculariseAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
    public override bool IsBlocking => true;
    public override string Name => "Circularise";

    string _details = "Circularise";
    public override string Details => LiveDetails();

    Entity _factionEntity;
    Entity _entityCommanding;
    internal override Entity EntityCommanding => _entityCommanding;

    NewtonSimpleMoveDB? _db;
    double _totalDv;

    public static CirculariseAction CreateCommand(Entity ship, DateTime? actionOnDate = null)
    {
        var cmd = new CirculariseAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            _entityCommanding = ship,
            CreatedDate = ship.StarSysDateTime,
            ActionOnDate = actionOnDate ?? ship.StarSysDateTime,
        };
        if (ship.TryGetDataBlob<OrbitDB>(out var leftover)
            && leftover.Eccentricity >= 1
            && leftover.Epoch != default
            && leftover.Epoch <= cmd.ActionOnDate)
        {
            // Extreme leftover (asteroid drop-in) cannot be propagated. Burn from
            // the dump epoch so CreateCommand / Execute never sample a future hyperbola.
            cmd.ActionOnDate = leftover.Epoch;
        }
        cmd.UpdateDetailString();
        return cmd;
    }

    /// <summary>
    /// Warp leftover hyperbolas around a tiny well cannot be integrated forward.
    /// Sample r,v at the leftover epoch (drop-in), not a later pulse instant.
    /// </summary>
    internal static DateTime SampleAt(Entity ship, DateTime at)
    {
        if (ship.TryGetDataBlob<OrbitDB>(out var orbit)
            && orbit.Eccentricity >= 1
            && orbit.Epoch != default
            && orbit.Epoch <= at)
            return orbit.Epoch;
        return at;
    }

    /// <summary>
    /// Leftover r,v → circular Kepler around the current SOI parent.
    /// Never throws: vis-viva NaN / degenerate leftover returns false.
    /// </summary>
    internal static bool TryCompute(
        Entity ship, DateTime at,
        out Entity parent, out KeplerElements startKE, out KeplerElements targetKE)
    {
        parent = null!;
        startKE = default;
        targetKE = default;

        try
        {
            parent = ship.GetSOIParentEntity();
            if (parent == null)
                return false;

            DateTime sampleAt = SampleAt(ship, at);
            var raw = MoveMath.GetRelativeFutureState(ship, sampleAt);
            var pos = raw.pos;
            var vel = raw.Velocity;
            if (!IsFiniteVec(pos) || pos.Length() < 1)
                return false;
            if (!IsFiniteVec(vel))
                return false;
            if (parent.TryGetDataBlob<MassVolumeDB>(out var parentMass) && pos.Length() <= parentMass.RadiusInM)
                return false;

            double sgp = OrbitMath.SGP(parent, ship);
            if (!(sgp > 0) || !double.IsFinite(sgp))
                return false;

            startKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, vel, sampleAt);
            targetKE = OrbitMath.KeplerCircularFromPosition(sgp, pos, sampleAt);
            if (!KeplerUsable(startKE, sampleAt) || !KeplerUsable(targetKE, sampleAt))
                return false;
            return true;
        }
        catch
        {
            parent = null!;
            startKE = default;
            targetKE = default;
            return false;
        }
    }

    static bool KeplerUsable(KeplerElements ke, DateTime at)
    {
        if (!double.IsFinite(ke.SemiMajorAxis) || !double.IsFinite(ke.Eccentricity)
            || !double.IsFinite(ke.StandardGravParameter) || ke.StandardGravParameter <= 0)
            return false;
        var state = OrbitMath.GetStateVectors(ke, at);
        if (!IsFiniteVec(state.position) || !double.IsFinite(state.velocity.X) || !double.IsFinite(state.velocity.Y))
            return false;
        double r = state.position.Length();
        return r >= 1 && r <= 1e14;
    }

    static bool IsFiniteVec(Vector3 v)
        => double.IsFinite(v.X) && double.IsFinite(v.Y) && double.IsFinite(v.Z);

    internal override void Execute(DateTime atDateTime)
    {
        if (IsRunning || atDateTime < ActionOnDate)
            return;

        if (!TryCompute(_entityCommanding, atDateTime, out var parent, out var startKE, out var targetKE))
        {
            Status = ActionStatus.Failed;
            _isFinished = true;
            return;
        }

        if (startKE.Eccentricity < 0.05)
        {
            IsRunning = true;
            _isFinished = true;
            _details = "Already circular";
            return;
        }

        DateTime node = startKE.Epoch != default ? startKE.Epoch : atDateTime;
        _totalDv = DvBetween(startKE, targetKE, node);
        if (!double.IsFinite(_totalDv))
        {
            Status = ActionStatus.Failed;
            _isFinished = true;
            return;
        }

        _db = new NewtonSimpleMoveDB(parent, startKE, targetKE, node);
        _entityCommanding.SetDataBlob(_db);
        NewtonSimpleProcessor.ProcessEntity(_entityCommanding, atDateTime);
        IsRunning = true;
    }

    public override void UpdateDetailString()
    {
        _details = LiveDetails();
    }

    string LiveDetails()
    {
        if (_entityCommanding == null)
            return "Circularise";
        if (_isFinished && _db == null)
            return _details;
        DateTime now = _entityCommanding.StarSysDateTime;
        if (ActionOnDate > now)
        {
            if (TryCompute(_entityCommanding, ActionOnDate, out _, out var startKE, out var targetKE))
            {
                DateTime node = startKE.Epoch != default ? startKE.Epoch : ActionOnDate;
                double dv = DvBetween(startKE, targetKE, node);
                if (double.IsFinite(dv))
                    return "Waiting to circularise, " + Stringify.Velocity(dv) + " Δv";
            }
            return "Waiting to circularise";
        }
        if (IsRunning && _db != null)
        {
            if (_db.IsComplete)
                return "Circularised";
            if (_db.IsFailed)
                return "Circularise failed";
            return "Circularising, " + Stringify.Velocity(RemainingDv()) + " Δv left";
        }
        return "Circularise";
    }

    double RemainingDv()
    {
        if (_db == null)
            return 0;
        if (_db.FuelTotal > 0)
        {
            double f = Math.Clamp(_db.FuelBurned / _db.FuelTotal, 0, 1);
            return (1 - f) * _totalDv;
        }
        return _totalDv;
    }

    static double DvBetween(KeplerElements start, KeplerElements target, DateTime at)
    {
        var v0 = (Vector3)OrbitMath.GetStateVectors(start, at).velocity;
        var v1 = (Vector3)OrbitMath.GetStateVectors(target, at).velocity;
        return (v1 - v0).Length();
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
        if (IsRunning && _db != null && _db.IsComplete)
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
