# Balance

Philosophy for costs, recipes, and time. Written 2026-10-25. This is not a plan. Do not retune data or redesign a system because this file exists. Read it when the task is costs, recipes, industry time, mining, fuel supply, research costs, or blueprint numbers.

Repo markdown wins. If a number here disagrees with the JSON, trust the JSON and fix this file.

## The rule

Physical ratios, game clock.

Kilograms, composition, density, exhaust velocity, thrust, and fuel flow come from the world and stay in a real ballpark. How long a job takes at the keyboard is a game choice. A real factory's tons per hour is not the target. A real bill of materials is.

The ladder below is in colony calendar days. `IndustryProcessor` currently spends points once a day. If that tick changes, scale the budgets so the calendar stays. A Merlin is still one day.

Three rules when a number is actually being set:

1. Mass is kilograms. Minerals and processed materials are 1 kg per unit, and mineral `VolumePerUnit` is 1/density. A structural recipe's resource coefficients sum to about 1. A refine recipe states a yield.
2. Difficulty is a tier. Build points are `mass_kg * tier`. Proposed tiers, not applied: 0.01 bulk structure, 0.1 ordinary machines, 1 engines and precision parts, 10 enrichment and electronics, 100 a project. A new blueprint picks a tier. It does not invent a `BuildPointCost` formula.
3. Facilities are sized against stock Earth: one factory, one refinery, one mine, one yard. A new part is "a Merlin", "a freighter", or "a second factory".

Fitting stays a shorter job than manufacturing. Hull assembly is `mass * 0.1` in `ShipDesign.Recalculate`. A mount or refit part is `max(1, MassPerUnit / 100000)`.

## Propulsion stays

Newtonian engines are the less-fluid part. A balance pass fixes mining, refining, and construction so they can feed these numbers. It does not slow an engine down so a refinery can keep up.

Changing exhaust velocity, burn rate, engine mass, or the thrust equation is a propulsion change. Call it out. Do not fold it into a recipe tweak.

The model lives in `GameData/basemod/TemplateFiles/engines.json` and `GameEngine/Movement/NewtonMove/NewtonionThrustAtb.cs`. Thrust in newtons is exhaust velocity times fuel kg/s. Delta-v is Tsiolkovsky (`NewtonThrustAbilityDB`, `OrbitMath`).

Stock conventional engine, level-0 techs:

- Exhaust velocity is the fuel's, from `materials.json`: RP-1 3510, methalox 3615, hydrolox 4462, NTP 7000 m/s. `tech-conventional-engine-exhaust-velocity` adds `level * 100`.
- Fuel burn is `mass_kg * 0.3` kg/s, times `tech-conventional-engine-fuel-consumption` (`1 - level * 0.01`). Level 0 is the full burn.
- Solid-core NTR burn is `mass_kg * 0.017` kg/s. Fuel is NTP.
- Volume is `max(1, mass / 150)` for a conventional engine.
- The Merlin design (`default-design-merlin`) is 470 kg on RP-1. That is 141 kg/s and about 495 kN. The fuel flow is the real-engine neighborhood. Leave it.

The Alcubierre drive's speed stays with engine power over ship mass (`mass * 1000` at efficiency 1). Its electrical budget is below. Do not retune creation, sustain, or the stock reactor to chase a different recharge without saying so.

## The rest can change

Simulation code is fluid when a different shape plays better. Say so in the session. Open examples:

- The industry tick. One day is a choice, not a law. The calendar ladder is the constraint.
- Reactors. The generic reactor no longer bills its lifetime fuel as a construction cost. `EnergyGenProcessor` still subtracts `LocalFuel` and does not stop when that counter is empty, and it does not draw cargo. A cargo tank, a real burnup, or a different lifetime model are still open. The Lifetime description says seconds. Default 8760 is one year if it is read as hours. RTG and steam-turbine fuel stay part of the machine's mass. The stock steam plant is the 15 kW warp reactor below, not the generic 50 kW/kg design.
- Build points as integers per day, and mine rates stored as `long`. A fractional rate becomes 0 in `MineResourcesAtbDB`.
- Two research clocks. A tech blueprint unlocks templates. `ComponentDesigner` also prices a specific design from `ResearchCost`. Either clock can be reshaped.
- Electricity as a refined cargo in `materials.json`. Power is already a flow on reactors, RTGs, and solar arrays.

## Clock ladder

Targets for stock Earth, one of each starting building. Applied 2026-10-25 except the rows that already matched. `BalanceBaselineTests` pins the numbers that changed.

