# Movement — Subsystem Reference

Newton physics, warp drive, inter-system jumps, pathfinding. Lives in `GameEngine/Movement/`.

---

## File Map

| Directory / File | Purpose |
|-----------------|---------|
| `NewtonMove/NewtonMoveDB.cs` | DataBlob: current velocity vector, parent body for relative position. Attached when an entity is in free-flight Newtonian motion. |
| `NewtonMove/NewtonSimpleMoveDB.cs` | Simplified version of NewtonMoveDB for certain cases. |
| `NewtonMove/NewtonThrustAbilityDB.cs` | DataBlob: the entity's propulsion capability (thrust, Isp, fuel capacity, fuel current). |
| `NewtonMove/NewtonThrustCommand.cs` | Order: apply a delta-V maneuver at a specific datetime. `CreateCommands(cargoLibrary, entity, manuvers)` builds a sequence of thrust commands. |
| `NewtonMove/NewtonSimpleCommand.cs` | Simplified thrust command. |
| `NewtonMove/NewtonionThrustAtb.cs` | Component design attribute that grants `NewtonThrustAbilityDB`. |
| `NewtonMove/NewtonianMovementProcessor.cs` | `IHotloopProcessor` (1 sec). Integrates velocity over delta-time, calls `MoveStateProcessor`. |
| `NewtonMove/NewtonSimpleProcessor.cs` | Simpler movement processor for non-thrust entities. |
| `WarpMove/WarpAbilityDB.cs` | DataBlob: warp drive capability (speed, fuel). |
| `WarpMove/WarpMovingDB.cs` | DataBlob: entity currently in warp transit (has destination, ETA). |
| `WarpMove/WarpMoveProcessor.cs` | `IHotloopProcessor`. Advances warp transit, delivers entity to destination orbit. |
| `WarpMove/WarpMoveCommand.cs` | Order: begin warp to a target orbit. |
| `WarpMove/WarpDriveAtb.cs` | Component attribute granting warp drive ability. |
| `WarpMove/WarpMath.cs` | `CalcMaxWarpSpeed()`, transit time calculations. |
| `NavSequence/NavSequenceDB.cs` | DataBlob: ordered list of waypoints/orders for an entity to execute. |
| `NavSequence/NavSequenceProcessor.cs` | `IInstanceProcessor`. Executes the next item in the nav sequence queue. |
| `NavSequence/NavSequenceCommand.cs` | Order: append or replace the nav sequence. |
| `Pathfinding/PathfindingManager.cs` | Graph-based pathfinder over the jump point network. |
| `Pathfinding/Graph.cs`, `Node.cs`, etc. | Standard A* graph data structures. |
| `InterSystemJumpProcessor.cs` | Handles entity movement through jump points between star systems. |
| `InterceptCalcs.cs` | `OrbitPhasingManuvers()`, intercept calculations used by missile guidance. |
| `MoveMath.cs` | `GetRelativeFutureVelocity()`, vector helpers. |
| `MoveState.cs`, `MoveStateProcessor.cs` | State machine for entity movement modes (orbiting / thrusting / warping). |
| `MoveToNearestAction.cs` et al. | Fleet action helpers: move to nearest colony, anomaly, geo survey target. |

---

## Movement Modes

An entity's movement mode is determined by which DataBlobs it currently holds:

| Mode | DataBlob | Processor |
|------|----------|-----------|
| Stable orbit | `OrbitDB` or `OrbitUpdateOftenDB` | `OrbitProcessor` (in `Orbits/`) |
| Free-flight (Newtonian) | `NewtonMoveDB` | `NewtonianMovementProcessor` (1 sec) |
| Warp transit | `WarpMovingDB` | `WarpMoveProcessor` |
| Idle / docked | none of the above | — |

