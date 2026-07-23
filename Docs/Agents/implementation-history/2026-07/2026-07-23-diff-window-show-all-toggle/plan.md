# Plan

## Plan Audit Cycles
- Audits: 2 — Plan Auditor v1.7.0
- Architectural Reviews: none — Plan Architect Reviewer v2.2.0

## Summary

The diff window currently shows all mods with context — changed mods at full visibility and unchanged mods grayed out at 45% opacity, with `···` separator items between non-adjacent groups. Although this provides spatial orientation, it creates visual noise that makes it harder to focus on actual changes. This plan adds a "Show all mods" toggle to `DiffWindow`. By default the window shows only actual changes (hiding `Unchanged` and `Separator` items entirely). When the user enables the toggle, all items are shown with unchanged mods grayed out — exactly the current behavior.

---

## Architectural Context

`DiffService.GetPluginsDiffAsync` returns an `IReadOnlyList<DiffLineModel>` that includes changed items, up to 1 `Unchanged` context item above/below each group, and `Separator` items between non-adjacent groups (`TrimToContextWindow`, `contextSize = 1`). The `DiffDialogViewModel` stores this as `ObservableCollection<DiffLineModel> DiffLines` and exposes it directly to `DiffWindow.xaml` via `ItemsSource="{Binding DiffLines}"`. The diff window renders each item in a `ListView`, with `Unchanged` and `Separator` items styled at lower opacity. `ScrollTargetIndex` is computed against the unfiltered `DiffLines` list and used by `DiffWindow.xaml.cs` to scroll the `ListView` to the first changed item via `DiffListView.Items[index]`.

---

## Approach / Architecture

Introduce a `ListCollectionView FilteredDiffLines` property on `DiffDialogViewModel` that wraps the existing `DiffLines` collection and applies a filter predicate. The filter hides `Unchanged` and `Separator` items when `ShowAllMods = false`. The `DiffWindow.xaml` `ListView` binds to `FilteredDiffLines` instead of `DiffLines`. A `CheckBox` toggle bound to `ShowAllMods` is placed between the description text and the list.

`DiffLines` remains the authoritative unfiltered collection for all command predicates (`AddedMods`, `HasAddedMods`, `HasInsertedMods`, `HasDifferences`) — these compute over the full data, not the view. `FilteredDiffLines` is purely a display-layer concern.

`ScrollTargetIndex` is updated to account for the view: when `ShowAllMods = false`, the filtered list contains only changed items so the first changed item is always at index 0; when `ShowAllMods = true`, the existing scan logic finds the first changed item in the full list.

The `DiffService` pipeline is unchanged. `contextSize = 1` stays intact — context lines are still generated and are available for display when `ShowAllMods = true`.

---

## Rationale

- **ViewModel-layer filter over Service-layer filter:** Filtering at the ViewModel layer keeps the service pure and reusable. The service always returns the complete context window; the view decides what to show.
- **ICollectionView over a second ObservableCollection:** `ListCollectionView` wrapping an existing `ObservableCollection` auto-syncs on all collection changes without requiring manual mirroring. A second `ObservableCollection` would require keeping two lists in sync across `ReplaceDiffLines`, `RefreshDiffAsync`, and all mutation paths.
- **Default = changes only:** The primary use case for opening the diff window is to understand what changed. Users do not need unchanged mods by default; showing them is an opt-in for spatial orientation.

---

## Considered Alternatives

| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| Filter location | `DiffDialogViewModel` (ViewModel layer) | `DiffService.TrimToContextWindow` with a parameter; a second `ObservableCollection` | Service-layer filter breaks reuse and forces API change; duplicate collection requires synchronization across every mutation path. ViewModel filter via `ICollectionView` is the idiomatic WPF pattern. |
| Collection type | `ListCollectionView` wrapping `DiffLines` | Second `ObservableCollection<DiffLineModel>`; `CollectionViewSource` in XAML | `ListCollectionView` is auto-synced, available via code-behind, and supports `DeferRefresh()`; `CollectionViewSource` in XAML cannot call `Refresh()` programmatically when `ShowAllMods` changes. |
| Toggle placement | `CheckBox` between description row and list | Toggle button inside the list border header; menu item; settings preference | A `CheckBox` is the lightest-weight persistent-state indicator for a binary view mode and is immediately visible without opening a menu. |
| Default state | `ShowAllMods = false` (changes only) | `ShowAllMods = true` (preserve current behavior) | New default directly addresses the user's stated problem. Users can opt in to the full view. Consistent with the strategic UX goal of "clearer screens." |

