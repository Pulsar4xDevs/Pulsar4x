# Construction components

Status: implemented. Tests: `Pulsar4X.Tests/ConstructionAtbTests.cs`. Depends on [construction attribute](construction-attribute.md). No command changes in this pass.

## Outcome

Basemod has a ship component that carries the installation rate, a Construction Office that writes that rate, a shipyard that also carries a refit rate, and a colony ship design Earth can build.

## What already runs

Templates live in `GameData/basemod/TemplateFiles/installations.json`. Designs live in `ScenarioFiles/designs/componentDesigns.json`. Ships live in `ScenarioFiles/designs/shipDesigns.json`. Earth's unlocked ids are in `ScenarioFiles/systems/sol/earth.json`.

`local-construction` is `PlanetInstallation` only. Its attribute line is an `installation` rate of Level times 5 on `ConstructionAtb`. The design `default-design-local-construction` is level 1.

The shipyard template is `ShipCargo, PlanetInstallation`. Its `IndustryAtb` rates are `component-construction` at Crew Size times 0.01, and `ship-assembly` at Crew Size times 0.02. Default Crew Size is 10000. Slip Size defaults to 10000. Its `ConstructionAtb` key is `ship-refit` at Crew Size times 0.0005.

The freighter in `shipDesigns.json` is the pattern for a small ship: bridge, tank, cargo, power, thrust. `sol-trade` is not edited.

## Decision

`GameData/basemod` only.

New template `ship-construction` in `installations.json`. `ShipComponent` only. `ConstructionAtb` with an `installation` rate of 5. One design, `default-design-ship-construction`.

Edit `local-construction` so the attribute args are an `installation` rate of Level times 5. Mount stays `PlanetInstallation`. It does not gain `ShipCargo`. The display name stays Construction Services.

Edit `shipyard`. Add `ConstructionAtb` with `ship-refit` only. Leave the `IndustryAtb` block as it is. Refit points are Crew Size times 0.0005, so the default yard has 5. That formula sits on a property next to Crew Size.

New ship `default-ship-design-colony-ship`. It carries `default-design-ship-construction`, a cargo hold, a bridge, a tank, power, and thrust, on the freighter's pattern. Add the id to Earth's `ShipDesigns`. Earth does not start with one in a fleet. Unlock `default-design-ship-construction` the same way Earth unlocks `default-design-local-construction`.

The current yard still drops a finished ship into orbit. Holding the hull is [yard holds hull](yard-holds-hull.md).

## UI

No new control. The shipyard row on the Production tab lists the faction's ship designs in the new-job combo, so the colony ship appears there once it is unlocked. The entity window does not offer a seed job.

## Tests

A loaded basemod faction has the ship-construction design, the colony ship, an office whose only construction key is `installation`, and a shipyard whose construction key is `ship-refit` and whose industry keys are unchanged. The colony ship design names the ship-construction component. `sol-trade` files are untouched.

## Left out

Any new command. Mount and assemble behavior. Refit behavior. A starting colony ship already in orbit.
