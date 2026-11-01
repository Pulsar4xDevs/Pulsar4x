# Sealed shipments

Status: plan from 2026-10-10. Not implemented. Do not build it until asked. When it is built, it comes before the fleet-order infrastructure suitability check, which depends on it (see Packages). It is not logistics plan 13, and it is not part of the 1–12 sequence. Office purses stay in `office-purses.md`. Ceres consignment stays in `GameEngine/Engine/Orders/plans/06-consignment.md`. Price finding stays the later economy note in `agent-notes.md`.

A shipment is a sealed pile with a source and a destination. It can be sealed in two ways. The non-market path seals goods with no sale: no buyer or seller factions, no prices, no fee, no intel row, and no ledger lines. The market path is one use case. In it, a logistics office buys listed cargo from a friendly office before any ship is in cargo range. The units leave the open piles and become one shipment, which can hold several goods. The buyer pays for the goods when the pile is sealed, and the buyer pays for the shipping by posting one haul on the intel list. Any free ship may claim that job, deliver the shipment, and be paid the fee.

On the market path, the buying office can belong to the player or to another faction. The same seal runs for both. This is not a player-only order against an NPC.

On the market path, the economy half is the seal, the two payments, and the intel row. The goals half is claiming that row onto a free ship. A goals-and-actions session can take that half from the section below without re-deciding the package or who pays.

## What is already true

`FactionStanceRules.CanTrade` is mutual Friendly or Allied. One-sided Friendly can see a market and cannot trade. Same-faction settlement moves cargo and does not touch the ledger. Cross-faction settlement writes `TransactionCategory.Trade`.

`MarketExchangeAction` runs only in cargo range. `CargoTransferDataDB` escrow is built with the transfer, and only once the ship is at the colony. It pulls units out of the pile for the ramp. It is not a hold you can open from another colony.

`Freighter` and `FleetHaulContractCommand` haul between colonies the faction owns. `Trade` buys at an ask and sells at a bid and keeps the spread. It will not hold a named pile for a named destination.

`OfferStockPlan.StockOf` returns 0 for a cargo id the faction library does not contain, so that id gets no policy row. `RunMarket` posts policy rows. The cargo book stays one row per library cargo id and still counts toward `LogiBaseDB.Capacity`.

`Goal` already has `CargoId`, `SourceEntityId`, `DestEntityId`, and `UnitShare`. `AgentProcessor.AssignGoal` is how a command becomes a `GivenGoal`. Append new `GoalType` values so saved enum ordinals stay put. An empty captain chair cannot take a goal.

## The shipment

`ShipmentCargo` is one `ICargoable` stored in the source colony's `TypeStore.Cargoables`, under a new int id from `Game.GetEntityID`. The store holds a count of 1. `ShipmentCargo` is the hold at the source only. When a ship loads all or part of a package, the whole lines it takes leave the sealed object and go aboard as themselves, each marked as sealed for this package. The rest stays sealed at the source. A line is never split. The package is done once its last line has arrived. `UniqueID` is `shipment-` plus that id. A shipment has one cargo type, and each cargo type gets its own shipment object: `CargoTypeID` is that type, and every line in it uses that type. Fuel uses `fuel-storage` and only boards ships with fuel-tank capacity. The same applies to `ordnance-storage` and `battery-storage`. General cargo uses `general-storage` as before. A line may be anything that can go in a ship's cargo, including crates and other component instances (see the line format note under Packages).

Mass per unit is the sum of each line's mass times its count. Volume per unit is the same sum for volume. The open piles lose those units, so the warehouse volume does not change.

The object carries the lines and the address. A line is a cargo id (a library cargo id, or an instance id for a crate or other instance), a count, and, on the market path, the price per unit paid for it. The address is the source colony id and the destination colony id. A market shipment also carries the buyer faction id and the seller faction id, and has one buyer and one seller. A non-market shipment carries neither, and its lines have no price.

Save every field with `[JsonProperty]`, including the int id and the lines. The stack key is that id. A reload that mints a new id loses the pile.

