# Office purses

Status: idea from 2026-10-06. Saved for later. Do not build it until asked. It is not logistics plan 13, and it is not part of the implemented 1–12 sequence.

Cross-faction trade pays from the office that is doing the trading. A fleet has one purse. A colony has one purse. A ship that no fleet lists has its own purse, and that is the purse a lone trader spends. The faction ledger stays the treasury: starting capital, research, and the faction snapshot keep reading `FactionInfoDB.Money`. The seated person has no purse. `AgentDB` is unchanged. Moving a captain leaves the cash on the office.

Remittance is a later idea after this one. This slice does not move money between the treasury and a purse, and every new purse starts at 0. A new game can still complete a foreign trade when the ask or bid is 0, and a priced foreign trade buys nothing until a later pass funds the purse. Same-faction hauls still move cargo only.

## The account

Add `PurseDB : BaseDataBlob` in `Pulsar4X/GameEngine/Factions/PurseDB.cs`, next to `Ledger`. It holds `public Ledger Money { get; } = new();`. Reuse `Ledger`. `BaseDataBlob` serializes with `MemberSerialization.OptIn`, so `Money` needs `[JsonProperty]`. `Clone` builds a new purse and replays `GetAllTransactions()` onto its ledger, which keeps the balance. `GetAllTransactions` is newest-first; readers sort again, so the clone's balance and the set of lines match.

A small static helper in that file, `Purses`:

- `Of(Entity holder)` returns the holder's `PurseDB`. When the blob is missing it attaches a new empty one with `SetDataBlob` and returns that. Hand-built test markets and old saves grow an empty purse on first use.
- `ForTrader(Entity ship)` returns the purse the ship spends. Walk `ship.Manager.GetAllEntitiesWithDataBlob<FleetDB>()` and take the first fleet whose `FleetDB.Children` contains the ship. Ships do not point back at their fleet: `AddChild` only appends to `Children`. The ship's `PositionDB` parent is a body, so it is the wrong link. The faction entity also carries a `FleetDB`, and that node lives on the global manager, so a scan of the ship's star-system manager does not treat it as the ship's fleet. When no fleet in that manager lists the ship, or the ship has no manager, the purse is the ship's own `PurseDB`.

Two children of one fleet spend that one fleet purse. A balance already sitting on a ship's purse stays there when the ship joins a fleet, and trade ignores it while the ship remains a child. This slice does not merge or sweep that balance.

## Where empty purses are attached

Attach `new PurseDB()` in the blob list at creation, funds 0:

- `FleetFactory.Create`
- `ShipFactory.CreateShip`, in the `dataBlobs` list passed to `AddEntity`
- `ColonyFactory.CreateFromBlueprint` and `ColonyFactory.CreateColony`

`Of` still covers a market or ship built without those factories. The market side of a trade is the market entity itself. In `MarketExchangeTests` that entity is a raw entity with cargo, a logistics office, and a position. In a real game it is the colony. The colony purse is that colony's `PurseDB`.

## Settlement

`MarketExchangeAction` is the only trade writer today. It debits and credits `FactionInfoDB.Money` on a cross-faction exchange (`Execute`, the block around the `TransactionCategory.Trade` lines) and `Affordable` reads that same faction ledger. Point both at the resolved purses.

- The trader purse is `Purses.ForTrader(_entityCommanding)`.
- The market purse is `Purses.Of(market)`.
- Buy from the market: the trader purse pays the ask, the market purse receives it.
- Sell to the market: the market purse pays the bid, the trader purse receives it.
- `crossFaction && price > 0` still caps quantity with `min(quantity, Affordable(payerPurse, price))`. A result of 0 still fails with `"Nothing to exchange"`.
- `Affordable` takes the payer's `Ledger` and the price. The floor division and the zero-funds result stay as they are.
- Same faction skips the cap and the write. Owned freight moves cargo with both purses and both faction ledgers left as they were.
- Cross faction writes `TransactionCategory.Trade` for `price * moved` on the two purses, including a 0 amount when the price is 0. Descriptions stay `{cargoId} {moved} {otherFactionId}`. The write uses the ship or fleet and the market entity already in `Execute`.

`ResearchProcessor` and `GameProjector.ProjectFaction` keep reading `FactionInfoDB.Money`. `FactionFactory.CreateBasicFaction` still deposits starting capital on the faction ledger. `Ledger` itself is unchanged.

## Tests

Update `Pulsar4X.Tests/MarketExchangeTests.cs`. `Scene.Build` currently deposits `shipFunds` and `marketFunds` on the faction ledgers. Deposit those amounts on the ship purse and the market purse. `CreateFaction` starts the faction ledger at 0, so a friendly trade should leave both faction ledgers at 0.

- `FriendlyBuy_PaysTheAsk`: ship purse ends at `1000 - 40`, market purse at `40`, one Trade line each, descriptions unchanged, both faction ledgers still 0.
- `SellToMarket_PaysTheBidFromTheMarketFaction`: market purse ends at `500 - 72`, ship purse at `72`.
- `MovedAmount_IsTheMinimumOfTheOffer_Sellable_AndFunds`: the by-funds case ends at ship purse `5` and market purse `20`, and the ship holds 2 iron.
- `SameFactionBuy_MovesCargo_AndDoesNotTouchTheLedger`: faction ledger unchanged, and both purses stay at 0 with no Trade lines. The same-faction cases in that fixture (`byOffer`, `bySellable`, `bySpace`) keep moving cargo with no purse write.
- Hostile, out of range, no listing, and unknown good: faction ledgers unchanged, and the purses that were seeded stay at the seeded balance with no Trade lines.

Add one fleet case in that fixture:

- Put the scene ship and a second ship into one `FleetFactory` fleet in the ship's manager. Fund the fleet purse. Give the second ship its own purse with a different balance.
- A priced friendly buy commanded by the second ship debits the fleet purse and leaves the second ship's own purse at its seeded balance.
- A second buy commanded by the first ship debits that same fleet purse.
- A ship that is a fleet child, with the funds sitting only on its own purse and the fleet purse at 0, buys nothing.

`FreighterPlannerTests.OwnedPair_LoadsTheHold_AndLeavesTheLedgerAlone` is a same-faction haul and should stay green. `LedgerTests` stays as it is.

Run from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor` against `Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj`: `MarketExchangeTests` and `FreighterPlannerTests`.

## After this idea

How much a purse sends home, and how much the treasury pushes out to a new fleet or colony, is a separate pass once these purses exist. This idea also leaves out a purse display, any change to trade or freight planning, and any sweep of a ship purse when the ship joins or leaves a fleet.
