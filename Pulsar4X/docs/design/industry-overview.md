# Industry overview

What the industry code does today, and the processor work still open. Reference, not a plan. The refine, mine, construction, refit and yard plans are all built and were deleted in the 2026-11 cleanup; this file keeps what they left behind. Open industry plans: [stations](../plans/stations.md) and [attribute split](../plans/industry-attribute-split.md). Administrator notes are [colony-administrator.md](colony-administrator.md).

Refinery, factory, and shipyard all use `IndustryAtb`. Whether that class splits is not decided; see [attribute split](../plans/industry-attribute-split.md).

Costs, recipes, and time: the philosophy is [balance.md](balance.md). Read it when the task is numbers. Do not retune from it until asked. Propulsion in `engines.json` stays put during a balance pass.

## What already runs, and is not a goal

- **Mines.** `MineResourcesProcessor` is a hotloop. A mine with a deposit fills the warehouse. `GoalType.Mine` has a weight and no planner. Leave it that way. Ship mining is the asteroid-mine goal (`AsteroidMineTests`).
- **Jobs already on a line.** `IndustryProcessor` calls `IndustryTools.ConstructStuff` once a day, first run at three hours. It does not choose a design.
- **Infrastructure.** `InfrastructureProcessor.GetEfficiency` scales the points a line spends when installations outgrow infrastructure. Earth starts with 100 infrastructure.
- **Local construction.** `LocalConstructionProcessor` spends the actor's installation points on the head of the queue. A colony mounts on itself. A ship with the rate mounts a hold crate, or assembles a site-only design, onto one same-faction colony in cargo-transfer range. The colony does not need a warehouse or its own rate. Each job spends the actor it was given. A mount does not spend the recipe. An assemble spends the copied recipe. A design id alone does not mount a free copy. The production-line queue and this queue are different systems. A finished hull stays on the shipyard until a launch pad puts it in orbit. A shipyard refit spends `ship-refit` points to change the parts on a ship in transfer range. Stations are the next slice and are not started ([stations](../plans/stations.md)).
- **Crates and site-only designs.** `ShipCargo` plus `PlanetInstallation` is a crate: a factory builds the instance, a ship can freight it, and a mount spends `max(1, MassPerUnit / 100000)` installation points, copied onto the job when it is queued. No `ShipCargo` means site-only: assembled in place at the design's `IndustryPointCosts`, and a ship hold refuses it. Site-only templates: `mine`, `research-lab`, `shipyard`, `naval-academy`, `admin-complex`, `launch-complex`, `spaceport`, and the site twins `factory-site`, `refinery-site`, `local-construction-site`, `logistics-office-site`, `warehouse-site`, `infrastructure-site`. The crates are `local-construction`, `factory`, `refinery`, `logistics-office`, `warehouse-facility`, `planetary-spaceport-crate`, `automine` and `infrastructure`. The old unused `space-port` template is gone.
- **The fuel farm.** `default-design-fuel-farm-5000k` is a stainless-steel tank. It stores fuel. It does not refine it.

## Processor work, not a goal plan

These are worth doing because a refining job will sit on the daily processor. They are not extra colony goals.

- `ConstructStuff` throws on a bad job: missing cargo storage, missing faction, a design id that is not in `IndustryDesigns`, an industry type the line does not have, a recipe whose resources sum to 0, or an input that is neither cargo nor an internal component. That throw is inside the hotloop, so it stops time the same way a missing mineral used to. A normal stainless-steel batch on Earth's refinery has a registered design, type `refining`, and a non-zero recipe, so the happy path does not hit those throws. A bad job still should be skipped, not thrown.
- One completion adds `OutputAmount` cargo (100 for stainless steel, 8500 for RP-1, methalox, and hydrolox). `RunMarketPlan` treats `Max − stock` as a completion count. Those two meanings disagree. The refine goal orders one completion itself and leaves `AutoProduce` false so the market path does not order a hundred batches. The mismatch should be fixed before a second producer uses `AutoProduce`.
- `IndustryOrder2.Clone`, the four construction-queue order `Clone` methods, and `QueueShipRefitOrder.Clone` throw `NotImplementedException`. Nothing in the action queue calls `Clone` today.
- `MiningDB`'s copy constructor does not copy the rate dictionaries.
- `InstallationsDB` still describes employment and partial installation counts. Nothing in the industry code reads it to turn buildings on or off, and there is no population draw on industry.

Population consumption, a preferred fuel, and a catalog price stay out of both the refine goal and this list until someone asks for them.

## Prefab and site facilities (2026-11-02 balance pass)

Built in `GameData/basemod` (`PrefabFacilitiesTests`; the plan file was deleted as done). Every colony facility that stays a crate has a "Prefab ..." display name and a site-built twin: a heavier build (about 2.5× the prefab mass, more crew, an iron / space-crete / stainless-steel bulk recipe) that is assembled on site. Research Lab and Ship Yard are site-only with no prefab. Ids stay; only display names changed. The five bootstrap design ids stay crates.

- Mars, Luna and Ceres start on crates only. City Hall is their one site-only exception: Ceres keeps its own, Luna gets one, Mars has none. Earth keeps its site buildings and installs 5 `infrastructure-site` units in place of the old 100 prefab crates.
- Infrastructure: the prefab window is set by the range techs and slid along a track set by the extension techs (`tech-infra-gravity-range`, the min/max extension techs, and the pressure equivalents). A built design does not widen when research finishes; an upgrade is a new design. The site infrastructure window has a fixed 0–100 m/s² / 0–10 atm track, the same width as the prefab, and a `Hostility` property that multiplies the bulk recipe by how far the window sits from Earth-standard. Stock prefab designs cover Mars, Luna and Ceres.
- `planetary-spaceport-crate` transfers cargo and does not store it; the site `spaceport` does both.

Left out of that pass: a locked or lockable range widget, a combo of live planets, ship-part balance, academy / admin / launch retunes, and widening crates that are already built.
