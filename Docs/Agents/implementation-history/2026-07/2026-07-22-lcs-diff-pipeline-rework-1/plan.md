
# Plan

## Plan Audit Cycles
- Audits: 2 — Plan Auditor v1.6.0
- Architectural Reviews: none — Plan Architect Reviewer v2.1.0

## Summary

This rework plan addresses all actionable items from the `2026-07-21-lcs-diff-pipeline` synthesis. It promotes every deferred item that benefits long-term application quality — adding isolated `ClassifyChanges` unit tests, fixing the 21 pre-existing test failures caused by locale-dependent assertions, cleaning up internal code inconsistencies in the classification pipeline, removing the stale historical simulation from `ReplacementDetectionDiagnostics.cs`, and optimizing the O(n²) LINQ in Step 6b. The plan is organized into four coherent sections: test infrastructure (locale), ClassifyChanges isolation tests, ClassifyChanges code hygiene, and diagnostic cleanup.

## Architectural Context

The diff pipeline lives in `Services/DiffService.cs` and exposes two `internal static` methods (`ComputeLcs`, `ClassifyChanges`) alongside the public `GetPluginsDiffAsync` entrypoint. All localized text flows through the `LocalizationService` singleton, which detects the system culture at startup (`CultureInfo.CurrentUICulture`). The test suite has no culture management — on a `fr-FR` system, 21 tests fail because they hardcode English expected values. The `ClassifyChanges` method has three minor code-quality issues identified during the original plan's review: an inconsistent null-`LineNumber` fallback strategy between Steps 1 and 3, a vacuous `.Where` filter in Step 6, and an O(n²) LINQ pattern in Step 6b.

## Approach / Architecture

1. **Locale-aware test fixture** — Introduce a shared xUnit `IClassFixture<T>` that forces `LocalizationService.Instance.SetCulture("en-US")` before any test class that asserts on localized strings. Fix all 21 pre-existing failures by applying the fixture to affected test classes and correcting the `LocalizationServiceTests.CurrentCulture_DefaultsToEnglish` test to validate system culture detection rather than asserting a hardcoded locale.

2. **ClassifyChanges isolation tests** — Add a dedicated test class that calls `ClassifyChanges` directly with hand-crafted `ModEntryModel` lists and pre-computed LCS pairs. Assert on structural properties (`ChangeType`, `FileName`, `ReferenceNumber`, `CurrentNumber`, `DependentChanges`) rather than localized `.Text` strings. Cover each classification step: Moved, Replaced, Inserted, Added, Removed, and dependent-change grouping.

3. **ClassifyChanges code hygiene** — Align the null-`LineNumber` fallback in Step 1 and Step 3 to a single consistent strategy, remove the vacuous `.Where` filter in Step 6, and replace the O(n²) LINQ in Step 6b with a two-pointer approach.

4. **Diagnostic cleanup** — Strip the historical two-pass simulation from `ReplacementDetectionDiagnostics.cs`, retaining only the behavioral assertion.

## Rationale

- **Locale fixture first:** The 21 pre-existing failures mask real regressions. Fixing them is a prerequisite for trusting the test suite as a safety net for subsequent changes.
- **Isolation tests before code changes:** Adding `ClassifyChanges` isolation tests before modifying the method provides regression detection for the hygiene changes.
- **All deferred items promoted:** Every item from the synthesis contributes to long-term code health — test coverage, consistency, performance scalability, and reduced confusion from stale artifacts. None are speculative.

## Considered Alternatives

| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| Locale fixture approach | `IClassFixture<T>` with `SetCulture("en-US")` in constructor | (A) `xunit.runner.json` with `"culture": "en-US"` (B) Per-test `SetCulture` calls (C) Assert against `_localization.GetString()` return values instead of hardcoded strings | (A) affects all tests globally including legitimate locale tests; (B) verbose and error-prone; (C) tests would never catch wrong localization key usage. Fixture approach is explicit, scoped, and follows xUnit conventions. |
| Step 6b optimization | Two-pointer scan with pre-sorted shifted list | (A) Leave as-is with a comment (B) Replace with dictionary lookup | Two-pointer is linear, preserves the existing sorted-list structure, and is simple to implement. Dictionary lookup changes the data structure unnecessarily. Leaving as-is defers a known quadratic pattern. |
| ReplacementDetectionDiagnostics cleanup | Remove simulation, keep behavioral assertion | (A) Rewrite simulation to trace LCS path (B) Delete entire file | Rewriting the simulation adds maintenance burden for a diagnostic-only file. Deleting entirely loses the Scenario 16 regression check. Keeping only the assertion is minimal and sufficient. |

