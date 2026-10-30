# Uphill warp paid as bubble sustain

Status: ready to implement. Written 2026-10-31. Do not start until plan 1 is playing. Do not start plan 3 from this session.

The velocity paste rewrites the ship's sun-frame energy and the reactor never sees it. Bubble creation is a battery spike. Sustain is a flat 15 kW that matches the stock 2 tonne reactor exactly, so a cruise has no surplus. This plan puts the climb on that sustain draw and lets the reactor limit warp speed.

One bubble, one speed, chosen when the bubble opens. The speed does not change along the leg. `WarpMath.GetInterceptPosition` keeps a constant speed, and a chord that passes through the star is not repriced.

## Decided

- Same `WarpAbilityDB`, same `WarpMoveProcessor`. No second datablob and no second processor.
- Coupling and bleed are two numbers on that blob, baked from the drive the way sustain already is. A zero coupling is today's flat sustain. A zero bleed is no downhill cap. An old save is missing both fields, so both stay zero, and that warp flies as it does today. No migration.
- Charge the greater of the existing overhead and the coupled climb, not the sum. The stock plant is already spent on the overhead. Adding the climb on top would drain the batteries on the first uphill minute.
- The climb is `coupling × mass × solar gravity × the outward component of warp speed`, in watts, booked as kilowatts. Gravity is the star at the ship's position when the bubble opens. Power is high near the Sun and lower out by Mars. Do not divide one fixed energy total by the trip time.
- Level 0 coupling is `0.0025`. A 40 tonne ship with a 15 kW plant, headed straight out from 1 AU, warps at about 25.3 km/s. The drive rating on that hull is 50 km/s. The same ship leaving Mars's orbit is back on the drive rating, because the Sun's pull is weaker. An Earth-to-Mars leg stays on the order of the current trip: longer than today, under three times today, not decades.
- The whole velocity vector is scaled. Cutting only the radial component would bend the straight-line intercept. A heading with no outward component pays no climb. An inward heading is a fall.
- Downhill pays nothing back. Sustain on a fall stays the flat overhead. Bleed is a fraction of drive speed. Level 0 is one half. Level 10 is the drive rating. Later bleed can be faster than the climb-limited speed, and it cannot pass the drive.
- The captain does not pick a speed or a path. The order is still "warp to Mars." The running order line says which limit applies.
- Reactor fuel is not the limit. The stock plant's fuel lasts tens of thousands of years because the burn divisor is `40e9`. Scaling the plant scales the fuel with it. Do not change `EnergyGenProcessor`.
- A running warp is not charged a second creation cost on load. The climb demand is stored on the bubble and is already inside the saved `EnergyGenAbilityDB.Demand`. Load does not add it again.
- This plan does not change the saved vector. It stays the parent-relative paste. Plan 3 still needs that frame written down before anyone builds the brake.

## Numbers

Stock 2 tonne steam reactor, 50% split, generator efficiency 0.8: 15 kW. Stock 2 tonne drive at tech level 0: engine power `2e6`, sustain 15 kW. Creation spike `4e6 / 3` kJ, two thirds of two 2 tonne batteries (`Mass × 500` kJ at battery tech 0).

Warp speed is `(engine power / mass in kg) × 1000` and is used as m/s. `WarpMath.MaxSpeedCalc` is that number. A 40 tonne hull with a 2 tonne drive is 50 km/s. An AU at that speed is on the order of a month.

`G` is `UniversalConstants.Science.GravitationalConstant` (`6.67430e-11`). `BasicSol` is `1.989e30` kg. At 1 AU (`149597870700` m) solar gravity is about `0.005932` m/s².

Pure radial, coupling `0.0025`, mass `40000` kg, plant `15` kW:

`speed = 15000 / (0.0025 × 40000 × g) ≈ 25287` m/s.

At 1.524 AU the same plant allows about 58 km/s, so the 50 km/s drive wins.

Raw joules do not fit. The Earth–Mars orbital-energy gap spread over a month is about 2 MW, roughly 130 times the stock plant. Holding 50 km/s straight out from Earth against solar gravity is about 12 MW. The coupling exists so ships do not pay that.

## What to build

### 1. The two numbers on the drive

In `GameData/basemod/TemplateFiles/techs.json`, next to `tech-alcubierre-sustain-cost`:

- `tech-alcubierre-gravitational-coupling`. Name "Alcubierre Gravitational Coupling". Category `tech-category-power-propulsion`. MaxLevel 10. `DataFormula` `1 - [Level] * 0.08`. Cost formula the same shape as the sustain tech, `500 + [Level] * 500`. Level 0 is 1. Level 10 is 0.2. The bill shrinks. It does not reach zero. Zero on the blob stays the "no climb" switch for an old save and for a test.
- `tech-alcubierre-bleed`. Name "Alcubierre Bleed". Same category, max, and cost. `DataFormula` `0.5 + [Level] * 0.05`. Level 0 is 0.5. Level 10 is 1.

