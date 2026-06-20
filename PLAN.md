# Pulsar4X — Development Plan

## Objective

Bring planetary/ground combat and planetary infrastructure to the same depth as space combat already has, and improve the UI. Space combat is the template; ground combat mirrors its architecture.

---

## Current State Assessment

### Space Combat — Mature (70%)
- Fire control assignment: ✅ complete
- Beam weapon physics + hit calculation: ✅ complete  
- Missile launch + orbital phasing: ✅ mostly complete (guidance has issues)
- Component-level damage: ❌ **commented out** — SimpleDamage placeholder used
- Armor / shield layers: ❌ not implemented
- Point-defense / anti-missile: ❌ not implemented
- Combat events + UI feedback: ✅ basic (events published, FireControlWindow exists)

### Ground Combat — Missing (5%)
- Ground unit types: ❌ not defined
- Invasion mechanics: ❌ not implemented
- Orbital bombardment damage: ❌ commented-out stub only
- Ground combat resolution: ❌ not implemented
- Ground combat UI: ❌ not implemented
- PlanetaryWindow.RenderInstallations(): ❌ **empty body**

### Planetary Infrastructure — Partial (40%)
- Colony creation + population: ✅ functional
- Mining: ✅ functional
- Industrial production (ships/components): ✅ functional
- Installation management: ✅ installations exist **as components** in `ComponentInstancesDB` (the `InstallationsDB` blob is dead — see `Industry/CLAUDE.md`)
- Infrastructure levels / specialization: ❌ not implemented
- Population happiness / growth sim: ❌ partial stub only
- PlanetaryWindow installations display: ❌ empty

### UI — Partial (50%)
- System map rendering: ✅ good
- Ship design window: ✅ good
- Colony management: ✅ basic
- PlanetaryWindow: ⚠️ general info + minerals work; installations tab empty
- Ground combat window: ❌ missing entirely
- Fleet window: ✅ basic

---

## Risk Register

| Risk | Severity | Notes |
|------|----------|-------|
| Complex damage system not wired | HIGH | Must fix before ground combat can mirror space combat |
| `async void` exception swallowing in EntityManager | MEDIUM | Can hide bugs in new processor code; add try/catch or switch to Task |
| TypeNameHandling.Objects in saves | MEDIUM | Renaming DataBlobs breaks save files; plan migrations |
| No combat tests | MEDIUM | Any regression in combat is invisible until manual play |
| Missile guidance half-implemented | LOW | Functional enough for the objective but needs attention |
| ViewModelLib confusion | LOW | Dead library — don't reference it |

---

## Phase 1 — Fix Space Combat Foundation (prerequisite for ground combat)

**Goal:** Make space combat internally consistent and fully wired before using it as the ground combat template.

### 1a. Wire complex damage
- `Damage/DamageComplex/DamageProcessor.cs`: Uncomment the real `OnTakingDamage()` call in `BeamWeaponProcessor.OnHit()`.
- Verify `EntityDamageProfileDB` gets created for ships at construction time (currently lazy-created on first hit — fragile).
- Confirm `DamageTools.DealDamageEnergyBeamSim()` produces sensible output.
- Remove the `SimpleDamage.OnTakingDamage(entity, 100, 500)` placeholder call.
- **Files:** `Weapons/WeaponBeam/BeamWeaponProcessor.cs` (line ~134), `Damage/DamageComplex/DamageProcessor.cs`, `Damage/DamageComplex/DamageTools.cs`

### 1b. Add combat tests
- `Pulsar4X.Tests/CombatTests.cs` (new): fire a beam weapon at a ship, assert damage applied, assert ship destroyed when HP → 0.
- `Pulsar4X.Tests/MissileTests.cs` (new): launch missile, verify entity created with correct thrust DB.

### 1c. Fix missile guidance
- Set `directAttack = true` or implement a functioning targeting command so missiles actually pursue targets.
- `Movement/NewtonMove/NewtonThrustCommand.cs` + `MissleProcessor.cs`

**Riskiest unknown:** Whether `DamageTools.DealDamageEnergyBeamSim()` works correctly. Read `DamageComplex/DamageTools.cs` carefully before wiring.

---

## Phase 2 — Planetary Infrastructure Depth

