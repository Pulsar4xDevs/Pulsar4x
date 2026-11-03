# Orders window: plan on click, not per row

Status: not started. Written 2026-11-03 after Jon reported that opening an asteroid list in a ship's Orders tab freezes the game. The cause is measured below. Slice 1 is a small fix for the freeze; slices 2 to 5 are the redesign Jon asked for. Slices 6 to 8 (added 2026-11-04) are the outstanding warp-order items, folded in so there is one list for the Orders and move work.

Related: [Warp brake](movement-warp-brake.md) (slice 8), [Newtonian SOI capture](movement-newtonian-soi-capture.md) slice 7 (the refusal and the other-orbit offers, which this plan moves from every row to the clicked row), [Lambert and porkchop](movement-lambert-porkchop.md), [interplanetary Newtonian](movement-interplanetary-newtonian.md).

## Decided

- **A destination is planned when it is clicked, not when it is drawn.** Drawing a row costs nothing the planner has to answer for. One click costs one `MovePlanner.Preview`.
- **Click selects, Execute orders.** A click on a row shows a preview. A separate `Execute` button sends the command. Today the row button is the order.
- **The preview is the whole answer.** Route legs, mode, Δv needed and held, departure, arrival, the refusal sentence when refused, and the other orbits that fit (slice 7 of the SOI plan). All of it comes from the one `TripPreview` the engine already builds.
- **Order options live next to Execute.** Target orbit (periapsis and apoapsis, or altitude with a circular default), arrival type, and mode (Auto, Warp, Newtonian). Changing an option replans the selected target only.
- **Rows show cheap facts only.** Anything on a row must be O(1) arithmetic over data the client already holds. No `OrbitDB` propagation, no Lambert, no `PreviewMove`.
- **Auto is the default mode, and it stays `MovePlanner.Select`.** Inside one sphere Newtonian wins; between planets warp wins when the ship has both. The player can force Warp or Newtonian, and the panel shows what the other mode would have done (slice 3). Nothing here changes the default choice.
- **Engine behaviour does not change** (slices 1 to 5; slices 6 to 8 name the exceptions). `MovePlanner.Preview`, `Evaluate` and `Select` keep their rules. 22 ms per plan is the price of three modes plus a scan, and a single click can pay it. This plan does not make the planner faster.

## What is going wrong

Measured on 2026-11-03 on `origin/DevBranch` (d73240f1), Early Tech Start, `Starship I` in low Earth orbit, headless, `dotnet test` Debug build, one thread, JIT warmed. The scratch test is `OrdersPreviewTimingScratch.cs` (not committed; it times `MovePlanner.Preview(ship, body, now)`, which is what `EngineGameServer.PreviewMove` runs).

| Rows | n | Mean per row | Median | p95 | Max | Total |
|---|---|---|---|---|---|---|
| Planets and moons (33, `non-asteroid`) | 33 | 8.5 ms | 0.02 ms | 22–26 ms | 29 ms | 281 ms |
| Asteroids and comets, the first 400 | 400 | 21.5 ms | 21.3 ms | 23 ms | 27–62 ms | 8.6 s |
| Asteroids and comets, all of them | 502 | 22.0 ms | 21.6 ms | 23.9 ms | 51 ms | 11.0 s |
| Same ship, tank drained to 2.9 km/s, first 100 asteroids (every row refused for Δv) | 100 | 21.5 ms | 21.4 ms | 22.8 ms | 25 ms | 2.2 s |

- Sol with this start has **502 asteroids and comets** and 33 other bodies with an orbit in the start system.
- Each asteroid costs about 22 ms. The planets that need a plan cost about 22 ms; the median is small because the ones in other spheres or with no route return at once. A refusal costs the same as a success.
- One expanded asteroid band is about 9 to 11 seconds of work on the UI thread, in one frame.

Where the cost is called from (`Pulsar4X.Client/Interface/Windows/FleetWindow.cs`):

