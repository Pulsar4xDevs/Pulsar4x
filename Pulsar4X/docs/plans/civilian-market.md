# Civilian market department

Status: plan, not started. Written 2026-11-04 for Grok Build. Do not build a slice until Jon says go.

Third sub-plan in the [civilian-administrators](civilian-administrators.md) plan of plans, after [civilian-recruitment](civilian-recruitment.md) and [civilian-industry](civilian-industry.md). It covers the market department head: adjusting prices, setting reserves, and taking requests from the other department heads.

## What exists today

- **Policy rows.** `ColonyMarketPolicyDB` holds one `MarketPolicyRow` per good: `Min` (the reserve), `Max`, `AutoProduce`, `Ask`, `Bid`. The player sets them; nothing adjusts them.
- **Posting.** `RunMarketPlan` reads the rows, holds back `max(Min, needed)` for each good, ranks goods by deficit then surplus, and posts listings at the row's `Ask`, `Bid` and `Reserve`. It only reposts when a listing changed.
- **The colony goal.** `OfferStockPlan` runs `RunIndustryPlan`, offers stock, then runs `RunMarketPlan`, all in one goal.
- **Administrator quality.** `ColonyAdministrator.Quality` returns 1.0 always, with or without an administrator.

## Decision

**Decided (Jon, 2026-11-04):** the market department head adjusts prices as needed and sets reserves when other department heads ask. The other heads do not edit market rows themselves; they send requests to the market head, who acts on them.

With no market head seated, the rows stay exactly as the player set them, and `RunMarketPlan` keeps posting them as today. Nothing adjusts prices.

## What the market head does

1. **Takes requests.** Other heads post a **market request** on the colony: who asked (portfolio), the good, what they want (raise the reserve to N, buy N, or free N for sale), and why. The market head reads them on its wake.
2. **Sets reserves.** A reserve request raises that good's `Min` to the requested amount. This is the protected-reserve list: the market never sells below it. The industry shortage rule uses this: industry raises a reserve request and signals the shortage.
3. **Adjusts prices.** Decided (Jon, 2026-11-04), with the caveat that pricing will need retuning as the game gains more expenses:
   - **The buyer is the main price setter.** The head prices mostly in response to what buyers bid; its own ask is a floor and a starting point, not the price it insists on.
   - **Sell floor.** The head never asks below what the item cost the colony to make. As a ballpark, the starting ask is double that production cost.
   - **Age and amount.** From there the ask comes down toward the floor the more stock it holds and the longer the stock has sat unsold.
   - **Buying.** While a good is below its reserve, the head raises its `Bid` to attract supply.
   - Personality (Patience once it replaces Greed) and the market slider weight bend how fast it moves. Step sizes are for tuning.
4. **Reads other colonies' prices.** Decided (Jon, 2026-11-04): the head looks at other colonies' asks and bids (through the market book and intel it already has) when setting its own bid and ask.
5. **Places buy orders and haul requests.** When a good is short, the head can post a buy order (a bid) for it. This uses the existing market path from the shipments design: when a seller fills the bid, the buying office pays at seal time and posts an `IntelKind.Haul` row with a freight fee, and a free ship claims it (`ClaimHaulCommand`, `DeliverShipment` goal) and is paid on delivery. So the haul request already exists; the head only needs to post the bid. Moving goods between the faction's own colonies has `HaulPairCommand` today; whether the head should use it for its own colonies is open (question 6).
6. **Settles clashes.** When two requests conflict (industry wants 50 Iron held, research wants 50 Iron freed), the higher slider weight wins; equal weights fall back to the capital's priority order. Each clash is logged once.
7. **Respects the player.** A row the player edited carries `PlayerSet`; the head never overwrites it. Requests on that good are logged as blocked ("Market (Ng): Iron reserve is player-set, request from Industry not applied").

Log lines, tagged Market: `Market (Ng) - raised Iron bid to 12.0, below reserve 200 (asked by Industry).`

## How other heads interact with it

| Head | Sends | Market head does |
|---|---|---|
| Industry | Reserve request when a build is short; buy request for missing inputs | Raises the reserve, raises the bid |
| Industry | Free-for-sale when a finished good piles up | Lowers the reserve back, lists the surplus |
| Research | (later) reserve for lab materials, if research ever consumes goods | Same as industry |
| Colony head | (later) reserve or buy requests for construction materials, when the colony head runs construction | Same as industry; the colony head is a seat only for now |

With the market seat empty, requests are kept but not acted on, and the requester logs "waiting on a market head".

## Slices

Each slice is one PR, tests first. Run tests from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor`, filtering `Pulsar4X.Tests`.

1. **Market seat and its own goal.** A market portfolio seat, filled through the recruitment picker. Split `RunMarketPlan` out of `OfferStockPlan` into the market head's goal. Tests: with no market head, posting matches today; with a head, posting still matches today (no adjustments yet); the seat survives save and load.
2. **Market requests.** A request list on the colony (requester, good, kind, amount, reason), saved and loaded. Tests: a request can be posted and read; it survives save and load; with no market head, it stays pending.
3. **Reserves from requests.** The head applies reserve requests to `Min`, skipping `PlayerSet` rows. Tests: a reserve request raises `Min`; a player-set row is untouched and the block is logged; the market never lists below the new reserve.
4. **Price adjustment.** Starting ask at double production cost, never below production cost, lowered by stock amount and time unsold; bid raised below reserve; other colonies' prices read as input. Tests: the ask never goes below production cost; a fresh good asks about double its cost; the ask falls as stock ages or grows; bid rises while below reserve and stops once stock reaches it; a higher bid elsewhere moves the head's price; market weight 0 changes nothing; same seed gives the same prices.
4b. **Buy orders.** The head posts a bid for a good below its reserve; a fill goes through the existing seal and `IntelKind.Haul` path. Tests: a short good gets a bid listing; a filled bid seals a shipment and posts a haul row; no new hauling code is added.
5. **Clashes.** Conflicting requests settle by slider weight, then capital priority. Tests: higher weight wins; equal weight uses the priority order; one log line per clash.
6. **Industry wiring.** The industry head's shortage rule posts reserve and buy requests here (slice 5 of the industry plan). Tests: an industry build short of Iron leads to a raised Iron reserve and bid on the next market wake.

## Open questions for Jon

1. **Partly decided (Jon, 2026-11-04):** the buyer is the main price setter; the sell floor is production cost, with double that as the starting ask, lowered by amount and age of stock. Still open: step sizes, a ceiling on the bid, and how "production cost" is computed (input goods at what price, plus build points or credit cost?). Expect retuning as more expenses are added.
2. **Decided (Jon, 2026-11-04):** yes, the head reads other colonies' prices.
3. **Decided (Jon, 2026-11-04):** once a reserve request is no longer needed, the reserve drops back, slowly (a gradual decay toward its earlier level, not an instant reset). Easiest first (Jon): a fixed step down on each market wake until it reaches the earlier level; tweak from there.
4. Should requests expire if not acted on, or wait forever? Easiest first (Jon, 2026-11-04): no expiry; a request stays until it is met or withdrawn by its sender. Tweak from there.
5. Does Patience bend price moves in the first cut, or wait for the Greed to Patience slice?
6. For the faction's own colonies, should the head move goods with `HaulPairCommand` (no market, no payment), or always go through bids?
