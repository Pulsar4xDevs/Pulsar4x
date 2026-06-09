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
Pulsar4X.Client       UI library — references ONLY Pulsar4X.Api; owns the replicated world model
  ▲                     + InProcessAdapter + MultiplayerAdapter (both : IGameClient)
  │
Pulsar4X.Client.Host  desktop EXE — composition root (SP: engine + InProcessAdapter; MP: adapter only)
```

Three run modes from the same components:
- **Single-player:** `Client.Host` builds `EngineGameServer`, hands it to `InProcessAdapter`.
- **Dedicated server:** `Pulsar4X.Server.Host`, headless, exposes `IGameServer` over the network.
- **Network client:** `Client.Host` uses `MultiplayerAdapter` → remote server.

## Why the replicated world model

The client is **immediate-mode (ImGui)** — it reads state synchronously every frame, so the
boundary cannot be `await`ed per-frame. Therefore `IGameClient` exposes `IClientWorld`: a
synchronously-readable cache the UI renders from, kept current by an initial snapshot plus the
server event stream. Commands go out async, off the render path. Today's
`GlobalUIState`/`SystemState`/`EntityState` already *are* this model (cache + `MessagePublisher`
updates) — porting converts them to hold view DTOs instead of live engine objects.

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
2. **Vertical slice:** `EngineGameServer` + `InProcessAdapter` implementing time control + the
   system-map read end-to-end through the API, to validate the shape.
3. **Commands:** port the ~45 `EntityCommand`s to `GameCommand` DTOs + server translation.
4. **Read surface:** port the ~55 views area by area; convert `EntityState`/`SystemState` to DTOs.
5. **Events:** map `MessagePublisher`/`EventManager` to the `GameEventEnvelope` stream.
6. **Network adapter + server host:** transport + serialization for `MultiplayerAdapter` and
   `Pulsar4X.Server.Host`.

## Contract surface (initial)

- Identity: `PlayerSession`, `ConnectRequest`, `ConnectResult`, `GameInfo`.
- Time: `TimeState`, `TimeControlRequest`.
- Reads: `SystemSummary`, `SystemSnapshot`, `EntitySnapshot` + `IComponentView` (`NameView`,
  `PositionView`, `OrbitView` so far), `OwnerRelation`, `Vec3`.
- Writes: `GameCommand` (+ `RenameCommand`), `CommandResult`.
- Events: `GameEventType`, `GameEventEnvelope`.
- Interfaces: `IGameServer`, `IGameClient`, `IClientWorld`, `IClientSystem`.

The pre-existing empty `Pulsar4X.Contracts` stub is superseded by `Pulsar4X.Api` and can be removed.
