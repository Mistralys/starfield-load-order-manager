# UI Design System

> Visual design conventions, component taxonomy, and interaction patterns.

---

## Theme

- **Material Design v5** with dark mode by default.
- **Primary color**: Lime. **Secondary color**: LightGreen.
- Semantic color brushes for theme consistency — never hardcoded hex values.
- Elevated surfaces with shadows for depth.
- Rounded corners (8px) for modern aesthetic.
- All UI text must use data bindings (no hardcoded strings in XAML) to support localization.

---

## Button Taxonomy

| Type | Style | Use Case | Examples |
|------|-------|----------|----------|
| **Raised** | Elevated, filled | Primary actions | Save, Create, Update, Fix Load Order |
| **Outlined** | Border, no fill | Secondary actions | Cancel, Browse, Close |
| **Flat** | No border or elevation | Tertiary / inline actions | Auto-detected path links |

### Label Conventions

- **Ellipsis suffix** (`...`): indicates a dialog or additional interaction will follow (e.g., "Save...", "Browse...", "Download options...").
- **Action verbs**: prefer specific verbs ("Delete Profile", "Fix Load Order") over generic labels ("OK", "Apply").

---

## Confirmation Dialogs

All confirmations use `ConfirmationDialog` (custom Material Design v5 control) — never `MessageBox`.

### Icon Types

| Icon | Color | Use Case |
|------|-------|----------|
| Information | Blue | Neutral notifications |
| Question | Purple | User decisions |
| Warning | Orange | Destructive or risky actions |
| Error | Red | Failures and critical problems |

### Destructive Action Pattern

- Warning icon with detailed impact summary (bullet-point list of changes).
- Default button set to "No" for safety.
- Action cannot be undone clearly stated.

### Risk-Gated Update Reference Confirmation

The "Update reference" action in the Diff Window uses a **risk-gated, content-aware** confirmation pattern:

- **Risky changes present (removed or inserted mods):** A `Warning`-icon dialog is shown with a composed message listing the count and risk description for each risky change type. Singular and plural locale strings are selected based on the actual count (`ConfirmRiskyDetail_RemovedSingular` / `ConfirmRiskyDetail_Removals`, `ConfirmRiskyDetail_InsertedSingular` / `ConfirmRiskyDetail_Insertions`). The detail lines are embedded into `ConfirmUpdateMessage_RiskyChanges` and presented to the user before the update proceeds.
- **Safe changes only (added or replaced mods):** No confirmation dialog is shown; the update proceeds directly. Safe changes carry no position-shift risk, so the dialog adds friction without information value.

This confirmation is the third layer of a three-layer guidance strategy: passive sidebar cards → per-item tooltips → risk-gated commit-time confirmation.

---

## Status Indicators

### Color Coding

| Type | Color | Brush |
|------|-------|-------|
| Info | Blue | Primary |
| Success | Green | Tertiary |
| Warning | Orange | Secondary |
| Error | Red | Error |

### XAML Brush Names

- Error: `{DynamicResource MaterialDesign.Brush.Error}`
- Success: `{DynamicResource MaterialDesign.Brush.Tertiary}`
- Info: `{DynamicResource MaterialDesign.Brush.Primary}`
- Warning: `{DynamicResource MaterialDesign.Brush.Secondary}`

Always use semantic brushes — never hardcoded hex values. Prefer colored foreground text over colored backgrounds for status messages.

### Diff Window Brush Resources (`Styles/DiffBrushes.xaml`)

All diff change type colors are defined as semantic brush resources and referenced via `{DynamicResource}` in `DiffWindow.xaml`.

