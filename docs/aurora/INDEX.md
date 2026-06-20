# Aurora 4X Design Reference — Index

Pulsar4X is a fan recreation of **Aurora 4X** (Steve Walmsley's C# space sim). For the subsystems Aurora already has and Pulsar does not (ground combat, deep planetary infrastructure), **Aurora is the design spec.** These documents capture Aurora's mechanics so we can diff them against Pulsar's current code and implement the gaps in Pulsar's own idiom.

> **These are a design reference, not an implementation spec.** Numbers here were extracted from secondary sources (community wiki + a community manual) and summarised by a model. Treat all specific values as *approximately correct, to be verified against the actual game before they are hard-coded.* Where sources disagreed, both values are shown and flagged ⚠️. The *shape* of each mechanic is reliable; the *exact constants* are not.

---

## Documents

| Doc | Covers | Maps to Pulsar |
|-----|--------|----------------|
| `GROUND-COMBAT.md` | Ground unit design, formations, combat resolution, transport, orbital drop, boarding, invasion/occupation | **All new** — no ground combat exists in Pulsar |
| `PLANETARY-INFRASTRUCTURE.md` | Installations, construction, mining, the economic loop | `Colonies/`, `Industry/`, `Energy/` — partially built |
| `COLONY-ENVIRONMENT-AND-POPULATION.md` | Colony cost/habitability, population & carrying capacity, workforce, terraforming, **unrest/occupation** | `Colonies/` — partial; supplies real formulas for the stubbed PopulationProcessor |
| `SPACE-COMBAT-BENCHMARK.md` | Aurora naval combat depth (the bar we are matching) + pointers to Pulsar's existing implementation | `Weapons/`, `Damage/`, `Sensors/` — built, see those CLAUDE.md |
| `SHIP-DESIGN.md` | Hull/tonnage, layered armor, engines, shields — the template ground-unit design copies | `Ships/`, `Damage/` — built |
| `SENSORS-AND-DETECTION.md` | Thermal/EM signatures, passive vs active sensors, EMCON/stealth | `Sensors/` — built |
| `RESEARCH-AND-TECH.md` | Research labs, scientists, the 9-category tech tree (incl. Ground Combat), prototypes | `Tech/` — built |
| `DIPLOMACY-AND-INTEL.md` | Relationship tiers, communication, treaties, ELINT intelligence | `Factions/` — minimal; **furthest from objective** |
| `LOGISTICS.md` | Fuel, maintenance (MSP/clocks), supply ships, transfer facilities | `Logistics/`, `Storage/`, `Industry/` — partial |
| `COMMANDERS-AND-OFFICERS.md` | Officer generation, ranks, **ground + naval skill bonuses** | `People/` — exists; bonuses maybe not applied |
| `EXPLORATION-AND-SURVEY.md` | Geological survey (minerals), gravitational survey (jump points) | `GeoSurveys/`, `JumpPoints/`, `Industry/MineralsDB` — built |
| `MISSILES-AND-FIRE-CONTROL.md` | Missile design, point defense, electronic warfare | `Weapons/` — built; missile guidance half-finished |
| `FLEETS-AND-SHIPYARDS.md` | Shipyards (build), task groups (move), fleet orders | `Fleets/`, `Ships/`, `Movement/` — built |

**Three groups — read by what you're doing:**

1. **Core — build new** (Pulsar doesn't have these; this is the objective):
   `GROUND-COMBAT.md`, `PLANETARY-INFRASTRUCTURE.md`, `COLONY-ENVIRONMENT-AND-POPULATION.md`.

2. **Direct support — Pulsar has the framework, ground combat plugs into it** (read when wiring the relevant hook):
   `COMMANDERS-AND-OFFICERS.md` (ground bonuses feed combat), `LOGISTICS.md` (supply/GSP), `EXPLORATION-AND-SURVEY.md` (minerals feed economy), `RESEARCH-AND-TECH.md` (unlock ground tech), `SHIP-DESIGN.md` (the unit-design template), `FLEETS-AND-SHIPYARDS.md` (build + deliver forces).

3. **Benchmark / calibration — already built in Pulsar, read to gauge "the same depth," little new work**:
   `SPACE-COMBAT-BENCHMARK.md`, `SENSORS-AND-DETECTION.md`, `MISSILES-AND-FIRE-CONTROL.md`, `DIPLOMACY-AND-INTEL.md`.

> Every doc ends with a **"Pulsar status & mapping"** section translating Aurora mechanics to the concrete Pulsar DataBlob/Processor/subsystem. The recurring rule across all of them: **reuse the existing framework, don't build a parallel one** (`CONVENTIONS.md` §6).

### Coverage — what's intentionally NOT mirrored

The reference covers all the major *simulation* systems (13 docs). A few aurora-manual chapters are deliberately skipped because they describe **how to play Aurora**, not **how Aurora's simulation works**, or because **Pulsar already implements them**:

- *Introduction, Game Setup, User Interface* (manual ch. 1–3): about operating the Aurora program itself — irrelevant to Pulsar's engine.
- *Star Systems / Planets / Asteroids generation* (ch. 4): Pulsar already generates these — see `GameEngine/Galaxy/`.
- *Navigation / jump transit* (ch. 10): Pulsar already implements — see `GameEngine/Movement/CLAUDE.md`.
- *Advanced topics — spoiler races, SpaceMaster mode, late-game strategy* (ch. 18): player strategy / hidden content, not core mechanics. (Time-increment mechanics from 18.2 are already understood — see `GameEngine/CLAUDE.md` on `ManagerSubPulse`.)

If a future need touches one of these, read the Pulsar subsystem first; only consult the Aurora manual chapter if Pulsar's behavior is unclear.

---

## How to use these docs

1. **Before designing a new ground/infrastructure system**, read the relevant Aurora doc for the intended mechanic, then read the Pulsar subsystem CLAUDE.md for what already exists.
2. **Mirror Pulsar's architecture, not Aurora's code.** Aurora is a monolithic single-threaded C# app with a SQL-backed data model. Pulsar is a hybrid-ECS with DataBlobs + Processors. A mechanic from Aurora must be re-expressed as DataBlobs + a Processor (see `CONVENTIONS.md`).
3. **Each Aurora doc ends with a "Pulsar Mapping" table** translating Aurora concepts to the concrete Pulsar DataBlob/Processor/Order that would implement them.

---

## Source list

Primary (fetchable):
- **aurora-manual** (community manual, GitHub) — `https://github.com/ErikEvenson/aurora-manual`, documenting Aurora C# v2.7.1. Chapters used: `13-ground-forces/`, `6-economy-and-industry/`, `12-combat/`, `5-colonies/`.

Secondary (reference, blocks direct fetch — used via search snippets):
- **AuroraWiki** (`aurorawiki.pentarch.org`) — pages `C-Ground_Combat`, `C-Ground_Units`, `C-Installations`, `Construction_Factory`. Returns HTTP 403 to automated fetch; content reached only through search excerpts.
- **Aurora4x Fandom wiki** (`aurora4x.fandom.com`) — installation stats.
- Official forum & changelogs (`aurora2.pentarch.org`) — version history.

---

## Known cross-source discrepancies (⚠️ verify before coding)

| Topic | Source A | Source B | Resolution |
|-------|----------|----------|------------|
| Ground combat round cadence | Wiki: **1 round / 3 hours** | Manual: **1 round / 8 hours** | Unresolved — verify in-game. Pick a configurable constant, do not hard-code. |
| GSP "per N rounds" | Manual: GSP = Pen×Dam×Shots **per 10 rounds** | — | Inherent supply = 10 rounds, consistent. |
| Installation worker req | "50,000 pop per installation" (most) | GFCC: "1,000,000 pop" | Both plausible (different installation classes). Verify per-installation. |

---

## Verification workflow (when a number actually matters)

When implementation needs a specific constant, do **not** trust this doc alone:
1. Check the aurora-manual raw markdown for the exact chapter.
2. Cross-check the AuroraWiki page (via web search if direct fetch 403s).
3. If still ambiguous, make it a **named constant in a config/blueprint JSON** (Pulsar loads game data from `GameEngine/Data/basemod/` — see root `CLAUDE.md`), so it can be tuned without a recompile. This matches how Pulsar already externalises component/mineral/tech data.
