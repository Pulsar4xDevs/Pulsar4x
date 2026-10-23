# Ship construction

Status: implemented. Tests: `Pulsar4X.Tests/ShipConstructionTests.cs`. Depends on [construction components](construction-components.md) and the job rules in [colony construction](colony-construction.md).

## Outcome

A ship that has the installation rate can mount a crate from its own hold onto a colony in transfer range, or assemble a site-only design from resources in that hold. A ship without the rate cannot.

## Rules

The points spent are the ship's installation pool. `OnComponentInstallation` of `ConstructionAtb` puts that pool on the ship the same way the office puts it on a colony. There is no second ability type.

Mount uses `max(1, MassPerUnit / 100000)` and moves the ship's instance onto the colony. Assemble uses the design's stored `IndustryPointCosts`, spends the ship's resources, and mounts a new instance on the colony. The colony does not need a warehouse or its own rate.

If the colony also has an office, each job spends the actor it was given. The office does not spend the ship's points.

`CargoInstallOrder` removes the part from the destination. Do not extend it.

## What already runs

The ship is the actor and spends its own installation points. A ship job stores the colony id on `TargetEntityId`. A colony job leaves that at zero and mounts on itself. The processor resolves the target before it spends. A missing colony, the wrong faction, or a colony out of range waits at the head and spends nothing.

Mount uses `max(1, MassPerUnit / 100000)` and moves the ship's instance onto the colony. Assemble copies `IndustryPointCosts` and the recipe, spends the ship's hold, and mounts a new instance. The colony does not need a warehouse or its own rate. An office on that colony spends only the jobs queued on the colony.

`InstallComponentCommand` and `AddToConstructionQueueCommand` take an optional colony id. A colony order ignores it. A ship order is rejected when the ship has no installation rate, when no colony is named, when that colony is out of range, or when a site-only design is offered as a crate. A freighter is rejected. The colony ship is accepted.

The ship entity window shows the construction section only when the ship has an installation rate: its points, hold crates, site-only designs it can pay for, and a picker of same-faction colonies in transfer range. Cargo Install enqueues a mount at the picked colony. With no colony in range the item is shown and disabled. A freighter does not show it. Colony Management stays a colony screen.

Range is cargo-transfer range. Both sides have cargo storage, they share a parent with a mass, and the transfer rate is above zero. An empty colony store still counts. Hulls in a yard, refit, and stations are not started.

## Decision

The ship is the actor. The target is one colony of the same faction in cargo-transfer range. The command is rejected when the ship has no positive `installation` rate, when the colony is out of range, or when the chosen object is a site-only design sitting in the hold as if it were a crate.

A freighter is rejected. The colony ship is accepted.

## UI

The ship entity window is the screen. A ship with an installation rate gets the same construction section as the colony tab: the ship's points, crates in its hold, and site-only designs it can pay for from that hold. A picker lists same-faction colonies in transfer range. A ship without the rate does not get the section. Colony Management does not grow a ship builder.

The cargo Install item on that ship enqueues a mount at the picked colony. With no colony in range, the item is disabled. A freighter never shows it.

## Tests

- The colony ship, in range, holding an infrastructure instance, mounts it and the ship loses it.
- Out of range is rejected.
- A ship with no installation rate is rejected.
- A site-only design offered as a crate is rejected.
- Assembling an office from resources in the ship's hold mounts it on the empty colony and spends those resources. The colony then reports the installation rate.

## Left out

Hulls waiting in a yard, refit, and stations.
