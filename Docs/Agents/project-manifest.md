# Project Manifest – Starfield Load Order Keeper

> Source-of-truth overview for future AI agents. Do not infer behavior beyond what is documented here.

---

## 1. Tech Stack & Patterns

- **Runtime / Platform**
  - .NET 9
  - WPF desktop application

- **Libraries / Frameworks**
  - `CommunityToolkit.Mvvm`
    - `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`, `RelayCommand`, `AsyncRelayCommand`, `IRelayCommand`, `IAsyncRelayCommand`
  - `MaterialDesignThemes` / `MaterialDesignColors` for dialogs, icons, and card layouts (v5)
  - `Gameloop.Vdf` for parsing Steam library configuration files (Valve Data Format)
  - Standard .NET `System.*` APIs for I/O, processes, collections, and JSON serialization

- **Architectural Patterns**
  - **MVVM**
    - ViewModels: `MainViewModel`, `SettingsViewModel`, `DiffDialogViewModel`, `SwitchProfileViewModel`, `ManageProfilesViewModel`, `ProfilePropertiesViewModel`, `ConfirmationDialogViewModel`, `AboutViewModel`, `UpdateOptionsViewModel`, `ReferenceHistoryViewModel`, `CommentInputViewModel`
    - Views: `MainWindow`, `SettingsWindow`, `DiffWindow`, `SwitchProfileWindow`, `ManageProfilesWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`, `UpdateOptionsDialog`, `ReferenceHistoryWindow`, `CommentInputDialog`
  - **Coordinator Pattern**
    - Coordinators handle specific domain logic and state management
    - All coordinators inherit from `CoordinatorBase` (provides `INotifyPropertyChanged` + `IDisposable`)
    - Event-driven communication between coordinators and ViewModels
    - Coordinators:
      - `FileMonitoringCoordinator`: Periodic file monitoring, change detection, Steam process detection, sorting recommendations
      - `StatusCoordinator`: Status message management and history tracking
      - `UpdateCheckCoordinator`: Background and manual update checking with caching
      - `ProfileCoordinator`: Active profile state management and switching
      - `ConfigurationCoordinator`: Configuration validation with caching and detailed error reporting
      - `GameLauncherCoordinator`: SFSE detection and game launching
      - `WindowManager`: Window lifecycle management and duplicate prevention
  - **Static Services**
    - `SettingsService`: configuration persistence and default path discovery (includes Steam library detection)
    - `FileService`: plugins/reference file operations plus diff helpers
    - `DiffService`: diff line construction for the UI
    - `ProfileService`: profile discovery, CRUD, switching, and file scaffolding
    - `VersionService`: centralized application version retrieval
    - `UpdateCheckService`: GitHub API integration for version checking with caching
    - `ReferenceHistoryService`: version history management, archiving, rollback, and pending changes tracking
    - `DateTimeFormattingService`: user-friendly date/time formatting utilities
  - **Modal Navigation**
    - `MainWindow` as shell
    - Secondary windows opened modally: `SettingsWindow`, `SwitchProfileWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`, `UpdateOptionsDialog`, `CommentInputDialog`
  - **Non-Modal Windows**
    - `DiffWindow`, `ManageProfilesWindow`, `ReferenceHistoryWindow` allow main window interaction while open; tracked by `WindowManager` coordinator to prevent duplicates
  - **File Monitoring**
    - `FileMonitoringCoordinator` uses fixed 3-second interval (optimized through testing)
    - Monitoring paused when configuration invalid to prevent unnecessary I/O operations
    - Detects Steam process running and shows warning banner when detected
  - **Steam Process Detection**
    - `FileMonitoringCoordinator` detects Steam process (steam.exe) running
    - Shows persistent warning banner when Steam is running
    - Provides tooltip explaining why Steam should be closed before making changes
    - Detection runs on same 3-second timer as file monitoring
    - Warning automatically dismissed when Steam closes
  - **Profile Management**
    - Profiles stored per active configuration under `Profiles/{profileId}` with `main.txt`, `reference.txt`, `profile.json`, `pending-changes.json`, and `History/` folder
    - `ProfileCoordinator` manages active profile state and switching
    - Commands and dialogs coordinate through `ProfileService` to switch and manage profiles
  - **Reference History**
    - Each profile maintains independent version history in `Profiles/{profileId}/History/`
    - Automatic versioning with pending changes system tracks modifications between updates
    - Maximum 16 versions per profile with automatic pruning of oldest versions
    - Rollback support replaces `Plugins.txt` with archived reference for review in diff window
    - User comments and change tracking (added/removed mods) stored in JSON metadata
    - On-demand migration creates initial version for existing installations transparently
  - **Confirmation Dialogs**
    - Custom Material Design styled `ConfirmationDialog` replaces all `MessageBox.Show` calls
    - Supports multiple icon types (Information, Question, Warning, Error) and button configurations (OK, OKCancel, YesNo, YesNoCancel)
  - **Update Notifications**
    - `UpdateCheckCoordinator` manages update checking and notification state
    - Non-intrusive info bar in `MainWindow` shows when updates available
    - Automatic background check on startup with 24-hour caching
    - Manual check via Help menu bypasses cache
    - `UpdateOptionsDialog` provides clickable download buttons for Nexusmods and GitHub
  - **Configuration Validation**
    - `ConfigurationCoordinator` manages validation state with caching to prevent excessive I/O
    - Error banner in `MainWindow` displays when paths are invalid with "Open settings" button
    - Status banner in `SettingsWindow` shows real-time validation feedback (error/success states)
    - Validation triggers: timer tick, config changes, settings save, auto-detected path clicks
    - Secondary windows append guidance message to errors when config invalid
    - Centralized error messages in `Constants/UserMessages.cs` for maintainability
  - **Steam Library Detection**
    - `SettingsService` parses Steam's `libraryfolders.vdf` to locate Starfield across all Steam library folders
    - Detects Steam installation via Windows registry, searches all configured libraries for Starfield (AppID: 1716740)
    - Validates installations by checking for `Data` folder presence
    - Silent failure with multi-level fallbacks ensures robust path detection

---

## 2. File Tree (Logical Overview)

