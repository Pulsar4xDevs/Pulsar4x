# Plan 8 — Inter-system routes

Status: implemented on `TradeAndTransport`. Tests: `Pulsar4X.Tests/InterSystemRouteTests.cs`. Kept separate from plans 6 and 7.

## Outcome

A trader or freighter may pick a market in another star system the faction knows, and the move leg jumps to get there.

## Depends on

Plan 6 or plan 7 working inside one system. Jump actions that finish without throwing. `movement-issues.md` section 1.4 (`EndWarpMove` throws if the destination starts thrusting, and the `None` move type throws) is still open. A route that warps onto a jump point hits that `None` case. Fix that arrival before this plan plots a jump.

## What exists

- `JumpOrder` is a fleet action: warp the fleet to a gate, then `ShipJumpAction` per ship.
- `PathfindingManager` builds a graph of jump-point nodes for the faction's `KnownSystems` and `KnownJumpPoints`. It does not know colonies or markets.
- `MovePlanner.TryBuildMoveActions` targets an entity in the ship's current manager. It is the in-system leg only.
- Plans 6 and 7 restrict candidates to `ship.Manager` on purpose.

## Work

1. Gate: a ship-level jump that completes, used as an action on the Trade or Freighter goal. If only `JumpOrder` (fleet) exists, add a ship action that warps to one jump point and transits, rather than wrapping the ship in a temporary fleet.
2. Candidates expand from "this EntityManager" to "markets in `FactionInfoDB.KnownSystems`" that pass the same `CanTrade` (trader) or same-owner (freighter) filter.
3. Cost adds jump hops to the in-system hour term. Hop count comes from `PathfindingManager.GetPath` between the source system's jump graph and the dest system's. Unknown jump points are not candidates. If no path, that pair is skipped.
4. The action list for a cross-system leg is: in-system move to the chosen jump point, jump action, repeat per hop, then in-system move to the market, then the same exchange action as today. The ship Active replan still drops queued follow-ons, so each wake emits only the current leg, using the route already stored on the goal.
5. A market in an unknown system is invisible. Do not scan every `Game.Systems` entry.

## Tests

- Two systems linked by a known jump: the trader's first actions target the jump point, not the far colony directly.
- An unknown system with a better price is not chosen.
- No path: that pair is skipped; a same-system pair can still win.
- Arrival at a jump point does not throw out of the warp processor.

## Later

Fuel for the jump, a standing multi-system circuit, and showing foreign markets on the galaxy map. The score stays the plan 6 or plan 7 score plus hop time.
