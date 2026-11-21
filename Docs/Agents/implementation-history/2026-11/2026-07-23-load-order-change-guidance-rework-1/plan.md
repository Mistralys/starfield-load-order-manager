
# Plan

## Plan Audit Cycles
- Audits: 2 — Plan Auditor v1.7.0
- Architectural Reviews: 2 — Plan Architect Reviewer v2.2.0

## Prior Project Context
The `2026-07-23-load-order-change-guidance` project delivered a three-layer contextual guidance system (sidebar cards, tooltips, risk-gated confirmation dialog) with full localization across 8 locales. The synthesis identified 5 deferred items, 5 out-of-scope items, and 5 strategic recommendations. This rework plan addresses the highest-value subset: test coverage gaps, foreground brush duplication, a missing sidebar card for the Moved change type, notification list simplification, and dead locale key cleanup.

## Summary
Harden the load order change guidance feature delivered in the prior plan by: (1) extracting the confirmation-message building logic into a testable method and adding the 2 skipped unit tests; (2) consolidating 7 inline foreground color hex literals in `DiffWindow.xaml` into named `SolidColorBrush` resources in `DiffBrushes.xaml`; (3) adding a Moved guidance sidebar card with full localization; (4) replacing the 24-call `UpdateDiffState()` notification list with a single `OnPropertyChanged(string.Empty)` call; and (5) removing the dead `DependentCause_Added` locale key from all 8 locales.

## Architectural Context
`DiffDialogViewModel` is a `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` subclass with two constructors: a production constructor that wires coordinator subscriptions and a test-seam constructor that accepts a pre-populated `ObservableCollection<DiffLineModel>` and skips coordinator wiring. The test-seam constructor stubs `UpdateReferenceCommand` as a no-op, which makes `UpdateReferenceWithConfirmationAsync()` unreachable from tests. All foreground colors in `DiffWindow.xaml` are currently inline hex strings. The sidebar has 4 change-type cards (Removal, Insertion, Replacement, Added) plus a No Changes card — the Moved type is the only classification without a card.

## Approach / Architecture
1. **Extract confirmation message builder**: Lift the message-building logic (DiffLines query → singular/plural formatting → composed message string) from `UpdateReferenceWithConfirmationAsync()` into an `internal` method `BuildConfirmationMessage()` that returns `string?` (null when no confirmation needed). The async method calls it and uses the result. Tests call it directly via `InternalsVisibleTo`.
2. **Foreground brush resources**: Add 7 named `SolidColorBrush` entries to `DiffBrushes.xaml` following the `DiffBrush.{ChangeType}Foreground` naming convention. Replace all 9 inline foreground hex literals in `DiffWindow.xaml` with `{DynamicResource DiffBrush.*Foreground}` references.
3. **Moved sidebar card**: Add `ShowMovedGuidance`, `MovedModCount`, `MovedModCountText`, and pass-through text properties to the ViewModel; add locale keys to all 8 locale files; add a card to the sidebar XAML following the existing card pattern. The card uses a blue information tone.
4. **Notification simplification**: Replace the 24 `OnPropertyChanged(nameof(...))` calls in `UpdateDiffState()` with `OnPropertyChanged(string.Empty)`. Retain the explicit `HasDifferences = ...` assignment (which fires its own notification via `SetProperty`).
5. **Dead key removal**: Delete the `DependentCause_Added` line from all 8 locale JSON files.

## Rationale
- **Extract method over interface mock**: The confirmation message is a pure function of DiffLines state and Texts formatting. Extracting it is simpler and more maintainable than introducing an `IConfirmationMessageBuilder` interface or making the command injectable. The method can return `string?` to signal "no confirmation needed" (safe-only changes), which the async wrapper interprets.
- **`OnPropertyChanged(string.Empty)` over listing**: The growing notification list (24 calls, was 16 before sidebar) is a maintenance burden — every new computed property requires adding another line. `string.Empty` is the standard WPF convention, supported by `ObservableObject`, and has negligible performance impact since all properties are cheap computed values.
- **Delete dead key over gender fix**: `DependentCause_Added` is unreferenced by any C# code. Fixing its gender agreement in it-IT would validate dead code. Removal is correct.
- **Moved card for completeness**: When all changes are Moved-only, the sidebar shows only the title — no cards appear. Adding a Moved card closes this gap with minimal code.