## Pattern Alignment

- **Test fixture pattern** — No existing test fixture for culture management exists; this introduces a new pattern. Justified: the project has a localization-heavy architecture and needs deterministic test behavior across developer machines.
- **`internal static` testing pattern** — `InternalsVisibleTo` is already configured in `Starfield Load Order Keeper.csproj` (L28–L30). Isolation tests follow the existing access pattern.
- **Code hygiene** — Aligning fallback strategies and removing dead code follows the project's conventions in `constraints.md` and `tech-stack.md`.

## Detailed Steps

### Section A: Locale-Aware Test Infrastructure (Fixes 21 Pre-Existing Failures)

**Step 1: Create `EnglishLocaleFixture` class**

Create `Tests/LoadOrderKeeper.Tests/Fixtures/EnglishLocaleFixture.cs`:

```csharp
using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.Tests.Fixtures;

public class EnglishLocaleFixture : IDisposable
{
    private readonly string _originalCulture;

    public EnglishLocaleFixture()
    {
        _originalCulture = LocalizationService.Instance.CurrentCulture;
        LocalizationService.Instance.SetCulture("en-US");
    }

    public void Dispose()
    {
        LocalizationService.Instance.SetCulture(_originalCulture);
    }
}
```

This fixture stores the original culture on construction, forces `en-US`, and restores on disposal. Any test class can opt in via `IClassFixture<EnglishLocaleFixture>`.

**Step 2: Apply fixture to `GameLauncherCoordinatorTests`**

Add `IClassFixture<EnglishLocaleFixture>` to `Tests/LoadOrderKeeper.Tests/Coordinators/GameLauncherCoordinatorTests.cs`. Accept `EnglishLocaleFixture` in the constructor (xUnit requires this to trigger fixture instantiation). Fixes 12 failures.

**Step 3: Apply fixture to `StatusCoordinatorTests`**

Add `IClassFixture<EnglishLocaleFixture>` to `Tests/LoadOrderKeeper.Tests/Coordinators/StatusCoordinatorTests.cs`. Fixes 6 failures.

**Step 4: Apply fixture to `ProfileServiceTests`**

Add `IClassFixture<EnglishLocaleFixture>` to `Tests/LoadOrderKeeper.Tests/ProfileServiceTests.cs`. Fixes 1 failure.

**Step 5: Apply fixture to `DiffServiceTests`**

Add `IClassFixture<EnglishLocaleFixture>` to `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs`. Fixes 1 failure (`Assert.Contains("line 2", ...)`).

**Step 6: Apply fixture to `UpdateCheckCoordinatorTests`**

Add `IClassFixture<EnglishLocaleFixture>` to `Tests/LoadOrderKeeper.Tests/Coordinators/UpdateCheckCoordinatorTests.cs`. This ensures `GetLatestVersion()` uses the English format template `"Version {0} is available!"` to match the English test message. Fixes 3 failures.

**Step 7: Fix `LocalizationServiceTests.CurrentCulture_DefaultsToEnglish`**

This test incorrectly asserts the singleton always defaults to `en-US`. On a `fr-FR` system, the singleton correctly detects `fr-FR`. Replace the test with one that validates the culture detection contract:

- Assert that `CurrentCulture` is a non-empty, valid culture string.
- Assert that if the system culture has a matching locale file, `CurrentCulture` matches it.
- Alternatively, rename to `CurrentCulture_DefaultsToSystemCulture` and assert that `CurrentCulture` matches `CultureInfo.CurrentUICulture.Name` (or its parent) when a locale file exists.

This test should NOT use the `EnglishLocaleFixture` since it validates the default culture detection behavior.

**Step 8: Run full test suite — confirm all 21 pre-existing failures are resolved**

### Section B: ClassifyChanges Isolation Tests

**Step 9: Create `ClassifyChangesTests` test class**

Create `Tests/LoadOrderKeeper.Tests/ClassifyChangesTests.cs` with the following test methods. Each test constructs `ModEntryModel` lists directly (no file I/O), computes the LCS via `ComputeLcs`, and calls `ClassifyChanges`. Assertions target structural properties only — `ChangeType`, `FileName`, `ReferenceNumber`, `CurrentNumber`, `DependentChanges.Count` — never localized `.Text`.

