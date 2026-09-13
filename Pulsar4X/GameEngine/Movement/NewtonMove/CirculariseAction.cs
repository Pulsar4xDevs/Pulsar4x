using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
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
    public override string Details => _details;

    Entity _factionEntity;
    Entity _entityCommanding;
    internal override Entity EntityCommanding => _entityCommanding;

    NewtonSimpleMoveDB? _db;

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
        cmd.UpdateDetailString();
        return cmd;
    }

    /// <summary>
    /// Leftover r,v → circular Kepler around the current SOI parent.
    /// </summary>
    internal static bool TryCompute(
        Entity ship, DateTime at,
        out Entity parent, out KeplerElements startKE, out KeplerElements targetKE)
    {
        parent = null!;
        startKE = default;
        targetKE = default;

        parent = ship.GetSOIParentEntity();
        if (parent == null)
            return false;

        var raw = MoveMath.GetRelativeFutureState(ship, at);
        var pos = raw.pos;
        var vel = raw.Velocity;
        if (!double.IsFinite(pos.X) || pos.Length() < 1)
            return false;
        if (parent.TryGetDataBlob<MassVolumeDB>(out var parentMass) && pos.Length() <= parentMass.RadiusInM)
            return false;

        double sgp = OrbitMath.SGP(parent, ship);
        startKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, vel, at);
        targetKE = OrbitMath.KeplerCircularFromPosition(sgp, pos, at);
        var rAtEpoch = OrbitMath.GetStateVectors(startKE, at).position;
        return double.IsFinite(rAtEpoch.X) && rAtEpoch.Length() <= 1e14;
    }

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

        _db = new NewtonSimpleMoveDB(parent, startKE, targetKE, atDateTime);
        _entityCommanding.SetDataBlob(_db);
        NewtonSimpleProcessor.ProcessEntity(_entityCommanding, atDateTime);
        IsRunning = true;
        UpdateDetailString();
    }

    public override void UpdateDetailString()
    {
        if (_entityCommanding == null)
            _details = "Circularise";
        else if (ActionOnDate > _entityCommanding.StarSysDateTime)
            _details = "Waiting to circularise";
        else if (IsRunning)
            _details = "Circularising";
        else
            _details = "Circularise";
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
