# Work in progress survives a load

Status: plan from 2026-10-17. Not implemented. Depends on `01-round-trip.md`. A load has to return a stepped game before this starts.

An order that was running when the player saved is the same order after load. It keeps the datablob it already had. It does not start a second one, and it can finish.

## Decided

- `Status` is the saved state. `IsRunning` stays obsolete and stays unserialized. Rehydrate sets `IsRunning` from `Status == Running` so the old flag matches.
- One virtual `Rehydrate(Game)` on `EntityAction`. `Game.Load` walks every `ActionQueueDB` and calls it after the managers are wired and before `UpdateActivityState`. The action processor keeps calling `Execute`. It does not call `IsValidCommand`.
- The base method resolves `EntityCommanding` from `EntityCommandingGuid`. A missing entity sets `Status` to `Failed`.
- A subclass whose start path creates a datablob, a processor, or another order overrides `Rehydrate`. It attaches the datablob already on the entity. It does not call the start path. If it cannot reattach, it fails the action with a message.
- Instant orders that only edit a queue get the base resolve and no override.

## What is already true

`ActionQueueDB.ActionList` serializes the action objects. `EntityAction` already saves `Status`, `EntityCommandingGuid`, `ActionOnDate`, `ParentGoalId`, and `_isFinished`. The live fields are private and are not saved: `_entityCommanding`, and on a warp `_targetEntity` and `_warpingDB`.

`WarpMoveAction.Execute` starts a bubble only when `Status != Running`. A warp saved as `Running` does not start a second bubble and does not charge `BubbleCreationCost` again. `IsFinished` then stays false, because `_warpingDB` is null and `IsRunning` is false, including after `EndWarpMove` removes the blob. The movement lane stays blocked. `movement-issues.md` section 2.3 still describes a second bubble. That description is stale. `Status` is saved now.

A warp saved as `Queued` (still charging) enters the start path on the next `Execute` and touches `_entityCommanding`, which is null.

`NewtonSimpleAction.IsFinished` now checks `_db` for null. `Execute` still creates a new `NewtonSimpleMoveDB` when `IsRunning` is false and the date is due. `CirculariseAction`, `MatchOrbitAction`, and `ChangeOrbitalAltitudeAction` do the same. `NewtonThrustAction` creates a `NewtonMoveDB`.

`GeoSurveyOrder.Execute` builds a new `GeoSurveyProcessor` and resets `PreviousUpdate` when `IsRunning` is false. Survey points live on the body's survey datablob. `IsFinished` reads that datablob.

The instance queue for the star system is already in the save. Plan 1 checks that its entries still point at the manager's entities. This plan checks that a running warp's arrival entry is one of them.

## Rehydrate

Base: resolve the commanding entity. Set `IsRunning` when `Status` is `Running`.

`WarpMoveAction`: resolve `TargetEntityGuid` as well. When `Status` is `Running` and the ship has a `WarpMovingDB`, point `_warpingDB` at that blob. Do not call `TryStartWarp`. Do not queue another `CirculariseAction`. A `Queued` warp stays queued, with both entities resolved, so the next `Execute` can start the one bubble it has not started yet.

`NewtonSimpleAction`, `CirculariseAction`, `MatchOrbitAction`, `ChangeOrbitalAltitudeAction`: when `Status` is `Running` and the ship has a `NewtonSimpleMoveDB`, point the action's `_db` at it. Do not construct another one.

`NewtonThrustAction`, `ThrustToTargetCmd`, `Thrust90ToTargetCmd`: the same, for `NewtonMoveDB`.

`MoveToNearestAction` and `WarpFleetTowardsTargetOrder`: their not-running path enqueues more orders. A `Running` one must not enqueue them again. Read the method before writing the override. Attach the warp it already started, using the warp rule above.

`GeoSurveyOrder` and `JPSurveyOrder`: set `IsRunning` from `Status`, and construct the processor the same way `Execute` does. Do not clear survey points and do not move `PreviousUpdate` backwards. Confirm the processor reads the points already on the body before relying on that constructor. If construction zeroes them, fail the action instead.

`AsteroidMineOrder`, `CargoTransferOrder`, `CargoInstallOrder`, and `IndustryOrder2`: same rule. The not-running branch must not run again for a `Running` action. Attach the datablob or processor already in progress. Read each `Execute` before writing the override.

Any other `Execute` that creates a datablob, a processor, or a follow-on order inside a not-running or not-`Running` branch gets the same override in this pass. Instant queue edits do not.

## Test

Depends on the Earth round-trip from plan 1.

Warp: put a ship on a warp and step until `WarpMovingDB.HasStarted` is true and `Status` is `Running`. Record stored energy and the arrival interrupt's time and entity id. Save, zero the id statics, load. The ship has one `WarpMovingDB`, the action's status is `Running`, stored energy is unchanged, and the arrival interrupt is still that ship at that time. Step to the arrival. The blob is gone, the action is no longer blocking the movement lane, and energy was not charged a second creation cost.

Burn: a `NewtonSimpleAction` or `CirculariseAction` that has already set its `NewtonSimpleMoveDB`. After load there is still one such blob, and `Execute` does not add another. Step until it completes and the lane clears.

Survey: a geo survey that has already earned points. After load the points are the same. One further day adds points once.

Keep these tests.

## Out of this plan

Changing warp math, splitting the arrival burn, removing the second warp-creation path, or the dead orbit helpers. Those stay in `movement-issues.md`.

Serializing `IsRunning`.

Restoring the open windows or the selected ship.

The crash-save beside the fault dialog.
