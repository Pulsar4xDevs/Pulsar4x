# Skilled warp-brake

Status: playing. Implemented 2026-11-01. The hops are ordinary warps, and they pay the plan 2 sustain bill.

A high-Nav captain who cannot capture the pasted speed climbs inside the destination well, warps back to low orbit once the parent-relative speed has fallen, and repeats until plan 1's burn can finish or three bubbles have been spent. A low-Nav captain, and an empty chair, stay on plan 1.

## Frame

The saved vector stays parent-relative. `WarpMovingDB` keeps storing `MoveMath.GetRelativeState(...).Velocity`, and that path still does not consult `UseRelativeVelocity`. Plan 2's climb is a sustain draw along the warp and does not change the saved vector. A sun-frame saved vector would change every arrival, including plan 1's capture test, and it wants launch windows. That is a different plan.

Under this paste, gravity changes the saved speed only while the bubble is down. Climbing away from the same parent trades speed for height. The next bubble saves the slower speed and drops back at low orbit, discarding the height. That is the brake. A rocket burn that simply subtracts the same metres per second before departure costs the same Δv as braking after arrival, so this plan does not add one.

## Decided

- The drop stays the tangential low orbit from `PlannedWarpExitOffset`. The climb is the coast out to 90% of the drop-in parent's sphere, the same fraction `TryCaptureSpeed` uses for apoapsis. A higher drop sheds less, because escape speed is lower there.
- At most three bubbles, counting the warp that enters the well and each warp back to low orbit. A coast is not a bubble. `MaxBrakeWarps = 3` next to `CaptureAllowsWarp`.
- A hop counts only when the speed at the cap is at least 1 m/s slower than the speed at low orbit on that same energy. A step that does not shed is not flown.
- The ship warps again only while the drop-in parent is still the parent. Leaving the sphere makes the next save sun-relative. That ends the brake.
- `EvaluateWarp` still refuses with `already in destination gravity well`. The rewarp is built by `TryBuildMoveActions` and does not go through that gate.
- One action per plan. `AgentProcessor`, when an action for the goal succeeds, drops every still-queued follow-on and calls `Plan` again. A list of warp, coast, warp, circularise queued up front loses everything after the first warp. Emit the next step only.
- The last step is plan 1. When `TryArrivalBurn` succeeds, the existing in-well path queues `CirculariseAction`. This plan does not emit that burn.
- The captain knows the named skill in the next section. That is Nav quality of at least 1.25 (skill 25). Quality is `1 + skill / 100`. A cap of 40 can reach 1.25. A fresh captain is 1.0 and stays on plan 1. The failure sentence stays plan 1's `Not enough Δv to orbit ...`. The skill name is not in that sentence. That matches `agents-and-goals-design.md`: skill never fails a plan. This gate chooses a plot.
- A commander aboard that ship, the same rule as `CaptureAllowsWarp`. A fleet commander who is not aboard does not count.
- A game restart is required. This adds a class. It will not apply with Edit and Continue.

## The skill

The Nav gate is a named skill, **Warp brake**. A captain knows it when Nav quality is at least 1.25. It is derived from Nav. It is not a fourth `SkillDomain`, and it does not spend its own XP. The working name can change. The shape should not: one definition per skill of this type, so the next one is another row.

The definition sits next to `CommanderSkills`. It holds the display name, one line a player can read, and the predicate. The planner asks the predicate. It does not compare 1.25 itself.

`CommanderSnapshot` does not carry skills. Project the known skills as the name plus that line. An old save is unchanged, because nothing new is stored on `CommanderDB`. Adding the snapshot field needs a game restart.

Show the list on the person card, under a Skills header, and omit the header when the captain knows none.

- `CommanderWindow.DisplayPersonDetail`, the card beside the roster. `DisplayPeopleList` is the table (name, type, rank, assignment, years). It does not grow a skill column.
- The assign-captain chooser. `FleetWindow` opens `DisplayHelpers.PeopleChooser`, and that pane repeats the portrait, the columns, and the bonus list.

Those two panes are copies. Pull that block into one shared function and call it from both. `EntityCard.DisplayCommander` is a third copy of the same card. It calls the same function.

## What the coast is worth

Speed at a wider radius, same parent, same energy:

```
v(r2)² = v(r1)² + 2 μ (1/r2 − 1/r1)
```

`μ` is `OrbitMath.SGP(parent, ship)`. `r1` is `OrbitMath.LowOrbitRadius`. `r2` is `0.9 * OrbitMath.GetSOIRadius` of the drop-in parent's orbit.

The Earth-orbit ship in `CaptureEllipseTests.EarthShipAimedAtMars` pastes about 7.8 km/s onto that Mars. One coast to the cap leaves about 6.2 km/s, still above capture. A second coast leaves about 3.9 km/s, under `TryCaptureSpeed`, so `TryArrivalBurn` succeeds even with a 1 m/s tank. Three bubbles, two coasts, then plan 1's burn.

