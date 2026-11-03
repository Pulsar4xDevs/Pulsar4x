# Newtonian transfer into a body's sphere of influence

Status: in progress (2026-11-03). Slices 1, 2, 3, 4, 6, and 7 are in and tested. Slice 5 waits on the Lambert plan and is not part of this pass. The file stays until slice 5 is built too. Written 2026-11-03 for the Early Tech Start mod, where a ship has a reaction drive and no warp. Revised 2026-11-03 after Jon's answers (refuse a short tank and say so in the Fleet window, one sphere radius, the Starship start ship, ellipse exit detection).

Plan 1 of 3. Plan 2 ([Lambert and porkchop](movement-lambert-porkchop.md)) supplies the solver slice 5 needs. Plan 3 ([interplanetary](movement-interplanetary-newtonian.md)) builds on this one. Slices 1 and 2 are small, stand alone, and can ship first.

A ship in low Earth orbit cannot fly to Luna today without warp. `MovePlanner.EvaluateNewtonian` (`GameEngine/Movement/MoveToPlanners.cs`) refuses with `target body has a usable SOI; warp` (line ~581) whenever the target's own sphere contains the drop-in point. The Early Tech Start mod has no warp, so Luna is simply unreachable. This plan makes the first real Newtonian trip: leave a parking orbit, coast to the Moon's sphere, and capture into a low lunar orbit.

## Decided

- **Patched conics, one frame at a time.** The engine already switches frames. `OrbitDB.OnSetToEntity` (`Orbits/OrbitDB.cs` ~line 620) schedules `ChangeSOIProcessor` when `Eccentricity >= 1`, and `ScheduleSOIEntry` (~line 665) scans for a sibling's sphere and schedules `EnterSOIProcessor`. Both are in `Orbits/OrbitProcessor.cs` (lines 190 and 255). Both rebuild the orbit with `OrbitDB.FromVector` in the new parent's frame and publish `EventType.SOIChanged`. This plan relies on that and does not add a new integrator.
- **Do not queue the second burn up front.** `ChangeOrbitalAltitudeAction` and `MatchOrbitAction` store a whole burn list in one frame (`_burns`, `TryPlanBurns`). A cross-sphere trip cannot: the arrival burn is in the Moon's frame, which does not exist until `EnterSOIProcessor` fires. The new action plans the departure burn, coasts, and plans the arrival burn from the real state it finds. This is the same "emit the next step only" rule the warp-brake plan states for `AgentProcessor`.
- **Aim by impact parameter, not at the centre.** A conic aimed at the Moon's centre arrives with periapsis inside the body. The planner chooses an arrival periapsis `OrbitMath.LowOrbitRadius(target)` (1.1 body radii) and solves the hyperbola for the offset that gives it.
- **The capture burn is a circularise at periapsis.** At periapsis of the arrival hyperbola, `TryArrivalBurn` (`MoveToPlanners.cs` ~815) already knows how to price it. It falls back to the capture-ellipse rule from [movement-capture-ellipse](movement-capture-ellipse.md) if the tank is short: a bound orbit with apoapsis inside 90% of the sphere (`TryCaptureSpeed`, ~1129). One rule, not two.
- **Burn execution is unchanged.** `NewtonSimpleProcessor` fails a short burn whole and spends no fuel. The capture-ellipse plan says not to change it. This plan agrees. The planner must check the whole trip's Δv before the first burn, so a ship never commits to a departure it cannot finish.
- **A new action, not a change to `MatchOrbitAction`.** Working name `SoiTransferAction` beside the other Newtonian actions in `Movement/NewtonMove/`. It is `ActionLanes.Movement`, blocking, with a `Clone()` (see `MovementActionCloneTests`) and a `Rehydrate` that survives save and load. Its state is `Depart`, `Coast`, `Capture`.
- **A short tank is refused, not parked (Jon, 2026-11-03).** If the whole trip (departure plus capture to the wanted orbit) does not fit in `NewtonThrustAbilityDB.DeltaV`, the planner refuses before the first burn. The capture-ellipse fallback is not used to rescue a Newtonian trip for now. The refusal must be obvious in the Fleet window, and the window offers other orbits that do fit. Slice 7.
- **One sphere radius.** `Entity.GetSOI_m` and `OrbitMath.GetSOIRadius` are the same formula with different inputs, and `ChangeSOIProcessor` applies a further `0.9`. They are unified in slice 6, before anything in this plan relies on an exit or capture radius.
- **Starting ship.** The Early Tech Start mod starts with a Starship-like ship (`early-ship-design-starship`, `Starship I`), not the Newton Tug. Δv 13.6 km/s with an empty hold, so Earth to Luna (3.84 km/s) is easy and the refusal in slice 7 is tested by removing fuel, not by the start. See Numbers. The Newton Tug design stays in the mod as a short-tank example and is no longer in the start fleet.
- **Planner mode.** No new `MoveMode`. It is `NewtonianTransfer`, with a message like `Transfer to Luna, arrive <date>`.

