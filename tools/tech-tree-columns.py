#!/usr/bin/env python3
"""Print the tech tree as columns: depth of every tech, its parents (with the unlock level), and warnings.

  python3 tools/tech-tree-columns.py [techs.json ...]      (default: the basemod techs.json and techs-bureaucracy.json)

Depth (column) is the longest chain of parents from a root, a root being column 1. Unlocks are OR, so a tech opens
when any parent reaches the listed level. Warnings: unlock ids that are neither a tech nor a known component id are
ignored (components are not listed); cycles and roots are reported.
"""
import json
import sys
from collections import defaultdict

base = "Pulsar4X/GameData/basemod/TemplateFiles/"
files = sys.argv[1:] or [base + "techs.json", base + "techs-bureaucracy.json"]
techs = {}
for f in files:
    for e in json.load(open(f)):
        if e["Type"] == "Tech":
            techs[e["Payload"]["UniqueID"]] = e["Payload"]
parents = defaultdict(list)
for t, p in techs.items():
    for level, ids in p.get("Unlocks", {}).items():
        for i in ids:
            if i in techs:
                parents[i].append((t, int(level)))
depth, stack = {}, set()
cycles = []

def col(t):
    if t in depth:
        return depth[t]
    if t in stack:
        cycles.append(t)
        return 0
    stack.add(t)
    depth[t] = 1 + max([col(p) for p, _ in parents[t]] or [0])
    stack.discard(t)
    return depth[t]

for t in techs:
    col(t)
by_col = defaultdict(list)
for t, c in depth.items():
    by_col[c].append(t)
print("techs", len(techs), "edges", sum(len(v) for v in parents.values()), "columns", max(depth.values()),
      "sizes", [len(by_col[c]) for c in sorted(by_col)])
roots = sorted(t for t in techs if not parents[t])
print("roots (%d):" % len(roots), ", ".join(roots))
print("cycles:", cycles or "none")

def show(t, indent=0, seen=None):
    seen = seen or set()
    note = "" if len(parents[t]) < 2 else "  (parents: %s)" % ", ".join(p for p, _ in parents[t])
    lv = [l for p, l in parents[t]]
    gate = "" if not lv or max(lv) == 1 else "  [needs parent level %d]" % max(lv)
    print("%s[col %d] %s  (%s)%s%s" % ("  " * indent, depth[t], t, techs[t]["Name"], gate, note))
    kids = sorted((k for k in techs if any(p == t for p, _ in parents[k]) and (parents[k][0][0] == t)), key=lambda k: (depth[k], k))
    for k in kids:
        show(k, indent + 1)

for r in roots:
    show(r)
