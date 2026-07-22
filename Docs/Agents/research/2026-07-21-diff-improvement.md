# Research Report: Improving Load Order Diff Experience

## Problem Statement

The current diff system for comparing load order changes is brittle despite handling a conceptually simple task. The implementation uses position-based heuristics and dictionary lookups to classify changes between a reference mod list and the current `Plugins.txt`. While it works for common cases (add, remove, replace), it produces "strange and limited" results in edge cases — particularly around move detection, dependent change attribution, and combined scenarios. The question: can a proper diff algorithm (possibly via a library) replace the custom heuristics and produce more reliable, understandable results?

## Problem Decomposition

1. **Core diff algorithm**: The current approach compares two ordered lists using position lookups rather than sequence alignment. It has no concept of "longest common subsequence" — every mod that changed position is reported as "moved," even when the move is a cascading side-effect of an insertion or deletion.
2. **Change classification heuristics**: The code distinguishes Added vs. Inserted, detects Replacements by positional adjacency (with a two-pass shift-correction system), and groups "dependent" moves under their causal change. These heuristics are the brittlest part.
3. **Dependent change detection**: The `DetectAndAssignDependentChanges` method uses reference-position ranges to attribute cascading moves to their root cause (a deletion or insertion). This logic is complex (~80 lines) and fragile when multiple changes interact.
4. **UI presentation**: The diff window shows a flat list of `DiffLineModel` items with color-coded change types. This is adequate but could benefit from a clearer visual structure.

## Context & Constraints

- **.NET 9 / WPF** — any library must target .NET Standard 2.0+ or .NET 8/9.
- **Mod lists are 10–500 lines** — performance is not a concern; O(n²) is fine.
- **Case-insensitive** — filenames like `Mod.esm` and `mod.esm` are the same entry.
- **No duplicates** — each mod appears at most once in each list.
- **Existing test suite** — 20+ tests in `DiffServiceTests.cs` and `ScenarioTests.cs` covering 16 documented scenarios. Any refactor must pass these.
- **Existing consumers** — `DiffDialogViewModel`, `FileMonitoringCoordinator`, `DebugStateService`, `ErrorLoggingService` all consume `DiffService.GetPluginsDiffAsync()` output.
- **Current NuGet dependencies** — CommunityToolkit.Mvvm 8.4, Gameloop.Vdf 0.6.2, MaterialDesign 5.3. Adding one focused dependency is acceptable.

## Prior Art & Known Patterns

### Pattern 1: DiffLib (LCS-based generic sequence diff)

- **Description:** .NET library that computes LCS-based diffs on `IList<T>` with pluggable equality and alignment strategies. Produces `DiffElement<T>` items with `Operation` (Match/Insert/Delete/Replace/Modify) and indices from both collections.
- **Where used:** 580K NuGet downloads, actively maintained (v2025.0.0, Jan 2025). Targets .NET 8/9.
- **Strengths:** Generic API works directly on `IList<string>`. Structured output includes original indices from both collections. Pluggable aligners (basic, replace-aware, similarity-based). Battle-tested LCS algorithm.
- **Weaknesses:** No built-in move detection — `DiffOperation` enum has Match/Insert/Delete/Replace/Modify but no Move. Custom license (not MIT/Apache). Moderate adoption.
- **Fit:** Strong foundation. The LCS sections would replace `FileService.GetModDiffInternalAsync()` and eliminate the position-based heuristics. Move detection still needs a post-processing layer, but it would be built on solid alignment data rather than raw position lookups.

### Pattern 2: DiffPlex (Myers diff for text)

- **Description:** Most popular .NET diff library (37.5M downloads). Uses Myers diff algorithm. Includes WPF controls via `DiffPlex.Wpf`.
- **Where used:** Widely adopted, actively maintained (v1.9.0, Sep 2025). .NET Standard 1.0+.
- **Strengths:** Very popular and well-maintained. Includes WPF rendering controls. Fast Myers algorithm.
- **Weaknesses:** **String-only API** — takes `string oldText, string newText`, not collections. You'd join your lists with `\n`, diff, then parse back. No move detection. No generic collection support. The WPF controls render a standard text diff view (side-by-side or inline), not the custom change-type presentation you need.
- **Fit:** Poor. The abstraction level is wrong — this is a text diff tool, not a list diff tool. Wrapping it would add complexity without solving the core problem.

### Pattern 3: Custom LCS Implementation