**Goal:** Give colonies meaningful internal structure so ground combat has something to fight over.

### 2a. Installations display (corrected — do NOT use InstallationsDB)
- `Pulsar4X.Client/Interface/Windows/PlanetaryWindow.cs`: `RenderInstallations()` is empty **and** its tab is gated on the dead `InstallationsDB` (`PlanetaryWindow.cs:107`), so the tab never appears.
- Re-gate on `ComponentInstancesDB` (every colony has one) and render the colony's installations — they are components. Reuse the existing `ComponentInstancesDBDisplay` panel or build a table from `ComponentInstancesDB.DesignsAndComponentCount`.
- Optionally add construction queueing of new installations through the existing industry/component pipeline.
- See `docs/aurora/PLANETARY-INFRASTRUCTURE.md` §6 and `CONVENTIONS.md` §6.

### 2b. Infrastructure levels
- Add `InfrastructureLevel` (int) to `ColonyInfoDB` or a new `InfrastructureDB`.
- Infrastructure unlocks production capacity multipliers, population caps, defense bonuses.
- IndustryProcessor reads this to scale output.
- **Files:** `Colonies/ColonyInfoDB.cs`, `Industry/IndustryProcessor.cs`

### 2c. Population simulation
- `Colonies/PopulationProcessor.cs`: Implement growth (sigmoid toward carrying capacity), decay (starvation, bombardment casualties).
- Carrying capacity driven by life support, food production, housing installations.
- Add `ColonyHappinessDB` for morale tracking.

### 2d. Production specialization
- Allow colonies to specialize (mining world, shipyard, research station) with bonus multipliers.
- Represent as a field on `ColonyInfoDB` or a new `ColonySpecializationDB`.
- UI: add specialization selector to ColonyManagementWindow.

**Riskiest unknown:** Carrying capacity formula — Aurora uses a complex habitability calculation involving species-body compatibility. Read `ColonyLifeSupportDB` and `AtmosphereDB` before designing this.

---

## Phase 3 — Orbital Bombardment

**Goal:** Connect space combat to planetary surface — ships can damage colonies from orbit.

### 3a. Uncomment and complete colony damage
- `Damage/DamageComplex/DamageProcessor.cs`: The commented-out block (lines ~101–181) handles colony damage.
- Missing types to recreate or substitute: `ColonyInfoDB.ColonyComponentDictionary`, installation HP tracking.
- Implement population casualties (flat damage based on yield, modified by atmosphere/shelter).
- Implement dust/radiation effects on atmosphere.
- **Files:** `Damage/DamageComplex/DamageProcessor.cs`, `Colonies/ColonyInfoDB.cs`, `Galaxy/AtmosphereDB.cs`

### 3b. Bombardment orders
- New `InvadeOrder` / `BombardOrder` in `Colonies/`.
- A ship in orbit over an enemy colony can issue a bombardment order.
- Hooks into `MissileProcessor` (missiles → colony damage) and a new `BombardmentProcessor` for kinetic strikes.

### 3c. Orbital bombardment UI
- Button in `FleetWindow` or `SystemWindow` when hovering enemy colony.
- Show projected damage and population casualties before confirming.

**Riskiest unknown:** The commented-out colony damage code references `MassVolumeDB.Volume_km3` which may not exist or may have been renamed. Verify before implementing.

---

## Phase 4 — Ground Combat System

**Goal:** Build ground combat at parity with space combat. Mirror the space combat architecture: DataBlobs → Processors → Orders → UI.

> **Read first:** `docs/aurora/GROUND-COMBAT.md` (Aurora is the design spec; it has a full Pulsar-mapping table) and `CONVENTIONS.md` §6 (units = component-bearing entities). Aurora designs ground units from researched components — model a `GroundUnitDesign` as an `IConstructableDesign` whose weapons/armor/HQ/logistics are `ComponentDesign`s with new `*Atb` attributes, installed into a `ComponentInstancesDB`, exactly like a ship. Prefer a dedicated `GameEngine/GroundForces/` subsystem dir over scattering files into `Colonies/`.

