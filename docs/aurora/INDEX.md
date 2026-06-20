# Aurora 4X Design Reference — Index

Pulsar4X is a fan recreation of **Aurora 4X** (Steve Walmsley's C# space sim). For the subsystems Aurora already has and Pulsar does not (ground combat, deep planetary infrastructure), **Aurora is the design spec.** These documents capture Aurora's mechanics so we can diff them against Pulsar's current code and implement the gaps in Pulsar's own idiom.

> **These are a design reference, not an implementation spec.** Numbers here were extracted from secondary sources (community wiki + a community manual) and summarised by a model. Treat all specific values as *approximately correct, to be verified against the actual game before they are hard-coded.* Where sources disagreed, both values are shown and flagged ⚠️. The *shape* of each mechanic is reliable; the *exact constants* are not.

---

## Documents

| Doc | Covers | Maps to Pulsar |
|-----|--------|----------------|
| `GROUND-COMBAT.md` | Ground unit design, formations, combat resolution, transport, orbital drop, boarding, invasion/occupation | **All new** — no ground combat exists in Pulsar |
| `PLANETARY-INFRASTRUCTURE.md` | Installations, construction, mining, population, economy, infrastructure-for-colony-cost | `Colonies/`, `Industry/`, `Energy/` — partially built |
| `SPACE-COMBAT-BENCHMARK.md` | Aurora naval combat depth (the bar we are matching) + pointers to Pulsar's existing implementation | `Weapons/`, `Damage/`, `Sensors/` — built, see those CLAUDE.md |
| `SHIP-DESIGN.md` | Hull/tonnage, layered armor, engines, shields — the template ground-unit design copies | `Ships/`, `Damage/` — built |
| `SENSORS-AND-DETECTION.md` | Thermal/EM signatures, passive vs active sensors, EMCON/stealth | `Sensors/` — built |
| `RESEARCH-AND-TECH.md` | Research labs, scientists, the 9-category tech tree (incl. Ground Combat), prototypes | `Tech/` — built |
| `DIPLOMACY-AND-INTEL.md` | Relationship tiers, communication, treaties, ELINT intelligence | `Factions/` — minimal; **furthest from objective** |

**Two tiers of doc:**
- **Core to the objective** (new work needed): `GROUND-COMBAT.md`, `PLANETARY-INFRASTRUCTURE.md`.
- **Benchmark / completeness** (Pulsar mostly already has these — read to calibrate "the same depth," not to build from scratch): `SPACE-COMBAT-BENCHMARK.md`, `SHIP-DESIGN.md`, `SENSORS-AND-DETECTION.md`, `RESEARCH-AND-TECH.md`, `DIPLOMACY-AND-INTEL.md`.

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
