# Aurora 4X — Colony Cost, Population & Terraforming (Design Reference)

Source: aurora-manual `5-colonies/` (5.2, 5.3, 5.5) (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** A planet is rarely a perfect home. **Colony Cost (CC)** is one number that says "how hostile is this rock to my people" — 0.0 means Earth-perfect, higher means worse (too cold, no air, poison gas, low gravity). The worse it is, the more **infrastructure** you must ship in to support each person, and the fewer people can live there. **Terraforming** slowly fixes the air and temperature to drive CC back toward 0. **Population** grows on its own up to a cap set by infrastructure and planet size, and your people are your **workforce** — every factory needs bodies to run it. After you *conquer* a world, you need **garrison** troops to keep the locals from revolting. This doc is core to the infrastructure half of our objective, and it hands us the occupation math ground combat needs.

---

## 1. Colony Cost (how hostile a world is)

**Key rule: only the single worst factor counts** — costs do NOT stack. CC = the highest of:

| Factor | When it bites | Cost |
|--------|---------------|-----:|
| **Temperature** | outside species band (humans −10…38°C) | `°outside / (range/2)` — biggest driver; e.g. −60°C ≈ 2.08, 200°C ≈ 6.75 |
| **Pressure** | over species max (4.0 atm) | 2.0+, scales if way over |
| **Breathable gas** | O₂ <0.1 atm or >30% of atmosphere | flat 2.0 |
| **Dangerous gas** | per-gas ppm threshold (CO₂ 5000ppm, halogens 1ppm…) | +2.0 (+3.0 halogens); worst one only |
| **Gravity** | below species min (humans 0.1g) | +1.0 & double infrastructure; above max = **can't colonize** |
| **Hydrosphere** | <20% water | linear 0→2.0 as water 20%→0% |

- **Tide-locked worlds:** 20% of normal temperature cost.
- **Gravity can't be terraformed** — a too-heavy world is permanently off-limits; too-light is permanently +1.0.

**Surface temperature** (the thing terraforming changes):
```
Temp(K) = BaseTemp(K) × GreenhouseFactor × Albedo / AntiGreenhouseFactor
Greenhouse = 1 + Pressure/10 + greenhouse-gas pressure   (cap 3.0; gases: CO₂,CH₄,NO₂,SO₂,Aestusium)
AntiGreenhouse = 1 + anti-greenhouse-gas pressure         (cap 3.0; Frigusium + bombardment dust)
Albedo = base + 0.0015 × Hydro%                           (max +0.15)
```

---

## 2. Population & carrying capacity

```
Max capacity      = (BodyArea / EarthArea) × 12,000,000,000
Max SUPPORTED pop = Infrastructure / (ColonyCost × 100)   (in millions)   ← the infrastructure cap
```
- Growth is normal up to **33% of capacity**, then declines linearly to **0 at capacity**.
- A CC-0 world needs **no** infrastructure; any CC>0 world needs `Infra = MaxPop(millions) × CC × 100` to support that many people.
- Floor: any non-gas-giant within gravity tolerance supports ≥50,000.
- >75% water shrinks capacity (down to 1% at 100% water); tide-locked = 20% capacity.
- Typical growth ~1–2%/yr (2% doubles pop in ~35 yrs), modified by infrastructure, governor, overcrowding, environment.

---

## 3. Workforce (population = workers)

~60% of population is available workforce. Some are tied up by hostility:
```
Agriculture/Environment workers = 5% + (5% × ColonyCost)
```
| CC | farm/env | industrial |
|---:|---------:|-----------:|
| 0 | 5% | 95% |
| 2 | 15% | 85% |
| 4 | 25% | 75% |
| ≥5 | rising | → 0 (extreme worlds can't industrialize at all) |

**Worker demand per installation** (this is the "50,000 rule," refined):
| Tier | Workers | Examples |
|------|--------:|----------|
| Very high | 1,000,000 | Ground Force Construction Complex, Research Facility |
| High | 125k–250k | Terraforming (125k), Genetic Mod Centre (250k) |
| Medium | 50,000 | Construction Factory, Mine, Ordnance/Fuel/Maintenance/Financial |
| Low | 5,000 | Forced Labour Camp |
| None | 0 | Infrastructure, Automated Mine, Military Academy, Mass Driver, Deep Space Tracking |

---

## 4. Terraforming (driving CC toward 0)

- **Ground installation:** 50,000 t, 125,000 workers, 300 BP, base **0.00025 atm/yr** (→ 0.00375 at top tech). **Space module:** 100 HS, 500 BP, no population, can terraform uncolonized bodies.
- Smaller worlds terraform faster (Mars ~3.5×, Luna ~13.5× Earth rate).
- Add gases (N₂ filler, O₂ breathable, CO₂/CH₄/Aestusium warming, Frigusium cooling, H₂O water) or remove them (toxics, excess CO₂/O₂). Target ≈ 0.78 N₂ / 0.21 O₂ / trace CO₂ / some water = CC 0.
- Timelines: trace toxics 5–20 yrs; full Mars 20–100 yrs; Earth/Venus-class 100–500+ yrs.
- **Nuclear bombardment cooling:** warhead dust adds `Dust/20,000` to anti-greenhouse factor (20,000 dust = 1.0 cooling); decays ~10%/yr. (This ties orbital bombardment to terraforming/atmosphere — relevant to Phase 3.)

---

## 5. Unrest & occupation (the ground-combat aftermath)

After conquest you must hold the population down:
```
Required occupation strength = ((Determination + Militancy + Xenophobia)/300)
                               × Population × PoliticalStatusModifier
```
| Political status | Modifier |
|------------------|---------:|
| Slave colony | 1.5 |
| Conquered | 1.0 |
| Occupied | 0.75 |
| Subjugated | 0.25 |

- **Police/garrison** reduce unrest: `UnrestReduction = 100 × (PoliceStrength / EffectivePop)` per year.
- Unrest 1–10 negligible, 10–39 minor, 40+ production penalties.
- **Forced Labour Camp:** 40 BP, outputs like one factory/mine, consumes 100,000 pop up front + 5,000 workers, **+5 unrest**, produces no wealth.

---

## 6. Pulsar status & mapping

This is **directly core to the objective.** Pulsar has a partial version; this doc supplies the real formulas to finish it.

| Aurora mechanic | Pulsar now | What it tells us |
|-----------------|-----------|------------------|
| Colony Cost = worst single factor | `Species.ColonyCost(planet)` (exists, see `Colonies/CLAUDE.md`) | verify it uses "worst factor," not a sum |
| **MaxPop = Infrastructure / (CC × 100)** | `PopulationProcessor` uses `popSupport / needsSupport / colonyCost` (approx) | **this is the target formula** to replace the stub; PLAN Phase 2c |
| Growth normal→0 from 33%→100% capacity | placeholder −50% die-off | **replace the −50% placeholder** with the linear-decline curve (root `CLAUDE.md`, `Colonies/CLAUDE.md` note the stub) |
| Workforce = pop, per-installation worker demand | verify if enforced | infrastructure depth (PLAN Phase 2) |
| Terraforming | not implemented | new installation-component + a `TerraformingProcessor` (`CONVENTIONS.md` §6); ties to `AtmosphereDB` |
| **Occupation/garrison/unrest** | not implemented | **ground-combat aftermath** — build alongside invasion (PLAN Phase 4); a conquered colony gets a political-status + unrest state, garrison troops suppress it |
| Bombardment dust → cooling | `Damage/` colony block (commented out) | links orbital bombardment to atmosphere (Phase 3) |

**Takeaway:** two concrete wins here. (1) The population stub has a real target formula now — `MaxPop = Infrastructure/(CC×100)` with the 33%→100% growth curve. (2) Occupation/unrest is the missing back-half of invasion: taking a colony isn't the end, *holding* it (garrison vs. unrest) is the game. Both fit existing subsystems — see `CONVENTIONS.md` §6.
