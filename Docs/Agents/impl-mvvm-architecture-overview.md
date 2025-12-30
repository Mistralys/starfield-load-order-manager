# MVVM Structure and Navigation Summary (for Agent Consumption)

This project is a WPF `.NET 9` desktop app using `CommunityToolkit.Mvvm` with a focused, dialog-based UI. The core of the app revolves around managing Starfield load order files via an MVVM structure.

---

## View Models

### `MainViewModel`

**Role:** Application shell and primary orchestrator for `MainWindow`.

**Key Responsibilities:**
- Holds current `AppConfigModel` (`Config`) and reference-file existence state (`RefExists`).
- Validates configuration and reference state on startup via `LoadInitialStateAsync`.
- Drives the main user workflow:
  - Create reference file from the current `Plugins.txt`.
  - Apply load-order fixes using `FileService`.
- Surfaces user feedback:
  - `StatusMessage` reflects current state/errors.
  - `IsBusy` controls UI busy states and command availability.
- Triggers navigation to the `SettingsWindow`.

**Important Properties:**
- `Config : AppConfigModel`
- `RefExists : bool`
- `StatusMessage : string`
- `IsBusy : bool`

**Primary Commands / Interactions:**
- `FixLoadOrderCommand` (`FixLoadOrderAsync`)
  - Can execute when: `Config.IsValid() && RefExists && !IsBusy`.
  - Calls `FileService.ApplyLoadOrderAsync(Config)`.
- `CreateReferenceCommand` (`CreateReferenceAsync`)
  - Can execute when: `Config.IsValid() && !RefExists && !IsBusy`.
  - Calls `FileService.CreateReferenceFileAsync(Config)` and sets `RefExists = true`.
- `OpenSettingsCommand` (`OpenSettings`)
  - Creates a `SettingsViewModel` from current `Config`.
  - Opens `SettingsWindow` modally (UI implementation).
  - On successful close:
    - Updates `Config` from `settingsVm.GetConfig()`.
    - Persists via `SettingsService.SaveSettingsAsync(Config)`.
    - Re-evaluates `RefExists` and `StatusMessage`.
- `ExitApplicationCommand` (`ExitApplication`)
  - Shuts down the WPF `Application`.

---

### `SettingsViewModel`

**Role:** Backs the configuration UI in `SettingsWindow`.

**Key Responsibilities:**
- Holds user-editable settings:
  - `StarfieldAppDataPath`
  - `StarfieldGamePath`
- Initializes values using:
  - Existing `AppConfigModel` (if present).
  - Fallback defaults from `SettingsService.TryGetDefaultAppDataPath()` and `TryGetDefaultSteamPath()`.
- Returns updated configuration via `GetConfig()`.

**Important Properties:**
- `StarfieldAppDataPath : string`
- `StarfieldGamePath : string`

**Primary Commands / Interactions:**
- `BrowsePathCommand` (`BrowsePath(string pathType)`)
  - CommandParameter: `"AppData"` or `"GamePath"`.
  - Implemented in the window’s code-behind:
    - Opens a folder-picker dialog.
    - On success, sets either `StarfieldAppDataPath` or `StarfieldGamePath`.
- `GetConfig() : AppConfigModel`
  - Called by `MainViewModel` after the window closes to persist user choices.

> Note: Any `SaveSettingsCommand` mentioned in UI specs is expected to be handled in `SettingsWindow` code-behind, where it:
> - Validates user input if desired.
> - Calls `GetConfig()` on the view model.
> - Saves via `SettingsService`.
> - Sets `DialogResult = true` and closes the window.

---

## Models

### `AppConfigModel`

**Purpose:** Holds Starfield path configuration and basic validation.

**Key Members:**
- `StarfieldAppDataPath : string` (location holding `Plugins.txt`)
- `StarfieldGamePath : string` (game root, must contain `Data` folder)
- `IsValid() : bool`
  - Checks non-empty strings and existence of:
    - `StarfieldAppDataPath`
    - `StarfieldGamePath`
    - `StarfieldGamePath\Data`
- `GetPluginsFilePath() : string`
- `GetReferenceFilePath() : string`

---

## Services

### `SettingsService` (static)

**Purpose:** Load/save `AppConfigModel` to disk, and provide simple path defaults.

**Key Methods:**
- `LoadSettingsAsync() : Task<AppConfigModel>`
  - Reads JSON from `%LOCALAPPDATA%\LoadOrderKeeper\config.json`.
  - Returns a default `AppConfigModel` on missing or invalid file.
- `SaveSettingsAsync(AppConfigModel config)`
  - Writes indented JSON to the same config path.
- `TryGetDefaultSteamPath() : string`
  - Attempts `Program Files (x86)\Steam\steamapps\common\Starfield`, returns empty if invalid.
- `TryGetDefaultAppDataPath() : string`
  - Attempts `%LOCALAPPDATA%\Starfield`, returns empty if invalid.

