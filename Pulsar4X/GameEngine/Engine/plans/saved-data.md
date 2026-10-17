# Saved things should be data

Status: note from 2026-10-18. Do not build it until asked.

Data and logic stay apart. The file is records. Load builds the runners and fills them from those records. A datablob is the usual shape for entity data. Game-level scalars can stay fields on `Game`. They do not need to become blobs.

`OnSetToEntity` and `OnRemovedFromEntity` already break the rule. So does `Rehydrate` on an action. Those stay known debt. This note is about the save graph, not a rewrite of movement.

## Pull the data off the runners

`MasterTimePulse` and `ManagerSubPulse` are the simulation loop, and they are also what the file writes. Load already builds a new `ProcessorManager` and a new order handler. These two can follow that pattern.

The clock's data is the global date, the tick length, the tick frequency, and `EntityDictionary` (a pending jump: a time, a system, an entity). `LastProcessingTime` and `LastSubtickTime` are diagnostics. The running task and the fault report are already left out.

The subpulse's data is the system date, the hotloop next-run map, the frequency multiplier, and the instance queue (a time, a processor name, an entity). `PostLoadInit` already builds the runner and already leaves that queue alone. The saved type should be the schedule record, not the runner.

## Stop the accidental writes

`EntityManager` is not opt-in, so `GlobalManager`'s public `Random RNG` is in the file. The seed is the data. Load already makes a new `Random` from it. Make the manager opt-in, or ignore `RNG`. A `StarSystem` is already opt-in, so a system's inherited `RNG` is not written.

`GameSettings` is the other side of the same hole. The live object is a `NewGameSettings`, the class saves every public member, and one-time generation flags and both passwords ride along with the rules. Make it opt-in. Keep the running rules: threads, Newton flags, theme, seeds, start date, halt-on-events. Leave the generation request out of the file.

## Take the generator out of the file

`GalaxyFactory` is a function. Its settings are already on `Game` as `SystemGenSettings`. `SystemIndexes` is never written. The factory does not need to be saved.

## One copy

A sensor contact already lives on `FactionInfoDB`. The manager's dictionary is a second pointer at the same objects, and `SystemSensorContacts.Changes` is a live message queue. Save the faction copy. Rebuild the manager index on load. `SensorContact`'s constructor subscribes to entity-removed. That belongs with the other load-time wiring.

`StartingGameData` is the mod catalog, and `Game` also stores its own themes, gases, tech categories, and system-gen settings. One catalog is enough. Saving every blueprint is a later, larger step: the file would keep the mod manifest and player-authored designs, and load would read the rest back from the mod.

`StarSystem.NameDB` is a datablob sitting on the system instead of in the store. Put it in the store so a blob has one home.

## Leave these as data on the game record

The name, the git hashes, `LocalHumanFactionId`, the faction mask index, and the two id counters are scalars. The entity shell (`Id`, `IsValid`, `FactionOwnerID`) is the identity the blobs hang from. `Player` is already opt-in. None of these need to become a datablob.

## Do not start here

The hooks that do real work are the orbit and burn blobs: attaching one strips the others and can schedule a sphere-of-influence exit. `LogiBaseDB`'s override is empty. Moving those rules onto the processors is the aligned shape, and it is a behavior change in the movement code. Actions already live inside `ActionQueueDB`. `Rehydrate` is the load-time function, and that is a reasonable place for it.

Order when this is picked up: opt-in the leaks, then the clock and subpulse records, then drop `GalaxyFactory`, then one copy of contacts and the catalog, then `NameDB` into the store. The orbit hooks stay where they are until a movement change needs them.
