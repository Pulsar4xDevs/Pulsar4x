# Plan 7 — Owned freight

Status: implemented on `TradeAndTransport`. Tests: `Pulsar4X.Tests/FreighterPlannerTests.cs`.

## Outcome

The player assigns `GoalType.Freighter` to a cargo ship. It fills the largest buy request on one of that faction's colonies in the system, taking goods from another of that faction's colonies, then moves and exchanges.

## Depends on

Plan 3 for same-faction settlement (book updates, ledger untouched). Plan 4 so colonies actually post buy requests and sell offers. Plan 6's route fields on `Goal` and the rule that the ship emits move actions instead of assigning `MoveTo` to itself. Those fields can be added with this plan if plan 6 has not landed; do not invent a second route store.

Can be implemented in parallel with plan 6 after plans 3 and 4. Share one helper for "list markets, build move actions, build the exchange action". Do not share the score.

## Planner

`FreighterPlan : IGoalPlanner` for `GoalType.Freighter`. One ship. A fleet fails with `"Freighter is one ship"`.

Candidates are colonies in this `EntityManager` whose `FactionOwnerID` equals the ship's, with `LogiBaseDB`. No foreign market, even a friendly one.

If the goal has no route:

- Consider each buy listing with `BuyQuantity > 0`.
- Source is another own colony with `SellQuantity > 0` for that cargo id, or stock above reserve when sell quantity is 0. Prefer a posted sell offer over unlisted surplus.
- The ship must be able to carry that `CargoTypeID`.
- Score is units the ship can move: `min(buy quantity, source available, free unit space)`. Highest units wins. Ties break toward the shorter position distance. There is no bid-minus-ask term. A zero-unit pair is ignored.
- No pair: `Fail("no haul")`.
- Otherwise store `CargoId`, `SourceEntityId`, `DestEntityId` through the same `PlanResult` route plan 6 uses.

If the route is set, the legs match the trader: buy (or load) at source, move, sell at dest. Same-faction `MarketExchange` does not move money. Loading unlisted surplus still goes through settlement: if the source has stock above reserve but sell quantity 0, the planner does not invent a side channel. It only hauls a posted sell offer in this plan. Unlisted surplus is plan 4's job to post.

Lost listing or a colony that changed owner: `Fail("listing gone")`.

Low fuel uses the same fail as the trader: `Fail("low on fuel")`, do not assign `RefuelAt`.

`PruneImpossibleGoals`: `Freighter` is possible when the ship has cargo storage and a warp or newton drive.

`HelpOwn` stays a stance weight. This plan does not read it. Plan 9 is what turns that weight into an assigned goal.

Command: `FreighterCommand(TargetEntityId)` assigns `GoalType.Freighter` with an empty route.

## Tests

`Pulsar4X.Tests/FreighterPlannerTests.cs`

- Two owned colonies, one selling 500 iron, one buying 800, ship capacity 200: route is that pair, first exchange moves 200, ledgers unchanged.
- A friendly foreign buy request is ignored even when it is larger.
- No own buy request: Failed, `"no haul"`.
- After the load, the next plan aims at the buying colony, same cargo id.
- Fleet assignment fails.

## Later

Hauling for a friendly faction (that is `Trade`), multi-stop loops, and command-span limits on the leaf. Fleet fan-out is plan 10. A freighter leaf is one ship.
