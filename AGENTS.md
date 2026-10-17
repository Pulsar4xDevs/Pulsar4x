# Pulsar4X

Checkout is DevBranch. Do not commit or push unless asked. `git add` every new file and leave it uncommitted. A test written only to check a guess is not added; delete it instead.

## Start of a session

Read the Open section of `Pulsar4X/agent-notes.md`, then the first unfinished item in `Pulsar4X/near-term.md`.

If no bug is named, do that near-term item. If a bug is named, do that and leave the list alone.

Read a design doc only when the task is inside it. Linked from agent-notes: movement, damage, agents-and-goals, agent wakes, weighting, logistics plans, industry plans, colony-administrator, and Ceres plans under `GameEngine/Engine/Orders/plans/`.

Repo markdown wins over chat memory.

## While editing

If you are already in the code an open note describes, and the fix is small, fix it and mark the note done. If it is unrelated or not small, append one heading to agent-notes and leave that code alone.

A plan, a saved idea, or an inbox item that says "do not build until asked" stays unbuilt. That includes Ceres plan 6, sealed shipments, office purses, colony-administrator personality, price finding, faction-grants-later, and any logistics plan 13.

JSON mod data: edit basemod only. Leave sol-trade unchanged unless asked.

When asked to save an idea, write the note and do not implement it. When asked for a plan only, do not change game code. A test that checks a guess is the exception, and it follows the rule in Tests. When there is no room for a full plan, add one short open item to agent-notes.

When a choice would change behavior, scope, or a written design, ask before doing it. Match the surrounding code for local style, names, and shape. Do not stop to ask about those.

Prefer the plain form. Do not add a helper, abstraction, or parameter for a single call site. Do not compress a few clear lines into a clever one-liner.

## Tests

A test may be written while examining a problem, including when the task is only to look. Use it to confirm a guess or to pin down the failure. Delete it when the question is answered. Keep it when it is the check that the bug stays fixed, and `git add` that file.

`Pulsar4X/global.json` pins SDK 8.0.122. Run `dotnet` with a working directory outside this repo, and pass the test project by its full path. The root disk fills up, so `TMPDIR` must be a directory under `/home/se5a`.

    TMPDIR=/home/se5a/.cache/build-tmp DOTNET_ROLL_FORWARD=LatestMajor \
      dotnet test <repo>/Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj --filter <Name>
