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

    Task<ConnectResult> ConnectAsync(ConnectRequest request);
    Task DisconnectAsync();

    Task<CommandResult> SubmitCommandAsync(GameCommand command);
    Task SetTimeControlAsync(TimeControlRequest request);

    /// <summary>Tell the server which system the player is watching (engine processing priority).</summary>
    Task SetSystemFocusAsync(string? systemId);

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
