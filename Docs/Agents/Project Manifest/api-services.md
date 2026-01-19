# Services API

> Public API signatures for all static service classes.

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

## History Services

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
        IReadOnlyList<DiffLineModel> changeList);
    
    // Private implementation details
    private static string SanitizePath(string path);
    private static Task<List<string>> ReadFileContentsAsync(string filePath);
}
```

---

[<< Back to Index](README.md)
