# Plan

## Plan Audit Cycles
- Audits: 2 — Plan Auditor v1.6.0
- Architectural Reviews: 2 — Plan Architect Reviewer v2.1.0

## Summary

Replace the brittle position-based diff heuristics in `DiffService` and `FileService` with a Longest Common Subsequence (LCS) based pipeline. The current system builds dictionaries of mod positions and compares them independently — every position change is treated equally, making it impossible to distinguish a deliberate reorder from a cascading side-effect of an insertion or deletion. LCS alignment identifies the "stable spine" of mods that maintained their relative order, providing a mathematically precise foundation for change classification, replacement detection, and dependent-change attribution. The refactor consolidates the two-layer system (`FileService.GetModDiffInternalAsync` → `DiffService.GetPluginsDiffAsync`) into a single coherent pipeline while preserving the exact output contract (`IReadOnlyList<DiffLineModel>`) and all existing consumer behavior.

## Architectural Context

The diff system currently spans two static services:

1. **`FileService.GetModDiffInternalAsync`** (`Services/FileService.cs` L148–L205) reads reference and current mod lists, builds dictionaries keyed by filename, unions the keys, and produces `List<ModDiffModel>` with `ReferenceNumber`/`CurrentNumber` positions. The `ModDiffModel` computed properties (`IsNew`, `IsRemoved`, `IsMoved`) are purely position-based.

2. **`DiffService.GetPluginsDiffAsync`** (`Services/DiffService.cs` L10–L98) consumes the `ModDiffModel` list and builds `List<DiffLineModel>` using three heuristic passes:
   - `DetectReplacements` — two-pass position matching (exact then shift-corrected)
   - Main loop — classifies Added vs. Inserted using `maxReferenceNumber`
   - `DetectAndAssignDependentChanges` — range-scanning to group cascading moves under their causal insertion/deletion

The `WouldSortingChangeDiffsAsync` method (`Services/FileService.cs` L207–L212) compares normal vs. reference-aligned diffs to determine if sorting would help. It uses `AlignCurrentModsWithReference` which reorders current mods to match reference order — this is orthogonal to the diff algorithm and must be preserved.

## Approach / Architecture

Introduce a custom LCS computation as a private method within `DiffService`, then rebuild the classification pipeline on top of the LCS alignment. The pipeline flows:

```
1. Read reference[] and current[] (existing FileService.ReadFileAsync)
2. Compute LCS(reference, current) → stable spine (paired indices)
3. Classify each item:
   - In LCS → Unchanged (same relative order in both lists)
   - In reference only → Removed
   - In current only → candidate New (Added/Inserted/Replacement/Moved)
4. Reconcile same-filename pairs (Moved detection):
   - A mod that appears in both "Removed" and "New" sets under the same
     filename was deliberately reordered (swapped); LCS dropped it from
     the stable spine because its relative order inverted. Pull both
     entries from their respective sets and emit as Moved.
5. Detect replacements:
   - A Removed item at reference position R paired with a new item at the
     same aligned position → Replaced
6. Classify Added vs. Inserted:
   - New item whose current position is beyond all surviving reference
     mods → Added
   - New item whose current position is within the range of surviving
     reference mods → Inserted
7. Attribute dependent changes:
   - LCS items whose absolute positions differ (but relative order is
     preserved) are "shifted" — group them under the causal
     insertion/deletion
8. Return List<DiffLineModel> (same output contract)
```

The key insight: items in the LCS are, by definition, in the same relative order in both lists. If their absolute positions differ, it's _solely_ because of insertions or deletions between them — making dependent-change detection trivial.

### Replacement Detection with LCS

The current two-pass `DetectReplacements` uses position matching with shift correction. With LCS, replacement detection improves naturally:

1. Build the LCS to identify unchanged mods (the stable spine).
2. Identify removed mods (in reference, not in current) and new mods (in current, not in reference).
3. Reconcile same-filename pairs: before replacement matching, iterate the removed set; any removed mod whose filename also appears in the new set is a reordered (swapped) mod — not a removal or replacement candidate. Remove both from their sets and emit Moved.
4. For replacement matching: walk through reference positions sequentially. When a removed mod at reference position R has a new mod at the corresponding current-list position (after accounting for the LCS alignment), pair them as a replacement.

