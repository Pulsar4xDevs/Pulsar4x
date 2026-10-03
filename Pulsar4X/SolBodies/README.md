# SolBodies

Pulls the largest measured asteroids from the JPL Small-Body Database and writes them into basemod. No API key. The conversion itself is `SbdbSmallBodyImporter` in GameEngine; this console only fetches and writes files.

From the repo root:

```bash
dotnet run --project Pulsar4X/SolBodies/SolBodies.csproj -- --count 500
dotnet run --project Pulsar4X/SolBodies/SolBodies.csproj -- --count 200 --field main-belt --field trojans
dotnet run --project Pulsar4X/SolBodies/SolBodies.csproj -- --dry-run
dotnet run --project Pulsar4X/SolBodies/SolBodies.csproj -- --help
```

The project targets net8.0. If the installed SDK is newer than the one pinned in `global.json`, prefix the command with `DOTNET_ROLL_FORWARD=LatestMajor`.

## Options

| Option | Meaning |
|---|---|
| `--count N` | How many bodies to keep. Default 500. This is a total, not per field. |
| `--field NAME` | Limit to one field. Repeat the flag to combine fields. Omit it to take every field. |
| `--basemod PATH` | Basemod directory that contains `modInfo.json`. Normally found by walking up from the working directory. |
| `--dry-run` | Query and print the counts. Write nothing. |

Fields: `main-belt`, `mars-crossers`, `near-earth`, `trojans`, `centaurs`, `trans-neptunian`, `other`.

`main-belt` is IMB, MBA, and OMB. `near-earth` is IEO, ATE, APO, and AMO. `other` is anything left over, including AST. Asking for `other` leaves the JPL class filter off and drops the named fields locally.

## What a run writes

Bodies are the largest ones that have a measured diameter and an eccentricity below 1. Names already used by a hand-authored Sol system body (planets, moons, Ceres, the other dwarfs, Halley) are skipped. A previous `asteroids-*.json` is not a skip source, so a rerun can replace itself.

Files land in `GameData/basemod/ScenarioFiles/systems/sol/`:

- `asteroids-main-belt.json`
- `asteroids-mars-crossers.json`
- `asteroids-near-earth.json`
- `asteroids-trojans.json`
- `asteroids-centaurs.json`
- `asteroids-trans-neptunian.json`
- `asteroids-other.json`

An empty field is not written. A field file left over from an earlier run is deleted. `sol.json` has its `asteroid-*` body ids replaced with the new set. `modInfo.json` lists the new files just after `comets.json`. Both manifests are rewritten indented. Sol-trade is not touched.

Orbits are heliocentric, including Jupiter trojans. Semi-major axis is stored in kilometres. The orbit epoch is the JPL epoch date. The JSON keeps the real inclination. At runtime `CreateFromBlueprint` still flattens inclination to prograde or retrograde.
