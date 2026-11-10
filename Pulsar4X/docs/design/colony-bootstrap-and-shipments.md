# Colony bootstrap and sealed shipments

Reference, not a plan. Everything below is built (PR #488 single-ship goal, PR #492 sealed shipments, PR #493 fleet Bootstrap packages). The plan files for these were deleted in the 2026-11 cleanup and the out-of-repo stubs (`fleet-bootstrap-stub.md`, `fleet-order-stub.md`, `colony-bootstrap-goal-plan.md`) were folded in here. What is still open is in [crate-vs-site-only-designs](../plans/crate-vs-site-only-designs.md) and the [master plan](../master-plan.md).

## Single-ship goal: `BootstrapColony`

One captained ship with a Construction Bay carries finished crates from one owned colony to another, and the bay mounts them from the ship's hold. The destination needs no office, so this also installs the first office on an empty colony.

- Command: `BootstrapColonyCommand` (source colony, destination colony, crate designs with a count each). The translator rejects: no captain, a fleet, no Construction Bay, no cargo store, a warehouse, the same colony twice, an empty list, a count under 1, an unknown design, a design that is not a crate (`LocalConstructionJob.IsCrate`), and a pair outside the ship's reach (`LogisticsSpan.PairReaches`). The Fleet Management picker is `BootstrapPicker` (`Pulsar4X.Api`, snapshots only).
- Planner: `BootstrapColonyPlan` re-derives state from the world every `Plan`. It picks crate instances once (crates already in the hold first, then the source warehouse) and keeps them in `Goal.BootstrapCrateIds`. Steps: load at source (Fail "doesn't fit hold" if they do not fit; one trip, no splitting) → fly into transfer range of the destination (Fail with a reason if it cannot) → `MountFromHoldAction` for everything aboard → Done when every picked crate is mounted.
- `MountFromHoldAction` wraps `AddToConstructionQueueOrder.CreateMount` and keeps running until each job has left the bay's `BuildQueue`. It Fails, and removes its own jobs, when the colony is gone or out of range or a crate is neither mounted nor aboard.
- `GoalType.BootstrapColony` is appended to the enum, is not in `BaseWeights` (never auto-picked), and has no `SkillDomains` line. A queued load, mount or transfer for the goal makes the planner wait, and this check runs before the refuel interrupt. `LocalConstructionProcessor` is unchanged. The player picks the build order; crates mount in the order chosen.
- The warp-brake gap is fixed: `goal` is passed at all three call sites in `BootstrapColonyPlan` (the destination move, `PlanPackage`, and `PlanLoad`). `Bootstrap_FlyingToAFarColony_UsesTheWarpBrake` covers each one.

## Fleet order: `Bootstrap`

`FleetBootstrapPlan` / `FleetBootstrapCommand` / `BootstrapColonyPlan`. It sets up a whole new colony and hands `BootstrapColony` goals to its ships (sub-fleets included). A ship with its own `BootstrapColony` goal keeps it; the fleet does not duplicate that work.

- **Splitting and trips.** The planner splits the crate list across ships by hold capacity and logistics reach. Multi-trip back-and-forth runs are the main value over the ship goal.
- **Default list lives in mod data** (`GameData/scenarios/sol-default/bootstrapDefaults.json`, loaded by basemod), minimal and self-sustaining, fixed for v1 and tweakable per order: Construction Office, Factory, one Auto Mine (the crate-able RoboMiner, not the site-only Mine), the crate-able Planetary Spaceport Complex (`default-design-planetary-spaceport-crate`), and the crate-able `infrastructure`.
- **Refinery is built on site**, after the factory is up; it is not shipped.
- **Prioritizable crate list.** The fleet window has a reorderable list, a quantity per crate, and add-extra-crates. The player's priority order decides the first-trip layout; there is no fixed office-first rule. The UI works like the single-ship one, and the fleet manages ships and trips.
- **Resources are derived**, not picked: the player lists the site-only facilities to build at the destination, and the order hauls design cost × count of resources for them. Crates arrive before resources (crates are what let the colony use the resources).
- **Packages.** The order carries crates and resources in one non-market sealed shipment made when the order is built (`BootstrapPackage.TrySeal`, called from `CommandTranslator`, sealing last after every other rejection). It is released with `ShipmentBook.Release` when the order fails or is cancelled. A package replaces the crate-id claim and the old `MoveGoods` resource trips; keeping both would double-book cargo.
- **Who mounts.** Whichever of the ship's Construction Bay and the destination colony has more free construction capacity mounts the crates; a tie goes to the colony so the ship is freed. A crate mounted by the ship stays in its hold until built. No separate unpack step.
- **Crates carry no seal mark.** Resource lines aboard are `SealedLineCargo`; a crate is never wrapped, and once released is an ordinary crate that another goal's crate pick or a `MoveGoods` could take. Accepted.

### Infrastructure suitability check (order-build time only)

Infrastructure only works inside its design's window (`InfrastructureProcessor.SumProvidedCapacity`, `GetPopulationSupportValue`). One crate template (`infrastructure`, "Prefab Infrastructure", `ShipCargo, PlanetInstallation`) has four stock designs (2026-11-02 balance pass): Earth-Standard `default-design-infrastructure` (8.8–10.8 m/s², 0.9–1.1 atm), `-mars` (3.0–4.9 m/s², 0.85–1.05 atm), `-luna` (1.4–1.8, 0–0.2) and `-ceres` (0.1–0.5, 0–0.2). A separate site-only template `infrastructure-site` has `default-design-infrastructure-site`, which Earth installs; it cannot be freighted or sealed.

- Search only the source planet's cargo for any infrastructure design that fits the destination body. Designs the faction owns elsewhere do not count.
- Best fit = the window whose midpoint is closest to the body's gravity and pressure; each distance is divided by that design's window width before combining; a tie goes to the narrowest combined window. Best fit is the default pick and the player can tweak it in the UI.
- No fit: the order cannot be started; tooltip "No suitable infrastructure for [destination planet]".
- The check runs once, when the order is built, and not again at execution. On success the infrastructure is sealed with `ShipmentBook.TrySeal`.

## Sealed shipments (`ShipmentCargo`, `ShipmentBook`)

A sealed shipment is a logical hold on cargo between a source and a destination, not a physical container. It is general: a market sale (buyer, seller, prices, fee, an `IntelKind.Haul` row) is one use; a non-market package for a fleet order is another.

- `ShipmentCargo` is one `ICargoable` in the source colony's `TypeStore.Cargoables` under an id from `Game.GetEntityID`, count 1, every field saved with `[JsonProperty]` (a reload that mints a new id loses the pile). It is not registered in the faction cargo library. Mass and volume per unit are the sum of its lines; the open piles lose those units. While sealed, recipes, `Trade`, `Freighter`, `OfferStock` and `RunMarket` cannot touch it.
- **Contents.** Anything that can go in ship cargo: resources, fuel, minerals, and crates (tracked by instance id). The test is the `ShipCargo` flag on `ComponentMountType` ("Cargo Hold") for components, via `CargoMath.Accepts`, and a matching hold type for resources. Designs are not cargo, and a site-only design (no `ShipCargo`) cannot be sealed.
- **One shipment per cargo type.** A mixed load becomes one package per type. A package is tied to one order, not reusable, and is released and removed at the destination.
- **Splits at the line, never inside one.** A package can go across several ships or trips; a crate travels whole; it is fully delivered when every line has arrived.
- **Aboard.** Lines leave the sealed object and travel as themselves. A resource line is a `SealedLineCargo` with its own `Game.GetEntityID()` id (copies type, mass and volume per unit, keeps the resource id and shipment id), so escrow, `MoveFromEscro`, mass and `Clone` work unchanged and industry, mining and refuelling cannot take it.
- **Destination full.** It unloads what fits; the ship stays and retries.
- **Market path.** The buying office pays the goods at seal time and posts one `IntelKind.Haul` row with the freight fee. A free ship claims the row (`DeliverShipment` goal, `FleetDeliverShipmentPlan` for fleets), does not call `MarketExchange`, and is paid the fee on delivery. Do not interrupt a ship that already has an order; no second `ActiveGoal` on the colony. A claim that fails before loading clears the claimant so another ship can take it.
- **Cancel** only while the shipment is wholly in the source warehouse and unclaimed: unpack every line, restore sell/buy quantities, reverse goods lines, remove the intel row. After a claim or any unit aboard there is no cancel and no refund.
- Append new `GoalType` values so saved enum ordinals stay put. An empty captain chair cannot take a goal.

Not built (carried to the master plan): cancel after loading, refund when the ship is destroyed, a computed freight tariff, a hauler that interrupts work it is already doing.
