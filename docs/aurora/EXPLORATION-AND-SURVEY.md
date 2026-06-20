# Aurora 4X — Exploration & Survey (Design Reference)

Source: aurora-manual `17-exploration/` (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** A new system starts mostly unknown. You send survey ships to do two jobs. **Geological survey** scans planets/moons/asteroids to find **minerals** (and how easy they are to dig out) — this is what feeds your whole mining-and-industry economy. **Gravitational survey** scans empty points in space to find **jump points** — the doorways that connect one star system to the next. So geo survey = "what can I mine here," grav survey = "where can I go next." This matters to us because **mineral discovery feeds the infrastructure/economy** side of the objective, and Pulsar already has a geo-survey subsystem to build on.

---

## 1. Geological survey (finding minerals)

Each body needs a number of **survey points** based on size:
| Body | Points |
|------|-------:|
| Large planet | 500+ |
| Terrestrial planet | 200–400 |
| Dwarf planet / moon | 50–200 |
| Asteroid / comet | 10–50 |

**Sensors** generate points per hour: Base 1/hr (24/day) → Improved 2 → Advanced 3 → Phased 5. Multiple sensors add up. `Time = PointsNeeded ÷ PointsPerDay` (a 300-point planet at 30/day = 10 days).

**A finished survey reveals:** every mineral type present, the **amount** (tons), the **accessibility** (0.1–1.0), and any **alien ruins**.

- **Accessibility** multiplies mining speed — 1,000,000 t at 0.3 mines at 30% rate. As you dig, accessibility drops (easy ore first).
- Only **~5%** of bodies have minerals; **Duranium** is twice as likely as others; gas giants have only Sorium.
- **Automated** standing orders survey batches of 5 or 30 bodies; ships won't chase targets past ~10 billion km.
- **Ground-based survey** (a vehicle component, 0.1 pts/day) works on bodies >4,000 km; a body's "survey potential" (None 60% … Excellent 1%) can grant accessibility bonuses.

---

## 2. Gravitational survey (finding jump points → new systems)

Each system has **~30 survey locations** to check; **2–8 jump points** hide among them (many locations are empty).

**Sensors:** Base 1/hr (2,000 RP) → Improved 2 (10,000) → Advanced 3 (35,000) → Phased 5 (100,000).

- A jump point is a **two-way door** between exactly two systems; position is **permanent** once found.
- Surveying out a brand-new system's last unknown jump point guarantees that system has ≥2 jump points (so the galaxy stays connected).
- Standing grav-survey orders ignore locations past ~10 billion km.

(Jump points are how fleets — including invasion fleets — travel between stars; the transit mechanics live in `Movement/`.)

---

## 3. Pulsar status & mapping

Pulsar **already has** survey: `GameEngine/GeoSurveys/` (geo survey ability + processor) and `GameEngine/JumpPoints/` (jump points, `JPSurveyFactory`, inter-system travel — see `Movement/CLAUDE.md`). This is mostly a **benchmark**, with one real tie to the objective:

| Aurora idea | Pulsar | Relevance to objective |
|-------------|--------|------------------------|
| Geo survey reveals minerals + accessibility | `GeoSurveys/`, `Industry/MineralsDB` (exist) | the **front of the economy pipeline** that infrastructure depends on — already wired (`MineralsDB { Amount, Accessibility }`, see `Industry/CLAUDE.md`) |
| Grav survey reveals jump points | `JumpPoints/`, `GeoSurveys/` (exist) | how invasion fleets reach a target system; reuse, don't rebuild |
| Ground-based survey component | maybe partial | a ground-unit survey component (`GROUND-COMBAT.md` §3) reuses the same survey-points idea |

**Takeaway:** survey is largely done in Pulsar. The only ground-combat touchpoint is the **ground survey component** on units, which should reuse the existing survey-point mechanic rather than a new one. `CONVENTIONS.md` §6.