**Step 9a: Test — Unchanged list returns empty**
- Reference: `[a, b, c]` with LineNumbers `[1, 2, 3]`
- Current: same
- LCS: `[(0,0), (1,1), (2,2)]`
- Expected: empty result (no changes)

**Step 9b: Test — Single removal**
- Reference: `[a, b, c]` with LineNumbers `[1, 2, 3]`
- Current: `[a, c]` with LineNumbers `[1, 2]`
- Expected: one `Removed` entry for `b` with `ReferenceNumber == 2`. One dependent `Moved` for `c` (shifted from 3 to 2).

**Step 9c: Test — Single insertion**
- Reference: `[a, b, c]` with LineNumbers `[1, 2, 3]`
- Current: `[a, new, b, c]` with LineNumbers `[1, 2, 3, 4]`
- Expected: one `Inserted` entry for `new` with `CurrentNumber == 2`. Dependent `Moved` entries for `b` and `c`.

**Step 9d: Test — Single addition at end**
- Reference: `[a, b]` with LineNumbers `[1, 2]`
- Current: `[a, b, new]` with LineNumbers `[1, 2, 3]`
- Expected: one `Added` entry for `new` with `CurrentNumber == 3`.

**Step 9e: Test — Replacement at same position**
- Reference: `[a, b]` with LineNumbers `[1, 2]`
- Current: `[a, c]` with LineNumbers `[1, 2]`
- Expected: one `Replaced` entry for `b` with `ReplacementFileName == "c"`.

**Step 9f: Test — Moved (swapped) mods**
- Reference: `[a, b, c]` with LineNumbers `[1, 2, 3]`
- Current: `[b, a, c]` with LineNumbers `[1, 2, 3]`
- Expected: two `Moved` entries — `a` from #1 to #2, `b` from #2 to #1.

**Step 9g: Test — Dependent change grouping under removal**
- Reference: `[a, b, c, d, e]` with LineNumbers `[1, 2, 3, 4, 5]`
- Current: `[a, c, d, e]` with LineNumbers `[1, 2, 3, 4]`
- Expected: one `Removed` for `b`. `c`, `d`, `e` are dependent changes (shifted up) grouped under `b`.

**Step 9h: Test — Multiple consecutive replacements**
- Reference: `[a, b, c, d]` with LineNumbers `[1, 2, 3, 4]`
- Current: `[a, x, y, d]` with LineNumbers `[1, 2, 3, 4]`
- Expected: two `Replaced` entries — `b` replaced by `x`, `c` replaced by `y`. This specifically tests the `cumulativeDeletions` counter fix from WP-002.

**Step 9i: Test — Replacement with position shift (deletion before replacement)**
- Reference: `[a, b, c, d]` with LineNumbers `[1, 2, 3, 4]`
- Current: `[a, c, x]` with LineNumbers `[1, 2, 3]`
- Expected: `b` is `Removed`, `d` is `Replaced` by `x` (at aligned position 4−1=3). Tests that the cumulative deletion offset correctly aligns the replacement.

**Step 9j: Test — Insertion with dependent change attribution**
- Reference: `[a, b, c]` with LineNumbers `[1, 2, 3]`
- Current: `[a, new, b, c]` with LineNumbers `[1, 2, 3, 4]`
- Expected: `new` is `Inserted` at position 2 with `b` and `c` as `DependentChanges`.

### Section C: ClassifyChanges Code Hygiene

**Step 10: Align null-`LineNumber` fallback strategy**

In `Services/DiffService.cs`, align Step 1 and Step 3 to use the same null-handling approach:

- **Step 1** (L218–L219): Currently uses `ri + 1` / `ci + 1` as fallback. This is a reasonable position proxy.
- **Step 3** (L266): Currently uses `0` as a sentinel to exclude entries from replacement matching.

The strategies serve different purposes: Step 1 needs a proxy position for shift detection, Step 3 needs to exclude unpositioned entries. Add a `Debug.Assert(reference[ri].LineNumber.HasValue)` at the top of the LCS loop in Step 1 and a `Debug.Assert(m.LineNumber.HasValue)` in the Step 3 `remainingNew` loop to make the invariant explicit. Keep the existing fallback logic as defensive code, but add comments explaining the invariant and why the fallbacks differ.

