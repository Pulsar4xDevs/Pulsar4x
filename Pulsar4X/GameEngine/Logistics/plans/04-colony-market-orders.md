# Plan 4 — Colony market orders

Status: implemented on `TradeAndTransport`. Tests: `Pulsar4X.Tests/ColonyMarketOrderTests.cs`.

## Outcome

A player tells one colony to run its market. On each agent wake the colony posts sell offers for surplus, posts buy requests for shortages and industry inputs, and can enqueue industry jobs for goods it is set to produce.

## Depends on

Plan 2 (the book and `SetListing`). Plan 3 before a posted buy request can actually be filled. This plan only writes the book and the industry queue.

## Decision: one goal, two jobs

`GoalsDB` holds a single `ActiveGoal`. `AssignGoal` replaces it. Two issued goals would overwrite each other, so this plan adds one task, `GoalType.RunMarket`. Produce-and-sell and request-and-buy are the two halves of that planner's pass.

`AgentProcessor` today fails an active goal whose entity is neither a ship nor a fleet (`"managed entity is not a ship or a fleet"`). A colony branch is part of this plan.

## Policy, separate from the book

Friendly factions will see listings (plan 5). They do not see production targets. Store policy on the colony, not on `MarketListing`:

```csharp
public sealed class MarketPolicyRow
{
    public string CargoId;
    public long Min;          // becomes the listing reserve
    public long Max;          // produce-up-to target
    public bool AutoProduce;
    public decimal Ask;
    public decimal Bid;
}
```

`ColonyMarketPolicyDB` holds `Dictionary<string, MarketPolicyRow>`.

`SetMarketPolicyCommand` and `ClearMarketPolicyCommand` target the colony. Reject a colony with no `LogiBaseDB`, a negative min/max/price, or `Max < Min`. `RunMarketCommand` calls `AgentProcessor.AssignGoal` with `GoalType.RunMarket`. The translator rejects a colony with no logistics office.

## What one planner pass does

`RunMarketPlan` implements `IGoalPlanner`. It does not write the book and does not enqueue jobs itself. It returns actions. The agent submits them.

For each policy row, let `stock` be `MarketBook.Stock`, and let `needed` be the sum of `ResourcesRequiredRemaining` for that cargo id across the colony's current industry jobs.

- `hold = max(Min, needed)`
- `SellQuantity = max(0, stock − hold)`, `Ask` from the row, `Reserve = Min`
- `BuyQuantity = max(0, hold − stock)`, `Bid` from the row
- If `AutoProduce` and `stock + alreadyQueued < Max`, also return one `IndustryOrder2` new-job action for `min(Max − stock − alreadyQueued, ushort.MaxValue)`. Use the production line whose rates include that design's `IndustryTypeID`. If no line can build it, skip the job and still post the listing. `alreadyQueued` is the sum of `NumberOrdered − NumberCompleted` for that design on that colony.

Rows beyond `LogiBaseDB.Capacity` are not posted. Sort key: buy deficit (`hold − stock`) descending, then sell surplus descending. The goal `Message` names any row skipped for capacity or for a missing production line.

If the desired listing already matches the book and no job is needed, return no action for that row. A pass that returns no actions leaves the goal Active. This goal does not complete on its own. The player replaces it by assigning another goal.

Administrator quality is a function that returns 1.0 in this plan, including when `AdministratorDB` is missing. Listings match the policy numbers. Do not add a price or quantity curve here.

## Colony wake

In `AgentProcessor`, a managed entity with `ColonyInfoDB`:

- **Planning.** Run the planner, submit the actions, set Active, `ScheduleAgent` at `RecheckInterval`.
- **Active.** If an action for this goal failed, fail the goal and `ClearFor`. Remove succeeded actions. If nothing for this goal is still queued, plan again. Do not mark Completed because the queue is empty. Schedule the next recheck.

`AssignGoal` already creates `GoalsDB` and can wake immediately. Colonies do not need a hotloop processor.

`PruneImpossibleGoals`: `RunMarket` is possible only when the entity has `LogiBaseDB`. Add the goal type to `BaseWeights` at 0.5 so plan 9 has a number, and mark it impossible in the prune when no office is present. Do not call `PickAutonomousTask`.

Industry jobs go through `IndustryOrder2.CreateNewJobOrder`, the same order the API translator already dispatches. Do not add a second industry queue.

## Tests

`Pulsar4X.Tests/ColonyMarketOrderTests.cs`

- Policy with min 100 and stock 250 posts sell 150, reserve 100, buy 0.
- Stock 40 against min 100 posts buy 60 and sell 0.
- An industry job that still needs 80 of that good holds 80 even if min is 10: sell is `stock − 80` when stock is higher, buy is `80 − stock` when stock is lower.
- `AutoProduce` with stock under max enqueues one industry job for the gap, and a second pass does not enqueue another while the first remains.
- More policy rows than capacity posts only the highest deficits, and the goal message names a skipped row.
- A colony wake does not fail with "not a ship or a fleet". An empty action list leaves the goal Active.
- No logistics office: the run command is rejected.

## Later

Sector-wide orders, population consumption, an administrator skill curve, XP for admins, and a policy editor in the UI (plan 5 is read-only).
