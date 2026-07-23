# Synthesis Report — `2026-07-22-lcs-diff-pipeline-rework-1`

**Project:** LCS Diff Pipeline Rework — Deferred Items Promotion  
**Date:** 2026-07-22  
**Status:** COMPLETE (5/5 WPs)  
**Agents:** Developer · QA · Reviewer · Documentation · Synthesis

---

## Executive Summary

This rework cycle addressed every actionable item deferred from the prior `2026-07-21-lcs-diff-pipeline` synthesis. Five work packages were completed across a single session:

- **WP-001** introduced `EnglishLocaleFixture` and a `LocaleSequentialCollection` to make the test suite locale-deterministic, eliminating 21 pre-existing test failures on non-English developer machines.
- **WP-002** removed ~130 lines of stale historical simulation code from `ReplacementDetectionDiagnostics.cs`, leaving only the behavioral Scenario 16 assertion.
- **WP-003** added 10 dedicated `ClassifyChangesTests` isolation tests covering every classification sub-step (9a–9j), backed by structural-property assertions with zero dependency on localized text.
- **WP-004** inserted `Debug.Assert` guards at three LineNumber/ReferenceNumber invariant points in `DiffService.ClassifyChanges` and removed a vacuous `.Where` filter in Step 6.
- **WP-005** replaced the O(n²) LINQ chain in Step 6b with a two-pointer scan, reducing insertion attribution to amortized O(k) across all insertions.

All 5 WPs passed all pipeline stages (implementation → QA → code-review → documentation). The test suite ended at **439 pass / 1 fail**, where the single failure is a confirmed pre-existing file-encoding bug in `ProfileServiceTests.GenerateProfileId_AccentedCharacters_RemovesAccents` that predates this plan and is unrelated to any WP in scope.

---

## Metrics

| WP | Tests Pass | Tests Fail | Rework Cycles | Pipeline Stages |
|----|-----------|-----------|---------------|-----------------|
| WP-001 | 429 | 1 (pre-existing) | 1 impl + 1 QA | 4 (impl×2, qa×2, review, docs) |
| WP-002 | 429 | 1 (pre-existing) | 0 | 4 |
| WP-003 | 439 | 1 (pre-existing) | 0 | 4 |
| WP-004 | 439 | 1 (pre-existing) | 0 | 4 |
| WP-005 | 439 | 1 (pre-existing) | 0 | 4 |

**Net new tests added:** 10 (ClassifyChangesTests, WP-003)  
**Net tests fixed (locale failures):** 21 (WP-001)  
**Pre-existing failure (out of scope):** 1 (`ProfileServiceTests.GenerateProfileId_AccentedCharacters_RemovesAccents` — garbled Windows-1252 encoded test input bytes)

### Files Modified

| File | WP(s) |
|------|--------|
| `Tests/LoadOrderKeeper.Tests/Fixtures/EnglishLocaleFixture.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/Fixtures/LocaleSequentialCollection.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/Coordinators/GameLauncherCoordinatorTests.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/Coordinators/StatusCoordinatorTests.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/Coordinators/UpdateCheckCoordinatorTests.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/ProfileServiceTests.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/ViewTexts/LocalizationServiceTests.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/ReplacementDetectionDiagnostics.cs` | WP-002 |
| `Tests/LoadOrderKeeper.Tests/README.md` | WP-002 (new) |
| `Tests/LoadOrderKeeper.Tests/ClassifyChangesTests.cs` | WP-003 (new) |
| `Services/DiffService.cs` | WP-003, WP-004, WP-005 |
| `Docs/Agents/project-manifest/api-surface.md` | WP-002, WP-003, WP-004, WP-005 |
| `Docs/Agents/project-manifest/file-tree.md` | WP-001, WP-002, WP-005 |
| `Docs/Agents/project-manifest/localization.md` | WP-001 |

---

## Strategic Recommendations (Gold Nuggets)

### 1. Dual-Mechanism xUnit Pattern for Singleton-Shared Global State
The combination of `IClassFixture<EnglishLocaleFixture>` (per-class constructor injection) and `[Collection(LocaleSequentialCollection.Name)]` with `DisableParallelization=true` is the correct pattern when tests share a global mutable singleton. `IClassFixture<T>` alone does not prevent parallel test *class* execution — the collection fixture attribute is required to serialize class-level concurrency. This pattern is now documented in both `EnglishLocaleFixture.cs` (XML doc) and `localization.md`.