## What is going wrong

- `EvaluateNewtonian` rejects `dropInParent == target`. For Earth to Luna the answer is a refusal with no warp offered, so the goal fails.
- It then rejects `parent != dropInParent || targetOrbit.Parent != dropInParent` with `target is under a different SOI parent; no interplanetary transfer maths yet` (~line 584). That also blocks leaving a Moon orbit for Earth. Plan 3 owns the heliocentric case. This plan owns one-level up and one-level down only (parent to child, child to parent).
- **Arbitrary state vectors.** It needs a ship `OrbitDB` (`ship is not in a stable orbit to transfer from`). It only transfers from a near-circular orbit, `MaxEccentricityForTransfer = 0.01`, and circularises first. A ship mid-burn, mid-warp, or on a leftover ellipse or hyperbola cannot plan from where it is. The source says `MISSING MATHS: deriving a transfer from an arbitrary state vector`. `TryCurrentRelativeState` (~1259) and `CirculariseAction.SampleAt` already give position and velocity; nothing consumes them for a transfer.
- **Phasing is unverified.** `OrbitalMath.OrbitPhasingManuvers` (`Pulsar4X.OrbitalMath/OrbitalMath.cs` ~1696) is called with `phaseAngle = NormaliseAngle(targetAngle - shipAngle)` (`MoveToPlanners.cs` ~608). A comment there says its sign convention is unverified. By hand, the formula looks right for a prograde circular orbit: target ahead gives a shorter phasing period, target behind a longer one. It has never been run against a retrograde or inclined orbit, and no test flies it.
- `HohmannOE` (~1751) takes `phase0` from two `% (2π)` true anomalies, so `phase0` lies in (−2π, 2π). `k` only repairs one wrap, so `tWait` can come out negative or longer than a synodic period. This is a suspicion, not a confirmed bug. Slice 1 settles it.
- **Jump goal row.** `FleetWindow.cs` line ~531 offers `Jump...` to every ship. Warp and grav-survey rows are gated (`WarpAbilityView`, `CanGravSurvey`); this one is not. The manual selectable (~662) only checks `!ShipHasCaptain`. On Early Tech Start the row is a dead end. The server projects a `JumpPointView` for a gate only once discovered (~947), so a ship with no warp sees `Jump...` with an empty target list.

## What to build

### 1. Verify phasing and `HohmannOE` before trusting them

Add `Pulsar4X.Tests/NewtonianTransferTests.cs`. Fly the maths, do not just call it.

- Circular Earth orbit, a second body (a buddy ship, as in `EarlyTechStartTests.NewtonianOnlyShip_IsOfferedNoWarpMove_AndCanHohmannWithinTheWell`) on the same radius at +30°, −30°, and +170°. Run `MatchOrbitAction.TryPlanBurns`, apply both burns with `NewtonSimpleProcessor.ProcessEntity`, propagate to the second burn, and assert the along-track separation ends under `AlongTrackArrivedSeparation`.
- Same with a retrograde buddy and a 5° inclined one. If retrograde fails, `AngleOf` (~1285) must use the sense of the orbit, not raw longitude.
- Hohmann between radii, target at many starting phases (0.1° to 359.9°, step 7°). Assert `tWait >= 0`, `tWait <= synodic period`, and the ship arrives within the arrival tolerance.
- Fix whatever fails, with the smallest change in `OrbitalMath.cs`. Keep the test even when nothing fails.

Acceptance: all phases pass, or the fix is in and they pass.

### 2. Hide the Jump goal row for ships that cannot jump

`FleetWindow.cs` ~531 and ~662. Gate both on a new ship and fleet flag. Pattern: `CanGravSurvey` in `Pulsar4X.Api/Snapshots.cs` (~997 and ~1105), set in `GameEngine/Api/GameProjector.cs` (~1854 for a fleet, ~1958 for a ship).

