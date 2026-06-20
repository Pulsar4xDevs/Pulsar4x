# Build Check — Compile and Test

Run the full build and test suite. Use before pushing any changes and after any significant edit session.

## Steps

Run in order from the repo root (`/home/user/Pulsar4x/`):

```bash
dotnet build Pulsar4X/Pulsar4X.sln
```

If build passes:

```bash
dotnet test Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj --logger "console;verbosity=normal"
```

## Report Format

After running, report:

```
BUILD:  PASS / FAIL
  [error summary if fail — file:line and message]

TESTS:  X passed, Y failed, Z skipped
  [names of any failing tests]

WARNINGS (non-trivial only):
  [list any warnings that are not nullable reference or async void — those are known]
```

## Rules

- Failing build = hard stop. Do not commit or push. Fix first.
- Failing test = hard stop unless the test was already failing before your change.
  Confirm pre-existing failure with: `git stash && dotnet test && git stash pop`
- `async void` warnings on `EntityManager.AddEntity / SetDataBlob / RemoveDatablob / TagEntityForRemoval`
  are **known and expected** — do not treat as errors.
- Nullable reference warnings are suppressed project-wide (`NoWarn>0649`) — expected.
