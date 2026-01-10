# Key Data Flows

> Complete overview of application workflows and coordinator interactions.

---

## Startup & Configuration

- `App.OnStartup` creates `MainWindow`, sets `DataContext = new MainViewModel()`, and shows it.
- `MainViewModel` initializes 6 coordinators: `FileMonitoringCoordinator`, `StatusCoordinator`, `UpdateCheckCoordinator`, `ProfileCoordinator`, `ConfigurationCoordinator`, `GameLauncherCoordinator`.
- Loads settings via `SettingsService.LoadSettingsAsync()`, validates Profiles folder via `ProfileService.EnsureProfilesFolderExists()`.
- Updates all coordinators with configuration via `UpdateConfiguration()` calls.
- `ConfigurationCoordinator.UpdateConfiguration()` validates paths and Profiles folder writability.
- `ProfileCoordinator.RefreshActiveProfileAsync()` loads active profile state.
- `GameLauncherCoordinator.UpdateConfiguration()` detects SFSE installation and updates play button text.
- `FileMonitoringCoordinator.UpdateState()` initializes monitoring with config, reference existence, and validation state.
- `UpdateCheckCoordinator.CheckForUpdatesBackgroundAsync()` checks for new versions on startup.
- Ensures default profile files exist through `ProfileService.EnsureDefaultProfileFilesAsync()`.
- If Profiles folder cannot be created or accessed, error dialog shown with option to open settings or shutdown.
- If no reference exists yet but `Plugins.txt` is present, `FileService.CreateReferenceFileAsync()` seeds the active profile reference automatically.

---

## Coordinator Communication

- Coordinators raise events when state changes; `MainViewModel` subscribes and propagates to UI via `OnPropertyChanged()`.
- `FileMonitoringCoordinator.ChangeDetected` ? `MainViewModel.OnChangeDetected()` ? refreshes diff window if open.
- `ProfileCoordinator.ProfileChanged` ? `MainViewModel.OnProfileChanged()` ? shows status message.
- `ConfigurationCoordinator.ValidationChanged` ? `MainViewModel.OnConfigValidationChanged()` ? notifies commands to refresh `CanExecute`.
- All coordinator properties exposed as pass-through properties in `MainViewModel` for UI binding.

---

## Profile Initialization & Switching

- `MainViewModel.SwitchProfileCommand` opens `SwitchProfileWindow` with `SwitchProfileViewModel`, which loads profiles via `ProfileService.LoadProfilesAsync()`.
- Selecting a profile calls `ProfileService.SwitchProfileAsync()`: the current `Plugins.txt` is persisted to the active profile's `main.txt`, the target profile's `main.txt` and `reference.txt` are ensured, the target `main.txt` replaces `Plugins.txt`, and `ActiveProfileId` is saved.
- After switching, `ProfileCoordinator.RefreshActiveProfileAsync()` updates active profile state and fires `ProfileChanged` event.
- `FileMonitoringCoordinator.UpdateState()` called to use new profile's reference file for monitoring.

---

## Profile Management

- `MainViewModel.ManageProfilesCommand` opens `ManageProfilesWindow` backed by `ManageProfilesViewModel`.
- The manage view requests CRUD actions: `ProfileService.CreateProfileAsync()`, `UpdateProfileAsync()`, `DeleteProfileAsync()`, and `CopyProfileAsync()` handle persistence; `ProfilePropertiesWindow` + `ProfilePropertiesViewModel` validates labels/descriptions before save.
- The profiles list refreshes after each operation so the UI and `MainViewModel` reflect edits.

---

## Settings Flow

- `MainViewModel.OpenSettingsCommand` shows `SettingsWindow`; on success, `SettingsService.SaveSettingsAsync()` persists `AppConfigModel`.
- `ConfigurationCoordinator.UpdateConfiguration()` validates new configuration and fires `ValidationChanged` event if state changes.
- `ProfileCoordinator.UpdateConfiguration()` updates profile context.
- `GameLauncherCoordinator.UpdateConfiguration()` redetects SFSE and updates button text.
- `RefExists` is recomputed and `FileMonitoringCoordinator.UpdateState()` called with new config.
- Configuration edits retain `ActiveProfileId`, so profile-specific references stay aligned.
- `SettingsService.TryGetDefaultSteamPath()` intelligently detects Starfield installation by:
  1. Calling `TryGetSteamInstallPath()` to find main Steam installation via Windows registry (CurrentUser, LocalMachine paths)
  2. If found, calling `TryFindStarfieldInSteamLibraries()` to parse `libraryfolders.vdf` using Gameloop.Vdf
  3. Iterating through numeric library keys (0, 1, 2, ...) to check each library's `apps` collection for Starfield AppID (1716740)
  4. Validating installation by checking for `Data` subfolder presence
  5. Falling back to default Steam installation location if VDF parsing fails
  6. Final fallback to Program Files location if all detection methods fail
