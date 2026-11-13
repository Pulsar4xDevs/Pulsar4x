# Civilian construction: the colony head

Status: plan, not started. Written 2026-11-04 for Grok Build. Do not build a slice until Jon says go.

Fourth sub-plan in the [civilian-administrators](civilian-administrators.md) plan of plans, after [civilian-recruitment](civilian-recruitment.md), [civilian-industry](civilian-industry.md), and [civilian-market](civilian-market.md). It gives the colony head (a seat only until now) its first job: construction.

## What exists today

- **The construction queue.** `LocalConstructionDB` holds a colony's (or ship's) construction queue. `LocalConstructionProcessor` spends the actor's installation points on the head of the queue: a colony mounts on itself, a ship mounts on a same-faction colony in cargo-transfer range. Every remaining input must already be in the warehouse; a short input spends nothing and the job waits at the head.
- **What it builds.** Mounting crates and assembling site-only buildings (the cargo-spending kind). Ship construction and refits are industry (production lines), not this.
- **Orders.** `AddToConstructionQueueOrder`, `RemoveFromConstructionQueueOrder`, `MoveUpInConstructionQueueOrder`, `MoveDownInConstructionQueueOrder` (and `QueueShipRefitOrder` for ships). The player drives all of it; nothing queues construction automatically.

## Decision

**Decided (Jon, 2026-11-04):** construction is the colony head's work. The colony head gets the inputs by sending requests to the other department heads:

- to the **industry head**: make the crates or parts the build needs;
- to the **market head**: reserve the materials the colony already holds, or buy what it is short of (which uses the existing bid and haul path).

The colony head does not run production lines or set prices itself. It uses the same request path the market plan defines, extended so the industry head can receive requests too.

With the colony head seat empty, construction stays exactly as today: the player queues it by hand.

**Decided (Jon, 2026-11-05): the player gives a build list, not a queue.** Instead of queuing a run of facilities by hand, the player gives the colony head a list of facilities (components) to build, something like ship design but simpler: pick facilities and amounts. The colony head hands the work out to industry and the market, and queues each facility for construction once its inputs are in the warehouse.

**Decided (Jon, 2026-11-05): ships stay with the player and come first.** The ship construction queue is the player's alone; the colony head never adds, removes, or reorders ship jobs. Ship construction takes priority over the colony head's facility builds: when both need the same input or installation points, the ship job wins.

## What the colony head does

1. **Read the build list.** The player's list of facilities and amounts for the colony. The colony head works down it.
2. **Request inputs.** For each listed facility, compares its inputs with the warehouse. For crates or parts the colony can make, sends a build request to industry. For raw materials, sends a reserve request (held stock) or a buy request (short stock) to market. Inputs a player ship job needs are left for the ship job.
3. **Queue when ready.** Once a facility's inputs are in the warehouse, adds it to the construction queue with the existing orders. Ship jobs keep priority over these. A job the player queued by hand is never removed or reordered.
4. **Wait and log.** A facility waiting on inputs logs who it is waiting on: `Colony (Ito) - Admin Complex waiting on 4 crates from Industry.`
5. **Decide what to build (later).** Adding to the build list by itself (by the growth slider, by population support) is a later slice, if at all.

## Requests to industry

The market plan's request list gets a second receiver. An industry request names the good (a crate or part design), the amount, and who asked. The industry head treats it like a feeder job: it queues the job on the fastest line, as `AutoAddSubJobs` does today. With no industry head seated, the request waits, and the colony head logs "waiting on an industry head".

## Slices

Each slice is one PR, tests first. Run tests from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor`, filtering `Pulsar4X.Tests`.

1. **Colony head goal.** The colony head seat gets a standing goal with its own wake (from the next construction job finishing, plus the weekly backstop). It does nothing yet beyond waking. Tests: seating a colony head adds the goal; unseating removes it; an empty seat leaves construction as today.
2. **Build list.** A per-colony list of facilities and amounts the player sets (new DB plus orders to add, remove, and change amounts), saved with the colony, plus a simple UI. Tests: orders edit the list; save and load keeps it; an empty seat ignores the list.
3. **Requests to industry.** Extend the market plan's request list so industry can receive requests; the industry head queues a job for each. Tests: an industry request for crates queues a job on the fastest line; with no industry head the request stays pending; save and load keeps it.
4. **Request inputs and queue.** The colony head works down the build list, sends industry and market requests for missing inputs, and queues a facility once its inputs are in. Tests: a listed building short of crates sends one industry request; short of a raw material it sends a market buy request; held material gets a reserve request; no duplicate requests on the next wake; a ready facility gets queued; a player-queued job is never touched.
5. **Ship priority.** Ship jobs win clashes with the colony head's facility builds, for inputs and installation points. Tests: a ship job and a listed facility needing the same crates, the ship gets them; the colony head never edits the ship queue.
6. **Choosing what to build (later, maybe).** The colony head adds to the build list itself. Needs the Direction tab plan (growth slider).

## Open questions for Jon

1. Decided: the player gives a build list (2026-11-05). Still open: whether the colony head ever adds to the list itself (slice 6).
2. Should the colony head build the list in order, or start whichever facility has its inputs ready first?
3. Should requests from the colony head outrank requests from other heads, or go through the same slider-weight clash rule? (The colony head has no slider today.) Ship needs already outrank it.
4. Decided: ship construction stays with the player and takes priority (2026-11-05).
5. How does ship priority work in practice: do ship jobs reserve their inputs as soon as they're queued, or only win when both are ready to build?
