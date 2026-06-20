# Pulsar4X — Coding Conventions to Mirror

**Purpose:** Pulsar4X is an established, deliberately-structured codebase. It is often *more defensive and more save/load-aware* than a greenfield design would be. When adding code, **match these patterns rather than imposing a different style.** Every rule below was extracted from the actual source — file references included. Read this before writing any new DataBlob, Processor, Factory, or Order.

> Guiding principle: **the codebase is the style guide.** If your instinct conflicts with a pattern here, the pattern wins. Reasons are given so you can apply them to new cases, not just copy.

---

## 1. DataBlobs — the copy-constructor + `Clone()` pattern

Every DataBlob follows this exact shape. Reference: `Colonies/ColonyInfoDB.cs`, `Industry/InstallationsDB.cs`.

```csharp
public class ExampleDB : BaseDataBlob
{
    [JsonProperty]
    public Dictionary<int, long> SomeState { get; internal set; } = new ();

    public ExampleDB() { }                       // parameterless ctor required (deserialization)

    public ExampleDB(ExampleDB other)            // copy constructor — DEEP-copy collections
    {
        SomeState = new Dictionary<int, long>(other.SomeState);
    }

    public override object Clone() => new ExampleDB(this);   // Clone delegates to copy ctor

    public new static List<Type> GetDependencies()           // optional: required co-blobs
        => new List<Type>() { typeof(NameDB) };
}
```

Rules:
- **`[JsonProperty]` on every field that must survive save/load.** No attribute = not saved.
- **`{ get; internal set; }`** with an **inline `= new()` initializer** for collections.
- **`Clone()` is a copy constructor**, not `MemberwiseClone`. Deep-copy dictionaries/lists; it is used when entities move between managers (system jumps).
- Some clones are intentionally **shallow** (e.g. `ComponentInstancesDB.Clone()` does not clone referenced component instances — it says so in a comment). Match the documented intent of the blob you're cloning.
- **Do not** add a constructor that throws or does heavy work — blobs are constructed during deserialization.

---

## 2. Serialize one canonical collection; rebuild indexes on load

The single most important resilience pattern. Reference: `Engine/Components/ComponentInstancesDB.cs`.

When a blob keeps derived/index collections for fast lookup, **persist only the source-of-truth collection** and **rebuild the rest in an `[OnDeserialized]` callback**:

```csharp
[JsonProperty]
internal readonly Dictionary<string, ComponentInstance> AllComponents = new();   // saved

[JsonIgnore] public Dictionary<Type, List<ComponentInstance>> ComponentsByAttribute = new();  // rebuilt
// ...other [JsonIgnore] indexes...

[OnDeserialized]
private void Deserialized(StreamingContext context)
{
    foreach (var item in AllComponents)
        AddComponentInstance(item.Value);   // re-derives every index from the saved collection
}
```

Why: saving derived indexes bloats saves and risks them going out of sync with the canonical data. Any new blob with caches/indexes must do this.

---

## 3. `TryGet*` everywhere — return `false`, don't throw

The codebase strongly prefers `bool TryGetX(out T value)` over exceptions or nullable returns. References: `ComponentInstancesDB.TryGetComponentsByAttribute<T>`, `entity.TryGetDatablob<T>(out var db)`, `Manager.TryGetEntityById(id, out var e)`.

```csharp
if (entity.TryGetDatablob<ColonyInfoDB>(out var colony))
{
    // use colony
}
```

- New query APIs should offer a `TryGet` form.
- Reach for `HasDataBlob<T>()` before `GetDataBlob<T>()` if absence is normal.
- Don't throw on "not found" where the codebase returns `false`/empty. Match the non-throwing contract.

---

## 4. Sentinels and guard-before-mutate, not exceptions

The code is defensive in small, consistent ways. Mirror them:
- **Sentinel values:** `Entity.InvalidEntity` (`Id = -1`); `shipDesign.DesignVersion == 0` means "not built yet" (`ShipFactory.cs:91`). Use sentinels rather than null where the codebase does.
- **Guard before dictionary mutation:** `if (!dict.ContainsKey(k)) dict.Add(k, new()); dict[k].Add(v);` (pervasive in `ComponentInstancesDB.AddComponentInstance`).
- **Null-coalesce inputs:** `colonyBlueprint.StartingPopulation ?? 1000`, `item.Type ?? "byMass"` (`ColonyFactory.cs`). Provide sensible defaults instead of failing.

---

## 5. Factories — static, blob-list, `AddEntity`, then post-add

Entity creation is centralised in static `*Factory` classes. Reference: `Colonies/ColonyFactory.cs`, `Ships/ShipFactory.cs`. The canonical sequence:

