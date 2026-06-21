# Session State — Current Known Status

This document tracks what we know about the codebase state at the close of each session. A new session reads this first after the CLAUDE.md files, to skip rediscovery.

---

## Last Updated

Session ending ~2026-06-20. Branch: `claude/amazing-clarke-7s118n` (not yet merged to main — user intent confirmed, awaiting explicit instruction).

---

## Build Status

**UNKNOWN — .NET is not installed in the cloud execution environment, and CANNOT be installed under the current network policy.**

Cannot run `dotnet build` or `dotnet test` from Claude's remote session. The developer must pull the branch to their Windows machine and build there to establish a baseline.

**Verified 2026-06-21 — .NET SDK install is blocked by network egress allowlist.**
A self-install was attempted (`dotnet-install.sh`). The install script downloads fine (GitHub is allowlisted), but every Microsoft .NET binary host returns **HTTP 403 (egress-blocked)**:
- `builds.dotnet.microsoft.com` — 403 (SDK binaries)
- `ci.dot.net` — 403 (version feed + fallback binaries)
- `dotnetcli.azureedge.net`, `aka.ms` — 403

Allowlisted/reachable: `github.com`, `raw.githubusercontent.com`, `api.nuget.org`, `www.nuget.org`, `dot.net` (redirect only).

**To enable Claude to build/test itself**, the developer must add the dotnet hosts to the environment's network egress settings (see https://code.claude.com/docs/en/claude-code-on-the-web) — at minimum `builds.dotnet.microsoft.com` and `ci.dot.net`. Then a container setup script can `dotnet-install.sh --channel 8.0`. **Even then, running the GAME (UI) still needs the developer** — the container is headless (no display/GPU for SDL2+OpenGL). Do NOT re-test the network each session; this is the confirmed state until the policy changes.

**What we know from reading source (not compiling):**
- No code changes have been made — docs and infrastructure only.
- The original repo was a working fork; baseline source should compile.
- The branch is documentation and `.claude/` config only — zero C# changes.

**Action for next session's first code change:** developer builds locally first, captures pass/fail, adds result here.

---

## Test Baseline

**UNKNOWN** for the same reason — cannot run `dotnet test` remotely.

From reading the test project structure: tests exist for EntityManager, orbits, save/load, mining, population, pathfinding. **No tests for combat, damage, or ground combat.**

---

## What's Been Done This Session

All documentation. No code has been touched. These files were created or corrected:

| File | Status |
|------|--------|
| `CLAUDE.md` (root) | Updated — added plain-language rule, design references, corrected Gotcha #4 (InstallationsDB) |
| `CONVENTIONS.md` | New — 15-section idiom reference extracted from source |
| `PLAN.md` | Updated — corrected InstallationsDB references, added priority order, combat model table |
| `OBJECTIVE.md` | New — concrete build spec (this session) |
| `ARCHITECTURE.md` | Updated — corrected colony data flow, InstallationsDB correction |
| `GameEngine/Colonies/CLAUDE.md` | Updated — corrected installation system section |
| `GameEngine/Industry/CLAUDE.md` | Updated — corrected InstallationsDB rows |
| `Pulsar4X.Client/CLAUDE.md` | Updated — corrected PlanetaryWindow entry |
| `GameEngine/Fleets/CLAUDE.md` | New (this session) |
| `.claude/settings.json` | New — hooks + read-deny for bin/obj |
| `.claude/hooks/session_start.sh` | New |
| `.claude/hooks/session_stop.sh` | New |
| `.claude/hooks/lint_csharp.sh` | New |
| `.claude/commands/build-check.md` | New |
| `.claude/commands/phase-status.md` | New |
| `.claude/commands/damage-audit.md` | New |
| `docs/aurora/*.md` (13 files) | New — full Aurora design reference |
| `SESSION_STATE.md` | New (this file) |

---

## Pinned Change Points (Next Code Work)

### Phase 2a — Fix Installations Tab (FIRST)

**File:** `Pulsar4X/Pulsar4X.Client/Interface/Windows/PlanetaryWindow.cs`

Two broken lines to fix:
- **Line 107:** `if (_lookedAtEntity.Entity.HasDataBlob<InstallationsDB>())` — this is the tab's render gate; it's always false because `InstallationsDB` is never attached to any colony. Change to `HasDataBlob<ComponentInstancesDB>()`.
- **Line 221:** `if (_lookedAtEntity != null && _lookedAtEntity.Entity.HasDataBlob<InstallationsDB>())` — same dead gate inside `RenderInstallations()`. Change to `ComponentInstancesDB`, then implement the render body using `ComponentInstancesDB.DesignsAndComponentCount` (a dict of design entity → count).

The `ComponentInstancesDBDisplay` panel already exists in the client — search for it and reuse rather than writing a new table.

**Read before touching:** `GameEngine/Colonies/CLAUDE.md`, `CONVENTIONS.md` §6.

---

### Phase 1a — Wire Complex Damage (can run parallel with Phase 2)

**File:** `Pulsar4X/GameEngine/Weapons/WeaponBeam/BeamWeaponProcessor.cs`

