# Agents, Goals and Actions — Design Intent

Status: **early / exploratory.** A vertical slice exists for `MoveTo` only. `GoalType`, the `AgentDB`
personality floats, `GoalsDB` weighting, and the commander/administrator entities in
`GameEngine/People/` are all **placeholder**. `GoalType` in particular is due for re-examination
(see decision 6). This document records *what we are trying to achieve* and *which decisions are
load-bearing*, so the reasoning survives the placeholders being replaced.

## What this system is for

The player currently issues fully-specified orders — plot this warp, burn this ΔV. Fine for a
handful of ships, miserable for a hundred. Goals let the player state **intent** instead and have
the entity work out the actions.

1. **Reduce micromanagement without removing agency.** Delegation is earned and chosen, not applied
   behind the player's back.
2. **One command surface for humans and AI.** AI factions drive the game through the same goals,
   gates and infrastructure. There is no separate "AI API". This is the tightest constraint here.
3. **Make the command hierarchy matter mechanically.** Officers, bridges and sector offices should
   *do* something, not be paperwork.

## The three layers

```
Goal          intent, possibly open-ended and persistent      GoalsDB.GivenGoal
  │  IGoalPlanner.Plan()
  ▼
EntityAction  concrete, queued, individually executable       ActionQueueDB
  │  OrderHandler.HandleOrder() — validates, enqueues, schedules
  ▼
execution     the existing order/processor machinery, unchanged
```

Actions carry `ParentGoalId`, so `ActionQueueDB.ActionsFor(goal)` / `ClearFor(goal)` let an agent
monitor and abandon its own work.

`AgentProcessor.ProcessEntity` branches on what the unit *is*, and that branch is the backbone:

- **`FleetDB`** → `GoalsProcessor`: decompose into sub-goals for each capable child; each child is
  itself an agent that plans on its own wake.
- **`ShipInfoDB`** → `ActionsProcessor`: run the planner, submit actions, monitor to completion.

An agent may run on the unit itself or on a commander assigned to it (`CommanderDB.AssignedTo`).

### Current flow:
- UI orders MoveToEntity
- InProcessAdapter.SubmitCommandAsynch
- EngineGameServer.SubmitCommand
- CommandTranslater.Translate
- MovementGoalsAndActions.AssignMoveTo Creates Goal
- static AgentProcessor.RunAgentNow(fleetEntity)
- AgentProcessor.ProcessEntity checks if entity is an agent or gets assigned agent from entity.
- 
- 

## Decisions

### 1. Tiers are gated by span of command, not planner sophistication

A fighter deciding on its own when to break off and rearm is maximally open-ended *and* the most
basic thing a pilot does. A tramp freighter picks its own routes forever under a junior captain.
**Self-directed does not mean senior.** The axis is: *how many other units does this goal commit?*

| Tier | Requires | Goals |
|---|---|---|
| **0** | nothing | raw `EntityAction`s — every burn plotted by the player/AI |
| **Ship** | occupied `AdminLevel.Ship` seat, **any rank** | anything scoped to one's own hull: `StayAlive`, `DontRunOutOfFuel`, `RefuelAt`, `RearmAt`, `RepairAt`, `MoveTo`, `Trade`, `MakeProfit`, `Freighter`, `Mine`, `Scout`, `ListeningPost`, `ScanBodies`, single-ship `Patrol` |
| **TaskUnit → Fleet** | seat at the echelon, rank ≥ seat | the *same* goals given to a **fleet** that must decompose them, plus inherently multi-unit ones (`Blockade`, area `Defend`) |
| **Sector / Empire** | planet office + tech | standing policy — the agent *selects* goals rather than receiving them |

Little is locked at ship level; the meaningful step is captain vs no captain, and per decision 3
that is cheap to recover from.

**`GoalType` alone does not determine the tier** — `(GoalType, does this entity decompose?)` does.
`Patrol` on one destroyer is a captain's job; on a task group it needs a staff. Make it a function,
not a static table.

### 2. One engine-side gate predicate, checked where the goal is injected

`CanAccept(unit, goalType, out reason)`: required tier → find commanding seat → seat level → seat
occupied → rank ≥ seat → tech → physical capability.

- **Not in the client.** The AI hits the identical check (decision 2 above); a client-side gate is
  not a gate.