| Brush Key | Hex Color | Used For |
|-----------|-----------|----------|
| `DiffBrush.Added` | `#1A4CAF50` | Added mod row background |
| `DiffBrush.Removed` | `#1AF44336` | Removed mod row background |
| `DiffBrush.Inserted` | `#1AFFEB3B` | Inserted mod row background |
| `DiffBrush.Moved` | `#1A2196F3` | Moved mod row background |
| `DiffBrush.Replaced` | `#1A9C27B0` | Replaced mod row background |
| `DiffBrush.Context` | `#08FFFFFF` | Unchanged context line row background |
| `DiffBrush.SortingBannerBackground` | `#FFFFA726` | Sorting recommendation banner background |
| `DiffBrush.SortingBannerBorder` | `#FFFF9800` | Sorting banner border |
| `DiffBrush.GuidanceRemovalBackground` | `#33F44336` | Guidance sidebar card — removal change type |
| `DiffBrush.GuidanceInsertionBackground` | `#33FF9800` | Guidance sidebar card — insertion change type |
| `DiffBrush.GuidanceReplacementBackground` | `#339C27B0` | Guidance sidebar card — replacement change type |
| `DiffBrush.GuidanceAddedBackground` | `#334CAF50` | Guidance sidebar card — added change type |
| `DiffBrush.GuidanceSidebarBackground` | `#0AFFFFFF` | Guidance sidebar panel overall background |

The `DiffBrush.Guidance*` brushes use 20% alpha (`#33`) to match the semantic hue of the existing
change-type row brushes (`DiffBrush.Added`, `DiffBrush.Removed`, etc.) at a slightly higher opacity
appropriate for card backgrounds rather than full-row highlights.

### Diff Window Context Lines and Separators

- **Default view**: The list shows only changed entries (`Added`, `Removed`, `Moved`, `Replaced`, `Inserted`) to reduce visual noise.
- **Show all mods toggle**: A `MaterialDesignCheckBox` between description and list toggles full-context mode.
- **Context lines** (`DiffChangeType.Unchanged`): Hidden by default; shown in full-context mode with 3% background opacity (`DiffBrush.Context`) and text opacity 0.45. Display 1 unchanged neighbor above and below each change group for spatial orientation. Not selectable (no context menu).
- **Separator** (`DiffChangeType.Separator`): Hidden by default; shown in full-context mode as centered `···` text at 35% opacity between non-adjacent context groups. Transparent background. Not hit-test visible.

### Diff Window Layout

The DiffWindow uses a two-column layout:

- **Left column (star-sized):** The diff list (`FilteredDiffLines`), sorting recommendation banner, status message, no-differences message, and button row. This column contains the existing 7-row grid structure.
- **Right column (260 px fixed):** The recommendations sidebar panel.

**Default dimensions:** 1060 px wide, 720 px minimum width, 494 px height.

**Sidebar column width rationale:** The 260 px fixed width (`ColumnDefinition Width="260"`) was chosen to comfortably fit all five guidance card titles and their body text at current copy lengths while leaving the star-sized diff list column at least ~452 px of effective width at `MinWidth=720`. This is an intentional sizing decision, not an arbitrary default. If card content is substantially expanded in a future plan (e.g., longer risk copy, action links, or bullet lists), revisit this value. If the sidebar layout is adopted in a second view, extract the constant to a shared resource to prevent drift.

#### Recommendations Sidebar

The sidebar is a scrollable `StackPanel` with an overall `DiffBrush.GuidanceSidebarBackground` background and a 1 px left-edge divider (`BorderBrush=MaterialDesignDivider`, `BorderThickness="1,0,0,0"`) that separates it from the diff list. It contains a header label ("Recommendations") and one or more guidance cards. The sidebar container is always rendered; individual cards appear and disappear based on which `DiffChangeType`s are present in the current diff.

Cards use `materialDesign:Card` (the semantic Material Design v5 card control — not `Border + MaterialDesignCardBackground`) and are color-coded by risk:

| Card | Visibility property | Background brush | Risk level |
|------|---------------------|------------------|------------|
| Removal guidance | `ShowRemovalGuidance` | `DiffBrush.GuidanceRemovalBackground` | High (red) |
| Insertion guidance | `ShowInsertionGuidance` | `DiffBrush.GuidanceInsertionBackground` | Moderate (orange) |
| Replacement guidance | `ShowReplacementGuidance` | `DiffBrush.GuidanceReplacementBackground` | Safe (purple) |
| Added guidance | `ShowAddedGuidance` | `DiffBrush.GuidanceAddedBackground` | Safe (green) |
| No-changes note | `ShowNoChangesGuidance` | — *(intentional; see note below)* | Neutral |

Each risk-level card (Removal, Insertion, Replacement, Added) carries an explicit `DiffBrush.Guidance*Background` value. The **No-changes card intentionally omits a `Background` attribute** — it inherits the Material Design card default to convey a visually neutral state. This asymmetry is deliberate: a colored background would imply a risk level that does not exist. Future contributors adding a new card should not infer that the missing brush is an oversight.

Each card displays a count string (e.g., "1 mod removed" or "3 mod(s) inserted"), a heading, and a body paragraph. All text comes from `DiffDialogTexts` pass-through properties bound in XAML — no direct `Texts.*` paths. Card-level visibility is driven by the five `Show*Guidance` boolean properties on `DiffDialogViewModel`, which are refreshed in `UpdateDiffState()` whenever `DiffLines` changes.

**Design note:** `materialDesign:Card` was chosen over the informal `Border + MaterialDesignCardBackground` pattern because it provides `UniformCornerRadius` (a single `double` DP), corner clipping for child content, and elevation support. This sets a consistent precedent for future card elements.

#### Per-Item Tooltips

All diff change type icons in the list have tooltips:

- **Inserted** — `InsertedWarningTooltip` (pre-existing; unchanged).
- **Added**, **Removed**, **Replaced**, **Moved** — `AddedTooltip`, `RemovedTooltip`, `ReplacedTooltip`, `MovedTooltip` (added in WP-004 of the load-order-change-guidance plan).

Tooltips are delivered via `ToolTip` setter on `PackIcon` style triggers in `DiffWindow.xaml` and bound to pass-through properties on `DiffDialogViewModel`.

### Main Window Banners

Banners stack vertically when multiple are active:

1. **Configuration error** — non-dismissable red banner with "Open settings" button.
2. **Update available** — info bar with "Download options..." button; dismissable for current session.
3. **Steam warning** — orange banner with tooltip; auto-dismisses when Steam closes.

### Settings Window

- Permanent status banner — green (valid) or red (specific error message). Updates in real-time on field changes.

### Change Badge

- "Manage load order" button text appends `(X changes)` when primary changes are present.
- When dependent changes also exist: appends `(X changes, +Y affected)` where X is the primary count and Y is the dependent count.
- `FileMonitoringCoordinator.ChangeCount` counts primary changes only (excludes `Unchanged` and `Separator` items).
- `FileMonitoringCoordinator.DependentChangeCount` counts all dependent position shifts.

---

## Window Types

### Modal Windows

Block parent interaction until closed. Center on parent, dim parent background.

- SettingsWindow, SwitchProfileWindow, ProfilePropertiesWindow, ConfirmationDialog, AboutWindow, UpdateOptionsDialog, CommentInputDialog, ErrorDialog

### Non-Modal Windows

Allow parent interaction. Single-instance prevention via `WindowManager`. Auto-refresh on data changes.

- DiffWindow, ManageProfilesWindow, ReferenceHistoryWindow, ViewPendingChangesWindow

### Keyboard Conventions

- **Escape**: closes modal (when `IsCancel` button present).
- **Enter**: activates default button (when `IsDefault` set).

---

## ConfigInvalidOverlay

Reusable `UserControl` shown in secondary windows when configuration becomes invalid.

- Semi-transparent dark background with `Panel.ZIndex="1000"`.
- Centered Material Design card with alert icon and concise message.
- Hidden during active file operations (`IsOperationInProgress` flag).
- Auto-hides when configuration becomes valid again.

---

[<< Back to Index](README.md)
