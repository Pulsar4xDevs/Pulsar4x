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
   The **FleetWindow** is fully ported: selection is by fleet id
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
   the API `RenameCommand` by entity id (fully id/name-based; the `SetEntity` engine shim is gone —
   its last caller, the entity context menu, now passes id + name).
   The **ResearchWindow** is fully ported. Labs are entities with a `ResearcherView` (design,
   location, scientist, cost/points-per-day as `ModifiedValue` breakdowns for tooltips, funding,
   tech-queue ids) — projected only for the owning faction, so lab internals never leak. Faction
   research state travels as a `ResearchSnapshot` (`ResearchChanged` push on connect + each clock
   advance): tech categories, every unlocked tech (`TechSnapshot` with progress, researchability,
   next-level unlock names pre-resolved server-side), and the faction's scientists
   (`CommanderSnapshot` incl. bonuses with resolved filter names, consumed by a snapshot-based
   `DisplayHelpers.PeopleChooser` overload — now the only one; the engine-backed overload left with
   the AdminWindow). Because the research instant orders mutate DataBlobs without raising messages,
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
   The **EntityWindow** (the per-entity popup, previously the most engine-coupled window) is ported:
   it's constructed from (entity id, system id) and re-resolves its `EntitySnapshot` each frame,
   closing itself if the entity leaves the faction's view. New/extended views: `ShipView` carries
   crew/commander plus component-health aggregates and armor (owner-only — non-owners get the
   name-only view), `ThrustView` (with **max-ΔV pre-computed server-side**, removing the client's
   rocket-equation math), `WarpAbilityView`, `WarpMovingView`, `OrdersView` (per-entity order queue
   with `Details` and an `IsEditableManeuver` flag), `GeoSurveyView` point counts,
   `GravSurveyView` progress, `MineralDepositsView` upgraded from marker to masked deposit rows
   (amounts pre-obscured server-side at partial access), `InfrastructureView.HasInstalledInfrastructure`,
   and `StarView.LuminosityClassDescription`. Shared snapshot displays now cover population, mineral
   deposits, the mining table (`ColonyMiningDisplay`) and the gravitational-anomaly panel. Two
   deferred engine bridges remain: the camera-pin button (rendering still tracks live entities) and
   the maneuver-edit click (the maneuver panel still edits live `NewtonThrustCommand`s).
   The **ComponentDesignWindow** is ported with a *client-side designer, server-validated create*.
   The interactive designer re-evaluates moddable formulas on every slider drag — far too chatty for
   a per-input server round-trip (a server-evaluated variant was tried first and the echo latency was
   unacceptable even in-process) — so the engine `ComponentDesigner` keeps running in the client,
   exactly as before the port, with the server involved only at the boundary: (a) the
   template/design *lists* travel as a `ComponentDesignsSnapshot` (`ComponentDesignsChanged`, pushed
   on connect and after a create; each design carries the `DesignerInput`s it was created with so
   "edit from existing" replays them); (b) the single write is `CreateComponentDesignCommand`
   (faction-targeted) carrying the designer's player-set state as `DesignerInput`s — the shared
   engine helper `DesignerInputs` (Extract/Apply/Build) converts between a live designer and that
   serializable form on both ends, and the translator replays the inputs onto a fresh designer to
   validate (bounds clamp, formulas evaluate, bad mod data rejects rather than crashes) before
   calling `CreateDesign`, then re-pushes designs *and* research (a new design registers a research
   project). The designer's *data* dependencies (the faction's unlocked `FactionDataStore`, tech
   levels, missile designs) reach the UI through `IDesignDataProvider` on the adapter — in-process a
   zero-copy handoff from `EngineGameServer.GetFactionDesignData` (deliberately not on `IGameServer`,
   which stays engine-type-free); a network adapter implements the same interface from state synced
   on connect (`FactionInfoDB`/`FactionTechDB` are already save-serializable, and the client holds
   the same mod data by design — `ConnectRequest.ModManifestHash` enforces the match). One subtlety
   `DesignerInputs.Extract` encodes: the upper bound of a range-slider pair is `GuiHint.None` like
   untouchable bookkeeping properties (attribute-constructor args), but *is* player-set — it's
   included by pairing, while other `None` properties are excluded (replaying them would overwrite
   their formulas with constants).
   The **ShipDesignWindow** follows the same client-side-designer shape. All the live stat math
   (damage profile, ΔV, warp, cargo/fuel aggregation) keeps running in the client against
   `IDesignDataProvider` data (it also reads the component/ship-design and armor lists straight from
   the provider's `FactionInfoDB` — no new read snapshot). The writes — previously direct faction-data
   mutations — are now commands: `SaveShipDesignCommand` (create when `DesignId` is null, else
   update **in place**, preserving the design id industry references; the server resolves
   component/armor ids against the faction's own designs, recalculates via `Initialise`, and computes
   `IsValid` server-side — mass + thrust + energy gen/store, false when obsolete),
   `DeleteShipDesignCommand`, and `SetShipDesignObsoleteCommand`. A new design stays a local working
   copy until first save (the server assigns the id; the client re-selects it by name). This port
   also fixes pre-port quirks: the edit flow used `ShipDesign.Clone` (which generates a fresh id) and
   re-registered edits as *new* designs, leaving the selection highlight broken and the version-bump
   branch unreachable — selection now tracks the original id and saves update in place.
   The **CommanderWindow** was redesigned as part of its port: it's now the corporation's personnel
   roster (every commander in a sortable table, click a row for a details pane — type, rank,
   commissioned/promoted dates, experience, posting, bonuses). It reads `Galaxy.Commanders` — the
   existing `CommanderSnapshot` extended with `Rank`/`RankName` (theme rank titles resolved
   server-side; navy track only today), `RankedOn`, and `AssignmentName` (the posting's display
   name: `AssignedTo` covers labs/admin posts, while ship command lives on `ShipInfoDB.CommanderID`,
   so the projector reverse-maps it from the faction's fleet tree). The roster is pushed via
   `CommandersChanged` on connect, each clock advance (experience/service time accrue), and after
   every accepted command (assignment orders mutate commanders with no engine message). Read-only —
   assignment stays with the owning windows' choosers. This port retired the old `AdminWindow`
   (which the toolbar's "Commanders" button actually opened) along with its colony-hex-map button
   and the `ColonyHexMapWindow` + the engine-backed `PeopleChooser` overload, all of which it was
   the last user of; the toolbar/hotkey now open the new window.
   **Cargo transfer** is ported around the existing `CargoStorageView` read surface plus one write:
   `TransferCargoCommand` (commanded entity = the source; items as `CargoItemView.Id` + units, which
   the translator resolves against the source's stores — covering entity-specific cargoables like
   component instances — and hands to the engine's paired two-sided `CargoTransferOrder`, now
   returning the dispatch result). The `CreateTransferWindow` reads partner candidates from the
   system snapshot (`CargoStorageView` presence, which is owner-only) and both sides' stores from
   the snapshots, re-resolved per frame; selection state is just ids + chosen unit counts. It
   replaces the old map-click `CargoTransferWindow` everywhere (context menu, selector, names map),
   which is deleted along with its `CargoListPanelComplex` and the dead `EntityDisplay` helper.
   Two old-window niceties did not carry over: the live ΔV-difference/transfer-rate readout and the
   transfer-then-install-on-partner button (self-install remains via `InstallComponentCommand`).
   The **FireControl window** is ported. New owner-only `FireControlView`: the entity's fire
   controls (`FireControlSnapshot` — target, engagement state, assigned weapon ids), all its
   weapons (`WeaponSnapshot` — fire-control assignment, magazine fill, loaded ordnance with the
   cargo count resolved server-side), and the loadable ordnance in its cargo (`OrdnanceStoreItem`).
   Commands: `SetFireControlWeaponsCommand` (replaces a fire control's whole weapon set — the
   client computes add/remove against the snapshot), `SetFireControlTargetCommand`,
   `AssignOrdnanceCommand`, `SetFireModeCommand` (open/cease). The engine's four fire-control order
   factories now return the `HandleOrder` bool; `SetOrdinanceToWpnOrder.CreateCommand` also lost an
   unconditional NRE (it dereferenced the not-yet-resolved `EntityCommanding` to find the order
   handler) and a `KeyNotFound` on bad ordnance ids in validation. Target candidates come from the
   system snapshot (`Relation == Hostile`, plus own/friendly behind the "show own" toggle); the
   post-command `EntityChanged` push refreshes the view immediately.
   The **OrdersListWindow** (per-entity order queue) is ported: `OrderSnapshot` now carries
   `OrderId`, the action-lane flags + `IsBlocking`, and `PauseOnAction`; the pause checkbox submits
   `SetOrderPauseCommand` (the flag has no engine order of its own — the pre-port UI flipped it by
   reference — so the translator sets it directly on the queued order). The window resolves its
   entity snapshot each frame and closes itself when the entity leaves the faction's view.
   **Positions** are now client-propagated: `OrbitView` carries the full Keplerian element set and
   the client `SnapshotOrbits` helper computes positions per frame from it (see the resolved gap
   below) — the read foundation for the map and the movement-order windows.
   The **system-map and galaxy-map rendering** are ported. The pivot is `SnapshotPosition`, an
   `IPosition` whose every read resolves the entity's current snapshot through `IClientGalaxy` and
   propagates its orbit to the galaxy clock (with a same-snapshot/same-tick memo) — icons keep
   consuming the `IPosition` seam they always had, so they hold no game data, only derived geometry
   built from snapshot scalars at construction. `SystemMapRendering` holds a system *id* and
   reconciles its icon set against the system's snapshots each frame: snapshots are immutable, so a
   reference change is the rebuild signal (the only retained per-entity state is that sync key); all
   `SystemState`/`EntityState`/`MessagePublisher`/sensor subscriptions are gone from the render
   path. Ported icons take snapshot constructors: `OrbitEllipseIcon` (from `OrbitView` elements),
   `StarIcon` (`StarView.SpectralTypeIndex` added), `SysBodyIcon` (by `BodyKind`), `ShipIcon`,
   `WarpMovingIcon` (`WarpMovingView` extended with entry/exit points + target id),
   `PointOfInterestIcon` (`GravSurveyView` marker). The label family (`EntityLabel`/ExtCombo/
   distributor) is rewritten snapshot-based — name, relation colour and body kind from the views, no
   rename subscriptions (rebuild covers it). `GalacticMapRender` lazily syncs per-system maps and
   gal-map star icons from `Galaxy.KnownSystems` (unknown systems no longer appear as "??" markers —
   the client simply doesn't have them). Camera pinning is id-based (`PinToEntity(id, systemId,
   state)` via `SnapshotPosition`), retiring the EntityWindow/context-menu camera-pin engine bridge.
   The initially deferred pieces are ported too: `NewtonMoveView` (SOI parent + radius, current
   vector, maneuver ΔV, thrust, and the current trajectory as an embedded `OrbitView`) and
   `NewtonSimpleMoveView` drive the newtonian-trajectory icons — those icons rebuild on each
   per-tick mover push, so their snapshot constructors carry no engine state and their physics
   pass just re-anchors the trail; `OrbitView.ParentSoiRadiusM` enables hyperbolic orbit rings
   (`OrbitHyperbolicIcon2`); `ProjectileView`/`BeamView` (endpoints re-pushed per tick — beams and
   simple movers joined the per-tick mover refresh) drive the combat icons; and the label hover
   panels are snapshot-based (`Displays.Ship` from `ThrustView` ΔV/max-ΔV, `Displays.SystemBody`
   from Body/Atmosphere/GeoSurvey/Colonizable/MineralDeposits views + colony lookup,
   `Displays.Star` from `StarView`).
   The **ComponentsWindow** (the read-only component-library browser) is ported with zero new API
   surface: everything it shows — the faction's unlocked `ComponentTemplates`, its
   `ComponentDesigns`, cargo-good names, and the `ComponentDesigner` it constructs to evaluate a
   template's default stats — already travels through the designer windows' `IDesignDataProvider`
   bridge, so the port just swaps the live-faction `GetDataBlob` reads for the provider (with a
   graceful "design data is not available" bail). No commands; same network-play caveat as the
   other provider-bridged windows.
   The **movement-order cluster** (the map-coupled windows the system-map port unblocked) is ported.
   The principle throughout: the interactive maneuver math — intercepts, insertion orbits, transfer
   previews, patched-conics prediction — runs client-side over snapshot elements (`SnapshotMoves`,
   the movement counterpart of `SnapshotOrbits`, plus `OrbitalMath` gaining the pure functions
   `IntegrateOneStep`/`OrbitPhasingManuvers` that used to live in the engine), while the submitted
   command carries only the player's intent and the server recomputes what matters. Three commands
   cover the whole cluster: `NewtonThrustCommand` (node time + orbit-relative ΔV; the server
   computes burn duration from its own fuel state and rejects burns beyond the ship's ΔV),
   `WarpMoveCommand` (destination + optional insertion point — destination visibility enforced at
   the boundary; without an insertion point the server plots the default low-orbit arrival, and
   either way it recomputes the intercept and post-warp orbit, so the window's elaborate orbit
   shaping is preview-only), and `CancelOrderCommand` (remove a queued, not-yet-running order — like
   `SetOrderPause`, a direct translator mutation since no engine order exists for it). Ported
   windows: **ChangeCurrentOrbitWindow** and **WarpOrderWindow** (their `OrbitOrderIcon`/
   `WarpMoveOrderWidget` map widgets now take `IPosition` + scalars / snapshot ids), and
   **NavWindow**, keeping its five functional modes (manual thrust planning, Hohmann, interplanetary
   Hohmann, phase change, escape SOI) and dropping the dead ones (Hohmann2/OE — their
   `NewtonSimpleCommand`/`NavSequenceCommand` dispatch was already commented out in the engine —
   plus the Phasing/High-ΔV/Porkchop stubs). The **maneuver-node UI** (`ManuverNode`, the maneuver
   lines, `ManeuverNodePanel`, and the orbit-click orchestration in `GlobalUIState`) is snapshot-
   based: nodes capture burn scalars from the snapshot, encounter/patched-conics prediction
   enumerates the SOI parent's faction-visible children, orbit-line hit-testing resolves icons by
   entity id from `SystemMapRendering` (this had silently broken in the map port, which stopped
   populating `EntityState.OrbitIcon`), and editing an existing maneuver works from
   `OrderSnapshot.ManeuverNodeTime`/`ManeuverDeltaVMps` with commit = cancel + resubmit — retiring
   the EntityWindow's last engine bridge. `GameInfo` now carries the `StrictNewtonian`/
   `UseRelativeVelocity` movement-rule settings the warp window adapts its inputs to. The
   **OrderCreationWindow** (a non-functional prototype whose action button did nothing) was retired
   rather than ported.
   The **DistanceRuler** needed no API surface at all — it is pure camera/screen math; its only
   engine dependency was `Stringify`, the unit-formatting helper every window uses. `Stringify`
   moved into `Pulsar4X.Api` (dropping its one dead engine-typed method), since presenting
   contract values as display strings is a client-facing concern both sides of the boundary share
   (engine order `Details` strings use it too). That clears the most widespread transitional
   `using Pulsar4X.Engine` ahead of the phase-6 reference cut; call sites that don't otherwise
   import `Pulsar4X.Api` reach it through a `using Stringify = Pulsar4X.Api.Stringify;` alias,
   which can't collide with same-named engine command types the way a namespace import could.
   The **FleetWindow's Standing Orders tab** (the conditional-order editor, the window's last
   engine-backed piece) is ported. The contract: `StandingOrder` (name + `StandingOrderCondition`s
   with comparison/threshold/And-Or chaining + action ids), serializable so it doubles as the read
   model on `FleetSnapshot.StandingOrders` and the payload of the single write,
   `SetStandingOrdersCommand` — the editor works on a client-side copy and Save replaces the
   fleet's whole list in one validated command (validate-then-swap, so a rejected list leaves the
   existing orders untouched). The condition/action registries are engine code rather than mod
   data, so their ids are part of the contract (`StandingOrderTypes`): the client builds its
   pick-lists from them (replacing the engine-typed `OrderRegistry`, now deleted), the translator
   maps them back to engine conditions/actions and the projector the other way. Like research and
   cancel-order, replacing the list raises no engine message, so the server re-pushes the fleet
   tree after an accepted set.
   The **launcher/cleanup sweep** closed out the read-surface phase: the entity-action gating
   (`EntityUIWindows` + the Actions panel + the context menu) now gates on snapshot views instead
   of `HasDataBlob<>` checks — which also makes the buttons faction-scoped for free, since
   owner-only views simply aren't projected for entities the faction doesn't own. `OrdersView` is
   now projected even when the queue is empty (its presence marks the entity orderable), and
   `GravSurveyView` gained `JumpPointToSystemId` (revealed on survey completion) so the
   "Go to system" action works from the snapshot. The **PowerGenWindow** — which turned out to
   have been unreachable (its open-dispatch branch was missing) — was ported onto a new owner-only
   `EnergyView` (load/output/demand, stored energy, and the plot histogram unrolled from the
   engine's ring buffer; energy entities re-push each clock advance since generation runs on
   engine-scheduled interrupts with no message) and re-wired, also dropping the old window's habit
   of running the energy processor client-side to animate its plot. Dead weight deleted: the
   engine-backed `IndustryDisplay`/`ConstructionDisplay`/`IndustryPanel`/`PowerDBDisplay`/
   `StarInfoDBDisplay`/`CargoListPanelSimple`/`SmallBodyEntityInfoWindow` (orphaned by earlier
   ports — the files holding both engine and snapshot display extensions stay, snapshot halves in
   use), the icons' engine-entity constructors and their `MessagePublisher` subscriptions
   (`OrbitIconBase`/`OrbitEllipseIcon`/`OrbitHyperbolicIcon2`/`ShipIcon`/`ProjectileIcon`/
   `NewtonMoveIcon`/`NewtonSimpleIcon`; `OrbitHypobolicIcon` deleted outright), and the
   never-implemented jump-through-jump-point launcher path.
5. **Events (done):** the `MessagePublisher` sync-state messages were bridged in phase 2; this
   phase bridged the `EventManager` game-log stream. A `LogEvent` is display-ready — the event
   type travels as its engine name (a string, so the 200-value engine enum isn't mirrored into
   the contract) and entity/faction names are resolved server-side with the subscriber's faction
   scope. Each subscription bridges `EventManager` with the same faction filter as the engine's
   own `FactionEventLog` (addressed-to or concerned); on subscribe the faction's persisted log is
   pushed as a backlog envelope (the live bridge starts after, so nothing double-delivers), then
   singles stream as they happen. The client galaxy accumulates them on
   `IClientGalaxy.EventLog`, and the **GameLogWindow** is ported to read it — no engine usings
   left; its per-type hide filter keys off the event-type string. Not carried over:
   `FactionEventLog.HaltsOn` (pause-on-event) has no UI today; when one is built it should be a
   small command + a flag on `LogEvent`.
6. **Client composition (`Pulsar4X.Client.Host`) — done.** The extraction is complete:
   `Pulsar4X.Client.Host` is the desktop executable (entry point, app icon) and `Pulsar4X.Client`
   is a library. The host owns the engine-backed development tooling — the `Debug/` windows,
   `SMWindow`, `EntitySpawnWindow` and `DamageViewerWindow` moved into its `DevTools/` — and wires
   it through a registry on `GlobalUIState` (`DevToolRegistration`: key, label, toggle,
   active-check, placement): the library's settings list, toolbar, main menu ("SM Mode") and
   hotkeys render whatever the composition root registered without referencing the tools, and an
   `OnGameLoaded` event replaces the direct `DebugWindow.SetGameEvents()` calls from the new/load
   flows (the orbit-debug toggle's availability check became snapshot-based in the process). The
   library is `InternalsVisibleTo` the host.
   The **game-lifecycle seam** is in: `IGameLifecycle` (in the UI library, engine-free) is
   implemented by the host's `GameLifecycle` and assigned to `GlobalUIState.Lifecycle` at startup.
   It owns mod scanning (`ModsState` moved to the host), mod loading, and the whole
   create/quickstart/load/save flows — factories, faction/species/colony setup. The UI side is
   data-driven: the new-game wizard builds its pick-lists from `NewGameCatalog`/`ModOption` DTOs
   and submits a `NewGameRequest`; `GlobalUIState.ActivateGameUI` finishes up (select system,
   point camera, open default windows). `NewGameMenu`/`LoadGame`/`SaveGame` have no engine usings
   left, and the main loop's game-tick detection reads the galaxy clock instead of `Game.TimePulse`.
   **The `GameEngine` reference is dropped from `Pulsar4X.Client`** — the library references only
   `Pulsar4X.Api` and `Pulsar4X.Orbital`. What that took:
   - **Session binding flipped.** `GlobalUIState.Game`/`Faction`/`PlayerFaction`/`SetFaction` are
     gone. The host's `GameLifecycle` owns the engine `Game`, one `EngineGameServer` per game
     (`SetGame`), and the player-faction entity; `BindFaction` connects an `InProcessAdapter`
     session and hands it to `GlobalUIState.OnGameClientBound(client, gameInfo)` (which disconnects
     the previous client, rewires `EventReceived`, and raises `OnFactionChanged`). The session's
     faction is `GlobalUIState.FactionId` (from the connect handshake); game-master mode goes
     through `IGameLifecycle.SetGameMasterMode` (the host rebinds to `Game.GameMasterFaction` or
     the remembered player faction). The settings window's Game tab edits engine processing rules
     through a `GameRules` record (`GetGameRules`/`ApplyGameRules` on the lifecycle).
   - **`SystemState` deleted, `EntityState` slimmed.** The click pipeline is id-based end to end:
     `EntityClicked(id, systemId, button)` resolves the `EntitySnapshot` from the galaxy and builds
     a four-field `EntityState` (id, system id, name, display body-type). Per-system camera saves
     live in a `GlobalUIState` dictionary keyed by system id; `SetActiveSystem` is engine-free and
     tells the server which system to prioritise via the new `IGameServer.SetSystemFocus`
     (replacing the engine `Increment/DecrementExternalObserver` calls; the server maps focus to
     observer promotion, and clears it on disconnect).
   - **Client-local `IPosition`.** The icons/camera/widgets seam (`Pulsar4X.Client.IPosition`,
     implemented by `SnapshotPosition`/`StaticPosition`) replaced `Pulsar4X.Interfaces.IPosition`;
     the icons' dead engine constructors (`PositionDB`/DataBlob overloads) were deleted, and the
     engine's `OrbitMath` calls became base-class `OrbitalMath`.
   - **`IDesignDataProvider` relocated.** The designer windows (`ComponentDesignWindow`,
     `ShipDesignWindow`, `ComponentsWindow`, design displays) moved to the host's `Designer/` —
     they evaluate engine `ComponentDesigner` client-side by design, so they're composition-root
     code now. The provider moved off the adapter onto the host `GameLifecycle`
     (`_server.GetFactionDesignData(session)`); resolution is `Lifecycle is IDesignDataProvider`.
     `ModFileEditing/` and the parked ordnance/logistics windows moved to the host too.
   - **Registry-driven designer surfaces.** `DevToolRegistration` gained `ToolbarIcon`, `Order`
     and an `SMToolbar` placement; the toolbar merges host-registered buttons with the built-ins
     by order (designer buttons first), the "Editor" main-menu button and the F5/1/2 hotkeys go
     through `ToggleDevTool` keys, and SM-only buttons render on the SM toolbar.
   - **Displays split.** The five DB display files and `Displays.cs` kept only their
     snapshot halves; the one engine half still used (`CargoStorageDB.Display`, DebugWindow) moved
     to the host with an `Entity` parameter. Engine-typed helpers left the library
     (`EntityExtensions` → host; `EntityNameSelector`, `Utils.EntityBodyType` deleted as dead);
     the library grew its own `StringExtensions`/`EnumExtensions.ToDescription`.
   - **Host dev-tool bridge.** Dev tools reach the engine through `GameLifecycle.Instance`
     (`Game`, `Faction`, `SelectedSystem`, `SelectedSystemState` — a thin host `SystemState`
     wrapping the engine system) and `EngineUiBridge` (`EntityState.GetEntity()` resolves the
     engine entity behind a click; `PositionDBAdapter` adapts engine positions to the UI seam;
     `RawBmpTextures` uploads engine `RawBmp`s).
7. **Network adapter + server host:** transport + serialization for `MultiplayerAdapter` and the
   headless `Pulsar4X.Server.Host`.

## Contract surface (initial)

- Identity: `PlayerSession`, `ConnectRequest`, `ConnectResult`, `GameInfo`.
- Time: `TimeState`, `TimeControlRequest`.
- Reads: `SystemSummary`, `SystemSnapshot`, `EntitySnapshot` + `IComponentView` (`NameView`,
  `PositionView`, `OrbitView`, `MassVolumeView`, `BodyView`, `StarView`, `ColonyView`, `ShipView`,
  `GeoSurveyView`, `GravSurveyView`, `JumpPointView`, `CargoStorageView`, `ResearcherView`,
  `AtmosphereView`, `InfrastructureView`, `InstallationsView`, `ColonyMiningView`,
  `NavalAcademyView`, `IndustryView`, `ConstructionView`, `ColonizableView`, `MineralDepositsView`,
  `FireControlView` (+ `FireControlSnapshot`, `WeaponSnapshot`, `OrdnanceStoreItem`),
  `EnergyView` (+ `EnergyHistogramPoint`)
  so far), `OwnerRelation`, `Vec3`, `ModifiedValue`/`ValueModifier`; fleet
  hierarchy: `FleetSnapshot`, `ShipSnapshot`, `OrderSnapshot`; research: `ResearchSnapshot`
  (`TechCategorySnapshot`, `TechSnapshot`, `CommanderSnapshot`/`CommanderKind`/`CommanderBonusSnapshot`
  — `CommanderSnapshot` doubles as the personnel roster, pushed faction-wide via `CommandersChanged`);
  component design: `ComponentDesignsSnapshot` (`ComponentTemplateSummary`, `ComponentDesignSummary`)
  and `DesignerInput` (the serializable form of a designer's player-set state).
- Writes: `GameCommand` (+ `RenameCommand`, `CreateFleetCommand`, `CreateColonyCommand`, `DisbandFleetCommand`,
  `SetStandingOrdersCommand` (+ `StandingOrder`, `StandingOrderCondition`, `StandingOrderTypes`),
  `CreateComponentDesignCommand`, `SaveShipDesignCommand` (+ `ShipComponentCount`),
  `DeleteShipDesignCommand`, `SetShipDesignObsoleteCommand`,
  `ChangeFleetParentCommand`, `ReassignShipCommand`, `SetFlagshipCommand`, `MoveToBodyCommand`,
  `GeoSurveyCommand`, `GravSurveyCommand`, `JumpCommand`, `RefuelAtCommand`,
  `AssignScientistCommand`, `UnassignScientistCommand`, `SetResearchFundingCommand`,
  `AddTechToQueueCommand`, `RemoveTechFromQueueCommand`, `MoveTechInQueueCommand`,
  `TransferCargoCommand` (+ `CargoTransferItem`),
  `SetFireControlWeaponsCommand`, `SetFireControlTargetCommand`, `AssignOrdnanceCommand`,
  `SetFireModeCommand`, `SetOrderPauseCommand`, `CancelOrderCommand`,
  `NewtonThrustCommand`, `WarpMoveCommand`,
  `UninstallComponentCommand`, `InstallComponentCommand`, `QueueIndustryJobCommand`,
  `ChangeIndustryJobPriorityCommand`, `CancelIndustryJobCommand`, `AddToConstructionQueueCommand`,
  `MoveConstructionJobCommand`, `RemoveConstructionJobCommand`), `CommandResult`.
- Events: `GameEventType`, `GameEventEnvelope`, `LogEvent` (the faction's game log, on
  `IClientGalaxy.EventLog`).
- Interfaces: `IGameServer`, `IGameClient`, `IClientGalaxy`, `IClientSystem`.

The pre-existing empty `Pulsar4X.Contracts` stub is superseded by `Pulsar4X.Api` and can be removed.

## Known gaps (to address as porting proceeds)

- ~~Command validation isn't surfaced.~~ **Resolved (phase 3):** `HandleOrder` returns a validity
  bool; `SubmitCommand` does an ownership pre-check and returns the engine's real accept/reject.
- ~~`EntityAdded` isn't visibility-filtered.~~ **Resolved (with the map port):** the server's event
  bridge now drops entity add/change/rename/reveal pushes for entities the subscribing faction can't
  see (`EntityManager.IsEntityVisibleToFaction`, the per-entity form of `GetFilteredEntities`) — and
  for entities that can't be resolved at all (mid-construction `DBAdded` messages used to leak
  id-only envelopes). Fixing this surfaced a worse bug: `Connect()`'s naive first-faction fallback
  was binding every session to the **GameMaster** faction (created first, sees everything); it now
  binds to the first player faction.
- **Continuous state (positions): client-side orbit propagation — foundation done.** Decision:
  Keplerian movement is propagated client-side, not streamed. `OrbitView` carries the full element
  set (metres/radians + epoch + μ), `SnapshotOrbits` (client) rebuilds `KeplerElements` and computes
  relative/absolute positions per frame via the shared `Pulsar4X.OrbitalMath`, and the server
  re-pushes only the *non-Keplerian* movers (entities with `WarpMovingDB`/`NewtonMoveDB`) each clock
  advance so their `PositionView` is at most a tick old. The element round-trip is verified against
  engine positions in `ApiOrbitPropagationTests`. Remaining: the map itself (and the map-coupled
  order windows below) consume this when ported.
- ~~The remaining movement-order windows are map work, not window work.~~ **Resolved (with the
  movement-order port):** `WarpOrderWindow`, `NavWindow`, `ChangeCurrentOrbitWindow`,
  `ManeuverNodePanel` and their map widgets are snapshot-based; the
  `WarpMoveCommand`/`NewtonThrustCommand`/`CancelOrderCommand` DTOs landed (see phase 4).
- **Faction selection on connect is naive** — binds to the first faction. Real selection/auth via
  `ConnectRequest.Credential` lands with networking.
- ~~The in-process adapter lives in `Pulsar4X.Api`.~~ **Resolved:** `InProcessAdapter` and the
  `ClientGalaxy`/`ClientSystem` model now live in `Pulsar4X.Client/Api/` (namespace `Pulsar4X.Client`),
  which references `Pulsar4X.Api` directly. The contracts assembly holds only interfaces + DTOs, and
  `Pulsar4X.Tests` drives `EngineGameServer` through `IGameServer` directly (no UI dependency).
- **Two `PositionDB` classes exist** (`Pulsar4X.Datablobs` legacy/excluded vs the live
  `Pulsar4X.Movement`); projection uses the live one. Worth cleaning up the dead copy separately.
- ~~The FleetWindow's Standing Orders tab is still engine-backed.~~ **Resolved:** the
  conditional-order contract (`StandingOrder`/`StandingOrderTypes`) and the whole-list
  `SetStandingOrdersCommand` landed; the editor is client-side with a single validated write
  (see phase 4).
- **Quiet DataBlob mutations need an engine message.** Some engine code mutates DataBlobs without
  raising a `MessagePublisher` message, leaving already-pushed entity views stale (same family as
  the positions gap). The fix pattern (like `FleetReorganized`): the mutating engine code publishes
  `MessageTypes.EntityChanged`, which the server's existing message map turns into a self-contained
  `EntityChanged` push. `ResearchProcessor` does this when it dequeues a lab's tech mid-tick
  (without it the lab's `ResearcherView` queue froze on the finished tech — fixed bug), and
  `SubmitCommand` additionally re-pushes the commanded entity after every accepted command. Still
  open: geo/grav survey completion (`GeoSurveyView`/`GravSurveyView` staleness — nothing is
  published there today).
- **The component designer's data is an in-process bridge.** The client-side designer evaluates
  against the faction's live `FactionInfoDB`/`FactionTechDB`, handed over zero-copy via
  `IDesignDataProvider` (the in-process adapter downcasts to `EngineGameServer`). For network play
  the `MultiplayerAdapter` must implement the same interface from replicas synced on connect — both
  DataBlobs are already save-serializable — and refresh them when techs level (the design-relevant
  changes already arrive as `ResearchChanged`/`ComponentDesignsChanged` pushes to use as triggers).
- **SM/debug tooling stays engine-backed by design.** `SMWindow`, `EntitySpawnWindow`, the
  `Debug/` windows (DebugWindow, EntityInspector, OrbitalDebugWindow, SensorDraw, DataViewerWindow,
  PerformanceWindow, GraphicDebugWidget, BlueprintsWindow, DebugGUIWindow) and `DamageViewerWindow`
  (a damage-sim test sandbox that fires synthetic projectiles and pokes component health) are
  development tools, not player UI. They will not get a faction-scoped API surface; **as of the
  phase-6 extraction they live in `Pulsar4X.Client.Host/DevTools/`** (which references the engine)
  and reach the UI through the dev-tool registry. The player-facing slice of what DamageViewer
  shows — component health, armor — already travels via `ShipView`/`InstallationsView`.
- **Procedural body generation never attaches `GeoSurveyableDB`** (only the blueprint/JSON body
  paths do), so procedurally generated systems currently offer nothing to geo-survey. Engine
  inconsistency noted while porting; not an API-layer issue.
