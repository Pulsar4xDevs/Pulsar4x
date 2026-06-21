# Damage — Subsystem Reference

Three implementations exist: `Simple/` (active placeholder for beam hits), `DamageComplex/` (partial component-level system), and `DamageVeryComplex/` (new particle physics simulation, active for asteroids). Lives in `GameEngine/Damage/`.

---

## File Map

| File | Purpose |
|------|---------|
| `Simple/SimpleDamage.cs` | Active placeholder for beam hits. Random component selection, random 100–500 HTK damage. |
| `DamageComplex/DamageProcessor.cs` | Partial component-level damage path. `OnTakingDamage()` partially wired; colony damage block is commented out. |
| `DamageComplex/DamageTools.cs` | `DealDamageEnergyBeamSim()` — spatial component damage kernel. |
| `DamageComplex/EntityDamageProfileDB.cs` | DataBlob: HTK remaining per component instance on a ship. Created lazily on first hit. |
| `DamageComplex/ComponentPlacement.cs` | Spatial placement data for components in a ship cross-section. |
| `DamageVeryComplex/DamageMap.cs` | **NEW** — 2D particle grid representing a ship or object cross-section. The substrate the physics sim runs on. |
| `DamageVeryComplex/DamagePhysicsSim.cs` | **NEW** — Physics loop: moves particles, detects collisions, resolves them, handles photon beams. Recursive — spawns sub-maps for high-velocity particles. |
| `DamageVeryComplex/KineticMath.cs` | Kinetic particle math: collision detection, elastic/inelastic resolution, fastest-particle query. |
| `DamageVeryComplex/PhotonMath.cs` | Photon/beam propagation through the DamageMap. |
| `DamageVeryComplex/PressureMath.cs` | Pressure wave propagation (for explosions/detonations). |
| `DamageVeryComplex/TempratureMath.cs` | Thermal effects on particles (heating from beams, ablation). |
| `DamageVeryComplex/Particle.cs` | `PhysicalParticle` struct: position, velocity, material properties. The atom of the physics sim. |
| `DamageVeryComplex/AsteroidDamage.cs` | **Active path for asteroid kinetic impacts.** Wires an asteroid strike into the particle physics sim. |

---

## Active Path: SimpleDamage

```csharp
SimpleDamage.OnTakingDamage(Entity entityToDamage, int damageMin, int damageMax)
  → if entity has ComponentInstancesDB with components:
      → pick random component index
      → damage = RNG.Next(damageMin, damageMax)
      → component.HTKRemaining -= damage
      → if HTKRemaining <= 0: remove component from ComponentInstancesDB
      → if no components remain:
          → if ShipInfoDB present: ShipFactory.DestroyShip(entity)
          → else: entity.Destroy()
  → returns DamageResult { Damage, Destroyed }
```

Called from: `BeamWeaponProcessor.OnHit()` with `(target, 100, 500)`.

**This is a placeholder.** The 100–500 range and random component selection are not physics-based.

---

## Intended Path: DamageProcessor (PARTIALLY STUBBED)

`DamageProcessor.OnTakingDamage(Entity damageableEntity, DamageFragment damageFragment)`:

```
if entity has EntityDamageProfileDB:
    DamageTools.DealDamageEnergyBeamSim(profile, fragment) → damages
    foreach damage: profile.ComponentLookupTable[id].HTKRemaining -= damage.damageAmount

if entity has ComponentInstancesDB: [body is EMPTY — not yet implemented]
```

**Colony damage block (lines ~101–181) is entirely commented out.** It was the design for orbital bombardment:
- Population casualties based on damage amount
- Atmospheric dust + radiation effects
- Random installation targeting using `MassVolumeDB.Volume_km3` (may no longer exist)
- `ComponentInstanceData` type reference (may have been renamed)

### DamageFragment (struct, likely in DamageProcessor.cs)
Fields used in the commented call:
- `Velocity`, `RelativePosition`, `Mass`, `Density`, `Momentum`, `Length`, `Energy`

### EntityDamageProfileDB
- Created from `ShipInfoDB.Design` when a ship first takes damage (lazy creation).
- `ComponentLookupTable: Dictionary<id, ComponentLookupEntry>` where entry has `HTKRemaining`.
- Should ideally be pre-created at ship construction, not lazily — lazy creation can miss the first hit.

---

## DamageVeryComplex — The New Physics Sim

This is the active direction on DevBranch. It's a full 2D particle physics simulation:

- A `DamageMap` is a grid of `PhysicalParticle` objects representing a cross-section of the target.
- When a weapon fires, particles (kinetic slugs) or photon beams enter the map at a given velocity.
- `DamagePhysicsSim.PhysicsLoop()` runs until all particles stop moving:
  - Updates particle positions each tick.
  - Detects and resolves elastic/inelastic collisions between particles.
  - Processes photon beams via `PhotonMath`.
  - Recursively spawns sub-maps for very fast particles (multi-scale simulation).
- `AsteroidDamage.cs` is the only fully-wired caller today — asteroid kinetic strikes use this path.
- Beam weapon damage (lasers) is partially wired: `PhotonMath.BeamProcessing()` exists but the integration with `BeamWeaponProcessor` is not complete.

**This system is more ambitious than Aurora's armor-grid model.** It's also incomplete. Before building ground combat damage on this, confirm with the developer whether to:
- Use this system for ground combat (scientifically accurate, high complexity)
- Use a simplified version mirroring DamageComplex (closer to Aurora's per-component HTK model)

---

## Decision Point: Which Damage Path Forward?

| Path | Status | Pros | Cons |
|------|--------|------|------|
| `SimpleDamage` | Active for beam hits | Works | Random, not physics-based |
| `DamageComplex` | Partially stubbed | Aurora-style component HTK | Incomplete, colony damage commented out |
| `DamageVeryComplex` | Active for asteroids, partial for beams | Physically rigorous | Incomplete, complex to extend |

**Do not start ground combat damage until this decision is made.** Ground combat damage (AP rounds, artillery, orbital strikes) should use whichever path becomes the official forward direction.

---

## Ground Combat Extension Point (After Path Decision)

For DamageComplex path:
1. Uncomment and update the colony damage block in `DamageProcessor.OnTakingDamage()` (~lines 101–181).
2. Verify `MassVolumeDB.Volume_km3` still exists (it does — verified in Galaxy/).
3. Create `GroundUnitDamageProfileDB` mirroring `EntityDamageProfileDB` for ground units.

For DamageVeryComplex path:
1. Create a `DamageMap` for ground unit cross-sections.
2. Wire `GroundCombatProcessor` to call `DamagePhysicsSim.PhysicsLoop()` with appropriate projectile particles.

---

## Gotchas

1. **`EntityDamageProfileDB` is lazily created.** The first hit on a ship without this blob creates it inline. This means the first hit's spatial placement may be slightly wrong (the profile is built from current state, not initial state). Fix: create at ship construction.

2. **Colony damage code references types that may not exist.** `ComponentInfoDB`, `ComponentInstanceData` in the commented block may have been renamed/removed. Verify each type before uncommenting.

3. **`DamageTools.DealDamageEnergyBeamSim()`** is the spatial simulation kernel. Read it carefully before wiring — it computes hit location against a component placement grid. If `ComponentPlacement` data is not populated, results will be wrong.
