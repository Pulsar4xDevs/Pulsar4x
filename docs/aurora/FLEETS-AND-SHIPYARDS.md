# Aurora 4X — Fleets, Task Groups & Shipyards (Design Reference)

Source: aurora-manual `9-fleet-management/` (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** Ships are **built** at shipyards and **organized** into fleets. A shipyard has "slipways" (build slots) and a tonnage limit (biggest ship it can build); you grow both over time, and switching a yard to a new ship design costs "retooling" time. Once built, ships travel as **fleets (task groups)** — you give orders to the fleet, not to single ships, and the fleet moves at the speed of its **slowest** member. This matters to us in two ways: ground forces are **built** through a construction system just like ships, and an invasion is **delivered** by a transport fleet — so the same build-and-move machinery applies.

---

## 1. Shipyards (where ships are built)

| Yard type | Builds | Start capacity | Workers/ton |
|-----------|--------|---------------:|------------:|
| Naval | warships (military parts) | 1,000 t | 250 |
| Commercial | freighters/colony/support | 10,000 t | 25 |
| Light Naval | military ships ≤1,000 t | 1,000 t (no retool) | — |
| Repair | repairs only | = commercial | — |

- A yard has **slipways** (parallel build slots) and a **tonnage capacity** (max ship size). Both grow via construction tasks.
- Each slipway ≈ **400 BP/year** (researchable). Build time = ShipBP ÷ (slipways × BP/yr).
- **Retooling:** assigning a new primary class costs time (first assignment: time only, no BP/minerals). A "secondary class" builds without retooling if its refit cost < 20% of the primary.
- Workers needed = WorkersPerTon × slipways × capacity (a 10,000-t naval yard, 1 slipway = 2.5M workers).

(Ships also cost **build points + minerals**, drawn from the colony stockpile, same as installations — see `PLANETARY-INFRASTRUCTURE.md`.)

---

## 2. Fleets / task groups (how ships move & fight)

- **Every ship belongs to a task group**; all movement and standing orders are issued at the **fleet** level.
- **Fleet speed = slowest ship.** A "Use Maximum Speed" flag recalculates when composition changes. Fuel is time-based, so throttled ships burn proportionally less.
- **Sub-fleets** nest for organization (move with the parent) and can **detach** into independent fleets — useful for splitting off a transport or escort group.
- **Initiative** (capped by the commander's leadership) sets move order in combat: low initiative moves first (forces engagement); high initiative moves last (better interception / escape).
- **Conditional orders** auto-react to fuel level, ammo, shields, contacts, etc. ("Always set a low-fuel conditional on combat groups" — a fleet that runs dry in enemy space is lost.)
- Practical doctrine: group ships by **speed tier** (don't let 3,500 km/s cruisers drag down 5,000 km/s destroyers). Standard group types: Combat, Survey, Transport, Logistics, Patrol.

---

## 3. Pulsar status & mapping

Pulsar **already has** fleets and ship building: `GameEngine/Fleets/` (`FleetFactory`, `FleetDB`, fleet orders, flagship), `Ships/ShipFactory`, and shipyard installations (see `DefaultStartFactory.ShipYard(...)`). Movement/jumps are in `Movement/` (well-documented). This is mostly **benchmark/infrastructure that already exists.**

| Aurora idea | Pulsar | Relevance to objective |
|-------------|--------|------------------------|
| Shipyard builds ships from BP+minerals | `Industry/` + shipyard component (exist) | **ground forces build the same way** — a Ground Force Construction Complex is just another build facility (`GROUND-COMBAT.md` §7) |
| Task group / fleet orders | `Fleets/` (exist) | an **invasion fleet** is a transport task group — reuse fleet movement + a new "land troops" `INavAction` (`Movement/CLAUDE.md`) |
| Fleet speed = slowest | `Fleets/`/`Movement/` | reuse as-is |
| Detach sub-fleet | `Fleets/` | reuse for splitting the assault echelon |

**Takeaway:** building and moving forces is already solved in Pulsar. Ground combat **reuses** the construction pipeline (to build units) and the fleet/movement pipeline (to deliver them) — the new work is the unit, the landing action, and the ground battle, not the transport. `CONVENTIONS.md` §5–6.
