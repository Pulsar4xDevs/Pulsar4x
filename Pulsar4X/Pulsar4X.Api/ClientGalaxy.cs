namespace Pulsar4X.Api;

/// <summary>
/// The client-side replicated galaxy model: a synchronously-readable cache the UI renders from each
/// frame. An <see cref="IGameClient"/> keeps it current from bulk snapshots and the server event
/// stream, so the immediate-mode UI never has to await the (possibly remote) server while drawing.
/// </summary>
public interface IClientGalaxy
{
    TimeState Time { get; }
    IReadOnlyCollection<SystemSummary> KnownSystems { get; }
    IClientSystem? GetSystem(string systemId);
}

/// <summary>A single star system within the client galaxy model.</summary>
public interface IClientSystem
{
    string SystemId { get; }
    string Name { get; }
    DateTime DateTime { get; }
    IReadOnlyCollection<EntitySnapshot> Entities { get; }
    EntitySnapshot? GetEntity(int entityId);
}
