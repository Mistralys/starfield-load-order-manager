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
  - Standard .NET `System.*` APIs for I/O, processes, and collections

- **Architectural Patterns**
  - **MVVM**
    - ViewModels: `MainViewModel`, `SettingsViewModel`, `DiffDialogViewModel`
    - Views: `MainWindow`, `SettingsWindow`, `DiffWindow`
  - **Static Services**
    - `SettingsService`: configuration persistence and default path discovery
    - `FileService`: all plugins/reference file operations and diff helpers
    - `DiffService`: diff line construction for the UI
  - **Modal Navigation**
    - `MainWindow` as shell
    - `SettingsWindow` and `DiffWindow` opened modally from `MainViewModel`
  - **File Monitoring**
    - `MainViewModel` uses `DispatcherTimer` to monitor `Plugins.txt` vs reference

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
├─ Services/
│  ├─ DiffService.cs
│  ├─ FileService.cs
│  ├─ SettingsService.cs
├─ ViewModels/
│  ├─ DiffDialogViewModel.cs
│  ├─ MainViewModel.cs
│  ├─ SettingsViewModel.cs
├─ Views/
│  ├─ DiffWindow.xaml
│  ├─ DiffWindow.xaml.cs
│  ├─ SettingsWindow.xaml
│  ├─ SettingsWindow.xaml.cs
├─ Converters/
│  ├─ ReplacementCommandParameterConverter.cs
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
│  ├─ LoadOrderKeeper.Tests/
│  │  ├─ LoadOrderKeeper.Tests.csproj
│  │  ├─ DiffServiceTests.cs
│  │  ├─ FileServiceTests.cs
│  │  └─ TestConfigContext.cs
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

---

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

#### `LoadOrderKeeper.Services.DiffService`

```csharp
public static class DiffService
{
    public static Task<IReadOnlyList<DiffLineModel>> GetPluginsDiffAsync(AppConfigModel config);
}
```

---

### 3.3 ViewModels

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

> Commands generated by `[RelayCommand]` are not declared as public fields or properties here; they are exposed indirectly through the view and events.

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
    public string CurrentTargetLabel { get; }
    public string TargetPrefixText { get; }
    public string PluginsModifiedWarningText { get; }
    public string ShowChangesButtonText { get; }
    public bool PluginsFileChangedExternally { get; set; }
    public string SortingRecommendationMessage { get; set; }
    public bool SortingRecommendationActive { get; set; }

    public IRelayCommand OpenPluginsFileCommand { get; }
    public IRelayCommand OpenReferenceFileCommand { get; }
    public IRelayCommand OpenAppDataFolderCommand { get; }
    public IRelayCommand OpenGameFolderCommand { get; }
    public IRelayCommand PlayGameCommand { get; }
    public IAsyncRelayCommand ShowDiffCommand { get; }
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

    public IAsyncRelayCommand UpdateReferenceCommand { get; }
    public IAsyncRelayCommand FixLoadOrderCommand { get; }
    public IAsyncRelayCommand<DiffLineModel> ReEnableModCommand { get; }
    public IAsyncRelayCommand<DiffLineModel> RemoveNewModCommand { get; }
    public IAsyncRelayCommand<(DiffLineModel Removed, DiffLineModel Replacement)> ReplaceRemovedModCommand { get; }

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

---

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

- `App.OnStartup` creates `MainWindow`, sets `DataContext = new MainViewModel()`, and shows the window.
- `MainViewModel` startup:
  - Calls `SettingsService.LoadSettingsAsync()` to get `AppConfigModel`.
  - Sets `RefExists = FileService.DoesReferenceFileExist(Config)`.
  - Ensures valid configuration by opening `SettingsWindow` with `SettingsViewModel` if needed.
  - Ensures reference file exists; may call `FileService.CreateReferenceFileAsync(Config)`.
  - Configures a `DispatcherTimer` to periodically call `CheckPluginsFileAsync()`.
- Settings flow:
  - `MainViewModel.OpenSettingsAsync()` (via command) creates `SettingsViewModel` from `Config` and opens `SettingsWindow` modally.
  - `SettingsWindow` responds to `BrowseAppDataRequested` / `BrowseGamePathRequested` with folder dialogs, then calls `UpdateAppDataPath` / `UpdateGamePath`.
  - `SaveSettings` command raises `SaveRequested`; window sets `DialogResult = true` and closes.
  - `MainViewModel` then calls `settingsVm.GetConfig()`, assigns `Config`, calls `SettingsService.SaveSettingsAsync(Config)`, recomputes `RefExists`, may auto-create reference, and refreshes monitoring state.