- Add `CanJump` next to `CanGravSurvey` on `ShipSnapshot` and `FleetSnapshot`. The projector fills it.
- The projector rule must match what `ShipJumpAction` actually requires. Read `JumpPoints/JumpOrder.cs` and `Movement/InterSystemJumpProcessor.cs` first. `JumpOrder` creates per-ship warp-then-jump commands, so a ship with no `WarpAbilityDB` can probably never reach a gate. If that is the whole rule, `CanJump` is the warp flag plus any jump component. If a jump drive component is needed, test for it.
- The Fleet window hides `Jump...` when the flag is false. A fleet shows it when any ship can.
- Tests: `EarlyTechStartTests` start-menu ship has `CanJump == false`; a base-mod ship with a warp drive has `CanJump == true`. Add to that class and assert on the snapshot `GameProjector` builds (`LunaConcordTests` and `ShipRefitTests` already call it).

### 3. State-free arrival maths: the hyperbola for a given periapsis

In `Pulsar4X.OrbitalMath/OrbitalMath.cs` (pure, no engine types). New functions, each with a unit test in `OrbitTests.cs`:

- `HyperbolaForPeriapsis(mu, vInf, rp)` returns `(e, a, aimingOffset b, turnAngle)`. Check by `KeplerFromPositionAndVelocity` round trip.
- `CaptureBurn(mu, vInf, rp)` returns Δv from the hyperbola's periapsis speed down to circular at `rp`.
- `EscapeBurn(mu, rPark, vInf)` returns Δv from circular at `rPark` up to the hyperbola leaving with `vInf`. Compare with `InterceptCalcs.InterPlanetaryHohmann`'s `dva`. That one stops at the sphere's radius with an elliptical burn, which is not what a leaving hyperbola needs.

Acceptance: for Earth to Luna, `vInf` at the Moon is about 0.83 km/s from a circular 384,400 km transfer apoapsis, and the capture Δv into 1.1 body radii is about 0.8 km/s. Those are hand numbers; the test fixes the real ones.

### 4. Earth to Luna, circular parking orbit, closed form

`EvaluateNewtonian`, replacing the `usable SOI; warp` refusal when `dropInParent == target` and the ship's parent is the target's orbit parent.

- Transfer ellipse: periapsis at the ship's radius, apoapsis at the Moon's orbit radius. Δv1 about 3.0 km/s from 7,000 km. Time of flight is half the period, about 5 days.
- Departure time: the ship's phase relative to the Moon at arrival, like `HohmannOE`, but aiming at the offset point from slice 3, so periapsis at the Moon is the target low orbit and not the centre. The wait is under one synodic period (the Moon's is about 29.5 days against a 90 minute orbit; the ship's own phase repeats every orbit, so the wait is mostly the Moon).
- `MoveOption` carries the total Δv (departure plus capture) and an ETA. Infeasible if the total exceeds `NewtonThrustAbilityDB.DeltaV`: `needs X m/s Δv, have Y m/s`, same words as today.
- `TryBuildMoveActions` returns `SoiTransferAction`. A captained ship goes through the capture-ellipse gate too, so a ship that could leave and not capture is refused before the first burn.
- `SoiTransferAction`:
  - `Depart`: apply the departure burn at its node time via `NewtonSimpleMoveDB`, as `ChangeOrbitalAltitudeAction.TryAdvanceBurn` does.
  - `Coast`: wait. `EnterSOIProcessor` swaps the orbit to Luna's frame. The action wakes on an orbit parent that equals the target. A time-out of 1.5 times the planned flight fails the action with `Missed the encounter with <target>`.
  - `Capture`: build the burn from the real state (`TryCurrentRelativeState` plus `TryArrivalBurn`) at periapsis. If periapsis is already behind, the capture is a retrograde burn at the current radius. If that is not possible, fail with the same Δv sentence as the capture-ellipse plan.
  - `Succeeded`: orbit circular at `LowOrbitRadius`, or elliptical with the capture-ellipse details line. The log keeps `Arrived at Luna.`
