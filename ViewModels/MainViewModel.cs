using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Helpers;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewTexts;
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
        private readonly WindowLifecycleService _windowService;
        private readonly FileOperationsService _fileOperations;
        private readonly ReferenceManagementService _referenceManager;
        private readonly ViewModelInitializer _initializer;
        private readonly CancellationTokenSource _shutdownCts = new();
        private bool _disposed;

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

        [ObservableProperty]
        private string _showChangesButtonText = "Manage load order";

        // Menu and UI text properties
        public MenuViewModel Menu { get; } = new();
        public MainWindowTexts MainWindowTexts { get; } = new();
        public CommonTexts CommonTexts { get; } = new();

        public string PlayButtonText => _gameLauncher.PlayButtonText;

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
            _windowService = new WindowLifecycleService();
            _fileOperations = new FileOperationsService();
            _referenceManager = new ReferenceManagementService(
                AddStatusMessage,
                async () => await _windowService.RefreshReferenceHistoryWindowAsync());
            _initializer = new ViewModelInitializer(
                AddStatusMessage,
                GetReadyStatusMessage,
                UpdateCoordinatorsWithConfig);

            // Use CoordinatorEventBinder to consolidate property change forwarding
            var binder = new CoordinatorEventBinder(OnPropertyChanged);

            // File Monitor property bindings
            binder.BindPropertiesDirect(_fileMonitor, 
                nameof(PluginsFileChangedExternally),
                nameof(SortingRecommendationMessage),
                nameof(SortingRecommendationActive),
                nameof(ShowSteamWarning),
                nameof(SteamWarningTooltip),
                nameof(IsSteamInstalled),
                nameof(IsSteamRunning));

            // Special handling for PluginsFileChangedExternally with status message
            binder.BindPropertyWithAction(_fileMonitor, nameof(FileMonitoringCoordinator.PluginsFileChangedExternally), () =>
            {
                if (_fileMonitor.PluginsFileChangedExternally)
                {
                    _statusCoordinator.AddStatusMessage(Menu.PluginsModifiedWarningText, StatusMessageType.Warning);
                }
                else
                {
                    _statusCoordinator.AddStatusMessage(_statusCoordinator.GetReadyStatusMessage(Config.IsValid()), StatusMessageType.Info);
                }
                ShowDiffCommand?.NotifyCanExecuteChanged();
            });

            // Special handling for ChangeCount
            binder.BindPropertyWithAction(_fileMonitor, nameof(FileMonitoringCoordinator.ChangeCount), () =>
            {
                UpdateChangeCountDisplay(_fileMonitor.ChangeCount);
            });

            // Status Coordinator property bindings
            binder.BindPropertiesDirect(_statusCoordinator,
                nameof(StatusMessage),
                nameof(StatusMessageHistory));

            // Update Check Coordinator property bindings
            binder.BindPropertiesDirect(_updateCheckCoordinator,
                nameof(UpdateAvailable),
                nameof(UpdateMessage),
                nameof(UpdateInfoBarVisible));

            // Profile Coordinator property bindings
            binder.BindPropertiesDirect(_profileCoordinator,
                nameof(ActiveProfileLabel));

            // Configuration Coordinator property bindings
            binder.BindProperty(_configCoordinator, 
                nameof(ConfigurationCoordinator.ShowErrorBanner),
                nameof(ConfigErrorBannerVisible));

            // Game Launcher Coordinator property bindings
            binder.BindPropertiesDirect(_gameLauncher,
                nameof(PlayButtonText));

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
            var result = await _initializer.LoadInitialStateAsync(
                _configCoordinator,
                _profileCoordinator,
                _fileMonitor,
                _updateCheckCoordinator);

            Config = result.Config;
            RefExists = result.RefExists;
            UpdateFileMonitorState();
        }

        private void UpdateCoordinatorsWithConfig(AppConfigModel config)
        {
            _configCoordinator.UpdateConfiguration(config);
            _profileCoordinator.UpdateConfiguration(config);
            _gameLauncher.UpdateConfiguration(config);
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
                bool success = await _referenceManager.CreateOrUpdateReferenceAsync(
                    Config,
                    RefExists,
                    WpfApplication.Current?.MainWindow);

                if (success)
                {
                    RefExists = true;
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
            try
            {
                _fileOperations.OpenPluginsFile(Config);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to open Plugins.txt: {ex.Message}");
            }
        }

        private void OpenReferenceFile()
        {
            try
            {
                _fileOperations.OpenReferenceFile(Config);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to open reference file: {ex.Message}");
            }
        }

        private void OpenAppDataFolder()
        {
            try
            {
                _fileOperations.OpenAppDataFolder(Config);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void OpenGameFolder()
        {
            try
            {
                _fileOperations.OpenGameFolder(Config);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void OpenConfigFolder()
        {
            try
            {
                _fileOperations.OpenConfigFolder();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
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
                DataContext = settingsVm
            };

            // Safely set Owner if MainWindow is available
            var mainWindow = WpfApplication.Current?.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded)
            {
                window.Owner = mainWindow;
            }

            bool? result = window.ShowDialog();
            if (result == true)
            {
                Config = settingsVm.GetConfig();
                await SettingsService.SaveSettingsAsync(Config);
                
                // Update coordinator configurations
                _profileCoordinator.UpdateConfiguration(Config);
                
                RefExists = FileService.DoesReferenceFileExist(Config);
                
                // Show appropriate status message
                _statusCoordinator.AddStatusMessage(Config.IsValid()
                    ? "Configuration updated."
                    : "Configuration is invalid.", Config.IsValid() ? StatusMessageType.Success : StatusMessageType.Warning);
                
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
                UpdateFileMonitorState();
                await _fileMonitor.CheckPluginsFileAsync();
                // Status message is handled by ProfileChanged event
            }
        }

        [RelayCommand]
        private async Task ManageProfilesAsync()
        {
            _windowService.ShowManageProfilesWindow(
                Config, 
                _configCoordinator, 
                WpfApplication.Current?.MainWindow,
                async () => await UpdateActiveProfileLabelAsync());
        }

        [RelayCommand]
        private void ShowReferenceHistory()
        {
            _windowService.ShowReferenceHistoryWindow(
                Config, 
                _configCoordinator, 
                WpfApplication.Current?.MainWindow,
                async (s, version) => await HandleRollbackRequestAsync(version, (System.Windows.Window)s!));
        }

        [RelayCommand]
        private void ViewPendingChanges()
        {
            _windowService.ShowViewPendingChangesWindow(Config, _configCoordinator, WpfApplication.Current?.MainWindow);
        }

        private async Task HandleRollbackRequestAsync(ReferenceVersionMetadataModel version, System.Windows.Window parentWindow)
        {
            await _referenceManager.HandleRollbackAsync(
                Config,
                version,
                parentWindow,
                async () => await _fileMonitor.CheckPluginsFileAsync());
        }

        private async Task RefreshReferenceHistoryWindowAsync()
        {
            await _windowService.RefreshReferenceHistoryWindowAsync();
        }

        private async Task UpdateActiveProfileLabelAsync()
        {
            await _profileCoordinator.RefreshActiveProfileAsync();
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
                ? string.Format(MainWindowTexts.ShowChangesButtonTextWithCount, changeCount)
                : MainWindowTexts.ShowChangesButtonText;
        }
 
        private async Task ShowDiffAsync()
        {
            try
            {
                await _windowService.ShowDiffWindowAsync(Config, this, WpfApplication.Current?.MainWindow);
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

            try
            {
                await _referenceManager.DiscardChangesAsync(Config);
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

        [RelayCommand]
        private void ThrowTestException()
        {
            throw new InvalidOperationException("This is a test exception to verify the error dialog and logging functionality. Check the error.log file in your application data folder.");
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
            _windowService?.CloseAllWindows();
        }
    }
}
