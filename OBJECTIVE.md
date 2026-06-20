# Pulsar4X — What We're Building (The Actual Goal)

This is the concrete spec. Not "space marines." What it actually is, what it looks like when done, and what order we build it in.

---

## The End State

When this is finished, Pulsar will have **two combat theaters with equal depth**.

**Space combat** (already built) lets you:
- Group ships into fleets
- Assign individual fire controls to individual ships within a fleet
- Watch beam and missile exchanges resolve in real time
- Lose ships to component-level damage (once we fix the damage bug)

**Ground combat** (what we're building) will let you:
- Group ground units into formations (just like fleets)
- Assign fire orders to individual squads, tank columns, artillery batteries within a formation
- Watch exchanges resolve on the ground while your ships control orbit
- Lose units to the same damage system ships use (same core, different skin)

The level of resolution stops at the **squad / vehicle column**. You don't direct individual soldiers. A squad of marines is the smallest thing you click on — same as a ship is the smallest thing you click on in space combat. Platoons, batteries, armor columns: those are the atoms.

---

## The Combat Model

### The Hierarchy

```
Formation  (like a fleet)
  └── Formation Element  (like a ship — squad, tank column, artillery battery)
        └── Equipment  (like ship components — weapons, armor, HQ gear, sensors)
```

- The **player operates at Formation level** for bulk orders (move here, attack that colony).
- The **player can also operate at Formation Element level** for fire control assignment — exactly like telling a specific ship to target a specific enemy ship with a specific weapon.
- Individual units inside an element are not individually clickable in the UI. They're abstracted as element strength (like a ship's HP).

### What Aurora Got Right (We Keep This)

- **Component-based unit design.** A marine squad is a design you compose: you pick weapons (energy rifle atb, gauss cannon atb), armor rating, a logistical package, a sensor package. Same design interface as ships. Same construction pipeline.
- **Fortification digs in over time.** A ground unit sitting still gets harder to kill — dig in rating goes up by roughly 0.5/day, capped at 5. Attackers pay extra to dislodge.
- **Terrain modifies combat.** Mountains, cities, jungle, open plains each have an attack/defense modifier. A fortified infantry unit in a mountain = nightmare to dislodge.
- **Orbital support.** Ships in orbit can bombard. Missile impacts reduce fortification, kill troops, damage installations. This is what makes naval supremacy matter.
- **Supply (GSP).** Each ground unit has a supply pool that depletes in combat. Supply lines matter. Cut them and units degrade. This ties the ground war to the logistics system already in Pulsar.
- **Occupation after conquest.** Taking a planet doesn't end the mission. The population's "determination + militancy + xenophobia" score drives unrest. You need garrison troops to hold the planet, or production tanks and rebellion starts. This is the aftermath loop.

### Where We'll Diverge (Aurora Is the Starting Point, Not the Law)

Aurora's exact constants (combat round timing, exact penetration formula exponents, exact fortification rate) are approximate — the sources disagree on some of them and Aurora's actual values require in-game verification (see `docs/aurora/INDEX.md`, Known Discrepancies table). We'll implement the **shape** of every mechanic with a named constant for every number, so tuning is a config change, not a code change. We will diverge intentionally on anything that feels wrong in play.

Divergence is expected and good. Document it in the relevant `docs/aurora/` file when it happens.

---

## The Three Phases of What We're Building

### Phase A — Infrastructure (Build the Thing Worth Fighting Over)

**What it looks like when done:** You can click on any colony and see what's on it — mines, factories, research labs, ordnance plants. You can queue new ones for construction. The population formula is real (not a placeholder). You can see how many people a colony supports and why. Terraforming works as an installation you can build that slowly makes hostile worlds less hostile.

Nothing fights yet. But now there's something on the ground worth invading.

**Concrete deliverables:**
1. Fix the Installations tab in `PlanetaryWindow` — re-gate it on `ComponentInstancesDB` (which every colony has) instead of the dead `InstallationsDB` (which nothing has). Render what's actually there.
2. Replace the population stub in `PopulationProcessor` with the real Aurora formula: `MaxPop(millions) = Infrastructure / (ColonyCost × 100)`, with the 33%→100% capacity growth-decline curve instead of the −50% die-off placeholder.
3. Implement terraforming as a component (installation type) with a `TerraformingProcessor` that ticks the atmosphere toward breathable — reads and writes `AtmosphereDB`.
4. Add the occupation/unrest data model to colonies: political status, unrest level, required garrison strength. This sets up Phase C without building the combat yet.

---

### Phase B — Orbital Bombardment (Connect Space to Ground)

