# Bootstrap colony

Status: implemented as a single-ship goal. The fleet-level Bootstrap order is built separately (`FleetBootstrapPlan`, `FleetBootstrapCommand`; see section "Later"). It is not logistics plan 13.

One captained ship with a Construction Bay carries finished crates from one owned colony to another. The bay mounts them from the ship's own hold. The destination needs no office of its own, so this also installs the first office on an empty colony.

## What the player does

Order `BootstrapColonyCommand` on a ship with a captain: a source colony, a destination colony, and a list of crate-able designs with a count each. The translator rejects a ship with no captain, a fleet, no Construction Bay, no cargo store, a warehouse, the same colony twice, an empty list, a count under 1, an unknown design, a design that is not a crate (`LocalConstructionJob.IsCrate`), and a pair outside the ship's reach (`LogisticsSpan.PairReaches`).

Fleet Management has a Bootstrap colony order for a selected ship. It is offered only when the ship has a captain and a Construction Bay. A fleet never gets it. From and To are the Move goods trees: owned colonies with a warehouse, the same span blocking, and To leaves out the source. Below them each crate design with finished stock at the source (plus crates already in the hold) gets a count and an All button. The panel states the first failed check, in the translator's order, and the button stays disabled until there is none. The option lists and checks are in `BootstrapPicker` (`Pulsar4X.Api`), which reads snapshots only. `CargoItemView` carries `DesignId` and `IsCrate`, and `ShipSnapshot` carries `HasConstructionBay`.

## What the goal does

`BootstrapColonyPlan` derives the state from the world at every `Plan`. It picks crate instances once and keeps their ids in `Goal.BootstrapCrateIds`, taking crates already in the hold first, then the source warehouse. Each picked crate is mounted, aboard, or at the source.

1. Anything at the source: fly into transfer range and `LoadGoodsAction` (`plotDestination:false`). If the crates do not fit the hold, Fail "doesn't fit hold". There is one trip and no splitting.
2. Everything aboard or mounted: fly into transfer range of the destination. A ship that cannot get into range Fails with the reason, not a silent stall.
3. In range: `MountFromHoldAction` for all aboard crates.
4. Every picked crate mounted: Done. Only crates this goal picked count.

`MountFromHoldAction` wraps `AddToConstructionQueueOrder.CreateMount` and stays running until each job has left the bay's `BuildQueue` with the crate on the colony. The order is instant and the bay works daily, so without a running action the goal would complete early. It Fails, and removes its own jobs from the queue, when the colony is gone or out of transfer range, or a crate is neither mounted nor aboard.

## Rules kept

* `GoalType.BootstrapColony` is appended to the enum.
* It is not in `BaseWeights`, so it is never auto-picked. `GoalRoles` stays at the Task default.
* A queued load, mount or transfer for the goal makes the planner wait, and this check runs before the refuel interrupt.
* `LocalConstructionProcessor` is unchanged.

## Not built

* Factory production at the source when crates are not in stock.
* More than one trip (in this single-ship goal; the fleet Bootstrap order does the multi-trip runs).
* A `SkillDomains` line: no domain fits.
* Build order. All aboard crates are queued in the order picked.
  Already decided (2026-11-01): the player picks the order; crates are mounted in the order chosen (current behaviour stands).

## Later

The fleet `Bootstrap` order is built: it decides which facilities a new colony needs and hands `BootstrapColony` goals to its ships (`FleetBootstrapPlan.cs`). It is not described here; see `fleet-bootstrap-stub.md` and `colony-bootstrap-goal-plan.md` §7. Decided but not built: the fleet order will carry its crates in a sealed shipment (package) instead of `Goal.BootstrapCrateIds` (`fleet-bootstrap-stub.md` decision 15, `sealed-shipments.md`).