`CargoMath.AddRemoveCargoByVolume` already inserts an unknown `ICargoable` into `Cargoables` the first time that id is added. Removal uses the same id. Do not register the shipment in the faction cargo library.

While it sits in the warehouse it is not iron, fuel, or whatever it was. Recipes, `Trade`, and `Freighter` look at the open id and the book. They cannot take the sealed units. `OfferStock` and `RunMarket` do not list it.

## Who buys

This section covers the market path only. A non-market seal needs no buyer, no seller, no office on either side, and no `CanTrade`. Same-faction shipments are a valid non-market use, not a reject.

The buyer is a logistics office, not a ship and not the human at the keyboard. Any office whose faction `CanTrade`s with the seller may seal. The player's colony and a friendly NPC colony call the same `ShipmentBook`. A player button is that office being ordered. It does not name a ship.

A colony has one `ActiveGoal`. Do not `AssignGoal` a second goal onto the buying office. That would replace `OfferStock`. The standing market pass may call the seal. The player command calls the same seal and does not replace the office goal.

On the market path, reject when `CanTrade` is false, when either side has no office, or when every line clamps to 0. One-sided Friendly can see the book and cannot seal.

## Buy

The buying office seals one or more lines against the seller's asks. Each line clamps to `SellQuantity`, to `Sellable`, and to what the buyer can still afford. Ask 0 needs no funds. Leave `Reserve` alone. Drop each `SellQuantity` by the sealed count. The next `RunMarket` wake posts from the stock that remains.

Goods payment happens at the seal, one ledger pair per line. Ask above 0: buyer expense and seller income, category `Trade`, description `shipment {id} {cargo id} {the other faction id}`. Ask 0 writes no line for that good.

The shipment stays in the seller's warehouse until a carrier docks. A later buy sees the lower sellable.

The buyer then posts the haul. The freight fee is a number the buyer sets on that post. This plan does not invent a tariff. Price finding can change the number later. A fee of 0 is a valid post.

## Sell

The seller does not seal and does not post the haul. Filling a bid is the buying office pulling the seller's stock. The goods sit at the seller, the buyer pays the bid at the seal, and the buyer posts the same intel job on its own office. Drop the buyer's `BuyQuantity` by the sealed count. Bid 0 writes no goods line.

When the player sells into a friendly bid, the remote office is the buyer of record. The player's office is the source. Do not let the seller charge that office and post a job in its name. The remote office's market agent is who accepts, pays, and posts. A player command may ask that office to accept a named set of lines from the player's stock, and it runs only when that office can afford the goods and chooses the fee.

## Cancel

On the market path, the buying office may cancel only while the shipment is still entirely in the source warehouse and no ship has claimed the job. Unpack every line back to its open cargo id, restore the sell quantities or the buy quantity, reverse the goods lines, and remove the intel row. A price of 0 reverses nothing.

Once a ship has claimed the job, or any unit is aboard, this plan does not cancel and does not refund the goods. The goal runs to the destination. A lost ship is a later hole. Do not build insurance here. A claim that fails before loading clears the claimant so another free ship can take the row. The goods stay sealed.

Gap to close: these rules cover only the buying office on the market path. A package held for a fleet order must be releasable back to open stock when that order fails or is cancelled, which is the case `TakeBack` handles today. This plan does not define that release yet.

## The haul on the intel list

The buyer posts one row on the buying office's `LogiBaseDB.Intel`. Append `IntelKind.Haul` so saved survey ordinals stay put. `Subject` is the shipment `UniqueID`. `Ask` is the freight fee. `ForSale` is true until a ship claims it. The result tag is `deliver`. The name lists how many goods are inside. The row also carries the source colony name and the destination colony name, so the carrier knows where to fly.

This row does not copy a secret and it does not pay anyone on claim. `IntelBook.TryBuy` for `Haul` is the wrong verb. Add a claim that sets `ForSale` false and stores the claimant ship id. `IntelListing` gains `ClaimantId`, default -1, so a survey row ignores it. A second claim is rejected. The claim belongs to the claimant, which may carry the package over several trips, or hand parts to its own fleet's ships. Other factions' ships cannot split it.

