# Ceres faction jobs

Plans for Strata Survey, Lode Mining, and Ceres Depot. They are not logistics plan 13. Logistics plans 1–12 and 5b stay as they are. Price finding stays the later economy session noted in `agent-notes.md`. The general rule that a ship changes only `ActiveGoal` under someone else's order stays unwired. See `agents-and-goals-design.md`.

Do them in order. Each one is playable on its own. Do not start a later plan before the one it lists under Depends on.

| # | Plan | Depends on |
|---|---|---|
| 1 | [Consigned charts and fixed prices](01-consigned-charts.md) | Survey charts already implemented. Offer stock already implemented. |
| 2 | [Standing survey and mine jobs](02-standing-jobs.md) | 1 |
| 3 | [Buy fuel](03-buy-fuel.md) | 2 |
| 4 | [Depot room](04-sell-ore.md) | 1 |
| 5 | [Direct offer](05-direct-offer.md) | 1, 2, 4 |
| 6 | [Consignment](06-consignment.md) | 4, 5 |

Out of these plans: a new colony or station, a faction-level agent, autonomous Decide, price finding, a price argument, and daily warehouse rent. A larger office than the second one waits on the colony administrator.
