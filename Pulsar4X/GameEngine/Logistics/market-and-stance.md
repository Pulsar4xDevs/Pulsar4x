# Market and stance

Status: **design for plans 1–3** of the trade roadmap. Not implemented.
Implementation steps for those three are `GameEngine/Logistics/plans/01-faction-stance.md`, `02-market-book.md`, and `03-settlement.md`. The index of all nine plans is `GameEngine/Logistics/plans/README.md`.
Later plans (colony orders, bar graph, trader, freighter) use the words defined here.

Companion: `GameEngine/Engine/Orders/agents-and-goals-design.md`. This note does not add goals. Settlement is an action. Ships are not moved.

## Purpose

1. A faction can be friendly to another faction, and the snapshot says so.
2. A logistics office holds a market book: what that location will sell, what it will buy, and at what price. Stock stays in `CargoStorageDB`.
3. A ship in transfer range can buy from a sell offer or sell into a buy request. Cargo and the book change together. A foreign trade moves faction money. An own-faction trade does not.

## Vocabulary

| Word | Meaning |
|---|---|
| **Stance** | How faction A treats faction B. Stored on A. Missing entry means Hostile. |
| **Listing** | One cargo id at one market: sell quantity and ask, buy quantity and bid, reserve. |
| **Ask** | Price per unit a buyer pays the market to take goods from a sell offer. |
| **Bid** | Price per unit the market pays a seller who fills a buy request. |
| **Reserve** | Units of stock that must stay. They are not offered. |
| **Sellable** | `max(0, stock − reserve)`. The posted sell quantity can be higher; a trade moves `min(sell quantity, sellable, what the ship can hold)`. |
| **Cargo id** | `ICargoable.UniqueID` (`"iron"`, `"sorium-fuel"`). Shared across factions. Not the runtime `Mineral.ID`, and not an `ICargoable` object reference. |
| **Market** | An entity with `LogiBaseDB`. Installed by `LogiBaseAtb` (the logistics office). |

`EntityFilter.Friendly` is not stance. In `EntityManager.GetFilteredEntities` that flag means "owner id equals the querying faction". Leave it alone. Trade does not read it.

`Game.NeutralFactionId` (−99) is unowned bodies (stars, planets, jump points). It is not a diplomatic Neutral. It has no stance and cannot trade.

## Decisions

1. **Stance is one-directional data, mutual for trade.** Each faction stores its own stance. The map shows the viewer's stance. Settlement requires both sides to be Friendly or Allied, or the same faction.
2. **Allied and Friendly trade the same way.** The snapshot has no Allied value, so both project as `OwnerRelation.Friendly`. The stored enum still has Allied.
3. **Diplomatic Neutral and unowned Neutral share `OwnerRelation.Neutral`.** Neither can trade. They look the same on the map until a later note needs to split them.
4. **One book per logistics office.** `ListedItems`, `demandSupplyWeight`, bids, and in-transit maps go away. Stock is `CargoMath.GetUnitsStored` (escrow not included).
5. **Capacity is how many distinct cargo ids the office can list.** The installation template already says that (`"Logistic Capacity"`, default 5, range 5–100). Updating an existing id does not consume a new slot. Capacity is not a volume cap.
6. **Prices are decimal per unit, authored on the listing.** No price catalog in this note. Zero is allowed. Negative is rejected.
7. **Same-faction settlement does not touch `Ledger`.** There is one wallet per faction, so a colony-to-colony haul cannot pay itself. Cross-faction settlement debits the payer and credits the receiver by `price × units`.
8. **The commercial commit is immediate and in range.** The action moves cargo with the existing add/remove helpers, shrinks the listing, and writes the ledger in one execute. It does not enqueue `CargoTransferOrder` (that order would move the same goods again) and it does not move the ship. Out of range fails. A later planner is responsible for `MoveTo` first.
9. **No reservation field.** The listing shrinks by the units actually moved. Two ships settling the same offer in one pulse can oversell; the pulse is single-threaded and this slice does not add a claim map. Add one only if a test shows the hole.
10. **The old logistics cycle stops running in this work.** It reads `ListedItems` and issues `WarpMoveAction.CreateCommandEZ`. Leaving it callable will not compile, and leaving it running fights the book. Deleting `LogiShipperDB` waits for the trader plan; it must not be ticked.

