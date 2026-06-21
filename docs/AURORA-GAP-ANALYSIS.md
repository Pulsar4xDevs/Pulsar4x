# Pulsar4X vs Aurora 4X — Gap Analysis

**Written:** 2026-06-21  
**Purpose:** Honest accounting of where Pulsar stands relative to Aurora's full simulation depth. Use this to set priorities and calibrate ambition. Does not cover the UI layer — see `Pulsar4X.Client/CLAUDE.md`.

---

## The Short Answer

Pulsar is roughly **50–55% of Aurora's simulation depth overall.**

The high end (~75–85%) is the space physics layer: movement, orbits, sensors, weapons fire control, system generation. This part was the original project focus and it shows — it's genuinely rigorous, in some ways more physically correct than Aurora.

The low end is everything that touches the planet surface and the strategic/human layer: ground combat is zero, orbital bombardment is commented out, the population formula is a stub, the installations UI is broken, and commander skill bonuses don't apply to anything. That is the entire objective of this fork.

---

## Subsystem-by-Subsystem Breakdown

| Subsystem | Aurora depth | Pulsar status | Gap |
|-----------|-------------|---------------|-----|
| Movement / Navigation | Newtonian thrust, Warp drives, jump transit | ✅ Fully functional | Minimal |
| Orbits | Kepler mechanics for all bodies | ✅ Fuzz-tested, reliable | Minimal |
| Sensors / Detection | EM/thermal signatures, attenuation, contacts | ✅ More rigorous than Aurora | Small — missing EMCON controls and EW jamming |
| System / Planet Generation | Procedural star systems, atmospheres | ✅ Functional, Aurora greenhouse formula already in code | Small — no TerraformingProcessor yet |
| Research / Tech | Labs → points → scientist multipliers → unlock | ✅ Works end-to-end | Small — "ground-combat" tech category not confirmed in JSON |
| Logistics | Automated cargo routes, profit-based bidding | ✅ Bidding market functional | Medium — Maintenance Supply Points (MSP) completely absent |
| Space Combat (Weapons) | Beam fire control, missiles, point defense | ⚠️ Beams work; missile guidance hardcoded to broken path | Medium — `directAttack = false` hardcoded; fix needed |
| Damage | Component-level armor penetration, HTK | ❌ Placeholder active — random 100–500 HP damage | **Large — this is the blocker for all combat** |
| Fleets | Task groups, orders, flagship commander | ⚠️ FleetDB + orders work | Medium — commander bonuses don't reach any formula |
| Colonies / Population | Colony cost, carrying capacity, growth | ⚠️ Colony cost formula exists; population growth is -50.0 stub | **Large — core loop is broken** |
| Industry / Production | Construction factories, mining, refineries | ⚠️ Partially built — see `Industry/CLAUDE.md` | Medium |
| People / Commanders | Officers, skills, fleet bonuses, ground bonuses | ⚠️ NavalAcademy generates officers; experience is stored and never read; **NO bonus fields on CommanderDB** | Large — the whole "quality matters" mechanic doesn't exist |
| Ground Combat | Formations, elements, combat resolution, invasion | ❌ Zero | **100% gap — the primary objective** |
| Orbital Bombardment | Strike packages vs colonies, installation damage | ❌ Design intent is a commented-out block in `DamageProcessor.cs` ~lines 101–181 | **100% gap** |
| Terraforming | Modify atmosphere over time | ❌ AtmosphereDB + hook exists; no TerraformingProcessor | 90% gap — hook designed, processor missing |
| Diplomacy / Intel | Faction relations, ELINT, treaties | ❌ Minimal faction state only | Deliberate low priority |
| EW / Jamming | Active jamming vs sensors | ❌ Not present | Low priority, comes after ground combat |
| Installations UI | View what's installed on a colony | ❌ Broken — gated on a dead DataBlob that is never attached | **UI blocker — first fix to make** |

---

## The Four Critical Gaps

These are the things that must be fixed before ground combat is buildable. Everything else can wait.

### 1. The damage system is a placeholder
**`DamageProcessor.cs`** — the real component-level damage path is commented out. The active code calls `SimpleDamage.OnTakingDamage(entity, 100, 500)` which just rolls a random number between 100 and 500 and subtracts it. Every "hit" in the game right now is a random number. Ground combat and orbital bombardment both need real damage — so this gets fixed first, or everything built on top of it is wrong.

### 2. The population formula is a stub
**`PopulationProcessor.cs`** — growth is set to a constant `-50.0` rate. There is no colony cost calculation being applied, no infrastructure cap, and no habitability factor. A colony grows or shrinks by 50 regardless of what planet it's on or what infrastructure it has. The `ColonyCost()` formula exists in `SpeciesDBExtensions.cs` — it's just not being called.

