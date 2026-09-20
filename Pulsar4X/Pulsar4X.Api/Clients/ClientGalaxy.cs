namespace Pulsar4X.Api.Clients;

/// <summary>
/// The default mutable <see cref="IClientGalaxy"/> maintained by an adapter. The UI reads it through
/// the read-only interface; only the owning adapter mutates it (single-threaded with the UI loop).
/// </summary>
internal sealed class ClientGalaxy : IClientGalaxy
{
    private readonly Dictionary<string, ClientSystem> _systems = new();
    private readonly List<SystemSummary> _knownSystems = new();
    private readonly List<FleetSnapshot> _fleets = new();
    private readonly List<ShipSnapshot> _unattachedShips = new();
    private readonly List<CommanderSnapshot> _commanders = new();
    private readonly List<LogEvent> _eventLog = new();

    public TimeState Time { get; internal set; } = new(default, false, false, TimeSpan.FromHours(1), TimeSpan.FromSeconds(1));

    public IReadOnlyCollection<SystemSummary> KnownSystems => _knownSystems;

    public IReadOnlyList<FleetSnapshot> Fleets => _fleets;

    public IReadOnlyList<ShipSnapshot> UnattachedShips => _unattachedShips;

    public FactionSnapshot? Faction { get; internal set; }

    public ResearchSnapshot? Research { get; internal set; }

    public ComponentDesignsSnapshot? ComponentDesigns { get; internal set; }

    public IReadOnlyList<CommanderSnapshot> Commanders => _commanders;

    public IReadOnlyList<LogEvent> EventLog => _eventLog;

    public IClientSystem? GetSystem(string systemId)
        => _systems.TryGetValue(systemId, out var system) ? system : null;

    internal void SetFleets(IEnumerable<FleetSnapshot> fleets, IEnumerable<ShipSnapshot> unattachedShips)
    {
        _fleets.Clear();
        _fleets.AddRange(fleets);
        _unattachedShips.Clear();
        _unattachedShips.AddRange(unattachedShips);
    }

    internal void SetCommanders(IEnumerable<CommanderSnapshot> commanders)
    {
        _commanders.Clear();
        _commanders.AddRange(commanders);
    }

    internal void AddLogEvents(IEnumerable<LogEvent> events)
        => _eventLog.AddRange(events);

    internal void SetKnownSystems(IEnumerable<SystemSummary> summaries)
    {
        _knownSystems.Clear();
        _knownSystems.AddRange(summaries);
    }

    internal void AddKnownSystem(SystemSummary summary)
    {
        if (!_knownSystems.Any(s => s.SystemId == summary.SystemId))
            _knownSystems.Add(summary);
    }

    internal void UpsertSystem(SystemSnapshot snapshot)
        => _systems[snapshot.SystemId] = new ClientSystem(snapshot);

    internal ClientSystem? GetMutableSystem(string systemId)
        => _systems.TryGetValue(systemId, out var system) ? system : null;

    /// <summary>Drops all replicated state, for rebinding the owning adapter to a different faction.</summary>
    internal void Reset()
    {
        _systems.Clear();
        _knownSystems.Clear();
        _fleets.Clear();
        _unattachedShips.Clear();
        _commanders.Clear();
        _eventLog.Clear();
        Faction = null;
        Research = null;
        ComponentDesigns = null;
    }
}

internal sealed class ClientSystem : IClientSystem
{
    private readonly Dictionary<int, EntitySnapshot> _entities;

    public ClientSystem(SystemSnapshot snapshot)
    {
        SystemId = snapshot.SystemId;
        Name = snapshot.Name;
        DateTime = snapshot.DateTime;
        _entities = snapshot.Entities.ToDictionary(e => e.Id);
    }

    public string SystemId { get; }
    public string Name { get; }
    public DateTime DateTime { get; internal set; }

    public IReadOnlyCollection<EntitySnapshot> Entities => _entities.Values;

    public EntitySnapshot? GetEntity(int entityId)
        => _entities.TryGetValue(entityId, out var entity) ? entity : null;

    internal void Remove(int entityId) => _entities.Remove(entityId);
    internal void Upsert(EntitySnapshot entity) => _entities[entity.Id] = entity;
}