```csharp
public static Entity CreateThing(Entity faction, Entity parent, ...)
{
    var blobs = new List<BaseDataBlob>();
    blobs.Add(new NameDB(...));
    blobs.Add(new SomeDB(...));
    // ...all blobs known at creation time...

    var entity = Entity.Create();                 // generates Id
    entity.FactionOwnerID = faction.Id;
    parent.Manager.AddEntity(entity, blobs);      // adds to the StarSystem manager

    // Post-add: blobs/components that need the entity to already exist
    entity.SetDataBlob(new NameDB(...));          // or
    entity.AddComponent(design, count);           // installations/ship parts go here

    return entity;
}
```

- Build the bulk of blobs into a `List<BaseDataBlob>` and pass them to `AddEntity` in one shot.
- Things that depend on the entity existing (named after its Id, components, fleet links) go **after** `AddEntity`.
- Register the new entity with its owners (`factionInfo.Colonies.Add(...)`, `FactionOwnerDB.SetOwned(...)`).

---

## 6. Abilities = Components + `*Atb` attributes (do NOT invent parallel systems)

This is the architectural heart, and the biggest place to "adapt to how Pulsar is written." References: `Engine/Components/ComponentInstancesDB.cs`, `DefaultStartFactory.cs:212-225`, `ShipFactory.cs:127-130`.

- A unit's capabilities come from **`ComponentDesign`s carrying `*Atb` attributes** (`IComponentDesignAttribute`), installed into the entity's **`ComponentInstancesDB`** via `entity.AddComponent(design, count)`.
- Processors find capable components with `componentInstances.TryGetComponentsByAttribute<TAtb>(out var list)` or `ComponentsByAttribute[typeof(TAtb)]`.
- **Ships, colonies, and (future) ground units are all "an entity with a `ComponentInstancesDB`."** Installations are components. Mining, population support, industry, sensors — all are component attributes.

