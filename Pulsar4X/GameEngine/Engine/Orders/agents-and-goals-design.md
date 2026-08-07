# Agents, Goals and Actions

Status: **working vertical slice** for `MoveTo` and `ServeyBodies` (leaf + fleet survey dispatch).
Hierarchy tiers, autonomous weighting, and `GoalType` as an enum are still placeholders. This
records load-bearing decisions and the current pipeline.

## Purpose

1. **Intent over micromanagement** — player/AI states a goal; the unit plans actions.
2. **One command surface** — humans and AI use the same goals, gates, and queue. No separate AI API.
3. **Command hierarchy has mechanical cost** — seats, rank, and relay delay matter.

## Pipeline

```
UI / AI command
  → EngineGameServer (auth: faction owns commanded unit)
  → CommandTranslator (DTO → Goal { Type, TargetEntityID }; optional cheap checks)
  → AgentProcessor.AssignGoal → GoalsDB.GivenGoal
  → AgentProcessor (thin loop)
       resolve host (unit or CommanderDB.AssignedTo)
       fleet?  planner.PlanSubGoals → AssignGoal(child, RelayDelay)
       ship?   planner.PlanActions  → OrderHandler.HandleOrder
  → ActionQueueDB
  → ActionQueueProcessor (lanes, Execute, Status)
  → feature processors (WarpMove, NewtonSimple, GeoSurvey, …)
  → action Succeeded/Failed → RunAgentNow → prune / ClearFor / rollup
```

- Actions link to goals via `ParentGoalId` (`ActionsFor` / `ClearFor`).
- Leaf / player inject: `RunAgentNow` (no delay). Echelon hand-down: `ScheduleAgent` + `RelayDelay`.
- `RecheckInterval` is a safety net while Active; preferred wake is action/sub-goal completion.

### Agent vs planner

| **AgentProcessor** | **IGoalPlanner** (feature folder) |
|---|---|
| Host binding, schedule/wake | Domain feasibility and work product |
| `AssignGoal` / `HandleOrder` | `PlanActions` (ship) / `PlanSubGoals` (fleet) |
| Pending → Active; rollup Complete/Fail | Plan-time Failed/Completed + `Message` |
| One planner per `GoalType` (dictionary) | Stateless: re-read world + child goals each call |

Planners are **not** subclasses of the agent. The agent is a thin loop; feature behaviour lives in
planners. Prefer **re-calling `Plan*` while Active** (idempotent: skip busy units, only emit new work)
over a separate “top-up” API — same Decide step, OODA-style re-entry.

```csharp
interface IGoalPlanner
{
    GoalType Type { get; }
    IEnumerable<EntityAction> PlanActions(Goal goal, Entity ship);
    IEnumerable<(Entity subordinate, Goal goal)> PlanSubGoals(Goal goal, Entity fleet);
}
```

Register via **reflection** (concrete, parameterless ctor, implement `IGoalPlanner`). **One planner
instance per `GoalType`** — fleet and leaf roles share a class; duplicate types must throw, not
overwrite. Default empty `PlanActions` / `PlanSubGoals` (or no-op base) so a move-only planner need
not implement fleet.

### Goal status ownership

| Writer | When |
|---|---|
| **Planner** | Plan-time terminal: impossible, nothing to do |
| **Agent** | `Pending` → `Active` after a plan that left status Pending; rollup `Completed` / `Failed` from actions or sub-goals |
| **Everyone else** | Read only (`Message`, UI, parent `SubGoalsOf`) |

Actions never write `GoalsDB.GivenGoal`. Non-terminal waits (warp charging) stay action **`Queued`**
with `Details` — not goal `Failed`.

### Validation layers

| Layer | Checks |
|---|---|
| **Server / translator** | Auth: owns **commanded** unit. Secondary ownership only if the target must be yours. Goals may pass **ids through** without resolving; existence can be a cheap Reject for UX. |
| **Planner** | `TargetEntityID` → entity; domain (ability, surveyable, `CanMove`, parceling). |
| **Action** | Resolve entities needed to execute; **no** faction-ownership gate on the commanded unit (auth already did that). |

