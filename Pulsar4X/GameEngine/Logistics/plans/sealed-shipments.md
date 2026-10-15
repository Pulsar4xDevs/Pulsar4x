# Sealed shipments

Status: plan from 2026-10-10. Not implemented. Do not build it until asked. It is not logistics plan 13, and it is not part of the 1–12 sequence. Office purses stay in `office-purses.md`. Ceres consignment stays in `GameEngine/Engine/Orders/plans/06-consignment.md`. Price finding stays the later economy note in `agent-notes.md`.

A logistics office buys listed cargo from a friendly office before any ship is in cargo range. The units leave the open piles and become one shipment, which can hold several goods. The buyer pays for the goods when the pile is sealed, and the buyer pays for the shipping by posting one haul on the intel list. Any free ship may claim that job, deliver the shipment, and be paid the fee.

The buying office can belong to the player or to another faction. The same seal runs for both. This is not a player-only order against an NPC.

The economy half is the seal, the two payments, and the intel row. The goals half is claiming that row onto a free ship. A goals-and-actions session can take that half from the section below without re-deciding the package or who pays.

## What is already true

`FactionStanceRules.CanTrade` is mutual Friendly or Allied. One-sided Friendly can see a market and cannot trade. Same-faction settlement moves cargo and does not touch the ledger. Cross-faction settlement writes `TransactionCategory.Trade`.

`MarketExchangeAction` runs only in cargo range. `CargoTransferDataDB` escrow is built with the transfer, and only once the ship is at the colony. It pulls units out of the pile for the ramp. It is not a hold you can open from another colony.

`Freighter` and `FleetHaulContractCommand` haul between colonies the faction owns. `Trade` buys at an ask and sells at a bid and keeps the spread. It will not hold a named pile for a named destination.

`OfferStockPlan.StockOf` returns 0 for a cargo id the faction library does not contain, so that id gets no policy row. `RunMarket` posts policy rows. The cargo book stays one row per library cargo id and still counts toward `LogiBaseDB.Capacity`.

`Goal` already has `CargoId`, `SourceEntityId`, `DestEntityId`, and `UnitShare`. `AgentProcessor.AssignGoal` is how a command becomes a `GivenGoal`. Append new `GoalType` values so saved enum ordinals stay put. An empty captain chair cannot take a goal.

## The shipment

`ShipmentCargo` is one `ICargoable` stored in the source colony's `TypeStore.Cargoables`, under a new int id from `Game.GetEntityID`. The store holds a count of 1. `UniqueID` is `shipment-` plus that id. `CargoTypeID` is `general-storage`. Every line in the shipment has to use that type. A line of another type stays out of this plan.

Mass per unit is the sum of each line's mass times its count. Volume per unit is the same sum for volume. The open piles lose those units, so the warehouse volume does not change.

The object carries the lines and the address. A line is a library cargo id, a count, and the price per unit paid for it. The address is the source colony id, the destination colony id, the buyer faction id, and the seller faction id. One shipment has one buyer and one seller.

Save every field with `[JsonProperty]`, including the int id and the lines. The stack key is that id. A reload that mints a new id loses the pile.

`CargoMath.AddRemoveCargoByVolume` already inserts an unknown `ICargoable` into `Cargoables` the first time that id is added. Removal uses the same id. Do not register the shipment in the faction cargo library.

While it sits in the warehouse it is not iron, fuel, or whatever it was. Recipes, `Trade`, and `Freighter` look at the open id and the book. They cannot take the sealed units. `OfferStock` and `RunMarket` do not list it.

## Who buys

The buyer is a logistics office, not a ship and not the human at the keyboard. Any office whose faction `CanTrade`s with the seller may seal. The player's colony and a friendly NPC colony call the same `ShipmentBook`. A player button is that office being ordered. It does not name a ship.

A colony has one `ActiveGoal`. Do not `AssignGoal` a second goal onto the buying office. That would replace `OfferStock`. The standing market pass may call the seal. The player command calls the same seal and does not replace the office goal.

Reject when `CanTrade` is false, when buyer and seller are the same faction (that haul is `Freighter`), when either side has no office, or when every line clamps to 0. One-sided Friendly can see the book and cannot seal.

## Buy

The buying office seals one or more lines against the seller's asks. Each line clamps to `SellQuantity`, to `Sellable`, and to what the buyer can still afford. Ask 0 needs no funds. Leave `Reserve` alone. Drop each `SellQuantity` by the sealed count. The next `RunMarket` wake posts from the stock that remains.

Goods payment happens at the seal, one ledger pair per line. Ask above 0: buyer expense and seller income, category `Trade`, description `shipment {id} {cargo id} {the other faction id}`. Ask 0 writes no line for that good.

The shipment stays in the seller's warehouse until a carrier docks. A later buy sees the lower sellable.

