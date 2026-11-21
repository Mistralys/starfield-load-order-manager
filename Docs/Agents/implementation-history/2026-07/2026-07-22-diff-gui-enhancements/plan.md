
# Plan

## Plan Audit Cycles
- Audits: 1 — Plan Auditor v1.7.0
- Architectural Reviews: 2 — Plan Architect Reviewer v2.2.0

## Summary

Improve the diff window GUI by replacing all hardcoded hex colors with semantic brush resources, adding context lines that show unchanged neighbors around each change group for spatial orientation, localizing the hardcoded English dependent-change summary with causal text explaining *why* items shifted, introducing a smarter change count that distinguishes primary changes from dependent shifts, and localizing remaining hardcoded tooltip text. These are GUI-only changes that build on the already-implemented LCS diff pipeline — no algorithm changes.

## Architectural Context

The diff window (`Views/DiffWindow.xaml`) renders a flat `ListView` of `DiffLineModel` items produced by `DiffService.GetPluginsDiffAsync()`. Each item has a `DiffChangeType` (Added, Removed, Moved, Replaced, Inserted) with a corresponding hardcoded hex background color set via `DataTrigger` in the `ListViewItem` style. The `DiffChangeType.Unchanged` enum value exists but is never included in the output — only changed items are returned.

The LCS pipeline in `DiffService.ClassifyChanges()` already has all the information needed to support context lines: it knows which items are in the LCS (unchanged), and during Step 6 dependent-change grouping, it knows *which* causal change (Removed/Inserted) caused each group of shifts. This information is currently discarded — the dependent summary is a hardcoded English string (`"+ {count} mod positions affected by this change"`) that doesn't identify the cause.

The change count flows from `FileMonitoringCoordinator.UpdateChangeCountDisplayAsync()` → `ChangeCount` property → `CoordinatorEventBinder` → `MainViewModel.UpdateChangeCountDisplay()` → `ShowChangesButtonText`. Currently, the count sums all items including all dependent changes, giving a single undifferentiated number.

Key files:
- `App.xaml` — theme brushes (L21–L30), no diff-specific brushes
- `Views/DiffWindow.xaml` — 11 hardcoded hex colors across change types and banners
- `Models/DiffLineModel.cs` — model with `DependentChangesSummary` hardcoded English (L48)
- `ViewModels/DiffDialogViewModel.cs` — `HasDifferences`, `ComputeScrollTargetIndex`, change queries
- `Coordinators/FileMonitoringCoordinator.cs` — `UpdateChangeCountDisplayAsync` (L265–L289)
- `ViewModels/MainViewModel.cs` — `UpdateChangeCountDisplay` (L559–L563)
- `ViewTexts/DiffDialogTexts.cs` — 35 localized string properties
- `Services/DiffService.cs` — `ClassifyChanges()` builds output list (L330–L505)

## Approach / Architecture

### 1. Semantic Brushes

Define named `SolidColorBrush` resources in `App.xaml` for all diff colors, following the existing `MaterialDesign.Brush.*` override pattern. Replace all hardcoded hex values in `DiffWindow.xaml` with `{DynamicResource}` references. This is a pure find-and-replace with no behavioral change.

### 2. Context Lines & Separators

Extend the output of `ClassifyChanges()` to include `Unchanged` items for context, then add a post-processing step `TrimToContextWindow()` that retains only unchanged items within 1 position of a change. Insert `Separator` items (new enum value) between non-adjacent context groups. The ListView already has a transparent default background for unknown types, so `Unchanged` items display naturally with dimmed styling via a new `DataTrigger`.

### 3. Localized Dependent-Change Summary

Add `DependentChangeCauseFileName`, `DependentChangeCauseAction`, and `DependentChangesSummary` as settable string properties to `DiffLineModel`. During Step 6 of `ClassifyChanges()`, use the already-available `_localization` field in `DiffService` to format `DependentChangesSummary` as a ready-to-display string — the same pattern used for every other `DiffLineModel.Text` value. The model remains a pure data container; no `LocalizationService` coupling is introduced into the model layer.

### 4. Smarter Change Count

Split the change count into primary and dependent counts. `FileMonitoringCoordinator` exposes both. `MainViewModel` formats the button text as "Manage load order (3 changes, +5 affected)" when dependent changes exist, or "Manage load order (3 changes)" when they don't.

### 5. Tooltip Localization

