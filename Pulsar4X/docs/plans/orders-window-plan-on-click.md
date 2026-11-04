# Orders window: plan on click, not per row

Status: built, not played (2026-11-04). Click-to-plan, the preview, Execute, departure timing, the porkchop, the jump check, and the bootstrap brake test are in. The Phobos flights do not arrive: the goal completes when the departure burn finishes and the arrival burn is dropped. That is an open bug. Written 2026-11-03 after Jon reported that opening an asteroid list in a ship's Orders tab freezes the game. The cause is measured below. Slice 1 is a small fix for the freeze; slices 2 to 5 are the redesign Jon asked for. Slices 6 to 8 (added 2026-11-04) are the outstanding warp-order items, folded in so there is one list for the Orders and move work.

Related: [Warp brake](movement-warp-brake.md) (slice 8), [Newtonian SOI capture](movement-newtonian-soi-capture.md) slice 7 (the refusal and the other-orbit offers, which this plan moves from every row to the clicked row), [Lambert and porkchop](movement-lambert-porkchop.md), [interplanetary Newtonian](movement-interplanetary-newtonian.md).

## Decided

- **A destination is planned when it is clicked, not when it is drawn.** Drawing a row costs nothing the planner has to answer for. One click costs one `MovePlanner.Preview`.
- **Click selects, Execute orders.** A click on a row shows a preview. A separate `Execute` button sends the command. Today the row button is the order.
- **The preview is the whole answer.** Route legs, mode, Δv needed and held, departure, arrival, the refusal sentence when refused, and the other orbits that fit (slice 7 of the SOI plan). All of it comes from the one `TripPreview` the engine already builds.
- **Order options live next to Execute.** Target orbit (periapsis and apoapsis, or altitude with a circular default), arrival type, and mode (Auto, Warp, Newtonian). Changing an option replans the selected target only.
- **Rows show cheap facts only.** Anything on a row must be O(1) arithmetic over data the client already holds. No `OrbitDB` propagation, no Lambert, no `PreviewMove`.
- **Auto is the default mode, and it stays `MovePlanner.Select`.** Inside one sphere Newtonian wins; between planets warp wins when the ship has both. The player can force Warp or Newtonian, and the panel shows what the other mode would have done (slice 3). Nothing here changes the default choice.
- **Departure timing is an order option.** `Best window` (default: least Δv within a wait cap, 10% tolerance), `Depart now`, or `Pick on porkchop`. The preview shows the wait, departure, arrival, Δv and fuel for the chosen timing and the alternative. Warp has no window, so timing applies to Newtonian plans, including the local transfer a warp ends with (slice 7).
- **Engine behaviour does not change** (slices 1 to 5; slice 7 names the exception: the default window rule). `MovePlanner.Preview`, `Evaluate` and `Select` keep their rules. 22 ms per plan is the price of three modes plus a scan, and a single click can pay it. This plan does not make the planner faster.

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
- **Timing rows.** For a Newtonian plan the panel shows the chosen timing and its alternative: wait, depart, arrive, Δv, fuel (slice 7c). From the same scan, so no extra planning.
- **Other orbits that fit.** For a Δv refusal the panel lists `Alternatives` as slice 7 of the SOI plan asked (three rows at most, each with Δv, periapsis and apoapsis). Choosing one fills the order options (slice 4); it does not send anything.
- Cache: one entry per selected target and option set, keyed with the game day as today. Replanning happens on click, on an option change, or when the day changes and the selection is still open. A day change replans one target, not a band.
- Tests: selecting a target makes exactly one `PreviewMove`; changing an option makes exactly one more; closing the panel makes none.

### 4. Execute button and order options

