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
- `GameEngine/Logistics/plans/` — trade and transport plans. Plans 1–12 and 5b are implemented on `TradeAndTransport`. `office-purses.md` is an idea for later, not plan 13.
- `GameEngine/Industry/plans/` — colony industry. Refine-one-batch is implemented. It is not logistics plan 13.
- `GameEngine/Engine/Orders/colony-administrator.md` — standing colony intent, patience, specialization, and bids. Notes only. Not an implementation plan.

## Done

### Colonies list warehouse stock

2026-10-03. A blueprint colony with a logistics office is assigned `GoalType.OfferStock` after its cargo is loaded. `OfferStockPlan` adds a Min 0 policy row for each warehouse pile that has none, largest first, up to office capacity, then `RunMarket` posts the sell. Luna Concord lists iron, titanium, silicon, and methalox. New rows ask and bid 0. `OfferStockTests`, `EarthStartTests`, and `LunaConcordTests.PlacedColony_ListsItsWarehouse` pass.

### Sol start: the first mining hour kills the clock

2026-10-03. A mine rate for a mineral the body does not have is omitted in `MiningHelper.CalculateActualMiningRates`, and `MineResourcesProcessor.MineResources` skips a rate whose deposit is missing. `TimeStopDiagnosticTests.SolStart_FirstHour_MiningDoesNotKillTheClock` passes.

## Open

### Surface a simulation fault, and keep a crash save

Spitballed 2026-10-03. Do not build it until asked.

One `try`/`catch` inside the simulation task, around a single step in `SimulateTimeUntil`. Not one catch per processor. In that catch: if a debugger is attached, `Debugger.Break()` while the stack is still the processor frame; write the exception text, `ManagerSubPulse.CurrentProcess`, the system, and both clocks. Stop discarding `t.Exception`. The client shows a dialog, not a silent pause.

Two files. `Game.Save` of the live game, plus that text, is the crash report a player can send. Loading it and pressing play hits the same line again, which is what a dev wants. A resume file is the previous completed master step, taken after `GameGlobalDateTime` is assigned, on a real-time interval of a few minutes. The half-finished step is discarded. Offer that checkpoint from the dialog. If the checkpoint dies on the next unpause, say so and point at the crash folder. A data bug that is already in the checkpoint replays on the next unpause. Continuing past a fault only works when the fault does not replay.

`EnableMultiThreading` defaults to false. If it is on, checkpoint before the parallel pass.

`Pulsar4X.Client/CrashReports/DiscordCrashLogger.cs` posts a Discord embed. It is not this path.

### Quickstart failure leaves a blank window

`MainMenuItems` sets `IsActive = false` after `NewGameMenu.QuickstartGame()` whether or not a game was created (`Pulsar4X.Client/Interface/Menus/MainMenuItems.cs`). `QuickstartGame` returns immediately when `GameLifecycle.Quickstart()` returns null, and does not turn the menu back on (`NewGameMenu.cs`). `Quickstart` catches failures and returns null (`Pulsar4X.Client.Host/GameLifecycle.cs`). The result is a blank viewport and no Escape menu.

### Master pulse keeps a jump interrupt forever

`MasterTimePulse.ProcessNextInterupt` runs the jump pairs under `EntityDictionary.Keys.Min()` and returns that time. Nothing removes the key. Once `GameGlobalDateTime` reaches it, `SimulateTimeUntil` returns the same instant on every pass. The only writer is `InterSystemJumpProcessor.SetJump`. `ShipJumpAction` transfers the ship itself and does not call `SetJump`. Noted in `GeoSurveyFleetTests.FleetSurvey_MarsAndMoons_ShipsSurvey_TankerMovesToParent`, which calls `ProcessSystem` for that reason. Not the mining crash above.

### Geo survey: finishing a site can hang the subpulse

Same test comment: the first tick after a geo survey completes replans into a warp-to-self, and `ProcessSystem` hangs. The test stops at 24h and uses a 15s timeout. Not chased past that comment. See also `GameEngine/Movement/movement-issues.md`.
