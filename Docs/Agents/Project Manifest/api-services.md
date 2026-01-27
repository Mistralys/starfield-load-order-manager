# Services API

> Public API signatures for all static and instance service classes.

---

## Configuration Services

### `LoadOrderKeeper.Services.SettingsService`

```csharp
public static class SettingsService
{
    public static Task<AppConfigModel> LoadSettingsAsync();
    public static Task SaveSettingsAsync(AppConfigModel config);
    public static string TryGetDefaultSteamPath();
    public static string TryGetDefaultAppDataPath();
    public static string GetConfigFolderPath();
    public static bool IsStarfieldInstalledViaSteam();
    public static bool IsSteamRunning();
    
    // Private methods for Steam detection
    private static string? TryGetSteamInstallPath();
    private static string? TryFindStarfieldInSteamLibraries(string steamInstallPath);
    private static string? TryGetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName);
    private static string NormalizePath(string path);
}
```

---

## File & Diff Services

### `LoadOrderKeeper.Services.FileService`

```csharp
public static class FileService
{
    public static bool DoesReferenceFileExist(AppConfigModel config);
    public static Task CreateReferenceFileAsync(AppConfigModel config);
    public static Task ApplyLoadOrderAsync(AppConfigModel config);
    public static Task<bool> HasPluginsFileChangedAsync(AppConfigModel config);
    public static Task<PluginsComparisonResult> ComparePluginsWithReferenceAsync(AppConfigModel config);
    public static Task<IReadOnlyList<ModDiffModel>> GetModDiffAsync(AppConfigModel config);
    public static Task<bool> WouldSortingChangeDiffsAsync(AppConfigModel config);
    public static Task DiscardChangesAsync(AppConfigModel config);
    public static Task<bool> ReEnableModAsync(AppConfigModel config, string modFileName);
    public static Task<bool> RemoveNewModAsync(AppConfigModel config, string modFileName);
    public static Task<bool> ReplaceModWithNewAsync(
        AppConfigModel config,
        string removedModFileName,
        string replacementModFileName);
    public static Task<(List<string> AddedMods, List<string> RemovedMods)> CalculateReferenceChangesAsync(AppConfigModel config);
}
```

### `LoadOrderKeeper.Services.DiffService`

```csharp
public static class DiffService
{
    public static Task<IReadOnlyList<DiffLineModel>> GetPluginsDiffAsync(AppConfigModel config);
}
```

### `LoadOrderKeeper.Services.FileOperationsService`

```csharp
public class FileOperationsService
{
    public void OpenPluginsFile(AppConfigModel config);
    public void OpenReferenceFile(AppConfigModel config);
    public void OpenAppDataFolder(AppConfigModel config);
    public void OpenGameFolder(AppConfigModel config);
    public void OpenConfigFolder();
}
```

**Purpose:** Handles file and folder opening operations with shell integration. Centralizes Process.Start logic for launching files/folders.

---

## Profile Services

### `LoadOrderKeeper.Services.ProfileService`

```csharp
public static class ProfileService
{
    public static Task<IReadOnlyList<ProfileModel>> LoadProfilesAsync(AppConfigModel config);
    public static Task<ProfileModel> GetActiveProfileAsync(AppConfigModel config);
    public static Task<ProfileModel> CreateProfileAsync(AppConfigModel config, string label, string description);
    public static Task UpdateProfileAsync(AppConfigModel config, ProfileModel oldProfile, string newLabel, string newDescription);
    public static Task DeleteProfileAsync(AppConfigModel config, string profileId);
    public static Task<ProfileModel> CopyProfileAsync(AppConfigModel config, string sourceProfileId, string newLabel);
    public static Task SwitchProfileAsync(AppConfigModel config, string targetProfileId);
    public static string GenerateProfileId(string label, ISet<string> existingIds);
    public static Task EnsureProfileMainFileAsync(AppConfigModel config, string profileId);
    public static Task EnsureProfileReferenceFileAsync(AppConfigModel config, string profileId);
    public static Task EnsureDefaultProfileFilesAsync(AppConfigModel config);
    public static void EnsureProfilesFolderExists(AppConfigModel config);
    public static string GetProfilesFolder(AppConfigModel config);
    public static string GetProfileFolder(AppConfigModel config, string profileId);
    public static string GetProfileMainFilePath(AppConfigModel config, string profileId);
    public static string GetProfileReferenceFilePath(AppConfigModel config, string profileId);
}
```

---

## Reference Management Services

### `LoadOrderKeeper.Services.ReferenceHistoryService`

