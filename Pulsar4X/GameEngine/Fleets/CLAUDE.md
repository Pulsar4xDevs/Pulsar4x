# Fleets Subsystem — Developer Reference

**What it does:** Groups ships into named fleets so the player gives orders to the fleet, not to individual ships. A fleet is just an entity with a `FleetDB` blob — ships are assigned to it by entity ID. The fleet can have sub-fleets (tree structure), a flagship, and standing conditional orders.

**Why it matters for ground combat:** Ground forces need transport ships. Transport ships are in fleets. The invasion order ("land troops") is issued by a fleet in orbit over a target colony. Ground forces that travel between systems ride inside fleet-managed transport ships. **Phase 4 wires directly into this subsystem — read this before building the landing order.**

---

## Files

| File | Role |
|------|------|
| `FleetDB.cs` | The DataBlob. Holds flagship ID, parent fleet ID (tree hierarchy via `TreeHierarchyDB`), `StandingOrders` list, and `InheritOrders` flag. |
| `FleetFactory.cs` | Creates fleet entities. `FleetFactory.Create(manager, factionID, name)` — creates the entity with `FleetDB`, `OrderableDB`, `NameDB`, `OwnedDB`. |
| `FleetOrder.cs` | All player-issued fleet commands — `Create`, `Disband`, `ChangeParent`, `AssignShip`, `UnassignShip`, `SetFlagShip`, `ToggleInheritOrders`. Each is an `EntityCommand` with `ActionLaneTypes.InstantOrder`. |
| `FleetOrderProcessor.cs` | The `IInstanceProcessor` that executes `FleetOrder` commands. |
| `RefuelAction.cs` | INavAction — fleet-level refueling action. |
| `ResupplyAction.cs` | INavAction — fleet-level resupply action. |
| `ServeyAnomalyAction.cs` | INavAction — survey action (anomaly investigation). |

---

## Data Model

```
FleetDB extends TreeHierarchyDB
  FlagShipID         int        — entity ID of the flagship ship
  InheritOrders      bool       — if true, uses parent fleet's orders
  StandingOrders     SafeList<ConditionalOrder>
```

A fleet entity has these DataBlobs: `FleetDB`, `OrderableDB`, `NameDB`, `OwnedDB`.

Ships are assigned to fleets through `FleetOrder.AssignShip()`. The ship's entity ID is added to the fleet's member list (managed inside `FleetOrderProcessor`). The fleet doesn't store members directly in `FleetDB` — membership is on the ship's side (the ship has a parent fleet ID via the tree hierarchy).

---

## How Orders Work

All fleet commands go through `FleetOrder` (an `EntityCommand`). The pattern:

```csharp
var order = FleetOrder.AssignShip(factionId, fleetEntity, shipEntity);
StaticRefLib.OrderHandler.HandleOrder(order);
```

`FleetOrderProcessor` is an `IInstanceProcessor` — it executes orders at the scheduled datetime, not on a hotloop. Orders are instant (`ActionLaneTypes.InstantOrder`), so they fire immediately.

---

## Sub-Fleets (Tree Hierarchy)

`FleetDB` extends `TreeHierarchyDB`, which means fleets nest. A transport group can be a sub-fleet of the invasion fleet. When the parent fleet moves, sub-fleets with `InheritOrders = true` follow. Sub-fleets can be detached into independent fleets via `ChangeParent`.

This is the mechanism ground combat needs: the assault echelon (transports + troops) starts as a sub-fleet of the invasion fleet, detaches to land, then the escort holds orbit.

---

## INavActions (Movement Actions Fleets Can Take)

`RefuelAction`, `ResupplyAction`, and `ServeyAnomalyAction` implement `INavAction` — they are movement-phase actions tied to specific waypoints or targets. The landing action for ground combat will be a **new INavAction** — `LandTroopsAction` or similar — that triggers when the fleet is in orbit over a target colony and the player issues the invasion order.

See `Movement/CLAUDE.md` for the full INavAction pattern.

---

## Pulsar Status

Fleet management is **fully functional**. Ships can be created, assigned to fleets, given movement orders, and the fleet tree hierarchy works.

**What does NOT exist yet (Phase 4 will add):**
- Any ground-force-related fleet action (there is no "load troops" or "land troops" action)
- Any cargo-loading order that specifically tracks ground units as cargo (though the cargo system exists — `Storage/`)
- Fleet composition checks for "orbital dominance" (no concept of controlling the space over a colony)

---

## Phase 4 Hook Points

| Ground combat need | Hook here | Notes |
|-------------------|-----------|-------|
| Load ground units into transport | `FleetOrder` + cargo system | Ground unit entities go into the transport ship's `CargoStorageDB` |
| Move to target system | Fleet movement (already works) | No new code — fleet moves like any other fleet |
| Establish orbital dominance | New check in invasion order | Query enemy ships in same orbit zone over target colony |
| Issue landing order | New `LandTroopsAction : INavAction` | Fires when fleet in orbit, checks dominance, creates `GroundCombatDB` on colony |
| Detach escort sub-fleet | `FleetOrder.ChangeParent()` (already works) | Transports detach, escorts remain in orbit |