**Step 11: Remove vacuous `.Where` filter in Step 6**

In `Services/DiffService.cs` (L396–L399), replace:
```csharp
var shiftedByRefPos = shiftedLines
    .Where(s => s.ReferenceNumber.HasValue)
    .OrderBy(s => s.ReferenceNumber!.Value)
    .ToList();
```
with:
```csharp
Debug.Assert(shiftedLines.All(s => s.ReferenceNumber.HasValue),
    "All shifted lines must have ReferenceNumber — they are constructed from LCS pairs with explicit refPos.");
var shiftedByRefPos = shiftedLines
    .OrderBy(s => s.ReferenceNumber!.Value)
    .ToList();
```

Add `using System.Diagnostics;` if not already present.

**Step 12: Optimize Step 6b insertion attribution — replace O(n²) LINQ with two-pointer scan**

In `Services/DiffService.cs` (L426–L461), replace the nested LINQ pattern with a two-pointer approach:

Before each inserted entry is processed, maintain an index into `shiftedByRefPos` (pre-sorted by `CurrentNumber` for the lookup portion). For each insertion:
1. Binary-search or linear-scan `shiftedByRefPos` (by `CurrentNumber`) to find the first unassigned entry at or after the insertion's `CurrentNumber`.
2. Walk forward from that point, assigning entries until the next removal boundary or the end of the range.

This reduces the amortized cost to O(k) total across all insertions when attribution ranges are disjoint (common case). Worst-case remains O(r × k) where r = insertedEntries count and all insertions share the same attribution range — acceptable for typical mod-list sizes.

Implementation sketch:
```csharp
// Pre-sort shifted by CurrentNumber for insertion attribution
var shiftedByCurPos = shiftedByRefPos
    .OrderBy(s => s.CurrentNumber ?? int.MaxValue)
    .ToList();

int shiftedIdx = 0;
foreach (var inserted in insertedEntries)
{
    int insertedCurPos = inserted.CurrentNumber!.Value;

    // Advance past already-assigned or entries before this insertion
    while (shiftedIdx < shiftedByCurPos.Count &&
           (assignedShifted.Contains(shiftedByCurPos[shiftedIdx]) ||
            shiftedByCurPos[shiftedIdx].CurrentNumber < insertedCurPos))
    {
        shiftedIdx++;
    }

    if (shiftedIdx >= shiftedByCurPos.Count)
        break;

    var firstAffected = shiftedByCurPos[shiftedIdx];
    int startRefPos = firstAffected.ReferenceNumber!.Value;

    // Next removal in reference space as upper boundary
    var nextRemoved = removedEntries
        .Where(r => r.ReferenceNumber >= startRefPos)
        .OrderBy(r => r.ReferenceNumber)
        .FirstOrDefault();
    int? stopBefore = nextRemoved?.ReferenceNumber;

    foreach (var shifted in shiftedByRefPos)
    {
        if (assignedShifted.Contains(shifted))
            continue;

        int refPos = shifted.ReferenceNumber!.Value;
        if (refPos < startRefPos)
            continue;
        if (stopBefore.HasValue && refPos >= stopBefore.Value)
            break;

        inserted.DependentChanges.Add(shifted);
        assignedShifted.Add(shifted);
    }
}
```

Note: The `nextRemoved` lookup per insertion is O(r) where r = removed count. For typical mod lists this is negligible. If removedEntries were also indexed by position, this could be O(log r), but that is not justified at current scale.

### Section D: Diagnostic Cleanup

**Step 13: Simplify `ReplacementDetectionDiagnostics.cs`**

In `Tests/LoadOrderKeeper.Tests/ReplacementDetectionDiagnostics.cs`, remove the historical simulation section (approximately lines 100–230 — from the `GetModDiffAsync` call through the second-pass heuristic trace). Retain:
- The class-level XML documentation (update to remove references to the simulation section).
- The Arrange block (Scenario 16 test data setup).
- The Act block (call to `GetPluginsDiffAsync`).
- The Assert block (`Assert.Equal(DiffChangeType.Replaced, fragile.ChangeType)`).
- A simplified diagnostic trace that logs the LCS pipeline result (the 4 lines starting at "=== ACTUAL DIFF SERVICE RESULT ===").

