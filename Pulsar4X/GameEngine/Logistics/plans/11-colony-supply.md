# Plan 11 — Colony supply

Status: implemented on `TradeAndTransport`. Tests: `Pulsar4X.Tests/ColonySupplyTests.cs`. Colonies stay out of `FleetDB`. Do not fold this into plan 4.

2026-10-06: membership uses `LogisticsSpan`, one step wider than survey. Earth's colony bridge covers Sol. A system bridge also covers one known jump. Survey is unchanged.

Reviewed against the colony agent wake, `CommandSpan`, `RunMarketPlan`, and city-hall seats. The order posts books and hands out `RunMarket`. Cargo already moves when a freighter, a fleet haul, or an autonomous freighter is running. This plan does not add a mover, a governor, or population demand.

## Outcome

The player gives one owned colony a supply order. That colony is the root office. Membership is `LogisticsSpan`: one step wider than the colony's survey span. A ship bridge covers the well. A colony, planet, or sphere-of-influence bridge covers this star system. A system bridge covers this system and one remembered jump. Each member keeps its own market. The order posts books and hands out `RunMarket`. It does not fly cargo.

A governor entity can wait. Issuing this goal at a colony is enough.

## Depends on

Plan 4 (`RunMarket`, `ColonyMarketPolicyDB`, the colony branch in `AgentProcessor`). Plan 5b so a policy row is what the next `RunMarket` wake keeps: ask, bid, and reserve. Plan 7 so a posted sell and a posted buy can be hauled. Plan 10 is not required.

`CommandSpan.Of` already reads `AdminSpaceDB` on the commanded entity when it is not a fleet. A colony with no seats is survey Body, so supply covers the well. Earth's city hall (`default-design-city-hall` on `admin-complex`) defaults to admin level Colony, which survey reads as Well and supply reads as this star system. A seated commander is not required: span reads the seat's `AdminLevel`, and an empty `CommanderID` still counts. A system seat, and anything wider, covers one remembered jump. That template and `tech-administration-level` already exist. Do not add a level, a tech, or a seat UI in this plan.

## What this plan uses, unchanged

- The colony Planning wake already submits `PlanResult.Actions` and `AssignGoal`s each `SubGoals` entry. The colony Active wake plans again when that goal's action queue is empty, and it leaves the goal Active. It has no fleet-style rollup: a child's failure does not fail the parent, and a child's completion does not complete it. Leave that branch as it is.
- `RunMarketPlan.Plan` does not read `goal.Type`. Call it for the root after the policy edit and return those actions. Do not copy the hold / sell / buy formula.
- `IndustryOrder2` stays in the queue until `NumberCompleted == NumberOrdered`. A root that auto-produces will not take a second supply pass until that job finishes. The first pass still hands out members, because Planning assigns sub-goals before the goal is Active. Do not change the wake so supply can rebalance during a long job.
- `SubmitActions` runs the root's actions. Member policy has to be on the blob before the child's own `RunMarket` plans. Write the member's `ColonyMarketPolicyDB` in the planner, then return the child's `RunMarket` goal. The child is scheduled after `RelayDelay`, so the row is already there.
- `PruneImpossibleGoals` only walks `BaseWeights`. Leaving `SupplyLocal` out of that dictionary keeps it out of autonomous pick and out of the prune. Do not add a trade or admin skill.

## Goal type

One new type, `GoalType.SupplyLocal`, appended after `FleetFreighter` so saved enum values stay put. Planner `SupplyLocalPlan`. Add `Goal.SupplyMode` with default `Run`:

- `Run` — hand out `RunMarket`. Do not write policy.
- `Balance` — make sure a surplus and a shortage of the same good can see each other, then hand out `RunMarket`.
- `Stockpile` — raise the root's reserve for goods the group already has a surplus of, then hand out `RunMarket`.

The translator sets the mode before `AssignGoal`.

Leave `SupplyLocal` out of `BaseWeights` and out of `PickAutonomousTask`. No skill term. No XP. `SkillDomains.DomainOf` stays as it is; this goal does not complete, so it grants nothing.

Command: `SupplyLocalCommand(colonyId, mode)`. Reject a target that is not an owned colony, and a colony with no `LogiBaseDB`.

The goal does not complete on its own. A pass with nothing to do returns Continue with an empty action list and stays Active.

`SupplyMode` lives in `Pulsar4X.Api` beside the command. `Goal.SupplyMode` stores that value. The translator sets `goal.Name` to the button label below, so the existing order line reads Run markets, Balance, or Stockpile.

## Who is in the group

Owned colonies with `LogiBaseDB` in systems `LogisticsSpan` reaches. The root is always in. Same `FactionOwnerID` only. A friendly foreign colony is not a member. Do not call `MarketRun.Markets()`: that list unions every known system, and a supply order stops at one remembered jump.

The anchor body is the root's `ColonyInfoDB.PlanetEntity`. Membership uses that planet, not `CommandSpan.Expand`. A moon colony is parented to the moon, so it is a grandchild of the planet and `Expand` will not see it.

`LogisticsSpan.Of(root)` picks the width. Survey Body (no seats, or a ship-to-fleet seat) is supply Well. Survey Well (colony, planet, or SOI) is this star system. Survey System (system, sector, or empire) is this system and one remembered jump. Do not invent a wider empire rung.

- **Well:** the anchor planet, a second colony on that planet, and a colony whose planet is a direct `PositionDB` child of the anchor. Read `anchor.PositionDB.Children` (`SafeList` is `IEnumerable`; LINQ `Contains` works).
- **System:** every owned colony with an office in the root's star system.
- **One jump:** those colonies, plus owned offices in systems one remembered jump from the root's star. Two hops stay out.

