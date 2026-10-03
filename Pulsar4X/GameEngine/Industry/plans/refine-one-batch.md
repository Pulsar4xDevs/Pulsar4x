# Refine one batch

Status: implemented. Tests: `Pulsar4X.Tests/ColonyIndustryTests.cs`. This is not a logistics plan number. Do not fold it into logistics plan 4, and do not give the colony a second active goal.

## Outcome

A colony that can refine, and has the inputs, keeps one refined good topped up by a single batch. Earth, on a new game, puts one stainless-steel batch on its refinery. Luna Concord has no refinery, so its market stays the warehouse listing from Offer stock. The production tab shows the job. The market keeps updating while that job runs.

## Depends on

Logistics plan 4 (`IndustryOrder2`, `ColonyMarketPolicyDB`, `RunMarketPlan`, the colony wake). Logistics plan 12 (`OfferStock` is the standing goal this pass joins). Mines already fill the warehouse on their own processor. This plan does not replace that.

## What already runs

- `MineResourcesProcessor` is a hotloop. `GoalType.Mine` has a base weight and no planner. Leave it that way. `OfferStock` is a `GivenGoal`, so an idle pick never reaches Mine.
- `IndustryProcessor` advances jobs already on a production line, once a day, first run at three hours. It does not choose a design.
- `RunMarketPlan` enqueues industry only when a policy row has `AutoProduce` and `stock + alreadyQueued < Max`. Offer stock's new rows set `AutoProduce` false and `Max` 0, so that path stays quiet.
- One `IndustryOrder2` stays in the colony action queue until `NumberCompleted == NumberOrdered`. The colony Active wake plans again only when that goal's queue is empty. A refining batch on this goal would freeze the market pass until the batch finished.

The processor hazards around `ConstructStuff`, and the completion-count versus `OutputAmount` mismatch, are listed in [the industry plans index](README.md). This plan orders one completion and does not turn `AutoProduce` on, so it does not depend on fixing that mismatch first.

## Decision: one wake, one batch, refining only

Add `GoalType.RunIndustry`, appended after `OfferStock`. Planner `RunIndustryPlan`. It stays out of `BaseWeights`. No command, no button, no skill term, no XP. `SkillDomains.DomainOf` stays as it is. The goal does not complete, and the factory does not assign it.

`OfferStockPlan` calls `RunIndustryPlan` before it adds warehouse rows and before `RunMarketPlan`. It returns the industry actions and the market actions together. Do not `AssignGoal(RunIndustry)` on the colony. That would replace Offer stock.

A supply order still replaces Offer stock. While that order is the goal, this pass does not run. Rows and jobs already queued stay.

### Which good

On each production line, consider `FactionInfoDB.IndustryDesigns` that are processed materials and whose `IndustryTypeID` is one of that line's `IndustryTypeRates` keys. Earth's refinery rate key is `refining`. Stainless steel, RP-1, methalox, and hydrolox are the unlocked recipes. Components, installations, and ship designs are not candidates.

Skip a line that already has a job with `NumberCompleted < NumberOrdered`. That includes a job the player queued from the production tab.

A candidate needs every `ResourceCosts` input in stock for one batch. `NumberOrdered` is 1. One completion adds `OutputAmount` to the warehouse (100 for stainless steel, 2 for each of those fuels). Do not order `OutputAmount` as the batch count. `RunMarketPlan` treats the gap as a completion count, so this planner returns its own `IndustryOrder2` and leaves `AutoProduce` false. `AutoAddSubJobs` is false.

Rank the candidates that can be paid for by whole batches already in stock: `stock / OutputAmount`, lowest first, then cargo id. Queue that one. A recipe with nothing stored waits while any refined good is already in the warehouse. Earth also unlocks plastic, nuclear thruster propellant, fissile fuels, and electricity with no pile; stainless steel is the thinnest pile it stores, so that is the batch. When the colony stores none of its refined goods, every payable recipe stays in that ranking. An empty recipe, or one whose output has no volume, is not a candidate. Electricity is both.

