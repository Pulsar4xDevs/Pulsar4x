# Direct offer

Status: not started.

The depot buys a mineral it has posted a bid for. The ship sells into that bid. Nobody argues the price. This is the small half of getting ore off the prospector.

## Depends on

[Depot room](04-sell-ore.md) for a purse that can pay. [Consigned charts and fixed prices](01-consigned-charts.md) for the iron ask and bid. [Standing survey and mine jobs](02-standing-jobs.md) for the ship.

## Outcome

Ceres Depot keeps selling the iron it has at ask 2, and it also posts a buy at bid 1, up to a fixed stock target above the starting pile. A Lode ship with iron and no owned warehouse sells into that buy. The standing mine job stays active. A purse that cannot cover one unit waits and tries again. It does not fail the mine.

Other minerals are not given bids here. Seven slots stay free for [consignment](06-consignment.md). Filling them with standing buys would leave the broker with nothing to list.

## What is wrong

`RunMarket` posts one side. `hold` is `max(Min, industry need)`. Sell quantity is `max(0, stock − hold)`. Buy quantity is `max(0, hold − stock)`. The depot iron row is min 0, max 0, not auto-produce, ask 2, bid 1, and the warehouse starts with 2000 iron. Hold is 0, so the book shows the whole pile for sale, the bid copied across, and a buy quantity of 0.

`MineAsteroidsPlan.CanSellAny` requires that buy quantity, free cargo space, and a trade stance. Lode owns no colony that can take general cargo, so the return finds no destination. The ship queues `UnloadWaitOrder` for a day, then plans the same wait.

Writing a buy quantity onto the book at placement does not last. Offer stock wakes `RunMarket`, and the next pass posts buy 0 again. `AsteroidMineTests` already sells when a buy quantity is above 0.

## What to build

One change to the buy line in `RunMarket`, and one number on the depot iron row.

When the bid is above 0, auto-produce is false, and max is above stock, buy quantity is `max(0, hold − stock, max − stock)`. Sell quantity stays `max(0, stock − hold)`. A bid of 0 does not start buying. An auto-produce row does not gain this buy.

Set the depot iron max to a named stock target in `CeresStart`, twice the 2000 iron the blueprint loads. Min stays 0. Methalox and water stay bid 0. Offer stock already skips a cargo id that has a row.

Do not change `CanSellAny`'s quantity check. In `SellActions`, cap the units at what the depot can pay at the bid, the same floor the exchange uses. If that cap is 0, return no sale. `PlanReturn` already turns an empty sale into the one-day wait. A failed exchange fails the whole mine goal.

The standing fleet does not need a new child type. The ship already sells from its `MineAsteroids` plan once a buyer exists. There is no counter-offer. The posted bid is the price.

## Tests

Do not run the clock.

- After placement, depot iron still has a sell quantity of the stock on hand, ask 2, and bid 1, and the buy quantity is the stock target minus that stock. Luna iron's buy quantity stays 0. Methalox and water buy quantities stay 0.
- A `RunMarket` row with a bid, auto-produce off, and max above stock posts both sides. A bid of 0 does not. An auto-produce row with max above stock still posts buy 0. The existing surplus, shortage, and alloy cases stay as they are.
- A Lode ship with iron, no owned warehouse, and the depot buy open is handed a sell, not an unload wait. Open the cargo-transfer range for that assertion. The spawn orbit is outside the default 100 m/s, and movement already calls it at Ceres. After the exchange, Lode's balance rose by bid times units, the depot's fell by the same, the ship's iron fell, and the depot's rose. The units are the minimum of the buy quantity, free depot space, the ship's iron, and what the depot can pay.
- With the depot purse then emptied, the next plan is the unload wait, the mine goal stays active, and the ship's remaining iron stays aboard.

## Limit

The clock is not run. A ship that mined a rock and flew back is dropped in low orbit, which is inside cargo range of the surface colony. A ship left on the spawn orbit is not in range. The sell test opens the range.

This plan buys iron only. A rock of ten minerals still leaves nine on the ship.

## Out

No price argument. No consignment. No new bids for the other minerals. No change to `OfferStockPlan`'s default prices. Earth and Luna stay bid 0.