Tests set seats the way `FleetLogisticsTests.AttachBridge` does. They do not need a city hall component. A real Earth with its city hall covers Sol without a seated commander.

## Who is handed RunMarket

The root does not get `AssignGoal(RunMarket)`. That would replace `SupplyLocal`. The planner returns the root's `RunMarketPlan` actions.

A member is left alone when its `ActiveGoal` is Planning or Active and any of these hold:

- the goal type is not `RunMarket` (a second `SupplyLocal` on that colony stays), or
- it is `RunMarket` and `ParentGoalId` is this order.

Anyone else with at least one policy row after this pass's edits gets `new Goal(GoalType.RunMarket) { ParentGoalId = parent.Id }`. A failed or completed `RunMarket` can be handed again. A colony with no policy row is not handed a goal.

`RelayDelay` with no commander is non-zero, so a test that needs `ActiveGoal` on the sibling calls `AssignGoal(sibling, subGoal)` itself. `Plan()` is what asserts the sub-goal list, the same way fleet logistics tests do.

## Balance

Look at policy, not the public book. A row's reserve is its `Min`. A colony with no row has reserve 0. Surplus is stock above that reserve. Shortage is stock below `Min`, which means the short side already has a row. Stock cannot be below 0, so a colony with no row is never the shortage.

For each cargo id where at least one member is short and at least one other member has surplus:

- The side that already has a row keeps its Min, Max, Ask, Bid, and AutoProduce.
- The side that lacks a row gets one. Ask and Bid are copied from an existing row for that cargo in the group. When several rows have a price, copy the one on the colony with the lowest entity id. `AutoProduce` is false. `Max` equals `Min`. `Min` is 0, so `RunMarket` posts the surplus as a sell. Do not average prices and do not read the listing.
- If nobody in the group has a price for that cargo, skip it. Do not invent a price.
- A full office does not gain the row. Count rows against `LogiBaseDB.Capacity` before adding. Name the cargo id on the goal `Message`. Do not remove a row the player already has.

`RunMarket` then posts sell as stock above hold and buy as hold above stock, including industry need. This plan does not write `MarketListing` quantities itself.

## Stockpile

The sink is the root. For each cargo the root already lists, sum stock-above-reserve on the other members. If that sum is greater than 0, set the root's `Min` to `max(current Min, root stock + surplus)`. Do not lower any Min. Do not change the exporter's row. On the root, leave Ask, Bid, and AutoProduce alone, and raise Max when Min would pass it (the plan 5b rule).

The new Min stays after this goal is replaced. The player edits the reserve back. Other colonies keep selling whatever is above their own reserve.

A later wake can raise Min again when the root's stock or the group's surplus has grown. An exporter that already has `AutoProduce` can keep that ratchet going. This plan does not turn AutoProduce off. The next raise waits until the root's queue for this goal is empty, so a root industry job pauses it.

## UI

The three modes are buttons on the Market tab of Manage Colonies (`ColonyManagementWindow`), under the bars in `MarketBarsDisplay`. That editor is already owner-only: show the buttons when `MarketView` is present and `CanEdit` is true. A colony with no office keeps the current "no logistics office" line and shows no buttons. There is no toolbar button and no new window.

Each button sends `SupplyLocalCommand` for the selected colony:

- Run markets — `SupplyMode.Run`
- Balance — `SupplyMode.Balance`
- Stockpile — `SupplyMode.Stockpile`

Sending one replaces the colony's current goal. The buttons stay on screen while an order is active.

Above the buttons, one line from the colony's logistics reach: "Covers this well", "Covers this system", or "Covers this system and one jump". `ToColonyView` fills that label from `LogisticsSpan.Of` for the owning faction, and leaves it empty on a friendly snapshot. The fleet command line still shows the survey span. The player does not pick the width.

Under the buttons, one sentence: Stockpile raises this colony's reserve, and that reserve stays after the order is replaced.

`OrdersView` already carries the goal name, status, and message, and the entity window already prints them. The Market tab prints those three when the name is one of the labels above, so a cargo skipped for a full office is visible here. A `RunMarket` handed to another colony shows on that colony's own goal line.

This tab does not list the member colonies, edit prices, or change the city hall. Listings stay on the bars above.

## Tests

`Pulsar4X.Tests/ColonySupplyTests.cs`. Call `SupplyLocalPlan.Plan` directly. Give each colony planet a `SystemBodyInfoDB` only if a test also runs an in-range exchange. These tests do not need one.

- Run, two colonies on one body, both with a policy row: `Plan()` returns one `RunMarket` sub-goal for the sibling, parented to this order, and the root's actions include its own post. `AssignGoal` on that sub-goal makes the sibling's `ActiveGoal` `RunMarket`.
- A ship seat includes a colony on a moon and excludes another planet. A planet seat includes another planet in the system and excludes one jump. A system seat includes one known jump and excludes two hops. The moon is a `PositionDB` child of the anchor planet. The moon colony's `PlanetEntity` is the moon.
- A friendly foreign colony is not a member.
- Balance: one colony short of iron with an ask and a bid, one colony with iron stock and no iron row. The stocked colony gains a row, `AutoProduce` false, `Min` 0, prices copied. The short colony's Min is unchanged.
- Balance with no price anywhere does not add a row.
- Stockpile raises the root Min by root stock plus the exporter's surplus, and does not lower the exporter's Min.
- A member whose active goal is `MoveTo`, or whose active goal is another `SupplyLocal`, is left alone.
- An empty pass returns Active (Continue), not Completed.

## Later

A governor entity whose children are colonies. Sector and empire width past this star system. Restoring the old Min when a stockpile order ends. Population consumption (population grows today and does not eat cargo). Admin XP. A member list on the Market tab. Rebalancing while a root industry job is still in the queue.
