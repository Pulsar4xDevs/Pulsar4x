# MoveTo Phobos (moon you cannot orbit)

## Problem

Phobos's SOI is smaller than a low-orbit warp offset, so drop-in parents the ship to **Mars**.
The old replan then Lambert-scanned a transfer straight from the leftover hyperbola onto Phobos
(often ~9 km/s). Burns ran without an OrbitDB, the goal finished early or hung on an eccentric
Mars leftover, and the Phobos WarpOrder / MoveToPlan tests stayed red.

## Design (Jon)

Treat it like a rendezvous, not a patched-conic capture into Phobos:

1. **Warp** to Phobos+offset (drop-in parents to Mars).
2. **Circularise** around Mars on the leftover.
3. **Match** Phobos's orbit (Hohmann / phase), same as `MatchOrbitAction` for a station.

## What changed

- `MovePlanner.EvaluateNewtonian`: for same-parent targets, **circularise before** any Lambert
  window when leftover e > 0.01.
- `CirculariseAction`: "already circular" cutoff aligned to 0.01 (was 0.05), so it does not
  no-op-loop against the planner.
- `EvaluateAlreadyThere` (moon path): require same orbit sense; treat phase-close co-orbital as
  arrived (Phobos e makes pure Euclidean "along-track" too tight).
- `MatchOrbitAction`: do not treat nearby retrograde leftovers as already matching.

## Still open

- **Retrograde leftovers** (`CircularRetrograde-Phobos`, `LeftoverCircularAtMarsRetrograde-Phobos`):
  need an explicit plane-change (or a Lambert that finishes) before match. Not done here.
- **Empty tank** after circularise-first: ability Δv may still look non-zero; tighten fuel checks.
- Matching Phobos's **true ellipse** (not circular SMA) would tighten separation without
  relaxing the assert for `a*e`.
