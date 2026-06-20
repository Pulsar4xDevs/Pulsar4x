# Aurora 4X — Ground Combat & Ground Forces (Design Reference)

Source: aurora-manual `13-ground-forces/` (v2.7.1) + AuroraWiki `C-Ground_Units` / `C-Ground_Combat`.
Status: design reference. Constants are approximate — see `INDEX.md` caveats. ⚠️ = cross-source conflict.

This is the **single largest gap** between Pulsar and Aurora. Pulsar has **no ground combat at all** — no ground unit entity, no formation concept, no ground combat processor, no invasion order, no UI. Everything below is new work. The good news: Aurora's "units are designed from researched components" model maps almost 1:1 onto Pulsar's existing **component/attribute/`ComponentInstancesDB`** framework (see `CONVENTIONS.md` → "Units are component-bearing entities").

---

## 1. Organisational Hierarchy

Aurora ground forces are four nested tiers:

```
Ground Unit Class      ← a single soldier or vehicle "design" (analogous to a ShipDesign)
   │  (researched, built from components: weapons, armor, support gear)
   ▼
Formation Element      ← N identical Ground Unit Classes grouped (e.g. "120× Infantry")
   │
   ▼
Formation              ← multiple Elements that move as one unit; splits into Elements in combat
   │
   ▼
Formation Template     ← the blueprint a Formation is built from (like a saved loadout)
```

- **Movement** happens at the Formation level (a Formation loads/transports/drops as a whole).
- **Combat** happens at the individual-unit level — each soldier/vehicle in each Element rolls to hit.
- A single Formation **cannot be split across transport ships** — it must fit in one vessel's troop capacity.

---

## 2. Ground Unit Base Types

Each Ground Unit Class is built on one base type, which sets equipment slots, hit points, and fortification ceiling.

| Base Type | Equip Slots | Self-Fort | Max Fort | Hit Points | To-Hit modifier vs it |
|-----------|:-----------:|:---------:|:--------:|:----------:|:---------------------:|
| Infantry | 1 | 3 | 6 | 1 | 1.0 |
| Light Vehicle | 1 | 2 | 3 | 3 | 0.4 |
| Medium Vehicle | 2 | 2 | 3 | 4 | 0.6 |
| Heavy Vehicle | 2 | 2 | 3 | 6 | 0.8 |
| Super-Heavy Vehicle | 3 | 1.5 | 2 | 12 | 0.9 |
| Ultra-Heavy Vehicle | 4 | 1.25 | 1.5 | 24 | 0.95 |
| Static Weapon | 1 | 3 | 6 | 3 | 1.0 (immobile) |

Larger vehicles are **harder to hit** (lower to-hit modifier) but cost more and fortify less. Infantry is cheap, fortifies best, dies to one penetrating hit.

---

## 3. Components (Equipment)

A Ground Unit Class fills its slots with researched components. Every component carries: **Size (tons)**, **Armor-Penetration (AP)**, **Damage**, **Shots/round**, plus any special role.

### Weapons
| Component | AP | Dmg | Shots | GSP | Notes |
|-----------|:--:|:---:|:-----:|:---:|-------|
| Personal Weapons | 1 | 1 | 1 | 1 | Anti-infantry baseline |
| Crew-Served Anti-Personnel | 1 | 1 | 6 | 6 | High shots, low pen |
| Light/Medium/Heavy Anti-Vehicle | e.g. 4 | 4 | 1 | 16 (med) | High pen, single shot |
| Super-Heavy Anti-Vehicle | high | high | 1 | — | Super/ultra-heavy chassis only |
| Light/Med/Heavy/Super-Heavy Bombardment | 2 | 6 | 3 | 36 (heavy) | **Indirect fire** (Support position) |
| Light/Medium/Heavy Anti-Aircraft | — | — | — | — | Fires at ground-support fighters |

**GSP (Ground Support Points)** = `AP × Damage × Shots`. This is both the supply-consumption metric and the firepower metric.

### Support / Non-combat
| Component | Size | Effect |
|-----------|------|--------|
| Headquarters (HQ) | varies | Command capacity in tons. Infantry HQ ≈ 24,000 t; medium ≈ 45,000 t; high command ≈ 250,000 t. Over-capacity → bonuses scale down proportionally. |
| Forward Fire Direction (FFD) | — | Each FFD enables **1 orbital-bombardment support ship** to assist this formation. |
| Construction Equipment | 150 t | 0.05 Construction-Factory-Equivalent each (terraforming/building from the field). |
| Standard Logistics Module | 50 t | Carries 1,000 GSP. Light-vehicle+; can resupply *other* formations. |
| Small Logistics Module | 10 t | Carries 100 GSP. Infantry-only; resupplies *own* formation only. |
| CIWS | — | Close-in defence vs incoming missiles (planetary point defence). |
| STO (Surface-To-Orbit) | — | Energy weapon that fires at ships in orbit. |
| Geosurvey / Xenoarchaeology / Decontamination | 100 t | Field survey, ruin research, radiation cleanup (+~0.01%/yr). |

