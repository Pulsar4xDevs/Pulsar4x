# Mine asteroids

A ship with an asteroid miner finds a surveyed asteroid in the current system, mines into its hold, and hauls the ore home.

Home is the nearest colony this faction owns that can take at least one mineral in the hold. Nearest means the least travel time, not the shortest line. If no owned colony can take the ore, the ship sells at the nearest friendly colony that is buying it. Same-faction unload does not touch the ledger. A friendly sale uses the existing market bid.

The gate is `BodyType.Asteroid`, and the faction's survey of that rock must be finished. Moons and planets are out. The ship has to be at the rock. The old sphere-of-influence check is not the gate.

`GoalType.Mine` stays the unused colony weight. The ship uses `GoalType.MineAsteroids` and `AsteroidMineAbilityDB`. It does not create `MiningDB`. One tech, `tech-asteroid-mining`, unlocks the `asteroid-miner` template. Earth starts with that tech researched and one ship, Prospector I, in the Mining Fleet.

The mine order finishes when the hold is full or the rock is empty. The ship then picks the next surveyed asteroid itself. A fleet gives each free miner a different rock and does not hand a second rock to a ship that is still working.

## Travel time

`TravelTime.TryNearest` picks the candidate that takes the least time to reach. The same system uses straight-line warp time. Another system adds the known-jump path. No warp drive returns none. Unreachable candidates are skipped. A tie goes to the lower entity id.

## Later

Notes only. Do not build these until asked.

- A hauler meets the miner. The mine order already ends when the hold is full, so a haul can replace the return.
- A cache on the rock, so the miner does not have to carry the ore home.
- Moons and other small bodies. The question of which bodies are in scope is still open.
- Search surveyed asteroids in systems one known jump away, ranked by `TravelTime`.
- Further tech levels: a higher rate, then mining without entering orbit.
- A mineral filter, a mining skill as quality on the rate, and a claim on a rock.
