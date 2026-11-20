#!/usr/bin/env python3
"""Compare two scenario tech-state dumps (before and after a tech-tree change).

Make a dump with the explicit test (run it once on each side of the change):
  TECH_DUMP=/tmp/before.json dotnet test Pulsar4X/Pulsar4X.Tests --filter "FullyQualifiedName~ScenarioTechStateDump"
Then:
  python3 tools/compare-tech-state.py /tmp/before.json /tmp/after.json [removed-tech-id ...]

For every scenario faction it checks that each unlocked tech has the same level (so the researched and researchable sets
match), and that the unlocked component templates, armor, cargo types and industry types match. Ids named on the command
line are techs the change removed on purpose: they are ignored, and the locked-tech count may drop by the ones that were
still locked. Exit status 1 on any other difference.
"""
import json
import sys

before = json.load(open(sys.argv[1]))
after = json.load(open(sys.argv[2]))
removed = set(sys.argv[3:])
bad = False
if sorted(before) != sorted(after):
    print("factions differ:", sorted(set(before) ^ set(after)))
    bad = True
for name in sorted(set(before) & set(after)):
    b, a = before[name], after[name]
    diffs = []
    tech_b = {t: l for t, l in b["techs"].items() if t not in removed}
    if tech_b != a["techs"]:
        diffs.append(("techs", {t: (tech_b.get(t), a["techs"].get(t)) for t in set(tech_b) | set(a["techs"]) if tech_b.get(t) != a["techs"].get(t)}))
    for field in ("components", "armor", "cargoTypes", "industryTypes"):
        if b[field] != a[field]:
            diffs.append((field, sorted(set(b[field]) ^ set(a[field]))))
    dropped = b["lockedTechCount"] - a["lockedTechCount"]
    still_locked = len(removed) - sum(1 for t in removed if t in b["techs"])
    if dropped != still_locked:
        diffs.append(("lockedTechCount", b["lockedTechCount"], a["lockedTechCount"], "expected drop", still_locked))
    print(("OK   " if not diffs else "DIFF ") + name, diffs if diffs else "")
    bad |= bool(diffs)
print("PARITY FAILED" if bad else "PARITY OK")
sys.exit(1 if bad else 0)
