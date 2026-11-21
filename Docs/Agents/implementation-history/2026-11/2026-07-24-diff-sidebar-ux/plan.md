# Plan

## Plan Audit Cycles
- Audits: 2 — Plan Auditor v1.7.0
- Architectural Reviews: 2 — Plan Architect Reviewer v2.2.0

## Summary

Add two UX improvements to the DiffWindow: (1) a draggable `GridSplitter` between the diff list and the recommendations sidebar so users can resize the two panels to taste, with the chosen width persisted across sessions; and (2) collapsible sidebar guidance cards where each card header is a clickable toggle, the body hides/shows with a chevron indicator, card states are also persisted, and the "Removed Mods" card (deleted mods) starts expanded while all others start collapsed.

## Architectural Context

The DiffWindow is a WPF `Window` (`Views/DiffWindow.xaml` + `Views/DiffWindow.xaml.cs`) backed by `DiffDialogViewModel` (`ViewModels/DiffDialogViewModel.cs`), which follows the MVVM + Coordinator pattern. Layout is currently a `Grid` with two fixed `ColumnDefinitions` (`Width="*"` and `Width="260"`). The five guidance sidebar cards were added in the prior `2026-07-23-load-order-change-guidance` project; each card uses `materialDesign:Card` with a two-element `StackPanel` (header row + body text). User preferences live in `AppConfigModel` (`Models/AppConfigModel.cs`), persisted via `SettingsService.SaveSettingsAsync`.

## Approach / Architecture

**Draggable splitter:** Replace the two-column inner grid with a three-column layout (diff list | 6 px splitter column | sidebar). A `GridSplitter` in the middle column enables runtime resizing. The sidebar `ColumnDefinition` is named (`x:Name="SidebarColumn"`) so the code-behind can restore its width from `AppConfigModel.DiffSidebarWidth` on `Loaded` and record it back on `Closed`.

**Collapsible cards:** Each card's flat header row is wrapped in a `MaterialDesignFlatButton` (existing project pattern) that commands `ToggleXxxCardCommand`. A `Grid` inside the button holds the icon, title, and a chevron `PackIcon` whose `Kind` binds to `IsXxxCardExpanded` via a new `BoolToChevronKindConverter` (returns `ChevronDown` when `true`, `ChevronRight` when `false`). The converter is declared once in `DiffWindow.xaml`'s `Window.Resources` and reused by all six card headers. The card body (count text + body text) is wrapped in a `StackPanel` whose `Visibility` binds to the card's `IsXxxCardExpanded` property via the already-declared `BooleanToVisibilityConverter`. Card states are persisted in six new `bool` properties on `AppConfigModel`.

**Persistence:** `DiffDialogViewModel` exposes `GetSidebarWidth()` (reads from config) and `PersistWindowStateAsync(double)` (writes sidebar width and all six card states to `_mainViewModel.Config`, then calls `SettingsService.SaveSettingsAsync`). All persistence is batched in this single method — no `partial void` callbacks are used for card state. All config access is guarded for the test-only constructor where `_mainViewModel` is `null`.

## Rationale

- `GridSplitter` is a zero-dependency WPF built-in that provides the dragging UX without additional libraries.
- Persisting width in `AppConfigModel` reuses the existing serialization pipeline — no new infrastructure.
- Batching all config writes (sidebar width + all card states) inside `PersistWindowStateAsync` keeps persistence logic in one place and is consistent with how sidebar width is already handled — eliminating the asymmetry that six `partial void` callbacks would introduce.
- `BoolToChevronKindConverter` eliminates six nearly-identical `PackIcon.Style`+`DataTrigger` blocks; the converter fits naturally alongside the seven existing converters and is reused by all cards for free.
- Fire-and-forget save on `Window.Closed` (not `Closing`) is the correct WPF pattern for non-critical UI state; acceptable data loss risk is negligible for layout preferences.
- No new locale keys are needed because the collapse toggle is icon-only (chevron).

## Considered Alternatives

| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| Column resize persistence | Store width in `AppConfigModel` (existing JSON config) | Separate JSON file; `IsolatedStorage`; `System.Windows.Application.Current.Properties` | `AppConfigModel` is already serialized and loaded — zero new infrastructure; consistent with `PreferredLanguage` precedent. |
| Save trigger | `Window.Closed` fire-and-forget | `Window.Closing` async-void; auto-save on every drag end | `Closed` is the simplest safe hook that fires after window teardown; drag-end auto-save would require `DragCompleted` event wiring that is not needed. |
| Card collapse toggle UI | `MaterialDesignFlatButton` wrapping card header | `Expander` control; `ToggleButton`; `MouseDown` on panel | Flat button matches the existing `DependentChangesSummary` toggle pattern in the same window; native `Expander` control doesn't match the card's visual hierarchy. |
| Chevron indicator | `BoolToChevronKindConverter` declared once in `Window.Resources` | Per-card `PackIcon.Style`+`DataTrigger`; implicit `Style` with relative binding | Converter eliminates six ~6-line XAML repetitions; future cards reuse it at zero cost; fits alongside the seven existing converters. |
| Card state persistence | Batch all writes in `PersistWindowStateAsync` | Six `partial void OnIsXxxCardExpandedChanged` callbacks; eager disk write per toggle | Batching keeps all persistence in one method alongside sidebar width; `partial void` callbacks are the right pattern for side effects, not simple value copies. |
| Chevron direction convention | ChevronDown = expanded, ChevronRight = collapsed | ChevronUp = expanded | ChevronDown/ChevronRight is standard Material Design accordion convention for horizontal layouts. |

## Pattern Alignment

- `GridSplitter` layout: new pattern for this project but standard WPF; no deviation from architecture.
- `MaterialDesignFlatButton` as inline toggle: follows `DependentChangesSummary` button in `DiffWindow.xaml` (L278–L295).
- `BoolToChevronKindConverter`: new converter, follows the established pattern in `Converters/` (seven already exist); fits naturally alongside `BooleanToVisibilityConverter` and peers.
- `[ObservableProperty]` backing fields without `partial void` callbacks: card state has no synchronous side effects requiring immediate reaction — batching writes in `PersistWindowStateAsync` is simpler and consistent. The `partial void` pattern is preserved for cases with real side effects (`OnIsConfigValidChanged`, `OnIsOperationInProgressChanged`).
- `AppConfigModel` for UI state: follows `PreferredLanguage` precedent (UI preference stored in config model).
- Fire-and-forget save on `Window.Closed`: new for DiffWindow, but aligned with the non-critical save pattern used elsewhere.

## Detailed Steps

1. **`Models/AppConfigModel.cs`** — Add 7 new properties using C# property initializers (so deserialization from old `config.json` files that lack these keys still yields the correct defaults):
   - `public double DiffSidebarWidth { get; set; } = 300.0;`
   - `public bool DiffRemovalCardExpanded { get; set; } = true;`
   - `public bool DiffInsertionCardExpanded { get; set; } = false;`
   - `public bool DiffReplacementCardExpanded { get; set; } = false;`
   - `public bool DiffAddedCardExpanded { get; set; } = false;`
   - `public bool DiffMovedCardExpanded { get; set; } = false;`
   - `public bool DiffNoChangesCardExpanded { get; set; } = false;`

2. **`ViewModels/DiffDialogViewModel.cs`** — Add card expansion state and persistence:
   - Six `[ObservableProperty]` backing fields: `_isRemovalCardExpanded` must use a C# field initializer (`private bool _isRemovalCardExpanded = true;`) so that the test-only constructor (which never runs production-constructor code and sets `_mainViewModel = null!`) inherits the correct `true` default for AC-05 tests. All other backing fields (`_isInsertionCardExpanded`, etc.) may use the C# default (`false`). In the production constructor, all six fields are overwritten from `_mainViewModel.Config`.
   - Six `[RelayCommand]` toggle methods (`ToggleRemovalCard`, `ToggleInsertionCard`, `ToggleReplacementCard`, `ToggleAddedCard`, `ToggleMovedCard`, `ToggleNoChangesCard`) — each inverts the corresponding boolean.
   - No `partial void OnIsXxxCardExpandedChanged` callbacks — card states are not written to `_mainViewModel.Config` on every toggle; they are batched in `PersistWindowStateAsync`.
   - `internal double GetSidebarWidth()` — returns `_mainViewModel?.Config.DiffSidebarWidth ?? 300.0`.
   - `internal async Task PersistWindowStateAsync(double sidebarWidth)` — writes `sidebarWidth` to `_mainViewModel.Config.DiffSidebarWidth`, writes all six card boolean states to the corresponding `_mainViewModel.Config.DiffXxxCardExpanded` properties, then calls `await SettingsService.SaveSettingsAsync(_mainViewModel.Config)`. Entire method is guarded with `if (_mainViewModel is null) return;`.

