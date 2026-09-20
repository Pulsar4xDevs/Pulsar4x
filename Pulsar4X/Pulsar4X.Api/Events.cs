namespace Pulsar4X.Api;

/// <summary>
/// Server→client notifications that keep the client's replicated galaxy model current. The
/// sync-state types mirror the engine's <c>MessageTypes</c>; <see cref="LogEvent"/> carries the
/// game's log/notification stream (combat, research, construction, …).
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
    ResearchChanged, // the faction's research state changed; payload carries Research
    ComponentDesignsChanged,  // the faction's templates/designs changed; payload carries ComponentDesigns
    CommandersChanged, // the faction's personnel roster changed; payload carries Commanders
    LogEvent,        // one or more game-log entries for the faction; payload carries Log
}

/// <summary>
/// One entry of the faction's game log (the engine's <c>EventManager</c> stream, faction-filtered).
/// Display-ready: the event type travels as its engine name and the entity/faction names are
/// resolved server-side with the subscriber's faction scope, so the client renders rows without
/// resolving anything.
/// </summary>
public sealed record LogEvent(
    DateTime StarDate,
    string EventType,
    string Message,
    string? SystemId = null,
    int? EntityId = null,
    string? EntityName = null,
    string? FactionName = null);

/// <summary>
/// A single faction-scoped notification. Deltas are <b>self-contained</b>: the payload carries the new
/// state so the client applies it without a follow-up request (essential over a network — no per-event
/// round-trip). <see cref="Entity"/> is set for entity add/reveal/change/rename; <see cref="Time"/> for
/// <see cref="GameEventType.TimeChanged"/>; <see cref="System"/> for
/// <see cref="GameEventType.SystemRevealed"/> (the new system with its faction-visible entities);
/// <see cref="Log"/> for <see cref="GameEventType.LogEvent"/> (the backlog on connect, then
/// singles as they happen). Identity fields locate the target in the galaxy model.
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
    FactionSnapshot? Faction = null,
    ResearchSnapshot? Research = null,
    ComponentDesignsSnapshot? ComponentDesigns = null,
    IReadOnlyList<CommanderSnapshot>? Commanders = null,
    IReadOnlyList<LogEvent>? Log = null);