The fee is paid once the shipment is fully unloaded at the destination (see Goals and actions). The buyer pays the carrier's faction, category `Trade`, description `haul {shipment id} {carrier faction id}`. Fee 0 writes no line. The buyer's own ship is same-faction pay and writes no line either. The goods lines from the seal are not repeated.

Friendly offices already project for-sale intel. A hauler scans offices in its system and in known systems, the same set `MarketRun.Markets` walks, and reads `Haul` rows. Hostile offices are not in that projection.

## Goals and actions

This is the half that shares the goal pipeline. Implement it against `agents-and-goals-design.md`. The seal and the intel row can be tested with no time pulse. The claim can land in the same pass or after.

Append `GoalType.DeliverShipment`. One planner, `DeliverShipmentPlan`, in `GameEngine/Logistics/`, next to `FreighterPlan`. `Goal.CargoId` is the shipment `UniqueID`. `SourceEntityId` and `DestEntityId` are the colonies. `UnitShare` stays 0. The shipment count is 1, and the lines live on the object. A goal may carry the whole package or a part of whole lines. Do not add route fields.

A free ship may take one posted haul. Free means no `GivenGoal`, or a `GivenGoal` that is already completed or failed, plus a cargo hold and a captain. Do not interrupt `Trade`, `Freighter`, mining, or survey. Do not replace a colony's standing goal. The ship's faction must `CanTrade` with the buyer and with the seller. The ship does not have to belong to either of them.

The planner does not call `MarketExchangeAction` and does not score a spread. The goods are already paid. The fee is paid once everything is unloaded.

On a ship:

- Shipment still at the source and the ship is out of cargo range: move there. Use the same move and jump actions a freight leg uses, including one remembered jump when the faction knows it.
- In range: a cargo transfer onto the ship of the package's lines, all of them or the whole lines that fit this ship's matching hold, each keeping its seal mark. The existing `CargoTransferDataDB` escrow runs for that ramp and only then.
- Aboard and out of range of the destination: move there.
- In range of the destination: unload and unpack as much of every line into the buyer's warehouse as it can take. If anything is still aboard, stay and retry until everything is off the ship. Once this ship's load is all off, its goal completes. Only once every line of the package has arrived: pay the fee, remove the intel row, and close the package. The fee is not paid before that.
- Shipment missing while still at the source: complete with the message `shipment closed` and clear the claim.
- Low fuel: `Fail("low on fuel")`, same as trade and freight. If nothing has loaded, clear the claim.

On a fleet: append `GoalType.FleetDeliverShipment`. The fleet does not fly or pay. It hands the package to free cargo ships as they become available, each child `DeliverShipment` carrying whole lines (see Packages), when the fleet itself is free and when `LogisticsSpan.ContractReaches(fleet, source, dest)` holds. That reach is capped by every fleet above it (`CommandSpan.Effective`). A failed child stays in place for the fleet rollup, as in `FleetParcel`. A lone ship uses the known-system route, the way `Trade` does.

`SealShipmentCommand` is issued by the buying office. It carries the seller office, the lines, and the freight fee. It does not carry a ship. The translator seals, posts the intel row, and assigns no goal. `ClaimHaulCommand` targets the buying office, names the shipment and the claimant ship, and assigns `DeliverShipment` on that ship. The ship must be free and owned by the session. The office often is not.

`EngineGameServer.SubmitCommand` rejects a command whose target the session does not own. Allow `SealShipmentCommand` when the target is the session's office, or a friendly office the session can trade with and the session's office is the buyer of a bid-fill. Allow `ClaimHaulCommand` when the target has the haul row and the session `CanTrade`s with the buyer, the same kind of hole as `BuyIntelCommand`. The claimant ship still has to belong to the session. An NPC ship claims inside the planner, with no command.

## What the player sees

This section covers the market path.

On a friendly market, **Buy and ship** seals that office's asks to one of the player's colonies and posts the haul on the player's office. The player types the fee. The button does not pick a ship.

