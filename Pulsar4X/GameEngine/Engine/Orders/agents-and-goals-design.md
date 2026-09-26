# Agents, Goals and Actions

Status: **working vertical slice** for `MoveTo`, `ServeyBodies`, `ScanAnomalies`, and `RefuelAt`
(leaf + fleet). Agent applies `PlanResult`; planners do not mutate goals. Orient
(`GoalWeighting`) runs each agent pass; autonomous pick / interrupt-resume not fully wired.
Commander Command/Nav/Survey XP is quality on relay, recheck, and plan/action *requests*; processors stay physics.

## Purpose

1. **Intent over micromanagement** — player/AI states a goal; the unit plans actions.
2. **One command surface** — humans and AI use the same goals, gates, and queue.
3. **Command hierarchy has mechanical cost** — seats, rank, and relay delay matter.

## Pipeline

```
UI / AI command
  → EngineGameServer (auth: owns commanded unit)
  → CommandTranslator (DTO → Goal { Type, TargetEntityID })
  → AgentProcessor.AssignGoal → GoalsDB.GivenGoal + ActiveGoal (Planning)
  → AgentProcessor wake
       Orient: GoalWeighting.Recalculate (context, e.g. fuel → RefuelAt)
       Act on ActiveGoal:
         Planning → planner.Plan(unit, goal) → PlanResult
                    agent applies Failed/Completed or enqueues Actions / AssignGoal SubGoals
                    → Active + ScheduleAgent(RecheckInterval)
         Active   → fleet: re-Plan (top-up) + SubGoalsOf rollup
                    ship: prune Succeeded / ClearFor Failed / complete when empty
  → ActionQueueProcessor → feature processors
  → action terminal → RunAgentNow (leaf, no RelayDelay)
```

- Work links via `ParentGoalId` (`ActionsFor` / `ClearFor` / `SubGoalsOf`).
- Player inject: `RunAgentNow`. Echelon hand-down: `ScheduleAgent` + `CommanderSkills.RelayDelay` (Command skill).
- `RecheckInterval` (~30 min × Nav/Command quality) is the slow re-eval tick; action completion is the fast path.

### GivenGoal vs ActiveGoal

| Field | Meaning |
|---|---|
| **`GivenGoal`** | Ordered intent (player / superior). Sticky; weighting should not overwrite. |
| **`ActiveGoal`** | What the agent plans and rollups **now**. |

`AssignGoal` currently sets both to the same instance. Interrupt later: change only `ActiveGoal`
(e.g. `RefuelAt`), keep `GivenGoal`, restore Active when interrupt completes.

### Agent vs planner

| **AgentProcessor** | **IGoalPlanner** |
|---|---|
| Host bind, schedule/wake, Orient | Domain feasibility + work product |
| Apply `PlanResult` (status, actions, sub-goals) | `Plan(unit, goal) → PlanResult` only — **no** goal mutation |
| Rollup Active from queue / children | Stateless; re-read world + child goals each call |

```csharp
interface IGoalPlanner
{
    GoalType Type { get; }
    PlanResult Plan(Entity managedEntity, Goal goal);
}

// Continue → Status.Active + Actions and/or SubGoals
// Done / Fail → terminal; agent copies Status + Message onto the goal
```

Reflection registration: one concrete planner per `GoalType` (fleet + leaf in the same class).
Duplicates throw. Planner branches on unit shape (`FleetDB` vs `ShipInfoDB`); agent does not.

### Goal roles (weighting)

| Role | Examples | Behaviour |
|---|---|---|
| **Drive** | `StayAlive`, `DontRunOutOfFuel` | Context only; not planned as missions |
| **Task** | `MoveTo`, `ServeyBodies`, `RefuelAt` | Has planner; can be `ActiveGoal` |
| **Stance** | `HelpOwn`, `Stealth` | Soft bias on other goals |

