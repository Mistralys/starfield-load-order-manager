# Key Data Flows

> Complete overview of application workflows and coordinator interactions.

---

## Startup & Configuration

- `App.OnStartup` initializes `LocalizationService` first, loads settings via `SettingsService.LoadSettingsAsync()`, applies preferred language via `LocalizationService.SetCulture()`.
- Creates `MainWindow`, sets `DataContext = new MainViewModel()`, and shows it.
- `MainViewModel` initializes 6 coordinators: `FileMonitoringCoordinator`, `StatusCoordinator`, `UpdateCheckCoordinator`, `ProfileCoordinator`, `ConfigurationCoordinator`, `GameLauncherCoordinator`.
- Validates Profiles folder via `ProfileService.EnsureProfilesFolderExists()`.
- Updates all coordinators with configuration via `UpdateConfiguration()` calls.
- `ConfigurationCoordinator.UpdateConfiguration()` validates paths and Profiles folder writability.
- `ProfileCoordinator.RefreshActiveProfileAsync()` loads active profile state.
- `GameLauncherCoordinator.UpdateConfiguration()` detects SFSE installation and updates play button text.
- `FileMonitoringCoordinator.UpdateState()` initializes monitoring with config, reference existence, and validation state.
- `UpdateCheckCoordinator.CheckForUpdatesBackgroundAsync()` checks for new versions on startup.
- Ensures default profile files exist through `ProfileService.EnsureDefaultProfileFilesAsync()`.
- If Profiles folder cannot be created or accessed, error dialog shown.
- If no reference exists yet but `Plugins.txt` is present, `FileService.CreateReferenceFileAsync()` seeds the active profile reference automatically.
- **Invalid Configuration Handling**: Application remains open with error banner when configuration is invalid. No automatic settings dialog is shown - users click the "Open settings" button in the error banner when ready. Secondary windows can be opened but show modal overlay preventing operations until configuration is fixed.

---

## Localization Flow

- `LocalizationService` singleton accessed via `App.LocalizationService` property; initialized in `App.OnStartup()` before any windows.
- Culture preference loaded from `AppConfigModel.PreferredLanguage` (defaults to `"auto"` for system culture detection).
- `LocalizationService.SetCulture(cultureName)` applies culture to both `CurrentUICulture` and `CurrentCulture` for consistent resource lookup and formatting.
- Resource files (`.resx`) generate satellite assemblies in culture-specific folders (e.g., `bin/Debug/net9.0-windows/fr/StarfieldLoadOrderKeeper.resources.dll` for French).
- ViewModels access resources via strongly-typed properties (e.g., `AboutWindowResources.ApplicationName` returns localized string based on current culture).
- `LocalizationService.CultureChanged` event allows ViewModels to refresh localized properties dynamically (not currently exposed in UI, prepared for future settings).
- `UserMessages` class provides centralized facade over `CommonResources` for consistent message access.
- Designer files (`.Designer.cs`) auto-generated with `PublicResXFileCodeGenerator` to create public strongly-typed accessors.
- Culture files follow naming convention: `CommonResources.fr.resx` (French), `CommonResources.de.resx` (German).
- `FlowDirection` property prepared in AboutViewModel for future RTL (Right-to-Left) language support.

---

## Coordinator Communication

- Coordinators raise events when state changes; `MainViewModel` subscribes and propagates to UI via `OnPropertyChanged()`.
- `FileMonitoringCoordinator.ChangeDetected` ? `MainViewModel.OnChangeDetected()` ? refreshes diff window if open.
- `ProfileCoordinator.ProfileChanged` ? `MainViewModel.OnProfileChanged()` ? shows status message.
- `ConfigurationCoordinator.ValidationChanged` ? `MainViewModel.OnConfigValidationChanged()` ? notifies commands to refresh `CanExecute` and updates secondary window overlays.
- All coordinator properties exposed as pass-through properties in `MainViewModel` for UI binding.

---

## Invalid Configuration Handling

- **ConfigurationCoordinator** tracks `IsConfigValid` state via `AppConfigModel.IsValid()` validation.
- **ValidationChanged Event**: Fired when configuration validity state changes, includes `WasValid`, `IsValid`, and `StateChanged` properties.
- **Main Window Error Banner**: Displayed when `ConfigurationCoordinator.ShowErrorBanner` is true; provides "Open settings" button for quick access.
- **Secondary Window Overlays**: DiffWindow, ManageProfilesWindow, ReferenceHistoryWindow, ViewPendingChangesWindow, and SwitchProfileWindow include `ConfigInvalidOverlay` control.
- **ConfigInvalidOverlay Control**: Reusable Material Design v5 user control with semi-transparent dark background and centered message card.
- **Overlay Display Logic**: `ShowOverlay` property computed as `!IsConfigValid && !IsOperationInProgress` to hide overlay during active operations.
- **ViewModel Integration**: Each secondary window ViewModel subscribes to `ConfigurationCoordinator.ValidationChanged` event and updates `IsConfigValid` property.
- **Coordinator Injection**: MainViewModel passes `_configCoordinator` reference when creating secondary window ViewModels.
- **Automatic Recovery**: Overlay disappears immediately when configuration becomes valid; windows remain open and preserve state.
- **Operation Management**: `IsOperationInProgress` flag prevents overlay during file operations to allow completion without interruption.
- **UI Command Changes**: `ShowDiffCommand` no longer checks `Config.IsValid()` in CanExecute; overlay protection replaces button disabling for better UX.
- **Preserved Restrictions**: File menu commands (`OpenPluginsFile`, `OpenReferenceFile`, etc.) and Play button still require valid configuration as they operate in main window.