```text
.
├─ Starfield Load Order Keeper.csproj
├─ App.xaml
├─ App.xaml.cs
├─ AssemblyInfo.cs
├─ MainWindow.xaml
├─ MainWindow.xaml.cs
├─ Constants/
│  └─ UserMessages.cs
├─ Coordinators/
│  ├─ ICoordinator.cs
│  ├─ CoordinatorBase.cs
│  ├─ FileMonitoringCoordinator.cs
│  ├─ StatusCoordinator.cs
│  ├─ UpdateCheckCoordinator.cs
│  ├─ ProfileCoordinator.cs
│  ├─ ConfigurationCoordinator.cs
│  ├─ GameLauncherCoordinator.cs
│  ├─ WindowManager.cs
│  └─ Events/
│     ├─ CoordinatorEventArgs.cs
│     ├─ ChangeDetectedEventArgs.cs
│     ├─ SortingRecommendationChangedEventArgs.cs
│     ├─ SteamWarningChangedEventArgs.cs
│     ├─ ProfileChangedEventArgs.cs
│     └─ ConfigValidationChangedEventArgs.cs
├─ Models/
│  ├─ AppConfigModel.cs
│  ├─ DiffLineModel.cs
│  ├─ ModDiffModel.cs
│  ├─ ModEntryModel.cs
│  ├─ PendingChangesModel.cs
│  ├─ PluginsComparisonResult.cs
│  ├─ ProfileModel.cs
│  ├─ ReferenceVersionMetadataModel.cs
│  ├─ StatusMessageModel.cs
│  └─ UpdateCheckResult.cs
├─ Services/
│  ├─ DateTimeFormattingService.cs
│  ├─ DiffService.cs
│  ├─ FileService.cs
│  ├─ ProfileService.cs
│  ├─ ReferenceHistoryService.cs
│  ├─ SettingsService.cs
│  ├─ UpdateCheckService.cs
│  └─ VersionService.cs
├─ ViewModels/
│  ├─ AboutViewModel.cs
│  ├─ CommentInputViewModel.cs
│  ├─ ConfirmationDialogViewModel.cs
│  ├─ DiffDialogViewModel.cs
│  ├─ MainViewModel.cs
│  ├─ ManageProfilesViewModel.cs
│  ├─ ProfilePropertiesViewModel.cs
│  ├─ ReferenceHistoryViewModel.cs
│  ├─ SettingsViewModel.cs
│  ├─ SwitchProfileViewModel.cs
│  └─ UpdateOptionsViewModel.cs
├─ Views/
│  ├─ AboutWindow.xaml
│  ├─ AboutWindow.xaml.cs
│  ├─ CommentInputDialog.xaml
│  ├─ CommentInputDialog.xaml.cs
│  ├─ ConfirmationDialog.xaml
│  ├─ ConfirmationDialog.xaml.cs
│  ├─ DiffWindow.xaml
│  ├─ DiffWindow.xaml.cs
│  ├─ ManageProfilesWindow.xaml
│  ├─ ManageProfilesWindow.xaml.cs
│  ├─ ProfilePropertiesWindow.xaml
│  ├─ ProfilePropertiesWindow.xaml.cs
│  ├─ ReferenceHistoryWindow.xaml
│  ├─ ReferenceHistoryWindow.xaml.cs
│  ├─ SettingsWindow.xaml
│  ├─ SettingsWindow.xaml.cs
│  ├─ SwitchProfileWindow.xaml
│  ├─ SwitchProfileWindow.xaml.cs
│  ├─ UpdateOptionsDialog.xaml
│  └─ UpdateOptionsDialog.xaml.cs
├─ Converters/
│  ├─ ActiveProfileVisibilityConverter.cs
│  ├─ ChangeSummaryConverter.cs
│  ├─ CountToVisibilityConverter.cs
│  ├─ InverseBooleanToVisibilityConverter.cs
│  ├─ InverseCountToVisibilityConverter.cs
│  └─ ReplacementCommandParameterConverter.cs
├─ Styles/
│  ├─ ButtonStyles.xaml
│  ├─ DataGridStyles.xaml
│  ├─ TextStyles.xaml
│  └─ WindowStyles.xaml
├─ Docs/
│  ├─ project-manifest.md (this file)
│  ├─ Agents/
│  │  ├─ application-description.md
│  │  ├─ impl-mvvm-architecture-overview.md
│  │  ├─ impl-diff-detection-overview.md
│  │  ├─ impl-periodic-change-checking.md
│  │  ├─ implementation-guidelines.md
│  │  ├─ example-plugins.txt
│  │  └─ Development History/
│  │     ├─ 01-initial-agent-plan.md
│  │     ├─ 02-add-content-diff.md
│  │     ├─ 03-numbered-mod-order.md
│  │     ├─ 04-enabled-disabled-status-awareness.md
│  │     ├─ 05-problem-resolution-controls.md
│  │     ├─ 06-profiles-feature.md
│  │     ├─ 07-group-dependent-mod-changes.md
│  │     ├─ 13-steam-guard.md
│  │     ├─ 14-refactor-file-monitoring-coordinator.md
│  │     ├─ 15-window-manager-coordinator.md
│  │     ├─ 16-status-coordinator.md
│  │     ├─ 17-update-check-coordinator.md
│  │     ├─ 18-profile-coordinator.md
│  │     ├─ 19-configuration-coordinator.md
│  │     ├─ 20-game-launcher-coordinator.md
│  │     └─ coordinator-refactoring-complete-summary.md
├─ Tests/
│  └─ LoadOrderKeeper.Tests/
│     ├─ LoadOrderKeeper.Tests.csproj
│     ├─ DiffServiceTests.cs
│     ├─ FileServiceTests.cs
│     ├─ ProfileServiceTests.cs
│     ├─ SettingsServiceTests.cs
│     └─ TestConfigContext.cs
```

---

## 3. Public API (Signatures Only)

### 3.1 Coordinators

#### `LoadOrderKeeper.Coordinators.ICoordinator`

```csharp
public interface ICoordinator : IDisposable
{
    void Initialize();
}
```

#### `LoadOrderKeeper.Coordinators.CoordinatorBase`

```csharp
public abstract class CoordinatorBase : ObservableObject, ICoordinator
{
    public virtual void Initialize();
    public void Dispose();
    protected virtual void Dispose(bool disposing);
    protected virtual void OnDisposing();
    protected void ThrowIfDisposed();
}
```

#### `LoadOrderKeeper.Coordinators.FileMonitoringCoordinator`

```csharp
public sealed class FileMonitoringCoordinator : CoordinatorBase
{
    // Properties
    public bool PluginsFileChangedExternally { get; }
    public int ChangeCount { get; }
    public string SortingRecommendationMessage { get; }
    public bool SortingRecommendationActive { get; }
    public bool ShowSteamWarning { get; }
    public string SteamWarningTooltip { get; }
    public bool IsSteamInstalled { get; }
    public bool IsSteamRunning { get; }
    
    // Events
    public event EventHandler<ChangeDetectedEventArgs>? ChangeDetected;
    public event EventHandler<SortingRecommendationChangedEventArgs>? SortingRecommendationChanged;
    public event EventHandler<SteamWarningChangedEventArgs>? SteamWarningChanged;
    
    // Methods
    public void UpdateState(AppConfigModel config, bool refExists, bool isBusy, bool configIsInvalid);
    public Task CheckPluginsFileAsync();
}
```

#### `LoadOrderKeeper.Coordinators.StatusCoordinator`

```csharp
public sealed class StatusCoordinator : CoordinatorBase
{
    // Properties
    public string StatusMessage { get; }
    public ObservableCollection<StatusMessageModel> StatusMessageHistory { get; }
    
    // Methods
    public void AddStatusMessage(string message, StatusMessageType type = StatusMessageType.Info);
    public string GetReadyStatusMessage(bool configIsValid);
}
```

