# Move own goods

Status: implemented 2026-10-24. It is not logistics plan 13.

One trip, own warehouse to own warehouse, on the company's own ship. Haul, Haul contract, and Trade stay the posted-book orders. This order does not read the market, a reserve, or a logistics office.

Sealed shipments, repeating runs, HelpOwn, fuel, and rescue stay out of this plan.

## What the player does

Fleet Management gains **Move goods ...** next to Haul.

The panel picks an owned source colony, an owned destination colony, and lines from the source warehouse (`CargoStorageView` item id and a quantity). Minerals, fuel, materials, component instances, and uninstalled installations are all lines. A line is `CargoTransferItem` (`CargoItemView.Id`, units).

* **Ship with a captain.** `MoveGoodsCommand` assigns one goal. The captain flies to the source, loads, flies to the destination, unloads, and the goal completes.
* **Ship with an empty chair.** The player still queues actions. The queue does not replan.
  * **Load** and **Unload** work only in cargo-transfer range. Out of range rejects with "out of range" and escrows nothing.
  * **Move goods** queues the source flight, plotted now, then a load action. When that load finishes, the load action inserts the destination flight, plotted from where the ship is then, and an unload action immediately after itself. A move plotted at click time would start from the wrong place.
* **Fleet.** `FleetMoveGoodsCommand` assigns `FleetMoveGoods`. The fleet does not fly. It hands `MoveGoods` to one free cargo ship that has a captain and can reach both colonies. An empty chair is left alone. One ship, one trip. The lines are not split.

A colony ship mounting a crate stays the construction order. This order only puts the crate in a warehouse.

## Rules

Both ends are owned colonies with `CargoStorageDB`. The carrier is a ship with `CargoStorageDB` and a warp or newton drive. Source and destination are different colonies. No `LogiBaseDB`. No `MarketBook`, no `MarketExchangeAction`, no reserve, no price.

`CargoTransferOrder` moves the goods. Call it only once the ship is in range, the same rule as `CreateUnloadPair`: the constructor escrows immediately. Add `CreateLoadPair` as the mirror of `CreateUnloadPair`. The ship is primary. A positive amount loads from the colony. A negative amount unloads onto the colony. `HandleOrder` the colony half. The ship's half is the queued action. `TakeAvailibleAtOrder`.

In range means `MarketExchangeAction.InRange` (the larger transfer Δv). Getting there uses `BuyFuelPlan`'s move-into-range: `MovePlanner.TryBuildMoveActions`, and when that list is empty because the ship is already on the body but still outside cargo range, the low-orbit drop. Another system uses `JumpRoute.LegToward`. A colony the ship cannot reach fails the order.

Amounts clamp at escrow to stock still in the source and free space on the receiver. What does not fit stays where it was. The goal remembers, per line, how many units this trip actually loaded (`Loaded`). The unload moves that loaded count, not the ship's whole stack of the same id. `AddCargoByUnit` already stores the same `ICargoable` under that id, so a component instance stays that instance.

Outcomes:

* Nothing loaded (empty pile, or no free hold): Fail "nothing to load". Do not fly on.
* Load moved units, destination can take none of them: Fail "no place to unload". Goods stay aboard.
* Some or all of the load is in the destination: Completed. The message names units left at the source and units left aboard.
* Low fuel on a captain or fleet goal: Fail "low on fuel", same check as `FreighterPlan`. The empty-chair action queue does not add a fuel gate.
* Cancel returns every unit still in escrow to the store it was taken from, then drops `CargoTransferDB`. `CargoTransferOrder` has no `Stop` today, and `GoalCancel` only calls `Stop`, so a cancel would eat the escrow. Add that `Stop`. It covers this order and any other transfer cancelled the same way.

The goal is not in `GoalsDB.BaseWeights` and is not an autonomous pick. Any faction that owns the ship and both colonies can be given it. This plan does not decide when an AI faction issues it. `HelpOwn` stays fuel and rescue.

`FleetMoveGoods` grants Command on completion, same as `FleetFreighter`. The ship goal grants nothing.

## Goal and commands

Append `GoalType.MoveGoods` and `GoalType.FleetMoveGoods` so saved ordinals stay put.

On `Goal`, add a saved list of lines: item id, requested units, loaded units. Default empty. Add a saved leg, `Load` then `Unload`, default `Load`. Leave `CargoId` for the book orders.

Commands, in `Pulsar4X.Api/Commands.cs`:

* `MoveGoodsCommand(shipId, sourceId, destId, items)` — captain. Empty chair rejects "This ship has no captain. Plot an action instead."
* `QueueMoveGoodsCommand(shipId, sourceId, destId, items)` — empty chair. A seated captain rejects "This ship has a captain. Issue a goal instead."
* `LoadGoodsCommand(shipId, colonyId, items)` and `UnloadGoodsCommand(shipId, colonyId, items)` — empty chair, in range only. A captain gets the same reject.
* `FleetMoveGoodsCommand(fleetId, sourceId, destId, items)` — fleet only.