Move the hardcoded Inserted warning icon tooltip to a `DiffDialogTexts` property backed by a locale key.

## Rationale

- **Semantic brushes** fix a documented `ui-design.md` rule violation and enable theme-consistent color management.
- **Context lines** solve a real usability problem: a flat list of 8 changed items among 200 mods gives no spatial orientation. Showing 1 unchanged neighbor above and below each change group immediately tells the user where in the load order the change occurred.
- **Causal dependent-change text** leverages information the LCS pipeline already computes but discards, replacing a generic count with an explanation that helps users understand cascading effects.
- **Split change count** prevents alarm — "42 changes" sounds critical when only 3 mods were actually changed and 39 just shifted position. "3 changes (+39 affected)" conveys the real scope.
- **Tooltip localization** eliminates the last hardcoded English string in the diff window.

## Considered Alternatives

| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| Context lines approach | Include Unchanged in DiffService output, trim with `TrimToContextWindow()` | (A) Separate context query in ViewModel; (B) Show all unchanged items | (A) duplicates file reading and classification; (B) overwhelms the display. Trim-in-service keeps a single output pipeline and XAML handles visibility. |
| Separator implementation | `DiffChangeType.Separator` enum value with dedicated `DiffLineModel` | (A) XAML-only separator via converter; (B) GroupStyle separator | (A) requires complex XAML logic to detect adjacency; (B) requires restructuring the flat list into groups. Enum value is simple and consistent with existing pattern. |
| Change count split | Two properties on `FileMonitoringCoordinator` | (A) Single property with formatted string; (B) Separate badge widget | (A) breaks the existing `int` contract; (B) requires MainWindow layout changes. Two properties preserve the existing pattern and let `MainViewModel` format. |
| Dependent-change cause | Properties on `DiffLineModel` set during classification | (A) Reverse-lookup in ViewModel; (B) Separate model class | (A) requires re-running classification logic; (B) breaks the existing `DependentChanges` list type. Properties on the model keep data co-located with where it's produced. |

## Pattern Alignment

- **Semantic brush resources** — follows the one-file-per-concern `Styles/*.xaml` convention established by `DataGridStyles.xaml`, `ButtonStyles.xaml`, `TextStyles.xaml`, `WindowStyles.xaml`; new `Styles/DiffBrushes.xaml` merged in `App.xaml` alongside the existing four entries
- **DiffChangeType enum extension** — follows existing pattern of enum-per-change-type with `Prefix` switch and XAML `DataTrigger` per value — `Models/DiffLineModel.cs`
- **Text ViewModel localization** — follows existing `DiffDialogTexts.cs` pattern: property → `GetString()` → `OnCultureChanged` raises all
- **ObservableProperty on coordinator** — follows existing `ChangeCount` pattern on `FileMonitoringCoordinator.cs`
- **Departure: Unchanged and Separator items in output** — currently `ClassifyChanges()` returns only changed items. Including context items is a deliberate departure to enable the context feature. All existing consumers that filter by specific `DiffChangeType` values (`Moved`, `Added`, `Removed`, `Replaced`, `Inserted`) are unaffected. Consumers that count or iterate all items (`UpdateChangeCountDisplayAsync`, `ComputeScrollTargetIndex`, `HasDifferences`) already filter on `ChangeType != Unchanged` or need minor updates.

## Detailed Steps

### Phase 1: Semantic Brushes

**Step 1: Create `Styles/DiffBrushes.xaml` with semantic diff brush resources.**

Create a new resource dictionary `Styles/DiffBrushes.xaml` following the one-file-per-concern convention established by the existing four `Styles/*.xaml` files:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Diff change type brushes -->
    <SolidColorBrush x:Key="DiffBrush.Added" Color="#1A4CAF50" />
    <SolidColorBrush x:Key="DiffBrush.Removed" Color="#1AF44336" />
    <SolidColorBrush x:Key="DiffBrush.Inserted" Color="#1AFFEB3B" />
    <SolidColorBrush x:Key="DiffBrush.Moved" Color="#1A2196F3" />
    <SolidColorBrush x:Key="DiffBrush.Replaced" Color="#1A9C27B0" />
    <SolidColorBrush x:Key="DiffBrush.Context" Color="#08FFFFFF" />

    <!-- Sorting recommendation banner -->
    <SolidColorBrush x:Key="DiffBrush.SortingBannerBackground" Color="#FFFFA726" />
    <SolidColorBrush x:Key="DiffBrush.SortingBannerBorder" Color="#FFFF9800" />

    <!-- Multiple replacements help banner -->
    <SolidColorBrush x:Key="DiffBrush.HelpBannerBackground" Color="#FFD3E4FD" />
    <SolidColorBrush x:Key="DiffBrush.HelpBannerBorder" Color="#FF0B57D0" />
    <SolidColorBrush x:Key="DiffBrush.HelpBannerForeground" Color="#FF001A41" />

