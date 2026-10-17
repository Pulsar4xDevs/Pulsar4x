# A save comes back as the same game

Status: implemented 2026-10-17. Branch `save-load`. `SaveRoundTripTests.EarthColony_RoundTripsTheSavedGame` passes, including one step after load. Plan 2 is implemented in `02-work-in-progress.md`.

A save written from the save window loads back as the same simulation. The host then binds the human faction, and the client receives that faction's slice. The file itself stays the full game.

## Decided

- One file, one `Game`. `Game.Save` keeps the current Newtonsoft settings: indented, `PreserveReferencesHandling.Objects`, `TypeNameHandling.Objects`, `NonPublicResolver`. Do not save the client galaxy.
- `LocalHumanFactionId` is an `int` on `Game`, default `-1`. `CreateGameCore` sets it to the faction it just created with `CreateBasicFaction`. A future server ignores the field when a connection names its own faction. The field does not decide what is serialized and it does not decide what a snapshot includes.
- `GameLifecycle.LoadGame` binds that id with `setAsPlayer: true` and does not send the space-master credential. A missing id, an unknown id, or a faction with no known system fails the load with a message and leaves the menu up. There is no fallback to `Factions.Last()`, to the first non-master faction, or to the game master.
- The activation is that faction's first `KnownSystems` entry and nothing else. `SetActiveSystem` already centers a first visit on the star. `ClearGameState` already clears the camera cache and turns the master toggle off.
- Load does not call `PostNewGameInitialization`. Jump links, the one-shot energy tick, and the one-shot sensor tick stay as they were saved.
- After the managers are wired, call `UpdateActivityState` on every system. `ActivityState` is not saved and comes back as `Stasis`. Faction systems have to leave stasis or their industry never runs. A system that was already behind still uses `CatchUpFromStasis`.
- A save waits until the current step has finished. `SaveGame` calls `PauseTime` when `TimePulse.IsRunning` is true, waits until that task has finished, then writes. The clock stays paused.

## What is already true

This section is the note from before the work. The load throw it describes is fixed.

`Game.Save` on an Earth colony in Sol writes about 6.2 MB. `Game.Load` then throws:

`Unable to find a constructor to use for type Pulsar4X.Industry.AsteroidMinerAtb`

That attribute has an `int` constructor and a `double` constructor and no parameterless one. `IndustryAtb` already has a private `[JsonConstructor]`. `GenericWeaponAtb` already has a private parameterless constructor. `NavalAcademyAtb` has three constructors and no parameterless one. It was not in that Earth file. A second null-reference showed up only when the constructor error was swallowed. Check it again after the constructor throw is gone.

The star system's `_instanceProcessorsQueue` is in the file (16 entries on that colony). The global manager's queue is empty. `HotLoopProcessorsNextRun` is in the file. `TimeQueue` round-trips when serialized on its own.

`Game.Load` then calls `EntityManager.Initialize` with `postLoad: false`, and calls `ManagerSubPulse.Initialize` a second time. `Initialize` does `new Random(seed)` with the argument, which is `-1` on that call. The `postLoad: true` branch and `PostLoadInit` exist and are unused. `PostLoadInit` does not set `_game`. `AddSystemInterupt` reads `_game`. `ManagerSubPulse.Initialize` assigns `_systemLocalDateTime` from the global date and calls `InitHotloopProcessors`.

`NextEntityID` is a get-only property over the static `EntityIDGenerator.NextId`. The number is written and never read back. `EntityIDCounterValue` is the same shape over `EntityIDCounter`. `NextFactionMaskIndex` already has a setter. Leave it.

`Connect` binds any faction id except the game master. The master id returns every entity from `GetFilteredEntities`. `PushInitialState` builds the slice from the session's faction id and that faction's `KnownSystems`.

`Game.Players` stays empty on this path. `CreatePlayerFaction` is not called. Do not create a `Player`, do not check access roles, and do not change `Connect`.

## Constructors

Give `AsteroidMinerAtb` a private parameterless constructor marked `[JsonConstructor]`, in the same shape as `IndustryAtb`. Keep both existing constructors.

