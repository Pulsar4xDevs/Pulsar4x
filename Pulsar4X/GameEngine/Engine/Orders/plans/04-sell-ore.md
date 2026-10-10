# Depot room

Status: implemented.

Ceres Depot can list five cargo ids, holds a purse of 100, and has one warehouse. The prospector will bring more ore than that purse can buy. This plan only makes room. It does not make the ship sell.

## Depends on

[Consigned charts and fixed prices](01-consigned-charts.md) for the placed depot. No new goal.

## Outcome

The depot has two logistics offices, so the book holds 10 cargo ids. It has two warehouses. Its purse is 100000. Strata and Lode stay at 100. Earth and Luna keep one office and one warehouse.

Methalox, iron, and water still use three of the ten slots. Seven are free. Iron's buy quantity stays 0. The prospector still waits.

## What is already true

`AddComponent` installs `Amount` instances, and each one runs `OnComponentInstallation`. The depot already does this with two fuel tanks. Earth's two labs and Luna's two mines are the same pattern.

`LogiBaseAtb` sets capacity to the sum of the logistics offices on that colony. One office is the template default of 5. A second copy of `default-design-logistics-office` makes 10. Do not edit that shared design. Earth and Luna use it.

`CargoStorageAtb` adds each warehouse's volume onto the same general-storage pile. The warehouse design asks for 1000000 m³. The template max is 10000, and `SetValueFromInput` clamps to that max, so one warehouse is 10000 m³. The prospector's hold is 1000 m³. One warehouse already holds ten full holds. The starting iron is a fraction of one m³. A second warehouse makes 20000 m³, which is spare room for ore that sits while the ship keeps mining.

## What to build

On the Ceres Depot colony blueprint only:

- `default-design-logistics-office` amount 2.
- `default-design-warehouse` amount 2.
- `StartingFunds` 100000.

Add `CeresStart.DepotFunds` as 100000. Leave `StartingFunds` at 100 for Strata and Lode. The depot test that expects the shared constant follows `DepotFunds`.

100000 is about a hundred days of a ten-mineral rock at the miner's 100 units per mineral per day, if every unit is bought at 1. A full hold of iron is on the order of eight million units, which this purse does not buy. The hold will not be full of one mineral on any run we care about. 10000 would cover about ten such days and then the wait would return.

## Tests

Do not run the clock.

- After placement the depot book capacity is 10, general storage max volume is 20000, and the depot balance is 100000.
- Strata and Lode are still 100, plus or minus the chart sale from placement.
- Luna's office capacity is still 5.

## Limit

The clock was not run. Iron's buy quantity is still 0, so the prospector still has nowhere to sell.

## Out

No buy quantity. No consignment. No rent. No change to Earth, Luna, or the shared office and warehouse designs. No colony-administrator construction.
