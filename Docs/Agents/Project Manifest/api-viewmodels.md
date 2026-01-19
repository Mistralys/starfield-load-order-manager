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
    public string ReferenceHistoryMenuText { get; }
    public string ViewPendingChangesMenuText { get; }
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
    public IRelayCommand ShowReferenceHistoryCommand { get; }
    public IRelayCommand ViewPendingChangesCommand { get; }
    public IAsyncRelayCommand OpenSettingsCommand { get; }
    public IAsyncRelayCommand OpenSettingsFromErrorBannerCommand { get; }
    public IAsyncRelayCommand CheckForUpdatesCommand { get; }
    public IRelayCommand DismissUpdateNotificationCommand { get; }
    public IRelayCommand OpenDownloadPageCommand { get; }
    public IRelayCommand ShowAboutCommand { get; }
    public IRelayCommand ExitApplicationCommand { get; }
    
    public FileMonitoringCoordinator GetFileMonitoringCoordinator();
    public ConfigurationCoordinator GetConfigurationCoordinator();
    public void Dispose();
}
```

**Window Management:**
- Tracks `_diffWindow`, `_manageProfilesWindow`, `_referenceHistoryWindow`, and `_viewPendingChangesWindow` references to prevent duplicate instances.
- Only tracks window references, not ViewModels—each window is self-managing via direct event subscriptions.
- `GetFileMonitoringCoordinator()` exposes the coordinator for diff window change detection event subscriptions.
- `GetConfigurationCoordinator()` exposes the coordinator for secondary windows to subscribe to validation changes for overlay management.

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

    public void UpdateAppDataPath(String selectedPath);
    public void UpdateGamePath(String selectedPath);
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
```

**Auto-Refresh Architecture:**
- Constructor subscribes to `FileMonitoringCoordinator.ChangeDetected` and `ConfigurationCoordinator.ValidationChanged` events via `mainViewModel.GetFileMonitoringCoordinator()` and `mainViewModel.GetConfigurationCoordinator()`.
- When `ChangeDetected` fires, `OnFileChangeDetected()` handler automatically calls `RefreshDiffAsync()`.
- When `ValidationChanged` fires, `OnConfigValidationChanged()` updates `IsConfigValid` property for overlay management.
- `RefreshDiffAsync()` fetches latest diff, compares signatures, and updates `DiffLines` collection if changed.
- `ReplaceDiffLines()` clears and repopulates collection, triggering UI updates via `UpdateDiffState()` and `OnPropertyChanged(nameof(DiffLines))`.
- `Dispose()` unsubscribes from both events when window closes.
- Status messages include timestamps for user feedback ("Detected changes at HH:mm:ss").
- `ShowOverlay` computed as `!IsConfigValid && !IsOperationInProgress` to display/hide configuration invalid overlay.

---

## Profile ViewModels

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
    public string WindowTitle { get; }

    public event EventHandler<ProfileModel>? ProfileSelected;

    public Task LoadProfilesAsync();
    public void SelectProfile(ProfileModel profile);
    public bool IsActiveProfile(ProfileModel profile);
}
```

**Configuration Overlay:**
- Constructor accepts optional `ConfigurationCoordinator` reference for validation tracking.
- Subscribes to `ConfigurationCoordinator.ValidationChanged` event if coordinator provided.
- `ShowOverlay` computed as `!IsConfigValid && !IsOperationInProgress` to display/hide configuration invalid overlay.
- `IsConfigValid` updated automatically when configuration validity changes.

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

**Configuration Overlay:**
- Constructor accepts optional `ConfigurationCoordinator` reference for validation tracking.
- Subscribes to `ConfigurationCoordinator.ValidationChanged` event if coordinator provided.
- `ShowOverlay` computed as `!IsConfigValid && !IsOperationInProgress` to display/hide configuration invalid overlay.
- `IsConfigValid` updated automatically when configuration validity changes.

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
    public ReferenceHistoryViewModel(AppConfigModel config, ConfigurationCoordinator? configCoordinator = null);

    public ObservableCollection<ReferenceVersionMetadataModel> Versions { get; set; }
    public ReferenceVersionMetadataModel? SelectedVersion { get; set; }
    public bool IsLoading { get; set; }
    public bool HasVersions { get; }
    public bool IsConfigValid { get; set; }
    public bool IsOperationInProgress { get; set; }
    public bool ShowOverlay { get; }
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

**Configuration Overlay:**
- Constructor accepts optional `ConfigurationCoordinator` reference for validation tracking.
- Subscribes to `ConfigurationCoordinator.ValidationChanged` event if coordinator provided.
- `ShowOverlay` computed as `!IsConfigValid && !IsOperationInProgress` to display/hide configuration invalid overlay.
- `IsConfigValid` updated automatically when configuration validity changes.

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
    public string WindowTitle { get; }
    public string ExplanationText { get; }
    public string CommentLabel { get; }
    public string AddedModsLabel { get; }
    public string RemovedModsLabel { get; }
    public string EditCommentButtonText { get; }
    public string CloseButtonText { get; }
    public string NoPendingChangesMessage { get; }

    public event EventHandler? CloseRequested;

    public Task LoadPendingChangesAsync();
}
```

**Configuration Overlay:**
- Constructor accepts optional `ConfigurationCoordinator` reference for validation tracking.
- Subscribes to `ConfigurationCoordinator.ValidationChanged` event if coordinator provided.
- `ShowOverlay` computed as `!IsConfigValid && !IsOperationInProgress` to display/hide configuration invalid overlay.
- `IsConfigValid` updated automatically when configuration validity changes.

---

## Utility ViewModels

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

**Exception Handling:**
- Displays unhandled exceptions in a user-friendly dialog
- `ErrorMessage` shows the exception message
- `ErrorDetails` shows exception type and message (can be expanded for stack trace if needed)
- `OpenLogFolderCommand` opens application data folder containing `error.log`
- `ReportBugCommand` opens GitHub issues page in browser
- `ExitCommand` immediately shuts down the application
- `IgnoreCommand` closes dialog and attempts to continue (unsafe)
- `CloseRequested` event raised when dialog should close (Ignore action)
- `ExitRequested` event raised when application should exit (Exit action)

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

---

## Comment Input ViewModel

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

[<< Back to Index](README.md)