On the player's intel list, a haul row shows the goods count, the fee, and Claim. Claim is for one of their free ships. Another faction's free ship claims through its planner and does not need this button.

A sealed pile can show in the source warehouse under its shipment name. It does not get a cargo-market bar.

No toolbar button. No change to survey goals, mine goals, or the Ceres standing jobs. Survey intel rows stay copy-on-sale.

## Packages (2026-11-01)

Decided 2026-11-01: the "package" idea from fleet Bootstrap planning is this plan. A package is a sealed shipment: a container with a source and a destination that resources, crates and similar items go into. It is to be built before the fleet-order infrastructure suitability check.

Why: it solves the fleet-order escrow gap. The suitability check passes, the chosen infrastructure goes into a shipment, and nothing else can touch it until the fleet loads it. Today nothing holds goods at order-build time. `CargoTransferDataDB` escrow (`CargoStorageDB.EscroItems`, `CargoTransferProcessor.MoveFromEscro`) starts only when `LoadGoodsAction` starts and needs two stores. Fleet Bootstrap's `Goal.BootstrapCrateIds` only stops ships under the same goal from taking the same crate.

How packages widen the plan above:

- Clarified by Jon, 2026-11-01: a sealed shipment is conceptual. It does not have to be a literal package that is physically packed up and unpacked. The seal is a logical hold on the cargo between source and destination, not a container operation. `ShipmentCargo` under "The shipment" carries that hold at the source. Once aboard, the hold is the seal mark on each line. "Unpack" in Goals and actions stays as the market-path delivery step and means releasing the hold into the buyer's warehouse.
- A package is not assumed to be a market sale. It is a general sealed shipment between a source and a destination. A market sale (buyer, seller, prices, fee), as written above, is one possible use. Others include a same-faction shipment with no prices, fee or intel row, and other non-market uses such as a fleet order holding its cargo until loading.
- Decided by Jon, 2026-11-01: a package holds anything that can go in a ship's cargo: resources, crates, and anything else ship cargo can carry. Designs are not cargo, so they are out. Supporting any ship cargo includes crates, which are tracked instance by instance. The original line format (library cargo id, count, price per unit) does not handle them, so it is extended to carry instance ids.
- Decided by Jon, 2026-11-01: the test for "can go in a ship's cargo" is the `ShipCargo` flag on `ComponentMountType` (shown in-game as "Cargo Hold") for component lines, checked via `CargoMath.Accepts` against ship cargo in general (a seal happens at the source colony before any ship is chosen). Resources, fuel and minerals are not components, so for them the test is whether their cargo type matches a hold type.
- Decided by Jon, 2026-11-01: consequence of that test: a site-only design (`PlanetInstallation` without `ShipCargo`), such as City Hall, can sit in a colony warehouse but cannot go aboard a ship, so it cannot be sealed in a package either.
- Decided by Jon, 2026-11-01: one shipment object per cargo type. A mixed load is split into one package per cargo type. Fuel, ordnance and batteries each get their own shipment and only board ships with matching hold capacity.
- Decided by Jon, 2026-11-01: a package is tied to one fleet order and is not reusable. It is released and removed at the destination.
- Decided by Jon, 2026-11-01: when a package arrives at a destination that cannot take everything, it unloads as much as it can, and the ship stays and retries until the remaining contents fit.
- Decided by Jon, 2026-11-01: a package may be carried partly across several ships or trips. Splits happen at the line, never inside a line: a crate or other instance travels whole on one ship. A package with several crated facilities can go one crate at a time, but never half a crate. This replaces the one-ship-per-shipment rule: the fleet hands the package to free ships as they become available, and the package is only fully delivered once every line has arrived.
- Decided by Jon, 2026-11-01: no separate unpack step before mounting. Crates are mounted at the destination directly from the held cargo.
- Decided by Jon, 2026-11-01: once a package is aboard, its lines travel as themselves, marked as sealed for this package. The sealed object does not ride aboard to be released into lines on arrival. Unload and mounting work as they do today. The seal survives the cargo transfer as decided below. The `CargoTransferDataDB` escrow lifetime stays as it is.
- Decided by Jon, 2026-11-01: the seal survives the cargo transfer through a separate cargo id for each sealed line. A sealed line is a small `ICargoable` class that gets its own id from `Game.GetEntityID()`, copies `CargoTypeID`, `MassPerUnit` and `VolumePerUnit` from the resource it holds, and keeps the resource id and the shipment id. Sealing removes the resource units and adds the line. Delivery removes the line and adds the units back under the library id. Crates are already component instances with their own ids, so they need no wrapper. Escrow, `MoveFromEscro`, `ReturnEscrow`, free space, mass and `Clone` work unchanged. Seal enforcement comes for free: industry, mining, refuelling and the market all ask for the library id, so they cannot see or consume sealed units by accident.
- Decided by Jon, 2026-11-01: a package replaces the `Goal.BootstrapCrateIds` claim rather than sitting alongside it. The package carries the crate ids inside it, so the bootstrap goal points at the package instead of listing crate ids separately. Keeping both would double-book the same cargo.

