# Aurora 4X — Research & Technology (Design Reference)

Source: aurora-manual `7-research/` (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** Research is how you unlock better gear. You build **research labs** (each makes ~200 "research points" a year), and you put a **scientist** in charge of a project. Scientists have a specialty (engines, weapons, etc.) and a cap on how many labs they can run. Match the scientist to their specialty and they work way faster — a 15%-in-his-field scientist beats a 40%-generalist. Techs come in chains: you research level 1 before level 2, and some unlock others. Early game, the smart move is "broad, not deep" — level 5 in lots of things beats level 12 in one thing, except for your main weapon and your engines (those are what win fights).

---

## 1. How research produces results

```
Time to finish = RP_Cost / (Labs_assigned × 200 × (1 + Scientist_Bonus))
```
- Each **research lab** ≈ **200 RP/year** base.
- A scientist multiplies that output by their bonus.
- More labs = faster, but only up to the scientist's **administration cap** (you can't assign more labs than the scientist can manage).

**Worked example:** 10 labs × 200 × (1 + 0.30 bonus) = **2,600 RP/year**.

---

## 2. Scientists

| Trait | Range | Meaning |
|-------|-------|---------|
| Research bonus | 5%–50% | flat speed multiplier (higher is rarer) |
| Primary field | one category | **bonus is quadrupled in-field** (20% → effectively 80%, i.e. 1.8× output) |
| Secondary field | optional | normal bonus |
| Administration rating | 5–45+ (steps of 5) | max labs they can run |

**The in-field multiplier is huge.** A 25% scientist gets ~2.0× output *in their specialty* but only 1.25× outside it. Always match scientist → project field. Reassigning is free (no penalty).

Research also costs **wealth**, scaled by species research-speed: `Wealth per RP = 1 / Research_Speed` (a 50%-speed species pays 2× wealth and takes 2× as long).

---

## 3. The tech tree

**Nine categories:**
1. Power & Propulsion
2. Sensors & Control Systems
3. Direct Fire Weapons (energy *and* kinetic)
4. Missiles
5. Construction / Production
6. Logistics
7. Defensive Systems
8. Biology / Genetics
9. **Ground Combat**  ← the category our objective lives in

- Techs form **chains** (level 1 → 2 → 3 …); some need prerequisites from *other* categories.
- A few techs have no prerequisites and are available at game start.
- Costs climb steeply per level. Example — engines, 15 levels, ~25–28% more power each level (roughly doubles every 3 levels):

| Engine tech | RP | Power/HS |
|-------------|----|---------:|
| Conventional | 500 | 1.0 |
| Nuclear Thermal (start TN) | 2,000 | 6.4 |
| Ion Drive | 10,000 | 12.5 |
| Magnetic Confinement Fusion | 40,000 | 20.0 |
| Solid-Core Antimatter | 150,000 | 32.0 |
| Photonic | 2,500,000 | 80.0 |
| Quantum Singularity | 5,000,000 | 100.0 |

(v2.8.0 flattened very-high-tech costs so super-expensive lines cost less than the old straight doubling.)

---

## 4. Prototypes (designing ahead of your tech)

Components can be built as prototypes before/at your tech level:

| State | Tag | Meaning |
|-------|-----|---------|
| Normal | — | researched, production-ready |
| Current Prototype | (P) | uses current tech, design-only until built |
| Future Prototype | (FP) | uses next-tier tech you don't have yet |
| Research Prototype | (RP) | turned into an actual research project |

Gotcha: after a prototype's research finishes you must "Refresh Tech," or the design won't show up in the shipyard.

Player-designed component research cost scales with the research-speed setting (`Adjusted = Base × Research_Speed`), unlike the fixed predefined techs.

---

## 5. Pulsar status & mapping

Pulsar **already has** research (`GameEngine/Tech/` — research points, scientists, tech unlocks; `ResearchWindow` in the UI). This is a **benchmark**, not new work. Relevance to the objective:

| Aurora idea | Pulsar | Note |
|-------------|--------|------|
| 9 categories incl. **Ground Combat** | `Tech/` | Ground-unit components must be **researchable** like ship components — hook new ground `*Atb` component designs into the existing research/unlock path (`ComponentDesigner`, `FactionInfoDB.Data.Unlock/IncrementTechLevel` seen in `ColonyFactory`). |
| Tech gates a component | `Tech/` + components | A new "Heavy Anti-Vehicle weapon" is just a component design behind a research node — no new system needed. |
| Scientist bonuses | `People/` + `Tech/` | exists; ground research reuses it. |

**Takeaway:** every ground-combat unlockable (weapons, armor tiers, drop modules, genetic enhancement) should be a **research node + component design**, riding the existing tech system. See `CONVENTIONS.md` §6 and §13.