- `IssueOrdersDisplay` (~797) runs every ImGui frame. For Move to it calls `DisplayTargetTree(..., blocked: e => MoveBlock(previewShip, e), ...)` (~884, the `blocked:` argument at 889).
- `DisplayTargetTree` (~1250) rebuilds the whole tree every frame: dictionaries, `FoldAsteroidBands`, and `SortNodes` (OrderBy and ThenBy over `OrbitSlotKm` and `NameOf`).
- `DisplayTargetNode` (~1582) calls `blocked?.Invoke(entity)` for **every row it draws** (~1627). `MoveBlock` (~2550) calls `CachedPreview`, which on a miss calls `client.PreviewMove(shipId, bodyId)`, and that goes `InProcessAdapter` to `EngineGameServer.PreviewMove` (~314) to `MovePlanner.Preview`.
- ImGui draws every child of an open tree node. There is no clipper, so an open band of 500 asteroids is 500 plans in one frame.
- `CachedPreview` (~2560) keys on `(ship, body, day)` and runs `if(movePreviewCache.Count > 400) movePreviewCache.Clear()`. An open band of more than 400 rows fills the cache, clears it, and starts over. **Every frame misses on most rows.** The game does not recover until the band is closed. That is the freeze, not a one-off hitch.
- The day in the key also throws the whole cache away every game day. At speed, an open Orders tab replans every visible row once a day: about 0.3 s for the planets and moons, seconds for a band.
- Asteroid bands are closed by default, which is why the freeze shows up only when someone opens one.

SOI plan slice 7 said "compute once per (ship, target, game day) when the row is first drawn, and cache it." That is the line that was built, and it does not survive 500 rows. It was written for a handful of planets and moons.

Also noticed, not measured: the goods and bootstrap pickers pass `blocked:` callbacks too (`Block(e, goodsDestId)` and friends, ~1751 to 1934). Slice 1 audits them.

## What to build

### 1. Stop the freeze (small, do first)

In `IssueOrdersDisplay`, stop passing the planning `blocked` callback for Move to. Rows still draw, and the click still issues the order as today, so the refusal comes back as the `CommandResult.Reject` text. The Trip block (`DisplayTrip`, ~2575) keeps planning, for the one selected target, through `CachedPreview`.

- Delete `if(movePreviewCache.Count > 400) movePreviewCache.Clear();` and replace it with a cap that evicts the oldest entries, not everything. The cache holds only the selected target and the last few.
- Audit the other `blocked:` callbacks (goods, bootstrap). If any of them reaches the engine per row, treat it the same way.
- Test: a headless test with a spy `IGameClient` that counts `PreviewMove` calls while the tree-building logic runs over 500 rows and asserts the count is 0 (see slice 2 for the extraction that makes this testable).

Acceptance: open the asteroid band with 500 rows on Early Tech Start; the frame time is the tree sort and draw only.

### 2. Cheap row hints, no planner

Extract the row facts from `FleetWindow` into a plain class, `MoveTargetHints` (`Pulsar4X.Client`, no ImGui), so it can be tested.

- Per row, from the snapshots the client already holds: same sphere as the ship, other system, known or unknown jump route, has an orbit, is the ship's current parent. Output is one of: `Here`, `Same sphere`, `Other sphere`, `Other system`, `No route`. The row shows it as a short grey tag.
- Optional: a lower-bound Δv `~` from vis-viva at the two orbit radii, one multiply and a square root per row, greyed and prefixed `~` so it is not mistaken for the plan. Build only if the tags are not enough.
- Nothing here calls `PreviewMove` or `OrbitMath` propagation.
- Tests: `MoveTargetHints` over a 500-row fixture returns in under 5 ms and never touches the client's `PreviewMove` (spy).

### 3. Click selects, a preview panel plans

