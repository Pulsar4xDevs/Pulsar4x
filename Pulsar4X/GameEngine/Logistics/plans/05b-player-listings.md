# Plan 5b — Friendly markets, and player listings

Status: implemented on `TradeAndTransport`. Tests: `Pulsar4X.Tests/API/ApiMarketViewTests.cs` (11 passed in that fixture, with `ColonyMarketOrderTests` still green). The client library built with 0 errors. The form was not opened in a running game.

## Outcome

Opening a body shows the market of every owned or friendly colony on it. The player can add, replace, and remove listings on a colony they own, whether or not an administrator is seated.

## Depends on

Plan 5 for `MarketView`. Plan 4 so a player edit stays in step with a running `RunMarket` goal.

## Work

1. `EntityWindow` draws a default-open header for each colony in the system whose `ColonyView.PlanetEntityId` is the opened body and that has `MarketView`. The header uses the colony name. Opening the colony entity itself still shows its own market header. `GetColony()` stays owner-only.
2. `SystemWindow` lists Owned and Friendly colonies on each body. An owned name opens Colony Management. A friendly name opens that colony's entity window. Friendly colonies stay out of Colony Management.
3. `MarketView` gains `CanEdit` (true for the owner) and `Addable` (unlocked cargo not already listed, empty for a friendly viewer). `ColonyMarketPolicyDB` is not projected.
4. `SetMarketListingCommand` calls `MarketBook.SetListing` and upserts the policy row: `Min` is the reserve, `Ask` and `Bid` are the prices. An existing row keeps `AutoProduce` and keeps `Max` unless `Min` is higher. A new row has `Max` = `Min` and `AutoProduce` false. `ClearMarketListingCommand` removes the cargo id from the book and the policy. A clear of a missing id is a success. No office, an empty or unknown cargo id, a negative number, or a full office is rejected. A seated administrator does not reject the command.
5. On an owned market the widget shows Set and Remove on each row, and an Add row for `Addable`. A friendly market draws bars only.

The next `RunMarket` wake copies ask, bid, and reserve from the policy row and recomputes sell and buy from stock and hold. `RunMarketPlan` is unchanged.

## Later

A separate policy screen, a RunMarket button, price history, and a galaxy-wide market browser.
