# Lambert solver, transfer windows and a porkchop plot

Status: not started. Do not build until asked. Written 2026-11-03 for the Early Tech Start mod.

Plan 2 of 3. Slices 1 to 3 are engine maths with no UI and no game-state change; they unblock slice 5 of [the SOI capture plan](movement-newtonian-soi-capture.md) and the heliocentric leg of [the interplanetary plan](movement-interplanetary-newtonian.md). Slices 4 and 5 are the NavWindow porkchop.

Jon asked for Hohmann manoeuvres that work better and porkchop plots. Today there is no way to ask "when should I leave?" The closed-form `Hohmann`, `HohmannOE` and `OrbitPhasingManuvers` in `Pulsar4X.OrbitalMath/OrbitalMath.cs` only answer for circular, coplanar orbits, and the waiting time comes from a phase formula, not a search. There is no Lambert solver, no transfer-window scan and no porkchop plot anywhere in the repo. `docs/design/api-layer.md` records that the old porkchop stubs were dropped.

## Decided

- **The maths is pure and lives in `Pulsar4X.OrbitalMath`.** New file `LambertSolver.cs` (namespace `Pulsar4X.Orbital`, with `Vector3`). No engine types. Both the engine planner and the client call it. The client already computes manoeuvres over snapshots this way: `Pulsar4X.Client/Api/SnapshotMoves.cs` mirrors `InterceptCalcs` with `EntitySnapshot.GetRelativeState(DateTime)` and `ResolveOrbit()`. No new `Pulsar4X.Api` command or query is needed for the plot.
- **Universal-variable Lambert.** Izzo or Gooding would be faster and handle more cases, but a universal-variable solver (Bate, Mueller and White; Curtis algorithm 5.2) is short, well known, and easy to test against textbook numbers. It covers elliptic, parabolic and hyperbolic transfers. Replace it later behind the same signature if speed matters.
- **Zero revolutions, prograde and retrograde.** Multi-revolution solutions are out. The porkchop grid is dominated by the zero-revolution family.
- **Two kinds of body state.** The scan takes two delegates, `Func<DateTime, (Vector3 pos, Vector3 vel)>` for origin and for target, both in the same parent frame. The engine side builds them from `MoveMath.GetRelativeState` / `OrbitDB.GetPosition` and `OrbitMath.InstantaneousOrbitalVelocityVector_m`. The client builds them from snapshots. The scan itself has no idea which.
- **Δv is the figure of merit, with the vehicle's budget as a hard line.** Cells are coloured by total Δv (departure plus arrival) and greyed out over the ship's `NewtonThrustAbilityDB.DeltaV`.
- **The plot only selects.** Clicking a cell hands departure date and time of flight to a planner. It does not issue burns itself. Burn issue goes through the existing `NewtonThrustCommand` (`Pulsar4X.Api`), the same path `NavWindow.SubmitBurn` uses.
- **Engine and UI share one result type.** `TransferWindow { DateTime Depart; DateTime Arrive; Vector3 DepartDeltaV; Vector3 ArriveDeltaV; double TotalDeltaV; double VInfDepart; double VInfArrive; }`.

## What is going wrong

- `OrbitalMath.HohmannOE` waits for the next window by phase arithmetic, with `k` repairing one wrap. It is untested (plan 1, slice 1 tests it). It cannot say "this launch is worse than the next one".
- `InterceptCalcs.InterPlanetaryHohmann` (`GameEngine/Movement/InterceptCalcs.cs` line ~23) and `SnapshotMoves.InterPlanetaryHohmann` (`Pulsar4X.Client/Api/SnapshotMoves.cs` ~157) are two copies of one approximation. The window is `rads / closinRads` (angle difference over relative mean motion). That is the time until the two bodies line up, not until they reach the Hohmann phase angle. The code computes `gpHommanAngle` and never uses it, and it contains `1/2*Math.Sqrt(2)`, which is integer division and evaluates to `0`. The escape burn `dva` is a transfer to the sphere's edge, not the hyperbolic-excess burn the heliocentric leg needs.
- `NavWindow` (`Pulsar4X.Client/Interface/Windows/NavWindow.cs`) has five modes (`NavMode`, line 84): manual thrust, Hohmann, interplanetary, phase change, escape SOI. The Hohmann mode has a TODO at ~line 518: `this should be radius from orbiting body not major axies`. Every mode picks one answer and shows numbers. None shows alternatives.
- Nothing in the repo can compute a transfer from state vectors, which is what a ship that isn't on a clean circle needs.

