using System;
using System.Threading.Tasks;
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

        public MainViewModel()
        {
            _ = LoadInitialStateAsync();
        }

        private async Task LoadInitialStateAsync()
        {
            Config = await SettingsService.LoadSettingsAsync();
            RefExists = FileService.DoesReferenceFileExist(Config);

            StatusMessage = Config.IsValid()
                ? "Ready. Configuration is valid."
                : "Configuration is required. Please set paths in the Settings window.";
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
        }

        private bool CanCreateReference() => Config.IsValid() && !IsBusy;

        partial void OnRefExistsChanged(bool value)
        {
            ReferenceButtonText = value ? "Update reference file" : "Create Reference";
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
            }
        }

        [RelayCommand]
        private void ExitApplication()
        {
            WpfApplication.Current?.Shutdown();
        }
    }
}
