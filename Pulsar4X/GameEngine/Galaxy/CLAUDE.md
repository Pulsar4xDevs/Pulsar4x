# Galaxy / System Generation Subsystem — Developer Reference

**What it does:** Generates and stores everything that isn't a ship or colony — star systems, planets, moons, asteroids, atmospheres, ruins. This is the world-building layer. When a new game starts, `GalaxyFactory` and `StarSystemFactory` procedurally generate the playing field.

**Why it matters for ground combat:** `AtmosphereDB` is already here and **already implements the Aurora temperature formula** — this is the data Phase 2a (colony cost/habitability) reads to compute whether a world is hostile. The atmosphere system is more complete than any other "to-do" subsystem.

---

## Files

| File | Role |
|------|------|
| `GalaxyFactory.cs` | Top-level generator. Creates the galaxy (multiple systems, warp point network). |
| `StarSystemFactory.cs` | Creates a single star system: star + planets + moons + asteroids. |
| `StarFactory.cs` | Creates star entities with `StarInfoDB`, `MassVolumeDB`, `SensorProfileDB`, etc. |
| `SystemBodyFactory.cs` | Creates planet/moon/asteroid entities. Assigns body type, mass, orbit, atmosphere. |
| `AtmosphereDB.cs` | DataBlob on any body with an atmosphere. Holds `Pressure`, `SurfaceTemperature`, `GreenhouseFactor`, `GreenhousePressure`, `Hydrosphere`, `HydrosphereExtent`, `Composition` (gas name → atm pressure), `CompositionByPercent`. |
| `AtmosphereProcessor.cs` | Static processor. Computes surface temperature from gas composition using the **Aurora greenhouse formula** (explicitly cited in comments). Called during system generation and whenever atmosphere changes. |
| `AtmosphereDBExtensions.cs` | Helper methods on `AtmosphereDB`. |
| `MassVolumeDB.cs` | Mass, volume, density, radius, surface gravity, escape velocity. `Volume_km3` is **referenced in the commented-out colony damage block** in `DamageProcessor.cs` — this field exists. |
| `MassVolumeProcessor.cs` | Processes mass/volume-related changes. |
| `StarInfoDB.cs` | Star type, luminosity, spectral class. |
| `SystemBodyInfoDB.cs` | Body type (terrestrial, gas giant, etc.), albedo, base temperature. |
| `AsteroidDamageDB.cs` | Damage state for asteroids (kinetic impact mechanics). |
| `AsteroidFactory.cs` | Creates asteroid entities. |
| `RuinsDB.cs` | Alien ruins on a body (exploration reward). |
| `PopulationSupportAtbDB.cs` | Component attribute for infrastructure items that support colonists. `PopulationCapacity` = how many million people this unit of infrastructure supports at CC 1.0. |
| `VisibleByDefaultDB.cs` | Tag blob — entities with this are visible without sensors (stars, known bodies). |

---

## AtmosphereDB — the Key Data for Phase 2

`AtmosphereDB` holds everything the colony cost formula needs:

```csharp
Pressure            float   — total atmospheric pressure in atm
SurfaceTemperature  float   — in °C, after greenhouse effects
GreenhouseFactor    float   — computed by AtmosphereProcessor
GreenhousePressure  float   — sum of (gas pressure × greenhouse effect factor)
Hydrosphere         bool    — does liquid water exist?
HydrosphereExtent   decimal — 0–100% water coverage
Composition         Dictionary<string, float>  — gas ID → pressure in atm
```

---

## AtmosphereProcessor — Already Implements Aurora's Formula

This is the most complete Aurora-derived implementation in the codebase. From the source comments (verbatim):

> "From Aurora: Greenhouse Factor = 1 + (Atmospheric Pressure / 10) + Greenhouse Pressure (Maximum = 3.0)"

The actual implementation uses a slightly adjusted constant (0.035 instead of 0.1) for calibration — this is intentional for Pulsar's scale. The structure is correct.

Surface temperature calculation:
```
SurfaceTemp(K) = BaseTemp(K) × (1 - Albedo)^0.25 + BaseTemp(K) × GreenhouseFactor × (1 - Albedo)^0.25
```
(Applies Stefan-Boltzmann law — more physically accurate than Aurora's simpler version.)

**For terraforming (Phase 2d):** A `TerraformingProcessor` modifies `AtmosphereDB.Composition` over time (adds or removes gas entries), then calls `AtmosphereProcessor.UpdateAtmosphere()` to recompute temperature and pressure. The hook is already designed — no structural changes needed.

---

## PopulationSupportAtbDB — the Infrastructure Link

This component attribute is on installations that make hostile worlds habitable. `PopulationCapacity = 10000` means "this unit supports 10,000 people at CC 1.0." 

The aurora formula is: `MaxPop(millions) = Infrastructure / (CC × 100)`

Where `Infrastructure = sum of PopulationCapacity across all installed units`. This is what `PopulationProcessor` should read to replace the stub.

---

## Pulsar Status vs Aurora

| Aurora concept | Pulsar | Status |
|----------------|--------|--------|
| Procedural system/planet generation | `GalaxyFactory`, `StarSystemFactory` | ✅ functional |
| Atmosphere with gas composition | `AtmosphereDB.Composition` | ✅ functional |
| Surface temperature from greenhouse | `AtmosphereProcessor` (Aurora formula, cited) | ✅ functional |
| Hydrosphere percentage | `AtmosphereDB.HydrosphereExtent` | ✅ stored |
| Ruins generation | `RuinsDB` | ✅ stored |
| Colony cost calculation | `SpeciesDB.ColonyCost()` (see `People/CLAUDE.md`) | ✅ exists, verify formula |
| Infrastructure → population capacity | `PopulationSupportAtbDB.PopulationCapacity` | ✅ stored, stub in PopulationProcessor |
| Terraforming (modify atmosphere over time) | `AtmosphereDB` supports it; no `TerraformingProcessor` | ❌ hook exists, processor missing |
| Asteroid kinetic impact | `AsteroidDamageDB` | ✅ partial |

---

## Phase 2 Hook Points

| Phase 2 task | Hook point |
|-------------|------------|
| Population formula (Phase 2c) | `PopulationProcessor` reads `sum(PopulationSupportAtbDB.PopulationCapacity)` from `ComponentInstancesDB` on the colony, divides by `(ColonyCost × 100)` for max pop |
| Terraforming (Phase 2d) | New `TerraformingAtbDB` component attribute on terraformer installation; new `TerraformingProcessor` modifies `AtmosphereDB.Composition` then calls `AtmosphereProcessor.UpdateAtmosphere()` |
| Colony bombardment dust (Phase 3) | Orbital strikes add a "dust" gas to `AtmosphereDB.Composition` with anti-greenhouse effect; `AtmosphereProcessor` recalculates cooling naturally |