- Clicking a row sets `tripTargetId` and starts one preview. The panel under the tree shows `Planning…` until it returns.
- First version: call `PreviewMove` synchronously on the click. It is 22 ms. Do not make it async until measured on a real map with the server on another thread; the `InProcessAdapter` runs on the UI thread today and `Pulsar4X.Api` may hand the query to the game thread over a network client. If it is slow there, run it on a task and show `Planning…`.
- The panel shows what `TripPreview` carries today: `Mode`, `DeltaV` and `Have`, departure (`DepartInSeconds`), arrival (`EtaSeconds`), and `Reason` in red when refused with the words `Refused, nothing was started`. It adds the route as legs (escape, cruise, capture, or warp) once `TripPreview` carries them (slice 5).
- **The other mode, for free.** `MovePlanner.Preview` already calls `Evaluate`, which answers every mode (Already there, Newtonian, Warp), and then throws away all but the chosen one. Keep them: `TripPreview` gains `OtherModes`, a short list of `(Mode, Feasible, EtaSeconds, DeltaV, Reason)` for the modes `Select` did not pick. The panel shows one line under the chosen mode, for example `Warp would arrive 3 h 10 min sooner` or `Newtonian would need 1,240 m/s, arrive in 6 d`. No extra planning cost; the numbers were computed already. A faster option is only a hint. Choosing it sets the Mode option (slice 4) and replans; it never sends anything.
- **Other orbits that fit.** For a Δv refusal the panel lists `Alternatives` as slice 7 of the SOI plan asked (three rows at most, each with Δv, periapsis and apoapsis). Choosing one fills the order options (slice 4); it does not send anything.
- Cache: one entry per selected target and option set, keyed with the game day as today. Replanning happens on click, on an option change, or when the day changes and the selection is still open. A day change replans one target, not a band.
- Tests: selecting a target makes exactly one `PreviewMove`; changing an option makes exactly one more; closing the panel makes none.

### 4. Execute button and order options

- `Execute` is enabled only when the preview is feasible. It sends the command with the options in the panel. The row button goes away for Move to.
- Options, all optional and defaulting to what happens today:
  - **Mode**: Auto (default), Warp, Newtonian. Auto is `MovePlanner.Select` as it is. Warp and Newtonian force one mode; if the forced mode is refused, the panel says so and Execute stays off. A ship with no warp drive shows Warp disabled with the words `no warp drive`; a ship with no reaction drive shows Newtonian disabled the same way. The choice is per order, not remembered.
  - **Target orbit**: low circular (default), a circular orbit at a chosen altitude, or periapsis and apoapsis. `SubmitMove` already sends a periapsis and apoapsis in `MoveToBodyCommand` and `GoToBodyCommand`; the panel reuses that path. SOI plan slice 7 gives `SoiTransferAction` an optional arrival orbit; this slice fills it from the panel.
  - **Arrival type**: orbit (default), flyby, or stop at the target. Only orbit exists in the engine today. Show the others disabled with the words `not built`, so the room is visible but nothing is promised.
- `PreviewMove` takes the options: `PreviewMove(shipId, bodyId, MoveOrderOptions options)`, with an options record that has `Mode`, `Periapsis`, `Apoapsis`, `Arrival`. The old two-argument call stays as the defaults. `MovePlanner.Preview` already has a mode and an arrival radius inside it; this exposes them and does not add a planner.
- Tests: `Preview_ForcedWarp_DoesNotPickNewtonian`, `Preview_WithApoapsis_ReportsTheSmallerDeltaV`, and `Execute_SendsTheOptionsThePanelShows` for the command translator.

Acceptance: remove fuel from `Starship I`, click Luna, see the red reason and the other orbits, click one, see the options fill in and the preview turn feasible, click Execute, and the trip flies and ends in that orbit.

### 5. Carry the route in `TripPreview`

`TripPreview` has a single `Mode` string today. For a plan with an escape burn, a cruise, a mid-course and a capture it should hold a short list of legs `(kind, Δv, start, end)` so the panel can draw them. The engine already has them inside `MoveOption` and `SoiTransferAction`; this copies them out. Do after slice 4. Not needed to ship slices 1 to 4.