</ResourceDictionary>
```

Merge in `App.xaml` alongside the existing four style entries (after L35):

```xml
<ResourceDictionary Source="Styles/DiffBrushes.xaml" />
```

**Step 2: Replace hardcoded colors in `DiffWindow.xaml` with semantic brushes.**

Replace all 11 hardcoded hex values:

- Change type `DataTrigger` backgrounds (L155–L178): `#1A4CAF50` → `{DynamicResource DiffBrush.Added}`, etc.
- Sorting banner (L275–L276): `Background="#FFFFA726"` → `Background="{DynamicResource DiffBrush.SortingBannerBackground}"`, `BorderBrush="#FFFF9800"` → `BorderBrush="{DynamicResource DiffBrush.SortingBannerBorder}"`
- Help banner (L301–L302): `Background="#FFD3E4FD"` → `Background="{DynamicResource DiffBrush.HelpBannerBackground}"`, `BorderBrush="#FF0B57D0"` → `BorderBrush="{DynamicResource DiffBrush.HelpBannerBorder}"`
- Help banner icon (L309): `Foreground="#FF0B57D0"` → `Foreground="{DynamicResource DiffBrush.HelpBannerBorder}"` (reuses same brush)
- Help banner text (L315): `Foreground="#FF001A41"` → `Foreground="{DynamicResource DiffBrush.HelpBannerForeground}"`

### Phase 2: Context Lines & Separators

**Step 3: Add `Separator` value to `DiffChangeType` enum.**

In `Models/DiffLineModel.cs`, add `Separator` after `Inserted` in the `DiffChangeType` enum. Update the `Prefix` switch to return `string.Empty` for `Separator` (same as `Unchanged`).

**Step 4: Implement `TrimToContextWindow()` in `DiffService`.**

Add a private static method after `ClassifyChanges()`:

```csharp
private static List<DiffLineModel> TrimToContextWindow(List<DiffLineModel> items, int contextSize = 1)
```

Logic:
1. Mark each item's index as "keep" if it's a non-Unchanged/non-Separator item, or if it's within `contextSize` positions of such an item.
2. Build a new list: include all "keep" items. Between non-adjacent kept items, insert a `Separator` `DiffLineModel` (empty filename, "···" text, `DiffChangeType.Separator`).
3. Return the trimmed list.

**Step 5: Include Unchanged items in `ClassifyChanges()` output.**

In `Services/DiffService.cs`, after Step 5 (building the top-level result list at ~L330), add Unchanged items for all LCS entries that were NOT shifted (i.e., items in the LCS that have the same absolute position in both lists):

```csharp
// Context lines — unchanged LCS items (same position in both lists)
foreach (var (refIdx, curIdx) in lcs)
{
    var refMod = reference[refIdx];
    var curMod = current[curIdx];
    if (refMod.LineNumber == curMod.LineNumber)
    {
        result.Add(new DiffLineModel(
            refMod.FileName, refMod.FileName, DiffChangeType.Unchanged,
            refMod.LineNumber, curMod.LineNumber));
    }
}
```

Then, after the final sort (L493–L503), apply context trimming:

```csharp
result = TrimToContextWindow(result);
```

**Step 6: Add context line and separator display in `DiffWindow.xaml`.**

Add two new `DataTrigger` blocks in the `ListViewItem` style (after the existing Replaced trigger):

For `Unchanged`:
- `Background` → `{DynamicResource DiffBrush.Context}`
- `ContextMenu` → `{x:Null}` (no right-click actions on context lines)

For `Separator`:
- `Background` → `Transparent`
- `ContextMenu` → `{x:Null}`
- `IsHitTestVisible` → `False` (not selectable)

In the `ItemTemplate`, add a conditional display for Separator items: hide the normal prefix+text layout and show a centered "···" text with reduced opacity (0.35). Use a `DataTrigger` on the StackPanel to swap visibility.

