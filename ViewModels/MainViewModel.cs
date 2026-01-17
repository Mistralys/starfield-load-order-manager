using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.Views;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace LoadOrderKeeper.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private const int MaxStatusHistoryCount = 3;
        
        private readonly FileMonitoringCoordinator _fileMonitor;
        private readonly StatusCoordinator _statusCoordinator;
        private readonly UpdateCheckCoordinator _updateCheckCoordinator;
        private readonly ProfileCoordinator _profileCoordinator;
        private readonly ConfigurationCoordinator _configCoordinator;
        private readonly GameLauncherCoordinator _gameLauncher;
        private readonly CancellationTokenSource _shutdownCts = new();
        private bool _disposed;

        // Track non-modal windows to prevent duplicates
        private ManageProfilesWindow? _manageProfilesWindow;
        private DiffWindow? _diffWindow;
        private ReferenceHistoryWindow? _referenceHistoryWindow;
        private ViewPendingChangesWindow? _viewPendingChangesWindow;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private AppConfigModel _config = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private bool _refExists;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private string _referenceButtonText = "Create Reference";

        [ObservableProperty]
        private string _fixLoadOrderButtonText = "Sort mods";

        public string PlayButtonText => _gameLauncher.PlayButtonText;

        public string WindowTitle => $"Starfield Load Order Keeper v{VersionService.GetApplicationVersion()}";
        public string FileMenuHeader { get; } = "_File";
        public string OpenPluginsMenuText { get; } = "Open _Plugins.txt";
        public string OpenReferenceMenuText { get; } = "Open _Reference File";
        public string OpenAppDataFolderMenuText { get; } = "Open _AppData Folder";
        public string OpenGameFolderMenuText { get; } = "Open _Game Folder";
        public string ExitMenuText { get; } = "E_xit";
        public string EditMenuHeader { get; } = "_Edit";
        public string SettingsMenuText { get; } = "_Settings...";
        public string HelpMenuHeader { get; } = "_Help";
        public string CheckForUpdatesMenuText { get; } = "Check for _Updates...";
        public string AboutMenuText { get; } = "_About...";
        public string DebugMenuHeader { get; } = "_Debug";
        public string ResetConfigMenuText { get; } = "_Reset Configuration (for testing)...";
        public string OpenConfigFolderMenuText { get; } = "Open _Configuration Folder";
        public string DownloadOptionsButtonText { get; } = "Download options...";
        public string CurrentTargetLabel { get; } = "Current Plugins.txt target:";
        public string TargetPrefixText { get; } = "Target: ";
        public string PluginsModifiedWarningText { get; } = "Plugins.txt was modified outside Load Order Keeper.";
        public string ActiveProfilePrefixText { get; } = "Active Profile: ";
        public string ProfileMenuHeader { get; } = "_Profile";
        public string SwitchProfileMenuText { get; } = "_Switch Profile...";
        public string ManageProfilesMenuText { get; } = "_Manage Profiles...";
        public string RecentStatusMessagesText { get; } = "Recent Status Messages:";
        public string ReferenceHistoryMenuText { get; } = "History of changes...";
        public string ViewPendingChangesMenuText { get; } = "_View Pending Changes...";

        [ObservableProperty]
        private string _showChangesButtonText = "Manage load order";

        // Pass-through properties from FileMonitoringCoordinator
        public bool PluginsFileChangedExternally => _fileMonitor.PluginsFileChangedExternally;
        public string SortingRecommendationMessage => _fileMonitor.SortingRecommendationMessage;
        public bool SortingRecommendationActive => _fileMonitor.SortingRecommendationActive;
        public bool ShowSteamWarning => _fileMonitor.ShowSteamWarning;
        public string SteamWarningTooltip => _fileMonitor.SteamWarningTooltip;
        public bool IsSteamInstalled => _fileMonitor.IsSteamInstalled;
        public bool IsSteamRunning => _fileMonitor.IsSteamRunning;

        // Pass-through properties from StatusCoordinator
        public string StatusMessage => _statusCoordinator.StatusMessage;
        public ObservableCollection<StatusMessageModel> StatusMessageHistory => _statusCoordinator.StatusMessageHistory;

        // Pass-through properties from UpdateCheckCoordinator
        public bool UpdateAvailable => _updateCheckCoordinator.UpdateAvailable;
        public string UpdateMessage => _updateCheckCoordinator.UpdateMessage;
        public bool UpdateInfoBarVisible => _updateCheckCoordinator.UpdateInfoBarVisible;

        // Pass-through properties from ProfileCoordinator
        public string ActiveProfileLabel => _profileCoordinator.ActiveProfileLabel;

        // Pass-through properties from ConfigurationCoordinator
        public bool ConfigErrorBannerVisible => _configCoordinator.ShowErrorBanner;

        public IRelayCommand OpenPluginsFileCommand { get; }
        public IRelayCommand OpenReferenceFileCommand { get; }
        public IRelayCommand OpenAppDataFolderCommand { get; }
        public IRelayCommand OpenGameFolderCommand { get; }
        public IRelayCommand OpenConfigFolderCommand { get; }
        public IRelayCommand PlayGameCommand { get; }
        public IAsyncRelayCommand ShowDiffCommand { get; }

        public MainViewModel()
        {
            _fileMonitor = new FileMonitoringCoordinator();
            _statusCoordinator = new StatusCoordinator();
            _updateCheckCoordinator = new UpdateCheckCoordinator();
            _profileCoordinator = new ProfileCoordinator();
            _configCoordinator = new ConfigurationCoordinator();
            _gameLauncher = new GameLauncherCoordinator();

            // Wire up property change events for UI bindings
            _fileMonitor.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(FileMonitoringCoordinator.PluginsFileChangedExternally):
                        OnPropertyChanged(nameof(PluginsFileChangedExternally));
                        if (_fileMonitor.PluginsFileChangedExternally)
                        {
                            _statusCoordinator.AddStatusMessage(PluginsModifiedWarningText, StatusMessageType.Warning);
                        }
                        else
                        {
                            _statusCoordinator.AddStatusMessage(_statusCoordinator.GetReadyStatusMessage(Config.IsValid()), StatusMessageType.Info);
                        }
                        ShowDiffCommand?.NotifyCanExecuteChanged();
                        break;
                    case nameof(FileMonitoringCoordinator.SortingRecommendationMessage):
                        OnPropertyChanged(nameof(SortingRecommendationMessage));
                        break;
                    case nameof(FileMonitoringCoordinator.SortingRecommendationActive):
                        OnPropertyChanged(nameof(SortingRecommendationActive));
                        break;
                    case nameof(FileMonitoringCoordinator.ShowSteamWarning):
                        OnPropertyChanged(nameof(ShowSteamWarning));
                        OnPropertyChanged(nameof(SteamWarningTooltip));
                        break;
                    case nameof(FileMonitoringCoordinator.IsSteamInstalled):
                        OnPropertyChanged(nameof(IsSteamInstalled));
                        break;
                    case nameof(FileMonitoringCoordinator.IsSteamRunning):
                        OnPropertyChanged(nameof(IsSteamRunning));
                        break;
                    case nameof(FileMonitoringCoordinator.ChangeCount):
                        UpdateChangeCountDisplay(_fileMonitor.ChangeCount);
                        break;
                }
            };

            // Wire up StatusCoordinator property changes for UI bindings
            _statusCoordinator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StatusCoordinator.StatusMessage))
                {
                    OnPropertyChanged(nameof(StatusMessage));
                }
                else if (e.PropertyName == nameof(StatusCoordinator.StatusMessageHistory))
                {
                    OnPropertyChanged(nameof(StatusMessageHistory));
                }
            };

            // Wire up UpdateCheckCoordinator property changes for UI bindings
            _updateCheckCoordinator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UpdateCheckCoordinator.UpdateAvailable))
                {
                    OnPropertyChanged(nameof(UpdateAvailable));
                }
                else if (e.PropertyName == nameof(UpdateCheckCoordinator.UpdateMessage))
                {
                    OnPropertyChanged(nameof(UpdateMessage));
                }
                else if (e.PropertyName == nameof(UpdateCheckCoordinator.UpdateInfoBarVisible))
                {
                    OnPropertyChanged(nameof(UpdateInfoBarVisible));
                }
            };

            // Wire up ProfileCoordinator property changes for UI bindings
            _profileCoordinator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProfileCoordinator.ActiveProfileLabel))
                {
                    OnPropertyChanged(nameof(ActiveProfileLabel));
                }
            };

            // Wire up ConfigurationCoordinator property changes for UI bindings
            _configCoordinator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConfigurationCoordinator.ShowErrorBanner))
                {
                    OnPropertyChanged(nameof(ConfigErrorBannerVisible));
                }
            };

            // Wire up GameLauncherCoordinator property changes for UI bindings
            _gameLauncher.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GameLauncherCoordinator.PlayButtonText))
                {
                    OnPropertyChanged(nameof(PlayButtonText));
                }
            };

            // Wire up coordinator events
            _fileMonitor.ChangeDetected += OnChangeDetected;
            _fileMonitor.SortingRecommendationChanged += OnSortingRecommendationChanged;
            _fileMonitor.SteamWarningChanged += OnSteamWarningChanged;
            _profileCoordinator.ProfileChanged += OnProfileChanged;
            _configCoordinator.ValidationChanged += OnConfigValidationChanged;

            OpenPluginsFileCommand = new RelayCommand(OpenPluginsFile, CanAccessAppDataPath);
            OpenReferenceFileCommand = new RelayCommand(OpenReferenceFile, CanAccessAppDataPath);
            OpenAppDataFolderCommand = new RelayCommand(OpenAppDataFolder, CanAccessAppDataPath);
            OpenGameFolderCommand = new RelayCommand(OpenGameFolder, CanAccessGamePath);
            OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder);
            PlayGameCommand = new RelayCommand(PlayGame, CanAccessGamePath);
            ShowDiffCommand = new AsyncRelayCommand(ShowDiffAsync);

            // Initialize status history with the initial message (handled by StatusCoordinator.Initialize())

            _ = LoadInitialStateAsync();
        }

        private async Task LoadInitialStateAsync()
        {
            Config = await SettingsService.LoadSettingsAsync();
            
            // Update coordinator configurations
            _configCoordinator.UpdateConfiguration(Config);
            _profileCoordinator.UpdateConfiguration(Config);
            _gameLauncher.UpdateConfiguration(Config);
            
            // Validate configuration early, including Profiles folder
            if (Config.IsValid())
            {
                // Ensure Profiles folder exists and is writable
                try
                {
                    ProfileService.EnsureProfilesFolderExists(Config);
                }
                catch (IOException ex)
                {
                    // Show error message and offer to open settings, but don't force shutdown
                    ConfirmationDialog.Show(
                        "Profiles Folder Error",
                        $"{ex.Message}\n\n{Constants.UserMessages.ProfilesFolderRequired}",
                        ConfirmationIcon.Error,
                        ConfirmationButton.OK,
                        ConfirmationResult.OK,
                        WpfApplication.Current?.MainWindow);
                    
                    // Configuration is now invalid, error banner will appear automatically
                    // Re-validate configuration to update error state
                    _configCoordinator.ValidateConfiguration();
                }
            }
            
            // Ensure default profile exists
            await ProfileService.EnsureDefaultProfileFilesAsync(Config);
            
            RefExists = FileService.DoesReferenceFileExist(Config);

            await EnsureValidConfigurationAsync();

            var referenceResult = await EnsureReferenceFileExistsAsync();
            if (referenceResult == ReferenceInitializationResult.AlreadyExists)
            {
                AddStatusMessage(GetReadyStatusMessage(), StatusMessageType.Info);
            }

            // Initialize profile coordinator with config and load active profile
            _profileCoordinator.UpdateConfiguration(Config);
            await _profileCoordinator.RefreshActiveProfileAsync();

            // Update file monitor with initial state
            UpdateFileMonitorState();

            // Perform immediate initial check to eliminate startup delay
            // This provides instant feedback on the current state without waiting for the first timer tick
            _ = _fileMonitor.CheckPluginsFileAsync();

            // Check for updates in the background
            _ = _updateCheckCoordinator.CheckForUpdatesBackgroundAsync();
        }

        private async Task EnsureValidConfigurationAsync()
        {
            if (Config.IsValid())
            {
                return;
            }

            await ShowSettingsDialogInternalAsync();

            // If configuration is still invalid after showing settings, don't shut down
            // The error banner will be visible and inform the user
            if (!Config.IsValid())
            {
                // Re-validate to ensure error banner is shown
                _configCoordinator.ValidateConfiguration();
            }
        }

        [RelayCommand(CanExecute = nameof(CanFixLoadOrder))]
        private async Task FixLoadOrderAsync()
        {
            IsBusy = true;
            AddStatusMessage("Applying load order fix...", StatusMessageType.Info);

            try
            {
                await FileService.ApplyLoadOrderAsync(Config);
                AddStatusMessage("Load order successfully applied and fixed!", StatusMessageType.Success);
            }
            catch (Exception ex)
            {
                AddStatusMessage($"ERROR: {ex.Message}", StatusMessageType.Error);
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to fix load order: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
            }
            finally
            {
                IsBusy = false;
            }

            await _fileMonitor.CheckPluginsFileAsync();
        }

        private bool CanFixLoadOrder() => Config.IsValid() && RefExists && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanCreateReference))]
        private async Task CreateReferenceAsync()
        {
            IsBusy = true;

            try
            {
                // If updating an existing reference, archive it first with version history
                if (RefExists)
                {
                    AddStatusMessage("Updating reference file...", StatusMessageType.Info);

                    // Prompt for optional comment
                    var commentDialog = new CommentInputDialog
                    {
                        Owner = WpfApplication.Current?.MainWindow
                    };

                    bool? commentResult = commentDialog.ShowDialog();
                    
                    // If user cancelled, abort the operation
                    if (commentResult != true)
                    {
                        AddStatusMessage("Reference update cancelled.", StatusMessageType.Info);
                        return;
                    }

                    string? comment = commentDialog.Comment;

                    // Load pending changes from previous update (what changed LAST time)
                    var pendingChanges = await ReferenceHistoryService.LoadPendingChangesAsync(Config);

                    // Calculate current changes (what changed THIS time)
                    var (currentAddedMods, currentRemovedMods) = await FileService.CalculateReferenceChangesAsync(Config);

                    // Archive current reference with PREVIOUS changes (including the previous comment from pending changes)
                    // This makes the history entry describe what that version accomplished
                    try
                    {
                        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
                            Config, 
                            pendingChanges.AddedMods, 
                            pendingChanges.RemovedMods);
                        
                        // Refresh history window if open
                        await RefreshReferenceHistoryWindowAsync();
                    }
                    catch (Exception ex)
                    {
                        AddStatusMessage($"Warning: Failed to archive version: {ex.Message}", StatusMessageType.Warning);
                        // Continue with update even if archiving fails
                    }

                    // Store CURRENT changes and comment as pending for the NEXT update
                    var newPendingChanges = PendingChangesModel.Create(comment, currentAddedMods, currentRemovedMods);
                    try
                    {
                        await ReferenceHistoryService.SavePendingChangesAsync(Config, newPendingChanges);
                    }
                    catch (Exception ex)
                    {
                        AddStatusMessage($"Warning: Failed to save pending changes: {ex.Message}", StatusMessageType.Warning);
                        // Continue even if saving pending changes fails
                    }
                }
                else
                {
                    AddStatusMessage("Creating reference file...", StatusMessageType.Info);
                    
                    // First reference creation - no changes to track yet
                    // Clear any stale pending changes
                    await ReferenceHistoryService.ClearPendingChangesAsync(Config);
                }

                // Update/create the reference file
                await FileService.CreateReferenceFileAsync(Config);
                RefExists = true;

                if (RefExists)
                {
                    AddStatusMessage("Reference file updated successfully!", StatusMessageType.Success);
                }
                else
                {
                    AddStatusMessage("Reference created successfully! You can now fix the load order.", StatusMessageType.Success);
                }
            }
            catch (Exception ex)
            {
                AddStatusMessage($"ERROR: {ex.Message}", StatusMessageType.Error);
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to {(RefExists ? "update" : "create")} reference: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
            }
            finally
            {
                IsBusy = false;
            }

            await _fileMonitor.CheckPluginsFileAsync();
        }

        private bool CanCreateReference() => Config.IsValid() && !IsBusy;
 
        partial void OnRefExistsChanged(bool value)
        {
            ReferenceButtonText = value ? "Accept changes" : "Create Reference";
            UpdateFileMonitorState();
        }
 
        partial void OnConfigChanged(AppConfigModel value)
        {
            // Update coordinators with new configuration
            _configCoordinator.UpdateConfiguration(value);
            _profileCoordinator.UpdateConfiguration(value);
            _gameLauncher.UpdateConfiguration(value);
            
            UpdateFileMonitorState();
            NotifyFileCommandsCanExecuteChanged();
            PlayGameCommand?.NotifyCanExecuteChanged();
        }

        partial void OnIsBusyChanged(bool value)
        {
            UpdateFileMonitorState();
            NotifyFileCommandsCanExecuteChanged();
            PlayGameCommand?.NotifyCanExecuteChanged();
        }
 
        private void OpenPluginsFile()
        {
            var path = Config.GetPluginsFilePath();
            if (!File.Exists(path))
            {
                ShowError($"Plugins file not found: {path}");
                return;
            }

            LaunchShellTarget(path, "Failed to open Plugins.txt");
        }

        private void OpenReferenceFile()
        {
            var path = Config.GetReferenceFilePath();
            if (!File.Exists(path))
            {
                ShowError($"Reference file not found: {path}");
                return;
            }

            LaunchShellTarget(path, "Failed to open reference file");
        }

        private void OpenAppDataFolder()
        {
            var path = Config.StarfieldAppDataPath;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                ShowError("AppData folder is not configured or does not exist.");
                return;
            }

            LaunchShellTarget(path, "Failed to open AppData folder");
        }

        private void OpenGameFolder()
        {
            var path = Config.StarfieldGamePath;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                ShowError("Game folder is not configured or does not exist.");
                return;
            }

            LaunchShellTarget(path, "Failed to open game folder");
        }

        private void OpenConfigFolder()
        {
            var path = SettingsService.GetConfigFolderPath();
            if (!Directory.Exists(path))
            {
                ShowError($"Configuration folder not found: {path}");
                return;
            }

            LaunchShellTarget(path, "Failed to open configuration folder");
        }

        private void PlayGame()
        {
            if (!_gameLauncher.LaunchGame())
            {
                var executablePath = _gameLauncher.GetExecutablePath();
                if (executablePath == null)
                {
                    ShowError("Unable to find game executable.");
                }
                else
                {
                    ShowError($"Failed to launch Starfield: {executablePath}");
                }
            }
        }

        private bool CanAccessAppDataPath()
        {
            return !IsBusy && Config.IsValid();
        }

        private bool CanAccessGamePath()
        {
            return !IsBusy && Config.IsValid();
        }
 
        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            await ShowSettingsDialogInternalAsync();
        }

        private async Task<bool> ShowSettingsDialogInternalAsync()
        {
            var settingsVm = new SettingsViewModel(Config);
            var window = new SettingsWindow
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = settingsVm
            };

            bool? result = window.ShowDialog();
            if (result == true)
            {
                Config = settingsVm.GetConfig();
                await SettingsService.SaveSettingsAsync(Config);
                
                // Update coordinator configurations
                _profileCoordinator.UpdateConfiguration(Config);
                
                RefExists = FileService.DoesReferenceFileExist(Config);
                var referenceResult = await EnsureReferenceFileExistsAsync();
                if (referenceResult == ReferenceInitializationResult.AlreadyExists)
                {
                    _statusCoordinator.AddStatusMessage(Config.IsValid()
                        ? "Configuration updated."
                        : "Configuration is invalid.", Config.IsValid() ? StatusMessageType.Success : StatusMessageType.Warning);
                }
                else if (referenceResult == ReferenceInitializationResult.InvalidConfiguration)
                {
                    AddStatusMessage("Configuration is invalid.", StatusMessageType.Warning);
                }
                UpdateFileMonitorState();
                await _fileMonitor.CheckPluginsFileAsync();
                return true;
            }

            return false;
        }

        [RelayCommand]
        private void ExitApplication()
        {
            WpfApplication.Current?.Shutdown();
        }

        [RelayCommand]
        private async Task SwitchProfileAsync()
        {
            var switchVm = new SwitchProfileViewModel(Config, _configCoordinator);
            var switchWindow = new SwitchProfileWindow(Config)
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = switchVm
            };

            bool? result = switchWindow.ShowDialog();
            if (result == true)
            {
                // Profile was switched, reload state
                await _profileCoordinator.RefreshActiveProfileAsync();
                RefExists = FileService.DoesReferenceFileExist(Config);
                await EnsureReferenceFileExistsAsync();
                UpdateFileMonitorState();
                await _fileMonitor.CheckPluginsFileAsync();
                // Status message is handled by ProfileChanged event
            }
        }

        [RelayCommand]
        private async Task ManageProfilesAsync()
        {
            // If window is already open, bring it to front
            if (_manageProfilesWindow != null)
            {
                _manageProfilesWindow.Activate();
                _manageProfilesWindow.Focus();
                return;
            }

            var manageVm = new ManageProfilesViewModel(Config, _configCoordinator);
            _manageProfilesWindow = new ManageProfilesWindow(Config)
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = manageVm
            };

            // Handle window closed event to clear reference
            _manageProfilesWindow.Closed += (s, e) => 
            {
                _manageProfilesWindow = null;
            };

            _manageProfilesWindow.Show();
            
            // Refresh active profile label in case it was edited (when window is closed)
            _manageProfilesWindow.Closed += async (s, e) => 
            {
                await UpdateActiveProfileLabelAsync();
            };
        }

        [RelayCommand]
        private void ShowReferenceHistory()
        {
            // If window is already open, bring it to front
            if (_referenceHistoryWindow != null)
            {
                _referenceHistoryWindow.Activate();
                _referenceHistoryWindow.Focus();
                return;
            }

            var historyVm = new ReferenceHistoryViewModel(Config, _configCoordinator);
            _referenceHistoryWindow = new ReferenceHistoryWindow
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = historyVm
            };

            // Handle rollback request
            historyVm.RollbackRequested += async (s, version) =>
            {
                await HandleRollbackRequestAsync(version, _referenceHistoryWindow);
            };

            // Handle window closed event to clear reference
            _referenceHistoryWindow.Closed += (s, e) => 
            {
                _referenceHistoryWindow = null;
            };

            _referenceHistoryWindow.Show();
        }

        [RelayCommand]
        private void ViewPendingChanges()
        {
            // If window is already open, bring it to front
            if (_viewPendingChangesWindow != null)
            {
                _viewPendingChangesWindow.Activate();
                _viewPendingChangesWindow.Focus();
                return;
            }

            var pendingChangesVm = new ViewPendingChangesViewModel(Config, _configCoordinator);
            _viewPendingChangesWindow = new ViewPendingChangesWindow
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = pendingChangesVm
            };

            // Handle window closed event to clear reference
            _viewPendingChangesWindow.Closed += (s, e) => 
            {
                _viewPendingChangesWindow = null;
            };

            _viewPendingChangesWindow.Show();
        }

        private async Task HandleRollbackRequestAsync(ReferenceVersionMetadataModel version, System.Windows.Window parentWindow)
        {
            // Show confirmation dialog
            var result = ConfirmationDialog.Show(
                "Rollback Confirmation",
                $"Are you sure you want to rollback to version {version.VersionNumber}?\n\n" +
                $"Date: {version.FormattedTimestamp}\n" +
                $"Changes: {version.TotalModsChanged}\n" +
                $"Summary: {version.GetChangeSummary()}\n\n" +
                $"The current Plugins.txt will be replaced with the list from version {version.VersionNumber}." +
                "You will then have the opportunity to review the changes before accepting them.",
                ConfirmationIcon.Question,
                ConfirmationButton.YesNo,
                ConfirmationResult.No,
                parentWindow);

            if (result != ConfirmationResult.Yes)
            {
                return;
            }

            try
            {
                // Perform rollback
                await ReferenceHistoryService.RollbackToVersionAsync(Config, version.VersionNumber);
                AddStatusMessage($"Rolled back to version {version.VersionNumber}. Accept the changes to confirm.", StatusMessageType.Success);

                // Close history window
                parentWindow.Close();

                // Trigger change detection to show in DIFF window
                await _fileMonitor.CheckPluginsFileAsync();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.Show(
                    "Rollback Failed",
                    $"Failed to rollback to version {version.VersionNumber}: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    parentWindow);
            }
        }

        private async Task RefreshReferenceHistoryWindowAsync()
        {
            if (_referenceHistoryWindow?.DataContext is ReferenceHistoryViewModel historyVm)
            {
                await historyVm.RefreshVersionsAsync();
            }
        }

        private async Task UpdateActiveProfileLabelAsync()
        {
            await _profileCoordinator.RefreshActiveProfileAsync();
        }

        private void LaunchShellTarget(string target, string failureMessage)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ShowError($"{failureMessage}: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            AddStatusMessage($"ERROR: {message}", StatusMessageType.Error);
            ConfirmationDialog.Show(
                "Error",
                message,
                ConfirmationIcon.Error,
                ConfirmationButton.OK,
                ConfirmationResult.OK,
                WpfApplication.Current?.MainWindow);
        }

        private void NotifyFileCommandsCanExecuteChanged()
        {
            OpenPluginsFileCommand?.NotifyCanExecuteChanged();
            OpenReferenceFileCommand?.NotifyCanExecuteChanged();
            OpenAppDataFolderCommand?.NotifyCanExecuteChanged();
            OpenGameFolderCommand?.NotifyCanExecuteChanged();
            OpenConfigFolderCommand?.NotifyCanExecuteChanged();
        }

        private void UpdateChangeCountDisplay(int changeCount)
        {
            ShowChangesButtonText = changeCount > 0 
                ? $"Manage load order ({changeCount} changes)"
                : "Manage load order";
        }
 
        private async Task ShowDiffAsync()
        {
            // If diff window is already open, bring it to front
            if (_diffWindow != null)
            {
                _diffWindow.Activate();
                _diffWindow.Focus();
                return;
            }

            try
            {
                var diffLines = await DiffService.GetPluginsDiffAsync(Config);
                
                var diffViewModel = new DiffDialogViewModel(diffLines, this);
                _diffWindow = new DiffWindow
                {
                    Owner = WpfApplication.Current?.MainWindow,
                    DataContext = diffViewModel
                };

                // Handle window closed event to clear reference
                _diffWindow.Closed += (s, e) => 
                {
                    _diffWindow = null;
                };

                _diffWindow.Show();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to display changes: {ex.Message}");
            }
        }

        private async void OnChangeDetected(object? sender, Coordinators.Events.ChangeDetectedEventArgs e)
        {
            // DiffWindow now subscribes directly to FileMonitoringCoordinator.ChangeDetected
            // No need to manually refresh here anymore
        }

        private void OnSortingRecommendationChanged(object? sender, Coordinators.Events.SortingRecommendationChangedEventArgs e)
        {
            // UI updates already handled by PropertyChanged
        }

        private void OnSteamWarningChanged(object? sender, Coordinators.Events.SteamWarningChangedEventArgs e)
        {
            // UI updates already handled by PropertyChanged
        }

        private void OnProfileChanged(object? sender, Coordinators.Events.ProfileChangedEventArgs e)
        {
            // Profile changed - update status message
            AddStatusMessage($"Switched to profile '{e.NewProfile.Label}'.", StatusMessageType.Success);
        }

        private void OnConfigValidationChanged(object? sender, Coordinators.Events.ConfigValidationChangedEventArgs e)
        {
            // Configuration validation state changed
            if (e.StateChanged)
            {
                // Notify commands to re-check CanExecute when config becomes valid
                if (e.IsValid)
                {
                    NotifyFileCommandsCanExecuteChanged();
                    PlayGameCommand?.NotifyCanExecuteChanged();
                    CreateReferenceCommand?.NotifyCanExecuteChanged();
                    FixLoadOrderCommand?.NotifyCanExecuteChanged();
                    DiscardChangesCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Updates the file monitor with current configuration, reference existence, busy state, and invalid config state.
        /// Call this whenever any of these states change.
        /// </summary>
        private void UpdateFileMonitorState()
        {
            _fileMonitor.UpdateState(Config, RefExists, IsBusy, !_configCoordinator.IsConfigValid);
        }

        [RelayCommand]
        private async Task DiscardChangesAsync()
        {
            IsBusy = true;
            AddStatusMessage("Discarding load order changes...", StatusMessageType.Info);

            try
            {
                await FileService.DiscardChangesAsync(Config);
                AddStatusMessage("Plugins.txt restored from reference.", StatusMessageType.Success);
            }
            catch (Exception ex)
            {
                AddStatusMessage($"ERROR: {ex.Message}", StatusMessageType.Error);
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to discard changes: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
            }
            finally
            {
                IsBusy = false;
            }

            await _fileMonitor.CheckPluginsFileAsync();
        }

        private bool CanDiscardChanges() => Config.IsValid() && RefExists && !IsBusy;

        private enum ReferenceInitializationResult
        {
            AlreadyExists,
            Created,
            MissingPluginsFile,
            InvalidConfiguration,
            Failed
        }

        private async Task<ReferenceInitializationResult> EnsureReferenceFileExistsAsync()
        {
            if (!Config.IsValid())
            {
                return ReferenceInitializationResult.InvalidConfiguration;
            }

            if (RefExists)
            {
                return ReferenceInitializationResult.AlreadyExists;
            }

            string pluginsPath = Config.GetPluginsFilePath();
            if (!File.Exists(pluginsPath))
            {
                AddStatusMessage($"Plugins.txt not found at {pluginsPath}. Unable to create reference automatically.", StatusMessageType.Warning);
                return ReferenceInitializationResult.MissingPluginsFile;
            }

            try
            {
                AddStatusMessage("No reference file found. Creating one from current Plugins.txt...", StatusMessageType.Info);
                await FileService.CreateReferenceFileAsync(Config);
                RefExists = true;
                AddStatusMessage("Reference file created automatically from current Plugins.txt.", StatusMessageType.Success);
                return ReferenceInitializationResult.Created;
            }
            catch (Exception ex)
            {
                AddStatusMessage($"ERROR: Failed to create reference automatically: {ex.Message}", StatusMessageType.Error);
                return ReferenceInitializationResult.Failed;
            }
        }

        [RelayCommand]
        private void ShowAbout()
        {
            var aboutVm = new AboutViewModel();
            var aboutWindow = new AboutWindow
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = aboutVm
            };

            aboutWindow.ShowDialog();
        }

        private void AddStatusMessage(string message, StatusMessageType type = StatusMessageType.Info)
        {
            _statusCoordinator.AddStatusMessage(message, type);
        }

        private string GetReadyStatusMessage()
        {
            return _statusCoordinator.GetReadyStatusMessage(Config.IsValid());
        }

        /// <summary>
        /// Gets the file monitoring coordinator for subscribing to change events.
        /// </summary>
        public FileMonitoringCoordinator GetFileMonitoringCoordinator()
        {
            return _fileMonitor;
        }

        /// <summary>
        /// Gets the configuration coordinator for subscribing to validation events.
        /// </summary>
        public ConfigurationCoordinator GetConfigurationCoordinator()
        {
            return _configCoordinator;
        }

        [RelayCommand]
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var result = await _updateCheckCoordinator.CheckForUpdatesManualAsync();

                if (!result.UpdateAvailable)
                {
                    ConfirmationDialog.Show(
                        "No Updates Available",
                        $"You are using the latest version ({result.CurrentVersion}).",
                        ConfirmationIcon.Information,
                        ConfirmationButton.OK,
                        ConfirmationResult.OK,
                        WpfApplication.Current?.MainWindow);
                }
            }
            catch
            {
                ShowUpdateCheckErrorDialog();
            }
        }

        [RelayCommand]
        private void DismissUpdateNotification()
        {
            _updateCheckCoordinator.DismissUpdateNotification();
        }

        [RelayCommand]
        private void OpenDownloadPage()
        {
            var latestVersion = _updateCheckCoordinator.GetLatestVersion();
            if (string.IsNullOrEmpty(latestVersion))
            {
                // Fallback if no update info available
                ShowUpdateCheckErrorDialog();
                return;
            }

            var currentVersion = VersionService.GetApplicationVersion();

            var updateVm = new UpdateOptionsViewModel(currentVersion, latestVersion);
            var updateDialog = new UpdateOptionsDialog
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = updateVm
            };

            updateDialog.ShowDialog();
        }

        private void ShowUpdateCheckErrorDialog()
        {
            var currentVersion = VersionService.GetApplicationVersion();

            var updateVm = new UpdateOptionsViewModel(currentVersion, "Unknown");
            var updateDialog = new UpdateOptionsDialog
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = updateVm
            };

            updateDialog.ShowDialog();
        }

        [RelayCommand]
        private async Task OpenSettingsFromErrorBannerAsync()
        {
            await ShowSettingsDialogInternalAsync();
        }

        [RelayCommand]
        private async Task ResetConfigurationAsync()
        {
            var result = ConfirmationDialog.Show(
                "Reset Configuration",
                "This will clear all configuration settings and reset them to empty values for testing purposes.\n\nThis action cannot be undone. Are you sure you want to continue?",
                ConfirmationIcon.Warning,
                ConfirmationButton.YesNo,
                ConfirmationResult.No,
                WpfApplication.Current?.MainWindow);

            if (result != ConfirmationResult.Yes)
            {
                return;
            }

            try
            {
                // Create a new empty config
                Config = new AppConfigModel
                {
                    StarfieldAppDataPath = string.Empty,
                    StarfieldGamePath = string.Empty,
                    ActiveProfileId = "default"
                };

                // Save the empty config
                await SettingsService.SaveSettingsAsync(Config);

                // Update coordinators
                _configCoordinator.UpdateConfiguration(Config);
                _profileCoordinator.UpdateConfiguration(Config);
                _gameLauncher.UpdateConfiguration(Config);
                
                // Reset reference existence flag
                RefExists = false;
                
                // Update file monitor state
                UpdateFileMonitorState();

                AddStatusMessage("Configuration has been reset to empty values.", StatusMessageType.Success);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to reset configuration: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Dispose coordinators
            _fileMonitor?.Dispose();
            _statusCoordinator?.Dispose();
            _updateCheckCoordinator?.Dispose();
            _profileCoordinator?.Dispose();
            _configCoordinator?.Dispose();
            _gameLauncher?.Dispose();
            
            try
            {
                _shutdownCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            
            _shutdownCts?.Dispose();
            
            // Close non-modal windows if open
            _diffWindow?.Close();
            _manageProfilesWindow?.Close();
            _referenceHistoryWindow?.Close();
            _viewPendingChangesWindow?.Close();
        }
    }
}