- **Walk *up* the command tree.** A destroyer with a plain bridge, in a fleet whose flagship has a
  Fleet-level CIC with an admiral aboard, *should* accept a fleet-tier goal. `GoalsProcessor` hands
  sub-goals to children that would each individually fail a fleet-tier check. Getting this wrong
  breaks fleets outright.

It also gives the API boundary a real synchronous verdict — goal assignment is otherwise async, so
`CommandTranslator` has nothing to report. "No officer aboard capable of independent navigation"
beats a generic rejection.

### 3. Officers are plentiful; skill is a quality axis, never a gate

Low-tier officers should be abundant, including battlefield commissions from crew. Losing a captain
is a setback, not a bricked ship. Mechanically a field commission is a `CommanderDB` at `Rank` 0–1
with a low `ExperienceCap` — a warm body who plateaus early; `NavalAcademyProcessor` already rolls
that cap on a bell curve.

**Never gate goals on skill.** A hard skill gate can't be planned around, and since captaincy is
abundant, gating on skill just re-creates the scarcity we removed. Skill affects *how well* a goal
runs: replan cadence (`RecheckInterval`), plan quality, likelihood of sliding to `Failing` instead
of recovering, and order relay time (decision 5).

### 4. AI factions get free tech, not free seats

Give AI factions administration tech at start so they need no research climb — but they still
**build and crew** the components. Skipping the physical layer would hand them a mechanical edge,
make flagship losses free, and stop them exercising the code paths humans hit.

Symmetry buys a real property: **an AI faction is itself an agent in an Empire-level seat.** The
infrastructure that lets a human delegate is what lets an AI think at scale. A primitive AI empire
is forced to micromanage; a developed one becomes strategic — emergent, no difficulty knob. A strike
on an AI's sector office genuinely degrades its decision-making.

### 5. Order propagation costs game time, and that time is a skill sink

Handing a goal down an echelon takes time (`GoalsProcessor` currently uses a placeholder `+1s`).
Because the cost is **per hop**, it composes with hierarchy depth for free: Empire → Sector → Fleet →
TaskGroup → Ship pays four relays; a lone captain pays none. Deep hierarchies delegate more but
respond slower — a genuine structural tradeoff with no new stat. Leaf ships have zero hops, so the
Tier-Ship goals in decision 1 stay instant, which is correct.

Delay should scale with commander experience, **`AdminSpaceAtb.ConsoleSpace`** (bigger staff relays
faster — and this gives `ConsoleSpace` a job; it is currently computed and discarded), and
subordinate count. Keep it distinct from `RecheckInterval`: propagation is "when do my subordinates
know", recheck is "how often do I notice things changed".

**This makes goal projection a prerequisite, not polish.** "Queue empty because it's still relaying"
and "queue empty because it's broken" are the same observable state. Acceptance must make the *goal*
visible — `Pending`, with an ETA — not the actions. Consequently `RunAgentNow` should record and
acknowledge the goal immediately but **not** force the whole tree to plan synchronously; propagation
takes game time by design. Magnitude matters: it should bite on reactive decisions (intercepts,
retargeting) and be irrelevant for routine ones. Hours of staff work to say "go to Mars" reads as
punishment.

### 6. Feature code lives in feature folders; `IGoalPlanner` is the one extension point

The codebase already does this everywhere else. Every `EntityAction` lives with its feature
(`Movement/WarpMove/WarpMoveAction.cs`, `GeoSurveys/GeoSurveyOrder.cs`, `Tech/Orders/*`); only the
abstract base is here. All 30 processors live with their feature and are **auto-discovered by
reflection** in `ProcessManager.CreateProcessors`, so adding one touches nothing central.

The goal layer is the outlier — **five places in `AgentProcessor` know about movement**: the
`_planners` array, `AssignMoveTo`, `GoalsProcessor`'s hardcoded `!= GoalType.MoveTo`, the
`PruneImpossibleGoals` switch (which reaches into `SensorAbilityDB`, `GeoSurveyAbilityDB`,
`JPSurveyAbilityDB`, `MiningDB`), and planner registration.

Fix: grow `IGoalPlanner` to own everything feature-specific, and auto-discover it exactly as
processors are.

```csharp
public interface IGoalPlanner
{
    GoalType Type { get; }
    IEnumerable<EntityAction> Plan(Goal goal, Entity ship);            // leaf: goal → actions
    bool CanPerform(Entity entity);                                     // replaces PruneImpossibleGoals arm
    AdminLevel RequiredSeat(Entity unit);                               // decision 1's tier
    IEnumerable<Entity> SelectSubordinates(Goal goal, Entity fleet);    // replaces the MoveTo hardcode
}
```

