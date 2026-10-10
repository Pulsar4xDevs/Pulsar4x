# Buy fuel

Status: not started.

When a standing fleet job needs fuel, it hands the ship a purchase of methalox from Ceres Depot at the depot's ask. The ship pays. `RefuelAt` stays a free cargo transfer and is not this step.

## Depends on

[Standing survey and mine jobs](02-standing-jobs.md). The fleet goal has to be the thing that hands the child. The depot's methalox ask comes from [plan 1](01-consigned-charts.md).

## Outcome

Both standing planners, before handing a survey or a mine, check the flagship tanks. Low fuel hands a buy-fuel child instead of the next rock. The child buys methalox from the depot with `MarketExchangeAction`, at the listed ask, up to the free tank space and up to what the faction can pay. A full tank is not required. At 3 per unit the tanks hold more than the starting purse.

The buy is a child of the standing fleet goal. The ship does not assign it. When the purchase finishes, the fleet goal continues with the rock or the mine it postponed. If the depot has no methalox for sale, or the purse is empty, the standing goal stays active and retries later. It does not fail the company's job.

Ships start with full tanks, so placement does not buy fuel. The test lowers the tank.

## What to build

One child goal, or one action the standing planners return, used by both fleets. It moves cash. It does not call `CargoTransferOrder.CreateRefuelPair`. Range is the same cargo-transfer range a market fill already uses, and both ships already start at Ceres.

Do not wire `ShouldInterruptForRefuel`. That changes `ActiveGoal` under a foreign `GivenGoal` and restores it afterwards. These fleets have no order above the standing job, so a child of that job is enough.

## Tests

- A standing survey fleet with a low tank is handed a fuel buy, not a survey child. After the buy, Strata's balance dropped by ask times units, the depot's balance rose by the same, and the tank rose.
- The units bought are limited by cash and by free space.
- A full tank is not handed a fuel child.
- An empty purse leaves the standing goal active.
- `RefuelAt` still creates a cargo pair and does not touch the ledger.

## Out

No price movement. No refill of a whole tank from the starting purse. No interrupt of a player or superior order.
