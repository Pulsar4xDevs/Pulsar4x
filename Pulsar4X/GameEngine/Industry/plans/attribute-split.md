# Industry folder, by attribute

Note from 2026-10-23, updated when the construction plans were written. Not a plan. Do not move files until asked. Do not change what a job spends from this note.

The construction rules are the plans in this folder: attribute, components, manufacture instances, colony construction, ship construction, yard holds the hull, ship refit, and stations. Fitting is not an attribute. `ConstructionAtb` is the name in those plans. The folder sort waits until those plans exist in code. Whether `IndustryAtb` becomes three classes is still open, and none of those plans decide it.

## Mounting

`InstallComponentInstanceOrder` and `UninstallComponentInstanceOrder` mount and unmount an instance. They are not an attribute. `OnComponentInstallation` is the callback every attribute runs when a part is mounted. The construction plans own the player-facing work.

## What the folder has

`IndustryAtb` is one attribute. `IndustryProcessor` calls `IndustryTools.ConstructStuff` for every installation that carries it. The rate dictionary on the installation picks the work. The types live in `GameData/basemod/TemplateFiles/industryTypes.json`.

| Installation | Attribute | Rates |
|---|---|---|
| Refinery | `IndustryAtb` | `refining` |
| Factory | `IndustryAtb` | `component-construction`, `installation-construction`, `ordnance-construction` |
| Ship yard | `IndustryAtb` | `component-construction`, `ship-assembly` |

There is no refine attribute, no factory attribute, and no shipyard attribute. "Construction" on the factory is those three rate keys. Construction Services is a different attribute, `ConstructionAtb`, and it is not the factory.

Attributes that already stand alone:

- `MineResourcesAtbDB` — the colony mine and the robo-miner. `MineResourcesProcessor`.
- `AsteroidMinerAtb` — the ship miner. Not the colony mine.
- `ConstructionAtb` — Construction Services. Points, then `AddComponent`. It does not take cargo.
- `InfrastructureCapacityAtb` — support capacity. Not a producer.

`LaunchComplexAtb` is the pad that puts a finished ship into orbit. It lives in `GameEngine/Ships/`, not in this folder. The shipyard does not use it.

Also in this folder, and not one of those attributes: minerals and deposits, `InfrastructureProcessor`, `LotOfferAction`, `InstallationsDB`, and the construction-queue orders.

## The sort

The intended buckets are mine, refine, factory, and shipyard. The code does not match that list. Mine is already its own attribute. Refine, factory, and shipyard are one attribute with different rate keys. Construction is the plans named above, not a fourth manufacturing class.

Do not sort the folder ahead of those plans. The class split stays undecided.
