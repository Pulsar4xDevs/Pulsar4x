namespace Pulsar4X.Api;

/// <summary>
/// The server-authoritative surface the engine exposes. Every read and command is scoped to a
/// <see cref="PlayerSession"/> (a faction), so the server enforces visibility and ownership.
///
/// Implemented in the engine by <c>EngineGameServer</c>.
/// </summary>
public interface IGameServer
{
    // --- connection ---
    ConnectResult Connect(ConnectRequest request);
    void Disconnect(PlayerSession session);

    // --- time (host / space-master authority) ---
    TimeState GetTimeState(PlayerSession session);
    void SetTimeControl(PlayerSession session, TimeControlRequest request);

    // --- commands (write) ---
    CommandResult SubmitCommand(PlayerSession session, GameCommand command);

    // --- queries (faction-scoped reads) ---
    IReadOnlyList<SystemSummary> GetKnownSystems(PlayerSession session);
    SystemSnapshot GetSystemSnapshot(PlayerSession session, string systemId);
    EntitySnapshot? GetEntitySnapshot(PlayerSession session, int entityId);

    // --- events (push) ---
    /// <summary>Subscribe to this faction's event stream. Dispose the returned token to unsubscribe.</summary>
    IDisposable Subscribe(PlayerSession session, Action<GameEventEnvelope> handler);
}