The buyer then posts the haul. The freight fee is a number the buyer sets on that post. This plan does not invent a tariff. Price finding can change the number later. A fee of 0 is a valid post.

## Sell

The seller does not seal and does not post the haul. Filling a bid is the buying office pulling the seller's stock. The goods sit at the seller, the buyer pays the bid at the seal, and the buyer posts the same intel job on its own office. Drop the buyer's `BuyQuantity` by the sealed count. Bid 0 writes no goods line.

When the player sells into a friendly bid, the remote office is the buyer of record. The player's office is the source. Do not let the seller charge that office and post a job in its name. The remote office's market agent is who accepts, pays, and posts. A player command may ask that office to accept a named set of lines from the player's stock, and it runs only when that office can afford the goods and chooses the fee.

## Cancel

The buying office may cancel only while the shipment is still entirely in the source warehouse and no ship has claimed the job. Unpack every line back to its open cargo id, restore the sell quantities or the buy quantity, reverse the goods lines, and remove the intel row. A price of 0 reverses nothing.

Once a ship has claimed the job, or any unit is aboard, this plan does not cancel and does not refund the goods. The goal runs to the destination. A lost ship is a later hole. Do not build insurance here. A claim that fails before loading clears the claimant so another free ship can take the row. The goods stay sealed.

## The haul on the intel list

The buyer posts one row on the buying office's `LogiBaseDB.Intel`. Append `IntelKind.Haul` so saved survey ordinals stay put. `Subject` is the shipment `UniqueID`. `Ask` is the freight fee. `ForSale` is true until a ship claims it. The result tag is `deliver`. The name lists how many goods are inside. The row also carries the source colony name and the destination colony name, so the carrier knows where to fly.

This row does not copy a secret and it does not pay anyone on claim. `IntelBook.TryBuy` for `Haul` is the wrong verb. Add a claim that sets `ForSale` false and stores the claimant ship id. `IntelListing` gains `ClaimantId`, default -1, so a survey row ignores it. A second claim is rejected.

The fee is paid when the shipment unpacks at the destination. The buyer pays the carrier's faction, category `Trade`, description `haul {shipment id} {carrier faction id}`. Fee 0 writes no line. The buyer's own ship is same-faction pay and writes no line either. The goods lines from the seal are not repeated.

Friendly offices already project for-sale intel. A hauler scans offices in its system and in known systems, the same set `MarketRun.Markets` walks, and reads `Haul` rows. Hostile offices are not in that projection.

## Goals and actions

This is the half that shares the goal pipeline. Implement it against `agents-and-goals-design.md`. The seal and the intel row can be tested with no time pulse. The claim can land in the same pass or after.

Append `GoalType.DeliverShipment`. One planner, `DeliverShipmentPlan`, in `GameEngine/Logistics/`, next to `FreighterPlan`. `Goal.CargoId` is the shipment `UniqueID`. `SourceEntityId` and `DestEntityId` are the colonies. `UnitShare` stays 0. The shipment count is 1, and the lines live on the object. Do not add route fields.

A free ship may take one posted haul. Free means no `GivenGoal`, or a `GivenGoal` that is already completed or failed, plus a cargo hold and a captain. Do not interrupt `Trade`, `Freighter`, mining, or survey. Do not replace a colony's standing goal. The ship's faction must `CanTrade` with the buyer and with the seller. The ship does not have to belong to either of them.

The planner does not call `MarketExchangeAction` and does not score a spread. The goods are already paid. The fee is paid at unpack.

On a ship:

- Shipment still at the source and the ship is out of cargo range: move there. Use the same move and jump actions a freight leg uses, including one remembered jump when the faction knows it.
- In range: a cargo transfer of that one shipment object onto the ship. The existing `CargoTransferDataDB` escrow runs for that ramp and only then.
- Aboard and out of range of the destination: move there.
- In range of the destination: unload, unpack every line into the buyer's warehouse, pay the fee, remove the intel row, and complete the goal.
- Shipment missing while still at the source: complete with the message `shipment closed` and clear the claim.
- Low fuel: `Fail("low on fuel")`, same as trade and freight. If nothing has loaded, clear the claim.

On a fleet: append `GoalType.FleetDeliverShipment`. The fleet does not fly or pay. It hands one child `DeliverShipment` to a cargo ship when the fleet itself is free and in `LogisticsSpan` of both colonies. A lone ship uses the known-system route, the way `Trade` does.

`SealShipmentCommand` is issued by the buying office. It carries the seller office, the lines, and the freight fee. It does not carry a ship. The translator seals, posts the intel row, and assigns no goal. `ClaimHaulCommand` targets the buying office, names the shipment and the claimant ship, and assigns `DeliverShipment` on that ship. The ship must be free and owned by the session. The office often is not.

