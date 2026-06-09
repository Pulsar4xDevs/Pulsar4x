# Pulsar4X Client/Engine API Layer — Design

Status: **in progress** (branch `api`, fork-only). Goal: insert an API layer between the Client
and the GameEngine so the client has **no direct engine references** and all interaction flows
through a serializable contract — enabling in-process single-player, a headless dedicated server,
and network multiplayer from the same pieces.

## Decisions

1. **State boundary — bespoke view DTOs (CQRS read-model).** The engine projects faction-scoped,
   serializable *view* DTOs purpose-built for the UI (`EntitySnapshot` carrying `IComponentView`s).
   The engine only populates views a faction may see, so visibility/security is enforced at the
   boundary. Each aspect ports independently.
2. **Composition — layered, with a separate headless server host.** `EngineGameServer` lives in
   `GameEngine` and has no UI dependency, so it can run headless. The client *UI library* references
   **only** `Pulsar4X.Api`.
3. **Local path — zero-copy in-process adapter.** Single-player uses `InProcessAdapter` calling
   `EngineGameServer` directly (no serialization). The desktop host exe references the engine to
   wire it up; the UI library does not.

## Project graph

```
Pulsar4X.Api          contracts: IGameClient, IGameServer, DTOs        (no engine/UI deps)
  ▲          ▲
GameEngine   │        implements IGameServer (EngineGameServer) — pure sim, headless-capable
  ▲          │
Pulsar4X.Server.Host  ◄─ headless dedicated-server EXE (engine + network host, no UI)
             │
Pulsar4X.Client       UI library — references ONLY Pulsar4X.Api; owns the replicated galaxy model
  ▲                     + InProcessAdapter + MultiplayerAdapter (both : IGameClient)
  │
Pulsar4X.Client.Host  desktop EXE — composition root (SP: engine + InProcessAdapter; MP: adapter only)
```

Three run modes from the same components:
- **Single-player:** `Client.Host` builds `EngineGameServer`, hands it to `InProcessAdapter`.
- **Dedicated server:** `Pulsar4X.Server.Host`, headless, exposes `IGameServer` over the network.
- **Network client:** `Client.Host` uses `MultiplayerAdapter` → remote server.

## Why the replicated galaxy model

The client is **immediate-mode (ImGui)** — it reads state synchronously every frame, so the
boundary cannot be `await`ed per-frame. Therefore `IGameClient` exposes `IClientGalaxy`: a
synchronously-readable cache the UI renders from, kept current by an initial snapshot plus the
server event stream. Commands go out async, off the render path. Today's
`GlobalUIState`/`SystemState`/`EntityState` already *are* this model (cache + `MessagePublisher`
updates) — porting converts them to hold view DTOs instead of live engine objects.

**Update timing (atomic, frame-aligned).** Server events arrive on engine/threadpool threads, so the
adapter only *enqueues* them (thread-safe). The galaxy is mutated solely on the UI thread in
`IGameClient.Update()`, which the main loop (`PulsarMainWindow.Update`) calls once per frame
before any window reads `Galaxy`. The whole batch of pending updates is applied at that single frame
boundary, so within a frame the galaxy is consistent and never torn by a background thread.

**Replication is push-only on the hot path (network-ready).** The server pushes *self-contained* deltas
— entity add/reveal/change/rename carry the new `EntitySnapshot`; the clock pushes `TimeChanged`
(carrying `TimeState`) both when it advances (`MasterTimePulse.GameGlobalDateChangedEvent`) and when its
controls change (inside `SetTimeControl`). A `SystemRevealed` push carries the whole new
`SystemSnapshot` — the system plus every entity now visible to that faction — so the client adds it with
no follow-up pull. The adapter applies every delta with no callback to the server, so nothing is polled
and a network adapter just feeds the same inbound queue from a socket. Request/response is reserved for
genuine bulk/cold pulls only: the initial connect snapshot and an explicit `LoadSystemAsync`.

## Current coupling (what we're replacing)

| Dimension | Today | Surface |
|---|---|---|
| Reads | client holds live `Game`/`Entity`/`DataBlob`; `Entity.GetDataBlob<T>()` per frame | 55+ DataBlobs |
| Events engine→client | `MessagePublisher` (8 sync-state msgs) + `EventManager` (150+ log events) + sensor-contact queue | — |
| Commands client→engine | `EntityCommand` → `game.OrderHandler.HandleOrder` | ~45 commands |
| Lifecycle/time | `Game.Save/Load`, `new Game`, `MasterTimePulse` | small |

Notes: no networking exists yet (greenfield); DataBlobs are mostly serializable POCOs but some hold
live `Entity` references — bespoke view DTOs sidestep that entirely. Entities are identified by
`int Id` within `string` system/manager ids.

## Phased plan

1. **API layer scaffold (done):** `Pulsar4X.Api` project + `IGameServer`/`IGameClient`,
   session/time/snapshot/command/event contracts, example views & one example command.