```csharp
public static class ReferenceHistoryService
{
    public static Task<IReadOnlyList<ReferenceVersionMetadataModel>> LoadVersionHistoryAsync(AppConfigModel config);
    public static Task<int> ArchiveCurrentReferenceAsync(
        AppConfigModel config,
        IReadOnlyList<string> addedMods,
        IReadOnlyList<string> removedMods);
    public static Task RollbackToVersionAsync(AppConfigModel config, int versionNumber);
    public static Task DeleteVersionAsync(AppConfigModel config, int versionNumber);
    public static Task ClearAllHistoryAsync(AppConfigModel config);
    public static Task UpdateVersionCommentAsync(AppConfigModel config, int versionNumber, string? newComment);
    public static string GetPendingChangesFilePath(AppConfigModel config);
    public static Task<PendingChangesModel> LoadPendingChangesAsync(AppConfigModel config);
    public static Task SavePendingChangesAsync(AppConfigModel config, PendingChangesModel pendingChanges);
    public static Task ClearPendingChangesAsync(AppConfigModel config);
    public static string GetHistoryFolder(AppConfigModel config);
    
    // Private implementation details
    private static Task PruneOldVersionsAsync(AppConfigModel config);
}
```

### `LoadOrderKeeper.Services.ReferenceManagementService`

```csharp
public class ReferenceManagementService
{
    public ReferenceManagementService(
        Action<string, StatusMessageType> addStatusMessage,
        Func<Task> refreshHistoryWindow);
    
    public Task<bool> CreateOrUpdateReferenceAsync(
        AppConfigModel config,
        bool refExists,
        Window? owner);
    
    public Task DiscardChangesAsync(AppConfigModel config);
    
    public Task<bool> HandleRollbackAsync(
        AppConfigModel config,
        ReferenceVersionMetadataModel version,
        Window parentWindow,
        Func<Task> onSuccess);
}
```

**Purpose:** Manages reference file operations including creation, updating with version history, discard workflow, and rollback handling. Coordinates comment input dialogs, pending changes, and archiving. Injected into MainViewModel with callbacks for status messages and window refresh.

---

## Window Management Services

### `LoadOrderKeeper.Services.WindowLifecycleService`

```csharp
public class WindowLifecycleService
{
    public void ShowManageProfilesWindow(
        AppConfigModel config,
        ConfigurationCoordinator configCoordinator,
        Window? owner = null,
        Action? onClosed = null);
    
    public Task ShowDiffWindowAsync(
        AppConfigModel config,
        MainViewModel mainViewModel,
        Window? owner = null);
    
    public void ShowReferenceHistoryWindow(
        AppConfigModel config,
        ConfigurationCoordinator configCoordinator,
        Window? owner = null,
        EventHandler<ReferenceVersionMetadataModel>? onRollbackRequested = null);
    
    public void ShowViewPendingChangesWindow(
        AppConfigModel config,
        ConfigurationCoordinator configCoordinator,
        Window? owner = null);
    
    public Task RefreshReferenceHistoryWindowAsync();
    public void CloseAllWindows();
    
    // Property checks
    public bool IsDiffWindowOpen { get; }
    public bool IsManageProfilesWindowOpen { get; }
    public bool IsReferenceHistoryWindowOpen { get; }
    public bool IsViewPendingChangesWindowOpen { get; }
}
```

**Purpose:** Manages non-modal window lifecycle including singleton tracking (prevents duplicates), window activation/focus, and cleanup. Replaces repetitive window management code in ViewModels.

---

## Initialization Services

### `LoadOrderKeeper.Services.ViewModelInitializer`

```csharp
public class ViewModelInitializer
{
    public ViewModelInitializer(
        Action<string, StatusMessageType> addStatusMessage,
        Func<string> getReadyStatusMessage,
        Action<AppConfigModel> updateCoordinators);
    
    public Task<InitializationResult> LoadInitialStateAsync(
        ConfigurationCoordinator configCoordinator,
        ProfileCoordinator profileCoordinator,
        FileMonitoringCoordinator fileMonitor,
        UpdateCheckCoordinator updateCheckCoordinator);
}

public class InitializationResult
{
    public AppConfigModel Config { get; }
    public bool RefExists { get; }
}
```

**Purpose:** Handles MainViewModel startup sequence including configuration loading, validation, profile setup, coordinator initialization, and initial state establishment. Does not automatically show settings dialog - relies on error banner to guide users when configuration is invalid. Extracted from MainViewModel to improve testability and maintainability.