This replaces the two-pass shift-correction heuristic because LCS alignment inherently accounts for all position shifts.

### WouldSortingChangeDiffsAsync Preservation

`FileService.WouldSortingChangeDiffsAsync` calls `GetModDiffInternalAsync` twice (normal and aligned) and compares via `DiffSequencesEqual`. This logic is about sorting simulation, not change classification. It will be preserved as-is in `FileService`. The `GetModDiffInternalAsync` method will be retained (simplified if possible) solely for `WouldSortingChangeDiffsAsync` — it is no longer called by `DiffService`.

## Rationale

1. **The brittleness stems from no sequence alignment.** The current code compares positions independently. A mod that moved because it was deliberately reordered looks the same as one that shifted because a neighbor was deleted. LCS solves this by identifying which items maintained their relative order.

2. **A custom implementation is preferable to a library.** The inputs are two lists of 10–500 unique, case-insensitive strings. A textbook DP-based LCS is ~40 lines. The classification layer is domain-specific regardless of implementation. Adding DiffLib saves ~40 lines but introduces a dependency with a custom license and an API whose `Replace` semantics don't match the domain.

3. **The existing test suite provides a safety net.** 15+ unit tests and 16 scenario tests with documented expectations validate the refactor immediately.

4. **Dependent-change detection becomes trivial.** Given LCS alignment, shifted items are those in the LCS whose absolute positions differ — the current 80-line range-scanning heuristic reduces to ~20 lines of straightforward logic.

## Considered Alternatives

| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| LCS implementation | Custom DP (~40 lines) | DiffLib NuGet package | Custom avoids a dependency with a non-standard license, gives full control over the domain-specific classification, and the algorithm is trivial for lists of ≤500 unique strings. |
| Pipeline architecture | Single unified pipeline in DiffService | Keep two-layer (FileService → DiffService) | Unifying eliminates the intermediate `ModDiffModel` transformation and the conceptual split between "raw diff" and "classified diff." The two-layer split added complexity without value. |
| ModDiffModel fate | Retain for `WouldSortingChangeDiffsAsync` | Remove entirely | `WouldSortingChangeDiffsAsync` and `FileServiceTests` still use `ModDiffModel` via `GetModDiffAsync`. Removing it would require reworking the sorting simulation, which is out of scope. |
| Replacement detection | LCS-aligned position matching | Keep two-pass shift heuristic | LCS alignment inherently accounts for position shifts, eliminating the fragile shift-correction second pass. |
| FileService access method | Thin public wrapper `ReadModListAsync` | Change `ReadFileAsync` to `internal` | Wrapper keeps `ReadFileAsync` private and aligns with the project's convention of named public cross-service APIs; `internal` would expose the implementation method to any future assembly-internal caller without an explicit contract. |
| Same-filename reconciliation | Reconcile before replacement detection | No reconciliation (rely solely on replacement detection) | Without reconciliation, swapped mods (same filename, inverted relative order) fall through all classification branches as `Removed + Added`; the reconciliation pass (~10 lines) correctly emits `Moved` before replacement detection runs on the remaining truly-removed and truly-new items. |

## Pattern Alignment

- **Static service pattern** — `Services/DiffService.cs` — follows existing convention; no departure.
- **Localization via `LocalizationService.Instance.GetString()`** — `Services/DiffService.cs` L8 — follows existing convention; no departure.
- **`DiffLineModel` output contract** — `Models/DiffLineModel.cs` — follows existing convention; no departure.
- **Test infrastructure with `TestConfigContext`** — `Tests/LoadOrderKeeper.Tests/` — follows existing convention; no departure.
- **`ModDiffModel` intermediate format** — `Models/ModDiffModel.cs` — partial departure: `DiffService` will no longer consume `ModDiffModel`. It is retained for `FileService.GetModDiffAsync` / `WouldSortingChangeDiffsAsync` backward compatibility. No code changes to `ModDiffModel` itself.

