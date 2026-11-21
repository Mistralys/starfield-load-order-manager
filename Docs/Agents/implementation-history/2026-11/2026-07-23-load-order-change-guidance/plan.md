# Plan

## Plan Audit Cycles
- Audits: 1 — Plan Auditor v1.7.0
- Architectural Reviews: 1 — Plan Architect Reviewer v2.2.0

## Prior Project Context
The LCS diff pipeline (2026-07-21, 2026-07-22) established the classification system (`Added`, `Removed`, `Moved`, `Replaced`, `Inserted`) and dependent change tracking. The "Show all mods" toggle (2026-07-23) introduced the `FilteredDiffLines` CollectionView pattern. Both are stable foundations for this UI-layer enhancement.

The repository's mid-term strategic goal is to "improve the user experience overall in the GUI with more guidance, and clearer screens." This plan directly addresses that goal by adding contextual risk guidance to the application's most information-dense screen.

## Summary
Add a recommendations sidebar panel to the DiffWindow that dynamically displays contextual guidance about the consequences of detected load order changes. The sidebar replaces the banner-based approach considered in the research phase, providing more space for verbose explanations without interfering with the existing button controls. Additionally, per-item tooltips on all change type icons and a risk-gated confirmation dialog on "Accept Changes" (shown only when removed or inserted mods are present) provide defense-in-depth guidance at up to three interaction stages: passive browsing (sidebar), hovering (tooltips), and committing (risky-changes confirmation).

## Architectural Context
The DiffWindow (`Views/DiffWindow.xaml`) currently uses a single-column `Grid` layout (7 rows) inside a `DockPanel`. The diff ListView occupies Row 2 with star-sizing. Below it sit the sorting recommendation banner (Row 3), status message (Row 4), no-differences message (Row 5), and button row (Row 6). The ViewModel (`DiffDialogViewModel`) exposes computed properties like `HasDifferences`, `HasAddedMods`, `HasInsertedMods`, and `ShowSortingRecommendation` that drive UI state. Localized strings come from `DiffDialogTexts`, which reads from the `DiffDialog` section of locale JSON files.

The existing `InsertedWarningTooltip` on the Inserted icon's `PackIcon` is the sole per-item tooltip — other change types have no guidance. The "Accept Changes" confirmation dialog uses a static generic message regardless of what types of changes are present.

## Approach / Architecture
Split the DiffWindow content area into a two-column layout: the existing diff list on the left and a new recommendations sidebar on the right. The sidebar is a scrollable panel that shows contextual recommendation cards based on which change types are present in the current diff. Cards are driven by boolean visibility properties on the ViewModel (e.g., `ShowRemovalGuidance`, `ShowInsertionGuidance`, `ShowSafeChangesNote`), so they appear and disappear dynamically as the diff state changes.

The sidebar uses `materialDesign:Card` controls (from MaterialDesignThemes v5) with icon, heading, and body text for each recommendation. Cards are color-coded by risk level (red for removals, orange for insertions, green for safe additions, blue for replacements). The sidebar panel is always visible; individual cards appear and disappear based on which change types are present.

In parallel, per-item tooltips are added to all change type icons that currently lack one (Added, Removed, Replaced, Moved — the Inserted trigger already has `InsertedWarningTooltip` and is left unchanged), and the "Accept Changes" confirmation dialog is enhanced with a risk-specific message shown only when removed or inserted mods are present. Safe-only changes (additions and replacements) skip the confirmation dialog as they do today — the sidebar already communicates their safety.

## Rationale
- **Sidebar over banners**: The user specifically requested a sidebar to avoid cluttering the button area. A sidebar provides significantly more text space than stacking banners, allows multiple recommendations simultaneously without vertical compression, and naturally scales with window height.
- **Cards over plain text**: Material Design cards provide visual hierarchy and grouping. Each card addresses one concern, making the sidebar scannable.
- **Three-layer guidance**: Sidebar (always visible), tooltips (on hover), and confirmation dialog (at commit) ensure users encounter guidance regardless of their interaction pattern.
- **No changes to diff logic**: All changes are purely in the UI/ViewModel/localization layers. The `DiffService` classification pipeline and `DiffLineModel` data structures remain untouched.

## Considered Alternatives

| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| Guidance container | Right sidebar panel | Stacked banners below diff list | Sidebar gives more space, doesn't compress button row, less intrusive. Banners are simpler but clutter the vertical layout with multiple concerns. |
| Sidebar card element | `materialDesign:Card` (semantic MD control) | `Border + MaterialDesignCardBackground` (current informal pattern) | `materialDesign:Card` is the correct semantic control for card-like information units in Material Design. It provides `UniformCornerRadius` (a `double` DP), content clipping at corners, and elevation support. `Border` is simpler but doesn't clip children and perpetuates an informal pattern. Introducing the proper control here sets a consistent precedent for future card elements. |
| Sidebar text binding | Pass-through properties on `DiffDialogViewModel` | Direct `{Binding Texts.*}` path in XAML | `DiffWindow.xaml` already has a consistent convention of binding to pass-through properties (e.g., `ShowAllModsToggleText`, `NoDifferencesMessage`). Mixing in `Texts.*` sub-paths creates two conventions in one file. Pass-through properties are one more line per string in the ViewModel but ensure a single, consistent convention throughout. |
| Sidebar visibility gating | Sidebar container always rendered; individual cards control their own visibility | `ShowGuidanceSidebar` boolean property on ViewModel | `ShowGuidanceSidebar` simplifies to `HasDifferences \|\| !HasDifferences` (always `true`), making it dead code. The sidebar is genuinely always-visible; no container-level visibility binding is needed. Individual `Show*Guidance` card properties correctly handle card-level show/hide. |
| Safe-changes confirmation | Preserve current behavior (no dialog for safe-only changes) | Add a lightweight `ConfirmUpdateMessage_SafeChanges` dialog | The current code intentionally skips the confirmation dialog when only additions/replacements are present. The sidebar's "New Mods — Safe" and "Replaced Mods — Safe" cards already communicate safety. Adding a blocking confirmation on top creates friction without adding information. The risky-changes dialog enhancement (for removals/insertions) proceeds unchanged. |
| Sidebar visibility | Always visible | Collapsible/toggleable sidebar | Always-visible ensures guidance is never missed. Toggle adds interaction complexity and risks users never opening it. |
| Recommendation granularity | One card per change type detected | Single combined message | Per-card approach is scannable and allows color-coding. Single message would be a wall of text. |
| Window width increase | 1060px default, 720px minimum | Keep current 810px and compress diff list | Increasing width preserves diff list readability. Sidebar needs ~250px; diff list must not shrink below current effective width. |

## Pattern Alignment
- **Zero-hardcoding localization** (`Docs/Agents/project-manifest/localization.md`): All new strings go through locale JSON → `DiffDialogTexts` → XAML binding. No departures.
- **Semantic brushes** (`Styles/DiffBrushes.xaml`): New sidebar brushes follow the `DiffBrush.*` naming convention. No departures.
- **ViewModel computed properties** (`ViewModels/DiffDialogViewModel.cs`): New boolean properties follow the existing `HasAddedMods` / `HasInsertedMods` pattern, refreshed in `UpdateDiffState()`. No departures.
- **Text ViewModel pattern** (`ViewTexts/DiffDialogTexts.cs`): New text properties follow the existing one-property-per-key pattern with `OnCultureChanged` refresh. No departures.
- **`materialDesign:Card` as semantic card element** (departure from current informal pattern): Existing card-like containers in the project use `Border + MaterialDesignCardBackground`. Sidebar guidance cards use the proper `materialDesign:Card` control instead, which provides `UniformCornerRadius` (a single `double` DP), content clipping at rounded corners, and elevation support. This intentional departure sets a consistent precedent for future card elements; existing `Border`-based containers are not migrated here.
- **PackIcon tooltip pattern** (`Views/DiffWindow.xaml` Inserted trigger): Extending the existing `ToolTip` setter on `PackIcon` style triggers to Added, Removed, Replaced, and Moved. The existing `InsertedWarningTooltip` binding is left unchanged. No departures.
- **Confirmation dialog via event** (`ViewModels/DiffDialogViewModel.cs`): Enhanced message is still built in ViewModel and delivered via `ConfirmationRequested` event. No departures.

## Detailed Steps

### Step 1: Add Locale Strings to en-US.json

Add the following keys to the `DiffDialog` section of `ViewTexts/Locales/en-US.json`:

**Per-item tooltips** (Added, Removed, Replaced, Moved only — Inserted already has `InsertedWarningTooltip` and is unchanged):
- `AddedTooltip`: `"This mod was added at the end of the load order. This is safe and won't affect existing mod positions."`
- `RemovedTooltip`: `"This mod was removed from the load order. All subsequent mods shifted up, which can break save game references. Right-click for options."`
- `ReplacedTooltip`: `"This mod was replaced at the same position. No other mods are affected — this is a safe change."`
- `MovedTooltip`: `"This mod's position changed due to other modifications in the load order."`

