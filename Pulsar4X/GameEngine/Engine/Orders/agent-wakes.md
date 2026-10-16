# Agent wakes, the survey hang, and the jump key

Status: done. Written 2026-10-17 from near-term item 2. Implemented 2026-10-17.

`ScheduleAgent` drops that entity's queued `AgentProcessor` rows, then adds one. A second pass that is still due at the current instant throws and names the processor. `ProcessNextInterupt` removes the key it just ran. `PulseActionDictionary.DoAction` invokes the jump action with one argument so that key is removed after the action runs. A ship parked on a `MoveTypes.None` site uses `PositionDB.Velocity` (zero when sitting still), so a later warp can be plotted. The Mars fleet survey returns past the first survey tick. The grav on-site survey returns with the outer anomaly included. The Mars run did not name a repeating warp interrupt, so `MoveToPlanners` and `WarpMoveProcessor` were not changed.

Lode Mining and Strata Survey stay on the clock, and the debug Instance Processors list stays at one future `AgentProcessor` wake per ship, fleet, or colony. A finished geo survey can run on into the next site. A master-pulse jump key cannot pin the global clock on one instant.

The ship's own order list is already one mine, warp, or survey at a time. This plan does not change which orders get queued.

## Decided

- One plan, three slices, in the order below.
- A finished or failed action wakes that agent immediately. One later poll stays as a backstop. Any new wake replaces that poll. A quiet dig does not add wakes.
- The same replace rule applies to every agent host: ships, fleets, and colonies. Colonies keep a poll because they still schedule one.
- The backstop stays `CommanderSkills.RecheckInterval` (30 minutes, longer with Nav).
- Live jumps stay on `ShipJumpAction`. `SetJump` is not the way a ship changes systems.

## What is going wrong

### Slice 1. Wakes pile up

`AgentProcessor.ScheduleAgent` always `TimeQueue.Add`s. `TimeQueue.Add` does not replace.

A poll that fires has already been removed by `Split` before the agent runs. The active-goal path then schedules one replacement. While a mine or survey action is still running, that path only confirms the action and plants the next poll, so a quiet stretch stays at one wake.

The count climbs on a real completion. `ActionQueueProcessor` calls `RunAgentNow` when a goal-tagged action succeeds or fails. That call leaves the already-queued poll in the queue, and the plan then schedules another. Each finished burn, dig, or sale adds one leftover. `MineStanding` and `SurveyStanding` never complete, so those leftovers never drain. The re-entry gate inside `RunAgentNow` schedules the same way. Pathfinder does this too.

The debug window's Instance Processors tab is that queue. It shows future wakes, not a history.

### Slice 2. A finished survey freezes `ProcessSystem`

`GeoSurveyFleetTests.FleetSurvey_MarsAndMoons_ShipsSurvey_TankerMovesToParent` stops at 24 hours. Mars in that fixture needs 575 points and the survey kit has Speed 600, so the first daily survey tick (about 26 hours after the order starts) finishes Mars. The test comment says that tick replans into a warp to the ship's own position and `ProcessSystem` never returns. The grav on-site test leaves off the next anomaly for the same reason: a warp to a `MoveTypes.None` site can sit on one instant.

`ProcessSystem` loops while `StarSysDateTime < target`. Each pass runs out to `GetNextInterupt`. If that time is the instant the system is already on, the pass has a zero span and the outer loop repeats it. `AddEntityInterupt` already drops a same-instant add while instance processors are inside `Split`. A zero-length warp is still able to come from outside that guard.

`WarpMath.GetInterceptPosition` for a `MoveTypes.None` target returns `atDateTime + distance/speed`. Distance 0 means the exit time is now. `ScheduleArrival` only queues an exit that is strictly after `NextSafeInterruptTime`. `FinishArrival` for `None` parks the ship and calls `WakeActionQueue` inline. `AlreadyThere` is supposed to yield an empty move list when the ship is on a static site (within 100 km) or already in a circular orbit at the body. The hang means some replan on that tick still emits a warp, or a zero-length warp still schedules the current instant.