- `Execute` is enabled only when the preview is feasible. It sends the command with the options in the panel. The row button goes away for Move to.
- Options, all optional and defaulting to what happens today:
  - **Mode**: Auto (default), Warp, Newtonian. Auto is `MovePlanner.Select` as it is. Warp and Newtonian force one mode; if the forced mode is refused, the panel says so and Execute stays off. A ship with no warp drive shows Warp disabled with the words `no warp drive`; a ship with no reaction drive shows Newtonian disabled the same way. The choice is per order, not remembered.
  - **Timing**: Best window (default), Depart now, Pick on porkchop. Live only when the plan is Newtonian; with a warp it is disabled with `warp leaves now`. The Pick option shows the porkchop in the panel (slice 7b). Fields `Timing` and `PickedWindow` join `MoveOrderOptions`.
  - **Target orbit**: low circular (default), a circular orbit at a chosen altitude, or periapsis and apoapsis. `SubmitMove` already sends a periapsis and apoapsis in `MoveToBodyCommand` and `GoToBodyCommand`; the panel reuses that path. SOI plan slice 7 gives `SoiTransferAction` an optional arrival orbit; this slice fills it from the panel.
  - **Arrival type**: orbit (default), flyby, or stop at the target. Only orbit exists in the engine today. Show the others disabled with the words `not built`, so the room is visible but nothing is promised.
- `PreviewMove` takes the options: `PreviewMove(shipId, bodyId, MoveOrderOptions options)`, with an options record that has `Mode`, `Timing`, `PickedWindow`, `Periapsis`, `Apoapsis`, `Arrival`. The old two-argument call stays as the defaults. `MovePlanner.Preview` already has a mode and an arrival radius inside it; this exposes them and does not add a planner.
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

### 7. Departure timing: wait for a window, or go now (decided), and the five Phobos tests

**Decided (Jon, 2026-11-04).** The five Phobos `WarpMoveTests` failures are a gameplay choice, not a bug. With the new solver a ship waits for a convenient window to transfer instead of going now and spending more fuel circularising at the destination. That stays. What was missing is that the player has no say, and no way to see what waiting costs. So the order gets a **departure timing** option with a sane default, shown in the preview, inside the flow select destination → calculate → Execute. The planner gate in the earlier draft of this slice (circularise when already at the target) is dropped.

**What happens today.** After a warp to Phobos the ship drops into Mars's well about 13 km from where Phobos will be, on a leftover hyperbola. `EvaluateNewtonian` takes the Lambert branch first (`closedForm` is false because the leftover needs circularising), `TryLambertWindow` finds a "Transfer window" to Phobos, and that is returned before the old `Circularise to match` fallback (`MoveToPlanners.cs`, the `if (!closedForm)` block). The window is chosen by `TransferPlanner.TryAffordableWindow`: a 32 × 20 `TransferWindows.Scan` from now over one synodic period (capped at four years), then `TransferGrid.TryBestUnderBudget`, which takes the **earliest** departure the tank can afford. So today's rule is "earliest affordable", and it is not shown to the player.

#### 7a. The timing option and the Best window rule

`MoveOrderOptions` (slice 4) gains `Timing`: `BestWindow` (default), `DepartNow`, `PickOnPorkchop`. A fourth field, `PickedWindow` (`TransferWindow?`), carries the cell for `PickOnPorkchop`. The option rides on `MoveToBodyCommand` and on the `Goal` (`Goal.Timing`, `Goal.PickedWindow`), because the agent replans after a warp drop-in and must plan the same way.