---

## Version & Update Services

### `LoadOrderKeeper.Services.VersionService`

```csharp
public static class VersionService
{
    public static string GetApplicationVersion();
}
```

### `LoadOrderKeeper.Services.UpdateCheckService`

```csharp
public static class UpdateCheckService
{
    public static Task<UpdateCheckResult> CheckForUpdatesAsync(bool bypassCache = false);
    public static string GetNexusModsUrl();
    public static string GetGitHubReleasesUrl();
    
    // Private implementation details
    private static Task<GitHubRelease?> FetchLatestReleaseAsync();
    private static SemanticVersion? ParseVersion(string versionString);
    private static bool IsNewerVersion(SemanticVersion latest, SemanticVersion current);
    private static CacheInfo GetCacheInfo();
    private static void SaveToCache(UpdateCheckResult result);
    private static string GetCacheFilePath();
}
```

---

## Utility Services

### `LoadOrderKeeper.Services.DateTimeFormattingService`

```csharp
public static class DateTimeFormattingService
{
    public static string FormatFriendly(DateTime dateTime);
    public static string FormatTimestamp(DateTime dateTime);
    public static string FormatIso(DateTime dateTime);
}
```

### `LoadOrderKeeper.Services.ErrorLoggingService`

```csharp
public static class ErrorLoggingService
{
    public static string GetErrorLogPath();
    public static void InitializeErrorLog();
    public static Task<bool> LogExceptionAsync(
        Exception exception, 
        AppConfigModel? config, 
        IReadOnlyList<DiffLineModel>? changeList);
    
    // Private implementation details
    private static string SanitizeText(string text);
}
```

### `LoadOrderKeeper.Services.DebugStateService`

```csharp
public static class DebugStateService
{
    public static Task<string> CaptureDebugStateAsync(
        AppConfigModel config, 
        IReadOnlyList<DiffLineModel> changeList,
        StatusCoordinator? statusCoordinator = null);
    
    // Private implementation details
    private static string SanitizePath(string path);
    private static Task<List<string>> ReadFileContentsAsync(string filePath);
}
```

**Purpose:** Captures complete application state snapshot for troubleshooting and debugging. Serializes data to prettified JSON format with user privacy protection.

**Debug State Contents:**
- **ApplicationVersion**: Current semantic version
- **Configuration**: App data path, game path, active profile ID (paths sanitized)
- **Steam**: Installation and running status
- **TotalChangesDetected**: Count of detected differences
- **PluginsTxtContents**: Complete Plugins.txt file as line array
- **ReferenceContents**: Complete reference file as line array
- **ChangeList**: Full diff with all detected modifications
- **StatusMessages**: Complete internal log of all status messages from session start (via `StatusCoordinator.GetAllMessages()`)

**Privacy & Security:**
- All file paths sanitized automatically (`%USERPROFILE%` placeholder replaces user profile paths)
- User-specific information removed before export
- Safe for sharing with developers for troubleshooting

**Access Points:**
- Debug menu "Copy Debug State" command (main window and diff window)
- Global exception handling (included in error.log)

**JSON Formatting:**
- Prettified with indentation for readability
- Uses `System.Text.Json` with `WriteIndented = true`
- Ignores reference cycles for safety

---

## Localization Services

### `LoadOrderKeeper.ViewTexts.LocalizationService`

```csharp
public sealed class LocalizationService : ObservableObject
{
    // Singleton access
    public static LocalizationService Instance { get; }
    
    // Properties
    public string CurrentCulture { get; }
    
    // Events
    public event EventHandler? CultureChanged;
    
    // String retrieval
    public string GetString(string section, string key);
    public string GetString(string section, string key, params object[] args);
    
    // Culture management
    public void SetCulture(string cultureName);
    public void InitializeFromConfig(string preferredLanguage);
    public void ReloadCurrentCulture();
    
    // Culture discovery (zero-hardcoding architecture)
    public IReadOnlyList<string> GetAvailableCultures();
    public string GetLocaleName(string cultureName);
    
    // Testing/maintenance
    internal void ClearCache();
    
    // Private implementation
    private LocalizationService();
    private string DetectSystemCulture();
    private void LoadCulture(string cultureName);
    private Dictionary<string, string> BuildParentCultureMap();
}
```

**Purpose:** Singleton service managing JSON-based localization with **zero-hardcoding architecture**. Provides thread-safe string retrieval with format support, runtime culture switching, automatic system locale detection, dynamic locale discovery, and fallback to English for unsupported cultures.