### 4a. Ground unit data model
```
New files:
  GameEngine/Colonies/GroundUnitDB.cs        ← unit type, strength, organization, equipment
  GameEngine/Colonies/GroundUnitDesign.cs    ← design (mirrors ShipDesign/ComponentDesign)
  GameEngine/Colonies/GroundUnitFactory.cs   ← entity creation (mirrors ShipFactory)
```
- Ground units live as entities in the same StarSystem as their planet (like ships in orbit).
- Each ground unit has a `ColonyInfoDB`-linked home (their garrison planet).
- Equipment slots mirror ship components (weapons, armor, sensors, logistics).

### 4b. Ground combat processor
```
New file: GameEngine/Colonies/GroundCombatProcessor.cs (IHotloopProcessor)
  RunFrequency: 1 game-day
  DataBlob: GroundCombatDB (new) — attached when combat is active on a body
```
Resolution loop (mirror of space combat turn):
1. Attacker units fire at defender units (use `GenericFiringWeaponsProcessor` pattern).
2. Defender units return fire.
3. Apply casualties via the **complex `DamageProcessor`** (penetration-vs-armor + component HTK), **not** `SimpleDamage`. Ground and naval combat share one damage core, so Phase 1 must land first. See `docs/aurora/GROUND-COMBAT.md` §4 for the to-hit → penetrate → destroy sequence.
4. Check win conditions (all defenders destroyed, attacker retreats, siege timer).

### 4c. Invasion order
```
New file: GameEngine/Colonies/InvasionOrder.cs (IInstanceProcessor)
```
- Issued by a transport ship in orbit with ground troops in cargo.
- Checks orbital superiority (no enemy ships in same orbit zone).
- Lands troops → creates GroundCombatDB on colony → triggers GroundCombatProcessor.

### 4d. Ground combat UI
```
New file: Pulsar4X.Client/Interface/Windows/GroundCombatWindow.cs
```
- Shows attacker units, defender units, current strength bars.
- Shows combat log (events).
- Mirrors FireControlWindow style.
- Hook: PlanetaryWindow gets "Ground Combat" tab when GroundCombatDB is present.

### 4e. Production integration
- Colonies with `IndustryAbilityDB` can queue ground unit production (like ships).
- Ground unit designs added to `IndustryJob` via new `GroundUnitConstructableDesign`.

**Riskiest unknown:** Whether ground units as entities in a StarSystem will cause performance problems at scale. Measure with Benchmarks project before shipping. If performance is a concern, consider a pure data-structure approach (ground combat as a sub-simulation inside a single DataBlob rather than many entities).

---

## Phase 5 — UI Polish

**Goal:** Make the existing UI more usable and add missing depth displays.

### 5a. PlanetaryWindow improvements
- `RenderInstallations()`: render the colony's installations from `ComponentInstancesDB` (type, count, status) — re-gate off the dead `InstallationsDB` first (see Phase 2a).
- Add colony specialization display/setter.
- Add population carrying capacity bar.
- Add defense strength indicator.

### 5b. Colony Management depth
- `ColonyManagementWindow`: show industry queue, mining rates, research output side-by-side.
- Add sorting/filtering.

### 5c. Ground combat event log
- Extend `FactionEventLog` (already exists in `Factions/FactionEventLog.cs`) with ground combat event types.
- Display in `GameLogWindow`.

### 5d. Map indicators
- Show ground combat icon on planet in system map when `GroundCombatDB` is present.
- Show bombardment effect overlay.

---

## Sequence Dependency

```
Phase 1 (Combat fix) → Phase 3 (Orbital bombardment) → Phase 4 (Ground combat)
Phase 2 (Infrastructure) → Phase 4 (Ground combat — needs something to fight over)
Phase 4 → Phase 5 (UI — needs systems to display)
```

Phase 1 and Phase 2 can be worked in parallel. Phase 3 depends on Phase 1. Phase 4 depends on Phases 1, 2, and 3. Phase 5 wraps everything.

---

## First Thing to Tackle

**Phase 1a: Wire complex damage.** It is the single highest-leverage fix in the codebase. Once beam weapons apply real component-level damage, the full space combat loop is working, tests can be written, and the same damage path can be extended for ground units and colony bombardment. Everything downstream depends on this.

Read `Damage/DamageComplex/DamageTools.cs` and `Damage/DamageComplex/EntityDamageProfileDB.cs` in full before touching `BeamWeaponProcessor.OnHit()`.