### How far to go

If the good has no policy row, add one while `Rows.Count` is under `LogiBaseDB.Capacity`: Min 0, Max = stock + `OutputAmount`, AutoProduce false, Ask 0, Bid 0. Then queue the one batch, because stock is under that Max.

If the office is full, do not add the row and do not queue. The message is "{cargoId} skipped for capacity".

If a row already exists, leave Min, Max, Ask, Bid, and AutoProduce as they are. Queue one batch only when stock is under Max, the line is idle, and the inputs for one batch are in stock. An Offer stock row with Max 0 does not qualify. A row with `AutoProduce` true is left to `RunMarketPlan`.

`OfferStock` runs after this and does not rewrite a row that now exists. The other warehouse piles still fill the remaining office slots.

The batch is not on the line until the agent executes the `IndustryOrder2`. The same wake's `RunMarket` pass can still list the inputs at the full surplus. The next wake sees `ResourcesRequiredRemaining` and holds those inputs the way logistics plan 4 already does.

### Colony wake

In the colony Active branch, an `IndustryOrder2` still on this goal's queue does not count as "still queued" for the replan check. Any other action for the goal still does. The planner's "line already has a job" check is what stops a second batch. Ship and fleet wakes stay as they are.

## What a new game shows

Earth has a refinery and the recipes. The first Offer stock pass queues one stainless-steel batch and sets that row's Max to the steel already in the warehouse plus 100. Iron, chromium, and hydrocarbons drop by one recipe (88, 11, and 1) when the industry day processes the batch. The market lists steel and the other large piles, five goods, same as Offer stock. After the batch, stock meets Max and the line goes idle. A later wake queues another single batch only if steel is under Max again and the inputs are there.

Luna Concord has mines and no refining line. Nothing is queued. Its four listings stay iron, titanium, silicon, and methalox.

## Tests

`Pulsar4X.Tests/ColonyIndustryTests.cs`. Call `RunIndustryPlan.Plan` directly, except one test that calls `OfferStockPlan` and one that drives the colony wake.

- A refining line, inputs for stainless steel, and no output: one `IndustryOrder2`, `NumberOrdered` 1, `AutoAddSubJobs` false, the refinery's line id. The new row has Max equal to `OutputAmount`, AutoProduce false, Ask 0, Bid 0.
- A second pass while that job is on the line returns no industry action.
- Short one input: no action and no row.
- An existing row with Max 0 is unchanged and queues nothing. An existing row with Max above stock queues one batch and does not change Ask, Bid, Min, or AutoProduce.
- A row with AutoProduce true queues nothing from this planner.
- A component design on a factory line queues nothing.
- A line that already holds a player job queues nothing.
- No `IndustryAbilityDB`: no actions.
- `OfferStockPlan` on that refining colony returns the industry action and the market actions in one result. The standing goal type stays `OfferStock`.
- `GoalsDB.BaseWeights` does not contain `RunIndustry`.
- Colony Active plans again while an unfinished `IndustryOrder2` for this goal is on the queue, and does not add a second batch.

Update `EarthStartTests`: the steel row's Max is the stored steel plus 100, and one refining job is queued. Luna's listing test stays four goods and no industry job.

## Left for the administrator

Patience and specialization live on the seated person and decide how the office spends its slots. This planner does not read `AgentDB`. Bid stays 0. An empty chair still runs one batch on a refining line that already exists. The administrator notes are `GameEngine/Engine/Orders/colony-administrator.md`. They are not a plan.

A factory goal, installation construction, and a shipyard goal stay their own plans, and they wait until a seated administrator can place a bid. Building a new installation waits until that bid has brought the goods in, and until construction spends those goods. `LocalConstructionProcessor` adds the component and does not take cargo.

## Later

Population use, a preferred fuel, a catalog price, putting Offer stock back after a supply order replaces it, and an office gained after the colony is founded.
