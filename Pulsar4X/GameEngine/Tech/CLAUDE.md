# Research & Tech Subsystem — Developer Reference

**What it does:** Manages technology research. Colonies with research labs generate "research points" over time. Scientists assigned to a colony multiply those points and steer them toward specific technologies. When a tech accumulates enough points, it unlocks — changing formulas, enabling new components, raising caps.

Think of it like a nuclear power plant's R&D department: the labs are the staff producing analysis, the scientist is the department head multiplying their output through expertise, and the tech tree is the list of upgrades to pursue.

**Why it matters for ground combat:** Ground combat units are unlocked by tech. The tech tree includes a Ground Combat category. Scientist bonuses (which currently work for space systems) will apply to ground unit research as well — we get this for free.

---

## Files

| File | Role |
|------|------|
| `EntityResearchDB.cs` | DataBlob on a colony. Holds `Labs` — dict of `ComponentInstance` (a lab installation) → int (points assigned). This is the per-colony research state. |
| `ResearchProcessor.cs` | `IHotloopProcessor` running every **1 day**. Collects points from labs, applies scientist bonuses, distributes points to assigned projects, fires `ResearchCompleted` event. |
| `ResearchPointsAtbDB.cs` | Component attribute on a University installation. Holds `PointsPerEconTick` — how many research points this lab generates per processor tick. |
| `Scientist.cs` | A `TeamObject` subclass. Has `Bonuses` (dict of tech category ID → float multiplier), `MaxLabs` (how many lab slots they control), `AssignedLabs`, and `ProjectQueue` (ordered list of tech IDs to research). |
| `TeamObject.cs` | Base class for scientists and commanders. Holds `TeamType` and name. |
| `Tech.cs` | Represents one technology. Has `Level`, `ResearchProgress`, `ResearchCost` (computed from `CostFormula` via NCalc), `Category`, `Unlocks` list, `MaxLevel`. |

---

## How Research Works (DoResearch in ResearchProcessor)

Each day, for each entity with `EntityResearchDB`:
1. Collect all `ResearchPointsAtbDB` components on the entity (via `ComponentInstancesDB.TryGetComponentsByAttribute`). Sum their `PointsPerEconTick` → total raw points.
2. Find all `Scientist` objects from `TeamsHousedDB.TeamsByType[TeamTypes.Science]`.
3. Each scientist gets `AssignedLabs` worth of points. Their `Bonuses` dict multiplies points for their specialty tech categories.
4. Each scientist works through their `ProjectQueue` (ordered list of tech IDs). Points flow to the first researchable project.
5. When `ResearchProgress >= ResearchCost`, the tech is complete: level increments, new level's cost is recalculated, `ResearchCompleted` event fires.

**Scientist bonus example:** a scientist with `{"propulsion": 1.5}` makes every lab point toward propulsion tech worth 1.5 points.

---

## Tech Data Model

```csharp
Tech
  Level             int     — current level (0 = not started)
  MaxLevel          int     — caps at this
  ResearchProgress  int     — accumulated points toward next level
  ResearchCost      int     — computed: CostFormula(Level) via NCalc
  Category          string  — maps to a tech category (e.g. "ground-combat")
  Unlocks           list    — what this tech enables (component designs, formulas)
```

Costs are formulas (NCalc), not flat values — e.g., `"1000 * (Level + 1)^2"`. This means each successive level costs more. Same NCalc engine used by component design.

Tech data lives in `GameEngine/Data/basemod/blueprints/techs.json` and `techCategories.json`.

---

## Pulsar Status vs Aurora

| Aurora concept | Pulsar | Status |
|----------------|--------|--------|
| Research labs generate points | `ResearchPointsAtbDB` on University components | ✅ functional |
| Scientists multiply lab output | `Scientist.Bonuses` applied in `ResearchProcessor.DoResearch` | ✅ functional |
| Scientists assigned to projects | `ProjectQueue` on Scientist | ✅ functional |
| Multi-level technologies | `Tech.Level` / `MaxLevel`, cost recalculated per level | ✅ functional |
| Tech categories (9 in Aurora) | `Tech.Category` + `techCategories.json` | ✅ functional — check JSON for category list |
| Unlocks trigger new components | `Tech.Unlocks` list | ✅ functional |
| Scientist in-field 4× bonus (Aurora: field research bonus) | Not found | ⚠️ unknown |
| Prototype construction | Not found | ❌ unknown/missing |

**Verdict: research is substantially built.** Scientists work, bonuses apply, the NCalc formula system handles cost scaling. The main unknowns are the in-field scientist bonus and prototypes — neither are blocking ground combat.

---

## Phase 4 Relevance

A "Ground Combat" tech category must exist in `techCategories.json` for ground unit upgrades to unlock. Check before building ground unit designs — the category ID is the string key both `Tech.Category` and `Scientist.Bonuses` use. If the category doesn't exist yet, add it to the JSON file (no code change needed).

Ground unit weapons, armor, and sensor components will all be researchable exactly like ship components — same NCalc cost formula in the tech JSON, same unlock mechanism. We get this for free.
