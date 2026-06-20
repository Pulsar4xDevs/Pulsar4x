# Aurora 4X — Planetary Infrastructure & Economy (Design Reference)

Source: aurora-manual `6-economy-and-industry/` + `5-colonies/` (v2.7.1); AuroraWiki / Fandom installation pages.
Status: design reference — constants approximate, verify before hard-coding (see `INDEX.md`).

Pulsar already has a real economy core (mining, construction, population, components-as-installations). This doc captures Aurora's full model so we can see **which installations and mechanics Pulsar is missing** and add them in the existing `ComponentInstancesDB` + `Industry/` framework.

> **Critical Pulsar fact (verified in code):** In Pulsar, **installations ARE components.** A colony is an entity with a `ComponentInstancesDB`; mines, factories, refineries, labs, shipyards, etc. are added via `colonyEntity.AddComponent(design, count)` (see `DefaultStartFactory.cs:212-225`). The separate `InstallationsDB` class is **vestigial/abandoned** — never attached to any colony, no `[JsonProperty]` fields. Do **not** build new infrastructure on `InstallationsDB`. Build it as component designs with `*Atb` attributes. See `Industry/CLAUDE.md` and `Colonies/CLAUDE.md`.

---

## 1. The core economic loop

> "Construction factories building more construction factories is the fundamental economic growth loop." Early game: pour BP into factories + mines that feed them → compound growth.

```
Mines ──extract──► Mineral stockpile ──consumed by──► Construction Factories
                                                              │ produce
                                                              ▼
                          more factories, mines, installations, ground units,
                          ship components, ordnance, fighters, PDCs
```

---

## 2. Installations

Most installations need **~50,000 workers each** drawn from colony population; a colony short on population can't run them all at full rate.

### Production
| Installation | Build (BP) | Minerals | Workers | Output |
|--------------|:----------:|----------|:-------:|--------|
| Construction Factory | 120 | 60 Duranium + 60 Neutronium | 50,000 | 10 BP/yr (researchable) |
| Ordnance Factory | 120 | 120 Tritanium | 50,000 | 10 BP/yr (ordnance) |
| Fighter Factory (Light Naval) | 120 | 120 Vendarite | 50,000 | 10 BP/yr; any ≤1,000 t military design, no retool |
| Fuel Refinery | — | — | 50,000 | 20,000 L/yr (Sorium→fuel) |
| Financial Centre | — | — | 50,000 | +wealth |
| Ground Force Construction Complex | big | ~2,400 Vendarite | 1,000,000 | 250 BP/yr (researchable to 500/1,000) |

### Mining
| Installation | Build (BP) | Workers | Output |
|--------------|:----------:|:-------:|--------|
| Mine (manned) | 120 | 50,000 | 10 t/yr/mine (researchable) |
| Automated Mine | 240 | 0 | 10 t/yr/mine (no population) |
| Mass Driver | — | — | throws minerals between colonies |

### Research / Support
| Installation | Build (BP) | Notes |
|--------------|:----------:|-------|
| Research Lab | 2,400 | one project per lab; alerts each increment |
| Maintenance Facility | — | 50,000 workers; supports ~200 t of orbiting military ship maintenance each |
| Repair Yard | 1,200 (was 2,400) | 1,200 Duranium + 1,200 Neutronium; 10,000 t base; repairs only, no retool |
| Military Academy | — | trains/commissions commanders |
| Terraforming Installation | — | shifts atmosphere/temperature |
| Mass Driver / Spaceport | — | logistics throughput |