## What to build

### 1. The solver

`Pulsar4X.OrbitalMath/LambertSolver.cs`:

```
bool TrySolve(double mu, Vector3 r1, Vector3 r2, double tof, bool prograde, out Vector3 v1, out Vector3 v2)
```

- Universal variable `z`, Stumpff functions `C(z)` and `S(z)`, a bracketed Newton or bisection on time of flight. Reject `tof <= 0`, `|r1| == 0`, collinear 180° transfers (return false with the plane undefined; the caller nudges), and non-finite input. Never throw: the engine treats a NaN from closed forms as the only guard today (`MoveToPlanners.cs` ~640, `transfer solution did not converge`), so return false.
- Prograde is chosen from the sign of `(r1 × r2).Z` against the intended sense. The sim is mostly flat, but do not hard-code the plane.
- Document the `mu` convention: `OrbitMath.SGP(parent, ship)` (body plus ship mass), as every other transfer function in the engine uses.

Tests in `Pulsar4X.Tests/LambertTests.cs`:
- Curtis example 5.2 (r1, r2 about Earth, 1 hour) gives the textbook v1 and v2 to 1e-3 relative error.
- Round trip: pick a Kepler orbit, take two points a known time apart with `OrbitalMath.GetStateVectors`, solve, and assert the solved v1 equals the true velocity and `KeplerFromPositionAndVelocity` returns the same elements.
- The solver's Hohmann point: coplanar circular r1 and r2 at 180° with `tof = π√(a³/μ)` gives `|v1| − v_circ1` equal to `OrbitalMath.Hohmann` Δv1 within 1%. (The 180° singular case is why the test uses 179.9°.)
- Hyperbolic: a fast `tof` gives `Eccentricity > 1`.
- 1,000 random cases from the `OrbitFuzzTesting.cs` style never return NaN with `true`.

Acceptance: the five tests pass; `dotnet test --filter FullyQualifiedName~LambertTests`.

### 2. Transfer window scan

`Pulsar4X.OrbitalMath/TransferWindows.cs`:

```
TransferGrid Scan(double mu, Func<DateTime,(Vector3,Vector3)> origin, Func<DateTime,(Vector3,Vector3)> target,
                  DateTime departFrom, DateTime departTo, int departSteps,
                  double tofMin, double tofMax, int tofSteps,
                  Func<Vector3,Vector3,double> departCost, Func<Vector3,Vector3,double> arriveCost)
```

- For each `(t_d, tof)`: sample both bodies, `TrySolve`, then `vInfDepart = v1 − vOrigin`, `vInfArrive = v2 − vTarget`. The cost callbacks turn a v-infinity into a Δv. The default for a body-to-body transfer in one frame is `|vInf|` (no parking orbit); plan 1 slice 3 and plan 3 supply the escape and capture costs.
- `TransferGrid` holds the cost matrix, the axes, and a `Best` window plus `BestUnderBudget(double dv)`.
- Cells where `TrySolve` fails get `double.NaN`.
- Size: 120 × 120 solves is 14,400 and should run in well under a second. Cap the grid at 400 × 400 and say so in the doc comment.
- Departure range default: one synodic period of the pair (`1 / |1/T1 − 1/T2|`), found by `OrbitalMath.GetOrbitalPeriodInSeconds`. Time-of-flight range default: 0.5 to 1.5 times the Hohmann time.

An engine-side wrapper `GameEngine/Movement/TransferPlanner.cs` builds the two delegates from entities. It reads `OrbitDB` for bodies. For a ship it reads `TryCurrentRelativeState` and propagates the orbit.

