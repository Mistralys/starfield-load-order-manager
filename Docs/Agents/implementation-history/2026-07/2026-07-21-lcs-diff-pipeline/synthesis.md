# Synthesis Report — LCS Diff Pipeline

**Plan:** `2026-07-21-lcs-diff-pipeline`
**Date:** 2026-07-21
**Status:** COMPLETE — 3 / 3 work packages delivered

---

## Executive Summary

This plan replaced the brittle position-based diff heuristics in `DiffService` and `FileService` with a mathematically precise Longest Common Subsequence (LCS) pipeline. A custom O(mn) DP-backtrack `ComputeLcs` method identifies the stable spine of mods that preserved their relative order, and a new six-step `ClassifyChanges` method builds atop that spine to classify every mod change as Moved, Replaced, Inserted, Added, Removed, or a dependent shift — eliminating the fragile position-dictionary approach and the two-pass shift-correction heuristic that preceded it.

The refactor was delivered in three work packages: WP-001 introduced the two new internal static methods and their unit tests; WP-002 wired the pipeline into the live `GetPluginsDiffAsync` entrypoint, removed the legacy `DetectReplacements` and `DetectAndAssignDependentChanges` methods, and validated all 16 scenario tests; WP-003 completed the documentation record. All 11 functional acceptance criteria were met, the `IReadOnlyList<DiffLineModel>` output contract is unchanged, and no consumer code required modification.

---

## Metrics

| Work Package | Implementation | QA | Code Review | Documentation | Tests Pass | Tests Fail |
|---|---|---|---|---|---|---|
| WP-001 | PASS | PASS | PASS | PASS | 407 | 24 (pre-existing) |
| WP-002 | PASS | PASS | PASS | PASS | 406 | 24 (pre-existing) |
| WP-003 | — | — | — | PASS | — | — |

- **Total pipeline stages executed:** 13
- **Rework cycles:** 0 on all WPs
- **Pre-existing test failures:** 24 (French locale `fr-FR` string mismatches and `UpdateCheckCoordinator` reflection issues) — confirmed unrelated to this plan and present before and after the work
- **Build warnings:** 0 (clean build throughout)
- **Correctness bugs found and fixed during integration (WP-002):** 2 (see Strategic Recommendations)

---

## Files Modified

| File | Work Package(s) |
|---|---|
| `Services/DiffService.cs` | WP-001, WP-002 |
| `Services/FileService.cs` | WP-002 |
| `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs` | WP-001 |
| `Tests/LoadOrderKeeper.Tests/ScenarioTests.cs` | WP-002 |
| `Tests/LoadOrderKeeper.Tests/DetectReplacementsTests.cs` | WP-002 (deleted) |
| `Tests/LoadOrderKeeper.Tests/ReplacementDetectionDiagnostics.cs` | WP-002, WP-002 Doc |
| `Starfield Load Order Keeper.csproj` | WP-001 |
| `Docs/Agents/project-manifest/api-surface.md` | WP-001 Doc, WP-002 Doc, WP-003 |
| `Docs/Agents/project-manifest/tech-stack.md` | WP-001 Doc |
| `Docs/Agents/project-manifest/data-flows.md` | WP-003 |

---

## Strategic Recommendations ("Gold Nuggets")

### 1. Two Correctness Bugs Were Found and Fixed During WP-002 Integration

The WP-001 implementation of `ClassifyChanges` contained two bugs that only manifested under integration conditions:

- **Step 3 — Replacement counter over-increment:** `cumulativeDeletions` was incremented for every removed item, including replacements. This caused the second and subsequent consecutive replacements (e.g. Scenario 09) to receive an incorrect aligned position and be missed. Fixed: the counter now only increments for true (unmatched) removals.
- **Step 6 — Greedy causal attribution:** Dependent-change attribution used a greedy "first causal match" loop that assigned all shifted LCS items to the first removal they followed. Fixed: replaced with a range-based strategy that mirrors the removed `DetectAndAssignDependentChanges` logic — each removal owns the shifted items between its reference position and the next removal.

**Takeaway:** LCS-based classification logic with multiple interdependent steps benefits from end-to-end integration testing (all 16 scenarios) as part of WP-001 or alongside the first unit tests. The bugs were invisible to the isolated `ClassifyChanges` unit tests added in WP-001 because they require the realistic interplay of multiple classification steps.

### 2. `ClassifyChanges` Still Lacks Isolated Unit Tests

All classification logic is currently covered only through the end-to-end `GetPluginsDiffAsync` path (ScenarioTests and DiffServiceTests). The Reviewer flagged this in WP-001 as a documentation-forward item, and QA identified it as a coverage gap. Dedicated unit tests for `ClassifyChanges` covering at minimum Moved, Replaced, Inserted/Added, and dependent-change grouping would catch regressions much earlier and provide live documentation of the expected behavior of each step.

### 3. LineNumber Fallback Inconsistency Between Classification Steps