For Unchanged items in the normal prefix+text layout, reduce text opacity to 0.45 to visually distinguish from active changes.

**Step 7: Update `DiffDialogViewModel` to handle new item types.**

- `HasDifferences` (L221): Already filters `Unchanged`. Also exclude `Separator`:
  ```csharp
  HasDifferences = DiffLines.Any(line => line.ChangeType != DiffChangeType.Unchanged && line.ChangeType != DiffChangeType.Separator);
  ```
- `ComputeScrollTargetIndex()` (L235–L249): Also skip `Separator`:
  ```csharp
  if (DiffLines[index].ChangeType != DiffChangeType.Unchanged && DiffLines[index].ChangeType != DiffChangeType.Separator)
  ```

### Phase 3: Localized Dependent-Change Summary

**Step 8: Add cause and summary properties to `DiffLineModel`.**

Add three new settable properties to `DiffLineModel`:

```csharp
public string? DependentChangeCauseFileName { get; set; }
public string? DependentChangeCauseAction { get; set; }
public string DependentChangesSummary { get; set; } = string.Empty;
```

All three are set post-construction during Step 6 of `ClassifyChanges()`, following the same pattern as `DependentChanges` (the mutable list populated after construction). `DependentChangesSummary` changes from a computed getter to a plain auto-property — the model holds the pre-formatted display string rather than calling `LocalizationService` itself.

**Step 9: Populate cause properties in `DiffService.ClassifyChanges()` Step 6.**

In the Step 6a loop (removal attribution), when a shifted item is assigned to a `Removed` entry as a dependent change, set the parent entry's cause properties:
- `DependentChangeCauseFileName` = the removed mod's `FileName`
- `DependentChangeCauseAction` = `"DependentCause_Removed"`

In the Step 6b loop (insertion attribution), similarly:
- `DependentChangeCauseFileName` = the inserted mod's `FileName`
- `DependentChangeCauseAction` = `"DependentCause_Inserted"` or `"DependentCause_Added"` as appropriate

The cause is set on the *parent* entry (the one with `DependentChanges`), not on each dependent child.

**Step 10: Format and set `DependentChangesSummary` in `DiffService.ClassifyChanges()` Step 6.**

After setting `DependentChangeCauseFileName` and `DependentChangeCauseAction` on the parent entry, also set `DependentChangesSummary` using the `_localization` field already present in `DiffService` — the same mechanism used to set `Text` on every other `DiffLineModel`:

```csharp
// After Step 6a/6b attribution is complete for a parent entry:
string causeAction = _localization.GetString("DiffDialog", parentEntry.DependentChangeCauseAction!);
string summaryKey = parentEntry.DependentChanges.Count == 1
    ? "DependentChangesSummary_Singular"
    : "DependentChangesSummary";
parentEntry.DependentChangesSummary = _localization.GetString(
    "DiffDialog", summaryKey,
    parentEntry.DependentChanges.Count, parentEntry.DependentChangeCauseFileName, causeAction);
```

For any parent entries that have `DependentChanges` but no cause attribution (fallback), set:

```csharp
parentEntry.DependentChangesSummary = _localization.GetString(
    "DiffDialog", "DependentChangesSummary_Generic", parentEntry.DependentChanges.Count);
```

This keeps `DiffLineModel` a pure data container with no `LocalizationService` dependency, consistent with how every other text field on the model is produced.

### Phase 4: Smarter Change Count

**Step 11: Add `DependentChangeCount` property to `FileMonitoringCoordinator`.**

Add a new `[ObservableProperty]` alongside `ChangeCount`:

```csharp
[ObservableProperty]
private int _dependentChangeCount;
```

**Step 12: Update `UpdateChangeCountDisplayAsync()` to compute split counts.**

In `FileMonitoringCoordinator.cs` (L265–L289), update the counting logic:

```csharp
var diffLines = await DiffService.GetPluginsDiffAsync(_config);

// Primary changes: exclude Unchanged, Separator, and dependent Moved items
int primaryCount = diffLines.Count(line =>
    line.ChangeType != DiffChangeType.Unchanged &&
    line.ChangeType != DiffChangeType.Separator);

// Dependent changes: total across all items
int dependentCount = diffLines.Sum(line => line.DependentChanges.Count);

ChangeCount = primaryCount;
DependentChangeCount = dependentCount;
```

