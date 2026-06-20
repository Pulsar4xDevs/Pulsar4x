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
| `InstallationsDB.cs` | DataBlob: installation inventory on a colony (types + counts, on/off toggles). |
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
                - Installations → InstallationsDB.Installations
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

`InstallationsDB` (on colony entities):
```csharp
Dictionary<string, float> Installations      // typeID → total count (float: partial = under construction)
Dictionary<string, int> WorkingInstallations // typeID → fully-operational count
List<InstallationEmployment> EmploymentList  // per-type on/off switch
```

Installations are distinct from physical `ComponentInstance` objects (which appear in `ComponentInstancesDB`). The relationship:
- `ComponentInstancesDB` — the physical components that *provide* abilities (mining equipment, population support, power plants, etc.).
- `InstallationsDB` — the high-level count of "building" types (e.g., "Construction Factory: 5").

There is overlap and some inconsistency: some colony abilities are driven by components in `ComponentInstancesDB` (e.g., mining rate from `MineResourcesAtbDB`), while the `InstallationsDB` tracks the same items at a different level of abstraction. Read both when adding new infrastructure types.

**The UI for `InstallationsDB` is a stub.** `PlanetaryWindow.RenderInstallations()` fetches the blob but renders nothing. Filling this in is Phase 2a work.

---

## Key Extension Points for Ground Combat

1. **New `IndustryJob` subtype** — add `GroundUnitConstructionJob` to allow colonies to build ground units through the existing production system.
2. **New `IConstructableDesign`** — `GroundUnitDesign` must implement `IConstructableDesign` (the interface checked by `IndustryTools.ConstructStuff()`).
3. **Installation specialization** — add a `SpecializationDB` or extend `InstallationsDB` with a production multiplier per installation type. Colonies with a "Military Installation" type would multiply ground unit construction throughput.

---

## Gotchas

1. **`IndustryTools.ConstructStuff()` uses `lock` on the production line.** New job types must respect this lock or risk race conditions when orders arrive mid-tick.

2. **Partial installations use `float` in `InstallationsDB.Installations`.** A value of 0.5 means 50% progress toward completing the next installation. The `WorkingInstallations` (int) count only fully completed ones. Production code must write to both fields correctly.

3. **Mineral depletion is permanent.** `MineralsDB` is on the planet entity with `[JsonProperty]`. Once depleted, deposits do not regenerate. This is by design (mirrors Aurora 4x) but has save implications — mining a body to zero is irreversible.

4. **`MineResourcesProcessor` and `IndustryProcessor` both run daily** but have a `FirstRunOffset` of 3 hours for Industry, so there is no exact same-tick race. If adding a processor that must run after mining and before production, set `FirstRunOffset` between 0 and 3 hours.
