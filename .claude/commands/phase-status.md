# Phase Status — Current Work Review

Review what phase of PLAN.md is active and what progress has been made. Use at the start of a new session or when deciding what to work on next.

## Steps

1. Read `PLAN.md` — identify the current active phase and list its tasks.
2. Run `git log --oneline -10` to see recent work.
3. For each task in the current phase, read the relevant source file to confirm whether it is actually done — do not rely on memory or prior session summaries.
4. Report status below.

## Key Files

| What | Where |
|------|-------|
| Plan | `PLAN.md` |
| Root reference | `CLAUDE.md` |
| Architecture | `ARCHITECTURE.md` |
| Engine core | `Pulsar4X/GameEngine/CLAUDE.md` |
| Space combat | `Pulsar4X/GameEngine/Weapons/CLAUDE.md` |
| Damage | `Pulsar4X/GameEngine/Damage/CLAUDE.md` |
| Colonies | `Pulsar4X/GameEngine/Colonies/CLAUDE.md` |
| Industry | `Pulsar4X/GameEngine/Industry/CLAUDE.md` |
| Movement | `Pulsar4X/GameEngine/Movement/CLAUDE.md` |
| UI | `Pulsar4X/Pulsar4X.Client/CLAUDE.md` |

## Report Format

```
PHASE STATUS
============
Active Phase: Phase N — [Name]

DONE:
  ✓ [task description] — confirmed at [file:line]

IN PROGRESS:
  ~ [task description]
      Done: [what's complete]
      Remaining: [what's left, exact file and function]

NOT STARTED:
  ✗ [task description] — [file to edit when ready]

BLOCKED:
  ! [task description] — blocked by: [dependency]

NEXT ACTION:
  [Single most important next step with exact file path and function name]
```

## Rules

- Do not mark a task done without reading the source file to confirm.
- If PLAN.md is out of date with what was found in source, update PLAN.md.
- If a phase is complete, update the status marker in PLAN.md and identify what Phase N+1 requires.