Translator rejects a missing warehouse, a colony the faction does not own, the same colony twice, an empty line list, a non-positive amount, and an item id that is not in the source warehouse at click time. Set `goal.Name` to "Move goods".

## Planners

`MoveGoodsPlan` in `GameEngine/Logistics/`. One ship. A fleet fails with "Move goods is one ship".

The agent already plans again when this goal's action queue is empty. The planner returns the next actions and does not stack a second transfer while one is queued.

* Leg `Load`, not in range of the source: move-into-range toward the source.
* Leg `Load`, in range: `CreateLoadPair` for `min(requested, stock, free hold)` on each line. Write `Loaded` from the clamped amount. If every line clamps to 0, Fail "nothing to load".
* Next wake, lines have `Loaded > 0`: set the leg to `Unload`.
* Leg `Unload`, not in range: move-into-range toward the destination.
* Leg `Unload`, in range: `CreateUnloadPair` for each loaded count, clamped to what is aboard and what the destination can store. If that clamp is 0, Fail "no place to unload".
* Next wake after that transfer: Completed.

`FleetMoveGoodsPlan` picks one child. A cargo ship has `ShipInfoDB`, `CargoStorageDB`, a warp or newton drive, and `CommanderID >= 0`. Free means no live goal of its own and no player-plotted actions (`FleetChildDuty.HasPlayerPlottedActions`). It must `JumpRoute.CanReach` both colonies. Closest to the source wins. Equal distance, lower entity id. Hand one `MoveGoods` with the lines and this goal as parent. No tanker `MoveTo`. No second ship.

No cargo ships: Fail "We have no subordinates to manage". Ships exist and none qualify: Fail "no ship". A child failure fails the fleet through the existing rollup. When the child completes, the fleet completes.

## Empty chair

`LoadGoodsAction` and `UnloadGoodsAction` are blocking `EntityAction`s. They escrow nothing in the constructor.

On start, out of range: Fail "out of range". In range: create the load or unload pair, `HandleOrder` the colony half, and stay running until the ship's transfer is finished. `Stop` stops that transfer.

`QueueMoveGoodsCommand` appends move-into-range toward the source, plotted now, then one `LoadGoodsAction` carrying the destination id and the lines. When the load finishes with `Loaded > 0`, insert immediately after this action the destination move-into-range, plotted then, and an `UnloadGoodsAction` for the loaded counts. Then this action succeeds. A load of 0 fails "nothing to load" and does not plot the destination.

`LoadGoodsCommand` and `UnloadGoodsCommand` enqueue only the matching action.

## UI

`FleetWindow` issue list. **Move goods ...** for a fleet, a captained ship, and an empty chair. Empty chair also lists **Load at colony in range** and **Unload at colony in range**.

The picker matches Haul contract's From / To trees, filtered to owned colonies with `CargoStorageView` instead of `MarketView`. Cargo buttons are warehouse piles with a quantity, not the sell/buy overlap. Submitting uses the command for that chair. The panel text says one trip, and that goods that do not fit stay behind.

`CreateTransferWindow` and `TransferCargoCommand` stay the in-range transfer between two stores. They still escrow at click time.

## Tests

`Pulsar4X.Tests/MoveGoodsTests.cs`. Call the planner directly, and run the empty-chair commands through `CommandTranslator`.

* Two owned colonies, warehouses, no logistics office. Captain, hold of 10, source has 50 iron. First plan is the flight. In range, the transfer loads 10. Source keeps 40. A posted sell on that iron stays at its old quantity. Next plan flies to the destination. Unload completes the goal. Destination has 10.
* A component instance moved by id is that same object in the destination warehouse and is gone from the source.
* Destination with no free volume fails "no place to unload". The loaded units stay on the ship.
* Empty chair `QueueMoveGoodsCommand` leaves `GivenGoal` unset, queues the source flight plus a load action, and a load action started out of range escrows nothing.
* Fleet hands the goal to the captained cargo ship and skips the empty chair.
* Another faction's colony is rejected.

Run from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor` and `TMPDIR=/home/se5a/.cache/build-tmp`, project `Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj`. Also run `FreighterPlannerTests` and `ApiCargoTransferTests`.

## Leave alone

`FreighterPlan`, `FleetFreighterPlan`, `FleetHaulContractCommand`, `TradePlan`, `MarketExchangeAction`, `SupplyLocal`, `OfferStockPlan`, `HelpOwn`, sealed shipments, repeating until a reserve is met, and the construction install path.