| Job | Target | Now |
|---|---|---|
| Merlin, materials on hand | 1 day | about 1 day (470 points, factory 500/day). Engines were not retuned. |
| Fuel for 1 minute of that Merlin | 1 refinery-day | one batch. RP-1, methalox, and hydrolox are 500 points, 4,250 kg hydrocarbons in, 8,500 kg out. Yield stays 2. Exhaust velocities are unchanged. |
| Ore for that engine, from the mine | a few days | default mine is 4,500 kg/day of each mineral on the body before accessibility. A size-5 robo-miner is 500 kg/day. `tech-mining-production` multiplies both by `1 + level * 1.5`. Level 0 is 1. |
| Freighter hull, parts already built | a couple of weeks | about 20 days (`mass * 0.1` / 200 assembly points) |
| A tech step | a week to a month | 25 days for a 500-point step with both labs |
| A second factory | a season | 90 days. Size is kilograms, same as the refinery. Default 5,000 kg, 500 points/day, build `Size * 9`, iron 3,750 kg. |
| A new slip | about a year | 400 days. Yard mass stays `slip * 8` (800,000 kg). Build is `mass * 0.25` (200,000 points) on the factory's 500 installation points/day. |
| Naval Academy, factory then install | a season | 90 factory days plus one mount day. Mass 90,000 kg, build `mass * 0.5` (45,000 points). |
| Mount a crate that already exists | about a day | already about that. Yard refit stays 5 points/day. |

A facility is manufactured once. `BuildPointCost` is that cost, paid by the factory. When the completion checkbox queues construction, the follow-up is `max(1, mass_kg / 100000)` installation points, including a planet-only design. The recipe is not spent again. Queuing that design from the Construction tab, with no factory job, still pays `BuildPointCost` and the recipe, because that tab is the build. Earth's Construction Office design is level 5, 25 points a day. A job already copied onto a queue keeps its old cost.

Earth's RP-1 stock is 50 million kg, about 100 hours of one Merlin. One refinery replaces about one minute of that burn per day. A homeworld deposit is at least accessibility 0.5, so a poor bulk deposit is about half the table.

## Warp electrical budget

Applied with the processor change. `BalanceBaselineTests` pins the level-0 designs. Warp speed was not changed. Newtonian engines, the fuel tank, and battery mass were not changed.

Reference hull: the freighter. One 2 t drive, two 2 t batteries, one 2 t steam reactor. Engine power of that drive is 2,000,000. Each battery is `mass * 500` kJ at battery-tech level 0, so the pair stores 2 GJ.

| Piece | Level 0 |
|---|---|
| Bubble creation | `enginePower * 2 / 3` kJ. The 2 t drive is 1,333,333 kJ, two thirds of those batteries. |
| Bubble sustain | 15 kW for that drive (`enginePower * 15 / 2000000`). Four 500 kg drives add up to the same bill. |
| Stock reactor | 15 kW. Heat per kg of fuel is 0.03125. Mass stays 2,000 kg. The design name is `15kW Steam Turbine Reactor`. |
| Recharge | Creation leaves about one third. Generation equals sustain, so a cruise holds the battery. Refilling the hole after drop-out takes about a day. |

`EnergyGenProcessor` adds `(generation − demand) * seconds` to the store. A backwards timestamp only moves the mark. Warp still raises the demand for one second, so the creation cost is paid once as a lump. Matched sustain does not drain the bank and does not refill it.

Warp still spends no fuel, so the cruise length stays open. Stopping still spends methalox. That range was left alone.

## Where the numbers live

Data, under `GameData/basemod/`:

- `TemplateFiles/materials.json` — refine recipes. `IndustryPointCosts` is the batch. `OutputAmount` is kg out. `ResourceCosts` is kg in. One completion adds `OutputAmount` (`ProcessedMaterial`).
- `TemplateFiles/minerals.json` — 1 kg per unit. Abundance by body class.
- `TemplateFiles/industryTypes.json` — labels only (`refining`, `component-construction`, `installation-construction`, `ordnance-construction`, `ship-assembly`). Rates are not here.
- `TemplateFiles/installations.json` — mine, automine, refinery, factory, shipyard, research lab. Daily points are property formulas.
- `TemplateFiles/engines.json` — propulsion, and the Alcubierre electrical coefficients. Leave the newtonian formulas. Creation and sustain are the warp budget above.
- `TemplateFiles/energy.json` — reactor, battery, RTG, steam turbine, solar. The stock steam plant is 15 kW. The generic reactor's fuel-as-construction removal stays. Its 50 kW/kg operating stat was left alone.
- `TemplateFiles/storage.json`, `weapons.json`, `electronics.json`, `armor.json`, `ordnance.json` — other templates.
- `TemplateFiles/techs.json`, `techs-bureaucracy.json`, `techCategories.json` — research. `Unlocks` is sparse. `tech-modern-technology` unlocks a pile at level 1.
- `ScenarioFiles/designs/componentDesigns.json` — stock designs (Merlin, factory, mine, yard).
- `ScenarioFiles/systems/sol/earth.json` — starting installations and cargo. Minerals are `randomHW`.
- `Industry.mmd` — hand-drawn chain. Stale. Trust the JSON.