**Step 13: Update `ChangeDetectedEventArgs` to carry `DependentChangeCount`.**

Add `DependentChangeCount` to the event args so the event payload is complete. Although `MainViewModel.OnChangeDetected` is currently empty (the DiffWindow subscribes directly and the count display flows via the `ChangeCount` property binder), carrying both counts on the event args means any future direct subscriber receives the full picture without requiring an additional property lookup on the coordinator.

Also update both `new ChangeDetectedEventArgs(...)` constructor call sites in `FileMonitoringCoordinator.cs`:
- The "no change" path (currently `new ChangeDetectedEventArgs(false, 0)`) → `new ChangeDetectedEventArgs(false, 0, 0)`
- The "has change" path (currently `new ChangeDetectedEventArgs(hasChanged, ChangeCount)`) → `new ChangeDetectedEventArgs(hasChanged, ChangeCount, DependentChangeCount)`

**Step 14: Update `MainViewModel` to display split change count.**

In `ViewModels/MainViewModel.cs`, update `UpdateChangeCountDisplay` to accept both counts as **required** (non-optional) parameters and format accordingly:

```csharp
private void UpdateChangeCountDisplay(int changeCount, int dependentChangeCount)
{
    if (changeCount > 0)
    {
        ShowChangesButtonText = dependentChangeCount > 0
            ? string.Format(MainWindowTexts.ShowChangesButtonTextWithDependents, changeCount, dependentChangeCount)
            : string.Format(MainWindowTexts.ShowChangesButtonTextWithCount, changeCount);
    }
    else
    {
        ShowChangesButtonText = MainWindowTexts.ShowChangesButtonText;
    }
}
```

Also update the existing binder call site in `MainViewModel` (the `BindPropertyWithAction` call for `ChangeCount`, currently at L148–L151) to pass both counts:

```csharp
binder.BindPropertyWithAction(_fileMonitor, nameof(FileMonitoringCoordinator.ChangeCount), () =>
{
    UpdateChangeCountDisplay(_fileMonitor.ChangeCount, _fileMonitor.DependentChangeCount);
});
```

Making `dependentChangeCount` a required parameter (not optional with a default) ensures the compiler immediately surfaces any future call site that forgets to pass the dependent count.

**Step 15: Add `ShowChangesButtonTextWithDependents` to `MainWindowTexts` and locale files.**

New locale key: `"ShowChangesButtonTextWithDependents": "Manage load order ({0} changes, +{1} affected)"`

### Phase 5: Tooltip Localization

**Step 16: Move hardcoded Inserted tooltip to locale system.**

Add locale key `"InsertedWarningTooltip": "This mod was inserted into the middle of the load order. Consider sorting the list first."` to the DiffDialog section.

Add property `InsertedWarningTooltip` to `DiffDialogTexts.cs`, add to `OnCultureChanged`.

In `DiffWindow.xaml`, replace the hardcoded `ToolTip="..."` with:
```xml
ToolTip="{Binding DataContext.InsertedWarningTooltip, RelativeSource={RelativeSource AncestorType=ListView}}"
```

The tooltip binding targets the `DiffDialogViewModel`'s `Texts` property. Since the tooltip is on the `PackIcon` inside the `ItemTemplate`, it needs to reach the parent `ListView`'s `DataContext`. Add an `InsertedWarningTooltip` pass-through property to `DiffDialogViewModel`:

```csharp
public string InsertedWarningTooltip => Texts.InsertedWarningTooltip;
```

### Phase 6: Localization

**Step 17: Add new localization keys to `en-US.json`.**

DiffDialog section — new keys:
- `"DependentChangesSummary": "+ {0} mods shifted because {1} was {2}"`
- `"DependentChangesSummary_Singular": "+ 1 mod shifted because {1} was {2}"`
- `"DependentChangesSummary_Generic": "+ {0} mod positions affected by this change"`
- `"DependentCause_Removed": "removed"`
- `"DependentCause_Inserted": "inserted"`
- `"DependentCause_Added": "added"`
- `"InsertedWarningTooltip": "This mod was inserted into the middle of the load order. Consider sorting the list first."`

MainWindow section — new key:
- `"ShowChangesButtonTextWithDependents": "Manage load order ({0} changes, +{1} affected)"`

**Step 18: Add corresponding properties to text ViewModels.**