This slice starts by extending that test and naming the interrupt that repeats. The notes say it is a warp to self. Confirm that on the run before editing the planner or the warp processor.

### Slice 3. The master pulse keeps a jump key

`MasterTimePulse.ProcessNextInterupt` runs the pairs under `EntityDictionary.Keys.Min()` and returns that time. Nothing removes the key. Once `GameGlobalDateTime` reaches it, every `AdvanceOneStep` returns the same instant and `SimulateTimeUntil` repeats it.

The only writer is `InterSystemJumpProcessor.SetJump`. Nothing in the live game calls it. `ShipJumpAction.Execute` transfers the ship itself. The geo fleet test calls `ProcessSystem` rather than `TimeStep` because a static test `Game` can already be holding that key.

## What to build

### 1. One pending agent wake

Add a remover on `ManagerSubPulse` that drops queued rows whose processor is `nameof(AgentProcessor)` and whose entity is the one being scheduled. Leave every other processor on that entity in the queue. Warp arrivals and action-queue interrupts stay.

`ScheduleAgent` removes those rows, then adds one at `when`. The existing clamp stays: a time at or before `NextSafeInterruptTime` becomes `now + RecheckInterval`.

Remove from the highest index downward. `RemoveEntity` walks low to high and shifts the tail. Do not copy that loop, and do not call `RemoveEntity` here. That call drops the warp interrupt too.

`RunAgentNow` stays the immediate wake from the action queue. It does not need its own remove. The plan's call to `ScheduleAgent` replaces the old poll. A completion path that does not schedule leaves at most the poll that was already there. That poll fires, sees a finished `GivenGoal`, and returns without planting another. Standing jobs never take that path, so their count stays at one only because of the replace.

Applies on the next wake of a game that already piled rows: the first `ScheduleAgent` clears the extras and leaves one.

### 2. The survey tick must move the clock

Extend `FleetSurvey_MarsAndMoons_ShipsSurvey_TankerMovesToParent` past the first survey tick, still under the 15 second timeout. Mars completes on that tick. The call has to return, and system time has to move past it.

On that run, record which queued interrupt is at `StarSysDateTime` when the spin starts. Fix that producer.

The shape to aim at:

- A ship already at the target gets an empty move list from `AlreadyThere`. The survey order can still be appended. A warp to that same body is not added.
- A warp whose exit time is not strictly after `NextSafeInterruptTime` arrives inside the current call. It does not insert an interrupt at the current instant.
- `ProcessSystem` may run one pass for an interrupt that is already due. If the earliest interrupt is still at or before `StarSysDateTime` after that pass, throw. Name the processor and the entity on the queue head. A missed producer then stops the clock through the existing fault report. It must not freeze the window.

`FleetGravSurvey_OnSite_CompletesAssignedAnomalies` can include the outer anomaly again once a warp to a `MoveTypes.None` site returns. The pending-wake assert in that test stays, and after slice 1 the count is one per surveyor once the work is underway.

### 3. Drop the jump key after it runs

At the end of `ProcessNextInterupt`, remove the datetime key that was just invoked. `SetJump` writes the exit and the entry as two keys. Removing the one that just ran leaves the later key in place.

A step whose next key is in the future advances to that key or to the caller's target, whichever is sooner. A second step after the key's time moves past it.

`ShipJumpAction` stays as it is.

## Tests