#### `LoadOrderKeeper.Coordinators.UpdateCheckCoordinator`

```csharp
public sealed class UpdateCheckCoordinator : CoordinatorBase
{
    // Properties
    public bool UpdateAvailable { get; }
    public string UpdateMessage { get; }
    public bool UpdateInfoBarVisible { get; }
    
    // Methods
    public Task<UpdateCheckResult> CheckForUpdatesBackgroundAsync();
    public Task<UpdateCheckResult> CheckForUpdatesManualAsync();
    public void DismissUpdateNotification();
    public string? GetLatestVersion();
}
```

#### `LoadOrderKeeper.Coordinators.ProfileCoordinator`

```csharp
public sealed class ProfileCoordinator : CoordinatorBase
{
    // Properties
    public ProfileModel ActiveProfile { get; }
    public string ActiveProfileLabel { get; }
    
    // Events
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
    
    // Methods
    public void UpdateConfiguration(AppConfigModel? config);
    public Task RefreshActiveProfileAsync();
    public Task<bool> SwitchProfileAsync(string targetProfileId);
    public bool IsActiveProfile(string profileId);
}
```

#### `LoadOrderKeeper.Coordinators.ConfigurationCoordinator`

```csharp
public sealed class ConfigurationCoordinator : CoordinatorBase
{
    // Properties
    public bool IsConfigValid { get; }
    public bool ShowErrorBanner { get; }
    
    // Events
    public event EventHandler<ConfigValidationChangedEventArgs>? ValidationChanged;
    
    // Methods
    public void UpdateConfiguration(AppConfigModel? config);
    public void ValidateConfiguration();
    public ValidationResult GetValidationResult();
}

public sealed class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    
    public static ValidationResult Success();
    public static ValidationResult Failed(string errorMessage);
}
```

#### `LoadOrderKeeper.Coordinators.GameLauncherCoordinator`

```csharp
public sealed class GameLauncherCoordinator : CoordinatorBase
{
    // Properties
    public string PlayButtonText { get; }
    public bool HasSfseInstalled { get; }
    
    // Methods
    public void UpdateGamePath(string? gamePath);
    public void UpdateConfiguration(AppConfigModel? config);
    public bool LaunchGame();
    public string? GetExecutablePath();
}
```

#### `LoadOrderKeeper.Coordinators.WindowManager`

```csharp
public sealed class WindowManager : CoordinatorBase
{
    // Methods (examples - full API in WindowManager documentation)
    public bool IsWindowOpen<T>() where T : Window;
    public void RegisterWindow<T>(T window) where T : Window;
    public void UnregisterWindow<T>() where T : Window;
    public void BringToFront<T>() where T : Window;
}
```

### 3.2 Coordinator Events

#### `LoadOrderKeeper.Coordinators.Events.ChangeDetectedEventArgs`

```csharp
public sealed class ChangeDetectedEventArgs : EventArgs
{
    public bool HasChanges { get; }
    public int ChangeCount { get; }
}
```

#### `LoadOrderKeeper.Coordinators.Events.SteamWarningChangedEventArgs`

```csharp
public sealed class SteamWarningChangedEventArgs : EventArgs
{
    public bool ShowWarning { get; }
    public string Tooltip { get; }
}
```

#### `LoadOrderKeeper.Coordinators.Events.SortingRecommendationChangedEventArgs`

```csharp
public sealed class SortingRecommendationChangedEventArgs : EventArgs
{
    public bool RecommendSorting { get; }
    public string Message { get; }
}
```

#### `LoadOrderKeeper.Coordinators.Events.ProfileChangedEventArgs`

```csharp
public sealed class ProfileChangedEventArgs : EventArgs
{
    public ProfileModel OldProfile { get; }
    public ProfileModel NewProfile { get; }
}
```

#### `LoadOrderKeeper.Coordinators.Events.ConfigValidationChangedEventArgs`

```csharp
public sealed class ConfigValidationChangedEventArgs : EventArgs
{
    public bool WasValid { get; }
    public bool IsValid { get; }
    public bool StateChanged { get; }
}
```

### 3.3 Models

#### `LoadOrderKeeper.Constants.UserMessages`

```csharp
public static class UserMessages
{
    public const string ConfigInvalidGuidance = 
        "\n\nThe likely cause is that the current configuration is invalid. Please refer to the error message in the main window to fix this.";
    
    public const string ProfilesFolderRequired = 
        "The application requires a 'Profiles' folder in your configured app data path to store profile data. " +
        "This folder could not be created or accessed. Please check folder permissions or select a different app data path in settings.";
    
    public const string ProfilesFolderAccessDenied = 
        "Access denied when creating the Profiles folder. You may need administrator rights or to choose a different location.";
    
    public const string PluginsTxtRequired = 
        "The Plugins.txt file was not found in the configured app data path. " +
        "This file is required for the application to function. " +
        "Please ensure you have run Starfield at least once to generate this file, or select the correct app data folder in settings.";
}
```

#### `LoadOrderKeeper.Models.AppConfigModel`

```csharp
public class AppConfigModel
{
    public string StarfieldAppDataPath { get; set; }
    public string StarfieldGamePath { get; set; }
    public string? ActiveProfileId { get; set; }

    public bool IsValid();
    public string GetPluginsFilePath();
    public string GetReferenceFilePath();
}
```

#### `LoadOrderKeeper.Models.DiffChangeType`

```csharp
public enum DiffChangeType
{
    Unchanged,
    Added,
    Removed,
    Moved,
    Replaced,
    Inserted
}
```

#### `LoadOrderKeeper.Models.DiffLineModel`

```csharp
public sealed class DiffLineModel
{
    public DiffLineModel(
        string fileName,
        string text,
        DiffChangeType changeType,
        int? referenceNumber = null,
        int? currentNumber = null,
        string? replacementFileName = null);

    public string FileName { get; }
    public string Text { get; }
    public DiffChangeType ChangeType { get; }
    public int? ReferenceNumber { get; }
    public int? CurrentNumber { get; }
    public string? ReplacementFileName { get; }
    public List<DiffLineModel> DependentChanges { get; }
    public bool HasDependentChanges { get; }
    public string DependentChangesSummary { get; }
    public bool IsDependentChangesExpanded { get; set; }
    public string Prefix { get; }
}
```

#### `LoadOrderKeeper.Models.ModEntryModel`

```csharp
public sealed class ModEntryModel : IEquatable<ModEntryModel>
{
    public string FileName { get; }
    public bool IsEnabled { get; }
    public int? LineNumber { get; set; }
    public int? OriginalLineNumber { get; set; }

    public ModEntryModel(string line, int? lineNumber = null, int? originalLineNumber = null);

    public string ToLine();
    public override string ToString();
    public bool Equals(ModEntryModel? other);
    public override bool Equals(object? obj);
    public override int GetHashCode();
}
```

#### `LoadOrderKeeper.Models.ModDiffModel`