Tests: the Hohmann-matching cell in an Earth-to-Mars scan (Early Tech Start's Sol system, or a hand-built two-planet fixture like `OrbitTests`) has a total Δv within 2% of the closed-form Hohmann, and its time of flight within 5% of π√(a³/μ). Successive best windows are one synodic period apart ±5%.

### 3. Make the planner use the scan

`MovePlanner.EvaluateNewtonian`'s `Hohmann to X` branch asks `TransferPlanner` for the best window within the budget and lets `HohmannOE` stay as the fast path and as a cross-check (assert both agree for the circular coplanar case). Nothing changes for the player on a circular orbit. For non-circular and non-coplanar transfers, the planner has an answer where it had none.

Add `MoveOption.Window` (the chosen `TransferWindow`) so the NavWindow and the log can say when the burn is.

### 4. The porkchop plot in NavWindow

`NavWindow.cs`: a sixth `NavMode.Porkchop`, a button `Transfer Windows` beside `Hohmann Transfer` (~line 216).

- Target selection: reuse `TargetBodyCombo` (used at ~line 517). Frame: the ship's SOI parent for same-sphere targets; plan 3 widens it.
- Controls: departure range (days), time-of-flight range (days), grid size, and a Δv ceiling slider with the ship's budget marked.
- Compute: run `TransferWindows.Scan` off the UI thread (`Task.Run`), show `Computing…`, and keep the last grid until the new one finishes. Never block `Display()`.
- Draw: ImGui draw list rectangles per cell (`ImGui.GetWindowDrawList().AddRectFilled`, already used by `SurveyProgressBar.cs` and `DisplayHelpers.cs`; there is no ImPlot in the client). Colour ramp from green (low Δv) to red, grey for NaN or over budget. Axes labelled with dates and days. Contours are optional, a 5% band around the best cell is enough.
- Interact: hover shows the cell's depart date, arrival date, Δv split and `vInf`. Left click selects. A marker shows the selection and the best cell.
- Actions: `Preview` puts `ManuverNode`s on the existing `_manuverLines.EditingNodes` (as `DisplayHohmannMode` does, ~line 540) so the orbit view draws the transfer. `Make it so` submits the burns with `SubmitBurn` and adds a `ManuverSequence` named `Transfer window`, like the Hohmann mode does.

### 5. Hand a chosen window to the planner

The NavWindow selection and the goal path must not disagree. Add `MovePlanner.PlanFromWindow(ship, target, window)` that builds the actions from a `TransferWindow` (plan 1's `SoiTransferAction` or plan 3's heliocentric action) so the plot, the Fleet window's `Move to ...` and an autonomous captain all use the same planner. The window is a hint: the planner re-solves at burn time from the real state and fails with the existing Δv sentence if the budget moved.

Tests: an `ActionQueue`-level test that the planned burns match the selected cell's Δv within 1%. The UI is checked by a play-test checklist in the PR, because there is no UI test project.

## Numbers

- Earth to Mars, circular coplanar: Hohmann 8.6 months (about 259 days), synodic period about 780 days, departure `vInf` about 2.9 km/s. The best cell in the scan should land near these.
- Earth to Luna: time of flight about 5 days (plan 1). The porkchop for the Moon is a different shape (short, steep), which is a good second test of the axis defaults.

## Open questions

- Is the Early Tech Start system Sol with the real planet orbits? Mars is used in `EarlyTechStartTests`. Confirm both bodies' `OrbitDB` are in the same plane (Luna is inclined 5°); Lambert handles it, the plot axes assume `tof` only.
- Multi-revolution solutions: a long wait for a cheaper orbit is a real tactic. Add later behind a `maxRevs` argument?
- ImGui draw lists are fine for a 120 × 120 grid. Is a texture upload wanted for finer grids? Only if the draw list is slow.
- Should an AI captain pick the minimum-Δv window or the earliest one that fits the budget? The plan assumes earliest-in-budget, since an agent that waits two years for 100 m/s is wrong more often than right. Jon's call.
- `SnapshotMoves.InterPlanetaryHohmann` and `InterceptCalcs.InterPlanetaryHohmann`: decided (Jon, 2026-11-03: fix or delete, either is fine). Delete and rewrite, in plan 3 slice 5. This plan does not touch them, and the porkchop replaces their NavWindow mode.
- The porkchop's Δv axis should show the ship's budget as a contour (the `Starship I` budget is 13.6 km/s, so on Early Tech Start almost the whole plot is affordable; a drained ship is the case that shows a limit). Warp stays the default interplanetary mode (plan 3); the plot is where a player chooses the Newtonian one.

## Out

Low-thrust spirals and finite burns (the engine fires impulsive burns through `NewtonSimpleProcessor`; a long burn is a separate problem). Gravity assists. Multi-leg tours. Rendering the transfer arc in the system map beyond what `ManuverNode` already draws. Changes to `NewtonSimpleProcessor`. Server API changes.

## Stop if

The solver fails the Hohmann cross-check by more than 2%, or the scan misses the known Earth-to-Mars window. A porkchop that points at a bad window is worse than none; fix the maths before any UI.
