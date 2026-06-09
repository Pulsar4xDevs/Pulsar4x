using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// The default mutable <see cref="IClientGalaxy"/> maintained by an adapter. The UI reads it through
/// the read-only interface; only the owning adapter mutates it (single-threaded with the UI loop).
/// </summary>
internal sealed class ClientGalaxy : IClientGalaxy
{
    private readonly Dictionary<string, ClientSystem> _systems = new();
    private IReadOnlyCollection<SystemSummary> _knownSystems = Array.Empty<SystemSummary>();

    public TimeState Time { get; internal set; } = new(default, false, 1f, TimeSpan.FromHours(1), TimeSpan.FromSeconds(1));

    public IReadOnlyCollection<SystemSummary> KnownSystems => _knownSystems;

    public IClientSystem? GetSystem(string systemId)
        => _systems.TryGetValue(systemId, out var system) ? system : null;

    internal void SetKnownSystems(IEnumerable<SystemSummary> summaries)
        => _knownSystems = summaries.ToList();

    internal void UpsertSystem(SystemSnapshot snapshot)
        => _systems[snapshot.SystemId] = new ClientSystem(snapshot);

    internal ClientSystem? GetMutableSystem(string systemId)
        => _systems.TryGetValue(systemId, out var system) ? system : null;
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
