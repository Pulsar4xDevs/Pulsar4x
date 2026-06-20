# Aurora 4X — Sensors & Detection (Design Reference)

Source: aurora-manual `11-sensors-and-detection/` (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** Every ship is a bit like a submarine. It makes "noise" two ways — **heat** (thermal, from running its engines) and **radio noise** (EM, from turning on its radar or raising its shields). Enemies have two kinds of "ears": **passive** sensors that listen for your noise, and **active** sensors (radar) that ping out and listen for the echo. Radar finds *silent* targets, but turning it on is like switching on a floodlight — everyone sees *you* from much farther than you can see them. So combat is a game of who detects whom first, and good captains stay quiet (engines low, radar off, shields down) until they choose to be seen.

---

## 1. Signatures — the "noise" a ship makes

### Thermal (heat from engines)
```
Thermal Signature = Total Engine Power / Engine Thermal Reduction
Moving:  Thermal = (Current Speed / Max Speed) × Max Thermal
Idle:    Thermal = 5% of ship size in HS  (= 0.1% of tonnage)  ← floor, can't go lower
```
- A 10,000-ton ship (200 HS) idles at thermal **10** even with engines off. There is **no way to reach zero heat.**
- Power plants and shields add **no** heat. Engines are the only real source.
- Slowing down cuts heat proportionally (half speed = half heat, down to the idle floor).

### EM (radio noise from active systems)
| Source | EM output |
|--------|-----------|
| Active sensor (radar on) | `Strength × Size(HS) × Resolution` (the "GPS" value) |
| Shields (raised) | `3 × Shield Strength` |
| ECM / jammers | (unverified) |

A cruiser with a big search radar (GPS ~20,000) and shields up (~450) screams at ~20,450 EM. Turn both off → EM drops to **zero** (heat remains).

### Thermal-reduction tech (quieter engines, at a fuel cost)
Research buys engine heat multipliers from 1.00× down to 0.01× (1,500 RP for 0.75× … 2,500,000 RP for 0.01×). Quieter engines burn proportionally more fuel.

---

## 2. Passive sensors (listening)

- Detect a target's **thermal** or **EM** signature. No emission of their own — using them never gives you away.
- Bigger signature = detected from farther. Even an idle warship is found at short range by its heat floor.
- This is the "stay alive" sensor: you run passives all the time.

---

## 3. Active sensors (radar — pinging)

Finds even silent, drifting, fully-dark ships — but lights you up on enemy EM.

**Detection range (full C# formula):**
```
Range = SQRT( (Active_Strength × HS × EM_Sensitivity × Resolution^(2/3)) / PI ) × 1,000,000 km
```

**Resolution vs target size** — the key idea. A sensor has a "resolution" tuned to a target size. Against anything *smaller* than that, range shrinks:
```
Effective Range = Rated Range × SQRT(Target_Size / Resolution_Size)
```
Example (resolution 100 HS = 5,000 t, rated 300M km):

| Target | Range |
|--------|-------|
| 25,000 t | 300M km (full) |
| 5,000 t | 300M km |
| 2,500 t | 212M km |
| 1,000 t | 134M km |
| 500 t | 95M km |
| 50 t (missile) | 30M km |

So you need **different radars for different jobs:**
| Radar type | Resolution | Job |
|------------|-----------|-----|
| Fleet search | 100–500 HS | spot big warships far away |
| Missile-detection | 1 HS | short range, but the only way to see incoming missiles |
| Fire control | matched to target | guide weapons onto one target at a time |

Active sensors ≤1 HS are "commercial" components; larger are "military."

---

## 4. Staying hidden (EMCON / stealth)

1. **EMCON** — turn off active sensors & emitters → EM to zero (heat stays if engines run).
2. **Slow down** → less heat.
3. **Stop** (no move orders) → heat drops to the idle floor.
4. **Cloaking devices** (researched) → % cut to both heat and EM.
5. **Design** — don't over-build engines; keep radar off until you commit.

---

## 5. Pulsar status & mapping

Pulsar **already has** a sensors subsystem (`GameEngine/Sensors/` — EM/thermal signatures, scanning, contacts). This doc is mostly a **benchmark**, not new work. Relevance to the objective:

| Aurora idea | Pulsar | Note for ground combat |
|-------------|--------|------------------------|
| Thermal/EM signatures | `Sensors/` (exists) | Ground units & installations could carry signatures too (STO guns, active radar on the ground) — reuse the same signature model rather than inventing one. |
| Active vs passive detection | `Sensors/` | A planet's ground forces being "detected from orbit" can reuse contact logic. |
| Resolution vs target size | `Sensors/` | Same math applies to spotting small ground craft. |

No separate Pulsar mapping table needed — when ground forces need detection, **reuse `Sensors/`** rather than building a parallel system (the `CONVENTIONS.md` §6 rule again: don't duplicate an existing framework).
