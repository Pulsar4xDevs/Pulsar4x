# Colony construction

Status: implemented. Tests: `Pulsar4X.Tests/ColonyConstructionTests.cs`. Depends on [construction attribute](construction-attribute.md). Reads instances produced by [manufacture instances](manufacture-instances.md).

## Outcome

A colony with an installation rate can mount a crate from its warehouse, or assemble a design that cannot be cargo. Both spend the colony's installation points. Mounting does not spend the recipe again.

## Rules

`installation` is the only key this plan reads. A job spends the colony's pool.

`ShipCargo` plus `PlanetInstallation` is a crate. No `ShipCargo` means site-only. Site-only today: `mine`, `naval-academy`, `admin-complex`, and `launch-complex`. `local-construction` became a crate (`ShipCargo, PlanetInstallation`) so a ship can carry the first office to an empty colony. `space-port` has `ShipCargo` and is a different template.

A crate does not store a point cost. It points at its design.

The cost to build a design is `IndustryPointCosts`, stored when the design is calculated from `BuildPointCost`. An assemble job copies that number onto itself.

A mount job copies `max(1, MassPerUnit / 100000)`. A default factory is mass 5,000,000, so the mount cost is 50. Infrastructure is mass 1,000, so the mount cost is 1. The divisor lives in one function. A later edit to the design does not change a job already queued. Two crates of the same design cost the same.

## What already runs

`LocalConstructionProcessor` spends the colony's installation points on the head of the queue. A mount copies `LocalConstructionJob.MountCost` (`max(1, MassPerUnit / 100000)`) and, on completion, removes that warehouse instance and mounts it. An assemble copies `IndustryPointCosts` and the recipe. It spends points, and a share of the recipe, only while every remaining input is on hand. A short input waits and changes no stock. A design id with `ShipCargo` and no instance does not mount. Cargo Install on a colony crate enqueues a mount. A ship with the rate installs onto a colony in transfer range; that is [ship construction](ship-construction.md). The Construction tab lists warehouse crates and site-only designs. Move to Storage is unchanged.

## Decision

Two job kinds on the colony's installation pool.

Mount: one warehouse instance with `PlanetInstallation` and `ShipCargo`. On completion, remove that instance and `AddComponent` that same instance. The recipe is not spent.

Assemble: a design with `PlanetInstallation` and no `ShipCargo`. The warehouse holds every `ResourceCosts` entry. Points and resources are spent as the job runs, the same way `ConstructStuff` spends a recipe. Short resources wait. They do not throw. On completion, mount a new instance.

The processor no longer mounts a free copy from a design id alone.

## UI

The Construction tab is this plan's screen. The header says installation points per day. Each queue row says Mount or Assemble and shows the point cost copied onto the job. The list has two groups: crates in the warehouse, with the mass cost, and site-only designs, with `IndustryPointCosts` and any missing resources. A freightable design with no crate is absent. `ship-refit` is absent. The empty state asks for a Construction Office.

The cargo list's Install item returns on a colony. It enqueues a mount and the tooltip shows the mass cost. It does not mount on the click. Move to Storage stays as it is.

## Tests

- Mounting a stored factory removes that instance and adds one installation. The job's point cost is the mass formula, not `IndustryPointCosts`.
- Assembling a mine with the recipe spends it and mounts a mine. That job's point cost is the mine's `IndustryPointCosts`.
- Short one input: no mine, and the stock is unchanged.
- A mount whose cost is above the day's points does not finish on the first day.
- The instance has no point-cost field.

## Left out

Ship actors, hulls, refit, and stations. `IndustryAtb` is not split.
