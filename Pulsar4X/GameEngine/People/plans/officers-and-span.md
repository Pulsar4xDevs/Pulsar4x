# Officers, the academy, and command span

Status: done.

This is the ship half of near-term item 19 and the definition pass of item 15. Scientists, colony administrators, personality, bids, and the Luna civilian stay in `GameEngine/Engine/Orders/colony-administrator.md`. Crew headcount and payroll stay item 20.

## What the player does

Earth can design and assemble a Naval Academy on the first day. The template is unlocked and a stock design exists. Earth does not start with one mounted. It stays site-only (`PlanetInstallation`, no `ShipCargo`). The construction office assembles it.

Mounting the building does not start a class. Colony Management's existing Naval Academy tab appears once one is mounted. The tab shows each mounted academy as idle, or the course it is running and the date it finishes. Two mounted academies are two rows. Each runs one course.

From an idle academy the player orders one of two courses. Both take the design's class length.

* **Train a class.** Graduates that many rank-1 ensigns. They are unassigned. The colony must have 1,000 people present per ensign. The course removes one person per ensign when it starts. The button names the population it needs and the population the colony has.
* **Command course.** One unassigned officer who already has the experience. They stay off the ships until the date. The course either raises their experience cap to this school's mean, or promotes them one span step. It never does both, and banked experience never skips a step.

Cancelling a class returns the people to the colony. Cancelling a command course leaves the officer unassigned and unchanged. A busy academy rejects a second course.

Fleet Management's Orders tab groups goals into three bands. A band the ship cannot use stays visible and grey. The reason names the bridge, the rank, or both. The line under the captain reads the result, for example "Body. This bridge can cover a system. Ensign Kim covers this ship." There is one span on that line. The logistics span label goes away.

A ship with no officer still has no goals. The player plots actions, as today.

## One span

`CommandSpan.Of` stays the hardware reading of the bridge. Ship through Fleet are Body. Colony, Planet, and SOI are Well. System and wider are System. Do not give the enum value `Fleet` a new meaning.

The span the officer may use:

| Rank | Span |
|---|---|
| 1–3, Ensign through Lieutenant | Body |
| 4–5, Lieutenant Commander and Commander | Well |
| 6 and above, Captain and above | System |

Effective span is the narrower of the hardware and the rank. No seated officer means Body, whatever the bridge is. A fleet uses the flagship's bridge and the flagship captain's rank. The fleet commander is still that captain. A second chair is not this plan.

Survey fans out with the effective span, unchanged: Body is the anchor, Well is the anchor and its children, System is the anchor and every descendant.

A haul is two named ends. The freight reach is one step wider than that same effective span, and it is a sentence on the band, not a second span:

| Effective span | Survey | Haul, trade, move goods |
|---|---|---|
| Body | That body. | Two bodies in its well. Earth and Luna. Not Earth and Mars. |
| Well | That body and its moons. | Any two colonies in this star system. Earth and Ceres. |
| System | The star and everything under it. | One end may sit in a system one known jump away. |

`LogisticsSpan.Of` becomes that step, applied to the effective span. Delete the window header that prints it. `Neighbor` remains the freight reading of System. It is not a fourth band and not a rank.

An asteroid survey keeps its neighbourhood around the rock. That is the order, not a span.

Day one does not move. A rank-6 captain on a ship bridge is Body: Planet Express still hauls Earth–Luna and still cannot haul Earth–Ceres. Surveyor I and the miner mount system bridges, so their rank-6 captains still cover the system. An ensign on Surveyor I surveys one body and hauls inside the well.

## Ship bridge blueprint

The ship bridge template (`ship-command` in `GameData/basemod/TemplateFiles/storage.json`) offers Admin Level, an enum from Ship through System. Several of those values are the same span. Fleet is still Body. Colony is Well. The player cannot tell what they are buying.

Replace that control with Command Span, three choices: Body, Well, System. Console space stays the other tweakable and still drives mass. A Body choice stores admin level 0, Well stores 6, and System stores 8, which is what `CommandSpan.FromAdminLevel` already maps. The three stock designs keep their current reach: `default-design-bridge-ship`, `default-design-bridge-well`, and `default-design-bridge-system`. Do not offer Task Unit, Fleet, Colony, Sector, or Empire on a ship bridge. Leave the colony administrative complex on its own admin-level control.