### 6. Jump… row: only for ships that can jump

Most of this is built. `d007f2cd` added the `CanJump` flag to `ShipSnapshot` and `FleetSnapshot` (`GameProjector`: the hull or any child has `WarpAbilityDB`), and `FleetWindow` already hides the Goals row (`if (jump)`, ~535) and the manual-plot row (`CanJump: true`, ~667) for ships that cannot. Early Tech Start ships have no warp drive and `EarlyTechStartTests` asserts `CanJump` is false for them. What is left:

- **The engine does not check.** `CommandTranslator.TranslateJump` rejects an undiscovered point and a captainless ship, but not a ship with no `WarpAbilityDB`. An API or AI caller, or a stale selection, can still send the order; `JumpRoute` fails it later with its own text. Reject at the translator with `This ship has no warp drive, so it cannot jump.` (and for a fleet, no ship in it can). Test: `Translator_RejectsJumpFromAShipWithoutWarp`.
- **Stale selection.** Selecting Jump and then clicking a ship that cannot jump already falls back to Move to (~836, the `shipJump` test). Keep that rule when slice 4 moves the row buttons.
- Same hiding rule for the new Mode option in slice 4 (Warp disabled with no warp drive).

Small. Can ship with slice 1.

### 7. The five Phobos `WarpMoveTests` failures: a decision

**What happens.** After the Lambert/patched-conic work, a warp to Phobos drops the ship into Mars's well about 13 km from where Phobos will be, on a leftover hyperbola. The agent replans from there. Before, the planner circularised at the drop-in radius (which is already Phobos's orbit) and was done in one action. Now `EvaluateNewtonian` takes the Lambert branch first: the hyperbolic leftover makes `closedForm` false (`needsCircularise`), `TryLambertWindow` finds a "Transfer window" to Phobos, and it returns that before the old `Circularise to match` fallback is reached (`MoveToPlanners.cs`, the `if (!closedForm)` block). So a ship that warped to within kilometres of Phobos plans a roughly six hour phasing transfer to it.

The five failing tests (all in `WarpMoveTests`) show the result, none of them is flaky:

| Test | Fails because |
|---|---|
| `MoveToPlan_HyperbolicAroundMarsNearPhobos_MatchesPhobosOrbit` | Plan is 2 actions (a transfer), test wants 1 `CirculariseAction` |
| `CircularPrograde-Phobos`, `CircularRetrograde-Phobos`, `EccentricPrograde-Phobos` | After the drop-in the goal is `Transfer to <Phobos>, depart/arrive ~6 h` with `NewtonThrustAction` running and no `OrbitDB`; the test wants an orbit or a circularise move |
| `WarpOrder_EarthToPhobos_SurvivesOneLongTickPastArrival` | Goal fails: `Newtonian needs 3,804 m/s, have 0 m/s`; the ship stays at e = 0.72 in Mars's well. The old path queued the circularise burn even with a short tank |

**Options.**

1. **Planner gate: already there, so circularise (recommended).** If the ship is in the same sphere as the target and within the drop-in offset of it (the distance `PlannedWarpExitOffsetLength(target)` plus a small margin, or equivalently the ship's radius is within a percent of the target's semi-major axis), skip the Lambert scan and take the existing `Circularise to match` branch, with the existing "a short tank still queues the burn" rule. After the circularise the goal replans, sees a near-circular orbit, and finishes through `AlreadyThere` or a short Hohmann phase. The five tests pass unchanged. It matches what the warp was for: the warp already did the transfer, so planning another is the wrong default. Cost: one condition and one test for the boundary (just inside and just outside the gate); the gate is a distance, so the Newtonian trip from far away is untouched.
2. **Change the tests to the new behaviour.** Assert that the goal is a Lambert "Transfer window" and that the ship ends in Phobos's orbit after the burns. Cheap, but it blesses a six hour phasing leg after a warp that ended 13 km away, and the no-fuel test would have to be rewritten to assert a failed goal, which is a worse player outcome than the old queued circularise.
3. **Make the warp arrive circular at the target.** Nothing to replan. A bigger change to `WarpMoveProcessor`'s drop-in and an engine behaviour change that slices 1 to 5 did not need; park it.

