# Damage — Subsystem Reference

Two implementations exist: `Simple/` (active) and `DamageComplex/` (stubbed). Lives in `GameEngine/Damage/`.

---

## File Map

| File | Purpose |
|------|---------|
| `Simple/SimpleDamage.cs` | Active damage path. Random component selection, random HTK damage, ship destruction check. |
| `DamageComplex/DamageProcessor.cs` | Intended real damage path. Currently only `OnTakingDamage()` is wired (partially). Colony damage is commented out. |
| `DamageComplex/DamageTools.cs` | `DealDamageEnergyBeamSim()` — spatial component damage simulation. Used by DamageProcessor. |
| `DamageComplex/EntityDamageProfileDB.cs` | DataBlob: tracks HTK remaining per component instance on a ship or colony. Created from `ShipInfoDB.Design` on first hit. |
| `DamageComplex/ComponentPlacement.cs` | Spatial placement data for components within a ship cross-section. Used by DamageTools. |

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

## Priority Fix (Phase 1)

To restore the real damage path:

1. In `BeamWeaponProcessor.OnHit()`, replace:
   ```csharp
   var damageResult = SimpleDamage.OnTakingDamage(beamInfo.TargetEntity, 100, 500);
   ```
   with a real `DamageFragment` construction and call to `DamageProcessor.OnTakingDamage()`.

2. Complete the empty `ComponentInstancesDB` handling block in `DamageProcessor.OnTakingDamage()` — this is where components should be marked as destroyed when HTK → 0.

3. Ensure `EntityDamageProfileDB` is created at ship construction in `ShipFactory`, not lazily.

4. Implement ship destruction check inside `DamageProcessor` (currently only in `SimpleDamage`).

---

## Ground Combat Extension Point

To extend damage to ground units and colonies:

1. Uncomment and update the colony damage block in `DamageProcessor.OnTakingDamage()` (~lines 101–181).
2. Verify `MassVolumeDB.Volume_km3` vs `Volume_m3` (rename may have occurred).
3. Create `GroundUnitDamageProfileDB` mirroring `EntityDamageProfileDB` for ground units.
4. Call `DamageProcessor.OnTakingDamage()` from the ground combat processor.

---

## Gotchas

1. **`EntityDamageProfileDB` is lazily created.** The first hit on a ship without this blob creates it inline. This means the first hit's spatial placement may be slightly wrong (the profile is built from current state, not initial state). Fix: create at ship construction.

2. **Colony damage code references types that may not exist.** `ComponentInfoDB`, `ComponentInstanceData` in the commented block may have been renamed/removed. Verify each type before uncommenting.

3. **`DamageTools.DealDamageEnergyBeamSim()`** is the spatial simulation kernel. Read it carefully before wiring — it computes hit location against a component placement grid. If `ComponentPlacement` data is not populated, results will be wrong.