`DontRunOutOfFuel` raises **`RefuelAt`** context via `FuelSituation` (tank fraction × Caution).
Tactical “prepend refuel into a move” stays in MovePlanner; strategic choice uses weights.

### Status ownership

| Writer | When |
|---|---|
| **Agent** only | Applies `PlanResult` to `ActiveGoal`; rollup Complete/Fail |
| **Planner** | Returns status in `PlanResult` — does not write the goal |
| **UI / others** | Read `Status`, `Message`, action `Details` |

Non-terminal waits (warp charge) stay action **`Queued`** + `Details`, not goal Failed.

### Validation

| Layer | Checks |
|---|---|
| Server / translator | Owns **commanded** unit; optional cheap Reject |
| Planner | Id → entity; domain (ability, `CanMove`, parceling) |
| Action | Resolve refs to execute; no re-auth of ownership |

Goals store **ids**, not live `Entity` refs.

### Commander skill (quality, not a gate)

XP lives on the **person** (`CommanderDB.Skills`), not the hull and not `AgentDB` (personality stays there). No commander → quality **1.0** (today’s numbers). Skill **never Fails a Plan** and never changes intercept / burn / orbit math in processors.

| Domain | Leaf grant | Fleet grant |
|---|---|---|
| **Nav** | `MoveTo` complete | — |
| **Survey** | `ServeyBodies` / `ScanAnomalies` complete | — |
| **Command** | — | fleet rollup of those types complete |

`SkillDomains.DomainOf(type, isFleet)` is the only map (same idea as `GoalRoles.RoleOf`). Adding a goal type is one line there, not an `if` in every planner.

**Award:** `AgentProcessor` already writes `ActiveGoal` Completed; it then calls `CommanderSkills.GrantForCompleted`. Observe only — does not change status or plan.

**Consume — allowed**

| Layer | Effect |
|---|---|
| **AgentProcessor** | `RelayDelay` shorter with Command; `RecheckInterval` longer with Nav/Command |
| **Plans / actions** | What we *ask* physics to do. Nav: warp exit offset (`PlannedWarpExitOffset`) — 90° to departure v at quality 1; along-track lead (`v_circ · t_burn`) as quality → 2 so a finite circularise finishes nearer circular |
| **Survey processors** | Kit **Survey Speed** (module points/tick, not ship velocity) × Survey quality. Geo/grav ticks *are* the work, unlike movement. |

**Consume — forbidden in movement physics**

`NewtonSimpleProcessor`, `WarpMoveProcessor`, intercept, Kepler interpolation stay pure physics. They execute the offset / target Kepler they are given. No fuel × skill, no fudged eccentricity, no skill checks inside the burn.

Do not put skill inside `IGoalPlanner.Plan` feasibility (no “Nav too low → Fail”). Weighting / autonomous pick is still unwired; do not add skill bias there until Decide exists.

Quality is `1 + skill/cap` (cap default 100). Missing commander = 1.0 so existing tests stay on the old 90° exit and 1 s / 30 min timings.

### Command span (the gate)

Skill does not decide **how wide** an order is. Span comes from the **flagship / subfleet-leader command bridge** (`AdminSpaceDB` max `AdminLevel`). Missing bridge → **Body**.

| Span | Seats | Fan-out of `TargetEntityID` |
|---|---|---|
| **Body** | `Ship` … `Fleet`, or none | root only |
| **Well** | `Colony` / `Planet` / `SOI` | root + direct `PositionDB.Children` |
| **System** | `System`+ | all matching POIs in that star system |

`MoveTo` does not fan out. Geo/grav `PlanSubGoals` call `CommandSpan.Expand`. Leaf `PlanActions` stays one body. Processors unchanged.

Scenario hulls mount `ship-command` designs (`default-design-bridge-ship` / `-well` / `-system`). `CreateShip` installs `AdminSpaceAtb` → `AdminSpaceDB`.

## Decisions (load-bearing)