- Path normalization converts forward slashes to backslashes for Windows consistency
- All Steam detection failures are silent to avoid disrupting user experience

---

## Reference & Load Order Controls

- `CreateReferenceCommand` and `FixLoadOrderCommand` call `FileService.CreateReferenceFileAsync()` and `FileService.ApplyLoadOrderAsync()` respectively; both commands gate on configuration validity and `IsBusy`.
- `DiscardChangesCommand` resets `Plugins.txt` from the active profile reference via `FileService.DiscardChangesAsync()`.

---

## Monitoring & Diffing

- `FileMonitoringCoordinator` runs periodic checks every 3 seconds when state is valid (config valid, reference exists, not busy).
- `CheckPluginsFileAsync()` calls `FileService.ComparePluginsWithReferenceAsync()` to compare against the active profile's reference.
- Detects Steam process (steam.exe) running and updates `IsSteamRunning`, `ShowSteamWarning`, and `SteamWarningTooltip` properties.
- Fires `ChangeDetected` event when file changes detected, `SteamWarningChanged` when Steam state changes, `SortingRecommendationChanged` when sorting issues detected.
- On differences, `FileService.WouldSortingChangeDiffsAsync()` sets the sorting recommendation, `DiffService.GetPluginsDiffAsync()` feeds both the badge count and the `DiffDialogViewModel`.
- Switching profiles triggers `DiffDialogViewModel.RefreshDiffAsync()` when diff window is open.

---

## Steam Process Detection

- `FileMonitoringCoordinator.DetectSteamProcess()` checks if steam.exe is running using `Process.GetProcessesByName()`.
- Updates `IsSteamInstalled` (checks registry for Steam installation).
- Updates `IsSteamRunning` (checks for running steam.exe process).
- Calculates `ShowSteamWarning` (true when both Steam installed and running).
- Generates `SteamWarningTooltip` with contextual message explaining why Steam should be closed.
- Fires `SteamWarningChanged` event when warning state changes.
- `MainViewModel` exposes pass-through properties for UI binding.
- Warning banner in `MainWindow` shows/hides automatically based on Steam state.

---

## Diff Dialog Operations

- In `DiffDialogViewModel`, commands trigger `FileService.ReEnableModAsync()`, `RemoveNewModAsync()`, `ReplaceModWithNewAsync()`, and `MainViewModel.DiscardChangesCommand` (which calls `FileService.DiscardChangesAsync()`), refreshing diffs afterward.
- Update reference and discard changes actions request confirmation via `ConfirmationRequested` event, which is handled by `DiffWindow` to show `ConfirmationDialog`.
- **Multiple Replacements Help**: When 2+ removals or replacements detected, `ShowMultipleReplacementsHelp` property becomes true, triggering blue info banner display.
- Info banner explains two workflow options: (1) Accept after each replacement, or (2) Make all replacements then accept once.
- Banner message stored in `MultipleReplacementsHelpMessage` property for consistent display.
- `UpdateDiffState()` notifies `ShowMultipleReplacementsHelp` property change whenever diff lines collection changes.

---

## Confirmation Dialogs

- All `MessageBox.Show` calls replaced with `ConfirmationDialog.Show()` static method.
- `ConfirmationDialog` provides Material Design v5 styled dialogs with icon support (Information, Question, Warning, Error) and multiple button configurations.
- `DiffDialogViewModel` raises `ConfirmationRequested` event for critical actions (update reference, discard changes); `DiffWindow` handles the event and shows the dialog.
- Error messages in `MainViewModel` and profile management windows use `ConfirmationDialog.Show()` for consistent UX.

---

## About & Version Info

- `MainViewModel.ShowAboutCommand` opens `AboutWindow` with `AboutViewModel`.
- `VersionService.GetApplicationVersion()` retrieves clean semantic version from assembly attributes, stripping commit hashes.
- `AboutViewModel.OpenHomepageCommand` launches the project homepage URL in the default browser.

---

## Status History

- `StatusCoordinator` maintains `StatusMessageHistory` (ObservableCollection) with last 3 status messages.
- Each status message has a timestamp and type (Info, Success, Warning, Error).
- `MainViewModel` calls `StatusCoordinator.AddStatusMessage()` for all status updates.
- Displayed in main window UI via pass-through properties for quick reference of recent operations.