**Sidebar recommendation cards:**
- `GuidanceSidebarTitle`: `"Recommendations"`
- `GuidanceRemovalTitle`: `"Removed Mods — High Risk"`
- `GuidanceRemovalBody`: `"Removing a mod shifts all subsequent mod positions. Save games store references by position — shifted mods can cause missing items, broken quests, or crashes.\n\nRecommendation: Right-click each removed mod and choose 'Replace with...' to substitute a placeholder mod. This preserves the positions of all mods below."`
- `GuidanceRemovalCountFormat`: `"{0} mod(s) removed"`
- `GuidanceRemovalCountSingular`: `"1 mod removed"`
- `GuidanceInsertionTitle`: `"Inserted Mods — Moderate Risk"`
- `GuidanceInsertionBody`: `"Mods inserted in the middle of the list shift all subsequent positions down. This has the same risks as removing a mod.\n\nRecommendation: Click 'Fix Load Order' to sort the list. Sorting moves inserted mods to the end, restoring the original positions of existing mods."`
- `GuidanceInsertionCountFormat`: `"{0} mod(s) inserted"`
- `GuidanceInsertionCountSingular`: `"1 mod inserted"`
- `GuidanceReplacementTitle`: `"Replaced Mods — Safe"`
- `GuidanceReplacementBody`: `"Replaced mods occupy the same position as the original. No other mods are shifted — this is a safe operation."`
- `GuidanceReplacementCountFormat`: `"{0} mod(s) replaced"`
- `GuidanceReplacementCountSingular`: `"1 mod replaced"`
- `GuidanceAddedTitle`: `"New Mods — Safe"`
- `GuidanceAddedBody`: `"Mods added at the end of the load order do not affect existing positions. This is always safe."`
- `GuidanceAddedCountFormat`: `"{0} mod(s) added"`
- `GuidanceAddedCountSingular`: `"1 mod added"`
- `GuidanceNoChangesTitle`: `"No Changes"`
- `GuidanceNoChangesBody`: `"The current load order matches the reference file."`

**Enhanced confirmation dialog** (shown only when removed or inserted mods are present; safe-only changes skip the dialog as today):
- `ConfirmUpdateMessage_RiskyChanges`: `"You are about to accept changes that include risky modifications:\n\n{0}\nAccepting these changes will update the reference file. This action creates a new version in the history, but the position shifts will become permanent.\n\nAre you sure you want to accept all changes?"`
- `ConfirmRiskyDetail_Removals`: `"• {0} removed mod(s) — positions shifted, save references may break"`
- `ConfirmRiskyDetail_Insertions`: `"• {0} inserted mod(s) — subsequent positions shifted"`
- `ConfirmRiskyDetail_RemovedSingular`: `"• 1 removed mod — positions shifted, save references may break"`
- `ConfirmRiskyDetail_InsertedSingular`: `"• 1 inserted mod — subsequent positions shifted"`

### Step 2: Add Locale Strings to All Other Locale Files

Add the same keys from Step 1 to all 7 remaining locale files (`de-DE.json`, `fr-FR.json`, `es-ES.json`, `it-IT.json`, `zh-CN.json`, `ja-JP.json`, `pt-BR.json`) with appropriate translations. Follow the formatting rules in `Docs/Agents/project-manifest/localization.md` — particularly the Asian locale guidelines for zh-CN and ja-JP (aki spacing, full-width punctuation, proper quotation marks, menu hotkey placement).

### Step 3: Add Text Properties to DiffDialogTexts

Add new read-only properties to `ViewTexts/DiffDialogTexts.cs` for each new locale key added in Step 1. Each property follows the existing pattern:

```csharp
public string GuidanceSidebarTitle => _localization.GetString("DiffDialog", "GuidanceSidebarTitle");
```

Add all new property names to the `OnCultureChanged()` handler to ensure they refresh on language switch.

### Step 4: Add Sidebar Brush Resources

Add the following brush resources to `Styles/DiffBrushes.xaml`:

```xml
<!-- Sidebar guidance card backgrounds -->
<SolidColorBrush x:Key="DiffBrush.GuidanceRemovalBackground" Color="#33F44336" />
<SolidColorBrush x:Key="DiffBrush.GuidanceInsertionBackground" Color="#33FF9800" />
<SolidColorBrush x:Key="DiffBrush.GuidanceReplacementBackground" Color="#339C27B0" />
<SolidColorBrush x:Key="DiffBrush.GuidanceAddedBackground" Color="#334CAF50" />
<SolidColorBrush x:Key="DiffBrush.GuidanceSidebarBackground" Color="#0AFFFFFF" />
```

These use 20% alpha versions of the same base colors as the existing diff row brushes, providing visual consistency between the diff list and the sidebar.

### Step 5: Add Computed Properties to DiffDialogViewModel

Add the following computed properties to `ViewModels/DiffDialogViewModel.cs`:

```csharp
// Change type counts
public int RemovedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Removed);
public int InsertedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Inserted);
public int ReplacedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Replaced);
public int AddedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Added);

// Sidebar card visibility (sidebar container has no visibility binding — it is always rendered)
public bool HasRemovedMods => RemovedModCount > 0;
public bool ShowRemovalGuidance => HasDifferences && HasRemovedMods;
public bool ShowInsertionGuidance => HasDifferences && HasInsertedMods;
public bool ShowReplacementGuidance => HasDifferences && ReplacedModCount > 0;
public bool ShowAddedGuidance => HasDifferences && HasAddedMods;
public bool ShowNoChangesGuidance => !HasDifferences;

// Count display strings (singular/plural)
public string RemovedModCountText => RemovedModCount == 1
    ? Texts.GuidanceRemovalCountSingular
    : string.Format(Texts.GuidanceRemovalCountFormat, RemovedModCount);
public string InsertedModCountText => InsertedModCount == 1
    ? Texts.GuidanceInsertionCountSingular
    : string.Format(Texts.GuidanceInsertionCountFormat, InsertedModCount);
public string ReplacedModCountText => ReplacedModCount == 1
    ? Texts.GuidanceReplacementCountSingular
    : string.Format(Texts.GuidanceReplacementCountFormat, ReplacedModCount);
public string AddedModCountText => AddedModCount == 1
    ? Texts.GuidanceAddedCountSingular
    : string.Format(Texts.GuidanceAddedCountFormat, AddedModCount);

// Pass-through text properties for sidebar card headings and body text
// (DiffWindow.xaml binds to VM properties, not Texts.* sub-paths — consistent with existing bindings)
public string GuidanceSidebarTitle => Texts.GuidanceSidebarTitle;
public string GuidanceRemovalTitle => Texts.GuidanceRemovalTitle;
public string GuidanceRemovalBody => Texts.GuidanceRemovalBody;
public string GuidanceInsertionTitle => Texts.GuidanceInsertionTitle;
public string GuidanceInsertionBody => Texts.GuidanceInsertionBody;
public string GuidanceReplacementTitle => Texts.GuidanceReplacementTitle;
public string GuidanceReplacementBody => Texts.GuidanceReplacementBody;
public string GuidanceAddedTitle => Texts.GuidanceAddedTitle;
public string GuidanceAddedBody => Texts.GuidanceAddedBody;
public string GuidanceNoChangesTitle => Texts.GuidanceNoChangesTitle;
public string GuidanceNoChangesBody => Texts.GuidanceNoChangesBody;

// Tooltip pass-through properties for PackIcon bindings (Added, Removed, Replaced, Moved only)
public string AddedTooltip => Texts.AddedTooltip;
public string RemovedTooltip => Texts.RemovedTooltip;
public string ReplacedTooltip => Texts.ReplacedTooltip;
public string MovedTooltip => Texts.MovedTooltip;
```

Add `OnPropertyChanged` calls for all new properties in `UpdateDiffState()`.

### Step 6: Restructure DiffWindow XAML Layout

Modify `Views/DiffWindow.xaml` to wrap the existing content area in a two-column layout. The key structural change:

1. **Increase default window size**: Change `Width="810"` to `Width="1060"` and `MinWidth="480"` to `MinWidth="720"`.

2. **Insert a Grid with two columns** at the level where Grid.Row="2" (the diff ListView) currently sits. The left column gets the diff ListView (star-sized), and the right column gets the recommendations sidebar (fixed width ~260px).

3. The two-column Grid replaces the current single Border+ListView in Row 2. All rows above (description, checkbox) and below (sorting banner, status, buttons) remain unchanged.

Structural outline of the modified Row 2:

```xml
<Grid Grid.Row="2">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />        <!-- Diff list -->
        <ColumnDefinition Width="260" />      <!-- Sidebar -->
    </Grid.ColumnDefinitions>

    <!-- Existing diff ListView in Column 0 -->
    <Border Grid.Column="0" ...>
        <ListView ... />
    </Border>

    <!-- Recommendations sidebar in Column 1 — always rendered; card-level visibility bindings control content -->
    <Border Grid.Column="1" Margin="8,0,0,0"
            Background="{DynamicResource DiffBrush.GuidanceSidebarBackground}"
            BorderThickness="1"
            BorderBrush="{DynamicResource MaterialDesignDivider}"
            CornerRadius="4">
        <ScrollViewer VerticalScrollBarVisibility="Auto">
            <StackPanel Margin="12">
                <!-- Title -->
                <TextBlock Text="{Binding GuidanceSidebarTitle}" ... />
                <!-- Cards (each with Visibility bound to Show*Guidance) -->
                ...
            </StackPanel>
        </ScrollViewer>
    </Border>
</Grid>
```

### Step 7: Build Sidebar Recommendation Cards

Inside the sidebar's `StackPanel`, add one `materialDesign:Card` per guidance type. Each card follows this template:

```xml
<materialDesign:Card Margin="0,0,0,8"
                     Background="{DynamicResource DiffBrush.GuidanceRemovalBackground}"
                     UniformCornerRadius="8"
                     Padding="12"
                     Visibility="{Binding ShowRemovalGuidance, Converter={StaticResource BooleanToVisibilityConverter}}">
    <StackPanel>
        <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
            <materialDesign:PackIcon Kind="AlertCircleOutline" Width="18" Height="18"
                                     Foreground="#EF5350" VerticalAlignment="Center" Margin="0,0,6,0" />
            <TextBlock Text="{Binding GuidanceRemovalTitle}"
                       FontWeight="SemiBold" Foreground="{DynamicResource MaterialDesignBody}" />
        </StackPanel>
        <TextBlock Text="{Binding RemovedModCountText}"
                   FontSize="12" Foreground="{DynamicResource MaterialDesignBodyLight}" Margin="0,0,0,4" />
        <TextBlock Text="{Binding GuidanceRemovalBody}"
                   TextWrapping="Wrap" FontSize="12" Foreground="{DynamicResource MaterialDesignBody}" />
    </StackPanel>
</materialDesign:Card>
```

All other cards follow the same pattern: bind to `GuidanceInsertionTitle` / `GuidanceInsertionBody`, `GuidanceReplacementTitle` / `GuidanceReplacementBody`, `GuidanceAddedTitle` / `GuidanceAddedBody`, and `GuidanceNoChangesTitle` / `GuidanceNoChangesBody` respectively — never through a `Texts.*` sub-path.

Cards are ordered by risk severity: Removal (red) → Insertion (orange) → Replacement (blue/purple) → Added (green) → No Changes.

Each card uses:
- The corresponding `DiffBrush.Guidance*Background` brush
- A `PackIcon` matching the change type's icon color scheme
- The count text (singular/plural) as a subtitle
- The body text with full explanation and recommendations
- Visibility bound to `Show*Guidance` properties

### Step 8: Add Per-Item Tooltips to PackIcon Triggers

In `Views/DiffWindow.xaml`, add `ToolTip` setters to the existing `PackIcon` style triggers for `Added`, `Removed`, `Replaced`, and `Moved` types only. The `Inserted` trigger already binds to `InsertedWarningTooltip` and is left exactly as-is — no alias, no rename.

For example, add to the `Added` trigger:
```xml
<Setter Property="ToolTip" Value="{Binding DataContext.AddedTooltip, RelativeSource={RelativeSource AncestorType=ListView}}" />
```

Apply the same pattern for `RemovedTooltip`, `ReplacedTooltip`, and `MovedTooltip`. Each binding uses `DataContext.{ChangeType}Tooltip` with `RelativeSource AncestorType=ListView` to reach the `DiffDialogViewModel` from within the item template.

### Step 9: Enhance Confirmation Dialog

Modify `UpdateReferenceWithConfirmationAsync()` in `ViewModels/DiffDialogViewModel.cs` to build a risk-aware confirmation message:

