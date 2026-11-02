# Prefab and site facilities

Status: implemented. Basemod only. Mars starts at 4,000 people. Twenty prefab units at 200 population support each cover that, and `MarsColonyTests` expects 4,000. Earth installs 5 site infrastructure units instead of 100 prefab crates. The site window is the same width as the prefab.

Ids stay. Display `Name` fields change. Bootstrap design ids stay:

- `default-design-local-construction`
- `default-design-factory`
- `default-design-auto-mine`
- `default-design-planetary-spaceport-crate`
- `default-design-infrastructure`

`ShipCargo` plus `PlanetInstallation` is a crate. `PlanetInstallation` without `ShipCargo` is site-only. A crate mounts for `max(1, mass / 100000)` installation points and can be freighted. A site-only design is assembled in place at `IndustryPointCosts` and a ship hold refuses it.

## Facilities

Every colony facility that stays a crate also gets a site-built twin. Research Lab and Ship Yard do not: they stay site-only, with no prefab. Fuel tanks, the construction bay, engines, and weapons stay ship parts. Do not put "Prefab" on those.

A crate's `BuildPointCost` is the factory cost to produce the instance. A site template's `BuildPointCost` is the installation-point cost of the assemble, so it must not copy a factory formula like `[Mass] * 9`. Once Research Lab and Ship Yard lose `ShipCargo`, their current build formulas become installation costs (`[Mass] / 2` is 50,000 points; the yard's `[Mass] * 0.25` is about 200,000). Replace those with the figures below.

Shared recipe shape:

- Crate: about 40% of today's mass where mass scales with the slider. Crew about a fifth of today's. Iron stops being the bulk of the recipe. Use aluminium, titanium, and stainless steel, plus a little copper.
- Site: about 2.5× the prefab mass for the same slider setting, or the fixed mass below. More `CrewReq`. Recipe is iron, space-crete, and stainless steel, plus a little copper. No titanium.

`CrewReq` is the crew. Only infrastructure has a separate population-support stat.

| Template | Change |
|---|---|
| `mine` | Stays the site twin of the RoboMiner. Shift the recipe off tungsten and nickel onto iron 0.45, space-crete 0.20, stainless-steel 0.10, silicon 0.10, aluminium 0.05, copper 0.05, graphite 0.05. Crew becomes `PropertyValue('Area') * 0.008`. |
| `automine` | Stays the crate. Name `Prefab RoboMiner`. Design name `Prefab Auto Mine`. Mass `PropertyValue('Size') * 800` (today `* 2000`). Crew stays 0. Recipe: iron 0.10, aluminium 0.35, titanium 0.30, silicon 0.10, copper 0.05, plastic 0.05, rare-earth-elements 0.05. |
| `research-lab` | `MountType` becomes `PlanetInstallation`. Name stays `Research Lab`. Mass stays 100,000. Crew 80 (today 20). `BuildPointCost` 10,000. Recipe: iron 0.50, space-crete 0.30, stainless-steel 0.15, copper 0.05. |
| `shipyard` | `MountType` becomes `PlanetInstallation`. Name stays `Ship Yard`. Mass formula stays. Default crew 20,000. `BuildPointCost` 20,000. Same bulk recipe as the lab. |
| `naval-academy`, `admin-complex`, `launch-complex` | Stay site-only. No recipe pass. Launch complex is one pad. The yard holds the finished hull; the pad spends fuel and creates the ship in orbit if the hull fits the pad's tonnage. |
| `factory` | Stays a crate. Name `Prefab Factory`, design name too. Mass `PropertyValue('Size') * 0.4`. Crew `[Mass] * 0.02`. `BuildPointCost` stays `PropertyValue('Size') * 9`. Recipe: iron 0.15, aluminium 0.25, titanium 0.35, stainless-steel 0.15, copper 0.10. |
| `factory-site` | New. `PlanetInstallation` only. Name `Factory`. Same industry output formulas. Mass `PropertyValue('Size') * 2.5`. Crew `[Mass] * 0.08`. `BuildPointCost` 8,000. Bulk recipe. |
| `refinery` | Stays a crate. Name `Prefab Refinery`, design name too. Mass `PropertyValue('Size') * 0.4`. Crew `[Mass] * 0.02`. Recipe: iron 0.15, aluminium 0.25, titanium 0.35, stainless-steel 0.15, copper 0.10. |
| `refinery-site` | New. `PlanetInstallation` only. Name `Refinery`. Same refinery-point formula. Mass `PropertyValue('Size') * 2.5`. Crew `[Mass] * 0.08`. `BuildPointCost` 8,000. Bulk recipe. |
| `local-construction` | Stays the bootstrap crate. Name `Prefab Construction Office`, design name too. Mass `PropertyValue('Level') * 2000` (today `* 5000`). Crew `PropertyValue('Level') * 10` (today `* 55`). Max level 5. Points stay `Level * 5`. Recipe: aluminium 0.35, titanium 0.30, stainless-steel 0.20, copper 0.15. |
| `local-construction-site` | New. `PlanetInstallation` only. Name `Construction Office`. Mass `PropertyValue('Level') * 15000`. Crew `PropertyValue('Level') * 80`. Max level stays 256. Installation points `Level * 8`. `BuildPointCost` `PropertyValue('Level') * 2000`. Bulk recipe. |
| `logistics-office` | Stays the crate. Name `Prefab Logistics Office`. Mass 2,000. Crew 4. Capacity max 10, default 5. Recipe: aluminium 0.40, titanium 0.30, plastic 0.20, copper 0.10. |
| `logistics-office-site` | New. `PlanetInstallation` only. Name `Logistics Office`. Mass 25,000. Crew 40. `BuildPointCost` 8,000. Capacity slider 10–80, step 5, default 20. Same `LogiBaseAtb` (offices already sum). Bulk recipe. |
| `warehouse-facility` | Stays the crate. Name `Prefab Warehouse`. Cap storage volume at 2,000, default 500. Mass `PropertyValue('Size Efficiency') * 0.6` (today `* 1.5`). Crew stays the small formula. `BuildPointCost` stays `[Mass] * 4.5`. Recipe: aluminium 0.45, titanium 0.30, plastic 0.15, copper 0.10. |
| `warehouse-site` | New. `PlanetInstallation` only. Name `Warehouse`. Storage volume min 10,000, max 2,000,000, default 100,000. Mass `PropertyValue('Size Efficiency') * 4`. Crew `Max(10, PropertyValue('Size Efficiency') * 0.25)`. `BuildPointCost` 8,000. Bulk recipe. Move `default-design-warehouse` onto this template and keep its volume of 1,000,000. Earth keeps that installation. Mars, Luna, and Ceres do not. Add `default-design-warehouse-prefab` on the crate, volume 500, and install that on Mars, Luna, and Ceres in place of the site warehouse. |
| `planetary-spaceport-crate` | Stays the bootstrap crate. Name `Prefab Spaceport`. Remove warehouse size, size efficiency, total cargo stored, and the storage attribute. It transfers cargo and does not store it. Mass 2,000. Crew 4. Transfer rate max 50, default 20. Transfer range max 2,000, default 1,000. Write those onto `default-design-planetary-spaceport-crate` and drop the warehouse property. Recipe: aluminium 0.40, titanium 0.35, stainless-steel 0.15, copper 0.10. |
| `spaceport` (`storage.json`) | The port colonies mount (`default-design-spaceport`). `MountType` becomes `PlanetInstallation`. Name `Spaceport`. Minimum `Size` 50,000. Set that design's `Size` to 100,000. Mass `PropertyValue('Size') * 2.5`. Crew `Max(40, PropertyValue('Size') * 0.15)`. Add storage of `PropertyValue('Size') * 0.5` through `CargoStorageAtb`. `BuildPointCost` 8,000 (today `[Mass] * 4.5`). Bulk recipe, no titanium. |
| `space-port` (`installations.json`) | Delete this template. It is the unused `ShipComponent` port (crew 1,000,000). No basemod scenario references it. The site port is `spaceport`. The crate is `planetary-spaceport-crate`. |
| `solarArray` | `MountType` is `1` (`ShipComponent` only). Change to `ShipComponent, ShipCargo`. Do not add `PlanetInstallation`. Every other ship part already has `ShipCargo`. Missiles stay `Missile`. |

Earth keeps the prefab logistics office, factory, refinery, and construction office it already has installed, and it keeps its site buildings (the mine, the large warehouse, the spaceport, labs, and yards).

## Default designs

Every new buildable template gets a default design in `componentDesigns.json`, and that design id is added to Earth's `ComponentDesigns` and `StartingItems` so it can be built. That list:

- `factory-site`, `refinery-site`, `local-construction-site`, `logistics-office-site`, `warehouse-site`, `infrastructure-site`
- `default-design-warehouse-prefab` on `warehouse-facility`

Do not install those site designs on Mars, Luna, or Ceres.

## Mars, Luna, and Ceres start with crates only

`colony-mars-outpost`, `colony-luna-concord`, and `colony-ceres-depot` install crate designs, plus one site-only exception: City Hall. Replace every other site-only installation with the crate twin.

- Warehouse: `default-design-warehouse-prefab` instead of `default-design-warehouse`.
- Spaceport: `default-design-planetary-spaceport-crate` instead of `default-design-spaceport`.
- Mine, on Mars and Luna: `default-design-auto-mine` instead of `default-design-mine`.
- City Hall (`default-design-city-hall`, the site `admin-complex`): Ceres keeps the one it has. Luna does not have one today, so add one. Mars does not get one.
- Logistics office, refinery, fuel tank, and passive sensor stay. They are already crates.
- Infrastructure: the crate designs in the infrastructure section, not `infrastructure-site`.

`infrastructure` and `infrastructure-site` follow the same mass, crew, and recipe split. Details are below. Prefab crew is 4. Site crew is 80. The range sliders stay as they are. This plan does not add a lock.

## Infrastructure research

The range techs set the prefab window width. The extension techs move the track the window slides on. Stop adding the range tech into the track formulas. An already built design does not widen when research finishes. The upgrade is a new design.

Gravity, in `techs.json` and on the prefab properties:

- `tech-infra-gravity-range`: `MaxLevel` 6, `DataFormula` `0.1 + [Level] * 0.08`. Width is `9.81 * 2 * TechData(...)`. Level 0 is 1.962 m/s². Level 3 is about 6.7, enough to cover Earth and Mars in one window. Level 6 is about 11.4, enough to cover Luna through Earth. Rewrite the description to say it widens the prefab window and does not move the track.
- Low-gravity and high-gravity extensions keep their data formulas (down to 0, up to about 100). Rewrite the descriptions to say they open track for the prefab to slide into.
- Prefab track floor: `Max(0, 9.81 * (1.0 - 0.1 - TechData('tech-infra-gravity-min-extension')))`.
- Prefab track ceiling: `Min(100, 9.81 * (1.0 + 0.1 + TechData('tech-infra-gravity-max-extension')))`.
- At level 0 the window fills the track, so the center cannot move. Low-gravity level 3 opens the floor to about 2.94, and a level-0-width window can then be aimed at Mars without covering Earth.

Pressure:

- `tech-infra-pressure-range` stays `0.1 + [Level] * 0.05`, max level 3. Width is `2 * TechData(...)`: 0.20 at level 0, 0.50 at level 3. Description: widens the prefab window only.
- `tech-infra-pressure-min-extension`: `DataFormula` `[Level] * 0.17`, max level 5. Description: opens the floor toward vacuum. Level 5 reaches 0.
- `tech-infra-pressure-max-extension`: `DataFormula` `[Level] * [Level] * 0.4`, max level 5. Description: opens the ceiling, capped at 10 atm. A runaway-greenhouse atmosphere (system gen allows up to 200) stays out of reach.
- Prefab track floor: `Max(0, 0.85 - TechData('tech-infra-pressure-min-extension'))`.
- Prefab track ceiling: `Min(10, 1.15 + TechData('tech-infra-pressure-max-extension'))`.
- Level 0 track is 0.85–1.15 with a 0.20 window, so the player can slide between an Earth-centered window and one that also covers scenario Mars (0.87 atm).

## Two infrastructure templates

### `infrastructure` — Prefab Infrastructure

Stays `ShipCargo, PlanetInstallation`.

- Mass 400. Crew 4. `BuildPointCost` stays 100. Mount cost stays 1 point.
- Support capacity 400. Storage 200. Population support 200. The prefab keeps this storage. It is still how an empty colony gets its first warehouse. The prefab spaceport does not.
- Width formulas are the range techs above. Track formulas are the extension tracks above. The player still drags the handles and the bar.
- Recipe, light plus exotic, scaled by the baked width. At level 0 both factors are 1.
  - iron `[Mass] * 0.2`, aluminium `[Mass] * 0.1`
  - titanium `[Mass] * 0.35 * (Max Gravity - Min Gravity) / 1.962`
  - tungsten `[Mass] * 0.15 * (Max Pressure - Min Pressure) / 0.2`
Stock design `default-design-infrastructure`, name `Prefab Earth-Standard Infrastructure`. Window is the level-0 band: gravity about 8.83–10.79 centered on 9.81, pressure 0.90–1.10. Earth does not install this crate. It installs the site design.

Three more crate designs on this same template. Each is installed on that colony and is not the site template.

- `default-design-infrastructure-mars`, name `Prefab Mars Infrastructure`. Gravity 3.0–4.9, pressure 0.85–1.05. Covers scenario Mars (3.72076 m/s², 0.87 atm). Mars keeps its 20 units.
- `default-design-infrastructure-luna`, name `Prefab Luna Infrastructure`. Gravity 1.4–1.8, pressure 0–0.2. Covers Luna (1.625 m/s², no atmosphere). Replace Luna's 15 Earth-standard units with these.
- `default-design-infrastructure-ceres`, name `Prefab Ceres Infrastructure`. Ceres does not set `Gravity` in JSON, so the body uses surface gravity from its mass (9.3835×10²⁰ kg) and radius (469.73 km), about 0.28 m/s², and it has no atmosphere. Window gravity 0.1–0.5, pressure 0–0.2. Confirm the generated gravity and keep the body inside the window. Ceres has no infrastructure today. Install enough units that population support covers the starting 800 people (prefab support is 200, so 4 units).

### `infrastructure-site` — Infrastructure

New template. `PlanetInstallation` only. `CargoTypeID` `None`. Same tolerance, capacity, and population-support attributes. No storage attribute. The prefab is what gives an empty colony its first warehouse.

- Mass 20,000. Crew 80. `BuildPointCost` 5,000.
- Support capacity 8,000. Population support 4,000.
- Track is fixed, not tech: gravity 0–100, pressure 0–10. The window width matches the prefab: gravity `9.81 * 2 * TechData(tech-infra-gravity-range)` (1.962 at level 0, wide enough for the Mars prefab band 3.0–4.9), pressure `2 * TechData(tech-infra-pressure-range)` (0.20 at level 0). Step is 0.1 for both. The handles still move, up to that max gap.
- A `Hostility` text property:

  `1 + Abs((Min Gravity + Max Gravity) / 2 - 9.81) / 9.81 + Abs((Min Pressure + Max Pressure) / 2 - 1)`

  If `Abs` is rejected by the expression engine, use `if(x < 0, -x, x)`.
- Recipe uses bulk goods times that property: iron `[Mass] * 0.45`, space-crete `[Mass] * 0.25`, stainless-steel `[Mass] * 0.15`, copper `[Mass] * 0.05`. Earth-centered is about 1×. A window aimed at Luna (about 1.6 m/s² and 0 atm) is about 2.8×.

Add `default-design-infrastructure-site` on this template, aimed at Earth (gravity about 9.6–10.0, pressure 0.9–1.1), and list it on Earth. Earth installs 5 of these units. That matches the old 100 prefab crates: 20,000 population support and 40,000 capacity. Do not install it on Mars, Luna, or Ceres.

`MarsColonyTests` requires Mars's installed infrastructure to provide capacity above 0, efficiency 1, and population support of at least 4,000. Mars starts with 4,000 people, which is what twenty prefab units support. The Mars prefab window above must cover Mars. Do not rebalance Luna's population.

## Left out

- A locked or lockable range widget. No new `GuiHint`. The existing range slider stays.
- A combo of live planets.
- Ship-part balance, and academy, admin, and launch retunes. The mine recipe change above is in scope because it is the site twin of the RoboMiner.
- Widening crates that are already built.

## Checks

`DesignerInputs_roundtrip_reproduces_the_designer_state` must keep passing.

`MarsColonyTests` must keep passing. The installed Mars prefab covers Mars.

The five bootstrap design ids stay crates. `local-construction` stays a crate, so `AnOfficeCrate_IsCrateAbleNotSiteOnly` still passes.

One new fixture:

- `research-lab` and `shipyard` are site-only, and `solarArray` has `ShipCargo`. `space-port` is gone.
- Each new `*-site` template and `warehouse-site` is site-only. `factory`, `refinery`, `local-construction`, `warehouse-facility`, and `planetary-spaceport-crate` stay crates.
- `planetary-spaceport-crate` has a cargo-transfer attribute and no cargo-storage attribute. `spaceport` has both.
- The Mars prefab covers 3.72 m/s² and 0.87 atm and does not cover 9.81 m/s².
- The Luna prefab covers 1.625 m/s² and 0 atm.
- The Ceres prefab covers Ceres's generated surface gravity and 0 atm.
- Mars starts with no site-only installation. Luna and Ceres start with City Hall as their only site-only installation.
- Hostility on an Earth-centered site window is about 1, and on a Luna-centered site window is greater.

When this is built, update the site-only sentence in [colony construction](colony-construction.md) so it matches the new mount flags. That sentence also calls `space-port` a separate template. Delete that clause with the template.
