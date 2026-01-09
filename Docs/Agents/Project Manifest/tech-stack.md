# Tech Stack & Patterns

> Detailed overview of runtime, libraries, frameworks, and architectural patterns used in the application.

---

## Runtime / Platform

- .NET 9
- WPF desktop application

---

## Libraries / Frameworks

- **CommunityToolkit.Mvvm**
  - `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`, `RelayCommand`, `AsyncRelayCommand`, `IRelayCommand`, `IAsyncRelayCommand`
- **MaterialDesignThemes** / **MaterialDesignColors** for dialogs, icons, and card layouts (v5)
- **Gameloop.Vdf** for parsing Steam library configuration files (Valve Data Format)
- Standard .NET `System.*` APIs for I/O, processes, collections, and JSON serialization

---

## Architectural Patterns

### MVVM

**ViewModels:**
- `MainViewModel`, `SettingsViewModel`, `DiffDialogViewModel`, `SwitchProfileViewModel`, `ManageProfilesViewModel`, `ProfilePropertiesViewModel`, `ConfirmationDialogViewModel`, `AboutViewModel`, `UpdateOptionsViewModel`, `ReferenceHistoryViewModel`, `CommentInputViewModel`

**Views:**
- `MainWindow`, `SettingsWindow`, `DiffWindow`, `SwitchProfileWindow`, `ManageProfilesWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`, `UpdateOptionsDialog`, `ReferenceHistoryWindow`, `CommentInputDialog`

### Coordinator Pattern

Coordinators handle specific domain logic and state management:

- All coordinators inherit from `CoordinatorBase` (provides `INotifyPropertyChanged` + `IDisposable`)
- Event-driven communication between coordinators and ViewModels
- **Coordinators:**
  - `FileMonitoringCoordinator`: Periodic file monitoring, change detection, Steam process detection, sorting recommendations
  - `StatusCoordinator`: Status message management and history tracking
  - `UpdateCheckCoordinator`: Background and manual update checking with caching
  - `ProfileCoordinator`: Active profile state management and switching
  - `ConfigurationCoordinator`: Configuration validation with caching and detailed error reporting
  - `GameLauncherCoordinator`: SFSE detection and game launching
  - `WindowManager`: Window lifecycle management and duplicate prevention

### Static Services

- `SettingsService`: configuration persistence and default path discovery (includes Steam library detection)
- `FileService`: plugins/reference file operations plus diff helpers
- `DiffService`: diff line construction for the UI
- `ProfileService`: profile discovery, CRUD, switching, and file scaffolding
- `VersionService`: centralized application version retrieval
- `UpdateCheckService`: GitHub API integration for version checking with caching
- `ReferenceHistoryService`: version history management, archiving, rollback, and pending changes tracking
- `DateTimeFormattingService`: user-friendly date/time formatting utilities

---

## Navigation

### Modal Navigation

- `MainWindow` as shell
- Secondary windows opened modally: `SettingsWindow`, `SwitchProfileWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`, `UpdateOptionsDialog`, `CommentInputDialog`

### Non-Modal Windows

- `DiffWindow`, `ManageProfilesWindow`, `ReferenceHistoryWindow` allow main window interaction while open; tracked by `WindowManager` coordinator to prevent duplicates

---

## Feature Patterns

### File Monitoring

- `FileMonitoringCoordinator` uses fixed 3-second interval (optimized through testing)
- Monitoring paused when configuration invalid to prevent unnecessary I/O operations
- Detects Steam process running and shows warning banner when detected

### Steam Process Detection

- `FileMonitoringCoordinator` detects Steam process (steam.exe) running
- Shows persistent warning banner when Steam is running
- Provides tooltip explaining why Steam should be closed before making changes
- Detection runs on same 3-second timer as file monitoring
- Warning automatically dismissed when Steam closes

### Profile Management

- Profiles stored per active configuration under `Profiles/{profileId}` with `main.txt`, `reference.txt`, `profile.json`, `pending-changes.json`, and `History/` folder
- `ProfileCoordinator` manages active profile state and switching
- Commands and dialogs coordinate through `ProfileService` to switch and manage profiles

### Reference History

- Each profile maintains independent version history in `Profiles/{profileId}/History/`
- Automatic versioning with pending changes system tracks modifications between updates
- Maximum 16 versions per profile with automatic pruning of oldest versions
- Rollback support replaces `Plugins.txt` with archived reference for review in diff window
- User comments and change tracking (added/removed mods) stored in JSON metadata
- On-demand migration creates initial version for existing installations transparently

### Confirmation Dialogs

- Custom Material Design styled `ConfirmationDialog` replaces all `MessageBox.Show` calls
- Supports multiple icon types (Information, Question, Warning, Error) and button configurations (OK, OKCancel, YesNo, YesNoCancel)

### Update Notifications

- `UpdateCheckCoordinator` manages update checking and notification state
- Non-intrusive info bar in `MainWindow` shows when updates available
- Automatic background check on startup with 24-hour caching
- Manual check via Help menu bypasses cache
- `UpdateOptionsDialog` provides clickable download buttons for Nexusmods and GitHub

### Configuration Validation

- `ConfigurationCoordinator` manages validation state with caching to prevent excessive I/O
- Error banner in `MainWindow` displays when paths are invalid with "Open settings" button
- Status banner in `SettingsWindow` shows real-time validation feedback (error/success states)
- Validation triggers: timer tick, config changes, settings save, auto-detected path clicks
- Secondary windows append guidance message to errors when config invalid
- Centralized error messages in `Constants/UserMessages.cs` for maintainability

### Steam Library Detection

- `SettingsService` parses Steam's `libraryfolders.vdf` to locate Starfield across all Steam library folders
- Detects Steam installation via Windows registry, searches all configured libraries for Starfield (AppID: 1716740)
- Validates installations by checking for `Data` folder presence
- Silent failure with multi-level fallbacks ensures robust path detection

---

[? Back to Index](README.md)