`EngineGameServer.SubmitCommand` rejects a command whose target the session does not own. Allow `SealShipmentCommand` when the target is the session's office, or a friendly office the session can trade with and the session's office is the buyer of a bid-fill. Allow `ClaimHaulCommand` when the target has the haul row and the session `CanTrade`s with the buyer, the same kind of hole as `BuyIntelCommand`. The claimant ship still has to belong to the session. An NPC ship claims inside the planner, with no command.

## What the player sees

On a friendly market, **Buy and ship** seals that office's asks to one of the player's colonies and posts the haul on the player's office. The player types the fee. The button does not pick a ship.

On the player's intel list, a haul row shows the goods count, the fee, and Claim. Claim is for one of their free ships. Another faction's free ship claims through its planner and does not need this button.

A sealed pile can show in the source warehouse under its shipment name. It does not get a cargo-market bar.

No toolbar button. No change to survey goals, mine goals, or the Ceres standing jobs. Survey intel rows stay copy-on-sale.

## Later

Cancel after loading, a refund when the ship is destroyed, a computed freight tariff, office purses, and a hauler that interrupts work it is already doing. The posted job and the fee are in this plan.

## Files to add

- `Pulsar4X/GameEngine/Logistics/ShipmentCargo.cs` — the `ICargoable` and its saved address.
- `Pulsar4X/GameEngine/Logistics/ShipmentBook.cs` — seal, cancel, unpack, post the haul, pay the fee. One place for the stock edit, the listing counts, and both ledger payments.
- `Pulsar4X/GameEngine/Logistics/DeliverShipmentPlan.cs` — ship and fleet planners for the two new goal types.
- `Pulsar4X/Pulsar4X.Tests/ShipmentTests.cs` — seal, pay, cancel, and the in-range deliver.

## Files to change

- `Pulsar4X/GameEngine/Engine/Orders/GoalsDB.cs` — append `DeliverShipment` and `FleetDeliverShipment`.
- `Pulsar4X/Pulsar4X.Api/Commands.cs` — `SealShipmentCommand` and `ClaimHaulCommand`. Append `IntelKind.Haul`.
- `Pulsar4X/GameEngine/Logistics/IntelBook.cs` — claim for `Haul`. Survey grants stay as they are. `IntelListing.ClaimantId` defaults to -1.
- `Pulsar4X/GameEngine/Api/GameProjector.cs` — the haul row's name, the `deliver` tag, and the two colony names.
- `Pulsar4X/GameEngine/Api/CommandTranslator.cs` — seal and post, or claim and `AssignGoal`.
- `Pulsar4X/GameEngine/Api/EngineGameServer.cs` — the `CanTrade` hole for claim, then refresh the buying office and the ship.
- `Pulsar4X/Pulsar4X.Client/Interface/Displays/IntelDisplay.cs` — Claim on a haul row. Do not run the survey buy.
- `Pulsar4X/Pulsar4X.Client/Interface/Windows/EntityWindow.cs` and `ColonyManagementWindow.cs` — **Buy and ship** on a friendly market.
- `Pulsar4X/GameEngine/Engine/Orders/agents-and-goals-design.md` — the pointer already added with this plan. Leave the pipeline alone.

## Files to leave alone

`MarketBook` listing shape, `MarketExchangeAction`, `CargoTransferDataDB` and its escrow lifetime, `TradePlan`, `FreighterPlan`, `FleetHaulContractCommand`, `OfferStockPlan`'s ask-0 default, `RunMarketPlan`, the Geo, Pin, and Jump grants, Ceres plans 1–6, `office-purses.md`, and `colony-administrator.md`.

## Tests

`ShipmentTests`, from outside the repo, `DOTNET_ROLL_FORWARD=LatestMajor`, project `Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj`. The client library should build. The ImGui form is not clicked.

- A buy seal of two goods removes both open piles, adds one shipment whose mass and volume are the sum, drops both sell quantities, and writes one goods line per id. Ask 0 writes no line for that id. The buyer's intel list has one `Haul` row whose ask is the fee. No ship goal is assigned by the seal.
- A second buy cannot take the sealed units. `Sellable` is the stock that remains, minus reserve.
- The seal works when the buyer is a faction other than the session. Insufficient funds, one-sided Friendly, and same faction reject. No shipment and no intel row.
- `OfferStock` and a `RunMarket` wake do not add a cargo row for the shipment id. They do not remove the haul row.
- Cancel before a claim restores every line, the listing counts, the ledgers, and removes the row.
- A bid-fill is posted by the buying office. The goods sit at the seller. The seller's faction does not gain an intel row.
- A free ship of a third faction claims the row. A second ship is rejected. With that ship in range of the source, it loads the one shipment. With it in range of the destination, every line is unpacked, the buyer pays the carrier the fee, and the row is gone. The buyer's own carrier writes no fee line.

Do not advance a multi-hop clock in these tests. The move between two worlds can be asserted by the goal's source and destination ids and by the planner's next action type. Jump timing stays with the freight tests that already fly.
