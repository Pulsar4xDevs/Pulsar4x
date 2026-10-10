# Survey charts

Status: implemented. It is not logistics plan 13, and it is not part of the 1–12 sequence. Tests: `Pulsar4X.Tests/API/ApiIntelBookTests.cs` (6 passed). The client library built. The ImGui form was not clicked. Colony industry stays in `GameEngine/Industry/plans/`. Office purses stay in `office-purses.md`.

A finished survey is the first intel sold from a logistics office. The office stores a general intel list. This slice fills it with three survey kinds only. The cargo book is unchanged. Traders, freighters, `RunMarket`, and `OfferStock` do not read intel.

## Later intel, and what that changes

Other data should be able to use this list later: a contact, a design, a ruin, a price sheet. That changes the names and the subject key. It does not change the three survey grants, the ledger, who may buy, or the decision to keep cargo as it is.

What is generic in this slice:

- One dictionary on `LogiBaseDB`, `Intel`, not a survey-only dictionary and not a second book beside it.
- One row shape: `IntelKind`, `Subject`, `Ask`, `ForSale`.
- `Subject` is a string. These three kinds store the entity id in invariant decimal form. A later kind that is not an entity, such as a design id, uses its own string and does not need a new column.
- Three commands for every kind: set, clear, buy. A new kind does not add a command.
- One market section, titled Intel. Columns are Item, Result, Ask. The widget does not switch on kind. It prints the name and the result tag the projector already filled in.
- One switch in `IntelBook`, four questions per kind: the seller has it, the buyer has it, grant it, and list the owner's candidates with a public label. A new kind adds an enum value and those cases.

What stays specific, because the secrets are not the same shape:

- Geo, pin, and jump grants, including `JumpPointReveal.Grant`.
- The body-window line, the pin line, and the map mark. They look up rows whose kind is Geo or Pin and whose subject is that entity. A later kind appears in the Intel table and grows a hook only when it has a place to show. A jump has no map mark, and that rule stays.
- Spoiler text. The projector asks `IntelBook` for the public name and the result tag. Mineral numbers, gate coordinates, and a destination the buyer does not know are never copied onto the row.

What this slice does not build: a handler registry, a mod file of intel types, a courier item, or any kind besides Geo, Pin, and Jump.

A candidate may name a star system. These three kinds always do. A later faction-level secret can leave the system blank. The system filter shows blank-system rows only under All.

## Outcome

The player prices a finished survey on an owned colony's Market tab. A faction that can trade with them buys it from that office, or from the body or grav pin the chart describes. Payment is instant. The buyer receives the same facts a survey would have written. The seller keeps the facts. The same chart can be sold to every trade partner.

## Depends on

Plans 1, 2, and 5. Stance decides who may trade and who may see an office. The office is `LogiBaseDB`. The Market tab already exists. Survey completion already exists. This plan does not depend on traders, freighters, supply, or offer-stock.

## The three kinds

The key on one office is `(Kind, Subject)`.

| Kind | Subject string | What the seller already has | What the buyer receives |
|---|---|---|---|
| Geo | Body entity id | `GeoSurveyableDB.IsSurveyComplete(seller)` | Status set to complete, `MineralsDB.GrantFactionPartialAccess`, and `EventType.GeoSurveyCompleted` |
| Pin | `JPSurveyableDB` entity id | `JPSurveyableDB.IsSurveyComplete(seller)` | `SurveyPointsRemaining` set to 0, then `HideNeutralEntityFromFaction` for the buyer |
| Jump | `JumpPointDB` entity id | `JumpPointDB.IsDiscovered` contains the seller | The same reveal a successful grav roll writes, including the far side |

A grav survey does not store "this pin found this gate." `JPSurveyProcessor.RollToDiscoverJumpPoint` rolls against remaining gates and does not write the result back onto the pin. `JPSurveyableDB.JumpPointTo` is never assigned outside its copy constructor, so `GravSurveyView.JumpPointToSystemId` stays empty in a real game. The two records the faction actually holds are cleared pins and discovered gates. The list sells those two records. A pin row is tagged `pin cleared`. A jump row is tagged `jump`. The sale does not roll.

