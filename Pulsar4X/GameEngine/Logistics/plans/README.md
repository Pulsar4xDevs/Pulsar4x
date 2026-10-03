# Trade and transport plans

Implementation plans for the trade roadmap. Design vocabulary for the first three is `GameEngine/Logistics/market-and-stance.md`. Goal and action rules are `GameEngine/Engine/Orders/agents-and-goals-design.md`.

Plans 1–12 and 5b are implemented on `TradeAndTransport`. Do not start a later plan before the ones it lists under Depends on.

| # | Plan | Depends on |
|---|---|---|
| 1 | [Faction stance](01-faction-stance.md) | — |
| 2 | [Market book](02-market-book.md) | 1 only for who may read a foreign book. The book itself can land first. |
| 3 | [Settlement](03-settlement.md) | 2 |
| 4 | [Colony market orders](04-colony-market-orders.md) | 2, and 3 before a posted request can be filled |
| 5 | [Market bars](05-market-bars.md) | 2. Plan 1 for a friendly market to appear as Friendly. |
| 5b | [Player listings](05b-player-listings.md) | 5, and 4 so a player edit stays in step with a running market. |
| 6 | [Independent trader](06-independent-trader.md) | 1, 3, and a book that has both an ask and a bid |
| 7 | [Owned freight](07-owned-freight.md) | 3, 4. Parallel with 6 after those. |
| 8 | [Inter-system routes](08-inter-system-routes.md) | 6 or 7 in one system, and jump actions that finish |
| 9 | [Autonomous pick](09-autonomous-pick.md) | Issued 4, 6, and 7 playable |
| 10 | [Fleet trade and freight](10-fleet-trade.md) | 6 and 7. Parallel with 11. |
| 11 | [Colony supply](11-colony-supply.md) | 4 and 5b. Parallel with 10. |
| 12 | [Offer warehouse stock](12-offer-stock.md) | 4. A blueprint colony with an office lists its warehouse. |

Tests for these plans run from outside the repo (for example `/tmp`) with `DOTNET_ROLL_FORWARD=LatestMajor`, project `Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj`. `global.json` pins SDK 8.0.122; the local SDK may be newer.
