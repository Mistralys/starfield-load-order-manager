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
    - `DiffWindow`, `ManageProfilesWindow`, `ReferenceHistoryWindow` allow main window interaction while open; `MainViewModel` tracks instances to prevent duplicates and manages window lifecycle
  - **File Monitoring**
    - `MainViewModel` uses `DispatcherTimer` with fixed 3-second interval (optimized through testing)
    - Monitoring paused when configuration invalid to prevent unnecessary I/O operations
  - **Profile Management**
    - Profiles stored per active configuration under `Profiles/{profileId}` with `main.txt`, `reference.txt`, `profile.json`, `pending-changes.json`, and `History/` folder
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
    - Non-intrusive info bar in `MainWindow` shows when updates available
    - Automatic background check on startup with 24-hour caching
    - Manual check via Help menu bypasses cache
    - `UpdateOptionsDialog` provides clickable download buttons for Nexusmods and GitHub
  - **Configuration Validation**
    - `MainViewModel` maintains cached validation state to prevent excessive I/O on invalid paths
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
│  │     └─ 07-group-dependent-mod-changes.md
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

### 3.1 Models

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
  - `MainViewModel` loads settings via `SettingsService.LoadSettingsAsync()`, validates Profiles folder via `ProfileService.EnsureProfilesFolderExists()`, ensures the default profile files exist through `ProfileService.EnsureDefaultProfileFilesAsync()`, checks `FileService.DoesReferenceFileExist()`, and enforces configuration validity by displaying `SettingsWindow` when needed.
  - If Profiles folder cannot be created or accessed, error dialog shown with option to open settings or shutdown.
  - If no reference exists yet but `Plugins.txt` is present, `FileService.CreateReferenceFileAsync()` seeds the active profile reference automatically.
  
- **Profile Initialization & Switching**
  - `MainViewModel.SwitchProfileCommand` opens `SwitchProfileWindow` with `SwitchProfileViewModel`, which loads profiles via `ProfileService.LoadProfilesAsync()`.
  - Selecting a profile calls `ProfileService.SwitchProfileAsync()`: the current `Plugins.txt` is persisted to the active profile's `main.txt`, the target profile's `main.txt` and `reference.txt` are ensured, the target `main.txt` replaces `Plugins.txt`, and `ActiveProfileId` is saved.
  - After switching, `MainViewModel` refreshes `ActiveProfileLabel`, `RefExists`, timer state, and kicks off `CheckPluginsFileAsync()` so monitoring uses the new profile's reference.
  
- **Profile Management**
  - `MainViewModel.ManageProfilesCommand` opens `ManageProfilesWindow` backed by `ManageProfilesViewModel`.
  - The manage view requests CRUD actions: `ProfileService.CreateProfileAsync()`, `UpdateProfileAsync()`, `DeleteProfileAsync()`, and `CopyProfileAsync()` handle persistence; `ProfilePropertiesWindow` + `ProfilePropertiesViewModel` validates labels/descriptions before save.
  - The profiles list refreshes after each operation so the UI and `MainViewModel` reflect edits.
  
- **Settings Flow**
  - `MainViewModel.OpenSettingsCommand` shows `SettingsWindow`; on success, `SettingsService.SaveSettingsAsync()` persists `AppConfigModel`, `RefExists` is recomputed, and monitoring restarts.
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
  - `DispatcherTimer` invokes `CheckPluginsFileAsync()` every 3 seconds (fixed interval optimized through testing); the method calls `FileService.ComparePluginsWithReferenceAsync()` to compare against the active profile's reference.
  - Timer pauses when configuration becomes invalid to prevent unnecessary I/O operations; resumes when configuration valid again.
  - Validation state cached in `MainViewModel._configIsInvalid` to minimize repeated checks.
  - On differences, `FileService.WouldSortingChangeDiffsAsync()` sets the sorting recommendation, `DiffService.GetPluginsDiffAsync()` feeds both the badge count and the `DiffDialogViewModel`, and switching profiles triggers `DiffDialogViewModel.RefreshDiffAsync()` when open.
  
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
  - `MainViewModel` maintains `StatusMessageHistory` (ObservableCollection) with last 3 status messages.
  - Each status message has a timestamp and type (Info, Success, Warning, Error).
  - Displayed in main window UI for quick reference of recent operations.

