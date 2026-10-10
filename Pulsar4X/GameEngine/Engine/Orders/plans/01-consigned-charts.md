# Consigned charts and fixed prices

Status: implemented.

Strata Survey starts with a few existing belt asteroids already geo-surveyed. Those charts are for sale on Ceres Depot's intel list, and the seller is Strata. Ceres Depot does not gain the surveys. Lode Mining can buy one. Cargo listings stay owner-only. No goals are assigned in this plan except the Offer stock goal the depot already gets.

## Depends on

Implemented survey charts (`IntelBook`, `BuyIntelCommand`) and Offer stock. `OfferStockPlan` skips a cargo id that already has a policy row.

## Outcome

A new game has all three of these:

- Strata's `GeoSurveyStatus` is complete on a few existing asteroids near Ceres, with the same partial mineral access a finished geo survey grants. No new bodies. Lode and Ceres Depot do not have those flags.
- Each of those charts is a `ForSale` intel row on Ceres Depot. The row names Strata as the seller. `TryBuy` checks Strata still has the survey and pays Strata. Ceres Depot's ledger and survey flags do not change.
- Ceres Depot sells methalox at ask 3, the processed-material `WealthCost`. It bids for iron below its ask, and it offers the iron on hand at that ask. Water, if listed, uses a fixed non-zero ask too, so this colony does not gain a zero row from Offer stock. Other colonies still get ask 0 and bid 0 from `OfferStockPlan`.
- Strata, Lode, and Ceres Depot each start with cash enough to pay for several chart purchases. A fuel fill is not required yet. Methalox at 3 per unit does not fit in a few chart-sized purses, because a tank is on the order of a million units. Plan 3 buys only what the purse can afford.

`SetIntelListing` and cargo edits stay owner-only. A friendly faction still cannot post its own cargo bid or ask. The consigned row is written at placement, not through a visitor edit.

## What to build

Add a seller id on `IntelListing`. Empty means the office owner, so rows already stored keep today's behavior. `IntelBook.SellerHas` and the payment in `TryBuy` use the seller. `GrantGeo` still writes the buyer's survey flag and partial mineral access only.

Placement order matters. `PlaceOwnedColonies` runs Offer stock as soon as the depot's cargo is loaded. Write the priced `ColonyMarketPolicyDB` rows for methalox, iron, and water before that assign. Offer stock will see the rows and leave the prices. After `PlaceFactions`, mark the chosen asteroids surveyed for Strata and write the consigned intel rows onto the depot. The rocks are the nearest existing asteroids to Ceres, a small fixed count, not a new generator.

`StartingFunds` is already on the faction blueprint and is 0 for both ship factions. Set it. Ceres Depot is created with a hardcoded 0 in `PlaceOwnedColonies`. Give that path a starting balance too.

## Tests

Extend the Ceres placement tests. Do not run the clock.

- Strata has a finished survey and partial minerals on the seeded rocks. Lode and Ceres Depot do not.
- The depot intel row is for sale, the ask is the chart constant, and the seller is Strata.
- `IntelBook.TryBuy` as Lode grants Lode the survey, pays Strata, and leaves Ceres Depot's survey flag unset.
- The depot's methalox ask is 3 and its iron bid is below its iron ask. A Luna or Earth warehouse row is still ask 0 and bid 0.
- Each of the three factions has a non-zero balance.

## Limit

Iron's bid is stored on the policy row and copied onto the listing. `RunMarket` still posts only one side: with stock on hand and min 0, sell quantity is the stock and buy quantity stays 0. A live buy quantity is later work. `MineAsteroidsPlan.CanSellAny` needs buy quantity above 0 before Lode can sell ore here.

## Out

No fleet goal, no buy-charts action, no fuel purchase, no ship-bridge change, no change to `OfferStockPlan`'s default prices. `RunMarket` is unchanged.