## Detailed Steps

### Step 1: Add LCS Computation Method to DiffService

Add a private static method `ComputeLcs` to `Services/DiffService.cs`:

```csharp
private static List<(int refIndex, int curIndex)> ComputeLcs(
    IReadOnlyList<string> reference,
    IReadOnlyList<string> current,
    StringComparer comparer)
```

Implementation: standard DP-based LCS algorithm. Build a 2D table of size `(reference.Count + 1) × (current.Count + 1)`, then backtrack to extract the paired indices. Returns a list of `(refIndex, curIndex)` tuples representing the longest common subsequence — items that appear in both lists in the same relative order.

The method accepts `StringComparer` to support case-insensitive comparison (`StringComparer.OrdinalIgnoreCase`).

### Step 2: Add LCS-Based Classification Method to DiffService

Add a private static method `ClassifyChanges` to `Services/DiffService.cs`:

```csharp
private static List<DiffLineModel> ClassifyChanges(
    IReadOnlyList<ModEntryModel> referenceMods,
    IReadOnlyList<ModEntryModel> currentMods,
    List<(int refIndex, int curIndex)> lcs)
```

This method:

1. Builds a set of LCS reference indices and LCS current indices from the `lcs` result.
2. Identifies **removed mods**: items in reference whose index is not in the LCS reference index set.
3. Identifies **new mods**: items in current whose index is not in the LCS current index set.
4. Reconciles **same-filename pairs (Moved)**: iterates the removed set; if a removed mod's filename also appears in the new set, both entries are removed from their respective sets and a `Moved` `DiffLineModel` is emitted for that mod. This handles deliberately reordered mods (e.g., swaps) that the LCS drops from the stable spine because their relative order inverted. Without this pass, swap scenarios (Scenarios 2, 10, 14) would produce incorrect `Removed + Added` pairs instead of `Moved`.
5. Detects **replacements**: walks reference positions sequentially. For each removed mod at reference position R, checks if there is a new mod at the "aligned current position." Alignment is computed by finding the nearest LCS neighbors — the current-list gap between two consecutive LCS entries corresponds to the reference-list gap. A removed mod paired with a new mod in the corresponding gap is a replacement.
6. Classifies remaining new mods as **Added** (current position > max current position of any LCS item) or **Inserted** (within the range of LCS items).
7. Identifies **moved/dependent mods**: LCS items whose absolute positions differ (`referenceMods[refIdx].LineNumber != currentMods[curIdx].LineNumber`). These are items in the same relative order but shifted by insertions/deletions.
8. Groups dependent mods under their causal change (the nearest preceding insertion or deletion in reference-position order).
9. Constructs `DiffLineModel` entries with localized text using the existing `_localization.GetString()` pattern.

### Step 3: Refactor `GetPluginsDiffAsync` to Use LCS Pipeline

Modify `Services/DiffService.cs` `GetPluginsDiffAsync`:

1. **Replace** the call to `FileService.GetModDiffAsync(config)` with direct calls to `FileService.ReadModListAsync` for each file path to read the reference and current mod lists.
2. Extract filename arrays from the `ModEntryModel` lists.
3. Call `ComputeLcs(referenceNames, currentNames, StringComparer.OrdinalIgnoreCase)`.
4. Call `ClassifyChanges(referenceMods, currentMods, lcs)` — this includes the same-filename reconciliation pass (Moved detection) before replacement detection.
5. **Remove** the calls to `DetectReplacements` and `DetectAndAssignDependentChanges`.
6. Return the classified result.

### Step 4: Add `ReadModListAsync` Wrapper to FileService

Add a thin public wrapper method to `Services/FileService.cs`:

```csharp
public static Task<IReadOnlyList<ModEntryModel>> ReadModListAsync(string filePath, bool isReferenceFile = false)
```