**Recommend option 1.** Do it as its own small PR, not in the Orders window PRs: it touches `MoveToPlanners.cs` only, and the five tests are the acceptance. Jon's call before it starts. Related review note: `MoveToPlanners.cs` is already very large; put the gate in a named helper (`ArrivedByWarp`), not inline.

### 8. Bootstrap warp-brake test

The warp-brake plan says the Bootstrap gap is fixed: `goal` is passed to `MoveGoodsPlan.TryMoveIntoRange` so the planner can use the skilled warp-brake (`EmitBrakeWarp`, `goal.BrakeSteps`). `colony-bootstrap-and-shipments.md` says "both call sites". Today there are three in `BootstrapColonyPlan` (`PlanPackage`, `PlanLoad`, and the main destination move), and nothing tests any of them: `WarpBrakeTests` only drives `TryBuildMoveActions` with a `MoveTo` goal, and `BootstrapColonyTests` has no warp case. A caller that drops `goal` again would silently lose the brake.

Add `Bootstrap_FlyingToAFarColony_UsesTheWarpBrake` (in `BootstrapColonyTests`, reusing the `CaptureEllipseTests` fixture helper `EarthShipAimedAtMars(1, captained: true)` that `WarpBrakeTests` uses):

- A Nav 25 captain on a ship with a Construction Bay and a hold, a source colony at Earth and a destination colony at Mars, crates loaded.
- Run `BootstrapColonyPlan.Plan`. Assert the move it queues is a warp (`WarpMoveAction`) and that `goal.BrakeSteps` is 3 after the first leg. This is what proves `goal` reached the planner.
- Same setup with a Nav 0 captain: the move is still planned, `BrakeSteps` stays 0 (low-Nav stays on plan 1).
- Same assertion for the package path (a fleet Bootstrap package, `goal.CargoId` set) and the load path, so all three call sites are covered.

Acceptance: delete `goal` from one of the three `TryMoveIntoRange` calls and a test fails. Update the "both call sites" sentence in `colony-bootstrap-and-shipments.md` to say three, in the same commit.

## Numbers

- 22 ms per plan, 502 asteroids: 11 s a frame if the cache clears; with a perfect cache the first draw of an open band is still 11 s once.
- With plan-on-click: 1 plan per click, 22 ms. A row draw is the tree sort and the tag, a few hundred microseconds for 500 rows (to be measured in slice 1).

## Open questions

- Should a band that is open also show the hint tag on every asteroid, or just the band's summary (count, nearest)? 500 identical tags are noise. Jon's call.
- Should Execute stay on a day-change replan that turns feasible into refused? The plan says the panel shows the new answer and turns Execute off, with a note.
- Is the preview worth running off the UI thread at all? 22 ms is one dropped frame. Measure in the real client first.
- Do the goods and bootstrap pickers need the same treatment? Slice 1 audits; no measurement yet.
- Slice 7: option 1, 2 or 3? Recommended: 1, as its own PR.
- Slice 3: should the other-mode line also show the warp sustain cost, or only time and Δv? Time and Δv are free; sustain needs the plan 2 bill.
- Timing was a Debug build. A Release build will be faster, but a band of 500 will still be seconds.

## Out

Making `MovePlanner.Preview` faster. Changing `MovePlanner.Select`. New arrival types in the engine. Warp changes, except the Phobos gate in slice 7 if Jon picks option 1 (or 3). A porkchop inside the Orders tab (the NavWindow has it).

## Stop if

Slice 1 shows the freeze is in the tree sort or draw and not in the preview. Then the numbers above are not the whole cause and the plan needs a second measurement before slices 2 to 4.
