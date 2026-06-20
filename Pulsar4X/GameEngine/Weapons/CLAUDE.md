# Weapons — Subsystem Reference

Space combat weapons: beam weapons, missiles, fire control. Lives in `GameEngine/Weapons/`.

---

## File Map

| File | Purpose |
|------|---------|
| `WeaponBeam/BeamInfoDB.cs` | DataBlob for an in-flight beam entity. Holds target, velocity, energy, position pair, state machine state. |
| `WeaponBeam/BeamWeaponProcessor.cs` | HotloopProcessor (1 sec). Advances beam state machine: Fired → AtTarget/MissedTarget. Calls damage on hit. |
| `WeaponBeam/GenericBeamWeaponAtb.cs` | Component design attribute. Configures beam energy, wavelength, velocity. Has `FireWeapon()` called by GenericFiringWeaponsProcessor. |
| `WeaponFireControl/FireControlAbilityDB.cs` | DataBlob: list of FireControlAbilityStates (one per fire control unit on the ship). |
| `WeaponFireControl/FireControlAbilityState.cs` | State for one fire control: assigned weapons, target entity, active/cease-fire. |
| `WeaponFireControl/BeamFireControlAtbDB.cs` | Component attribute for beam fire control. |
| `WeaponGeneric/GenericFiringWeaponsDB.cs` | DataBlob: arrays of weapon states per weapon slot. Internal magazine qty, reload rate, fire control assignments. |
| `WeaponGeneric/GenericFiringWeaponsProcessor.cs` | HotloopProcessor (1 sec). Iterates all weapon slots, fires when ammo ≥ min shots and target valid, reloads. |
| `WeaponGeneric/GenericWeaponAtb.cs` | Component attribute for generic weapons (ammo capacity, reload rate, damage). |
| `WeaponGeneric/WeaponState.cs` | Per-weapon state struct (internal magazine current amount, name). |
| `WeaponMissile/MissleLauncherAtb.cs` | Component attribute: configures missile launcher. Has `FireWeapon()` → `MissileProcessor.LaunchMissile()`. |
| `WeaponMissile/MissleLauncherAbilityDB.cs` | DataBlob for missile launcher ability. |
| `WeaponMissile/MissleProcessor.cs` | Static class. `LaunchMissile()` creates missile entity with NewtonMoveDB + ordnance design components. |
| `WeaponMissile/ProjectileInfoDB.cs` | DataBlob on a missile entity: who launched it, count. |
| `OrdnanceDesign.cs` | Missile/ordnance design data (wet/dry mass, burn rate, exhaust velocity, payload). |
| `OrdnanceDesignFromJson.cs` | JSON loading for ordnance designs. |
| `OrdnancePayloadAtb.cs` | Component attribute: warhead payload type. |
| `SetFireControlOrder.cs` | Order to assign a fire control to a target and set fire mode (OpenFire / CeaseFire). |
| `IFireWeaponInstr.cs` | Interface: `FireWeapon(owner, target, shots)`. Implemented by weapon component attributes. |
| `WeaponUtils.cs` | Static helpers: `TimeToTarget()`, `ToHitChance()`, `PredictTargetPositionAndTime()`. |

---

## How Space Combat Works

### 1. Fire Control Assignment (player action)
```
Player → SetFireControlOrder → FireControlAbilityState.Target = targetEntity
                                                               .FireMode = OpenFire
```

### 2. Weapon Firing (HotLoop, every 1 sec)
```
GenericFiringWeaponsProcessor.UpdateWeapons(GenericFiringWeaponsDB db)
  for each weapon slot:
    shots = floor(internalMagQty / amountPerShot)
    if shots >= minShotsPerFire AND target.IsValid:
      fireInstructions[i].FireWeapon(owner, target, shots)
          ↓
      BeamWeaponAtb.FireWeapon() → BeamWeaponProcessor.FireBeamWeapon()
      OR
      MissleLauncherAtb.FireWeapon() → MissileProcessor.LaunchMissile()
    
    reload: internalMagQty = min(internalMagQty + reloadRate, magSize)
```