```csharp
public sealed class ModDiffModel
{
    public string FileName { get; init; }
    public int? ReferenceNumber { get; init; }
    public int? CurrentNumber { get; init; }

    public bool IsNew { get; }
    public bool IsRemoved { get; }
    public bool IsMoved { get; }
}
```

#### `LoadOrderKeeper.Models.PluginsComparisonResult`

```csharp
public readonly record struct PluginsComparisonResult(bool HasDifferences, string PluginsSignature);
```

#### `LoadOrderKeeper.Models.ProfileModel`

```csharp
public sealed class ProfileModel
{
    public string Id { get; init; }
    public string Label { get; set; }
    public string Description { get; set; }
    public bool IsDefault { get; }

    public ProfileModel();
    public ProfileModel(string id, string label, string description = "");

    public static ProfileModel CreateDefault();
}
```

#### `LoadOrderKeeper.Models.ReferenceVersionMetadataModel`

```csharp
public sealed class ReferenceVersionMetadataModel
{
    public int VersionNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Comment { get; set; }
    public List<string> RemovedMods { get; set; }
    public List<string> AddedMods { get; set; }
    public int TotalModsChanged { get; }
    public string FormattedTimestamp { get; }

    public string GetChangeSummary();
}
```

#### `LoadOrderKeeper.Models.StatusMessageModel`

```csharp
public sealed class StatusMessageModel
{
    public StatusMessageModel(string message, DateTime timestamp, StatusMessageType type = StatusMessageType.Info);

    public string Message { get; }
    public DateTime Timestamp { get; }
    public StatusMessageType Type { get; }
    public string FormattedTimestamp { get; }
    public string DisplayText { get; }
}

public enum StatusMessageType
{
    Info,
    Success,
    Warning,
    Error
}
```

#### `LoadOrderKeeper.Models.UpdateCheckResult`

```csharp
public sealed record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? DownloadUrl);
```

#### `LoadOrderKeeper.Models.PendingChangesModel`

```csharp
public sealed class PendingChangesModel
{
    public List<string> AddedMods { get; set; }
    public List<string> RemovedMods { get; set; }
    public bool IsEmpty { get; }
    public int TotalChanges { get; }

    public static PendingChangesModel CreateEmpty();
    public static PendingChangesModel Create(IReadOnlyList<string> addedMods, IReadOnlyList<string> removedMods);
}
```

### 3.2 Services

#### `LoadOrderKeeper.Services.SettingsService`

```csharp
public static class SettingsService
{
    public static Task<AppConfigModel> LoadSettingsAsync();
    public static Task SaveSettingsAsync(AppConfigModel config);
    public static string TryGetDefaultSteamPath();
    public static string TryGetDefaultAppDataPath();
    
    // Private methods for Steam detection
    private static string? TryGetSteamInstallPath();
    private static string? TryFindStarfieldInSteamLibraries(string steamInstallPath);
    private static string? TryGetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName);
    private static string NormalizePath(string path);
}
```