---

## Version Check & Updates

- `UpdateCheckCoordinator` manages all update checking and notification state.
- `CheckForUpdatesBackgroundAsync()` called on startup, uses 24-hour cache.
- If update available, fires `PropertyChanged` for `UpdateAvailable`, `UpdateMessage`, and `UpdateInfoBarVisible`.
- `MainViewModel` exposes these as pass-through properties for UI binding.
- `MainViewModel.CheckForUpdatesCommand` (from Help menu) calls `CheckForUpdatesManualAsync()` which bypasses cache.
- Manual check shows `ConfirmationDialog` if no update available, or updates info bar if update found.
- `MainViewModel.OpenDownloadPageCommand` shows `UpdateOptionsDialog` with download buttons for Nexusmods and GitHub.
- `UpdateOptionsViewModel` opens URLs in default browser via `Process.Start()` and closes dialog automatically.
- Network failures in background check are silent; manual check shows `UpdateOptionsDialog` with error message.
- `UpdateCheckService` caches results in `%LOCALAPPDATA%\StarfieldLoadOrderKeeper\update-check-cache.json` with 24-hour expiration.
- Version comparison uses semantic versioning, ignores pre-release versions, and only notifies for newer stable releases.
- GitHub API endpoint: `https://api.github.com/repos/Mistralys/starfield-load-order-manager/releases/latest` with 10-second timeout.

---

## Reference History & Versioning

- `MainViewModel.ShowReferenceHistoryCommand` opens `ReferenceHistoryWindow` backed by `ReferenceHistoryViewModel`, tracking the instance to prevent duplicates.
- `ReferenceHistoryViewModel.LoadVersionsAsync()` calls `ReferenceHistoryService.LoadVersionHistoryAsync()` to read all version metadata from the active profile's `History/` folder.
- When user clicks "Update reference" in `DiffDialogViewModel`:
  1. Shows `CommentInputDialog` for optional comment (cancelling aborts the update)
  2. Loads pending changes via `ReferenceHistoryService.LoadPendingChangesAsync()`
  3. Calculates current changes via `FileService.CalculateReferenceChangesAsync()`
  4. Archives current reference with **previous** pending changes via `ReferenceHistoryService.ArchiveCurrentReferenceAsync()`
  5. Stores **current** changes as new pending via `ReferenceHistoryService.SavePendingChangesAsync()`
  6. Updates reference file via `FileService.CreateReferenceFileAsync()`
  7. Refreshes history window if open via `MainViewModel.RefreshReferenceHistoryWindowAsync()`
- On-demand migration: When history is empty and no pending changes exist, `ArchiveCurrentReferenceAsync()` automatically creates "Initial version" with no changes, then stores current diff as pending.
- `ReferenceHistoryViewModel.RollbackRequested` event triggers `MainViewModel.HandleRollbackRequestAsync()`:
  1. Shows confirmation dialog with version details
  2. Calls `ReferenceHistoryService.RollbackToVersionAsync()` to restore archived reference as current reference
  3. Closes history window
  4. Triggers `FileMonitoringCoordinator.CheckPluginsFileAsync()` to show changes in diff window for review
- Context menu actions call `ReferenceHistoryService.UpdateVersionCommentAsync()`, `DeleteVersionAsync()`, and `ClearAllHistoryAsync()` with confirmation dialogs.
- History window auto-refreshes when new versions created while window is open (non-modal behavior).
- Each profile maintains independent history with maximum 16 versions; `ReferenceHistoryService.PruneOldVersionsAsync()` removes oldest versions after each archive.
- All version files and metadata stored as UTF-8 without BOM in `Profiles/{profileId}/History/` folder.
- `DateTimeFormattingService.FormatFriendly()` provides user-friendly timestamps ("Today 14:56", "Yesterday 16:41", "Jan 15 14:56", "Dec 25, 2023 14:56").

---

## Game Launching

- `GameLauncherCoordinator` manages SFSE detection and game launching.
- `UpdateConfiguration()` called when game path changes, triggers SFSE detection.
- Checks for `sfse_loader.exe` presence in game folder.
- Updates `HasSfseInstalled` and `PlayButtonText` ("Play (SFSE)" or "Play (Vanilla)") accordingly.
- `MainViewModel.PlayGame()` calls `GameLauncherCoordinator.LaunchGame()`.
- Returns success/failure; `MainViewModel` shows error if launch fails.
- Automatically selects correct executable (SFSE loader or vanilla) based on detection.

---

[? Back to Index](README.md)