Load the Earth colony again. Collect the remaining deserialization errors the same way, with an error handler that records the message and continues. Fix each type that still cannot be constructed. Delete that probe when the load returns a game. `NavalAcademyAtb` is the likely next one if a save contains it.

After that load succeeds, compare the miner design's `UnitsPerDay` with the value from before the save. A single-constructor attribute whose JSON names do not match its parameters comes back as zeroes. Mark that constructor `[JsonConstructor]` when a compared value has zeroed. Do not edit attributes that round-trip.

## Load rehydration

`Game.Load`, after a successful deserialize:

1. `SetEntities` on every manager in `GlobalManagerDictionary`. This sets `Manager` and datablob owners. It does not clear the event bus and it does not build processors.
2. The accept callback, when the caller passed one. The host reads `FactionInfoDB` here through `Entity.TryGetDataBlob`. A throw restores the id counters and does not continue.
3. `ClearGlobalState`. A rejected save has not reached this step.
4. `TimePulse.Initialize`.
5. A new `ProcessorManager` and a new `StandAloneOrderHandler`. The processors are rebuilt by type. The saved queue stores processor names. Processor `Init` subscribes on the event bus, so this stays after the clear.
6. `Initialize(game, postLoad: true)` once per manager in `GlobalManagerDictionary`. Do not call it again on `GlobalManager`. Do not call `ManagerSubPulse.Initialize`.
7. Subscribe each faction event log, including the game master, and run `FactionStanceRules.MigrateGrants`.
8. `Rehydrate` on every saved action.
9. `UpdateActivityState` on every system.

When a debugger is attached, a throw from deserialize or from accept is not caught, so the debugger stops on that line. The id counters are still restored. The load and save windows do the same: they show the exception and its stack when no debugger is attached, and they let it propagate when one is.

`EntityManager.Initialize` on the post-load path uses the saved `_rngSeed` for `new Random`. Seed 0 is a valid seed. Remove the throw that treats 0 as unset. `SetEntities` still runs, so datablob owners are wired.

`ManagerSubPulse.PostLoadInit` sets `_entityManager`, `_processManager`, and `_game`. It does not change `_systemLocalDateTime`, `_processToDateTime`, `_subStepDateTime`, `HotLoopProcessorsNextRun`, or `_instanceProcessorsQueue`.

The id properties need setters. `NextEntityID` writes `EntityIDGenerator.NextId`. `EntityIDCounterValue` writes `EntityIDCounter`. Deserialization has to run those setters. A fresh process starts both statics at 0, so the next `Entity.Create` or `GetEntityID` would otherwise reuse an id already in the file.

## Host

`GameLifecycle.SaveGame` still writes `Game.Save` of the host's `_game`, in either faction view. It pauses a running clock first, as decided above. A thrown save stays on the save window with the exception and its stack. The window does not close over a failure. When a debugger is attached, the window does not catch it.

`LoadGame` reads `LocalHumanFactionId` and binds that faction. The activation system is `KnownSystems[0]` when that id is a system in `game.Systems`. Otherwise the load fails with a message. No camera position and no zoom are set.

## Test

An Earth colony on Sol, built the way `EarthStartTests` builds one. Set `LocalHumanFactionId` to that faction. Step the clock once so the instance queue is non-empty. Save, set `EntityIDGenerator.NextId` and `EntityIDCounter` back to 0, load.

The loaded game matches the saved one on: global date, each system's `StarSysDateTime`, entity ids, datablob types per entity, `LocalHumanFactionId`, both id counters, the instance-queue count, and the processor name on each queue entry. Each queue entry's entity is the entity with that id in the manager, not a copy. The miner's `UnitsPerDay` matches. The loaded Sol system is `Background`. A new `Entity.Create` gets an id that was not in the file.

Jump-point `DestinationId` values match. `LinkAllJumpPoints` was not run again.

Keep this test. The commented body in `SavingAndLoadingTests.cs` calls the old `Game` constructor. Leave that file commented.

## Out of this plan

Action pointers, `IsRunning`, a warp in progress, and a burn in progress. Those are plan 2, implemented 2026-10-17.

Restoring the system the player was looking at, the selected entity, the camera, the open windows, or space-master mode.

A `Player`, passwords, and access checks on `Connect`.

The crash-save beside the fault dialog.

The nine failing tests named in the README.