**Consequence for new work:** before adding a new DataBlob to model some capability, ask *"can this be a component design + an `*Atb` attribute + a processor?"* That is almost always the right answer here, and it gets you research/unlock, construction, save/load, and the design UI for free. Only add a bespoke DataBlob for genuinely entity-level state (e.g. a formation's morale), not for per-capability abilities.

> Worked example — the abandoned alternative: `InstallationsDB` is a bespoke "list of installation counts" blob. It was **superseded by the component approach and left dead** (never attached, no `[JsonProperty]`). It is the cautionary tale: don't model infrastructure as a parallel registry; model it as components. See `Industry/CLAUDE.md`.

---

## 7. Processors — implement the interface, stay trivial, get auto-discovered

References: `Engine/ProcessManager.cs`, any `*Processor.cs`. There is **no manual registration** — `ProcessorManager.CreateProcessors()` reflects over the assembly and instantiates every `IHotloopProcessor`/`IInstanceProcessor`.

```csharp
public class ExampleProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency => TimeSpan.FromDays(1);
    public TimeSpan FirstRunOffset => TimeSpan.FromHours(3);   // stagger vs other daily procs
    public Type GetParameterType => typeof(ExampleDB);
    public void Init(Game game) { }                            // keep trivial

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var entities = manager.GetAllEntitiesWithDataBlob<ExampleDB>();
        foreach (var e in entities) ProcessEntity(e, deltaSeconds);
        return entities.Count;
    }
    public void ProcessEntity(Entity entity, int deltaSeconds) { /* ... */ }
}
```

- **Constructors and `Init` must not throw** — a broken processor crashes startup (it's `Activator.CreateInstance`'d). Root `CLAUDE.md` gotcha #2.
- Use `FirstRunOffset` to avoid same-tick races (e.g. Industry runs 3h after mining so stockpiles update first).
- Hot-loop = periodic over all matching entities. Instance = scheduled one-shot for specific entities (interrupt-driven).

---

## 8. `async void` mutations swallow exceptions — keep bodies minimal

`EntityManager.AddEntity / SetDataBlob / RemoveDatablob / TagEntityForRemoval` are **`async void`** (needed for `MessagePublisher.Publish`). Exceptions inside propagate to the thread pool **unobserved**. References: `Engine/Entities/EntityManager.cs`; root `CLAUDE.md` gotcha #5.

- Do not put logic that can throw inside these calls.
- Cover entity mutations with tests; a silent failure won't surface at runtime.

---

## 9. Cross-entity references: prefer IDs, resolve via the manager

The code stores cross-entity links as **integer IDs** and resolves them lazily, e.g. `ShipInfoDB.CommanderID` → `Manager.TryGetEntityById(id, out var commander)` (`ShipFactory.DestroyShip`). Some older blobs store `Entity` directly (`ColonyInfoDB.PlanetEntity`, `FighterStockpile`) — both exist, but **IDs survive cross-manager moves and serialization more cleanly.** For new code, store an `int` Id (or string design `UniqueID`) and resolve on use, unless you're matching an existing blob's field.

- Entity Ids are `int` (`Entity.Create()`); component/design ids are **string GUIDs** (`UniqueID`).

---

## 10. Thread-safety: `SafeDictionary` / `SafeList`, and never reach across managers untimed

Systems are processed in `Parallel.ForEach` over `StarSystem`s (`MasterTimePulse.DoProcessing`). References: root `CLAUDE.md`, `Engine/ManagerSubPulse.cs`.

- Shared/long-lived collections that may be touched by multiple threads use `SafeDictionary<K,V>` / `SafeList<T>` (ReaderWriterLockSlim wrappers).
- **Never mutate another StarSystem's entities directly** from a processor. Cross-system events go through `MasterTimePulse.AddSystemInteractionInterupt()` so the systems are synced first (used by inter-system jumps).
- `IndustryTools.ConstructStuff()` takes a `lock` on the production line — respect existing locks; orders can arrive mid-tick.

---

## 11. Time & scheduling: never schedule in the past

References: `Engine/ManagerSubPulse.cs`; root `GameEngine/CLAUDE.md` gotchas #3–4.

- Scheduling something for a datetime already passed throws **`Temporal Anomaly Exception`** — this is a real-bug detector, never suppress it.
- Base future datetimes on **`entity.StarSysDateTime`** (or `_processToDateTime` inside the subpulse), not on global time.
- `SetDataBlob` automatically schedules the blob's processor via `AddSystemInterupt(blob)`. You usually don't schedule manually.

---

## 12. Save/load discipline (TypeNameHandling.Objects)

References: root `CLAUDE.md` gotcha #7; `Engine/SaveLoad/`.

- Saves embed **C# type names**. **Renaming or moving a DataBlob class breaks existing saves** — add a `[JsonConverter]`/migration if you must.
- `[JsonProperty]` persisted; runtime-only refs `[JsonIgnore]` and rebuilt on load (see §2).
- Private/internal fields are serialized via `NonPublicResolver` — internal setters still persist.

---

## 13. Externalise tunable numbers into mod data, don't hard-code

The game loads designs, minerals, tech, and component blueprints from JSON under `GameEngine/Data/basemod/` via `ModLoader` into `Game.StartingGameData`. References: root `GameEngine/CLAUDE.md`.

- New tunable constants (combat coefficients, installation rates, ground-unit stats) belong in **blueprint JSON**, not as C# literals — matching how components/minerals/tech already work.
- This also resolves the Aurora-constant-uncertainty problem (`docs/aurora/INDEX.md`): put the approximate value in data, tune later without recompiling.

---

## 14. Naming suffixes (enforced)

| Suffix | Meaning |
|--------|---------|
| `*DB` | DataBlob (component/state) |
| `*Processor` | `IHotloopProcessor` / `IInstanceProcessor` |
| `*Atb` | component design attribute (`IComponentDesignAttribute`) |
| `*Order` / `*Command` | player-issued `EntityCommand` |
| `*Factory` | static entity-creation helper |
| `*Blueprint` | JSON-loadable data template |

---

## 15. Docs travel with code

When you change a subsystem that has a `CLAUDE.md`, **update that `CLAUDE.md` in the same commit** (working agreement in root `CLAUDE.md`). When a change invalidates something here, update this file. Stale docs that point at the wrong function are worse than none — the prior recon's `InstallationsDB` description is the example that motivated this whole pass.

---

## Quick "am I coding like Pulsar?" checklist

- [ ] New capability modelled as **component + `*Atb` + processor**, not a bespoke registry?
- [ ] DataBlob has parameterless ctor, copy ctor, `Clone()`, `[JsonProperty]` on saved fields?
- [ ] Derived indexes `[JsonIgnore]` + rebuilt in `[OnDeserialized]`?
- [ ] Queries are `TryGet*`/`Has*`, non-throwing?
- [ ] Guards before dict mutation; sentinels/defaults instead of throwing on bad input?
- [ ] Processor ctor/`Init` trivial (won't crash startup)?
- [ ] No throwing logic inside `async void` manager mutations?
- [ ] Cross-entity links stored as IDs, resolved via `TryGetEntityById`?
- [ ] No direct cross-manager mutation; future datetimes based on `StarSysDateTime`?
- [ ] Tunable numbers in `Data/basemod/` JSON, not hard-coded?
- [ ] Touched subsystem's `CLAUDE.md` updated in the same commit?
