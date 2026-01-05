using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.Views;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace LoadOrderKeeper.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private const int MaxStatusHistoryCount = 3;
        
        private readonly DispatcherTimer _pluginsMonitorTimer;
        private bool _isCheckingPluginsFile;
        private DiffDialogViewModel? _activeDiffDialog;
        private string _lastObservedPluginsSignature = string.Empty;

        // Track non-modal windows to prevent multiple instances
        private ManageProfilesWindow? _manageProfilesWindow;
        private DiffWindow? _diffWindow;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private AppConfigModel _config = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private bool _refExists;

        [ObservableProperty]
        private string _statusMessage = "Loading settings...";

        [ObservableProperty]
        private ObservableCollection<StatusMessageModel> _statusMessageHistory = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private string _referenceButtonText = "Create Reference";

        [ObservableProperty]
        private string _fixLoadOrderButtonText = "Sort mods";

        private string _playButtonText = "Play (Vanilla)";
        public string PlayButtonText
        {
            get => _playButtonText;
            private set => SetProperty(ref _playButtonText, value);
        }

        [ObservableProperty]
        private bool _updateAvailable;

        [ObservableProperty]
        private string _updateMessage = string.Empty;

        [ObservableProperty]
        private bool _updateInfoBarVisible;

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
        public string DownloadOptionsButtonText { get; } = "Download options...";
        public string CurrentTargetLabel { get; } = "Current Plugins.txt target:";
        public string TargetPrefixText { get; } = "Target: ";
        public string PluginsModifiedWarningText { get; } = "Plugins.txt was modified outside Load Order Keeper.";
        public string ActiveProfilePrefixText { get; } = "Active Profile: ";
        public string ProfileMenuHeader { get; } = "_Profile";
        public string SwitchProfileMenuText { get; } = "_Switch Profile...";
        public string ManageProfilesMenuText { get; } = "_Manage Profiles...";
        public string RecentStatusMessagesText { get; } = "Recent Status Messages:";

        [ObservableProperty]
        private string _showChangesButtonText = "Manage load order";

        [ObservableProperty]
        private bool _pluginsFileChangedExternally;

        [ObservableProperty]
        private string _sortingRecommendationMessage = string.Empty;

        [ObservableProperty]
        private bool _sortingRecommendationActive;

        [ObservableProperty]
        private string _activeProfileLabel = "Default";

        public IRelayCommand OpenPluginsFileCommand { get; }
        public IRelayCommand OpenReferenceFileCommand { get; }
        public IRelayCommand OpenAppDataFolderCommand { get; }
        public IRelayCommand OpenGameFolderCommand { get; }
        public IRelayCommand PlayGameCommand { get; }
        public IAsyncRelayCommand ShowDiffCommand { get; }

        public MainViewModel()
        {
            _pluginsMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_config.PluginCheckIntervalSeconds > 0 ? _config.PluginCheckIntervalSeconds : 5)
            };
            _pluginsMonitorTimer.Tick += OnPluginsMonitorTick;

            OpenPluginsFileCommand = new RelayCommand(OpenPluginsFile, CanAccessAppDataPath);
            OpenReferenceFileCommand = new RelayCommand(OpenReferenceFile, CanAccessAppDataPath);
            OpenAppDataFolderCommand = new RelayCommand(OpenAppDataFolder, CanAccessAppDataPath);
            OpenGameFolderCommand = new RelayCommand(OpenGameFolder, CanAccessGamePath);
            PlayGameCommand = new RelayCommand(PlayGame, CanAccessGamePath);
            ShowDiffCommand = new AsyncRelayCommand(ShowDiffAsync);

            // Initialize status history with the initial message
            AddStatusMessage("Loading settings...", StatusMessageType.Info);

            _ = LoadInitialStateAsync();
        }

        private async Task LoadInitialStateAsync()
        {
            Config = await SettingsService.LoadSettingsAsync();
            
            // Ensure default profile exists
            await ProfileService.EnsureDefaultProfileFilesAsync(Config);
            
            RefExists = FileService.DoesReferenceFileExist(Config);

            await EnsureValidConfigurationAsync();

            var referenceResult = await EnsureReferenceFileExistsAsync();
            if (referenceResult == ReferenceInitializationResult.AlreadyExists)
            {
                AddStatusMessage(GetReadyStatusMessage(), StatusMessageType.Info);
            }

            await UpdateActiveProfileLabelAsync();

            ConfigurePluginsMonitor();
            await CheckPluginsFileAsync();

            // Check for updates in the background
            _ = CheckForUpdatesBackgroundAsync();
        }

        private async Task EnsureValidConfigurationAsync()
        {
            if (Config.IsValid())
            {
                return;
            }

            await ShowSettingsDialogInternalAsync();

            if (!Config.IsValid())
            {
                ConfirmationDialog.Show(
                    "Configuration Required",
                    "Configuration is required before using Starfield Load Order Keeper. The application will now exit.",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
                WpfApplication.Current?.Shutdown();
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

            await CheckPluginsFileAsync();
        }

        private bool CanFixLoadOrder() => Config.IsValid() && RefExists && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanCreateReference))]
        private async Task CreateReferenceAsync()
        {
            IsBusy = true;
            AddStatusMessage("Creating reference file...", StatusMessageType.Info);

            try
            {
                await FileService.CreateReferenceFileAsync(Config);
                RefExists = true;
                AddStatusMessage("Reference created successfully! You can now fix the load order.", StatusMessageType.Success);
            }
            catch (Exception ex)
            {
                AddStatusMessage($"ERROR: {ex.Message}", StatusMessageType.Error);
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to create reference: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
            }
            finally
            {
                IsBusy = false;
            }

            await CheckPluginsFileAsync();
        }

        private bool CanCreateReference() => Config.IsValid() && !IsBusy;
 
        partial void OnRefExistsChanged(bool value)
        {
            ReferenceButtonText = value ? "Update reference file" : "Create Reference";
            ConfigurePluginsMonitor();
        }
 
        partial void OnConfigChanged(AppConfigModel value)
        {
            ConfigurePluginsMonitor();
            NotifyFileCommandsCanExecuteChanged();
            PlayGameCommand?.NotifyCanExecuteChanged();
            ShowDiffCommand?.NotifyCanExecuteChanged();
            UpdatePlayButtonText();
        }

        partial void OnIsBusyChanged(bool value)
        {
            NotifyFileCommandsCanExecuteChanged();
            PlayGameCommand?.NotifyCanExecuteChanged();
            ShowDiffCommand?.NotifyCanExecuteChanged();
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

        private void PlayGame()
        {
            var gamePath = Config.StarfieldGamePath;
            if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
            {
                ShowError("Game folder is not configured or does not exist.");
                return;
            }

            string executablePath = HasSfseExecutable()
                ? Path.Combine(gamePath, "sfse_loader.exe")
                : Path.Combine(gamePath, "starfield.exe");

            if (!File.Exists(executablePath))
            {
                ShowError($"Unable to find executable: {executablePath}");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = gamePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to launch Starfield: {ex.Message}");
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
                RefExists = FileService.DoesReferenceFileExist(Config);
                var referenceResult = await EnsureReferenceFileExistsAsync();
                if (referenceResult == ReferenceInitializationResult.AlreadyExists)
                {
                    AddStatusMessage(Config.IsValid()
                        ? "Configuration updated."
                        : "Configuration is invalid.", Config.IsValid() ? StatusMessageType.Success : StatusMessageType.Warning);
                }
                else if (referenceResult == ReferenceInitializationResult.InvalidConfiguration)
                {
                    AddStatusMessage("Configuration is invalid.", StatusMessageType.Warning);
                }
                ConfigurePluginsMonitor();
                await CheckPluginsFileAsync();
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
            var switchVm = new SwitchProfileViewModel(Config);
            var switchWindow = new SwitchProfileWindow(Config)
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = switchVm
            };

            bool? result = switchWindow.ShowDialog();
            if (result == true)
            {
                // Profile was switched, reload state
                await UpdateActiveProfileLabelAsync();
                RefExists = FileService.DoesReferenceFileExist(Config);
                await EnsureReferenceFileExistsAsync();
                ConfigurePluginsMonitor();
                await CheckPluginsFileAsync();
                AddStatusMessage($"Switched to profile '{ActiveProfileLabel}'.", StatusMessageType.Success);
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

            var manageVm = new ManageProfilesViewModel(Config);
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

        private async Task UpdateActiveProfileLabelAsync()
        {
            try
            {
                var activeProfile = await ProfileService.GetActiveProfileAsync(Config);
                ActiveProfileLabel = activeProfile.Label;
            }
            catch
            {
                ActiveProfileLabel = "Default";
            }
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
        }

        private void UpdatePlayButtonText()
        {
            PlayButtonText = HasSfseExecutable()
                ? "Play (SFSE)"
                : "Play (Vanilla)";
        }

        private async Task UpdateChangeCountDisplayAsync(bool hasDifferences)
        {
            if (!hasDifferences)
            {
                UpdateChangeCountDisplay(0);
                return;
            }

            try
            {
                var diffLines = await DiffService.GetPluginsDiffAsync(Config);
                int totalCount = diffLines.Count;
                
                // Include dependent changes in the total count
                foreach (var line in diffLines)
                {
                    totalCount += line.DependentChanges.Count;
                }
                
                UpdateChangeCountDisplay(totalCount);
            }
            catch
            {
                UpdateChangeCountDisplay(0);
            }
        }

        private void UpdateChangeCountDisplay(int changeCount)
        {
            ShowChangesButtonText = changeCount > 0 
                ? $"Manage load order ({changeCount} changes)"
                : "Manage load order";
        }
 
        private async Task ShowDiffAsync()
        {
            if (!Config.IsValid())
            {
                return;
            }

            // If diff window is already open, bring it to front and refresh
            if (_diffWindow != null && _activeDiffDialog != null)
            {
                _diffWindow.Activate();
                _diffWindow.Focus();
                await _activeDiffDialog.RefreshDiffAsync("Manual refresh requested");
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

                _activeDiffDialog = diffViewModel;

                // Handle window closed event to clear references
                _diffWindow.Closed += (s, e) => 
                {
                    _diffWindow = null;
                    _activeDiffDialog = null;
                };

                _diffWindow.Show();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to display changes: {ex.Message}");
            }
        }

        private void ConfigurePluginsMonitor()
        {
            _pluginsMonitorTimer.Interval = GetMonitorInterval();

            if (Config.IsValid() && RefExists)
            {
                if (!_pluginsMonitorTimer.IsEnabled)
                {
                    _pluginsMonitorTimer.Start();
                }
            }
            else
            {
                if (_pluginsMonitorTimer.IsEnabled)
                {
                    _pluginsMonitorTimer.Stop();
                }

                PluginsFileChangedExternally = false;
            }
        }

        private TimeSpan GetMonitorInterval()
        {
            int intervalSeconds = Config.PluginCheckIntervalSeconds > 0
                ? Config.PluginCheckIntervalSeconds
                : 5;
            return TimeSpan.FromSeconds(intervalSeconds);
        }

        private async void OnPluginsMonitorTick(object? sender, EventArgs e)
        {
            await CheckPluginsFileAsync();
        }
 
        private async Task CheckPluginsFileAsync()
        {
            if (_isCheckingPluginsFile || IsBusy)
            {
                return;
            }

            if (!Config.IsValid() || !RefExists)
            {
                PluginsFileChangedExternally = false;
                UpdateChangeCountDisplay(0);
                return;
            }

            _isCheckingPluginsFile = true;

            try
            {
                var comparison = await FileService.ComparePluginsWithReferenceAsync(Config);
                bool hasChanged = comparison.HasDifferences;
                bool signatureChanged = !string.Equals(_lastObservedPluginsSignature, comparison.PluginsSignature, StringComparison.Ordinal);
                _lastObservedPluginsSignature = comparison.PluginsSignature;
                bool sortingRecommendation = false;
                bool hasInsertedMods = false;

                if (hasChanged)
                {
                    sortingRecommendation = await FileService.WouldSortingChangeDiffsAsync(Config);
                    
                    // Check if there are any inserted mods
                    var diffLines = await DiffService.GetPluginsDiffAsync(Config);
                    hasInsertedMods = diffLines.Any(line => line.ChangeType == DiffChangeType.Inserted);
                }

                await UpdateChangeCountDisplayAsync(hasChanged);

                 if (hasChanged != PluginsFileChangedExternally)
                 {
                     PluginsFileChangedExternally = hasChanged;
                     AddStatusMessage(hasChanged
                        ? PluginsModifiedWarningText
                        : GetReadyStatusMessage(), hasChanged ? StatusMessageType.Warning : StatusMessageType.Info);
                    ShowDiffCommand?.NotifyCanExecuteChanged();
                 }

                UpdateSortingRecommendationState(sortingRecommendation, hasInsertedMods);

                if (_activeDiffDialog is not null)
                {
                    bool needsDiffRefresh = signatureChanged || (!hasChanged && _activeDiffDialog.HasDifferences);
                    if (needsDiffRefresh)
                    {
                        string reason = hasChanged
                            ? "Detected new external modifications"
                            : "Plugins.txt now matches the reference";
                        await _activeDiffDialog.RefreshDiffAsync(reason);
                    }
                }
            }
            catch (Exception ex)
            {
                AddStatusMessage($"ERROR: Failed to monitor Plugins.txt: {ex.Message}", StatusMessageType.Error);
                UpdateChangeCountDisplay(0);
            }
            finally
            {
                _isCheckingPluginsFile = false;
            }
        }

        private string GetReadyStatusMessage()
        {
            return Config.IsValid()
                ? "Ready. Configuration is valid."
                : "Configuration is required. Please set paths in the Settings window.";
        }

        private bool HasSfseExecutable()
        {
            var gamePath = Config?.StarfieldGamePath;
            if (string.IsNullOrWhiteSpace(gamePath))
            {
                return false;
            }
 
            string sfsePath = Path.Combine(gamePath, "sfse_loader.exe");
            return File.Exists(sfsePath);
        }

        [RelayCommand(CanExecute = nameof(CanDiscardChanges))]
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

            await CheckPluginsFileAsync();
        }

        private bool CanDiscardChanges() => Config.IsValid() && RefExists && !IsBusy;

        private void UpdateSortingRecommendationState(bool sortingRecommended, bool hasInsertedMods = false)
        {
            if (sortingRecommended)
            {
                if (hasInsertedMods)
                {
                    SortingRecommendationMessage = "?? IMPORTANT: Mods were inserted in the middle of the load order. Sort the list first to move them to the end before making other changes.";
                }
                else
                {
                    SortingRecommendationMessage = "Sorting recommended: run Fix Load Order before resolving other changes.";
                }
                SortingRecommendationActive = true;
            }
            else
            {
                SortingRecommendationMessage = string.Empty;
                SortingRecommendationActive = false;
            }
        }

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
            var statusEntry = new StatusMessageModel(message, DateTime.Now, type);
            
            // Add to beginning of collection (most recent first)
            StatusMessageHistory.Insert(0, statusEntry);
            
            // Keep only the last MaxStatusHistoryCount messages
            while (StatusMessageHistory.Count > MaxStatusHistoryCount)
            {
                StatusMessageHistory.RemoveAt(StatusMessageHistory.Count - 1);
            }

            // Update the current status message for backward compatibility
            StatusMessage = message;
        }

        [RelayCommand]
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var result = await UpdateCheckService.CheckForUpdatesAsync(bypassCache: true);

                if (result.UpdateAvailable)
                {
                    UpdateAvailable = true;
                    UpdateMessage = $"Version {result.LatestVersion} is available!";
                    UpdateInfoBarVisible = true;
                }
                else
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

        private async Task CheckForUpdatesBackgroundAsync()
        {
            try
            {
                var result = await UpdateCheckService.CheckForUpdatesAsync(bypassCache: false);

                if (result.UpdateAvailable)
                {
                    UpdateAvailable = true;
                    UpdateMessage = $"Version {result.LatestVersion} is available!";
                    UpdateInfoBarVisible = true;
                }
            }
            catch
            {
                // Silent failure for background check
            }
        }

        [RelayCommand]
        private void DismissUpdateNotification()
        {
            UpdateInfoBarVisible = false;
        }

        [RelayCommand]
        private void OpenDownloadPage()
        {
            if (string.IsNullOrEmpty(UpdateMessage))
            {
                // Fallback if no update info available
                ShowUpdateCheckErrorDialog();
                return;
            }

            var currentVersion = VersionService.GetApplicationVersion();
            var latestVersion = UpdateMessage.Replace("Version ", "").Replace(" is available!", "").Trim();

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
    }
}