**What it looks like when done:** A ship in orbit over an enemy colony can issue a bombardment order. Missiles and kinetic strikes land on the surface. They damage installations (reduce component HP), kill population (stochastic based on yield and atmosphere), and reduce fortification. If you bomb long enough, you can wreck a colony before ever landing troops. But collateral damage stacks unrest, making the planet harder to hold after you take it.

**Concrete deliverables:**
1. Fix the commented-out complex damage system first (prerequisite — `BeamWeaponProcessor.OnHit()` must call `DamageProcessor.OnTakingDamage()` not `SimpleDamage`). This is the damage code both ships AND colonies will share.
2. Wire the commented-out colony damage block in `DamageProcessor.cs` (~lines 101–181) — replace missing types with current equivalents, add population casualty calculation.
3. Add a `BombardOrder` that a ship in orbit can issue against an enemy colony.
4. Show the bombardment effect in `PlanetaryWindow` — which installations took damage, population count delta.

---

### Phase C — Ground Combat (The Main Event)

**What it looks like when done:** You design ground units from components (weapons, armor, logistics). You build them in colonies with a Ground Force Construction Complex. You load them into transport ships. You fly them to the target, establish orbital dominance, and issue a landing order. Combat resolves in rounds — your formations exchange fire with the defender's formations, morale and strength degrade, and eventually one side breaks. Then you garrison the planet, watch unrest tick, and keep enough troops there to stop a revolt.

**Concrete deliverables:**
1. Data model: `GroundUnitDB`, `GroundUnitDesign`, `GroundUnitFactory`, `FormationDB` — in a new `GameEngine/GroundForces/` directory. Units are entities in the StarSystem, exactly like ships.
2. Unit design: weapons/armor/logistics as components with `*Atb` attributes. Design window in the client.
3. Production: `GroundUnitConstructableDesign` plugs into the existing industry pipeline. A colony with a GFCC can build units.
4. Transport: units load into transport ship cargo bays. Transport moves to target system.
5. Landing: `InvasionOrder` checks for orbital supremacy, lands troops, attaches `GroundCombatDB` to the colony.
6. Combat resolution: `GroundCombatProcessor` (HotloopProcessor) runs the round — fire orders, hit/penetrate/destroy rolls, morale/supply tracking.
7. UI: `GroundCombatWindow` — formation display, element fire-control assignment (mirrors `FireControlWindow`), combat log. `PlanetaryWindow` gets a "Ground Combat" tab when `GroundCombatDB` is present.
8. Occupation: garrison duty reduces unrest; below minimum garrison, unrest rises; at unrest threshold, production penalties → revolt event.

---

## Dependency Order

```
Phase A (Infrastructure) ──────────────────────┐
                                               ▼
Phase B (requires damage fix first, then       Phase C (Ground Combat)
         orbital bombardment)  ────────────────┘
```

You can't do Phase B until the complex damage system is wired. You can do Phase A completely in parallel with fixing the damage system. Phase C needs both — you need something to fight over (A) and a way to bombard it (B) before the ground fight matters.

**Start here: Phase A, step 1 — fix the Installations tab.**

---

## The One Rule That Governs All of This

> **Mirror the space combat architecture, not Aurora's code.**

Aurora is a monolithic C# app with a SQL database. Pulsar is component-based entities and processors. Every ground mechanic gets expressed as:
- A `DataBlob` that holds state
- A `Processor` that acts on it each tick
- A `ComponentDesign` / `*Atb` for equipment
- An `Order` for player input

See `CONVENTIONS.md` for Pulsar's exact idioms. See `docs/aurora/GROUND-COMBAT.md` for the Aurora mechanic spec with the Pulsar-mapping table. The rule in `CONVENTIONS.md` §6 — "abilities are components, do NOT invent parallel systems" — is the single most important design constraint.

---

## What "The Same Depth" Means

Space combat in Aurora (and in Pulsar once the damage fix lands) has:
- Individual fire control assignment per ship
- Component-level damage (specific weapons can be destroyed, engines can be crippled)
- Multiple weapon types with different trade-offs
- Fleet coordination mechanics
- Meaningful logistics (fuel, maintenance)

Ground combat at the same depth means:
- Individual fire control assignment per formation element (squad/column)
- Equipment-level damage (a unit can lose its heavy weapons, its comms, its sensor package)
- Multiple unit types with different trade-offs (infantry vs armor vs artillery vs support)
- Formation coordination mechanics
- Meaningful logistics (GSP supply pool, supply lines)

That's the bar. Not an abstraction that just outputs "attacker wins / defender wins." A simulation you can stare at and understand why you're losing.