## 1. Faction stance

On `FactionInfoDB`:

```csharp
public enum FactionStance { Hostile, Neutral, Friendly, Allied }

public Dictionary<int, FactionStance> Stances { get; } = new();
```

Key is the other faction's entity id. `Clone` / the copy constructor must copy the dictionary. Do not take on the rest of that clone (it already drops `Money`).

```csharp
static bool CanTrade(FactionInfoDB viewer, int otherFactionId, FactionInfoDB other, int viewerFactionId)
```

- Same faction id → true.
- Either id is `NeutralFactionId`, or either faction is missing → false.
- Viewer's stance toward the other is Friendly or Allied, **and** the other's stance toward the viewer is Friendly or Allied → true.
- Anything else, including a missing key, → false.

`GameProjector.RelationOf(entity, viewerFactionId)`:

| Case | `OwnerRelation` |
|---|---|
| `entity.FactionOwnerID == viewer` | Owned |
| `entity.FactionOwnerID == NeutralFactionId` | Neutral |
| viewer's stance toward the owner is Friendly or Allied | Friendly |
| viewer's stance toward the owner is Neutral | Neutral |
| missing or Hostile | Hostile |

One-sided Friendly shows as Friendly and still fails `CanTrade`. That is deliberate.

### Command

`SetFactionStanceCommand(int TargetEntityId, int OtherFactionId, FactionStance Stance) : GameCommand`

Target is the faction entity the session owns. The translator writes `Stances[OtherFactionId] = Stance` and nothing on the other faction. Reject when the target is not that faction, the other id is the target, the other id is `NeutralFactionId`, or the other faction does not exist. No UI in this note. Tests that need a trade set both sides.

## 2. Market book

`LogiBaseDB` keeps its name and `Capacity`. Replace the other fields with:

```csharp
public sealed class MarketListing
{
    public string CargoId;       // ICargoable.UniqueID
    public long SellQuantity;
    public decimal Ask;
    public long BuyQuantity;
    public decimal Bid;
    public long Reserve;
}

public Dictionary<string, MarketListing> Listings = new();
```

Remove `DesiredLevels`, `ListedItems`, `ItemsWaitingPickup`, `ItemsInTransit`, and `TradeShipBids`.

`LogiBaseAtb` is unchanged: install creates the blob and sums capacity; uninstall subtracts, and capacity 0 removes the blob.

Helpers, static, no processor:

| Helper | Behaviour |
|---|---|
| `TryGet(entity, cargoId)` | Listing or false. Requires `LogiBaseDB`. |
| `SetListing(entity, listing)` | Reject negative quantities or prices. If `CargoId` is new and `Listings.Count >= Capacity`, reject. Otherwise replace that id. |
| `Stock(entity, cargo)` | `GetUnitsStored(storage, cargo, includeEscrow: false)`. |
| `Sellable(entity, listing, cargo)` | `max(0, Stock − Reserve)`. |

Each faction resolves `CargoId` through its own `FactionDataStore.CargoGoods`. A trade needs both factions to have that id.

Default start, replacing the iron `(1000, 1)` / `(-1000, 1)` lines:

| Colony | Cargo | Sell | Ask | Buy | Bid | Reserve |
|---|---|---|---|---|---|---|
| Earth | `iron` | 1000 | 10 | 0 | 0 | 0 |
| Mars | `iron` | 0 | 0 | 1000 | 12 | 0 |

The spread is scenario data so a later trader test has a profit. It is not a rule that bids exceed asks.

