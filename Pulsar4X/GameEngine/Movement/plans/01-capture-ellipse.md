# Capture into an ellipse, or refuse before the warp

Status: playing. Implemented 2026-10-31. Do not start plan 2 or plan 3 until asked.

A MoveTo from a deep well (Hermes, Earth low orbit, to Mars) warps, then the circularise asks for the full drop from the pasted speed down to circular. The tank cannot pay that. `NewtonSimpleProcessor` refuses the whole burn before spending any fuel, and the ship is left on the arrival hyperbola. It never tries for a bound ellipse. A captain can already know this before the bubble, and today the planner prices the warp at 0 m/s.

This plan makes two changes. The arrival burn aims at the best bound orbit the tank can buy, and succeeds when that orbit stays inside the drop-in parent's sphere of influence. A ship with a captain does not open the warp when even that orbit is out of reach. An empty chair still plots the warp. The burn then captures or fails without spending fuel.

## Decided

- One plan, two slices, in the order below. Slice 1 is the burn. Slice 2 is the captain gate. Slice 1 is useful on its own.
- Do not change `NewtonSimpleProcessor`. A short burn still fails whole and spends nothing. Hohmann, match-orbit, and altitude changes keep that rule. The arrival action builds a target the tank can finish.
- The budget is `NewtonThrustAbilityDB.DeltaV`, the same number `EvaluateNewtonian` already compares. The target Δv must fit that budget, or the processor will fail the burn on the first tick.
- The velocity that gets pasted is the parent-relative speed `WarpMovingDB` saves (`MoveMath.GetRelativeState`). The `UseRelativeVelocity` setting is not consulted there. The estimate uses that same speed. Do not "fix" the setting in this plan.
- Nav's along-track lead does not change the speed. Ignore it in the estimate.
- No refuel prepend, no tanker, no second warp, no change to `CreateCommandEZ`.
- A game restart is required. This adds methods. It will not apply with Edit and Continue.

## What is going wrong

`EvaluateWarp` returns Δv 0. `BuildWarpAndCircularise` always queues `WarpMoveAction.CreateWarpOnly` plus a `CirculariseAction` when the target orbits.

On drop-in the saved parent-relative vector is reused as velocity about the new parent. The exit point is placed so that vector is tangential, at `OrbitMath.LowOrbitRadius` (1.1× body radius). An Earth low orbit is about 7.8 km/s. Around Mars that is above escape at every radius outside the surface, so the leftover is a hyperbola. `CirculariseAction.TryCompute` always targets `KeplerCircularFromPosition`. The Δv is several km/s. The processor's fuel check fails first, `Fail` installs the untouched start Kepler, and the action fails with "not enough ΔV". `MoveToPlan.PlanActions` then fails the goal with "a move action failed".

`PredictDropInParent` is the parent that drop-in will use: the target when its sphere of influence contains the exit offset, otherwise the target's own parent. Mars contains a low orbit. A small moon or an asteroid often does not, and the parent becomes Mars or the star.

`TranslateGoToBody` and `TranslateWarpToBody` reject a plot when the ship has a captain or is a fleet (`RejectPlotOnCaptainOrFleet`). Captains arrive through `MoveToPlan` and the other planners that call `TryBuildMoveActions`. An empty chair is the only caller of the hand plot.

## What to build

### 1. The burn aims at a bound orbit

Add one helper next to `PredictDropInParent` in `MoveToPlanners.cs`. `CirculariseAction.TryCompute` uses it. Do not add a new type.

Inputs: the ship, the drop-in parent, the position, the tangential velocity, and the Δv budget.

The loosest orbit that counts is tangential at that position, with apoapsis at 90% of the parent's sphere of influence (`OrbitMath.GetSOIRadius` on the parent's `OrbitDB`). Call that the capture orbit. A parent with no orbit, or a sphere that does not contain the radius, has no capture orbit. That is the star when a rock's own sphere is smaller than the exit. Do not spend fuel trying to circularise around the star.

From the current speed `v` at radius `r`:

- Circular speed is `sqrt(μ/r)`. Escape speed is `sqrt(2μ/r)`. Use `OrbitMath.SGP`.
- If the budget can reach circular speed, the target stays `KeplerCircularFromPosition`. Today's full circularise is unchanged, including the "Already circular" exit when eccentricity is under 0.05.
- If the budget can reach the capture orbit but not circular, the target speed is `v` minus the budget, and not below circular speed. Same position, same direction, speed reduced. Build it with `KeplerFromPositionAndVelocity`. That is the tightest ellipse the tank can buy.
- If the budget cannot reach the capture orbit, there is no target.

`TryCompute` then:

- Already circular: succeed as today, details "Already circular". No burn.
- A target exists: start the `NewtonSimpleMoveDB` at that target. The processor can finish it.
- No target: do not start a burn and do not spend fuel. Fail with "not enough ΔV", the same words as today, so the existing failure log stays.

When the finished orbit is elliptical, the action still succeeds. Details, exact shape, so the log and the goal can copy them:

`Elliptical orbit around <parent name>, <shortfall> short of circular`

`<parent name>` is the drop-in parent's name for the ship's faction. `<shortfall>` is `Stringify.Velocity` of the Δv from the finished speed down to circular. `Stringify.Velocity` writes `Km/s`. Example: `Elliptical orbit around Mars, 1.4 Km/s short of circular`.

