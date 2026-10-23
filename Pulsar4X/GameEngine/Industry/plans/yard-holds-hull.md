# Yard holds the hull

Status: implemented. Tests: `Pulsar4X.Tests/YardHoldsHullTests.cs`. Does not depend on the construction rate. The shipyard's `IndustryAtb` `ship-assembly` rate is the one that builds the hull.

## Outcome

A finished ship stays on the yard until a launch pad puts it in orbit. The slip size rejects a design that is too heavy. A yard with no pad does not create a ship.

## What already runs

A ship-assembly completion records the hull on that production line: design id, name, and mass. It does not call `CreateShip`. `LaunchComplexProcessor` is the only path that creates the orbiting ship. Each free pad launches at most one fitting hull per day, after the colony's existing launch queue. A hull with no free pad, not enough fuel, or mass over every pad stays on the yard. A yard with no pad does not create a ship.

Slip size is the line's `MaxVolume`. A ship design heavier than that slip is refused before the job is queued. The comparison is the same one the pad uses against `MassPerUnit`. A component built on the same line is not a hull and is not slip-checked.

The Production tab lists each waiting hull under the shipyard line, with design, name, mass, and why it is still there. A design over the slip is disabled in the new-job combo. The production view carries slip size, design mass, and pad state. Scenario launch-queue entries still launch. Refit and stations are not started.

## Decision

Completion records the hull on the yard: design id, name, and mass. It does not call `CreateShip`. `LaunchComplexProcessor` is the only path that creates the orbiting ship. A hull with no free pad waits. A design whose mass is over the slip is refused before the job starts.

This plan does not add `ship-refit` and does not change construction.

## UI

The Production tab's shipyard line is the screen. An "In yard" list under that line shows each waiting hull: design, name, and mass, and why it is still there: no free pad, not enough fuel, or tonnage over the pad. No launch window. A design over the slip is disabled in the new-job combo, and the tooltip says so. The production view carries slip size, design mass, and pad state.

## Tests

- A ship-assembly completion leaves no ship entity and leaves one hull on the yard.
- A pad with fuel and enough tonnage creates the ship and clears the hull.
- No pad: the hull remains after the launch tick.
- Over-slip: the job is refused.

## Left out

Refit, stations, and releasing a hull from a yard that is already in orbit.
