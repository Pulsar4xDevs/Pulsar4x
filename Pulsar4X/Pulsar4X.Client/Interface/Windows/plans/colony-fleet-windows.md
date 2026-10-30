# Colony and fleet windows

Status: implemented 2026-10-27. Summary columns, the administrator button, and the ship Construction tab were adjusted 2026-10-30. Not clicked through in a running game. Written 2026-10-27 from near-term item 12.

Colony Management and Fleet Management both manage an owned thing. This pass makes them a list plus tabs, and makes a ship's summary use the same three-column shape as a colony's. The window titles stay "Manage Colonies" and "Fleets and Ships".

No shared window type. The trees still differ: a fleet row drags and has a context menu, and a ship click still toggles the drag checkbox.

## List

Fleet Management keeps its tree. The middle orders column goes away, so the window is the list and the tabs.

Colony Management uses the same left width, 256px. A system with no owned colony is left out. A colony row is a tree leaf with the normal highlight, and the population stays on the label. The red button goes away. The inner header reads "Colonies", with "Select a colony to manage it."

When the open colony does not resolve, the window selects the first owned colony in the list. That is the first known system that has one, then the first owned colony in that system. `SelectColony` from the map, the system window, or the entity card still wins.

## Tabs

A selected fleet's tabs are Summary, Orders, Standing Orders, and Log.

A selected ship's tabs are Summary, Orders, and Log. A ship whose construction view is a ship bay also gets Construction after Orders. A freighter does not, and a fleet does not. The Components tab goes away. Its contents move into the summary.

Colony tabs stay Summary, Production, Construction, Market, Mining and Naval Academy when those exist, and Log.

The Orders tab has three equal columns. The first is the live queue: the goal, Cancel now, Cancel when convenient, and the queued lines with pause and cancel. An empty queue still says "None". The second is the list of goals or actions. The third is the form that adds one. Standing Orders stays fleet-only.

## Ship summary

Three equal columns, the same split as the colony summary.

The first column is the ship, in the place of the planet dossier. It keeps the current block: name, design, captain, and system. Crew sits under that block: one row, "Required", from `ShipView.CrewRequired`. That number is the design total. There is no headcount aboard, and no species list. Power stays under crew.

The second column is components only, in the place of installations. Components is the current Components tab: `InstallationsView`, headed "Components". An empty ship says "No components on this ship."

The third column is cargo, in the place of the stockpile. It is the cargo block the summary already shows. Above it, Initiate Transfer calls `CreateTransferWindow.SetLeft` with the ship's id and its system id, then opens that window. The partner list already includes any entity in that system with a cargo view. The button is hidden when the ship has no system id or no stores. The range check inside the transfer window is still the existing todo.

A fleet with no ship selected keeps the single Fleet Information block. It is not split into three. It has no crew, no component list, and no cargo of its own. Its Orders tab still shows the fleet queue in the first column.

## Colony summary

Three equal columns.

The left column starts with a Colony block: the colony's own name, and an Administrator button. The button opens the same people chooser as a captain, limited to civilians, including None. A colony with no colony-level chair shows None and does not open. Population and infrastructure sit under that block, then the planet dossier and atmosphere.

The middle column is installations only. The stockpile stays in the third column.

The chair is the first admin seat at colony level or wider. `AssignAdministratorCommand` seats a civilian of this faction, or clears the chair when the id is negative. Someone already posted elsewhere leaves that post first. A navy officer or a scientist is rejected. The log notes taking or leaving the post. This does not draw a civilian from the population.

## Ship construction

A ship with a construction bay has a Construction tab in Fleet Management, after Orders. It is the same construction display the colony uses, including the colony picker. Cargo Install on the ship summary reads that pick. The entity card does not show the queue, and it sizes to the summary.

## Left for later

Tracked as near-term item 18. Colonies will group the way fleets do, by sphere of influence rather than by a fleet. Earth's sphere would include a colony on Luna. One colony in the sphere is the capital, and the player chooses it. A body with no colony, such as Jupiter, is not a candidate. A colony on one of its moons is. An administrator for that sphere is a later piece. The title for that seat is not chosen. This pass does not add the group or the capital.

Drawing a civilian from the population stays near-term item 19. Personality and bids stay in the colony-administrator note.
