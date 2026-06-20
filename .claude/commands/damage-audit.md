# Damage Audit — Complex Damage System Wiring Check

Audit whether the complex damage path is correctly wired. This is the Phase 1 critical gate — orbital bombardment and ground combat cannot be built until this is correct.

## What to Check

### 1. BeamWeaponProcessor.OnHit() — the entry point
File: `Pulsar4X/GameEngine/Weapons/WeaponBeam/BeamWeaponProcessor.cs`

Read the file. Find `OnHit()`. Check which damage call is active:

- **Not done:** `SimpleDamage.OnTakingDamage(beamInfo.TargetEntity, 100, 500)` is the active call.
- **Done:** `SimpleDamage` call is removed; a `DamageFragment` is constructed and passed to `DamageProcessor.OnTakingDamage()`.

### 2. DamageProcessor.OnTakingDamage() — component damage path
File: `Pulsar4X/GameEngine/Damage/DamageComplex/DamageProcessor.cs`

Read the file. Check:

- **ComponentInstancesDB block:** Is it still empty, or implemented?
- **Ship destruction check:** Is there logic to call `ShipFactory.DestroyShip()` when HTK reaches 0?
- **Colony damage block (~lines 101–181):** Should remain commented out through Phase 1. Do not uncomment until Phase 3.

### 3. EntityDamageProfileDB — lazy vs eager creation
Files:
- `Pulsar4X/GameEngine/Damage/DamageComplex/EntityDamageProfileDB.cs`
- `Pulsar4X/GameEngine/Ships/ShipFactory.cs`

Check: Is `EntityDamageProfileDB` created inside `DamageProcessor` on first hit (lazy — not done), or is it created in `ShipFactory.CreateShip()` at construction time (done)?

### 4. DamageFragment — does the struct exist and is it complete?
File: `Pulsar4X/GameEngine/Damage/DamageComplex/DamageProcessor.cs`

Check: Does `DamageFragment` have all required fields?
Expected: `Velocity`, `RelativePosition`, `Mass`, `Density`, `Momentum`, `Length`, `Energy`

### 5. Combat tests
Directory: `Pulsar4X/Pulsar4X.Tests/`

Check: Do any test files cover beam weapon firing, damage application, or ship destruction?
Phase 1 is not complete without at least one damage test.

## Report Format

```
DAMAGE SYSTEM WIRING AUDIT
===========================

1. BeamWeaponProcessor.OnHit()
   Status:  [ ] SimpleDamage ACTIVE — not done
            [x] DamageProcessor ACTIVE — done
   Location: BeamWeaponProcessor.cs:LINE

2. DamageProcessor — ComponentInstancesDB block
   Status:  [ ] EMPTY — not done
            [x] IMPLEMENTED — done
   Location: DamageProcessor.cs:LINE

3. DamageProcessor — Ship destruction check
   Status:  [ ] MISSING — not done
            [x] PRESENT — done

4. DamageProcessor — Colony damage block
   Status:  [x] COMMENTED OUT — correct for Phase 1
            [ ] ACTIVE — premature, do not enable yet

5. EntityDamageProfileDB creation
   Status:  [ ] LAZY in DamageProcessor — not done
            [x] EAGER in ShipFactory — done
   Location: ShipFactory.cs:LINE

6. DamageFragment struct
   Status:  [ ] MISSING or INCOMPLETE
            [x] COMPLETE with all required fields

7. Combat tests
   Status:  [ ] NONE — Phase 1 incomplete
            [x] EXISTS: [filename(s)]

─────────────────────────────────────────────────
VERDICT:
  Phase 1 INCOMPLETE — resolve items marked [ ] above before any Phase 2 work.
  Phase 1 COMPLETE   — ready for Phase 2 (InstallationsDB UI).
```

## Rules

- Do not skip reading the actual source files. The status in PLAN.md may lag behind reality.
- Do not enable the colony damage block until Phase 3. It references types that need verification first.
- If `MassVolumeDB.Volume_km3` vs `Volume_m3` discrepancy is found in the colony block, note it — it must be resolved in Phase 3 before uncommenting.
