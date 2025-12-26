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

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(FixLoadOrderCommand))]
        private AppConfigModel _config = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(FixLoadOrderCommand))]
        private bool _refExists;

        [ObservableProperty]
        private string _statusMessage = "Loading settings...";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(FixLoadOrderCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private string _referenceButtonText = "Create Reference";

        [ObservableProperty]
        private bool _pluginsFileChangedExternally;

        public IRelayCommand OpenPluginsFileCommand { get; }
        public IRelayCommand OpenReferenceFileCommand { get; }
        public IRelayCommand OpenAppDataFolderCommand { get; }
        public IRelayCommand OpenGameFolderCommand { get; }

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

            _ = LoadInitialStateAsync();
        }

        private async Task LoadInitialStateAsync()
        {
            Config = await SettingsService.LoadSettingsAsync();
            RefExists = FileService.DoesReferenceFileExist(Config);

            StatusMessage = GetReadyStatusMessage();

            ConfigurePluginsMonitor();
            await CheckPluginsFileAsync();
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
        }

        partial void OnIsBusyChanged(bool value)
        {
            NotifyFileCommandsCanExecuteChanged();
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

        private bool CanAccessAppDataPath()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Config?.StarfieldAppDataPath);
        }

        private bool CanAccessGamePath()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Config?.StarfieldGamePath);
        }
 
        [RelayCommand]
        private async Task OpenSettingsAsync()
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
                StatusMessage = Config.IsValid()
                    ? "Configuration updated."
                    : "Configuration is invalid.";
                ConfigurePluginsMonitor();
                await CheckPluginsFileAsync();
            }
        }

        [RelayCommand]
        private void ExitApplication()
        {
            WpfApplication.Current?.Shutdown();
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

        private string GetReadyStatusMessage()
        {
            return Config.IsValid()
                ? "Ready. Configuration is valid."
                : "Configuration is required. Please set paths in the Settings window.";
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
                return;
            }

            _isCheckingPluginsFile = true;

            try
            {
                bool hasChanged = await FileService.HasPluginsFileChangedAsync(Config);
                if (hasChanged != PluginsFileChangedExternally)
                {
                    PluginsFileChangedExternally = hasChanged;
                    StatusMessage = hasChanged
                        ? "Plugins.txt was modified outside Load Order Keeper."
                        : GetReadyStatusMessage();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: Failed to monitor Plugins.txt: {ex.Message}";
            }
            finally
            {
                _isCheckingPluginsFile = false;
            }
        }
    }
}
