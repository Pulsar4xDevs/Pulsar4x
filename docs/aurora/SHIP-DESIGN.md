# Aurora 4X — Ship Design: Hull, Armor, Engines (Design Reference)

Source: aurora-manual `8-ship-design/` (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** A ship is a tonnage budget. Everything you bolt on — engines, weapons, armor, fuel — eats tonnage, and the total can't exceed the hull. **Engines** give speed, but speed is a tug-of-war: more engine = faster, more weight = slower. **Armor** is like layers of belt plating wrapped around the hull; a shot has to chew straight down through a column of plates before it reaches your guts. Wider ships have more armor columns but are easier to hit. The whole craft of ship design is spending a limited tonnage budget on the right mix of go-fast, hit-hard, and don't-die. This matters to us because **ground units are designed the exact same way** — a tonnage/slot budget spent on weapons vs armor vs support (see `GROUND-COMBAT.md`).

---

## 1. Hull size & tonnage

- **1 Hull Space (HS) = 50 tons.** Ships are built in 50-ton steps.
- All components' tonnage must fit within the hull size.
- A **Bridge** (min 1 HS) is mandatory.
- Bigger ship = easier to detect (more heat) and **gets hit more often** in combat.
- **Light Naval (≤1,000 t):** full commander bonuses; ships >1,000 t get **half** bonuses for crew training, survey, engineering, tactical, carrier ops, ground support.

---

## 2. Armor — the layered grid (most relevant to our damage model)

> Armor is modeled as a **grid**: width = number of hull spaces, depth = number of layers you choose. A shot lands on one column and must destroy every layer in that column before it reaches internal components.

- Each grid cell has hit points = the armor tech's **strength per HS**.
- Wider hull → more columns → damage gets spread thinner.
- Armor weight grows **linearly** with layers, but speed drops **non-linearly** (more armor → slower, fast).

**Armor tech ladder (strength per HS):**
| Tech | Str | Tech | Str |
|------|----:|------|----:|
| Conventional Steel | 1 | Crystalline Composite | 21 |
| Duranium | 4 | Superdense | 25 |
| Ceramic Composite | 10 | Bonded Superdense | 30 |
| Compressed Carbon | 15 | Collapsium | 45 |

**Rules of thumb (layers):** 0 = non-combat; 2–3 = escorts; 4–5 = cruisers; 6–8 = battleships; 9+ = fortress (huge hulls only).

**Damage spread templates** — different weapons dig differently. The "gradient" is how sharply damage concentrates into the center column:
| Weapon | Gradient | 25-damage shape |
|--------|:--------:|-----------------|
| Missiles / Carronades | 1 (wide, shallow) | 1,2,3,4,5,4,3,2,1 |
| Railguns / Particle Beams | 2 | 1,3,5,7,5,3,1 |
| Lasers | 3–4 (narrow, deep) | 3,6,8,5,3 |
| Particle Lance | — | single column only |
| Gauss Cannon | — | 1 dmg, 1 column |
| Meson | — | bypasses shields; 40%/layer chance to stop |

This is the **same penetration-vs-armor idea** ground units use (AP vs armor, then HP). See `GROUND-COMBAT.md` §4 and `Damage/CLAUDE.md` — both should run on Pulsar's complex `DamageProcessor`.

---

## 3. Engines & speed

```
Speed (km/s) = Total_Engine_Power × 50,000 / Ship_Tonnage
            = Total_Engine_Power × 1,000 / Ship_Size_HS
```
Speed is a **ratio** — add engine power to go faster, add any weight (armor/weapons/cargo) to go slower.

- **Engine power** climbs with tech (Conventional 1.0 → Nuclear Thermal 6.4 → Ion 12.5 → Fusion 20–25 → Antimatter 32–64 → Photonic 80 → Singularity 100, EP per HS).
- **Durability:** `Engine HTK = SQRT(size in HS)` — so **many small engines survive better** than one big one (4×1HS = 4 HTK total vs 1×4HS = 2 HTK).
- **Fuel efficiency:** `Fuel modifier = SQRT(10 / size)` — bigger engines sip fuel; tiny engines are thirsty.
- **Power vs fuel tradeoff** — boosting an engine above 100% power burns fuel *exponentially*:
  | Power | Fuel use |
  |------:|---------:|
  | 0.5× | 0.32× |
  | 1.0× | 1.0× |
  | 1.5× | 3.05× |
  | 2.0× | 9.53× |
- **Commercial engine** (≤50% power *and* ≥25 HS): 10× bulkier but no maintenance failures — used on freighters/colony ships. Military engines can push power up to 10× modifiers.
- Engine total power → **thermal signature** (ties back to `SENSORS-AND-DETECTION.md`).

---

## 4. Shields (quick note)

- `Shield HTK = floor(SQRT(size in HS))` — bigger generators give more protection per ton.
- Raised shields emit EM = `3 × strength` (you light up when shields are up).
- C# shields use no fuel; built from Corbomite.

---

## 5. Pulsar status & mapping

Pulsar **already has** ship design (`GameEngine/Ships/`, `ComponentDesignWindow`, `ShipDesignWindow`) and a layered-damage model in the (currently stubbed) complex `DamageProcessor` + `EntityDamageProfileDB`. This is a **benchmark**, not new work. Why it's in scope:

| Aurora idea | Pulsar | Relevance to objective |
|-------------|--------|------------------------|
| Design from a tonnage/slot budget | `ShipDesign`, `ComponentDesign` | **Ground unit design copies this** — `GroundUnitDesign : IConstructableDesign`, slots filled with component designs. |
| Layered armor + penetration | `Damage/DamageComplex/` (stubbed) | Ground AP-vs-armor uses the **same damage core**. Fix it once (PLAN Phase 1), use it for both. |
| Engines/speed ratio | `Movement/NewtonMove/` | ground units don't need this (they move at tactical scale — `Movement/CLAUDE.md`). |

**Takeaway:** ship design is the working template the objective points at. Mirror its *structure* (design → components → factory → damage), in Pulsar's idiom, for ground forces. See `CONVENTIONS.md` and `GROUND-COMBAT.md` §8.