- Luna to Earth is the same maths run backwards (`ChangeSOIProcessor` handles the exit because the departure orbit is a hyperbola in Luna's frame). Build both directions in this slice, because the engine already owns the symmetric events.

Tests, in `NewtonianTransferTests.cs`, using the Early Tech Start start:
- The tug at Earth plans to Luna: `Feasible`, no `WarpMoveAction`, a `SoiTransferAction`.
- Run it forward for 30 game days on `game.TimePulse`. Assert the ship's `OrbitDB.Parent` is Luna, eccentricity under 0.05 (or the ellipse sentence), and fuel dropped by the Tsiolkovsky amount for the planned Δv within 5%.
- Luna back to Earth, same checks.
- A tug with a tank for 2 km/s refuses before burning: fuel unchanged, reason contains `Δv`.
- Save and load while coasting. `SoiTransferAction` resumes (the `Rehydrate` case).

### 5. Plan from an arbitrary state vector

Needs plan 2 slice 1 (the Lambert solver).

- Replace the `shipOrbit.Eccentricity > MaxEccentricityForTransfer` circularise-first rule with a solve from `TryCurrentRelativeState`. Pick a departure state `(r0, v0)` at the first moment the ship can burn (the sample instant `CirculariseAction.SampleAt` already picks). Pick the arrival state at the target at `t0 + TOF`. Solve Lambert for the transfer velocity. The burn is `v_transfer − v0`.
- A ship with no `OrbitDB` (mid-burn) is left alone: it must finish or abort the current burn first. `NewtonSimpleProcessor.Abort` already ends a burn cleanly. The refusal text becomes `finish the current burn first` instead of `not in a stable orbit`.
- Keep slice 4's closed form as the circular-orbit fast path and as a test oracle: Lambert from a circular state must match it to within 1%.
- A ship on a hyperbola about Earth after a warp leftover dump (`TryArrivalBurn` territory) plans a transfer to Luna directly, if the Δv allows. If not, the existing circularise path stays.

Tests: eccentric start (e = 0.3 about Earth) to Luna; a ship passing periapsis and a ship at apoapsis both plan; a hyperbolic leftover plans or refuses with a clear Δv sentence; the planned Δv is within 1% of closed form for a circular start.

### 6. One sphere radius, and detect a bound ellipse that leaves its sphere

Small, stand alone, and a prerequisite for slices 4 and 5. Two parts.

**A. One radius.** The formula is the same in both places, `GetSOI(sma, mass, parentMass) = |sma · (m/M)^0.4|` (`OrbitalMath.cs` ~1491). The inputs and the edge cases differ:

| Where | Inputs | At the root (the star) | Used by |
|---|---|---|---|
| `Entity.GetSOI_m` (`EntityExtensions.cs` ~62) | live `MassDry` and the live `OrbitDB` semi-major axis | `+Infinity` | `NewtonianMovementProcessor`, `InterceptCalcs`, `OrbitDB` (~149, 194, 706, 748), `OrbitProcessor` (~207, 280), `WarpMoveProcessor` (several), `GameProjector` (~340), `DebugWindow` |
| `OrbitMath.GetSOIRadius(OrbitDB)` (`OrbitMath.cs` ~491) | `_myMass` and `_parentMass` cached when the `OrbitDB` was attached | `NaN` | `OrbitDB.OnSetToEntity` (~631, 674: the exit time, and the entry scan), `MoveToPlanners.cs` (~928, 1135, 1153, 1167), `GameProjector` (~294) |
| `OrbitDB.SOI_m` (cached property, ~522) | same cache as above | `NaN` | `FindSOIForOrbit` |

The `0.9` appears in two more places and is a different thing: the stale-interrupt guard in `ChangeSOIProcessor` (`OrbitProcessor.cs` ~207: ignore the exit if the ship is inside `0.9 × GetSOI_m()`), and the capture rule (`MoveToPlanners.cs` ~928 and ~1153; `TryCaptureSpeed` puts the apoapsis at 90% of the sphere).

Consequence, by reading the code: for static bodies the two radii are numerically equal today, so no exit has been seen to go wrong. They can diverge in three ways. At the root one is `+Infinity` and the other `NaN`, and every comparison with `NaN` is false, so a missed exit looks like "no exit". The cached masses go stale if a body's mass or orbit is replaced without re-attaching its children. And the guard uses a different radius from the scheduler, so a ship that is scheduled to exit at the sphere is only ignored if it is inside 90% of it. That is fine for a hyperbola, which is always outside 90% when the exit fires, but it is a second number to keep in step.

Do this:
- Keep one function, `Entity.GetSOI_m`, with `+Infinity` at the root. Make `OrbitMath.GetSOIRadius` and `OrbitDB.SOI_m` call it (or delete them and update the callers listed above).
- Name the `0.9`: `SoiCaptureFraction` in one place, documented as a planning margin for capture rules, and not used for detection. The `ChangeSOIProcessor` stale guard compares to the sphere radius with a relative epsilon (1e-6), not to `0.9`.
- Test, `SoiRadiusTests.cs`: every body in the Sol system returns one radius from all three entry points; the root returns `+Infinity` from all; a ship's exit time uses that radius; an exit interrupt left over from a replaced orbit does nothing (`AddEntityInterupt` never cancels old interrupts, the processors guard themselves).

**B. An ellipse that crosses its sphere.** `OrbitDB.OnSetToEntity` (~620) schedules `ChangeSOIProcessor` only for `Eccentricity >= 1`.

- Does an orbit change mutate the `OrbitDB` or make a new one? It makes a new one. The element properties have `protected set`, and every change goes through `OrbitDB.FromKeplerElements`, `FromVector`, `FromPosition` or `FromVelocity` followed by `entity.SetDataBlob(...)`, which calls `OnSetToEntity`. So a check in `OnSetToEntity` sees every change, and nothing can alter an orbit behind its back.
- The cheap test is on the **apoapsis**, not the periapsis. A bound orbit leaves the sphere when it goes out past it, so the condition is `Eccentricity < 1 && Apoapsis > sphereRadius`. (A periapsis outside the sphere would mean the ship is already outside, which `FindSOIForOrbit` handles at creation.) `CalculateExtendedParameters` already caches `Apoapsis`. For `e >= 1` `Apoapsis` is negative, so the `e < 1` guard is required.
- When it is true, schedule the same `ChangeSOIProcessor` interrupt the hyperbola path uses, at `OrbitMath.TimeToRadius(this, sphereRadius)`. Check that `TimeToRadius` returns the first outward crossing for an ellipse (the code path exists, but no test calls it with `e < 1`). `ChangeSOIProcessor` already rebuilds the orbit from position and velocity in the parent's frame with `OrbitDB.FromVector`, so it should work unchanged. Test it.
- Capture ellipses from the capture-ellipse plan end at 0.9 of the sphere, so they never trigger this. A test holds that: a ship on a capture ellipse stays in its frame for a year.
- Tests, in `OrbitTests.cs` or a new `SoiExitTests.cs`: a ship burned to `e = 0.9` with an apoapsis at 1.2 sphere radii changes frame at the right time and ends with the new parent; at 0.95 sphere radii it does not; a retrograde and an inclined case; a hyperbola still works as before.
- The other direction, a sibling's sphere moving into the ship's ellipse, is the entry scan. It is not here. It is [plan 3](movement-interplanetary-newtonian.md) slice 1, because a heliocentric cruise is where it bites.

Acceptance: both parts tested, the radii agree, an ellipse that leaves is caught. Strike the "burn that leaves the parent's sphere is not detected" line in master-plan section 4.

### 7. Make a refusal obvious in the Fleet window, and offer other orbits

Where: `Pulsar4X.Client/Interface/Windows/FleetWindow.cs`, the **Orders** tab. It has three side-by-side panels: `Orders-Queue` (the queue), `IssueOrders-List` (the kinds of order, `DisplayOrderChoices`) and, on the right, `IssueOrdersDisplay` (~792), which holds the target tree for the chosen kind (`DisplayTargetTree`, ~1245). The right-hand panel is the "righthand column" Jon means.

Today a refusal is only the `goal.Message` text that `CommandTranslator.TranslateMoveToBody` (~1205) returns in `CommandResult.Reject`. The tree itself never shows it, and `MoveTo` calls `DisplayTargetTree` with no `blocked` callback. `DisplayTargetTree` already has one (`Func<EntitySnapshot, string?>? blocked`, drawn at ~1622) and the goods and bootstrap pickers use it. So the hook exists.

- **Per-target refusal in the tree.** For `MoveTo`, pass `blocked`. A refused row shows the reason in red in place of the button: `Needs 6.1 km/s, have 4.2 km/s` or `No warp drive, no route`. The reason is the planner's `MoveOption.Reason`, the same string the captain would see. Computing `MovePlanner.Evaluate` for every body each frame is too much, so compute it once per (ship, target, game day) when the row is first drawn, through the API (look at how `Pulsar4X.Api` exposes other read-side queries and use the same pattern), and cache it.
- **Selected-target panel under the tree.** Once a target is clicked, a small `Trip` block: mode, Δv needed and have, arrival date, and, if refused, the reason in red with the words `Refused, nothing was started`. The click that gets refused keeps the `CommandResult.Reject` text on screen until the next click (check that the window already keeps it; if not, add a red line that persists).
- **Other orbits that fit.** When the trip is refused for Δv, the same panel lists orbits the ship can reach with the Δv it has, each a button that issues the plot. For a capture the candidates are: a circular orbit at a larger radius (periapsis chosen so the capture Δv fits), and a bound ellipse with the apoapsis inside `SoiCaptureFraction` of the sphere (the capture-ellipse rule, `TryCaptureSpeed`). Three rows at most, each with Δv and the resulting periapsis and apoapsis. The rows are the player's choice. The captain never takes one on its own (the decision above).
- **The action takes a target orbit.** `SoiTransferAction` (slice 4) gets an optional `(periapsis, apoapsis)` for the arrival orbit. Default is the low circular orbit. Without this the offers cannot be issued.
- Tests: `ShortTank_PreviewListsOrbitsInsideTheTank`. A full tank is feasible and the alternative list is empty. Drained to 3 km/s the query refuses with the Δv sentence and the list is empty: the Earth departure burn alone is about 3.03 km/s, so no lunar arrival fits. Drained to 3.5 km/s (under the low circular trip, over the bound ellipse) the list is non-empty, each orbit is within the tank, and the cheapest one builds a `SoiTransferAction` with those radii. Earth to Mars is a route refusal and offers nothing. The ImGui part has no test harness: play-test it from the start screen with fuel removed.

Acceptance: remove fuel from `Starship I`, click Luna, see the red reason and the alternative orbits, click one, and the trip flies and ends in that orbit.

## Numbers (by hand, to be replaced by test output)

- Earth GM 3.986e14 m³/s². Parking radius 7,008 km (`LowOrbitRadius` = 1.1 × 6,371 km). Circular speed 7.54 km/s.
- Transfer perigee speed 10.57 km/s, Δv1 about 3.03 km/s. Apoapsis speed at the Moon's distance 0.19 km/s against the Moon's 1.02 km/s, so `vInf` about 0.83 km/s.
- Luna GM 4.905e12 m³/s², low orbit 1.1 × 1,737 km = 1,911 km. Hyperbolic periapsis speed 2.41 km/s, circular 1.60 km/s, capture about 0.81 km/s. Total about 3.84 km/s.
- Start ship, measured by `EarlyTechStartTests.StartingShip_IsStarshipLike_AndHasTheDeltaVForEarthToMars`: dry 28.5 t, 1,196.8 t methalox, three Raptors (ve 3,615 m/s, Isp about 369 s, 6.5 MN, 1,800 kg/s), Δv 13.6 km/s with an empty hold. With 100 t of cargo it is about 8.4 km/s. Earth to Luna and capture (3.84 km/s) leaves a large reserve, and so does the Earth to Mars round trip (about 11.2 km/s).
- The dry mass is light against the real Starship (about 100 t or more) because the game hulls it from thin steel armour. About 0.25 t per unit of armour thickness (12.75 gives 28.5 t, 100 gives 58 t). It can be thickened in `ships.json` if the Δv is too generous.
- Luna's sphere is about 66,000 km. `ScheduleSOIEntry` scans one orbital period in 180 steps, about 2.5 hours for this ellipse, which is fine at the Moon's relative speed. The scan is not fine at heliocentric scale. Plan 3 covers that.

## Open questions

- Fuel: answered. The start ship is the Starship, 13.6 km/s. Open only if Jon wants a harder start, in which case drain the tanks in `colonies.json` or thicken the hull.
- Free return and lunar flyby are out of scope. Say if they are wanted.
- Resolved by slice 6: the exit for `e < 1` (apoapsis test at orbit creation) and the two radii (one function, `0.9` named and kept out of detection).
- Resolved (Jon, 2026-11-03): a short tank is refused, not parked, and the Fleet window says so and offers other orbits (slice 7). Open: should the alternatives also be offered for a trip with enough Δv but a refused plan for another reason (no route)? The plan says no.

## Out

Warp changes. `NewtonSimpleProcessor`. Interplanetary legs (plan 3). The porkchop UI (plan 2). Low-thrust and continuous burn arcs. Aerobraking. Moons of moons. Any rework of `MatchOrbitAction`, other than sharing helpers.

## Stop if

Slice 1 shows `OrbitPhasingManuvers` or `HohmannOE` are wrong in a way that needs a rewrite. Report it before building slice 4; it stands on the same two functions' conventions.