### 2. `Debug.Assert` as Invariant Documentation, Not Runtime Guard
Inserting `Debug.Assert` at known-invariant callsites (LineNumber always set by `FileService.ReadFileAsync`; ReferenceNumber always set for shifted lines entering Step 6) is valuable in Debug builds for immediate failure signaling, while the existing `?? fallback` defensive code remains active in Release. This is the correct layered-defense strategy for invariants that cannot fail in production but could break silently under future refactors.

### 3. Two-Pointer Pattern for Ascending-Sorted Insertion Attribution
The Step 6b two-pointer optimization demonstrates a general pattern applicable anywhere two ascending-sorted sequences must be joined positionally: pre-sort both sequences, declare a monotonically-advancing index outside the outer loop, and never retreat it. The result is O(k) amortized (vs. O(n²) repeated scans). The implementation is now commented with a block explaining the amortization guarantee and the precondition (insertedEntries sorted ascending by CurrentNumber).

### 4. Locale-Agnostic Test Assertions as a Hard Rule
`ClassifyChangesTests` establishes the correct precedent: all assertions target structural properties (`ChangeType`, `FileName`, `ReferenceNumber`, `CurrentNumber`, `DependentChanges`) with zero `.Text` assertions. Any test added to the diff pipeline coverage going forward should follow this rule — locale fixture isolation is a second line of defense, not a replacement for locale-agnostic assertion design.

### 5. Diagnostic Test README and Trait Filter Documentation
New contributors need to know that `[Trait("Category", "Diagnostic")]` tests are excluded from standard CI runs and must be invoked manually with `--filter Category=Diagnostic`. The new `Tests/LoadOrderKeeper.Tests/README.md` fills this gap and should be kept current as the test project grows.

---

## Deferred & Follow-Up Items

The following items were explicitly flagged during this cycle as out-of-scope, deferred to future work, or debt items for a future cleanup pass.

### Deferred (Intentionally Postponed)

| # | Source | Originating Agent | Description | Priority | Rationale |
|---|--------|-------------------|-------------|----------|-----------|
| D-01 | WP-004 / WP-005 | Developer, QA, Reviewer | `DiffService.cs` Step 2 (L247–248) uses `removed.LineNumber ?? 0` and `matchingNew.LineNumber ?? 0` in `MovedText_Description` without a `Debug.Assert`. Same `FileService.ReadFileAsync` invariant applies. | Medium | Same invariant, different step — consistent assertion pattern not yet complete. Should be addressed in a future `DiffService.cs` cleanup pass. |
| D-02 | WP-004 / WP-005 | Developer, QA, Reviewer | `DiffService.cs` Step 3 `removedByRefPos` sort (L298–305) uses `OrderBy(m => m.LineNumber ?? 0)` without a `Debug.Assert`. Same invariant applies. | Medium | Consistent with D-01. Both gaps documented in `api-surface.md` under "Deferred assertion gaps". |
| D-03 | WP-004 | Reviewer | `DiffService.cs` Step 4 `maxSurvivingRefCurPos` loop (L329–333) uses `current[ci].LineNumber ?? (ci+1)` without an assert. | Low | Same invariant; additional assertion site not yet addressed. |
| D-04 | WP-005 | Reviewer | `DiffService.cs` Step 6b: `nextRemoved` lookup per insertion is still O(r) LINQ (`Where + OrderBy + FirstOrDefault` over `removedEntries`). A binary-search upgrade to O(log r) per insertion is possible since `removedEntries` is already sorted. | Low | WP-005 spec explicitly deferred this — negligible at current mod-list scales. Track for future scalability hardening. |

### Out-of-Scope (Beyond This Plan's Boundaries)