### 3. Beam Physics (HotLoop, every 1 sec)
```
BeamWeaponProcessor.UpdateBeam(BeamInfoDB)
  State: Fired
    → calc timeToTarget = distance / beamVelocity
    → if timeToTarget <= deltaSeconds:
        → CalculateHit() → RNG check using ToHitChance()
        → if hit: state = AtTarget, beam positioned at target
        → if miss: state = MissedTarget, energy dissipates 10%/sec
  State: AtTarget
    → OnHit() → **SimpleDamage.OnTakingDamage(target, 100, 500)**  ← PLACEHOLDER
    → OwningEntity.Destroy() (remove beam from game)
  State: MissedTarget
    → UpdatePhysics() → move beam forward
    → decay energy, destroy when energy == 0
```

### 4. Missile Launch
```
MissileProcessor.LaunchMissile(launcher, target, launchForce, design, count)
  → creates Entity with: ProjectileInfoDB, ComponentInstancesDB, PositionDB,
                          MassVolumeDB, NameDB, NewtonMoveDB, OrderableDB
  → adds components from OrdnanceDesign (propulsion, warhead, sensors)
  → issues NewtonThrustCommands for orbital phasing maneuvers toward target
  → removes missile from launcher cargo
```

---

## Damage Status (CRITICAL — read before modifying)

**The complex damage path is disabled.** In `BeamWeaponProcessor.OnHit()` (~line 134):

```csharp
// COMMENTED OUT — the real path:
// DamageProcessor.OnTakingDamage(beamInfo.TargetEntity, damage);

// ACTIVE — the placeholder:
var damageResult = SimpleDamage.OnTakingDamage(beamInfo.TargetEntity, 100, 500);
```

`SimpleDamage.OnTakingDamage(entity, min, max)`:
- Picks a random component from `ComponentInstancesDB.AllComponents`.
- Deals `RNG.Next(100, 500)` damage to that component's `HTKRemaining`.
- If HTK ≤ 0, removes the component.
- If no components remain, calls `ShipFactory.DestroyShip()`.

This works minimally but has no armor, no shields, no spatial component placement, and uses hardcoded magic numbers. **Phase 1 priority: replace with `DamageProcessor.OnTakingDamage()`.**

---

## Missile Guidance Status (PARTIAL)

`MissileProcessor.LaunchMissile()` has `bool directAttack = false` hardcoded. The `false` branch uses orbital phasing maneuvers via `InterceptCalcs.OrbitPhasingManuvers()`. The `true` branch uses `ThrustToTargetCmd` (direct pursuit). The commented-out direct-attack targeting code is in lines ~93–105.

Phasing maneuvers work for targets in stable orbits but may fail for moving ships. Fix needed before ground combat strikes (which would use the missile path).

---

## Adding a New Weapon Type

Pattern (copy beam weapon approach):

1. Create `WeaponXxx/XxxAtb.cs` implementing `IComponentDesignAttribute` and `IFireWeaponInstr`.
2. In `FireWeapon()`, either create a new entity (like BeamInfoDB) or call a processor static method (like MissileProcessor).
3. If the weapon has in-flight physics, create `XxxInfoDB.cs` (DataBlob) and `XxxProcessor.cs` (IHotloopProcessor).
4. The processor auto-registers — no manual setup needed.
5. Register the weapon's component template in `Data/basemod/blueprints/components/`.

---

## Gotchas

1. `ValidateTargetExists()` in GenericFiringWeaponsProcessor only sends CeaseFire once even if multiple fire controls have invalid targets. This is a minor bug — all invalid targets should receive CeaseFire.

2. `BeamWeaponProcessor.CalculateHit()` uses a hardcoded 95% base hit chance: `WeaponUtils.ToHitChance(..., 0.95)`. This is a placeholder. The actual hit-chance formula should account for evasion, range, and beam divergence.

3. Beam entities are added to the same `StarSystem` as the firing ship. If the target is in a different system (impossible currently, but keep in mind for future multi-system combat), this would break.

4. `MissleLauncherAbilityDB` is spelled with one 's' — `Missle` not `Missile` throughout this directory. Do not "fix" the spelling in file/class names without updating all references.
