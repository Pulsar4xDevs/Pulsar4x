# Civilian industry department

Status: plan, not started. Written 2026-11-04 for Grok Build. Do not build a slice until Jon says go.

Second sub-plan in the [civilian-administrators](civilian-administrators.md) plan of plans. It follows [civilian-recruitment](civilian-recruitment.md), which supplies the people. This plan covers the industry department head: what they do, and what stops happening when the seat is empty.

## What exists today

- **Automatic sub-jobs.** `IndustryOrder2.AutoAddSubJobs` defaults to `true`. When a new job is queued, `IndustryTools.AutoAddSubJobs` walks the job's `ResourcesRequiredRemaining`, and for each input the colony's stockpile is short of that is itself buildable (`IConstructableDesign`), it creates a job for the shortfall, puts it on the fastest line (`SetJobToFastest`), and recurses. That is how a factory job that needs plastic queues a refining job, and a shipyard job that needs components queues factory jobs.
- It runs for every player-queued job, with no person involved. `RunIndustryPlan` and `RunMarketPlan` already set `AutoAddSubJobs = false` for their own jobs.
- There are no tests for the sub-job chaining itself (only `ColonyIndustryTests` checking the flag is off for the plan's orders).

## Decision

**Decided (Jon, 2026-11-04):** automatic sub-jobs become the industry department head's work. With an industry head seated on the colony, queuing a job chains its feeder jobs as today. With the seat empty, a queued job adds only itself; the player queues the refining and factory jobs by hand, and the job waits on its inputs.

This makes the seat worth filling on day one, keeps the behaviour players already know, and gives the head a first, simple job before any slider or personality work.

## What the industry head does

In order of slices:

1. **Feeder jobs (this plan's core).** When a job is queued on the colony, the head adds the sub-jobs for missing buildable inputs, recursively, on the fastest lines. The log records it: `Industry (Lee) - queued 40 Plastic for Hull Section.`
2. **Idle lines (later slice).** Put idle lines to work in proportion to the industry weight, using `RunIndustryPlan`. No named-good focus: the head reads market signals (prices, bids, demand) together with need signals (stock of goods the colony's own builds or downstream lines require), not value alone, so a high-value item never crowds out a lower-value input the colony needs. Guidance for that slice, not a decided implementation.
3. **Shortages (later slice).** When a build is short of a material, the head raises that material's protected reserve to the requested amount and signals the shortage to the market administrator, who raises its bid. Needs the market department, so it waits for that plan.

Scope: every production line, meaning refineries, factory and component lines, and the shipyard (hulls and refits), all on `IndustryProcessor`. Construction that spends cargo (`LocalConstructionProcessor`) stays out.

## Slices

Each slice is one PR, tests first. Run tests from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor`, filtering `Pulsar4X.Tests`.

1. **Industry seat.** An industry portfolio seat on the colony, filled through the recruitment picker. Tests: a hired admin can be seated as industry head; unseating empties it; the seat survives save and load.
2. **Gate sub-jobs on the seat.** `IndustryOrder2` chains sub-jobs only when the colony has a seated industry head. First add tests for today's chaining (factory job short of plastic queues a refining job; shipyard job short of components queues factory jobs, recursively). Then: with no head, a queued job adds only itself; with a head, chaining matches today; the plan orders (`RunIndustryPlan`, `RunMarketPlan`) are unchanged. A log line per chained job, tagged Industry.
3. **Starting state.** Decided (Jon, 2026-11-04): it depends on the start. The base mod start begins with department heads seated (including industry), so feeder jobs chain on day one. The early start begins without them, so feeder jobs stop until the player hires. Luna Concord's seats are filled either way. Tests: base mod start chains on day one; early start does not until an industry head is seated; Luna chains on day one in both.
4. **Idle lines.** As described above. Needs the colony direction sliders (Direction tab plan).
5. **Shortages.** As described above. Needs the market department plan.

## Open questions for Jon

1. **Decided (Jon, 2026-11-04):** the base mod start begins with department heads seated; the early start begins without them.
2. Should the client's industry window keep a per-job "add sub-jobs" tick box for when a head is seated, or does the head always chain?
3. Should a vacant seat show a hint on a stalled job ("Inputs missing. No industry head to queue them.")?
4. Should the head chain across colonies later (a sub-job on another colony with better lines), or stay local?
