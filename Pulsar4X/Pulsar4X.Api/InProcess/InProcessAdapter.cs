namespace Pulsar4X.Api;

/// <summary>
/// Zero-copy in-process <see cref="IGameClient"/> for single-player / local games: forwards directly
/// to an <see cref="IGameServer"/> in the same process (no serialization), and keeps the replicated
/// <see cref="IClientWorld"/> current from bulk snapshots and the server event stream.
/// </summary>
public sealed class InProcessAdapter : IGameClient
{
    private readonly IGameServer _server;
    private readonly ClientWorld _world = new();
    private IDisposable? _subscription;

    public InProcessAdapter(IGameServer server) => _server = server;

    public PlayerSession Session { get; private set; } = PlayerSession.None;
    public bool IsConnected { get; private set; }
    public IClientWorld World => _world;
    public event Action<GameEventEnvelope>? EventReceived;

    public Task<ConnectResult> ConnectAsync(ConnectRequest request)
    {
        var result = _server.Connect(request);
        if (result.Success)
        {
            Session = result.Session;
            IsConnected = true;
            _subscription = _server.Subscribe(Session, OnServerEvent);
            _world.SetKnownSystems(_server.GetKnownSystems(Session));
            _world.Time = _server.GetTimeState(Session);
        }
        return Task.FromResult(result);
    }

    public Task DisconnectAsync()
    {
        _subscription?.Dispose();
        _subscription = null;
        if (IsConnected) _server.Disconnect(Session);
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<CommandResult> SubmitCommandAsync(GameCommand command)
        => Task.FromResult(_server.SubmitCommand(Session, command));

    public Task SetTimeControlAsync(TimeControlRequest request)
    {
        _server.SetTimeControl(Session, request);
        _world.Time = _server.GetTimeState(Session);
        return Task.CompletedTask;
    }

    public Task LoadSystemAsync(string systemId)
    {
        _world.UpsertSystem(_server.GetSystemSnapshot(Session, systemId));
        return Task.CompletedTask;
    }

    private void OnServerEvent(GameEventEnvelope evt)
    {
        ApplyToWorld(evt);
        EventReceived?.Invoke(evt);
    }

    // Minimal world maintenance for the slice: structural changes re-fetch the affected entity.
    // As views/events are fully ported this grows into incremental snapshot patching.
    private void ApplyToWorld(GameEventEnvelope evt)
    {
        if (evt.SystemId is null) return;
        var system = _world.GetMutableSystem(evt.SystemId);
        if (system is null || evt.EntityId is not { } entityId) return;

        switch (evt.Type)
        {
            case GameEventType.EntityRemoved:
            case GameEventType.EntityHidden:
                system.Remove(entityId);
                break;

            case GameEventType.EntityAdded:
            case GameEventType.EntityRevealed:
            case GameEventType.EntityChanged:
            case GameEventType.EntityRenamed:
                var snapshot = _server.GetEntitySnapshot(Session, entityId);
                if (snapshot != null) system.Upsert(snapshot);
                break;
        }
    }
}
