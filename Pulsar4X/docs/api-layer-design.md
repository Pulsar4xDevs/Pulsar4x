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

**Replication is fully push-based (network-ready); the client never fetches.** On `Subscribe` the server
immediately pushes the subscriber's starting state — a `TimeChanged` and a `SystemRevealed`
(carrying a full `SystemSnapshot`) for every system the faction already knows — so the client gets its
world with no request. Thereafter the server pushes *self-contained* deltas: entity
add/reveal/change/rename carry the new `EntitySnapshot`; the clock pushes `TimeChanged` both when it
advances (`MasterTimePulse.GameGlobalDateChangedEvent`) and when its controls change (inside
`SetTimeControl`); a newly revealed system pushes its whole `SystemSnapshot`. The adapter applies every
delta with no callback to the server, so nothing is polled or fetched — its only server calls are
`Connect`/`Subscribe`/`Disconnect` and the command writes (`SubmitCommand`, `SetTimeControl`). A network
adapter just feeds the same inbound queue from a socket. `IGameServer` is therefore **push-only** — its
whole surface is `Connect`/`Disconnect`, the two writes, and `Subscribe`; there are no read/query
methods at all. (Projection is exercised in tests via the internal `GameProjector` directly.)

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
   covered by the fixtures in `Pulsar4X.Tests/API/` (one file per area — connection, system
   projection, time control, commands, fleets, research, colonies — sharing `ApiTestBase`; no UI
   dependency).
3. **Commands (foundation done):** `IOrderHandler.HandleOrder` now returns a validity bool, so
   `SubmitCommand` reports real results. `EngineGameServer` has an extensible translator registry
   with a uniform ownership pre-check (a faction may only command entities it owns — or itself, for
   faction-targeted commands like `CreateFleet`). `RenameCommand` is the ported reference.
   **Porting recipe for each remaining command:** (a) add a `GameCommand`
   DTO in Pulsar4X.Api; (b) make the engine's `CreateXxx` factory return the `HandleOrder` bool;
   (c) add a `Translate*` method + one registry entry in `CommandTranslator`. Commands with a secondary
   target carry it as a DTO field the translator resolves (only the commanded entity is ownership-checked).
   The fleet surface is ported (11 of ~45): `CreateFleet` (server generates the name), `DisbandFleet`,
   `ChangeFleetParent` (faction id = re-parent to root), `ReassignShip` (the translator finds and
   detaches from the current holder, so the client sends one command), `SetFlagship`, `MoveToBody`,
   `GeoSurvey`/`GravSurvey` (warp + survey pair), `Jump` (rejected unless the faction has discovered
   the jump point — visibility enforced at the boundary), and `RefuelAt` (warp + refuel transfer).