1. Count removed and inserted mods from `DiffLines`.
2. If either count > 0, build a detail string by concatenating formatted `ConfirmRiskyDetail_Removals` / `ConfirmRiskyDetail_RemovedSingular` and `ConfirmRiskyDetail_Insertions` / `ConfirmRiskyDetail_InsertedSingular` lines as applicable, then embed them into `ConfirmUpdateMessage_RiskyChanges`. Pass the built message with a `Warning` icon to `ConfirmationRequestedEventArgs`.
3. If only safe changes (added/replaced) are present, preserve the current behavior — proceed without showing a confirmation dialog. The sidebar's "New Mods — Safe" and "Replaced Mods — Safe" cards already communicate safety to the user.

### Step 10: Update Manifest Documents

Update the following project manifest documents to reflect the new components:

- **`api-surface.md`**: Add new computed properties on `DiffDialogViewModel` (sidebar visibility, count, tooltip properties). Add new `DiffDialogTexts` properties.
- **`ui-design.md`**: Add sidebar documentation section: layout, card taxonomy, color coding, responsive behavior. Add new brush keys to the DiffBrushes table.
- **`file-tree.md`**: No new files — only modifications.
- **`constraints.md`**: Add entry documenting that the sidebar guidance is purely informational and does not block or modify any operations. Note that the sidebar does not replace the sorting recommendation banner (which remains in its existing position below the diff list, as it contains an actionable button).
- **`data-flows.md`**: Document that sidebar state is recomputed in `UpdateDiffState()` alongside existing computed properties.