- **Version Check & Updates**
  - `MainViewModel.LoadInitialStateAsync()` calls `CheckForUpdatesBackgroundAsync()` on startup to check for new versions.
  - Background check calls `UpdateCheckService.CheckForUpdatesAsync(bypassCache: false)` with 24-hour cache.
  - If update available, `UpdateAvailable`, `UpdateMessage`, and `UpdateInfoBarVisible` properties are set, triggering info bar display.
  - `MainViewModel.CheckForUpdatesCommand` (from Help menu) calls `UpdateCheckService.CheckForUpdatesAsync(bypassCache: true)` for immediate check.
  - Manual check shows `ConfirmationDialog` if no update available, or sets info bar properties if update found.
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
    4. Triggers `CheckPluginsFileAsync()` to show changes in diff window for review
  - Context menu actions call `ReferenceHistoryService.UpdateVersionCommentAsync()`, `DeleteVersionAsync()`, and `ClearAllHistoryAsync()` with confirmation dialogs.
  - History window auto-refreshes when new versions created while window is open (non-modal behavior).
  - Each profile maintains independent history with maximum 16 versions; `ReferenceHistoryService.PruneOldVersionsAsync()` removes oldest versions after each archive.
  - All version files and metadata stored as UTF-8 without BOM in `Profiles/{profileId}/History/` folder.
  - `DateTimeFormattingService.FormatFriendly()` provides user-friendly timestamps ("Today 14:56", "Yesterday 16:41", "Jan 15 14:56", "Dec 25, 2023 14:56").

---

## 5. Current Constraints & Invariants

- **Configuration validity**
  - `AppConfigModel.IsValid()` requires non-empty paths, existing `StarfieldAppDataPath` and `StarfieldGamePath`, plus `StarfieldGamePath/Data` present.
  - `AppConfigModel.IsValid()` also requires `Plugins.txt` to exist in `StarfieldAppDataPath` (cannot be auto-generated, user must run Starfield at least once).
  - `AppConfigModel.IsValid()` validates Profiles folder creation and writability with test file.
  - The app shuts down when configuration remains invalid after the settings dialog.
  
- **Profile storage**
  - Profiles live under `StarfieldAppDataPath/Profiles/{profileId}` with `profile.json`, `main.txt`, and `reference.txt`; folders are created automatically.
  - Profiles folder existence and writability validated via `ProfileService.EnsureProfilesFolderExists()` which tests write access with temporary file.
  - Profiles folder validation integrated into `AppConfigModel.IsValid()` and settings window validation.
  - Profile operations fail with actionable error messages if Profiles folder cannot be created or accessed.
  - `ActiveProfileId` (default `default`) resides in `AppConfigModel` and is persisted through `SettingsService`.
  - The default profile (`id = default`) is virtual, cannot be deleted or edited, and is auto-recreated when files are missing.
  - Profile labels must be unique (case-insensitive), 2–30 chars, trimmed, and cannot be `Default`; IDs are transliterated ASCII with dash separators via `ProfileService.GenerateProfileId()` and gain numeric suffixes for uniqueness.
  
- **Profile switching guarantees**
  - Switching always backs up the current `Plugins.txt` into the old profile's `main.txt`, ensures the target `main.txt` and `reference.txt`, writes UTF-8 (no BOM), and updates `ActiveProfileId` before monitoring continues.
  
- **File locations & I/O**
  - `Plugins.txt` stays under `StarfieldAppDataPath`; references are profile-specific (`Profiles/{id}/reference.txt`).
  - `Plugins.txt` must exist for configuration to be valid (cannot be auto-generated, created by Starfield on first run).
  - All disk operations in services are asynchronous; plugins-related writes use UTF-8 without BOM, and reference creation copies raw files to retain comments.
  
- **Case restoration**
  - `FileService.ApplyLoadOrderAsync()` builds a case map from `StarfieldGamePath/Data` (`*.esm` / `*.esp`) so output lines reuse on-disk casing.
  
