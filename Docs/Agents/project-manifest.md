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
  - `MaterialDesignThemes` / `MaterialDesignColors` for dialogs, icons, and card layouts
  - Standard .NET `System.*` APIs for I/O, processes, collections, and JSON serialization

- **Architectural Patterns**
  - **MVVM**
    - ViewModels: `MainViewModel`, `SettingsViewModel`, `DiffDialogViewModel`, `SwitchProfileViewModel`, `ManageProfilesViewModel`, `ProfilePropertiesViewModel`
    - Views: `MainWindow`, `SettingsWindow`, `DiffWindow`, `SwitchProfileWindow`, `ManageProfilesWindow`, `ProfilePropertiesWindow`
  - **Static Services**
    - `SettingsService`: configuration persistence and default path discovery
    - `FileService`: plugins/reference file operations plus diff helpers
    - `DiffService`: diff line construction for the UI
    - `ProfileService`: profile discovery, CRUD, switching, and file scaffolding
  - **Modal Navigation**
    - `MainWindow` as shell
    - Secondary windows opened modally: `SettingsWindow`, `DiffWindow`, `SwitchProfileWindow`, `ManageProfilesWindow`, `ProfilePropertiesWindow`
  - **File Monitoring**
    - `MainViewModel` uses `DispatcherTimer` to monitor `Plugins.txt` vs the active profile reference
  - **Profile Management**
    - Profiles stored per active configuration under `Profiles/{profileId}` with `main.txt`, `reference.txt`, and `profile.json`
    - Commands and dialogs coordinate through `ProfileService` to switch and manage profiles

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
├─ Models/
│  ├─ AppConfigModel.cs
│  ├─ DiffLineModel.cs
│  ├─ ModDiffModel.cs
│  ├─ ModEntryModel.cs
│  ├─ PluginsComparisonResult.cs
│  └─ ProfileModel.cs
├─ Services/
│  ├─ DiffService.cs
│  ├─ FileService.cs
│  ├─ ProfileService.cs
│  └─ SettingsService.cs
├─ ViewModels/
│  ├─ DiffDialogViewModel.cs
│  ├─ MainViewModel.cs
│  ├─ ManageProfilesViewModel.cs
│  ├─ ProfilePropertiesViewModel.cs
│  ├─ SettingsViewModel.cs
│  └─ SwitchProfileViewModel.cs
├─ Views/
│  ├─ DiffWindow.xaml
│  ├─ DiffWindow.xaml.cs
│  ├─ ManageProfilesWindow.xaml
│  ├─ ManageProfilesWindow.xaml.cs
│  ├─ ProfilePropertiesWindow.xaml
│  ├─ ProfilePropertiesWindow.xaml.cs
│  ├─ SettingsWindow.xaml
│  ├─ SettingsWindow.xaml.cs
│  ├─ SwitchProfileWindow.xaml
│  ├─ SwitchProfileWindow.xaml.cs
│  ├─ MainWindow.xaml
│  └─ MainWindow.xaml.cs
├─ Converters/
│  └─ ReplacementCommandParameterConverter.cs
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
│  │     └─ 06-profiles-feature.md
├─ Tests/
│  └─ LoadOrderKeeper.Tests/
│     ├─ LoadOrderKeeper.Tests.csproj
│     ├─ DiffServiceTests.cs
│     ├─ FileServiceTests.cs
│     └─ TestConfigContext.cs
```

---

## 3. Public API (Signatures Only)

### 3.1 Models

#### `LoadOrderKeeper.Models.AppConfigModel`

```csharp
public class AppConfigModel
{
    public string StarfieldAppDataPath { get; set; }
    public string StarfieldGamePath { get; set; }
    public int PluginCheckIntervalSeconds { get; set; }
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
    Replaced
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

### 3.2 Services

#### `LoadOrderKeeper.Services.SettingsService`

```csharp
public static class SettingsService
{
    public static Task<AppConfigModel> LoadSettingsAsync();
    public static Task SaveSettingsAsync(AppConfigModel config);
    public static string TryGetDefaultSteamPath();
    public static string TryGetDefaultAppDataPath();
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

### 3.3 ViewModels

> Commands emitted by `[RelayCommand]` follow the `{MethodName}Command` naming pattern and expose `IRelayCommand` / `IAsyncRelayCommand` properties automatically.

#### `LoadOrderKeeper.ViewModels.SettingsViewModel`

```csharp
public partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel(AppConfigModel initialConfig);

