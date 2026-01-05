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
    - ViewModels: `MainViewModel`, `SettingsViewModel`, `DiffDialogViewModel`, `SwitchProfileViewModel`, `ManageProfilesViewModel`, `ProfilePropertiesViewModel`, `ConfirmationDialogViewModel`, `AboutViewModel`
    - Views: `MainWindow`, `SettingsWindow`, `DiffWindow`, `SwitchProfileWindow`, `ManageProfilesWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`
  - **Static Services**
    - `SettingsService`: configuration persistence and default path discovery (includes Steam library detection)
    - `FileService`: plugins/reference file operations plus diff helpers
    - `DiffService`: diff line construction for the UI
    - `ProfileService`: profile discovery, CRUD, switching, and file scaffolding
    - `VersionService`: centralized application version retrieval
  - **Modal Navigation**
    - `MainWindow` as shell
    - Secondary windows opened modally: `SettingsWindow`, `SwitchProfileWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`
  - **Non-Modal Windows**
    - `DiffWindow`, `ManageProfilesWindow` allow main window interaction while open; `MainViewModel` tracks instances to prevent duplicates
  - **File Monitoring**
    - `MainViewModel` uses `DispatcherTimer` to monitor `Plugins.txt` vs the active profile reference
  - **Profile Management**
    - Profiles stored per active configuration under `Profiles/{profileId}` with `main.txt`, `reference.txt`, and `profile.json`
    - Commands and dialogs coordinate through `ProfileService` to switch and manage profiles
  - **Confirmation Dialogs**
    - Custom Material Design styled `ConfirmationDialog` replaces all `MessageBox.Show` calls
    - Supports multiple icon types (Information, Question, Warning, Error) and button configurations (OK, OKCancel, YesNo, YesNoCancel)
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
├─ Models/
│  ├─ AppConfigModel.cs
│  ├─ DiffLineModel.cs
│  ├─ ModDiffModel.cs
│  ├─ ModEntryModel.cs
│  ├─ PluginsComparisonResult.cs
│  ├─ ProfileModel.cs
│  └─ StatusMessageModel.cs
├─ Services/
│  ├─ DiffService.cs
│  ├─ FileService.cs
│  ├─ ProfileService.cs
│  ├─ SettingsService.cs
│  └─ VersionService.cs
├─ ViewModels/
│  ├─ AboutViewModel.cs
│  ├─ ConfirmationDialogViewModel.cs
│  ├─ DiffDialogViewModel.cs
│  ├─ MainViewModel.cs
│  ├─ ManageProfilesViewModel.cs
│  ├─ ProfilePropertiesViewModel.cs
│  ├─ SettingsViewModel.cs
│  └─ SwitchProfileViewModel.cs
├─ Views/
│  ├─ AboutWindow.xaml
│  ├─ AboutWindow.xaml.cs
│  ├─ ConfirmationDialog.xaml
│  ├─ ConfirmationDialog.xaml.cs
│  ├─ DiffWindow.xaml
│  ├─ DiffWindow.xaml.cs
│  ├─ ManageProfilesWindow.xaml
│  ├─ ManageProfilesWindow.xaml.cs
│  ├─ ProfilePropertiesWindow.xaml
│  ├─ ProfilePropertiesWindow.xaml.cs
│  ├─ SettingsWindow.xaml
│  ├─ SettingsWindow.xaml.cs
│  ├─ SwitchProfileWindow.xaml
│  └─ SwitchProfileWindow.xaml.cs
├─ Converters/
│  ├─ ActiveProfileVisibilityConverter.cs
│  ├─ CountToVisibilityConverter.cs
│  ├─ InverseBooleanToVisibilityConverter.cs
│  ├─ InverseCountToVisibilityConverter.cs
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
│     ├─ ProfileServiceTests.cs
│     ├─ SettingsServiceTests.cs
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
    public string DetectedAppDataPath { get; }
    public string DetectedGamePath { get; }
    public bool HasDetectedAppDataPath { get; }
    public bool HasDetectedGamePath { get; }

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
    public string AboutMenuText { get; }
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
    public static ConfirmationResult Show(string title, string message, ConfirmationIcon icon = ConfirmationIcon.Information, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK, Window? owner = null);
}
```

#### `LoadOrderKeeper.Views.AboutWindow`

```csharp
public partial class AboutWindow : Window
{
    public AboutWindow();
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

---

## 4. Key Data Flows

- **Startup & Configuration**
  - `App.OnStartup` creates `MainWindow`, sets `DataContext = new MainViewModel()`, and shows it.
  - `MainViewModel` loads settings via `SettingsService.LoadSettingsAsync()`, ensures the default profile files exist through `ProfileService.EnsureDefaultProfileFilesAsync()`, checks `FileService.DoesReferenceFileExist()`, and enforces configuration validity by displaying `SettingsWindow` when needed.
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
    4. Validating installation by checking for `Data` folder existence
    5. Falling back to default Steam installation location if VDF parsing fails
    6. Final fallback to Program Files location if all detection methods fail
  - Path normalization converts forward slashes to backslashes for Windows consistency
  - All Steam detection failures are silent to avoid disrupting user experience
  
- **Reference & load order controls**
  - `CreateReferenceCommand` and `FixLoadOrderCommand` call `FileService.CreateReferenceFileAsync()` and `FileService.ApplyLoadOrderAsync()` respectively; both commands gate on configuration validity and `IsBusy`.
  - `DiscardChangesCommand` resets `Plugins.txt` from the active profile reference via `FileService.DiscardChangesAsync()`.
  
- **Monitoring & diffing**
  - `DispatcherTimer` invokes `CheckPluginsFileAsync()`; the method calls `FileService.ComparePluginsWithReferenceAsync()` to compare against the active profile's reference.
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
  - Switching always backs up the current `Plugins.txt` into the old profile's `main.txt`, ensures the target `main.txt` and `reference.txt`, writes UTF-8 (no BOM), and updates `ActiveProfileId` before monitoring continues.
  
- **File locations & I/O**
  - `Plugins.txt` stays under `StarfieldAppDataPath`; references are profile-specific (`Profiles/{id}/reference.txt`).
  - All disk operations in services are asynchronous; plugins-related writes use UTF-8 without BOM, and reference creation copies raw files to retain comments.
  
- **Case restoration**
  - `FileService.ApplyLoadOrderAsync()` builds a case map from `StarfieldGamePath/Data` (`*.esm` / `*.esp`) so output lines reuse on-disk casing.
  
- **Diff semantics & monitoring**
  - `FileService.GetModDiffAsync()` bases `ModDiffModel` flags on original vs current line numbers; `DiffService` translates them to `DiffLineModel` change types (`Added`, `Removed`, `Moved`, `Replaced`, `Inserted`).
  - The monitor compares trimmed file contents, tracks a `PluginsSignature`, and only runs when `Config.IsValid()` and `RefExists` are true.
  - Dependent changes are tracked and displayed: when a mod is removed/added, all mods that shift position as a result are shown as dependent changes.
  
- **Navigation & threading**
  - Modal windows (`SettingsWindow`, `SwitchProfileWindow`, `ProfilePropertiesWindow`, `ConfirmationDialog`, `AboutWindow`) block until close; viewmodels flow back via dialog results/events.
  - Non-modal windows (`DiffWindow`, `ManageProfilesWindow`) allow main window interaction while open; `MainViewModel` tracks instances to prevent duplicates and manages window lifecycle.
  - `DispatcherTimer` runs on the UI thread; service calls are awaited, `IsBusy` gates commands, and UI updates stay on the dispatcher thread.
  
- **Error handling**
  - Services throw `InvalidOperationException`, `IOException`, or `ArgumentException` when invariants break; `MainViewModel` captures these, updates `StatusMessage`, and surfaces `ConfirmationDialog` for errors.
  - All user-facing dialogs use `ConfirmationDialog` with appropriate icon types (Error, Warning, Information) for consistent Material Design v5 styling.
  - Steam library detection (`TryFindStarfieldInSteamLibraries`) silently catches all exceptions (missing VDF file, parse errors, I/O errors) and returns null, allowing fallback detection methods to execute.

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