- **Diff semantics & monitoring**
  - `FileService.GetModDiffAsync()` bases `ModDiffModel` flags on original vs current line numbers; `DiffService` translates them to `DiffLineModel` change types (`Added`, `Removed`, `Moved`, `Replaced`, `Inserted`).
  - The monitor compares trimmed file contents, tracks a `PluginsSignature`, and only runs when `Config.IsValid()` and `RefExists` are true.
  - Fixed 3-second check interval (constant `PluginCheckIntervalSeconds` in `MainViewModel`).
  - Cached validation state (`_configIsInvalid`) prevents excessive I/O operations on invalid paths.
  - Dependent changes are tracked and displayed: when a mod is removed/added, all mods that shift position as a result are shown as dependent changes.
  
- **Navigation & threading**
  - Modal windows (`SettingsWindow`, `SwitchProfileWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`, `UpdateOptionsDialog`, `CommentInputDialog`) block until close; viewmodels flow back via dialog results/events.
  - Non-modal windows (`DiffWindow`, `ManageProfilesWindow`, `ReferenceHistoryWindow`) allow main window interaction while open; `MainViewModel` tracks instances to prevent duplicates and manages window lifecycle.
  - `DispatcherTimer` runs on the UI thread; service calls are awaited, `IsBusy` gates commands, and UI updates stay on the dispatcher thread.
  
- **Error handling**
  - Services throw `InvalidOperationException`, `IOException`, or `ArgumentException` when invariants break; `MainViewModel` captures these, updates `StatusMessage`, and surfaces `ConfirmationDialog` for errors.
  - All user-facing dialogs use `ConfirmationDialog` with appropriate icon types (Error, Warning, Information) for consistent Material Design v5 styling.
  - `IOException` includes specific messages for common issues: access denied, disk full, network paths.
  - Profiles folder creation failures caught at startup with actionable error dialogs offering settings access.
  - Profile operations (create, copy) validate Profiles folder exists via `ProfileService.EnsureProfilesFolderExists()` before proceeding.
  - All profile folder errors include actionable guidance (check permissions, change location).
  - Secondary windows (ManageProfilesWindow, etc.) append `UserMessages.ConfigInvalidGuidance` or `UserMessages.ProfilesFolderRequired` based on error type.
  - Steam library detection (`TryFindStarfieldInSteamLibraries`) silently catches all exceptions (missing VDF file, parse errors, I/O errors) and returns null, allowing fallback detection methods to execute.

- **Configuration Validation**
  - `MainViewModel` caches validation state in `_configIsInvalid` field, updated on timer ticks, config changes, and settings dialog close.
  - `AppConfigModel.IsValid()` validates paths AND Plugins.txt existence AND Profiles folder creation/writability with test file.
  - Error banner (`ConfigErrorBannerVisible`) shown in main window when paths invalid; includes "Open settings" button.
  - Status banner in settings window provides real-time feedback with error/success states:
    - Error state: Shows specific path issues (app data invalid, game path invalid, both invalid, Data folder missing, Plugins.txt missing, Profiles folder access issues)
    - Success state: Confirms "The configured paths are valid" with checkmark icon
  - Validation runs on: window open, input blur, save button click, auto-detected path click.
  - Validation order: paths configured → paths exist → Data folder exists → Plugins.txt exists → Profiles folder writable.
  - All operations gated by validation check to prevent I/O failures with invalid paths.
  - Centralized error messages in `Constants/UserMessages.cs` for easy modification and future localization.

- **Reference History System**
  - Each profile stores version history independently in `Profiles/{profileId}/History/` with `reference_vX.txt` and `reference_vX.json` files.
  - Version numbers are sequential integers starting at 1, determined by `existingVersions.Max(v => v.VersionNumber) + 1`.
  - Maximum 16 versions per profile enforced by `PruneOldVersionsAsync()`; oldest versions (lowest numbers) are deleted first when limit exceeded.
  - Pending changes stored per-profile in `Profiles/{profileId}/pending-changes.json` with `AddedMods` and `RemovedMods` lists.
  - Pending changes system ensures each archived version describes what changed **when creating that version**, not what comes after it.
  - First version (when history empty and no pending changes) automatically labeled "Initial version" with empty change lists.
  - All history files written as UTF-8 without BOM using `System.Text.Json` with indented formatting.
  - Archive failures log warning but allow reference update to proceed (non-blocking).
  - Load failures return empty history or empty pending changes (graceful degradation).
  - Corrupted JSON files silently ignored; missing folders automatically created.
  - Rollback replaces current reference but does **not** modify `Plugins.txt` directly—user reviews in diff window first.
  - Version metadata includes: `VersionNumber`, `Timestamp` (ISO 8601), `Comment` (nullable string), `AddedMods`, `RemovedMods`.
  - Comments limited to 500 characters; empty/null comments allowed (defaults to "Initial version" for first version only).
  - History window tracks single instance in `MainViewModel._referenceHistoryWindow`; existing window brought to front when command invoked again.
  - History window auto-refreshes via `ReferenceHistoryViewModel.RefreshVersionsAsync()` when `MainViewModel` creates new version.
  - Date/time formatting uses `DateTimeFormattingService` for consistency: friendly display (no seconds) in history, timestamps (with seconds) in status messages.

