# Tech Stack & Patterns

> Detailed overview of runtime, libraries, frameworks, and architectural patterns used in the application.

---

## Runtime / Platform

- .NET 9
- WPF desktop application
- **Localization**: JSON-based, zero-hardcoding architecture
  - Supported locales: en-US, de-DE, fr-FR, es-ES, it-IT, zh-CN, ja-JP, pt-BR
  - Automatic system language detection with fallback
  - Runtime culture switching support

---

## Libraries / Frameworks

- **CommunityToolkit.Mvvm**
  - `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`, `RelayCommand`, `AsyncRelayCommand`, `IRelayCommand`, `IAsyncRelayCommand`
- **MaterialDesignThemes** / **MaterialDesignColors** for dialogs, icons, and card layouts (v5)
- **Gameloop.Vdf** for parsing Steam library configuration files (Valve Data Format)
- **System.Text.Json** for JSON serialization/deserialization (localization files, configuration, metadata)
- **System.Text.Encodings.Web** for JSON Unicode handling in localization
- Standard .NET `System.*` APIs for I/O, processes, collections, and globalization

---

## Architectural Patterns

### MVVM

**ViewModels:**
- `MainViewModel`, `SettingsViewModel`, `DiffDialogViewModel`, `SwitchProfileViewModel`, `ManageProfilesViewModel`, `ProfilePropertiesViewModel`, `ConfirmationDialogViewModel`, `AboutViewModel`, `UpdateOptionsViewModel`, `ReferenceHistoryViewModel`, `CommentInputViewModel`, `ViewPendingChangesViewModel`, `ErrorDialogViewModel`

**Text ViewModels (Localization):**
- `MenuViewModel`, `MainWindowTexts`, `MainWindowStatusTexts`, `AboutViewModel`, `ErrorDialogTexts`, `CommentInputTexts`, `ConfirmationDialogTexts`, `ConfigInvalidOverlayTexts`, `SettingsWindowTexts`, `DiffDialogTexts`, `ManageProfilesTexts`, `ProfilePropertiesTexts`, `SwitchProfileTexts`, `ReferenceHistoryTexts`, `ReferenceManagementStatusTexts`, `UpdateOptionsTexts`, `ViewPendingChangesTexts`, `ViewModelInitializerStatusTexts`, `CommonTexts`

**Views:**
- `MainWindow`, `SettingsWindow`, `DiffWindow`, `SwitchProfileWindow`, `ManageProfilesWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`, `UpdateOptionsDialog`, `ReferenceHistoryWindow`, `CommentInputDialog`, `ViewPendingChangesWindow`, `ErrorDialog`

**User Controls:**
- `ConfigInvalidOverlay`

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
- `DiffService`: LCS-based diff pipeline — computes the longest common subsequence of mod lists, classifies changes (Removed, Moved, Replaced, Inserted, Added), groups dependent shifts under their causal change, and constructs localized `DiffLineModel` entries for the UI
- `ProfileService`: profile discovery, CRUD, switching, and file scaffolding
- `VersionService`: centralized application version retrieval
- `UpdateCheckService`: GitHub API integration for version checking with caching
- `ReferenceHistoryService`: version history management, archiving, rollback, and pending changes tracking
- `DateTimeFormattingService`: user-friendly date/time formatting utilities
- `ErrorLoggingService`: exception logging with user privacy protection (path sanitization)
- `DebugStateService`: application state capture for debugging with sanitized paths

### Localization Services

- `LocalizationService`: singleton service managing JSON-based translations
  - Thread-safe string retrieval with culture-specific lookups
  - Format string support with placeholder replacement
  - Runtime culture switching with event notification
  - Automatic system locale detection with parent culture mapping
  - Fallback to English (en-US) for unsupported cultures
- `LocalizationJsonNormalizer`: utility for JSON file normalization
  - Reads, deserializes, and re-serializes JSON with proper encoding
  - Uses `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` for readable output
  - Ensures consistent formatting and Unicode character handling

### Instance Services

Instance services support MainViewModel with dependency injection via constructor callbacks:

- `FileOperationsService`: file and folder opening with shell integration (Plugins.txt, reference, folders)
- `ReferenceManagementService`: reference file creation/update workflow including comment input, archiving, pending changes, and rollback
- `WindowLifecycleService`: non-modal window management (singleton tracking, activation, cleanup)
- `ViewModelInitializer`: MainViewModel startup sequence (config loading, validation, profile setup, coordinator initialization)

### Helper Classes

- `CoordinatorEventBinder`: simplifies property change forwarding from coordinators to ViewModels using declarative binding methods
- `MenuViewModel`: consolidates all menu and UI text properties for centralized management and easier localization

---

## Localization System

### Architecture

- **JSON-based**: Translations stored in `ViewTexts/Locales/*.json` files
- **Supported Languages**: English (en-US), German (de-DE), French (fr-FR), Spanish (es-ES), Italian (it-IT), Simplified Chinese (zh-CN), Japanese (ja-JP), Portuguese Brazil (pt-BR)
- **Text ViewModels**: Observable ViewModels in `ViewTexts/` providing localized strings to UI

### Key Features

1. **Automatic Language Detection**
   - Detects system culture via `CultureInfo.CurrentUICulture`
   - Maps parent cultures (e.g., `fr-CA` ? `fr-FR`, `de-AT` ? `de-DE`)
   - Falls back to English for unsupported languages

2. **Manual Language Override**
   - `AppConfigModel.PreferredLanguage` setting (default: "auto")
   - Initialized early in `ViewModelInitializer.LoadInitialStateAsync`
   - Persists user preference across sessions

3. **Runtime Culture Switching**
   - `LocalizationService.SetCulture(cultureName)` changes active language
   - `CultureChanged` event notifies all Text ViewModels
   - UI updates automatically via `INotifyPropertyChanged`

4. **Format String Support**
   - Placeholders: `{0}`, `{1}`, etc. for dynamic values
   - Example: `"Version {0} available!"` ? `"Version 1.5.0 available!"`
   - Safe fallback if formatting fails

5. **Thread Safety**
   - All operations protected by `lock` statement
   - Safe for concurrent access from multiple threads

### JSON File Structure

```json
{
  "SectionName": {
    "StringKey": "Translated value",
    "FormatKey": "Value with {0} placeholder"
  }
}
```

### Text ViewModel Pattern

All Text ViewModels follow this pattern:
- Inherit from `ObservableObject`
- Hold reference to `LocalizationService.Instance`
- Subscribe to `CultureChanged` event in constructor
- Expose localized strings as properties via `GetString(section, key)`
- Call `OnPropertyChanged` for all string properties on culture change

Example:
```csharp
public class SomeTexts : ObservableObject
{
    private readonly LocalizationService _loc = LocalizationService.Instance;
    
    public string SomeProperty => _loc.GetString("Section", "Key");
    
    public SomeTexts()
    {
        _loc.CultureChanged += (s, e) => OnPropertyChanged(nameof(SomeProperty));
    }
}
```

### Build Configuration

- JSON files configured with `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`
- Files copied to `bin/.../ViewTexts/Locales/` maintaining folder structure
- Runtime path resolution: `AppDomain.CurrentDomain.BaseDirectory + "ViewTexts/Locales"`

**CRITICAL**: When adding a new locale file (e.g., `zh-CN.json`):
1. Add the file to the `ViewTexts/Locales/` directory
2. **MUST** configure build action in `.csproj`:
   ```xml
   <ItemGroup>
     <Content Include="ViewTexts\Locales\zh-CN.json">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </Content>
   </ItemGroup>
   ```
3. Without this configuration, the file will NOT be copied to output directory
4. Result: Language will not appear in dropdown even though code supports it
5. Verify after build: Check `bin/Debug/net9.0-windows/ViewTexts/Locales/` for new file

**Existing configured locales**: en-US.json, de-DE.json, fr-FR.json, es-ES.json, it-IT.json, zh-CN.json

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

### Global Exception Handling

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

[<< Back to Index](README.md)
