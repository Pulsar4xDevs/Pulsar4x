# Civilian recruitment: where administrators and lab technicians come from

Status: built (PR #500, 2026-11-05). Written 2026-11-04 for Grok Build. Leftovers: tuning the pool numbers, a real wage (placeholder 100), and a dedicated AI lab-technician hire test.

Jon flagged this as one of the big open questions (open question 9) in [civilian-administrators.md](civilian-administrators.md): where administrators and research lab technicians come from, how they are recruited, and how often the player gets to choose them. The placeholder there, "the starting factory makes them as it does today", is not enough. This plan covers the time before a civilian academy exists, and how a later academy fits on top.

**First sub-plan to implement** in the [civilian-administrators](civilian-administrators.md) plan of plans. It depends on nothing else there and should be fairly quick.

Read first: [civilian-administrators.md](civilian-administrators.md), and the officer plan `GameEngine/People/plans/officers-and-span.md` ("Officers, the academy, and command span"), recoverable with `git show cc4861cd^:Pulsar4X/GameEngine/People/plans/officers-and-span.md`.

## What exists today

Everything below is in code now. Nothing recruits civilians during play.

- **Administrators.** `CommanderFactory.CreateAdmin(Game)` returns a `CommanderDB` with a random name, rank 1, `CommanderTypes.Civilian`. It is only called by `ColonyFactory.SeatStartingAdministrator` (when a colony blueprint has `SeatAdministrator`), by the benchmark, and by the client's test-game setup in `GameLifecycle`. The starting admin is backdated (commissioned 10 years ago, ranked 1 year ago) and placed in the first empty `AdminSpaceAbilityState` in `AdminSpaceDB.CommanderSeats`. Seats come from `AdminSpaceAtb` (one per installed admin component, with an `AdminLevel`). `AssignAdministratorOrder` and `UnassignAdministratorOrder` move admins in and out of seats.
- **Scientists: two different things.** There are two "scientist" types and they are not connected:
  - `CommanderFactory.CreateScientist(Game)` returns a `CommanderDB` with `CommanderTypes.Scientist`. Only the benchmark and `GameLifecycle` call it. Nothing in the engine uses it for research.
  - `CommanderFactory.CreateScientist(Entity faction, Entity location)` returns a `Technology.Scientist` (a `TeamObject`, `TeamType = Science`) with a hard-coded name ("Augusta King"), age 30, `MaxLabs = 25`, and a fixed bonus of 1.1 to `tech-category-power-propulsion`. It is added to `FactionTechDB.AllScientists` and the colony's `TeamsHousedDB`. This is the one `ResearchProcessor` uses. `ColonyFactory`, `FactionFactory` and `DefaultStartFactory` each make exactly one per starting colony.
- **Officers (the pattern to copy).** The Naval Academy: `NavalAcademyAtb` (class size, class length), `NavalAcademyDB`, `NavalAcademyCourse`, `NavalAcademyProcessor`. A class takes people out of `ColonyInfoDB.Population` (one per seat, refused if the colony can't support it), and graduation calls `CommanderFactory.CreateAcademyGraduate` to make unassigned rank-1 navy officers. Cancel returns the people. Techs `tech-academy-larger-classes`, `tech-academy-better-instruction`, `tech-academy-candidate-search` improve it. The academy is site-only, built by the construction office, and is idle until the player orders a class.
- **Population.** `PopulationProcessor.GrowPopulation` runs every 30 days per colony: growth rate 20 / pop^(1/3), capped, limited by population support. It is per species in `ColonyInfoDB.Population`.
- **Migration.** None. There is no code that moves population between colonies (the only "migrate" in the engine is `FactionStanceRules.MigrateGrants`, a save migration). Migration is not a source until someone builds it.

So the only real sources are: the starting blueprint, and population (which the academy already draws on for officers).

## Recruitment sources

Considered:

- **Starting blueprint (day one only).** The starting factory is no longer the answer for where civilians come from; it only seeds day-one people from the blueprint. Civilian-administrators.md decided that Luna Concord starts with all admin seats filled and Earth's seats start empty. Earth still starts with its one lab scientist so research works on day one.
- **Population (the main source).** Recruits are drawn from a colony's `ColonyInfoDB.Population`, the same pool the Naval Academy uses. A recruit removes one person from the population, like an academy seat. This makes big colonies the place talent comes from, and costs something real.
- **Migration (later).** Not real yet. When colony-to-colony migration exists, a recruit could be hired on one colony and moved to another. Out of this plan.
- **Faction-wide drip (not chosen).** A fixed "one new civilian per year per faction" ignores population and gives the player nothing to grow.

## Recruitment rate

**Decided (Jon, 2026-11-04):** recruitment is a periodic **recruitment event** that presents the player with a shortlist to choose from, like the Naval Academy pattern but lighter (no building, no class). The shortlist's size scales with the colony's population: a bigger population means more or better recruits. The cadence is fixed at roughly monthly. The exact scaling formula is open for tuning (open question 2). The numbers below are a starting suggestion:

- Each colony with at least a minimum population (say 100,000; a number for Jon) gets a **shortlist** at each recruitment event. **Decided:** a rolling refresh roughly once a month, piggy-backing on a scheduled event for that colony (not a new hot-loop processor).
- Pool size grows slowly with population: 1 candidate at the minimum, plus one per order of magnitude above it, capped at 5 (numbers for Jon).
- **Decided:** at each monthly refresh, one or two unpicked candidates are removed and one or two new ones are added, so the pool turns over gradually instead of being discarded all at once.
- **Decided:** pool size depends on the colony's population. The 100,000 minimum and the 1 to 5 range are starting numbers for tuning (open question 2).

Earth (billions of people) keeps a full pool of up to 5, with one or two fresh faces each month. A small colony has one candidate or none. That gives a big colony a steady trickle of choices, which keeps people scarce, as decision 3 of the officer work says officers should be.

## Player choice

How the player picks:

- A **Recruitment** section in `ColonyManagementWindow` lists the colony's current candidates. Each card shows name, age, and the candidate's leanings: an administrator aptitude (which portfolio they lean to, once `AgentDB` Specialization exists from civilian-administrators slice 1) and a lab aptitude (a favoured tech category and a `MaxLabs` value).
- The player hires a candidate **as** an administrator or **as** a lab technician. The same person can go either way. The card shows what they would be in each role, so the choice is real.
- A hire is one command (follow `AssignCaptainCommand`: translator, snapshot, client). It removes one person from population and makes the civilian unassigned on that colony.
- With no click, nothing happens; candidates gradually roll off the pool. An empty recruitment section is greyed with the reason ("Population below 100,000" or "Next candidates on <date>").
- The AI (Luna Concord) hires automatically: fill empty admin seats first, then labs.

## Filling a position: the candidate picker

**Decided (Jon, 2026-11-04):**

- Each colony has its own candidate pool (as above). Pools are per colony, never faction-wide.
- Filling a position (an empty admin seat, or a lab needing a technician) opens a **candidate list**. It shows **all** of the faction's current candidates from every colony's pool, not just the colony with the position.
- Each row shows the candidate's name, **location** (the colony whose pool they are in), and **wage**. The wage is a placeholder number for now, the same flat value for everyone, until payroll exists; it is shown but nothing is charged.
- Picking a row hires that candidate into that position in one step, using the same hire command. A candidate from another colony is hired where they stand; moving them is left for migration (out of this plan).
- Leanings (admin portfolio, lab category) show when available. Admin Specialization needs `AgentDB` from the administrators plan, so until then the list shows name, age, location, and wage only, and this plan stays independent.

## Administrators versus lab technicians

| | Administrator | Lab technician (scientist) |
|---|---|---|
| Created as | `CommanderDB`, `CommanderTypes.Civilian`, via `CreateAdmin` | `Technology.Scientist` in `TeamsHousedDB` and `FactionTechDB.AllScientists`, via `CreateScientist(faction, location)` |
| Goes into | An `AdminSpaceAbilityState` seat (needs admin space) | Labs on its colony (up to `MaxLabs`) |
| Hired traits | Rank 1, personality (`AgentDB`) | Name, age, one favoured category bonus, `MaxLabs` |
| Limit | Seats from admin space; admin complex level gates portfolios (decided in OQ 7) | Labs installed |

The pool hands out people, not roles. The role is picked at hire. Today's `Scientist` creation must stop being hard-coded (random name from `NameFactory`, random favoured category, `MaxLabs` in a range) before candidates mean anything. The unused `CommanderTypes.Scientist` `CommanderDB` stays unused in this plan (see open questions).

## Tie to the officer academy and a later civilian academy

The officer academy turns population into officers through a building and a class. Recruitment here turns population into civilians without a building. A later **civilian academy** (out of this plan) would sit on top the same way the Naval Academy does:

- Same pattern: a site-only building with class size and length, `CivilianAcademyAtb/DB/Processor` mirroring the naval ones, a class takes population and graduates people.
- What it adds over the pool: chosen numbers instead of luck, better graduates (higher rank or skill cap), and a choice of track (administration or research) up front.
- The candidate pool stays as the low-end source. The academy is the reliable one.

## Decision

A periodic **recruitment event per colony** keeps a population-drawn shortlist, sized by population (formula open for tuning), that rolls over roughly monthly by removing a random number of unpicked candidates and adding a random number of new ones (usually one or two, sometimes more, each count rolled separately with seeded pseudo-random generation, deterministic from the game's seed like other random elements in Pulsar4X; exact range open for tuning), so it turns over gradually and is never discarded all at once, from which the player hires each candidate as either an administrator or a lab technician. Keep the blueprint for day-one people. Leave migration and the civilian academy for later, with the academy following the Naval Academy pattern exactly.


**Randomness (project-wide rule).** This is a global rule for all of Pulsar4X, not just this plan, and every other plan should follow it too: every random element anywhere in the game uses pseudo-random generation seeded from the game's seed, never true random, so the same seed always produces the same game. In this plan that covers candidate names, ages, leanings, `MaxLabs`, and the monthly add and remove counts.
## Slices

Each slice is one PR, tests first. Run tests from outside the repo with `DOTNET_ROLL_FORWARD=LatestMajor`, filtering `Pulsar4X.Tests`.

1. **Real scientists.** `CreateScientist(faction, location)` takes a name from `NameFactory`, a random age, one random favoured tech category, and `MaxLabs` in a range, from the game's seeded random. Starting colonies still get one. Tests: two scientists made in one game have different names; same seed gives the same scientist; a starting colony still has exactly one scientist in `TeamsHousedDB` and `AllScientists`; research still runs on day one.
2. **Candidate pool data.** A `CandidatePoolDB` on colonies holding candidates (name, age, admin leaning, lab leaning, expiry). Refreshed roughly monthly by a scheduled event that removes a random number of unpicked candidates and adds a random number of new ones, each count rolled separately with seeded pseudo-random generation from the game's seed, within a small range (usually one or two, sometimes more; exact range open for tuning); size by population, empty below the minimum. Tests: Earth gets a full pool on the first refresh; a colony below the minimum gets none; a refresh removes and adds candidates in counts within the configured range, never replacing the whole pool; add and remove counts vary across refreshes with the same seed giving the same counts; the pool survives save and load; no population is removed by a refresh.
3. **Hire command.** Hire-as-administrator and hire-as-lab-technician (translator, snapshot, client). Tests: hiring as administrator removes one person and adds an unassigned `CommanderTypes.Civilian` commander on the faction; hiring as lab technician removes one person and adds a `Scientist` to the colony's `TeamsHousedDB` and `AllScientists`; a hired candidate leaves the pool; hiring from another faction's colony is refused; the new admin can be seated with `AssignAdministratorOrder`.
4. **Recruitment UI and candidate picker.** A Recruitment section in `ColonyManagementWindow` with candidate cards and the two hire buttons, greyed with a reason when empty. Filling an empty admin seat or lab opens the candidate list: every candidate in the faction's colony pools, each with name, location, and a placeholder wage; picking one hires them into that position. Tests: the snapshot lists candidates for the owning faction only; the picker lists candidates from all of the faction's colonies with their location and the placeholder wage; picking a candidate fills the position and removes them from their pool; the empty reason is reported.
5. **AI hiring.** Luna Concord hires to fill empty admin seats, then labs. Tests: an AI colony with an empty seat and a candidate fills it on refresh; a player faction never auto-hires.

Out of this plan: migration, a civilian academy, payroll, promotion of civilians, and loyalty or poaching.

## Open questions for Jon

1. **Decided (Jon, 2026-11-04):** a periodic recruitment event with a shortlist, sized and timed by population, like the Naval Academy but lighter.
2. The scaling formula: how does population map to shortlist size and recruit quality? Are a minimum population of 100,000, and a cap of 5 candidates a sensible starting point for tuning?
3. Should a hire cost only one person from population, or also money or wealth once payroll exists?
4. Can one candidate go either way (admin or lab), or should each candidate be one or the other?
5. Should `Technology.Scientist` and the unused `CommanderTypes.Scientist` `CommanderDB` be merged into one person, so a lab technician can later become a research administrator?
6. Does Earth keep its one hard-coded starting scientist, or get a small starting pool instead?
7. **Decided (Jon, 2026-11-04):** a rolling pool. Roughly monthly, one or two unpicked candidates leave and one or two new ones arrive.
8. When the civilian academy comes, does the candidate pool stay, shrink, or go away?
9. What range should the random monthly add and remove counts roll from (for example 0 to 3 each), and should the add count lean higher on bigger colonies?

## Status (2026-11-05)

All five slices are implemented and merged in [PR #500](https://github.com/Pulsar4xDevs/Pulsar4x/pull/500). Tests are in `Pulsar4X.Tests/CivilianRecruitmentTests.cs` (25 passing). Still open: tuning the numbers, a real wage (it is a placeholder of 100 for now), and the AI's lab-technician hire, which has no dedicated test yet.