## Considered Alternatives

| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| Confirmation-message testability | Extract `internal BuildConfirmationMessage()` method | Interface injection (`IConfirmationMessageBuilder`); make `UpdateReferenceCommand` invocable from test-seam | Extracted method is zero-dependency, requires no new types, and follows the existing pure-computation property pattern. Interface injection is over-engineered for a single call site. |
| Notification simplification | `OnPropertyChanged(string.Empty)` | Keep explicit list; CommunityToolkit `[ObservableProperty]` source-gen migration | `string.Empty` is a one-line fix that eliminates the maintenance burden. Source-gen migration is a larger refactor (every computed property needs restructuring) and should be its own plan if pursued. |
| Dead key disposition | Remove from all 8 locales | Fix gender agreement in it-IT | Key is unreferenced in code. Fixing grammar on dead code adds noise. |

## Pattern Alignment
- **`DiffBrush.*` naming convention** (`Styles/DiffBrushes.xaml`): New foreground brushes follow the established `DiffBrush.{ChangeType}Foreground` pattern alongside existing `DiffBrush.{ChangeType}` background brushes. No departure.
- **Test-seam constructor** (`ViewModels/DiffDialogViewModel.cs`): `BuildConfirmationMessage()` uses `internal` visibility with `InternalsVisibleTo`, consistent with the test-seam pattern documented in `constraints.md`. No departure.
- **Sidebar card pattern** (`Views/DiffWindow.xaml`): Moved card follows the identical structure (Card → StackPanel → icon + title row, count text, body text) as the 4 existing cards. No departure.
- **Locale key 1:1 mapping** (`ViewTexts/DiffDialogTexts.cs`): New `GuidanceMoved*` properties map 1:1 to locale keys. No departure.
- **Zero-hardcoding localization** (`Docs/Agents/project-manifest/localization.md`): All new strings go through locale JSON → `DiffDialogTexts` → XAML binding. No departure.

## Detailed Steps

### Step 1: Extract Confirmation-Message Builder

Extract the message-building logic from `UpdateReferenceWithConfirmationAsync()` (lines 629–660 of `DiffDialogViewModel.cs`) into a new `internal` method:

```csharp
/// <summary>
/// Builds a risk-specific confirmation message based on the current diff state.
/// Returns <c>null</c> when no confirmation is needed (safe-only changes).
/// </summary>
internal string? BuildConfirmationMessage()
{
    var removedCount = DiffLines.Count(line => line.ChangeType == DiffChangeType.Removed);
    var insertedCount = DiffLines.Count(line => line.ChangeType == DiffChangeType.Inserted);

    if (removedCount == 0 && insertedCount == 0)
        return null;

    var detailLines = new List<string>();

    if (removedCount > 0)
    {
        detailLines.Add(removedCount == 1
            ? Texts.ConfirmRiskyDetail_RemovedSingular
            : string.Format(Texts.ConfirmRiskyDetail_Removals, removedCount));
    }

    if (insertedCount > 0)
    {
        detailLines.Add(insertedCount == 1
            ? Texts.ConfirmRiskyDetail_InsertedSingular
            : string.Format(Texts.ConfirmRiskyDetail_Insertions, insertedCount));
    }

    string detailBlock = string.Join("\n", detailLines);
    return string.Format(Texts.ConfirmUpdateMessage_RiskyChanges, detailBlock);
}
```

Then update `UpdateReferenceWithConfirmationAsync()` to call it:

```csharp
private async Task UpdateReferenceWithConfirmationAsync()
{
    string? confirmMessage = BuildConfirmationMessage();

    if (confirmMessage is not null)
    {
        var eventArgs = new ConfirmationRequestedEventArgs(
            Texts.ConfirmUpdateTitle,
            confirmMessage,
            ConfirmationIcon.Warning,
            ConfirmationButton.YesNo);
        ConfirmationRequested?.Invoke(this, eventArgs);

        if (eventArgs.Result != ConfirmationResult.Yes)
        {
            DiffStatusMessage = Texts.ReferenceUpdateCancelledStatus;
            return;
        }
    }

    if (_mainViewModel.CreateReferenceCommand?.CanExecute(null) ?? false)
    {
        await _mainViewModel.CreateReferenceCommand.ExecuteAsync(null);
    }
}
```

### Step 2: Add Confirmation-Message Unit Tests

Add 2 tests to `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs`:

1. **`BuildConfirmationMessage_RiskyChanges_IncludesRemovedCount`**: Create a ViewModel with 2 Removed + 1 Inserted line. Call `BuildConfirmationMessage()`. Assert the result is non-null and contains the expected removed and inserted detail strings.

2. **`BuildConfirmationMessage_SafeChanges_ReturnsNull`**: Create a ViewModel with only Added and Replaced lines. Call `BuildConfirmationMessage()`. Assert the result is null.

Update the class-level `<remarks>` XML doc to remove the references to the skipped tests and note they are now implemented.

3. **Delete the two existing `[Fact(Skip = ...)]` methods** (`ConfirmationMessage_RiskyChanges_IncludesRemovedCount` and `ConfirmationMessage_SafeChanges_SkipsDialog`). Their test intent is replaced by the new `BuildConfirmationMessage_*` tests.

### Step 3: Foreground Brush Resources

Add 7 named `SolidColorBrush` entries to `Styles/DiffBrushes.xaml`:

```xml
<!-- Diff change type foreground (icon) brushes -->
<SolidColorBrush x:Key="DiffBrush.AddedForeground" Color="#66BB6A" />
<SolidColorBrush x:Key="DiffBrush.RemovedForeground" Color="#EF5350" />
<SolidColorBrush x:Key="DiffBrush.MovedForeground" Color="#FFA726" />
<SolidColorBrush x:Key="DiffBrush.ReplacedForeground" Color="#42A5F5" />
<SolidColorBrush x:Key="DiffBrush.InsertedForeground" Color="#FF9800" />
<SolidColorBrush x:Key="DiffBrush.GuidanceReplacementForeground" Color="#9C27B0" />
<SolidColorBrush x:Key="DiffBrush.GuidanceMovedForeground" Color="#2196F3" />
```

