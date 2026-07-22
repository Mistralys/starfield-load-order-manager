# LoadOrderKeeper.Tests

Test suite for the Starfield Load Order Keeper application.

---

## Test Categories

### Standard Tests (run in CI)

All tests without a `[Trait]` attribute run automatically as part of the standard test suite. This includes:

- **Scenario tests** (`ScenarioTests.cs`) — cover the documented diff/sort scenarios using `ScenarioTestBase` infrastructure.
- **Unit tests** — cover individual services, coordinators, models, and view text classes.

### Diagnostic Tests (manual, excluded from CI)

Tests decorated with `[Trait("Category", "Diagnostic")]` are excluded from the standard CI run. They are companion tests designed to emit a human-readable diagnostic log when a regression is suspected — useful for debugging, but too verbose for routine CI output.

**Currently registered diagnostic tests:**

| Class | Method | Purpose |
|---|---|---|
| `ReplacementDetectionDiagnostics` | `DetailedReplacementDetection_WithPositionShifts` | Companion diagnostic to `ScenarioTests.Scenario_16`. Logs the full LCS diff output for the Scenario 16 replacement-under-position-shift case and asserts `Fragile.esm` is classified as `Replaced`. |

**When to run diagnostic tests manually:**

Run diagnostic tests when:
- `ScenarioTests.Scenario_16` (or a related replacement-detection test) fails and the assertion message alone is not enough to identify the root cause.
- You suspect a regression in `DiffService.ClassifyChanges` Step 3 (replacement detection) or `DiffService.ComputeLcs`.
- You need a step-by-step trace of what `GetPluginsDiffAsync` produces for the Scenario 16 data set.

**How to run diagnostic tests:**

```
dotnet test --filter "Category=Diagnostic"
```

Or from Visual Studio Test Explorer, filter by trait `Category = Diagnostic`.

---

## Scenario Test Infrastructure

`ScenarioTestBase` provides:

- `StandardModList` — the 18-mod reference list shared across all scenarios.
- `SetupStandardReferenceAsync` / `SetupCurrentOrderAsync` — helpers for writing test fixture files.
- `CreateMockModFiles` — creates stub mod files in a temp `Data` folder (used for case-sensitivity tests).
- Assertion helpers: `AssertModAdded`, `AssertModRemoved`, `AssertModMoved`, `AssertModReplaced`, `AssertChangeCount`.

Each scenario test uses `TestConfigContext` (an `IDisposable` wrapper) to isolate file I/O in a temporary directory that is cleaned up after the test.

---

## Locale-Sensitive Tests

Tests that depend on a specific locale (e.g., date formatting, localization string output) use the `EnglishLocaleFixture` and `LocaleSequentialCollection` to force `en-US` for their test class lifetime and disable parallelization. See `Fixtures/` for details.