2. **Vertical slice (done):** `EngineGameServer` (in `GameEngine/Api/`) + `InProcessAdapter` +
   mutable client galaxy model, implementing connect, time control, the system-map read (with
   `Name`/`Position`/`Orbit` view projection), command routing, and a `MessagePublisher`→event
   bridge. The `InProcessAdapter` + galaxy model live in `Pulsar4X.Client/Api/`; the server contract is
   covered by `Pulsar4X.Tests/ApiVerticalSliceTests.cs` (8 tests, no UI dependency).
3. **Commands (foundation done):** `IOrderHandler.HandleOrder` now returns a validity bool, so
   `SubmitCommand` reports real results. `EngineGameServer` has an extensible translator registry
   with a uniform ownership pre-check (a faction may only command entities it owns). `RenameCommand`
   is the ported reference. **Porting recipe for each remaining command:** (a) add a `GameCommand`
   DTO in Pulsar4X.Api; (b) make the engine's `CreateXxx` factory return the `HandleOrder` bool;
   (c) add a translator method + one registry entry in `EngineGameServer`. Commands with a secondary
   target carry it as a DTO field the translator resolves (only the commanded entity is ownership-checked).
4. **Read surface (in progress):** the engine-side projection now covers `Name`, `Position`, `Orbit`,
   `MassVolume`, `Body`, `Star`, `Colony`, and `Ship` views (8 of ~55). **Projection recipe per view:**
   (a) add an `IComponentView` DTO in Pulsar4X.Api; (b) add a `TryGetDataBlob`→view block in
   `EngineGameServer.Project`; (c) extend the snapshot test. Remaining: port the rest of the views, then
   the larger client-side step — rewire `EntityState`/`SystemState` (and the UI windows) to read these
   views via `IClientGalaxy` instead of live engine `Entity`/`DataBlob` objects. First UI ports done:
   the **Selector**'s system list reads `Galaxy.KnownSystems`, and the **TimeControl** reads
   `Galaxy.Time` and drives the clock via `IGameClient.SetTimeControlAsync` (pause/start/step/tick
   length/frequency) — no longer touching `MasterTimePulse` directly.
5. **Events:** map `MessagePublisher`/`EventManager` to the `GameEventEnvelope` stream.
6. **Client composition (`Pulsar4X.Client.Host`):** once the UI consumes the galaxy model (4) and the
   event stream (5), extract a thin desktop executable `Pulsar4X.Client.Host` as the composition root —
   it references both `Pulsar4X.Client` and `GameEngine`, and wires single-player as
   `new InProcessAdapter(new EngineGameServer(game))` (and, later, `MultiplayerAdapter` for remote
   play). Then drop the `GameEngine` `ProjectReference` from `Pulsar4X.Client` so the UI library
   depends only on `Pulsar4X.Api` — completing the "no engine references in the client" goal. (This is
   also the point to relocate `InProcessAdapter` out of `Pulsar4X.Api` if desired.)
7. **Network adapter + server host:** transport + serialization for `MultiplayerAdapter` and the
   headless `Pulsar4X.Server.Host`.

## Contract surface (initial)

- Identity: `PlayerSession`, `ConnectRequest`, `ConnectResult`, `GameInfo`.
- Time: `TimeState`, `TimeControlRequest`.
- Reads: `SystemSummary`, `SystemSnapshot`, `EntitySnapshot` + `IComponentView` (`NameView`,
  `PositionView`, `OrbitView`, `MassVolumeView`, `BodyView`, `StarView`, `ColonyView`, `ShipView`
  so far), `OwnerRelation`, `Vec3`.
- Writes: `GameCommand` (+ `RenameCommand`), `CommandResult`.
- Events: `GameEventType`, `GameEventEnvelope`.
- Interfaces: `IGameServer`, `IGameClient`, `IClientGalaxy`, `IClientSystem`.

The pre-existing empty `Pulsar4X.Contracts` stub is superseded by `Pulsar4X.Api` and can be removed.

## Known gaps (to address as porting proceeds)

- ~~Command validation isn't surfaced.~~ **Resolved (phase 3):** `HandleOrder` returns a validity
  bool; `SubmitCommand` does an ownership pre-check and returns the engine's real accept/reject.
- **Continuous state (positions) isn't streamed yet.** Position/orbit updates mutate existing
  DataBlobs without firing add/remove events, so galaxy entity snapshots don't yet receive per-tick
  position changes. The map still reads live engine state; when it's ported, positions will need either
  a periodic position delta or client-side orbit propagation from `OrbitView`.
- **Faction selection on connect is naive** — binds to the first faction. Real selection/auth via
  `ConnectRequest.Credential` lands with networking.
- ~~The in-process adapter lives in `Pulsar4X.Api`.~~ **Resolved:** `InProcessAdapter` and the
  `ClientGalaxy`/`ClientSystem` model now live in `Pulsar4X.Client/Api/` (namespace `Pulsar4X.Client`),
  which references `Pulsar4X.Api` directly. The contracts assembly holds only interfaces + DTOs, and
  `Pulsar4X.Tests` drives `EngineGameServer` through `IGameServer` directly (no UI dependency).
- **Two `PositionDB` classes exist** (`Pulsar4X.Datablobs` legacy/excluded vs the live
  `Pulsar4X.Movement`); projection uses the live one. Worth cleaning up the dead copy separately.