`ClassifyChanges` Step 1 uses `ri + 1` as the null-`LineNumber` fallback when building `lcsShifted`, while Step 3 uses `0` as a sentinel that excludes entries from replacement matching. In practice `ReadFileAsync` always assigns `LineNumber`, so this is latent-only — but the two strategies are inconsistent and could cause subtle failures if `ClassifyChanges` is ever called from a different code path. Align both steps to the same strategy in a follow-up.

### 4. O(n²) Inner LINQ in Step 6b Insertion Attribution

`ClassifyChanges` Step 6b performs `.Where(...).OrderBy(...)` inside a `foreach(inserted)` loop — O(k log k) per inserted entry, O(k²) overall for `k` shifted items. At current mod list sizes (hundreds) this is not measurable. If mod list sizes grow significantly, a two-pointer or sorted-index approach would reduce this to O(k).

### 5. `ReplacementDetectionDiagnostics.cs` — Historical Artifact Retained

The file was originally a trace of the old two-pass replacement detection algorithm. Its behavioral assertion (`Assert.Equal Replaced`) still passes against the LCS pipeline and now serves as a regression check. The class-level narrative was updated in WP-002 to label the inline simulation section as a historical reference. However, the simulation code still describes the old heuristic rather than the new pipeline. A future cleanup could either rewrite the simulation to trace the LCS path or remove it entirely, keeping only the behavioral assertion.

### 6. `HasIndependentMovedModsAsync` Was Missing from the API Surface

The method at `DiffService.cs` L50, called by `FileMonitoringCoordinator`, was absent from `api-surface.md` before this plan. It was added during WP-003 as a pre-existing gap fix. A periodic audit of the API surface manifest against the actual public API would catch similar gaps earlier.

---

## Deferred & Follow-Up Items

| # | Source | Agent | Type | Description | Priority |
|---|---|---|---|---|---|
| 1 | WP-001 QA | QA | **Follow-up** | Add isolated unit tests for `ClassifyChanges` covering each classification step independently (Moved, Replaced, Inserted, Added, dependent grouping). Currently tested only via end-to-end `GetPluginsDiffAsync`. | Medium |
| 2 | WP-002 Reviewer | Reviewer | **Follow-up** | Align `ClassifyChanges` Step 1 (`ri + 1` fallback) and Step 3 (`0` sentinel) to a consistent `null`-`LineNumber` strategy. Latent only — `ReadFileAsync` always assigns `LineNumber`. | Low |
| 3 | WP-002 Reviewer | Reviewer | **Follow-up** | Replace O(n²) inner LINQ in `ClassifyChanges` Step 6b insertion attribution with a two-pointer or sorted-index approach. Not measurable at current scale, but should be addressed if mod list sizes grow. | Low |
| 4 | WP-002 Reviewer | Reviewer | **Follow-up** | Remove the vacuous `.Where(s => s.ReferenceNumber.HasValue)` filter on `shiftedByRefPos` in Step 6 (the invariant guarantees it is always true). Replace with a `Debug.Assert` to make the invariant explicit, or simply remove the filter. | Low |
| 5 | WP-002 Developer | Developer | **Follow-up** | Update or remove `ReplacementDetectionDiagnostics.cs` inline simulation section. The simulation describes the removed two-pass heuristic. Its behavioral assertion passes, but the narrative is stale. Options: rewrite to trace the LCS path, or remove the simulation and keep only the behavioral assertion. | Low |
| 6 | WP-001 Documentation | Documentation | **Follow-up** | Update `api-surface.md` ClassifyChanges usage-contract note to replace "Pure function — no shared mutable state" with a more precise note once `ClassifyChanges` isolation tests are added (item 1 above). | Low |
| 7 | WP-001 Reviewer | Reviewer | **Out-of-scope** | `InternalsVisibleTo` is set via MSBuild AssemblyAttribute in the `.csproj`. If the project ever adds a dedicated `AssemblyInfo.cs`, the attribute should be moved there for consistency. | Low |

---

## Next Steps for Planner / Manager

1. **ClassifyChanges isolation tests** — Schedule a focused test-coverage work package to add dedicated unit tests for each `ClassifyChanges` classification step. This is the highest-value follow-up item from this plan (see item 1 above). The Reviewer's documentation-forward note and the QA coverage-gap comment both point to this as the right next action.

2. **Pre-existing locale test failures** — 24 test failures on the `fr-FR` system predate this plan and should be addressed in a dedicated localization work package. The `GetPluginsDiffAsync_DetectsReplacements` test specifically should use `Assert.Contains("2", ...)` rather than the English literal `"line 2"`.

3. **`ReplacementDetectionDiagnostics.cs` cleanup** — A small housekeeping work package to either rewrite the simulation narrative to describe the LCS pipeline or remove the simulation section entirely. Low effort, prevents future confusion.

4. **Step 6b performance** — Not urgent, but if the project roadmap ever targets very large mod lists (1000+), the O(n²) inner LINQ in `ClassifyChanges` should be addressed before it becomes measurable.

5. **Next diff system evolution** — The LCS pipeline now provides a clean, well-tested foundation. Future capabilities that become tractable include: detecting batch reorders (multiple mods simultaneously moved as a group), multi-file diffs, and per-change confidence scoring. None of these were in scope for this plan but the architectural foundation supports them.
