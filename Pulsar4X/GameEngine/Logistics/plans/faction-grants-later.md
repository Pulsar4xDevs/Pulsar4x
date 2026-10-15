# Faction grants, later

Status: stub from 2026-10-15. The checkbox pass is in. Do not build this until asked. It is not logistics plan 13.

The landed pass is one-way grants on the granter. Status (Hostile, Neutral, Friendly, Allied) is map colour only and does not tick the boxes. Trade shows their market and, for now, their colonies. Ships shows their ships only. A trade finishes only when both granted Trade. Geo, Grav, and Research are saved and do nothing. A Friendly or Allied blueprint seeds Trade and Ships both ways. `Game.Load` fills that pair once where a Friendly or Allied stance has no grant entry yet. A stored None stays None.

## Next slice: colony locations, separate from ships

Add a one-way **Colonies** box beside Ships. Theirs shows you their colony locations. Yours shows them yours. The two directions can disagree. Ships does not reveal a colony. Colonies does not reveal a ship.

What a revealed colony carries: name, faction, position, and which body it sits on. Not cargo, installations, industry, or editing. Owned colonies still open colony management. Any other colony opens the entity window.

Today the Trade box reveals every colony, including one with no office. That bundling goes away. Trade keeps the one colony that holds the office, so the market bars still have a body. Every other colony, and a colony with no office, waits for the Colonies box.

A Friendly or Allied placement should seed Colonies as well as Trade and Ships. A save that can see those colonies now should still see them after the split. A stored None is not refilled.

## Left from this pass, not in that slice

Each of these is its own later piece. Do not fold one into the colony-location change.

- **Geo, Grav, Research.** The boxes exist. Nothing reads them. What each box allows is not chosen yet.
- **Transponder / IFF.** A later component can set the Ships grant for one hull. That is the identified-contact slot. Encrypted allied IFF and detection quality on the client are the same piece.
- **Sensor profiles.** `ShipFactory` and `ColonyFactory` still do not add `SensorProfileDB`. An empty profile on every ship and colony must not make it visible. The blueprint signature pass (what should carry `SensorSignatureAtb`, and the infrastructure placeholder) is the same piece. No range tuning in that pass.
- **Opinion and diplomats.** Two-sided opinions that can disagree, and anyone who changes a box other than the player. Not started.
- **Status in combat.** Hostile, Friendly, and Allied still only change colour. Allied and Friendly share one colour.
- **Fog leftovers.** `AccessControl.IsOwnedEntityAuthorized` still returns true. A contact's detection quality is not sent to the client. Grants already re-send known systems to both factions when Trade or Ships changes.
