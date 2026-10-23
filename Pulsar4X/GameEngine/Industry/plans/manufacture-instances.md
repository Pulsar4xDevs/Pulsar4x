# Manufacture instances

Status: implemented. Tests: `Pulsar4X.Tests/ManufactureInstanceTests.cs`. Depends on [construction attribute](construction-attribute.md) for the checkbox that queues a mount. The output change itself does not require the rate dictionary.

## Outcome

A factory completion puts a component instance in the warehouse. It does not mount the part, and it does not store the design object. Refined goods and ordnance are unchanged.

## What already runs

`IndustryProcessor` calls `IndustryTools.ConstructStuff` once a day, first run at three hours. `ComponentDesign.OnConstructionComplete` either mounts a new instance, when `IndustryJob.InstallOn` is set, or calls `AddCargoByUnit` with the design object.

The Production tab checkbox is "Auto-install on completion?". It is shown when `GuiHints` is `CanBeInstalled`, which `ComponentDesigner` sets for `PlanetInstallation`. The command sets `InstallOn` to the colony. The cargo list shows Install only for a `ComponentInstance`, and that command mounts it immediately.

`BuildPointCost` on the template is evaluated by `ComponentDesigner.SetBuildCost` into `ComponentDesign.IndustryPointCosts`. A factory job spends that stored number. This plan does not change that cost.

## Decision

`OnConstructionComplete` for a component stores `new ComponentInstance(design)` in the colony warehouse. It does not call `AddComponent`. It does not store the design.

The checkbox no longer mounts. When the colony has an `installation` rate and the design is a colony installation, a checked box enqueues a construction mount of that instance. The mount is paid later by [colony construction](colony-construction.md). With the box clear, or with no installation rate, the instance stays in cargo.

Until colony construction, a queued mount stays in `LocalConstructionDB.BuildQueue` and names the warehouse instance. `LocalConstructionProcessor` does not spend points on it and does not call `AddComponent`. A design job with no instance still mounts a free copy, so the Construction tab's current rows still finish. Cargo Install is hidden and `InstallComponentCommand` is rejected.

A refining completion still adds the material. Ordnance still completes on its own path.

## UI

On the Production tab, the checkbox says that it queues construction. It is disabled, with the reason in the tooltip, when the colony has no installation rate or the design is not a colony installation. The cost table stays the manufacturing cost, `IndustryPointCosts`.

The cargo list's Install item is hidden, so this slice cannot mount a finished instance from that menu. The Construction tab is unchanged until colony construction.

## Tests

- A factory job for a freightable installation adds one instance to general storage and does not mount it. A second completion is a second instance.
- The checkbox on a colony with an office enqueues one construction job and does not mount during the industry tick.
- A colony with no office leaves the instance in cargo.
- A refining batch still adds the material, not a component.

## Left out

The mount cost, assemble, the ship, the yard, and refit. Whether `IndustryAtb` is split into separate classes is not this plan.