**Key Features:**
- **Zero-Hardcoding Design**: New languages require only JSON file, no code changes
- **Dynamic Discovery**: Scans file system for available locales automatically
- **Metadata from JSON**: Reads `LocaleName` and `ParentCulture` from locale files
- Thread-safe operations (all methods protected by lock)
- Automatic culture detection via `CultureInfo.CurrentUICulture`
- Dynamic parent culture mapping (e.g., `fr-CA` ? `fr-FR`)
- Format string support with safe fallback
- Event-driven UI updates via `CultureChanged` event
- Caches translations in memory for performance

**Supported Cultures:**
- `en-US` (English - default fallback)
- `de-DE` (German - Deutsch)
- `fr-FR` (French - Français)
- `es-ES` (Spanish - Español)
- `it-IT` (Italian - Italiano)

**Adding New Languages:**
To add a new language (e.g., Portuguese):
1. Create `pt-BR.json` with required metadata at root level:
   ```json
   {
     "LocaleName": "Português (Brasil)",
     "ParentCulture": "pt",
     "MainWindow": { ... }
   }
   ```
2. No code changes needed - language appears in dropdown automatically

**JSON File Structure:**
- **Root-level metadata** (strings, skipped by LoadCulture):
  - `LocaleName`: Native language name for dropdown display
  - `ParentCulture`: Two-letter ISO 639-1 code for auto-detection
- **Translation sections** (objects, processed by LoadCulture):
  - `MainWindow`, `Menu`, `Settings`, `ErrorDialog`, etc.

**JSON File Location:** `ViewTexts/Locales/{culture}.json`

**Runtime Path Resolution:** `AppDomain.CurrentDomain.BaseDirectory + "ViewTexts/Locales"`

**Public Methods:**
`GetAvailableCultures()`:
- Scans `Locales` folder for all `*.json` files
- Returns culture codes (e.g., `["de-DE", "en-US", "es-ES", "fr-FR", "it-IT"]`)
- Sorted alphabetically
- Used by `SettingsViewModel` to populate language dropdown

`GetLocaleName(string cultureName)`:
- Reads `LocaleName` property from specified locale file
- Returns native language name (e.g., "Deutsch", "Français")
- Falls back to culture code if file not found or property missing
- Used by `SettingsViewModel.BuildLanguageList()` for dropdown display

`InitializeFromConfig(string preferredLanguage)`:
- Called by `ViewModelInitializer` on application startup
- Applies user's saved language preference from `config.json`
- Value `"auto"` triggers automatic system locale detection
- Specific culture codes (e.g., `"de-DE"`) override auto-detection

**Private Methods:**
`DetectSystemCulture()`:
- Reads `CultureInfo.CurrentUICulture` from system
- Checks for exact culture match (e.g., `fr-FR`)
- Falls back to parent culture mapping if exact match not found
- Uses `BuildParentCultureMap()` for dynamic parent lookup
- Returns `"en-US"` if no supported culture found

`BuildParentCultureMap()`:
- Scans all locale files in `Locales` folder
- Reads `ParentCulture` property from each file
- Builds dictionary mapping parent codes to specific cultures
- Example: `{"en": "en-US", "de": "de-DE", "fr": "fr-FR", ...}`
- Enables automatic detection without hardcoded mappings

`LoadCulture(string cultureName)`:
- Loads translations from JSON file for specified culture
- Skips root-level properties that aren't objects (LocaleName, ParentCulture)
- Processes only translation sections (MainWindow, Settings, etc.)
- Falls back to `en-US.json` if specified culture file not found
- Merges translations into cache for fallback support

### `LoadOrderKeeper.Tools.LocalizationJsonNormalizer`

```csharp
public static class LocalizationJsonNormalizer
{
    public static void NormalizeFile(string filePath);
    public static void NormalizeAllLocales(string localesPath);
    public static bool ValidateFile(string filePath);
}
```

**Purpose:** Utility for normalizing JSON localization files. Reads, deserializes, and re-serializes JSON with proper Unicode encoding and consistent formatting.

**Key Features:**
- Uses `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` for readable output
- Ensures consistent UTF-8 encoding
- Validates JSON structure
- Safe for accented characters (é, è, à, ê, etc.)
- Preserves root-level metadata (LocaleName, ParentCulture)

**Console Tool:** `Tools/JsonNormalizer/Program.cs` - Standalone console app for batch normalization

**Usage:** Run after editing any JSON localization files, before committing to version control

---

[<< Back to Index](README.md)