Geo always grants partial mineral access, the same ±20% amounts `GameProjector.ObscureWithError` already shows. `GrantFactionAccess` stays the colony-founding path. A homeworld the seller has fully surveyed still sells as a partial chart. `GrantFactionPartialAccess` leaves a faction that already has full access on full access.

The seller's copy is not removed. Buying a row the buyer already has is rejected. An unfinished subject cannot be listed.

## The office list

`LogiBaseDB` gains `Dictionary` `Intel`, keyed by kind and subject string. `Capacity` still counts cargo ids only. `MarketBook` and `MarketListing` stay as they are.

`BaseDataBlob` serializes with `MemberSerialization.OptIn`. The new dictionary needs `[JsonProperty]`. `Clone` copies each row. Cargo `Listings` are left as they are.

A row holds `Kind`, `Subject`, `Ask`, and `ForSale`. Ask is never written by `OfferStock` or `RunMarket`. A new game lists nothing. An empty chair does not publish intel.

One office may list a survey from any system the owner has finished, including a system other than the office's own. Each office has its own list and its own ask. Buying from Earth does not withdraw Luna's row. The second buy fails because the buyer already has the knowledge.

## Sale

`BuyIntelCommand` targets the colony that holds the office. The commanding faction is the buyer.

Reject when any of these is true:

- The colony has no logistics office, or the row is missing, or `ForSale` is false.
- `FactionStanceRules.CanTrade` is false. Same faction is rejected here. The owner uses set and clear, not buy. One-sided Friendly can see the office today and still cannot buy.
- Ask is negative, or ask is above `FactionInfoDB.Money.GetCurrentFunds()`. Ask 0 grants the intel and writes no ledger line.
- The buyer already has that geo completion, that pin completion, or that jump in `IsDiscovered`. The check is the kind's "buyer has it" case.
- The seller no longer has the underlying survey or discovery.

On success, apply that kind's grant, then the ledger. Cross-faction and ask above 0: buyer `AddExpense`, seller `AddIncome`, `TransactionCategory.Trade`. The description names the kind, the subject, and the other faction id. Same-faction ledger rules do not arise, because same-faction buy is rejected.

Jump grant goes through one helper, `JumpPointReveal.Grant`, used by the sale and by `JPSurveyProcessor` after a successful roll. The helper does what the roll's success path and `RevealOtherSide` do now: `IsDiscovered`, `RememberJumpPoint`, `ShowNeutralEntityToFaction`, far side, `KnownSystems`, and the existing `JumpPointDetected` and `NewSystemDiscovered` events. The roll, the chance, and `jpRemaining.First()` stay in the processor. A bought gate does not consume a pin and does not hide anybody else's pins.

Do not draw or project an undiscovered jump entity just because someone is selling it. The gate's position is the secret. The offer lives on the office view. After the grant, the normal `JumpPointView` projection shows the gate.

## Commands

All three target the colony. `IntelKind` lives in `Pulsar4X.Api` so the command and the view share it. This slice's values are Geo, Pin, and Jump.

- `SetIntelListingCommand(colonyId, subject, kind, ask, forSale)`. Requires an office, a non-empty subject, a non-negative ask, and a subject the owner has actually finished for that kind. Upserts the row.
- `ClearIntelListingCommand(colonyId, subject, kind)`. Drops the row. Missing row succeeds. Knowledge stays.
- `BuyIntelCommand(colonyId, subject, kind)`. The rules in Sale.

Register them in `CommandTranslator` next to `SetMarketListingCommand`. A seated administrator does not reject set, clear, or buy.

## Projection

New component view, projected from the same place as `ToMarketView`: the entity has `LogiBaseDB`, and `RelationOf` is Owned or Friendly (Allied already projects as Friendly). Hostile and diplomatic Neutral get no intel view. `MarketView` stays the cargo book so existing market tests keep their shape.

`IntelBookView` on the office:

- `CanEdit` true only for the owner.
- `Rows`: rows with `ForSale` true, plus, for the owner, rows they have saved with `ForSale` false. Each row has kind, subject, ask, for-sale, a result tag (`partial minerals`, `pin cleared`, or `jump`), the buyer's already-owned flag, a display name, and the system id when the secret belongs to a system.
- The display name for a geo or a pin is the subject's name. For a jump the viewer does not yet know, the name is `Jump` plus the system the seller's gate sits in, with no destination and no coordinates. Two unknown gates in Sol are `Jump · Sol` and `Jump · Sol 2`. The owner, and a buyer who already discovered that gate, get the real name and may include the destination system, because they already have it.
- `Candidates`, owner only: every finished geo, cleared pin, and discovered jump the faction holds, with kind, subject, system id, and display name. The friendly view gets an empty candidate list. Sol's asteroid belt makes this list long. The widget filters it. The snapshot still carries it, because the client has no separate query.

`CanTrade` is not the visibility check. It is enforced on buy. A one-sided Friendly office still shows its for-sale rows, and Buy comes back rejected.

## What the player sees

Market tab of Manage Colonies, under the cargo bars. The section title is Intel. The `Listings n / capacity` line is unchanged.

The table is the office's rows for the selected system filter. Columns are Item, Result, Ask. The owner gets an ask field, Set, a For sale toggle, and Remove. The filter defaults to the office's star system. A second choice, All, shows every candidate. Unlisted finished surveys are an Add combo of candidates in the current filter that are not already on this office, the same shape as the cargo Add row. Friendly offices show for-sale rows only, with Buy, or Owned when the viewer already has that secret. Mineral numbers are not on the row. Buying a geo row is what puts the ±20% figures on the body.

On a body window, when this faction's geo survey is not complete and some visible friendly office lists that body for sale, one line sits with the survey state: seller name, `geo chart`, ask, and Buy. Asteroids and comets use the same line. The mineral table appears after the buy, from the existing partial-access projection.

On a grav pin the faction has not finished, the same line reads `pin cleared` when a visible office lists that pin. The system map keeps the pin icon and adds a small mark. The map tooltip shows the same one-line offer. No mark is drawn for a jump. The jump is bought from the Intel table.

No toolbar button. No Fleet Management order. No change to survey-ship goals.

## Later

A courier item in a hold. A bid or a contract that pays someone to fly a survey that does not exist yet. An administrator who prices intel. A system bundle. A seller toggle that prints the destination on a jump tag. A friendly faction that buys a listed row on its own when the ask is under a budget. Any intel kind besides Geo, Pin, and Jump. None of these are in this plan. A new kind, when it is asked for, extends `IntelKind` and the four cases in `IntelBook`.

## Files to add

- `Pulsar4X/GameEngine/Logistics/IntelBook.cs` — `IntelListing`, and `IntelBook` with `TryGet`, `SetListing`, `RemoveListing`, `Buy`, and the candidate plus public-label query the projector uses. `Buy` checks the shared rules, switches on kind for the grant, calls `JumpPointReveal.Grant` for a jump, and writes the ledger.
- `Pulsar4X/GameEngine/JumpPoints/JumpPointReveal.cs` — `JumpPointReveal.Grant(Game, factionId, jumpEntity, at)`. The sale and the processor both call it.
- `Pulsar4X/Pulsar4X.Client/Interface/Displays/IntelDisplay.cs` — the Intel table, the system filter, the Add combo, Set, Remove, and Buy. Submits the three commands through `GlobalUIState.GameClient.SubmitCommandAsync`. No kind switch in the widget.
- `Pulsar4X/Pulsar4X.Tests/API/ApiIntelBookTests.cs` — fixture style of `ApiMarketViewTests`.

## Files to change

