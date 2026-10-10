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

- `GameEngine/Movement/movement-issues.md` — movement plumbing that is still broken.
- `GameEngine/Damage/damage-system.md` — damage as it works today and what should change.
- `GameEngine/Engine/Orders/agents-and-goals-design.md` — goal, action, and agent decisions.
- `GameEngine/Engine/Orders/weighting-vertical-slice.md` — autonomous weighting slice.
- `GameEngine/Logistics/plans/` — trade and transport plans. Plans 1–12 and 5b are implemented on `TradeAndTransport`. `office-purses.md` is an idea for later, not plan 13. Do not build it until asked. `survey-charts.md` is implemented: finished surveys are the first kinds on a general intel list, not plan 13.
- `GameEngine/Industry/plans/` — colony industry. Refine-one-batch is implemented. It is not logistics plan 13.
- `GameEngine/Engine/Orders/colony-administrator.md` — standing colony intent, patience, specialization, and bids. Notes only. Not an implementation plan.
- `GameEngine/Engine/Orders/plans/` — Ceres faction jobs, three slices. Not logistics plan 13. Do not build them until asked.

## Done

### Colonies list warehouse stock

2026-10-03. A blueprint colony with a logistics office is assigned `GoalType.OfferStock` after its cargo is loaded. `OfferStockPlan` adds a Min 0 policy row for each warehouse pile that has none, largest first, up to office capacity, then `RunMarket` posts the sell. Luna Concord lists iron, titanium, silicon, and methalox. New rows ask and bid 0. `OfferStockTests`, `EarthStartTests`, and `LunaConcordTests.PlacedColony_ListsItsWarehouse` pass.

### Sol start: the first mining hour kills the clock

2026-10-03. A mine rate for a mineral the body does not have is omitted in `MiningHelper.CalculateActualMiningRates`, and `MineResourcesProcessor.MineResources` skips a rate whose deposit is missing. `TimeStopDiagnosticTests.SolStart_FirstHour_MiningDoesNotKillTheClock` passes.

## Open

### A processor fault stops the clock quietly — the dialog is in, the crash save is not

2026-10-03. The silent half is done. `MasterTimePulse.AdvanceOneStep` catches one step, `Debugger.Break()` when a debugger is attached, and stores `LastFault` (exception text, `CurrentProcess`, system, both clocks). `NotifyWhenStopped` records a fault that escapes the task instead of dropping `t.Exception`. The client gets `GameEventType.SimulationFaulted` and shows a modal. `TimeStopDiagnosticTests.ProcessorFault_stops_the_clock_and_keeps_the_report` passes. No save is written.

The crash-save half is still unbuilt. Do not build it until asked. Two files: a `Game.Save` of the live game plus the fault text (a replay dump — the step may already be torn), and a resume file taken after `GameGlobalDateTime` is assigned, every few minutes. The half-finished step is discarded. Offer the checkpoint from the dialog. If it dies on the next unpause, say so. A data bug already in the checkpoint throws again. If `EnableMultiThreading` is on, checkpoint before the parallel pass. `DiscordCrashLogger` is not this path.

### Quickstart failure leaves a blank window

`MainMenuItems` sets `IsActive = false` after `NewGameMenu.QuickstartGame()` whether or not a game was created (`Pulsar4X.Client/Interface/Menus/MainMenuItems.cs`). `QuickstartGame` returns immediately when `GameLifecycle.Quickstart()` returns null, and does not turn the menu back on (`NewGameMenu.cs`). `Quickstart` catches failures and returns null (`Pulsar4X.Client.Host/GameLifecycle.cs`). The result is a blank viewport and no Escape menu.

### Master pulse keeps a jump interrupt forever

`MasterTimePulse.ProcessNextInterupt` runs the jump pairs under `EntityDictionary.Keys.Min()` and returns that time. Nothing removes the key. Once `GameGlobalDateTime` reaches it, `SimulateTimeUntil` returns the same instant on every pass. The only writer is `InterSystemJumpProcessor.SetJump`. `ShipJumpAction` transfers the ship itself and does not call `SetJump`. Noted in `GeoSurveyFleetTests.FleetSurvey_MarsAndMoons_ShipsSurvey_TankerMovesToParent`, which calls `ProcessSystem` for that reason. Not the mining crash above.

### Geo survey: finishing a site can hang the subpulse

Same test comment: the first tick after a geo survey completes replans into a warp-to-self, and `ProcessSystem` hangs. The test stops at 24h and uses a 15s timeout. Not chased past that comment. See also `GameEngine/Movement/movement-issues.md`.

### Price finding is a later economy session

2026-10-10. `OfferStockPlan` posts a new row at ask 0 and bid 0 because cargo has no catalog price. Strata Survey, Lode Mining, and Ceres Depot need non-zero prices so charts, fuel, and ore actually move cash. Do not build price finding in the faction-goal pass. What a chart, a unit of ore, and a unit of fuel are worth, and how a bid moves, belongs in an economy session. `GameEngine/Engine/Orders/colony-administrator.md` already holds the bid notes. Do not implement them from this inbox item.

Interim for the Ceres trade test only. Methalox already has `WealthCost` 3 on the processed-material blueprint. Use that as the depot's fuel ask. Ore and survey charts have no catalog number, so their starting rows use fixed non-zero ask and bid, with the bid below the ask. Leave `OfferStockPlan`'s default of 0 in place. Luna Concord and Earth keep listing at 0 until that session.

### A single-ship faction cannot post its own buy or sell orders

2026-10-10. Strata Survey and Lode Mining have a ship and no colony. A market order is stored on the colony that owns the logistics office. `EngineGameServer.SubmitCommand` rejects a command against an entity the faction does not own. `BuyIntelCommand` is the exception, and it buys a chart the owner already listed. A friendly ship can fill an existing listing with `MarketExchangeAction` when `FactionStanceRules.CanTrade` is true and the ship is in cargo-transfer range. It cannot add a bid or an ask. `MarketView.CanEdit` is true only for the owner. Do not build a visitor order book unless asked.
