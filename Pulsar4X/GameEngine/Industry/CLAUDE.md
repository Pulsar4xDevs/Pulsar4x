# Industry — Subsystem Reference

Production, mining, and material processing. Lives in `GameEngine/Industry/`.

---

## File Map

| File | Purpose |
|------|---------|
| `IndustryAbilityDB.cs` | DataBlob: production lines with job queues. Attached to colonies and possibly ships with fabrication bays. |
| `IndustryAtb.cs` | Component design attribute that grants a production line with a given build-point rate. |
| `IndustryJob.cs` | One production job: what to build, how many, priority, repeat flag. |
| `IndustryOrder.cs` | Player order to add/remove/reprioritize production jobs. |
| `IndustryProcessor.cs` | `IHotloopProcessor` (daily). Calls `IndustryTools.ConstructStuff()`. |
| `IndustryTools.cs` | Static helpers: `ConstructStuff()`, `AddJob()`, `EditExsistingJob()`, `CancelExsistingJob()`, `ChangeJobPriority()`. |
| `InstallationsDB.cs` | **DEAD/vestigial DataBlob** — never attached to any colony, no `[JsonProperty]`. Installations are really components in `ComponentInstancesDB`. Do not build on this. |
| `InfrastructureDB.cs` | **NEW (DevBranch)** DataBlob on a colony. Tracks `CapacityProvided` (from infra installations) and `CapacityRequired` (from all other installations). `Efficiency` = Provided/Required, capped at 1.0. |
| `InfrastructureCapacityAtb.cs` | **NEW (DevBranch)** Component design attribute. Marks a component as "infrastructure." `Capacity` = units of support provided per installed unit. Filtered by gravity/pressure tolerance. |
| `InfrastructureProcessor.cs` | **NEW (DevBranch)** Static class. `RecalcCapacity(colony)` — sums provided vs required capacity and writes to `InfrastructureDB`. Called on `ComponentInstancesDB` recalc (whenever an installation changes). `GetEfficiency(colony)` — returns the output multiplier. |
| `JobBase.cs` | Abstract base for job types. |
| `MineResourcesAtbDB.cs` | Component attribute DataBlob: grants mining ability (which minerals, at what rate). |
| `MineResourcesProcessor.cs` | `IHotloopProcessor` (daily). Runs `MiningHelper.CalculateActualMiningRates()` and transfers minerals. |
| `MineralsDB.cs` | DataBlob on a planet: mineral deposits (type → `MineralDeposit { Amount, Accessibility }`). |
| `MiningDB.cs` | DataBlob on a colony: active mining configuration. |
| `MiningHelper.cs` | `CalculateActualMiningRates()` — determines effective mining rate per mineral per colony. |
| `MineralDepositFactory.cs` | Creates mineral deposits during system generation. |
| `Mineral.cs` | Mineral type definition (name, ID). |
| `ProcessedMaterial.cs` | Processed material definition (refined output from raw minerals). |

---

## Production System

### Data Model

`IndustryAbilityDB` holds a dictionary of named `ProductionLine` objects, each with:
- `BuildPoints` — build points per day contributed by this line.
- `Jobs: List<IndustryJob>` — ordered job queue.

`IndustryAtb` (component design attribute) contributes a production line when installed on an entity.

### Flow

```
IndustryProcessor (daily)
    → IndustryTools.ConstructStuff(entity)
        → for each ProductionLine in IndustryAbilityDB:
            → process top job in Jobs queue
            → apply build points toward job completion
            → on completion: deliver output to appropriate stockpile
                - Ships → Entity launched into StarSystem
                - Components → ColonyInfoDB.ComponentStockpile
                - Ordnance → ColonyInfoDB.OrdinanceStockpile
                - Installations → added as components via Entity.AddComponent() (ComponentInstancesDB)
```

### Adding a Job (player action)
```
IndustryOrder → IndustryTools.AddJob(entity, productionLineID, job)
    → locks ProductionLine and appends job
```

### Job reprioritization
`IndustryTools.ChangeJobPriority(entity, prodLine, jobID, delta)` — moves a job up or down in the list by `delta` positions.

---

## Mining System

### Data Model

- `MineralsDB` on the **planet**: maps mineral ID → `MineralDeposit { Amount (long), Accessibility (double 0–1) }`.
- `MiningDB` on the **colony**: active mining setup (which minerals are being extracted).
- `MineResourcesAtbDB` on installed **components**: specifies which minerals can be mined and the base rate.

### Flow

```
MineResourcesProcessor (daily)
    → MiningHelper.CalculateActualMiningRates(colonyEntity)
        → sums rates from all MineResourcesAtbDB components
        → scales by mineral Accessibility
        → returns Dict<mineralID, long rate>
    → for each mineral:
        → removes Amount from MineralsDB (planet's deposit depletes)
        → adds Amount to CargoStorageDB (colony's cargo hold)
```