3. **`Converters/BoolToChevronKindConverter.cs`** — Create new converter class:
   - Implement `IValueConverter` with `Convert` returning `PackIconKind.ChevronDown` when the value is `true` (expanded) and `PackIconKind.ChevronRight` when `false` (collapsed). `ConvertBack` throws `NotSupportedException`. Follows the pattern of the seven existing converters in the same directory.

4. **`Views/DiffWindow.xaml`** — Layout and card structure changes:
   - Inner `Grid` (Row 2): Replace 2-column `ColumnDefinitions` with 3-column: `Width="*" MinWidth="200"` | `Width="6"` | `x:Name="SidebarColumn" Width="300" MinWidth="180"`.
   - Add `<GridSplitter Grid.Column="1" Width="6" HorizontalAlignment="Stretch" Background="Transparent" Cursor="SizeWE" ResizeBehavior="PreviousAndNext" />` between the two content areas.
   - Move sidebar `Border` from `Grid.Column="1"` to `Grid.Column="2"`.
   - Declare `<converters:BoolToChevronKindConverter x:Key="BoolToChevronKindConverter" />` in `Window.Resources` (alongside the existing converters).
   - For each of the five guidance cards and the NoChanges card: convert the header `StackPanel` into a `MaterialDesignFlatButton` with `Command="{Binding ToggleXxxCardCommand}"`, `HorizontalAlignment="Stretch"`, `HorizontalContentAlignment="Stretch"`, `Padding="0"`, and content consisting of a three-column `Grid` (icon | title TextBlock | chevron `PackIcon` whose `Kind` binds as `Kind="{Binding IsXxxCardExpanded, Converter={StaticResource BoolToChevronKindConverter}}"`). Wrap the card body (count text + body text) in a `StackPanel` with `Visibility="{Binding IsXxxCardExpanded, Converter={StaticResource BooleanToVisibilityConverter}}"`.

5. **`Views/DiffWindow.xaml.cs`** — Persistence wiring:
   - In constructor: add `Closed += OnWindowClosed;`.
   - Add `OnWindowClosed(object sender, EventArgs e)`: if `DataContext is DiffDialogViewModel vm`, call `_ = vm.PersistWindowStateAsync(SidebarColumn.Width.Value)`.
   - In `OnDiffWindowLoaded`: after `ScrollToTargetLine()`, call `RestoreSidebarWidth()`.
   - Add `RestoreSidebarWidth()`: if `DataContext is DiffDialogViewModel vm`, set `SidebarColumn.Width = new GridLength(vm.GetSidebarWidth())`.
   - **Note — disposal ordering:** The existing `OnClosed` override calls `viewModel.Dispose()` before `base.OnClosed(e)`. WPF raises the `Closed` event inside `base.OnClosed(e)`, so `OnWindowClosed` (and therefore `PersistWindowStateAsync`) fires *after* `Dispose`. This is currently safe because `Dispose` only unsubscribes event handlers and does not null out `_mainViewModel` or the bool properties. Add a code comment on the `Closed +=` line warning future maintainers: widening `Dispose` to null out `_mainViewModel` would silently break persistence.

6. **`Docs/Agents/project-manifest/api-surface.md`** — Update `AppConfigModel` section with the 7 new properties; update `DiffDialogViewModel` section with the 6 new observable properties, 6 new commands, and 2 new internal methods; add new `BoolToChevronKindConverter` entry to the Converters section.

## Dependencies

- No new NuGet packages required.
- Step 2 depends on Step 1 (ViewModel reads from `AppConfigModel`).
- Step 3 (converter) is independent and can be done alongside Steps 1–2.
- Steps 4 and 5 depend on Steps 2 and 3 (XAML and code-behind bind to ViewModel properties/commands and reference the new converter).
- Step 6 can be done last, in parallel with or after any code step.

## Required Components

- `Models/AppConfigModel.cs` (modified)
- `ViewModels/DiffDialogViewModel.cs` (modified)
- `Converters/BoolToChevronKindConverter.cs` (new — `IValueConverter` returning `PackIconKind.ChevronDown`/`ChevronRight`)
- `Views/DiffWindow.xaml` (modified)
- `Views/DiffWindow.xaml.cs` (modified)
- `Docs/Agents/project-manifest/api-surface.md` (modified)

## Assumptions

