# Plan 1 — Faction stance

Status: implemented on `TradeAndTransport`. Spec: `GameEngine/Logistics/market-and-stance.md` section 1. Tests: `Pulsar4X.Tests/FactionStanceTests.cs` (8 passed).

## Outcome

A faction stores how it treats each other faction. The snapshot reports that stance. Trade permission is a separate mutual check, used by later plans.

## Depends on

Nothing.

## Work

1. Add `FactionStance` (`Hostile`, `Neutral`, `Friendly`, `Allied`) and `Dictionary<int, FactionStance> Stances` on `FactionInfoDB`, keyed by the other faction's entity id. A missing key means Hostile.
2. Copy `Stances` in the `FactionInfoDB` copy constructor. Leave the rest of that clone as it is.
3. Add `FactionStanceRules.CanTrade`. Same faction id is allowed. `NeutralFactionId` (−99) is never a trade partner. Otherwise both stored stances must be Friendly or Allied.
4. Change `GameProjector.RelationOf`:
   - owner match → `Owned`
   - `NeutralFactionId` → `Neutral`
   - viewer's stance Friendly or Allied → `Friendly` (no Allied value on `OwnerRelation`)
   - viewer's stance Neutral → `Neutral`
   - missing or Hostile → `Hostile`
5. Add `SetFactionStanceCommand(TargetEntityId, OtherFactionId, Stance)` in `Pulsar4X.Api/Commands.cs`. Target is the faction entity. Translator writes only that faction's dictionary. Reject self, `NeutralFactionId`, and an unknown faction id.
6. Do not change `EntityFilter` or `EntityManager.GetFilteredEntities`. That Friendly flag means "owner id equals the querying faction".

## Tests

`Pulsar4X.Tests/FactionStanceTests.cs`

- Missing key is Hostile. One-sided Friendly projects as Friendly and `CanTrade` is false. Both sides Friendly: `CanTrade` is true.
- Allied projects as Friendly and trades once the other side is Friendly or Allied.
- Neutral faction id never trades.
- The command rejects self and the neutral id, and does not write the other faction's dictionary.

## Later

Treaties, wars, opinion, a stance UI, and any split between Allied and Friendly on the snapshot.
