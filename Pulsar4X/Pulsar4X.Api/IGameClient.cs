namespace Pulsar4X.Api;

/// <summary>
/// The surface the client UI programs against, with no knowledge of whether the engine is
/// in-process or across a network. Implemented by <c>InProcessAdapter</c> (zero-copy, single
/// player) and <c>MultiplayerAdapter</c> (network).
///
/// Writes are asynchronous so they stay off the render path; the UI reads game state
/// synchronously from <see cref="World"/>, which the adapter keeps current from snapshots and the
/// server event stream.
/// </summary>
public interface IGameClient
{
    PlayerSession Session { get; }
    bool IsConnected { get; }

    /// <summary>The synchronously-readable replicated world model the UI renders from.</summary>
    IClientWorld World { get; }

    Task<ConnectResult> ConnectAsync(ConnectRequest request);
    Task DisconnectAsync();

    Task<CommandResult> SubmitCommandAsync(GameCommand command);
    Task SetTimeControlAsync(TimeControlRequest request);

    /// <summary>Ensure the given system is loaded into <see cref="World"/> (initial bulk fetch).</summary>
    Task LoadSystemAsync(string systemId);

    /// <summary>Raised after an incoming server event has been applied to <see cref="World"/>.</summary>
    event Action<GameEventEnvelope>? EventReceived;
}