- **Line 132:** `// DamageProcessor.OnTakingDamage(beamInfo.TargetEntity, damage);` — uncomment this
- **Line 134:** `var damageResult = SimpleDamage.OnTakingDamage(beamInfo.TargetEntity, 100, 500);` — remove or comment out this

**Read first:** `GameEngine/Damage/DamageComplex/DamageTools.cs` and `DamageProcessor.cs` in full — understand what `DamageProcessor.OnTakingDamage()` actually needs as arguments before removing the SimpleDamage call. The signature may not match.

**Read:** `GameEngine/Damage/CLAUDE.md`.

---

### Phase 2c — Population Formula

**File:** `Pulsar4X/GameEngine/Colonies/PopulationProcessor.cs`

The stub is at **line 54:** `growthRate = -50.0;` — this fires when pop exceeds `maxPopulation`. This is the placeholder die-off.

**Target formula (from `docs/aurora/COLONY-ENVIRONMENT-AND-POPULATION.md` §2):**
- Growth is normal up to 33% of capacity
- Declines linearly from 33% → 100% capacity (i.e., growth rate × (1 − ((pop/capacity − 0.33) / 0.67)))
- At 100% capacity, growth rate = 0 (not −50%)

The `maxPopulation` calculation on line 49 is already close to the Aurora formula — verify it matches `Infrastructure / (CC × 100)` in millions before changing the growth curve.

---

## Known Broken Things (Don't Touch Without Reading the Doc First)

| Issue | File | Line | Doc |
|-------|------|------|-----|
| Installations tab never appears | PlanetaryWindow.cs | 107, 221 | Colonies/CLAUDE.md Gotcha #1 |
| SimpleDamage placeholder | BeamWeaponProcessor.cs | 132–134 | Damage/CLAUDE.md, root CLAUDE.md Gotcha #1 |
| Colony damage block commented out | DamageProcessor.cs | ~101–181 | Damage/CLAUDE.md, root CLAUDE.md Gotcha #8 |
| Population −50% die-off stub | PopulationProcessor.cs | 54 | COLONY-ENVIRONMENT-AND-POPULATION.md |
| Missile guidance hardcoded false | MissleProcessor.cs | directAttack | root CLAUDE.md Gotcha #3 |

---

## What's In the Game Data Already

**`GameEngine/Data/basemod/blueprints/installations.json`** defines these installation component types (all as `MountType: PlanetInstallation` or similar):

| ID | Name | Notes |
|----|------|-------|
| `mine` | Mine | Mines all resources, `MiningAmountDict` attr |
| `automine` | RoboMiner | Unmanned, transportable |
| `university` | University | Research points, `ResearchPointsAtbDB` attr |
| `refinery` | Refinery | Refines minerals to materials |
| `factory` | Factory | Produces components, installations, ordnance |
| `shipyard` | Ship Yard | Builds ships |
| `logistics-office` | Logistics Office | Import/export, `LogiBaseAtb` attr |
| `fuel-cargo-hold` | Fuel Storage | Also `fuel-tank` variant |
| `naval-academy` | Naval Academy | Graduates officers, `NavalAcademyAtb` attr |
| `spaceport` | Planetary Spaceport Complex | Cargo transfer + storage |
| `infrastructure` | Infrastructure | Population life support on hostile worlds |
| `space-port` | Space Port | Cargo transfer (simpler variant) |

This tells us the Installations tab, once fixed, has real data to display — it won't be empty just because of the render bug.

**Note:** `infrastructure` description says "currently non functional other than as cargospace" — the CC/pop formula tie-in is the Phase 2c work.

---

## What CLAUDE.md Files Are Still Missing

| Subsystem | Directory | Priority | Why It Matters |
|-----------|-----------|----------|----------------|
| Fleets | `GameEngine/Fleets/` | HIGH | Phase 4 — transport ships, landing operations |
| Research/Tech | `GameEngine/Tech/` | MEDIUM | Phase 4 — unlocking ground combat tech |
| Sensors | `GameEngine/Sensors/` | LOW | Benchmark; not on critical path |
| Orbits | `GameEngine/Orbits/` | LOW | Already well-understood |
| Galaxy/System Gen | `GameEngine/Galaxy/` | LOW | Not on critical path |
| Logistics | `GameEngine/Logistics/` | MEDIUM | Phase 4 — supply lines, GSP |

**Fleets CLAUDE.md** was written this session (see `GameEngine/Fleets/CLAUDE.md`).

---

## Environment Notes

### Claude's execution environment
Remote Linux cloud container. **No .NET SDK installed.** Claude edits files here; the developer pulls and builds on their own machine.

### Developer's machine
| Item | Value |
|------|-------|
| OS | Windows |
| Shell | **PowerShell** — all commands given to the developer must be PowerShell-compatible |
| GPU | NVIDIA RTX 3090 FTW3 Ultra — 24 GB VRAM |
| CPU | AMD Ryzen 7 5800X3D — 8 cores / 16 threads |
| RAM | 32 GB |
| PSU | 850 W |

### Workflow for code changes
Claude writes change → developer runs in PowerShell from repo root:
```powershell
git pull origin claude/amazing-clarke-7s118n
dotnet build Pulsar4X/Pulsar4X.sln
dotnet test Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj
```
If build/test fails → paste error output here → Claude fixes → repeat.
