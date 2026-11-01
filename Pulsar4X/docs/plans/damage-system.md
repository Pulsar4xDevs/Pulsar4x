# Damage system

Status: WIP, open. Do not wire it into combat until the viewer run looks right and is cheap enough. Checked against the code on 2026-11-02: `BeamWeaponProcessor` still calls `SimpleDamage`; `KineticMath.GetFastestPart` never updates `mag` (problem 4); `UpdateComponetHealth` is only called from `DamageViewerWindow` (problem 8); `PhysicsLoop` has no gravity term. The other problems were not re-checked line by line.

WIP. Three implementations sit side by side. Combat still uses Simple because Complex and VeryComplex were never finished enough to replace it — not because they were skipped.

## How it is supposed to work

A ship, asteroid, or colony is a 2D slice of its 3D volume — a falling-sand particle grid. A kinetic fragment or laser is simulated through that grid: mass, velocity, material, heat, phase, and connectivity. Hits remove or detach particles; component health is derived from remaining particles, not a random HP roll.

**Gravity** is part of that model, not a permanent omission. In space (ships, asteroids) acceleration is ~0, so sand does not “fall.” The same sim on a planetary colony should use the body’s gravity so rubble, melt, and structure settle. Gravity is not implemented yet; `PhysicsLoop` never adds a body acceleration.

**Scale:** undamaged cells stay coarse (e.g. 1 m). Damaged / moving cells subdivide (e.g. 1 cm, then 1 mm) so large hulls stay cheap until they are actually hit.

**Persistence:** an undamaged ship shares its class `DamageProfile`. After a hit, the last sim frame becomes that ship’s profile. Fragments are stored so the UI can replay the sim without keeping every frame.

**Seeing the sim:** the bitmap / SDL textures exist so we can watch the sand run while developing. That same stream is meant to be prettied up later as a player replay of a hit. They are a view of the sim, not a separate product.

**Stretch:** the same machinery should eventually handle large contacts — ship–ship, asteroid–planet/colony — not only a weapon fragment into a hull.

## How it currently works

Three layers, newest last:

| Layer | Where | What it does | In combat? |
|---|---|---|---|
| Simple | `Simple/SimpleDamage.cs` | Placeholder: random component HP. Stays until a real sim is trustworthy | **Yes** — `BeamWeaponProcessor.OnHit` |
| Complex | `DamageComplex/` | Packed `RawBmp` as the sim grid (RGBA was a convenient cell record that also blits). R = material, G = component instance (max 255), A = health. Ray/step through pixels | **No** — not ready; `DamageProcessor.OnTakingDamage` unused |
| VeryComplex | `DamageVeryComplex/` | Same idea with explicit `PhysicalParticle`s instead of packed bytes. `PhysicsLoop`: beams → moving particles → nested DMap → collide, pressure, heat | **No** — not ready; viewer + timing tests only |

**Particle sim (`PhysicsLoop`):**

1. Time step from fastest particle (`CalculateTickLength`).
2. If the map has `BeamStarts`, `PhotonMath.BeamProcessing` walks transmit/reflect/absorb.
3. If `ParticlesPerMeter < PhysicsScale` (100 vs 1000), every moving particle gets a nested `DamageMap` and the loop recurses into it, then returns.
4. Else: move, collide (`KineticMath`), apply beam heat, reassess detachment, `PressureMath`, `TempratureMath`. No gravity term.
5. `damageMap.RunTime` is sim time; `DamagePhysicsSim.LastLoopElapsed` is wall time of the outermost loop.

**Building a map:** ships from `ShipDesign` + `ComponentInstancesDB` (placement along X, stacked on Y, materials from component resource costs). Asteroids/comets from `SystemBodyInfoDB` (grid allocated; fill still commented). Planets/moons/stars are skipped in the viewer (planet-sized grids used to freeze the debug window). Colonies are not mapped at all.

**Viewer:** `GetInstance` does not build maps. `Display` calls `Init` only while the window is open. Fire Particle / laser, StepSim, RunSimLoop live under “Fire a Weapon…”. Timing tests (`DamagePhysicsLoopTimingTests`) fire the same two paths at a Freighter: laser ~3 ms, particle ~2.6 s per step.

