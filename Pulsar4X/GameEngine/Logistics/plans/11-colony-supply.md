# Plan 11 — Colony supply

Status: not started. Colonies stay out of `FleetDB`. Do not fold this into plan 4.

## Outcome

The player gives one owned colony a supply order. That colony is the root office. The order covers other owned colonies on the same body, in the same gravity well, or in the same star system, clipped by the root's command bridge. Each member keeps its own market. The order posts books and hands out `RunMarket`. It does not fly cargo.

A governor entity can wait. Issuing this goal at a colony is enough.

## Depends on

Plan 4 (`RunMarket`, `ColonyMarketPolicyDB`). Plan 5b so a policy row is what the next `RunMarket` wake keeps: ask, bid, and reserve. Plan 10 is not required. Freight already carries a posted sell and a posted buy.

`CommandSpan.Of` already reads `AdminSpaceDB` on the commanded entity when it is not a fleet. A colony with no seats is Body.

## Goal type

One new type, `GoalType.SupplyLocal`, planner `SupplyLocalPlan`. The translator sets `Goal.SupplyMode` before `AssignGoal`:

- `Run` — hand out `RunMarket`. Do not write policy.
- `Balance` — make sure a surplus and a shortage of the same good can see each other, then hand out `RunMarket`.
- `Stockpile` — raise the root's reserve for goods the group already has a surplus of, then hand out `RunMarket`.

Leave `SupplyLocal` out of `BaseWeights` and out of `PickAutonomousTask`. No skill term. No XP in this plan.

Command: `SupplyLocalCommand(colonyId, mode)`. Reject a target that is not an owned colony, and a colony with no `LogiBaseDB`.

The goal does not complete on its own. A pass with nothing to do stays Active. The colony wake already re-plans when its queue is empty.

## Who is in the group

Owned colonies in the root's star system that have `LogiBaseDB`. The root is always in.

The anchor body is the root's `ColonyInfoDB.PlanetEntity`.

- **Body:** `PlanetEntity` is that body. A second colony on the same planet is in. A moon colony is out.
- **Well:** the body set, plus a colony whose planet is a direct `PositionDB` child of the anchor.
- **System:** every owned colony in the system.

`CommandSpan` picks the width from the root's seats: no seats or a ship-to-fleet seat is Body; colony, planet, or SOI is Well; system or wider is System. Sector and empire seats still mean this star system. A hierarchy above the system is later.

A member with a live goal that is not `RunMarket` is left alone. A member already running `RunMarket` as a child of this parent is left alone. Anyone else with at least one policy row is given `RunMarket` with this parent id.

## The root still posts

`AssignGoal(RunMarket)` on the root would replace `SupplyLocal`. The planner returns the root's own `RunMarket` actions (the existing pass, not a second formula) plus sub-goals for the other members. The colony Active branch already submits both.

## Balance

For each cargo id where one member has stock above its reserve and another has stock under its reserve:

- The side that already has a policy row keeps its Min, Max, Ask, Bid, and AutoProduce.
- The side that lacks a row gets one. Ask and Bid are copied from a row for that cargo already in the group. `AutoProduce` is false. `Max` equals `Min`. `Min` is the reserve already implied by stock (the surplus colony's current extra is not required; `Min` 0 is enough for a new exporter row so `RunMarket` posts the surplus). A shortage that already has a row already posts a buy. Balance does not raise or lower an existing Min.
- If nobody in the group has a price for that cargo, skip it. Do not invent a price.
- A full office does not gain the row. Name the cargo id on the goal `Message`. Do not remove a row the player already has.

`RunMarket` then posts sell as stock above hold and buy as hold above stock, including industry need. This plan does not write `MarketListing` quantities itself.

## Stockpile

The sink is the root. For each cargo the root already lists, if another member has stock above its own reserve, set the root's `Min` to `max(current Min, root stock + that surplus)`. Sum the surplus across members. Do not lower any Min. Do not change Ask, Bid, Max, or AutoProduce except to raise Max when Min would pass it (the plan 5b rule). Industry need still lifts hold above Min inside `RunMarket`.

The new Min stays after this goal is replaced. The player edits the reserve back. Other colonies keep selling whatever is above their own reserve.

## Tests

`Pulsar4X.Tests/ColonySupplyTests.cs`

- Run, two colonies on one body, both with a policy row: the sibling's `ActiveGoal` is `RunMarket` parented to this order, and the root's plan still returns its own post action.
- A Well seat includes a colony on a moon. A Body seat does not. A System seat includes an owned colony on another planet.
- A friendly foreign colony is not a member.
- Balance: one colony short of iron with an ask and a bid, one colony with iron stock and no iron row. The stocked colony gains a row, `AutoProduce` false, prices copied. The short colony's Min is unchanged.
- Balance with no price anywhere does not add a row.
- Stockpile raises the root Min and does not lower the exporter's Min.
- A member with a live non-market goal is left alone.
- An empty pass leaves `SupplyLocal` Active.

## Later

A governor entity whose children are colonies. Sector and empire width. Restoring the old Min when a stockpile order ends. Population consumption. Admin XP.
