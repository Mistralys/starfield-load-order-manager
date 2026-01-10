# ViewModels API

> Public API signatures for all ViewModels.

> **Note**: Commands emitted by `[RelayCommand]` follow the `{MethodName}Command` naming pattern and expose `IRelayCommand` / `IAsyncRelayCommand` properties automatically.

---

## Main Application ViewModels

### `LoadOrderKeeper.ViewModels.MainViewModel`

```csharp
public partial class MainViewModel : ObservableObject, IDisposable
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
    
    public FileMonitoringCoordinator GetFileMonitoringCoordinator();
    public void Dispose();
}
```

**Window Management:**
- Tracks `_diffWindow`, `_manageProfilesWindow`, and `_referenceHistoryWindow` references to prevent duplicate instances.
- Only tracks window references, not ViewModels—each window is self-managing via direct event subscriptions.
- `GetFileMonitoringCoordinator()` exposes the coordinator for diff window event subscriptions.

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

    public event EventHandler? BrowseAppDataRequested;
    public event EventHandler? BrowseGamePathRequested;
    public event EventHandler? SaveRequested;

    public void UpdateAppDataPath(string selectedPath);
    public void UpdateGamePath(string selectedPath);
    public void ValidateConfiguration();
    public AppConfigModel GetConfig();
}
```

---

## Diff & Comparison ViewModels

### `LoadOrderKeeper.ViewModels.DiffDialogViewModel`

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
    public bool ShowMultipleReplacementsHelp { get; }
    public string MultipleReplacementsHelpMessage { get; }
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

**Auto-Refresh Architecture:**
- Constructor subscribes to `FileMonitoringCoordinator.ChangeDetected` event via `mainViewModel.GetFileMonitoringCoordinator()`.
- When event fires, `OnFileChangeDetected()` handler automatically calls `RefreshDiffAsync()`.
- `RefreshDiffAsync()` fetches latest diff, compares signatures, and updates `DiffLines` collection if changed.
- `ReplaceDiffLines()` clears and repopulates collection, triggering UI updates via `UpdateDiffState()` and `OnPropertyChanged(nameof(DiffLines))`.
- `Dispose()` unsubscribes from `ChangeDetected` event when window closes.
- Status messages include timestamps for user feedback ("Detected changes at HH:mm:ss").

---

## Profile ViewModels

### `LoadOrderKeeper.ViewModels.SwitchProfileViewModel`

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

### `LoadOrderKeeper.ViewModels.ManageProfilesViewModel`

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

---

## History ViewModels

### `LoadOrderKeeper.ViewModels.ReferenceHistoryViewModel`

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

---

## Dialog ViewModels

### `LoadOrderKeeper.ViewModels.ConfirmationDialogViewModel`

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

### `LoadOrderKeeper.ViewModels.CommentInputViewModel`

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

---

## Utility ViewModels

### `LoadOrderKeeper.ViewModels.AboutViewModel`

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

### `LoadOrderKeeper.ViewModels.UpdateOptionsViewModel`

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

---

[? Back to Index](README.md)