## Problems to fix

1. **Simple is still the combat path** because the other two are not good enough yet. Beams call `SimpleDamage` (random 100–500 HP). Hook-up is blocked on correctness and cost, not on missing a caller.
2. **Nested DMap is 1000× too big.** Nested constructor sets width/height to `PhysicsScale` (1000), not `PhysicsScale / parent.ParticlesPerMeter` (10). One kinetic particle → 1e6 sub-particles. That is the particle-step cost.
3. **Laser `ProcessBeam` can recurse on the same cell.** Collision does not skip the start cell; transmit starts at 100%. Capped at depth 256 for timing; still wrong physics (no advance, no power drop on a fully transparent hit).
4. **`GetFastestPart` never updates `mag`**, so it returns the last moving particle, not the fastest.
5. **`CalculateTickLength` used to NRE** when nothing is moving (now returns 0.1 s). Still coupled to the mag bug.
6. **Asteroid fill is commented** (`AsteroidDamageProfile`). Asteroid maps are empty grids. `DamageMap(entity, SystemBodyInfoDB)` used to treat planets as asteroids and allocate planet-sized arrays (viewer now skips non-asteroid/comet).
7. **Two sim representations, one viewer.** Complex `RawBmp` fire/replay is commented in the viewer; VeryComplex maps are what Fire/StepSim use. Designer still builds the bmp for layout preview. They are not kept in sync.
8. **Component health not applied automatically.** Viewer has a manual `updateComponetnts` button. `UpdateComponetHealth` is not called from `PhysicsLoop` or combat.
9. **`componentIDLookup` / `GenerateNewCompID`** double-increments; detachment can overflow `ushort` on large maps.
10. **Rendering cost:** `CreateTextureForFastestParticleRegion` runs `Distinct()` inside the pixel loop. Texture rebuild after every StepSim/RunSimLoop dominates UI freeze vs sim time for lasers.
11. **No bounds on Bresenham asteroid fill** (when uncommented): jitter can `GetIndex` out of range.
12. **Tests/CI:** particle timing test is ~2.6 s because of (2); will get worse if more moving particles.

## Features to add

1. **Finish a sim that can replace Simple:** fragment/beam → one trustworthy loop → component HP → destroy/scatter. Combat hook-up is the last step, after the viewer run looks right and is cheap enough.
2. **Correct multi-resolution storage:** subdivide only damaged/moving cells; child size = scale factor; collapse when the region is quiet again. Matches the original “recursive scaling array” note.
3. **Stable beam march:** skip the current cell, drop power each interaction, iterate with a cap, reject zero/NaN directions.
4. **Materials from blueprints everywhere:** armor, components, and projectiles using `ParticleMaterial` + `PartMatUniqueID` (placeholder `GetMaterialsList` still).
5. **Asteroids:** fill ice/rock profile, fracture (`AsteroidDamageDB`), spawn sub-bodies. Cap map size.
6. **Debris / sister maps:** `HandleOutOfBounds` has a todo to move particles onto a neighbour map instead of deleting them.
7. **Replay without storing frames:** keep the fragment list on `EntityDamageProfileDB` and re-sim for the player UI (already intended).
8. **Designer ↔ combat layout:** one source of component placement for the designer preview and the sim grid.
9. **Pretty replay:** the debug textures are the working view; later, the same fragment list re-simmed for the player.
10. **Kinetic weapons** through the same inject-into-map path as the viewer projectile, once that path is solid.
11. **Phase / pressure effects that matter:** melt, vent, structural break already sketched in heat/pressure/detachment — they need to feed component failure and decompression.
12. **Performance budget:** target milliseconds per hit at ship scale (timing tests as regression). After (2), add a budget assert so nested maps cannot silently return to 1e6 cells.
13. **Optional gravity:** per-map acceleration (0 in space, body *g* on colonies). Same sim, not a second engine. Colony layout (hex/installations → cells) plus a cap so a planet is never voxelized at ship resolution.
14. **Large contacts (stretch):** ship–ship, asteroid–planet/colony. Two maps (or two windows) overlapping in a shared frame, not one hull-sized array of the whole planet. Same gravity/sleep/refine rules; much stricter cell budget.
