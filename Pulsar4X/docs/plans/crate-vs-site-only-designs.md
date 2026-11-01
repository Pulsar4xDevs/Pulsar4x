# Crate-able vs site-only facility designs

Status: open. Carried over from the out-of-repo `colony-bootstrap-goal-plan.md` §8 and §5 when the plan files were cleaned up. How bootstrap works today is in [colony-bootstrap-and-shipments](../design/colony-bootstrap-and-shipments.md).

**Question.** Should each facility design be crate-able (`ShipCargo, PlanetInstallation`, built by a factory into a crate, hauled, then mounted; pays a weight premium for portability) or site-only (`PlanetInstallation` only; can be heavier and cheaper because it never travels; assembled on site from materials)?

## Done
- Construction Office is crate-able.
- A ship-hold check in `CargoMath` refuses site-only designs, with a test (commit `07022cc1`).
- The `spaceport` id clash is fixed (commit `78b76fbe`). The crate-able `planetary-spaceport-crate` loads.
- The Auto Mine (RoboMiner) is already crate-able, so the Mine needs no change.
- The prefab infrastructure designs (Earth, Mars, Luna, Ceres) are `ShipCargo, PlanetInstallation`.
- Site-only twins exist for factory, refinery, construction office, logistics office, warehouse and infrastructure (`*-site`, 2026-11-02 balance pass, tests in `PrefabFacilitiesTests`). That covers the site-only infrastructure design and the site-only Construction Office variant. Research Lab, Ship Yard and `spaceport` are site-only; the crate port is `planetary-spaceport-crate`. See [industry overview](../design/industry-overview.md).

## Not done
- **Design-time checkbox.** Each facility design gets a checkbox in the design UI that switches it between crate-able and site-only, stored on the design, not hard-coded per template. Site-only designs always queue construction ("queue construct on completion" checked and locked). Not found in the code (only `LocalConstructionJob.IsSiteOnly` reads the mount type).
- **Other gaps that can put site-only items into storage:** colony warehouses, factory jobs built without "install on completion" (`ComponentDesign.cs`), and direct uninstall commands in `CommandTranslator`. Only the ship-hold check is in. "Move to Storage" is only a UI guard.
- **Open question:** should City Hall / Administrative Complex (`admin-complex`), Launch Complex (`launch-complex`) and Naval Academy (`naval-academy`) also become crate-able?

## Open questions from the single-ship goal
- **Factory production at the source** when crates are not in stock (single-ship goal: no).
- **Time and size.** Check the list fits a 5000 m³ hold, and that the ship holds station and is not pulled off by `ShouldInterruptForRefuel` while the bay builds. The office crate's mass is unchecked. Office cost estimate ~1000 points (Level 5 × 200, derived).
- **Span.** Is a bridge-well plus rank 4+ captain the right requirement? The span check lives in the translator; there is no real `CanAccept`.
- **Personality.** No construction or colonising trait exists; the multiplier stays 1.
- **`GoalType.Colonise`** is a pruned-false stub with no planner. Recommendation: leave it for "found a new colony".