In `DiffDialogTexts.cs`: Add `InsertedWarningTooltip` property, add to `OnCultureChanged`. (The `DependentChangesSummary*` and `DependentCause_*` keys are consumed directly via `_localization.GetString` in `DiffService.ClassifyChanges()`, not through text ViewModel properties.)

In `MainWindowTexts.cs`: Add `ShowChangesButtonTextWithDependents` property, add to `OnCultureChanged`.

**Step 19: Add translations for all 7 non-English locales.**

Translate all new keys for de-DE, fr-FR, es-ES, it-IT, zh-CN, ja-JP, pt-BR following the conventions in `localization.md`:
- CJK: aki spacing between English/numbers and Asian characters, full-width punctuation
- Chinese quotes: `" "`, Japanese quotes: `「 」`
- Hotkeys at end: `(_X)` pattern (not applicable for these keys)

### Phase 7: Tests

**Step 20: Add context-line trimming tests.**

Add new test methods to `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs`:

- `TrimToContextWindow_SingleChangeMiddle_ShowsOneNeighborEachSide` — A list with one changed item at position 5 among 10 items → output contains the change plus 1 unchanged item above and 1 below.
- `TrimToContextWindow_AdjacentChanges_NoSeparatorBetween` — Two consecutive changed items → both shown with context, no separator between them.
- `TrimToContextWindow_DistantChanges_InsertsSeparator` — Two changed items 10 positions apart → separator between their context groups.
- `TrimToContextWindow_ChangeAtStart_NoContextAbove` — Changed item at position 1 → no context above, 1 context below.
- `TrimToContextWindow_ChangeAtEnd_NoContextBelow` — Changed item at last position → 1 context above, no context below.

**Step 21: Update existing tests for new output shape.**

Tests that assert on `diffLines.Count` or iterate all items need to account for `Unchanged` and `Separator` items in the output:

- `DiffServiceTests.GetPluginsDiffAsync_ReportsUnpairedAddedAndRemovedMods` — filter to non-Unchanged/Separator items before asserting counts
- `DiffServiceTests.GetPluginsDiffAsync_DetectsReplacements` — same filtering
- `DiffServiceTests.GetPluginsDiffAsync_ReturnsEmpty_WhenFilesIdentical` — output should be empty (no changes means no context lines either)
- `ScenarioTestBase.cs` — update `AssertChangeCount` to filter before counting:
  ```csharp
  Assert.Equal(expectedCount, diffs.Count(d =>
      d.ChangeType != DiffChangeType.Unchanged &&
      d.ChangeType != DiffChangeType.Separator));
  ```
  This one-line fix propagates to all call sites in `ScenarioTests` and `ReplacementDetectionDiagnostics` without per-call-site surgery.
- `ScenarioTests` — verify no per-test assertions remain that count raw `diffs.Count` without filtering

**Step 22: Add dependent-change summary tests.**

- `DiffServiceTests.DependentChanges_HaveCauseFileName` — verify `DependentChangeCauseFileName` is set on entries with dependent changes
- `DiffServiceTests.DependentChanges_HaveCauseAction` — verify `DependentChangeCauseAction` is set (e.g., `"DependentCause_Removed"`)

**Step 23: Run full test suite — verify all tests pass including new tests.**

## Dependencies

- Steps 1–2 (semantic brushes) are independent of all other phases. Can proceed in parallel.
- Steps 3–7 (context lines) depend on nothing — the `Unchanged` enum value already exists.
- Steps 8–10 (dependent cause text) are independent of context lines but depend on understanding the `ClassifyChanges()` Step 6 structure.
- Steps 11–15 (change count) depend on step 3 (need `Separator` enum value for filtering).
- Step 16 (tooltip) is independent.
- Steps 17–19 (localization) depend on steps 10, 15, 16 (all new strings must be defined first).
- Steps 20–23 (tests) depend on all prior steps.

## Required Components

