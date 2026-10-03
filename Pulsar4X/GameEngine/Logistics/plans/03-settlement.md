# Plan 3 — Settlement

Status: implemented on `TradeAndTransport`. Spec: `GameEngine/Logistics/market-and-stance.md` section 3. Tests: `Pulsar4X.Tests/MarketExchangeTests.cs`.

## Outcome

A ship that is already in cargo-transfer range can buy from a sell offer or sell into a buy request. Cargo, the listing, and (when the factions differ) the two ledgers change together. The ship is not moved.

## Depends on

Plan 2. Plan 1 for the cross-faction cases. Same-faction cases can be tested before stance exists by treating same-owner as allowed, which `CanTrade` already does.

## Work

1. Add `MarketSide` (`BuyFromMarket`, `SellToMarket`) and one `EntityAction` commanded by the ship. The market entity id, cargo id, side, and requested unit count are fields on the action.
2. Set `_isFinished` inside `Execute` only. A constant-true `IsFinished` accepts the order and never moves cargo.
3. Execute the checks in the spec, in order: listing exists, `CanTrade`, in range, both factions resolve the cargo id, then quantity.
4. Range uses the Δv check `CargoTransferProcessor` already uses against both `TransferRangeDv_mps`. Out of range fails. The action does not wait and does not emit `MoveTo`.
5. Quantity is the minimum of the request and the caps in the spec (offer or request, sellable or ship hold, free space on the receiver). When the factions differ and the price is above zero, also cap by `floor(funds / price)`. Payer is the ship faction on a buy and the market faction on a sell.
6. On a zero quantity, fail with `"Nothing to exchange"` and change nothing.
7. On success, move units with the existing cargo add/remove helpers, shrink `SellQuantity` or `BuyQuantity` by the units moved, and if the factions differ write `Ledger` `Trade` expense and income for `price × units`. Same faction: do not touch `Ledger`.
8. Do not enqueue `CargoTransferOrder`, `WarpMoveAction`, or `LogisticsSimple`. Those would move the same goods twice or plot a course this action does not own.
9. Auth stays on the ship. The market is mutated only after `CanTrade` succeeds. That is the one command path that writes another faction's cargo and ledger.

Failure messages, unchanged from the spec: `"No listing"`, `"Cannot trade"`, `"Out of range"`, `"Unknown good"`, `"Nothing to exchange"`. `Details` carries the message.

## Tests

`Pulsar4X.Tests/MarketExchangeTests.cs`

- Same-faction buy in range: ship stock rises, market stock falls, sell quantity falls, both ledgers unchanged.
- Two mutually friendly factions: buyer funds drop by `ask × units`, seller funds rise by the same.
- Sell-to-market pays the bid from the market faction to the ship faction.
- Hostile, or out of range: message set, cargo and funds unchanged.
- Request larger than the offer, the sellable stock, or the payer's funds: the moved amount is the minimum.
- `IsFinished` is false before `Execute`.

## Later

Choosing a destination, reservations, refunds if a transfer is cancelled, and any timed drip. This action commits the whole resolved quantity in one execute.
