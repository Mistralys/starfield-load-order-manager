using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
        private readonly DispatcherTimer _pluginsMonitorTimer;
        private bool _isCheckingPluginsFile;
        private DiffDialogViewModel? _activeDiffDialog;
        private string _lastObservedPluginsSignature = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(FixLoadOrderCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private AppConfigModel _config = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(FixLoadOrderCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private bool _refExists;

        [ObservableProperty]
        private string _statusMessage = "Loading settings...";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(FixLoadOrderCommand))]
        [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private string _referenceButtonText = "Create Reference";

        [ObservableProperty]
        private string _fixLoadOrderButtonText = "Fix Load Order";

        private string _playButtonText = "Play (Vanilla)";
        public string PlayButtonText
        {
            get => _playButtonText;
            private set => SetProperty(ref _playButtonText, value);
        }

        public string WindowTitle { get; } = "Starfield Load Order Keeper";
        public string FileMenuHeader { get; } = "_File";
        public string OpenPluginsMenuText { get; } = "Open _Plugins.txt";
        public string OpenReferenceMenuText { get; } = "Open _Reference File";
        public string OpenAppDataFolderMenuText { get; } = "Open _AppData Folder";
        public string OpenGameFolderMenuText { get; } = "Open _Game Folder";
        public string ExitMenuText { get; } = "E_xit";
        public string SettingsMenuHeader { get; } = "_Settings...";
        public string CurrentTargetLabel { get; } = "Current Plugins.txt target:";
        public string TargetPrefixText { get; } = "Target: ";
        public string PluginsModifiedWarningText { get; } = "Plugins.txt was modified outside Load Order Keeper.";
        [ObservableProperty]
        private string _showChangesButtonText = "Manage changes (0)";

        [ObservableProperty]
        private bool _pluginsFileChangedExternally;

        [ObservableProperty]
        private string _sortingRecommendationMessage = string.Empty;

        [ObservableProperty]
        private bool _sortingRecommendationActive;

        [ObservableProperty]
        private string _activeProfileLabel = "Default";

        public string ProfileMenuHeader { get; } = "_Profile";
        public string SwitchProfileMenuText { get; } = "_Switch Profile...";
        public string ManageProfilesMenuText { get; } = "_Manage Profiles...";

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
            ShowDiffCommand = new AsyncRelayCommand(ShowDiffAsync, CanShowDiff);

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
                StatusMessage = GetReadyStatusMessage();
            }

            await UpdateActiveProfileLabelAsync();

            ConfigurePluginsMonitor();
            await CheckPluginsFileAsync();
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
                WpfMessageBox.Show(
                    "Configuration is required before using Starfield Load Order Keeper. The application will now exit.",
                    "Configuration required",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                WpfApplication.Current?.Shutdown();
            }
        }

        [RelayCommand(CanExecute = nameof(CanFixLoadOrder))]
        private async Task FixLoadOrderAsync()
        {
            IsBusy = true;
            StatusMessage = "Applying load order fix...";

            try
            {
                await FileService.ApplyLoadOrderAsync(Config);
                StatusMessage = "Load order successfully applied and fixed!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: {ex.Message}";
                WpfMessageBox.Show($"Failed to fix load order: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            StatusMessage = "Creating reference file...";

            try
            {
                await FileService.CreateReferenceFileAsync(Config);
                RefExists = true;
                StatusMessage = "Reference created successfully! You can now fix the load order.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: {ex.Message}";
                WpfMessageBox.Show($"Failed to create reference: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                    StatusMessage = Config.IsValid()
                        ? "Configuration updated."
                        : "Configuration is invalid.";
                }
                else if (referenceResult == ReferenceInitializationResult.InvalidConfiguration)
                {
                    StatusMessage = "Configuration is invalid.";
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
                StatusMessage = $"Switched to profile '{ActiveProfileLabel}'.";
            }
        }

        [RelayCommand]
        private async Task ManageProfilesAsync()
        {
            var manageVm = new ManageProfilesViewModel(Config);
            var manageWindow = new ManageProfilesWindow(Config)
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = manageVm
            };

            manageWindow.ShowDialog();
            
            // Refresh active profile label in case it was edited
            await UpdateActiveProfileLabelAsync();
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
            StatusMessage = $"ERROR: {message}";
            WpfMessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
 
        private bool CanShowDiff()
        {
            return PluginsFileChangedExternally && Config.IsValid() && !IsBusy;
        }
 
        private async Task ShowDiffAsync()
        {
            if (!Config.IsValid())
            {
                return;
            }

            try
            {
                var diffLines = await DiffService.GetPluginsDiffAsync(Config);
                if (diffLines.Count == 0)
                {
                    WpfMessageBox.Show("No differences detected.", "Show Changes", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    PluginsFileChangedExternally = false;
                    return;
                }

                var diffViewModel = new DiffDialogViewModel(diffLines, this);
                var diffWindow = new DiffWindow
                {
                    Owner = WpfApplication.Current?.MainWindow,
                    DataContext = diffViewModel
                };

                _activeDiffDialog = diffViewModel;
                try
                {
                    diffWindow.ShowDialog();
                }
                finally
                {
                    _activeDiffDialog = null;
                }
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

                if (hasChanged)
                {
                    sortingRecommendation = await FileService.WouldSortingChangeDiffsAsync(Config);
                }

                await UpdateChangeCountDisplayAsync(hasChanged);

                 if (hasChanged != PluginsFileChangedExternally)
                 {
                     PluginsFileChangedExternally = hasChanged;
                     StatusMessage = hasChanged
                        ? PluginsModifiedWarningText
                        : GetReadyStatusMessage();
                    ShowDiffCommand?.NotifyCanExecuteChanged();
                 }

                UpdateSortingRecommendationState(sortingRecommendation);

                if (_activeDiffDialog is not null && signatureChanged)
                {
                    string reason = hasChanged
                        ? "Detected new external modifications"
                        : "Plugins.txt now matches the reference";
                    await _activeDiffDialog.RefreshDiffAsync(reason);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: Failed to monitor Plugins.txt: {ex.Message}";
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
                UpdateChangeCountDisplay(diffLines.Count);
            }
            catch
            {
                UpdateChangeCountDisplay(0);
            }
        }

        private void UpdateChangeCountDisplay(int changeCount)
        {
            SetProperty(ref _showChangesButtonText, $"Manage changes ({changeCount})", "ShowChangesButtonText");
         }
 
        [RelayCommand(CanExecute = nameof(CanDiscardChanges))]
        private async Task DiscardChangesAsync()
        {
            IsBusy = true;
            StatusMessage = "Discarding load order changes...";

            try
            {
                await FileService.DiscardChangesAsync(Config);
                StatusMessage = "Plugins.txt restored from reference.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: {ex.Message}";
                WpfMessageBox.Show($"Failed to discard changes: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }

            await CheckPluginsFileAsync();
        }

        private bool CanDiscardChanges() => Config.IsValid() && RefExists && !IsBusy;

        private void UpdateSortingRecommendationState(bool sortingRecommended)
        {
            if (sortingRecommended)
            {
                SortingRecommendationMessage = "Sorting recommended: run Fix Load Order before resolving other changes.";
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
                StatusMessage = $"Plugins.txt not found at {pluginsPath}. Unable to create reference automatically.";
                return ReferenceInitializationResult.MissingPluginsFile;
            }

            try
            {
                StatusMessage = "No reference file found. Creating one from current Plugins.txt...";
                await FileService.CreateReferenceFileAsync(Config);
                RefExists = true;
                StatusMessage = "Reference file created automatically from current Plugins.txt.";
                return ReferenceInitializationResult.Created;
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: Failed to create reference automatically: {ex.Message}";
                return ReferenceInitializationResult.Failed;
            }
        }
    }
}