- **Best window (default).** Candidates are the scan cells the tank can afford (`TotalDeltaV <= budget`, the same budget test as today) whose departure is within the **wait cap**. Let `dvMin` be the least `TotalDeltaV` among them. Take the **earliest departure whose `TotalDeltaV <= dvMin × 1.10`**. Ties at that date take the cheaper cell, then the shorter flight, as `TryBestUnderBudget` does now.
  - **Why Δv first.** Fuel is the scarce thing and is hauled around the whole game; waiting costs only time, and a parked ship is not doing anything the player has asked for. So the rule minimises Δv, and does not minimise arrival time.
  - **Why the 10% band.** The porkchop floor is flat near the best window. Waiting a day or a week for the last 2% is a bad trade. A 10% band takes the first window that is near best. Make both numbers constants (`BestWindowDvTolerance = 0.10`, `BestWindowWaitCapHohmannFlights = 2`) so the tests and Jon can move them.
  - **The wait cap.** `2 ×` the Hohmann flight time between the two radii (the centre of `TransferWindows.HohmannTofBand`), and never more than one synodic period (waiting past one period cannot find a cheaper window; the scan span already stops there). The cap scales with the trip: a six hour hop waits at most about a day, an Earth to Mars Newtonian trip can wait most of a synodic period. If nothing affordable is inside the cap, widen to the whole scan span, take the earliest affordable cell as today, and the preview says `No window inside the wait cap; earliest the tank can fly is in <time>`.
  - **Where.** A new `TransferGrid.TryBestWindow(budget, maxWaitSeconds, tolerance, out window)` next to `TryBestUnderBudget`; `TransferPlanner.TryAffordableWindow` takes the options and calls it. `TryBestUnderBudget` stays (it is the widen fallback).
- **Depart now.** The cheapest affordable cell in the first departure column (departure = now). If the first column has none, the earliest affordable cell, and the preview says `Cannot leave now; earliest is in <time>`. This is the old "go now and circularise at the end" for the local case.
- **Pick on porkchop.** The player clicks a cell (7b). `PickedWindow` goes to the planner through `PlanFromWindow` (it exists, and `TransferFromWindowCommand` already carries a window to `CommandTranslator`). If the cell is no longer affordable at Execute, the preview turns red with the Δv sentence; nothing is sent.
- **Warp vs Newtonian.** Timing is a Newtonian concept. A warp leaves now and has no window, so:
  - **Auto** is still `MovePlanner.Select` (no change to the default mode, see Decided). If `Select` picks warp, the Timing control is disabled with the words `warp leaves now`, and the preview shows the warp trip only. If `Select` picks Newtonian, Timing is live and defaults to Best window.
  - **Forced Newtonian**: Timing is live, default Best window. **Forced Warp**: Timing disabled.
  - **After a warp.** The drop-in leaves the ship near the target on a leftover orbit, and the agent replans from there as a Newtonian trip. That plan uses the order's timing, so the default is also what a warp to Phobos does at the end: wait for the window, transfer, arrive matching Phobos's orbit. The preview for a warp trip says so in one grey line (`After the warp, a local transfer is planned on arrival`). It does not promise a number, because the leftover orbit is not known until the drop-in.
  - **When both modes can fly the trip**, the existing "other mode" line (slice 3) carries the comparison: for example `Warp leaves now, arrives in 3 h 10 min, 0 m/s` against the Newtonian wait, ETA and Δv.
- **Engine behaviour change (named).** The default planning rule goes from earliest-affordable to Best window. For most trips the two agree or differ by hours. It is the reason the five Phobos tests change (7d).

