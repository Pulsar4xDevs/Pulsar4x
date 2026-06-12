using System;
using Pulsar4X.Api;
using Pulsar4X.Interfaces;
using Pulsar4X.Orbital;

namespace Pulsar4X.Client;

/// <summary>
/// An <see cref="IPosition"/> backed by the replicated galaxy: every read resolves the entity's
/// current snapshot and propagates its orbit to the galaxy clock, so map icons hold no game data —
/// just this (system id, entity id) handle. Keplerian movers propagate client-side; everything else
/// uses its last pushed <see cref="PositionView"/>.
/// </summary>
/// <summary>A fixed <see cref="IPosition"/>, for icons placed at synthetic coordinates (galaxy map).</summary>
public sealed class StaticPosition : IPosition
{
    public StaticPosition(Vector3 position)
    {
        AbsolutePosition = position;
        RelativePosition = position;
    }

    public Vector3 AbsolutePosition { get; }
    public Vector3 RelativePosition { get; }
}

public class SnapshotPosition : IPosition
{
    private readonly GlobalUIState _state;
    private readonly string _systemId;
    private readonly int _entityId;

    // Memo of the last computed result, keyed by (snapshot reference, clock) — a derived-value
    // cache only; positions are recomputed the moment the galaxy changes under it.
    private EntitySnapshot? _memoSnapshot;
    private DateTime _memoTime;
    private (Vector3 absolute, Vector3 relative) _memo;

    public SnapshotPosition(GlobalUIState state, string systemId, int entityId)
    {
        _state = state;
        _systemId = systemId;
        _entityId = entityId;
    }

    public Vector3 AbsolutePosition => Current().absolute;

    public Vector3 RelativePosition => Current().relative;

    private (Vector3 absolute, Vector3 relative) Current()
    {
        var galaxy = _state.GameClient?.Galaxy;
        var system = galaxy?.GetSystem(_systemId);
        var entity = system?.GetEntity(_entityId);
        if (galaxy == null || system == null || entity == null)
            return _memo;

        DateTime now = galaxy.Time.GameDateTime;
        if (ReferenceEquals(entity, _memoSnapshot) && now == _memoTime)
            return _memo;

        Vector3 relative;
        var orbit = entity.GetView<OrbitView>();
        if (orbit != null && orbit.StandardGravParameter > 0)
        {
            relative = orbit.RelativePositionM(now);
        }
        else
        {
            var position = entity.GetView<PositionView>();
            relative = position != null
                ? new Vector3(position.RelativePosition.X, position.RelativePosition.Y, position.RelativePosition.Z)
                : Vector3.Zero;
        }

        _memoSnapshot = entity;
        _memoTime = now;
        _memo = (entity.AbsolutePositionM(system, now), relative);
        return _memo;
    }
}