### `FileService` (static)

**Purpose:** Core file operations for reference creation and load-order application.

**Key Behaviors:**
- Case restoration based on on-disk `Data` folder.
- Order mods according to reference, then append new mods.

**Key Methods:**
- `DoesReferenceFileExist(AppConfigModel config) : bool`
  - Checks `config.GetReferenceFilePath()`.
- `CreateReferenceFileAsync(AppConfigModel config)`
  - Copies raw content of `Plugins.txt` to `Plugins.reference.txt` (preserves comments/blank lines).
- `ApplyLoadOrderAsync(AppConfigModel config)`
  - Preconditions: `config.IsValid()` must be true or an `InvalidOperationException` is thrown.
  - Steps:
    1. Build case-lookup map from `StarfieldGamePath\Data` for `*.esm` and `*.esp`.
    2. Read `Plugins.reference.txt` and `Plugins.txt` into `ModEntryModel` lists, filtering comments and empty lines.
    3. Identify new mods (present in current list but not reference).
    4. Build final order:
       - For each reference mod present in current:
         - Use on-disk casing if found; otherwise, reference filename.
       - Append new mods in current order, using case-lookup when available.
    5. Overwrite `Plugins.txt` with the final ordered list.

---

## Views and Navigation Pattern

### Main Window (`MainWindow`)

**DataContext:** `MainViewModel`.

**Layout / Interactions:**
- Menu:
  - `File → Settings` → bound to `OpenSettingsCommand`.
  - `File → Exit` → bound to `ExitApplicationCommand`.
- Path display:
  - Shows `Config.StarfieldAppDataPath`.
- Controls:
  - `Create Ref` button → `CreateReferenceCommand` (enabled via `CanCreateReference` logic).
  - `Fix Load Order` button → `FixLoadOrderCommand` (enabled via `CanFixLoadOrder` logic).
- Progress indicator:
  - `ProgressBar` bound to `IsBusy` (indeterminate, visibility toggled by Boolean-to-Visibility converter).
- Status:
  - `TextBlock` bound to `StatusMessage`.

### Settings Window (`SettingsWindow`)

**DataContext:** `SettingsViewModel` created by `MainViewModel.OpenSettings`.

**Layout / Interactions:**
- Two labeled path fields:
  - AppData path:
    - `TextBox.Text` ↔ `StarfieldAppDataPath` (`UpdateSourceTrigger=PropertyChanged`).
    - Browse button:
      - `Command="{Binding BrowsePathCommand}"`
      - `CommandParameter="AppData"`.
  - Game path:
    - `TextBox.Text` ↔ `StarfieldGamePath` (`UpdateSourceTrigger=PropertyChanged`).
    - Browse button:
      - `Command="{Binding BrowsePathCommand}"`
      - `CommandParameter="GamePath"`.
- Save button:
  - Implemented in code-behind (e.g. `SaveSettingsCommand` or click event):
    - Retrieves updated `AppConfigModel` via `GetConfig()`.
    - Saves via `SettingsService.SaveSettingsAsync`.
    - Sets `DialogResult = true` and closes window.

---

## Navigation Flow

1. **Startup**
   - `MainViewModel` is constructed and immediately runs `LoadInitialStateAsync`.
   - `Config` is loaded from disk via `SettingsService.LoadSettingsAsync`.
   - `RefExists` is computed via `FileService.DoesReferenceFileExist(Config)`.
   - `StatusMessage` is set based on `Config.IsValid()`:
     - Valid: `"Ready. Configuration is valid."`
     - Invalid: `"Configuration is required. Please set paths in the Settings window."`

2. **Open Settings**
   - User selects `File → Settings` or equivalent.
   - `OpenSettingsCommand`:
     - Creates `SettingsViewModel` with current `Config`.
     - Opens `SettingsWindow` modally.
   - On successful close:
     - `Config` is replaced with `settingsVm.GetConfig()`.
     - `SettingsService.SaveSettingsAsync(Config)` is invoked.
     - `RefExists` is re-evaluated.
     - `StatusMessage` updated (e.g. `"Configuration updated."` or an invalid configuration message).

3. **Create Reference File**
   - User clicks `Create Ref`.
   - `CreateReferenceCommand` runs only if `Config.IsValid() && !RefExists && !IsBusy`.
   - On success:
     - `RefExists = true`.
     - `StatusMessage` updated to indicate completion.

4. **Fix Load Order**
   - User clicks `Fix Load Order`.
   - `FixLoadOrderCommand` runs only if `Config.IsValid() && RefExists && !IsBusy`.
   - Uses `FileService.ApplyLoadOrderAsync(Config)` with case restoration and reference-driven ordering.
   - User feedback is provided via `StatusMessage` and `MessageBox` on error.

5. **Exit**
   - User selects `File → Exit` or equivalent.
   - `ExitApplicationCommand` shuts down the application.