Add both ids to the `tech-modern-technology` unlock list, beside the other alcubierre ids. Earth's `StartingItems` increments modern technology to level 1, which unlocks these at level 0. A blueprint faction increments every tech once after unlocking all of them, so its coupling tech is level 1 (`0.92 × 0.0025`). That is the same one-step head start sustain already gets. Leave it.

On the alcubierre template in `engines.json`, two `GuiTextDisplay` properties, and pass them at the end of the existing `AtbConstrArgs`:

- "Gravitational Coupling", formula `0.0025 * TechData('tech-alcubierre-gravitational-coupling')`.
- "Bleed Fraction", formula `TechData('tech-alcubierre-bleed')`.

`WarpDriveAtb` keeps the five-argument constructor. Add a seven-argument constructor: the same five, then coupling, then bleed. `OnComponentInstallation` writes them onto `WarpAbilityDB`.

Two drives do not sum these. Power, creation, sustain, and collapse still sum. Coupling on the ship is the greater of the two (a zero is the best drive, so a real coupling wins). Bleed is the lesser of the positive fractions. A zero bleed is ignored while the other drive has a fraction. If every drive is zero, the ship bleed stays zero.

`WarpAbilityDB` gains `Coupling` and `BleedFraction`, copied in the copy constructor. `AbilityDescription` appends both numbers. `CalcMaxWarpAndEnergyUsage` is unchanged: `MaxSpeed` stays the drive rating.

`BalanceBaselineTests.WarpPower_IsADayOnTheFreighterBatteries` unlocks a fixed tech list and then designs this drive. `TechData` throws if the id is missing. Add the two new ids to that list. Assert coupling `0.0025` and bleed `0.5`. The existing power, creation, and 15 kW asserts stay.

### 2. One speed when the bubble opens

Add `WarpMath.PriceCruise`. `TryStartWarp` and `EvaluateWarp` both call it. No other caller of `GetInterceptPosition` changes. The plot window still draws `WarpAbilityView.MaxSpeed`. The bubble replaces that intercept when it opens.

Inputs: the ship, the target, the time, and the exit offset `TryStartWarp` already passes.

Steps:

1. Star is `PositionDB.Root` of the ship. Star mass is that entity's `MassVolumeDB.MassDry`. Ship mass is `MassVolumeDB.MassTotal`, the same mass `MaxSpeedCalc` uses. Plant output is `EnergyGenAbilityDB.TotalOutputMax`, in kW. Drive speed is `WarpAbilityDB.MaxSpeed`.
2. First intercept at drive speed, so there is a heading. Direction is that exit minus the ship's absolute position.
3. Price that heading. Second intercept at the priced speed. Price the new heading once more. If the speed changed, one last intercept at the second speed, then stop. Two heading checks, not a search.
4. The speed that flies is the second price. `PredictedExitTime` and `ExitPointAbsolute` come from the intercept that used that speed. `CurrentNonNewtonionVectorMS` is the normal from entry to that exit, times that speed.

Pricing one heading:

- `r` is the distance from the ship to the star, and at least the star's radius, and at least 1 m.
- `g = μ / r²` with `μ = GeneralMath.StandardGravitationalParameter(starMass)`. Ship mass is not added.
- `radial` is the dot of the heading and the outward unit vector (ship minus star). Positive is outward.
- No star, non-positive ship mass, non-positive drive speed, or non-finite `g`: cruise is the drive speed, demand is `BubbleSustainCost`, and the limit line is empty.
- `radial > 0` and `Coupling > 0` is a climb. `plantSpeed = outputKW × 1000 / (coupling × mass × g × radial)`. A non-positive or non-finite plant speed falls back to the drive speed and the overhead, with an empty limit line. That is a dead plant or a bad `r`. A small positive plant still flies, slowly.
- Climb cruise is `min(driveSpeed, plantSpeed)`. Outward speed is `cruise × radial`. `climbKW = coupling × mass × g × outward / 1000`. Demand is `max(BubbleSustainCost, climbKW)`.
- `radial <= 0` is a fall. Climb kilowatts are zero. Demand is `BubbleSustainCost`. A positive bleed fraction caps cruise at `driveSpeed × BleedFraction`. A zero bleed leaves the drive speed.
- A tangential heading is a climb with a tiny outward component, so the plant allows more than the drive, and the drive wins. Bleed does not apply to a tangent.

`TryStartWarp` adds `demandKW` where it now adds `BubbleSustainCost`, after the creation spike, and only when the bubble actually starts. Store that demand on `WarpMovingDB` as `SustainDemandKW`, and set `ChargedSustain`. Finish and `PayCollapse` remove `ChargedSustain ? SustainDemandKW : BubbleSustainCost`. An old save has `ChargedSustain` false and still removes `BubbleSustainCost`.

Copy both fields in the `WarpMovingDB` copy constructor.

The limit line is stored on the bubble as `LimitLine`. Empty means the order stays "Warping". Speeds go through `Stringify.Velocity`, which prints `Km/s`.