- **Steam Library Detection**
  - `SettingsService` includes intelligent Steam library folder detection for auto-discovering Starfield installations.
  - **Implementation Details**:
    - Uses `Gameloop.Vdf` library (v0.6.2) to parse Valve Data Format files
    - Reads `steamapps/libraryfolders.vdf` from main Steam installation
    - Starfield AppID constant: `1716740`
    - VDF structure: `libraryfolders` → numeric keys (0, 1, 2) → `path` + `apps` properties
    - Each library's `apps` object contains AppIDs as keys; presence indicates game installation in that library
    - Constructs path: `{library-path}/steamapps/common/Starfield`
    - Validates by checking `Data` subfolder existence
  - **Registry Keys Checked** (in order):
    1. `HKEY_CURRENT_USER\Software\Valve\Steam\SteamPath`
    2. `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam\InstallPath` (64-bit systems)
    3. `HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam\InstallPath` (32-bit systems)
  - **Fallback Sequence**:
    1. Parse VDF and search all libraries (preferred method)
    2. Check default Steam installation: `{steam-path}/steamapps/common/Starfield`
    3. Check Program Files: `%ProgramFiles(x86)%/Steam/steamapps/common/Starfield`
  - **Test Coverage**: 8 unit tests in `SettingsServiceTests` covering:
    - Basic detection with example VDF structure
    - First-match selection when multiple libraries contain Starfield
    - Null return when Starfield not found in any library
    - Null return when VDF file missing
    - Null return when Data folder missing (invalid installation)
    - Silent failure on corrupted VDF content
    - Path normalization (forward slashes → backslashes)
    - Handling libraries without `apps` property
  
- **UI/UX Conventions**
  - All text displayed in UI uses bindings (no hardcoded strings in XAML).
  - Material Design v5 semantic brushes used throughout for theme consistency.
  - Dark mode theme by default.
  - Confirmation dialogs shown for destructive actions (discard changes, update reference with removed/inserted mods).
  - Button labels include ellipsis ("...") when they open dialogs or require further interaction.
  - Design-time attributes (`d:` prefix with `mc:Ignorable="d"`) used for XAML designer support.

- **Version Check System**
  - `UpdateCheckService` is a static service with no instance state.
  - Constants for GitHub owner (`Mistralys`), repo (`starfield-load-order-manager`), Nexusmods URL, and GitHub releases URL.
  - Uses unauthenticated GitHub API requests (60 requests/hour limit, suitable for small user base).
  - Cache file location: `%LOCALAPPDATA%\StarfieldLoadOrderKeeper\update-check-cache.json`.
  - Cache expiration: 24 hours from last check timestamp.
  - Version parsing: semantic versioning (Major.Minor.Patch) with optional pre-release suffix after `-`.
  - Pre-release versions (containing `-beta`, `-rc`, etc.) are filtered out and ignored.
  - Version comparison: only major/minor/patch components compared; equal or older versions don't trigger notification.
  - Update info bar: dismissible per session, non-intrusive, appears at top of `MainWindow` below menu bar.
  - Update options dialog: modal, shows current and latest version, two download buttons with Material Design icons.
  - Download URLs open in default browser via `Process.Start()` with `UseShellExecute = true`.
  - Network timeout: 10 seconds for GitHub API requests.
  - Background check failures are completely silent (no user notification).
  - Manual check failures show `UpdateOptionsDialog` with error message and download links.
  - `HttpClient` instance is static and reused, includes `User-Agent: StarfieldLoadOrderKeeper` header.
  - JSON serialization uses `System.Text.Json` with indented formatting for cache files.
  - Cache saves are fire-and-forget; failures don't propagate to caller.
