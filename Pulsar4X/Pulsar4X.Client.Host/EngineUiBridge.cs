using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Movement;

namespace Pulsar4X.Client.Host;

/// <summary>
/// Bridges the UI library's engine-free types back to live engine objects for the host's dev
/// tooling: the player-facing UI carries only ids, but the debug/SM windows inspect the engine.
/// </summary>
public static class EngineUiBridge
{
    /// <summary>Resolve the engine entity behind a clicked/selected <see cref="EntityState"/>.</summary>
    public static Entity? GetEntity(this EntityState state)
    {
        if (state.StarSystemId is not { } systemId) return null;
        var system = GameLifecycle.Instance?.Game?.Systems.FirstOrDefault(s => s.ID.Equals(systemId));
        if (system == null) return null;
        return system.TryGetEntityById(state.Id, out var entity) ? entity : null;
    }
}

/// <summary>Adapts an engine <see cref="PositionDB"/> to the UI library's <see cref="IPosition"/>
/// seam so dev tooling can hang map widgets off live engine positions.</summary>
public sealed class PositionDBAdapter : IPosition
{
    private readonly PositionDB _db;

    public PositionDBAdapter(PositionDB db)
    {
        _db = db;
    }

    public Orbital.Vector3 AbsolutePosition => _db.AbsolutePosition;
    public Orbital.Vector3 RelativePosition => _db.RelativePosition;
}