## Dependencies
- Step 2 depends on Step 1 (en-US strings are the reference for translations).
- Steps 3–5 can proceed in parallel (they are independent C# changes).
- Steps 6–7 depend on Steps 3, 4, and 5 (XAML binds to ViewModel properties and brush resources).
- Step 8 depends on Steps 1 and 3 (tooltip strings must exist in locale and texts).
- Step 9 depends on Steps 1 and 3 (confirmation strings must exist).
- Step 10 depends on all other steps being complete.

## Required Components
- `ViewTexts/Locales/en-US.json` — add ~23 new DiffDialog keys
- `ViewTexts/Locales/de-DE.json` — translated strings
- `ViewTexts/Locales/fr-FR.json` — translated strings
- `ViewTexts/Locales/es-ES.json` — translated strings
- `ViewTexts/Locales/it-IT.json` — translated strings
- `ViewTexts/Locales/zh-CN.json` — translated strings (Asian formatting rules)
- `ViewTexts/Locales/ja-JP.json` — translated strings (Asian formatting rules)
- `ViewTexts/Locales/pt-BR.json` — translated strings
- `ViewTexts/DiffDialogTexts.cs` — ~23 new properties + `OnCultureChanged` entries
- `ViewModels/DiffDialogViewModel.cs` — ~25 new computed properties (counts, card visibility, pass-through text, tooltips) + `UpdateDiffState` refresh
- `Views/DiffWindow.xaml` — layout restructure (two-column Grid), sidebar panel, tooltip setters
- `Styles/DiffBrushes.xaml` — 5 new brush resources
- `Docs/Agents/project-manifest/api-surface.md` — new ViewModel API entries
- `Docs/Agents/project-manifest/ui-design.md` — sidebar design documentation
- `Docs/Agents/project-manifest/constraints.md` — sidebar behavior constraints
- `Docs/Agents/project-manifest/data-flows.md` — sidebar state computation

## Assumptions
- The sidebar width of 260px provides enough space for recommendation text without requiring excessive wrapping. This can be fine-tuned during implementation.
- The window width increase from 810px to 1060px is acceptable on typical displays (1920×1080 minimum assumed). The MinWidth of 720px ensures the sidebar doesn't collapse the diff list excessively on smaller screens.
- The `materialDesign:Card` control from MaterialDesignThemes v5.3.0 supports `UniformCornerRadius` (a `double` DP with default 4.0) and content clipping. Its `Background` property accepts any brush, including the custom `DiffBrush.Guidance*Background` brushes defined in Step 4. This has been verified against the library source.
- Translations for the 7 non-English locales can be provided by automated translation with manual review, following the existing pattern.

## Constraints
- **No changes to DiffService or DiffLineModel**: All changes are in the UI/ViewModel/localization layers. The classification pipeline and data model remain untouched.
- **Sorting recommendation banner remains**: The sidebar does not replace the existing sorting banner (Grid.Row="3"), which contains an actionable "Fix Load Order" button. The sidebar's insertion guidance card complements it with more detailed explanation.
- **Localization zero-hardcoding**: Every new user-facing string must go through the locale JSON → Text ViewModel → XAML binding chain.
- **ConfigInvalidOverlay must remain above sidebar**: The overlay at `Panel.ZIndex="1000"` must cover the entire window including the new sidebar.

## Out of Scope
- Replacing the sorting recommendation banner with sidebar content (the banner has an action button; the sidebar is informational only).
- Adding a collapsible/toggleable sidebar mechanism (always visible when differences exist).
- Custom tooltip controls or popup panels (standard WPF `ToolTip` is sufficient).
- Modifying the `DiffService` classification pipeline or `DiffLineModel` model.
- Adding guidance for `Moved` change type beyond a brief tooltip (moves are dependent changes caused by removals/insertions, which are already covered by the sidebar cards).
- Adding a "learn more" link or help documentation (out of scope for this plan; could be a future enhancement).

## Acceptance Criteria

- AC-01: The DiffWindow displays a recommendations sidebar to the right of the diff list when differences are detected.
- AC-02: The sidebar shows a "Removed Mods — High Risk" card with red styling when removed mods are present, explaining the risk and recommending "Replace with..." action.
- AC-03: The sidebar shows an "Inserted Mods — Moderate Risk" card with orange styling when inserted mods are present, explaining the risk and recommending sorting.
- AC-04: The sidebar shows a "Replaced Mods — Safe" card with purple/blue styling when replaced mods are present, confirming safety.
- AC-05: The sidebar shows a "New Mods — Safe" card with green styling when added mods are present, confirming safety.
- AC-06: Each sidebar card displays a count subtitle using singular/plural formatting (e.g., "1 mod removed" vs "3 mod(s) removed").
- AC-07: Sidebar cards appear and disappear dynamically as the diff state changes (e.g., after sorting, after replacing a mod).
- AC-08: Hovering over any change type icon (Added, Removed, Replaced, Moved, Inserted) in the diff list shows a tooltip explaining the change type and its implications.
- AC-09: The "Accept Changes" confirmation dialog displays a risk-specific message (with counts and risk detail lines) when removed or inserted mods are present. When only safe changes are present, the dialog is not shown and the operation proceeds directly.
- AC-10: The sidebar does not interfere with the existing sorting recommendation banner, button row, or ConfigInvalidOverlay.
- AC-11: The sidebar shows a "No Changes" card (`GuidanceNoChangesTitle`/`GuidanceNoChangesBody`) when `HasDifferences` is false.
- AC-12: The DiffWindow default width is increased to accommodate the sidebar without compressing the diff list below its current effective width.
- AC-13: All project manifest documents (api-surface, ui-design, constraints, data-flows) are updated to reflect the new sidebar components.

## Testing Strategy
All changes are in the UI/ViewModel/localization layers. Testing focuses on ViewModel computed property correctness (unit tests) and visual verification of the sidebar layout.

## Test Plan

**Testability note:** `DiffDialogViewModel`'s constructor requires a concrete `MainViewModel` with hard-coded coordinator dependencies (verified in `Tests/LoadOrderKeeper.Tests/ViewModels/MainViewModelTests.cs`). To make the 10 tests below viable, add an `internal` test-only constructor to `DiffDialogViewModel` that accepts a pre-populated `ObservableCollection<DiffLineModel>` and skips coordinator subscription. Tag it `[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]` to prevent accidental production use. The two confirmation-dialog tests (`ConfirmationMessage_RiskyChanges_IncludesRemovedCount`, `ConfirmationMessage_SafeChanges_SkipsDialog`) require the event-interception pattern (subscribe to `ConfirmationRequested` before triggering the command); they cannot reach `_mainViewModel.CreateReferenceCommand` through this constructor and should be marked `[Fact(Skip = "Requires MainViewModel")]` with a comment pointing to the MainViewModelTests barrier note.

- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `ShowRemovalGuidance_WhenRemovedModsPresent_ReturnsTrue`: Verify `ShowRemovalGuidance` returns true when `DiffLines` contains at least one `Removed` entry. — AC-02, AC-07
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `ShowRemovalGuidance_WhenNoRemovedMods_ReturnsFalse`: Verify false when no removed entries. — AC-07
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `ShowInsertionGuidance_WhenInsertedModsPresent_ReturnsTrue`: Verify `ShowInsertionGuidance` returns true when inserted entries exist. — AC-03, AC-07
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `ShowReplacementGuidance_WhenReplacedModsPresent_ReturnsTrue`: Verify `ShowReplacementGuidance` returns true when replaced entries exist. — AC-04, AC-07
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `ShowAddedGuidance_WhenAddedModsPresent_ReturnsTrue`: Verify `ShowAddedGuidance` returns true when added entries exist. — AC-05, AC-07
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `RemovedModCountText_Singular`: Verify `RemovedModCountText` uses singular format when count is 1. — AC-06
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `RemovedModCountText_Plural`: Verify `RemovedModCountText` uses plural format when count > 1. — AC-06
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `GuidanceVisibility_UpdatesAfterDiffRefresh`: Verify that sidebar visibility properties change correctly when `DiffLines` are replaced (simulating a diff refresh). — AC-07
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `ConfirmationMessage_RiskyChanges_IncludesRemovedCount`: Verify that the built confirmation message includes removal detail lines when removed mods are present. — AC-09
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `ConfirmationMessage_SafeChanges_SkipsDialog`: Verify that no confirmation event is raised when only additions/replacements are present (dialog is bypassed). — AC-09
- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — `ShowNoChangesGuidance_WhenNoDifferences_ReturnsTrue`: Verify `ShowNoChangesGuidance` returns true when `DiffLines` contains no changed entries (all `Unchanged`). — AC-11
- `Tests/LoadOrderKeeper.Tests/ViewTexts/LocalizationCompletenessTests.cs` — `AllLocaleFiles_HaveAllKeys_FromBaseLocale` already covers cross-locale key completeness automatically. No new test code is required once Steps 1 and 2 are complete. Run the existing test suite to confirm all 8 locale files contain the new DiffDialog keys. — AC-10

## Documentation Updates

- `Docs/Agents/project-manifest/api-surface.md` — Add new `DiffDialogViewModel` properties (`ShowRemovalGuidance`, `ShowInsertionGuidance`, `ShowReplacementGuidance`, `ShowAddedGuidance`, `ShowNoChangesGuidance`, `HasRemovedMods`, `RemovedModCount`, `InsertedModCount`, `ReplacedModCount`, `AddedModCount`, count text properties, sidebar text pass-through properties, tooltip properties). Add new `DiffDialogTexts` properties.
- `Docs/Agents/project-manifest/ui-design.md` — Add "Recommendations Sidebar" subsection under Diff Window: layout description, card taxonomy (4 risk-level cards), color coding, responsive behavior, relationship to sorting banner.
- `Docs/Agents/project-manifest/constraints.md` — Add entry: "Recommendations sidebar is informational only — it does not block operations or modify the diff state."
- `Docs/Agents/project-manifest/data-flows.md` — Add: "Sidebar recommendation cards are recomputed in `DiffDialogViewModel.UpdateDiffState()` whenever `DiffLines` changes. Cards bind to boolean visibility properties that evaluate change type counts from `DiffLines`."

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **Sidebar consumes too much horizontal space on small screens** | MinWidth set to 720px (sidebar is 260px, diff list gets ≥460px). Sidebar uses scrolling for overflow. Monitor usability at minimum width during testing. |
| **Recommendation text too verbose / wall-of-text effect** | Each card has a clear heading + subtitle + body structure. Body text is limited to 2–3 sentences. Cards are individually scrollable if content overflows. |
| **Multiple sidebar cards create visual noise when many change types present** | Cards are ordered by severity (red → orange → blue → green) and use distinct background colors for scanability. Safe-change cards are visually subtle (low-alpha backgrounds). |
| **Translations for 7 locales increase scope** | Translations can follow the existing pattern of automated translation with manual review. English strings are the reference. If translations are delayed, the `LocalizationService` fallback to English ensures the feature is usable in all locales immediately. |
| **Window width increase may not fit on low-resolution screens (1366×768)** | MinWidth of 720px is well within 1366×768 bounds. Default width of 1060px leaves 306px margin on a 1366-wide screen. The window is resizable. |
| **Risky-changes dialog may cause fatigue for power users who understand the risks** | The risky-changes dialog is shown only when removed or inserted mods are present — the genuinely high-risk cases. Safe-only changes bypass the dialog entirely, so casual accept operations remain frictionless. Users may still click "Yes" quickly if they understand the implications; the dialog is informational, not a blocking gate. |

## Recommended Workflow
- **Workflow:** ledger
- **Rationale:** Multi-module change spanning XAML layout, ViewModel, localization across 8 files, styling, and manifest updates — benefits from formal QA to verify visual layout and localization correctness across all locales.
