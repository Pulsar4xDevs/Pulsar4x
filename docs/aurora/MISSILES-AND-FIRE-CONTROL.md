# Aurora 4X — Missiles, Point Defense, Electronic Warfare (Design Reference)

Source: aurora-manual `12-combat/` (12.3–12.5) (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** Missiles are tiny ships you design yourself — you split a size budget between **engine** (speed), **warhead** (damage), **fuel** (range), and **sensors/guidance** (hitting a moving target). Fast missiles both catch the target *and* are likelier to hit. The defender shoots them down with **point defense** — short-range guns (CIWS/gauss) and anti-missile missiles — so attackers fire **big salvos** to overwhelm the defense and let a few "leakers" through. **Electronic warfare** is the jamming layer: jammers cut the enemy's accuracy and detection range, and the enemy's "ECCM" cancels jamming level-for-level. This is mostly here as the **depth benchmark** — Pulsar already has missiles, though its guidance code is half-finished (a known gotcha).

---

## 1. Missile design

- Measured in **MSP** (1 MSP = 2.5 t), up to 200 MSP. A launcher fires its own size or smaller.
- **Speed = EnginePower ÷ MissileSize.** Engines usually take ~50% of the budget.
- **Warhead damage = WarheadSize × WarheadTech** (Gun-Type Fission 2× → Gravitonic 30×). Square numbers (1,4,9,16,25) are optimal for the armor damage pattern.
- **Range = Fuel ÷ (ConsumptionRate × Speed)**; each MSP of fuel = 2,500 units. Out of fuel → flies straight, can't maneuver.
- **Guidance options** (each eats MSP): active seeker (corrects for target movement), Home-on-Jam, passive thermal/EM, decoys, ECM/ECCM, multiple warheads, retargeting AI.

**Hit chance = 0.1 × (MissileSpeed ÷ TargetSpeed)**, then × Active-Terminal-Guidance multiplier (+15%…+60%). Example: 30,000 vs 5,000 km/s = 60% base; ×1.5 ATG = 90%.

**Launchers:** reload time = `SQRT(size) × 30s ÷ ReloadTech`. **Box launchers** = single-shot tubes (half size, no magazine, fire all at once, but reload only at base; explode if hit while loaded). Missiles stored in **magazines**.

---

## 2. Point defense (shooting missiles down)

| Mode | Range | Notes |
|------|-------|-------|
| Point-Blank | 10,000 km | last-ditch, high hit chance |
| Ranged Defensive Fire | full FC range | engage incoming in the envelope |
| Area Defence | full FC range | shoot the closest threat |
| AMM (anti-missile missiles) | long | needs resolution-1 fire control |

**CIWS/Gauss** fire many 1-damage shots (1 hit kills a normal missile). Core formula:
```
Hit chance = min(1.0, FC_Tracking_Speed / Missile_Speed) × crew × ECM/ECCM × …
```
If your tracking is slower than the missile, accuracy drops proportionally — so PD must keep pace with missile-speed tech.

**Layered defense** multiplies survival: 100 missiles through 40%/20%/25%/50% layers → only 18 hit (vs 50 with CIWS alone). **This is why salvos must be big** — PD kills a fixed number per tick; a 50-missile salvo swamps a PD that kills 10/tick.

---

## 3. Electronic warfare (v2.2.0+)

Three independent jammer types, each countering one thing:
| Jammer | Hurts | 
|--------|-------|
| Sensor Jammer | enemy active sensors & missile fire controls (cuts detection/lock range) |
| Fire Control Jammer | enemy beam FCs, CIWS, STO (cuts hit chance) |
| Missile Jammer | enemy missile guidance |

**One formula for all:**
```
ECM Penalty = max(0, JammerLevel − TargetECCMLevel)
Effect = 1 − (ECM Penalty × 0.1)        // penalty 10+ = total denial
```
ECCM is now baked into fire controls/sensors (10% of their cost per level), not a separate part. Home-on-Jam missiles turn a sensor jammer into a beacon.

---

## 4. Pulsar status & mapping

Pulsar **already has** space combat: `GameEngine/Weapons/` (beams, missiles, generic fire control) and `OrdnanceDesignWindow` for missile design. See `Weapons/CLAUDE.md` and `SPACE-COMBAT-BENCHMARK.md`. **Known gotcha:** Pulsar's missile **guidance is half-finished** — `MissleProcessor` hardcodes `directAttack = false` and uses orbital phasing instead of direct pursuit (root `CLAUDE.md` gotcha #3, `Movement/CLAUDE.md`).

| Aurora idea | Pulsar | Relevance to objective |
|-------------|--------|------------------------|
| Missile design from a size budget | `Weapons/WeaponMissile/`, `OrdnanceDesignWindow` (exist) | benchmark; same "design from parts" idea as ground units |
| Point defense layers | partial | benchmark — not core to ground combat |
| Electronic warfare | verify presence | benchmark |
| Missiles vs ground (bombardment) | `Damage/` colony block (commented out) | **relevant:** orbital missile bombardment of colonies is PLAN Phase 3 — see `GROUND-COMBAT.md` §6 |

**Takeaway:** missiles/PD/EW are benchmark depth, mostly built. The one ground-combat touchpoint is **missile bombardment of ground forces/colonies** (Phase 3), which rides the complex `DamageProcessor`. Fixing missile guidance is separate optional cleanup (gotcha #3), not on the ground-combat critical path.
