# Master plan

The one index of Pulsar4X work. Open plans live in [`plans/`](plans/), reference notes in [`design/`](design/), the bug inbox is [`agent-notes.md`](agent-notes.md). Every plan that is finished was deleted in the 2026-11-01 cleanup. Its not-done items are carried below, so nothing is lost. Originals are in git history.

Rules for this file:
- A plan file is deleted once its work is built and tested. Anything not done goes under "Not done, from deleted plans" before the file goes.
- New plans go in `plans/` as `kebab-case.md`, with a `Status:` line, and get a row in the table below. Reference notes go in `design/`.
- Do not build anything marked "do not build until asked".

## 1. Work queue (in order)

Take the first unfinished item unless told otherwise. Delete an item when it is done.

1. **Pause checkbox fix.** Circularise and survey re-pause every tick while warping. The fix was written and put in `stash@{0}`, which is not in this clone's stash list. Check Jon's other clones before redoing it.
2. **Quickstart blank window.** `MainMenuItems.cs` (~line 54) hides the menu even if `GameLifecycle.Quickstart()` returns null, and `NewGameMenu.QuickstartGame` returns without turning the menu back on. Details in [agent-notes](agent-notes.md#quickstart-failure-leaves-a-blank-window).
3. **A bad industry job stops time.** `IndustryTools.ConstructStuff` throws inside the daily `IndustryProcessor` (about lines 94, 99, 105, 110, 139, 219, 244, 248): missing cargo storage, missing faction, a design not in `IndustryDesigns`, an industry type the line lacks, a recipe summing to 0, an input that is neither cargo nor an internal component. Skip the job and report; do not throw.
4. **Fix the 21 failing tests** (section 5). Several belong to plans that were called done.
5. **Play-test what is built but unplayed:** the Ceres direct offer (commit `61c4a603` says it was never played), the Movement plans in section 2 that say "playing" (including the Lambert porkchop, whose plot has not been clicked), fleet Bootstrap with packages (PR #493), and the colony/fleet windows (never clicked through).
6. **Earth and Luna list markets at price 0.** `OfferStockPlan` posts ask and bid 0. Price finding is a later economy session; see [agent-notes](agent-notes.md#price-finding-is-a-later-economy-session) for the interim numbers. Do not build it in a goals pass.
7. **Group colonies by sphere of influence** in the colonies window, with a player-chosen capital.
8. **Crew quarters and payroll.** Crew quarters, a crew requirement that fits, and payroll. There is no payroll code.
9. **Newtonian SOI capture** ([plan](plans/movement-newtonian-soi-capture.md)). Slices 1–4, 6, and 7 are in and tested (Earth–Luna both ways, one sphere radius, ellipse exit, Jump row hidden, Fleet window refusal and other orbits). Slice 5 can use the Lambert solver from item 10. Do not build slice 5 until asked. A game restart is needed (`SoiTransferAction`, `CanJump`, `PreviewMove`).
10. **Lambert and porkchop** ([plan](plans/movement-lambert-porkchop.md)). Built, playing. UI not play-tested. Solver, scan, planner, and the NavWindow plot are in. A game restart is needed (`TransferWindow`, `MoveOption.Window`, `NavMode.Porkchop`).
11. **Interplanetary Newtonian transfer** ([plan](plans/movement-interplanetary-newtonian.md)). Built, tests pass. UI not play-tested. Earth to Mars: escape, cruise, one mid-course, capture. Warp stays the default when both are feasible. A game restart is needed (`SoiTransferAction` phases, `TransferFromWindowCommand`, `MoveOption.Interplanetary`).
12. **Orders window freeze** ([plan](plans/orders-window-plan-on-click.md)). Opening an asteroid band in a ship's Orders tab plans every row each frame (22 ms each, 502 asteroids, cache cleared at 400) and freezes the game. Slice 1 is a small fix; slices 2 to 5 are plan-on-click with a preview, Execute and order options. Not started. High priority, move it up if wanted.

## 2. Open plans (kept)

| Plan | Status |
|---|---|
| [Movement: capture ellipse](plans/movement-capture-ellipse.md) | Built, tests pass, **playing** (unplayed). Delete once played. A short tank burns to a bound ellipse, or the captain refuses before the warp. |
| [Movement: warp sustain](plans/movement-warp-sustain.md) | Built, **playing**. Delete once played. Uphill warp is a sustain draw on the same bubble, one speed per leg. |
| [Movement: warp brake](plans/movement-warp-brake.md) | Built, **playing**. Delete once played. Nav quality 1.25 coasts in the destination well and warps back to low orbit, at most three bubbles. A game restart is needed (new class, commander snapshot skill list). |
| [Movement: Newtonian SOI capture](plans/movement-newtonian-soi-capture.md) | In progress. Slices 1–4, 6, and 7 are in and tested. Slice 5 can use the Lambert solver and is still not built. Earth to Luna and back by patched conics, one sphere radius, ellipse exit, Jump row hidden, Fleet window shows a Δv refusal and offers other orbits. |
| [Movement: Lambert and porkchop](plans/movement-lambert-porkchop.md) | Built, **playing**. UI not play-tested. Delete once played. Lambert solver, transfer-window scan, and the NavWindow porkchop. A game restart is needed (`TransferWindow`, `MoveOption.Window`, `NavMode.Porkchop`). |
| [Movement: interplanetary Newtonian](plans/movement-interplanetary-newtonian.md) | Built, tests pass. UI not play-tested. Delete once played. Earth to Mars with no warp: escape, cruise, one mid-course, capture. A game restart is needed (`SoiTransferAction` phases, `TransferFromWindowCommand`). |
| [Orders window: plan on click](plans/orders-window-plan-on-click.md) | Not started. Cause measured (22 ms per row, 502 asteroids, per-frame). Slice 1 stops the freeze; slices 2–5 plan on click with a preview panel, Execute and order options. |
| [Ceres: consignment](plans/ceres-consignment.md) | Not started. Do not build until asked. |
| [Stations](plans/stations.md) | Not started. Do not build until asked. Ship refit, its prerequisite, is built. |
| [Industry attribute split](plans/industry-attribute-split.md) | Open note, not a plan. Whether `IndustryAtb` splits into three classes is undecided. |
| [Saved data](plans/saved-data.md) | Open note. Do not build until asked. What the save file should and should not carry. |
| [Office purses](plans/office-purses.md) | Idea. Do not build until asked. No `PurseDB` in code. |
| [Faction grants, later](plans/faction-grants.md) | Stub. Do not build until asked. Colony locations as a separate grant from ships. |
| [Damage system](plans/damage-system.md) | WIP, open. Three sims side by side; combat still uses the random-HP `SimpleDamage`. A 12-item problem list and a 14-item feature list. |
| [Crate-able vs site-only designs](plans/crate-vs-site-only-designs.md) | Open items carried from the out-of-repo bootstrap plan. |

"Do not build until asked" also covers: "logistics plan 13" (any new logistics plan after the deleted 1–12), colony-administrator personality and bids, price finding, and the economy items in section 4.

## 3. Reference notes (`design/`)

| Note | What it is |
|---|---|
| [Agents and goals](design/agents-and-goals.md) | Goals, actions, agents, pipeline. Its TODO list is the open list for goals. |
| [Colony administrator](design/colony-administrator.md) | Notes only. Do not build personality, bids or construction from it. |
| [Colony bootstrap and sealed shipments](design/colony-bootstrap-and-shipments.md) | The built rules for the `BootstrapColony` goal, the fleet `Bootstrap` order, packages and sealed shipments. |
| [Market and stance](design/market-and-stance.md) | Stance, market book, settlement. Partly superseded by `FactionGrants` (2026-10-15). |
| [Industry overview](design/industry-overview.md) | What the industry code does today, and the processor work still open. |
| [Balance](design/balance.md) | Costs, time and propulsion philosophy. Do not retune until asked. |
| [API layer](design/api-layer.md) | `Pulsar4X.Api` design. Phases 1–6 built; phase 7 is open (section 4). |

Code-adjacent notes left where they are: [`HowDamageWorks.txt`](../GameEngine/Damage/DamageComplex/HowDamageWorks.txt) (damage) and [`NewtonManuvers.txt`](../Pulsar4X.Client/Rendering/ManuverNodes/NewtonManuvers.txt) (Newton maneuvers). Not touched because they are `.txt` design notes next to the code they describe.

## 4. Not done, from deleted plans

Every plan below was built and tested, then deleted. These are its leftovers, so a new plan can pick them up. All of these are "do not build until asked".

**Economy and trade (old logistics plans 1–12, `survey-charts`, `move-own-goods`, `sealed-shipments`)**
- Stance: treaties, wars, opinion, a stance UI.
- Settlement: destination choice, reservations, refund on cancel.
- Colony market orders: sector-wide orders, population consumption, an admin skill curve, a policy editor.
- Market bars: price history, a galaxy-wide market browser.
- Independent trader: several goods per hold, NPC merchants, fuel in the route score.
- Owned freight: friendly hauling, multi-stop loops.
- Inter-system routes: jump fuel, a standing circuit, foreign markets on the galaxy map.
- Autonomous pick: **refuel interrupt and restore** (`ShouldInterruptForRefuel` swapping Active and keeping Given), skill bias.
- Fleet trade: a convoy that jumps, sibling-cancel.
- Colony supply: a governor entity, sector width, population consumption, admin XP, a member list.
- Offer stock: catalog price, restoring the offer-stock job after a supply order, an office gained later.
- Sealed shipments: cancel after loading, refund on ship destruction, a computed freight tariff, a hauler that interrupts work it is already doing.
- Survey charts: a courier item, a bid or contract for a survey, admin pricing intel, a system bundle, a jump-tag destination toggle, auto-buy under a budget, other intel kinds.
- Bootstrap: factory production at the source when crates are not in stock; the open items in [crate-vs-site-only-designs](plans/crate-vs-site-only-designs.md).
- Ceres (plans 1–5 built): the [consignment](plans/ceres-consignment.md) plan, the direct-offer play-test (queue item 5), and the price items in queue item 6.

**Industry (refine, mine, construction, yard, refit, prefab facilities)**
- Asteroid mining: a hauler that meets the miner, a cache on the rock, moons, a one-jump search, higher tech, a mineral filter, skill and claims. Six `AsteroidMineTests` fail (section 5).
- Refining: population use, a preferred fuel, a catalog price.
- Processor work (full text in [industry overview](design/industry-overview.md)): `ConstructStuff` throws (queue item 3); `OutputAmount` disagrees with `RunMarketPlan`'s `Max − stock` completion count, to fix before a second producer uses `AutoProduce`; `IndustryOrder2.Clone`, `QueueShipRefitOrder.Clone` and four construction-queue order `Clone` methods throw `NotImplementedException` (nothing calls them today); `MiningDB`'s copy constructor does not copy the rate dictionaries; `InstallationsDB` employment is unused.
- Prefab and site facilities (built 2026-11-02, plan deleted): left out were a locked range widget, a combo of live planets, ship-part balance, academy / admin / launch retunes, widening crates already built. Details in [industry overview](design/industry-overview.md).
- Stations are the next industry slice ([plan](plans/stations.md)). Attribute split: [note](plans/industry-attribute-split.md).

**Movement and engine**
- Movement "Out" list (all three movement plans): delete `CreateCommandEZ`, split the arrival burn, a tanker, power reroute, the pause-checkbox stash (queue item 1). Dead orbit helpers.
- Save and load (`01-round-trip`, `02-work-in-progress` built): the **crash-save** half of the fault dialog ([agent-notes](agent-notes.md#a-processor-fault-stops-the-clock-quietly--the-dialog-is-in-the-crash-save-is-not)); restoring open windows, selection, camera and SM mode; Player, passwords and access checks on Connect. Smaller save-file clean-up is in [saved-data](plans/saved-data.md).

**People and agents**
- Officers and span (built): a second chair on the fleet bridge, commissioning an ensign, crew training, a warship/merchant track, admiral ranks, scientists, administrators, personality and bids.
- Agent log (built): archiving old lines, printing why a weight won, rewriting planner messages, a log on the entity card.
- Agent wakes (built): nothing left.
- [Agents and goals](design/agents-and-goals.md) TODO list: translator feedback, projecting goals into the UI, `GoalsDB.Clone` copying `GivenGoal`, `CanPerform`, fleet-fail sibling cancel, a real `CanAccept`.
- [Colony administrator](design/colony-administrator.md) open list: the bid number, whether a player edit sticks, Patience and Specialization effects, Greed→Patience weights; build order personality → Luna civilian → bid pass → construction that spends cargo.

**Movement plumbing** (old `movement-issues` handoff note, built and tested, deleted 2026-11-02; the note never lived in the repo, it came from Jon's laptop)
- Done in code: goal-tagged finished actions no longer mask the lane (`ActionQueueProcessorTests`); `NavSequenceProcessor` removed; `Clone()` on every movement action (`MovementActionCloneTests`); arrival parks instead of throwing; a warp survives save and load; a short-ΔV burn fails the action and the goal; `EntityAction.Goal` is gone and only `ParentGoalId` links an action to its goal; the `NameDB` lookup and `StartNonNewtTranslation` return value are gone; the warp now queues a `CirculariseAction` for the capture burn. Plan 3 removed both `InterPlanetaryHohmann` copies and `OrbitalMath.HohmannOE2` (no caller; `Hohmann2` stays). `ScheduleSOIEntry` now uses the band prefilter, the conservative step, and a horizon rescan.
- Not done: no test that a two-action plan (warp, then circularise) runs to the end on its own; `WarpMoveAction.CreateCommand` is still used by `CommandTranslator` (insertion point path) and `CreateCommandEZ` still throws if the target is already thrusting (deleting it is "Out", above); `WarpMoveProcessor.SetOrbitHereFullNewt` is never called, `NewtonThrustAction.Thrust90ToTargetCmd` is only used by a clone test, `InterceptCalcs.FTLIntercept` has no caller; `StrictNewtonion` still picks between a free teleport into orbit and a burn inside `WarpMoveProcessor`; turning loads off or a rescue for a ship that cannot charge, a commander refusing a move, and a tanker are "Later". Plan 1 slice 6 is in: one sphere radius (`Entity.GetSOI_m`), and a bound ellipse whose apoapsis crosses that sphere schedules an exit.

**Early Tech Start decisions (Jon, 2026-11-03):** the start ship is `Starship I` (design `early-ship-design-starship`: three Raptors, 1,197 t methalox, Δv 13.6 km/s, Earth to Mars about 5.6 km/s), and the Newton Tug stays in the mod as a design only. Warp is the default on interplanetary legs, and a faster-option calculation is future work. A short tank is refused, with the refusal and alternative orbits shown in the Fleet window Orders tab.

**Agent goal weighting** (old `weighting-vertical-slice` note, also from the laptop, deleted 2026-11-02)
- Built: `GoalRoles` (Drive, Task, Stance), `FuelSituation`, `GoalWeighting`, `RefuelShipPlanner`, `CargoTransferOrder.CreateRefuelPair`; the plans call `GoalWeighting.ShouldInterruptForRefuel`, and `AgentProcessor` calls `PickAutonomousTask`.
- Not done: a goal stack and resume after an interrupt (same item as the refuel interrupt in the economy list above); `MovePlanner` prepending a refuel stop to a survey; a tanker that moves to the needy ship; the four acceptance checks have no dedicated test (nothing in `Pulsar4X.Tests` names `GoalWeighting`, `FuelSituation` or `RefuelShipPlanner`).

**Client**
- Colony and fleet windows are built but were not clicked through; grouping colonies by sphere of influence is queue item 7.

**API layer** ([design](design/api-layer.md)): phase 7 `MultiplayerAdapter` and `Pulsar4X.Server.Host`; naive faction selection on connect; geo and grav completion staleness; the designer data in-process bridge; procedural bodies lack `GeoSurveyableDB`; two `PositionDB` classes.

**Balance** ([balance](design/balance.md)): its "Still off" list is not a work order.

## 5. Known failing tests

21 fail, 948 pass (969 total, including the new `PrefabFacilitiesTests`), on `docs-plan-cleanup` over `origin/DevBranch` at 3a423fb3. The same 21 fail as before the facility balance pass; it changed none of them. Run from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor`, full path to `Pulsar4X.Tests.csproj`.

| Tests | Count | Plan they came from |
|---|---|---|
| `TestEccHypAnomalyCalcs` and `TestingVariableKeplerConversions`, the four hyperbolic `OrbitFuzzTesting` cases each | 8 | Orbit maths, old |
| `AsteroidMineTests`: `Fleet_ParcelsOneRockPerShip`, `FullHold_ReturnsToTheOwnedColonyCloserInTime`, `OwnedColonyBeatsACloserFriendlyBuyer`, `ParkedOneDay_MovesOre_AndKeepsTheClockRunning`, `SecondShip_DoesNotTakeTheFirstShipsRock`, `SurveyedAsteroid_GetsAMineOrder_OtherBodiesDoNot` | 6 | mine-asteroids |
| `InterSystemRouteTests`: `KnownJump_FirstActionsTargetTheGate`, `Freighter_KnownJump_FirstActionsTargetTheGate`, `UnknownSystem_IsNotChosen` | 3 | inter-system routes |
| `TradePlannerTests.AfterTheBuy_TheNextPlanMovesTowardMars_AndKeepsThePair`, `FreighterPlannerTests.AfterTheLoad_TheNextPlanMovesTowardTheBuyer` | 2 | independent trader, owned freight |
| `ColonyMarketOrderTests.Prune_RunMarket_RequiresALogisticsOffice` | 1 | colony market orders |
| `OfficersAndSpanTests.Span_ClipsSurveyAndHaul_EmptyChairRejectsMoveGoods_PlotIsNotSpanned` | 1 | officers-and-span |

The plans claimed these passed. Treat them as regressions to look into, not as failures of the deleted plans' features.
