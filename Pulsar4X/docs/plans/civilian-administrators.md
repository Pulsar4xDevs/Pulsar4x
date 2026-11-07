# Civilian administrators: from micro to macro

Status: plan, not started. Written 2026-11-04 for Grok Build. Do not build a slice until Jon says go. Personality, bids, and construction were marked "do not build until asked" in the master plan. This plan is how they get asked, slice by slice.

Read first: [design/colony-administrator.md](../design/colony-administrator.md) (one intent on the colony, who writes the book, the person), [design/agents-and-goals.md](../design/agents-and-goals.md). The ship half of this ladder was the deleted plan `GameEngine/People/plans/officers-and-span.md` ("Officers, the academy, and command span"). Wakes and the log came from `GameEngine/Engine/Orders/agent-wakes.md` and `agent-log.md`. All three can be recovered with `git show cc4861cd^:Pulsar4X/<path>`.

## Plan of plans

**Decided (Jon, 2026-11-04):** this plan grew too big, so it is now a plan of plans. Pieces are split into their own plan files one at a time, in implementation order. Until a piece is split off, its design stays in the sections below; once split, the sub-plan is the source of truth and the section here shrinks to a pointer.

| Order | Sub-plan | Depends on | State |
|---|---|---|---|
| 1 | [Civilian recruitment](civilian-recruitment.md): per-colony candidate pools and the hire picker | Nothing | Built ([PR #500](https://github.com/Pulsar4xDevs/Pulsar4x/pull/500)) |
| 2 | [Civilian industry department](civilian-industry.md): automatic sub-jobs need a seated industry head; then idle lines and shortages | Recruitment | Split off |
| 3 | [Civilian market department](civilian-market.md): price adjustment, reserves set on request from other heads, market requests | Recruitment, industry | Split off |
| 4 | [Civilian construction](civilian-construction.md): the colony head runs construction and requests inputs from industry and market | Recruitment, industry, market | Split off |
| — | Remaining pieces (personality, several goals per colony, wakes, seats, Direction tab, research, log, faction capital, Greed to Patience) | — | Still in this file, to be split one by one |

## What the player gets

Today the player runs a colony by hand: edit listings, queue jobs, queue buildings. A seated captain already moves a ship's work up a level. This plan does the same for colonies. The player seats people, sets a direction for the colony, and reads why those people did what they did. The player can still reach down and edit anything.

The ladder, extended from the colony-administrator note:

| Seat | What the player stops doing |
|---|---|
| Empty chair | Nothing. Offer stock lists surplus at 0, as today. |
| Market administrator | Market book: what to list, what to want, reserves. |
| Industry, research, or growth administrator | That portfolio's queue: industry jobs, research projects, later buildings. |
| Several staffed colonies | Faction-level direction; a supply order still covers several offices. |

## The faction capital

The last rung of the ladder needs a seat, the same way a colony administrator needs a colony seat. That seat is a faction capital building. A faction has at most one. The colony that holds it is the capital colony.

What it does:

- It holds the faction-level seats: the head of the civil service and, optionally, a thin faction research agenda seat (see the note in the researchers section).
- Its holder sets the faction default direction that new colonies inherit, and can push a direction to several staffed colonies at once.
- Its rank and the building decide how many colonies take faction direction, the narrower of the two. Colonies outside that limit keep their own direction and are greyed in the capital's list with the reason.
- With the capital empty or unbuilt, nothing changes: each colony runs on its own administrators and the faction default stays whatever the player set.

Losing or moving the capital drops the faction seats back to empty. Their colonies keep their last direction until a new capital is seated. The capital is a later slice, after the Direction tab (slice 7), and is built only by site construction, never as a prefab, so it waits on colony construction before it can appear in a game.

## One governor, or several administrators?

### Option A: one administrator does everything
One person per colony, one `AgentDB`, one goal that calls the market, industry, and research passes.

- Good: matches "one intent on the colony". One wake, one log, one personality to read. Simple to save and to show.
- Bad: no reason to have more than one civilian. A mining specialist is also the research director. Nothing to grow into.

### Option B: a specialist per portfolio
Market, industry, growth, and research each have their own seat and their own goal.

- Good: lots of people to recruit and promote; each one's personality shapes one thing.
- Bad: several goals on one colony. Today a second goal replaces the standing one (`AssignGoal(RunIndustry)` stays out for that reason). They compete for the same warehouse, the same listing slots, and the same build queue, and nobody breaks ties.

### Decided: a specialist per portfolio (option B)
Jon chose option B on 2026-11-04. Each colony has up to four civilian seats: market, industry, growth, and research. Each seat holds one person with their own `AgentDB`, their own personality, and their own standing goal. A seat that is empty stays manual, greyed in the UI with the reason ("Industry is manual. Seat an industry administrator.").

- **Seats.** `AdminSpaceAbilityState.CommanderSeats` already exists. Each portfolio maps to a seat. More admin space, more seats. A colony without enough admin space for a portfolio's seat can't fill it.
- **One goal per seat.** The market administrator owns the market goal (replacing `OfferStock`), the industry administrator owns an industry goal, and so on. Each has its own wake and writes its own log lines, tagged with its portfolio.
- **Scarcity.** Civilians come from the same academy pattern as officers, later. Until then, the starting factory makes them as it does now (`CommanderFactory.CreateAdmin`).

### Open problem: several goals on one colony
Today a colony has one standing goal, and assigning a second replaces it (`AssignGoal(RunIndustry)` stays out for that reason). Option B needs that to change before any second portfolio ships:

- The colony must hold one standing goal per seat, keyed by portfolio, with a pending wake for each, still one wake per goal.
- The specialists share the same warehouse, listing slots, and build queue. Something has to break ties. Decided: in a clash for stock in the same tick, the higher slider weight wins; when weights are equal, ties break by a priority set from the faction capital (open question 12). Considered and not chosen: each portfolio owning its resources outright with first-claim reservations on stock, or a fixed priority order between portfolios. Until the faction capital exists (slice 10), slices 2 through 9 use a default priority order as a fallback: market, industry, research, unless changed.

This is now the first build slice after personality: multi-goal colonies, with tests that two goals on one colony both wake and neither replaces the other.

### Colony head (seat only, for now)

**Decided (Jon, 2026-11-04):** each colony also has a **colony head** seat, above its portfolio administrators. For now it is a seat only: it can be filled from the recruitment picker like any other position, shows in the UI, and survives save and load, but it has no standing goal and changes no behaviour. What the colony head will do (for example rebalancing the colony's sliders, settling department clashes locally, or carrying faction direction to the colony) is left for a later plan. **Idea (Jon, 2026-11-04):** construction (site buildings through `LocalConstructionProcessor`, the cargo-spending kind that the industry portfolio leaves out) could be the colony head's work. The colony head would decide what to build and send requests to the industry head (make the crates or parts) and the market head (reserve or buy the materials), the same request path the market plan uses. Split off into [civilian-construction.md](civilian-construction.md). This is a colony-level seat, separate from the head of the civil service at the faction capital.

## Researchers as civilian positions

Scientists already exist (`Tech/Scientist.cs`: `MaxLabs`, `AssignedLabs`, `ProjectQueue`), and `ResearchProcessor` runs whatever the player queued. Keep scientists as they are. They are the workers. Add a **research administrator** seat with its own standing goal:

- The research goal assigns free labs to scientists (up to `MaxLabs`) and refills an empty `ProjectQueue` from the colony direction (see below), cheapest available tech first. The research weight decides how many of the colony's free labs it fills and how quickly it refills queues: 0 fills none, a higher weight fills more.
- A scientist whose queue the player wrote is left alone. The goal only fills empty queues, and it writes a log line when it does.
- Research is faction-wide but labs sit on colonies. A colony's research administrator only touches scientists housed on that colony (`TeamsHousedDB`).
- Which tech categories research picks defaults to the administrator's Specialization. The player can override it by setting or changing category preferences on the colony manually; a player-set preference takes precedence over Specialization (open question 14).

**Faction research agenda (thin).** An optional faction-level seat, held at the faction capital, sets the overall research agenda by weighting favoured tech categories across the faction. Colony research administrators (a colony seat, decided in open question 5) work within that agenda using their own colony's labs. The faction seat only sets category weights. It does not assign labs or fill queues. With the seat empty, colony research administrators pick on their own.

## Colony direction: what the player sets

A new `ColonyDirectionDB` on the colony, edited from a new **Direction** tab in Colony Management. It holds four weights, one per portfolio: market, industry, research, and growth. The Direction tab shows them as four sliders. The weights always sum to 1.0: moving one slider up moves the other three down in proportion to their current values, so the sum stays at 1.0 (and moving one down raises the others the same way). A slider's value is the share of the colony's attention and resources that portfolio gets. Each portfolio's administrator reads its own weight every wake:

- **Market:** the market weight sets how much of the colony's surplus the market administrator lists and how hard it trades; 0 lists nothing new. Beside the market slider sits a **protected-reserve list**: goods and the reserve amount below which they are never sold, whatever the market weight. The market administrator never lists stock from that reserve. When an industry build is short of a material and that material's reserve is lower than the amount the build requested, the industry administrator raises that material's reserve to the requested amount, so the same shortage does not recur, and signals the shortage to the market administrator. The market administrator acts on that signal by raising its bid, the price the colony offers to buy that good, while the good is below its reserve, so the colony attracts supply. This is administrator behaviour, not a market mechanic. Once the need has passed, the reserve drops back slowly (see [civilian-market.md](civilian-market.md), open question 3).
- **Industry:** the industry weight sets how much of the colony's idle production capacity and stock the industry administrator puts to work; 0 adds nothing beyond what the player queues. There is no named-good focus setting: when lines are idle, the industry administrator reads market signals (prices, bids, demand) to decide what to queue. Design note for the industry slice (guidance, not a decided implementation detail): when choosing what to queue on idle lines, the industry administrator weighs market signals together with need signals (stock levels of goods the colony's own builds or downstream lines require), not value alone. A high-value item must not crowd out a lower-value input the colony needs.
  The industry portfolio covers every production line: refineries, factory and component lines, and the shipyard (hulls and refits). These run on `IndustryProcessor` and the production-line queue, so the industry administrator runs them, not the construction system. Construction that spends cargo (`LocalConstructionProcessor`, mounting crates and assembling site-only buildings) stays buildings only.
- **Research:** the research weight sets how many free labs the research administrator fills and how aggressively it refills empty queues; 0 changes nothing.
- **Growth:** the growth slider is recorded but drives nothing until construction exists. Construction is still out of this plan; later plans read it.

Plus one faction-level default: four slider values that a new colony inherits.

How administrators read it: each one takes its own portfolio's weight as the main input, and its personality bends it. Loyalty (or the existing obedience reading) is how closely the industry administrator sticks to its weight rather than its own judgement. Patience decides how much the market administrator holds back versus sells at a given market weight. Specialization steers which techs the research administrator picks within its weight. A portfolio with no seated administrator still shows its slider, but the slider does nothing, with the reason ("Industry is manual. Seat an industry administrator."). In the first cut, where industry is the only portfolio with an administrator, the market, research, and growth sliders are hidden, not shown, until their administrators exist (open question 15). Hidden sliders count as zero for the sum-to-1.0 constraint: only the visible sliders' weights must sum to 1.0. When a hidden slider first appears (for example when a market, research, or growth administrator becomes available and its slider becomes visible), it receives a starting weight and the other visible sliders shrink proportionally to make room, so all visible sliders still sum to 1.0. The exact starting weight is an open tuning question.

Player edits and administrators: a row (a slider or any other value) the player edits by hand gets a `PlayerSet` flag. No administrator overwrites a `PlayerSet` row. A "Hand back" button on the row clears the flag. This answers the open question in the design note one way.

## Wakes

Today the colony goal is re-armed every `CommanderSkills.RecheckInterval`, 30 game minutes. About 99.8% of those wakes do nothing (about 17,500 per colony per year, `/workspace/reports/market-wakes.log`).

Each administrator owns its own wake:

- Each standing goal schedules its next wake from its own portfolio's dates: the market goal from the next market settlement and cargo arrivals, the industry goal from the next job finishing, the research goal from the next project finishing. These are already events or processor dates.
- A backstop poll stays, but weekly, not 30 minutes. It is each administrator's own interval, so skill can shorten it.
- Events wake only the goal they concern, at once, through `RunAgentNow`: a contract settled or cargo delivered wakes market, a job done wakes industry, a tech researched wakes research. A direction change (any slider move, which shifts several weights) wakes each affected portfolio's goal; a seat change wakes the seat. Pending wakes are kept one per goal, not one per entity (this is part of the multi-goal slice), so they do not pile.
- An empty market seat keeps today's OfferStock behaviour with the weekly backstop. Other empty seats do nothing.

## Log and explainability

Use `ShipLogDB` (already on colonies) and the rules from the agent-log plan: one line per decision, no line for a wake that changed nothing. Every line is tagged with its portfolio and the administrator who wrote it. New lines:

- `Market (Ada) — listed 200 Iron, ask 0. Market 0.30.`
- `Industry (Lee) — queued Refine 1 batch of Steel. Industry 0.40.`
- `Research (Dr Kim) — took Ion Drives. Research 0.20.`
- `Industry — manual. No industry administrator seated.` (once, when it becomes true, not every wake)
- `Industry (Lee) — wanted 50 Iron, Market holds it reserved.` (once per clash, so ties are visible)
- Seat lines: `Ada took Market.` `Lee took Industry.`

Each goal returns a short reason string with what it chose; the log line is that reason plus the slider weight it followed. The Direction tab shows the last line per portfolio under its slider.

## Slices

Each slice is one PR, tests first. Run tests from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor`, filtering `Pulsar4X.Tests`.

1. **Personality on civilians.** Attach `AgentDB` in `CommanderFactory.CreateAdmin` and to scientists. Add Specialization and Loyalty. Leave Greed in place (renaming to Patience is its own slice). Tests: a new admin has an `AgentDB`, it survives save and load, a missing blob still reads as 1.
2. **Several goals on one colony.** A colony holds one standing goal per seat, keyed by portfolio, each with its own pending wake. Clashes go to the portfolio with the higher slider weight; equal weights break by a priority set from the faction capital (considered instead: ownership plus first-claim reservations on warehouse stock). Until the capital exists in slice 10, a default priority order is the fallback: market, industry, research, unless changed. A supply order still replaces only the goal it targets. Tests: two goals on one colony both wake and neither replaces the other; when two goals want the same stock in the same tick, the higher-weight goal gets it, and with equal weights the higher-priority goal gets it; save and load keeps both goals and their wakes.
3. **Industry administrator.** Moved to [civilian-industry.md](civilian-industry.md); the text below is kept until that plan is final. Industry goal: put idle lines to work in proportion to the industry weight, using existing `RunIndustryPlan`, choosing what to queue on idle lines from market signals (prices, bids, demand) rather than a named-good focus. Tests: an idle refinery with ore and industry weight above 0 gets one batch; industry weight 0 adds nothing; a higher industry weight puts more idle lines to work than a lower one; ore claimed by a higher-weight (or, on equal weights, higher-priority) goal is not used. Until the market administrator lands in slice 4, the market seat keeps running today's `OfferStock` behaviour.
4. **Market administrator.** Moved to [civilian-market.md](civilian-market.md); the text below is kept until that plan is final. New market goal and plan that does exactly today's `OfferStockPlan` work, replacing `OfferStock` as the market seat's standing goal. Behaviour must not change. Tests: Earth and Luna books are identical after 30 days with `OfferStock` vs the market goal; a supply order still replaces it.
5. **Wakes.** Each goal schedules from its own portfolio dates plus a weekly backstop, and the event wakes reach only the goal they concern. Tests: one year on Earth produces fewer than 400 market wakes (down from ~17,500) and the same book; a settled contract wakes the market goal at that instant and not industry; still one pending `AgentProcessor` row per goal (use `CountAgentInterrupts` pattern).
6. **Seats and the assign command.** Portfolio seats on `CommanderSeats`, limited by admin space; an assign-civilian command that seats a person in a named portfolio (follow `AssignCaptainCommand`: translator, snapshot, client). Tests: seating an industry administrator adds an industry goal; unseating removes it and leaves the others; no admin space for a seat means it can't be filled; an empty market seat still runs OfferStock.
7. **ColonyDirectionDB and the Direction tab.** Data, a set-direction command, the faction default, the tab. Only the industry weight (and the market weight, once slice 4 has landed) reaches code in this slice. Tests: direction saves and loads; sliders always sum to 1.0 after a move; moving one slider rescales the others proportionally; market weight 0 stops new sell rows; a new colony inherits the faction default slider values; a `PlayerSet` row survives a wake; hand-back clears it.
   Scope: ship a thin version in this pass. Basic four-slider data, the sum-to-1.0 constraint, the `PlayerSet` flag, and the faction default, with minimal UI in `ColonyManagementWindow`. Only the industry slider is shown; the market, research, and growth sliders stay hidden until their administrators exist and count as zero for the sum-to-1.0 constraint, so only visible sliders' weights must sum to 1.0. When a hidden slider first appears, it gets a starting weight (exact value an open tuning question) and the other visible sliders shrink proportionally so the visible sum stays 1.0. Deeper slider behaviour (locking, hidden sliders, research category steering, the protected reserve list, clash resolution by slider weight with capital priority as the tiebreaker) is a second pass; see open questions 11 to 16.
8. **Research administrator.** Research goal: assign free labs, refill empty queues by research weight, picking categories from Specialization unless the player has set category preferences, which override it. Tests: player-set category preferences override Specialization; research weight 0 changes nothing; a research weight above 0 fills an empty queue with an available tech; a higher weight fills more free labs; a player-written queue is untouched; labs on another colony are not used.
9. **Log lines and explain-on-tab.** Tests: one line per decision, tagged with portfolio; no line for an idle wake; the manual line written once; a reservation clash written once; lines survive save and load; other factions see no log.
10. **The faction capital.** Built by site construction only, so it waits on colony construction. Faction seats, faction default direction, push to colonies within its limit. Tests: an empty capital changes nothing; a seated capital pushes direction to colonies inside its limit and greys the rest; losing it empties the seats and colonies keep their last direction.
11. **Greed to Patience** (needs the trade/freight retune decision). Separate, do not start until Jon picks the curve.

Out of this plan: the second pass of slider behaviour (see slice 7's scope note), the bid number and price finding (master plan item 6), construction that spends cargo, a growth administrator goal (the growth slider is recorded but drives nothing until construction exists), a civilian academy, payroll, and friendly-faction tuning. (Luna Concord's starting civilians are in scope: slice 6 seats all of its administrator seats on day one.)

## Expected files

- New: `GameEngine/Colonies/MarketAdminPlan.cs`, `GameEngine/Colonies/IndustryAdminPlan.cs`, `GameEngine/Colonies/ResearchAdminPlan.cs`, `GameEngine/Colonies/ColonySeats.cs`, `GameEngine/Colonies/ColonyReservations.cs`, `GameEngine/Colonies/ColonyDirectionDB.cs`, `Pulsar4X.Tests/MultiGoalColonyTests.cs`, `Pulsar4X.Tests/ColonyAdministratorTests.cs`, `Pulsar4X.Tests/ColonyDirectionTests.cs`, `Pulsar4X.Tests/ResearchAdministratorTests.cs`.
- Change: `People/CommanderFactory.cs`, `Engine/Orders/AgentDB.cs`, `Engine/Orders/GoalsDB.cs` (one standing goal per portfolio; new goal types), `Engine/Orders/AgentProcessor.cs` (one pending wake per goal), `Logistics/OfferStockPlan.cs` (reused by the market goal), `Logistics/ColonyMarketPolicyDB.cs` (`ColonyAdministrator.Quality` reads the market seat; `PlayerSet` flag), `Colonies/ColonyFactory.cs`, `Api/CommandTranslator.cs`, `Api/GameProjector.cs`, `Pulsar4X.Client/Interface/Windows/ColonyManagementWindow.cs`.
- Docs: a row in `master-plan.md` section 2; update `design/colony-administrator.md` when slices land (it says "one intent on the colony", which option B changes to one intent per seat).

## Open questions for Jon

1. **Decided (Jon, 2026-11-04):** tie-breaking between administrators uses a priority set from the faction capital only when slider weights are equal. When weights differ, the higher slider weight wins (decided in open question 12). (Considered: ownership plus first-claim reservations, or a fixed priority order between portfolios.)
2. **Decided (Jon, 2026-11-04):** the player wins. A player edit sets the `PlayerSet` flag, the flag sticks, and the administrator does not overwrite it on the next wake.
3. **Decided (Jon, 2026-11-04):** only the industry portfolio is in the first cut. Market, research, and growth are deferred. Growth stays a stance only until construction exists.
4. **Decided (Jon, 2026-11-04):** sliders. Four sliders on the colony, one per portfolio (market, industry, research, growth), summing to 1.0; moving one rescales the others proportionally, and each value is that portfolio's share of the colony's attention and resources.
5. **Decided (Jon, 2026-11-04):** the research administrator is a colony seat.
6. **Decided (Jon, 2026-11-04):** a weekly backstop for each administrator.
7. **Partly decided (Jon, 2026-11-04):** yes, the admin complex level gates how many portfolios a colony can staff, as a progression mechanic. Still open: how many seats each admin complex level gives.
8. **Decided (Jon, 2026-11-04):** Luna Concord, as an AI faction, starts with all administrator seats filled on day one. Earth's seats depend on the start: the base mod start begins with department heads seated, the early start begins with them empty.
9. **Big open question (Jon, 2026-11-04):** where do administrators and research lab technicians come from, how are they recruited, and how often does the player get to choose them? The current placeholder (the starting factory makes them as it does today, `CommanderFactory.CreateAdmin`) is not enough. This needs its own recruitment plan, covering recruitment sources, the recruitment rate or frequency, player choice in who is recruited, and how administrators and lab technicians differ.
10. When a ship or component build is short of materials, how should the industry administrator get them: queue an automatic market buy order, send a supply request to another colony, or wait? Left for a later plan; flagged here so it is not forgotten. One possible approach to consider there: the protected-reserve rule above, where a shortage raises that material's reserve to the requested amount so the market stops selling it. The work is split: the industry administrator raises the reserve and signals the shortage, and the market administrator acts on that signal by raising its bid for the good while it is below its reserve. Not decided.
11. **Decided (Jon, 2026-11-04):** keep the protected-reserve list (goods never sold below a reserve) beside the market slider, as its companion.
12. **Decided (Jon, 2026-11-04):** the slider weight wins. In a clash for stock in the same tick, the portfolio with the higher slider weight takes precedence, overriding the capital's priority order. The capital's priority order applies only when slider weights are equal.
13. **Decided (Jon, 2026-11-04):** drop the old industry "Focus on one chain" choice. The industry administrator gets no named-good focus setting; instead it reads market signals (prices, bids, demand) to decide what to queue when lines are idle.
14. **Decided (Jon, 2026-11-04):** the player can override the administrator's Specialization when needed to steer which tech categories research picks. Specialization remains the default, but the player can set or change category preferences manually.
15. **Decided (Jon, 2026-11-04):** hidden. The market, research, and growth sliders are not shown in the UI until their respective administrators exist.
16. Should the sliders allow a portfolio to be locked (held fixed while others move), and how does a `PlayerSet` slider interact with an administrator or capital push that would rescale it?
