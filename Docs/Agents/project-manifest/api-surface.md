# API Surface

> Public API signatures for all classes in the codebase. Signatures only — no implementation logic.
>
> **Note**: Commands emitted by `[RelayCommand]` follow the `{MethodName}Command` naming pattern and expose `IRelayCommand` / `IAsyncRelayCommand` properties automatically.

---

## Contents

1. [Coordinators](#coordinators)
2. [Models & Constants](#models--constants)
3. [Services](#services)
4. [ViewModels](#viewmodels)
5. [Views & Converters](#views--converters)

---

## Coordinators

### `LoadOrderKeeper.Coordinators.ICoordinator`

```csharp
public interface ICoordinator : IDisposable
{
    void Initialize();
}
```

### `LoadOrderKeeper.Coordinators.CoordinatorBase`

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

### `LoadOrderKeeper.Coordinators.FileMonitoringCoordinator`

```csharp
public sealed class FileMonitoringCoordinator : CoordinatorBase
{
    // Properties
    public bool PluginsFileChangedExternally { get; }
    public int ChangeCount { get; }
    public int DependentChangeCount { get; }
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

**Event Firing Behavior:**
- `ChangeDetected` fires when `PluginsFileChangedExternally` state changes **OR** when file signature changes; enables both main window state updates and automatic refresh of open diff windows.
- Multiple subscribers can listen simultaneously (e.g., `MainViewModel` and `DiffDialogViewModel`).

### `LoadOrderKeeper.Coordinators.StatusCoordinator`

```csharp
public sealed class StatusCoordinator : CoordinatorBase
{
    // Properties
    public string StatusMessage { get; }
    public ObservableCollection<StatusMessageModel> StatusMessageHistory { get; }
    
    // Methods
    public void AddStatusMessage(string message, StatusMessageType type = StatusMessageType.Info);
    public IReadOnlyList<StatusMessageModel> GetAllMessages();
    public string GetReadyStatusMessage(bool configValid);
    public void ClearHistory();
}
```

**Internal Logging:** Maintains a rolling display window of recent messages for UI plus an unlimited internal log accessible via `GetAllMessages()`. Used by `DebugStateService` to include full status history in debug exports.

### `LoadOrderKeeper.Coordinators.UpdateCheckCoordinator`

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

### `LoadOrderKeeper.Coordinators.ProfileCoordinator`

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

### `LoadOrderKeeper.Coordinators.ConfigurationCoordinator`

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

### `LoadOrderKeeper.Coordinators.GameLauncherCoordinator`

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

### `LoadOrderKeeper.Coordinators.WindowManager`

```csharp
public sealed class WindowManager : CoordinatorBase
{
    public bool IsWindowOpen<T>() where T : Window;
    public void RegisterWindow<T>(T window) where T : Window;
    public void UnregisterWindow<T>() where T : Window;
    public void BringToFront<T>() where T : Window;
}
```

---

### Coordinator Event Arguments

#### `LoadOrderKeeper.Coordinators.Events.ChangeDetectedEventArgs`

```csharp
public sealed class ChangeDetectedEventArgs : EventArgs
{
    public bool HasChanges { get; }
    public int ChangeCount { get; }
    public int DependentChangeCount { get; }
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

---

## Models & Constants

### `LoadOrderKeeper.Constants.UserMessages`

```csharp
public static class UserMessages
{
    public const string ConfigInvalidGuidance = "...";
    public const string ProfilesFolderRequired = "...";
    public const string ProfilesFolderAccessDenied = "...";
    public const string PluginsTxtRequired = "...";
}
```

### `LoadOrderKeeper.Models.AppConfigModel`

```csharp
public class AppConfigModel
{
    public string StarfieldAppDataPath { get; set; }
    public string StarfieldGamePath { get; set; }
    public string? ActiveProfileId { get; set; }
    public string PreferredLanguage { get; set; } // Default: "auto"

    public bool IsValid();
    public string GetPluginsFilePath();
    public string GetReferenceFilePath();
}
```

**Language Preference:** `PreferredLanguage` defaults to `"auto"` (system locale detection); accepts specific culture codes such as `"en-US"`, `"de-DE"`.

### `LoadOrderKeeper.Models.ProfileModel`

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

### `LoadOrderKeeper.Models.DiffChangeType`

```csharp
public enum DiffChangeType
{
    Unchanged,
    Added,
    Removed,
    Moved,
    Replaced,
    Inserted,
    Separator      // Visual-only: a "···" divider between non-adjacent context groups
}
```

### `LoadOrderKeeper.Models.DiffLineModel`

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
    public string DependentChangesSummary { get; set; }              // Set by DiffService.ClassifyChanges; plain auto-property
    public string? DependentChangeCauseFileName { get; set; }       // FileName of the causal Removed/Inserted entry
    public string? DependentChangeCauseAction { get; set; }         // Locale key: "DependentCause_Removed" / "DependentCause_Inserted"
    public bool IsDependentChangesExpanded { get; set; }
    public string Prefix { get; }
}
```

**`DependentChanges`:** `List<DiffLineModel>` initialized to an empty list in the constructor and populated exclusively by `DiffService.ClassifyChanges` (Step 6) before the result is returned to callers. The mutable `List<T>` type is intentional — it allows `ClassifyChanges` to append dependents efficiently via `Add()` without copying. Callers receiving `DiffLineModel` instances from `GetPluginsDiffAsync` should treat `DependentChanges` as read-only; no production code path mutates it after `ClassifyChanges` returns.

### `LoadOrderKeeper.Models.ModEntryModel`

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

### `LoadOrderKeeper.Models.ModDiffModel`

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

### `LoadOrderKeeper.Models.PluginsComparisonResult`

```csharp
public readonly record struct PluginsComparisonResult(bool HasDifferences, string PluginsSignature);
```

### `LoadOrderKeeper.Models.ReferenceVersionMetadataModel`

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

### `LoadOrderKeeper.Models.PendingChangesModel`

```csharp
public sealed class PendingChangesModel
{
    public string? Comment { get; set; }
    public List<string> AddedMods { get; set; }
    public List<string> RemovedMods { get; set; }
    public bool IsEmpty { get; }
    public int TotalChanges { get; }

    public static PendingChangesModel CreateEmpty();
    public static PendingChangesModel Create(IReadOnlyList<string> addedMods, IReadOnlyList<string> removedMods);
    public static PendingChangesModel Create(string? comment, IReadOnlyList<string> addedMods, IReadOnlyList<string> removedMods);
}
```

### `LoadOrderKeeper.Models.DebugStateModel`

```csharp
public sealed class DebugStateModel
{
    public string ApplicationVersion { get; set; }
    public ConfigurationState Configuration { get; set; }
    public SteamState Steam { get; set; }
    public int TotalChangesDetected { get; set; }
    public List<string> PluginsTxtContents { get; set; }
    public List<string> ReferenceContents { get; set; }
    public List<DiffLineModel> ChangeList { get; set; }

    public sealed class ConfigurationState
    {
        public string AppDataPath { get; set; }
        public string GamePath { get; set; }
        public string? ActiveProfileId { get; set; }
    }

    public sealed class SteamState
    {
        public bool IsInstalled { get; set; }
        public bool IsRunning { get; set; }
    }
}
```

### `LoadOrderKeeper.Models.StatusMessageModel`

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

### `LoadOrderKeeper.Models.UpdateCheckResult`

```csharp
public sealed record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? DownloadUrl);
```

### `LoadOrderKeeper.ViewModels.LanguageOption`

```csharp
public sealed class LanguageOption
{
    public string Code { get; set; }
    public string DisplayName { get; set; }

    public LanguageOption(string code, string displayName);
}
```

**Purpose:** Model for the language selection dropdown in `SettingsWindow`. Bound via `AvailableLanguages` on `SettingsViewModel`.

---

## Services

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
}
```

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
    public static Task<IReadOnlyList<ModEntryModel>> ReadModListAsync(string filePath, bool isReferenceFile = false);
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

**`ReadModListAsync`:** Thin public wrapper around the private `ReadFileAsync`. Reads a `Plugins.txt`-format file and returns its parsed entries as `IReadOnlyList<ModEntryModel>`. Pass `isReferenceFile: true` when reading the reference file so that entries are tagged accordingly. Used by `DiffService.GetPluginsDiffAsync` to load both the reference and current mod lists before passing them to `ComputeLcs`.

### `LoadOrderKeeper.Services.DiffService`

```csharp
public static class DiffService
{
    public static Task<IReadOnlyList<DiffLineModel>> GetPluginsDiffAsync(AppConfigModel config);
    public static Task<bool> HasIndependentMovedModsAsync(AppConfigModel config);

    // Internal — accessible to LoadOrderKeeper.Tests via [InternalsVisibleTo]
    internal static List<(int refIndex, int curIndex)> ComputeLcs(
        IReadOnlyList<string> reference,
        IReadOnlyList<string> current,
        StringComparer comparer);

    internal static List<DiffLineModel> ClassifyChanges(
        IReadOnlyList<ModEntryModel> reference,
        IReadOnlyList<ModEntryModel> current,
        List<(int refIndex, int curIndex)> lcs);
}
```

**`HasIndependentMovedModsAsync`:** Calls `GetPluginsDiffAsync` and returns `true` if any `Moved` entry is NOT nested inside a `DependentChanges` group — indicating an independent reordering that sorting could resolve. Used by `FileMonitoringCoordinator` to set the sorting recommendation. Returns `false` if `config` is invalid or either file is absent.

**`ComputeLcs`:** Standard DP-based Longest Common Subsequence. Returns paired `(refIndex, curIndex)` indices in order, identifying which mods maintained their relative sequence between the two lists. The `comparer` parameter controls case-sensitivity — pass `StringComparer.OrdinalIgnoreCase` for the production path.

**`ClassifyChanges`:** Classifies mod list differences using the pre-computed LCS as the "stable spine." Runs a 7-step pipeline (Steps 0–6): (0) build lookup sets, (1) detect shifted LCS items, (2) same-filename reconciliation → `Moved`, (3) LCS-aligned replacement detection → `Replaced`, (4) classify remaining new mods as `Added` or `Inserted`, (5) build the top-level result list, (6) group shifted items as `DependentChanges` under their causal insertion/deletion. Step 6 is split into two sub-steps: (6a) attribute shifted mods to `Removed` entries using a reference-position range scan; (6b) attribute remaining shifted mods to `Inserted` entries using a two-pointer scan over a `shiftedByCurPos` list pre-sorted by `CurrentNumber` — `insertedEntries` is sorted ascending, so a single advancing `shiftedIdx` pointer achieves amortized O(k) for all `firstAffected` lookups across all insertions. All `DiffLineModel` entries are constructed via `_localization.GetString()` — no hardcoded UI strings. Requires `ModEntryModel.LineNumber` to be populated before calling (guaranteed by `FileService.ReadFileAsync`). Pure function — no shared mutable state. Covered by dedicated isolation tests in `ClassifyChangesTests.cs` targeting each classification step independently.

**`LineNumber` invariant and `Debug.Assert` pattern:** `FileService.ReadFileAsync` always assigns `LineNumber` to every `ModEntryModel` it produces. This invariant is enforced at three assertion sites inside `ClassifyChanges`:

- **Step 1 (LCS loop):** `Debug.Assert(reference[ri].LineNumber.HasValue)` and `Debug.Assert(current[ci].LineNumber.HasValue)` fire before the `?? (ri + 1)` / `?? (ci + 1)` fallbacks. The fallbacks use a 1-based LCS index as a *proxy position* so that shift detection can still run in Release even if the invariant is ever violated (1-based avoids collision with the 0-sentinel used in Step 3).
- **Step 3 (`remainingNew` loop):** `Debug.Assert(m.LineNumber.HasValue)` fires before the `?? 0` sentinel. The 0-sentinel differs from the Step 1 proxy intentionally: 0 is outside the valid line-number range, so any null-`LineNumber` entry is naturally excluded from position-based replacement matching and falls through to be classified as `Added`.
- **Step 6 (shifted-lines sort):** Two asserts fire at the top of the Step 6 guard block. `Debug.Assert(shiftedLines.All(s => s.ReferenceNumber.HasValue))` replaces the former vacuous `.Where(s => s.ReferenceNumber.HasValue)` filter, which was removed because all shifted lines are constructed from LCS pairs with explicit `refPos` values and the filter was unreachable dead code. `Debug.Assert(shiftedLines.All(s => s.CurrentNumber.HasValue))` covers the additional invariant required by the Step 6b two-pointer sort (`shiftedByCurPos` is ordered by `CurrentNumber`) — shifted lines are constructed from LCS pairs with explicit `curPos` values so `CurrentNumber` is always set.

All three `Debug.Assert` calls are elided in Release builds; the `??` fallbacks remain as defensive code. `using System.Diagnostics;` is required.

**Deferred assertion gaps (known maintenance debt):** Two additional `??` fallbacks inside `ClassifyChanges` share the same invariant but do not yet have corresponding assertions:

- **Step 2** (`MovedText_Description` string interpolation, `removed.LineNumber ?? 0` and `matchingNew.LineNumber ?? 0` at L247–248)
- **Step 3** (`removedByRefPos` sort, `OrderBy(m => m.LineNumber ?? 0)` at L298–300, and `removed.LineNumber ?? 0` at L305)

These gaps should be addressed in a future cleanup pass alongside any other `DiffService.cs` maintenance. The assertion pattern established by Steps 1, 3, and 6 is the intended template.

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

**Purpose:** Manages reference file operations including creation, updating with version history, discard workflow, and rollback. Coordinates comment input dialogs, pending changes, and archiving.

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
    
    public bool IsDiffWindowOpen { get; }
    public bool IsManageProfilesWindowOpen { get; }
    public bool IsReferenceHistoryWindowOpen { get; }
    public bool IsViewPendingChangesWindowOpen { get; }
}
```

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
}
```

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
}
```

**Debug State Contents:** Application version, configuration paths (sanitized), Steam state, diff change list, full Plugins.txt and reference file contents, complete status message history.

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
    
    // Culture discovery
    public IReadOnlyList<string> GetAvailableCultures();
    public string GetLocaleName(string cultureName);
}
```

**Purpose:** Singleton service managing JSON-based localization with zero-hardcoding architecture. Provides thread-safe string retrieval, runtime culture switching, dynamic locale discovery, and fallback to English.

### `LoadOrderKeeper.Tools.LocalizationJsonNormalizer`

```csharp
public static class LocalizationJsonNormalizer
{
    public static void NormalizeFile(string filePath);
    public static void NormalizeAllLocales(string localesPath);
    public static bool ValidateFile(string filePath);
}
```

**Purpose:** Development-only utility for normalizing JSON localization files before committing. Not used at runtime.

---

## ViewModels

### `LoadOrderKeeper.ViewModels.MainViewModel`

```csharp
public partial class MainViewModel : ObservableObject, IDisposable
{
    public MainViewModel();

    // Config & state
    public AppConfigModel Config { get; set; }
    public bool RefExists { get; set; }
    public bool IsBusy { get; set; }
    
    // Status
    public string StatusMessage { get; set; }
    public ObservableCollection<StatusMessageModel> StatusMessageHistory { get; set; }
    
    // UI text (pass-through from coordinators / text ViewModels)
    public string ReferenceButtonText { get; set; }
    public string FixLoadOrderButtonText { get; set; }
    public string PlayGameButtonText { get; }
    public string WindowTitle { get; }
    public string ShowChangesButtonText { get; set; }
    
    // Change detection
    public bool PluginsFileChangedExternally { get; set; }
    public string SortingRecommendationMessage { get; set; }
    public bool SortingRecommendationActive { get; set; }
    
    // Profile
    public string ActiveProfileLabel { get; set; }
    
    // Update
    public bool UpdateAvailable { get; set; }
    public string UpdateMessage { get; set; }
    public bool UpdateInfoBarVisible { get; set; }
    
    // Config error banner
    public bool ConfigErrorBannerVisible { get; set; }

    // Commands
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
    public IRelayCommand ShowReferenceHistoryCommand { get; }
    public IRelayCommand ViewPendingChangesCommand { get; }
    public IAsyncRelayCommand OpenSettingsCommand { get; }
    public IAsyncRelayCommand OpenSettingsFromErrorBannerCommand { get; }
    public IAsyncRelayCommand CheckForUpdatesCommand { get; }
    public IRelayCommand DismissUpdateNotificationCommand { get; }
    public IRelayCommand OpenDownloadPageCommand { get; }
    public IRelayCommand ShowAboutCommand { get; }
    public IRelayCommand ExitApplicationCommand { get; }
    
    // Coordinator accessors (for secondary window subscriptions)
    public FileMonitoringCoordinator GetFileMonitoringCoordinator();
    public ConfigurationCoordinator GetConfigurationCoordinator();
    
    public void Dispose();
}
```

### `LoadOrderKeeper.ViewModels.SettingsViewModel`

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
    public List<LanguageOption> AvailableLanguages { get; }

    public event EventHandler? BrowseAppDataRequested;
    public event EventHandler? BrowseGamePathRequested;
    public event EventHandler? SaveRequested;

    public void UpdateAppDataPath(string selectedPath);
    public void UpdateGamePath(string selectedPath);
    public void ValidateConfiguration();
    public AppConfigModel GetConfig();
}
```

### `LoadOrderKeeper.ViewModels.DiffDialogViewModel`

```csharp
public partial class DiffDialogViewModel : ObservableObject, IDisposable
{
    public DiffDialogViewModel(IEnumerable<DiffLineModel> diffLines, MainViewModel mainViewModel);

    public ObservableCollection<DiffLineModel> DiffLines { get; }
    public ICollectionView FilteredDiffLines { get; }
    public bool ShowAllMods { get; set; }
    public string ShowAllModsToggleText { get; }
    public bool ShowSortingRecommendation { get; }
    public string SortingRecommendationMessage { get; }
    public bool ShowMultipleReplacementsHelp { get; }
    public string MultipleReplacementsHelpMessage { get; }
    public IReadOnlyList<DiffLineModel> AddedMods { get; }
    public bool HasAddedMods { get; }
    public bool HasInsertedMods { get; }
    public string InsertedWarningTooltip { get; }
    public bool HasDifferences { get; }
    public int ScrollTargetIndex { get; }
    public string DiffStatusMessage { get; }
    public bool HasStatusMessage { get; }
    public bool IsConfigValid { get; set; }
    public bool IsOperationInProgress { get; set; }
    public bool ShowOverlay { get; }

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

public class ConfirmationRequestedEventArgs : EventArgs
{
    public string Title { get; }
    public string Message { get; }
    public ConfirmationIcon Icon { get; }
    public ConfirmationButton Buttons { get; }
    public ConfirmationResult Result { get; set; }

    public ConfirmationRequestedEventArgs(string title, string message, ConfirmationIcon icon = ConfirmationIcon.Warning, ConfirmationButton buttons = ConfirmationButton.YesNo);
}
```

**Auto-Refresh:** Subscribes to `FileMonitoringCoordinator.ChangeDetected` and `ConfigurationCoordinator.ValidationChanged` in constructor; unsubscribes in `Dispose()`. `ShowOverlay` computed as `!IsConfigValid && !IsOperationInProgress`.

### `LoadOrderKeeper.ViewModels.SwitchProfileViewModel`

```csharp
public partial class SwitchProfileViewModel : ObservableObject
{
    public SwitchProfileViewModel(AppConfigModel config, ConfigurationCoordinator? configCoordinator = null);

    public ObservableCollection<ProfileModel> Profiles { get; set; }
    public bool IsLoading { get; set; }
    public bool IsConfigValid { get; set; }
    public bool IsOperationInProgress { get; set; }
    public bool ShowOverlay { get; }

    public event EventHandler<ProfileModel>? ProfileSelected;

    public Task LoadProfilesAsync();
    public void SelectProfile(ProfileModel profile);
    public bool IsActiveProfile(ProfileModel profile);
}
```

### `LoadOrderKeeper.ViewModels.ManageProfilesViewModel`

```csharp
public partial class ManageProfilesViewModel : ObservableObject
{
    public ManageProfilesViewModel(AppConfigModel config, ConfigurationCoordinator? configCoordinator = null);

    public ObservableCollection<ProfileModel> Profiles { get; set; }
    public ProfileModel? SelectedProfile { get; set; }
    public bool IsLoading { get; set; }
    public bool IsConfigValid { get; set; }
    public bool IsOperationInProgress { get; set; }
    public bool ShowOverlay { get; }

    public event EventHandler? CloseRequested;
    public event EventHandler<ProfileModel>? AddProfileRequested;
    public event EventHandler<ProfileModel>? EditProfileRequested;
    public event EventHandler<ProfileModel>? CopyProfileRequested;

    public Task LoadProfilesAsync();
}
```

### `LoadOrderKeeper.ViewModels.ProfilePropertiesViewModel`

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

    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public (string Label, string Description) GetProfileData();
}
```

### `LoadOrderKeeper.ViewModels.ReferenceHistoryViewModel`

```csharp
public partial class ReferenceHistoryViewModel : ObservableObject
{
    public ReferenceHistoryViewModel(AppConfigModel config, ConfigurationCoordinator? configCoordinator = null);

    public ObservableCollection<ReferenceVersionMetadataModel> Versions { get; set; }
    public ReferenceVersionMetadataModel? SelectedVersion { get; set; }
    public bool IsLoading { get; set; }
    public bool HasVersions { get; }
    public bool IsConfigValid { get; set; }
    public bool IsOperationInProgress { get; set; }
    public bool ShowOverlay { get; }

    public event EventHandler? CloseRequested;
    public event EventHandler<ReferenceVersionMetadataModel>? RollbackRequested;

    public Task LoadVersionsAsync();
    public Task RefreshVersionsAsync();
}
```

### `LoadOrderKeeper.ViewModels.ViewPendingChangesViewModel`

```csharp
public partial class ViewPendingChangesViewModel : ObservableObject
{
    public ViewPendingChangesViewModel(AppConfigModel config, ConfigurationCoordinator? configCoordinator = null);

    public string Comment { get; set; }
    public string CommentDisplay { get; set; }
    public List<string> AddedMods { get; set; }
    public List<string> RemovedMods { get; set; }
    public bool HasAddedMods { get; set; }
    public bool HasRemovedMods { get; set; }
    public bool HasPendingChanges { get; set; }
    public bool IsLoading { get; set; }
    public int TotalChanges { get; set; }
    public bool IsConfigValid { get; set; }
    public bool IsOperationInProgress { get; set; }
    public bool ShowOverlay { get; }

    public event EventHandler? CloseRequested;

    public Task LoadPendingChangesAsync();
}
```

### `LoadOrderKeeper.ViewModels.ErrorDialogViewModel`

```csharp
public partial class ErrorDialogViewModel : ObservableObject
{
    public ErrorDialogViewModel(Exception exception);

    public string ErrorMessage { get; set; }
    public string ErrorDetails { get; set; }

    public event EventHandler? CloseRequested;
    public event EventHandler? ExitRequested;

    public IRelayCommand OpenLogFolderCommand { get; }
    public IRelayCommand ReportBugCommand { get; }
    public IRelayCommand ExitCommand { get; }
    public IRelayCommand IgnoreCommand { get; }
}
```

### `LoadOrderKeeper.ViewModels.ConfirmationDialogViewModel`

```csharp
public enum ConfirmationIcon { None, Information, Question, Warning, Error }
public enum ConfirmationButton { OK, OKCancel, YesNo, YesNoCancel }
public enum ConfirmationResult { None, OK, Cancel, Yes, No }

public partial class ConfirmationDialogViewModel : ObservableObject
{
    public ConfirmationDialogViewModel();
    public ConfirmationDialogViewModel(string title, string message, ConfirmationIcon icon, ConfirmationButton buttons, ConfirmationResult defaultResult);

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

    public event EventHandler? DialogResultChanged;
}
```

### `LoadOrderKeeper.ViewModels.CommentInputViewModel`

```csharp
public partial class CommentInputViewModel : ObservableObject
{
    public CommentInputViewModel();
    public CommentInputViewModel(string existingComment);

    public string Comment { get; set; }
    public string WindowTitle { get; set; }
    public string PromptText { get; set; }
    public string CommentPlaceholder { get; }
    public string OkButtonText { get; }
    public string CancelButtonText { get; }

    public event EventHandler? OkRequested;
    public event EventHandler? CancelRequested;
}
```

---

## Views & Converters

### `LoadOrderKeeper.App`

```csharp
public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e);
}
```

### `LoadOrderKeeper.MainWindow`

```csharp
public partial class MainWindow : Window
{
    public MainWindow();
}
```

### `LoadOrderKeeper.Views.SettingsWindow`

```csharp
public partial class SettingsWindow : Window
{
    public SettingsWindow();
}
```

### `LoadOrderKeeper.Views.DiffWindow`

```csharp
public partial class DiffWindow : Window
{
    public DiffWindow();
}
```

### `LoadOrderKeeper.Views.ManageProfilesWindow`

```csharp
public partial class ManageProfilesWindow : Window
{
    public ManageProfilesWindow(AppConfigModel config);
}
```

### `LoadOrderKeeper.Views.ReferenceHistoryWindow`

```csharp
public partial class ReferenceHistoryWindow : Window
{
    public ReferenceHistoryWindow();
}
```

### `LoadOrderKeeper.Views.ViewPendingChangesWindow`

```csharp
public partial class ViewPendingChangesWindow : Window
{
    public ViewPendingChangesWindow();
}
```

### `LoadOrderKeeper.Views.SwitchProfileWindow`

```csharp
public partial class SwitchProfileWindow : Window
{
    public SwitchProfileWindow(AppConfigModel config);
}
```

### `LoadOrderKeeper.Views.ProfilePropertiesWindow`

```csharp
public partial class ProfilePropertiesWindow : Window
{
    public ProfilePropertiesWindow();
}
```

### `LoadOrderKeeper.Views.ConfirmationDialog`

```csharp
public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog();
    public ConfirmationDialog(string title, string message, ConfirmationIcon icon = ConfirmationIcon.None, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK);

    public new ConfirmationResult ShowDialog();
    public static ConfirmationResult Show(string title, string message, ConfirmationIcon icon = ConfirmationIcon.Information, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK, Window? owner = null);
}
```

### `LoadOrderKeeper.Views.CommentInputDialog`

```csharp
public partial class CommentInputDialog : Window
{
    public CommentInputDialog();
    public CommentInputDialog(string? existingComment);

    public string? Comment { get; }
}
```

### `LoadOrderKeeper.Views.AboutWindow`

```csharp
public partial class AboutWindow : Window
{
    public AboutWindow();
}
```

### `LoadOrderKeeper.Views.UpdateOptionsDialog`

```csharp
public partial class UpdateOptionsDialog : Window
{
    public UpdateOptionsDialog();
}
```

### `LoadOrderKeeper.Views.ErrorDialog`

```csharp
public partial class ErrorDialog : Window
{
    public ErrorDialog();
}
```

---

### User Controls

#### `LoadOrderKeeper.Controls.ConfigInvalidOverlay`

```csharp
public partial class ConfigInvalidOverlay : UserControl
{
    public ConfigInvalidOverlay();
}
```

**Usage in XAML:**
```xaml
<controls:ConfigInvalidOverlay Grid.RowSpan="N"
                               Visibility="{Binding ShowOverlay, Converter={StaticResource BooleanToVisibilityConverter}}"
                               Panel.ZIndex="1000" />
```

`ShowOverlay` is computed as `!IsConfigValid && !IsOperationInProgress` on each secondary window ViewModel.

---

### Value Converters

| Class | Type | Purpose |
|---|---|---|
| `ReplacementCommandParameterConverter` | `IMultiValueConverter` | Packs two `DiffLineModel` values into a tuple for `ReplaceRemovedModCommand` |
| `ActiveProfileVisibilityConverter` | `IMultiValueConverter` | Hides the active profile indicator row for the currently active profile |
| `BooleanAndConverter` | `IMultiValueConverter` | Returns `true` only when all bound booleans are `true` |
| `CountToVisibilityConverter` | `IValueConverter` | `Visible` when count > 0, `Collapsed` otherwise |
| `InverseCountToVisibilityConverter` | `IValueConverter` | `Visible` when count == 0, `Collapsed` otherwise |
| `InverseBooleanToVisibilityConverter` | `IValueConverter` | `Visible` when `false`, `Collapsed` when `true` |
| `ChangeSummaryConverter` | `IValueConverter` | Formats a `ReferenceVersionMetadataModel` change summary for display |

---

[<< Back to Index](README.md)