The method-level XML documentation should be updated to remove references to the inline simulation.

**Step 14: Update `api-surface.md` ClassifyChanges documentation**

In `Docs/Agents/project-manifest/api-surface.md` (L554), update the `ClassifyChanges` documentation note from "Pure function — no shared mutable state" to "Pure function — no shared mutable state. Covered by dedicated isolation tests in `ClassifyChangesTests.cs` targeting each classification step independently."

### Section E: Verification

**Step 15: Run full test suite — confirm all tests pass, including the 10 new ClassifyChanges isolation tests and the previously-failing 21 tests.**

## Dependencies

- Step 1 (fixture) must be completed before Steps 2–7 (fixture application).
- Steps 2–8 (Section A) should be completed before Step 9 (Section B) to establish a green test baseline.
- Step 9 (isolation tests) should be completed before Steps 10–12 (Section C) to provide regression coverage.
- Steps 10–12 are independent of each other.
- Step 13 (diagnostic cleanup) is independent of all other steps.
- Step 14 depends on Step 9 (tests must exist before documenting their coverage).

## Required Components

- `Tests/LoadOrderKeeper.Tests/Fixtures/EnglishLocaleFixture.cs` — **new**
- `Tests/LoadOrderKeeper.Tests/ClassifyChangesTests.cs` — **new**
- `Tests/LoadOrderKeeper.Tests/Coordinators/GameLauncherCoordinatorTests.cs` — modification
- `Tests/LoadOrderKeeper.Tests/Coordinators/StatusCoordinatorTests.cs` — modification
- `Tests/LoadOrderKeeper.Tests/ProfileServiceTests.cs` — modification
- `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs` — modification
- `Tests/LoadOrderKeeper.Tests/Coordinators/UpdateCheckCoordinatorTests.cs` — modification
- `Tests/LoadOrderKeeper.Tests/ViewTexts/LocalizationServiceTests.cs` — modification
- `Services/DiffService.cs` — modification (Steps 10–12)
- `Tests/LoadOrderKeeper.Tests/ReplacementDetectionDiagnostics.cs` — modification (Step 13)
- `Docs/Agents/project-manifest/api-surface.md` — modification (Step 14)

## Assumptions

- `LocalizationService.Instance.SetCulture("en-US")` is thread-safe (it uses a `lock` internally — verified in research).
- xUnit's `IClassFixture<T>` ensures the fixture is instantiated once per test class and disposed after all tests in that class complete.
- The 21 pre-existing failures are exhaustively covered by the 6 test classes listed. If additional locale-dependent failures exist in classes not identified, the same fixture pattern applies.
- `ModEntryModel` can be constructed directly in tests with `FileName` and `LineNumber` set.

## Constraints