---

## Pattern Alignment

- **`[ObservableProperty]` for new bool** — follows existing `_isConfigValid`, `_isOperationInProgress` pattern in `DiffDialogViewModel.cs` — `ViewModels/DiffDialogViewModel.cs` (L44–L47).
- **Text ViewModel property for new string** — follows all existing `DiffDialogTexts` properties in `ViewTexts/DiffDialogTexts.cs`.
- **`ICollectionView` filter** — new pattern for this codebase; no departure from MVVM conventions. `DiffLines` remains the unfiltered source so no existing consumer is broken.
- **Localization-first**: all new UI text goes through `DiffDialogTexts` and the locale JSON files — no hardcoded strings.

---

## Detailed Steps

1. **Add `ShowAllModsToggleText` to `DiffDialogTexts`** (`ViewTexts/DiffDialogTexts.cs`)
   - Add `public string ShowAllModsToggleText => _localization.GetString("DiffDialog", "ShowAllModsToggleText");`
   - Add `OnPropertyChanged(nameof(ShowAllModsToggleText));` to `OnCultureChanged` only. (`RefreshAll` delegates entirely to `OnCultureChanged(this, EventArgs.Empty)` — adding it there is sufficient and avoids a double-fire when `RefreshAll` is called.)

2. **Add `ShowAllModsToggleText` to all 8 locale JSON files** (`ViewTexts/Locales/*.json`)
   - Add the key at the end of the `DiffDialog` section in each file.
   - Translations: `en-US` = `"Show all mods"`, `de-DE` = `"Alle Mods anzeigen"`, `fr-FR` = `"Afficher tous les mods"`, `es-ES` = `"Mostrar todos los mods"`, `it-IT` = `"Mostra tutti i mod"`, `zh-CN` = `"显示所有 MOD"`, `ja-JP` = `"すべての MOD を表示"`, `pt-BR` = `"Mostrar todos os mods"`.

3. **Add `ShowAllMods` property and `FilteredDiffLines` view to `DiffDialogViewModel`** (`ViewModels/DiffDialogViewModel.cs`)
   - Add `using System.Windows.Data;` import.
   - Add `private readonly ListCollectionView _filteredDiffLines;` field.
   - Add `[ObservableProperty] private bool _showAllMods = false;`
   - Expose `public ICollectionView FilteredDiffLines => _filteredDiffLines;`
   - In the constructor, after `DiffLines` is initialized: create `_filteredDiffLines = new ListCollectionView(DiffLines)` and set its `Filter` predicate: return `true` when `ShowAllMods || (item is DiffLineModel d && d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator)`.
   - Add `partial void OnShowAllModsChanged(bool value)`:  call `_filteredDiffLines.Refresh()`, call `UpdateDiffState()`, call `RequestScroll()`.
   - In `ReplaceDiffLines`, wrap the Clear/Add batch loop in `using (_filteredDiffLines.DeferRefresh()) { ... }` so the `ListCollectionView` receives a single bulk-update notification instead of one per item, preventing ListView flicker during auto-refresh.
   - Update `ComputeScrollTargetIndex()`: when `!ShowAllMods`, return `HasDifferences ? 0 : -1` immediately (the first item in the filtered view is already the first changed item); otherwise use the existing scan.
   - Add `ShowAllModsToggleText` pass-through property: `public string ShowAllModsToggleText => Texts.ShowAllModsToggleText;`