---

## Profile Initialization & Switching

- `MainViewModel.SwitchProfileCommand` opens `SwitchProfileWindow` with `SwitchProfileViewModel`, which loads profiles via `ProfileService.LoadProfilesAsync()`.
- `SwitchProfileViewModel` receives `ConfigurationCoordinator` reference and subscribes to validation changes for overlay display.
- Selecting a profile calls `ProfileService.SwitchProfileAsync()`: the current `Plugins.txt` is persisted to the active profile's `main.txt`, the target profile's `main.txt` and `reference.txt` are ensured, the target `main.txt` replaces `Plugins.txt`, and `ActiveProfileId` is saved.
- After switching, `ProfileCoordinator.RefreshActiveProfileAsync()` updates active profile state and fires `ProfileChanged` event.
- `FileMonitoringCoordinator.UpdateState()` called to use new profile's reference file for monitoring.

---

## Profile Management

- `MainViewModel.ManageProfilesCommand` opens `ManageProfilesWindow` backed by `ManageProfilesViewModel`.
- `ManageProfilesViewModel` receives `ConfigurationCoordinator` reference and subscribes to validation changes for overlay display.
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
- Fires `ChangeDetected` event when **file state changes (false?true or true?false) OR when file signature changes** (enables auto-refresh of open diff windows).
- Event includes `HasChanges` boolean and `ChangeCount` integer in `ChangeDetectedEventArgs`.
- `MainViewModel` subscribes to `ChangeDetected` for main window state updates; `DiffDialogViewModel` also subscribes for automatic diff refresh.
- On differences, `FileService.WouldSortingChangeDiffsAsync()` sets the sorting recommendation, `DiffService.GetPluginsDiffAsync()` feeds both the badge count and the `DiffDialogViewModel`.
- Switching profiles triggers `FileMonitoringCoordinator.CheckPluginsFileAsync()` to detect changes with new reference.
- Fires `SteamWarningChanged` when Steam state changes, `SortingRecommendationChanged` when sorting issues detected.

---

## Diff Window Auto-Refresh

- **Event-Based Architecture**: `DiffDialogViewModel` subscribes directly to `FileMonitoringCoordinator.ChangeDetected` event in constructor and to `ConfigurationCoordinator.ValidationChanged` for overlay management.
- When file changes detected (every 3 seconds), `OnFileChangeDetected()` handler calls `RefreshDiffAsync()` automatically.
- `RefreshDiffAsync()` fetches latest diff via `DiffService.GetPluginsDiffAsync()`, compares signatures, and updates `DiffLines` collection if changed.
- `ReplaceDiffLines()` clears and repopulates `ObservableCollection<DiffLineModel>`, triggering UI updates via `UpdateDiffState()` and `OnPropertyChanged(nameof(DiffLines))`.
- Status message updated with timestamp: "Detected changes at HH:mm:ss" or "No new differences detected (HH:mm:ss)".
- `_isRefreshing` flag prevents concurrent refresh operations.
- `MainViewModel` no longer tracks `_activeDiffDialog`; each diff window is self-managing and reactive.
- Only tracks `_diffWindow` reference to prevent duplicate windows (single-instance guarantee).
- Window properly unsubscribes from `ChangeDetected` and `ValidationChanged` events in `Dispose()` when closed.
- `ConfigInvalidOverlay` displayed when configuration becomes invalid; hides automatically when valid again.

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

- `StatusCoordinator` maintains two separate status message collections:
  - **Display History** (`StatusMessageHistory`): Rolling window of last 3 status messages for UI display
  - **Internal Log** (`_allMessages`): Unlimited internal storage of all messages logged during the session