### Infrastructure & Population support
| Installation | Build (BP) | Role |
|--------------|:----------:|------|
| **Infrastructure** | **2 BP/unit** | Raises supportable population on worlds with **Colony Cost > 0** (hostile env). The more hostile, the more infrastructure per capita. |
| Forced Labour Construction Camp (C# only) | — | factory-speed output, cheaper, but **permanently consumes population** + creates unrest |
| Conventional Industry (CI) | starting | pre-TN; upgrade to TN installations ASAP |

### Conventional-Industry → Trans-Newtonian conversion (game start)
| Convert to | Minerals | BP |
|------------|----------|:--:|
| Construction Factory | 10 Duranium + 10 Neutronium | 20 |
| Mine | 20 Corundium | 20 |
| Fuel Refinery | 20 Boronide | 20 |
| Financial Centre | 20 Corbomite | 20 |
| Ordnance Factory | 20 Tritanium | 20 |
| Fighter Factory | 20 Vendarite | 20 |

CI is ~10× less efficient than TN equivalents (1 CI ≈ 1 BP/yr vs 10 BP/yr factory; 1 CI ≈ 1.5 t/yr vs 10 t/yr mine).

---

## 3. Construction mechanics

- Each construction factory adds a fixed **BP/yr** to the colony queue.
- Queue runs **top-to-bottom**, with optional **% capacity split** across simultaneous projects (Construction / Ordnance / Fighters categories each have unused-capacity display).
- Every item also costs **specific minerals**, consumed as built from the colony stockpile; if a mineral runs out, that item **pauses** until resupplied.
- Controls: Add / Priority / Pause-Resume / Cancel / Repeat.
- Construction-Rate research ladder (BP/yr/factory): 10→12→14→16→20→25→30→36→42→50→60→70, at rising RP costs (3k→…→2.5M).
- Component dev cost: most ship components `RP = sqrt(BP × 5000)`; missiles & non-STO ground units `RP = sqrt(BP × 25000)`.

---

## 4. Mining

- Body holds mineral deposits, each with **Amount** and **Accessibility (0–1)**.
- Manned mine base 10 t/yr (needs 50,000 pop); automated mine same rate, **no pop**.
- Accessibility scales effective rate; deposits **deplete permanently**.
- Mass Driver throws extracted minerals to another colony (e.g. mining outpost → homeworld).

---

## 5. Population & Colony Cost

- **Colony Cost (CC)** = how hostile a body is to a species (atmosphere, temperature, gravity, pressure). CC 0 = ideal/home; higher = needs more life support per capita.
- **Infrastructure** raises the population a CC>0 world can support. CC 0 worlds need none.
- Population grows toward the supportable cap; over-cap populations decline.
- Population is the workforce: each installation pulls ~50,000 workers; insufficient pop → installations run below capacity.
- Wealth/trade and unrest modulate growth and output (financial centres, civilian economy, governors).

---

## 6. Pulsar current state vs Aurora (the gap)

| Aurora mechanic | Pulsar status | Where |
|-----------------|---------------|-------|
| Installations as buildable items | ✅ exists, **as components** in `ComponentInstancesDB` | `DefaultStartFactory`, `ColonyFactory` |
| Construction queue (BP, minerals, priority) | ✅ exists | `Industry/IndustryProcessor`, `IndustryTools.ConstructStuff()` |
| Mining (deposits, accessibility, depletion) | ✅ exists | `Industry/MineResourcesProcessor`, `MineralsDB` |
| Population growth vs carrying capacity | ⚠️ partial (placeholder die-off, no governors/radiation) | `Colonies/PopulationProcessor` |
| Colony Cost driving infrastructure need | ⚠️ partial (`Species.ColonyCost`, life support) | `Colonies/` |
| Workforce: 50k workers/installation | ❓ verify whether enforced | `Industry/` |
| Fuel refinery / maintenance / financial centre / mass driver / military academy / terraforming | ❓ verify which exist as component designs in `Data/basemod/` | `Data/basemod/blueprints/components/` |
| Installation on/off employment | ⚠️ `InstallationsDB.EmploymentList` exists but blob is **dead**; real toggle (if any) is per-component | needs design decision |
| Forced labour / unrest / subject populations | ❌ none | new |
| **PlanetaryWindow installations UI** | ❌ broken — see below | `Pulsar4X.Client/Interface/Windows/PlanetaryWindow.cs` |

### The installations-UI gap (corrected understanding)
The prior recon said "`RenderInstallations()` is empty, fill it from `InstallationsDB`." That is wrong twice over:
1. `InstallationsDB` is never attached, so the **tab button itself never appears** — `RenderTabOptions()` gates it on `HasDataBlob<InstallationsDB>()` (`PlanetaryWindow.cs:107`) and `RenderInstallations()` gates again at line 221.
2. The real installation data is in `ComponentInstancesDB`, and a `ComponentInstancesDBDisplay` panel **already exists**.

**Correct fix:** change the gate to `ComponentInstancesDB` (which every colony has) and render via the existing `ComponentInstancesDBDisplay`, OR add a purpose-built installations panel that reads `ComponentInstancesDB.DesignsAndComponentCount`. Do not resurrect `InstallationsDB`. This is Phase 2a in `PLAN.md`.

---

## 7. Pulsar Mapping (new infrastructure work)

| Aurora concept | Pulsar implementation | Reuses |
|----------------|----------------------|--------|
| New installation type | new `ComponentDesign` + `*Atb` attribute + JSON blueprint in `Data/basemod/blueprints/components/` | component framework, mod loader |
| Installation output (refine/research/etc.) | a `*Processor : IHotloopProcessor` reading components via `TryGetComponentsByAttribute<T>()` | auto-discovery |
| Workforce requirement | a `WorkerRequirementAtb` + check in the processor / `PopulationProcessor` | `Colonies/` |
| Infrastructure for colony cost | extend `ColonyLifeSupportDB` / `Species.ColonyCost` interaction | `Colonies/` |
| Mass driver transfer | reuse `Logistics/` + `Movement/` | logistics |
| Installations UI | fix `PlanetaryWindow` gate → `ComponentInstancesDB`; reuse `ComponentInstancesDBDisplay` | client |

**Rule:** every new installation is a component design with an attribute and (if it does something each tick) a hot-loop processor. This is identical to how ship abilities work — see `CONVENTIONS.md`.
