# Interplanetary Newtonian transfer (patched conics)

Status: not started. Do not build until asked. Written 2026-11-03 for the Early Tech Start mod.

Plan 3 of 3. It needs [plan 1](movement-newtonian-soi-capture.md) slices 3 and 4 (the escape and capture maths, and `SoiTransferAction`) and [plan 2](movement-lambert-porkchop.md) slices 1 to 3 (Lambert and the window scan). Do not start it before those are built and tested.

With no warp, a ship in low Earth orbit that wants to reach Mars needs three things: an escape burn that leaves Earth's sphere with the right hyperbolic excess, a heliocentric cruise of about eight months, and a capture burn at Mars. Today the planner refuses at `MovePlanner.EvaluateNewtonian` (`GameEngine/Movement/MoveToPlanners.cs` ~line 584): `target is under a different SOI parent; no interplanetary transfer maths yet`. The Early Tech Start test (`EarlyTechStartTests.NewtonianOnlyShip_IsOfferedNoWarpMove_AndCanHohmannWithinTheWell`) asserts that refusal for Mars. This plan removes it and the test flips to a pass.

## Decided

- **Three legs, one action.** Extend plan 1's `SoiTransferAction` into the general patched-conic action. States: `Depart`, `Escape` (coast out of the first sphere), `Cruise` (in the common parent's frame), `Correct` (optional), `Approach` (coast into the target sphere), `Capture`. Plan 1's single-sphere trip is the case with no `Cruise`. One queue entry, internally sequenced, like `MatchOrbitAction`. It is not a list of burns queued up front, for the reason plan 1 gives: later frames do not exist yet.
- **The engine already swaps frames.** `ChangeSOIProcessor` converts an escape hyperbola into a Sun-frame `OrbitDB`, and `EnterSOIProcessor` converts a Sun-frame orbit into a Mars-frame hyperbola (`Orbits/OrbitProcessor.cs` 190 and 255). Both publish `EventType.SOIChanged`. The action wakes on that event or on its own orbit-parent check. No new frame code.
- **Common ancestor, one level each side.** The lowest ancestor of ship parent and target is found by walking `GetSOIParentEntity`. This plan covers parent-planet to other-planet (Earth to Mars, Mars to Earth, Earth to Venus). A moon orbit to another planet is two escapes: out of the moon into the planet (plan 1, backwards), then this plan. It is composed by the planner, not built here.
- **Lambert from the real state, not from the planet's centre.** The escape hyperbola takes days to cross the sphere. At the sphere's edge the ship is about 0.9 million km from Earth. That is a start error of about a million km against a target a few thousand km across, so Lambert from Earth's centre alone misses Mars. The planner solves, simulates the leave, then re-solves:
  1. Plan 2's scan gives a first `vInf` and departure date from the planet's centre.
  2. Build the escape hyperbola for that `vInf` (plan 1 `EscapeBurn`).
  3. Predict the exit state with the same maths `ChangeSOIProcessor` uses: `OrbitMath.TimeToRadius`, `InstantaneousOrbitalVelocityVector_m`, plus the parent's state at that instant.
  4. Lambert from that exit state to the target aimed at `LowOrbitRadius` with the impact offset from plan 1.
  5. Adjust `vInf` by the difference. Repeat at most five times. If the exit state has not settled within 1,000 km at the target, fail with `transfer did not converge`.
- **One mid-course correction is allowed, and it is optional.** After the ship enters `Cruise`, the action solves Lambert from the ship's real state to the aim point and burns the difference if it is bigger than 1 m/s and affordable. This is the safety net for the integrator-versus-conic differences that the iteration above cannot see. It is `Correct`.
- **`Select` keeps preferring warp when a ship has both.** `MovePlanner.Select` today prefers `AlreadyThere`, then `NewtonianTransfer`, then `Warp`. If both are feasible on an interplanetary leg, an eight-month coast would beat warp. For a ship that has a warp drive keep today's behaviour for different-parent targets: warp first. Newtonian for these targets is chosen when warp is not feasible or the player picks it in the porkchop (plan 2, slice 4). Within one sphere (plan 1) the order is unchanged. See the open questions.
- **The planner checks the whole trip's Δv first,** as plan 1 does: escape plus (optional) correction allowance plus capture, against `NewtonThrustAbilityDB.DeltaV`. `NewtonSimpleProcessor` is not changed.