4. **Update `DiffWindow.xaml`** (`Views/DiffWindow.xaml`)
   - Add a new `Auto`-height row between the description row (`Row 0`) and the list row (now `Row 2`). Shift all existing `Grid.Row` assignments ≥ 1 up by 1.
   - Add a `CheckBox` in the new `Grid.Row="1"` with:
     - `Content="{Binding ShowAllModsToggleText}"`
     - `IsChecked="{Binding ShowAllMods, Mode=TwoWay}"`
     - `Style="{StaticResource MaterialDesignCheckBox}"`
     - `Margin="0,4,0,4"`
     - `HorizontalAlignment="Left"`
   - Change `ListView.ItemsSource` from `{Binding DiffLines}` to `{Binding FilteredDiffLines}`.

5. **Update `api-surface.md`** (`Docs/Agents/project-manifest/api-surface.md`)
   - Add `ShowAllMods`, `ShowAllModsToggleText`, and `FilteredDiffLines` to the `DiffDialogViewModel` API entry.

---

## Dependencies

- CommunityToolkit.Mvvm — already used; `[ObservableProperty]` and `partial void OnXxxChanged` hooks are already in use.
- `System.Windows.Data.ListCollectionView` — part of WPF standard library; no new NuGet package needed.

---

## Required Components

- `ViewModels/DiffDialogViewModel.cs` — existing file, modified
- `Views/DiffWindow.xaml` — existing file, modified
- `ViewTexts/DiffDialogTexts.cs` — existing file, modified
- `ViewTexts/Locales/en-US.json` — existing file, modified
- `ViewTexts/Locales/de-DE.json` — existing file, modified
- `ViewTexts/Locales/fr-FR.json` — existing file, modified
- `ViewTexts/Locales/es-ES.json` — existing file, modified
- `ViewTexts/Locales/it-IT.json` — existing file, modified
- `ViewTexts/Locales/zh-CN.json` — existing file, modified
- `ViewTexts/Locales/ja-JP.json` — existing file, modified
- `ViewTexts/Locales/pt-BR.json` — existing file, modified
- `Docs/Agents/project-manifest/api-surface.md` — existing file, modified

---

## Assumptions

- `System.Windows.Data.ListCollectionView` is available in the .NET 9 WPF target (it is — `PresentationFramework` includes it).
- The `DiffWindow.xaml.cs` `ScrollToTargetLine` method uses `DiffListView.Items[index]`, which reflects the `ItemsSource` binding. After the binding changes to `FilteredDiffLines`, `index` must be valid within the filtered set — addressed by the `ComputeScrollTargetIndex` update in Step 3.
- `OnShowAllModsChanged` (CommunityToolkit partial method hook) fires after the backing field changes. This is the correct lifecycle hook for refreshing the view.
- Calling `_filteredDiffLines.Refresh()` inside `OnShowAllModsChanged` is safe even when called from a non-UI thread — the `ListCollectionView` dispatcher is the WPF dispatcher and will marshal if necessary. Since all ViewModel mutations in this codebase occur on the UI thread, this is a non-issue.

---

## Constraints

- **`DiffLines` must remain the unfiltered authoritative source.** All command predicates, context menu items, and diff state computations use `DiffLines` directly — these must not be changed.
- **No change to `DiffService`.** The service pipeline is out of scope.
- **Localization invariant**: `ShowAllModsToggleText` must be added to all 8 locale files in the same change set. Building with a missing key will produce an empty string at runtime (no crash), but consistency is required per the zero-hardcoding architecture.
- **AGENTS.md MUST rule**: new public API → update `api-surface.md` before completing task.
- **MaterialDesign checkbox style**: use `{StaticResource MaterialDesignCheckBox}` (not the default WPF checkbox) for visual consistency.

---

## Out of Scope

- Persisting the `ShowAllMods` toggle state across window sessions (no settings persistence).
- Changing `contextSize` in `TrimToContextWindow`.
- Adding a "jump to next change" navigation control.
- Unit tests for `DiffDialogViewModel` toggle behavior (the ViewModel is not unit-testable without major refactoring due to hard-coded coordinator and file system dependencies, as documented in `Tests/LoadOrderKeeper.Tests/ViewModels/MainViewModelTests.cs`).

---

## Acceptance Criteria

