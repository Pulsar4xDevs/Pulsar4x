# Trade and transport plans

Implementation plans for the trade roadmap. Design vocabulary for the first three is `GameEngine/Logistics/market-and-stance.md`. Goal and action rules are `GameEngine/Engine/Orders/agents-and-goals-design.md`.

Plans 1–12 and 5b are implemented on `TradeAndTransport`. Do not start a later plan before the ones it lists under Depends on. Colony industry goals are not in this sequence. They live in `GameEngine/Industry/plans/`.

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

[Office purses](office-purses.md) is an idea saved on 2026-10-06. It is not plan 13. Leave it until someone asks to build it.

[Survey charts](survey-charts.md) is implemented. It is not plan 13. Finished surveys are the first rows on a general intel list at a logistics office. This slice sells a geo survey, a cleared grav pin, and a discovered jump. Tests: `ApiIntelBookTests`.

[Sealed shipments](sealed-shipments.md) is a plan saved on 2026-10-10. It is not plan 13. A friendly office's market agent buys listed cargo before a ship is in range. Several goods can share one shipment. The buyer pays for the goods and posts a haul on the intel list. Any free ship can claim it and be paid the fee. The delivery goal is the half a goals-and-actions session can take. Leave it until someone asks to build it.

[Move own goods](move-own-goods.md) is implemented. It is not plan 13. One trip carries named warehouse piles between two owned colonies. Haul and Haul contract stay the posted-book orders.

[Bootstrap colony](bootstrap-colony.md) is implemented for one ship. It is not plan 13. A captained ship with a Construction Bay carries finished crates between two owned colonies and mounts them from its own hold. Fleet Management has a ship picker. The fleet-level order is not built.

[Faction grants, later](faction-grants-later.md) is a stub saved on 2026-10-15. It is not plan 13. The checkbox pass is in. The next slice is a Colonies box separate from Ships. Geo, Grav, Research, a transponder, sensor profiles, opinion, and combat use of status stay listed there and unbuilt. Leave it until someone asks to build it.

Tests for these plans run from outside the repo (for example `/tmp`) with `DOTNET_ROLL_FORWARD=LatestMajor`, project `Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj`. `global.json` pins SDK 8.0.122; the local SDK may be newer.
