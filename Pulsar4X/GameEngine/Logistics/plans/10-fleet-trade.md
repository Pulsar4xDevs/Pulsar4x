# Plan 10 — Fleet trade and freight

Status: not started. Leaf `Trade` and `Freighter` stay one ship. Do not fold this into plans 6, 7, or 8.

## Outcome

The player gives a fleet one trade job or one freight job. The fleet splits that job across its cargo ships. Each ship still buys, moves, and sells on its own goal. The fleet does not fly, buy, or sell.

How far the open orders look is the flagship's command bridge (`CommandSpan`), the same clip survey uses. A contract names both ends and does not grow with the bridge.

## Depends on

Plans 6 and 7, including the route fields on `Goal` and `MarketRun` legs. The fleet hand-down already exists: a planner returns `SubGoals`, and the fleet Active branch re-plans to top up (`AgentProcessor`). `CommandSpan` and `ShipInfoDB.Tanker` already exist for survey.

Plan 8 is not required. Candidates in this plan are markets in the fleet's current star system. A convoy that jumps is later.

Plan 11 is not required. These orders carry listings that are already posted.

## Goal types

Two new task types, each with its own planner. Leaf planners keep failing a fleet with `"Trade is one ship"` and `"Freighter is one ship"`. `TradeCommand` and `FreighterCommand` stay ship commands. The existing fleet-rejection tests stay green.

- `GoalType.FleetTrade` — `FleetTradePlan`
- `GoalType.FleetFreighter` — `FleetFreighterPlan`

Leave both out of `GoalsDB.BaseWeights` and out of `PickAutonomousTask`. This plan is player-issued. On fleet completion, `SkillDomains.DomainOf` grants Command. Do not add a trade skill, and do not put a skill term in either planner.

## Commands

`FleetTradeCommand(fleetId, bodyId)` assigns `FleetTrade` with `TargetEntityID = bodyId` and an empty route.

`FleetFreighterCommand(fleetId, bodyId)` assigns `FleetFreighter` the same way.

`FleetHaulContractCommand(fleetId, sourceId, destId, cargoId)` assigns `FleetFreighter` with `CargoId`, `SourceEntityId`, and `DestEntityId` set. `TargetEntityID` is the destination colony's planet when it has one, so the tanker has a body to orbit.

The translator rejects a commanded entity that is not a fleet, a body or colony outside the fleet's system, a contract colony the faction does not own, and an empty cargo id.

## What the fleet planner does

Branch shape follows `GeoServeyPlanners.PlanSubGoals`. Return sub-goals. Do not write child goals from the agent side beyond the sub-goal list it already applies.

A cargo ship is a child with `ShipInfoDB`, `CargoStorageDB`, and a warp or newton drive. The tanker (`ShipInfoDB.Tanker`) is not a cargo ship.

A ship is free when it has no `ActiveGoal`, or that goal is Completed or Failed. A live goal that is not a child of this parent stays. Survey will reassign such a ship; this plan does not.

Open trade and open freight take markets in the fleet's `EntityManager` only, then clip to the anchor body:

- **Body** (no seats, or a ship-to-fleet seat): the anchor, plus markets parented to it.
- **Well** (colony, planet, or SOI seat): the body set, plus markets parented to a direct child of the anchor (a colony on a moon).
- **System** (system seat or wider): every matching market in the fleet's system.

Trade candidates pass `CanTrade`. Freight candidates are owned colonies (`ColonyInfoDB`, same `FactionOwnerID`). Scores stay the leaf scores: trade is bid minus ask minus hours; freight is units, then shorter distance. Neither score gains a skill term.

**Trade parcel.** Walk pairs the way `TradePlan` does. Skip a pair already given to an active child of this parent. Give the closest free ship the best remaining pair with a positive score. One pair per ship. Do not split one pair across ships.

**Freight parcel.** Walk pairs the way `FreighterPlan` does. Remaining units are the posted sell, the sellable stock, and the buy quantity, minus the `UnitShare` of active children already on that pair. Give the closest free ship `min(remaining, free space)`. One load per child goal.

**Contract.** Ignore span and ignore every other pair. Split that cargo between the named colonies with the freight unit rule. Both colonies must sit in the fleet's system.

**Tanker.** If the fleet has a tanker and it is not already on a `MoveTo` for this parent aimed at the anchor, add that `MoveTo`. Same idea as the survey tanker.

No free cargo ship: `Fail("We have no subordinates to manage")`. Ships exist and nothing qualifies: `Fail("no route")` for trade, `Fail("no haul")` for freight. Work remains, or a child is still out: `Continue` with the new sub-goals (possibly none). No work left and nobody is out: `Completed`.

## One load on the child

Add `Goal.UnitShare` (`long`, 0 if unset). The fleet planner sets it on the child `Trade` or `Freighter` goal, along with the route. `0` means today's leaf: the ship decides the size.

A child whose `ParentGoalId` is set:

- Buys at most `UnitShare` (freight still also caps at the buy request).
- After that load is sold and the hold is empty: `Completed` (`"share delivered"`).
- The listing is already gone, or the posted quantity is already 0: `Completed` (`"share closed"`). Do not `Fail("listing gone")`. The fleet rollup treats any child `Failed` as failure of the parent, and a filled listing is normal.
- Low fuel still `Fail("low on fuel")`. The parent then fails under the current rollup. Cancelling sibling goals is the open item in `agents-and-goals-design.md`, not this plan. Tests use warp-only ships so refuel does not trip.

A player-issued ship goal has no parent and `UnitShare` 0. Its fail messages stay as they are.

The fleet Active wake already re-plans, so a ship that completed a share can be given the residual on the next wake.

## Tests

`Pulsar4X.Tests/FleetLogisticsTests.cs`. Call `Plan` directly for the parcel, the way `GeoSurveyFleetTests` does.

- Two ships, two positive trade pairs: each ship gets a different pair.
- One trade pair: the second ship is not given a goal.
- Contract of 80, holds of 30 and 50: shares 30 and 50. A leaf buy is capped at its share.
- A ship with a live `MoveTo` is left free of this order.
- A Body bridge does not pick a colony on a moon. A Well bridge does. A System bridge picks an owned colony on another planet. Freight ignores a friendly foreign buy.
- A fleet-handed child whose listing is gone returns Completed. A ship-issued `Freighter` on a fleet entity still rejects with `"Freighter is one ship"`.

## Later

A convoy that jumps, then feeds the far system. Fuel in the score. Several goods in one hold. Sibling-cancel when one ship fails.