- Each status message has a timestamp and type (Info, Success, Warning, Error).
- `MainViewModel` calls `StatusCoordinator.AddStatusMessage()` for all status updates.
- Display history shown in main window UI via pass-through properties for quick reference of recent operations.
- Complete internal log accessible via `StatusCoordinator.GetAllMessages()` for debugging purposes.
- Full status history included in application state exports via `DebugStateService.CaptureDebugStateAsync()`.

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
- `ReferenceHistoryViewModel` receives `ConfigurationCoordinator` reference and subscribes to validation changes for overlay display.
- `ReferenceHistoryViewModel.LoadVersionsAsync()` calls `ReferenceHistoryService.LoadVersionHistoryAsync()` to read all version metadata from the active profile's `History/` folder.
- When user clicks "Update reference" in `DiffDialogViewModel`:
  1. Shows `CommentInputDialog` for optional comment (cancelling aborts the update)
  2. Loads pending changes via `ReferenceHistoryService.LoadPendingChangesAsync()`
  3. Calculates current changes via `FileService.CalculateReferenceChangesAsync()`
  4. Archives current reference with **previous** pending changes (including comment) via `ReferenceHistoryService.ArchiveCurrentReferenceAsync()`
  5. Stores **current** changes and comment as new pending via `ReferenceHistoryService.SavePendingChangesAsync()` using `PendingChangesModel.Create(comment, addedMods, removedMods)`
  6. Updates reference file via `FileService.CreateReferenceFileAsync()`
  7. Refreshes history window if open via `MainViewModel.RefreshReferenceHistoryWindowAsync()`
- **Comment Storage Flow**: Comments describe changes being accepted and are stored in pending changes. When the next reference update occurs, the pending changes (including the comment) are archived with that version. This ensures each version's comment accurately describes what changed when creating that version.
- **Pending Changes File**: Stored in `Profiles/{profileId}/pending-changes.json` with structure: `{ "Comment": "...", "AddedMods": [...], "RemovedMods": [...] }`
- `ArchiveCurrentReferenceAsync()` reads comment from pending changes internally, no longer accepts comment as parameter
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
- `ConfigInvalidOverlay` displayed when configuration becomes invalid; hides automatically when valid again.

---

## View Pending Changes

- `MainViewModel.ViewPendingChangesCommand` opens `ViewPendingChangesWindow` backed by `ViewPendingChangesViewModel`, tracking the instance to prevent duplicates.
- `ViewPendingChangesViewModel` receives `ConfigurationCoordinator` reference and subscribes to validation changes for overlay display.
- `ViewPendingChangesViewModel.LoadPendingChangesAsync()` calls `ReferenceHistoryService.LoadPendingChangesAsync()` to load current pending changes.
- Displays pending comment, added mods list, and removed mods list with Material Design v5 styling.
- **Edit Comment**: Opens `CommentInputDialog` to modify pending comment; saves immediately via `ReferenceHistoryService.SavePendingChangesAsync()`.
- Window is non-modal; can interact with main window while open.
- Single instance: Brings existing window to front if already open.
- `ConfigInvalidOverlay` displayed when configuration becomes invalid; hides automatically when valid again.

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

## Global Exception Handling

- Comprehensive unhandled exception capturing via `App.xaml.cs`:
  - **UI thread**: `Application.DispatcherUnhandledException`
  - **Non-UI threads**: `AppDomain.CurrentDomain.UnhandledException`
  - **Async tasks**: `TaskScheduler.UnobservedTaskException`
- All exceptions logged to `error.log` in application data folder with:
  - Timestamp and exception details (type, message, stack trace)
  - Full application state via `DebugStateService` (configuration, file contents, change list)
  - User privacy protection: all paths sanitized (replace user profile with `%USERPROFILE%`)
- `ErrorDialog` displayed after logging completes with user actions:
  - **Open Log Folder**: Opens app data folder in File Explorer
  - **Report Bug**: Opens GitHub issues page in browser
  - **Exit**: Immediately shuts down application (recommended)
  - **Ignore (Unsafe)**: Closes dialog and continues running (warning style)
- Log file reset on each application startup to keep logs focused on current session
- Test exception menu item in Debug menu for validation (`ThrowTestExceptionCommand`)

---

## Debug State Export

- `DebugStateService.CaptureDebugStateAsync()` provides comprehensive application state snapshot for troubleshooting.
- Accessible via Debug menu's "Copy Debug State" command in both main window and diff window.
- Exports complete JSON snapshot including:
  - **Application Version**: Current semantic version
  - **Configuration**: App data path, game path, active profile ID (all paths sanitized)
  - **Steam State**: Installation status and running status
  - **Total Changes**: Count of detected differences
  - **File Contents**: Complete `Plugins.txt` and reference file contents
  - **Change List**: Full diff with all detected modifications
  - **Status Messages**: Complete internal log of all status messages from session start
- All file paths sanitized automatically (`%USERPROFILE%` placeholder for user profile paths).
- JSON output prettified for readability with indentation.
- `StatusCoordinator.GetAllMessages()` provides chronological history of all logged messages.
- Status messages include timestamp, message text, and type (Info, Success, Warning, Error).
- Enables users to share complete diagnostic information with developers without exposing sensitive data.

---

[<< Back to Index](README.md)
