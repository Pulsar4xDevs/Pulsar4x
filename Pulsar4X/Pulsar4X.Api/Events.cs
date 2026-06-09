namespace Pulsar4X.Api;

/// <summary>
/// Server→client notifications that keep the client's replicated galaxy model current. Mirrors the
/// engine's <c>MessageTypes</c>. Richer log/notification events (combat, research, construction, …)
/// are layered on in a later phase.
/// </summary>
public enum GameEventType
{
    EntityAdded,
    EntityRemoved,
    EntityRevealed,
    EntityHidden,
    EntityChanged,   // one or more views on an entity changed; payload carries the new view
    SystemRevealed,
    EntityRenamed,
}

/// <summary>
/// A single faction-scoped notification. Carries enough identity for the client to locate the
/// affected entity/system in its galaxy model, plus an optional changed view as payload.
/// </summary>
public sealed record GameEventEnvelope(
    GameEventType Type,
    string? SystemId = null,
    int? EntityId = null,
    int? FactionId = null,
    IComponentView? Changed = null);