This delegates to the existing private `ReadFileAsync`. Keeping `ReadFileAsync` private and exposing a named public method aligns with the project's convention where all cross-service calls go through public static methods (`DoesReferenceFileExist`, `CreateReferenceFileAsync`, `ApplyLoadOrderAsync`, etc.). `DiffService` calls `FileService.ReadModListAsync` directly — no visibility change to `ReadFileAsync` is required.

### Step 5: Remove Obsolete Private Methods from DiffService

Remove from `Services/DiffService.cs`:
- `DetectReplacements` (L250–L300) — replaced by LCS-aligned replacement detection in `ClassifyChanges`
- `DetectAndAssignDependentChanges` (L141–L247) — replaced by LCS-based dependent-change attribution in `ClassifyChanges`

### Step 6: Preserve `HasIndependentMovedModsAsync`

`HasIndependentMovedModsAsync` calls `GetPluginsDiffAsync` and inspects the result for Moved mods not in any DependentChanges list. Since the output contract is preserved, this method requires **no changes**. Verify it continues to work by running the existing tests.

### Step 7: Preserve `FileService.GetModDiffAsync` and `WouldSortingChangeDiffsAsync`

`FileService.GetModDiffAsync`, `GetModDiffInternalAsync`, `AlignCurrentModsWithReference`, `DiffSequencesEqual`, and `WouldSortingChangeDiffsAsync` remain unchanged. They serve the sorting simulation use case and the `FileServiceTests`. `DiffService` no longer calls `GetModDiffAsync` — the methods are now only used by `WouldSortingChangeDiffsAsync` and tests.

### Step 8: Update Tests for Internal Changes

- **`Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs`**: All tests call `DiffService.GetPluginsDiffAsync()` and assert on `DiffLineModel` output. Since the output contract is preserved, most tests should pass without modification. Review each test to ensure behavioral expectations still hold with LCS-based classification.
- **`Tests/LoadOrderKeeper.Tests/ScenarioTests.cs`**: Same — all 16 scenarios test via `DiffService.GetPluginsDiffAsync()`. Scenario 16 (`ReplacementWithPositionShift`) has a dual IDEAL/FALLBACK assertion structure; with LCS, the IDEAL path (replacement detected) should always succeed. Update this test to remove the FALLBACK branch and assert the IDEAL behavior exclusively.
- **`Tests/LoadOrderKeeper.Tests/FileServiceTests.cs`**: Tests for `GetModDiffAsync` are unaffected since `FileService` methods are preserved. Tests for `WouldSortingChangeDiffsAsync` are also unaffected.
- **`Tests/LoadOrderKeeper.Tests/ReplacementDetectionDiagnostics.cs`**: This diagnostic test calls `FileService.GetModDiffAsync` — unaffected.
- **`Tests/LoadOrderKeeper.Tests/DetectReplacementsTests.cs`**: This file calls `DetectReplacements` via reflection (`BindingFlags.NonPublic | Static`) and asserts the method is not null. Since Step 5 removes `DetectReplacements`, this reflection assertion fails immediately with a hard `Assert.NotNull` failure. **Delete this file.** Replacement detection behavior is validated through `ClassifyChanges` pipeline output in `ScenarioTests`.

### Step 9: Verify All Consumers

Run the full test suite and verify no consumer is broken:
- `ViewModels/DiffDialogViewModel.cs` — calls `GetPluginsDiffAsync`, consumes `IReadOnlyList<DiffLineModel>`
- `ViewModels/MainViewModel.cs` — calls `GetPluginsDiffAsync`, consumes `IReadOnlyList<DiffLineModel>`
- `Coordinators/FileMonitoringCoordinator.cs` — calls `HasIndependentMovedModsAsync`
- `Services/WindowLifecycleService.cs` — calls `GetPluginsDiffAsync`
- `Services/ErrorLoggingService.cs` — receives `IReadOnlyList<DiffLineModel>`
- `Services/DebugStateService.cs` — receives `IReadOnlyList<DiffLineModel>`

### Step 10: Update Manifest Documentation

