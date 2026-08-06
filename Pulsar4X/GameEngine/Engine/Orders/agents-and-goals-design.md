# Agents, Goals and Actions

Status: **working vertical slice** for `MoveTo` and `ServeyBodies`. Hierarchy tiers, weighting, and
`GoalType` as an enum are still placeholders. This records load-bearing decisions and current
pipeline shape.

## Purpose

1. **Intent over micromanagement** — player/AI states a goal; the unit plans actions.
2. **One command surface** — humans and AI use the same goals, gates, and queue. No separate AI API.
3. **Command hierarchy has mechanical cost** — seats, rank, and relay delay matter.

## Pipeline

```
UI / AI command
  → CommandTranslator → AssignXxx (creates Goal, AgentProcessor.AssignGoal)
  → GoalsDB.GivenGoal
  → AgentProcessor
       FleetDB  → GoalsProcessor  → IGoalToGoalsPlanner  → sub-goals + RelayDelay
       ShipInfo → ActionsProcessor → IGoalToActionsPlanner → actions via OrderHandler
  → ActionQueueDB
  → ActionQueueProcessor (lanes, Execute, Status)
  → feature processors (WarpMoveProcessor, NewtonSimpleProcessor, …)
  → on action Succeeded/Failed: RunAgentNow → agent prunes / ClearFor / rollup
```

- Actions link to goals via `ParentGoalId` (`ActionsFor` / `ClearFor`).
- Agent may run on the unit or on its assigned commander (`CommanderDB.AssignedTo`).
- Leaf work uses `RunAgentNow` (no delay). Handing a goal down an echelon uses `RelayDelay`
  (decision 5). `RecheckInterval` is a safety net only while work is still Active.

### Status model

| Layer | Values | Notes |
|---|---|---|
| `GoalStatus` | Pending → Active → Completed \| Failed | Planner may set Failed/Completed on Pending (e.g. already there) |
| `ActionStatus` | Queued, Running, Succeeded, Failed | Single source of truth going forward |
| `IsRunning` / `IsFinished()` | legacy | Still dual-written with `Status`; ~69 call sites — do not delete yet |

**Non-terminal waits** (e.g. warp bubble charging) stay **`Queued`** with a useful `Details` string.
Do not `Failed` the goal for “not enough energy this tick.” Fail only when impossible (no drive,
capacitor can never hold creation cost).

**Feedback:** action `Name` / `Details` for the player; goal `Message` only on real terminal fail.
There is no `EntityAction.Goal` shadow object.

### Movement composition

- `MovePlanner.TryBuildMoveActions` — pure; never mutates goal status.
- Warp path: `CreateWarpOnly` + optional `NewtonSimpleAction` circularise from the planner
  (`BuildWarpAndCircularise`). Warp arrival parks from exit position + `SavedNewtonionVector` when
  no planned capture orbit is set.
- Legacy `CreateCommandEZ` still bundles circularise for old callers; goals path does not use it.
- Composers (e.g. `ScanBodyPlan`) call `TryBuildMoveActions`, then append feature actions.

## Decisions (load-bearing)

1. **Tier = span of command**, not “how smart the planner is.” Ship-scoped goals need any occupied
   ship seat; multi-unit decomposition needs a seat at that echelon. `(GoalType, does this unit
   decompose?)` — not a static GoalType→tier table.

2. **One engine-side `CanAccept` at injection**, walking **up** the command tree. Client is not a
   gate. AI hits the same check.

3. **Officers are plentiful; skill is never a gate.** Skill affects quality (replan cadence, relay
   time, recovery), not whether a goal is allowed.

4. **AI gets free admin tech, not free seats.** They still build and crew bridges so losses hurt and
   code paths stay shared.

5. **Relay costs game time per hop; leaf planning does not.** `RelayDelay` on sub-goal assign;
   `RunAgentNow` for leaf actions and player/AI injection acknowledgment. Magnitude should bite on
   intercepts, not on “go to Mars.”

6. **Feature code in feature folders; planners are the extension point.** `AgentProcessor` should
   not hardcode movement/survey. Target shape:

   ```csharp
   interface IGoalPlanner {
       GoalType Type { get; }
       bool CanPerform(Entity entity);
       IEnumerable<EntityAction> PlanActions(Goal goal, Entity ship);
       IEnumerable<(Entity sub, Goal g)> PlanSubGoals(Goal goal, Entity fleet);
   }
   ```

   Auto-discover like processors. Today registration is still a static list in `AgentProcessor`.
   `GoalType` as a C# enum cannot be mod-extended — likely becomes a string id later.

7. **Reuse `GameEngine/People/` admin infrastructure** (`AdminSpaceAtb`, seats, tech-gated offices).

## Adding a capability

| Piece | Where |
|---|---|
| Ability DB / Atb / Action / Processor | feature folder (processor auto-discovered) |
| Planner + `AssignXxx` | feature folder (registration central until reflection) |
| `GoalType` (+ `BaseWeights` if autonomous) | `GoalsDB.cs` — central for now |
| API command DTO + `CommandTranslator` | central by design (auth boundary) |

## TODO

**Done**
- [x] Agent wakes on goal-linked action Succeeded/Failed; agent prunes Succeeded; `ClearFor` on Failed
- [x] `MoveToPlan` / `AssignMoveTo` in `Movement/`; `ScanBodyPlan` / `ScanSystemBodiesPlan` in GeoSurvey
- [x] Pure warp + planner circularise; charge-wait via `Queued` + `Details`
- [x] NavSequence removed

**Pipeline / structure**
- [ ] Unify `IGoalToActionsPlanner` / `IGoalToGoalsPlanner` → one `IGoalPlanner`; auto-discover
- [ ] `IGoalPlanner.CanPerform` replaces `PruneImpossibleGoals` switch
- [ ] Transition-only agent wake on Failed (avoid re-waking every pass while Failed sits)
- [ ] Migrate off `IsRunning` → `ActionStatus` only
- [ ] Implement `CanAccept` (decision 2) + real `RelayDelay` formula (decision 5)
- [ ] Project `GivenGoal` on fleet/ship snapshots; surface `goal.Message` on failure
- [ ] Wire or quarantine weighting helpers (`EffectiveGoals` unused by the concrete path)

**Movement**
- [ ] NewtonComplex vs NewtonSimple (SOI transitions only on Complex today)
- [ ] Interplanetary transfer; eccentric orbits; replan from arbitrary state vector
- [ ] `NewtonSimpleProcessor` should Fail the action when Δv is insufficient, not spin forever
- [ ] Retire `CreateCommandEZ` circularise once legacy callers move to the planner
- [ ] Mode selection as utility vs goal preference (fuel vs time), not a fixed 3× ETA rule

**Other**
- [ ] `AdminSpaceAtb.OnComponentUninstallation` NotImplementedException
- [ ] `TranslateMoveToBody` visibility check (parity with warp)
- [ ] Re-examine `GoalType` (enum vs string id; player-issued vs autonomous)
- [ ] Time source consistency (`GameGlobalDateTime` vs `StarSysDateTime`); submit vs pulse re-entrancy
