# Near-term work

Ordered list from 2026-10-15. A player hits the first group as soon as a game is running. The second group is still play, but only after ships, the clock, and the windows behave. The third group is new systems and cleanup that can wait.

GitHub issues are not on this list. Logistics plans 1–12 and 5b, survey charts, refine-one-batch, mine-asteroids, and Ceres plans 1–5 are already marked done.

## First: bugs and missing controls

1. **Done: one-way grants for trade and ship positions.** Status is still the map colour. Each side has its own Trade, Ships, Geo, Grav, and Research boxes. Their Trade grant shows you their market and colonies. Their Ships grant shows you their ships. A trade finishes only when both granted Trade. Geo, Grav, and Research are saved and do nothing yet. A Friendly or Allied placement still opens Trade and Ships both ways.
2. **Done: one pending agent wake, and the clock moves past a finished survey and a jump key.** `ScheduleAgent` replaces that entity's queued `AgentProcessor` row. A finish wakes the agent once. One `RecheckInterval` poll stays as the backstop. The Mars fleet survey runs past the first survey tick. A grav survey that includes the outer anomaly returns. `ProcessNextInterupt` removes the jump key it just ran. A second pass still sitting on the current instant throws and names the processor. Plan: `GameEngine/Engine/Orders/agent-wakes.md` (2026-10-17).
3. **Done: the player can cancel a goal or an action.** Cancel now stops the work. A warp that is already up drops into an orbit around the sphere of influence the ship is in, with the saved newtonian. Cancel when convenient finishes the step that has already started and does not start the next. A MoveTo warp that is up is one transit and still circularises. One action line cancels immediately. `GivenGoal` stays, with `PlayerCancelled`, so the ship is not handed a new task. A colony order is rejected.
4. **Saving does not work.** Plan 1 is in. An Earth colony round-trips (`SaveRoundTripTests.EarthColony_RoundTripsTheSavedGame`), and the host binds `LocalHumanFactionId`. A bad file stays on the load dialog. Plan 2 is still open: a warp or a burn in progress does not resume. The crash-save beside the fault dialog stays a later piece. Plans: `GameEngine/Engine/plans/` (2026-10-17).
5. **The Pause checkbox does not hold a circularise or a survey.** That fix is sitting in `stash@{0}` and is not in the tree. Move-to can stop once. The other two pause again on every tick while the ship is still in warp.
6. **A few movement failures stop a ship or the clock.** Cloning a warp or a newton burn throws, so a standing order of either crashes the pulse. A warp whose target starts thrusting arrives by throwing. A ship that is short of ΔV, or waiting on "charging batteries", sits there with no failure the player can read.
7. **Quickstart can leave a blank window.** If `GameLifecycle.Quickstart()` returns null, the main menu is already hidden and Escape does nothing.
8. **A bad industry job throws inside the daily processor and stops time.** A normal stainless-steel batch is fine. A missing cargo store, a bad design, or an empty recipe is not.
9. **Zooming in too close to a planet freezes the UI.** The window stops responding. The cause is not known yet.

## Next: what the player uses every session

10. **Hauling your own goods is too hard.** `Freighter` and `FleetFreighter` are in, and Fleet Management can issue Haul and Haul Contract. The ship still only moves one posted good between your own colonies, and it fails with "no haul" when it cannot find a pair. `HelpOwn` is only a weight. It does not become an order. `LogisticsSimple` was going to become a goal and never did. The player needs a plain "take this from here to there" for their own stock.
11. **Install a facility that is sitting in cargo into a facility that is already built.** Colony Management has Construction and Installations. `LocalConstructionProcessor` spends points and adds the component. It does not take the facility out of cargo.
12. **Colony Management and the Factions window should work the same way.** Colony Management is "Manage Colonies": an owned-colony tree, then production, construction, and installations. Factions is `FactionStanceWindow`. One of them should be brought into line with the other.
13. **The entity window should be a short summary that links to a detail window.** `EntityWindow` is one long page: header, actions, market, orbit, survey, ship status, components, minerals, mining, installations, and colony environment.
14. **An agent log for one entity, readable by the player.** The debug window lists future processor interrupts. It is not a history, and it is not a player window. The log should say what that ship, fleet, or colony decided and why.
15. **Command span needs a definition pass.** The label on Fleet Management comes from the flagship bridge. A fleet-level bridge currently counts as Body, the same as a ship bridge. Well starts at Colony, Planet, or SOI. System starts at System, Sector, or Empire. Logistics span is one step wider than survey span. An asteroid survey ignores the span and uses a neighbourhood around the rock. The order tree greys out anything the bridge does not cover. The rules and the code should be made to match one definition.
16. **Earth and Luna markets show a price of 0.** New stock is listed at ask 0 and bid 0. Ceres fuel, ore, and charts use fixed test prices. A real price pass can wait. A player opening Earth's market should not see everything as free.
17. **Try the direct offer in a running game.** The code and `CeresDirectOfferTests` are in. Commit `61c4a603` says it has not been played. This is the same Lode return the action-multiplication bug shows up on.

## Later

These are written down. A player does not trip over them until the list above works.

**Economy, when you ask for it.** Ceres consignment (plan 6, not started). Sealed shipments (the plan is staged and not committed). Office purses (an idea, not a plan). A real price model, including how a bid moves. A ship-only faction still cannot post its own buy or sell. That limit stays until you ask for it.

**Grants, when you ask for it.** `GameEngine/Logistics/plans/faction-grants-later.md` is a stub, not plan 13. Next slice: colony locations as their own box, separate from ships. Trade would keep only the office colony so the bars still have a body. Geo, Grav, Research, a transponder, sensor profiles, opinion, and combat use of status are listed there and unbuilt.

**Colony growth, after someone is seated.** The administrator notes are not a plan: personality, a civilian on Luna, bids, then construction that spends cargo. Factory, shipyard, and other installation goals are unwritten and wait on that. Earth's chair stays empty.

**Goals, after issued orders are trustworthy.** Playtest MoveTo, survey, ScanAnomalies, and RefuelAt. Show the active goal and its message on the snapshot. Let a ship refuel and then resume what it was ordered to do. Autonomous choice when nothing was ordered. Stop a fleet failure from leaving the other ships running.

**Movement and damage cleanup.** Split the arrival burn out of the warp action. Make warp survive a save. Remove the second warp-creation path and the dead orbit helpers. Damage is still the simple random-HP path. The detailed sim is too expensive and is not hooked to combat. Component health is a manual viewer button.

**Smaller follow-ups already noted on finished plans.** One good per haul. No fuel cost on a jump. Unlisted surplus is not a haul by itself. A hauler meeting a miner, a cache on the rock, and mining moons. Restoring a colony's offer-stock job after a supply order replaces it. The crash-save half of the fault dialog.