Update the following manifest documents to reflect the architectural change:
- `Docs/Agents/project-manifest/data-flows.md` — update the diff flow description to mention LCS-based pipeline
- `Docs/Agents/project-manifest/api-surface.md` — add `FileService.ReadModListAsync` (new public method) to the `FileService` section; add `HasIndependentMovedModsAsync` (pre-existing public method, pre-existing gap) to the `DiffService` section

## Dependencies

- Step 2 depends on Step 1 (LCS computation)
- Step 3 depends on Steps 1, 2, and 4 (pipeline refactor needs LCS, classification, and file access)
- Step 5 depends on Step 3 (remove old methods only after new pipeline is in place)
- Step 8 depends on Steps 3–5 (test updates after code changes)
- Step 9 depends on Steps 3–8 (full verification)
- Step 10 depends on Steps 3–5 (documentation reflects final state)
- Steps 6 and 7 are verification-only, can run in parallel with Steps 8–9

## Required Components

- `Services/DiffService.cs` — major modification
- `Services/FileService.cs` — minor modification (add public wrapper `ReadModListAsync`)
- `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs` — review and potential minor updates
- `Tests/LoadOrderKeeper.Tests/ScenarioTests.cs` — update Scenario 16 to remove FALLBACK branch
- `Tests/LoadOrderKeeper.Tests/DetectReplacementsTests.cs` — delete (reflection-based test for removed method)
- `Docs/Agents/project-manifest/data-flows.md` — documentation update
- `Docs/Agents/project-manifest/api-surface.md` — documentation update

## Assumptions

- The LCS of two mod lists with unique, case-insensitive filenames will always produce the correct "stable spine" — this is guaranteed by the LCS algorithm for sequences with unique elements.
- The existing test suite provides sufficient coverage to validate the refactor. No scenarios are missing from the 16 documented cases.
- The replacement detection heuristic (removed + new mod in aligned gap) will match or exceed the accuracy of the current two-pass approach for all documented scenarios.
- `ModDiffModel` and `FileService.GetModDiffAsync` remain useful for `WouldSortingChangeDiffsAsync` and do not need to be changed.

## Constraints

- The output contract `IReadOnlyList<DiffLineModel>` must not change — all consumers depend on it.
- `DiffChangeType` enum values must not change — UI code maps these to colors and prefixes.
- `DiffLineModel.DependentChanges` behavior must be preserved — dependents are nested under their causal change and removed from the top-level list.
- Case-insensitive comparison (`StringComparer.OrdinalIgnoreCase`) must be used throughout.
- All localization strings must use `_localization.GetString()` — no hardcoded UI strings.
- UTF-8 without BOM for all file operations.

## Out of Scope

- Changes to `FileService.ApplyLoadOrderAsync` (sorting logic) — orthogonal to diff algorithm.
- Changes to `FileService.WouldSortingChangeDiffsAsync` or `AlignCurrentModsWithReference` — sorting simulation is preserved as-is.
- UI changes to `DiffWindow` or `DiffDialogViewModel` — the output contract is unchanged.
- New `DiffChangeType` values or new diff capabilities beyond what the current system supports.
- Performance optimization — mod lists are 10–500 items; DP-based LCS is $O(n \cdot m)$ which is ≤250,000 operations, negligible.
- Removing `ModDiffModel` or `FileService.GetModDiffAsync` — still used by sorting simulation and tests.

## Acceptance Criteria

- AC-01: The LCS computation method correctly identifies the longest common subsequence for two lists of case-insensitive unique strings.
- AC-02: `DiffService.GetPluginsDiffAsync` produces the same `IReadOnlyList<DiffLineModel>` output for all 16 documented scenarios as the current implementation (or improved results where the current implementation is known to be brittle, e.g., Scenario 16).
- AC-03: Replacement detection works correctly when position shifts occur due to earlier deletions (Scenario 16 IDEAL path always succeeds).
- AC-04: Dependent-change attribution correctly groups cascading position shifts under their causal insertion or deletion.
- AC-05: `HasIndependentMovedModsAsync` continues to correctly distinguish independent moves from dependent ones.
- AC-06: `WouldSortingChangeDiffsAsync` behavior is unaffected.
- AC-07: All existing unit tests pass (DiffServiceTests, ScenarioTests, FileServiceTests).
- AC-08: No consumer code changes are required — all consumers compile and function identically.
- AC-09: The obsolete `DetectReplacements` and `DetectAndAssignDependentChanges` methods are removed.
- AC-10: Manifest documentation (`data-flows.md`, `api-surface.md`) reflects the new architecture.
- AC-11: Swap and reorder scenarios (Scenarios 2, 10, 14) produce `Moved` `DiffLineModel` entries rather than `Removed + Added` pairs.