## What is going wrong

- `EvaluateNewtonian` has no interplanetary branch. The only maths that touches the idea is `InterceptCalcs.InterPlanetaryHohmann` (`GameEngine/Movement/InterceptCalcs.cs` ~23) and its client copy `SnapshotMoves.InterPlanetaryHohmann` (`Pulsar4X.Client/Api/SnapshotMoves.cs` ~157), used only by the NavWindow's `Interplanetary Transfer` mode (`NavWindow.cs` ~564). It has no test, and the planner does not call it. It returns three burns: an escape, and the two Hohmann burns at the heliocentric ends. There is no capture burn at the target planet and no frame change in between.
- Its escape burn `dva` goes from the parking orbit to the sphere's radius as an ellipse (`(2·SOI)/(a+SOI) − 1`). A ship on that ellipse reaches the edge slowly, barely bound, and is left with about the planet's own orbital speed. It does not carry the hyperbolic excess a Mars transfer needs (2.9 km/s at Earth). It also uses `GeneralMath.StandardGravitationalParameter(meMass + gpMass)` with the ship's mass in place of the right body.
- Its window `rads / closinRads` is the time until the planets' longitudes line up. A Hohmann transfer to Mars leaves when Mars is about 44° ahead of Earth. The variable `gpHommanAngle` holds the lead angle and is unused, and `1/2*Math.Sqrt(2)` in it is integer division, so it evaluates to `0`.
- `EnterSOIProcessor` is driven by `OrbitDB.ScheduleSOIEntry` (`Orbits/OrbitDB.cs` ~665). It scans 180 samples across at most one year. That is fine for Luna. For heliocentric cruise it has two weaknesses. A transfer longer than a year (Earth to Jupiter is about 2.7 years) is not scanned past the first year, so the encounter is never scheduled. And on a one-year, 180-step scan each step is about two days. At about 3 km/s relative speed that is 500,000 km a step against Mars's sphere radius of about 577,000 km. Mars is caught narrowly. A faster fly-by or a smaller body is missed, and the code treats a miss as "no encounter".
- `ChangeSOIProcessor` is only scheduled when `Eccentricity >= 1` (`OrbitDB.OnSetToEntity` ~620). The escape hyperbola is fine. A ship parked in the Sun frame whose ellipse touches Mars's sphere is handled only by the entry scan.
- `NewtonThrustAction.cs` ~line 416 carries `TODO: this is going to be even more broken now` for the continuous-thrust path (`NewtonMoveDB`, `ManuverVector`). Nothing in the planner uses that path. The planner flies impulsive burns through `NewtonSimpleMoveDB`, so this plan does not touch it.
- `NavWindow`'s `EscapeSOI` mode (~line 616) adds a flat `manuver.deltaV.Y += 1` m/s to a Hohmann to the sphere's edge. That is a hack, not an escape.

## What to build

### 1. Fix the entry scan before building on it

`Orbits/OrbitDB.cs` `ScheduleSOIEntry`. Not game rules, just detection:

- Choose the step size from physics: `step = min(period/180, soiRadius / (2 · relativeSpeedEstimate))`, with a ceiling on total samples (4,000) and a scan window of the lesser of the time to the next frame change and 10 years.
- After the first sampled hit, keep the existing 20-step bisection. When a miss passes through the sphere between two samples (distance to the child's centre dips inside the sphere only between samples), catch it by checking the closest approach in each step with a quadratic fit, not only the endpoints.
- Tests, `Pulsar4X.Tests/SoiEntryScanTests.cs`: a fast fly-by that the old scan misses is now found; a 2.7-year ellipse to Jupiter's orbit is scheduled; a trip that never enters is not.

### 2. Frame-level helper: common ancestor and body states

In `GameEngine/Movement/`, a small `PatchedConic` helper: `FindCommonAncestor(ship, target)`, `ExitStateAtSphere(escapeKE, parent, at)`, `StateRelativeToAncestor(entity, at)`. The last wraps `MoveMath.GetRelativeState` and `OrbitDB.GetPosition` so Lambert is fed positions in one frame. Tests: Earth to Sun frame sum equals `MoveMath.GetAbsoluteFuturePosition` for a ship, and Luna-in-Earth plus Earth-in-Sun equals Luna-in-Sun, to within a metre.

### 3. The patched-conic solve

`GameEngine/Movement/PatchedConicPlanner.cs`. Input: ship, target, now, budget. Output: a plan record (`DepartAt`, escape burn as a `KeplerElements` pair, expected `vInf` and exit state, expected arrival date, the Lambert state at the target, `TotalDeltaV`). Use the five-step loop in Decided. The departure burn:

- The escape burn is applied at the point of the parking orbit where the hyperbola's asymptote lines up with the required `vInf` direction. Rotate the `vInf` direction back by `acos(−1/e)` about the orbit normal. The ship passes that point once an orbit, so the departure wait is under a period.
- Sun-frame inclination: Earth's parking orbit is in the sim's reference plane. If the required `vInf` has a significant out-of-plane component (a target with an inclined orbit), the escape burn adds a plane-change component. Compute it from the vector, do not assume coplanar. Report it in the `MoveOption.Reason`.
- Do all the burns through `KeplerFromPositionAndVelocity` plus the existing `ProgradeToStateVector` helpers that `MatchOrbitAction.TryPlanBurns` uses.

Tests (`Pulsar4X.Tests/PatchedConicTests.cs`), no game loop yet, pure planner:
- Earth LEO to Mars: total Δv within 3% of the hand sum (escape about 3.5 km/s from 7,008 km, capture about 2.1 km/s into 1.1 Mars radii, total about 5.6 km/s), time of flight between 240 and 280 days when departure is at the Hohmann phase.
- Mars to Earth, and Earth to Venus (inner target, so the escape is retrograde against the planet's motion).
- Budget too low: infeasible with `needs X m/s Δv, have Y m/s`.
- Convergence: 100 random departure dates in one synodic period either converge or fail with a reason. None throws.

### 4. The action, end to end

Extend `SoiTransferAction` (plan 1) with `Escape`, `Cruise`, `Correct`, `Approach`.

- `Depart`: as plan 1.
- `Escape`: wait for the orbit parent to become the common ancestor. A time-out of 1.5 times the expected escape time fails with `Did not leave <parent>'s sphere`.
- `Cruise`: nothing to do except wait. Schedule one wake-up (`AddEntityInterupt`) at the correction point, half the flight time, and one at the expected encounter.
- `Correct`: from the real state, solve Lambert to the aim point. Skip if the difference is under 1 m/s. If it is over the remaining budget minus the capture Δv, skip and warn in the ship log: `Midcourse correction skipped, short of Δv`.
- `Approach`: wait for the orbit parent to equal the target. A time-out fails with `Missed the encounter with <target>`. The ship is then on a Sun-frame orbit and the captain must replan.
- `Capture`: as plan 1.
- `Rehydrate`: the state and the plan record are `[JsonProperty]`. A save in each state loads and finishes.

`MovePlanner`: `EvaluateNewtonian` calls `PatchedConicPlanner` when the target's orbit parent is neither the ship's parent nor the target itself (the cases plan 1 owns). `TryBuildMoveActions` returns the action. The `MoveOption.Message` is `Transfer to Mars, depart <date>, arrive <date>`.

Tests, `PatchedConicActionTests.cs`, using the Early Tech Start start and fuel added with `CargoTransferProcessor.AddRemoveCargoMass` so the tug has about 7 km/s:
- Plan Earth to Mars on day 0. Run `game.TimePulse` for up to 450 game days, in the same stepping the existing `NewGameWithEarlyMod_RunsAMonthOfGameTimeAndSurvivesASaveAndLoad` uses.
- Assert, in order: `SOIChanged` events `Exited SOI of Earth`, `Entered SOI of Mars`; the final `OrbitDB.Parent` is Mars; periapsis of the arrival hyperbola within 25% of the target; the finished orbit's eccentricity is under 0.05 (or the ellipse sentence); fuel burned within 5% of the Tsiolkovsky figure for the planned Δv (plus the correction).
- Save after the escape, again mid-cruise, again during the approach; load; the trip still finishes.
- A short tank: refuses with `Δv` before the first burn; fuel unchanged.
- Delete the `Mars refused` asserts in `EarlyTechStartTests.NewtonianOnlyShip_IsOfferedNoWarpMove_AndCanHohmannWithinTheWell` and replace them with `Feasible` or a Δv refusal, depending on the tug's tank (see open questions).

### 5. NavWindow, and remove the duplicates

- Rewire `NavMode.InterplanetaryTransfer` (~line 564, `DisplayInterPlanetaryHohmannMode`) to the porkchop with the SOI parent's siblings as targets (plan 2 slice 4), and to `Make it so` through `MovePlanner.PlanFromWindow` (plan 2 slice 5). Show the escape, correction allowance and capture Δv separately.
- Rewire `EscapeSOI` (~616) to plan 1's `EscapeBurn` and drop the `+1` hack.
- Delete `InterceptCalcs.InterPlanetaryHohmann` and `SnapshotMoves.InterPlanetaryHohmann`. `OrbitalMath.Hohmann2` stays: `ChangeOrbitalAltitudeAction.TryPlanBurns` (~166) uses it. Remove `HohmannOE2` if it still has no caller, and note it in master-plan section 4 if not.
- Update master-plan section 4, "Movement plumbing": strike `InterPlanetaryHohmann has no test and the planner does not use it` and `a burn that leaves the parent's sphere of influence is not detected` if plan 1's decision covers it.

Checked by a play-test list in the PR: Earth to Mars from the start screen with a fuelled tug, then Mars back.

## Numbers (by hand, to be replaced by test output)

- Earth to Mars Hohmann: about 259 days, heliocentric `vInf` at Earth about 2.94 km/s, at Mars about 2.65 km/s.
- Escape from 7,008 km: hyperbolic periapsis speed 11.06 km/s against circular 7.54 km/s, Δv about 3.5 km/s.
- Capture at Mars low orbit (1.1 × 3,390 km = 3,729 km): periapsis speed 5.48 km/s, circular 3.39 km/s, Δv about 2.1 km/s.
- Total about 5.6 km/s. The Newton Tug I will almost certainly need more tank than its mod design has, or a refuel (open questions).
- Earth's sphere is about 925,000 km. At 3 km/s the leaving hyperbola crosses it in about 4 days. Mars's sphere is about 577,000 km.

## Open questions

- Fuel. A tug with enough Δv for Earth to Mars and nothing left is a one-way trip. Do we give the mod a bigger tanker design, rely on refuelling at Mars (there are no colonies there yet in Early Tech Start), or leave Mars as a goal the player has to engineer for? Jon's call.
- Should a ship that has both warp and a reaction drive pick warp on interplanetary legs (the plan's default), or let the player choose per order? A per-order toggle is more UI. The plan defaults to "warp first, Newtonian from the porkchop".
- Aerobraking and gravity assists are out. Say if gravity assists are wanted as a later plan; they need `EnterSOIProcessor` to handle a fly-by that leaves the sphere again, which it does today only via `ChangeSOIProcessor` for hyperbolas.
- Is a second mid-course correction wanted? One is planned. Two doubles the work for little accuracy gain unless the integrator drifts.
- Time acceleration: an eight-month transfer under the game's pulse needs `SoiTransferAction` to wake only at events (frame changes, the correction time), not every tick. `AddEntityInterupt` does that. Check at 1-day-per-step time compression that nothing is skipped.
- Plane: Early Tech Start Earth and Mars orbits are near-coplanar. A target with a large inclination needs the plane-change cost in the porkchop (plan 2).

## Out

Warp changes. `NewtonSimpleProcessor`. Low-thrust arcs. Moon-to-other-planet (composed by the planner from plan 1 and this). Gravity assists. Multi-leg tours. Refuel and tanker logistics. Anything about jump travel.

## Stop if

The iteration in slice 3 does not converge for Earth to Mars at the Hohmann phase with the engine's own sphere-exit conversion. Either the conic prediction and the processor disagree (fix the prediction to call the processor's functions) or the sphere radius functions disagree (plan 1's open question). Do not paper over it with a larger correction burn.
