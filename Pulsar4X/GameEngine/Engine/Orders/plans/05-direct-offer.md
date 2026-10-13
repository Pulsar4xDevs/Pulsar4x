# Direct offer

Status: implemented.

The ship offers its whole mineral hold to one office. That office buys every mineral if it has room, and the ship delivers the hold itself. The price is one named rate for the lot. Nobody argues, and the ship does not shop mineral by mineral.

An earlier draft of this plan posted an iron buy up to a stock target. That is the wrong sale. Take it out while building this one.

## Depends on

[Depot room](04-sell-ore.md) for warehouse volume and a purse that can pay. [Standing survey and mine jobs](02-standing-jobs.md) for the ship. The depot spaceport is already placed, so cargo range is not this plan.

## Outcome

A Lode ship with mined minerals and no warehouse of its own sells the whole hold to the closest friendly office that can take it. The office buys every mineral in the hold, including the cheap ones. What to do with that mix is the office's problem.

The price is `CeresStart.LotPrice`, half the iron bid, the same rate for every mineral. The payment is that rate times the number of mineral units. Fuel in the tanks is not part of the lot.

The office must have a logistics office, room in general storage for the volume of the whole hold, and funds for the whole payment. The closest such office is the buyer. Ceres Depot is that office while its warehouse can hold the load. If it cannot, the next closest office that can is the buyer. The hold is never split across offices.

If no office can take the whole lot, the ship waits a day and tries again. The mine goal stays active. A sale the office cannot pay is not submitted. A failed exchange would fail the mine.

The standing fleet does not gain a new child type. The ship's `MineAsteroids` plan does this on the way home.

## What is wrong

The code posts a public iron buy. `CeresStart.IronStockTarget` is 4000, the depot iron row's max is that target, and `RunMarketPlan.BuyQuantityOf` buys up to max when the bid is above 0 and auto-produce is off. The depot then buys iron at bid 1. `MineAsteroidsPlan.SellActions` sells only a mineral whose buy quantity is above 0, capped by what the office can pay. The other minerals stay aboard. One rock of ten minerals sells the iron and keeps the nine.

`CanSellAny` picks the nearest office that is buying something in the hold. It does not ask an office to take the rest. Lode owns no warehouse, so minerals with no buy have nowhere to go. The return queues `UnloadWaitOrder` for a day.

That public buy is not the offer. A market exchange also pays the posted bid and needs a buy row per cargo id. This sale has one rate and no new row.

## What to build

Remove the stock-target buy. Drop `IronStockTarget` and the iron max set in `WriteDepotPrices`. `BuyQuantityOf` returns only the shortage again, `max(0, hold − stock)`. A bid with a max above stock does not start a buy. Delete the market tests that expect that buy, and the direct-offer tests that sell iron into it. After that, depot iron is for sale at ask 2 with bid 1 stored and a buy quantity of 0. Methalox and water stay bid 0. Earth and Luna stay bid 0.

Add `CeresStart.LotPrice` as half of `IronBid`.

On the mine return, when the hold has minerals, pick one office:

- The factions can trade.
- It has a logistics office.
- Free general-storage volume covers the volume of every carried mineral.
- Its faction can pay `LotPrice` times the total mineral units. Use the same floor as `MarketExchangeAction.Affordable`.

The closest office that passes is the destination. Travel time is the same nearest pick the return already uses. If none pass, queue `UnloadWaitOrder` and leave the minerals aboard.

Out of cargo range, move to that office and do not transfer yet. In range, move every carried mineral into the office's store and pay once, `LotPrice` times the units, category `Trade`. The minerals become that colony's stock. If any line cannot move, move none and wait. Do not add a policy row or a book row for the sale.

The fuel tanks stay as they are. The standing mine stays active. Do not seal a shipment, post a haul, or consign the hold.

## Tests

Do not run the clock. Open the cargo-transfer range when a transfer has to run. A ship that is not alongside asserts the move and does not transfer.

- After placement, depot iron's sell quantity is the stock on hand, ask 2, bid 1, and buy quantity 0. Luna iron's buy quantity is 0. Methalox and water buy quantities are 0.
- A `RunMarket` row with a bid, auto-produce off, and max above stock still posts buy 0 above the hold. The existing surplus, shortage, and alloy cases stay as they are.
- A Lode ship alongside Ceres, carrying two minerals and with fuel in the tanks, loses both minerals. The depot's store gains both. The fuel aboard is unchanged. Lode's balance rises by `LotPrice` times the mineral units, and the depot's falls by the same. No new book row appears. The mine goal and the standing mine stay active.
- With the depot warehouse too full for that volume, and another friendly office able to hold and pay for the lot, the destination is that other office. Both minerals stay in the one sale.
- With no office able to hold the volume, or none able to pay, the plan is the unload wait. Both minerals stay aboard. No exchange is queued. The mine goal stays active.
- A ship that is not alongside is given the move to the chosen office. Funds and cargo stay put on that wake.

## Limit

The clock is not run, so the depot's own offer-stock goal does not wake. The delivered minerals sit as that colony's stock, unlisted, until a later market pass. This plan does not choose their ask.

The lot rate is a fixed stand-in. Finding a price from the mix is the economy session.

## Out

No price argument. No consignment. No sealed shipment and no haul on the intel list. No splitting one hold across offices. No new public bids. No sale of the fuel. No change to `OfferStockPlan`'s default prices. Earth and Luna gain no policy rows. `sealed-shipments.md` stays as it is.
