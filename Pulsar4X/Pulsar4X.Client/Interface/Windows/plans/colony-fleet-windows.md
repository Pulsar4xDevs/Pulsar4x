# Colony and fleet windows

Status: implemented 2026-10-27. Not clicked through in a running game. Written 2026-10-27 from near-term item 12.

Colony Management and Fleet Management both manage an owned thing. This pass makes them a list plus tabs, and makes a ship's summary use the same three-column shape as a colony's. The window titles stay "Manage Colonies" and "Fleets and Ships".

No shared window type. The trees still differ: a fleet row drags and has a context menu, and a ship click still toggles the drag checkbox.

## List

Fleet Management keeps its tree. The middle orders column goes away, so the window is the list and the tabs.

Colony Management uses the same left width, 256px. A system with no owned colony is left out. A colony row is a tree leaf with the normal highlight, and the population stays on the label. The red button goes away. The inner header reads "Colonies", with "Select a colony to manage it."

When the open colony does not resolve, the window selects the first owned colony in the list. That is the first known system that has one, then the first owned colony in that system. `SelectColony` from the map, the system window, or the entity card still wins.

## Tabs

A selected fleet's tabs are Summary, Orders, Standing Orders, and Log.

A selected ship's tabs are Summary, Orders, and Log. The Components tab goes away. Its contents move into the summary.

Colony tabs stay Summary, Production, Construction, Market, Mining and Naval Academy when those exist, and Log.

The Orders tab has three equal columns. The first is the live queue: the goal, Cancel now, Cancel when convenient, and the queued lines with pause and cancel. An empty queue still says "None". The second is the list of goals or actions. The third is the form that adds one. Standing Orders stays fleet-only.

## Ship summary

Three equal columns, the same split as the colony summary.

The first column is the ship, in the place of the planet dossier. It keeps the current block: name, design, captain, and system. Power stays under that, the way the atmosphere sits under the planet.

The second column is crew, in the place of population, then components, in the place of installations. Crew is one row, "Required", from `ShipView.CrewRequired`. That number is the design total. There is no headcount aboard, and no species list. Components is the current Components tab: `InstallationsView`, headed "Components". An empty ship says "No components on this ship."

The third column is cargo, in the place of the stockpile. It is the cargo block the summary already shows. Above it, Initiate Transfer calls `CreateTransferWindow.SetLeft` with the ship's id and its system id, then opens that window. The partner list already includes any entity in that system with a cargo view. The button is hidden when the ship has no system id or no stores. The range check inside the transfer window is still the existing todo.

A fleet with no ship selected keeps the single Fleet Information block. It is not split into three. It has no crew, no component list, and no cargo of its own. Its Orders tab still shows the fleet queue in the first column.

## Left for later

Tracked as near-term item 18. Colonies will group the way fleets do, by sphere of influence rather than by a fleet. Earth's sphere would include a colony on Luna. One colony in the sphere is the capital, and the player chooses it. A body with no colony, such as Jupiter, is not a candidate. A colony on one of its moons is. An administrator for that sphere is a later piece. The title for that seat is not chosen. This pass does not add the group, the capital, or the chair.

The colony administrator plan stays as it is. Earth's chair stays empty.