Note: Two change types have a `DiffBrush.{ChangeType}Foreground` / `DiffBrush.Guidance{ChangeType}Foreground` pair because the diff-list and sidebar card contexts require different colors. For Replacement: diff-list uses `#42A5F5` (blue, contrast against purple row) vs. sidebar uses `#9C27B0` (canonical purple on lighter card). For Moved: diff-list uses `#FFA726` (orange, contrast against blue row) vs. sidebar uses `#2196F3` (blue, matching the card's informational-blue background hue). Every sidebar card pairs its icon foreground with its background hue — this brush preserves that invariant.

### Step 4: Replace Inline Hex Foregrounds in DiffWindow.xaml

Replace all 9 inline foreground hex literals in `Views/DiffWindow.xaml` with `{DynamicResource DiffBrush.*Foreground}` references:

**Diff-list PackIcon triggers:**
- `Foreground="#66BB6A"` → `Foreground="{DynamicResource DiffBrush.AddedForeground}"`
- `Foreground="#EF5350"` → `Foreground="{DynamicResource DiffBrush.RemovedForeground}"`
- `Foreground="#FFA726"` → `Foreground="{DynamicResource DiffBrush.MovedForeground}"`
- `Foreground="#42A5F5"` → `Foreground="{DynamicResource DiffBrush.ReplacedForeground}"`
- `Foreground="#FF9800"` → `Foreground="{DynamicResource DiffBrush.InsertedForeground}"`

**Sidebar card icons:**
- Removal card `Foreground="#EF5350"` → `Foreground="{DynamicResource DiffBrush.RemovedForeground}"`
- Insertion card `Foreground="#FF9800"` → `Foreground="{DynamicResource DiffBrush.InsertedForeground}"`
- Replacement card `Foreground="#9C27B0"` → `Foreground="{DynamicResource DiffBrush.GuidanceReplacementForeground}"`
- Added card `Foreground="#66BB6A"` → `Foreground="{DynamicResource DiffBrush.AddedForeground}"`

### Step 5: Add Moved Guidance Locale Keys

Add to the `DiffDialog` section of `ViewTexts/Locales/en-US.json`:

```json
"GuidanceMovedTitle": "Moved Mods — Informational",
"GuidanceMovedBody": "These mods changed position due to other modifications in the load order. No action is needed — their new positions are a natural consequence of other changes.",
"GuidanceMovedCountFormat": "{0} mod(s) moved",
"GuidanceMovedCountSingular": "1 mod moved"
```

Add corresponding translated keys to all 7 other locale files (de-DE, fr-FR, es-ES, it-IT, zh-CN, ja-JP, pt-BR).

### Step 6: Add Moved Guidance Brush

Add to `Styles/DiffBrushes.xaml`:

```xml
<SolidColorBrush x:Key="DiffBrush.GuidanceMovedBackground" Color="#332196F3" />
```

This follows the existing `#33{color}` pattern for sidebar card backgrounds (33 = 20% alpha).

### Step 7: Add Moved Guidance ViewModel Properties

Add to `ViewModels/DiffDialogViewModel.cs`:

**Count property** (in the counts section):
```csharp
public int MovedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Moved);
```

**Visibility property** (in the sidebar visibility section):
```csharp
public bool ShowMovedGuidance => HasDifferences && MovedModCount > 0;
```

**Count text property** (in the count display section):
```csharp
public string MovedModCountText => MovedModCount == 1
    ? Texts.GuidanceMovedCountSingular
    : string.Format(Texts.GuidanceMovedCountFormat, MovedModCount);
```

**Pass-through text properties** (in the sidebar text section):
```csharp
public string GuidanceMovedTitle => Texts.GuidanceMovedTitle;
public string GuidanceMovedBody => Texts.GuidanceMovedBody;
```

Note: `UpdateDiffState()` changes in Step 11 will cover notification for these properties via `OnPropertyChanged(string.Empty)`.

### Step 8: Add Moved Guidance Text Properties to DiffDialogTexts

Add to `ViewTexts/DiffDialogTexts.cs`:

**Properties:**
```csharp
public string GuidanceMovedTitle => _localization.GetString("DiffDialog", "GuidanceMovedTitle");
public string GuidanceMovedBody => _localization.GetString("DiffDialog", "GuidanceMovedBody");
public string GuidanceMovedCountFormat => _localization.GetString("DiffDialog", "GuidanceMovedCountFormat");
public string GuidanceMovedCountSingular => _localization.GetString("DiffDialog", "GuidanceMovedCountSingular");
```

**OnCultureChanged additions** (4 entries):
```csharp
OnPropertyChanged(nameof(GuidanceMovedTitle));
OnPropertyChanged(nameof(GuidanceMovedBody));
OnPropertyChanged(nameof(GuidanceMovedCountFormat));
OnPropertyChanged(nameof(GuidanceMovedCountSingular));
```

### Step 9: Add Moved Guidance Sidebar Card to DiffWindow.xaml

Insert a new card between the Added card and the No Changes card in `Views/DiffWindow.xaml`, following the identical pattern:

```xml
<!-- Moved card — Informational (blue) -->
<materialDesign:Card UniformCornerRadius="8"
                     Margin="0,0,0,8"
                     Background="{DynamicResource DiffBrush.GuidanceMovedBackground}"
                     Visibility="{Binding ShowMovedGuidance, Converter={StaticResource BooleanToVisibilityConverter}}">
    <StackPanel Margin="10,8">
        <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
            <materialDesign:PackIcon Kind="SwapVertical"
                                     Width="18" Height="18"
                                     VerticalAlignment="Center"
                                     Margin="0,0,6,0"
                                     Foreground="{DynamicResource DiffBrush.GuidanceMovedForeground}" />
            <TextBlock Text="{Binding GuidanceMovedTitle}"
                       d:Text="Moved Mods — Informational"
                       FontWeight="SemiBold"
                       TextWrapping="Wrap"
                       VerticalAlignment="Center"
                       Foreground="{DynamicResource MaterialDesignBody}" />
        </StackPanel>
        <TextBlock Text="{Binding MovedModCountText}"
                   d:Text="1 mod moved"
                   FontSize="11"
                   Margin="0,0,0,4"
                   Foreground="{DynamicResource MaterialDesignBodyLight}" />
        <TextBlock Text="{Binding GuidanceMovedBody}"
                   d:Text="These mods changed position due to other changes."
                   TextWrapping="Wrap"
                   FontSize="12"
                   Foreground="{DynamicResource MaterialDesignBody}" />
    </StackPanel>
</materialDesign:Card>
```

### Step 10: Add Moved Guidance Unit Tests

Add tests to `DiffDialogViewModelTests.cs`:

1. **`ShowMovedGuidance_WhenMovedModsPresent_ReturnsTrue`**: Create ViewModel with a Moved line. Assert `ShowMovedGuidance == true`.
2. **`ShowMovedGuidance_WhenNoMovedMods_ReturnsFalse`**: Create ViewModel with only Added lines. Assert `ShowMovedGuidance == false`.
3. **`MovedModCount_ReturnsCorrectCount`**: Create ViewModel with 2 Moved + 1 Added. Assert `MovedModCount == 2`.
4. **`MovedModCountText_Singular`**: Create ViewModel with 1 Moved. Assert text equals `"1 mod moved"`.
5. **`MovedModCountText_Plural`**: Create ViewModel with 3 Moved. Assert text equals `"3 mod(s) moved"`.

### Step 11: Simplify UpdateDiffState() Notifications

Replace the 24 individual `OnPropertyChanged(nameof(...))` calls in `UpdateDiffState()` with a single `OnPropertyChanged(string.Empty)`. The `HasDifferences = ...` assignment stays because it uses `SetProperty` (which fires its own notification and is a write, not just a refresh).

Also update `GuidanceVisibility_UpdatesAfterDiffRefresh` in `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs`. This test currently asserts that specific property names (`"ShowRemovalGuidance"`, `"ShowAddedGuidance"`, `"ShowNoChangesGuidance"`) appear in the `PropertyChanged` event list. After the `string.Empty` change, only `""` appears. Update the assertions to check `changedProperties.Contains(string.Empty)` (or equivalently `changedProperties.Contains("")`) instead.

Before:
```csharp
private void UpdateDiffState()
{
    HasDifferences = DiffLines.Any(line => line.ChangeType != DiffChangeType.Unchanged && line.ChangeType != DiffChangeType.Separator);
    ScrollTargetIndex = ComputeScrollTargetIndex();
    OnPropertyChanged(nameof(ShowSortingRecommendation));
    OnPropertyChanged(nameof(AddedMods));
    // ... 22 more lines ...
}
```

After:
```csharp
private void UpdateDiffState()
{
    HasDifferences = DiffLines.Any(line => line.ChangeType != DiffChangeType.Unchanged && line.ChangeType != DiffChangeType.Separator);
    ScrollTargetIndex = ComputeScrollTargetIndex();
    OnPropertyChanged(string.Empty);
}
```

### Step 12: Remove Dead `DependentCause_Added` Locale Key

Delete the `"DependentCause_Added"` line from all 8 locale JSON files:
- `ViewTexts/Locales/en-US.json`
- `ViewTexts/Locales/de-DE.json`
- `ViewTexts/Locales/fr-FR.json`
- `ViewTexts/Locales/es-ES.json`
- `ViewTexts/Locales/it-IT.json`
- `ViewTexts/Locales/zh-CN.json`
- `ViewTexts/Locales/ja-JP.json`
- `ViewTexts/Locales/pt-BR.json`

### Step 13: Update Documentation

Update the following manifest documents:

- **`Docs/Agents/project-manifest/api-surface.md`**: Add `BuildConfirmationMessage()` to `DiffDialogViewModel` section. Add Moved guidance properties (`ShowMovedGuidance`, `MovedModCount`, `MovedModCountText`, `GuidanceMovedTitle`, `GuidanceMovedBody`). Add `GuidanceMoved*` properties to `DiffDialogTexts` section.
- **`Docs/Agents/project-manifest/ui-design.md`**: Add `DiffBrush.*Foreground` entries to the brush table. Add Moved card to sidebar layout section.
- **`Docs/Agents/project-manifest/localization.md`**: Update key count for `DiffDialog` section. Add Moved guidance key group to any enumeration of sidebar keys.
- **`Docs/Agents/project-manifest/data-flows.md`**: Add `MovedModCount`, `MovedModCountText`, `ShowMovedGuidance` to the `UpdateDiffState()` sidebar property enumeration (or note the `OnPropertyChanged(string.Empty)` simplification).
- **`Docs/Agents/project-manifest/constraints.md`**: Note the `OnPropertyChanged(string.Empty)` pattern in the ViewModel test-seam section as an established convention.

## Dependencies
- Step 2 depends on Step 1 (tests call extracted method)
- Step 4 depends on Step 3 (XAML references require brush resources to exist)
- Step 9 depends on Steps 5, 6, 7, 8 (card XAML binds to ViewModel properties and brushes)
- Step 10 depends on Step 7 (tests call ViewModel properties)
- Step 11 has no dependencies but should run after Steps 7 (new Moved properties no longer need explicit notification)
- Step 12 is independent
- Step 13 depends on all prior steps

## Required Components
- `ViewModels/DiffDialogViewModel.cs` — modify (extract method, add properties, simplify notifications)
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — modify (add 7 new tests, update 1 existing test)
- `Styles/DiffBrushes.xaml` — modify (add 8 brush resources: 7 foreground in Step 3, 1 card background in Step 6)
- `Views/DiffWindow.xaml` — modify (replace 9 inline hex values, add Moved card)
- `ViewTexts/DiffDialogTexts.cs` — modify (add 4 properties + 4 OnCultureChanged entries)
- `ViewTexts/Locales/en-US.json` — modify (add 4 keys, remove 1 key)
- `ViewTexts/Locales/de-DE.json` — modify (add 4 keys, remove 1 key)
- `ViewTexts/Locales/fr-FR.json` — modify (add 4 keys, remove 1 key)
- `ViewTexts/Locales/es-ES.json` — modify (add 4 keys, remove 1 key)
- `ViewTexts/Locales/it-IT.json` — modify (add 4 keys, remove 1 key)
- `ViewTexts/Locales/zh-CN.json` — modify (add 4 keys, remove 1 key)
- `ViewTexts/Locales/ja-JP.json` — modify (add 4 keys, remove 1 key)
- `ViewTexts/Locales/pt-BR.json` — modify (add 4 keys, remove 1 key)
- `Docs/Agents/project-manifest/api-surface.md` — modify
- `Docs/Agents/project-manifest/ui-design.md` — modify
- `Docs/Agents/project-manifest/localization.md` — modify
- `Docs/Agents/project-manifest/data-flows.md` — modify
- `Docs/Agents/project-manifest/constraints.md` — modify

## Assumptions
- `ObservableObject.OnPropertyChanged(string.Empty)` does not invoke `partial void On{Property}Changed()` hooks for `[ObservableProperty]` fields — only raises the `PropertyChanged` event. This is the documented CommunityToolkit.Mvvm behavior.
- The Moved sidebar card uses `DiffBrush.GuidanceMovedForeground` (`#2196F3`, blue) for its `PackIcon` foreground, consistent with the sidebar's icon-matches-background-hue pattern (every existing sidebar card pairs its icon color with its card background hue: red/red, orange/orange, purple/purple, green/green). The diff-list Moved icon continues to use `DiffBrush.MovedForeground` (`#FFA726`, orange) for contrast against the blue row background.
- The `DependentCause_Added` key is truly unreferenced — verified by grep across all `*.cs` files returning zero matches.

## Constraints
- Tooling constraint: No typographic quotes (`U+201C`/`U+201D`) in locale JSON files — the `edit_file` tool transcribes them as ASCII 34. Use `「」` (U+300C/U+300D) for zh-CN and ja-JP quoting (documented in `localization.md`).
- UTF-8 without BOM for all text files.

## Out of Scope
- O-2: No Changes card explicit `Background` brush — intentional neutral-state design, documented in XAML comment.
- O-4: Locale file structural grouping for `CountFormat`/`CountSingular` pairs — requires format migration.
- O-5: ViewTexts XML `<summary>` + `<remarks>` doc consistency across all ViewText classes — cosmetic pass, low value.
- CommunityToolkit.Mvvm `[ObservableProperty]` source-generator migration for `DiffDialogViewModel` — a separate refactor plan if ever pursued.

## Acceptance Criteria

- AC-01: `BuildConfirmationMessage()` exists as an `internal` method on `DiffDialogViewModel`, returns `string?`, and is called by `UpdateReferenceWithConfirmationAsync()`.
- AC-02: `BuildConfirmationMessage()` returns `null` when only Added/Replaced/Moved/Unchanged lines are present.
- AC-03: `BuildConfirmationMessage()` returns a formatted message containing removed and/or inserted counts when those change types are present.
- AC-04: 2 new unit tests verify AC-02 and AC-03. Zero skipped tests remain in `DiffDialogViewModelTests.cs`.
- AC-05: All inline foreground hex literals in `DiffWindow.xaml` are replaced with `{DynamicResource DiffBrush.*Foreground}` references.
- AC-06: 7 named foreground `SolidColorBrush` resources exist in `DiffBrushes.xaml`.
- AC-07: A Moved guidance sidebar card appears in `DiffWindow.xaml` when `ShowMovedGuidance` is true.
- AC-08: `ShowMovedGuidance`, `MovedModCount`, `MovedModCountText`, `GuidanceMovedTitle`, and `GuidanceMovedBody` properties exist on `DiffDialogViewModel`.
- AC-09: 4 new `GuidanceMoved*` locale keys exist in all 8 locale files.
- AC-10: 5 new unit tests verify Moved guidance ViewModel properties (visibility, count, count text singular/plural).
- AC-11: `UpdateDiffState()` uses `OnPropertyChanged(string.Empty)` instead of individual calls; `GuidanceVisibility_UpdatesAfterDiffRefresh` is updated to assert `changedProperties.Contains(string.Empty)` instead of specific property names.
- AC-12: `DependentCause_Added` key is removed from all 8 locale files.
- AC-13: All existing tests pass (495 passing + 2 skipped currently; after removing 2 skipped (AC-04) and adding 7 new: **502 tests, 0 skipped**).
- AC-14: Documentation manifests are updated to reflect all changes.

## Testing Strategy
All new logic is testable through the existing test-seam constructor. `BuildConfirmationMessage()` is a pure function of `DiffLines` state and `Texts` formatting — no mocking needed. Moved guidance properties follow the same test pattern as existing sidebar properties. The `OnPropertyChanged(string.Empty)` change requires updating `GuidanceVisibility_UpdatesAfterDiffRefresh`: that test currently asserts specific property names (`"ShowRemovalGuidance"` etc.) that will no longer appear after the simplification — only `string.Empty` will be raised. The test is updated in Step 11 to assert `changedProperties.Contains(string.Empty)`. Brush consolidation and XAML changes are verified by building the project successfully.

## Test Plan

- `DiffDialogViewModelTests.BuildConfirmationMessage_RiskyChanges_IncludesRemovedCount` — Asserts non-null result with removed + inserted detail strings — AC-03, AC-04
- `DiffDialogViewModelTests.BuildConfirmationMessage_SafeChanges_ReturnsNull` — Asserts null result for Added + Replaced only — AC-02, AC-04
- `DiffDialogViewModelTests.ShowMovedGuidance_WhenMovedModsPresent_ReturnsTrue` — Asserts `ShowMovedGuidance == true` — AC-08, AC-10
- `DiffDialogViewModelTests.ShowMovedGuidance_WhenNoMovedMods_ReturnsFalse` — Asserts `ShowMovedGuidance == false` — AC-08, AC-10
- `DiffDialogViewModelTests.MovedModCount_ReturnsCorrectCount` — Asserts count = 2 for mixed collection — AC-08, AC-10
- `DiffDialogViewModelTests.MovedModCountText_Singular` — Asserts `"1 mod moved"` — AC-08, AC-10
- `DiffDialogViewModelTests.MovedModCountText_Plural` — Asserts `"3 mod(s) moved"` — AC-08, AC-10
- `DiffDialogViewModelTests.GuidanceVisibility_UpdatesAfterDiffRefresh` (modified) — Updated to assert `changedProperties.Contains(string.Empty)` instead of specific named property assertions — AC-11
- Full test suite pass (502 tests, 0 skipped) — AC-13

## Documentation Updates

- `Docs/Agents/project-manifest/api-surface.md` — Add `BuildConfirmationMessage()`, Moved guidance properties to ViewModel section; add `GuidanceMoved*` to Texts section
- `Docs/Agents/project-manifest/ui-design.md` — Add `DiffBrush.*Foreground` entries to brush table; add Moved card to sidebar layout
- `Docs/Agents/project-manifest/localization.md` — Update DiffDialog key count; add Moved guidance key group
- `Docs/Agents/project-manifest/data-flows.md` — Update `UpdateDiffState()` description to note `OnPropertyChanged(string.Empty)` simplification; add Moved properties
- `Docs/Agents/project-manifest/constraints.md` — Add `OnPropertyChanged(string.Empty)` as documented convention in ViewModel section

## Deferred Items

| # | Deferred Item | Origin | Reason Deferred | Notes |
|---|---|---|---|---|
| 1 | No Changes card explicit `Background` brush | O-2 from prior synthesis | Intentional neutral-state design; XAML comment documents the decision | Reconsider if a second view adopts the sidebar pattern and needs consistent neutral-card styling |
| 2 | Locale file structural grouping (JSONC/YAML) | O-4 from prior synthesis | Requires format migration across all 8 locales and tooling updates | Reconsider when adding 10+ new key groups or if translation tooling mandates structured formats |
| 3 | ViewTexts `<summary>` + `<remarks>` XML doc consistency | O-5 from prior synthesis | Cosmetic pass across many files with no functional impact | Reconsider during a dedicated documentation quality sweep |
| 4 | CommunityToolkit.Mvvm `[ObservableProperty]` source-gen migration | D-2/Rec #3 from prior synthesis (partial) | Full migration restructures every computed property and is a separate refactor scope | Reconsider when adding a new ViewModel with many computed properties, or when the maintenance burden of `OnPropertyChanged(string.Empty)` proves insufficient |

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **`OnPropertyChanged(string.Empty)` fires for command properties** | Benign — WPF rebinds `CanExecute` but the commands' `CanExecute` delegates are cheap. Verified by running full test suite. |
| **`GuidanceVisibility_UpdatesAfterDiffRefresh` breaks under `string.Empty`** | Addressed in Step 11: update the test to assert `changedProperties.Contains(string.Empty)` instead of specific property names. |
| **Moved card adds visual weight to sidebar** | Card uses information-blue tone (lowest visual priority) and only appears when Moved lines exist. The sidebar's risk hierarchy (red → orange → purple → green → blue → neutral) remains intuitive. |
| **Typographic quote corruption in locale JSON edits** | Use `「」` (U+300C/U+300D) for zh-CN and ja-JP, as documented in `localization.md`. Avoid Unicode typographic quotes in all locale files. |

## Recommended Workflow
- **Workflow:** ledger
- **Rationale:** 5 distinct concerns across 18+ files with locale translations, XAML changes, ViewModel modifications, and test additions benefit from structured WP tracking and formal review.