- AC-01: By default (toggle unchecked), the diff window shows only changed items — `Added`, `Removed`, `Moved`, `Replaced`, `Inserted`. No `Unchanged` or `Separator` items are visible.
- AC-02: Checking the "Show all mods" toggle immediately shows all items, with `Unchanged` items at 45% opacity and `Separator` items as `···` markers.
- AC-03: Unchecking the toggle immediately hides `Unchanged` and `Separator` items again.
- AC-04: The toggle label comes from the locale system and is not hardcoded.
- AC-05: The toggle label is correctly translated in all 8 supported locales.
- AC-06: The scroll-to-first-change behavior works correctly in both toggle states.
- AC-07: All existing diff behaviors (commands, context menu, auto-refresh, dependent changes expansion) are unaffected.
- AC-08: The API surface manifest is updated.

---

## Testing Strategy

The `DiffService` pipeline (which produces `Unchanged` and `Separator` items) is fully covered by `ClassifyChangesTests.cs` and `ScenarioTests.cs`. No regression is possible there since `DiffService` is unchanged.

The ViewModel toggle behavior is a UI-layer concern. Automated testing of `DiffDialogViewModel` requires `MainViewModel` which requires coordinators and file system — the existing `MainViewModelTests.cs` documents this limitation explicitly. Manual verification covers AC-01 through AC-07 via the running application.

AC-08 (manifest update) is a documentation check, verifiable by code review.

---

## Test Plan

- Manual test (AC-01): Open the diff window with known changes. Verify only changed items appear. Verify no `···` separators and no grayed-out items are visible.
- Manual test (AC-02): Check "Show all mods". Verify unchanged context mods appear grayed out and `···` separators are visible.
- Manual test (AC-03): Uncheck "Show all mods". Verify unchanged items disappear immediately.
- Manual test (AC-04, AC-05): Switch language to each of the 8 supported locales and verify the toggle label changes correctly (no empty/missing label).
- Manual test (AC-06): With toggle unchecked, open diff window and verify it scrolls to the first changed item (index 0 of filtered list). With toggle checked, verify scroll goes to first changed item in the full list.
- Manual test (AC-07): Use context menus (re-enable, remove, replace); expand dependent changes; verify auto-refresh on file change; verify "Accept Changes" and "Discard Changes" still work.
- Code review (AC-08): Confirm `api-surface.md` lists `ShowAllMods`, `FilteredDiffLines`, `ShowAllModsToggleText`.

---

## Documentation Updates

- `Docs/Agents/project-manifest/api-surface.md` — add `ShowAllMods`, `ShowAllModsToggleText`, `FilteredDiffLines` to `DiffDialogViewModel` entry (AGENTS.md MUST rule).
- `Docs/Agents/project-manifest/ui-design.md` — update the "Diff Window Context Lines and Separators" section to document the new default behavior (changes only) and the "Show all mods" toggle.
- `Docs/Agents/project-manifest/localization.md` — change "Total Strings: 198 translated strings per locale" to "Total Strings: 199 translated strings per locale" (AGENTS.md MUST rule: new locale key added → update localization.md).

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **`ScrollTargetIndex` off-by-one when filtered** | When `ShowAllMods = false`, override `ComputeScrollTargetIndex` to always return 0 when `HasDifferences` (first item in filtered view IS the first changed item). |
| **`ListCollectionView` not thread-safe** | All ViewModel mutations in this codebase occur on the UI thread; no cross-thread access to `FilteredDiffLines`. |
| **`OnPropertyChanged(nameof(DiffLines))` in `ReplaceDiffLines` triggers unexpected rebind** | After changing `ItemsSource` to `FilteredDiffLines`, this notification only affects bindings to `DiffLines`. Since no XAML element binds to `DiffLines` after the change, it is a no-op and does not need to be removed (though it can be for cleanliness). |
| **Locale key missing from one of the 8 files** | All 8 files are modified in the same step; the build produces no error but `LocalizationService` falls back to the key name for missing entries. Code review of the locale files before merging is sufficient. |
| **Material Design checkbox style not applied** | Use `Style="{StaticResource MaterialDesignCheckBox}"` explicitly; do not rely on implicit styles. |

---

## Recommended Workflow
- **Workflow:** standalone
- **Rationale:** Single-area UI change within a well-understood ViewModel/View pattern; no new architecture, no cross-cutting service changes, and no need for formal QA or security audit stages.
