# Stations

Status: not started. Do not build until asked, and not before [ship refit](ship-refit.md).

## Outcome

A station is a ship design that does not need thrust and cannot move. A planetary yard builds it and a pad launches it. A station's own yard can refit a ship in range and can put a finished hull in orbit without a pad.

## What already runs

`IsShipDesignValid` requires mass, newtonian thrust, and energy generation plus storage. The designer that saves designs is `Pulsar4X.Client.Host/Designer/ShipDesignWindow.cs`. The ported client has no designer. `ComponentMountType` has no station value. A shipyard completion's orbit path is changed by [yard holds hull](yard-holds-hull.md): no pad means the hull waits.

## Decision

A station flag on the ship design. With the flag set, the designer saves the design without newtonian thrust. Movement orders reject it. This plan does not port a designer into `Pulsar4X.Client`.

A planetary yard and a pad still launch it. A station that has a yard may refit a ship in range, using the refit plan. It may release a hull from its own yard without a pad, because it is already in orbit.

## UI

`ShipDesignWindow` gains the flag. The station uses the ship entity window, and that window does not offer a move. Its yard line shows waiting hulls the same way a colony yard does. A Release control puts the hull in orbit without a pad. The same line can refit a ship in range.

## Tests

- A station design saves with no thrust and is valid.
- A move order against a station is rejected.
- A station yard releases one waiting hull without a pad.
- A colony yard still waits for a pad.

## Left out

A new entity type. Porting the designer. Combat, sensor, and logistics differences for a station.