A paste near 30 km/s sheds one escape-speed squared per hop. That is dozens of bubbles. The cap refuses it before the first extra warp. A parent with no orbit, or a sphere that does not reach past low orbit (`rCap <= rLow * 1.05`), has no room to climb. `TryCaptureSpeed` already rejects the star. An asteroid well fails the room test. Those stay plan 1.

## What to build

The Nav check in the steps below calls the Warp brake predicate from the skill section. The card work in that section is part of this build.

### 1. The closed form

Add the helper next to `CaptureAllowsWarp` in `MoveToPlanners.cs`. No new planner.

Inputs: the ship, the target, the time. It uses the live parent-relative speed and radius, the tank's `DeltaV`, and plan 1's `TryArrivalBurn`.

`rLow = OrbitMath.LowOrbitRadius(target)` when the target has a mass. Drop-in parent from `PredictDropInParent` with that radius. No capture speed, or `rCap <= rLow * 1.05`: the brake does not fit.

Simulate, and count a bubble each time the ship warps:

- Outside the drop-in parent (`GetSOIParentEntity` is neither the target nor that parent): the first bubble drops at low orbit with the departure speed. `PlannedWarpExitOffset` stays the exit.
- Inside, radius below the midpoint `(rLow + rCap) / 2`: coast. Speed becomes `v(rCap)` from the formula above. The speed must fall by at least 1 m/s. This adds no bubble.
- Inside, radius at or above the midpoint: one bubble, back to low orbit, speed unchanged. That is the paste.
- Stop when the state is at low orbit and `TryArrivalBurn` returns true.
- Stop with "does not fit" when the next bubble would be the fourth, the coast would not shed 1 m/s, or the speed at the cap is not finite.

The midpoint test is how a replan tells a fresh drop (coast) from a ship that has already climbed (rewarp).

### 2. One step, then replan

In `TryBuildMoveActions`, after `Select`:

- The chosen option is feasible. Leave it. A capturable arrival, including one that plan 1 already allows before the first warp, never starts a brake. A ship that already has the Δv burns.
- Warp was chosen, and `CaptureAllowsWarp` is false. The commander is aboard and Nav quality is at least 1.25, and the closed form fits. Emit one `WarpMoveAction.CreateWarpOnly` to the usual low-orbit offset. Do not also queue `CirculariseAction`. Return true. The reason is the sentence below.
- Nothing is feasible, the ship is inside the drop-in parent, the same commander test passes, and the closed form fits. Radius below the midpoint: emit one `ShedSpeedAction`. Radius at or above the midpoint, and the live speed is at least 1 m/s under the speed this orbit has at `rLow`: emit one `CreateWarpOnly` back to low orbit, offset from the current parent-relative velocity. Return true.
- Otherwise return false. Outside, the reason stays the `CaptureAllowsWarp` sentence. Inside, a commander who was offered the brake and does not fit gets that same sentence, built the same way (`Not enough Δv to orbit <parent> (need <capture Δv>, have <budget>)`, both through `Stringify.Velocity`). An empty chair inside the well keeps today's reason. `CaptureAllowsWarp` returns true when there is no commander, so an empty chair still gets the normal warp and circularise, and the later replan does not coast.

`Goal.BrakeSteps` is an int, default 0. An old save leaves it at 0. `TryBuildMoveActions` gains an optional `Goal goal = null` after the time argument. The short overload forwards it. When a brake step is emitted, `goal` is not null, and `BrakeSteps` is 0, set it to the bubble count from this state's simulation. Later plans read it. This is the only write. Do not write `Goal.Status`.

`n` is `BrakeSteps` once set, otherwise the bubble count from this state. `k` is `n - bubblesStillToOpen + 1`, and `bubblesStillToOpen` includes the warp this step belongs to. The coast and the rewarp it is waiting on share a `k`.

Pass `goal` from every planner that already has one and calls `TryBuildMoveActions`: `MoveToPlan`, geo survey, grav survey, `StoragePlanners`, `JumpRoute`, `BuyFuelPlan`, `MoveGoodsPlan`, `MarketRun`, `MineAsteroidsPlan`. Hand plots and the existing capture tests omit it.

### 3. The coast

New `ShedSpeedAction` in `GameEngine/Movement/ShedSpeedAction.cs`. Movement lane, blocking. It spends no fuel and opens no bubble. `OrbitProcessor` already propagates the hyperbola.

Fields: the drop-in parent's id, the cap radius, the parent-relative speed at `Execute`, the wake time, and the detail sentence.