Code:

- `GameEngine/Industry/IndustryProcessor.cs` — once a day, first run at three hours. `IndustryTools.ConstructStuff`.
- `GameEngine/Industry/IndustryJob.cs` — points and recipe copied off the design.
- `GameEngine/Industry/MineResourcesProcessor.cs`, `MineResourcesAtbDB.cs`, `MiningHelper.cs` — daily mine. Accessibility multiplies the rate, then `Convert.ToInt64` rounds. Under 0.5 becomes 0.
- `GameEngine/Industry/LocalConstructionProcessor.cs` — mount and assemble. Separate from the factory line.
- `GameEngine/Engine/Components/ComponentDesign.cs` — a checked factory completion of a planet-only design queues the mount cost. The Construction tab still pays `BuildPointCost`.
- `GameEngine/Ships/ShipDesign.cs` — hull points are `mass * 0.1`.
- `GameEngine/Engine/Components/ComponentDesigner.cs` — evaluates `BuildPointCost`, `ResearchCost`, and `ResourceCost`.
- `GameEngine/Movement/NewtonMove/NewtonionThrustAtb.cs` — thrust and delta-v.
- `GameEngine/Energy/EnergyGenProcessor.cs` — store change is kilowatts times seconds. Warp's one-second creation spike depends on the backwards timestamp only moving the mark.
- `GameEngine/Tech/Tech.cs` — `CostFormula` sees the current `Level`. A formula `(1 + [Level]) * 500` costs 500 at level 0.

Behavior of construction, refit, and the yard is `GameEngine/Industry/plans/`. That folder is not this file. Costs and time stay here.

UI, for a later readout. Do not build until asked:

- `Pulsar4X.Client.Host/Designer/ComponentsWindow.cs` and `ComponentDesignDisplay.cs` — credit, research, industry points, resource table. No "days on Earth".
- `Pulsar4X.Client.Host/DevTools/BlueprintsWindow.cs` — one blueprint, key and value.
- `Pulsar4X.Client.Host/ModFileEditing/` — JSON editors (`ComponentBluprintUI`, `ProcessedMaterialsUI`, `TechBlueprintUI`).

The useful readout is days on Earth's factory, refinery, or yard, plus kilograms of each ore after walking refine recipes, plus the three reference rows (Merlin, freighter, this factory). A generated chain replaces `Industry.mmd`. A tech tree waits until `Unlocks` has edges.

## Still off, after the 2026-10-25 pass

Not a work order.

- The tier table is not applied to every template. Cargo-hold and warehouse mass stay as they are. Changing them changes stock ship mass and delta-v.
- NTP: 101 kg in, 100 kg out, 100 points. About 500 kg/day on the default refinery, under the chemical fuels.
- Fissile fuel: 110 kg in, 1 kg out, 10 points, `WealthCost` 1000. About 50 kg/day. The yield shape (enrichment discards mass) can stay. The batch size is a clock decision.
- Stainless steel: 88 iron + 11 chromium + 1 hydrocarbons → 100 kg, 20 points. About 2,500 kg/day. This shape is a fine alloy.
- Plastic: 1 kg in, 2 kg out, 5 points. Electronics: 6 kg in, 5 kg out, 100 points, on the refining line.
- Chemical fuel still invents oxidizer mass at yield 2. The batch size is the clock fix. A real oxygen or water input is a later recipe.
- `MineResourcesAtbDB` still casts the rate to `long`. The default mine and robo-miner are whole kilograms, so the cast keeps them. A fraction still becomes 0.
- Shipyard rates stay `crew * 0.01` component, `crew * 0.02` assembly, `crew * 0.0005` refit. `IndustryAtb` casts to int, so a small crew refit truncates to 0.
- The mine lists every mineral at one rate. Accessibility scales it. Homeworlds floor at 0.5, so a rare mineral on Earth is not rare.
- Warehouse design: 1,000,000 m³. Mass is 1.5% of volume (15,000 kg). Build cost is `mass * 4.5`.
- Research lab: 10 points/day, mass 100,000 kg, build `mass / 2`, operating cost 10,000. Earth has two. Antenna Bandwidth has 95 levels. The step at level 94 costs 47,500 points, about 2,400 days with both labs. That is a slider.
- Generic reactor: power is 50 kW per kg. Fuel kg/s is `power * 1e-7`, about ten thousand times a real fission burn. Construction no longer pays that load. The operating stat was left alone.

## When a session does touch this

1. Read this file. Do not re-walk the JSON unless a figure here is in doubt.
2. Leave `engines.json` and `NewtonionThrustAtb` alone unless the task is the propulsion model.
3. Prefer a data change that hits the ladder over a new cost system. Rework the processor, the tick, or the reactor when that plays better. Keep the calendar.
4. A new blueprint picks a tier and a reference row.
5. The days-on-Earth column, the generated chain, and the tech tree are not started.