Transitions between modes work by adding/removing DataBlobs:
- Issuing a warp order → adds `WarpMovingDB`, may remove `OrbitDB`.
- Completing warp → removes `WarpMovingDB`, adds `OrbitDB` at destination.
- Thrusting → `NewtonMoveDB` present; after burn-out the entity coasts.

---

## Newtonian Movement

`NewtonianMovementProcessor.ProcessManager()`:
1. Gets all entities with `NewtonMoveDB`.
2. Calls `NewtonMove(db, toDateTime)` — integrates position using current velocity.
3. Calls `MoveStateProcessor.ProcessForType(nmdb, toDateTime)` — checks for state transitions (orbit capture, thrust execution).

`NewtonThrustCommand` encodes a delta-V to apply at a specific datetime. The `NewtonianMovementProcessor` applies it when the scheduled time is reached.

**Fuel is tracked in `NewtonThrustAbilityDB`.** Thrust commands consume fuel. When fuel = 0 the entity can no longer maneuver but still coasts.

---

## Warp Movement

Warp is an abstraction that moves an entity from one orbit to another without simulating the full transit physics.

`WarpMoveCommand` → adds `WarpMovingDB` with target body and ETA → `WarpMoveProcessor` counts down → at ETA: removes `WarpMovingDB`, creates appropriate orbit at destination.

Warp speed is calculated by `WarpMath.CalcMaxWarpSpeed()` based on `WarpAbilityDB` properties.

---

## Nav Sequence

`NavSequenceDB` holds an ordered queue of `INavAction` items (move-to-body, warp, refuel, resupply, survey anomaly, etc.). `NavSequenceProcessor` (instance processor) pulls the next action off the queue and schedules it. This is the primary way to give an entity a multi-step mission.

---

## Inter-System Jumps

`InterSystemJumpProcessor` handles the cross-system transit:
1. Entity reaches jump point.
2. `MasterTimePulse.AddSystemInteractionInterupt()` is called — forces all systems to sync at this datetime.
3. Entity is removed from source `StarSystem` manager, added to destination manager.
4. Its nav sequence is transferred via `ManagerSubPulse.TransferEntity()`.

---

## Pathfinding

`PathfindingManager` builds a graph over the jump point network (nodes = star systems, edges = jump points) and runs A* to find shortest routes. Used by UI to calculate travel routes and by automated fleet orders (`MoveToNearestColonyAction`, etc.).

---

## Relevance to Ground Combat

Ground combat requires landing troops, which requires:
1. Transport ship in orbit (using `WarpAbilityDB` / `OrbitDB` to arrive).
2. Descent from orbit to surface — currently no "landing" action exists. This will be a new `INavAction` appended to `NavSequenceDB`.
3. Ground unit entities, once landed, do **not** need `NewtonMoveDB` — they move at tactical scale on the planet, not the star system scale.

---

## Gotchas

1. **`directAttack = false` in `MissileProcessor`.** The Newtonian thrust-to-intercept path (`ThrustToTargetCmd`) exists but is never used because `directAttack` is hardcoded `false`. Phasing maneuvers are used instead, which can fail when the target isn't in a stable orbit. Fix in `Weapons/WeaponMissile/MissleProcessor.cs`.

2. **`NewtonSimpleProcessor` vs `NewtonianMovementProcessor`.** Both exist. Simple is for projectiles / beams that don't thrust; full is for ships and missiles. Don't attach `NewtonMoveDB` to a non-thrusting entity expecting the simple behavior — use `NewtonSimpleMoveDB`.

3. **`Temporal Anomaly Exception` when scheduling movement.** If a movement command is scheduled for a datetime already past in the manager's timeline, `ManagerSubPulse.AddEntityInterupt()` throws. Always use `entity.StarSysDateTime` as the base when constructing future datetimes for commands.

4. **`MoveToNearestAction.cs` family** — these are fleet automation helpers, not player orders. They are used by fleet order sequences (e.g., "patrol nearest colony"). Do not confuse with `NavSequenceCommand` (which is the player-facing order).
