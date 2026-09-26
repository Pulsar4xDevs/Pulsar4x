# Plan 2 — Market book

Status: not started. Spec: `GameEngine/Logistics/market-and-stance.md` section 2.

## Outcome

Each logistics office has a book of listings. Stock stays in `CargoStorageDB`. The old supply/demand dictionaries are gone, and the old bidding cycle does not run.

## Depends on

Plan 1 is required only for foreign readers. This plan can land on owned colonies first.

## Work

1. Add `MarketListing` (`CargoId`, `SellQuantity`, `Ask`, `BuyQuantity`, `Bid`, `Reserve`). `CargoId` is `ICargoable.UniqueID`.
2. Replace `LogiBaseDB`'s item dictionaries and bid list with `Dictionary<string, MarketListing> Listings`. Keep `Capacity`.
3. Add static helpers: `TryGet`, `SetListing`, `Stock` (`CargoMath.GetUnitsStored`, escrow excluded), `Sellable` (`max(0, stock − reserve)`).
4. `SetListing` rejects negative quantities and negative prices. A new cargo id is rejected when `Listings.Count >= Capacity`. Replacing an existing id is allowed. Zero prices are allowed.
5. `LogiBaseAtb` stays as it is: the office creates the blob and sums capacity. The installation template already defines capacity as "how many different items this office can handle" (default 5).
6. Default start (`DefaultStartFactory`): Earth iron sell 1000 at ask 10. Mars iron buy 1000 at bid 12. Reserve 0 on both. Remove the `ListedItems` lines.
7. Add `TransactionCategory.Trade` on `Ledger`. Do not write transactions in this plan.
8. Make `LogiBaseProcessor` and `LogiShipProcessor` return without calling `LogisticsCycle`. Empty the bidding methods, or delete `LogisticsCycle` if nothing else references it. Do not call `LogisticsSimple` or `LogisticsNewtonion`.
9. Delete `SetLogisticsOrder`. Live callers are the parked windows, already removed from the Client.Host compile. Leave those parked files uncompiled.
10. Leave `LogiShipperDB` and `ShipLogisticsOrders` on disk, uncalled, for plan 6 to delete.

`Clone` on `LogiBaseDB` must copy listings.

## Tests

`Pulsar4X.Tests/MarketBookTests.cs`

- A new cargo id past capacity is rejected. Replacing iron on a full book is accepted.
- Negative price or quantity is rejected. Stock is not stored on the listing. `Sellable` subtracts reserve.
- Default Earth listing is iron sell 1000 at 10. Mars is iron buy 1000 at 12.
- A logistics processor tick does not enqueue a warp order on a ship that still has `LogiShipperDB`.

## Later

Settlement, colony policy, the bar view, and deleting the shipper blob.
