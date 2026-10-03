# Plan 12 — Offer warehouse stock

Status: implemented on `TradeAndTransport`. Tests: `Pulsar4X.Tests/OfferStockTests.cs`, plus the Earth and Luna start checks. Do not fold this into plan 4.

## Outcome

A colony with a logistics office lists what is already in its warehouse. The job is assigned at the end of `ColonyFactory.CreateFromBlueprint`, after cargo is loaded, on every blueprint colony that has an office. That includes Earth and Luna Concord. The Market tab reads those listings. Luna is friendly, so the player sees the bars and cannot edit them.

## Goal

`GoalType.OfferStock`, appended after `SupplyLocal`. Planner `OfferStockPlan`. The goal name is "Offer stock". It stays out of `BaseWeights`. No skill term. No XP. `SkillDomains.DomainOf` is unchanged. The goal does not complete.

Each pass, for warehouse cargo with stock above 0 and no policy row: while `Rows.Count` is under `LogiBaseDB.Capacity`, add a row with Min 0, Max 0, AutoProduce false, Ask 0, Bid 0. Largest stock first, then cargo id. An existing row is left as it is, including one the player edited. A full office adds nothing further. The message is "{n} skipped for capacity" when any pile is left out. Then return `RunMarketPlan` actions for this colony. Do not `AssignGoal(RunMarket)` here.

Ask and bid stay 0 because cargo definitions have no price. Sell quantity is the stock above the reserve. With Min 0 and no industry need, the buy quantity is 0.

`CreateColony` does not assign this goal. A colony founded in play has no office yet.

## Later

A catalog price. Putting the standing job back after a supply order replaces it. Listing goods on a colony that gains an office after it is founded.