### 3. Commander bonuses don't exist
**`CommanderDB.cs`** — there are no bonus fields on the class. Officers have a rank and experience number, but those numbers feed into nothing. Compare: `Scientist.cs` has a full `Bonuses` dictionary that actually multiplies research output. `CommanderDB` has no equivalent. This means fleet quality, ground commander quality, and flagship bonuses are all zero in the current game.

### 4. The installations UI is wired to a dead blob
**`PlanetaryWindow.cs` lines 107 and 221** — the Installations tab is gated on `HasDataBlob<InstallationsDB>()`. That blob is never attached to any colony — `ColonyFactory` doesn't add it. So the tab never appears and the render method never runs. Real installations live in `ComponentInstancesDB`, which is a completely different blob. This is the first fix in Phase 2a and it's about 10 lines of code.

---

## What Pulsar Does Well (Keep, Don't Break)

These subsystems are genuinely solid and in some respects more rigorous than Aurora:

**Sensors** — Aurora uses a simplified thermal + EM dual-band model. Pulsar uses actual wavelength bands with triangular spectral overlap for detection quality. This is physically more correct. Do not redesign this when wiring ground combat sensors — it's the wrong tool for ground-unit spotting (that's line-of-sight and terrain, not EM attenuation), but leave the space sensor model alone.

**Orbits / Movement** — Kepler orbital mechanics with fuzz testing. Newtonian thrust with delta-V budgeting. These are tested and correct. Nothing in the ground combat stack touches them.

**Atmosphere / System Generation** — Aurora's greenhouse formula is already implemented verbatim, cited by name in the source comments. The `AtmosphereDB` model (gas composition dictionary, surface temperature, hydrosphere) is complete. The hook for terraforming is already designed — adding gas to `Composition` and calling `AtmosphereProcessor.UpdateAtmosphere()` is all that's needed.

**Research / Tech** — Scientists with per-category bonuses actually work. Labs generate points. Points flow to the project queue. Unlocks fire events. The ground-combat tech category needs to be confirmed in `techCategories.json`, but the infrastructure is solid.

**Logistics** — The automated freight-market system (ships bid on profitable cargo runs) is functional. Ground unit transport is cargo movement — a `GroundUnitDesign` implementing `ICargoable` is all that connects ground forces to this system.

---

## Aurora's Missing Depth — What's Totally Absent

These are Aurora systems that Pulsar has zero implementation of:

| System | Why it matters | Priority for this fork |
|--------|----------------|------------------------|
| Ground combat (formations, elements, resolution) | The entire objective | **Highest** |
| Orbital bombardment (strike packages → colony damage) | Required for invasion to have teeth | **High** |
| Maintenance Supply Points | Ships degrade without MSP; Aurora's maintenance clock | Low |
| Electronic warfare / jamming | EW against sensors | Low |
| Prototype construction | Research unlocks → build one before mass producing | Low |
| Diplomacy / ELINT | Faction relations, intelligence gathering | Lowest |

---

## The Real Gap Is Architecture Coverage, Not Code Quality

The code that exists is generally well-written. It follows the ECS pattern consistently. The JSON mod system is flexible. The NCalc formula engine for component and tech costs is elegant.

The problem is that roughly half of Aurora's *simulation scope* simply hasn't been attempted yet:
- The planet surface as a strategic theater
- The human capital layer (commanders that actually matter)
- The economic loop closing (population formula, infrastructure cap)
- The physics of things hitting planets

None of those require redesigning what's already there. They require building new DataBlobs and Processors on top of the existing framework, with a few targeted fixes to the stubs and commented-out blocks. That is exactly what this fork's objective covers.

---

## Recommended Fix Sequence

This matches `PLAN.md` Phase ordering, grounded in the gap analysis:

1. **Fix installations UI** (`PlanetaryWindow.cs` lines 107, 221) — 10 lines of code, unblocks seeing what's actually on a colony.
2. **Fix population formula** (`PopulationProcessor.cs`) — wire `ColonyCost()` and `PopulationSupportAtbDB` into the growth calculation.
3. **Restore complex damage** (`DamageProcessor.cs`) — uncomment and fix the real damage path; this unblocks both naval and ground combat.
4. **Add commander bonus fields** (`CommanderDB.cs`) — mirror `Scientist.Bonuses` pattern; without this, ground commanders are decoration.
5. **Build ground combat stack** — once 1–4 are solid, the full hierarchy (`FormationDB`, `FormationElementDB`, `GroundCombatProcessor`) builds on a working foundation.