4. **Read surface (in progress):** the engine-side projection now covers `Name`, `Position`, `Orbit`,
   `MassVolume`, `Body`, `Star`, `Colony`, and `Ship` views (8 of ~55). **Projection recipe per view:**
   (a) add an `IComponentView` DTO in Pulsar4X.Api; (b) add one entry to `GameProjector.ViewProjectors`
   (plus a small `To*View` helper if it needs logic); (c) extend the snapshot test. Remaining: port the
   rest of the views, then
   the larger client-side step — rewire `EntityState`/`SystemState` (and the UI windows) to read these
   views via `IClientGalaxy` instead of live engine `Entity`/`DataBlob` objects. First UI ports done:
   the **Selector**'s system list reads `Galaxy.KnownSystems`, and the **TimeControl** reads
   `Galaxy.Time` and drives the clock via `IGameClient.SetTimeControlAsync` (pause/start/step/tick
   length/frequency) — no longer touching `MasterTimePulse` directly. The **Selector's celestial-body
   list** reads the active system's `EntitySnapshot`s (hierarchy from `OrbitView`/`PositionView`
   `ParentId`, sorted by `OrbitView.SemiMajorAxisKm`, classified via `EntitySnapshot.Kind`/`BodyKind`),
   selecting through the id-based `EntityClicked` + `Camera.CenterOnPosition`. Known systems (with their
   entities) are pushed to the galaxy model on connect, so no per-system load is needed. The
   **Selector's fleet/ship list** reads `Galaxy.Fleets` — a faction-scoped `FleetSnapshot` tree
   (sub-fleets, member `ShipSnapshot`s, flagship, orders, location) projected server-side and pushed via
   a `FleetsChanged` delta on connect, when a fleet op runs (the engine raises a `FleetReorganized`
   message from `FleetOrder.Execute` — create/disband/assign/transfer/etc., which reshape the
   `TreeHierarchyDB` with no entity add/remove), and as a backstop on faction entity add/remove/rename;
   ship clicks use `EntityClicked(id,…)` + the ship's `PositionView` for centring, fleet clicks
   `FleetWindow.SelectFleet(id)`. The **Selector's colonies list** needs no new model — colonies are
   already `Kind == Colony` entities in the per-system snapshots; it aggregates those with
   `Relation == Owned` across known systems, selecting via `ColonyManagementWindow.SelectColony(id, systemId)`.
   The **Selector's Corporation section** reads `Galaxy.Faction` (a `FactionSnapshot`: name, abbreviation,
   funds) — pushed via `FactionChanged` on connect and on each clock advance (funds track the economy);
   pushed per-subscription since funds are faction-specific.
   The **FleetWindow** is ported except its Standing Orders tab (see gaps): selection is by fleet id
   against `Galaxy.Fleets`, re-resolved every frame since fleet pushes replace the whole tree.
   `FleetSnapshot` carries what its Summary/list UI shows (flagship + commander names, current
   system id/name, the nearest *faction-visible* orbit parent — resolved server-side so hidden
   anomalies are skipped — `OrderSnapshot`s with run state, geo/grav survey ability flags) and
   `ShipSnapshot` carries design/commander/orders for tooltips; the hierarchy push also includes the
   faction root's `UnattachedShips`. The fleet tree (and order progress/locations within it) is
   re-pushed on every clock advance in addition to `FleetReorganized` deltas. Its Issue Orders tab
   reads the fleet's system snapshot, filtering on views: `BodyView`+`PositionView` (move),
   `GeoSurveyView`/`GravSurveyView` (incomplete surveys, faction-scoped), `JumpPointView` (only
   projected once discovered), `CargoStorageView` on colonies (refuel). The **RenameWindow** submits
   the API `RenameCommand` by entity id (a `SetEntity` shim remains for unported callers).
   The **ResearchWindow** is fully ported. Labs are entities with a `ResearcherView` (design,
   location, scientist, cost/points-per-day as `ModifiedValue` breakdowns for tooltips, funding,
   tech-queue ids) — projected only for the owning faction, so lab internals never leak. Faction
   research state travels as a `ResearchSnapshot` (`ResearchChanged` push on connect + each clock
   advance): tech categories, every unlocked tech (`TechSnapshot` with progress, researchability,
   next-level unlock names pre-resolved server-side), and the faction's scientists
   (`CommanderSnapshot` incl. bonuses with resolved filter names, consumed by a snapshot-based
   `DisplayHelpers.PeopleChooser` overload; the engine overload remains for the unported
   AdminWindow). Because the research instant orders mutate DataBlobs without raising messages,
   `SubmitCommand` now re-projects and pushes the commanded entity (`EntityChanged`) after every
   accepted command — a generic post-write refresh all windows benefit from.
   The **ColonyManagementWindow** is fully ported. Colony selection is by entity id + system id
   against the system snapshots. New views: `AtmosphereView` (on the planet, gas names pre-resolved),
   `InfrastructureView`, `InstallationsView` (grouped by design, with a server-computed `CanStore`),
   a full `CargoStorageView` replacing the old marker (stores → items with escrow/mass/volume and a
   server-computed `CanInstall`; still owner-only, so its presence keeps marking refuel targets),
   `ColonyMiningView` (a read-model joining colony mining rates, the planet's faction-masked deposits
   and the stockpile into display rows), `NavalAcademyView`, plus `ColonyView.SpeciesPopulations` and
   `BodyView` radiation/dust. All colony-internal views are owner-only — enforced and tested at the
   boundary. Commands: `UninstallComponentCommand` (design id; the translator picks an instance and
   chains uninstall + add-to-storage) and `InstallComponentCommand` (cargo-item id; remove-from-storage
   + install). Colony economics mutate quietly every econ tick, so the server re-pushes each faction's
   colonies (and their planets) on every clock advance. The snapshot-based atmosphere/installations/
   cargo displays live alongside their engine overloads (still used by EntityWindow/ship UI).
   Its **Production tab** reads an `IndustryView`: production lines with their job queues
   (`IndustryJobView` incl. status/progress and pre-resolved remaining-resource names) and what each
   line can build (`ConstructibleItemView` with per-unit industry points, outputs, auto-install
   eligibility, and `IndustryCostItem` cost previews against the local stockpile — so the client
   never constructs an engine `IndustryJob` for preview math the way the old display did). Commands:
   `QueueIndustryJobCommand` (the translator builds and initialises the engine job, applying
   auto-install only for colony installations), `ChangeIndustryJobPriorityCommand`,
   `CancelIndustryJobCommand`. The **Construction tab** reads a `ConstructionView` (points/day, the
   FIFO queue with progress, and the faction's queueable installation designs) with
   `AddToConstructionQueueCommand` and, since local-construction jobs carry no id,
   `MoveConstructionJobCommand`/`RemoveConstructionJobCommand` addressing jobs by queue position.
   Both tabs render via new snapshot-based `ColonyProductionDisplay`/`ColonyConstructionDisplay`
   singletons (the engine `IndustryDisplay`/`ConstructionDisplay` remain for the unported
   EntityWindow).
   The **SystemWindow** is fully ported: it rebuilds the body tree from the snapshot hierarchy
   (`OrbitView`/`PositionView` parent ids, stars at the root) and reads everything else from
   existing views. Small additions: `GeoSurveyView` now carries `HasSurveyStarted`/`PercentComplete`
   (survey progress is faction-scoped, computed server-side), `GasAmount` carries the gas id +
   partial pressure (for the oxygen column), and `ColonizableView`/`MineralDepositsView` markers.
   The Colonize button submits `CreateColonyCommand` (faction-targeted like `CreateFleet`; the
   server settles the faction's first species). The `BodyKind`→display-enum mapping moved from
   `Selector` to `UserOrbitSettings.FromBodyKind` for shared use with the map view-filter.
   The **GalaxyWindow** (galaxy browser) reads `Galaxy.KnownSystems` — no new surface needed.
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
  `PositionView`, `OrbitView`, `MassVolumeView`, `BodyView`, `StarView`, `ColonyView`, `ShipView`,
  `GeoSurveyView`, `GravSurveyView`, `JumpPointView`, `CargoStorageView`, `ResearcherView`,
  `AtmosphereView`, `InfrastructureView`, `InstallationsView`, `ColonyMiningView`,
  `NavalAcademyView`, `IndustryView`, `ConstructionView`, `ColonizableView`, `MineralDepositsView`
  so far), `OwnerRelation`, `Vec3`, `ModifiedValue`/`ValueModifier`; fleet
  hierarchy: `FleetSnapshot`, `ShipSnapshot`, `OrderSnapshot`; research: `ResearchSnapshot`
  (`TechCategorySnapshot`, `TechSnapshot`, `CommanderSnapshot`/`CommanderKind`/`CommanderBonusSnapshot`).
- Writes: `GameCommand` (+ `RenameCommand`, `CreateFleetCommand`, `CreateColonyCommand`, `DisbandFleetCommand`,
  `ChangeFleetParentCommand`, `ReassignShipCommand`, `SetFlagshipCommand`, `MoveToBodyCommand`,
  `GeoSurveyCommand`, `GravSurveyCommand`, `JumpCommand`, `RefuelAtCommand`,
  `AssignScientistCommand`, `UnassignScientistCommand`, `SetResearchFundingCommand`,
  `AddTechToQueueCommand`, `RemoveTechFromQueueCommand`, `MoveTechInQueueCommand`,
  `UninstallComponentCommand`, `InstallComponentCommand`, `QueueIndustryJobCommand`,
  `ChangeIndustryJobPriorityCommand`, `CancelIndustryJobCommand`, `AddToConstructionQueueCommand`,
  `MoveConstructionJobCommand`, `RemoveConstructionJobCommand`), `CommandResult`.
- Events: `GameEventType`, `GameEventEnvelope`.
- Interfaces: `IGameServer`, `IGameClient`, `IClientGalaxy`, `IClientSystem`.

The pre-existing empty `Pulsar4X.Contracts` stub is superseded by `Pulsar4X.Api` and can be removed.

## Known gaps (to address as porting proceeds)

- ~~Command validation isn't surfaced.~~ **Resolved (phase 3):** `HandleOrder` returns a validity
  bool; `SubmitCommand` does an ownership pre-check and returns the engine's real accept/reject.
- **`EntityAdded` isn't visibility-filtered.** The engine's `EntityAdded` message carries no faction id,
  so the server forwards it to every faction and the adapter upserts the projected entity into the
  galaxy — even an entity the faction shouldn't yet see. Current consumers mask this (the colonies list
  filters `Relation == Owned`; the body list filters celestial kinds), but it must be fixed before the
  map is ported (which shows all visible entities) — the server should drop adds the faction can't see.
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
- **The FleetWindow's Standing Orders tab is still engine-backed.** The conditional-order editor
  mutates `FleetDB.StandingOrders`/`ConditionalOrder` engine objects directly (it pre-dates the
  command pipeline entirely), so it needs a serializable conditional-order contract plus
  add/remove/reorder/update commands — deliberately deferred to its own pass. The tab resolves the
  engine entity from the selected fleet id; everything else in the window is API-only.
- **Quiet DataBlob mutations need an engine message.** Some engine code mutates DataBlobs without
  raising a `MessagePublisher` message, leaving already-pushed entity views stale (same family as
  the positions gap). The fix pattern (like `FleetReorganized`): the mutating engine code publishes
  `MessageTypes.EntityChanged`, which the server's existing message map turns into a self-contained
  `EntityChanged` push. `ResearchProcessor` does this when it dequeues a lab's tech mid-tick
  (without it the lab's `ResearcherView` queue froze on the finished tech — fixed bug), and
  `SubmitCommand` additionally re-pushes the commanded entity after every accepted command. Still
  open: geo/grav survey completion (`GeoSurveyView`/`GravSurveyView` staleness — nothing is
  published there today).
- **Procedural body generation never attaches `GeoSurveyableDB`** (only the blueprint/JSON body
  paths do), so procedurally generated systems currently offer nothing to geo-survey. Engine
  inconsistency noted while porting; not an API-layer issue.
