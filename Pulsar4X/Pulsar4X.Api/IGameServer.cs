namespace Pulsar4X.Api;

/// <summary>
/// The server-authoritative surface the engine exposes, scoped to a <see cref="PlayerSession"/>
/// (a faction). It is deliberately <b>push-only</b>: clients connect, subscribe, and issue writes —
/// all state arrives through the event stream (initial snapshot on <see cref="Subscribe"/>, then
/// self-contained deltas). <see cref="PreviewMove"/> is the one read: a move plan is too heavy
/// to put on every snapshot, and the window asks for the one target the player clicked.
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

    /// <summary>Plan a move from this ship to this body. Does not start anything.
    /// A null <paramref name="options"/> is today's defaults.</summary>
    TripPreview PreviewMove(PlayerSession session, int shipId, int bodyId, MoveOrderOptions? options = null);

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