#### `LoadOrderKeeper.Services.FileService`

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
}
```

#### `LoadOrderKeeper.Services.ProfileService`

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

#### `LoadOrderKeeper.Services.DiffService`

```csharp
public static class DiffService
{
    public static Task<IReadOnlyList<DiffLineModel>> GetPluginsDiffAsync(AppConfigModel config);
}
```

#### `LoadOrderKeeper.Services.VersionService`

```csharp
public static class VersionService
{
    public static string GetApplicationVersion();
}
```

#### `LoadOrderKeeper.Services.UpdateCheckService`

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

#### `LoadOrderKeeper.Services.ReferenceHistoryService`

```csharp
public static class ReferenceHistoryService
{
    public static Task<IReadOnlyList<ReferenceVersionMetadataModel>> LoadVersionHistoryAsync(AppConfigModel config);
    public static Task<int> ArchiveCurrentReferenceAsync(
        AppConfigModel config,
        string? comment,
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

#### `LoadOrderKeeper.Services.DateTimeFormattingService`

```csharp
public static class DateTimeFormattingService
{
    public static string FormatFriendly(DateTime dateTime);
    public static string FormatTimestamp(DateTime dateTime);
    public static string FormatIso(DateTime dateTime);
}
```

### 3.3 ViewModels

> Commands emitted by `[RelayCommand]` follow the `{MethodName}Command` naming pattern and expose `IRelayCommand` / `IAsyncRelayCommand` properties automatically.

#### `LoadOrderKeeper.ViewModels.SettingsViewModel`

```csharp
public partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel(AppConfigModel initialConfig);

    public string StarfieldAppDataPath { get; set; }
    public string StarfieldGamePath { get; set; }
    public bool StatusBannerVisible { get; set; }
    public string StatusBannerMessage { get; set; }
    public bool StatusBannerIsError { get; set; }
    public string DetectedAppDataPath { get; }
    public string DetectedGamePath { get; }
    public bool HasDetectedAppDataPath { get; }
    public bool HasDetectedGamePath { get; }

    public event EventHandler? BrowseAppDataRequested;
    public event EventHandler? BrowseGamePathRequested;
    public event EventHandler? SaveRequested;

    public void UpdateAppDataPath(string selectedPath);
    public void UpdateGamePath(string selectedPath);
    public void ValidateConfiguration();
    public AppConfigModel GetConfig();
}
```

#### `LoadOrderKeeper.ViewModels.MainViewModel`

```csharp
public partial class MainViewModel : ObservableObject
{
    public MainViewModel();

    public AppConfigModel Config { get; set; }
    public bool RefExists { get; set; }
    public string StatusMessage { get; set; }
    public ObservableCollection<StatusMessageModel> StatusMessageHistory { get; set; }
    public bool IsBusy { get; set; }
    public string ReferenceButtonText { get; set; }
    public string FixLoadOrderButtonText { get; set; }
    public string PlayGameButtonText { get; }
    public string WindowTitle { get; }
    public string FileMenuHeader { get; }
    public string OpenPluginsMenuText { get; }
    public string OpenReferenceMenuText { get; }
    public string OpenAppDataFolderMenuText { get; }
    public string OpenGameFolderMenuText { get; }
    public string ExitMenuText { get; }
    public string EditMenuHeader { get; }
    public string SettingsMenuText { get; }
    public string HelpMenuHeader { get; }
    public string CheckForUpdatesMenuText { get; }
    public string AboutMenuText { get; }
    public string DownloadOptionsButtonText { get; }
    public string CurrentTargetLabel { get; }
    public string TargetPrefixText { get; }
    public string PluginsModifiedWarningText { get; }
    public string ActiveProfilePrefixText { get; }
    public string ProfileMenuHeader { get; }
    public string SwitchProfileMenuText { get; }
    public string ManageProfilesMenuText { get; }
    public string RecentStatusMessagesText { get; }
    public string ShowChangesButtonText { get; set; }
    public bool PluginsFileChangedExternally { get; set; }
    public string SortingRecommendationMessage { get; set; }
    public bool SortingRecommendationActive { get; set; }
    public string ActiveProfileLabel { get; set; }
    public bool UpdateAvailable { get; set; }
    public string UpdateMessage { get; set; }
    public bool UpdateInfoBarVisible { get; set; }
    public bool ConfigErrorBannerVisible { get; set; }

    public IRelayCommand OpenPluginsFileCommand { get; }
    public IRelayCommand OpenReferenceFileCommand { get; }
    public IRelayCommand OpenAppDataFolderCommand { get; }
    public IRelayCommand OpenGameFolderCommand { get; }
    public IRelayCommand PlayGameCommand { get; }
    public IAsyncRelayCommand ShowDiffCommand { get; }
    public IAsyncRelayCommand FixLoadOrderCommand { get; }
    public IAsyncRelayCommand CreateReferenceCommand { get; }
    public IAsyncRelayCommand DiscardChangesCommand { get; }
    public IAsyncRelayCommand SwitchProfileCommand { get; }
    public IAsyncRelayCommand ManageProfilesCommand { get; }
    public IAsyncRelayCommand OpenSettingsCommand { get; }
    public IAsyncRelayCommand OpenSettingsFromErrorBannerCommand { get; }
    public IAsyncRelayCommand CheckForUpdatesCommand { get; }
    public IRelayCommand DismissUpdateNotificationCommand { get; }
    public IRelayCommand OpenDownloadPageCommand { get; }
    public IRelayCommand ShowAboutCommand { get; }
    public IRelayCommand ExitApplicationCommand { get; }
}
```

#### `LoadOrderKeeper.ViewModels.DiffDialogViewModel`

```csharp
public class ConfirmationRequestedEventArgs : EventArgs
{
    public string Title { get; }
    public string Message { get; }
    public ConfirmationIcon Icon { get; }
    public ConfirmationButton Buttons { get; }
    public ConfirmationResult Result { get; set; }

    public ConfirmationRequestedEventArgs(string title, string message, ConfirmationIcon icon = ConfirmationIcon.Warning, ConfirmationButton buttons = ConfirmationButton.YesNo);
}

public partial class DiffDialogViewModel : ObservableObject, IDisposable
{
    public DiffDialogViewModel(IEnumerable<DiffLineModel> diffLines, MainViewModel mainViewModel);

    public ObservableCollection<DiffLineModel> DiffLines { get; }
    public string Title { get; }
    public string Description { get; }
    public string UpdateReferenceButtonText { get; }
    public string FixLoadOrderButtonText { get; }
    public string DiscardChangesButtonText { get; }
    public string CloseButtonText { get; }
    public string NoDifferencesMessage { get; }
    public string ReEnableModMenuText { get; }
    public string ReplaceWithMenuText { get; }
    public string RemoveModMenuText { get; }
    public bool ShowSortingRecommendation { get; }
    public string SortingRecommendationMessage { get; }
    public IReadOnlyList<DiffLineModel> AddedMods { get; }
    public bool HasAddedMods { get; }
    public bool HasInsertedMods { get; }
    public bool HasDifferences { get; }
    public int ScrollTargetIndex { get; }
    public string DiffStatusMessage { get; }
    public bool HasStatusMessage { get; }

    public IAsyncRelayCommand UpdateReferenceCommand { get; }
    public IAsyncRelayCommand FixLoadOrderCommand { get; }
    public IAsyncRelayCommand<DiffLineModel> ReEnableModCommand { get; }
    public IAsyncRelayCommand<DiffLineModel> RemoveNewModCommand { get; }
    public IAsyncRelayCommand<(DiffLineModel Removed, DiffLineModel Replacement)> ReplaceRemovedModCommand { get; }
    public IRelayCommand<DiffLineModel> ToggleDependentChangesCommand { get; }

    public event EventHandler? CloseRequested;
    public event EventHandler? ScrollRequested;
    public event EventHandler<ConfirmationRequestedEventArgs>? ConfirmationRequested;

    public Task<bool> RefreshDiffAsync(string? reason = null);
    public void Dispose();
}
```

#### `LoadOrderKeeper.ViewModels.SwitchProfileViewModel`

```csharp
public partial class SwitchProfileViewModel : ObservableObject
{
    public SwitchProfileViewModel(AppConfigModel config);

    public ObservableCollection<ProfileModel> Profiles { get; set; }
    public bool IsLoading { get; set; }
    public string WindowTitle { get; }

    public event EventHandler<ProfileModel>? ProfileSelected;

    public Task LoadProfilesAsync();
    public void SelectProfile(ProfileModel profile);
    public bool IsActiveProfile(ProfileModel profile);
}
```

#### `LoadOrderKeeper.ViewModels.ManageProfilesViewModel`

```csharp
public partial class ManageProfilesViewModel : ObservableObject
{
    public ManageProfilesViewModel(AppConfigModel config);

    public ObservableCollection<ProfileModel> Profiles { get; set; }
    public ProfileModel? SelectedProfile { get; set; }
    public bool IsLoading { get; set; }
    public string WindowTitle { get; }
    public string AddProfileButtonText { get; }
    public string FileMenuText { get; }
    public string AddProfileMenuText { get; }
    public string EditProfileMenuText { get; }
    public string DeleteProfileMenuText { get; }
    public string CopyProfileMenuText { get; }
    public string CloseButtonText { get; }

    public event EventHandler? CloseRequested;
    public event EventHandler<ProfileModel>? AddProfileRequested;
    public event EventHandler<ProfileModel>? EditProfileRequested;
    public event EventHandler<ProfileModel>? CopyProfileRequested;

    public Task LoadProfilesAsync();
}
```

#### `LoadOrderKeeper.ViewModels.ProfilePropertiesViewModel`

```csharp
public partial class ProfilePropertiesViewModel : ObservableObject
{
    public ProfilePropertiesViewModel(IReadOnlyList<ProfileModel> existingProfiles);
    public ProfilePropertiesViewModel(ProfileModel profileToEdit, IReadOnlyList<ProfileModel> existingProfiles);

    public string Label { get; set; }
    public string Description { get; set; }
    public string? LabelError { get; set; }
    public string? DescriptionError { get; set; }
    public bool HasErrors { get; set; }
    public bool IsEditMode { get; }
    public string WindowTitle { get; }
    public string SaveButtonText { get; }
    public string CancelButtonText { get; }
    public string LabelLabelText { get; }
    public string DescriptionLabelText { get; }

    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public (string Label, string Description) GetProfileData();
}
```

#### `LoadOrderKeeper.ViewModels.ConfirmationDialogViewModel`

```csharp
public enum ConfirmationIcon
{
    None,
    Information,
    Question,
    Warning,
    Error
}

public enum ConfirmationButton
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

public enum ConfirmationResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

public partial class ConfirmationDialogViewModel : ObservableObject
{
    public ConfirmationDialogViewModel();
    public ConfirmationDialogViewModel(string title, string message, ConfirmationIcon icon = ConfirmationIcon.None, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK);

    public string Title { get; set; }
    public string Message { get; set; }
    public ConfirmationIcon Icon { get; set; }
    public ConfirmationButton Buttons { get; set; }
    public ConfirmationResult DefaultResult { get; set; }
    public ConfirmationResult Result { get; }
    public string IconKind { get; }
    public string IconColor { get; }
    public bool ShowIcon { get; }
    public bool ShowOKButton { get; }
    public bool ShowCancelButton { get; }
    public bool ShowYesButton { get; }
    public bool ShowNoButton { get; }
    public string OKButtonText { get; }
    public string CancelButtonText { get; }
    public string YesButtonText { get; }
    public string NoButtonText { get; }

    public event EventHandler? DialogResultChanged;
}
```

#### `LoadOrderKeeper.ViewModels.AboutViewModel`

```csharp
public partial class AboutViewModel : ObservableObject
{
    public AboutViewModel();

    public string ApplicationName { get; }
    public string ApplicationVersion { get; }
    public string Copyright { get; }
    public string Description { get; }
    public string HomepageUrl { get; }
    public string HomepageButtonText { get; }
    public string CloseButtonText { get; }
    public string VersionLabelText { get; }

    public event EventHandler? CloseRequested;
}
```

#### `LoadOrderKeeper.ViewModels.UpdateOptionsViewModel`

```csharp
public partial class UpdateOptionsViewModel : ObservableObject
{
    public UpdateOptionsViewModel(string currentVersion, string? latestVersion);

    public string WindowTitle { get; }
    public string MessageText { get; }
    public string NexusmodsButtonText { get; }
    public string GitHubButtonText { get; }
    public string CancelButtonText { get; }
    public string NexusmodsUrl { get; }
    public string GitHubUrl { get; }

    public event EventHandler? CloseRequested;

    public IRelayCommand OpenNexusmodsCommand { get; }
    public IRelayCommand OpenGitHubCommand { get; }
    public IRelayCommand CancelCommand { get; }
}
```

#### `LoadOrderKeeper.ViewModels.ReferenceHistoryViewModel`

```csharp
public partial class ReferenceHistoryViewModel : ObservableObject
{
    public ReferenceHistoryViewModel(AppConfigModel config);

    public ObservableCollection<ReferenceVersionMetadataModel> Versions { get; set; }
    public ReferenceVersionMetadataModel? SelectedVersion { get; set; }
    public bool IsLoading { get; set; }
    public bool HasVersions { get; }
    public string WindowTitle { get; }
    public string RollbackButtonText { get; }
    public string ClearHistoryButtonText { get; }
    public string CloseButtonText { get; }
    public string NoVersionsMessage { get; }
    public string VersionColumnHeader { get; }
    public string DateColumnHeader { get; }
    public string ChangesColumnHeader { get; }
    public string SummaryColumnHeader { get; }
    public string FileMenuText { get; }
    public string ExitMenuText { get; }
    public string EditMenuText { get; }
    public string ClearHistoryMenuText { get; }

    public event EventHandler? CloseRequested;
    public event EventHandler<ReferenceVersionMetadataModel>? RollbackRequested;

    public Task LoadVersionsAsync();
    public Task RefreshVersionsAsync();
}
```

#### `LoadOrderKeeper.ViewModels.CommentInputViewModel`

```csharp
public partial class CommentInputViewModel : ObservableObject
{
    public CommentInputViewModel();
    public CommentInputViewModel(string? existingComment);

    public string? Comment { get; set; }
    public string WindowTitle { get; }
    public string PromptText { get; }
    public string CommentPlaceholder { get; }
    public string OkButtonText { get; }
    public string CancelButtonText { get; }

    public event EventHandler? OkRequested;
    public event EventHandler? CancelRequested;
}
```

### 3.4 Views / Application

#### `LoadOrderKeeper.App`

```csharp
public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e);
}
```

#### `LoadOrderKeeper.MainWindow`

```csharp
public partial class MainWindow : Window
{
    public MainWindow();
}
```

#### `LoadOrderKeeper.Views.SettingsWindow`

```csharp
public partial class SettingsWindow : Window
{
    public SettingsWindow();
}
```

#### `LoadOrderKeeper.Views.DiffWindow`

```csharp
public partial class DiffWindow : Window
{
    public DiffWindow();
}
```

#### `LoadOrderKeeper.Views.SwitchProfileWindow`

```csharp
public partial class SwitchProfileWindow : Window
{
    public SwitchProfileWindow(AppConfigModel config);
}
```

#### `LoadOrderKeeper.Views.ManageProfilesWindow`

```csharp
public partial class ManageProfilesWindow : Window
{
    public ManageProfilesWindow(AppConfigModel config);
}
```

#### `LoadOrderKeeper.Views.ProfilePropertiesWindow`

```csharp
public partial class ProfilePropertiesWindow : Window
{
    public ProfilePropertiesWindow();
}
```

#### `LoadOrderKeeper.Views.ConfirmationDialog`

```csharp
public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog();
    public ConfirmationDialog(string title, string message, ConfirmationIcon icon = ConfirmationIcon.None, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK);

    public new ConfirmationResult ShowDialog();
    public static ConfirmationResult Show(string title, string message, ConfirmationIcon icon = ConfirmationIcon.Information, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult =ConfirmationResult.OK, Window? owner = null);
}
```

#### `LoadOrderKeeper.Views.AboutWindow`

```csharp
public partial class AboutWindow : Window
{
    public AboutWindow();
}
```

#### `LoadOrderKeeper.Views.UpdateOptionsDialog`

```csharp
public partial class UpdateOptionsDialog : Window
{
    public UpdateOptionsDialog();
}
```

#### `LoadOrderKeeper.Views.ReferenceHistoryWindow`

```csharp
public partial class ReferenceHistoryWindow : Window
{
    public ReferenceHistoryWindow();
}
```

#### `LoadOrderKeeper.Views.CommentInputDialog`

```csharp
public partial class CommentInputDialog : Window
{
    public CommentInputDialog();
    public CommentInputDialog(string? existingComment);

    public string? Comment { get; }
}
```

#### `LoadOrderKeeper.Converters.ReplacementCommandParameterConverter`

```csharp
public sealed class ReplacementCommandParameterConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture);
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture);
}
```

#### `LoadOrderKeeper.Converters.ActiveProfileVisibilityConverter`

```csharp
public sealed class ActiveProfileVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

#### `LoadOrderKeeper.Converters.CountToVisibilityConverter`

```csharp
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

#### `LoadOrderKeeper.Converters.InverseCountToVisibilityConverter`

```csharp
public sealed class InverseCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

#### `LoadOrderKeeper.Converters.InverseBooleanToVisibilityConverter`

```csharp
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

#### `LoadOrderKeeper.Converters.ChangeSummaryConverter`

```csharp
public sealed class ChangeSummaryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

---

## 4. Key Data Flows

- **Startup & Configuration**
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
  
- **Coordinator Communication**
  - Coordinators raise events when state changes; `MainViewModel` subscribes and propagates to UI via `OnPropertyChanged()`.
  - `FileMonitoringCoordinator.ChangeDetected` → `MainViewModel.OnChangeDetected()` → refreshes diff window if open.
  - `ProfileCoordinator.ProfileChanged` → `MainViewModel.OnProfileChanged()` → shows status message.
  - `ConfigurationCoordinator.ValidationChanged` → `MainViewModel.OnConfigValidationChanged()` → notifies commands to refresh `CanExecute`.
  - All coordinator properties exposed as pass-through properties in `MainViewModel` for UI binding.
  
- **Profile Initialization & Switching**
  - `MainViewModel.SwitchProfileCommand` opens `SwitchProfileWindow` with `SwitchProfileViewModel`, which loads profiles via `ProfileService.LoadProfilesAsync()`.
  - Selecting a profile calls `ProfileService.SwitchProfileAsync()`: the current `Plugins.txt` is persisted to the active profile's `main.txt`, the target profile's `main.txt` and `reference.txt` are ensured, the target `main.txt` replaces `Plugins.txt`, and `ActiveProfileId` is saved.
  - After switching, `ProfileCoordinator.RefreshActiveProfileAsync()` updates active profile state and fires `ProfileChanged` event.
  - `FileMonitoringCoordinator.UpdateState()` called to use new profile's reference file for monitoring.
  
- **Profile Management**
  - `MainViewModel.ManageProfilesCommand` opens `ManageProfilesWindow` backed by `ManageProfilesViewModel`.
  - The manage view requests CRUD actions: `ProfileService.CreateProfileAsync()`, `UpdateProfileAsync()`, `DeleteProfileAsync()`, and `CopyProfileAsync()` handle persistence; `ProfilePropertiesWindow` + `ProfilePropertiesViewModel` validates labels/descriptions before save.
  - The profiles list refreshes after each operation so the UI and `MainViewModel` reflect edits.
  
- **Settings Flow**
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
  
- **Reference & load order controls**
  - `CreateReferenceCommand` and `FixLoadOrderCommand` call `FileService.CreateReferenceFileAsync()` and `FileService.ApplyLoadOrderAsync()` respectively; both commands gate on configuration validity and `IsBusy`.
  - `DiscardChangesCommand` resets `Plugins.txt` from the active profile reference via `FileService.DiscardChangesAsync()`.
  
- **Monitoring & diffing**
  - `FileMonitoringCoordinator` runs periodic checks every 3 seconds when state is valid (config valid, reference exists, not busy).
  - `CheckPluginsFileAsync()` calls `FileService.ComparePluginsWithReferenceAsync()` to compare against the active profile's reference.
  - Detects Steam process (steam.exe) running and updates `IsSteamRunning`, `ShowSteamWarning`, and `SteamWarningTooltip` properties.
  - Fires `ChangeDetected` event when file changes detected, `SteamWarningChanged` when Steam state changes, `SortingRecommendationChanged` when sorting issues detected.
  - On differences, `FileService.WouldSortingChangeDiffsAsync()` sets the sorting recommendation, `DiffService.GetPluginsDiffAsync()` feeds both the badge count and the `DiffDialogViewModel`.
  - Switching profiles triggers `DiffDialogViewModel.RefreshDiffAsync()` when diff window is open.
  
- **Steam Process Detection**
  - `FileMonitoringCoordinator.DetectSteamProcess()` checks if steam.exe is running using `Process.GetProcessesByName()`.
  - Updates `IsSteamInstalled` (checks registry for Steam installation).
  - Updates `IsSteamRunning` (checks for running steam.exe process).
  - Calculates `ShowSteamWarning` (true when both Steam installed and running).
  - Generates `SteamWarningTooltip` with contextual message explaining why Steam should be closed.
  - Fires `SteamWarningChanged` event when warning state changes.
  - `MainViewModel` exposes pass-through properties for UI binding.
  - Warning banner in `MainWindow` shows/hides automatically based on Steam state.
  
- **Diff dialog operations**
  - In `DiffDialogViewModel`, commands trigger `FileService.ReEnableModAsync()`, `RemoveNewModAsync()`, `ReplaceModWithNewAsync()`, and `MainViewModel.DiscardChangesCommand` (which calls `FileService.DiscardChangesAsync()`), refreshing diffs afterward.
  - Update reference and discard changes actions request confirmation via `ConfirmationRequested` event, which is handled by `DiffWindow` to show `ConfirmationDialog`.
  
- **Confirmation Dialogs**
  - All `MessageBox.Show` calls replaced with `ConfirmationDialog.Show()` static method.
  - `ConfirmationDialog` provides Material Design v5 styled dialogs with icon support (Information, Question, Warning, Error) and multiple button configurations.
  - `DiffDialogViewModel` raises `ConfirmationRequested` event for critical actions (update reference, discard changes); `DiffWindow` handles the event and shows the dialog.
  - Error messages in `MainViewModel` and profile management windows use `ConfirmationDialog.Show()` for consistent UX.
  
- **About & Version Info**
  - `MainViewModel.ShowAboutCommand` opens `AboutWindow` with `AboutViewModel`.
  - `VersionService.GetApplicationVersion()` retrieves clean semantic version from assembly attributes, stripping commit hashes.
  - `AboutViewModel.OpenHomepageCommand` launches the project homepage URL in the default browser.
  
- **Status History**
  - `StatusCoordinator` maintains `StatusMessageHistory` (ObservableCollection) with last 3 status messages.
  - Each status message has a timestamp and type (Info, Success, Warning, Error).
  - `MainViewModel` calls `StatusCoordinator.AddStatusMessage()` for all status updates.
  - Displayed in main window UI via pass-through properties for quick reference of recent operations.

- **Version Check & Updates**
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

- **Reference History & Versioning**
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

- **Game Launching**
  - `GameLauncherCoordinator` manages SFSE detection and game launching.
  - `UpdateConfiguration()` called when game path changes, triggers SFSE detection.
  - Checks for `sfse_loader.exe` presence in game folder.
  - Updates `HasSfseInstalled` and `PlayButtonText` ("Play (SFSE)" or "Play (Vanilla)") accordingly.
  - `MainViewModel.PlayGame()` calls `GameLauncherCoordinator.LaunchGame()`.
  - Returns success/failure; `MainViewModel` shows error if launch fails.
  - Automatically selects correct executable (SFSE loader or vanilla) based on detection.

---

## 5. Current Constraints & Invariants

- **Coordinator Architecture**
  - All coordinators inherit from `CoordinatorBase` which provides `INotifyPropertyChanged` and `IDisposable`.
  - Coordinators are initialized in `MainViewModel` constructor and disposed in `Dispose()` method.
  - Communication between coordinators and ViewModels is event-driven via `PropertyChanged` and custom events.
  - `MainViewModel` reduced from ~1300 lines to ~900 lines (31% reduction) through coordinator extraction.
  - Pass-through properties in `MainViewModel` expose coordinator state for UI binding.
  - Each coordinator has single responsibility: file monitoring, status, updates, profiles, configuration, or game launching.

- **Configuration validity**
  - `ConfigurationCoordinator` manages validation state with caching to prevent excessive I/O.
  - `AppConfigModel.IsValid()` requires non-empty paths, existing `StarfieldAppDataPath` and `StarfieldGamePath`, plus `StarfieldGamePath/Data` present.
  - `AppConfigModel.IsValid()` also requires `Plugins.txt` to exist in `StarfieldAppDataPath` (cannot be auto-generated, user must run Starfield at least once).
  - `AppConfigModel.IsValid()` validates Profiles folder creation and writability with test file.
  - The app shuts down when configuration remains invalid after the settings dialog.
  - `ConfigurationCoordinator.ValidationChanged` event fires when validation state changes, triggering command `CanExecute` updates.
  
- **Profile storage**
  - Profiles live under `StarfieldAppDataPath/Profiles/{profileId}` with `profile.json`, `main.txt`, and `reference.txt`; folders are created automatically.
  - Profiles folder existence and writability validated via `ProfileService.EnsureProfilesFolderExists()` which tests write access with temporary file.
  - Profiles folder validation integrated into `AppConfigModel.IsValid()` and settings window validation.
  - Profile operations fail with actionable error messages if Profiles folder cannot be created or accessed.
  - `ActiveProfileId` (default `default`) resides in `AppConfigModel` and is persisted through `SettingsService`.
  - The default profile (`id = default`) is virtual, cannot be deleted or edited, and is auto-recreated when files are missing.
  - Profile labels must be unique (case-insensitive), 2–30 chars, trimmed, and cannot be `Default`; IDs are transliterated ASCII with dash separators via `ProfileService.GenerateProfileId()` and gain numeric suffixes for uniqueness.
  - `ProfileCoordinator` manages active profile state and fires `ProfileChanged` event when profile switches.
  
- **Profile switching guarantees**
  - Switching always backs up the current `Plugins.txt` into the old profile's `main.txt`, ensures the target `main.txt` and `reference.txt`, writes UTF-8 (no BOM), and updates `ActiveProfileId` before monitoring continues.
  - `ProfileCoordinator.SwitchProfileAsync()` delegates to `ProfileService.SwitchProfileAsync()` and updates coordinator state.
  
- **File locations & I/O**
  - `Plugins.txt` stays under `StarfieldAppDataPath`; references are profile-specific (`Profiles/{id}/reference.txt`).
  - `Plugins.txt` must exist for configuration to be valid (cannot be auto-generated, created by Starfield on first run).
  - All disk operations in services are asynchronous; plugins-related writes use UTF-8 without BOM, and reference creation copies raw files to retain comments.
  
- **Case restoration**
  - `FileService.ApplyLoadOrderAsync()` builds a case map from `StarfieldGamePath/Data` (`*.esm` / `*.esp`) so output lines reuse on-disk casing.
  
- **Diff semantics & monitoring**
  - `FileMonitoringCoordinator` handles all periodic checking (3-second interval) and change detection.
  - `FileService.GetModDiffAsync()` bases `ModDiffModel` flags on original vs current line numbers; `DiffService` translates them to `DiffLineModel` change types (`Added`, `Removed`, `Moved`, `Replaced`, `Inserted`).
  - The monitor compares trimmed file contents, tracks a `PluginsSignature`, and only runs when state is valid (config valid, reference exists, not busy, config not invalid).
  - Monitoring paused when `configIsInvalid` is true to prevent I/O operations on invalid paths.
  - Dependent changes are tracked and displayed: when a mod is removed/added, all mods that shift position as a result are shown as dependent changes.
  - `FileMonitoringCoordinator` fires `ChangeDetected` event with `HasChanges` and `ChangeCount` when changes detected.
  
- **Steam Process Detection**
  - `FileMonitoringCoordinator` detects Steam installation via Windows registry.
  - Checks for running steam.exe process on same 3-second timer as file monitoring.
  - Warning shown when both Steam installed and Steam running.
  - Warning automatically dismissed when Steam closes.
  - Tooltip message: "Steam is running. To prevent conflicts, it is recommended to close Steam before making changes to the load order."
  - Detection uses `Process.GetProcessesByName("steam")` for efficient process checking.
  - `SteamWarningChanged` event fired when warning state changes (Steam starts/stops).
  
- **Navigation & threading**
  - Modal windows (`SettingsWindow`, `SwitchProfileWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`, `UpdateOptionsDialog`, `CommentInputDialog`) block until close; viewmodels flow back via dialog results/events.
  - Non-modal windows (`DiffWindow`, `ManageProfilesWindow`, `ReferenceHistoryWindow`) allow main window interaction while open; `WindowManager` coordinator tracks instances to prevent duplicates and manages window lifecycle.
  - `FileMonitoringCoordinator` timer runs on the UI thread; service calls are awaited, `IsBusy` gates commands, and UI updates stay on the dispatcher thread.
  
- **Error handling**
  - Services throw `InvalidOperationException`, `IOException`, or `ArgumentException` when invariants break; `MainViewModel` captures these, updates status via `StatusCoordinator.AddStatusMessage()`, and surfaces `ConfirmationDialog` for errors.
  - All user-facing dialogs use `ConfirmationDialog` with appropriate icon types (Error, Warning, Information) for consistent Material Design v5 styling.
  - `IOException` includes specific messages for common issues: access denied, disk full, network paths.
  - Profiles folder creation failures caught at startup with actionable error dialogs offering settings access.
  - Profile operations (create, copy) validate Profiles folder exists via `ProfileService.EnsureProfilesFolderExists()` before proceeding.
  - All profile folder errors include actionable guidance (check permissions, change location).
  - Secondary windows (ManageProfilesWindow, etc.) append `UserMessages.ConfigInvalidGuidance` or `UserMessages.ProfilesFolderRequired` based on error type.
  - Steam library detection (`TryFindStarfieldInSteamLibraries`) silently catches all exceptions (missing VDF file, parse errors, I/O errors) and returns null, allowing fallback detection methods to execute.

- **Configuration Validation**
  - `ConfigurationCoordinator` caches validation state to minimize repeated file system checks.
  - `AppConfigModel.IsValid()` validates paths AND Plugins.txt existence AND Profiles folder creation/writability with test file.
  - Error banner (`ShowErrorBanner` from coordinator) shown in main window when paths invalid; includes "Open settings" button.
  - Status banner in settings window provides real-time feedback with error/success states:
    - Error state: Shows specific path issues (app data invalid, game path invalid, both invalid, Data folder missing, Plugins.txt missing, Profiles folder access issues)
    - Success state: Confirms "The configured paths are valid" with checkmark icon
  - Validation runs on: window open, input blur, save button click, auto-detected path click.
  - Validation order: paths configured → paths exist → Data folder exists → Plugins.txt exists → Profiles folder writable.
  - All operations gated by validation check to prevent I/O failures with invalid paths.
  - `ConfigurationCoordinator.GetValidationResult()` provides detailed error messages for debugging and user feedback.
  - Centralized error messages in `Constants/UserMessages.cs` for easy modification and future localization.
