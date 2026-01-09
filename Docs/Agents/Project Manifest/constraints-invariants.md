# Current Constraints & Invariants

> Rules, guarantees, and system constraints that must be maintained.

---

## Coordinator Architecture

- All coordinators inherit from `CoordinatorBase` which provides `INotifyPropertyChanged` and `IDisposable`.
- Coordinators are initialized in `MainViewModel` constructor and disposed in `Dispose()` method.
- Communication between coordinators and ViewModels is event-driven via `PropertyChanged` and custom events.
- `MainViewModel` reduced from ~1300 lines to ~900 lines (31% reduction) through coordinator extraction.
- Pass-through properties in `MainViewModel` expose coordinator state for UI binding.
- Each coordinator has single responsibility: file monitoring, status, updates, profiles, configuration, or game launching.

---

## Configuration Validity

- `ConfigurationCoordinator` manages validation state with caching to prevent excessive I/O.
- `AppConfigModel.IsValid()` requires non-empty paths, existing `StarfieldAppDataPath` and `StarfieldGamePath`, plus `StarfieldGamePath/Data` present.
- `AppConfigModel.IsValid()` also requires `Plugins.txt` to exist in `StarfieldAppDataPath` (cannot be auto-generated, user must run Starfield at least once).
- `AppConfigModel.IsValid()` validates Profiles folder creation and writability with test file.
- The app shuts down when configuration remains invalid after the settings dialog.
- `ConfigurationCoordinator.ValidationChanged` event fires when validation state changes, triggering command `CanExecute` updates.

---

## Profile Storage

- Profiles live under `StarfieldAppDataPath/Profiles/{profileId}` with `profile.json`, `main.txt`, and `reference.txt`; folders are created automatically.
- Profiles folder existence and writability validated via `ProfileService.EnsureProfilesFolderExists()` which tests write access with temporary file.
- Profiles folder validation integrated into `AppConfigModel.IsValid()` and settings window validation.
- Profile operations fail with actionable error messages if Profiles folder cannot be created or accessed.
- `ActiveProfileId` (default `default`) resides in `AppConfigModel` and is persisted through `SettingsService`.
- The default profile (`id = default`) is virtual, cannot be deleted or edited, and is auto-recreated when files are missing.
- Profile labels must be unique (case-insensitive), 2–30 chars, trimmed, and cannot be `Default`; IDs are transliterated ASCII with dash separators via `ProfileService.GenerateProfileId()` and gain numeric suffixes for uniqueness.
- `ProfileCoordinator` manages active profile state and fires `ProfileChanged` event when profile switches.

---

## Profile Switching Guarantees

- Switching always backs up the current `Plugins.txt` into the old profile's `main.txt`, ensures the target `main.txt` and `reference.txt`, writes UTF-8 (no BOM), and updates `ActiveProfileId` before monitoring continues.
- `ProfileCoordinator.SwitchProfileAsync()` delegates to `ProfileService.SwitchProfileAsync()` and updates coordinator state.

---

## File Locations & I/O

- `Plugins.txt` stays under `StarfieldAppDataPath`; references are profile-specific (`Profiles/{id}/reference.txt`).
- `Plugins.txt` must exist for configuration to be valid (cannot be auto-generated, created by Starfield on first run).
- All disk operations in services are asynchronous; plugins-related writes use UTF-8 without BOM, and reference creation copies raw files to retain comments.

---

## Case Restoration

- `FileService.ApplyLoadOrderAsync()` builds a case map from `StarfieldGamePath/Data` (`*.esm` / `*.esp`) so output lines reuse on-disk casing.

---

## Diff Semantics & Monitoring