`Goal` stores **ids**, not live `Entity` refs (serialize, survive despawn, re-resolve each plan).

### Action status

| `ActionStatus` | Meaning |
|---|---|
| Queued | Accepted, not started (includes charge-wait) |
| Running | In progress (e.g. bubble up / transit) |
| Succeeded / Failed | Terminal |

`IsRunning` / `IsFinished()` are **legacy** — dual-write with `Status` until call sites migrate (~69).
Do not add extra enum values for charge-wait.

### Movement / survey notes

- `MovePlanner.TryBuildMoveActions` — pure; does not mutate goal status.
- Warp: `CreateWarpOnly` + planner-emitted circularise (`NewtonSimpleAction`). Arrival parks from exit
  state when no planned capture orbit; legacy `CreateCommandEZ` still circularises for old callers.
- Fleet survey: greedy nearest POI → free capable ship; one body per sub-goal. Leftovers need Active
  re-plan (re-call `PlanSubGoals`), not planner-local memory.

## Decisions (load-bearing)

1. **Tier = span of command**, not planner sophistication. `(GoalType, does this unit decompose?)`.
2. **One engine-side `CanAccept` at injection**, walk **up** the tree. Client is not a gate.
3. **Officers plentiful; skill never a gate** — quality only (replan, relay, recovery).
4. **AI: free admin tech, not free seats** — still build and crew.
5. **Relay costs game time per hop; leaf plan does not.** `RunAgentNow` at inject; `RelayDelay` on
   sub-goal assign.
6. **Feature folders + `IGoalPlanner` as the extension point**; auto-discover; agent has no feature
   switches. `GoalType` enum is central for now (likely string id later for mods).
7. **Reuse `GameEngine/People/` admin infrastructure.**

## Adding a capability

| Piece | Where |
|---|---|
| Ability / Action / Processor | Feature folder (processors already auto-discovered) |
| `IGoalPlanner` | Feature folder (reflection-registered; **one** class per `GoalType`) |
| `GoalType` (+ `BaseWeights` if autonomous) | `GoalsDB.cs` |
| API DTO + `CommandTranslator` entry | Central by design (auth boundary) |

## TODO

**Done**
- [x] Goal-linked action completion wakes agent; prune Succeeded; `ClearFor` on Failed
- [x] Planners in feature folders; pure warp + planner circularise; charge-wait via Queued + Details
- [x] Single `IGoalPlanner` interface; reflection registration
- [x] NavSequence removed

**Pipeline**
- [ ] Ensure Pending sets `Active` after a successful plan (planner left status Pending)
- [ ] Fleet vs ship: `else if` (flagships must not run both PlanSubGoals and PlanActions)
- [ ] Active re-call `PlanSubGoals` for survey (and similar) so leftover POIs get assigned
- [ ] `CanPerform` on planner; retire `PruneImpossibleGoals` switch
- [ ] Transition-only wake on Failed actions (no re-wake every pass)
- [ ] Migrate off `IsRunning` → `ActionStatus` only
- [ ] `CanAccept` (decision 2) + real `RelayDelay` formula (decision 5)
- [ ] Project `GivenGoal` + `Message` on fleet/ship snapshots
- [ ] Wire or quarantine unused weighting helpers

**Movement / survey**
- [ ] NewtonComplex vs Simple (SOI transitions); interplanetary; eccentric orbits
- [ ] Fail NewtonSimple when Δv short (don’t spin forever)
- [ ] Retire `CreateCommandEZ` circularise when legacy callers move
- [ ] Mode selection by goal preference (fuel vs time), not fixed 3× ETA

**Other**
- [ ] Admin uninstall NotImplemented; move visibility parity with warp
- [ ] `GoalType` enum vs string id; time-source / submit-thread re-entrancy (shared with HandleOrder)