`AgentProcessor` then knows goals, seats, scheduling and rollup — and nothing about movement,
sensors or mining. Planners move to their feature folders (`MoveToPlan` → `Movement/`,
`ScanBodiesPlan` → `GeoSurveys/`), carrying their `AssignXxx` helper with them.

**Central by design vs central by accident.** The API DTO and the `CommandTranslator` entry are
deliberately central — that is the auth/validation boundary, and `docs/api-layer-design.md` states
the goal as "the command surface grows in one isolated place". Do not distribute those. Everything
else that is central today is an accident.

**`GoalType` is the one that won't distribute.** A C# enum can't be extended from a feature folder,
and can never be moddable — while the rest of the codebase already uses string ids for exactly this
(`tech-administration-level`, `admin-complex`, `general-storage`, `StandingOrderTypes`). A mod adding
a mining rig will want a mining goal. This needs re-examining soon: `GoalType` likely becomes a
string id and `BaseWeights` becomes data. Related wrinkle — `MoveTo`/`RefuelAt`/`RearmAt`/`RepairAt`
are in the enum but *not* in `BaseWeights`, so the enum is already implicitly split between
player-issued and autonomously-selected goals, by accident rather than design.

### 7. Build on the command infrastructure that already exists

`GameEngine/People/` has most of the physical layer, already moddable:

- `AdminSpaceAtb` / `AdminLevel` — `Ship, TaskUnit, TaskGroup, TaskForce, Fleet, Colony, Planet, SOI,
  System, Sector, Empire`. These are echelons of *how many units* — decision 1's axis; the enum was
  already telling us the answer.
- `AdminSpaceDB.CommanderSeats` — `(SeatType, CommanderID)`, occupied or empty.
- Ship component (`storage.json`, `ComponentType: "Admin"`) — Admin Level 0–5 + Console Space.
- Planet office (`installations.json`, `admin-complex`) — max `TechData('tech-administration-level')
  + 5`. **Tech already gates admin level here, in data.** Extend that pattern to the ship component
  rather than inventing a parallel mechanism.

## Adding a new capability

| What | Where | Central? |
|---|---|---|
| `XxxAbilityDB` + `XxxAtb` | feature folder | no |
| `XxxAction : EntityAction` | feature folder | no |
| `XxxProcessor` | feature folder | no — auto-discovered |
| `XxxPlan : IGoalPlanner` (+ its `AssignXxx` helper) | feature folder | no — *once decision 6 lands* |
| `GoalType` entry (+ `BaseWeights` only if autonomously selectable) | `GoalsDB.cs` | **yes** — see decision 6 |
| component template | `GameData/basemod/TemplateFiles/` | data |
| `XxxCommand` DTO | `Pulsar4X.Api/Commands.cs` | **yes, by design** |
| `_translators` entry + `TranslateXxx` | `CommandTranslator.cs` | **yes, by design** |
| view DTO + `GameProjector` case | `GameEngine/Api/` | if the client must see it |
| UI to issue it | `Pulsar4X.Client` | — |

## Load-bearing vs placeholder

Worth getting right now — expensive to retrofit:

1. **The gating axis** (span of command). Every planner keys off it.
2. **One predicate, engine-side, at goal injection, walking up the tree.**
3. **`IGoalPlanner` as the sole feature extension point** (decision 6).
4. **Whether a `Goal` knows its own scope.**

Churn freely: the goal→tier mapping, rank numbers, `RecheckInterval` and relay-delay values, the
`AgentDB` floats, `GoalsDB.BaseWeights`, whether skill affects replan rate or plan quality.

Do **not** harden `GoalType` — it currently does double duty as "kind of task" and "level of
abstraction", and `Trade`/`DontRunOutOfFuel` show those come apart.

## TODO

**Bugs**
- [ ] `AdminSpaceAtb.OnComponentUninstallation` throws `NotImplementedException` — uninstalling a
      bridge crashes, and `CommandTranslator.TranslateUninstallComponent` can reach it.
- [ ] `AdminSpaceProcessor.CalcEntityAdminSpace:33` accumulates `seats += atb.ConsoleSpace` and never
      uses it — one seat per component regardless. Needed by decision 5.
