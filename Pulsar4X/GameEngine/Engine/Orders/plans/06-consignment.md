# Consignment

Status: not started.

The office holds ore for another faction and sells it for them. A later buyer pays that faction minus a cut. This is not how the miner unloads. A whole hold is sold by [Direct offer](05-direct-offer.md).

## Depends on

[Depot room](04-sell-ore.md) for free slots and warehouse volume. [Direct offer](05-direct-offer.md) so the standing mine's return is already a lot sale. Consigned units stay out of that sale and out of the depot's own stock.

## Outcome

A ship alongside Ceres Depot offers named minerals. The office accepts what fits in free general storage. The transfer is partial: units move until the offer or the free volume is used up. Less than one unit of room leaves that mineral aboard. The rest of an offer can still move. The goal that made the offer stays active.

The standing mine does not make this offer. Its return is the lot sale. A hold that sale already moved is the depot's stock, and this plan does not take it back.

Consigned units are not the depot's stock. They do not raise the depot's sell quantity, and they do not enter the depot's purse. A buyer of a consigned row pays the seller nine tenths and the depot one tenth. The cut is that named fraction.

A consigned cargo id is listed when a slot is free and the depot does not already list that id from its own stock. Methalox, iron, and water stay the depot's rows. Other accepted minerals use the free slots, one id each. Cargo that was accepted but does not fit on the book still sits in the warehouse, unlisted, until a slot is free.

No daily rent in this plan. No price argument. The office sets the public ask. Where the depot has an ask for that cargo, use it. Otherwise use a named consignment ask on `CeresStart`.

## What is wrong today

The cargo book has one pile per cargo id, and that pile is the office's. `RunMarket` sells a surplus as the depot's and pays the depot. A chart consignment cannot be copied onto it: intel rows have a seller id and no volume, and they do not use the cargo slots. There is no way to leave ore in the warehouse under another faction and have a later buyer pay that faction.

The depot book holds ten ids. Methalox, iron, and water already use three.

## What to build

A side record on the office, keyed by cargo id and seller, holding units. Volume stays in the warehouse's general storage, so free space is real. The record is what keeps those units out of `RunMarket`'s stock count and out of the depot's purse.

The offer is explicit. The office accepts up to free volume. A refusal leaves that mineral aboard and does not fail the goal that offered it. Do not queue the mine's one-day unload wait from this path.

Listing a consigned id uses one book slot and names the seller. `RunMarket` leaves that row alone on later wakes. A sale removes units from the consigned record and from the warehouse, then splits the payment. The depot's own rows keep today's payment.

Do not consign a cargo id the depot already stores as its own. That is iron, water, and methalox at placement. Do not call this from `MineAsteroidsPlan`. Earth and Luna do not accept an offer.

## Tests

Do not run the clock. Open the cargo-transfer range when a transfer has to run. Call the offer directly. Do not go through the mine return.

- A Lode ship carrying a mineral the depot does not stock moves units into the warehouse. The depot's own iron count stays 2000. The depot's iron sell quantity stays the stock on hand. Lode's balance does not change on the transfer. The depot's stock of that mineral does not rise.
- The moved units are limited by free volume. A warehouse with room for a few units accepts those and leaves the rest on the ship. The goal that offered stays active.
- With a free slot, that mineral is listed and the seller is Lode. With the book filled to capacity, the units still sit and no new row appears.
- A buy of that row pays Lode nine tenths and the depot one tenth, and the consigned count falls by the units sold. The depot's iron count and iron sell quantity do not move.
- Iron offered for consignment stays aboard. No consigned iron record appears, and the depot's iron row is unchanged.

## Limit

The clock is not run, so there is no daily rent. Unsold consignments cost nothing while they sit. Rent is a later pass, after a tick is safe. A finished geo survey can still warp into itself and hang `ProcessSystem`.

The public book still has seven slots after methalox, iron, and water. Minerals past those slots sit unlisted until a slot is free. Ten slots is not a row for every mineral plus fuel.

## Out

No price argument. No colony-administrator upgrade. No rent tick. No change to chart consignment. No change to the lot sale. Earth and Luna do not accept consignments. The standing mine does not offer one.