- `LocalizationService` is a singleton — `SetCulture` changes global state. Tests must restore the original culture via fixture disposal.
- `ClassifyChanges` isolation tests must not assert on `.Text` content to remain locale-independent.
- The `InternalsVisibleTo` attribute is configured via MSBuild in the `.csproj`, not in `AssemblyInfo.cs`. This is sufficient and should not be moved (deferred item #7 from synthesis — not promoted; there is no current `AssemblyInfo.cs` to move it to, and the MSBuild approach is functionally equivalent).

## Out of Scope

- Next diff system evolution (batch reorders, multi-file diffs, per-change confidence scoring) — identified in synthesis Next Steps #5 as future capabilities, not rework items.
- `InternalsVisibleTo` relocation (deferred item #7) — no `AssemblyInfo.cs` exists; MSBuild approach is standard and functional.
- French locale translation quality — the locale strings themselves are not under review.

## Acceptance Criteria

- AC-01: All 21 pre-existing test failures pass on a `fr-FR` system.
- AC-02: `EnglishLocaleFixture` exists and is reusable by any test class that needs deterministic English locale.
- AC-03: `LocalizationServiceTests.CurrentCulture_DefaultsToEnglish` is replaced with a culture-detection test that passes on any system locale.
- AC-04: `ClassifyChangesTests` contains at least 10 isolation tests covering: unchanged, removal, insertion, addition, replacement, moved/swapped, dependent grouping under removal, multiple consecutive replacements, replacement with position shift, and insertion with dependents.
- AC-05: All `ClassifyChangesTests` assertions use structural properties (`ChangeType`, `FileName`, `ReferenceNumber`, `CurrentNumber`, `DependentChanges`), not localized `.Text`.
- AC-06: `ClassifyChanges` Step 1 and Step 3 null-`LineNumber` handling includes `Debug.Assert` making the invariant explicit.
- AC-07: The vacuous `.Where(s => s.ReferenceNumber.HasValue)` filter in Step 6 is removed and replaced with a `Debug.Assert`.
- AC-08: Step 6b insertion attribution uses a linear-time approach instead of O(n²) LINQ.
- AC-09: `ReplacementDetectionDiagnostics.cs` contains only the behavioral assertion and LCS pipeline diagnostic trace — no historical simulation code.
- AC-10: `api-surface.md` ClassifyChanges entry references the isolation test coverage.
- AC-11: Full test suite passes with zero failures.

## Testing Strategy

This plan is test-centric by nature. Section A fixes the test infrastructure itself, Section B adds new tests, Section C modifies production code covered by both the new isolation tests and the existing end-to-end tests. Each section concludes with a test run to verify the changes incrementally.

## Test Plan

- `Tests/LoadOrderKeeper.Tests/ClassifyChangesTests.cs` — 10 new tests covering each `ClassifyChanges` classification step (Steps 9a–9j) — covers AC-04, AC-05
- `Tests/LoadOrderKeeper.Tests/Coordinators/GameLauncherCoordinatorTests.cs` — 12 existing tests now pass with fixture — covers AC-01
- `Tests/LoadOrderKeeper.Tests/Coordinators/StatusCoordinatorTests.cs` — 6 existing tests now pass with fixture — covers AC-01
- `Tests/LoadOrderKeeper.Tests/ProfileServiceTests.cs` — 1 existing test now passes with fixture — covers AC-01
- `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs` — 1 existing test now passes with fixture — covers AC-01
- `Tests/LoadOrderKeeper.Tests/Coordinators/UpdateCheckCoordinatorTests.cs` — 3 existing tests now pass with fixture — covers AC-01
- `Tests/LoadOrderKeeper.Tests/ViewTexts/LocalizationServiceTests.cs` — `CurrentCulture_DefaultsToEnglish` redesigned — covers AC-03
- Full test suite run — covers AC-11

## Documentation Updates

- `Docs/Agents/project-manifest/api-surface.md` — Update `ClassifyChanges` documentation note to reference isolation test coverage (Step 14) — covers AC-10
- `Docs/Agents/project-manifest/file-tree.md` — Add `Tests/LoadOrderKeeper.Tests/Fixtures/EnglishLocaleFixture.cs` and `Tests/LoadOrderKeeper.Tests/ClassifyChangesTests.cs`
- `Docs/Agents/project-manifest/tech-stack.md` — Add a "Testing Conventions" subsection documenting the `EnglishLocaleFixture` / `IClassFixture<T>` pattern as the established approach for locale-sensitive test classes

## Deferred Items

| # | Deferred Item | Origin | Reason Deferred | Notes |
|---|---------------|--------|-----------------|-------|
| 1 | `InternalsVisibleTo` relocation to `AssemblyInfo.cs` | Synthesis deferred item #7 | No `AssemblyInfo.cs` exists in the project. The MSBuild `<AssemblyAttribute>` approach in the `.csproj` is standard and functionally equivalent. Moving it would first require creating a file that doesn't exist. | Reconsider only if `AssemblyInfo.cs` is created for other reasons. |

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **Singleton locale fixture causes test ordering issues** | xUnit runs test classes in isolation by default. The fixture restores the original culture on disposal. If parallel test execution causes conflicts, add `[Collection("Locale")]` to serialize affected classes. |
| **Step 6b optimization changes dependent-change grouping behavior** | The 16 scenario tests + 10 new isolation tests provide comprehensive regression coverage. Run before and after the optimization to confirm identical output. |
| **`ModEntryModel` construction in isolation tests may not match production shape** | Use `FileService.ReadModListAsync`'s conventions (1-based `LineNumber`, `IsEnabled = true`) as the reference. The research brief confirms `LineNumber` is always assigned by `ReadFileAsync`. |

## Recommended Workflow
- **Workflow:** ledger
- **Rationale:** Four distinct concern areas (test infra, isolation tests, code hygiene, cleanup) spanning 7+ files with interdependencies benefit from formal WP decomposition and staged verification.
