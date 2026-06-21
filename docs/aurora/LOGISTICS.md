# Aurora 4X — Logistics: Fuel, Maintenance, Supply (Design Reference)

Source: aurora-manual `14-logistics/` + `9.2` (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** Ships and forces can't fight forever on their own — they need a supply train, just like a real fleet. Three things run out: **fuel** (made by refining a mineral called Sorium), **maintenance supplies** (spare parts; without them, parts start randomly breaking the longer a ship stays out), and **ordnance** (missiles). You keep forces in the field with **tankers** (fuel), **supply ships** (spare parts), and **colliers** (missiles), plus ground bases that refuel and rearm. The big lesson Aurora teaches: a fleet that can't be resupplied is already dead — it just doesn't know it yet. For us, this is the model for how **ground forces get their supply (GSP)** and how **colonies maintain orbiting warships**.

---

## 1. Fuel

**Made from Sorium** (a mined mineral) at **Fuel Refineries:**
- Base: **40,000 L/year** per refinery (→ 280,000 L/yr at top tech, 11 levels).
- Cost: 120 BP + 120 Boronide.
- **Sorium Harvesters** on ships pull fuel straight from gas-giant atmospheres (base 10/module).

**Storage tanks** (ship components) scale from 100 L (0.002 HS) to 5,000,000 L (100 HS); bigger = cheaper per liter.

**Consumption:** engines burn fuel based on size, power, and speed (faster ≈ much thirstier — see `SHIP-DESIGN.md` engine power-vs-fuel table). Range ≈ `Fuel ÷ ConsumptionRate × Speed`.

**Refueling:** colonies hold unlimited fuel; **tankers** (ships with spare tankage + the "Tanker" flag) refuel others underway. A "minimum fuel" setting keeps tankers from giving away their own reserve.

---

## 2. Maintenance (the "stay out too long and things break" system)

Every deployed warship runs **two clocks**, both reset only by a full **overhaul** at a naval shipyard:

| Clock | Tracks | Governs |
|-------|--------|---------|
| **Maintenance clock** | time since overhaul | chance of random component **failures** |
| **Deployment clock** | time away from port | crew **morale** |

- Past the ship's rated maintenance life, failure chance climbs from ~0 toward near-certain. A failed component stops working until repaired.
- **Death spiral:** failures can wreck crew quarters → overcrowding → deployment clock runs **4–5× faster** → more failures → … → at extreme over-deployment a ship can **explode**.
- Refuel/resupply do **not** reset the clocks — only overhaul does.

**Maintenance Supply Points (MSP)** = spare parts, made by **Maintenance Facilities** (25,000 t, 60 BP, 50,000 workers; 20 MSP/yr base → 100 at top tech). Each MSP costs 0.1 Duranium + 0.1 Gallicite + 0.05 Uridium. Ships carry MSP in storage bays and auto-repair failures using them.

- A facility supports a tonnage of orbiting military ships (1,000 t base → 6,250 t with tech). If total ship tonnage exceeds capacity, everyone gets a proportional **Effective Maintenance Rate** (e.g. 80% capacity → 80% MSP use, 20% failure chance).
- Normal MSP draw ≈ ClassCost/4; full overhaul ≈ ClassCost.
- **Commercial-engine ships are exempt from maintenance failures** (that's their whole point — freighters/colony ships run forever).

---

## 3. Supply ships & transfer infrastructure

| Ship type | Carries | Needs |
|-----------|---------|-------|
| Tanker | fuel | spare tankage + Tanker flag |
| Supply ship | MSP (spare parts) | maintenance storage bays |
| Collier | ordnance (missiles) | Ordnance Transfer System (500 t / 10 HS) |
| Freighter / Colony ship / Tug | cargo / colonists / towing | cargo holds, cryo bays, tractor |

**Ground/orbital transfer facilities** (service unlimited ships at once):
| Facility | BP | Cargo points |
|----------|---:|-------------:|
| Spaceport | 3,000 | 1,000,000 |
| Ordnance Transfer Station | 1,200 | 250,000 |
| Ordnance Transfer Hub | 2,400 | 100,000 t |

Base transfer rates: cargo 20 s/point, colonists 10 s/each, MSP 6 min/each — sped up by cargo-handling tech, spaceports, and commander **Logistics** bonus. **Underway Replenishment** tech lets transfers happen while moving (20% → 100% of dockside rate across 5 levels).

---

## 4. Pulsar status & mapping

Pulsar **already has** the bones: `GameEngine/Logistics/` (automated cargo routes), `Storage/` (cargo holds), `Industry/` (fuel refining as a component ability), and fuel tracked in `NewtonThrustAbilityDB` (see `Movement/CLAUDE.md`). Maintenance/MSP and the two-clock system appear **not** to be implemented.

| Aurora idea | Pulsar | Relevance to objective |
|-------------|--------|------------------------|
| Fuel refining / tankers | `Industry/`, `Logistics/`, `Storage/` (exist) | reuse for supplying invasion fleets |
| MSP / maintenance clocks | not implemented | **optional depth** — not required for ground combat; note as a gap, don't build unless asked |
| Ground supply (GSP) | not implemented | **build new**, but model it like fuel/MSP: a consumable a unit carries and a supply ship/base replenishes. See `GROUND-COMBAT.md` §5. |
| Transfer facilities (spaceport) | partially (installations = components) | a "spaceport" is just another installation-component |

**Takeaway:** when building ground-force supply, copy the fuel/MSP pattern (a stored consumable + a resupply order), don't invent a new mechanic. `CONVENTIONS.md` §6.