When the ship is already on an ellipse and the budget cannot lower the eccentricity by a meaningful amount (under the processor's 0.01 m/s snap), succeed immediately with that same details line. Do not start a burn that would only fail.

`PlanActions`, when every action for the goal has succeeded, passes that details line to `PlanResult.Done` if the circularise left an ellipse. A full circularise still completes with an empty message, and the log keeps "Arrived at <target>."

`ShipLogBook.Outcome` for a successful `MoveTo` uses the goal message when it starts with `Elliptical orbit`. One sentence: `Arrived at <target>. Elliptical orbit around Mars, 1.4 Km/s short of circular.` A full circularise is unchanged.

The goal completes. It must not fail and it must not replan another warp. The ship is bound. It is not in a matching circular orbit, and the sentence says so.

### 2. A captain does not warp when capture is impossible

In `TryBuildMoveActions`, after `Select` has chosen warp and before the actions are returned: if `CommanderSkills.TryGetCommander` finds a commander on that ship, run the same helper with the predicted drop-in.

Predicted state: parent from `PredictDropInParent`, radius `OrbitMath.LowOrbitRadius` of the target, speed the magnitude of `MoveMath.GetRelativeState` at `now`. Position can be `(radius, 0, 0)`. The speed is what will be pasted.

If that parent has no capture orbit, or the budget cannot reach it, return false. Do not queue a warp or a circularise. Reason, exact shape:

`Not enough Δv to orbit <parent name> (need <capture Δv>, have <budget>)`

Both speeds go through `Stringify.Velocity`. `PlanActions` already turns a false return into `PlanResult.Fail(reason)`. The goal fails before the bubble. The ship stays where it is.

The log already turns a failed MoveTo into `Could not reach <target>.` plus `PlayerReason`. The reason contains "Δv". `IsReach` matches that without regard to case, so the log keeps the reason. Do not add an internal token. Do not add a new log method.

This gate sits in `TryBuildMoveActions`, so a captained survey, mine, or haul uses it too. An empty chair has no commander, so `GoToBody` and `WarpToBody` still queue the warp and the circularise. Slice 1 then captures or fails without spending.

A fleet MoveTo gives each ship its own goal. The gate looks at the commander aboard that ship. A fleet commander who is not aboard does not count.

Hohmann, match-orbit, and altitude changes stay all-or-nothing. A new MoveTo while the ship is already in the destination well queues this same ellipse burn when the tank can finish a capture. Warp stays refused inside that well. A ship already slower than circular does not burn retrograde: if the apoapsis is inside 90% of the sphere the action succeeds with the elliptical sentence, and otherwise it fails without spending.

## Tests

Add `Pulsar4X.Tests/CaptureEllipseTests.cs`. Follow the fixtures in `NewtonSimpleProcessorTests.MakeCirculariseBurn` and `WarpMoveTests` (a real faction, a body on an orbit, `TMPDIR=/home/se5a/.cache/build-tmp`, `dotnet` from outside the repo).

- Hyperbolic leftover about Mars, budget between the capture Δv and the circularise Δv. After the circularise runs, the action has succeeded, eccentricity is below 1, apoapsis is inside 90% of Mars's sphere of influence, and fuel mass has dropped.
- Same leftover, budget under the capture Δv. The action fails with "not enough ΔV". Fuel mass is unchanged. Eccentricity stays at or above 1.
- Budget that can pay the circularise. Eccentricity ends under 0.05. This is the old success.
- Already on an ellipse, budget under the remaining circularise and over a useful trim. The action succeeds, eccentricity is lower, and the details start with `Elliptical orbit around`.
- Captained ship, Earth-orbit speed pasted onto a Mars low orbit, budget under the capture Δv. `TryBuildMoveActions` returns false, the reason starts with `Not enough Δv to orbit`, and no `WarpMoveAction` is in the list.
- Same numbers, no commander. `TryBuildMoveActions` returns a warp and a circularise.
- Ship already on a Mars hyperbola, budget between capture and circularise. `TryBuildMoveActions` to Mars returns a `CirculariseAction` and no warp. After the burn, the goal completes and the log starts `Arrived at Mars. Elliptical orbit around`.

`NewtonSimpleProcessorTests.ShortDeltaV_FailsWithoutSnappingToTarget` and `ShortDeltaV_FailsTheActionAndCoasts` stay as they are. They drive `NewtonSimpleAction` directly.

Run the new class and `FullyQualifiedName~NewtonSimpleProcessorTests|FullyQualifiedName~WarpMoveTests`.

## Out

Refuel before the warp. A tanker. Pre-departure burns. A second warp. Changing `SavedNewtonionVector` or `UseRelativeVelocity`. Deleting `CreateCommandEZ`. Any change to `NewtonSimpleProcessor`. The pause checkbox. Plans 2 and 3.

## Stop if

The capture orbit's apoapsis at 90% of the sphere is not actually bound for the Mars fixture (eccentricity at or above 1, or apoapsis outside the sphere). Fix the helper before wiring the captain gate. A gate that uses a different orbit than the burn will refuse ships that could have stayed, or launch ships that cannot.
