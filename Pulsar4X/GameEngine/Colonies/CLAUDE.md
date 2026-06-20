# Colonies — Subsystem Reference

Population, colony lifecycle, life support. Lives in `GameEngine/Colonies/`.

---

## File Map

| File | Purpose |
|------|---------|
| `ColonyInfoDB.cs` | Core colony DataBlob: population per species, stockpiles, parent planet reference, component dictionary for bombardment. |
| `ColonyLifeSupportDB.cs` | Tracks `MaxPopulation` — the carrying capacity ceiling calculated from infrastructure. |
| `ColonyBonusesDB.cs` | Modifier DataBlob for production/research/mining bonuses on a colony. |
| `ColonizeableDB.cs` | Marker DataBlob on planets that can be colonized. |
| `ColonyFactory.cs` | `CreateColony()` — creates a colony entity and attaches all required DataBlobs. |
| `CreateColonyOrder.cs` | Player order to found a colony on a planet. |
| `PopulationProcessor.cs` | `IHotloopProcessor` (monthly). Runs `GrowPopulation()` for each colony. |

---

## Colony Entity DataBlob Composition

A fully-formed colony entity typically holds:

`ColonyFactory.CreateColony()` attaches these blobs **directly** (verified in `ColonyFactory.cs`):

| DataBlob | Purpose |
|----------|---------|
| `NameDB` | Colony name |
| `ColonyInfoDB` | Core: population, stockpiles, parent planet |
| `ColonyBonusesDB` | Modifier tracking |
| `MiningDB` | Mining setup |
| `OrderableDB` | Marks the colony able to receive orders |
| `MassVolumeDB` | Mass / volume |
| `CargoStorageDB` | Mineral and goods storage |
| `PositionDB` | Location (on the planet surface) |
| `TeamsHousedDB` | Scientists / commanders housed here |
| `ComponentInstancesDB` | **Installations live here** (added via `AddComponent()`) |

> **NOT attached by `ColonyFactory`** (corrects the earlier recon):
> - **`InstallationsDB` is never attached to a colony** — it is vestigial/abandoned (no `[JsonProperty]` fields; only dead-code references). Installations are **components** in `ComponentInstancesDB` (see `DefaultStartFactory.cs:212-225`: `AddComponent(mineDesign)`, `AddComponent(facEntity)`, …).
> - Capability blobs such as `IndustryAbilityDB` are **granted by installed components** carrying the matching `*Atb` attribute, not added by the factory.
> - `ColonyLifeSupportDB.MaxPopulation` is (re)computed by `PopulationProcessor`, not added at creation.
>
> Verify the exact grant path in code before assuming a specific blob is present. See `CONVENTIONS.md` §6 (components = abilities).

---

## ColonyInfoDB Fields

```csharp
Dictionary<int, long> Population           // species entity ID → population count
Dictionary<string, int> ComponentStockpile // built component ID → count in stockpile
Dictionary<string, float> OrdinanceStockpile // ordnance ID → count
List<Entity> FighterStockpile             // fighter entities built but not launched
Entity PlanetEntity                        // the planet body this colony is on
Dictionary<Entity, double> ColonyComponentDictionary // for bombardment targeting
```

`ColonyComponentDictionary` is the hook for orbital bombardment damage. It maps installation entities to their volume (used to calculate random hit probability). Currently empty in practice — not populated during colony construction.

---

## Population Processor

`PopulationProcessor` runs monthly (`RunFrequency = TimeSpan.FromDays(30)`).

`GrowPopulation(colony)`:
1. Gets `popSupportValue = ComponentInstancesDB.GetPopulationSupportValue()` — total support from installed infrastructure.
2. For each species in `Population`:
   - If `species.ColonyCost(planet) > 0` (hostile environment — needs support):
     - `maxPopulation = popSupportValue / needsSupport / colonyCost`
     - If current pop > max: population decays at −50.0% growth rate (very harsh).
     - If current pop ≤ max: growth rate = `20 / (pop ^ (1/3))`, capped at 10%.
   - If `colonyCost == 0` (species is native to this environment): uncapped growth, same formula.
3. Publishes `EventType.PopulationChanged` event.

**Current gaps:**
- No governor modifiers (`// @todo: get external factors`)
- No radiation effect on growth
- The `−50.0` die-off rate is a placeholder, not a formula
- No distinction between starvation, disease, migration

---

## Life Support and Carrying Capacity

`ColonyLifeSupportDB.MaxPopulation` = total population support value from all installed infrastructure components that have a `PopulationSupportAtbDB` component attribute.

`ReCalcMaxPopulation()` in `PopulationProcessor` recomputes this from `ComponentInstancesDB.GetPopulationSupportValue()`.

`Species.ColonyCost(planet)` — returns a floating-point cost multiplier based on atmosphere compatibility, temperature, gravity, etc. Higher cost = more life support infrastructure needed per capita. Zero = native planet, no support needed.

---

## Installation System

**Installations are components, not a separate registry.** A colony's installations are `ComponentInstance`s in its `ComponentInstancesDB`, added via `colonyEntity.AddComponent(design, count)`. Query them by ability with `componentInstances.TryGetComponentsByAttribute<TAtb>(...)` or via `DesignsAndComponentCount`.

`InstallationsDB` (in `Industry/InstallationsDB.cs`) *looks* like the installation store — `Dictionary<string,float> Installations`, `WorkingInstallations`, `EmploymentList` — but it is **dead code**: never attached to any colony, no `[JsonProperty]` fields, its `ConstructJob` lists commented out. Treat it as abandoned. Do not build on it; do not "fill in" the UI against it.

**The installations UI is doubly broken.** `PlanetaryWindow.RenderInstallations()` not only has an empty body — its tab is gated on `HasDataBlob<InstallationsDB>()` (`PlanetaryWindow.cs:107`), which is always false, so the tab never appears. The correct fix renders from `ComponentInstancesDB` (reuse the existing `ComponentInstancesDBDisplay`). See `docs/aurora/PLANETARY-INFRASTRUCTURE.md` §6.

---

## Ground Combat Hook Points

The colony system is the target for all ground combat. Key integration points:

| What | Where | Status |
|------|-------|--------|
| Orbital bombardment damage to population | `DamageProcessor.cs` lines ~101–181 | Commented out |
| Orbital bombardment damage to installations | Same block | Commented out |
| `ColonyComponentDictionary` for randomized targeting | `ColonyInfoDB.cs` | Exists but never populated |
| Ground unit garrison attachment | *(not yet)* | New `GroundUnitDB` needed |
| Active combat state | *(not yet)* | New `GroundCombatDB` needed |

---

## Gotchas

1. **`ColonyComponentDictionary` is never populated.** `ColonyFactory.CreateColony()` constructs the colony entity but never fills `ColonyComponentDictionary`. The bombardment damage code that reads it would always find an empty dictionary. Fix this when implementing orbital bombardment.

2. **`PopulationSupportValue` is computed from `ComponentInstancesDB`, not `InstallationsDB`.** Installations that provide population support must be physical components attached via `ComponentInstancesDB` (with `PopulationSupportAtbDB`), not just entries in `InstallationsDB.Installations`. This is a design subtlety — track it when adding new installation types.

3. **PopulationProcessor runs on entities with `ColonyInfoDB`, not `InstallationsDB`.** A body with installations but no `ColonyInfoDB` (bare infrastructure, no population) will not be processed by `PopulationProcessor`.

4. **Colony entities live in a `StarSystem` manager like any other entity.** They are not sub-objects of the planet — they are sibling entities at the same level, linked to a planet via `ColonyInfoDB.PlanetEntity`.
