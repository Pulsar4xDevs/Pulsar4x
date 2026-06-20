# Pulsar4X — Architecture Reference

## Top-Down Map

```
Pulsar4X.sln
├── GameEngine            ← Core domain, no UI dependency
│   ├── Engine/           ← ECS infrastructure (Entity, EntityManager, processors, time pulse)
│   ├── Weapons/          ← Space combat: beams, missiles, fire control
│   ├── Damage/           ← Damage application (Simple + Complex)
│   ├── Colonies/         ← Colony state, population
│   ├── Industry/         ← Production, mining, refinement
│   ├── Movement/         ← Newton thrust, warp, jump, pathfinding
│   ├── Sensors/          ← EM signatures, scan, contacts
│   ├── Orbits/           ← Kepler orbit DataBlobs + updaters
│   ├── Galaxy/           ← Star system generation, bodies, atmosphere
│   ├── Fleets/           ← Fleet grouping and orders
│   ├── Logistics/        ← Automated cargo transfer
│   ├── Tech/             ← Research points and tech unlocks
│   ├── Energy/           ← Power generation/consumption
│   ├── Storage/          ← Cargo holds
│   ├── Ships/            ← Ship design + factory
│   ├── Factions/         ← Faction state, tech DB, event log
│   ├── People/           ← Commander/scientist entities
│   ├── Names/            ← Name generation themes
│   └── Data/             ← JSON mod data (basemod, testingmod)
│
├── Pulsar4X.OrbitalMath  ← Pure math library (Vector3, orbital mechanics)
│
├── Pulsar4X.Client       ← ImGui.NET + SDL2 frontend
│   ├── Interface/        ← Windows, displays, menus, HUD, widgets
│   ├── Rendering/        ← Camera, system map, icons, OpenGL renderer
│   ├── State/            ← GlobalUIState, EntityState, SystemState
│   ├── IMGUISDL/         ← SDL2 + ImGui wiring
│   └── ModFileEditing/   ← In-game blueprint editor
│
├── Pulsar4X.Tests        ← NUnit tests
├── Benchmarks            ← BenchmarkDotNet
└── ViewModelLib          ← LEGACY — WPF-era ViewModel, not used
```

---

## Game Loop Data Flow

```
Wall clock (System.Timers.Timer, 100ms default interval)
    │
    ▼
MasterTimePulse.DoProcessing(targetDateTime)
    │
    ├─ ProcessNextInterupt()          ← handles cross-system events (e.g. jump transits)
    │
    └─ For each StarSystem (parallel):
           ManagerSubPulse.ProcessSystem(nextDateTime)
               │
               ├─ RemoveTaggedEntities()   ← clean up destroyed entities
               │
               └─ ProcessToNextInterupt() loop:
                   │
                   ├─ HotLoopProcessors (frequency-based)
                   │   Each IHotloopProcessor runs ProcessManager(EntityManager, deltaSeconds)
                   │   over ALL entities that have its DataBlob type.
                   │   Examples:
                   │     GenericFiringWeaponsProcessor  (1 sec)
                   │     BeamWeaponProcessor            (1 sec)
                   │     NewtonianMovementProcessor     (variable)
                   │     WarpMoveProcessor              (variable)
                   │     MineResourcesProcessor         (daily)
                   │     IndustryProcessor              (daily)
                   │     ResearchProcessor              (monthly)
                   │     SensorReflectionProcessor      (variable)
                   │     PopulationProcessor            (yearly)
                   │
                   └─ InstanceProcessorsQueue (interrupt-based)
                       Specific entities processed at a specific datetime.
                       Scheduled by DataBlobs via ManagerSubPulse.AddEntityInterupt().
                       Examples:
                         NavSequenceProcessor (next waypoint arrival)
                         OrderableProcessor   (next order execution)
                         GeoSurveyProcessor   (survey completion)
```

---

## Entity-DataBlob Relationship

```
EntityManager
  ├─ _entities: Dict<int, Entity>
  └─ _datablobStores: Dict<Type, Dict<int, BaseDataBlob>>
                            │
                            └─ e.g. typeof(BeamInfoDB) → { entityId → BeamInfoDB instance }

Entity (id=42, factionId=1)
  │  (delegates all blob access to EntityManager)
  ├─ GetDataBlob<T>()   → EntityManager.GetDataBlob<T>(42)
  ├─ HasDataBlob<T>()   → EntityManager.HasDataBlob<T>(42)
  └─ SetDataBlob(blob)  → EntityManager.SetDataBlob(42, blob)
                              → triggers ManagerSubPulse.AddSystemInterupt(blob)
                                (schedules the blob's HotloopProcessor if one exists)
```

---

## Processor Registration (Auto-Discovery)

`ProcessorManager.CreateProcessors()` uses `Assembly.GetExecutingAssembly().GetTypes()` to find all types implementing `IHotloopProcessor` or `IInstanceProcessor` and instantiates them. No manual registration is needed. Adding a new processor class is sufficient to activate it.

**Consequence:** Any processor with a constructor that throws will crash startup. Keep constructors trivial; use `Init(Game game)` for initialization.

---

## Space Combat Data Flow