- **Description:** Implement the classic dynamic programming LCS algorithm (~50-80 lines) directly, then post-process to classify changes.
- **Where used:** Standard algorithm taught in every CS curriculum. Used internally by git, diff utilities, etc.
- **Strengths:** Full control over the algorithm and output model. No external dependency. Can build move detection, replacement detection, and dependent-change logic directly into the pipeline. Tailored to the exact domain (case-insensitive filename comparison).
- **Weaknesses:** Self-maintained algorithm code. Need to handle edge cases. More upfront development than using a library.
- **Fit:** Excellent. For lists of 10-500 unique strings, the DP-based LCS is trivial to implement and gives complete control. The critical insight is that the current brittleness comes from *not using LCS at all* — the current code builds dictionaries and compares positions independently.

### Pattern 4: DiffMatchPatch (Google's character-level diff)

- **Description:** Port of Google's diff-match-patch library. Character-level Myers diff with semantic cleanup.
- **Where used:** 11.9M downloads. Latest version targets .NET 10.
- **Strengths:** Very well-tested algorithm with semantic cleanup heuristics.
- **Weaknesses:** Wrong granularity — operates on characters, not list items. Designed for document synchronization, not structured list comparison.
- **Fit:** Not suitable.

## Alternative & Creative Approaches

### Approach A: LCS + Move Classification Pipeline

Replace the two-layer system (`FileService.GetModDiffAsync` → `DiffService.GetPluginsDiffAsync`) with a single pipeline:

1. **LCS alignment** — find the longest common subsequence between reference and current lists (case-insensitive). This identifies the "stable spine" of unchanged mods.
2. **Classify non-LCS items** — items in reference but not current = Removed. Items in current but not reference = Added/Inserted.
3. **Detect replacements** — a Removed item paired with an Added item at the same aligned position = Replaced.
4. **Classify Added vs. Inserted** — an Added item whose current position is within the range of surviving reference items = Inserted; otherwise = Added.
5. **Attribute dependent changes** — items in the LCS whose positions shifted *only because* of an adjacent insertion or deletion = Dependent. This is natural to compute from the LCS alignment because you can see exactly which items "moved" relative to their LCS neighbors.

- **Rationale:** The LCS alignment gives a mathematically precise answer to "which items stayed in the same relative order" vs. "which items were genuinely reordered." This eliminates the heuristic position-range scanning in the current `DetectAndAssignDependentChanges`.
- **Risk:** The replacement detection heuristic (removed + added at same position) is domain-specific and still needs careful handling. The two-pass shift-correction in `DetectReplacements` would need to be reimplemented.

### Approach B: DiffLib as LCS Engine + Custom Classification

Use DiffLib's `Diff.CalculateSections()` for the LCS computation, then build the classification layer on top of its `DiffElement<T>` output.

- **Rationale:** Avoids implementing LCS from scratch. DiffLib's alignment strategies (especially `BasicReplaceInsertDeleteDiffElementAligner`) could handle replacement detection automatically.
- **Risk:** DiffLib's `Replace` operation pairs adjacent deletes with inserts — this may not match the domain's definition of "replacement" (same position in reference). Would need validation against the 16 documented scenarios.

### Approach C: Keep Current Architecture, Fix Heuristics Only

Don't change the algorithm. Instead, fix specific brittleness:
- Improve `DetectReplacements` shift calculation to handle more edge cases
- Improve `DetectAndAssignDependentChanges` range scanning logic
- Add more test scenarios to cover edge cases

- **Rationale:** Minimally invasive. Preserves existing behavior for known-good cases.
- **Risk:** This is "whack-a-mole" — each fix risks breaking other scenarios. The fundamental issue (no sequence alignment) remains.

## Comparative Evaluation

| Criterion | A: Custom LCS Pipeline | B: DiffLib + Classification | C: Fix Heuristics |
|---|---|---|---|
| **Correctness** | Excellent — LCS is mathematically precise for sequence alignment | Good — LCS via library, but replacement semantics may need tuning | Fair — heuristics remain fragile |
| **Complexity** | Medium — ~100-150 lines for LCS + classification, replacing ~200 lines of current code | Medium — library handles LCS, ~80 lines for classification layer | Low — targeted fixes |
| **Maintainability** | High — single clear pipeline with well-understood algorithm | High — library handles the hard part | Low — heuristics accumulate special cases |
| **Risk** | Low — extensive test suite validates refactor | Medium — library API may not align with domain semantics | Low short-term, high long-term |
| **Dependencies** | None added | DiffLib NuGet package | None |
| **Move detection** | Built-in — LCS naturally identifies what "stayed" vs. what "moved" | Post-processing needed | Current approach preserved |
| **Dependent changes** | Natural — LCS alignment shows exactly which items shifted due to insertions/deletions | Possible but requires same post-processing as current | Current approach preserved (brittle) |
| **Time to implement** | Medium | Medium | Low |