## Testing Strategy

The existing test suite is the primary validation mechanism. With 15+ DiffService unit tests and 16 comprehensive scenario tests, the refactor is well-covered. The approach is:

1. **Keep all existing tests as-is** (except Scenario 16 FALLBACK removal) — they test behavioral output, not implementation details.
2. **Add targeted LCS unit tests** for the new `ComputeLcs` method to validate the algorithm independently.
3. **Run the full suite after each step** to catch regressions early.
4. **Strengthen Scenario 16** by removing the FALLBACK branch and asserting only the IDEAL path.

## Test Plan

- New test: `DiffServiceTests.ComputeLcs_ReturnsCorrectLcs_ForIdenticalLists` — verifies LCS = full list when both lists are identical — covers AC-01
- New test: `DiffServiceTests.ComputeLcs_ReturnsCorrectLcs_ForDisjointLists` — verifies LCS = empty when no common elements — covers AC-01
- New test: `DiffServiceTests.ComputeLcs_ReturnsCorrectLcs_ForPartialOverlap` — verifies LCS with insertions, deletions, and moves — covers AC-01
- New test: `DiffServiceTests.ComputeLcs_IsCaseInsensitive` — verifies case-insensitive matching — covers AC-01
- Existing: `DiffServiceTests` (all 15+ tests) — unchanged, verify AC-02, AC-04, AC-05, AC-07
- Existing: `ScenarioTests` (scenarios 01–15) — unchanged, verify AC-02, AC-07, AC-11 (Scenarios 02, 10, 14 specifically verify Moved detection for swapped mods)
- Modified: `ScenarioTests.Scenario16_ReplacementWithPositionShift_DetectsReplacement` — remove FALLBACK branch, assert only IDEAL behavior — verifies AC-03
- Delete: `Tests/LoadOrderKeeper.Tests/DetectReplacementsTests.cs` — file must be removed; `DetectReplacements` is deleted in Step 5 and the reflection-based `Assert.NotNull(method)` would fail immediately — covers AC-09
- Existing: `FileServiceTests` (all tests) — unchanged, verify AC-06, AC-07

## Documentation Updates

- `Docs/Agents/project-manifest/data-flows.md` — update the diff/monitoring flow description to reference LCS-based classification instead of position-based heuristics
- `Docs/Agents/project-manifest/api-surface.md` — add `FileService.ReadModListAsync` (new public method) to the `FileService` section; add `HasIndependentMovedModsAsync` (pre-existing public method, pre-existing gap) to the `DiffService` section

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **LCS alignment produces different dependent-change groupings than current heuristics, causing test failures** | Run tests incrementally after each step. The LCS-based grouping should be more correct, not different — if a test fails, the test expectation may need updating with documentation of why the new behavior is more accurate. |
| **Replacement detection differs from current heuristic in edge cases** | Scenario 16 explicitly tests this. The LCS approach should handle shift-corrected replacements more naturally. If unexpected mismatches occur, the replacement-pairing logic in `ClassifyChanges` can be tuned without affecting the LCS core. |
| **`ReadModListAsync` wrapper expands the public `FileService` surface** | The wrapper is three lines and delegates unchanged to the private `ReadFileAsync`. A named public method provides a stable contract and communicates intent clearly; it does not duplicate logic. This is consistent with the existing public API surface of `FileService`. |
| **Scenario 06 (combined changes) has loose assertions due to known current-implementation limitations** | Review Scenario 06 assertions after refactor. The LCS-based pipeline may correctly detect the replacement that the current heuristic misses — update test expectations accordingly. |