- [ ] `TranslateMoveToBody` has no visibility check on the target; `TranslateWarpMove` does
      (`IsEntityVisibleToFaction`). A faction can currently order a move to a body it hasn't seen.
- [ ] `NavSequenceProcessor` appears to `RemoveAt(0)` twice on the same sequence — one manoeuvre in
      every pair gets dropped.

**Refactor (decision 6)**
- [ ] Auto-discover `IGoalPlanner` by reflection, as `ProcessManager.CreateProcessors` does.
- [x] Move `MoveToPlan` → `Movement/`, `ScanBodiesPlan` → `GeoSurveys/`. *(`MoveToPlan` done —
      `Movement/MovementGoalsAndActions.cs`. `ScanBodiesPlan` still in `GoalProcessor.cs`.)*
- [x] Move `AssignMoveTo` out of `AgentProcessor` into `Movement/`.
- [ ] Replace the `PruneImpossibleGoals` switch with `IGoalPlanner.CanPerform`.
      `MovePlanner.CanMove` is the shape it wants; `GoalsProcessor` already calls it in place of
      the old hardcoded `HasDataBlob<WarpAbilityDB>` check.
- [ ] Replace `GoalsProcessor`'s hardcoded `!= GoalType.MoveTo` with `SelectSubordinates`.

**Movement maths**

These gate the `MovePlanner` decision tree (`Movement/MovementGoalsAndActions.cs`). Each one is
currently a "reject with a reason" branch rather than a wrong answer; the comments in
`EvaluateNewtonian` mark the exact spots.

- [ ] **Check and fix `NewtonComplex`.** It is the older of the two newtonian paths
      (`NewtonMoveDB` + `NewtonianMovementProcessor`, reached via `NewtonThrustAction`) and tries to
      fully integrate the thrust manoeuvre. The maths was never got right and the whole approach is
      probably overkill next to `NewtonSimple`, which derives fuel use from the *desired outcome*
      instead of simulating the burn. Decide: fix it, or fold what only it can do into
      `NewtonSimple`. It is the only path that currently handles SOI transitions
      (`NewtonianMovementProcessor:121-192`) — `NewtonSimpleProcessor` has none — so that capability
      has to land somewhere before newtonian moves can leave an SOI.
      `MovePlanner` deliberately emits `NewtonSimpleAction` only, until this is resolved.
- [ ] **Interplanetary transfer.** Escape burn → transfer about the grandparent → capture burn.
      `InterceptCalcs.InterPlanetaryHohmann` is an untested sketch with ad-hoc phasing and no SOI
      awareness; it is not wired up. Warp covers this case for now.
- [ ] **Orbital capture / insertion burn.** Arriving co-located with a body means falling into its
      SOI. Warp has an insertion vector (`OrbitProcessor.GetOrbitalInsertionVector`); the newtonian
      path has nothing, so "hop to the next moon over" currently still warps.
- [ ] **Eccentric orbits.** `Hohmann2`, `HohmannOE` and `OrbitPhasingManuvers` all assume circular.
      Wants a Lambert solver, or an explicit circularise-first leg.
- [ ] **Replan from an arbitrary state vector.** A ship mid-burn has no `OrbitDB`, so it can't plan a
      newtonian move at all until it coasts.
- [ ] Verify `OrbitalMath.OrbitPhasingManuvers`' sign convention for `phaseAngle` against both
      target-ahead and target-behind. `MovePlanner` passes `Atan2(target) - Atan2(ship)` on trust.
- [ ] `NewtonSimpleProcessor` silently no-ops forever when Δv is short of `TargetTrajectory`
      instead of failing the action. `MovePlanner` checks the budget up front, but a mid-flight
      mass change can still strand it.

**Dead code**
- [x] `Movement/MoveToSystemBodyOrder.cs` — deleted; superseded by `MoveToPlan`.
- [ ] `NewtonThrustAction.Thrust90ToTargetCmd` — its own comment says "never fully completed".
- [ ] `InterceptCalcs.FTLIntercept` — no callers.

**Feedback / projection**
- [ ] Project `GoalsDB.GivenGoal` (type, target, status, issued-at, effective-at) on
      `FleetSnapshot`/`ShipSnapshot` via `MessageTypes.OrdersChanged` → `FleetsChanged`, the channel
      order queues already use. **Prerequisite for decision 5.**