- `Pulsar4X/GameEngine/Logistics/LogiBaseDB.cs` — `Intel` dictionary, `[JsonProperty]`, copy in the clone constructor.
- `Pulsar4X/Pulsar4X.Api/Commands.cs` — `IntelKind`, `SetIntelListingCommand`, `ClearIntelListingCommand`, `BuyIntelCommand`.
- `Pulsar4X/Pulsar4X.Api/Snapshots.cs` — `IntelBookView`, `IntelRowView`, `IntelCandidateView`.
- `Pulsar4X/GameEngine/Api/GameProjector.cs` — project `IntelBookView` beside `ToMarketView`. Register it in the view list near line 226. Names and tags come from `IntelBook`.
- `Pulsar4X/GameEngine/Api/CommandTranslator.cs` — translate the three commands next to `TranslateSetMarketListing`.
- `Pulsar4X/GameEngine/Api/EngineGameServer.cs` — a buy whose target is a logistics office the session can trade with is accepted. The commanded entity and the subject entity are refreshed.
- `Pulsar4X/GameEngine/JumpPoints/JPSurveyProcessor.cs` — the successful roll calls `JumpPointReveal.Grant` instead of inlining `IsDiscovered`, `RememberJumpPoint`, `ShowNeutralEntityToFaction`, and `RevealOtherSide`. Roll math stays.
- `Pulsar4X/Pulsar4X.Client/Interface/Windows/ColonyManagementWindow.cs` — Market tab calls `IntelDisplay` after `MarketBarsDisplay.Display`.
- `Pulsar4X/Pulsar4X.Client/Interface/Windows/EntityWindow.cs` — both market headers (`Display` around the colony's own `MarketView`, and the per-colony header on a body) also call `IntelDisplay`. The geo offer line is in `DisplaySystemBodyContent` and `DisplaySmallBodyContent`, shown when `GeoSurveyView.IsSurveyComplete` is false. The pin offer line is in `DisplaySurveyInfo`.
- `Pulsar4X/Pulsar4X.Client/Interface/Displays.cs` — `SystemBody` and `GravitationalAnomlay` grow the same one-line offer. `GravitationalAnomlay` needs the entity snapshot, not only `GravSurveyView`, so it can see office intel rows.
- `Pulsar4X/Pulsar4X.Client/Rendering/Labels/EntityLabelExtCombo.cs` — the pin tooltip passes the entity into `GravitationalAnomlay`.
- `Pulsar4X/Pulsar4X.Client/Rendering/SystemMapRendering.cs` and `Pulsar4X/Pulsar4X.Client/Rendering/Icons/PointOfInterestIcon.cs` — a mark on a pin that a visible office lists. No mark on a jump.

## Files to leave alone

`MarketBook.cs`, `MarketListing`, `MarketView`, `MarketBarsDisplay` cargo rows, `RunMarketPlan`, `OfferStockPlan`, `TradePlan`, `FreighterPlan`, `FleetParcel`, `SupplyLocalPlan`, `MarketExchangeAction`, `GeoSurveyProcessor`, cargo JSON, `FleetWindow`, `office-purses.md`, and `colony-administrator.md`.

## Tests

`ApiIntelBookTests`, on `ApiTestBase`. Cover at least:

- An owned office projects a geo row the player set, and a friendly viewer sees it with `CanEdit` false. A hostile viewer gets no `IntelBookView`.
- Set rejects a negative ask, an empty subject, a missing office, and a body the owner has not finished. Clear of a missing row succeeds.
- Buy of a geo row sets the buyer's survey complete, grants partial mineral access, leaves the seller's access in place, and does not grant full access. The ledger moves by the ask. Ask 0 writes no ledger line. A second buy is rejected. Insufficient funds is rejected. One-sided Friendly is rejected. Same faction is rejected.
- Buy of a pin row completes and hides that pin for the buyer. No jump is revealed and no roll is taken.
- Buy of a jump row adds `IsDiscovered`, remembers the point, reveals the far side and `KnownSystems` when `DestinationId` is set, and does not set any pin's survey to complete.
- Cargo `Capacity` and `MarketView.Goods` are unchanged by intel rows.
- `OfferStock` and a `RunMarket` wake do not add intel rows.

Run from outside the repo, for example `/tmp`, with `DOTNET_ROLL_FORWARD=LatestMajor`, project `Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj`, filter `FullyQualifiedName~ApiIntelBookTests`. `global.json` pins SDK 8.0.122.

The client library should build. The ImGui form is not clicked by the test run. That check is the same limit as plans 5b, 10, and 11.
