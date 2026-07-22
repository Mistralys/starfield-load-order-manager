## Synthesis

### Completion Status
- Date: 2026-07-22
- Status: COMPLETE
- Completed by: Standalone Developer Agent
- Archived in Ledger: 2026-07-22

### Outcome Summary

All five diff GUI enhancement phases were implemented as specified in the plan: semantic brush resources extracted to `Styles/DiffBrushes.xaml`, context lines and separators added to the diff output pipeline, localized causal dependent-change summaries, a split primary/dependent change count displayed in the main window button, and the Inserted warning tooltip moved to the locale system. No algorithm changes were made — all work was confined to the GUI, localization, ViewModel, and coordinator layers, with the new `TrimToContextWindow()` post-processing step applied to the existing LCS output.

### Implementation Summary

- **Semantic Brushes (Phase 1):** Created `Styles/DiffBrushes.xaml` with 11 named `SolidColorBrush` resources covering all change types and both banners. Merged into `App.xaml` alongside existing style dictionaries. Replaced all hardcoded hex colors in `DiffWindow.xaml` with `{DynamicResource}` references.
- **Context Lines & Separators (Phase 2):** Added `DiffChangeType.Separator` enum value. Extended `ClassifyChanges()` to emit `Unchanged` items for non-shifted LCS entries, then apply `TrimToContextWindow()` (contextSize=1) before returning. Added `DataTrigger` blocks in `DiffWindow.xaml` for `Unchanged` (dimmed opacity) and `Separator` (centered `···`, not hit-test visible). Updated `HasDifferences` and `ComputeScrollTargetIndex` in `DiffDialogViewModel` to exclude both `Unchanged` and `Separator` items.
- **Localized Dependent-Change Summary (Phase 3):** Made `DependentChangesSummary` a settable auto-property on `DiffLineModel`; added `DependentChangeCauseFileName` and `DependentChangeCauseAction` properties. During Step 6 of `ClassifyChanges()`, set these properties and format `DependentChangesSummary` using `_localization.GetString()` (singular/plural/generic forms). Added a fallback sweep for any entry with dependents but no cause attribution.
- **Smarter Change Count (Phase 4):** Added `DependentChangeCount` observable property to `FileMonitoringCoordinator`. Updated `UpdateChangeCountDisplayAsync()` to compute separate primary and dependent counts. Added `DependentChangeCount` to `ChangeDetectedEventArgs`. Updated `MainViewModel.UpdateChangeCountDisplay(int changeCount, int dependentChangeCount)` to format `"X changes, +Y affected"` when dependents exist. Added `ShowChangesButtonTextWithDependents` to `MainWindowTexts`.
- **Tooltip Localization (Phase 5):** Added `InsertedWarningTooltip` property to `DiffDialogTexts`, pass-through on `DiffDialogViewModel`, and binding in `DiffWindow.xaml` replacing the hardcoded English string.
- **Localization (Phase 6):** Added 8 new keys to all 8 locale files (en-US, de-DE, fr-FR, es-ES, it-IT, pt-BR, zh-CN, ja-JP). CJK translations follow aki spacing, full-width punctuation, and bracket conventions.
- **Tests (Phase 7):** Added context-line trimming tests and dependent-cause tests. Updated all existing tests that used raw `.Count` or `Assert.Single(result)` on the full diff list to filter `Unchanged`/`Separator` items first. Updated `ScenarioTestBase.AssertChangeCount` to filter before comparing.

### Documentation Updates

- `api-surface.md` — added `DiffChangeType.Separator`, `DiffLineModel.DependentChangeCauseFileName`, `DiffLineModel.DependentChangeCauseAction`, updated `DependentChangesSummary` to `{ get; set; }`, added `FileMonitoringCoordinator.DependentChangeCount`, updated `ChangeDetectedEventArgs`, added `DiffDialogViewModel.InsertedWarningTooltip`.
- `file-tree.md` — added `Styles/DiffBrushes.xaml`.
- `ui-design.md` — added DiffBrush semantic color table, documented context line and separator visual patterns, updated Change Badge description with split count format.
- `constraints.md` — updated "Diff Semantics & Monitoring" section to mention context lines, `Unchanged`/`Separator` filtering requirement, split change count, and causal dependent text.
- `localization.md` — updated total string count from 189 to 198 per locale.

### Verification Summary

- Tests run: `dotnet test Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj --configuration Debug`
- Static analysis run: `dotnet build "Starfield Load Order Keeper.sln" --configuration Debug` (no new errors; pre-existing warnings unchanged)
- Result: 449 passed / 0 failed (including fix for pre-existing `ProfileServiceTests.GenerateProfileId_AccentedCharacters_RemovesAccents` — corrupted UTF-8 bytes in test file replaced with correct `"Élite Café"` literal)

### Code Insights

- [low] (debt) `Tests/LoadOrderKeeper.Tests/ProfileServiceTests.cs`: **FIXED:** ~~`GenerateProfileId_AccentedCharacters_RemovesAccents` had `É` and `é` corrupted to UTF-8 replacement characters (U+FFFD) in the source file, causing the test to always fail. Fixed by replacing the literal with `"\u00c9lite Caf\u00e9"` (i.e., `"Élite Café"`). Root cause was likely a copy-paste or encoding mismatch when the file was originally written.~~
- [low] (refactor) `Services/DiffService.cs` — The "context lines" loop added at the end of `ClassifyChanges()` iterates the LCS a second time. A future optimization could build context items during the existing Step 1 LCS walk, but the current O(LCS size) overhead is negligible for typical mod lists.
- [medium] (improvement) `Services/DiffService.cs` — **FIXED:** ~~Added `Debug.Assert(false, ...)` inside the fallback sweep that fires when any entry has `HasDependentChanges && string.IsNullOrEmpty(DependentChangesSummary)`, surfacing unexpected attribution failures immediately in debug builds.~~
- [low] (convention) `ViewTexts/Locales/zh-CN.json` — **FIXED:** ~~The existing `ShowChangesButtonTextWithCount` lacked aki spacing (`{0}项更改`). Both `ShowChangesButtonTextWithCount` and `ShowChangesButtonTextWithDependents` were written with the correct spacing (`{0} 项更改` / `{0} 项更改，+{1} 项受影响`) during implementation — no further change needed.~~

### Additional Comments

- The `TrimToContextWindow` separator logic uses original list indices (not output list indices) to detect gaps — this is correct and intentional; the gap is measured in terms of how many unchanged items were skipped in the sorted result, which directly maps to "how far apart are the two change groups in the load order."
- All existing scenario tests (16 scenarios via `ScenarioTests.cs`) continue to pass after the `AssertChangeCount` fix in `ScenarioTestBase.cs`.
- The `DependentChangesSummary` property change from a computed getter to a settable auto-property is backward-compatible: the old getter formula `$"+ {DependentChanges.Count} mod positions affected by this change"` is replaced by the generic key `DependentChangesSummary_Generic` which has identical English text, so the display is unchanged for en-US when no cause is attributed.