- `MaterialDesignFlatButton` style is available at the card scope (confirmed: used in same window for `DependentChangesSummary`).
- `BooleanToVisibilityConverter` declared in `Window.Resources` is accessible from all nested XAML in the file (confirmed: already used throughout the window).
- `ColumnDefinition` supports `x:Name` in WPF (confirmed: standard WPF feature).
- `System.Text.Json` serializes `double` and `bool` properties on `AppConfigModel` with default values when the property is absent from the JSON (confirmed: deserializer uses `default(T)` for missing keys, then property initializers apply).

## Constraints

- Zero hardcoded UI strings — the chevron toggle is icon-only; no new locale keys.
- Test constructor (`internal DiffDialogViewModel(ObservableCollection<DiffLineModel>)`) has `_mainViewModel = null!`. Every new access to `_mainViewModel` must be null-guarded.
- `DiffSidebarWidth` must not be set to a value below `MinWidth="180"` in XAML; the default 300.0 is well above this.
- Manifest update (`api-surface.md`) is required per AGENTS.md Section 2.

## Out of Scope

- Adding tooltips or ARIA labels to the collapse toggle (would require new locale keys).
- Animating the expand/collapse transition.
- Saving the window position or size (separate concern).
- Changes to the guidance card content, risk classification, or locale strings.
- Adding a "collapse all / expand all" control.

## Acceptance Criteria

- AC-01: The boundary between the diff list and the recommendations sidebar is draggable at runtime using a visible splitter.
- AC-02: The sidebar has a default width of 300 px (wider than the previous 260 px fixed value).
- AC-03: Dragging the splitter persists the new sidebar width; on next launch, the sidebar restores to the saved width.
- AC-04: Each guidance sidebar card can be individually collapsed and expanded by clicking its header row.
- AC-05: The "Removed Mods" card is expanded by default; all other cards are collapsed by default.
- AC-06: Card collapse/expand states are persisted; on next launch, each card restores to its last saved state.
- AC-07: A chevron icon in each card header reflects the current state (ChevronDown = expanded, ChevronRight = collapsed).
- AC-08: The existing test suite continues to pass with zero new failures.
- AC-09: No hardcoded UI strings are introduced (icon-only collapse toggle).
- AC-10: The project manifest (`api-surface.md`) is updated to reflect all new public API surface.

## Testing Strategy

All new ViewModel properties and commands are exercised by unit tests using the existing internal test-only constructor. No code-behind or XAML behavior requires automated testing (layout and visual behavior is manually verified). Existing tests must remain green.

## Test Plan

- `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` — Add tests for each new `IsXxxCardExpanded` default value (6 new tests), and one test per `ToggleXxxCardCommand` confirming the boolean inverts on execution (6 new tests). All use the existing `Create([...])` factory. — Covers AC-04, AC-05, AC-08.

## Documentation Updates

- `Docs/Agents/project-manifest/api-surface.md` — Add 7 new `AppConfigModel` properties; add 6 `[ObservableProperty]` booleans, 6 `IRelayCommand` properties, and 2 `internal` methods to `DiffDialogViewModel`; add new `BoolToChevronKindConverter` entry to the Converters section. — Required by AGENTS.md Section 2.

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **GridSplitter renders on top of / behind border at column boundaries** | Use `Background="Transparent"` and keep the splitter column thin (6 px); the existing `BorderBrush` on the sidebar already provides a visual separator. |
| **Flat button hover/ripple looks mismatched on colored card background** | `MaterialDesignFlatButton` has `Background="Transparent"` ripple by default; the ripple will overlay the card background color — acceptable given existing usage of the same button style on colored backgrounds within the same window. |
| **Sidebar width defaults clash with window MinWidth on small screens** | Window `MinWidth="720"` is much larger than the combined minimum (200 + 6 + 180 = 386 px); no clash possible. |
| **`ColumnDefinition.Width.Value` is NaN when Width is Auto or Star** | The sidebar column always uses absolute pixel width (initially 300, user-set via splitter) — `GridLength.IsAbsolute` is guaranteed; `Value` is always a valid number. |
| **Old `config.json` missing new properties deserializes to 0.0/false** | `double` default would be 0.0 (zero-width sidebar) — mitigated by setting `DiffSidebarWidth = 300.0` as the C# property initializer, which applies after JSON deserialization fills unrecognized keys with defaults. `bool` properties default to `false` which matches all-collapsed-except-removal intent (removal defaults `true`, others `false`). |

## Recommended Workflow
- **Workflow:** standalone
- **Rationale:** Single-window, single-ViewModel change with no new coordinators, services, or cross-cutting dependencies; a standalone developer session with self-review is sufficient.
