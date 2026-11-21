## Synthesis

### Completion Status
- Date: 2026-07-23
- Status: COMPLETE
- Completed by: Standalone Developer Agent
- Archived in Ledger: 2026-07-23

### Outcome Summary

Implemented a display-layer toggle for the Diff window that defaults to a changes-only view and can switch to full context on demand. The implementation keeps `DiffLines` as the authoritative unfiltered source and introduces a filtered `ICollectionView` for UI rendering, so existing command and diff logic remains stable. Localization and manifest updates were completed in the same change set to preserve architecture invariants.

### Implementation Summary
- Added `ShowAllMods` state and `FilteredDiffLines` (`ListCollectionView`) in `DiffDialogViewModel` with a filter that hides `Unchanged` and `Separator` items when unchecked.
- Updated scroll targeting so the first changed row is selected correctly in both filtered and full-context modes.
- Added `Show all mods` checkbox to `DiffWindow.xaml` and rebound the list to `FilteredDiffLines`.
- Added `ShowAllModsToggleText` to `DiffDialogTexts` and all locale files.
- Updated manifest docs for API surface, Diff window behavior, and localization string totals.

### Documentation Updates
- Updated `Docs/Agents/project-manifest/api-surface.md` to include `ShowAllMods`, `FilteredDiffLines`, and `ShowAllModsToggleText` on `DiffDialogViewModel`.
- Updated `Docs/Agents/project-manifest/ui-design.md` to document default changes-only behavior and the full-context toggle.
- Updated `Docs/Agents/project-manifest/localization.md` to reflect the additional localized key.

### Verification Summary
- Tests run: `dotnet test "Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj" --configuration Debug`
- Static analysis run: `get_errors` on modified ViewModel/XAML/text files (clean)
- Build run: `dotnet build "Starfield Load Order Keeper.sln" --configuration Debug`
- Result: PASS (existing warnings remain outside this scope)

### Code Insights
- [medium] (debt) `ViewModels/DiffDialogViewModel.cs`: ~~`OnFileChangeDetected` still sets hardcoded English status strings (`"Detected changes"`, `"Plugins.txt now matches the reference"`) instead of using localized text resources. Suggested follow-up: move these messages into `DiffDialog` locale keys and reference through `DiffDialogTexts`.~~ **DONE:** All texts are now correctly translated.
- [low] (improvement) `ViewModels/DiffDialogViewModel.cs`: ~~`ReplaceDiffLines` still raises `OnPropertyChanged(nameof(DiffLines))` after mutating the existing collection instance. Suggested follow-up: remove this notification if no bindings require collection reference rebinding.~~ **DONE:** The redundant property-change notification was removed; the view is bound to `FilteredDiffLines`.

### Additional Comments
- No service-layer changes were made; `DiffService` context generation remains intact and is only hidden/shown via ViewModel filtering.
