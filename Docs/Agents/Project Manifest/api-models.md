# Models API

> Public API signatures for data models, enums, and constants.

---

## Constants

### `LoadOrderKeeper.Constants.UserMessages`

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

---

## Configuration Models

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

**Language Preference**:
- `PreferredLanguage`: User's language choice
  - Default value: `"auto"` (automatic system locale detection)
  - Specific cultures: `"en-US"`, `"de-DE"`, `"fr-FR"`, `"es-ES"`, `"it-IT"`
  - Applied on application startup via `ViewModelInitializer`
  - Persisted across application restarts

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

---

## Diff & Comparison Models

### `LoadOrderKeeper.Models.DiffChangeType`

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
    public string DependentChangesSummary { get; }
    public bool IsDependentChangesExpanded { get; set; }
    public string Prefix { get; }
}
```

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

---

## History Models

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

---

## Debug & Diagnostics Models

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

---

## Status & UI Models

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

---

## UI Helper Models

### `LoadOrderKeeper.ViewModels.LanguageOption`

```csharp
public class LanguageOption
{
    public string Code { get; set; }
    public string DisplayName { get; set; }

    public LanguageOption(string code, string displayName);
}
```

**Purpose**: Model for language selection dropdown in Settings window.

**Usage**:
- `Code`: Culture code (e.g., `"auto"`, `"en-US"`, `"de-DE"`)
- `DisplayName`: Native language name (e.g., `"Automatic"`, `"English"`, `"Deutsch"`)
- Bound to ComboBox via:
  - `ItemsSource="{Binding AvailableLanguages}"`
  - `SelectedValuePath="Code"`
  - `DisplayMemberPath="DisplayName"`

**Example**:
```csharp
var languages = new List<LanguageOption>
{
    new LanguageOption("auto", "Automatic"),
    new LanguageOption("en-US", "English"),
    new LanguageOption("de-DE", "Deutsch"),
    new LanguageOption("fr-FR", "Français"),
    new LanguageOption("es-ES", "Español"),
    new LanguageOption("it-IT", "Italiano")
};
```

---

[<< Back to Index](README.md)