## `DiffDialogViewModel`

**Role:** Backs the diff dialog (`DiffWindow`). Presents differences between the current `Plugins.txt` and the reference file, and exposes commands to accept or revert changes.

**Key Responsibilities:**
- Holds the current diff state as a list of `DiffLineModel` instances.
- Tracks the last observed plugins signature so the dialog can be refreshed when external changes occur.
- Coordinates with `DiffService` and `FileService` to:
  - Recompute the diff (`RefreshDiffAsync`).
  - Apply changes by updating the reference file or restoring `Plugins.txt`.

**Important Properties:**
- `Lines : ObservableCollection<DiffLineModel>` – the current diff lines for the UI to render.
- `HasChanges : bool` – indicates whether there are any actionable differences.
- `IsBusy : bool` – used to disable commands while operations are in progress.
- `LastObservedPluginsSignature : string?` – used by `MainViewModel` to determine if a refresh is necessary.

**Primary Commands / Interactions:**
- `AcceptChangesCommand`
  - Updates the reference file so that the current `Plugins.txt` becomes the new reference.
  - Uses underlying services to write the updated reference and then refreshes the diff.
- `RevertChangesCommand`
  - Restores `Plugins.txt` to match the reference file, effectively discarding recent changes.
  - Uses underlying services to overwrite `Plugins.txt` and then refreshes the diff.
- `RefreshDiffAsync()`
  - Invoked by `MainViewModel` whenever monitoring detects a new plugins signature.
  - Recomputes the diff via `DiffService` and repopulates `Lines`, `HasChanges`, and any summary fields.

---

### Diff Window (`DiffWindow`)

**DataContext:** `DiffDialogViewModel` created and owned by `MainViewModel`.

**Layout / Interactions:**
- Diff display area:
  - Typically a `ListView`/`DataGrid`/`ItemsControl` bound to `Lines`.
  - Uses visual cues (e.g., colors, glyphs, or columns) to distinguish additions, deletions, and unchanged entries.
- Summary/header region:
  - Shows high-level status (e.g., “No differences detected” vs. “Differences found between Plugins.txt and reference”).
  - May bind to `HasChanges` or derived properties.
- Commands:
  - `Accept` button → bound to `AcceptChangesCommand`.
  - `Revert` button → bound to `RevertChangesCommand`.
  - Optional `Close` button → closes the dialog without applying changes.
- Refresh behavior:
  - `MainViewModel` caches an instance of `DiffDialogViewModel` while the dialog is open.
  - When the monitoring loop detects a new `_lastObservedPluginsSignature`, it calls `RefreshDiffAsync` on the cached view model so the dialog content stays in sync.

---

### Diff Navigation Flow

1. **Open Diff Window**
   - `MainViewModel.ShowDiffCommand`:
     - Ensures configuration and reference are valid.
     - Creates (or reuses) a `DiffDialogViewModel` instance.
     - Calls `RefreshDiffAsync` to populate initial diff data.
     - Opens `DiffWindow` modally with the view model as `DataContext`.
     - Stores the view model reference (e.g., `_activeDiffDialog`) for live refresh.

2. **Live Refresh While Open**
   - Background monitoring in `MainViewModel` detects changes in `Plugins.txt` via a file signature.
   - When a new signature is detected and `_activeDiffDialog` is not null:
     - `MainViewModel` invokes `_activeDiffDialog.RefreshDiffAsync()` with the new signature.
     - `DiffDialogViewModel` recomputes the diff and updates `Lines`/`HasChanges`.

3. **Accept Changes**
   - User clicks `Accept` in `DiffWindow`.
   - `AcceptChangesCommand`:
     - Calls into services to update the reference file to match `Plugins.txt`.
     - Refreshes the diff; typically results in `HasChanges = false`.
     - Status/notifications are surfaced via the dialog and/or `MainViewModel`.

4. **Revert Changes**
   - User clicks `Revert` in `DiffWindow`.
   - `RevertChangesCommand`:
     - Calls into services to restore `Plugins.txt` from the reference file.
     - Refreshes the diff so the UI reflects the reverted state.
     - Status/notifications are surfaced similarly to `Accept`.

5. **Close Diff Window**
   - User closes the dialog (Close button or window chrome).
   - `MainViewModel` clears its reference to `_activeDiffDialog`.
   - Monitoring continues, but no further `RefreshDiffAsync` calls are made until the diff dialog is opened again.

---

## Agent-Oriented Notes

- **All configuration and file operations should flow through the view models and services described above.**
- **Navigation is simple and modal:**
  - No page stacks.
  - Main window + `SettingsWindow` and `DiffWindow` modals.
- Any future dialogs or windows should follow the same pattern:
  - ViewModel created in `MainViewModel`.
  - Modal dialog created in code-behind with that ViewModel as `DataContext`.
  - Result applied back to `MainViewModel` upon dialog completion.