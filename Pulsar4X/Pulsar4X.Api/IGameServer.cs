namespace Pulsar4X.Api;

/// <summary>
/// The server-authoritative surface the engine exposes, scoped to a <see cref="PlayerSession"/>
/// (a faction). It is deliberately <b>push-only</b>: clients connect, subscribe, and issue writes —
/// all state arrives through the event stream (initial snapshot on <see cref="Subscribe"/>, then
/// self-contained deltas). There are no read/query methods, so nothing is ever polled or fetched.
///
/// Implemented in the engine by <c>EngineGameServer</c>.
/// </summary>
public interface IGameServer
{
    // --- connection ---
    ConnectResult Connect(ConnectRequest request);
    void Disconnect(PlayerSession session);

    // --- writes ---
    void SetTimeControl(PlayerSession session, TimeControlRequest request);
    CommandResult SubmitCommand(PlayerSession session, GameCommand command);

    /// <summary>Tell the server which system this session is watching, so the engine can
    /// prioritise its processing (foreground-observer scheduling). Null clears the focus.</summary>
    void SetSystemFocus(PlayerSession session, string? systemId);

    // --- events (push) ---
    /// <summary>
    /// Subscribe to this faction's event stream. The current state is pushed immediately (time,
    /// faction, known systems, fleets); thereafter self-contained deltas keep it current. Dispose the
    /// returned token to unsubscribe.
    /// </summary>
    IDisposable Subscribe(PlayerSession session, Action<GameEventEnvelope> handler);
}
