# Construction attribute

Status: implemented. Tests: `Pulsar4X.Tests/ConstructionAtbTests.cs`. Do not start [construction components](construction-components.md) in the same pass.

## Outcome

`LocalConstructionAtb` is `ConstructionAtb`. Its one point number is a dictionary of points per day. The basemod template names that class. The processor still mounts a free copy of a queued design.

## What already runs

`LocalConstructionAtb` stores `Level` and `PointsPerDay`. On install it adds `Level * PointsPerDay` to `LocalConstructionDB.PointsPerDay`. A level-1 office is 5 points a day. `LocalConstructionProcessor` runs once a day, first run at six hours, and calls `AddComponent` when `Design.IndustryPointCosts` is paid. It does not read cargo.

Saves use `TypeNameHandling.Objects` and name `Pulsar4X.Industry.LocalConstructionAtb`.

The Construction tab reads `ConstructionView.PointsPerDay`.

## Decision

The rate keys are new. Do not reuse `installation-construction`, `component-construction`, or `ship-assembly`.

- `installation` mounts a colony installation, or assembles a site-only design. Later plans spend it. This plan only stores it.
- `ship-refit` changes the parts on a ship. The shipyard's rate is [construction components](construction-components.md).

Templates pass a dictionary of rates. The office's `installation` entry is Level times 5. The basemod template's `AttributeType` is `Pulsar4X.Industry.ConstructionAtb`.

`LocalConstructionDB.PointsPerDay` is the installation pool, so the existing view keeps reporting one number. Mounting adds that key. A component with no installation rate does not create the pool. The processor is unchanged.

This plan does not add the ship component, and does not put `ship-refit` on the shipyard. A save that still names `LocalConstructionAtb` is not loaded.

## UI

No client change. The Construction tab header still shows one number, the installation pool. It does not mention `ship-refit`.

## Tests

- A level-1 office, built from `default-design-local-construction`, reports 5 installation points and no `ship-refit` points.

## Left out

Spending cargo, the ship component, factory output, the yard, refit, and stations. Whether `IndustryAtb` stays one class is not this plan. Loading a save that still names `LocalConstructionAtb`.
