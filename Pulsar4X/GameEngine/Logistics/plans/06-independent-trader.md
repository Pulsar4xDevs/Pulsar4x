# Plan 6 — Independent trader

Status: not started.

## Outcome

The player assigns `GoalType.Trade` to their own cargo ship. The ship picks one good, one buy market, and one sell market in the current system, moves there, buys, moves, and sells.

## Depends on

Plans 1 and 3. The book must contain a sell offer and a buy request (default start's Earth/Mars iron, or a test that posts them). `MoveTo` actions already exist via `MovePlanner.TryBuildMoveActions`. Do not assign a `MoveTo` goal onto the ship: `AssignGoal` would replace the Trade goal.

## Route remembered on the goal

A ship's active goal replans when one action succeeds, and that replan drops queued follow-on actions (`AgentProcessor`, ship Active branch). The planner must rebuild the next leg from the goal plus the world.

`PlanResult` gains an optional route the agent copies onto the goal when present. Planners still do not write the goal themselves. Fields on `Goal`:

- `CargoId` (string, empty if unset)
- `SourceEntityId` (where to buy)
- `DestEntityId` (where to sell)

`TargetEntityID` stays the single field other goals use. Trade uses the two new ids.

## Planner

`TradePlan : IGoalPlanner` for `GoalType.Trade`. Leaf ship only. A fleet fails with `"Trade is one ship"`.

Legal markets: this ship's `EntityManager` (one star system), `CanTrade` with the market owner, `LogiBaseDB` present. The ship must have a `CargoStorageDB` type store that accepts the good's `CargoTypeID`.

If the goal has no route yet, score every source/dest pair of distinct markets:

`score = dest.Bid − source.Ask − hours`

`hours = distance / warp max speed`, or a fail-closed large number when the ship has no warp drive. Distance is between the two markets' positions. One credit per hour. Fuel is not priced (fuel has no market price yet).

Take the highest score above zero. Return that route on the `PlanResult` plus the first leg's actions. If none score above zero, `Fail("no route")`.

If the goal already has a route, do not search again:

- Ship does not yet hold the cargo: if in range of the source, the exchange action `BuyFromMarket` for `min(sell quantity, free space)`. Else `MovePlanner.TryBuildMoveActions` toward the source.
- Ship holds the cargo: if in range of the dest, `SellToMarket` for `min(units on ship, buy quantity)`. Else move actions toward the dest.
- Source listing or dest listing gone, or `CanTrade` now false: `Fail` with the exchange message or `"listing gone"`.

One good per run. One pair per run.

Low fuel: if `GoalWeighting.ShouldInterruptForRefuel` is true, `Fail("low on fuel")`. Do not assign `RefuelAt`. The player issues that goal. This stays until interrupt-resume exists.

`PruneImpossibleGoals`: `Trade` is possible when the entity has cargo storage and a warp or newton drive. It is impossible otherwise. Remove the hardcoded `false` for `Trade` only. Leave `Freighter` and `MakeProfit` until their plans.

Command: `TradeCommand(TargetEntityId)` assigns `GoalType.Trade` with an empty route. Target is the ship. Reject a ship with no cargo storage.

## Retire the old shipper

Once an Earth-to-Mars iron test passes (listings from plan 2, ship assigned Trade, actions are a move or an exchange and never `WarpMoveAction.CreateCommandEZ` from `LogisticsCycle`):

- Delete `LogiShipperDB`, `ShipLogisticsOrders`, `LogisticsCycle`, `LogisticsSimple`, `LogisticsNewtonion`, and the two logistics processors if they are now empty.
- Leave the parked `LogisticsWindow` / `ColonyLogisticsDisplay` files uncompiled.

## Tests

`Pulsar4X.Tests/TradePlannerTests.cs`

- In-range ship, Earth selling iron, Mars buying iron, mutually tradable or same faction: first plan's actions are a buy at Earth, route stored by the agent, no warp from the logistics cycle.
- After the buy, the next plan's actions move toward Mars or sell if already in range. The pair does not change.
- No pair with a positive score: goal Failed, message `"no route"`.
- Hostile market is not a candidate.
- Low fuel: Failed, message `"low on fuel"`, goal type is still Trade (not replaced by RefuelAt).
- A fleet assigned Trade fails.

## Later

Several goods in one hold, NPC merchants (same planner on another faction's ship), `MakeProfit` as an automatic choice (plan 9), fuel priced into the score, and jump routes (plan 8).