`Execute` requires that parent. Wake time is `OrbitMath.TimeToRadius` of the cap. Register `AddEntityInterupt` for `ActionQueueProcessor` at that time, the same shape `NewtonThrustAction` uses. Do not register a time that is not after now.

`IsFinished` succeeds when the wake is due, the parent is still that body, and the live speed is at least 1 m/s under the speed stored at `Execute`. Parent changed: `Fail("left the gravity well")`. The wake is not in the future while the radius is still under the cap, or the speed did not fall: `Fail("speed did not fall")`. `FailedActionReason` already copies `FailureMessage` onto the goal. The log turns that into `Could not finish.` plus the sentence. Do not add a token to `IsReach`.

`Clone` copies the parent id, the cap, and the sentence, and leaves `Status` queued and `IsRunning` false. Do not copy a live orbit. `Rehydrate`, when `Running`, registers the interrupt again from the stored wake time. Saves already write the action type by name.

`MoveToPlan.Cancel` needs no change. Only one brake action is queued, and a warp in flight is already the action that cancel keeps.

### 4. The order line

Exact shape, on the goal message (`PlanActions` already passes the `TryBuildMoveActions` reason through) and on `ShedSpeedAction.Details`:

`Shedding speed, warp 2 of 3`

`k` and `n` are the numbers from the section above. Warp action details stay the warp's own line (`Warping`, `Charging batteries`). The coast is the long wait, and that line is what the player reads.

For the Earth-orbit paste onto the capture-test Mars, with `BrakeSteps` set by the entry plan: the entry reason is `Shedding speed, warp 1 of 3`, and the first coast is `Shedding speed, warp 2 of 3`.

## Tests

Add `Pulsar4X.Tests/WarpBrakeTests.cs`. Use the fixtures in `CaptureEllipseTests` (`EarthShipAimedAtMars`, `Place`, `TMPDIR=/home/se5a/.cache/build-tmp`, `dotnet` from outside the repo). Grant Nav with `CommanderSkills.Grant`. Do not advance the clock across a real coast.

- Closed form on that Earth-orbit ship, tank 1 m/s. The bubble count is 3. The speed after two coasts passes `TryArrivalBurn` at low orbit.
- Same ship, Nav 25. `MoveToPlan.Plan` returns one `WarpMoveAction` and no `CirculariseAction`. The message is `Shedding speed, warp 1 of 3`. `Goal.BrakeSteps` is 3.
- Nav 24, and Nav 0. `TryBuildMoveActions` returns false, the reason starts with `Not enough Δv to orbit`, and no warp is queued. `CaptainedShip_UnderCapture_RefusesTheWarp` and `EmptyChair_UnderCapture_StillPlotsWarpAndCircularise` stay as they are.
- Same goal, ship then `Place`d on Mars at low orbit with the Earth circular speed. The next `Plan` returns one `ShedSpeedAction` whose details are `Shedding speed, warp 2 of 3`.
- Ship `Place`d at the cap with the once-coasted speed, `BrakeSteps` 3, Nav 25. `Plan` returns one `WarpMoveAction`. A Nav 0 ship in that same place does not get a warp.
- Ship `Place`d at low orbit with the twice-coasted speed. `Plan` returns one `CirculariseAction`. The message does not start with `Shedding speed`.
- Parent-relative speed 30 km/s at Mars low orbit, Nav 25. The plan fails, the reason starts with `Not enough Δv to orbit`, and no `ShedSpeedAction` is queued.
- `ShedSpeedAction` on the fresh Mars hyperbola: `Execute` leaves it running, and the wake time is after now. Move the orbit to the cap at the vis-viva speed, and `IsFinished` succeeds. Set the parent to the star, and it fails with `left the gravity well`.

Run `FullyQualifiedName~WarpBrakeTests|FullyQualifiedName~CaptureEllipseTests`.

## Out

Launch windows. An unbounded search. A pre-departure burn, including an Oberth raise-apoapsis. An empty chair attempting the sequence. Removing plan 1's capture or its refusal. Changing `SavedNewtonionVector` or `UseRelativeVelocity`. A hole in `EvaluateWarp` that any caller can warp through. Deleting `CreateCommandEZ`. Any change to `NewtonSimpleProcessor`. A second warp processor. A planet-well term on top of plan 2. The pause checkbox.

## Stop if

The Earth-orbit paste onto the capture-test Mars does not reach a state `TryArrivalBurn` accepts within three bubbles. Fix the helper before the Nav gate. A gate that launches a sequence the burn cannot finish will open bubbles and still refuse.

`OrbitMath.TimeToRadius` for that same hyperbola, aimed at the cap, is not after the drop-in epoch. Fix the wake time before arming the coast. A coast that never wakes leaves the ship to cross the sphere, and the next save is sun-relative.
