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
    EntityChanged,   // one or more views on an entity changed; payload carries the new snapshot
    SystemRevealed,
    EntityRenamed,
    TimeChanged,     // the simulation clock advanced or its controls changed; payload carries Time
    FleetsChanged,   // the faction's fleet hierarchy changed; payload carries Fleets
    FactionChanged,  // the faction's identity/funds changed; payload carries Faction
}

/// <summary>
/// A single faction-scoped notification. Deltas are <b>self-contained</b>: the payload carries the new
/// state so the client applies it without a follow-up request (essential over a network — no per-event
/// round-trip). <see cref="Entity"/> is set for entity add/reveal/change/rename; <see cref="Time"/> for
/// <see cref="GameEventType.TimeChanged"/>; <see cref="System"/> for
/// <see cref="GameEventType.SystemRevealed"/> (the new system with its faction-visible entities).
/// Identity fields locate the target in the galaxy model.
/// </summary>
public sealed record GameEventEnvelope(
    GameEventType Type,
    string? SystemId = null,
    int? EntityId = null,
    int? FactionId = null,
    EntitySnapshot? Entity = null,
    TimeState? Time = null,
    SystemSnapshot? System = null,
    IReadOnlyList<FleetSnapshot>? Fleets = null,
    IReadOnlyList<ShipSnapshot>? UnattachedShips = null,
    FactionSnapshot? Faction = null);
