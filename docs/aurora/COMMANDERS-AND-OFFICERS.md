# Aurora 4X — Commanders & Officers (Design Reference)

Source: aurora-manual `16-commanders/` (v2.7.1). Constants approximate — verify before hard-coding (see `INDEX.md`).

> **In plain terms:** Every ship, fleet, ground formation, colony, and research lab can have an **officer** in charge, and a good officer makes that thing work noticeably better. Officers have **skills** (a ground commander might be +30% on the attack, a scientist +25% in engines) and a **rank** that decides what they're allowed to command. You grow officers at a **Military Academy** (about 10 new ones a year). For our project this matters a lot: **ground commanders directly multiply ground-combat results** — attack, defense, artillery, supply use, how big a garrison you need. So the officer system isn't flavor; it's a real combat modifier we'll have to feed into ground combat.

---

## 1. Where officers come from

- A new empire starts with **~300** officers.
- **Naval Academy** (ground installation, 2,400 BP + minerals, 50,000 workers) produces **~10 officers/year**.
- Default mix without a commandant: **60% naval, 25% ground, 8% administrator, 7% scientist** (a commandant shifts the mix toward their own type).
- Academy quality setting trades quantity for quality (1 = many/weak … 5 = few/strong).
- **Promotion:** must wait ≥1 year between promotions; promotion score thresholds rise by rank.
- **Retirement:** naval eligible at 10 yrs (+5/rank), ground at 20 yrs (+5/rank), scientists/admins 40 yrs. Unassigned officers retire **twice** as fast.

---

## 2. Rank hierarchy (naval)

Lieutenant Commander → Commander → Captain → Rear Admiral (lower) → Rear Admiral (upper) → Vice Admiral → Admiral → Fleet Admiral. (Ground forces have their own parallel ladder — Lieutenant up through General.)

**A ship's required rank rises with what's installed:** survey gear/jump drive = +1; weapons, hangars, main engineering, flag bridge = +2. Ships ≤1,000 t default to lowest rank. Ships also carry junior officers (XO, chief engineer, tactical officer) one or two ranks below the commander.

---

## 3. Skills & bonuses (the part that matters for combat)

Bonuses run **0–150%**. A commander applies their bonus to the thing they command; if they command more than their capacity, the bonus scales down.

### Ground commander skills — **directly drive ground combat**
| Skill | What it does |
|-------|--------------|
| Ground Combat Offence (GCO) | better attack accuracy |
| Ground Combat Defence (GCD) | better defensive bonus |
| Fortification | bigger dug-in bonus |
| Ground Combat Manoeuvre (GCM) | more breakthrough |
| Ground Combat Artillery (GCA) | better indirect/bombard fire |
| Ground Combat Anti-Aircraft (GCAA) | better air defense |
| Ground Combat Logistics (GCL) | **uses less supply** |
| Ground Combat Occupation (OCC) | **smaller garrison needed** |
| Ground Combat Tactics / Training (GCT) | overall effectiveness + faster morale recovery |
| Anti-Insurgency | suppresses unrest |
| Survey / Xenoarchaeology / Decontamination | field-specialist jobs |

### Naval commander skills (benchmark)
Ship Handling, Crew Training, Fleet Tactics, Energy/Kinetic/Missile Weapons, Electronic Warfare, Carrier Operations, Survey, Engineering, Diplomacy, Intelligence, Logistics, Mining, Production, Terraforming — each 0–150%.

### Scientists
One **primary** research field (huge in-field bonus) + maybe secondaries. See `RESEARCH-AND-TECH.md`.

**Crew training** (separate from officer skill): grade points 0–1,000, bonus = `SQRT(GradePoints) − 10` (max ~21.6%), accrues yearly from commander + XO bonuses.

---

## 4. Pulsar status & mapping

Pulsar **already has** people: `GameEngine/People/` (`CommanderFactory.CreateShipCaptain`, `CreateScientist`, ranks, commission dates — seen in `ColonyFactory`/`ShipFactory`), and a `CommanderWindow`. Whether naval/ground combat bonuses are *applied* to outcomes is unverified.

| Aurora idea | Pulsar | Relevance to objective |
|-------------|--------|------------------------|
| Officer entity + rank | `People/` (exists) | reuse for **ground commanders** — don't invent a new person system |
| Ground combat bonuses (GCO/GCD/…) | likely not applied | **build:** when writing `GroundCombatProcessor`, read the assigned commander's bonuses and fold them into the to-hit / supply / garrison math (`GROUND-COMBAT.md` §4, §7) |
| Academy produces officers | verify in `People/`/`Industry/` | academy = an installation-component |
| Command capacity scaling | verify | apply the "over capacity → reduced bonus" rule |

**Takeaway:** ground combat must *consume* commander bonuses to have Aurora-depth. Officers already exist in `People/`; the new work is reading their ground skills inside the ground-combat math. `CONVENTIONS.md` §6, §9 (resolve the commander via ID).