| # | Source | Originating Agent | Description | Priority | Rationale |
|---|--------|-------------------|-------------|----------|-----------|
| O-01 | WP-001 | Developer, QA | `ProfileServiceTests.GenerateProfileId_AccentedCharacters_RemovesAccents` — test input contains garbled characters (Windows-1252 encoding on a UTF-8 system: `'lite-caf'` vs expected `'elite-cafe'`). | Medium | Pre-existing before this plan. Needs a dedicated fix to correct the test source file encoding. |
| O-02 | WP-001 | Reviewer | `DiffServiceTests` and `ProfileServiceTests` are not marked `sealed`, unlike the three coordinator test classes. Minor inconsistency — xUnit test classes typically benefit from `sealed`. | Low | Pre-existing style inconsistency; worth a future cleanup pass for project-wide consistency. |
| O-03 | WP-001 | QA | `LocalizationServiceTests.GetString_ThreadSafe_NoExceptions` uses `Task.WaitAll()` which xUnit1031 flags as a potential deadlock risk. Consider rewriting as `async` with `Task.WhenAll`. | Low | Pre-existing warning, not introduced by this plan. |
| O-04 | WP-001 | Documentation | `file-tree.md` has pre-existing inaccuracies: `DiffServiceTests.cs` and `ProfileServiceTests.cs` were listed under `Services/` but actually reside at the Tests root level. | Low | Out of scope to avoid scope creep; needs a dedicated documentation pass. |
| O-05 | WP-002 | Reviewer | `ReplacementDetectionDiagnostics.cs` VERIFICATION block (L97–109) checks `fragile.ChangeType` twice — once in an if/else for human-readable output, then in `Assert.Equal`. Intentional diagnostic pattern, but could be collapsed to a single Assert with a clear failure message. | Low | Both Developer and QA confirmed this is intentional. No action required unless the file is refactored. |
| O-06 | WP-003 | QA, Reviewer | `ClassifyChangesTests` tests 9c (`SingleInsertion`) and 9j (`InsertionWithDependents`) are structurally identical — same reference/current lists and same assertions. Both intentionally present for WP plan traceability. | Low | A future consolidation pass could merge them into a single parameterized test method without losing scenario coverage. |
| O-07 | WP-003 | Reviewer | `DiffLineModel.DependentChanges` is a public mutable `List<DiffLineModel>`. Callers receiving `DiffLineModel` from `GetPluginsDiffAsync` can mutate `DependentChanges` in place. | Low | Pre-existing design pattern; now documented in `api-surface.md`. A future hardening pass could expose `IReadOnlyList<DiffLineModel>` on the public getter. |
| O-08 | WP-001 | Reviewer | `EnglishLocaleFixture.cs` originally had no XML doc comment (resolved within WP-001 documentation pipeline). Logged for awareness: any new locale-sensitive test class added must follow the dual-mechanism pattern (IClassFixture + Collection attribute). | Low | Resolved within this plan; documented in `localization.md` for future contributors. |

---

## Failed Metrics / Blockers Summary

No blocking failures. The only noteworthy events:

- **WP-001 rework (1 cycle):** QA bounced AC-2 because the initial implementation used a collection fixture pattern (`ICollectionFixture<EnglishLocaleFixture>`) rather than the prescriptive `IClassFixture<EnglishLocaleFixture>` + constructor injection specified in the AC. The Developer reworked to satisfy the literal AC while retaining `LocaleSequentialCollection` for `DisableParallelization=true` grouping. The final architecture is architecturally superior: each class gets its own per-class IClassFixture instance, and the collection ensures sequential execution to protect the singleton.
- **All other WPs:** Passed QA on the first attempt with zero rework.

---

## Next Steps (Recommendations for the Planner)

1. **Fix `ProfileServiceTests.GenerateProfileId_AccentedCharacters_RemovesAccents` (O-01)** — The encoding bug should be addressed first so the test suite runs at 440/440. The fix is mechanical: correct the source file encoding or update the test input to use properly encoded accented characters.

2. **Complete the `Debug.Assert` pattern in `DiffService.cs` (D-01, D-02, D-03)** — Steps 2, 3, and 4 each have `LineNumber ?? fallback` expressions without the corresponding invariant assertion. A small follow-up WP to add three more `Debug.Assert` calls would complete the full-coverage assertion pattern across the classification pipeline.

3. **Consider `DependentChanges` encapsulation (O-07)** — Exposing `IReadOnlyList<DiffLineModel>` on the public getter with a private `List<T>` backing field is a low-risk hardening change that makes the API contract explicit. Could be bundled into a future `DiffLineModel` cleanup WP.

4. **Address `file-tree.md` inaccuracies (O-04)** — A dedicated documentation-only WP to audit and correct `file-tree.md` would improve manifest accuracy, which is important for future Planner agents relying on it for context.

5. **Step 6b `nextRemoved` binary search (D-04)** — Low-priority scalability improvement. Only relevant if mod-list sizes grow significantly beyond current bounds. Defer until a performance concern is observed in practice.
