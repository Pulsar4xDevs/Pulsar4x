# Consignment

Status: not started.

The office sells ore on behalf of the prospector. Cargo moves into the warehouse, the ship can mine again, and a later buyer pays Lode minus a cut. This is the half that empties a hold of minerals the depot is not bidding for.

## Depends on

[Depot room](04-sell-ore.md) for free slots and warehouse volume. [Direct offer](05-direct-offer.md) so iron still sells into the bid and is not mixed into a consigned pile.

## Outcome

A mineral with a buy quantity above 0 takes the direct sale. A mineral with no buy is offered as a consignment when the warehouse has room for at least one unit. The transfer is partial: units move until the hold, the free volume, or the offer is used up. The rest stays aboard.

Consigned units are not the depot's stock. They do not raise the depot's sell quantity, and they do not count toward the iron stock target. A buyer of a consigned row pays Lode the sale minus the cut, and pays the depot the cut. The cut is a named fraction, one tenth.

A consigned cargo id is listed when a slot is free and the depot does not already list that id from its own stock. Methalox and water stay the depot's rows. Iron is a direct buy, so it is not consigned. Other minerals list in the free slots, one id each. Cargo that does not fit on the book still sits in the warehouse. The ship is empty of what was accepted. The standing mine job stays active.

No daily rent in this plan. No price argument. The office sets the public ask. Where the depot has an ask for that cargo, use it. Otherwise use a named consignment ask on `CeresStart`.

## What is wrong today

The cargo book has one pile per cargo id, and that pile is the office's. `RunMarket` sells a surplus as the depot's and pays the depot. A chart consignment cannot be copied onto it: intel rows have a seller id and no volume, and they do not use the five cargo slots.

The prospector mines every mineral on the rock, 100 units of each per day, into a 1000 m³ hold. Only a posted buy takes a type off the ship. The rest stay until the hold is full, and then mining stops for every type, including iron.

## What to build

A side record on the office, keyed by cargo id and seller, holding units. Volume stays in the warehouse's general storage, so free space is real. The record is what keeps those units out of `RunMarket`'s stock count and out of the depot's purse.

The ship offers the consignment. The office accepts up to free volume. Refusal when free volume is under one unit leaves that mineral aboard and does not fail the mine. The same one-day wait covers a mineral nobody accepted.

Listing a consigned id uses one book slot and names Lode as the seller. `RunMarket` leaves that row alone on later wakes. A sale removes units from the consigned record and from the warehouse, then splits the payment. The depot's own rows keep today's payment.

Do not consign a cargo id the depot already stores as its own. That is iron, water, and methalox at placement. Iron leaves by the direct bid.

## Tests

Do not run the clock. Open the cargo-transfer range when a transfer has to run, same as the fuel buy.

- A Lode ship carrying a mineral the depot does not stock or bid moves units into the warehouse. The depot's own iron count stays 2000. The depot's sell quantity for iron stays the direct-offer number. Lode's balance does not change on the transfer.
- The moved units are limited by free volume. A warehouse with room for a few units accepts those and leaves the rest on the ship. The mine goal stays active.
- With a free slot, that mineral is listed and the seller is Lode. With the book filled to capacity, the units still sit and no new row appears.
- A buy of that row pays Lode nine tenths and the depot one tenth, and the consigned count falls by the units sold. The depot's iron balance and iron stock do not move.
- Iron on the ship still takes the direct sale, not a consignment.

## Limit

The clock is not run, so there is no daily rent. Unsold consignments cost nothing while they sit. Rent is a later pass, after a tick is safe. A finished geo survey can still warp into itself and hang `ProcessSystem`.

The public book still has seven slots after methalox, iron, and water. Minerals past those slots sit unlisted until a slot is free. A second office is ten slots, not a row for every mineral plus fuel.

## Out

No price argument. No colony-administrator upgrade. No rent tick. No change to chart consignment. Earth and Luna do not accept consignments.