None, the Packages section is fully decided.

## Later

Cancel after loading, a refund when the ship is destroyed, a computed freight tariff, office purses, and a hauler that interrupts work it is already doing. The posted job and the fee are in this plan.

## Files to add

- `Pulsar4X/GameEngine/Logistics/ShipmentCargo.cs` — the `ICargoable` and its saved address (source and destination, plus buyer and seller on the market path).
- `Pulsar4X/GameEngine/Logistics/SealedLineCargo.cs` — the `ICargoable` for one sealed resource line. It gets its own id from `Game.GetEntityID()`, copies `CargoTypeID`, `MassPerUnit` and `VolumePerUnit` from the resource it holds, and keeps the resource id and the shipment id.
- `Pulsar4X/GameEngine/Logistics/ShipmentBook.cs` — seal (market and non-market), cancel, unpack, post the haul, pay the fee. One place for the stock edit, the listing counts, and, on the market path, both ledger payments. The non-market seal is a plain engine call with no command, office, fee or intel row, so a fleet order can seal at build time.
- `Pulsar4X/GameEngine/Logistics/DeliverShipmentPlan.cs` — ship and fleet planners for the two new goal types.
- `Pulsar4X/Pulsar4X.Tests/ShipmentTests.cs` — market and non-market seal, pay, cancel, and the in-range deliver. Include a non-market seal from a fleet order: it removes the chosen cargo from open stock with no ledger line or intel row.

The fleet-order infrastructure suitability check is not part of this plan's files. It is built after the package object and calls the non-market seal. It belongs with the fleet Bootstrap order (`FleetBootstrapPlan.cs`, `CommandTranslator.cs`, and the Bootstrap picker).

The fleet Bootstrap work should point the bootstrap goal at the package rather than at `Goal.BootstrapCrateIds`, since Jon decided on 2026-11-01 that a package replaces that claim (see Packages).

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
- The seal works when the buyer is a faction other than the session. Insufficient funds and one-sided Friendly reject. No shipment and no intel row.
- A same-faction non-market seal succeeds: one shipment with source and destination only, no prices, no ledger lines, no fee, and no intel row.
- `OfferStock` and a `RunMarket` wake do not add a cargo row for the shipment id. They do not remove the haul row.
- Cancel before a claim restores every line, the listing counts, the ledgers, and removes the row.
- A bid-fill is posted by the buying office. The goods sit at the seller. The seller's faction does not gain an intel row.
- A free ship of a third faction claims the row. A second ship is rejected. With that ship in range of the source, it loads the shipment, or a part of whole lines that fits. With it in range of the destination, every line is unpacked, the buyer pays the carrier the fee, and the row is gone. The buyer's own carrier writes no fee line.
- A package too big for one hold goes in parts of whole lines, over several trips or several fleet ships. No crate is split. The fee is paid and the row removed only after the last line arrives.

Do not advance a multi-hop clock in these tests. The move between two worlds can be asserted by the goal's source and destination ids and by the planner's next action type. Jump timing stays with the freight tests that already fly.