Tests: `BestWindow_WaitsForTheCheaperWindowInsideTheCap`, `BestWindow_TakesTheEarlierWindowInsideTheTolerance` (two windows 6% apart in Δv: takes the earlier), `BestWindow_NothingInsideTheCap_WidensAndSaysSo`, `DepartNow_TakesTheFirstColumn`, `DepartNow_NoWindowNow_SaysEarliestIs`, `Timing_IsIgnoredForWarp`, `Goal_KeepsTheTimingAcrossAWarpDropIn` (the replan after drop-in uses the order's timing, not the default).

#### 7b. The porkchop in the preview

The NavWindow already has the whole thing: an async `TransferWindows.Scan` (`StartPorkchop`, `TakePorkchopResult`), the heatmap and hover (`DrawPorkchop`), the best-cell mark (`MarkPorkchopBest`), and a cell selection (`_porkSelectI/J`), committed through `TransferFromWindowCommand`. Do not copy it into the Orders tab.

- Extract it into `PorkchopPanel` (`Pulsar4X.Client`, ImGui, no NavWindow state): `Start(shipKe, targetKe, axes)`, `Draw(size)`, `Selected` (the `TransferWindow?`), `Best`. `NavWindow` switches to it so there is one implementation. The Δv/axis helpers (`SuggestPorkchopAxes`, the cost delegates) move with it.
- The Orders preview shows it **only** when Timing is `PickOnPorkchop`. The scan is **not** run on a row click or on Best window. It starts when the player chooses Pick, on a task, with `Scanning…` shown; so the 22 ms click budget of slice 3 is untouched. One scan per (ship, target, game day); a day change marks it stale and rescans on the next draw.
- The heatmap marks two cells: the Best window cell (the default rule) and the Depart-now cell, and the picked cell is outlined. Clicking a cell sets `PickedWindow`, replans through `PlanFromWindow`, and updates the preview rows (7c). It does not send anything.
- Only for trips the scan can answer: a Newtonian transfer inside one parent or a one-level patched-conic transfer. For anything else the Pick option is disabled with the reason.
- Tests: `PorkchopPanel_PicksTheCellUnderTheMouse` (pure index maths, extract it so it can be tested without ImGui), `Preview_PickOnPorkchop_UsesTheChosenWindow`, `ClickingARow_DoesNotStartAScan` (spy on `TransferWindows.Scan`).

#### 7c. What the preview shows

Under the mode line, a two-row table with the **chosen** option and the **alternative** (the next best timing: for Best window the alternative is Depart now, for Depart now it is Best window, for Pick it is Best window). `TripPreview` gains `Timing` (the chosen kind), `WaitSeconds`, and `TimingOptions`, a short list of `(Kind, WaitSeconds, EtaSeconds, DeltaV, FuelKg, Feasible, Reason)`; it is the same list that feeds slice 3's other-mode line, so no new planning cost: the scan runs once and the cells are already priced.

```
              Wait      Depart            Arrive    Δv       Fuel
Best window   5 h 40    Nov 4 18:10       +6 h 20   212 m/s  41 kg   ← will fly this
Depart now    0         now               +1 h 05   340 m/s  66 kg
```

A line under it says `Best window saves 128 m/s (38%) for a 5 h 40 wait`. Fuel comes from the rocket equation on the ship's current mass and exhaust speed, using what `ThrustView` and `NewtonThrustAbilityDB` hold; if a number cannot be computed, show Δv only. `Execute` sends the order with the chosen timing. The order is the one the preview showed: the Execute handler passes the same `MoveOrderOptions`, so a changed option replans first.

#### 7d. Rewrite the five Phobos tests (`WarpMoveTests.cs`)

Do this **after 7a**, in its own PR. No planner change belongs in it. If a test exposes a real bug, stop and report it. Keep each test's intent; change what it expects to the new behaviour (a ship that waits for a transfer window and arrives matching Phobos's orbit).

