# Plan 5 — Market bars

Status: implemented on `TradeAndTransport`. Tests: `Pulsar4X.Tests/API/ApiMarketViewTests.cs` (4 passed). The client library built with 0 errors. The bars were not opened in a running game.

## Outcome

Opening a trade location shows one row per listing: stock against reserve, the buy request, the sell offer, and the bid and ask as numbers.

## Depends on

Plan 2 for `LogiBaseDB.Listings` and `Stock`. Plan 1 so a friendly owner projects as `OwnerRelation.Friendly`. Prices from plan 3 display as soon as they exist; quantities are enough for the bars. Policy editing belongs to plan 4's commands and is not drawn here.

## Work

1. Add a snapshot view in `Pulsar4X.Api/Snapshots.cs`:

```csharp
public sealed record MarketGoodView(
    string CargoId, string Name,
    long Stock, long Reserve,
    long BuyQuantity, decimal Bid,
    long SellQuantity, decimal Ask) : IComponentView;

public sealed record MarketView(
    int Capacity, IReadOnlyList<MarketGoodView> Goods) : IComponentView;
```

2. `GameProjector` adds `MarketView` only when the entity has `LogiBaseDB` and `RelationOf` is Owned or Friendly. Allied already projects as Friendly, so it is included. Hostile and diplomatic Neutral do not get the view. Name comes from the viewer's `CargoGoods` entry for that id; if the viewer lacks the id, use the cargo id string.
3. Stock is `MarketBook.Stock` resolved with the market owner's cargo definition. Reserve, quantities, and prices are copied off the listing. Do not project `ColonyMarketPolicyDB` (min, max, auto-produce). That blob may not exist yet; the view must not require it.
4. A small client widget, one row per `MarketGoodView`:
   - Good name.
   - Three `ImGui.ProgressBar`s sharing one scale, `max(stock, reserve, buy, sell, 1)`: stock, buy quantity, sell quantity. The reserve is a number beside the stock bar, not a fourth colour scheme.
   - Bid and ask printed on the row.
   - Caption with `Goods.Count` and `Capacity`.
   `ColonyConstructionDisplay` already uses `ImGui.ProgressBar`. Follow that call.
5. Show the widget in two places that share it:
   - A Market tab on `ColonyManagementWindow`, next to the production and construction tabs, for the selected owned colony.
   - `EntityWindow`, when the opened snapshot `HasView<MarketView>()`. That is how a friendly colony is opened. The colony manager lists only `OwnerRelation.Owned`, so it cannot be the only surface.
6. The widget only reads the snapshot. It does not call `SetLogisticsOrder` or any parked window. It does not submit policy commands.

## Tests

Engine projection, `Pulsar4X.Tests/API/ApiMarketViewTests.cs` (or beside `ApiSystemProjectionTests`):

- Owned colony with an iron listing: `MarketView` has that good, stock matches the hold, quantities and prices match the listing.
- A faction that is mutually Friendly sees the same view on the other's colony.
- A Hostile owner does not get `MarketView`.
- A colony with no `LogiBaseDB` has no `MarketView`.

The ImGui widget has no automated UI harness in this plan. Check it by opening an owned colony's Market tab and a friendly colony from the entity window once the client can run. State that in the implementation notes if the client is not launched.

## Later

Price history, a galaxy-wide market browser, and editors for policy. Those wait until one location's bars match the book.
