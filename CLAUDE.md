# Pulsar4X — Developer Reference

**Pulsar4X** is an open-source C# fan recreation of the 4X space sim Aurora 4x. Entry point: `Pulsar4X/Pulsar4X.Client/Program.cs`. Central game loop: `GameEngine/Engine/MasterTimePulse.cs`.

---

## Communicating With the Developer (READ FIRST — applies every session)

The developer is hands-on technical (US Navy nuclear-trained machinist's mate) but **not a professional software engineer**. Explain in **plain English**, not software jargon. This applies to chat replies *and* to docs you write.

- **Lead with what it does and why it matters**, then how it works. The point before the plumbing.
- **Spell out and define jargon on first use.** "DataBlob (a container that holds one kind of data for a game object)", not just "DataBlob". Avoid unexplained acronyms.
- **Use analogies** — mechanical, electrical, thermodynamic, shipboard analogies land well (e.g. "a processor is like a watch station that does one job every shift").
- **Don't dumb down the substance — simplify the language.** He can follow reactor physics; he just isn't fluent in code vocabulary. Keep the engineering honest; lose the CS lingo.
- **Short sentences. Concrete examples. Name the file and what it's for**, not abstract patterns.
- When something genuinely is complex, say so plainly and walk through it step by step rather than hiding it behind a term.

---

## Developer Objective (this fork)

Extend the fork so that **planetary/ground combat and planetary infrastructure eventually have the same depth that space combat already has**, and improve the UI. Survey priority: understand how space combat is built so it can be mirrored for ground systems, where ground/planetary systems currently live or would hook in, and how the UI layer is constructed.

---

## Project / Solution Layout

Solution file: `Pulsar4X/Pulsar4X.sln`

| Project | Directory | Role |
|---------|-----------|------|
| **GameEngine** | `Pulsar4X/GameEngine/` | Core game logic, data model, all processors. The heart of the game. |
| **Pulsar4X.Client** | `Pulsar4X/Pulsar4X.Client/` | ImGui.NET + SDL2 UI — the only runnable application. |
| **Pulsar4X.OrbitalMath** | `Pulsar4X/Pulsar4X.OrbitalMath/` | Standalone orbital mathematics library (`Pulsar4X.Orbital` namespace). Referenced by GameEngine. |
| **Pulsar4X.Tests** | `Pulsar4X/Pulsar4X.Tests/` | NUnit 3 test suite, references GameEngine directly. |
| **Benchmarks** | `Pulsar4X/Benchmarks/` | BenchmarkDotNet performance benchmarks. |
| **ViewModelLib** | `Pulsar4X/ViewModelLib/` | Legacy ViewModel/GL library from an older WPF frontend. Not referenced by the current client — ignore for new work. |

---

## Tech Stack

| Item | Value |
|------|-------|
| .NET target | net8.0 (all projects) |
| UI framework | ImGui.NET 1.88.0 + SDL2-CS |
| Rendering | OpenGL via SDL2 |
| Serialization | Newtonsoft.Json 13.0.3 (`PreserveReferencesHandling.Objects`, `TypeNameHandling.Objects`) |
| Math expressions | CoreCLR-NCalc 2.2.113 (used in component design formulas) |
| Tests | NUnit 3.13.3 + NUnit3TestAdapter |
| Benchmarks | BenchmarkDotNet 0.14.0 |
| Mod data | JSON files under `GameEngine/Data/basemod/` |

---

## Build / Run / Test Commands

> **Cloud environment note:** `.NET SDK is NOT installed in the remote Claude Code execution environment.` Claude can read and edit C# files but cannot run `dotnet build` or `dotnet test` remotely. **Workflow:** Claude writes the change → user pulls branch to Windows machine → user builds/tests locally (.NET 8 SDK + SDL2 required) → user pastes any errors back → Claude fixes. See `SESSION_STATE.md` for current build/test baseline.

All commands run from the repo root on the **user's local machine**:

```bash
# Build entire solution
dotnet build Pulsar4X/Pulsar4X.sln

# Run the game (Linux)
dotnet run --project Pulsar4X/Pulsar4X.Client/Pulsar4X.Client.csproj

# Run tests
dotnet test Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj

# Run tests with verbosity
dotnet test Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj --logger "console;verbosity=detailed"

# Run benchmarks (Release mode required)
dotnet run --project Pulsar4X/Benchmarks/Benchmarks.csproj -c Release
```

---

## Architecture Pattern: Hybrid ECS

Pulsar4X uses a **hybrid Entity-Component-System (ECS)** pattern:

- **Entity** (`Engine/Entities/Entity.cs`): A lightweight container with an integer `Id`. All data is stored in DataBlobs attached through the EntityManager.
- **DataBlob** (`Engine/Datablobs/BaseDataBlob.cs`): The "Component" — a strongly-typed C# class holding state. Named with a `DB` suffix. Attached to and retrieved from entities by type.
- **Processor** (`Engine/Processors/`): The "System" — processes all entities with a given DataBlob type. Two kinds:
  - `IHotloopProcessor`: Runs on a fixed game-time frequency for all entities in a manager (e.g., every 1 second game-time). Auto-discovered via reflection.
  - `IInstanceProcessor`: Runs at a specific scheduled instant for specific entities (interrupt-driven). Auto-discovered via reflection.
- **EntityManager** (`Engine/Entities/EntityManager.cs`): Owns a set of entities and their DataBlobs. `StarSystem` extends it to represent a solar system.
- **MasterTimePulse** (`Engine/MasterTimePulse.cs`): The global game loop driver. Steps all `StarSystem.ManagerSubpulses` forward in time, optionally in parallel.
- **ManagerSubPulse** (`Engine/ManagerSubPulse.cs`): Per-system scheduler that maintains two queues — a frequency-based HotLoop queue and a time-indexed instance queue.

See `ARCHITECTURE.md` for the full data-flow diagram.

---

## Subsystem Index

| Subsystem | Directory | CLAUDE.md |
|-----------|-----------|-----------|
| Game Engine Core | `GameEngine/Engine/` | `GameEngine/CLAUDE.md` |
| Space Combat (Weapons) | `GameEngine/Weapons/` | `GameEngine/Weapons/CLAUDE.md` |
| Damage | `GameEngine/Damage/` | `GameEngine/Damage/CLAUDE.md` |
| Colonies / Population | `GameEngine/Colonies/` | `GameEngine/Colonies/CLAUDE.md` |
| Industry / Production | `GameEngine/Industry/` | `GameEngine/Industry/CLAUDE.md` |
| Movement / Navigation | `GameEngine/Movement/` | `GameEngine/Movement/CLAUDE.md` |
| Sensors | `GameEngine/Sensors/` | *(not yet written — read source directly)* |
| Orbits | `GameEngine/Orbits/` + `Pulsar4X.OrbitalMath/` | *(not yet written — read source directly)* |
| Galaxy / System Gen | `GameEngine/Galaxy/` | *(not yet written — read source directly)* |
| Fleets | `GameEngine/Fleets/` | `GameEngine/Fleets/CLAUDE.md` |
| Logistics | `GameEngine/Logistics/` | *(not yet written — read source directly)* |
| Research/Tech | `GameEngine/Tech/` | *(not yet written — read source directly)* |
| UI Client | `Pulsar4X.Client/` | `Pulsar4X.Client/CLAUDE.md` |
| Tests | `Pulsar4X.Tests/` | *(see Testing section below)* |

---

## Design & Convention References

| Doc | Read it when |
|-----|--------------|
| `CONVENTIONS.md` | **Before writing any new code.** Pulsar's actual coding idioms (DataBlob copy-ctor/`Clone()`, serialize-one-collection-rebuild-indexes, `TryGet`/sentinel defensiveness, components+`*Atb`, processor auto-discovery). Match these, don't impose your own style. |
| `docs/aurora/INDEX.md` | Designing any ground-combat or infrastructure mechanic. Aurora 4X is the design spec for systems Pulsar lacks. |
| `docs/aurora/GROUND-COMBAT.md` | Building ground forces, formations, invasion, bombardment-vs-ground. |
| `docs/aurora/PLANETARY-INFRASTRUCTURE.md` | Adding installations, construction, economy depth. |
| `docs/aurora/SPACE-COMBAT-BENCHMARK.md` | Calibrating "the same depth space combat has." |

---

## Key Constants and Conventions

### Naming Conventions
| Pattern | Meaning | Example |
|---------|---------|---------|
| `*DB` suffix | DataBlob (component) | `ColonyInfoDB`, `BeamInfoDB` |
| `*Processor` suffix | HotloopProcessor or IInstanceProcessor | `BeamWeaponProcessor` |
| `*Atb` suffix | Component design attribute | `GenericBeamWeaponAtb` |
| `*Order` / `*Command` suffix | Player-issued order | `SetFireControlOrder`, `NewtonThrustCommand` |
| `*Factory` suffix | Entity creation helper | `ColonyFactory`, `ShipFactory` |
| `*Blueprint` suffix | JSON-loadable data template | `ComponentDesignBlueprint` |

### Code Conventions Observed
- `[JsonProperty]` on all fields that must survive save/load.
- `[JsonIgnore]` on runtime-only references (Manager, Game, etc.).
- `async void` used (not `async Task`) on `EntityManager` mutation methods — be aware this swallows exceptions.
- DataBlobs implement `Clone()` for when they are moved between managers (e.g., ship jumping systems).
- `SafeDictionary<K,V>` and `SafeList<T>` are thread-safe wrappers used for shared collections.
- `NullReferenceException` from `#nullable enable` warnings suppressed with `NoWarn>0649` in project files — nullable annotations exist but are unenforced in many places.

---

## Testing

All tests are NUnit 3 in `Pulsar4X.Tests/`. Run with `dotnet test`.

Coverage includes: EntityManager, DataBlobs, orbits (including fuzz testing), save/load, ship components, mining, population processor, pathfinding, system generation, serialization.

**No tests exist for space combat (weapons, damage, fire control) or ground combat.** Any new combat system must add tests.

Test utilities live in `TestHelper.cs` and `TestingUtilities.cs`.

---

## Critical Gotchas

1. **Complex damage is commented out.** `BeamWeaponProcessor.OnHit()` calls `SimpleDamage.OnTakingDamage(entity, 100, 500)` — a placeholder with hardcoded 100–500 random damage. The real component-level `DamageProcessor.OnTakingDamage()` call is commented out directly above it. Do not build ground combat on top of `SimpleDamage`; restore and complete the complex damage path first.

2. **ProcessorManager auto-discovers via reflection.** Any class implementing `IHotloopProcessor` or `IInstanceProcessor` is automatically registered on startup by `ProcessorManager.CreateProcessors()`. You do not register processors manually. The trade-off: a broken processor crashes startup.

3. **MissileProcessor guidance is half-finished.** The `directAttack = false` branch (orbital phasing maneuvers) has commented-out parts; the `directAttack = true` branch (direct thrust to target) is the functional path but is never taken because `directAttack` is hardcoded `false`. Missile tracking currently uses phasing maneuvers which can fail for non-orbiting targets.

4. **PlanetaryWindow.RenderInstallations() renders nothing — and the bug is worse than "empty body."** The method is gated on `HasDataBlob<InstallationsDB>()`, but **`InstallationsDB` is never attached to any colony** (verified: `ColonyFactory` and `DefaultStartFactory` only ever call `AddComponent(...)`). So the "Installations" tab button never even appears (`PlanetaryWindow.cs:107`). `InstallationsDB` is **vestigial/abandoned** (no `[JsonProperty]` fields, only dead-code references). Real installations are **components in `ComponentInstancesDB`** — the colony's `AddComponent(mineDesign)`, `AddComponent(facEntity)`, etc. The correct fix renders from `ComponentInstancesDB` (a `ComponentInstancesDBDisplay` panel already exists), not from `InstallationsDB`. See `docs/aurora/PLANETARY-INFRASTRUCTURE.md` and `CONVENTIONS.md` §6.

5. **`async void` on EntityManager mutations swallows exceptions.** `AddEntity`, `TagEntityForRemoval`, `SetDataBlob`, `RemoveDatablob` are all `async void` (needed for `MessagePublisher.Publish`). Any exception inside propagates to the thread pool and is unobservable. Keep mutation code minimal and well-tested.

6. **ViewModelLib is dead weight.** `ViewModelLib/` contains WPF-style ViewModel and OpenGL abstractions from a prior frontend. Nothing in the current `Pulsar4X.Client` references it. Do not add new code there.

7. **Save/load uses TypeNameHandling.Objects.** This means the JSON save file embeds C# type names. Renaming or moving a DataBlob class will break existing saves. When refactoring, add a `[JsonConverter]` or migration step.

8. **Colony damage code for ground bombardment exists as a commented-out block** in `DamageProcessor.cs` (~lines 101–181). It references now-missing types (`ComponentInfoDB`, `ComponentInstanceData`, `MassVolumeDB.Volume_km3`) but contains the design intent for orbital bombardment. Read it before designing the ground combat damage system.

---

## How to Work in This Repo (Working Agreement)

1. **Read `CONVENTIONS.md` before writing any code; read the subsystem `CLAUDE.md` before working on that subsystem.** Only read source directly when the doc is insufficient, then update the doc after. For ground-combat/infrastructure design questions, consult `docs/aurora/`.
2. **Keep all CLAUDE.md files current** whenever code changes — update the subsystem CLAUDE.md in the same commit as the code it describes. Stale docs are worse than no docs.
3. **Build and run tests before and after every change.** Never leave the build broken. `dotnet build` + `dotnet test` before pushing.
4. **Match existing conventions** (naming, `[JsonProperty]` discipline, `SafeDictionary`, processor auto-discovery pattern).
5. **Add tests for new systems.** Space combat has no tests; do not compound this pattern.
6. **Do not add features beyond what the task requires.** This is an ambitious codebase — scope creep compounds quickly.
7. **Update ARCHITECTURE.md** when data flow changes.
8. **Update this root CLAUDE.md** when a new subsystem is added, a subsystem moves, or a new gotcha is discovered.