### Armor (on the unit)
- **Base Armor Rating** × unit size → construction cost contribution (6-armor unit costs ~50% more than 4-armor).
- **Racial Armor Rating** = base × faction's best armor tech.
- Armor tech ladder: Conventional Steel(1) → Composite(2) → Adv Composite(3) → Duranium(4) → High-Density Duranium(6) → Composite(8) → Ceramic Composite(10).
- Armor/HP/components **freeze at the tech level present at design time** (like Pulsar component designs — see `CONVENTIONS.md`).

### Genetic enhancement (infantry only)
| Tier | HP mult | Cost mult |
|------|:-------:|:---------:|
| Basic | ×1.25 | ×1.5 |
| Improved | ×1.6 | ×2.0 |
| Advanced | ×2.0 | ×2.5 |

---

## 4. Combat Resolution

**Cadence:** one combat round per ⚠️ **3 hours (wiki) / 8 hours (manual)** of game time, resolved after the naval phase, whenever hostile forces share a body.

### Field positions
| Position | Can target | Fortification | Notes |
|----------|-----------|---------------|-------|
| **Front-Line Attack** | any enemy position | forfeits all fort bonuses | doubled morale gain from kills; needs supply |
| **Front-Line Defence** | front-line enemies only | keeps fort bonuses | standard morale |
| **Support** | indirect-fire only | n/a | bombardment; med-artillery targets elements, heavy targets whole formations; fires alongside orbital bombardment |
| **Rear Echelon** | very limited | n/a | logistics; 5% target-size modifier (hard to hit) |

### Hit → penetrate → destroy (three rolls)
```
1. TO-HIT:
   FinalToHit = BaseToHit(≈20%) × TerrainMod × MoraleRatio × BaseTypeToHitMod
               ─────────────────────────────────────────────────────────────
                       Fortification × EnvironmentMod × DefensiveValue

2. PENETRATE (if hit):
   PenChance = (WeaponAP / TargetArmor)²        (auto if AP ≥ Armor)

3. DESTROY (if penetrated):
   DestroyChance = (WeaponDamage / TargetHP)²    (auto if Damage ≥ HP)
```
Destroyed elements are removed immediately. Fortification does **not** decrease from casualties, but **moving or loading a formation resets fortification to 0**, and units re-fortify to their self-fort level over **30 days**.

### Terrain
| Terrain | Fort mult | To-hit mult |
|---------|:---------:|:-----------:|
| Steppe / Barren | 1.0 | 1.0 |
| Desert | 0.75 | 1.0 |
| Swamp | 0.5 | 1.0 |
| Temperate Forest | 1.25 | 0.5 |
| Mountain | 2.0 | 0.5 |
| Jungle | 1.5 | 0.25 |
| Jungle-Mountain | 3.0 | 0.125 |
| Rift Valley | 1.5 | 0.75 |

Combat-capability specialisations (Mountain/Jungle/Desert Warfare, Extreme Temp/Pressure/Gravity, Boarding) each apply a 0.5× to-hit penalty *against* a specialised unit in that terrain (i.e. 2× effective accuracy for the specialist). They stack multiplicatively.

### Morale & breakthrough
- Max morale = `100 + 5 × commanderTrainingBonus%`.
- Effectiveness scales linearly: `Strength × Morale/100`.
- Breakthrough = `Cohesion × BreakthroughRating`; at ≥30% the attacker gets extra attacks.
- Morale recovers between rounds per commander training bonus; base post-drop recovery ≈ 100/yr.

---

## 5. Supply & Logistics

- Each unit carries **inherent supply for 10 combat rounds**.
- Consumption per 10 rounds = its total **GSP** (`AP×Dmg×Shots` summed over weapons).
- Out of supply → **25% fire rate** and **cannot take Front-Line Attack**.
- Resupply hierarchy: own logistics elements → parent formation → up the chain. Vehicle logistics can cross formations; infantry logistics cannot.
- Engaging ground **and** air simultaneously doubles consumption.
- **Maintenance:** 12.5% of build cost per year as a wealth expense, regardless of combat.

---

## 6. Transport, Drop, Boarding (the invasion pipeline)

### Troop transport bays (ship components)
| Bay | Ship Size (HS) | Capacity (t) | Cost (BP) |
|-----|:--------------:|:------------:|:---------:|
| Very Small | 2 | 100 | 3 |
| Small | 5 | 250 | 6 |
| Standard | 20 | 1,000 | 20 |
| Large | 100 | 5,000 | 80 |
| Very Large | 500 | 25,000 | 320 |

- Load/unload only at a body **with a colony marker**. Base ≈ 10 days for a brigade-sized load, reduced by cargo-handling systems / spaceport.
- A Formation must fit entirely in one ship.