### Modified Files
- `App.xaml` — merge new `Styles/DiffBrushes.xaml`
- `Views/DiffWindow.xaml` — semantic brushes, context line display, separator display, tooltip localization
- `Models/DiffLineModel.cs` — `Separator` enum value, cause properties, localized summary
- `Services/DiffService.cs` — `TrimToContextWindow()`, include Unchanged items in output
- `ViewModels/DiffDialogViewModel.cs` — exclude Separator from HasDifferences/ScrollTarget, InsertedWarningTooltip
- `Coordinators/FileMonitoringCoordinator.cs` — `DependentChangeCount`, split counting
- `Coordinators/Events/ChangeDetectedEventArgs.cs` — add `DependentChangeCount`
- `ViewModels/MainViewModel.cs` — split change count display formatting
- `ViewTexts/DiffDialogTexts.cs` — `InsertedWarningTooltip` property
- `ViewTexts/MainWindowTexts.cs` — `ShowChangesButtonTextWithDependents` property
- `ViewTexts/Locales/en-US.json` — 8 new keys
- `ViewTexts/Locales/de-DE.json` — translations
- `ViewTexts/Locales/fr-FR.json` — translations
- `ViewTexts/Locales/es-ES.json` — translations
- `ViewTexts/Locales/it-IT.json` — translations
- `ViewTexts/Locales/zh-CN.json` — translations
- `ViewTexts/Locales/ja-JP.json` — translations
- `ViewTexts/Locales/pt-BR.json` — translations
- `Tests/LoadOrderKeeper.Tests/DiffServiceTests.cs` — new + updated tests
- `Tests/LoadOrderKeeper.Tests/ScenarioTests.cs` — updated assertions
- `Tests/LoadOrderKeeper.Tests/ScenarioTestBase.cs` — update `AssertChangeCount` to filter Unchanged/Separator items before comparing count

### New Files
- `Styles/DiffBrushes.xaml` — semantic brush resources for diff change type colors (follows one-file-per-concern `Styles/*.xaml` convention)

## Assumptions

- The `Unchanged` enum value in `DiffChangeType` is currently unused in output — adding items with this type is a non-breaking expansion.
- Context line count of 1 provides sufficient orientation. The `contextSize` parameter in `TrimToContextWindow()` allows future tuning without code changes.
- `DiffLineModel.DependentChangesSummary` is a plain settable string; its value is formatted by `DiffService.ClassifyChanges()` using `_localization`, the same as all other `DiffLineModel.Text` values. No `LocalizationService` coupling exists in the model layer.
- All causal changes in Step 6 of `ClassifyChanges()` have a single cause — no scenarios exist where a shifted item is attributable to multiple simultaneous causes.
- The `Separator` item type is purely visual — it does not participate in any command logic, is not counted, and is not selectable.

## Constraints

- All existing public API signatures must be preserved: `DiffService.GetPluginsDiffAsync()`, `DiffService.HasIndependentMovedModsAsync()`.
- `HasIndependentMovedModsAsync()` filters by `DiffChangeType.Moved` — unaffected by new `Unchanged`/`Separator` items in output.
- All 8 locale files must be updated in sync with any new localization keys.
- CJK locales (zh-CN, ja-JP) must follow aki spacing, full-width punctuation, and bracket conventions per `localization.md`.
- UTF-8 without BOM encoding for all file writes.
- `DiffLineModel` constructor signature is not changed — new properties are set post-construction like `DependentChanges`.

## Out of Scope

- **Algorithm changes** — The LCS pipeline is complete and working. No changes to `ComputeLcs()` or core `ClassifyChanges()` logic.
- **Side-by-side diff view** — Would require a complete UI redesign.
- **Inline editing** — Drag-and-drop reordering within the diff window.
- **Diff export/sharing** — No export functionality.
- **Code hygiene items from rework-1** — `null-LineNumber` fallback alignment, vacuous `.Where` removal, O(n²) LINQ optimization, `ReplacementDetectionDiagnostics` cleanup. These are in the separate `2026-07-22-lcs-diff-pipeline-rework-1` plan.
- **Test infrastructure** — `EnglishLocaleFixture` and `ClassifyChanges` isolation tests are in the rework-1 plan.

## Acceptance Criteria

- AC-01: All hardcoded hex colors in `DiffWindow.xaml` are replaced with named `DiffBrush.*` semantic brush resources defined in `Styles/DiffBrushes.xaml`.
- AC-02: Diff window displays 1 unchanged context line above and below each change group, with dimmed styling (reduced opacity).
- AC-03: Separator ("···") appears between non-adjacent context groups in the diff list.
- AC-04: `DiffLineModel.DependentChangesSummary` returns localized text including the causal mod name and action.
- AC-05: Main window button shows primary change count with optional dependent count suffix (e.g., "3 changes, +5 affected").
- AC-06: `FileMonitoringCoordinator.ChangeCount` excludes `Unchanged` and `Separator` items.
- AC-07: Inserted warning icon tooltip text is localized via the locale system.
- AC-08: All new user-facing strings are localized in all 8 locale files.
- AC-09: All existing tests pass with updated assertions for new output shape.
- AC-10: New tests cover context-line trimming and dependent-change causal text.
- AC-11: `HasDifferences` and `ComputeScrollTargetIndex` correctly skip `Unchanged` and `Separator` items.
- AC-12: Separator items are not selectable and have no context menu.

