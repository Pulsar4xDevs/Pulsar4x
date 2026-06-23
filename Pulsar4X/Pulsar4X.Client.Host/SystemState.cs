using Pulsar4X.Engine;

namespace Pulsar4X.Client.Host;

/// <summary>
/// Engine-backed system handle for the host's dev tooling. The UI library's old SystemState
/// (engine entity caches behind the click pipeline) is gone — player-facing UI reads the
/// replicated galaxy — but the debug/SM windows inspect live engine objects, which only the
/// composition root can reach.
/// </summary>
public sealed class SystemState
{
    public StarSystem StarSystem { get; }

    public SystemState(StarSystem starSystem)
    {
        StarSystem = starSystem;
    }
}