### Orbital drop
- Only drop-capable units can combat-drop.
- Sequence: orbital superiority → bombard defences → combat-drop → reinforce after beachhead.
- **Drop-module morale decay:** non-cryo modules lose **1 morale/day while loaded**. Cryogenic drop modules eliminate the loss.
- **Drop casualties:** a % of elements lost on descent; enemy AA raises it; heavier units suffer more; ground-combat tech lowers base rate.

### Boarding
- Boarding-equipped transport bay + **all-infantry** formation with Boarding Combat capability.
- Target ship must not be faster than the attacker; fleet must end movement at the target.
- Boarding Combat doubles transfer success and doubles to-hit in internal combat.

### Capture & occupation
- On conquest: reparations (wealth/minerals), possible alien tech from research installations, **intact installations begin producing for the conqueror**, population becomes subject pop.
- **Required garrison** ≈ `Population(millions) × (Determination/100) × (Militancy/100)`.
- **Occupation strength** per element ≈ `sqrt(Size × Units × Morale) / 10,000`.
- Police-specialised light infantry suppress unrest cheaply (distinct from frontline formations).

---

## 7. Construction (building ground forces)

- Built at a colony with a **Ground Force Construction Complex (GFCC)**: ≈500,000 cargo points, ~2,400 Vendarite, ~1,000,000 workers, base **250 BP/yr** (researchable to 500 / 1,000).
- Process mirrors ship/installation construction: queue Formation Templates → BP accrues from colony mineral stockpile → completes when accrued BP = cost.
- v2.7 auto-builds **replacement units** for formations with assigned templates that took losses (priority value, default 10; higher = replaced first). Replacements substitute by **Unit Series**, enabling auto-upgrade.
- Unit-class development cost (RP) ≈ `sqrt(BPcost × 25000)` for non-STO ground units (vs `sqrt(BPcost × 5000)` for most ship components).

### Commander bonuses (ground)
GCD (defence/fort), GCO (direct-fire), GCA (artillery), GCAA (anti-air), GCL (logistics), GCM (manoeuvre/breakthrough), OCC (occupation), GCT (training/morale), plus SRV/XEN/DEC for field specialists. Over command-capacity → bonuses scale down.

---

## 8. Pulsar Mapping (how to build this in-engine)

Mirror Pulsar's ship pipeline. A ground unit is **an entity with `ComponentInstancesDB`**, exactly like a ship — its weapons/armor/support gear are components with `*Atb` attributes, queried via `TryGetComponentsByAttribute<T>()`.

| Aurora concept | Proposed Pulsar implementation | Reuses existing |
|----------------|-------------------------------|-----------------|
| Ground Unit Class | `GroundUnitDesign : IConstructableDesign` (parallel to `ShipDesign`) | `ComponentDesigner`, research/unlock |
| Unit components (weapons/armor/HQ/logistics) | `ComponentDesign`s with new `*Atb` types (`GroundWeaponAtb`, `GroundArmorAtb`, `LogisticsAtb`, `HeadquartersAtb`…) | `ComponentInstancesDB`, attribute query |
| Formation Element / Formation | `GroundForceDB` on a ground-force entity (holds element list, position, morale, fortification, supply) | DataBlob + copy-ctor `Clone()` |
| Formation Template | JSON blueprint under `Data/basemod/` + a `*Blueprint` class | mod loader |
| Combat round | `GroundCombatProcessor : IHotloopProcessor` (RunFrequency = combat cadence) | auto-discovery, hot-loop |
| To-hit / pen / destroy | a `GroundDamage` helper — **reuse the complex `DamageProcessor` path once it is fixed** (Phase 1), do not build a parallel damage system | `Damage/DamageComplex/` |
| Build at GFCC | new `IConstructableDesign` + `IndustryJob` subtype, GFCC is a component (`IndustryAtb`-like) on the colony | `Industry/`, `ConstructStuff()` |
| Transport bay / drop / boarding | new `INavAction`s appended to `NavSequenceDB`; troop bay = ship component | `Movement/NavSequence/` |
| Invade / drop / board orders | `EntityCommand` subclasses via `OrderableDB` | Orders system |
| Garrison / occupation / unrest | fields on `GroundForceDB` + a colony `OccupationDB`; feed `PopulationProcessor` | `Colonies/` |
| Orbital bombardment ↔ ground | the colony-damage branch in `DamageProcessor` (currently commented out, ~lines 101–181) | `Damage/` Phase 3 |
| Ground UI | `GroundForcesWindow` + a `PlanetaryWindow` tab; reuse `ComponentInstancesDBDisplay` for unit loadouts | `Pulsar4X.Client/` |

**Dependency note:** ground-combat damage must sit on the **complex `DamageProcessor`**, which is currently stubbed/placeholder (`SimpleDamage` is active instead). That is why PLAN.md Phase 1 (fix complex damage) is a hard prerequisite for ground combat — see root `CLAUDE.md` gotcha #1 and `Damage/CLAUDE.md`.