    public string StarfieldAppDataPath { get; set; }
    public string StarfieldGamePath { get; set; }
    public int PluginCheckIntervalSeconds { get; set; }

    public event EventHandler? BrowseAppDataRequested;
    public event EventHandler? BrowseGamePathRequested;
    public event EventHandler? SaveRequested;

    public void UpdateAppDataPath(string selectedPath);
    public void UpdateGamePath(string selectedPath);
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
    public bool IsBusy { get; set; }
    public string ReferenceButtonText { get; set; }
    public string FixLoadOrderButtonText { get; set; }
    public string PlayButtonText { get; }
    public string WindowTitle { get; }
    public string FileMenuHeader { get; }
    public string OpenPluginsMenuText { get; }
    public string OpenReferenceMenuText { get; }
    public string OpenAppDataFolderMenuText { get; }
    public string OpenGameFolderMenuText { get; }
    public string ExitMenuText { get; }
    public string SettingsMenuHeader { get; }
    public string ProfileMenuHeader { get; }
    public string SwitchProfileMenuText { get; }
    public string ManageProfilesMenuText { get; }
    public string CurrentTargetLabel { get; }
    public string TargetPrefixText { get; }
    public string PluginsModifiedWarningText { get; }
    public string ShowChangesButtonText { get; }
    public bool PluginsFileChangedExternally { get; set; }
    public string SortingRecommendationMessage { get; set; }
    public bool SortingRecommendationActive { get; set; }
    public string ActiveProfileLabel { get; set; }

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
    public IRelayCommand ExitApplicationCommand { get; }
}
```

#### `LoadOrderKeeper.ViewModels.DiffDialogViewModel`

```csharp
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
    public bool ShowSortingRecommendation { get; }
    public string SortingRecommendationMessage { get; }
    public IReadOnlyList<DiffLineModel> AddedMods { get; }
    public bool HasAddedMods { get; }
    public event EventHandler? CloseRequested;
    public event EventHandler? ScrollRequested;
    public bool HasDifferences { get; }
    public int ScrollTargetIndex { get; }
    public string DiffStatusMessage { get; }
    public bool HasStatusMessage { get; }

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

#### `LoadOrderKeeper.Converters.ReplacementCommandParameterConverter`

```csharp
public sealed class ReplacementCommandParameterConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture);
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture);
}
```

---

## 4. Key Data Flows

- **Startup & Configuration**
  - `App.OnStartup` creates `MainWindow`, sets `DataContext = new MainViewModel()`, and shows it.
  - `MainViewModel` loads settings via `SettingsService.LoadSettingsAsync()`, ensures the default profile files exist through `ProfileService.EnsureDefaultProfileFilesAsync()`, checks `FileService.DoesReferenceFileExist()`, and enforces configuration validity by displaying `SettingsWindow` when needed.
  - If no reference exists yet but `Plugins.txt` is present, `FileService.CreateReferenceFileAsync()` seeds the active profile reference automatically.
- **Profile Initialization & Switching**
  - `MainViewModel.SwitchProfileCommand` opens `SwitchProfileWindow` with `SwitchProfileViewModel`, which loads profiles via `ProfileService.LoadProfilesAsync()`.
  - Selecting a profile calls `ProfileService.SwitchProfileAsync()`: the current `Plugins.txt` is persisted to the active profile’s `main.txt`, the target profile’s `main.txt` and `reference.txt` are ensured, the target `main.txt` replaces `Plugins.txt`, and `ActiveProfileId` is saved.
  - After switching, `MainViewModel` refreshes `ActiveProfileLabel`, `RefExists`, timer state, and kicks off `CheckPluginsFileAsync()` so monitoring uses the new profile’s reference.
- **Profile Management**
  - `MainViewModel.ManageProfilesCommand` opens `ManageProfilesWindow` backed by `ManageProfilesViewModel`.
  - The manage view requests CRUD actions: `ProfileService.CreateProfileAsync()`, `UpdateProfileAsync()`, `DeleteProfileAsync()`, and `CopyProfileAsync()` handle persistence; `ProfilePropertiesWindow` + `ProfilePropertiesViewModel` validates labels/descriptions before save.
  - The profiles list refreshes after each operation so the UI and `MainViewModel` reflect edits.