- [ ] Surface goal failure — a planner failing sets `Status`/`Message` and nothing reads it.
- [ ] Project available tier + reason per entity so the client greys out unavailable orders with a
      tooltip. Discovering rules by rejection message is miserable.
- [ ] Show the propagation front along `Goal.ParentGoalId` ("Fleet HQ ✓ → TG-3 ✓ → *Kestrel* pending
      02:14") — makes the cost of a deep chain of command visible.

**Design — open**
- [ ] **Wake the agent when its actions resolve, instead of only polling.** Goal rollup happens on
      `RecheckInterval` (30 min), so a goal is resolved up to that late and the latency **compounds
      per echelon** — ship finishes, up to 30 min for its own agent to mark `Completed`, up to 30 min
      more for the fleet to see it in `SubGoalsOf`. The failure path is worse than late: a `Failed`
      action deliberately keeps holding its `ActionLane` (see below), so the unit is *frozen* until
      the agent runs `ClearFor`. Fix is for `ActionQueueProcessor` to `ScheduleAgent` when an action
      of a goal-linked plan transitions to `Succeeded`/`Failed` — detect the *transition*, not the
      state, or the retention window turns the 30 min poll into a 10 min one. Schedule rather than
      `RunAgentNow`: re-entering the planner inside the queue's own pass has it mutate the
      `ActionList` being processed, and decision 5 says agent work costs game time anyway.
      Not urgent for correctness — the queue drives a multi-action plan forward on its own without
      the agent — but it is global to every goal type, and it's the hook mid-flight **replanning**
      wants later: `BuildActions` commits every `ActionOnDate` at plan time, so action boundaries are
      the natural place to re-evaluate. That needs a replan path in the `Active` branch plus the
      "replan from an arbitrary state vector" item above; don't build it until those land.
- [ ] **`Failed` actions hold their lane on purpose; `Succeeded` ones must not.** The lane mask in
      `ActionQueueProcessor` has no un-mask step — it is rebuilt each pass from whatever is in
      `ActionList`, which was self-cleaning while finished actions were removed unconditionally.
      Retaining goal-linked actions for rollup broke that, hence the `Status == Succeeded` skip at
      the top of the loop. `Failed` is excluded from that skip deliberately: unblocking the lane
      before the agent tears the plan down would fire burn 2 of a transfer whose burn 1 failed.
      Any future lane/queue work needs to preserve both halves of that.
- [ ] **Re-examine `GoalType`** (decision 6): string id vs enum, `BaseWeights` as data, and the
      implicit player-issued / autonomously-selected split.
- [ ] Implement `CanAccept` (decision 2) including the up-tree seat walk.
- [ ] Replace the placeholder `+1s` relay with the decision-5 formula.
- [ ] Wire the weighting layer — `PruneImpossibleGoals`/`RecalculateEffectiveGoals`/`EffectiveGoals`
      are an unused utility-AI sketch and the intended basis for the Sector/Empire tier.
- [ ] Planners beyond `MoveTo`. `ScanBodiesPlan` is registered but stubbed; nothing else exists.
- [ ] **Mode selection should be a utility comparison, not an ordering.** `MovePlanner.Select`
      currently prefers newtonian unless it is more than 3× the warp ETA. The right answer weighs
      (eta, Δv, energy) *by the owning goal* — a `Trade` run values fuel, an `Attack` values time,
      and a ship holding `DontRunOutOfFuel` should refuse an expensive option outright. That needs
      the goal layer to express a preference, which it currently can't.
- [ ] `MoveTargeting.TryResolve` reads a warping target's destination straight out of
      `WarpMovingDB`, regardless of whether we can see it. Should be gated on intel, and guessed
      from observed heading when we have none.
- [ ] Confirm the time source: `StandAloneOrderHandler:40` and the `Game.cs`/`DefaultStartFactory`
      call sites pass `Game.TimePulse.GameGlobalDateTime`, while the agent path uses
      `unit.StarSysDateTime`. Systems can diverge; actions may execute early relative to their own
      frame.
- [ ] Re-entrancy: `SubmitCommand` can run an agent while `ProcessSystem` is mid-pulse on another
      thread, mutating `GoalsDB`/`ActionQueueDB` concurrently. Pre-existing via
      `StandAloneOrderHandler`, but the goal path widens it. `ManagerSubPulse.IsProcessing` exists
      and is currently only read by the perf window.
