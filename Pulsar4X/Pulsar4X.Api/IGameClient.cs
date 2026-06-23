namespace Pulsar4X.Api;

/// <summary>
/// The surface the client UI programs against, with no knowledge of whether the engine is
/// in-process or across a network. Implemented by <c>InProcessAdapter</c> (zero-copy, single
/// player) and <c>MultiplayerAdapter</c> (network).
///
/// Writes are asynchronous so they stay off the render path; the UI reads game state
/// synchronously from <see cref="Galaxy"/>, which the adapter keeps current from snapshots and the
/// server event stream.
/// </summary>
public interface IGameClient
{
    PlayerSession Session { get; }
    bool IsConnected { get; }

    /// <summary>The synchronously-readable replicated galaxy model the UI renders from.</summary>
    IClientGalaxy Galaxy { get; }

    // The async writes carry a CancellationToken so a network adapter can abort an in-flight call
    // (e.g. a player cancelling a hung ConnectAsync). The in-process adapter completes synchronously,
    // so the token only short-circuits an already-cancelled call.
    Task<ConnectResult> ConnectAsync(ConnectRequest request, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Submit a command. Over a network the token cancels the <i>wait</i> for the server's
    /// result — not the command's effect, which the server may still apply once it arrives.</summary>
    Task<CommandResult> SubmitCommandAsync(GameCommand command, CancellationToken cancellationToken = default);
    Task SetTimeControlAsync(TimeControlRequest request, CancellationToken cancellationToken = default);

    /// <summary>Tell the server which system the player is watching (engine processing priority).</summary>
    Task SetSystemFocusAsync(string? systemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies all server updates received since the previous call to <see cref="Galaxy"/> as a single
    /// batch, then raises <see cref="EventReceived"/> for each. Call exactly once per frame on the UI
    /// thread, before any window reads <see cref="Galaxy"/>, so updates land atomically at a frame
    /// boundary — the galaxy stays consistent (and untouched by other threads) for the whole frame.
    /// </summary>
    void Update();

    /// <summary>Raised (during <see cref="Update"/>) after an incoming server event is applied to <see cref="Galaxy"/>.</summary>
    event Action<GameEventEnvelope>? EventReceived;
}