- Climb, plant slower than the drive: `Drive 50 Km/s. Plant holds 25.29 Km/s uphill. Warp at 25.29 Km/s.`
- Climb, drive wins: `Drive 50 Km/s. Uphill, plant can hold it. Warp at 50 Km/s.`
- Fall, bleed slower than the drive: `Downhill, bleed limited to 25 Km/s.`
- Fall, drive wins: `Downhill, at drive speed.`

`WarpMoveAction.UpdateDetailString`, while the bubble is up and not yet at the target, uses `LimitLine` when it is not empty. Charging and arrival text stay as they are.

`EvaluateWarp` quotes the arrival from `PriceCruise`'s intercept. The message stays `Warp to <name>, <date>`. A quoted date that used drive speed would be the wrong month.

Gravity is the entry well only. A leg that starts inward is a fall for the whole bubble, including a line that later passes the star and climbs out the other side. The next bubble is priced again at its own entry. Do not sample the closest point on the chord. An opposition Earth–Mars chord passes the star, and that gravity is unbounded.

### 3. What plan 3 can read

Each extra bubble costs one creation spike plus `SustainDemandKW` for as long as that bubble is up. The demand is on the bubble. Plan 3 does not start from this session, and this plan does not pick the saved-vector frame.

## Tests

Add `Pulsar4X.Tests/WarpSustainTests.cs`. Follow `WarpMoveTests` (`TestingUtilities.BasicSol`, `TMPDIR=/home/se5a/.cache/build-tmp`, `dotnet` from outside the repo). Set `Coupling`, `BleedFraction`, `MaxSpeed`, `BubbleSustainCost`, and `EnergyGenAbilityDB.MaxOutputFromReactor` on the fixture. The designer test is the one that pins the JSON.

- Pure radial out, 1 AU from `BasicSol`, mass 40000 kg, drive 50000 m/s, sustain 15 kW, output 15 kW, coupling 0.0025. Cruise is within 1% of 25287 m/s. Demand is 15, not 30. The limit line is `Drive` + `Stringify.Velocity(50000)` + `. Plant holds ` + `Stringify.Velocity(cruise)` + ` uphill. Warp at ` + `Stringify.Velocity(cruise)` + `.`
- Same ship at 1.524 AU, pure radial out. Cruise equals the drive speed. The line contains `plant can hold it`.
- Same 1 AU case, output 7.5 kW. Cruise is within 1% of half of 25287 m/s, and still under the drive speed.
- Same 1 AU case, mass 80000 kg, drive speed 25000 m/s. Cruise is within 1% of half of 25287 m/s.
- Heading inward, bleed 0.5, drive 50000 m/s. Cruise is 25000. The line is `Downhill, bleed limited to 25 Km/s.` Demand equals the overhead.
- Heading inward, bleed 0. Cruise equals the drive speed. The limit line is empty.
- Coupling 0, heading outward. Cruise equals the drive speed. Demand equals the overhead. The limit line is empty.
- Two `WarpDriveAtb` installs on one entity. The entity needs `ComponentInstancesDB` and `MassVolumeDB`, because `OnComponentInstallation` calls `CalcMaxWarpAndEnergyUsage`. Couplings 0.0025 and 0.001, bleeds 0.5 and 0.8. The blob coupling is 0.0025 and the bleed is 0.5.
- Earth and Mars on the same side of `BasicSol` (Mars at 1.524 AU, same direction as Earth). Ship as in the first case, batteries large enough to open. After the bubble starts, cruise is under the drive speed, `PredictedExitTime` is later than the same warp with coupling 0 and earlier than three times that warp. `Demand` is 15. An hour of `WarpMoveProcessor` does not drop stored energy by a stacked 15 kW.
- That same bubble, save and load. Stored energy is unchanged. `Demand` is unchanged. Stepping to arrival removes `SustainDemandKW` once and does not charge creation again. `RunningWarp_SurvivesSaveAndArrivesOnce` stays as it is: sustain 0, coupling 0.

Run `FullyQualifiedName~WarpSustainTests|FullyQualifiedName~WarpMoveTests|FullyQualifiedName~BalanceBaselineTests`.

## Out

A parallel warp processor. Paying the literal joules. Crediting the batteries for a fall. Changing `SavedNewtonionVector`, `UseRelativeVelocity`, or the capture ellipse. Deleting `CreateCommandEZ`. A speed that changes every hotloop tick. Pricing the chord's closest approach to the star. The plot window and `WarpAbilityView`. Reactor fuel. `EnergyGenProcessor`. The pause checkbox. Plan 3.

## Stop if

The pure-radial 1 AU case is not within 1% of 25287 m/s. The constant or the gravity is wrong. Fix `PriceCruise` before `TryStartWarp` uses it. A bubble that flies a different speed from the order line will tell the player one limit and keep another.

A game restart is required. This adds fields and a constructor. It will not apply with Edit and Continue.