1. **Tier = span of command**, not planner IQ. `(GoalType, does this unit decompose?)`.
2. **One engine-side `CanAccept` at inject**, walk up the tree. Client is not a gate.
3. **Officers plentiful; skill never a gate** — quality only (replan, relay, recovery, plan/action *requests*). Processors are physics.
4. **AI: free admin tech, not free seats.**
5. **Relay costs game time per hop; leaf plan does not.** Hop time scales with Command skill.
6. **`IGoalPlanner` + reflection**; agent has no feature switches. `GoalType` enum for now (string id later).
7. **Reuse `GameEngine/People/` admin infrastructure** (`CommanderDB` + `CommanderSkills`).

## Adding a capability

| Piece | Where |
|---|---|
| Ability / Action / Processor | Feature folder (processor = physics only) |
| `IGoalPlanner` (`Plan` → `PlanResult`) | Feature folder; one class per `GoalType` |
| `GoalType` (+ `BaseWeights` / role if autonomous) | `GoalsDB` / `GoalRoles` |
| `SkillDomains.DomainOf` line | if the type should grant XP |
| Command span (`AdminLevel` on flagship bridge) | `CommandSpan.Expand` in fleet planners |
| API DTO + translator | Central (auth boundary) |

## TODO

**Done**
- [x] Action completion wakes agent; prune Succeeded; `ClearFor` on Failed
- [x] `PlanResult`; agent-only goal status writes
- [x] Single `Plan(unit, goal)`; Move / Geo / Grav / Refuel planners converted
- [x] Reflection planner registry; fleet Active re-Plan (top-up)
- [x] `GivenGoal` + `ActiveGoal` fields; Orient each pass (`GoalWeighting` + fuel context)
- [x] Warp charge-wait via Queued + Details; pure move builder + planner circularise path
- [x] Commander Command/Nav/Survey XP; Grant on goal Complete; RelayDelay / RecheckInterval
- [x] Nav warp-exit along-track lead (`PlannedWarpExitOffset`); processors untouched

**Next (priority)**
1. **Playtest issued goals** — MoveTo, survey body/system, ScanAnomalies, RefuelAt (depot + fleet tanker)
2. **Translator feedback** — after sync `AssignGoal`, `Reject` if goal already Failed; else `Ok(goal.Id)`
3. **UI** — project `ActiveGoal` / `GivenGoal` + `Message` + action `Details` on snapshots
4. **Autonomous Decide** — when `ActiveGoal` null/terminal, `PickAutonomousTask` (start with RefuelAt + source id)
5. **Interrupt resume** — if `ShouldInterruptForRefuel`, set Active=RefuelAt, keep Given; on Refuel Done restore Given as Active
6. **`GoalsDB.Clone`** — copy `GivenGoal` as well as `ActiveGoal`

**Pipeline**
- [ ] `CanPerform` on planner; retire `PruneImpossibleGoals` switch
- [ ] Fleet fail cancels sibling sub-goals + their actions
- [ ] Transition-only wake on Failed (no spam while Failed sits)
- [ ] Migrate off `IsRunning` → `ActionStatus` only
- [ ] Real `CanAccept`; staff-size term in `RelayDelay` (skill term is in)
- [x] Survey skill scales kit Survey Speed in geo/grav processors (`SurveyRate`); movement processors stay physics-only
- [ ] Weighting skill bias only when autonomous Decide is wired
- [ ] Colonies / sector orgs on same `Plan` path (decompose vs leaf helpers)

**Movement / logistics**
- [ ] MovePlanner optional prepend refuel when fixed depot + short Δv (tactical; not weights)
- [ ] NewtonComplex vs Simple; fail NewtonSimple when Δv short
- [ ] Retire `CreateCommandEZ` circularise for legacy callers
- [ ] Mode selection by preference (fuel vs time)

**Other**
- [ ] `GoalType` enum vs string id; submit-thread vs engine-pulse (parity with HandleOrder)
- [ ] Admin uninstall NotImplemented; move visibility parity with warp
