# Industry plans

Colony industry goals live here. They are not part of the trade sequence under `GameEngine/Logistics/plans/`, and they are not numbered after plan 12.

The refining goal is one plan. Factory, shipyard, and construction goals are different decisions, because each one has to pick a design. Write those after a seated administrator can place a bid. Do not split refining into a stack of plans ahead of that. Administrator notes, which are not a plan, are `GameEngine/Engine/Orders/colony-administrator.md`.

Refinery, factory, and shipyard all use `IndustryAtb`. Whether that class splits is not decided. The note is [attribute-split.md](attribute-split.md). Construction is a separate set of plans. Do not start one until asked, and do not start the next in the same pass.

Costs, recipes, and time are not a plan in this folder. The philosophy is [balance.md](../../balance.md). Read it when the task is numbers. Do not retune from it until asked. Propulsion in `engines.json` stays put during a balance pass.

| Plan | Status |
|---|---|
| [Refine one batch](refine-one-batch.md) | Implemented. |
| [Mine asteroids](mine-asteroids.md) | Implemented. A ship goal, not a colony mine. |
| [Construction attribute](construction-attribute.md) | Implemented. |
| [Construction components](construction-components.md) | Implemented. |
| [Manufacture instances](manufacture-instances.md) | Implemented. |
| [Colony construction](colony-construction.md) | Implemented. |
| [Ship construction](ship-construction.md) | Implemented. |
| [Yard holds the hull](yard-holds-hull.md) | Implemented. |
| [Ship refit](ship-refit.md) | Implemented. |
| [Stations](stations.md) | Not started. Do not build until asked. |

## What already runs, and is not a goal

- **Mines.** `MineResourcesProcessor` is a hotloop. A mine with a deposit fills the warehouse. `GoalType.Mine` has a weight and no planner. Leave it that way. Ship mining is [Mine asteroids](mine-asteroids.md).
- **Jobs already on a line.** `IndustryProcessor` calls `IndustryTools.ConstructStuff` once a day, first run at three hours. It does not choose a design.
- **Infrastructure.** `InfrastructureProcessor.GetEfficiency` scales the points a line spends when installations outgrow infrastructure. Earth starts with 100 infrastructure.
- **Local construction.** `LocalConstructionProcessor` spends the actor's installation points on the head of the queue. A colony mounts on itself. A ship with the rate mounts a hold crate, or assembles a site-only design, onto one same-faction colony in cargo-transfer range. The colony does not need a warehouse or its own rate. Each job spends the actor it was given. A mount does not spend the recipe. An assemble spends the copied recipe. A design id alone does not mount a free copy. The production-line queue and this queue are different systems. A finished hull stays on the shipyard until a launch pad puts it in orbit. A shipyard refit spends `ship-refit` points to change the parts on a ship in transfer range. Stations are the next slice and are not started.
- **The fuel farm.** `default-design-fuel-farm-5000k` is a stainless-steel tank. It stores fuel. It does not refine it.

## Processor work, not a goal plan

These are worth doing because a refining job will sit on the daily processor. They are not extra colony goals.

- `ConstructStuff` throws on a bad job: missing cargo storage, missing faction, a design id that is not in `IndustryDesigns`, an industry type the line does not have, a recipe whose resources sum to 0, or an input that is neither cargo nor an internal component. That throw is inside the hotloop, so it stops time the same way a missing mineral used to. A normal stainless-steel batch on Earth's refinery has a registered design, type `refining`, and a non-zero recipe, so the happy path does not hit those throws. A bad job still should be skipped, not thrown.
- One completion adds `OutputAmount` cargo (100 for stainless steel, 8500 for RP-1, methalox, and hydrolox). `RunMarketPlan` treats `Max − stock` as a completion count. Those two meanings disagree. The refine plan orders one completion itself and leaves `AutoProduce` false so the market path does not order a hundred batches. The mismatch should be fixed before a second producer uses `AutoProduce`.
- `IndustryOrder2.Clone`, the four construction-queue order `Clone` methods, and `QueueShipRefitOrder.Clone` throw `NotImplementedException`. Nothing in the action queue calls `Clone` today.
- `MiningDB`'s copy constructor does not copy the rate dictionaries.
- `InstallationsDB` still describes employment and partial installation counts. Nothing in this folder reads it to turn buildings on or off, and there is no population draw on industry.

Population consumption, a preferred fuel, and a catalog price stay out of both the refine plan and this list until someone asks for them.
