using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Zero-copy in-process <see cref="IGameClient"/> for single-player / local games: forwards directly
/// to an <see cref="IGameServer"/> in the same process (no serialization), and keeps the replicated
/// <see cref="IClientGalaxy"/> current from bulk snapshots and the server event stream.
/// </summary>
public sealed class InProcessAdapter : IGameClient, IDesignDataProvider
{
    private readonly IGameServer _server;
    private readonly ClientGalaxy _galaxy = new();

    // Server events arrive on engine/threadpool threads; we only enqueue here (thread-safe) and apply
    // them to the galaxy on the UI thread in Update(), so the galaxy is never mutated mid-frame.
    private readonly ConcurrentQueue<GameEventEnvelope> _inbound = new();
    private IDisposable? _subscription;

    public InProcessAdapter(IGameServer server) => _server = server;

    public PlayerSession Session { get; private set; } = PlayerSession.None;
    public bool IsConnected { get; private set; }
    public IClientGalaxy Galaxy => _galaxy;
    public event Action<GameEventEnvelope>? EventReceived;

    public Task<ConnectResult> ConnectAsync(ConnectRequest request)
    {
        var result = _server.Connect(request);
        if (result.Success)
        {
            Session = result.Session;
            IsConnected = true;
            // Subscribing primes the inbound queue with the initial state (known systems + time)
            // pushed by the server — no client-side fetch. It's applied on the next Update().
            _subscription = _server.Subscribe(Session, OnServerEvent);
        }
        return Task.FromResult(result);
    }

    public Task DisconnectAsync()
    {
        _subscription?.Dispose();
        _subscription = null;
        if (IsConnected) _server.Disconnect(Session);
        IsConnected = false;
        // In-process the adapter owns this server instance; release its engine hooks.
        (_server as IDisposable)?.Dispose();
        return Task.CompletedTask;
    }

    public Task<CommandResult> SubmitCommandAsync(GameCommand command)
        => Task.FromResult(_server.SubmitCommand(Session, command));

    public Task SetTimeControlAsync(TimeControlRequest request)
    {
        // The server applies the change and broadcasts a TimeChanged delta back to us; the galaxy's
        // Time updates when we drain that in Update() — no local read-back (which would be a round-trip
        // over a network).
        _server.SetTimeControl(Session, request);
        return Task.CompletedTask;
    }

    // In-process the design-time data is the engine's own objects, zero-copy. The downcast is this
    // adapter's prerogative: it always wraps an EngineGameServer; a network adapter implements the
    // same interface from state synced on connect.
    public bool TryGetDesignData(out Pulsar4X.Factions.FactionInfoDB info, out Pulsar4X.Factions.FactionTechDB techs)
    {
        if (_server is Pulsar4X.Engine.Api.EngineGameServer engine
            && engine.GetFactionDesignData(Session) is { } data)
        {
            (info, techs) = data;
            return true;
        }

        info = null!;
        techs = null!;
        return false;
    }

    // Called on an engine/threadpool thread — just queue; do not touch the galaxy here.
    private void OnServerEvent(GameEventEnvelope evt) => _inbound.Enqueue(evt);

    public void Update()
    {
        // Drain everything received since last frame and apply it as one batch on the UI thread.
        // The clock arrives the same way (pushed TimeChanged deltas) — no polling.
        while (_inbound.TryDequeue(out var evt))
        {
            ApplyToGalaxy(evt);
            EventReceived?.Invoke(evt);
        }
    }

    // Applies a self-contained delta to the galaxy. Deltas carry their payload, so this never calls
    // back to the server (which would be a round-trip over a network).
    private void ApplyToGalaxy(GameEventEnvelope evt)
    {
        switch (evt.Type)
        {
            case GameEventType.TimeChanged:
                if (evt.Time != null) _galaxy.Time = evt.Time;
                return;

            case GameEventType.FleetsChanged:
                _galaxy.SetFleets(evt.Fleets ?? System.Array.Empty<FleetSnapshot>(),
                                  evt.UnattachedShips ?? System.Array.Empty<ShipSnapshot>());
                return;

            case GameEventType.FactionChanged:
                if (evt.Faction != null) _galaxy.Faction = evt.Faction;
                return;

            case GameEventType.ResearchChanged:
                if (evt.Research != null) _galaxy.Research = evt.Research;
                return;

            case GameEventType.ComponentDesignsChanged:
                if (evt.ComponentDesigns != null) _galaxy.ComponentDesigns = evt.ComponentDesigns;
                return;

            case GameEventType.SystemRevealed:
                // The reveal carries the whole system + its visible entities — apply it directly.
                if (evt.System != null)
                {
                    _galaxy.AddKnownSystem(new SystemSummary(evt.System.SystemId, evt.System.Name));
                    _galaxy.UpsertSystem(evt.System);
                }
                return;
        }

        if (evt.SystemId is null) return;
        var system = _galaxy.GetMutableSystem(evt.SystemId);
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
                if (evt.Entity != null) system.Upsert(evt.Entity);
                break;
        }
    }
}