- Reference & load order:
  - `MainViewModel.CreateReferenceAsync()` (via `CreateReferenceCommand`) calls `FileService.CreateReferenceFileAsync(Config)` and sets `RefExists = true`.
  - `MainViewModel.FixLoadOrderAsync()` (via `FixLoadOrderCommand`) calls `FileService.ApplyLoadOrderAsync(Config)`.
  - `FileService.ApplyLoadOrderAsync` uses case lookup from `StarfieldGamePath/Data` to restore correct case, orders mods by reference file and appends new mods, and writes `Plugins.txt` in UTF-8 without BOM.
- Monitoring & diff:
  - `DispatcherTimer` in `MainViewModel` periodically calls `CheckPluginsFileAsync()`.
  - `CheckPluginsFileAsync()` calls `FileService.ComparePluginsWithReferenceAsync(Config)` to get `HasDifferences` and `PluginsSignature`.
  - When differences exist, it optionally calls `FileService.WouldSortingChangeDiffsAsync(Config)` to recommend sorting, and `DiffService.GetPluginsDiffAsync(Config)` to update change count.
  - `MainViewModel.ShowDiffAsync()` uses `DiffService.GetPluginsDiffAsync(Config)` to build a `DiffDialogViewModel` and opens `DiffWindow` modally.
  - While `DiffWindow` is open, if the plugins signature changes, `MainViewModel` calls `DiffDialogViewModel.RefreshDiffAsync(reason)`.
- Diff operations (inside `DiffDialogViewModel`):
  - `ReEnableModAsync` calls `FileService.ReEnableModAsync(Config, fileName)`, then `RefreshDiffAsync()`.
  - `RemoveNewModAsync` calls `FileService.RemoveNewModAsync(Config, fileName)`, then `RefreshDiffAsync()`.
  - `ReplaceRemovedModAsync` calls `FileService.ReplaceModWithNewAsync(Config, removedFileName, replacementFileName)`, then `RefreshDiffAsync()`.
  - `DiscardChangesAsync` (command) delegates to `MainViewModel.DiscardChangesCommand`, which calls `FileService.DiscardChangesAsync(Config)` and re-checks plugins.

---

## 5. Current Constraints & Invariants

- **Configuration validity**
  - `AppConfigModel.IsValid()` must be true before any file operations; it requires:
    - Non-empty `StarfieldAppDataPath` and `StarfieldGamePath`.
    - Directories exist for both.
    - `StarfieldGamePath/Data` exists.
  - `MainViewModel` enforces configuration at startup; if still invalid after showing settings, the app shuts down.
- **File locations**
  - `Plugins.txt` and `Plugins.reference.txt` live under `StarfieldAppDataPath` (via `GetPluginsFilePath()` and `GetReferenceFilePath()`).
- **File I/O**
  - All service entry points that touch disk are asynchronous (`Task`-based).
  - Reading uses UTF-8; writing uses UTF-8 without BOM for plugins-related writes.
  - `FileService.CreateReferenceFileAsync` copies raw `Plugins.txt` to reference to preserve comments/blank lines.
- **Case restoration**
  - `FileService` must build a case lookup from `StarfieldGamePath/Data` (for `*.esm` and `*.esp`, all subdirectories).
  - Output lines use on-disk casing when available; otherwise they use the input filename.
- **Diff semantics**
  - `FileService.GetModDiffAsync` bases `ModDiffModel` on reference/current line numbers to determine `IsNew`, `IsRemoved`, and `IsMoved`.
  - `DiffService.GetPluginsDiffAsync` converts these into `DiffLineModel` with change types `Added`, `Removed`, `Moved`, and `Replaced` (when an addition shares the same line number as a removal).
- **Monitoring rules**
  - The plugins monitor timer is active only when `Config.IsValid()` and `RefExists` are true.
  - The monitor compares full file contents, ignoring trailing blank lines, and tracks a string `PluginsSignature` to detect changes.
- **Navigation**
  - All secondary windows (`SettingsWindow`, `DiffWindow`) are modal dialogs.
  - ViewModels are created by `MainViewModel` and passed into the views via `DataContext`.
  - Results flow back to `MainViewModel` through `ShowDialog()` results and/or ViewModel events.
- **Threading / UI safety**
  - `DispatcherTimer` runs on the UI thread.
  - Service calls are awaited from command handlers; `IsBusy` prevents re-entrancy for critical operations and pauses some monitoring behavior.
- **Error handling**
  - Services throw exceptions when invariants are violated (`InvalidOperationException`, `FileNotFoundException`, `ArgumentException`, etc.).
  - `MainViewModel` converts most errors into `StatusMessage` updates and `MessageBox` dialogs for the user.
