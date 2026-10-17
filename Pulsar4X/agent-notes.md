# Agent notes

Inbox for small bugs and follow-ups that came up while doing something else. Any agent session in this repo should read this at the start and append to it.

## How to use this file

- One item, one heading. Say what happens, the file and function, and how you know. A repro or a test name is enough. Skip a story about how you found it.
- Read the open items before you edit. If you are already in that code and the fix is small, do it in the same pass, then mark the note done.
- A new problem that is unrelated to the task, or that is not a small change, gets an entry and stays unfixed.
- When you fix one, replace the body with the date and the one-line outcome, or delete the entry. An open heading means it is still open.
- Longer writeups already have a home. Link them. Do not paste them here.
- Plans under `GameEngine/Logistics/plans/` own their own "later" lists. Do not copy those into this file.
- No secrets, no save files, no logs.

## Longer notes, already written

Status lives in the file itself, in `near-term.md`, or in `AGENTS.md`. This list is only where to look.

- `near-term.md` — ordered work list.
- `GameEngine/Movement/movement-issues.md` — movement.
- `GameEngine/Damage/damage-system.md` — damage.
- `GameEngine/Engine/Orders/agents-and-goals-design.md` — goals, actions, and agents.
- `GameEngine/Engine/Orders/agent-wakes.md` — agent wakes.
- `GameEngine/Engine/Orders/weighting-vertical-slice.md` — autonomous weighting.
- `GameEngine/Engine/Orders/colony-administrator.md` — colony administrator.
- `GameEngine/Engine/Orders/plans/` — Ceres faction jobs.
- `GameEngine/Logistics/plans/` — trade and transport. Includes `office-purses.md`, `sealed-shipments.md`, `survey-charts.md`, and `faction-grants-later.md`.
- `GameEngine/Industry/plans/` — colony industry.

## Done

### Colonies list warehouse stock

2026-10-03. A blueprint colony with a logistics office is assigned `GoalType.OfferStock` after its cargo is loaded. `OfferStockPlan` adds a Min 0 policy row for each warehouse pile that has none, largest first, up to office capacity, then `RunMarket` posts the sell. Luna Concord lists iron, titanium, silicon, and methalox. New rows ask and bid 0. `OfferStockTests`, `EarthStartTests`, and `LunaConcordTests.PlacedColony_ListsItsWarehouse` pass.

### Sol start: the first mining hour kills the clock

2026-10-03. A mine rate for a mineral the body does not have is omitted in `MiningHelper.CalculateActualMiningRates`, and `MineResourcesProcessor.MineResources` skips a rate whose deposit is missing. `TimeStopDiagnosticTests.SolStart_FirstHour_MiningDoesNotKillTheClock` passes.

### Master pulse keeps a jump interrupt forever

2026-10-17. `ProcessNextInterupt` removes the datetime key it just ran. A later key stays. `AgentWakeTests.JumpKey_IsGoneAfterThePulse_AndTheNextStepMovesOn` passes. Live jumps stay on `ShipJumpAction`.

### Geo survey: finishing a site can hang the subpulse

2026-10-17. `FleetSurvey_MarsAndMoons_ShipsSurvey_TankerMovesToParent` runs to 30 hours and Mars is complete. `FleetGravSurvey_OnSite_CompletesAssignedAnomalies` includes the outer anomaly and returns. A second pass still due at the current instant throws and names the processor. A ship parked on a `MoveTypes.None` site can plot the next warp.

## Open

### A processor fault stops the clock quietly — the dialog is in, the crash save is not

2026-10-03. The silent half is done. `MasterTimePulse.AdvanceOneStep` catches one step, `Debugger.Break()` when a debugger is attached, and stores `LastFault` (exception text, `CurrentProcess`, system, both clocks). `NotifyWhenStopped` records a fault that escapes the task instead of dropping `t.Exception`. The client gets `GameEventType.SimulationFaulted` and shows a modal. `TimeStopDiagnosticTests.ProcessorFault_stops_the_clock_and_keeps_the_report` passes. No save is written.

The crash-save half is still unbuilt. Do not build it until asked. Two files: a `Game.Save` of the live game plus the fault text (a replay dump — the step may already be torn), and a resume file taken after `GameGlobalDateTime` is assigned, every few minutes. The half-finished step is discarded. Offer the checkpoint from the dialog. If it dies on the next unpause, say so. A data bug already in the checkpoint throws again. If `EnableMultiThreading` is on, checkpoint before the parallel pass. `DiscordCrashLogger` is not this path.

### Quickstart failure leaves a blank window

`MainMenuItems` sets `IsActive = false` after `NewGameMenu.QuickstartGame()` whether or not a game was created (`Pulsar4X.Client/Interface/Menus/MainMenuItems.cs`). `QuickstartGame` returns immediately when `GameLifecycle.Quickstart()` returns null, and does not turn the menu back on (`NewGameMenu.cs`). `Quickstart` catches failures and returns null (`Pulsar4X.Client.Host/GameLifecycle.cs`). The result is a blank viewport and no Escape menu.

### Price finding is a later economy session

2026-10-10. `OfferStockPlan` posts a new row at ask 0 and bid 0 because cargo has no catalog price. Strata Survey, Lode Mining, and Ceres Depot need non-zero prices so charts, fuel, and ore actually move cash. Do not build price finding in the faction-goal pass. What a chart, a unit of ore, and a unit of fuel are worth, and how a bid moves, belongs in an economy session. `GameEngine/Engine/Orders/colony-administrator.md` already holds the bid notes. Do not implement them from this inbox item.

Interim for the Ceres trade test only. Methalox already has `WealthCost` 3 on the processed-material blueprint. Use that as the depot's fuel ask. Ore and survey charts have no catalog number, so their starting rows use fixed non-zero ask and bid, with the bid below the ask. Leave `OfferStockPlan`'s default of 0 in place. Luna Concord and Earth keep listing at 0 until that session.

### A single-ship faction cannot post its own buy or sell orders

2026-10-10. Strata Survey and Lode Mining have a ship and no colony. A market order is stored on the colony that owns the logistics office. `EngineGameServer.SubmitCommand` rejects a command against an entity the faction does not own. `BuyIntelCommand` is the exception, and it buys a chart the owner already listed. A friendly ship can fill an existing listing with `MarketExchangeAction` when `FactionStanceRules.CanTrade` is true and the ship is in cargo-transfer range. It cannot add a bid or an ask. `MarketView.CanEdit` is true only for the owner. Do not build a visitor order book unless asked.

### Remote buy is a sealed shipment

2026-10-10. Written up as `GameEngine/Logistics/plans/sealed-shipments.md`. Not logistics plan 13. Do not build it until asked. A goals-and-actions session can take the `DeliverShipment` half. The buying office pays the goods and posts the intel haul. Any free ship can claim it and be paid the fee. Several goods can share one shipment. Dockside escrow still starts only in cargo range.