Run from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor`. `Pulsar4X.Tests` is net8.0. The machine SDK may be newer. Point `TMPDIR` at `/home/se5a/.cache/build-tmp` if the root disk is full.

- After a `ScheduleAgent`, a second `ScheduleAgent` for the same entity leaves one `AgentProcessor` row, at the new time. A `WarpMoveProcessor` row on that entity is still there.
- A mine or survey action that is still running, woken by its poll several times, stays at one pending agent wake.
- A succeeded goal-tagged action wakes the agent and still leaves one pending agent wake.
- The Mars fleet survey processes past the first survey tick and returns inside the timeout. System time is past that tick.
- With the outer grav anomaly present, `ProcessSystem` returns after the on-site surveys complete.
- An interaction interrupt placed at a known time is absent from `EntityDictionary` after the pulse passes it, and the following step's global time is later.

`CountAgentInterrupts` in `GravSurveyFleetTests` is the pattern for the wake count.

## Expected files

One new file:

- `Pulsar4X.Tests/AgentWakeTests.cs` — a second `ScheduleAgent` leaves one agent wake, a piled queue drains to one, a running action stays at one across polls and across a finished action, and a jump key is gone after the pulse passes it.

These change:

- `GameEngine/Engine/ManagerSubPulse.cs` — drop queued `AgentProcessor` rows for one entity. Throw when a second pass is still sitting on the current instant.
- `GameEngine/Engine/Orders/AgentProcessor.cs` — `ScheduleAgent` removes, then adds.
- `GameEngine/Engine/MasterTimePulse.cs` — `ProcessNextInterupt` removes the key it just ran.
- `Pulsar4X.Tests/GeoSurveyFleetTests.cs` — the Mars fleet test runs past the first survey tick.
- `Pulsar4X.Tests/GravSurveyFleetTests.cs` — the on-site test includes the outer anomaly again.
- `GameEngine/Engine/Orders/agent-wakes.md` — status, once the work is done.
- `Pulsar4X/near-term.md` — item 2 marked done.
- `Pulsar4X/agent-notes.md` — the jump-key item and the geo-survey hang item marked done.
- `GameEngine/Engine/Orders/plans/02-standing-jobs.md` — the stop-if line, once the hang is gone.

These change only if the extended Mars run names them as the repeating interrupt:

- `GameEngine/Movement/MoveToPlanners.cs`
- `GameEngine/Movement/WarpMove/WarpMoveProcessor.cs`
- `GameEngine/Movement/WarpMove/WarpMath.cs`
- `GameEngine/GeoSurveys/GeoServeyPlanners.cs`
- `GameEngine/JumpPoints/GravServeyPlanners.cs`

`TimeQueue.cs` stays. `RemoveAt` is enough. `JumpOrder.cs` stays. No client file.

## Token use

Expected total to finish this plan: **180,000** tokens, input and output together. That is the implementation session or sessions, starting from this file and ending when the three slices are done and the notes above are marked. The session that wrote this plan is not part of the figure.

- Slice 1, about 50,000. The three engine edits, `AgentWakeTests.cs`, one test run.
- Slice 2, about 90,000. Extend the Mars test, name the interrupt, fix that producer, then the grav test. One repeat run is included.
- Slice 3, about 25,000. Remove the key and cover it in `AgentWakeTests.cs`.
- Notes and a last run of the named tests, about 15,000.

If slice 2 stops at the processor name, expect about 120,000 instead.

On completion, tell the user three numbers: the actual total, this 180,000, and the difference (actual minus expected). If the absolute difference is more than 40,000, add one sentence on what the extra or the saving was. Add the session totals together if the work took more than one session. Use the token total the client shows for those sessions. If that total is not shown, say so.

## Out

Clone of `WarpMoveAction` and `NewtonSimpleAction`, arrival against a target that started thrusting, a ship short of ΔV, and the "charging batteries" hold. Those are near-term item 6 and `movement-issues.md`.

The Pause checkbox. That fix is `stash@{0}` and is near-term item 5.

Clearing `GivenGoal`. That is near-term item 3.

Saving a warp in progress. That is near-term item 4 and movement-issues 2.3.

Changing how long `RecheckInterval` is. Deduping processors other than `AgentProcessor`. Routing live jumps through `SetJump`.

## Stop if

Slice 2's extended run spins on something other than a same-instant warp. Write down the processor name and stop. Do not patch `MovePlanner` or `WarpMoveProcessor` until that name is known.

The Ceres standing-job clock note in `plans/02-standing-jobs.md` is this hang. A Ceres clock test that hits it stops there. This plan is the fix it was waiting on.