`TransactionCategory.Trade` is added on `Ledger` now. No transactions are written until settlement.

### Old cycle

- `LogiBaseProcessor` and `LogiShipProcessor`: `ProcessEntity` / `ProcessManager` return without calling `LogisticsCycle`.
- `LogisticsCycle` bidding methods become empty (or the file is deleted if nothing else references it). `LogisticsSimple` and `LogisticsNewtonion` stay on disk until the trader plan removes them; nothing in this note calls them.
- Delete `SetLogisticsOrder`. Its only callers are the parked windows, which are already `Compile Remove` in `Pulsar4X.Client.Host.csproj`. Those parked files may keep referring to the old fields; they are not in the build.
- `LogiShipperDB` and `ShipLogisticsOrders` stay, uncalled, until the trader plan.

## 3. Settlement

One action, commanded entity is the ship. The market is a target. Ownership is checked on the ship. The market is checked with `CanTrade`, not with ownership. This is the one place a command mutates another faction's cargo and ledger.

```csharp
enum MarketSide { BuyFromMarket, SellToMarket }
```

`BuyFromMarket`: ship takes goods, pays the ask.
`SellToMarket`: ship gives goods, receives the bid.

Execute, once, and set finished inside execute (a constant-true `IsFinished` accepts the order and never runs it):

1. Market has `LogiBaseDB` and a listing for the cargo id. Else fail `"No listing"`.
2. `CanTrade` between ship faction and market faction. Else fail `"Cannot trade"`.
3. In cargo-transfer range: the same Δv check `CargoTransferProcessor` already uses against both `TransferRangeDv_mps`. Else fail `"Out of range"`. Do not wait.
4. Both factions resolve the cargo id. Else fail `"Unknown good"`.
5. Quantity is the minimum of the requested units and:
   - **Buy:** `SellQuantity`, `Sellable`, free unit space on the ship.
   - **Sell:** `BuyQuantity`, units on the ship, free unit space on the market.
6. If the factions differ and the price is > 0, also cap by what the payer can afford: `floor(funds / price)`. Payer is the ship faction on a buy, the market faction on a sell.
7. If the quantity is 0, fail `"Nothing to exchange"`.
8. Otherwise move that many units with the existing cargo add/remove helpers (mass or items, matching how that good is stored), subtract from `SellQuantity` or `BuyQuantity`, and if the factions differ write `Ledger` `Trade` entries: payer `AddExpense`, receiver `AddIncome`, description carries cargo id, unit count, and the other faction id. Same faction: skip the ledger.

Fail means no cargo change and no ledger change. `Details` carries the message.

The action does not create a `MoveTo`, a warp order, or a `CargoTransferOrder`.

## Tests

- Missing stance is Hostile. One side Friendly projects as Friendly and `CanTrade` is false. Both sides Friendly: `CanTrade` is true and the snapshot is Friendly. Neutral faction id never trades.
- `SetFactionStanceCommand` rejects self and the neutral id, and does not write the other faction's dictionary.
- A new cargo id past capacity is rejected. Replacing iron on a full book is accepted. Stock is not a field on the listing.
- Default Earth listing is iron sell 1000 at 10. Mars is iron buy 1000 at 12.
- Same-faction buy in range: ship stock rises, market stock falls, sell quantity falls, both ledgers unchanged.
- Two friendly factions, ship buys: buyer funds drop by `ask × units`, seller funds rise by the same, cargo and sell quantity move.
- Hostile, or out of range: failure message, cargo and funds unchanged.
- Request larger than the offer, the sellable stock, or the payer's funds: the moved amount is the minimum, not the request.
- After a logistics processor tick, a ship with `LogiShipperDB` has no new warp order.

## Not in this note

Colony produce / buy goals, the bar-graph view, trader and freighter planners, inter-system markets, autonomous Decide, population consumption, colony wallets, treaties, and deleting `LogiShipperDB`.