## Orders tab

Each order is listed once, under the lowest band that can issue it. A wider form of the same order is a row in the higher band, grey when the effective span is below that band.

**This ship.** Any seated officer. Move to, refuel, jump, trade, haul, move goods, mine this ship's rocks, geo or grav of one body. The plot actions stay in a fold under this band. An empty chair shows only this fold.

**This well.** Geo or grav that includes the moons. A haul, trade, or move-goods pair anywhere in the system.

**This system.** Clicking the star. A haul pair with one end one jump away.

Selecting a ship uses that ship's effective span. The target tree must stop forcing Body for every ship. Selecting a fleet uses the flagship. Grey a colony or a body the band does not cover, and say why.

Move goods, haul contract, trade, and a fleet haul all use the pair test. A ship haul lists the legal pairs. It does not offer one button that says the captain will pick. Manual warp and newton plots are not span-gated.

## Academy rules

The stock design is class size 2, class length 12 months. The designer allows class size 1 to 8, step 1, and length 6 to 48 months, step 1.

Retune the building so a class of 2 is not a shed. Mass is `80000 + ClassSize * 5000`. Crew requirement is `40 + ClassSize`. Length does not change the mass. The class-size description says "per course you order," not "each year."

A new ensign is rank 1, unassigned, with `CommissionedOn` and `RankedOn` set to the graduation date. The experience cap is a bell curve around `100 + (months - 24) + instruction`, clamped 0 to 300. The old upper clamp of 200 hides a long course. Starting experience stays the current 0–30 roll when the cap is above 30. Show the mean cap on the design slider.

The command course looks at total experience. Nav, Survey, and Command all count. Do not spend it. Do not require Command experience. A ship captain cannot earn Command until they are already the flagship.

* Cap below this school's mean: the course sets the cap to that mean. Rank stays. This is how a field officer becomes eligible to promote. The mean is the formula above, not a new random roll.
* Cap at or above the mean, and experience at least 8, and rank below 4: rank becomes 4. `RankedOn` is the graduation date. Publish `OfficerPromoted`.
* Same, rank 4 or 5, and experience at least 16: rank becomes 6.
* Otherwise the course is refused, and the button says which number is short.

One step per course. An ensign with 16 experience promotes to rank 4, not to captain.

The officer must be unassigned. An enrolled officer cannot be given a ship and cannot be enrolled again. Assigning a captain does not check rank. The chair accepts an ensign. The span clips.

Quality is `1 + skill / 100`. Grants stop at `ExperienceCap`, or at 100 when the cap is 0. A cap of 40 filled to 40 is quality 1.4. A cap of 100 filled to 100 stays quality 2, so the existing skill tests keep their numbers. A long course can pass 2.0. Skill still never fails a move, a survey, or a burn.

Population is the sum of `ColonyInfoDB.Population`. The people removed come out of the largest species group. A colony short of `class size * threshold` refuses the class. The threshold starts at 1,000.

## Research

The building has no tech gate. Three techs sit in `tech-category-construction-production`, next to the shipyard slip tech. They are not in Earth's `StartingItems`.

* **Larger classes.** Each level adds 4 to the designer maximum. Three levels, so the maximum becomes 20.
* **Better instruction.** Each level adds 12 to the cap mean. Three levels.
* **Candidate search.** Threshold is `max(100, 1000 - 250 * level)`. Four levels. The course still removes one person per ensign.

The class-size `MaxFormula` reads the first tech. The processor reads the other two off the owning faction.

## Data and code

