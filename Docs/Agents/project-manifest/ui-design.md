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
| `DiffBrush.HelpBannerBackground` | `#FFD3E4FD` | Multiple-replacements help banner background |
| `DiffBrush.HelpBannerBorder` | `#FF0B57D0` | Help banner border and icon |
| `DiffBrush.HelpBannerForeground` | `#FF001A41` | Help banner text color |

### Diff Window Context Lines and Separators

- **Default view**: The list shows only changed entries (`Added`, `Removed`, `Moved`, `Replaced`, `Inserted`) to reduce visual noise.
- **Show all mods toggle**: A `MaterialDesignCheckBox` between description and list toggles full-context mode.
- **Context lines** (`DiffChangeType.Unchanged`): Hidden by default; shown in full-context mode with 3% background opacity (`DiffBrush.Context`) and text opacity 0.45. Display 1 unchanged neighbor above and below each change group for spatial orientation. Not selectable (no context menu).
- **Separator** (`DiffChangeType.Separator`): Hidden by default; shown in full-context mode as centered `···` text at 35% opacity between non-adjacent context groups. Transparent background. Not hit-test visible.

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
