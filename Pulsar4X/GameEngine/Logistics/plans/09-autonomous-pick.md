# Plan 9 — Autonomous pick

Status: implemented on `TradeAndTransport`. Tests: `Pulsar4X.Tests/AutonomousTradeTests.cs`. Kept separate from plans 4, 6, and 7.

## Outcome

An idle ship with no given goal can be handed `Trade` or `Freighter`. An idle colony with a market policy can be handed `RunMarket`. The player-issued goals from those plans stay the way they are.

## Depends on

Plans 4, 6, and 7 playable as issued goals. `GoalWeighting.PickAutonomousTask` already exists and is unused. `AgentProcessor` returns immediately when `ActiveGoal` is null (around the "autonomous pick not wired yet" return). That return is the insertion point.

Interrupt-resume (`ShouldInterruptForRefuel` swapping Active and keeping Given) stays the separate item in `agents-and-goals-design.md`. This plan does not interrupt a running goal.

## Work

1. When `ActiveGoal` is null, or Completed, or Failed, and `GivenGoal` is null, call `PickAutonomousTask`. If it returns a task, `AssignGoal` that task. If `GivenGoal` is set, leave it. A finished player order is not replaced.
2. Reclassify `MakeProfit` as `GoalRole.Drive` in `GoalRoles`. It does not get a planner. In `GoalWeighting.BuildContextModifiers`, `MakeProfit`'s effective weight (base × greed, already computed for the drive) adds a context multiplier on `GoalType.Trade`. `HelpOwn` does the same for `GoalType.Freighter`. `RunMarket` is chosen only for an entity with `ColonyInfoDB` and `ColonyMarketPolicyDB` rows; ships do not pick it, colonies do not pick Trade or Freighter. Do that with the existing capability prune (`−1` when the entity cannot), which `PickAutonomousTask` already skips at weight ≤ 0. Confirm the prune sets `RunMarket` impossible without an office, `Trade`/`Freighter` impossible without cargo space and a drive, and both impossible on the wrong kind of entity.
3. Do not add a skill term inside `TradePlan`, `FreighterPlan`, or `RunMarketPlan`. Weighting is the only place greed and `HelpOwn` apply.
4. A picked Trade or Freighter goal still starts with an empty route. The planner from plan 6 or 7 chooses the pair. A picked `RunMarket` uses the policy already stored by plan 4. If the colony has no policy rows, prune `RunMarket` to impossible.
5. Low-fuel ships still fail their trade inside the planner (plan 6). This plan does not auto-assign `RefuelAt` over the top of a picked Trade. `RefuelAt` remains available to `PickAutonomousTask` on its own when no Trade goal is running, which is the existing weight, not a new interrupt.

## Tests

`Pulsar4X.Tests/AutonomousTradeTests.cs`

- Ship, no goals, cargo hold, drive, a profitable friendly pair in system: after one agent wake, `ActiveGoal.Type` is Trade.
- Same ship with `GivenGoal` set to MoveTo: wake does not replace it.
- Colony with policy rows and an office, no goal: wake assigns `RunMarket`.
- Colony with no policy: no goal assigned.
- Ship does not receive `RunMarket`. Colony does not receive `Trade`.
- `MakeProfit` has no planner registered. Greed above 1 raises Trade's effective weight above Freighter's when both are possible; a test can set personality and assert which one is picked. Missing `AgentDB` still picks (greed defaults inside weighting).

## Later

Interrupting a haul to refuel and then restoring it, and any skill bias on route score.
