# Save and load

Two plans. Both are implemented on branch `save-load` (2026-10-17). Plan 2 started only after a load returned a game that can be stepped.

The save is the host's full `Game`. The screen after a load is one faction's slice of that game. A future server loads this same file and pushes a slice per connection. These plans do not add a pull API, a network client, or a second human.

| # | Plan | Depends on |
|---|---|---|
| 1 | [A save comes back as the same game](01-round-trip.md) | — |
| 2 | [Work in progress survives a load](02-work-in-progress.md) | 1 |

Out of both plans: the crash-save beside the fault dialog, restoring the open windows, the selected ship, the camera, or space-master mode, and the nine tests already failing on `DevBranch` (`Prune_RunMarket_RequiresALogisticsOffice` and the eight hyperbolic cases in `OrbitFuzzTesting`).

Later, not a third plan: [saved things should be data](saved-data.md). Do not build it until asked.