```
Player issues SetFireControlOrder
    → FireControlAbilityState.Target = targetEntity

GenericFiringWeaponsProcessor (HotLoop, 1 sec)
    → reads GenericFiringWeaponsDB
    → if internal magazine ≥ min shots and target.IsValid:
        → FireInstructions[i].FireWeapon(owner, target, shots)
              │
              ├─ BeamWeaponAtb → BeamWeaponProcessor.FireBeamWeapon()
              │     → creates BeamInfoDB entity in StarSystem
              │
              └─ MissleLauncherAtb → MissileProcessor.LaunchMissile()
                    → creates missile Entity with NewtonMoveDB
    → reload internal magazine

BeamWeaponProcessor (HotLoop, 1 sec) per BeamInfoDB entity:
    Fired → calc timeToTarget → if hit: AtTarget else MissedTarget
    AtTarget → SimpleDamage.OnTakingDamage(target, 100, 500)
                    → picks random component, deals damage
                    → if all components gone: ShipFactory.DestroyShip()
    MissedTarget → dissipate energy, eventually destroy beam entity

[COMMENTED OUT – not active]:
    DamageProcessor.OnTakingDamage() → EntityDamageProfileDB →
    DamageTools.DealDamageEnergyBeamSim() → component-level HTK tracking
```

---

## Colony / Infrastructure Data Flow

```
Player creates colony (CreateColonyOrder)
    → ColonyFactory.CreateColony()
        → Entity with ColonyInfoDB, InstallationsDB, ComponentInstancesDB,
          CargoStorageDB, IndustryAbilityDB, MiningDB (if applicable)

IndustryProcessor (HotLoop, daily)
    → reads IndustryAbilityDB.Jobs queue
    → calculates build points available
    → for each job: produces output (ship, component, ordnance, installation)
    → pushes completed items to ColonyInfoDB stockpiles

MineResourcesProcessor (HotLoop, daily)
    → reads MiningDB + MiningHelper.CalculateActualMiningRates()
    → removes minerals from MineralsDB (planet)
    → pushes to CargoStorageDB (colony)

PopulationProcessor (HotLoop, yearly)
    → reads ColonyInfoDB.Population
    → applies growth/decay (partial implementation)

[NO GROUND COMBAT EXISTS]
    ColonyInfoDB has ColonyComponentDictionary for installation damage
    DamageProcessor.cs lines ~101-181 have a commented-out stub for
    colony damage from bombardment — design intent is there, code is not
```

---

## UI Architecture

```
Program.cs
    └─ PulsarMainWindow (ImGuiSDL2CSWindow)
            │
            ├─ GlobalUIState              ← singleton, holds game ref + open windows
            │    ├─ Game (engine ref)
            │    ├─ Faction (selected player faction)
            │    ├─ SystemState           ← active star system view state
            │    ├─ LoadedWindows         ← unique windows (one instance max)
            │    └─ LoadedNonUniqueWindows← windows with per-entity instances
            │
            ├─ GalacticMapRender          ← galaxy-level zoom (jump point graph)
            │
            ├─ SystemMapRendering         ← system-level zoom
            │    └─ Icons/               ← orbit ellipses, ship icons, warp lines
            │
            └─ PulsarGuiWindow subclasses (each has Display() called every frame)
                 Notable windows:
                 ├─ SystemWindow          ← system selector
                 ├─ FleetWindow           ← fleet management
                 ├─ ColonyManagementWindow← colony list + overview
                 ├─ PlanetaryWindow       ← planet detail (general/installs/minerals)
                 ├─ ShipDesignWindow      ← ship design UI
                 ├─ ComponentDesignWindow ← component designer
                 ├─ FireControlWindow     ← weapon/fire control assignment
                 ├─ ResearchWindow        ← tech research queue
                 ├─ LogisticsWindow       ← automated cargo routes
                 └─ NavWindow / WarpOrderWindow / NewtonOrderWindow
```

---

## Save / Load

- `Game.Save(game)` → `JsonConvert.SerializeObject` with `TypeNameHandling.Objects`
- `Game.Load(json)` → `JsonConvert.DeserializeObject<Game>` + re-initialization of non-serializable fields
- Post-load: `TimePulse.Initialize()`, `ProcessorManager` recreated, `ManagerSubPulse.PostLoadInit()` for each StarSystem
- **Breaking change risk**: Renaming DataBlob classes breaks existing saves because class names are embedded in JSON.

---

## Where Ground Combat Would Hook In

| Hook Point | File | Notes |
|------------|------|-------|
| Ground unit entity creation | `Colonies/ColonyFactory.cs` | Mirror ShipFactory pattern |
| Ground unit DataBlob | *(new)* `Colonies/GroundUnitDB.cs` | Population, equipment, organization |
| Ground combat processor | *(new)* `Colonies/GroundCombatProcessor.cs` | IHotloopProcessor, daily tick |
| Colony damage from bombardment | `Damage/DamageComplex/DamageProcessor.cs` | Uncomment + complete lines ~101-181 |
| Planetary window UI | `Pulsar4X.Client/Interface/Windows/PlanetaryWindow.cs` | `RenderInstallations()` is empty |
| New ground combat UI window | *(new)* `Pulsar4X.Client/Interface/Windows/GroundCombatWindow.cs` | |
| Orders for ground invasion | *(new)* `Colonies/InvasionOrder.cs` | Follow `SetFireControlOrder` pattern |