- `FileMonitoringCoordinator` handles all periodic checking (3-second interval) and change detection.
- `FileService.GetModDiffAsync()` bases `ModDiffModel` flags on original vs current line numbers; `DiffService` translates them to `DiffLineModel` change types (`Added`, `Removed`, `Moved`, `Replaced`, `Inserted`).
- The monitor compares trimmed file contents, tracks a `PluginsSignature`, and only runs when state is valid (config valid, reference exists, not busy, config not invalid).
- Monitoring paused when `configIsInvalid` is true to prevent I/O operations on invalid paths.
- Dependent changes are tracked and displayed: when a mod is removed/added, all mods that shift position as a result are shown as dependent changes.
- `FileMonitoringCoordinator` fires `ChangeDetected` event with `HasChanges` and `ChangeCount` when changes detected.

---

## Steam Process Detection

- `FileMonitoringCoordinator` detects Steam installation via Windows registry.
- Checks for running steam.exe process on same 3-second timer as file monitoring.
- Warning shown when both Steam installed and Steam running.
- Warning automatically dismissed when Steam closes.
- Tooltip message: "Steam is running. To prevent conflicts, it is recommended to close Steam before making changes to the load order."
- Detection uses `Process.GetProcessesByName("steam")` for efficient process checking.
- `SteamWarningChanged` event fired when warning state changes (Steam starts/stops).

---

## Navigation & Threading

- Modal windows (`SettingsWindow`, `SwitchProfileWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`, `UpdateOptionsDialog`, `CommentInputDialog`) block until close; viewmodels flow back via dialog results/events.
- Non-modal windows (`DiffWindow`, `ManageProfilesWindow`, `ReferenceHistoryWindow`) allow main window interaction while open; `WindowManager` coordinator tracks instances to prevent duplicates and manages window lifecycle.
- `FileMonitoringCoordinator` timer runs on the UI thread; service calls are awaited, `IsBusy` gates commands, and UI updates stay on the dispatcher thread.

---

## Error Handling

- Services throw `InvalidOperationException`, `IOException`, or `ArgumentException` when invariants break; `MainViewModel` captures these, updates status via `StatusCoordinator.AddStatusMessage()`, and surfaces `ConfirmationDialog` for errors.
- All user-facing dialogs use `ConfirmationDialog` with appropriate icon types (Error, Warning, Information) for consistent Material Design v5 styling.
- `IOException` includes specific messages for common issues: access denied, disk full, network paths.
- Profiles folder creation failures caught at startup with actionable error dialogs offering settings access.
- Profile operations (create, copy) validate Profiles folder exists via `ProfileService.EnsureProfilesFolderExists()` before proceeding.
- All profile folder errors include actionable guidance (check permissions, change location).
- Secondary windows (ManageProfilesWindow, etc.) append `UserMessages.ConfigInvalidGuidance` or `UserMessages.ProfilesFolderRequired` based on error type.
- Steam library detection (`TryFindStarfieldInSteamLibraries`) silently catches all exceptions (missing VDF file, parse errors, I/O errors) and returns null, allowing fallback detection methods to execute.

---

## Configuration Validation

- `ConfigurationCoordinator` caches validation state to minimize repeated file system checks.
- `AppConfigModel.IsValid()` validates paths AND Plugins.txt existence AND Profiles folder creation/writability with test file.
- Error banner (`ShowErrorBanner` from coordinator) shown in main window when paths invalid; includes "Open settings" button.
- Status banner in settings window provides real-time feedback with error/success states:
  - **Error state**: Shows specific path issues (app data invalid, game path invalid, both invalid, Data folder missing, Plugins.txt missing, Profiles folder access issues)
  - **Success state**: Confirms "The configured paths are valid" with checkmark icon
- Validation runs on: window open, input blur, save button click, auto-detected path click.
- Validation order: paths configured ? paths exist ? Data folder exists ? Plugins.txt exists ? Profiles folder writable.
- All operations gated by validation check to prevent I/O failures with invalid paths.
- `ConfigurationCoordinator.GetValidationResult()` provides detailed error messages for debugging and user feedback.
- Centralized error messages in `Constants/UserMessages.cs` for easy modification and future localization.

---

[? Back to Index](README.md)