Common helpers to add in the test class: `AssertArrivedMatchingPhobos(ship, mars, phobos)` (parent is Mars; `Eccentricity < 0.05`; `SemiMajorAxis` within 1% of Phobos's; separation along the orbit within `AlongTrackArrivedSeparation`), and `GoNowDeltaV(ship, phobos)` (previews with `Timing = DepartNow` and returns `DeltaV`).

| Test | Intent (keep) | New assertions |
|---|---|---|
| `MoveToPlan_HyperbolicAroundMarsNearPhobos_MatchesPhobosOrbit` | A ship at Phobos's distance on a leftover hyperbola ends up matching Phobos's orbit and does not warp. | Replace `Assert.AreEqual(1, actions.Count)` and `IsInstanceOf<CirculariseAction>` with: no `WarpMoveAction`; the first action is the transfer window burn (`NewtonThrustAction` named `Transfer window`, or `SoiTransferAction`, whichever `MovePlanner` returns); `Preview(...).Timing == BestWindow`; `WaitSeconds >= 0`. Then run the burns to arrival and call `AssertArrivedMatchingPhobos`. Delete the `CirculariseAction.TryCompute` block (a circularise is no longer the plan). Keep `Assert.DoesNotThrow` for constructing each planned action. Fuel: the preview `DeltaV` is `<= GoNowDeltaV`. |
| `WarpOrder_EarthToPhobos_ArrivesOnTimeInPlaceWithWellFormedOrbit` cases `CircularPrograde-Phobos`, `CircularRetrograde-Phobos`, `EccentricPrograde-Phobos` (the `-Mars` cases stay as they are) | One warp, drop-in once at the predicted exit time, no second warp, the goal completes in a well-formed orbit around Mars. | Keep everything up to and including the drop-in checks. Replace `HasDataBlob<OrbitDB> \|\| NewtonSimpleMoveDB` with: an orbit, or a pending `NewtonThrustAction` for the transfer. Replace the wait-for-`NewtonSimpleMoveDB` loop with: run until the goal completes, with the deadline set from the plan (`drop-in + WaitCap + transfer flight + 1 day`, read from the preview, not a fixed 2 days). Add: before it completes, assert the goal reason starts with `Transfer to` and a `Transfer window` burn is queued, i.e. the ship waited for a window (`WaitSeconds` from the preview > 0 for at least one case). At the end `AssertArrivedMatchingPhobos`. Keep the "never warps again" checks inside the loop. Fuel: the preview `DeltaV` is `<= GoNowDeltaV`, and the fuel the ship actually burned after the drop-in is no more than the fuel for `GoNowDeltaV` (rocket equation, 5% slack). If the retrograde case cannot afford the transfer, that is a finding: report it, do not weaken the assert. |
| `WarpOrder_EarthToPhobos_SurvivesOneLongTickPastArrival` | One long tick past arrival must not fling the ship out of Mars's well. | Split in two. (1) `...WithADrive_SurvivesOneLongTick`: give the ship a drive and tank (the `AttachThrust` helper the `MoveToPlan_*` tests use), keep `AdvanceTo(eti + 1 day)` and every existing bounds assert except `Eccentricity < 0.05` (a transfer may still be pending); instead assert the goal is not `Failed`, and either the orbit is near circular or a transfer window burn is queued with a finite position. (2) `...WithAnEmptyTank_StaysBoundToMars`: the ship as the test builds it today (no usable Δv). The goal fails with the `Newtonian needs … m/s, have 0 m/s` sentence; assert that, plus `orbit.Parent == mars`, `Eccentricity < 1`, position inside Mars's SOI and finite after the long tick. The old path queued a burn the tank could not fly; the new one fails the goal and leaves the ship bound at e ≈ 0.72. That is the honest outcome, and the test says so. |

Acceptance: `dotnet test --filter WarpMoveTests` is green; the whole suite shows 0 failures. A change that only edits the planner's rule fails these tests, as it should.

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
- Slice 7: are 10% and two Hohmann flights the right defaults for Best window? Both are constants; Jon can move them after playing it.
- Slice 7: a ship with an empty tank that warps to Phobos now fails the goal and sits bound in Mars's well (the old code queued a burn it could not fly). Is a failed goal with the Δv sentence enough, or should the preview warn before the warp?
- Slice 3: should the other-mode line also show the warp sustain cost, or only time and Δv? Time and Δv are free; sustain needs the plan 2 bill.
- Timing was a Debug build. A Release build will be faster, but a band of 500 will still be seconds.

## Out

Making `MovePlanner.Preview` faster. Changing `MovePlanner.Select`. New arrival types in the engine. Warp changes. Making the warp arrive circular (the Phobos option that was dropped). A second porkchop implementation: the Orders tab reuses the NavWindow's through `PorkchopPanel` (slice 7b).

## Stop if

Slice 1 shows the freeze is in the tree sort or draw and not in the preview. Then the numbers above are not the whole cause and the plan needs a second measurement before slices 2 to 4.