## Recommendation

**Approach A: Custom LCS Pipeline** is the best path forward.

### Rationale

1. **The current brittleness stems from not using sequence alignment.** The code builds dictionaries of positions and compares them independently. This means every position change is treated equally — a mod that moved because it was deliberately reordered looks the same as a mod that shifted because a neighbor was deleted. LCS solves this by identifying the "stable spine" of items that maintained their relative order.

2. **The domain is simple enough that a custom implementation is preferable to a library.** The inputs are two lists of 10-500 unique, case-insensitive strings. A textbook DP-based LCS is ~40 lines. The classification layer on top is domain-specific regardless of whether you use a library. Adding DiffLib saves ~40 lines of LCS code but introduces a dependency with a custom license and an API that doesn't perfectly match the domain (its `Replace` semantics differ from yours).

3. **The existing test suite (20+ tests, 16 scenarios) provides a safety net.** The refactor can be validated immediately against comprehensive, documented expectations.

4. **Dependent change detection becomes trivial with LCS.** Given the LCS alignment, you can compute exactly which items shifted position purely due to an insertion or deletion — these are items that are in the LCS (i.e., present in both lists in the same relative order) but whose absolute positions differ. The current 80-line `DetectAndAssignDependentChanges` with its range-scanning heuristic can be replaced with ~20 lines of LCS-aware logic.

### Architecture Sketch

```
┌─────────────────────────────────────────────────────────┐
│              New DiffService Pipeline                    │
│                                                         │
│  1. Read reference[] and current[] (existing FileService)│
│  2. Compute LCS(reference, current) → stable spine       │
│  3. Classify each item:                                  │
│     - In LCS, same position → Unchanged                  │
│     - In LCS, different position → candidate Moved/Dependent │
│     - In reference only → Removed                        │
│     - In current only → Added or Inserted                │
│  4. Detect replacements:                                 │
│     - Removed + Added at aligned position → Replaced     │
│  5. Attribute dependent changes:                         │
│     - LCS items that shifted position only because of    │
│       an adjacent insertion/deletion → Dependent         │
│  6. Return List<DiffLineModel> (same output contract)    │
└─────────────────────────────────────────────────────────┘
```

### Proof-of-Concept Outline

1. Implement `ComputeLcs(IReadOnlyList<string> a, IReadOnlyList<string> b, StringComparer comparer) → List<(int indexA, int indexB)>` using standard DP.
2. Build classification from LCS result: items in LCS = "stable," items not in LCS = changed.
3. Post-process to detect replacements (removed item whose reference position aligns with an added item's current position).
4. Post-process to detect dependent changes (LCS items whose positions shifted, grouped by the causal insertion/deletion).
5. Run existing test suite. Fix any failures by refining classification logic.
6. Remove `FileService.GetModDiffAsync`, `FileService.GetModDiffInternalAsync`, `DiffService.DetectReplacements`, and `DiffService.DetectAndAssignDependentChanges` — all replaced by the new pipeline.

## Open Questions

- **Replacement detection edge cases:** The current two-pass shift-corrected replacement detection handles scenarios where earlier deletions shift positions. The LCS-based approach should handle this more naturally (since alignment is based on identity, not position), but the 16 scenarios need to be verified.
- **`AlignCurrentToReference` for sorting:** `FileService.GetModDiffInternalAsync` has an `alignCurrentToReference` parameter used by `WouldSortingChangeDiffsAsync`. This sorting simulation logic is orthogonal to the diff algorithm and would need to be preserved or reimplemented.
- **`HasIndependentMovedModsAsync`:** This method in `DiffService` uses the dependent-change classification to determine if sorting would help. The new pipeline must produce equivalent classification for this to work correctly.
- **Performance of DP-based LCS:** For $n = 500$ items, the DP table is $500 \times 500 = 250{,}000$ cells — negligible. No concern.

## References

- DiffLib NuGet: https://www.nuget.org/packages/DiffLib — v2025.0.0, LCS-based generic collection diff
- DiffPlex NuGet: https://www.nuget.org/packages/DiffPlex — v1.9.0, Myers-based text diff
- DiffMatchPatch NuGet: https://www.nuget.org/packages/DiffMatchPatch — v5.0.0, Google's character-level diff
- Myers, E.W. (1986) "An O(ND) Difference Algorithm and Its Variations" — foundational diff algorithm paper
- Wagner-Fischer / LCS dynamic programming — standard textbook algorithm, O(nm) time and space
- Existing test scenarios: `Docs/Agents/Sorting Scenarios/` (16 documented scenarios)
- Existing test suite: `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs` (20+ tests)