Mineral `Accessibility` ranges 0.0–1.0. Low accessibility deposits are harder to extract — they produce less per mining unit. Deposits deplete as they are mined.

---

## Installations

**Installations are components.** A colony's installations are `ComponentInstance`s in its `ComponentInstancesDB`, added with `colonyEntity.AddComponent(design, count)` (see `DefaultStartFactory.cs:212-225`). Each installation type is a `ComponentDesign` carrying `*Atb` ability attributes (mining, industry, population support, etc.), and processors find them via `TryGetComponentsByAttribute<TAtb>()`. This is the **same** framework ships use for their components.

`InstallationsDB` (this directory) looks like an installation registry — `Dictionary<string,float> Installations`, `WorkingInstallations`, `EmploymentList`, plus commented-out `ConstructJob` lists — but it is **abandoned**: never attached to a colony, no `[JsonProperty]` fields. It is an earlier design superseded by the component approach. **Do not** use it, extend it, or render it.

**The installations UI gap:** `PlanetaryWindow.RenderInstallations()` is empty *and* its tab is gated on `HasDataBlob<InstallationsDB>()` (always false), so the tab never even shows. Phase 2a fix = render from `ComponentInstancesDB` (reuse `ComponentInstancesDBDisplay`). See `docs/aurora/PLANETARY-INFRASTRUCTURE.md` §6 and `CONVENTIONS.md` §6.

---

## Infrastructure System (DevBranch — Active)

Infrastructure is the **limiting factor on all colony production**. Think of it as the colony's utility grid: power, roads, comms, water. Every installation except infrastructure itself draws from the grid; infrastructure installations add to it. When demand exceeds supply, efficiency drops and all production scales down.

### How capacity is calculated

`InfrastructureProcessor.RecalcCapacity(colonyEntity)`:

**Provided** — sum across all `InfrastructureCapacityAtb` components. Each installed unit contributes `Capacity` units of support, but only if the colony's body gravity and atmospheric pressure are within the component's `GravityToleranceAtb` / `PressureToleranceAtb` limits (out-of-tolerance infrastructure provides nothing — matches the population support model).

**Required** — sum across every OTHER installed component:
```
capacity demanded = (design.MassPerUnit / 1000) + design.CrewReq
```
i.e., 1 unit per tonne of installation mass + 1 unit per crew member.

**Efficiency** = min(1.0, Provided / Required). Written to `InfrastructureDB.Efficiency`. Production processors read this via `InfrastructureProcessor.GetEfficiency(colony)`.

### Note on two parallel systems

`InfrastructureCapacityAtb` (production efficiency) and `PopulationSupportAtbDB` (population cap) are **separate**. An infrastructure installation provides BOTH — it adds to the production efficiency grid AND supports population up to a tolerance-filtered cap. They are different attributes on the same component design, read by different processors.

---

## Key Extension Points for Ground Combat

1. **New `IndustryJob` subtype** — add `GroundUnitConstructionJob` to allow colonies to build ground units through the existing production system.
2. **New `IConstructableDesign`** — `GroundUnitDesign` must implement `IConstructableDesign` (the interface checked by `IndustryTools.ConstructStuff()`).
3. **Installation specialization** — model a "military installation" / GFCC as a new `ComponentDesign` with a production-multiplier `*Atb` attribute (NOT by extending the dead `InstallationsDB`). The Ground Force Construction Complex in Aurora is just another installation = component. See `docs/aurora/GROUND-COMBAT.md` §7.

---

## Gotchas

1. **`IndustryTools.ConstructStuff()` uses `lock` on the production line.** New job types must respect this lock or risk race conditions when orders arrive mid-tick.

2. **`InstallationsDB` is dead — don't confuse it for the installation store.** Its `float Installations` / `int WorkingInstallations` fields suggest partial-vs-complete tracking, but nothing writes to them and nothing attaches the blob. Construction delivers installations as components via `Entity.AddComponent()` into `ComponentInstancesDB`. Partial-construction progress (if needed) lives in the industry job, not here.

3. **Mineral depletion is permanent.** `MineralsDB` is on the planet entity with `[JsonProperty]`. Once depleted, deposits do not regenerate. This is by design (mirrors Aurora 4x) but has save implications — mining a body to zero is irreversible.

4. **`MineResourcesProcessor` and `IndustryProcessor` both run daily** but have a `FirstRunOffset` of 3 hours for Industry, so there is no exact same-tick race. If adding a processor that must run after mining and before production, set `FirstRunOffset` between 0 and 3 hours.