- **Settings Flow**
  - `MainViewModel.OpenSettingsCommand` shows `SettingsWindow`; on success, `SettingsService.SaveSettingsAsync()` persists `AppConfigModel`, `RefExists` is recomputed, and monitoring restarts.
  - Configuration edits retain `ActiveProfileId`, so profile-specific references stay aligned.
- **Reference & load order controls**
  - `CreateReferenceCommand` and `FixLoadOrderCommand` call `FileService.CreateReferenceFileAsync()` and `FileService.ApplyLoadOrderAsync()` respectively; both commands gate on configuration validity and `IsBusy`.
  - `DiscardChangesCommand` resets `Plugins.txt` from the active profile reference via `FileService.DiscardChangesAsync()`.
- **Monitoring & diffing**
  - `DispatcherTimer` invokes `CheckPluginsFileAsync()`; the method calls `FileService.ComparePluginsWithReferenceAsync()` to compare against the active profile’s reference.
  - On differences, `FileService.WouldSortingChangeDiffsAsync()` sets the sorting recommendation, `DiffService.GetPluginsDiffAsync()` feeds both the badge count and the `DiffDialogViewModel`, and switching profiles triggers `DiffDialogViewModel.RefreshDiffAsync()` when open.
- **Diff dialog operations**
  - In `DiffDialogViewModel`, commands trigger `FileService.ReEnableModAsync()`, `RemoveNewModAsync()`, `ReplaceModWithNewAsync()`, and `MainViewModel.DiscardChangesCommand` (which calls `FileService.DiscardChangesAsync()`), refreshing diffs afterward.

---

## 5. Current Constraints & Invariants

- **Configuration validity**
  - `AppConfigModel.IsValid()` requires non-empty paths, existing `StarfieldAppDataPath` and `StarfieldGamePath`, plus `StarfieldGamePath/Data` present.
  - The app shuts down when configuration remains invalid after the settings dialog.
- **Profile storage**
  - Profiles live under `StarfieldAppDataPath/Profiles/{profileId}` with `profile.json`, `main.txt`, and `reference.txt`; folders are created automatically.
  - `ActiveProfileId` (default `default`) resides in `AppConfigModel` and is persisted through `SettingsService`.
  - The default profile (`id = default`) is virtual, cannot be deleted or edited, and is auto-recreated when files are missing.
  - Profile labels must be unique (case-insensitive), 2–30 chars, trimmed, and cannot be `Default`; IDs are transliterated ASCII with dash separators via `ProfileService.GenerateProfileId()` and gain numeric suffixes for uniqueness.
- **Profile switching guarantees**
  - Switching always backs up the current `Plugins.txt` into the old profile’s `main.txt`, ensures the target `main.txt` and `reference.txt`, writes UTF-8 (no BOM), and updates `ActiveProfileId` before monitoring continues.
- **File locations & I/O**
  - `Plugins.txt` stays under `StarfieldAppDataPath`; references are profile-specific (`Profiles/{id}/reference.txt`).
  - All disk operations in services are asynchronous; plugins-related writes use UTF-8 without BOM, and reference creation copies raw files to retain comments.
- **Case restoration**
  - `FileService.ApplyLoadOrderAsync()` builds a case map from `StarfieldGamePath/Data` (`*.esm` / `*.esp`) so output lines reuse on-disk casing.
- **Diff semantics & monitoring**
  - `FileService.GetModDiffAsync()` bases `ModDiffModel` flags on original vs current line numbers; `DiffService` translates them to `DiffLineModel` change types (`Added`, `Removed`, `Moved`, `Replaced`).
  - The monitor compares trimmed file contents, tracks a `PluginsSignature`, and only runs when `Config.IsValid()` and `RefExists` are true.
- **Navigation & threading**
  - Modal windows (`SettingsWindow`, `SwitchProfileWindow`, `ProfilePropertiesWindow`) block until close; viewmodels flow back via dialog results/events.
  - Non-modal windows (`DiffWindow`, `ManageProfilesWindow`) allow main window interaction while open; `MainViewModel` tracks instances to prevent duplicates and manages window lifecycle.
  - `DispatcherTimer` runs on the UI thread; service calls are awaited, `IsBusy` gates commands, and UI updates stay on the dispatcher thread.
- **Error handling**
  - Services throw `InvalidOperationException`, `IOException`, or `ArgumentException` when invariants break; `MainViewModel` captures these, updates `StatusMessage`, and surfaces `MessageBox` dialogs.