## Testing Strategy

Three layers of validation:

1. **Unit tests** for `TrimToContextWindow()` — isolated context-trimming logic with controlled input.
2. **Integration tests** via updated `DiffServiceTests` — verify the full pipeline includes context lines, separators, and causal dependent-change properties.
3. **Scenario tests** via updated `ScenarioTests` — verify all 16 scenarios produce correct results with the new output shape, filtering context items for change-type assertions.

## Test Plan

- `DiffServiceTests.TrimToContextWindow_SingleChangeMiddle_ShowsOneNeighborEachSide` — context around a single mid-list change — AC-02
- `DiffServiceTests.TrimToContextWindow_AdjacentChanges_NoSeparatorBetween` — no separator between adjacent changes — AC-03
- `DiffServiceTests.TrimToContextWindow_DistantChanges_InsertsSeparator` — separator between far-apart groups — AC-03
- `DiffServiceTests.TrimToContextWindow_ChangeAtStart_NoContextAbove` — boundary handling — AC-02
- `DiffServiceTests.TrimToContextWindow_ChangeAtEnd_NoContextBelow` — boundary handling — AC-02
- `DiffServiceTests.DependentChanges_HaveCauseFileName` — cause filename populated — AC-04
- `DiffServiceTests.DependentChanges_HaveCauseAction` — cause action key populated — AC-04
- Updated `DiffServiceTests.GetPluginsDiffAsync_ReportsUnpairedAddedAndRemovedMods` — filter context items — AC-09
- Updated `DiffServiceTests.GetPluginsDiffAsync_DetectsReplacements` — filter context items — AC-09
- Updated `DiffServiceTests.GetPluginsDiffAsync_ReturnsEmpty_WhenFilesIdentical` — no context when no changes — AC-09
- Updated `ScenarioTests` (all 16 scenarios) — filter context items for change assertions — AC-09

## Documentation Updates

- `Docs/Agents/project-manifest/api-surface.md` — add `DiffChangeType.Separator`, new `DiffLineModel` properties (`DependentChangeCauseFileName`, `DependentChangeCauseAction`), `FileMonitoringCoordinator.DependentChangeCount`
- `Docs/Agents/project-manifest/ui-design.md` — add DiffBrush semantic color table (referencing `Styles/DiffBrushes.xaml`), document context-line and separator visual patterns
- `Docs/Agents/project-manifest/constraints.md` — update "Diff Semantics & Monitoring" to mention context lines and split change count
- `Docs/Agents/project-manifest/localization.md` — update string count (189 → ~198 per locale)
- `Docs/Agents/project-manifest/file-tree.md` — add `Styles/DiffBrushes.xaml`

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **Context lines make the diff window feel cluttered for small mod lists** | Context lines use very low opacity (dimmed text, 3% background) and are limited to 1 neighbor per side. For very small lists (< 10 mods), most items are already visible changes, so context adds minimal noise. |
| **Separator display looks inconsistent across themes** | Separator uses a simple centered "···" text at low opacity, which is theme-agnostic. No background color, so it works on any theme. |
| **Existing tests break due to new Unchanged/Separator items in output** | All affected tests are identified in the test plan. The fix is mechanical: filter to `ChangeType != Unchanged && ChangeType != Separator` before asserting counts. |
| **`DependentChangesSummary` localization changes break existing display** | The property name and type are unchanged. The value is now set by `DiffService` (same as `Text`) rather than computed on demand — visually identical for en-US users, now correct for all locales. No runtime locale-switch edge case since the string is produced at classification time, consistent with all other model text fields. |
| **Split change count confuses users** | The format "3 changes, +5 affected" is additive — it only appears when dependent changes exist. When there are none, the display is unchanged ("3 changes"). |

## Recommended Workflow
- **Workflow:** standalone
- **Rationale:** All changes are within well-understood GUI patterns (XAML brushes, localization, ViewModel properties) with no architectural departures or cross-cutting concerns.
