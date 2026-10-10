# Standing survey and mine jobs

Status: not started.

Survey Flight and Mining Flight start with one standing goal each and keep doing it. The ship does not assign itself the next job. Player orders of `ServeyBodies` and `MineAsteroids` still complete when their work is done.

## Depends on

[Consigned charts and fixed prices](01-consigned-charts.md). The buy has to pay Strata and set Lode's survey flag before a mine goal has anything legal to dig.

## Outcome

Survey Flight's standing goal stays active. It hands Pathfinder one unscanned asteroid in the 0.2 AU neighborhood. When that survey finishes, the fleet consigns the new chart onto Ceres Depot the same way placement did, then hands the next rock. The seeded charts are already listed, so the first wake does not list them again.

Mining Flight's standing goal stays active. It buys one listed Strata chart Lode can afford and does not already have, then hands Prospector `MineAsteroids` for rocks Lode has surveyed. When the hold has ore, it sells to Ceres Depot's iron bid. When no chart is affordable and no surveyed rock remains, the goal stays active and wakes again later. It does not complete.

A child goal on the ship has `ParentGoalId` set to the fleet goal. When the child finishes, the ship stops. `FleetChildDuty` already treats a completed child as free, so the fleet's next wake can hand the next child. The ship must not call `AssignGoal` on itself. That replaces `GivenGoal`, and `BusyWithOwnWork` would then hide the ship from the fleet.

Pathfinder and Prospector mount a ship bridge. `default-ship-design-miner` stays on the system bridge because Earth uses it. The geo-surveyor design can change in place only if nothing else mounts it. Body span is enough: an asteroid survey uses the neighborhood, and `MineAsteroids` does not read span.

Ceres Depot keeps Offer stock as its only goal. It does not receive survey flags.

## What to build

Two fleet goal types, one planner each. Do not change the completion of player `ServeyBodies` or `MineAsteroids`.

- Survey standing goal. Fleet only. Child is `ServeyBodies` for one asteroid target. On completion of that child, write one consigned intel row if that body is now surveyed for Strata and the depot does not already list it.
- Mine standing goal. Fleet only. Child is a chart purchase through `IntelBook.TryBuy`, then the existing mine and sell path for ore the depot is buying. An empty belt returns active and schedules a recheck, for this goal only.

Assign both at the end of `PlaceFaction`, on the system fleet, not on the faction entity and not on the ship. `AssignGoal` with no time runs the first plan during placement.

## Tests

- After placement, Survey Flight's given goal is the standing survey goal, and Pathfinder's goal is a child of it or empty until the relay. The ship bridge is ship level.
- A finished Strata survey of a rock that was not seeded gains one depot row whose seller is Strata.
- After placement, Mining Flight's given goal is the standing mine goal. One `TryBuy` of an affordable chart sets Lode's survey flag. The mine planner will then accept that rock.
- With no affordable chart and no Lode survey, the standing mine goal is still active.
- A player `MineAsteroids` with nothing to mine still completes. A player `ServeyBodies` still completes.
- Completing the ship's child does not assign the ship a new given goal.

## Stop if

The open fault in `agent-notes.md` is still real: the first tick after a geo survey completes can replan into a warp-to-self and hang `ProcessSystem`. Plan 2's clock test should be one site. If that hang appears, stop. Fixing it is its own pass and blocks plan 3. Listing the chart from the planner, without relying on a second site, is still in scope.

## Out

No fuel purchase. `RefuelAt` does not pay, and plan 3 is that purchase. No ship-level re-issue. No second colony goal. The general active-only interrupt stays unwired.