* `GameData/basemod/TemplateFiles/storage.json` — `ship-command` Command Span control, three choices, stored as admin levels 0, 6, and 8.
* `GameData/basemod/ScenarioFiles/designs/componentDesigns.json` — the three stock bridges keep Body, Well, and System. `default-design-naval-academy`, class size 2, length 12.
* `GameData/basemod/TemplateFiles/installations.json` — class size, length, mass, crew, description. The install attribute must not schedule a graduation.
* `GameData/basemod/TemplateFiles/techs.json` — the three techs.
* `GameData/basemod/ScenarioFiles/systems/sol/earth.json` — add `naval-academy` to `StartingItems`, and the design to `ComponentDesigns`. Do not add it to `Installations`.
* `GameEngine/People/NavalAcademyAtb.cs`, `NavalAcademyDB.cs`, `NavalAcademyProcessor.cs` — idle until an order. One record per mounted component. A course stores whether it is a class or a named officer, the graduation date, and the population it took. The processor graduates that course and does not schedule another.
* A train order and a command-course order, plus cancel. API commands follow `AssignCaptainCommand`: translator, snapshot, client. The colony is the commanded entity. The order names which mounted academy.
* `GameEngine/People/CommanderSkills.cs` — quality scale 100, grants stop at the cap.
* `GameEngine/Engine/Orders/CommandSpan.cs` and `GameEngine/Logistics/LogisticsSpan.cs` — effective span, then the freight step. `ContractReaches` and the fleet parcel use that.
* `Pulsar4X.Client/Interface/Windows/FleetWindow.cs` — three bands, one span line, ship tree uses the ship's span.
* `Pulsar4X.Client/Interface/Windows/ColonyManagementWindow.cs` — fill the Naval Academy tab. Keep the tab hidden until an academy is mounted.
* `GameEngine/Engine/Orders/agents-and-goals-design.md` — decision 3. Officers are scarce. Rank clips span. Skill stays quality and never fails a plan.
* `near-term.md` — when the work is done, mark item 15 done and rewrite item 19 so the officer pipeline is done. Leave the scientist and administrator sentences open.

JSON edits are basemod only.

## Tests

Run from outside the repo, with `TMPDIR=/home/se5a/.cache/build-tmp` and `DOTNET_ROLL_FORWARD=LatestMajor`, filtering `Pulsar4X.Tests`.

* Mounting an academy schedules no graduation and creates no officers.
* A class on a colony of 2,000 people and a class size of 2 removes 2 people, and the graduation produces 2 unassigned rank-1 officers. A colony of 1,999 refuses. Cancel returns the people and leaves no course.
* A second course on a busy academy is refused.
* A command course on a seated officer is refused. An unassigned ensign with experience 8 and a cap at the school mean becomes rank 4, keeps the experience, and can be assigned again. A second course with experience 16 becomes rank 6. Experience 16 on the first course still stops at rank 4.
* An unassigned officer with cap 40 and experience 8 keeps rank 1 and leaves with the school's mean cap. The next course can promote them.
* Quality at skill 40 with cap 40 is 1.4. A grant past the cap does not stick. Skill 100 with cap 100 is still quality 2.
* Ship bridge and rank 6: survey span Body, Earth–Luna haul accepted, Earth–Mars haul rejected. System bridge and rank 1: same clip. System bridge and rank 6: system survey, and a haul with one end one jump away accepted. Well bridge and rank 4: well survey, in-system haul accepted.
* An empty chair rejects a move-goods goal, as today. A plotted move is not span-checked.
* A new game unlocks the template and the stock design. Earth's colony has no academy record.
* The ship bridge designer offers Body, Well, and System. The stock ship bridge is still Body, the planetary bridge is still Well, and the system bridge is still System.
* Update `ApiFleetTests` so an empty fleet still reads as one span of Body. Its freight reach may stay the well. The test must not require the window to show a second label.

## Later, not this plan

* A fleet bridge as a second chair. The flagship captain holds both jobs until a second officer is assigned. Extra Aurora-style seats change quality, not span.
* Commissioning an ensign from the crew of any ship with an empty chair. Rank 1, experience 0, cap 40, costs one crew member, span stays Body. The button waits until a ship has a crew headcount. Their first command course is the cap-raising course above.
* Crew training. Crew comes from population under item 20. The academy does not produce it.
* A separate warship and merchant track.
* Admiral ranks, and a span wider than a system. When that exists, the one-jump sentence moves off the system band.
* Scientists, administrators, personality, and bids.
