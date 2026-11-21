## Synthesis

### Completion Status
- Date: 2026-07-24
- Status: COMPLETE
- Completed by: Standalone Developer Agent
- Archived in Ledger: 2026-07-24

### Outcome Summary

Implemented two DiffWindow UX improvements: a draggable `GridSplitter` between the diff list and the recommendations sidebar (with sidebar width persisted to `AppConfigModel`), and collapsible guidance cards with chevron indicators (each card independently toggled and persisted across sessions). All acceptance criteria were met; the existing test suite passed with zero regressions.

### Implementation Summary
- **`Models/AppConfigModel.cs`** — Added `DiffSidebarWidth` (double, default 300.0) and six card-expanded boolean properties (default: removal `true`, all others `false`).
- **`ViewModels/DiffDialogViewModel.cs`** — Added six `[ObservableProperty]` card expansion fields (with `_isRemovalCardExpanded = true` field initializer for the test constructor), six `[RelayCommand]` toggle methods, `GetSidebarWidth()`, and `PersistWindowStateAsync(double)`. Production constructor restores all card states from config after initialization.
- **`Converters/BoolToChevronKindConverter.cs`** — New `IValueConverter` returning `PackIconKind.ChevronDown` (expanded) or `PackIconKind.ChevronRight` (collapsed).
- **`Views/DiffWindow.xaml`** — Updated from 2-column to 3-column inner grid; added `GridSplitter` (6 px, transparent); sidebar column named `SidebarColumn` at 300 px default; all six guidance cards converted to collapsible pattern with `MaterialDesignFlatButton` header, three-column header `Grid` (icon + title + chevron), and body `StackPanel` with `BooleanToVisibilityConverter` binding.
- **`Views/DiffWindow.xaml.cs`** — Added `OnWindowClosed` (fire-and-forget `PersistWindowStateAsync`), `RestoreSidebarWidth()` (called from `OnDiffWindowLoaded`), and `Closed += OnWindowClosed` subscription with a comment warning about disposal ordering.
- **`Tests/.../DiffDialogViewModelTests.cs`** — Added 12 new tests: 6 covering default card expansion values (AC-05) and 6 covering toggle command inversion (AC-04).
- **`Docs/Agents/project-manifest/api-surface.md`** — Added 7 new `AppConfigModel` properties; added 6 observable booleans, 6 relay commands, and 2 internal methods to `DiffDialogViewModel`; added `BoolToChevronKindConverter` entry to the Converters table.
- **`Docs/Agents/project-manifest/file-tree.md`** — Added `BoolToChevronKindConverter.cs` to the Converters directory listing.

### Documentation Updates
- `api-surface.md` updated with all new `AppConfigModel` properties, `DiffDialogViewModel` members, and the new converter entry. Required by AGENTS.md Section 2.
- `file-tree.md` updated to include the new converter file.

### Verification Summary
- Tests run: `dotnet test Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj --configuration Debug`
- Static analysis run: `dotnet build` (no warnings on changed files)
- Result: 511 tests passed, 0 failures, 0 regressions. Build succeeded.

### Code Insights
- [low] (convention) `Views/DiffWindow.xaml.cs` — The `OnClosed` override and `Closed` event handler are now two separate persistence hooks for the same window-close lifecycle event. The comment added to the `Closed +=` line adequately documents the ordering dependency, but a future maintainer unfamiliar with WPF event sequencing might still find this confusing. Consider consolidating by moving the `PersistWindowStateAsync` call into `OnClosed` (before `viewModel.Dispose()`) if the fire-and-forget pattern is acceptable there — this would remove the handler registration entirely and make ordering explicit.
- [low] (improvement) `ViewModels/DiffDialogViewModel.cs` — `PersistWindowStateAsync` assigns config properties and calls `SaveSettingsAsync` synchronously (from the fire-and-forget caller's perspective). If the app is killed immediately after `Window.Closed` fires (e.g., OS shutdown), the async write may be lost. This is acceptable per the plan's stated risk tolerance for UI layout preferences, but worth noting for future reference.
- [low] (debt) `Views/DiffWindow.xaml` — The six card header `Grid` definitions (icon + title + chevron) share identical `ColumnDefinition` structure but are duplicated six times. If a seventh card is ever added, this pattern would be worth extracting into a shared `DataTemplate` or `ControlTemplate` to reduce duplication.

### Additional Comments
- The `_isRemovalCardExpanded = true` field initializer was critical for the test constructor path (where `_mainViewModel` is `null!` and the config-loading block in the production constructor is never reached). Tests confirm the correct `true` default survives the test-only constructor.
- The `GridSplitter` column is `Width="6"` with `Background="Transparent"` — it is visually invisible but functionally active. The existing sidebar `BorderThickness="1,0,0,0"` provides the visible divider line.
