namespace Pulsar4X.Client;

/// <summary>
/// The position seam the map renders from: icons and widgets hold one of these instead of any
/// game data. Implemented by <see cref="SnapshotPosition"/> (replicated-galaxy backed) and
/// <see cref="StaticPosition"/> (fixed coordinates).
/// </summary>
public interface IPosition
{
    Orbital.Vector3 AbsolutePosition { get; }
    Orbital.Vector3 RelativePosition { get; }
}
