# Ship refit

Status: implemented. Tests: `Pulsar4X.Tests/ShipRefitTests.cs`. Depends on the shipyard's `ship-refit` rate from [construction components](construction-components.md).

## Outcome

A shipyard can change the parts on a ship in transfer range. The colony's Construction Office cannot. The job does not build a hull.

## Rules

`ship-refit` is the only construction key this plan spends. The yard's manufacturing rates are untouched.

Each part removed and each part mounted adds `max(1, MassPerUnit / 100000)`. The job copies the sum when it is queued. It does not read `IndustryPointCosts` for those parts. The slip must fit the ship, using the same comparison as [yard holds hull](yard-holds-hull.md).

Removed parts return to the colony warehouse as instances. Mounted parts leave that warehouse. The ship is unavailable until the job finishes. The job does not create a hull and does not touch a colony installation.

## What already runs

A refit names one ship in transfer range and the instances to remove and to mount. The yard spends `ship-refit` points. Each part removed and each part mounted adds `max(1, MassPerUnit / 100000)`, copied onto the job. A ship heavier than the line's slip is refused. An office with only `installation` is refused. A colony installation is refused. Removed parts return to the warehouse when the job finishes. Mounted parts leave it. The ship takes no new orders until then. Move to Storage still works. The job does not create a hull. `ShipRefitBegan` and `ShipRefitCompleted` are published. Stations are not started.

## Decision

A refit names one ship in transfer range and the instances to remove and to mount. An office with only `installation` rejects it. A ship over the slip is rejected. A colony installation is rejected.

## UI

The shipyard line on the Production tab is the screen. A refit section on that line picks a ship in transfer range, lists the parts on it and the ship-part instances in the colony warehouse, and shows the summed cost before the job is queued. The colony Construction tab does not offer this.

The ship's entity window shows the refit while it runs. It is not the editor. Its Move to Storage menu stays as it is.

## Tests

- A yard and a ship in range swap one component for one from cargo. The old part is in the warehouse. The job's cost is the mass formula for the parts that moved.
- An office with only `installation` rejects the same job.
- A ship over the slip is rejected.

## Left out

Stations. A new ship designer. Changing Move to Storage on the ship.
